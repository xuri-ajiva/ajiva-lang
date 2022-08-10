using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace ajivac_lib.AST;

public class AstVisitorBase<TResult>
    : IAstVisitor<TResult>
    where TResult : class
{
    private IAstVisitor<TResult> _astVisitorImplementation;

    public virtual TResult? VisitChildren(IAstNode? node)
    {
        if (node is null) ThrowHelper.ThrowArgumentNullException(nameof(node));

        var result = DefaultResult;
        foreach (var child in node.Children)
        {
            result = AggregateResult(result, child.Accept(this));
        }
        return result;
    }

    protected AstVisitorBase([AllowNull] TResult defaultResult)
    {
        DefaultResult = defaultResult;
    }

    protected virtual TResult? AggregateResult(TResult? aggregate, TResult? newResult)
    {
        return newResult;
    }

    protected TResult? DefaultResult { get; }

    /// <inheritdoc />
    public TResult Visit(RootNode node) => VisitChildren(node);

    /// <inheritdoc />
    public TResult Visit<T>(ValueExpression<T> node) => VisitChildren(node);

    /// <inheritdoc />
    public TResult Visit(IdentifierExpression node) => VisitChildren(node);

    /// <inheritdoc />
    public TResult Visit(Prototype node) => VisitChildren(node);

    /// <inheritdoc />
    public TResult Visit(LocalVariableDeclaration node) => VisitChildren(node);

    /// <inheritdoc />
    public TResult Visit(BinaryExpression node) => VisitChildren(node);

    /// <inheritdoc />
    public TResult Visit(UnaryExpression node) => VisitChildren(node);

    /// <inheritdoc />
    public TResult Visit(FunctionCallExpression node) => VisitChildren(node);

    /// <inheritdoc />
    public TResult Visit(FunctionDefinition node) => VisitChildren(node);

    /// <inheritdoc />
    public TResult Visit(ParameterDeclaration node) => VisitChildren(node);

    /// <inheritdoc />
    public TResult Visit(AttributeEaSt node) => VisitChildren(node);

    /// <inheritdoc />
    public TResult Visit(IfExpression node) => VisitChildren(node);

    /// <inheritdoc />
    public TResult Visit(AssignmentExpression node) => VisitChildren(node);
}
