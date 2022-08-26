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
if ( !(a == 20) ) {
    a = 10 + 2 a = 3
} 
else
{
    a = a * 100 + 2 + fac(5)
}
for (i32 i = 0 i < 10 i = i + 1) {
    a = a + 1
    Log(i)
}
Log(a)
while (a < 100) {
    a = a + 1
    Log(a)
    if (a == 50) {
        break
    }
    if(a < 10) {
        Log(8888)
        continue
        Log(99999)
    }
}
System.Console.WriteLine(a)
";
const string src2 = @"
@entry(10)
fn i32 fac(i32 n) {
    if (n == 0) 
        return 1
    else {
        return n * fac(n - 1)
    }
}
";
const string fisBuzz = @"
native void Log(i32 i)
native void Log(str s)
@entry(100)
fn void fizzbuzz(i32 n) {
    for (i32 i = 0 i < n i = i + 1) {
        if ((i % 3) == 0) {
            if ((i % 5) == 0) {
                Log(""FizzBuzz"")
            } else {
                Log(""Fizz"")
            }
        } else if ((i % 5) == 0) {
            Log(""Buzz"")
        } else {
            Log(i)
        }
    }
}
";

const string selected = src;
ILexer lexer = new Lexer(selected.Replace("\r\n", "  "));

Console.WriteLine(selected.Replace("\r\n", "\r\n>  "));
var parser = new Parser(lexer);
var time = BeginTime();
var ast = parser.ParseAll();
EndTime("Parse", time);
//Console.WriteLine(MakeIndentation(ast));

var interpreter = new Interpreter(s => Debug.WriteLine(s));
time = BeginTime();
interpreter.Load(parser.RuntimeState);
EndTime("Interpreter.Load", time);

time = BeginTime();
interpreter.Run(ast);
EndTime("Interpreter.Run", time);

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

long BeginTime() => DateTime.Now.Ticks;

void EndTime(string name, long start)
{
    var end = DateTime.Now.Ticks;
    Console.WriteLine($"{name} took {(end - start) / 10000}ms");
}
