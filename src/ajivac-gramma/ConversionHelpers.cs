using ajivac_lib.AST;

namespace ajivac_lib;

public static class ConversionHelpers
{
    public static Type BuildInTypeToType(BuildInTypeReference type) => BuildInTypeToType(type.Type);
    public static Type BuildInTypeToType(BuildInType type)
    {
        return type switch {
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

    public static BuildInType TypeToBuildInType(Type type)
    {
        if (type == typeof(int))
            return BuildInType.I32;
        if (type == typeof(long))
            return BuildInType.I64;
        if (type == typeof(uint))
            return BuildInType.U32;
        if (type == typeof(ulong))
            return BuildInType.U64;
        if (type == typeof(float))
            return BuildInType.F32;
        if (type == typeof(double))
            return BuildInType.F64;
        if (type == typeof(bool))
            return BuildInType.Bit;
        if (type == typeof(void))
            return BuildInType.Void;
        if (type == typeof(char))
            return BuildInType.Chr;
        if (type == typeof(string))
            return BuildInType.Str;
        if (type == typeof(object))
            return BuildInType.Unknown;
        throw new Exception("Unknown type");
    }
}
