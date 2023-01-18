using ajivac_lib.AST;

namespace ajivac_lib.ContextualAnalyzer;

public class TypeHelper
{
    // mapping for binary operators and their types
    private Dictionary<BinaryOperator, Dictionary<TypeKind, Dictionary<TypeKind, TypeKind>>> binaryOperatorTypes;
    // mapping for unary operators and their types
    private Dictionary<UnaryOperator, Dictionary<TypeKind, TypeKind>> unaryOperatorTypes;
    // mapping for types and their default values
    private Dictionary<TypeKind, object> defaultValues;
    // mapping for assigable types
    private Dictionary<TypeKind, List<TypeKind>> assignableTypes;

    public bool IsAssignable(TypeReference leftType, TypeReference rightType)
    {
        if (leftType == rightType)
            return true;
        if (!leftType.IsPrimitive || !rightType.IsPrimitive)
            throw new NotImplementedException("User defined types are not supported yet");

        var l = leftType.Kind;
        var r = rightType.Kind;
        return CanConvert(l, r);
    }

    private static bool CanConvert(TypeKind from, TypeKind to)
    {
        if (from == to) return true;
        return from switch {
            TypeKind.Bit => false /* or TypeKind.I32 or TypeKind.U32 or TypeKind.I64 or TypeKind.U64*/,
            TypeKind.U32 => to is TypeKind.U64 or TypeKind.I64 or TypeKind.F32 or TypeKind.F64,
            TypeKind.U64 => to is TypeKind.I32 or TypeKind.U32 or TypeKind.F32 or TypeKind.F64,
            TypeKind.I32 => to is TypeKind.F32 or TypeKind.F64 or TypeKind.I64 or TypeKind.U64,
            TypeKind.I64 => to is TypeKind.I32 or TypeKind.U32,
            TypeKind.F32 => to is TypeKind.F64,
            TypeKind.F64 => false, //r is  TypeKind.F32,
            TypeKind.Str => to is not TypeKind.Void or TypeKind.Unknown,
            TypeKind.Chr => false,
            _ => throw new SyntaxError("Unsupported type " + from)
        };
    }

    public static TypeKind FindCommonType(TypeKind l, TypeKind r)
    {
        if (CanConvert(l, r))
            return r;
        if (CanConvert(r, l))
            return l;
        throw new SyntaxError("Cannot convert from " + l + " to " + r);
    }

    public TypeReference ApplyBinaryOperator(BinaryOperator nodeOperator, TypeReference leftType, TypeReference rightType)
    {
        var res = ApplyBinaryOperator_int(nodeOperator, leftType, rightType);
        Console.WriteLine($"{leftType.Kind} {nodeOperator} {rightType.Kind} = {res.Kind}");
        return res;
    }

    public TypeReference ApplyBinaryOperator_int(BinaryOperator nodeOperator, TypeReference leftType, TypeReference rightType)
    {
        if (!leftType.IsPrimitive || !rightType.IsPrimitive)
            throw new NotImplementedException("User defined types are not supported yet");

        var l = leftType.Kind;
        var r = rightType.Kind;
        switch (nodeOperator)
        {
            case BinaryOperator.Assign: // todo remove this case
                throw new SyntaxError("Assign operator is not supported here");
            case BinaryOperator.Plus:
            case BinaryOperator.Minus:
            case BinaryOperator.Multiply:
            case BinaryOperator.Divide:
                return new TypeReference(FindCommonType(l, r));
            case BinaryOperator.Modulo:
                if (r is not TypeKind.U32 or TypeKind.U64)
                    throw new SyntaxError("Modulo operator can only be applied to integer types");
                return new TypeReference(r);
            case BinaryOperator.Equal:
            case BinaryOperator.NotEqual:
            case BinaryOperator.Greater:
            case BinaryOperator.GreaterEqual:
            case BinaryOperator.Less:
            case BinaryOperator.LessEqual:
                FindCommonType(l, r); // check if types are compatible
                return new TypeReference(TypeKind.Bit);
            case BinaryOperator.And:
            case BinaryOperator.Or:
            case BinaryOperator.Xor:
            case BinaryOperator.ShiftLeft:
            case BinaryOperator.ShiftRight:
                if (r is not TypeKind.U32 or TypeKind.U64 or TypeKind.I32 or TypeKind.I64)
                    throw new SyntaxError("Bitwise operator can only be applied to integer types");
                return new TypeReference(l);
            case BinaryOperator.Question:
            case BinaryOperator.Colon:
                throw new SyntaxError("Ternary operator is not supported yet");
            default:
                throw new ArgumentOutOfRangeException(nameof(nodeOperator), nodeOperator, null);
        }
    }

    public TypeReference ApplyUnaryOperator(UnaryOperator nodeOperator, TypeReference operandType)
    {
        if (!operandType.IsPrimitive)
            throw new NotImplementedException("User defined types are not supported yet");

        var o = operandType.Kind;
        switch (nodeOperator)
        {
            case UnaryOperator.Not:
                if (o is not TypeKind.Bit)
                    throw new SyntaxError("Not operator can only be applied to bit type");
                return operandType;
            case UnaryOperator.Increment:
            case UnaryOperator.Decrement:
            case UnaryOperator.Negate:
                if (o is not TypeKind.I32 or TypeKind.I64 or TypeKind.U64 or TypeKind.U32)
                    throw new SyntaxError("Negate operator can only be applied to integer types");
                return operandType;
            case UnaryOperator.Positive:
            case UnaryOperator.Negative:
                if (o is not TypeKind.I32 or TypeKind.I64 or TypeKind.U64 or TypeKind.U32 or TypeKind.F32 or TypeKind.F64)
                    throw new SyntaxError("Negative operator can only be applied to integer and float types");
                return operandType;
            default:
                throw new ArgumentOutOfRangeException(nameof(nodeOperator), nodeOperator, null);
        }
    }
}
