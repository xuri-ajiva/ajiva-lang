using System.Data;
using System.Reflection;
using ajivac_lib;
using ajivac_lib.AST;
using ajivac_lib.Semantics;

namespace ajivac_llvm;

public class Interpreter
{
    private readonly Action<string> logger;
    private StackManager _stackManager;
    private FunctionResolver _functionResolver;

    public Interpreter(Action<string> logger)
    {
        this.logger = logger;
        _stackManager = new StackManager(x => logger("Stack: " + x));
        _functionResolver = new FunctionResolver(x => logger("Func: " + x));
    }

    public void Run(IAstNode root)
    {
        Evaluate(root);
    }

    private IAstNode? Evaluate(IAstNode? node)
    {
        switch (node)
        {
            case AttributeEaSt attributeEaSt:
                if (attributeEaSt.Name != "entry")
                {
                    logger($"Unknown attribute {attributeEaSt.Name}");
                    return null;
                }
                _stackManager.Push();
                if (attributeEaSt.Operand is FunctionDefinition fd)
                {
                    var ret = CallFunction(fd.Signature, attributeEaSt.Arguments ?? new List<IExpression>());
                    if (ret is not null)
                    {
                        Console.WriteLine("Top Level Function " + fd.Signature.Name + "("+
                                          attributeEaSt.Arguments?.Select(x => x.Span.GetValue()).Aggregate((x, y) => x + ", " + y) 
                                          + ") returned:\n" + ret);
                    }
                    return null;
                }
                if (attributeEaSt.Operand is not null)
                {
                    return Evaluate(attributeEaSt.Operand);
                }
                throw new("Invalid Entry Point");
            case RootNode rootNode:
                foreach (var rootChild in rootNode.Children)
                {
                    var ret = Evaluate(rootChild);
                    if (IsControlFlow(ret))
                        return ret;
                }
                return null;
            case LocalVariableDeclaration localVariableDeclaration:
                _stackManager.CreateVariable(localVariableDeclaration.Name, EvaluateExpression(localVariableDeclaration.Initializer));
                return null;
            case IfExpression ifExpression:
                {
                    var condition = EvaluateExpression(ifExpression.Condition);
                    logger($"Condition: ({ifExpression.Condition.Span}) {condition}");
                    return Evaluate((condition is true
                        ? ifExpression.ThenExpression
                        : ifExpression.ElseExpression));
                }

            case BreakStatement:
            case ContinueStatement:
            case ReturnStatement:
                return node;
            case ForStatement(_, var initializer, var expression, var increment, var body):
                if (initializer is LocalVariableDeclaration lvd)
                {
                    _stackManager.CreateVariable(lvd.Name, EvaluateExpression(lvd.Initializer));
                }
                else
                {
                    Evaluate(initializer);
                }
                while (EvaluateExpression(expression) is true)
                {
                    var statement = Evaluate(body);
                    if (IsControlFlow(statement))
                    {
                        switch (statement)
                        {
                            case ReturnStatement:
                                return statement;
                            case BreakStatement:
                                return null;
                            case ContinueStatement:
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                    }
                    Evaluate(increment);
                }
                return null;
            case WhileStatement whileStatement:
                while (EvaluateExpression(whileStatement.Condition) is true)
                {
                    var statement = Evaluate(whileStatement.Body);
                    if (IsControlFlow(statement))
                    {
                        switch (statement)
                        {
                            case ReturnStatement:
                                return statement;
                            case BreakStatement:
                                return null;
                            case ContinueStatement:
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                    }
                }
                return null;
            case Prototype prototype:
                _functionResolver.AddFunction(prototype);
                return null;
            case FunctionDefinition function:
                _functionResolver.AddFunction(function);
                return null;
            case null:
            case EmptyStatement:
                return null;
            case IExpression expression:
                var value = EvaluateExpression(expression);
                if (value is not null)
                    logger($"Value discarding: ({expression.Span}) {value}");
                return null;
            default:
                throw new NotImplementedException(node.ToString());
        }
    }

    private static bool IsControlFlow(IAstNode? ret)
    {
        return ret is ReturnStatement or BreakStatement or ContinueStatement;
    }

    private object? EvaluateFunctionCall(Prototype prototype, object[] arguments)
    {
        var func = _functionResolver.Resolve(prototype);
        Dictionary<string, object> parameters = new Dictionary<string, object>();
        for (var index = 0; index < prototype.Parameters.Count; index++)
        {
            var value = arguments?[index];
            parameters[prototype.Parameters[index].Name] = value;
        }
        _stackManager.Push(new StackManager.InternStackFrame {
            Variables = parameters
        });
        var statement = Evaluate(func.Body);
        if (IsControlFlow(statement))
        {
            switch (statement)
            {
                case BreakStatement:
                case ContinueStatement:
                    throw new("break or continue not allowed in function");
                case ReturnStatement returnStatement:
                    var ret = returnStatement.Expression is not null
                        ? EvaluateExpression(returnStatement.Expression)
                        : null;
                    _stackManager.Pop();
                    return ret;
                default:
                    throw new NotImplementedException();
            }
        }
        if (statement is null && func.Signature.ReturnType.Kind is TypeKind.Void)
        {
            return null;
        }
        throw new("invalid return statement");
    }

    private object? EvaluateExtern(Prototype prototype, object[] arguments)
    {
        var callee = prototype;
        var method = _functionResolver.ResolveExtern(callee);
        return method.Invoke(null, arguments);
    }

    private object? EvaluateExpression(IExpression? node)
    {
        switch (node)
        {
            case UnaryExpression or BinaryExpression:
                return EvaluateBinUnary(node);
            case LiteralExpression literalExpression:
                return literalExpression.TypeReference.Kind switch {
                    TypeKind.I32 => int.Parse(literalExpression.Value),
                    TypeKind.I64 => long.Parse(literalExpression.Value),
                    TypeKind.U32 => uint.Parse(literalExpression.Value),
                    TypeKind.U64 => ulong.Parse(literalExpression.Value),
                    TypeKind.F32 => float.Parse(literalExpression.Value),
                    TypeKind.F64 => double.Parse(literalExpression.Value),
                    TypeKind.Chr => char.Parse(literalExpression.Value),
                    TypeKind.Str => literalExpression.Value,
                    TypeKind.Bit => bool.Parse(literalExpression.Value),
                    TypeKind.Void => null,
                    TypeKind.Unknown => throw new Exception("Unknown type"),
                    _ => throw new ArgumentOutOfRangeException()
                };
            case FunctionCallExpression functionCallExpression:
                return CallFunction(functionCallExpression.CalleeName, functionCallExpression.Arguments);
            case IdentifierExpression variableExpression:
                return _stackManager.GetVariable(variableExpression.Identifier);
            case AssignmentExpression assignmentExpression:
                var value = EvaluateExpression(assignmentExpression.AssignmentValue);
                _stackManager.SetVariable(assignmentExpression.Name, value);
                return value;
            case null:
                return null;
        }
        throw new NotImplementedException(node.ToString());
    }

    private object? CallFunction(string calleeName, List<IExpression> arguments)
    {
        var args = arguments.Select(EvaluateExpression).ToArray();
        var proto = _functionResolver.CallBlank(calleeName, args);
        return CallFunction(proto, args);
    }

    private object? CallFunction(Prototype prototype, IEnumerable<IExpression> arguments)
    {
        return CallFunction(prototype, arguments.Select(EvaluateExpression).ToArray());
    }

    private object? CallFunction(Prototype prototype, object[] arguments)
    {
        return prototype.IsExtern
            ? EvaluateExtern(prototype, arguments)
            : EvaluateFunctionCall(prototype, arguments);
    }

    private object EvaluateBinUnary(IExpression node)
    {
        //todo call operators / check for internal types
        switch (node)
        {
            case UnaryExpression unaryExpression:
                {
                    var value = EvaluateExpression(unaryExpression.Operand);
                    switch (unaryExpression.Operator)
                    {
                        case UnaryOperator.Not:
                            return !(value is true);
                        case UnaryOperator.Negate:
                            return -(int)value;
                        case UnaryOperator.Positive:
                            return +(int)value;
                        case UnaryOperator.Increment:
                            throw new NotImplementedException("Increment");
                        case UnaryOperator.Decrement:
                            throw new NotImplementedException("Decrement");
                    }
                    throw new NotImplementedException();
                }

            case BinaryExpression binaryExpression:
                {
                    var left = EvaluateExpression(binaryExpression.Left);
                    var right = EvaluateExpression(binaryExpression.Right);
                    switch (binaryExpression.Operator)
                    {
                        case BinaryOperator.Assign:
                            _stackManager.SetVariable(binaryExpression.Left.ToString(), right);
                            return right;
                        case BinaryOperator.Plus:
                            return (int)left + (int)right;
                        case BinaryOperator.Minus:
                            return (int)left - (int)right;
                        case BinaryOperator.Multiply:
                            return (int)left * (int)right;
                        case BinaryOperator.Divide:
                            return (int)left / (int)right;
                        case BinaryOperator.Modulo:
                            return (int)left % (int)right;
                        case BinaryOperator.Equal:
                            return (int)left == (int)right;
                        case BinaryOperator.NotEqual:
                            return (int)left != (int)right;
                        case BinaryOperator.Greater:
                            return (int)left > (int)right;
                        case BinaryOperator.GreaterEqual:
                            return (int)left >= (int)right;
                        case BinaryOperator.Less:
                            return (int)left < (int)right;
                        case BinaryOperator.LessEqual:
                            return (int)left <= (int)right;
                        case BinaryOperator.And:
                            return (bool)left && (bool)right;
                        case BinaryOperator.Or:
                            return (bool)left || (bool)right;
                        case BinaryOperator.Xor:
                            return (bool)left ^ (bool)right;
                        case BinaryOperator.ShiftLeft:
                            return (int)left << (int)right;
                        case BinaryOperator.ShiftRight:
                            return (int)left >> (int)right;
                        case BinaryOperator.Question:
                            return (bool)left ? (int)right : (int)left;
                        case BinaryOperator.Colon:
                            return (bool)left ? (int)left : (int)right;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
        }
        throw new NotImplementedException();
    }

    public void Load(RuntimeStateHolder parserRuntimeState)
    {
        _functionResolver.Load(parserRuntimeState);

        _functionResolver.Info();
    }
}
