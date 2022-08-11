using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection.Emit;
using ajivac_lib.AST;
using BinaryExpression = ajivac_lib.AST.BinaryExpression;
using UnaryExpression = ajivac_lib.AST.UnaryExpression;

namespace ajivac_lib;

public interface IParser
{
    BaseNode ParseValue();
    BaseNode ParseValue(BuildInType numberType);
    IdentifierExpression ParseIdentifier();
    Token ParseBuildInType(out BuildInType buildInType);
    LocalVariableDeclaration ParseLocalVariableDeclaration();
    List<T> ParseCommaSeparatedList<T>(Func<T?> parse, TokenType endOfList = TokenType.RParen);
    BaseNode ParseAttribute();
    FunctionCallExpression ParseFunctionCall();
    IAstNode? ParsePrimary();
    IExpression ParseParenthesis();
    BaseNode ParseUnary();
    BaseNode ParseComplexIdentifier();
    BaseNode ParseIf();
    BaseNode ParsePreprocessor();
    IAstNode ParseBlock();
    IExpression? ParseExpression();
    BaseNode LoadFunctionDefinition();
    BaseNode ParsArray();
    Token GuardAndEat(TokenType type);
    IAstNode ParseAll();
}
public class Parser : IParser
{
    private readonly ILexer _lexer;

    public Parser(ILexer lexer)
    {
        _lexer = lexer;
    }

    public BaseNode ParseValue()
    {
        var v = _lexer.LastValue;
        if (string.IsNullOrEmpty(v))
            return ParseLocalVariableDeclaration();

        if (int.TryParse(v, out _))
            return ParseValue(BuildInType.I32);
        if (long.TryParse(v, out _))
            return ParseValue(BuildInType.I64);
        if (bool.TryParse(v, out _))
            return ParseValue(BuildInType.Bit);
        if (uint.TryParse(v, out _))
            return ParseValue(BuildInType.U32);
        if (ulong.TryParse(v, out _))
            return ParseValue(BuildInType.U64);
        if (float.TryParse(v, out _))
            return ParseValue(BuildInType.F32);
        if (double.TryParse(v, out _))
            return ParseValue(BuildInType.F64);
        if (char.TryParse(v, out _))
            return ParseValue(BuildInType.Chr);
        return ParseValue(BuildInType.Str);
    }

    public BaseNode ParseValue(BuildInType numberType)
    {
        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
        BaseNode res = numberType switch {
            BuildInType.I32 => new ValueExpression<int>(_lexer.CurrentToken.Span, int.Parse(_lexer.LastValue)),
            BuildInType.I64 => new ValueExpression<long>(_lexer.CurrentToken.Span, long.Parse(_lexer.LastValue)),
            BuildInType.U32 => new ValueExpression<uint>(_lexer.CurrentToken.Span, uint.Parse(_lexer.LastValue)),
            BuildInType.U64 => new ValueExpression<ulong>(_lexer.CurrentToken.Span, ulong.Parse(_lexer.LastValue)),
            BuildInType.F32 => new ValueExpression<float>(_lexer.CurrentToken.Span, float.Parse(_lexer.LastValue)),
            BuildInType.F64 => new ValueExpression<double>(_lexer.CurrentToken.Span, double.Parse(_lexer.LastValue)),
            BuildInType.Chr => new ValueExpression<char>(_lexer.CurrentToken.Span, char.Parse(_lexer.LastValue)),
            BuildInType.Str => new ValueExpression<string>(_lexer.CurrentToken.Span, (_lexer.LastValue)),
            BuildInType.Bit => new ValueExpression<bool>(_lexer.CurrentToken.Span, bool.Parse(_lexer.LastValue)),
            _ => throw new UnexpectedTokenException("Expected Number but got {Token}", _lexer.CurrentToken)
        };
        _lexer.ReadNextToken(); //eat number
        return res;
    }

    public IdentifierExpression ParseIdentifier()
    {
        GuardCurrentToken(TokenType.Identifier);
        var res = new IdentifierExpression(_lexer.CurrentToken.Span, _lexer.LastIdentifier);
        _lexer.ReadNextToken(); //eat identifier
        return res;
    }

    public Token ParseBuildInType(out BuildInType buildInType)
    {
        var type = _lexer.CurrentToken;
        if (!LanguageDefinition.TryGetBuildInType(type.Type, out buildInType))
            throw new UnexpectedTokenException("Expected Type but got {Token}", _lexer.CurrentToken);
        _lexer.ReadNextToken(); //eat type
        return type;
    }

    public LocalVariableDeclaration ParseLocalVariableDeclaration()
    {
        var type = ParseBuildInType(out var buildInType);
        var identifier = ParseIdentifier();
        IExpression? expression = null;
        if (_lexer.CurrentToken.Type == TokenType.Assign)
        {
            _lexer.ReadNextToken(); //eat =
            expression = ParseExpression();
        }
        return new LocalVariableDeclaration(
            type.Span.Append(identifier.Span),
            identifier.Identifier,
            new BuildInTypeReference(buildInType),
            expression
        );
    }

    public List<T> ParseCommaSeparatedList<T>(Func<T?> parse, TokenType endOfList = TokenType.RParen)
    {
        var res = new List<T>();

        while (_lexer.CurrentToken.Type != endOfList)
        {
            var item = parse();
            if (item == null)
                throw new UnexpectedTokenException("Expected {Token} but got", _lexer.CurrentToken);
            res.Add(item);
            if (_lexer.CurrentToken.Type == TokenType.Comma)
                _lexer.ReadNextToken(); //eat comma
        }

        return res;
    }

    public BaseNode ParseAttribute()
    {
        var attrib = GuardAndEat(TokenType.Attribute);
        var identifier = ParseIdentifier();

        List<IExpression>? arguments = null;
        var span = attrib.Span.Append(identifier.Span);
        if (_lexer.CurrentToken.Type == TokenType.LParen)
        {
            var open = GuardAndEat(TokenType.LParen);
            arguments = ParseCommaSeparatedList(ParseExpression);
            var close = GuardAndEat(TokenType.RParen);
            span = identifier.Span.Append(close.Span);
        }
        var operand = ParsePrimary();
        return new AttributeEaSt(span, identifier.Identifier, operand, arguments);
    }

    private void GuardCurrentToken(TokenType expect)
    {
        if (_lexer.CurrentToken.Type == expect) return;
        ThrowUnexpected(expect);
    }

    [DoesNotReturn]
    private T ThrowUnexpected<T>(T expect) => throw new UnexpectedTokenException($"Expected {expect} but got {_lexer.CurrentToken.Type}", _lexer.CurrentToken);

    private Prototype? FindCallTarget(string calleeName)
    {
        // search defined functions first as they override extern declarations
        if (RuntimeState.FunctionDefinitions.TryGetValue(calleeName, out FunctionDefinition? definition))
        {
            return definition.Signature;
        }

        // search extern declarations
        return RuntimeState.NativeFunctionDeclarations.TryGetValue(calleeName, out Prototype? declaration)
            ? declaration
            : null;
    }

    public RuntimeStateHolder RuntimeState { get; } = new();

    public FunctionCallExpression ParseFunctionCall()
    {
        var fctName = ParseIdentifier();
        var open = GuardAndEat(TokenType.LParen);
        var arguments = ParseCommaSeparatedList(ParseExpression);
        var closing = GuardAndEat(TokenType.RParen);
        return new FunctionCallExpression(
            fctName.Span.Append(closing.Span),
            FindCallTarget(fctName.Identifier) ?? throw new UnexpectedTokenException("Function {Name} Not Defined", fctName.Identifier),
            arguments);
    }

    public IAstNode? ParsePrimary()
    {
        switch (_lexer.CurrentToken.Type)
        {
            case TokenType.LBrace:
                return ParseBlock();
            case TokenType.LBracket:
                return ParsArray();
            case TokenType.LParen:
                return ParseParenthesis();
            case TokenType.Attribute:
                return ParseAttribute();
            case TokenType.Preprocessor:
                return ParsePreprocessor();
            case TokenType.Value:
                return ParseValue();
            case TokenType.Fn:
                return LoadFunctionDefinition();
            case TokenType.I32 or TokenType.I64 or TokenType.U32 or TokenType.U64 or TokenType.F32 or TokenType.F64 or TokenType.Chr or TokenType.Str or TokenType.Bit:
                return ParseValue();
            case TokenType.Not or TokenType.Negate or TokenType.Increment or TokenType.Decrement:
                return ParseUnary();
            case TokenType.If:
                return ParseIf();
            case TokenType.Identifier:
                return ParseComplexIdentifier();
            case TokenType.Native:
                return LoadNativeDefinition();
            case TokenType.Return:
                return ParseReturn();
            case TokenType.For:
                return ParseFor();
            default:
                throw new ($"Unexpected token {_lexer.CurrentToken.Type}");
                _lexer.ReadNextToken(); // eat token
                return null;
            //ThrowUnexpected(TokenType.Unknown);
        }
    }

    private IAstNode ParseFor()
    {
        var forToken = GuardAndEat(TokenType.For);
        var open = GuardAndEat(TokenType.LParen);
        var init = ParsePrimary();
        var condition = ParseExpression();
        var increment = ParsePrimary();
        var close = GuardAndEat(TokenType.RParen);
        var body = ParseBlock();
        Debug.Assert(init != null, nameof(init) + " != null");
        Debug.Assert(condition != null, nameof(condition) + " != null");
        Debug.Assert(increment != null, nameof(increment) + " != null");
        return new ForStatement(forToken.Span.Append(close.Span), init, condition, increment, body);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return _lexer?.CurrentToken?.ToString() ?? "<no instruction>";
    }

    private IAstNode ParseReturn()
    {
        var returnToken = GuardAndEat(TokenType.Return);
        var expression = ParseExpression();
        return new ReturnStatement(returnToken.Span.Append(expression.Span), expression);
    }

    private IAstNode LoadNativeDefinition()
    {
        var native = GuardAndEat(TokenType.Native);
        var prototype = ParsePrototype(true);
        RuntimeState.NativeFunctionDeclarations.Add(prototype.Name, prototype);
        return prototype;
    }

    public IExpression ParseParenthesis()
    {
        GuardAndEat(TokenType.LParen);
        var res = ParseExpression();
        GuardAndEat(TokenType.RParen);
        return res!;
    }

    public BaseNode ParseUnary()
    {
        var op = _lexer.CurrentToken;
        _lexer.ReadNextToken(); //eat operator
        var operand = ParseExpression();
        if (operand is null)
            throw new UnexpectedTokenException("Expected operand but got {Token}", _lexer.CurrentToken);
        return new UnaryExpression(op.Span.Append(operand.Span), GetUnOp(op.Type), operand);
    }

    public BaseNode ParseComplexIdentifier()
    {
        var identifier = ParseIdentifier();
        if (_lexer.CurrentToken.Type == TokenType.LParen)
        {
            var open = GuardAndEat(TokenType.LParen);
            var arguments = ParseCommaSeparatedList(ParseExpression);
            var closing = GuardAndEat(TokenType.RParen);
            return new FunctionCallExpression(
                identifier.Span.Append(closing.Span),
                FindCallTarget(identifier.Identifier) ?? throw new UnexpectedTokenException("Function {Name} Not Defined", identifier.Identifier),
                arguments);
        }
        if (_lexer.CurrentToken.Type == TokenType.Assign)
        {
            var assign = GuardAndEat(TokenType.Assign);
            var expression = ParseExpression();
            return new AssignmentExpression(identifier.Span.Append(assign.Span), identifier.Identifier, expression);
        }
        return identifier;
    }

    public BaseNode ParseIf()
    {
        var ifToken = GuardAndEat(TokenType.If);
        var condition = ParseParenthesis();
        var body = ParseBlock();
        if (_lexer.CurrentToken.Type == TokenType.Else)
        {
            var elseToken = GuardAndEat(TokenType.Else);
            var elseBody = ParseBlock();
            return new IfExpression(
                ifToken.Span.Append(elseToken.Span),
                condition,
                body,
                elseBody,
                null
            );
        }
        return new IfExpression(
            ifToken.Span.Append(condition.Span),
            condition,
            body,
            null,
            null
        );
    }

    public BaseNode ParsePreprocessor()
    {
        var preprocessor = GuardAndEat(TokenType.Preprocessor);
        var identifier = _lexer.LastIdentifier;
        var idToken = GuardAndEat(TokenType.Identifier);

        switch (identifier)
        {
            case "pure":
                var afterPureSource = idToken.Span with {
                    Length = 0,
                    Position = idToken.Span.Position + idToken.Span.Length
                };
                var proto = new Prototype(
                    afterPureSource,
                    Guid.NewGuid().ToString("N"),
                    false,
                    new List<ParameterDeclaration>(),
                    new BuildInTypeReference(BuildInType.Void),
                    true
                );

                var functionDefinition = new FunctionDefinition(preprocessor.Span.Append(idToken.Span),
                    proto,
                    ParseBlock(),
                    true
                );
                RuntimeState.FunctionDefinitions.Add(proto.Name, functionDefinition);
                return functionDefinition;
            default:
                throw new UndefinedPreprocessorException(identifier);
        }
    }

    public IAstNode ParseBlock()
    {
        var children = new List<IAstNode>();

        void Add()
        {
            var item = ParsePrimary();
            if (item is not null)
                children.Add(item);
        }

        var pos = _lexer.CurrentToken.Span;
        //we have a singel line block
        if (_lexer.CurrentToken.Type != TokenType.LBrace)
            return ParsePrimary()!;

        GuardAndEat(TokenType.LBrace);
        if (_lexer.CurrentToken.Type == TokenType.Star)
        {
            _lexer.ReadNextToken(); //eat star
            while (_lexer.CurrentToken.Type != TokenType.EOF)
            {
                Add();
            }
            if (children.Count > 0)
                pos = pos.Append(children.Last().Span);
        }
        else
        {
            while (_lexer.CurrentToken.Type != TokenType.RBrace)
            {
                Add();
            }
            var close = GuardAndEat(TokenType.RBrace);
            pos = pos.Append(close.Span);
        }
        return new RootNode(pos, children);
    }

    public IExpression? ParseExpression()
    {
        var lhs = ParsePrimary();
        return lhs is null ? null : ParseBinOpRHS(0, lhs);
    }

    private IExpression? ParseBinOpRHS(int exprPrecedence, IAstNode lhs)
    {
        while (true)
        {
            var token = _lexer.CurrentToken;
            var tokenPrecedence = _lexer.GetTokenPrecedence();

            // If this is a binop that binds at least as tightly as the current binop,
            // consume it, otherwise we are done.
            if (tokenPrecedence < exprPrecedence)
                return (IExpression)lhs;

            _lexer.ReadNextToken(); //eat operator
            var rhs = ParsePrimary();
            if (rhs is null)
                return null;

            var nextPrecedence = _lexer.GetTokenPrecedence();
            if (tokenPrecedence < nextPrecedence)
            {
                rhs = ParseBinOpRHS(tokenPrecedence + 1, rhs);
                if (rhs is null)
                    return null;
            }

            lhs = new BinaryExpression(
                lhs.Span.Append(rhs.Span),
                GetBinOp(token.Type),
                (IExpression)lhs,
                (IExpression)rhs
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

    public BaseNode LoadFunctionDefinition()
    {
        var fn = GuardAndEat(TokenType.Fn);
        var proto = ParsePrototype(false);
        //for recursive functions we need to add the function to the list of functions
        RuntimeState.FunctionDefinitions.Add(proto.Name, new FunctionDefinition(
            proto.Span.Append(_lexer.CurrentToken.Span),
            proto,
            null!
        ));
        var body = ParseBlock();
        var functionDefinition = new FunctionDefinition(
            proto.Span.Append(_lexer.CurrentToken.Span),
            proto,
            body
        );
        //update the function definition in the list of functions
        RuntimeState.FunctionDefinitions[proto.Name] = functionDefinition;
        return functionDefinition;
    }

    private Prototype ParsePrototype(bool isExtern)
    {
        var type = ParseBuildInType(out var buildInType);
        var identifier = ParseIdentifier();
        var openParen = GuardAndEat(TokenType.LParen);
        var i = 0;
        var arguments = ParseCommaSeparatedList(() =>
        {
            var (sourceSpan, name, typeReference, initializer, isCompilerGenerated) = ParseLocalVariableDeclaration();
            return new ParameterDeclaration(sourceSpan, i++, name, typeReference, initializer, isCompilerGenerated);
        });
        var closeParen = GuardAndEat(TokenType.RParen);
        var proto = new Prototype(
            type.Span.Append(closeParen.Span),
            identifier.Identifier,
            isExtern,
            arguments,
            new BuildInTypeReference(buildInType)
        );
        return proto;
    }

    public BaseNode ParsArray()
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

    /// <inheritdoc />
    public IAstNode ParseAll()
    {
        _lexer.ReadNextToken(); // lead first token
        var children = new List<IAstNode?>();
        while (_lexer.CurrentToken.Type != TokenType.EOF)
        {
            children.Add(ParsePrimary());
        }
        return new RootNode(_lexer.CurrentToken.Span with { Length = _lexer.CurrentToken.Span.Position, Position = 0, },
            children.Where(x => x is not null).Cast<IAstNode>().ToList());
    }
}
public class RuntimeStateHolder
{
    public Dictionary<string, Prototype> NativeFunctionDeclarations { get; set; } = new();
    public Dictionary<string, FunctionDefinition> FunctionDefinitions { get; set; } = new();
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

    public UnexpectedTokenException(string template, params object[] args) : base(string.Format(template, args))
    {
    }
}
