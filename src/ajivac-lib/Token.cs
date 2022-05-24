namespace ajivac_lib;

public class Token
{
    public TokenType Type { get; }
    public string Value { get; }
    public TokenLocation Location { get; set; }

    public Token(TokenType type, string value, TokenLocation location)
    {
        Location = location;
        Type = type;
        Value = value;
    }

}
public struct TokenLocation
{
    public TokenLocation(int line, int column, string file)
    {
        this.Line = line;
        this.Column = column;
        this.File = file;
    }

    public int Line { get; set; }
    public int Column { get; set; }
    public string File { get; set; }
    
}
public enum TokenType
{
    // ReSharper disable once InconsistentNaming
    EOF,

    Unknown,
    
    
    BeginScope,
    EndScope,

    Attribute,
    Identifier,

    I32,
    U32,
    I64,
    U64,
    F32,
    F64,
    
    

    Chr,
    Str,
    Bit,
    
    If,
    Else,

    For,
    While,
    Break,
    Continue,

    Fn,
    Return,
    
    
    Void,
    True,
    False,
    Null,
    Number,


    BracketLeft,
    BracketRight,
    BraceLeft,
    BraceRight,
    SqBracketLeft,
    SqBracketRight,
    AngleBracketLeft,
    AngleBracketRight,

    Comma,
    Dot,
    Semicolon,
}
