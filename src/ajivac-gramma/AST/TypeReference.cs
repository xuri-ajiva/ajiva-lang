using System.Text;

namespace ajivac_lib.AST;

public abstract record TypeReference;
public record BuildInTypeReference(BuildInType Type) : TypeReference;
