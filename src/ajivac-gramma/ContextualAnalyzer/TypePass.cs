using ajivac_lib.AST;
using ajivac_lib.Visitor;

namespace ajivac_lib.ContextualAnalyzer;

public class TypePass : AstAllSubVisitorBase<TypeReference, Void>
{
    TypeHelper _typeHelper = new TypeHelper();

    public TypePass()
    {
    }

    public override TypeReference Visit(LiteralExpression node, ref Void arg)
    {
        return /*TypeReference.BuildIn*/(node.ValueTypeReference);
    }

    public override TypeReference Visit(IdentifierExpression node, ref Void arg)
    {
        if (node.Definition is null)
            throw new SyntaxError("Identifier not found", node);

        return node.Definition.TypeReference;
    }

    public override TypeReference Visit(BinaryExpression node, ref Void arg)
    {
        var leftType = node.Left.Accept(this, ref arg);
        var rightType = node.Right.Accept(this, ref arg);

        return _typeHelper.ApplyBinaryOperator(node.Operator, leftType, rightType);
    }

    public override TypeReference Visit(UnaryExpression node, ref Void arg)
    {
        var operandType = node.Operand.Accept(this, ref arg);
        return _typeHelper.ApplyUnaryOperator(node.Operator, operandType);
    }

    public override TypeReference Visit(FunctionCallExpression node, ref Void arg)
    {
        if (node.Definition is null)
            throw new SyntaxError("Function not found", node);
        return node.Definition.Signature.ReturnType;
    }

    public override TypeReference Visit(AssignmentExpression node, ref Void arg)
    {
        var leftType = node.AssignmentValue.Accept(this, ref arg);
        var rightType = node.Definition.TypeReference;

        if (!_typeHelper.IsAssignable(leftType, rightType))
            throw new SyntaxError("Cannot assign value of type " + leftType.Kind + " to variable of type " + rightType.Kind, node);

        return TypeReference.Void;
    }

    public override TypeReference Visit(ReturnStatement node, ref Void arg)
    {
        if (node.Expression is null)
            return /*TypeReference.BuildIn*/(TypeReference.Void);
        return node.Expression.Accept(this, ref arg);
    }

    public override TypeReference Visit(IfExpression node, ref Void arg)
    {
        var conditionType = node.Condition.Accept(this, ref arg);
        if (conditionType.Kind != TypeKind.Bit)
            throw new SyntaxError("Condition must be of type bool", node);

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
