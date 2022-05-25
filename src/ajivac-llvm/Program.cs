using System.Threading.Channels;
using ajivac_lib;

Console.WriteLine("Ajiva Compiler");


ILexer lexer = new Lexer(new StringReader(@"
@entry 
#pure
i32 a = 10 
if a = 20 {
    a = 10 + 2
}
"));

while (true)
{
    var token = lexer.ReadNextToken();

    Console.WriteLine($"{token.Type} {token.Value}");
    if (token.Type == TokenType.EOF)
        break;
}

