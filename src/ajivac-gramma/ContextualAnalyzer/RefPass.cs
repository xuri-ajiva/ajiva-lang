using ajivac_lib.AST;
using ajivac_lib.Visitor;

namespace ajivac_lib.ContextualAnalyzer;

public class RefPass : AstAllSubVisitorBase<Void, Void>
{
    IdentificationTable idTable = new IdentificationTable();
    FunctionTable funcTable = new FunctionTable();

    public override Void Visit(FunctionCallExpression node, ref Void arg)
    {
        if (funcTable.TryGetFunction(node.CalleeName, out var func))
        {
            node.Definition = func;
            if (func.Parameters.Count != node.Arguments.Count) //todo move to type checker
            {
                throw new SyntaxError("Invalid number of arguments", node);
            }
        }
        else
        {
            throw new SyntaxError("Function '" + node.CalleeName + "' not found", node);
        }
        return Void.Empty;
    }

    public override Void Visit(Prototype node, ref Void arg)
    {
        if (node.IsExtern)
        {
            funcTable.  AddFunction(new FunctionDefinition(node.Span, node, null!));
            return Void.Empty;
        }
        else
            return base.Visit(node, ref arg);
    }

    public override Void Visit(FunctionDefinition node, ref Void arg)
    {
        funcTable.AddFunction(node);
        idTable.OpenScope();
        base.Visit(node, ref arg);
        idTable.CloseScope();
        return Void.Empty;
    }

    public override Void Visit(IdentifierExpression node, ref Void arg)
    {
        if (idTable.TryGetVariable(node.Identifier, out var variable))
        {
            node.Definition = variable;
        }
        else
        {
            throw new SyntaxError("Variable not found", node);
        }
        return Void.Empty;
    }

    public override Void Visit(LocalVariableDeclaration node, ref Void arg)
    {
        idTable.Declare(node);
        return Void.Empty;
    }

    public override Void Visit(ParameterDeclaration node, ref Void arg)
    {
        idTable.Declare(node);
        return Void.Empty;
    }

    public override Void Visit(AssignmentExpression node, ref Void arg)
    {
        if(idTable.TryGetVariable(node.Name, out var variable))
        {
            node.Definition = variable;
        }
        else
        {
            throw new SyntaxError("Variable not found", node);
        }
        return base.Visit(node, ref arg);
    }

    public override Void Visit(RootNode node, ref Void arg)
    {
        idTable.OpenScope();
        base.Visit(node, ref arg);
        idTable.CloseScope();
        return Void.Empty;
    }
}
public class SyntaxError : Exception
{
    private readonly IAstNode? _node;

    public SyntaxError(string message, IAstNode? node = null) : base(message + (node != null ? $" at {node.Span}" : ""))
    {
        _node = node;
    }
}
