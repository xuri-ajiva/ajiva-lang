using ajivac_lib.AST;
using ajivac_lib.ContextualAnalyzer.HelperStructs;
using ajivac_lib.Visitor;

namespace ajivac_lib.ContextualAnalyzer;

public class RefPass : AstAllSubVisitorBase<NonRef, NonRef>
{
    private readonly Diagnostics _diagnostics;
    private readonly IdentificationTable idTable;
    private readonly Stack<FunctionDefinition> functionCallStack;

    public RefPass(Diagnostics diagnostics)
    {
        _diagnostics = diagnostics;
        idTable = new IdentificationTable(diagnostics);
        functionCallStack = new Stack<FunctionDefinition>();
    }

    public override NonRef Visit(FunctionDefinition node, ref NonRef arg)
    {
        functionCallStack.Push(node);
        idTable.OpenScope();
        base.Visit(node, ref arg);
        idTable.CloseScope();
        functionCallStack.Pop();

        return NonRef.Empty;
    }
    

    public override NonRef Visit(IdentifierExpression node, ref NonRef arg)
    {
        if (idTable.TryGetVariable(node.Identifier, out var variable))
        {
            node.Definition = variable;
        }
        else
        {
            _diagnostics.ReportVariableNotDefined(node.Identifier, node.Span);
        }
        return NonRef.Empty;
    }

    public override NonRef Visit(LocalVariableDeclaration node, ref NonRef arg)
    {
        idTable.Declare(node);
        return NonRef.Empty;
    }

    public override NonRef Visit(Prototype node, ref NonRef arg)
    {
        return node.IsExtern ? NonRef.Empty : base.Visit(node, ref arg);
    }

    public override NonRef Visit(ParameterDeclaration node, ref NonRef arg)
    {
        idTable.Declare(node);
        return NonRef.Empty;
    }

    public override NonRef Visit(AssignmentExpression node, ref NonRef arg)
    {
        if (idTable.TryGetVariable(node.Name, out var variable))
        {
            node.Definition = variable;
        }
        else
        {
            _diagnostics.ReportVariableNotDefined(node.Name, node.Span);
        }
        return base.Visit(node, ref arg);
    }

    public override NonRef Visit(CompoundStatement node, ref NonRef arg)
    {
        idTable.OpenScope();
        base.Visit(node, ref arg);
        idTable.CloseScope();
        return NonRef.Empty;
    }

    public override NonRef Visit(ReturnStatement node, ref NonRef arg)
    {
        node.Function = functionCallStack.Peek();
        return base.Visit(node, ref arg);
    }

    public override NonRef Visit(FunctionCallExpression node, ref NonRef arg)
    {
        foreach (var nodeArgument in node.Arguments)
        {
            nodeArgument.Accept(this, ref arg);
        }
        return NonRef.Empty;
    }
}
