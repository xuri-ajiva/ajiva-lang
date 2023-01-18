using System.Reflection;
using ajivac_lib;
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
        return SResolve(proto.Name, parameterTypes, proto.ReturnType);
    }

    public static MethodInfo SResolve(string name, Type[] parameterTypes, TypeReference? returnType)
    {
        if (name.Contains('.'))
        {
            var methodName = name[(name.LastIndexOf('.') + 1)..];
            var typeName = name[..name.LastIndexOf('.')];
            if (Type.GetType(typeName + ", " + typeName) is not { } type)
                throw new Exception($"Native type {typeName} not found");
            if (type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Default | BindingFlags.Public, parameterTypes) is not { } m)
                throw new Exception($"Native method {methodName} not found in type {typeName}");
            if (returnType != null && m.ReturnType != ResolveNative(returnType))
                throw new Exception($"Native method {methodName} in type {typeName} has wrong return type: {m.ReturnType} != {ResolveNative(returnType)}");

            return m;
        }

        if (typeof(NativeFunctions).GetMethod(name, BindingFlags.Static | BindingFlags.Default | BindingFlags.Public, parameterTypes) is not { } mi)
            throw new Exception($"Native method {name} not found");
        if (returnType != null && mi.ReturnType != ResolveNative(returnType))
            throw new Exception($"Native Interpreter method {name} has wrong return type: {mi.ReturnType} != {ResolveNative(returnType)}");

        return mi;
    }

    public static Type ResolveNative(TypeReference? typeReference)
    {
        if (typeReference is {} )
            return ConversionHelpers.BuildInTypeToType(typeReference.Value);
        return typeof(object);
    }

    public static TypeReference NativeResolve(Type type)
    {
        return ConversionHelpers.TypeToBuildInType(type);
    }
}
