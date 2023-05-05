using ajivac_lib.AST;
using ajivac_lib.ContextualAnalyzer.HelperStructs;
using ajivac_lib.Visitor;

namespace ajivac_lib.ContextualAnalyzer;

public class SemanticsPass : AstVisitorBase<NonRef, NonRef>
{
    private readonly Diagnostics _diagnostics;
    private RefPass _refPass;
    private TypePass _typePass;

    public SemanticsPass(Diagnostics diagnostics)
    {
        _diagnostics = diagnostics;
        _refPass = new RefPass(diagnostics);
        _typePass = new TypePass(diagnostics);
    }

    public override NonRef Visit(CompoundStatement node, ref NonRef arg)
    {
        _refPass.Visit(node, ref arg);
        _typePass.Visit(node, ref arg);
        return NonRef.Empty;
    }
}
