using ajivac_lib.AST;
using ajivac_lib.ContextualAnalyzer.HelperStructs;
using ajivac_lib.Visitor;

public class TreeBuildVisitor : AstVisitorBase<TreeRef, NonRef>
{
    public override TreeRef Visit(CompoundStatement node, ref NonRef arg)
    {
        return VisitChildren(node, ref arg);
    }

    protected override TreeRef VisitChildren(IAstNode? node, ref NonRef arg)
    {
        if (node is null)
            throw new ArgumentNullException(nameof(node));
        var res = new TreeRef(node.GetType().ToString());
        foreach (var child in node.Children)
            res.Childiren.Add(child.Accept(this, ref arg));
        return res;
    }

    public override TreeRef Visit(LiteralExpression node, ref NonRef arg)
    {
        return new TreeRef(node.Value, node.TypeReference);
    }

    public override TreeRef Visit(IdentifierExpression node, ref NonRef arg)
    {
        return new TreeRef(node.Identifier, node.TypeReference);
    }

    public override TreeRef Visit(Prototype node, ref NonRef arg)
    {
        var tree = VisitChildren(node, ref arg);
        tree.Name = node.Name;
        tree.Type = node.TypeReference.ToString();
        return tree;
    }

    public override TreeRef Visit(LocalVariableDeclaration node, ref NonRef arg)
    {
        var tree = VisitChildren(node, ref arg);
        tree.Name = node.Name;
        tree.Type = node.TypeReference.ToString();
        return tree;
    }

    public override TreeRef Visit(BinaryExpression node, ref NonRef arg)
    {
        var tree = VisitChildren(node, ref arg);
        tree.Text = node.Operator.ToString();
        tree.Type = node.TypeReference.ToString();
        return tree;
    }

    public override TreeRef Visit(UnaryExpression node, ref NonRef arg)
    {
        var tree = VisitChildren(node, ref arg);
        tree.Text = node.Operator.ToString();
        tree.Type = node.TypeReference.ToString();
        return tree;
    }

    public override TreeRef Visit(FunctionCallExpression node, ref NonRef arg)
    {
        var tree = VisitChildren(node, ref arg);
        tree.Text = "Call";
        tree.Name = node.CalleeName;
        tree.Type = node.TypeReference.ToString();
        return tree;
    }

    public override TreeRef Visit(FunctionDefinition node, ref NonRef arg)
    {
        var tree = VisitChildren(node, ref arg);
        tree.Text = "Func: " + node.Signature.Name;
        tree.Type = node.TypeReference.ToString();
        return tree;
    }

    public override TreeRef Visit(ParameterDeclaration node, ref NonRef arg)
    {
        var tree = VisitChildren(node, ref arg);
        tree.Text = "Param: " + node.Index;
        tree.Name = node.Name;
        tree.Type = node.TypeReference.ToString();
        return tree;
    }

    public override TreeRef Visit(AttributeEaSt node, ref NonRef arg)
    {
        var tree = VisitChildren(node, ref arg);
        tree.Name = node.Name;
        tree.Text = "@";
        return tree;
    }

    public override TreeRef Visit(IfExpression node, ref NonRef arg)
    {
        var tree = VisitChildren(node, ref arg);
        tree.Text = "If";
        return tree;
    }

    public override TreeRef Visit(AssignmentExpression node, ref NonRef arg)
    {
        var tree = VisitChildren(node, ref arg);
        tree.Text = "Assign";
        tree.Name = node.Name;
        return tree;
    }

    public override TreeRef Visit(BreakStatement node, ref NonRef arg)
    {
        return new TreeRef("Break");
    }

    public override TreeRef Visit(ReturnStatement node, ref NonRef arg)
    {
        return new TreeRef("Return");
    }

    public override TreeRef Visit(WhileStatement node, ref NonRef arg)
    {
        var tree = VisitChildren(node, ref arg);
        tree.Text = "While";
        return tree;
    }

    public override TreeRef Visit(ForStatement node, ref NonRef arg)
    {
        var tree = VisitChildren(node, ref arg);
        tree.Text = "For";
        return tree;
    }

    public override TreeRef Visit(ContinueStatement node, ref NonRef arg)
    {
        return new TreeRef("Continue");
    }

    public override TreeRef Default(ref NonRef arg)
    {
        return new TreeRef("Default");
    }
}