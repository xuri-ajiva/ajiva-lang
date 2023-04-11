using System.Diagnostics;
using System.Text;
using ajivac_il;
using ajivac_lib;
using ajivac_lib.AST;
using ajivac_lib.Semantics;
using ajivac_llvm;

Console.WriteLine("Ajiva Compiler");

const string src = @"
native void System.Console.WriteLine(i32 s)
native void System.Console.Write(str s)
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
System.Console.Write(""fac(10) = "");
System.Console.WriteLine(fac(10));
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
a = 0
while (a < 100) {
    a = a + 1
    if(a < 5) {
        System.Console.WriteLine(""Continue"")
        continue
        System.Console.WriteLine(""FAIL"")
    }
    Log(a)
    if (a == 10) {        
        System.Console.WriteLine(""Break"")
        break
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

const string Simple = @"
@entry(10)
fn i32 printn(i32 n) {
    if(!true) {
        return n
    }
    if(true) {
        return 20.3
    }
    else if(1) {
        return 'a'
    }
    printn(""Hello World"")
}
";
var interpreter = new Interpreter(s => Debug.WriteLine(s));
ILCodeGenerator iLGenerator = new ILCodeGenerator("result.dll", interpreter);
var sourceFile = new SourceFile(fisBuzz, "File.aj");
sourceFile.Print(Console.Out);
var compiler = new Compiler(sourceFile, Diagnostics.Console);
compiler.Run();
//compiler.PrintTree();
iLGenerator.Visit((RootNode)compiler.Ast);
iLGenerator.Finish();
iLGenerator.Run();
iLGenerator.Save("result.dll");

var time = BeginTime();
interpreter.Load(compiler.RuntimeState);
EndTime("Interpreter.Load", time);

time = BeginTime();
interpreter.Run(compiler.Ast);
EndTime("Interpreter.Run", time);

/*var compiler = new Compiler();
time = BeginTime();
var module = compiler.Compile(ast);
EndTime("Compiler.Compile", time);

time = BeginTime();
var runtime = new Runtime();
runtime.Load(module);
EndTime("Runtime.Load", time);

time = BeginTime();
runtime.Run();
EndTime("Runtime.Run", time);*/

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
