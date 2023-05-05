using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using ajivac_lib;
using ajivac_lib.AST;
using ajivac_lib.Visitor;
using ajivac_llvm;
using NativeResolver = ajivac_lib.ContextualAnalyzer.HelperStructs.NativeResolver;

namespace ajivac_il;

public struct IlFrame
{
    public static IlFrame Empty;
    public ILGenerator Il => Method.GetILGenerator();
    public MethodBuilder Method;
    public Dictionary<LocalVariableDeclaration, LocalBuilder> LocalVariableDeclarationToDynamicLocal = new();

    public Label BreakLabel;
    public Label ContinueLabel;

    public int Allocations;

    public IlFrame()
    {
        throw new UnreachableException("This constructor should never be called");
    }

    public IlFrame(MethodBuilder method)
    {
        Method = method;
        BreakLabel = default;
        ContinueLabel = default;
    }
}
public class ILCodeGenerator : AstVisitorBase<IlFrame, IlFrame>
{
    private readonly Interpreter _interpreter;
    private readonly AssemblyBuilder _assembly;
    private readonly TypeBuilder _type;
    private IlFrame _mainMethodFrame;

    private readonly Dictionary<Prototype, MethodInfo> _functionDefinitionToDynamicMethod = new();
    private Type? _result;

    public ILCodeGenerator(string assemblyName, Interpreter interpreter)
    {
        _interpreter = interpreter;
        _assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.RunAndCollect);
        _type = _assembly.DefineDynamicModule("MainModule").DefineType("Program");
        _mainMethodFrame = new IlFrame(_type.DefineMethod("Main", MethodAttributes.Public | MethodAttributes.Static));
    }

    public void Save(string resultDll)
    {
        if (_result is null) Finish();
        var assembly = Assembly.GetAssembly(_result);
        var generator = new Lokad.ILPack.AssemblyGenerator();
        generator.GenerateAssembly(assembly, resultDll);
    }

    public void Run()
    {
        if (_result is null) Finish();
        _result.GetMethod("Main")?.Invoke(null, null);
    }

    [MemberNotNull(nameof(_result))]
    public void Finish()
    {
        if (_result is not null) return;
        while (_mainMethodFrame.Allocations > 0)
        {
            _mainMethodFrame.Il.Emit(OpCodes.Pop);
            _mainMethodFrame.Allocations--;
        }
        _mainMethodFrame.Il.Emit(OpCodes.Ret);
        _result = _type.CreateType();
    }

    public void Visit(RootNode node) => VisitChildren(node, ref IlFrame.Empty);

    public override IlFrame Visit(FunctionDefinition node, ref IlFrame arg)
    {
        var frame = new IlFrame(_type.DefineMethod(node.Signature.Name, MethodAttributes.Public | MethodAttributes.Static, ConversionHelpers.BuildInTypeToType(node.Signature.ReturnType),
            node.Signature.Parameters.Select(p => ConversionHelpers.BuildInTypeToType(p.TypeReference)).ToArray()
        ));
        _functionDefinitionToDynamicMethod.Add(node.Signature, frame.Method);
        VisitChildren(node, ref frame);
        frame.Il.Emit(OpCodes.Ret); //catch missing return statement
        return frame;
    }

    public override IlFrame Visit(ParameterDeclaration node, ref IlFrame arg)
    {
        var parameter = arg.Method.DefineParameter(node.Index + 1,
            node.Initializer is not null ? ParameterAttributes.HasDefault : ParameterAttributes.None,
            node.Name);
        if (node.Initializer is not null)
            parameter.SetConstant(Eval(node.Initializer));
        return arg;
    }

    public override IlFrame Visit(LiteralExpression node, ref IlFrame arg)
    {
        var il = arg.Il;
        switch (node.TypeReference.Kind)
        {
            case TypeKind.Unknown:
                return arg; //
            case TypeKind.Bit:
                il.Emit(node.Value == "true" ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                break;
            case TypeKind.U32:
                il.Emit(OpCodes.Ldc_I4, int.Parse(node.Value));
                break;
            case TypeKind.U64:
                il.Emit(OpCodes.Ldc_I8, long.Parse(node.Value));
                break;
            case TypeKind.I32:
                il.Emit(OpCodes.Ldc_I4, int.Parse(node.Value));
                break;
            case TypeKind.I64:
                il.Emit(OpCodes.Ldc_I8, long.Parse(node.Value));
                break;
            case TypeKind.F32:
                il.Emit(OpCodes.Ldc_R4, float.Parse(node.Value));
                break;
            case TypeKind.F64:
                il.Emit(OpCodes.Ldc_R8, double.Parse(node.Value));
                break;
            case TypeKind.Str:
                il.Emit(OpCodes.Ldstr, node.Value);
                break;
            case TypeKind.Chr:
                il.Emit(OpCodes.Ldc_I4, node.Value[0]);
                break;
            case TypeKind.Void:
                return arg;
            case TypeKind.UserDefinedBegin:
                return arg;
            default:
                throw new ArgumentOutOfRangeException();
        }
        arg.Allocations++;
        return arg;
    }

    public override IlFrame Visit(IdentifierExpression node, ref IlFrame arg)
    {
        var il = arg.Il;
        if (node.Definition is ParameterDeclaration para)
            il.Emit(OpCodes.Ldarg_S, para.Index);
        else
            il.Emit(OpCodes.Ldloc, arg.LocalVariableDeclarationToDynamicLocal[node.Definition!]);
        arg.Allocations++;
        return arg;
    }

    public override IlFrame Visit(LocalVariableDeclaration node, ref IlFrame arg)
    {
        var il = arg.Il;
        var local = il.DeclareLocal(ConversionHelpers.BuildInTypeToType(node.TypeReference));
        arg.LocalVariableDeclarationToDynamicLocal.Add(node, local);
        if (node.Initializer is not null)
        {
            node.Initializer.Accept(this, ref arg);
            il.Emit(OpCodes.Stloc, local);
        }
        return arg;
    }

    public override IlFrame Visit(BinaryExpression node, ref IlFrame arg)
    {
        var il = arg.Il;
        node.Left.Accept(this, ref arg);
        node.Right.Accept(this, ref arg);
        il.Emit(node.Operator switch {
            BinaryOperator.Plus => OpCodes.Add,
            BinaryOperator.Minus => OpCodes.Sub,
            BinaryOperator.Multiply => OpCodes.Mul,
            BinaryOperator.Divide => OpCodes.Div,
            BinaryOperator.Modulo => OpCodes.Rem,
            BinaryOperator.Assign => OpCodes.Starg_S,
            BinaryOperator.Equal => OpCodes.Ceq,
            BinaryOperator.NotEqual => OpCodes.Ceq, //invert after
            BinaryOperator.Greater => OpCodes.Cgt,
            BinaryOperator.GreaterEqual => OpCodes.Clt, //invert after
            BinaryOperator.Less => OpCodes.Clt,
            BinaryOperator.LessEqual => OpCodes.Cgt, //invert after
            BinaryOperator.And => OpCodes.And,
            BinaryOperator.Or => OpCodes.Or,
            BinaryOperator.Xor => OpCodes.Xor,
            BinaryOperator.ShiftLeft => OpCodes.Shl,
            BinaryOperator.ShiftRight => OpCodes.Shr,
            BinaryOperator.Question => throw new Exception(),
            BinaryOperator.Colon => throw new Exception(),
            _ => throw new ArgumentOutOfRangeException()
        });
        if (node.Operator is BinaryOperator.NotEqual or BinaryOperator.LessEqual or BinaryOperator.GreaterEqual)
        {
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ceq);
        }
        arg.Allocations++;
        return arg;
    }

    public override IlFrame Visit(UnaryExpression node, ref IlFrame arg)
    {
        var il = arg.Il;
        node.Operand.Accept(this, ref arg);
        switch (node.Operator)
        {
            case UnaryOperator.Not:
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ceq);
                arg.Allocations++;
                break;
            case UnaryOperator.Negate:
                il.Emit(OpCodes.Not);
                arg.Allocations++;
                break;
            case UnaryOperator.Positive:
                il.Emit(OpCodes.Nop);
                break;
            case UnaryOperator.Negative:
                il.Emit(OpCodes.Neg);
                arg.Allocations++;
                break;
            case UnaryOperator.Increment:
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Add);
                arg.Allocations++;
                break;
            case UnaryOperator.Decrement:
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Sub);
                arg.Allocations++;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        return arg;
    }

    public override IlFrame Visit(FunctionCallExpression node, ref IlFrame arg)
    {
        return CallFunction(ref arg, node.Arguments, FindCallTarget(node));
    }

    public override IlFrame Visit(Prototype node, ref IlFrame arg)
    {
        if (node.IsExtern)
        {
            _functionDefinitionToDynamicMethod.Add(node, NativeResolver.SResolve(
                node.Name,
                node.Parameters.Select(x => ConversionHelpers.BuildInTypeToType(x.TypeReference)).ToArray(),
                node.ReturnType
            ));
        }
        else
        {
            VisitChildren(node, ref arg);
        }
        return arg;
    }

    public override IlFrame Visit(IfExpression node, ref IlFrame arg)
    {
        var il = arg.Il;
        node.Condition.Accept(this, ref arg);
        var elseLabel = il.DefineLabel();
        var endLabel = il.DefineLabel();
        il.Emit(OpCodes.Brfalse, elseLabel);
        il.BeginScope();
        node.ThenExpression.Accept(this, ref arg);
        il.EndScope();
        il.Emit(OpCodes.Br, endLabel);
        il.MarkLabel(elseLabel);
        il.BeginScope();
        node.ElseExpression?.Accept(this, ref arg);
        il.EndScope();
        il.MarkLabel(endLabel);
        il.Emit(OpCodes.Nop); //if tair is no instruction at the label the program is invalid 
        return arg;
    }

    public override IlFrame Visit(AssignmentExpression node, ref IlFrame arg)
    {
        var il = arg.Il;
        node.AssignmentValue?.Accept(this, ref arg);
        il.Emit(OpCodes.Stloc, arg.LocalVariableDeclarationToDynamicLocal[node.Definition!]);
        arg.Allocations--;
        return arg;
    }

    public override IlFrame Visit(BreakStatement node, ref IlFrame arg)
    {
        var il = arg.Il;
        il.Emit(OpCodes.Br, arg.BreakLabel);
        return arg;
    }

    public override IlFrame Visit(ReturnStatement node, ref IlFrame arg)
    {
        var il = arg.Il;
        node.Expression?.Accept(this, ref arg);
        //todo cleanup stack?
        il.Emit(OpCodes.Ret);
        return arg;
    }

    public override IlFrame Visit(ContinueStatement node, ref IlFrame arg)
    {
        var il = arg.Il;
        il.Emit(OpCodes.Br, arg.ContinueLabel);
        return arg;
    }

    public override IlFrame Visit(WhileStatement node, ref IlFrame arg)
    {
        var il = arg.Il;
        var breakLabel = il.DefineLabel();
        var continueLabel = il.DefineLabel();
        il.MarkLabel(continueLabel);
        node.Condition.Accept(this, ref arg);
        il.Emit(OpCodes.Brfalse, breakLabel);
        var ilFrame = arg with {
            BreakLabel = breakLabel,
            ContinueLabel = continueLabel
        };
        node.Body.Accept(this, ref ilFrame);
        il.Emit(OpCodes.Br, continueLabel);
        il.MarkLabel(breakLabel);
        il.Emit(OpCodes.Nop);
        return arg;
    }

    public override IlFrame Visit(ForStatement node, ref IlFrame arg)
    {
        var il = arg.Il;
        var breakLabel = il.DefineLabel();
        var continueLabel = il.DefineLabel();
        node.Initializer?.Accept(this, ref arg);
        il.MarkLabel(continueLabel);
        node.Condition?.Accept(this, ref arg);
        il.Emit(OpCodes.Brfalse, breakLabel);
        var ilFrame = arg with {
            BreakLabel = breakLabel,
            ContinueLabel = continueLabel
        };
        node.Body.Accept(this, ref ilFrame);
        node.Increment?.Accept(this, ref arg);
        il.Emit(OpCodes.Br, continueLabel);
        il.MarkLabel(breakLabel);
        il.Emit(OpCodes.Nop);
        return arg;
    }

    protected override IlFrame VisitChildren(IAstNode? node, ref IlFrame arg)
    {
        if (node is null)
            return arg;
        foreach (var child in node.Children)
        {
            child.Accept(this, ref arg);
        }
        return arg;
    }

    public override IlFrame Visit(AttributeEaSt node, ref IlFrame arg)
    {
        if (node.Operand is not null)
        {
            var res = node.Operand.Accept(this, ref arg);
            if (node.Name == "entry")
            {
                CallFunction(ref _mainMethodFrame, node.Arguments, res.Method);
            }
        }

        return arg;
    }

#region helpers

    private IlFrame CallFunction(ref IlFrame arg, IReadOnlyList<IExpression>? args, MethodInfo target)
    {
        if (args is not null)
        {
            foreach (var argument in args)
                argument.Accept(this, ref arg);
            arg.Allocations -= args.Count;
        }
        arg.Il.EmitCall(OpCodes.Call, target, null);
        if (target.ReturnType != typeof(void))
            arg.Allocations++;
        return arg;
    }

    private object? Eval(IExpression nodeInitializer) => _interpreter.EvaluateExpression(nodeInitializer);

    private MethodInfo FindCallTarget(FunctionCallExpression node)
    {
        return node.Definition switch {
            { Signature.IsExtern: true } externDefinition => NativeResolver.SResolve(node.CalleeName, externDefinition.Signature.Parameters.Select(x => ConversionHelpers.BuildInTypeToType(x.TypeReference)).ToArray(), externDefinition.Signature.ReturnType),
            { Signature: { } signature } => _functionDefinitionToDynamicMethod[signature],
            _ => throw new Exception()
        };
    }

#endregion
}
