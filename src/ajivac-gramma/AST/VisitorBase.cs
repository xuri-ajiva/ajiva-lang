using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace ajivac_lib.AST;

public class AstVisitorBase<TResult, TArg>
    : IAstVisitor<TResult, TArg>
    where TResult : struct where TArg : struct
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private TResult VisitChildren(IAstNode? node, ref TArg arg)
    {
        throw new UnreachableException("Their was a call to the baseVisitor");
    }

    /// <inheritdoc />
    public virtual TResult Visit(RootNode node, ref TArg arg) => VisitChildren(node, ref arg);

    /// <inheritdoc />
    public virtual TResult Visit<T>(ValueExpression<T> node, ref TArg arg) => VisitChildren(node, ref arg);

    /// <inheritdoc />
    public virtual TResult Visit(IdentifierExpression node, ref TArg arg) => VisitChildren(node, ref arg);

    /// <inheritdoc />
    public virtual TResult Visit(Prototype node, ref TArg arg) => VisitChildren(node, ref arg);

    /// <inheritdoc />
    public virtual TResult Visit(LocalVariableDeclaration node, ref TArg arg) => VisitChildren(node, ref arg);

    /// <inheritdoc />
    public virtual TResult Visit(BinaryExpression node, ref TArg arg) => VisitChildren(node, ref arg);

    /// <inheritdoc />
    public virtual TResult Visit(UnaryExpression node, ref TArg arg) => VisitChildren(node, ref arg);

    /// <inheritdoc />
    public virtual TResult Visit(FunctionCallExpression node, ref TArg arg) => VisitChildren(node, ref arg);

    /// <inheritdoc />
    public virtual TResult Visit(FunctionDefinition node, ref TArg arg) => VisitChildren(node, ref arg);

    /// <inheritdoc />
    public virtual TResult Visit(ParameterDeclaration node, ref TArg arg) => VisitChildren(node, ref arg);

    /// <inheritdoc />
    public virtual TResult Visit(AttributeEaSt node, ref TArg arg) => VisitChildren(node, ref arg);

    /// <inheritdoc />
    public virtual TResult Visit(IfExpression node, ref TArg arg) => VisitChildren(node, ref arg);

    /// <inheritdoc />
    public virtual TResult Visit(AssignmentExpression node, ref TArg arg) => VisitChildren(node, ref arg);

    /// <inheritdoc />
    public virtual TResult Visit(BreakStatement node, ref TArg arg) => VisitChildren(node, ref arg);

    /// <inheritdoc />
    public virtual TResult Visit(ReturnStatement node, ref TArg arg) => VisitChildren(node, ref arg);

    /// <inheritdoc />
    public virtual TResult Visit(WhileStatement node, ref TArg arg) => VisitChildren(node, ref arg);

    /// <inheritdoc />
    public virtual TResult Visit(ForStatement node, ref TArg arg) => VisitChildren(node, ref arg);

    /// <inheritdoc />
    public virtual TResult Visit(ContinueStatement node, ref TArg arg) => VisitChildren(node, ref arg);

    /// <inheritdoc />
    public TResult Default(ref TArg arg)
    {
        return default;
    }
}
