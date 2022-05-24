using System.Collections.ObjectModel;
using System.Text;

namespace ajivac_lib;

public interface ILexer
{
    Token CurrentToken { get; }

    string LastIdentifier { get; }

    Number LastNumber { get; }

    int TokenPrecedence { get; }

    Token ReadNextToken();
}
public class Lexer : ILexer
{
    /// <inheritdoc />
    public Token CurrentToken { get; set; }

    /// <inheritdoc />
    public string LastIdentifier { get; set; }

    /// <inheritdoc />
    public Number LastNumber { get; set; } = new Number();

    /// <inheritdoc />
    public int TokenPrecedence { get; set; }

    /// <inheritdoc />
    public Token ReadNextToken()
    {
        while (LanguageDefinition.IsWhiteSpace(ReadNextChar()))
        {
        }

        if (LanguageDefinition.IsValidIdentifierBegin(_currentChar))
        {
            _buffer.Append(_currentChar);
            while (LanguageDefinition.IsValidIdentifierContinuation(ReadNextChar()))
                _buffer.Append(_currentChar);

            var identifier = _buffer.ToString();
            _buffer.Clear();

            foreach (var (type, str) in LanguageDefinition.KeyWordMap)
            {
                if (string.Equals(identifier, str, StringComparison.OrdinalIgnoreCase))
                    return CurrentToken = new Token(type, identifier, _currentPosition);
            }

            LastIdentifier = identifier;
            return CurrentToken = new Token(TokenType.Identifier, identifier, _currentPosition);
        }

        if (LanguageDefinition.IsValidNumberBegin(_currentChar))
        {
            _buffer.Append(_currentChar);
            while (LanguageDefinition.IsValidNumberContinuation(ReadNextChar(), _buffer))
                _buffer.Append(_currentChar);

            var number = _buffer.ToString();
            _buffer.Clear();

            if (number.Contains('.') && number.Split('.') is { Length: 2 } parts && long.TryParse(parts[0], out var intPart) && long.TryParse(parts[1], out var floatPart))
            {
                LastNumber.IntPart = intPart;
                LastNumber.FloatPart = floatPart;
            }
            else
            {
                LastNumber.IntPart = long.Parse(number);
                LastNumber.FloatPart = 0;
            }
            return CurrentToken = new Token(TokenType.Number, number, _currentPosition);
        }

        if (LanguageDefinition.IsCommentBegin(_currentChar))
        {
            if (LanguageDefinition.IsSingleLineCommentContinuation(PeekNextChar()))
            {
                do
                {
                    ReadNextChar();
                    if (EOF) goto eof;
                } while (!LanguageDefinition.IsSingleCommentEnd(_currentChar));

                return ReadNextToken();
            }
            if (LanguageDefinition.IsMultiLineCommentContinuation(PeekNextChar()))
            {
                while (true)
                {
                    ReadNextChar();

                    if (EOF) goto eof;
                    if (LanguageDefinition.IsMultiLineCommentEnd(_currentChar) && LanguageDefinition.IsMultiLineCommentEndContinuation(PeekNextChar()))
                    {
                        break;
                    }
                }
                return ReadNextToken();
            }
        }

        eof:
        if (EOF)
        {
            return CurrentToken = new Token(TokenType.EOF, "", _currentPosition);
        }

        return CurrentToken = new Token(TokenType.Unknown, _currentChar.ToString(), _currentPosition);
    }

    private readonly StringBuilder _buffer = new StringBuilder();
    private char _currentChar = default!;
    private TokenLocation _currentPosition = default!;
    private bool EOF = false;
    private readonly TextReader _reader;

    private char ReadNextChar()
    {
        var read = _reader.Read();
        _currentPosition.Column++;
        
        if (read == -1)
        {
            EOF = true;
            return default;
        }
        if(read is '\n' or '\r')
        {
            _currentPosition.Line++;
            _currentPosition.Column = 0;
        }
        return _currentChar = (char)read;
    }

    private char PeekNextChar()
    {
        return (char)_reader.Peek();
    }

    public Lexer(TextReader reader)
    {
        _reader = reader;
    }
}
public class LanguageDefinition
{
    public static bool IsValidIdentifierBegin(char c)
    {
        return IsValidIdentifierContinuation(c);
    }

    public static bool IsValidIdentifierContinuation(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_';
    }

    public static bool IsWhiteSpace(char c)
    {
        return c == ' ' || c == '\t' || c == '\n' || c == '\r';
    }

    public static IDictionary<TokenType, string> KeyWordMap = new ReadOnlyDictionary<TokenType, string>(
        new Dictionary<TokenType, string>() {
            [TokenType.I32] = "i32",
            [TokenType.U32] = "u32",
            [TokenType.I64] = "i64",
            [TokenType.U64] = "u64",
            [TokenType.F32] = "f32",
            [TokenType.F64] = "f64",

            [TokenType.Chr] = "chr",
            [TokenType.Str] = "str",
            [TokenType.Bit] = "bit",

            [TokenType.If] = "if",
            [TokenType.Else] = "else",

            [TokenType.For] = "for",
            [TokenType.While] = "while",
            [TokenType.Break] = "break",
            [TokenType.Continue] = "continue",

            [TokenType.Fn] = "fn",
            [TokenType.Return] = "return",

            [TokenType.Void] = "void",
            [TokenType.True] = "true",
            [TokenType.False] = "false",
            [TokenType.Null] = "null",
        }
    );

    public static bool IsValidNumberBegin(char c)
    {
        return char.IsDigit(c) || c is '.' or '-' or '+';
    }

    public static bool IsValidNumberContinuation(char c, StringBuilder builder)
    {
        if (c != '.') return char.IsDigit(c);
        return !builder.ToString().Contains('.');
    }

    public static bool IsCommentBegin(char c)
    {
        return c is '/';
    }

    public static bool IsSingleLineCommentContinuation(char c)
    {
        return c is '/';
    }

    public static bool IsSingleCommentEnd(char c)
    {
        return c is '\n' or '\r';
    }

    public static bool IsMultiLineCommentContinuation(char c)
    {
        return c is '*';
    }

    public static bool IsMultiLineCommentEnd(char c)
    {
        return c is '*';
    }

    public static bool IsMultiLineCommentEndContinuation(char c)
    {
        return c is '/';
    }
}
