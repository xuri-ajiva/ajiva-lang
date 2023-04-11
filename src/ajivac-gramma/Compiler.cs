using System.Diagnostics;
using System.Text;
using ajivac_lib.AST;
using ajivac_lib.ContextualAnalyzer;
using ajivac_lib.ContextualAnalyzer.HelperStructs;
using ajivac_lib.Semantics;

namespace ajivac_lib;

public class Compiler
{
    private IAstNode? _ast;

    private readonly SourceFile _source;
    private readonly ILexer _lexer;
    private readonly IParser _parser;
    private readonly SemanticsPass _semanticsPass;
    private readonly Diagnostics _diagnostics;

    public Compiler(SourceFile source, Diagnostics diagnostics)
    {
        _source = source;
        _diagnostics = diagnostics;
        _lexer = new Lexer(source, _diagnostics);
        _parser = new Parser(_lexer, _diagnostics);
        _semanticsPass = new SemanticsPass(_diagnostics);
    }

    public RuntimeStateHolder RuntimeState => ((Parser)_parser).RuntimeState;
    public IAstNode Ast => _ast;

    public void ParseAll()
    {
        _ast = _parser.ParseAll();
    }

    public void Analyze()
    {
        if (_ast is not null)
            _ast.Accept(_semanticsPass, ref NonRef.Empty);
        else
            _diagnostics.ReportError(SourceSpan.Empty, "No AST to analyze", Sensitivity.Warning);
    }

    public void Run()
    {
        ParseAll();
        Analyze();
    }
}
