using System.Diagnostics;
using System.Text;
using System.Threading.Channels;
using ajivac_lib;
using ajivac_llvm;

Console.WriteLine("Ajiva Compiler");

const string src = @"
native void System.Console.WriteLine(i32 s)
native void Log(i32 s)
fn i32 fac(i32 n) {
    if (n == 0) 
        return 1
    else {
        return n * fac(n - 1)
    }
}
@entry 
#pure {*
Log(fac(10))
i32 a = 10 
if ( !(a != 20) ) {
    a = 10 + 2
} 
else
{
    a = a * 100 + 2 + fac(5)
}
System.Console.WriteLine(a)
";
ILexer lexer = new Lexer(src);

Console.WriteLine(src.Replace("\r\n", "\r\n>  "));
var parser = new Parser(lexer);
var ast = parser.ParseAll();
//Console.WriteLine(MakeIndentation(ast));

Interpreter interpreter = new Interpreter(s => Debug.WriteLine(s));
interpreter.Load(parser.RuntimeState);
interpreter.Run(ast);

string MakeIndentation(object? value)
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
