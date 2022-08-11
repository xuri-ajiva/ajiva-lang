using System.Data;
using System.Reflection;
using ajivac_lib;
using ajivac_lib.AST;

namespace ajivac_llvm;

public class Interpreter
{
    private readonly Action<string> logger;
    private readonly Dictionary<string, MethodInfo> nativeMethods = new();
    private readonly Dictionary<string, FunctionDefinition> functionDefinitions = new();
    private readonly Stack<InternStackFrame> stack = new();

    public Interpreter(Action<string> logger)
    {
        this.logger = logger;
    }

    public class InternStackFrame
    {
        public Dictionary<string, object> Variables { get; set; } = new();
        public object? Ret { get; set; }
    }

    public void Run(IAstNode root)
    {
        var s = BeginTime();
        Evaluate(root);
        EndTime(nameof(Run),s);
    }

    private void Evaluate(IAstNode node)
    {
        switch (node)
        {
            case AttributeEaSt attributeEaSt:
                if (attributeEaSt.Name == "entry")
                {
                    PushStack();
                    Evaluate((attributeEaSt.Operand as FunctionDefinition)!.Body);
                }
                break;
            case RootNode rootNode:
                foreach (var rootChild in rootNode.Children)
                {
                    Evaluate(rootChild);
                }
                break;
            case LocalVariableDeclaration localVariableDeclaration:
                SetVariable(localVariableDeclaration.Name, EvaluateExpression(localVariableDeclaration.Initializer));
                break;
            case IfExpression ifExpression:
                {
                    var condition = EvaluateExpression(ifExpression.Condition);
                    logger($"Condition: ({ifExpression.Condition.Span}) {condition}");
                    Evaluate((condition is true
                        ? ifExpression.ThenExpression
                        : ifExpression.ElseExpression)!);
                }
                break;
            case IExpression expression:
                EvaluateExpression(expression);
                break;
            case ReturnStatement returnStatement:
                stack.Peek().Ret = EvaluateExpression(returnStatement.Expression);
                break;
            case ForStatement(_, var initializer, var expression, var increment, var body):
                {
                    if (initializer is LocalVariableDeclaration localVariableDeclaration)
                    {
                        SetVariable(localVariableDeclaration.Name, EvaluateExpression(localVariableDeclaration.Initializer));
                    }
                    else
                    {
                        Evaluate(initializer);
                    }
                    var cond = EvaluateExpression(expression);
                    while (cond is true)
                    {
                        Evaluate(body);
                        Evaluate(increment);
                        cond = EvaluateExpression(expression);
                    }
                }
                break;
            case WhileStatement whileStatement:
                {
                    var condition = EvaluateExpression(whileStatement.Condition);
                    while (condition is true)
                    {
                        Evaluate(whileStatement.Body);
                        condition = EvaluateExpression(whileStatement.Condition);
                    }
                    break;
                }
            case Prototype:
            case FunctionDefinition:
                break;
            default:
                throw new NotImplementedException(node.ToString());
        }
    }

    private object? EvaluateFunctionCall(FunctionCallExpression functionCallExpression)
    {
        logger($"calling {functionCallExpression.Callee.Name} with {functionCallExpression.Arguments.Count} arguments");
        var func = functionDefinitions.GetValueOrDefault(functionCallExpression.Callee.Name);
        if (func is null) throw new Exception($"function {functionCallExpression.Callee.Name} not found");
        Dictionary<string, object> parameters = new();
        for (var index = 0; index < functionCallExpression.Callee.Parameters.Count; index++)
        {
            var value = EvaluateExpression(functionCallExpression.Arguments[index]);
            parameters[functionCallExpression.Callee.Parameters[index].Name] = value;
        }
        stack.Push(new InternStackFrame { Variables = parameters });
        Evaluate(func.Body);
        return PopStack();
    }

    private object? EvaluateExtern(FunctionCallExpression functionCallExpression)
    {
        var callee = functionCallExpression.Callee;
        var args = functionCallExpression.Arguments.Select(EvaluateExpression).ToArray();
        var method = nativeMethods[callee.Name];
        logger($"calling {functionCallExpression.Callee.Name}::{method.ToString()} with ({string.Join(", ", args)})");
        return method.Invoke(null, args);
    }

    private object EvaluateExpression(IExpression node)
    {
        switch (node)
        {
            case UnaryExpression or BinaryExpression:
                return EvaluateBinUnary(node);
            case ValueExpression<int> i32:
                return i32.Value;
            case ValueExpression<long> i64:
                return i64.Value;
            case ValueExpression<uint> u32:
                return u32.Value;
            case ValueExpression<ulong> u64:
                return u64.Value;
            case ValueExpression<float> f32:
                return f32.Value;
            case ValueExpression<double> f64:
                return f64.Value;
            case ValueExpression<bool> bit:
                return bit.Value;
            case ValueExpression<string> str:
                return str.Value;
            case ValueExpression<char> chr:
                return chr.Value;
            case FunctionCallExpression functionCallExpression:
                return (functionCallExpression.Callee.IsExtern
                    ? EvaluateExtern(functionCallExpression)
                    : EvaluateFunctionCall(functionCallExpression))!;
            case IdentifierExpression variableExpression:
                return GetVariable(variableExpression.Identifier);
            case AssignmentExpression assignmentExpression:
                var value = EvaluateExpression(assignmentExpression.AssignmentValue!);
                SetVariable(assignmentExpression.Name, value);
                return value;
        }
        throw new NotImplementedException(node.ToString());
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
                            SetVariable(binaryExpression.Left.ToString(), right);
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

    private object GetVariable(string name)
    {
        var frame = stack.Peek();
        if (frame.Variables.ContainsKey(name))
        {
            logger($"get variable {name} with {frame.Variables[name]}");
            return frame.Variables[name];
        }
        throw new Exception($"Variable {name} not found");
    }

    private void SetVariable(string name, object value)
    {
        var frame = stack.Peek();
        logger($"set variable {name} to {value}");
        frame.Variables[name] = value;
    }

    private void PushStack()
    {
        logger("new stack frame");
        stack.Push(new InternStackFrame());
    }

    private object? PopStack()
    {
        logger("pop stack frame");
        var ret = stack.Pop().Ret;
        if (ret is not null)
            logger($"return {ret}");
        return ret;
    }

    public void Load(RuntimeStateHolder parserRuntimeState)
    {
        var s = BeginTime();
        foreach (var (key, proto) in parserRuntimeState.NativeFunctionDeclarations)
        {
            var parameterTypes = new Type[proto.Parameters.Count];
            for (var i = 0; i < parameterTypes.Length; i++)
                parameterTypes[i] = ResolveType(proto.Parameters[i].TypeReference);
            var name = proto.Name;
            if (name.Contains('.'))
            {
                var methodName = name[(name.LastIndexOf('.') + 1)..];
                var typeName = name[..name.LastIndexOf('.')];
                if (Type.GetType(typeName + ", " + typeName) is not { } type)
                    throw new Exception($"Native type {typeName} not found");
                if (type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Default | BindingFlags.Public, parameterTypes) is not { } m)
                    throw new Exception($"Native method {methodName} not found in type {typeName}");
                if (m.ReturnType != ResolveType(proto.ReturnType))
                    throw new Exception($"Native method {methodName} in type {typeName} has wrong return type: {m.ReturnType} != {ResolveType(proto.ReturnType)}");
                nativeMethods.Add(key, m);
            }
            else
            {
                if (typeof(Interpreter).GetMethod(name, BindingFlags.Static | BindingFlags.Default | BindingFlags.Public, parameterTypes) is not { } mi)
                    throw new Exception($"Native method {name} not found");
                if (mi.ReturnType != ResolveType(proto.ReturnType))
                    throw new Exception($"Native Interpreter method {name} has wrong return type: {mi.ReturnType} != {ResolveType(proto.ReturnType)}");
                nativeMethods.Add(key, mi);
            }
        }

        foreach (var (key, value) in parserRuntimeState.FunctionDefinitions)
        {
            functionDefinitions.Add(key, value);
        }

        logger("Resolved NativeMathods:");
        foreach (var (key, value) in nativeMethods)
        {
            logger($"{key} -> {value}");
        }

        logger("Resolved FunctionDefinitions:");
        foreach (var (key, value) in functionDefinitions)
        {
            logger($"{key} -> {value}");
        }
        EndTime(nameof(Load),s);
    }

    private static Type ResolveType(TypeReference typeReference)
    {
        if (typeReference is BuildInTypeReference buildInTypeReference)
        {
            return buildInTypeReference.Type switch {
                BuildInType.I32 => typeof(int),
                BuildInType.I64 => typeof(long),
                BuildInType.U32 => typeof(uint),
                BuildInType.U64 => typeof(ulong),
                BuildInType.F32 => typeof(float),
                BuildInType.F64 => typeof(double),
                BuildInType.Bit => typeof(bool),
                BuildInType.Void => typeof(void),
                BuildInType.Chr => typeof(char),
                BuildInType.Str => typeof(string),
                BuildInType.Unknown => typeof(object), //todo is this correct?
                _ => throw new Exception("Unknown type"),
            };
        }
        return typeof(object);
    }

    long BeginTime() => DateTime.Now.Ticks;

    void EndTime(string name, long start)
    {
        var end = DateTime.Now.Ticks;
        Console.WriteLine($"{name} took {(end - start) / 10000}ms");
    }

    public static void Log(int message) => LogCore(message.ToString());
    public static void Log(string message) => LogCore(message);
    private static void LogCore(object message) => Console.WriteLine("Log: " + message);
}
