namespace ajivac_lib.AST;

public interface IAstNode
{
    SourceSpan Span { get; }
    IEnumerable<IAstNode> Children { get; }

    TResult? Accept<TResult>( IAstVisitor<TResult> visitor )
        where TResult : class;
}

public interface IVariableDeclaration : IAstNode
{
    string Name { get; }

    bool IsCompilerGenerated { get; }
}

public interface IExpression : IAstNode
{
}
