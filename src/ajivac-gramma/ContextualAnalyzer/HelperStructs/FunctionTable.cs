using System.Diagnostics.CodeAnalysis;
using ajivac_lib.AST;
using ajivac_lib.Semantics;

namespace ajivac_lib.ContextualAnalyzer.HelperStructs;

public class FunctionTable
{
    private Dictionary<string, List<FunctionDefinition>> functions = new();
    private Dictionary<string, List<FunctionDefinition>> natives = new();

    private readonly Diagnostics _diagnostics;

    public FunctionTable(Diagnostics diagnostics)
    {
        _diagnostics = diagnostics;
    }

    public void AddFunction(FunctionDefinition function)
    {
        if (functions.ContainsKey(function.Signature.Name))
        {
            functions[function.Signature.Name].Add(function);
        }
        else
        {
            functions.Add(function.Signature.Name, new List<FunctionDefinition> {
                function
            });
        }
    }

    public bool TryGetFunction(string name, IEnumerable<TypeReference> argTypes, TypeReference? returnType, [NotNullWhen(true)] out FunctionDefinition? function)
    {
        if (functions.TryGetValue(name, out var definitions))
        {
            if (FindFunction(argTypes, returnType, out function, definitions))
                return true; // found defined function
            //check if native exists
        }
        return TryGetNative(name, argTypes, returnType, out function);
    }

    private bool TryGetNative(string name, IEnumerable<TypeReference> argTypes, TypeReference? returnType, out FunctionDefinition? function)
    {
        if (natives.TryGetValue(name, out var definitions))
            if (FindFunction(argTypes, returnType, out function, definitions))
                return true; // found defined function

        //check if native exists
        try
        {
            var resolve = NativeResolver.SResolve(name, argTypes.Select(x => NativeResolver.ResolveNative(x)).ToArray(), returnType);

            var args = resolve.GetParameters().Select((x, i) => new ParameterDeclaration(SourceSpan.Empty, i, x.Name, NativeResolver.NativeResolve(x.ParameterType),
                x.HasDefaultValue
                    ? new LiteralExpression(SourceSpan.Empty, x.DefaultValue.ToString(), NativeResolver.NativeResolve(x.DefaultValue.GetType()))
                    : null)).ToArray();

            function = new FunctionDefinition(SourceSpan.Empty,
                new Prototype(SourceSpan.Empty, resolve.Name, true, args, NativeResolver.NativeResolve(resolve.ReturnType), true),
                new RootNode(SourceSpan.Empty, ArraySegment<IAstNode>.Empty));
            natives.Add(name, new List<FunctionDefinition> {
                function
            });
            
            return true;
        }
        catch (Exception e)
        {
            _diagnostics.ReportNativeFunctionNotFound(name, e.Message);
        }
        function = null;
        return false;
    }

    private static bool FindFunction(IEnumerable<TypeReference> argTypes, TypeReference? returnType, out FunctionDefinition? function, List<FunctionDefinition> definitions)
    {
        var argTypesList = argTypes.ToList();
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var definition in definitions)
        {
            //check argument count
            if (definition.Signature.Parameters.Count != argTypesList.Count)
                continue;

            //check argument types
            if (argTypesList.Where((t, i) => t != definition.Signature.Parameters[i].TypeReference).Any())
                continue;

            //check return type
            if (returnType is not null && returnType != definition.Signature.ReturnType)
                continue;

            function = definition;
            return true;
        }
        function = null;
        return false;
    }
}
