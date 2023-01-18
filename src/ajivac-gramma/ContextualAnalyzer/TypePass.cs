using ajivac_lib.AST;
using ajivac_lib.ContextualAnalyzer.HelperStructs;
using ajivac_lib.Visitor;

namespace ajivac_lib.ContextualAnalyzer;

public class TypePass : AstAllSubVisitorBase<TypeReference, NonRef>
{
    private readonly Diagnostics _diagnostics;
    private readonly FunctionTable _funcTable;
    private readonly TypeHelper _typeHelper;

    public TypePass(Diagnostics diagnostics)
    {
        _diagnostics = diagnostics;
        _funcTable = new FunctionTable(diagnostics);
        _typeHelper = new TypeHelper(diagnostics);
    }

    public override TypeReference Visit(FunctionDefinition node, ref NonRef arg)
    {
        _funcTable.AddFunction(node);
        return base.Visit(node, ref arg);
    }

    public override TypeReference Visit(Prototype node, ref NonRef arg)
    {
        if (node.IsExtern)
        {
            _funcTable.AddFunction(new FunctionDefinition(node.Span, node, null!));
            return node.TypeReference;
        }
        return base.Visit(node, ref arg);
    }

    public override TypeReference Visit(LiteralExpression node, ref NonRef arg)
    {
        return node.TypeReference;
    }

    public override TypeReference Visit(IdentifierExpression node, ref NonRef arg)
    {
        if (node.Definition is null)
            _diagnostics.ReportRefMissing(node, nameof(node.Definition));

        return node.TypeReference;
    }

    public override TypeReference Visit(BinaryExpression node, ref NonRef arg)
    {
        var leftType = node.Left.Accept(this, ref arg);
        var rightType = node.Right.Accept(this, ref arg);

        return node.TypeReference = _typeHelper.ApplyBinaryOperator(node.Operator, leftType, rightType, node.Span);
    }

    public override TypeReference Visit(UnaryExpression node, ref NonRef arg)
    {
        var operandType = node.Operand.Accept(this, ref arg);
        return node.TypeReference = _typeHelper.ApplyUnaryOperator(node.Operator, operandType, node.Span);
    }

    public override TypeReference Visit(FunctionCallExpression node, ref NonRef arg)
    {
        var argTypes = new List<TypeReference>();
        foreach (var nodeArgument in node.Arguments)
        {
            argTypes.Add(nodeArgument.Accept(this, ref arg));
        }

        if (!_funcTable.TryGetFunction(node.CalleeName, argTypes, null, out var func))
        {
            _diagnostics.ReportFunctionNotFound(node, node.CalleeName, argTypes);
            return TypeReference.Unknown;
        }
        
        node.Definition = func;
        if (func.Parameters.Count != node.Arguments.Count) //todo move to type checker
        {
            _diagnostics.ReportWrongNumberOfArguments(node, func.Parameters.Count, node.Arguments.Count);
        }
        //check argument types
        for (var i = 0; i < node.Arguments.Count; i++)
        {
            var argType = argTypes[i];
            if (argType != func.Parameters[i].TypeReference)
            {
                _diagnostics.ReportWrongArgumentType(node.Arguments[i].Span, func.Parameters[i].TypeReference, argType);
            }
        }
        return node.TypeReference;
    }

    public override TypeReference Visit(AssignmentExpression node, ref NonRef arg)
    {
        var leftType = node.AssignmentValue.Accept(this, ref arg);
        var rightType = node.Definition.TypeReference;

        if (!_typeHelper.IsAssignable(leftType, rightType))
            _diagnostics.ReportCannotAssign(node, leftType, rightType);

        return TypeReference.Void;
    }

    public override TypeReference Visit(ReturnStatement node, ref NonRef arg)
    {
        if (node.Expression is null)
            return TypeReference.Void;
        var type = node.Expression.Accept(this, ref arg);
        if (!_typeHelper.IsAssignable(node.Function.TypeReference, type))
            _diagnostics.ReportCannotAssign(node, node.Function.TypeReference, type);
        return node.TypeReference = type;
    }

    public override TypeReference Visit(IfExpression node, ref NonRef arg)
    {
        var conditionType = node.Condition.Accept(this, ref arg);
        if (conditionType.Kind != TypeKind.Bit)
            _diagnostics.ReportInvalidConditionType(node, conditionType);

        /*var thenType = node.ThenExpression.Accept(this, ref arg);
        var elseType = node.ElseExpression.Accept(this, ref arg);
        //todo populate resultvar
                if (!_typeHelper.IsAssignable(thenType, elseType))
            throw new SyntaxError("Branches of if expression must have the same type", node);

        */

        node.ThenExpression.Accept(this, ref arg);
        node.ElseExpression?.Accept(this, ref arg);

        return TypeReference.Void;
    }
}
