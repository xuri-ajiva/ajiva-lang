using ajivac.Tokenizer;

namespace ajivac.TokenDefinitions;

public abstract class SyntaxDefinition
{
    private SyntaxType _returnsToken;

    public SyntaxDefinition(SyntaxType returnsToken)
    {
        _returnsToken = returnsToken;
    }

    protected TokenMatch Fail() => new TokenMatch() { IsMatch = false, TokenType = _returnsToken };
    public TokenMatch Succeed(TextSpan input, int length, int start = 0) => new TokenMatch() { IsMatch = true, Value = input.Slice(start, length), TokenType = _returnsToken };
    public TokenMatch Succeed() => new TokenMatch() { IsMatch = true, TokenType = _returnsToken };
    public abstract TokenMatch Match(TextSpan input);
}