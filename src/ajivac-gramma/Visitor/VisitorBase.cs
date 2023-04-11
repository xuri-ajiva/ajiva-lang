using System.Diagnostics;
using System.Runtime.CompilerServices;
using ajivac_lib.AST;
using BinaryExpression = ajivac_lib.AST.BinaryExpression;
using UnaryExpression = ajivac_lib.AST.UnaryExpression;

namespace ajivac_lib.Visitor;

public class AstVisitorBase<TResult, TArg>
    : IAstVisitor<TResult, TArg>
    where TResult : struct where TArg : struct
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual TResult VisitChildren(IAstNode? node, ref TArg arg)
    {
        throw new UnreachableException("Their was a call to the baseVisitor");
    }

    /// <inheritdoc />
    public virtual TResult Visit(RootNode node, ref TArg arg) => VisitChildren(node, ref arg);

    /// <inheritdoc />
    public virtual TResult Visit(LiteralExpression node, ref TArg arg) => VisitChildren(node, ref arg);

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
    public virtual TResult Default(ref TArg arg) => default;
}
