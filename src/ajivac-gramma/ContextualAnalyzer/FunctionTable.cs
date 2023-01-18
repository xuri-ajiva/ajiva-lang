using System.Diagnostics.CodeAnalysis;
using ajivac_lib.AST;

namespace ajivac_lib.ContextualAnalyzer;

public class FunctionTable
{
    private Dictionary<string, FunctionDefinition> functions = new Dictionary<string, FunctionDefinition>();

    public void AddFunction(FunctionDefinition function)
    {
        functions.Add(function.Signature.Name, function);
    }

    public bool TryGetFunction(string name, [NotNullWhen(true)] out FunctionDefinition? function)
    {
        return functions.TryGetValue(name, out function);
    }

    public bool ContainsFunction(string name)
    {
        return functions.ContainsKey(name);
    }
}
