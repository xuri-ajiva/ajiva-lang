using System.Diagnostics.CodeAnalysis;
using ajivac_lib.AST;

namespace ajivac_lib.ContextualAnalyzer;

public class IdentificationTable
{
    public IdentificationTable()
    {
    }
    
    private readonly Stack<InternScopeFrame> stack = new();

    public class InternScopeFrame
    {
        public Dictionary<string, LocalVariableDeclaration> Variables { get; set; } = new();
    }
    
    public void OpenScope()
    {
        stack.Push(new InternScopeFrame());
    }
    
    public void CloseScope()
    {
        stack.Pop();
    }
    
    public void Declare(LocalVariableDeclaration variable)
    {
        var stackFrame = stack.Peek();
        //check if variable is already declared
        if (stackFrame.Variables.ContainsKey(variable.Name))
            throw new SyntaxError($"Variable {variable.Name} is already declared in this scope", variable);
        stackFrame.Variables.Add(variable.Name, variable);
    }
    
    public bool TryGetVariable(string name,[NotNullWhen(true)] out LocalVariableDeclaration? variable)
    {
        foreach (var scope in stack)
        {
            if (scope.Variables.TryGetValue(name, out variable))
            {
                return true;
            }
        }
        
        variable = null;
        return false;
    }
}
