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
            while (LanguageDefinition.IsValidIdentifierContinuation(PeekNextChar()))
                _buffer.Append(ReadNextChar());

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
            while (LanguageDefinition.IsValidNumberContinuation(PeekNextChar(), _buffer))
                _buffer.Append(ReadNextChar());

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
            if (LanguageDefinition.IsSingleLineCommentBegin(_currentChar, PeekNextChar()))
            {
                while (!LanguageDefinition.IsLineBreak(_currentChar) && !EOF)
                {
                    ReadNextChar();
                }

                return ReadNextToken();
            }
            if (LanguageDefinition.IsMultiLineCommentBegin(_currentChar, PeekNextChar()))
            {
                while (!LanguageDefinition.IsMultiLineCommentEnd(_currentChar, PeekNextChar()) && !EOF)
                {
                    ReadNextChar();
                }
                return ReadNextToken();
            }
        }

        if(LanguageDefinition.IsAttributeChar(_currentChar))
        {
            return CurrentToken = new Token(TokenType.Attribute, _currentChar.ToString(), _currentPosition);
        }
        
        if (LanguageDefinition.TryGetSpecialCharToken(_currentChar, PeekNextChar(), out var op, out var length))
        {
            _buffer.Append(_currentChar);
            while (length > 1)
            {
                _buffer.Append(ReadNextChar());
                length--;
            }
            var operatorString = _buffer.ToString();
            _buffer.Clear();
            return CurrentToken = new Token(op, operatorString, _currentPosition);
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
        if (read is '\n' or '\r')
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
        return char.IsLetter(c) || c == '_';
    }

    public static bool IsValidIdentifierContinuation(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_';
    }

    public static bool IsWhiteSpace(char c)
    {
        return c is ' ' or '\t' or '\n' or '\r';
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
        return char.IsDigit(c) || c is '.';
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

    public static bool IsSingleLineCommentBegin(char c, char next)
    {
        return c == '/' && next == '/';
    }

    public static bool IsMultiLineCommentBegin(char c, char next)
    {
        return c is '/' && next is '*';
    }

    public static bool IsMultiLineCommentEnd(char c, char next)
    {
        return c is '*' && next is '/';
    }

    public static bool IsSingleSpecialControlChar(char c)
    {
        return c is '+' or '+' or '+' or '-' or '-' or '-' or '*' or '*' or '/' or '/' or '%' or '%' or '^' or '^' or '^' or '&' or '&' or '&' or '|' or '|' or '|' or '!' or '!' or '=' or '=' or '<' or '<' or '<' or '>' or '>' or '>' or '?' or ':' or '~' or '.' or ',' or ';' or '(' or ')' or '{' or '}' or '[' or ']';
    }

    public static bool TryGetSpecialCharToken(char c, char next, out TokenType type, out int length)
    {
        if (IsSingleSpecialControlChar(c))
        {
            if (IsSingleSpecialControlChar(next))
            {
                type = c switch {
                    '+' when next is '+' => TokenType.Increment,
                    '+' when next is '=' => TokenType.AddAssign,
                    '-' when next is '-' => TokenType.Decrement,
                    '-' when next is '=' => TokenType.SubAssign,
                    '*' when next is '=' => TokenType.MulAssign,
                    '/' when next is '=' => TokenType.DivAssign,
                    '%' when next is '=' => TokenType.ModAssign,
                    '^' when next is '=' => TokenType.XorAssign,
                    '^' when next is '^' => TokenType.Xor,
                    '&' when next is '=' => TokenType.AndAssign,
                    '&' when next is '&' => TokenType.And,
                    '|' when next is '=' => TokenType.OrAssign,
                    '|' when next is '|' => TokenType.Or,
                    '!' when next is '=' => TokenType.NotEqual,
                    '=' when next is '=' => TokenType.Equal,
                    '<' when next is '=' => TokenType.LessEqual,
                    '<' when next is '<' => TokenType.ShiftLeft,
                    '>' when next is '=' => TokenType.GreaterEqual,
                    '>' when next is '>' => TokenType.ShiftRight,
                    _ => TokenType.Unknown
                };
                length = 2;
                if (type != TokenType.Unknown)
                    return true;
            }

            length = 1;
            type = c switch {
                '+' => TokenType.Add,
                '-' => TokenType.Sub,
                '*' => TokenType.Mul,
                '/' => TokenType.Div,
                '%' => TokenType.Mod,
                '^' => TokenType.BitXor,
                '&' => TokenType.BitAnd,
                '|' => TokenType.BitOr,
                '!' => TokenType.Not,
                '=' => TokenType.Assign,
                '<' => TokenType.Less,
                '>' => TokenType.Greater,
                '?' => TokenType.Question,
                ':' => TokenType.Colon,
                '~' => TokenType.Negate,
                '.' => TokenType.Dot,
                ',' => TokenType.Comma,
                ';' => TokenType.Semicolon,
                '(' => TokenType.LParen,
                ')' => TokenType.RParen,
                '{' => TokenType.LBrace,
                '}' => TokenType.RBrace,
                '[' => TokenType.LBracket,
                ']' => TokenType.RBracket,
                _ => TokenType.Unknown
            };
            return type != TokenType.Unknown;
        }
        type = TokenType.Unknown;
        length = 0;
        return false;
    }

    public static bool IsLineBreak(char c)
    {
        return c is '\n' or '\r';
    }

    public static bool IsAttributeChar(char c)
    {
        return c is '#' or '@';
    }
}
