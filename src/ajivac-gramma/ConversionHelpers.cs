using ajivac_lib.AST;

namespace ajivac_lib;

public static class ConversionHelpers
{
    public static Type BuildInTypeToType(TypeReference typeReference) => BuildInTypeToType(typeReference.Kind);
    public static Type BuildInTypeToType(TypeKind kind)
    {
        return kind switch {
            TypeKind.I32 => typeof(int),
            TypeKind.I64 => typeof(long),
            TypeKind.U32 => typeof(uint),
            TypeKind.U64 => typeof(ulong),
            TypeKind.F32 => typeof(float),
            TypeKind.F64 => typeof(double),
            TypeKind.Bit => typeof(bool),
            TypeKind.Void => typeof(void),
            TypeKind.Chr => typeof(char),
            TypeKind.Str => typeof(string),
            TypeKind.Unknown => typeof(object), //todo is this correct?
            _ => throw new Exception("Unknown type"),
        };
    }

    public static TypeReference TypeToBuildInType(Type type)
    {
        if (type == typeof(int))
            return TypeReference.I32;
        if (type == typeof(long))
            return TypeReference.I64;
        if (type == typeof(uint))
            return TypeReference.U32;
        if (type == typeof(ulong))
            return TypeReference.U64;
        if (type == typeof(float))
            return TypeReference.F32;
        if (type == typeof(double))
            return TypeReference.F64;
        if (type == typeof(bool))
            return TypeReference.Bit;
        if (type == typeof(void))
            return TypeReference.Void;
        if (type == typeof(char))
            return TypeReference.Chr;
        if (type == typeof(string))
            return TypeReference.Str;
        if (type == typeof(object))
            return TypeReference.Unknown;
        throw new Exception("Unknown type");
    }
}
