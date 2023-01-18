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

    public Compiler(SourceFile source)
    {
        _diagnostics = Diagnostics.Console;
        _source = source;
        _lexer = new Lexer(source,_diagnostics);
        _parser = new Parser(_lexer,_diagnostics);
        _semanticsPass = new SemanticsPass(_diagnostics);
    }

    public RuntimeStateHolder RuntimeState => ((Parser)_parser).RuntimeState;
    public IAstNode Ast => _ast;

    public void PrintSource()
    {
        _source.Print();
    }

    public void Run()
    {
        var time = BeginTime();
        _ast = _parser.ParseAll();
        EndTime("Parse", time);

        time = BeginTime();
        _semanticsPass.Visit((RootNode)_ast, ref NonRef.Empty);
        EndTime("SemanticsPass", time);
    }

    long BeginTime() => DateTime.Now.Ticks;

    void EndTime(string name, long start)
    {
        var end = DateTime.Now.Ticks;
        Console.WriteLine($"{name} took {(end - start) / 10000}ms");
    }

    private string MakeIndentation(object? value)
    {
        if (value is null)
        {
            return "Null";
        }

        string str = value.ToString()!;

        var buffer = new StringBuilder();
        var indentation = 0;
        bool indented = true;
        var openStrings = false;

        void Indent()
        {
            for (int j = 0; j < indentation; j++)
                buffer.Append("|   ");
            indented = true;
        }

        foreach (var t in str)
        {
            switch (t)
            {
                case '{' or '[' or '(' when !openStrings:
                    indentation++;
                    buffer.Append(t);
                    buffer.AppendLine();
                    Indent();
                    break;
                case '}' or ']' or ')' when !openStrings && indentation > 0:
                    indentation--;
                    buffer.AppendLine();
                    Indent();
                    buffer.Append(t);
                    break;
                case '\'' or '"':
                    buffer.Append(t);
                    openStrings = !openStrings;
                    break;
                case ',':
                    buffer.AppendLine(",");
                    Indent();
                    break;
                default:
                    {
                        if (!indented || t != ' ')
                        {
                            buffer.Append(t);
                            indented = false;
                        }
                        break;
                    }
            }
        }
        return buffer.ToString();
    }

    public void PrintTree()
    {
        if (_ast is null)
        {
            Console.WriteLine("No AST");
            return;
        }

        Console.WriteLine(MakeIndentation(_ast));
    }
}
