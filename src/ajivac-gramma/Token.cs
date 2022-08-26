using ajivac_lib.AST;

namespace ajivac_lib;

public class Token
{
    public TokenType Type { get; }
    public SourceSpan Span { get; set; }
    public ReadOnlySpan<char> GetValue()
    {
        return Span.GetValue();
    }

    public Token(TokenType type, SourceSpan sourceSpan)
    {
        Type = type;
        Span = sourceSpan;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{nameof(Type)}: {Type}, {nameof(Span)}: {Span}";
    }
}
public enum TokenType
{
    // ReSharper disable once InconsistentNaming
    EOF,

    Unknown,

    Attribute,
    Preprocessor,
    Identifier,
    Native,

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
    Value,
    

    Comma,
    Dot,
    Semicolon,
    
    Plus,
    Minus,
    Star,
    Slash,
    Percent,
    DoubleEquals,
    NotEqual,
    Greater,
    GreaterEqual,
    Less,
    LessEqual,
    And,
    Or,
    Xor,
    BitAnd,
    BitOr,
    BitXor,
    
    ShiftLeft,
    ShiftRight,
    Question,
    Colon,
    
    Not,
    Negate,
    Increment,
    Decrement,
    
    Assign,
    AddAssign,
    SubAssign,
    MulAssign,
    DivAssign,
    ModAssign,
    AndAssign,
    OrAssign,
    XorAssign,

    LParen,
    RParen,
    LBrace,
    RBrace,
    LBracket,
    RBracket
}
