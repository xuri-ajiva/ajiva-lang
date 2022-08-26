using System.Text;

namespace ajivac_lib.AST;

public abstract record TypeReference
{
    public abstract string Identifier { get; }
}
public record BuildInTypeReference(BuildInType Type) : TypeReference
{
    /// <inheritdoc />
    public override string Identifier => Type.ToString();
}
