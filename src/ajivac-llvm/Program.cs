using System.Text;
using System.Threading.Channels;
using ajivac_lib;
using ajivac_llvm;

Console.WriteLine("Ajiva Compiler");

var src = @"
native void System.Console.WriteLine(i32 s)
fn i32 fac(i32 n) {
    if (n == 0) {
        return 1
    } else {
        return n * fac(n - 1)
    }
}
@entry 
#pure {*
System.Console.WriteLine(fac(10))
i32 a = 10 
if ( !(a != 20) ) {
    a = 10 + 2
} 
else
{
    a = a * 100
}
a = a + fac(10)
System.Console.WriteLine(a)
";
ILexer lexer = new Lexer(src,
    //https://en.cppreference.com/w/c/language/operator_precedence
    new Dictionary<TokenType, int>() {
        [TokenType.Comma] = 10,

        [TokenType.Assign] = 20,

        [TokenType.AddAssign] = 30,
        [TokenType.SubAssign] = 30,

        [TokenType.MulAssign] = 30,
        [TokenType.DivAssign] = 30,
        [TokenType.ModAssign] = 30,

        [TokenType.AndAssign] = 30,
        [TokenType.OrAssign] = 30,
        [TokenType.XorAssign] = 30,

        [TokenType.Question] = 40,
        [TokenType.Colon] = 40,

        [TokenType.Or] = 50,
        [TokenType.Xor] = 60,
        [TokenType.And] = 70,
        [TokenType.BitOr] = 80,
        [TokenType.BitXor] = 90,
        [TokenType.BitAnd] = 100,

        [TokenType.DoubleEquals] = 110,
        [TokenType.NotEqual] = 110,

        [TokenType.Greater] = 120,
        [TokenType.GreaterEqual] = 120,
        [TokenType.Less] = 120,
        [TokenType.LessEqual] = 120,

        [TokenType.ShiftLeft] = 130,
        [TokenType.ShiftRight] = 130,

        [TokenType.Plus] = 140,
        [TokenType.Minus] = 140,

        [TokenType.Star] = 150,
        [TokenType.Slash] = 150,
        [TokenType.Percent] = 150,

        [TokenType.Not] = 160,
        [TokenType.Negate] = 160,

        [TokenType.Increment] = 170,
        [TokenType.Decrement] = 170,
    });

Console.WriteLine(src);
var parser = new Parser(lexer);
var ast = parser.ParseAll();
Console.WriteLine(ast.GetType() + ": " + ast.Span);
Console.WriteLine(MakeIndentation(ast));

Interpreter interpreter = new Interpreter();
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
