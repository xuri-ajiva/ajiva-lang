using System.Reflection;
using System.Text;
using ajivac_lib;
using ajivac_lib.AST;

namespace ajivac_llvm;

public class FunctionResolver
{
    private readonly Action<string> _logger;
    private NativeResolver _nativeResolver;

    private readonly Dictionary<string, Prototype> nativeMethodsProtos = new();
    private readonly Dictionary<string, MethodInfo> nativeMethods = new();
    private readonly Dictionary<string, FunctionDefinition> functionDefinitions = new();

    public FunctionResolver(Action<string> logger)
    {
        _logger = logger;
        _nativeResolver = new NativeResolver(x => logger("Native: " + x));
    }

    public FunctionDefinition Resolve(Prototype prototype)
    {
        _logger($"calling {prototype.Name}");
        var func = functionDefinitions.GetValueOrDefault(GetIdentifier(prototype));
        if (func is null) throw new Exception($"function {prototype.Name} not found");
        return func;
    }

    public MethodInfo ResolveExtern(Prototype callee)
    {
        var identifier = GetIdentifier(callee);
        var method = nativeMethods.GetValueOrDefault(identifier);
        if (method is null)
            return LoadExtern(callee);
        _logger($"resolved {callee.Name}:{identifier}:{method}");
        return method;
    }

    public void Load(RuntimeStateHolder parserRuntimeState)
    {
        foreach (var proto in parserRuntimeState.NativeFunctionDeclarations)
        {
            LoadExtern(proto);
        }

        foreach (var proto in parserRuntimeState.FunctionDefinitions)
        {
            functionDefinitions.Add(GetIdentifier(proto.Signature), proto);
        }
    }

    public void AddFunction(Prototype prototype)
    {
        if (prototype.IsExtern)
            LoadExtern(prototype);
    }

    public void AddFunction(FunctionDefinition function)
    {
        var identifier = GetIdentifier(function.Signature);
        if (functionDefinitions.ContainsKey(identifier))
        {
            _logger($"already defined {identifier}, skipping");
        }
        else
        {
            functionDefinitions.Add(identifier, function);
        }
    }

    private MethodInfo LoadExtern(Prototype proto)
    {
        var mi = _nativeResolver.Resolve(proto);
        var identifier = GetIdentifier(proto);
        if (nativeMethods.ContainsKey(identifier))
        {
            _logger($"already defined {identifier}, skipping");
        }
        else
        {
            nativeMethodsProtos.Add(identifier, proto);
            nativeMethods.Add(identifier, mi);
        }
        return mi;
    }

    public void Info()
    {
        _logger("Resolved NativeMethods:");
        foreach (var (key, value) in nativeMethods)
        {
            _logger($"{key} -> {value}");
        }

        _logger("Resolved FunctionDefinitions:");
        foreach (var (key, value) in functionDefinitions)
        {
            _logger($"{key} -> {value}");
        }
    }

    private static string GetIdentifier(Prototype callee)
    {
        if (callee.Parameters.Count <= 0) return callee.Name;
        StringBuilder sb = new(callee.Name);
        sb.Append('[');
        var first = true;
        foreach (var param in callee.Parameters)
        {
            if (first) first = false;
            else sb.Append(',');
            sb.Append(param.Name);
            sb.Append(':');
            sb.Append(param.TypeReference.Identifier);
        }
        sb.Append(']');
        return sb.ToString();
    }

    public Prototype CallBlank(string calleeName, object[] arguments)
    {
        foreach (var keyValuePair in functionDefinitions
                     .Where(keyValuePair => keyValuePair.Value.Signature.Name == calleeName))
        {
            var func = keyValuePair.Value;
            if (func.Signature.Parameters.Count != arguments.Length) continue;
            var i = 0;
            foreach (var param in func.Signature.Parameters)
            {
                if (NativeResolver.ResolveNative(param.TypeReference) != arguments[i].GetType())
                    continue;
                i++;
            }
            if (i == func.Signature.Parameters.Count)
                return func.Signature;
        }
        foreach (var keyValuePair in nativeMethods
                     .Where(keyValuePair => keyValuePair.Value.Name == calleeName))
        {
            var func = keyValuePair.Value;
            var parameters = func.GetParameters();
            if (parameters.Length != arguments.Length) continue;
            var i = 0;
            foreach (var param in parameters)
            {
                if (param.ParameterType != arguments[i].GetType())
                    continue;
                i++;
            }
            if (i == parameters.Length)
                return nativeMethodsProtos[keyValuePair.Key];
        }

        var proto = new Prototype(SourceSpan.Empty, calleeName,
            true,
            arguments.Select((x, i) =>
                new ParameterDeclaration(SourceSpan.Empty,
                    i,
                    x.GetType().Name,
                    new BuildInTypeReference(NativeResolver.NativeResolve(x.GetType())),
                    null,
                    true)
            ).ToArray(),
            new BuildInTypeReference(BuildInType.Void),
            true);
        LoadExtern(proto);
        return proto;
    }
}
