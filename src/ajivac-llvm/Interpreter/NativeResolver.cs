using System.Reflection;
using ajivac_lib.AST;

namespace ajivac_llvm;

public class NativeResolver
{
    private readonly Action<string> _logger;

    public NativeResolver(Action<string> logger)
    {
        _logger = logger;
    }

    public MethodInfo Resolve(Prototype proto)
    {
        var parameterTypes = new Type[proto.Parameters.Count];
        for (var i = 0; i < parameterTypes.Length; i++)
            parameterTypes[i] = ResolveNative(proto.Parameters[i].TypeReference);
        var name = proto.Name;
        if (name.Contains('.'))
        {
            var methodName = name[(name.LastIndexOf('.') + 1)..];
            var typeName = name[..name.LastIndexOf('.')];
            if (Type.GetType(typeName + ", " + typeName) is not { } type)
                throw new Exception($"Native type {typeName} not found");
            if (type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Default | BindingFlags.Public, parameterTypes) is not { } m)
                throw new Exception($"Native method {methodName} not found in type {typeName}");
            if (m.ReturnType != ResolveNative(proto.ReturnType))
                throw new Exception($"Native method {methodName} in type {typeName} has wrong return type: {m.ReturnType} != {ResolveNative(proto.ReturnType)}");

            return m;
        }

        if (typeof(NativeFunctions).GetMethod(name, BindingFlags.Static | BindingFlags.Default | BindingFlags.Public, parameterTypes) is not { } mi)
            throw new Exception($"Native method {name} not found");
        if (mi.ReturnType != ResolveNative(proto.ReturnType))
            throw new Exception($"Native Interpreter method {name} has wrong return type: {mi.ReturnType} != {ResolveNative(proto.ReturnType)}");

        return mi;
    }

    public static Type ResolveNative(TypeReference typeReference)
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

    public static BuildInType NativeResolve(Type type)
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
