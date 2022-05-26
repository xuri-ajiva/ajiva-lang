using System.Diagnostics.CodeAnalysis;
using ajivac_lib.AST;

namespace ajivac_lib;

public interface IParser
{
    EaSt ParsValue();
    EaSt ParsValue(BuildInType numberType);
    ParenthesisEaSt ParsParenthesis();
    IdentifierEaSt ParsIdentifier();
    Token ParsBuildInType(out BuildInType buildInType);
    TypedIdentifierEaSt ParsTypedIdentifier();
    List<T> ParsCommaSeparatedList<T>(Func<T> parse);
    EaSt ParsAttribute();
    CallEaSt ParseFunctionCall();
    EaSt? ParsPrimary();
    EaSt ParsPreprocessor();
    ListOfEaSt<EaSt> ParsBlock();
    EaSt? ParsExpression();
    EaSt ParsVariableExpression();
    EaSt ParsFunctionDefinition();
    EaSt ParsArray();
}
public class Parser : IParser
{
    private readonly ILexer _lexer;

    public Parser(ILexer lexer)
    {
        _lexer = lexer;
    }

    public EaSt ParsValue()
    {
        var v = _lexer.LastValue;
        if (bool.TryParse(v, out _))
            return ParsValue(BuildInType.Bit);
        if (uint.TryParse(v, out _))
            return ParsValue(BuildInType.U32);
        if (ulong.TryParse(v, out _))
            return ParsValue(BuildInType.U64);
        if (int.TryParse(v, out _))
            return ParsValue(BuildInType.I32);
        if (long.TryParse(v, out _))
            return ParsValue(BuildInType.I64);
        if (float.TryParse(v, out _))
            return ParsValue(BuildInType.F32);
        if (double.TryParse(v, out _))
            return ParsValue(BuildInType.F64);
        if (char.TryParse(v, out _))
            return ParsValue(BuildInType.Chr);
        return ParsValue(BuildInType.Str);
    }

    public EaSt ParsValue(BuildInType numberType)
    {
        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
        EaSt res = numberType switch {
            BuildInType.I32 => new I32EaSt(_lexer.CurrentToken.Source, int.Parse(_lexer.LastValue)),
            BuildInType.I64 => new I64EaSt(_lexer.CurrentToken.Source, long.Parse(_lexer.LastValue)),
            BuildInType.U32 => new U32EaSt(_lexer.CurrentToken.Source, uint.Parse(_lexer.LastValue)),
            BuildInType.U64 => new U64EaSt(_lexer.CurrentToken.Source, ulong.Parse(_lexer.LastValue)),
            BuildInType.F32 => new F32EaSt(_lexer.CurrentToken.Source, float.Parse(_lexer.LastValue)),
            BuildInType.F64 => new F64EaSt(_lexer.CurrentToken.Source, double.Parse(_lexer.LastValue)),
            BuildInType.Chr => new ChrEaSt(_lexer.CurrentToken.Source, char.Parse(_lexer.LastValue)),
            BuildInType.Str => new StrEaSt(_lexer.CurrentToken.Source, _lexer.LastValue),
            BuildInType.Bit => new BitEaSt(_lexer.CurrentToken.Source, bool.Parse(_lexer.LastValue)),
            _ => throw new UnexpectedTokenException("Expected Number but got {Token}", _lexer.CurrentToken)
        };
        _lexer.ReadNextToken(); //eat number
        return res;
    }

    public ParenthesisEaSt ParsParenthesis()
    {
        var open = GuardAndEat(TokenType.LParen);
        var close = open.Type switch {
            TokenType.LBrace => TokenType.RBrace,
            TokenType.LBracket => TokenType.RBracket,
            TokenType.LParen => TokenType.RParen,
            _ => ThrowUnexpected(TokenType.LBrace)
        };
        var expression = ParsExpression();
        var closing = GuardAndEat(close);
        return new ParenthesisEaSt(open.Source.Append(closing.Source), expression);
    }

    public IdentifierEaSt ParsIdentifier()
    {
        GuardCurrentToken(TokenType.Identifier);

        var res = new IdentifierEaSt(_lexer.CurrentToken.Source, _lexer.LastIdentifier);
        _lexer.ReadNextToken(); //eat identifier
        return res;
    }

    public Token ParsBuildInType(out BuildInType buildInType)
    {
        var type = _lexer.CurrentToken;
        if (!LanguageDefinition.TryGetBuildInType(type.Type, out buildInType))
            throw new UnexpectedTokenException("Expected Type but got {Token}", _lexer.CurrentToken);
        _lexer.ReadNextToken(); //eat type
        return type;
    }

    public TypedIdentifierEaSt ParsTypedIdentifier()
    {
        var type = ParsBuildInType(out var buildInType);
        var identifier = ParsIdentifier();
        return new TypedIdentifierEaSt(
            type.Source.Append(identifier.Source),
            identifier,
            new BuildInTypeReference(buildInType)
        );
    }

    public List<T> ParsCommaSeparatedList<T>(Func<T?> parse)
    {
        var res = new List<T>();
        var first = parse();
        if (first is not null)
            res.Add(first);
        do
        {
            GuardCurrentToken(TokenType.Comma);
            _lexer.ReadNextToken(); //eat comma
            var item = parse();
            if (item is not null)
                res.Add(item);
        } while (_lexer.CurrentToken.Type == TokenType.Comma);
        return res;
    }

    public EaSt ParsAttribute()
    {
        var attrib = GuardAndEat(TokenType.Attribute);
        var identifier = ParsIdentifier();

        if (_lexer.CurrentToken.Type != TokenType.LParen)
            return new AttributeEaSt(attrib.Source.Append(identifier.Source), identifier, ListOfEaSt<EaSt>.Empty(identifier.Source with {
                Length = 0,
                Position = identifier.Source.Position + identifier.Source.Length
            }));

        var open = GuardAndEat(TokenType.LParen);
        var arguments = ParsCommaSeparatedList(ParsExpression);
        var close = GuardAndEat(TokenType.RParen);
        return new AttributeEaSt(identifier.Source, identifier, new ListOfEaSt<EaSt>(open.Source.Append(close.Source), arguments));
    }

    private void GuardCurrentToken(TokenType expect)
    {
        if (_lexer.CurrentToken.Type == expect) return;
        ThrowUnexpected(expect);
    }

    [DoesNotReturn]
    private T ThrowUnexpected<T>(T expect) => throw new UnexpectedTokenException($"Expected {expect} but got {_lexer.CurrentToken.Type}", _lexer.CurrentToken);

    public CallEaSt ParseFunctionCall()
    {
        var fctName = ParsIdentifier();
        var open = GuardAndEat(TokenType.LParen);
        var arguments = ParsCommaSeparatedList(ParsExpression);
        var closing = GuardAndEat(TokenType.RParen);
        return new CallEaSt(fctName.Source.Append(closing.Source), fctName, new ListOfEaSt<EaSt>(open.Source.Append(closing.Source), arguments));
    }

    public EaSt? ParsPrimary()
    {
        switch (_lexer.CurrentToken.Type)
        {
            case TokenType.LBrace:
                return ParsBlock();
            case TokenType.LBracket:
                return ParsArray();
            case TokenType.LParen:
                return ParsParenthesis();
            case TokenType.Attribute:
                return ParsAttribute();
            case TokenType.Preprocessor:
                return ParsPreprocessor();
            case TokenType.Value:
                return ParsValue();
            case TokenType.Fn:
                return ParsFunctionDefinition();
            case TokenType.I32 or TokenType.I64 or TokenType.U32 or TokenType.U64 or TokenType.F32 or TokenType.F64 or TokenType.Chr or TokenType.Str or TokenType.Bit:
                return ParsVariableExpression();
            case TokenType.Not or TokenType.Negate or TokenType.Increment or TokenType.Decrement:
                return ParsUnary();
            case TokenType.If:
                return ParsIf();
            case TokenType.Identifier:
                return ParsComplexIdentifier();
            default:
                _lexer.ReadNextToken(); // eat token
                return null;
            //ThrowUnexpected(TokenType.Unknown);
        }
    }

    private EaSt ParsUnary()
    {
        var op = _lexer.CurrentToken;
        _lexer.ReadNextToken(); //eat operator
        var operand = ParsExpression();
        if (operand is null)
            throw new UnexpectedTokenException("Expected operand but got {Token}", _lexer.CurrentToken);
        return new UnaryEaSt(op.Source.Append(operand.Source), GetUnOp(op.Type), operand);
    }

    private EaSt ParsComplexIdentifier()
    {
        var identifier = ParsIdentifier();
        if (_lexer.CurrentToken.Type != TokenType.LParen)
            return identifier;
        var open = GuardAndEat(TokenType.LParen);
        var arguments = ParsCommaSeparatedList(ParsExpression);
        var closing = GuardAndEat(TokenType.RParen);
        return new CallEaSt(identifier.Source, identifier, new ListOfEaSt<EaSt>(open.Source.Append(closing.Source), arguments));
    }

    private EaSt ParsIf()
    {
        var ifToken = GuardAndEat(TokenType.If);
        var condition = ParsParenthesis();
        var body = ParsBlock();
        return new IfEaSt(ifToken.Source.Append(condition.Source), condition, body);
    }

    public EaSt ParsPreprocessor()
    {
        var preprocessor = GuardAndEat(TokenType.Preprocessor);
        var identifier = _lexer.LastIdentifier;
        var idToken = GuardAndEat(TokenType.Identifier);

        switch (identifier)
        {
            case "pure":
                var afterPureSource = idToken.Source with {
                    Length = 0,
                    Position = idToken.Source.Position + idToken.Source.Length
                };
                return new FunctionEaSt(preprocessor.Source.Append(idToken.Source),
                    new FunctionSignatureEaSt(
                        afterPureSource,
                        new IdentifierEaSt(
                            afterPureSource,
                            Guid.NewGuid().ToString("N")
                        ),
                        ListOfEaSt<TypedIdentifierEaSt>.Empty(afterPureSource),
                        new BuildInTypeReference(BuildInType.Void)
                    ),
                    ParsBlock()
                );
            default:
                throw new UndefinedPreprocessorException(identifier);
        }
    }

    public ListOfEaSt<EaSt> ParsBlock()
    {
        var res = new List<EaSt>();

        void Add()
        {
            var item = ParsExpression();
            if (item is not null)
                res.Add(item);
        }

        var pos = _lexer.CurrentToken.Source;
        GuardAndEat(TokenType.LBrace);
        if (_lexer.CurrentToken.Type == TokenType.Star)
        {
            _lexer.ReadNextToken(); //eat star
            while (_lexer.CurrentToken.Type != TokenType.EOF)
            {
                Add();
            }
        }
        else
        {
            while (_lexer.CurrentToken.Type != TokenType.RBrace)
            {
                Add();
            }
            GuardAndEat(TokenType.RBrace);
        }
        if (res.Count > 0)
            pos = pos.Append(res.Last().Source);

        return new ListOfEaSt<EaSt>(pos, res);
    }

    public EaSt? ParsExpression()
    {
        var lhs = ParsPrimary();
        return lhs is null ? null : ParseBinOpRHS(0, lhs);
    }

    private EaSt? ParseBinOpRHS(int exprPrecedence, EaSt lhs)
    {
        while (true)
        {
            var token = _lexer.CurrentToken;
            var tokenPrecedence = _lexer.GetTokenPrecedence();

            // If this is a binop that binds at least as tightly as the current binop,
            // consume it, otherwise we are done.
            if (tokenPrecedence < exprPrecedence)
                return lhs;

            _lexer.ReadNextToken(); //eat operator
            var rhs = ParsPrimary();
            if (rhs is null)
                return null;

            var nextPrecedence = _lexer.GetTokenPrecedence();
            if (tokenPrecedence < nextPrecedence)
            {
                rhs = ParseBinOpRHS(tokenPrecedence + 1, rhs);
                if (rhs is null)
                    return null;
            }

            lhs = new BinaryEaSt(
                lhs.Source.Append(rhs.Source),
                GetBinOp(token.Type),
                lhs,
                rhs
            );
        }
    }

    private BinaryOperator GetBinOp(TokenType tokenType)
    {
        return tokenType switch {
            TokenType.Assign => BinaryOperator.Assign,
            TokenType.And => BinaryOperator.And,
            TokenType.Or => BinaryOperator.Or,
            TokenType.Xor => BinaryOperator.Xor,
            TokenType.Plus => BinaryOperator.Plus,
            TokenType.Minus => BinaryOperator.Minus,
            TokenType.Star => BinaryOperator.Multiply,
            TokenType.Slash => BinaryOperator.Divide,
            TokenType.Percent => BinaryOperator.Modulo,
            TokenType.DoubleEquals => BinaryOperator.Equal,
            TokenType.NotEqual => BinaryOperator.NotEqual,
            TokenType.Greater => BinaryOperator.Greater,
            TokenType.GreaterEqual => BinaryOperator.GreaterEqual,
            TokenType.Less => BinaryOperator.Less,
            TokenType.LessEqual => BinaryOperator.LessEqual,
            TokenType.ShiftLeft => BinaryOperator.ShiftLeft,
            TokenType.ShiftRight => BinaryOperator.ShiftRight,
            TokenType.Question => BinaryOperator.Question,
            TokenType.Colon => BinaryOperator.Colon,
            _ => throw new UnexpectedTokenException("Expected binary operator but got {Token}", _lexer.CurrentToken)
        };
    }

    private UnaryOperator GetUnOp(TokenType tokenType)
    {
        return tokenType switch {
            TokenType.Not => UnaryOperator.Not,
            TokenType.Negate => UnaryOperator.Negate,
            TokenType.Minus => UnaryOperator.Negative,
            TokenType.Plus => UnaryOperator.Positive,
            TokenType.Increment => UnaryOperator.Increment,
            TokenType.Decrement => UnaryOperator.Decrement,
            _ => throw new UnexpectedTokenException("Expected binary operator but got {Token}", _lexer.CurrentToken)
        };
    }

    public EaSt ParsVariableExpression()
    {
        var identifier = ParsTypedIdentifier();
        if (_lexer.CurrentToken.Type != TokenType.Assign)
            return new VariableEaSt(identifier.Source, identifier, null);

        _lexer.ReadNextToken(); //eat assignment
        var expression = ParsExpression();
        return new VariableEaSt(identifier.Source, identifier, expression);
    }

    public EaSt ParsFunctionDefinition()
    {
        GuardCurrentToken(TokenType.Fn);
        var fn = _lexer.CurrentToken;
        _lexer.ReadNextToken(); //eat Fn
        var name = ParsTypedIdentifier();
        var openParen = GuardAndEat(TokenType.LParen);
        var arguments = ParsCommaSeparatedList(ParsTypedIdentifier);
        var closeParen = GuardAndEat(TokenType.RParen);
        GuardAndEat(TokenType.LBrace);
        var body = ParsBlock();
        GuardAndEat(TokenType.RBrace);
        return new FunctionEaSt(
            fn.Source.Append(_lexer.CurrentToken.Source),
            new FunctionSignatureEaSt(
                fn.Source,
                name.Identifier,
                new ListOfEaSt<TypedIdentifierEaSt>(openParen.Source.Append(closeParen.Source), arguments),
                name.TypeReference
            ),
            body
        );
    }

    public EaSt ParsArray()
    {
        throw new NotImplementedException();
    }

    public Token GuardAndEat(TokenType type)
    {
        var token = _lexer.CurrentToken;
        GuardCurrentToken(type);
        _lexer.ReadNextToken(); //eat token
        return token;
    }
}
internal class UndefinedPreprocessorException : Exception
{
    public UndefinedPreprocessorException(string identifier) : base("Undefined preprocessor: " + identifier)
    {
    }
}
public class UnexpectedTokenException : Exception
{
    public Token Token { get; }

    public UnexpectedTokenException(string template, Token token) : base(string.Format(template, token.Type))
    {
        Token = token;
    }
}
