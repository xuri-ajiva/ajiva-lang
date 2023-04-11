using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ajivac_lib.AST;
using BinaryExpression = ajivac_lib.AST.BinaryExpression;
using UnaryExpression = ajivac_lib.AST.UnaryExpression;

namespace ajivac_lib.Semantics;

public interface IParser
{
    BaseNode ParseLiteral();
    IdentifierExpression ParseIdentifier();
    Token ParseBuildInType(out TypeReference typeReference);
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
    private readonly Diagnostics _diagnostics;

    public Parser(ILexer lexer, Diagnostics diagnostics)
    {
        _lexer = lexer;
        _diagnostics = diagnostics;
    }

    public BaseNode ParseLiteral()
    {
        return string.IsNullOrEmpty(_lexer.LastValue)
            ? ParseLocalVariableDeclaration()
            : ParseLiteralExpression(_lexer.LastValue);
    }

    private BaseNode ParseLiteralExpression(string literal)
    {
        var span = _lexer.CurrentToken.Span;
        TypeReference typeReference;

        if (int.TryParse(literal, out _))
            typeReference = TypeReference.I32;
        else if (long.TryParse(literal, out _))
            typeReference = TypeReference.I64;
        else if (bool.TryParse(literal, out _))
            typeReference = TypeReference.Bit;
        else if (uint.TryParse(literal, out _))
            typeReference = TypeReference.U32;
        else if (ulong.TryParse(literal, out _))
            typeReference = TypeReference.U64;
        else if (float.TryParse(literal, out _))
            typeReference = TypeReference.F32;
        else if (double.TryParse(literal, out _))
            typeReference = TypeReference.F64;
        else if (char.TryParse(literal, out _))
            typeReference = TypeReference.Chr;
        else typeReference = TypeReference.Str;
        _lexer.ReadNextToken(); //eat number
        return new LiteralExpression(span, literal, typeReference);
    }

    public IdentifierExpression ParseIdentifier()
    {
        GuardCurrentToken(TokenType.Identifier);
        var res = new IdentifierExpression(_lexer.CurrentToken.Span, _lexer.LastIdentifier);
        _lexer.ReadNextToken(); //eat identifier
        return res;
    }

    public Token ParseBuildInType(out TypeReference typeReference)
    {
        var type = _lexer.CurrentToken;
        if (!LanguageDefinition.TryGetBuildInType(type.Type, out typeReference))
            _diagnostics.ReportUnexpectedToken(_lexer.CurrentToken, "build in type");
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
            /*TypeReference.BuildIn*/(buildInType),
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
                _diagnostics.ReportUnexpectedToken(_lexer.CurrentToken, "comma separated list");
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
        _diagnostics.ReportUnexpectedToken(_lexer.CurrentToken, expect.ToString());
        //throw new UnexpectedTokenException($"Expected {expect} but got {_lexer.CurrentToken.Type}", _lexer.CurrentToken);
    }

    private Prototype? FindCallTarget(string calleeName)
    {
        // search defined functions first as they override extern declarations
        foreach (var functionDefinition in RuntimeState.FunctionDefinitions)
        {
            if (functionDefinition.Signature.Name == calleeName)
                return functionDefinition.Signature;
        }

        // search extern declarations
        foreach (var externDeclaration in RuntimeState.NativeFunctionDeclarations)
        {
            if (externDeclaration.Name == calleeName)
                return externDeclaration;
        }
        return null;
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
            fctName.Identifier, //FindCallTarget(fctName.Identifier) ?? throw new UnexpectedTokenException("Function {Name} Not Defined", fctName.Identifier),
            arguments);
    }

    public IAstNode? ParsePrimary()
    {
        var res = ParsePrimaryCore();
        _diagnostics.ReportAstNode(_lexer.CurrentToken.Span, res);
        return res;
    }

    private IAstNode? ParsePrimaryCore()
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
                return ParseLiteral();
            case TokenType.Fn:
                return LoadFunctionDefinition();
            case TokenType.True or TokenType.False:
                return ParseLiteralExpression(_lexer.CurrentToken.Type.ToString());
            case TokenType.I32 or TokenType.I64 or TokenType.U32 or TokenType.U64 or TokenType.F32 or TokenType.F64 or TokenType.Chr or TokenType.Str or TokenType.Bit:
                return ParseLiteral();
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
            case TokenType.While:
                return ParseWhile();
            case TokenType.Break:
                return ParseBreak();
            case TokenType.Continue:
                return ParseContinue();
            case TokenType.Semicolon:
                return ParseEmptyStatement();
            default:
                _diagnostics.ReportUnexpectedToken(_lexer.CurrentToken, "primary expression");
                _lexer.ReadNextToken(); // eat token
                return null;
        }
    }

    private IAstNode ParseEmptyStatement()
    {
        var token = _lexer.ReadNextToken();
        return new EmptyStatement(token.Span);
    }

    private IAstNode ParseBreak()
    {
        var breakToken = GuardAndEat(TokenType.Break);
        return new BreakStatement(breakToken.Span);
    }

    private IAstNode ParseContinue()
    {
        var continueToken = GuardAndEat(TokenType.Continue);
        return new ContinueStatement(continueToken.Span);
    }

    private IAstNode ParseWhile()
    {
        var whileToken = GuardAndEat(TokenType.While);
        var condition = ParseExpression();
        var body = ParseBlock();
        Debug.Assert(condition != null, nameof(condition) + " != null");
        return new WhileStatement(whileToken.Span.Append(body.Span), condition, body);
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
        GuardAndEat(TokenType.Native);
        var prototype = ParsePrototype(true);
        RuntimeState.NativeFunctionDeclarations.Add(prototype);
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
            _diagnostics.ReportUnexpectedToken(_lexer.CurrentToken, "expression operand");
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
                identifier.Identifier, //FindCallTarget(identifier.Identifier) ?? throw new UnexpectedTokenException("Function {Name} Not Defined", identifier.Identifier),
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

        //todo handle by external preprocessor
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
                    /*TypeReference.BuildIn*/(TypeReference.Void),
                    true
                );

                var functionDefinition = new FunctionDefinition(preprocessor.Span.Append(idToken.Span),
                    proto,
                    ParseBlock(),
                    true
                );
                RuntimeState.FunctionDefinitions.Add(functionDefinition);
                return functionDefinition;
            default:
                _diagnostics.ReportUndefinedPreprocessor(identifier, preprocessor.Span);
                return new EmptyStatement(preprocessor.Span);
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
        switch (tokenType)
        {
            case TokenType.Assign:
                return BinaryOperator.Assign;
            case TokenType.And:
                return BinaryOperator.And;
            case TokenType.Or:
                return BinaryOperator.Or;
            case TokenType.Xor:
                return BinaryOperator.Xor;
            case TokenType.Plus:
                return BinaryOperator.Plus;
            case TokenType.Minus:
                return BinaryOperator.Minus;
            case TokenType.Star:
                return BinaryOperator.Multiply;
            case TokenType.Slash:
                return BinaryOperator.Divide;
            case TokenType.Percent:
                return BinaryOperator.Modulo;
            case TokenType.DoubleEquals:
                return BinaryOperator.Equal;
            case TokenType.NotEqual:
                return BinaryOperator.NotEqual;
            case TokenType.Greater:
                return BinaryOperator.Greater;
            case TokenType.GreaterEqual:
                return BinaryOperator.GreaterEqual;
            case TokenType.Less:
                return BinaryOperator.Less;
            case TokenType.LessEqual:
                return BinaryOperator.LessEqual;
            case TokenType.ShiftLeft:
                return BinaryOperator.ShiftLeft;
            case TokenType.ShiftRight:
                return BinaryOperator.ShiftRight;
            case TokenType.Question:
                return BinaryOperator.Question;
            case TokenType.Colon:
                return BinaryOperator.Colon;
            default:
                _diagnostics.ReportUnexpectedToken(_lexer.CurrentToken, "binary operator");
                throw new Exception();
        }
    }

    private UnaryOperator GetUnOp(TokenType tokenType)
    {
        switch (tokenType)
        {
            case TokenType.Not:
                return UnaryOperator.Not;
            case TokenType.Negate:
                return UnaryOperator.Negate;
            case TokenType.Minus:
                return UnaryOperator.Negative;
            case TokenType.Plus:
                return UnaryOperator.Positive;
            case TokenType.Increment:
                return UnaryOperator.Increment;
            case TokenType.Decrement:
                return UnaryOperator.Decrement;
            default:
                _diagnostics.ReportUnexpectedToken(_lexer.CurrentToken, "unary operator");
                throw new Exception();
        }
    }

    public BaseNode LoadFunctionDefinition()
    {
        var fn = GuardAndEat(TokenType.Fn);
        var proto = ParsePrototype(false);
        var def = new FunctionDefinition(
            fn.Span.Append(_lexer.CurrentToken.Span),
            proto
        );
        RuntimeState.FunctionDefinitions.Add(def);
        var body = ParseBlock();
        def.Body = body;
        return def;
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
            /*TypeReference.BuildIn*/(buildInType)
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
        return new RootNode(_lexer.CurrentToken.Span with {
                Length = _lexer.CurrentToken.Span.Position,
                Position = 0,
            },
            children.Where(x => x is not null).Cast<IAstNode>().ToList());
    }
}
public class RuntimeStateHolder
{
    public List<Prototype> NativeFunctionDeclarations { get; set; } = new();
    public List<FunctionDefinition> FunctionDefinitions { get; set; } = new();
}
