using ajivac.Tokenizer;

namespace ajivac.TokenDefinitions;

public class BeginWithSyntaxDefinition : SyntaxDefinition
{
    private readonly string _begin;

    /// <inheritdoc />
    public override TokenMatch Match(TextSpan input)
    {
        for (int i = 0; i < _begin.Length; i++)
        {
            if (input[i] != _begin[i])
            {
                return Fail();
            }
        }
        return Succeed(input, _begin.Length);
    }

    /// <inheritdoc />
    public BeginWithSyntaxDefinition(SyntaxType returnsToken, string begin) : base(returnsToken)
    {
        _begin = begin;
    }
}