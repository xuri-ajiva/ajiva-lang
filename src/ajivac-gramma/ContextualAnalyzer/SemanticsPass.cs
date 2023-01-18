using ajivac_lib.AST;
using ajivac_lib.Visitor;

namespace ajivac_lib.ContextualAnalyzer;

public class SemanticsPass : AstVisitorBase<Void, Void>
{
    private RefPass _refPass;
    private TypePass _typePass;

    public SemanticsPass()
    {
        _refPass = new RefPass();
        _typePass = new TypePass();
    }

    public override Void Visit(RootNode node, ref Void arg)
    {
        _refPass.Visit(node, ref arg);
        _typePass.Visit(node, ref arg);
        return Void.Empty;
    }
}
