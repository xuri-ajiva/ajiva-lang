namespace ajivac_lib.AST;

public interface IAstNode
{
    SourceSpan Span { get; }
    IEnumerable<IAstNode> Children { get; }

    TResult Accept<TResult, TArg>(IAstVisitor<TResult, TArg> visitor, ref TArg arg)
        where TResult : struct where TArg : struct;
}
public interface IVariableDeclaration : IAstNode
{
    string Name { get; }

    bool IsCompilerGenerated { get; }
}
public interface IExpression : IAstNode
{
}
