using ajivac_lib.AST;
using ajivac_lib.Semantics;

namespace ajivac_lib.ContextualAnalyzer.HelperStructs;

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

    private readonly Diagnostics _diagnostics;

    public TypeHelper(Diagnostics diagnostics)
    {
        _diagnostics = diagnostics;
    }

    public bool IsAssignable(TypeReference leftType, TypeReference rightType)
    {
        if (leftType == rightType)
            return true;
        if (!leftType.IsPrimitive || !rightType.IsPrimitive)
            throw new NotImplementedException("User defined types are not supported yet");

        return CanConvert(rightType.Kind, leftType.Kind);
    }

    private bool CanConvert(TypeKind from, TypeKind to)
    {
        if (from == to) return true;
        switch (from)
        {
            case TypeKind.Bit:
                return false /* or TypeKind.I32 or TypeKind.U32 or TypeKind.I64 or TypeKind.U64*/;
            case TypeKind.U32:
                return to is TypeKind.U64 or TypeKind.I64 or TypeKind.F32 or TypeKind.F64;
            case TypeKind.U64:
                return to is TypeKind.I32 or TypeKind.U32 or TypeKind.F32 or TypeKind.F64;
            case TypeKind.I32:
                return to is TypeKind.F32 or TypeKind.F64 or TypeKind.I64 or TypeKind.U64;
            case TypeKind.I64:
                return to is TypeKind.I32 or TypeKind.U32;
            case TypeKind.F32:
                return to is TypeKind.F64;
            case TypeKind.F64:
                return false; //r is  TypeKind.F32,
            case TypeKind.Str:
                return to is not TypeKind.Void or TypeKind.Unknown;
            case TypeKind.Chr:
                return false;
            default:
                _diagnostics.ReportInternalError($"Unexpected type {from}");
                return false;
        }
    }

    public TypeKind FindCommonType(TypeKind l, TypeKind r)
    {
        if (CanConvert(l, r))
            return r;
        if (CanConvert(r, l))
            return l;
        _diagnostics.ReportConversionError(l, r);
        return TypeKind.Unknown;
    }

    public TypeReference ApplyBinaryOperator(BinaryOperator nodeOperator, TypeReference leftType, TypeReference rightType, SourceSpan location)
    {
        var res = ApplyBinaryOperator_int(nodeOperator, leftType, rightType, location);
        //Console.WriteLine($"{leftType.Kind} {nodeOperator} {rightType.Kind} = {res.Kind}");
        return res;
    }

    private TypeReference ApplyBinaryOperator_int(BinaryOperator nodeOperator, TypeReference leftType, TypeReference rightType, SourceSpan location)
    {
        if (!leftType.IsPrimitive || !rightType.IsPrimitive)
            throw new NotImplementedException("User defined types are not supported yet");

        var l = leftType.Kind;
        var r = rightType.Kind;
        switch (nodeOperator)
        {
            case BinaryOperator.Assign: // todo remove this case
                _diagnostics.ReportOperatorNotSupported(location, nodeOperator, leftType, rightType, ", Assign operator is not supported here");
                throw new Exception("Assign operator is not supported here");
            case BinaryOperator.Plus:
            case BinaryOperator.Minus:
            case BinaryOperator.Multiply:
            case BinaryOperator.Divide:
                return new TypeReference(FindCommonType(l, r));
            case BinaryOperator.Equal:
            case BinaryOperator.NotEqual:
            case BinaryOperator.Greater:
            case BinaryOperator.GreaterEqual:
            case BinaryOperator.Less:
            case BinaryOperator.LessEqual:
                FindCommonType(l, r); // check if types are compatible
                return new TypeReference(TypeKind.Bit);
            case BinaryOperator.Modulo:
            case BinaryOperator.And:
            case BinaryOperator.Or:
            case BinaryOperator.Xor:
            case BinaryOperator.ShiftLeft:
            case BinaryOperator.ShiftRight:
                if (r is TypeKind.U32 or TypeKind.U64 or TypeKind.I32 or TypeKind.I64) return new TypeReference(l);
                _diagnostics.ReportOperatorNotSupported(location, nodeOperator, leftType, rightType, " only can be applied to integer types");
                return TypeReference.Unknown;
            case BinaryOperator.Question:
            case BinaryOperator.Colon:
                throw new NotImplementedException("Ternary operator is not supported yet");
            default:
                throw new ArgumentOutOfRangeException(nameof(nodeOperator), nodeOperator, null);
        }
    }

    public TypeReference ApplyUnaryOperator(UnaryOperator nodeOperator, TypeReference operandType, SourceSpan position)
    {
        if (!operandType.IsPrimitive)
            throw new NotImplementedException("User defined types are not supported yet");

        var o = operandType.Kind;
        switch (nodeOperator)
        {
            case UnaryOperator.Not:
                if (o is TypeKind.Bit) return operandType;
                _diagnostics.ReportOperatorNotSupported(position, nodeOperator, operandType, " only can be applied to bit type");
                return TypeReference.Unknown;
            case UnaryOperator.Increment:
            case UnaryOperator.Decrement:
            case UnaryOperator.Negate:
                if (o is not (not TypeKind.I32 or TypeKind.I64 or TypeKind.U64 or TypeKind.U32)) return operandType;
                _diagnostics.ReportOperatorNotSupported(position, nodeOperator, operandType, " only can be applied to integer types");
                return TypeReference.Unknown;
            case UnaryOperator.Positive:
            case UnaryOperator.Negative:
                if (o is not (not TypeKind.I32 or TypeKind.I64 or TypeKind.U64 or TypeKind.U32 or TypeKind.F32 or TypeKind.F64)) return operandType;
                _diagnostics.ReportOperatorNotSupported(position, nodeOperator, operandType, " only can be applied to integer and float types");
                return TypeReference.Unknown;
            default:
                throw new ArgumentOutOfRangeException(nameof(nodeOperator), nodeOperator, null);
        }
    }
}
