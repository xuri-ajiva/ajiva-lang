using System.Text;

namespace ajivac_lib.AST;

public abstract record BaseNode(SourceSpan Span) : IAstNode
{
    public void Print(StringBuilder stringBuilder) => PrintMembers(stringBuilder);

    protected bool PrintList(StringBuilder builder, string name, IEnumerable<IAstNode> astNodes)
    {
        builder.Append("Children = [");
        bool first = true;
        foreach (var astNode in Children)
        {
            if (first)
                first = false;
            else
                builder.Append(", ");
            builder.Append(astNode);
        }
        builder.Append(']');
        return true;
    }

    protected virtual bool PrintMembers(StringBuilder builder)
    {
        return false;
    }

    public abstract TResult? Accept<TResult>(IAstVisitor<TResult> visitor) where TResult : class;

    /// <inheritdoc />
    public abstract IEnumerable<IAstNode> Children { get; }
}
public record RootNode(SourceSpan Span, IEnumerable<IAstNode> Childs) : BaseNode(Span)
{
    /// <inheritdoc />
    public override TResult? Accept<TResult>(IAstVisitor<TResult> visitor) where TResult : class => visitor.Visit(this);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> Children => Childs;

    protected override bool PrintMembers(StringBuilder builder)
    {
        return PrintList(builder, nameof(Children), Children);
    }
}
public record ValueExpression<T>(SourceSpan Span, T Value) : BaseNode(Span), IExpression
{
    /// <inheritdoc />
    public override TResult? Accept<TResult>(IAstVisitor<TResult> visitor) where TResult : class => visitor.Visit(this);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> Children => Enumerable.Empty<IAstNode>();

    /// <inheritdoc />
    protected override bool PrintMembers(StringBuilder builder)
    {
        builder.Append(nameof(Value));
        builder.Append(" = ");
        builder.Append(Value);
        builder.Append(", ");
        builder.Append(nameof(Type));
        builder.Append(" = ");
        builder.Append(typeof(T));
        return true;
    }
}
public record IdentifierExpression(SourceSpan Span, string Identifier) : BaseNode(Span), IExpression
{
    /// <inheritdoc />
    public override TResult? Accept<TResult>(IAstVisitor<TResult> visitor) where TResult : class => visitor.Visit(this);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> Children => Enumerable.Empty<IAstNode>();
}
public record LocalVariableDeclaration(SourceSpan Span, string Name, TypeReference TypeReference, IExpression? Initializer, bool IsCompilerGenerated = false)
    : BaseNode(Span), IVariableDeclaration
{
    /// <inheritdoc />
    public override TResult? Accept<TResult>(IAstVisitor<TResult> visitor) where TResult : class => visitor.Visit(this);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> Children
    {
        get
        {
            if (Initializer != null)
            {
                yield return Initializer;
            }
        }
    }
}
public record BinaryExpression(SourceSpan Span, BinaryOperator Operator, IExpression Left, IExpression Right) : BaseNode(Span), IExpression
{
    /// <inheritdoc />
    public override TResult? Accept<TResult>(IAstVisitor<TResult> visitor) where TResult : class => visitor.Visit(this);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> Children
    {
        get
        {
            yield return Left;
            yield return Right;
        }
    }
}
public record UnaryExpression(SourceSpan Span, UnaryOperator Operator, IExpression Operand) : BaseNode(Span), IExpression
{
    /// <inheritdoc />
    public override TResult? Accept<TResult>(IAstVisitor<TResult> visitor) where TResult : class => visitor.Visit(this);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> Children
    {
        get { yield return Operand; }
    }
}
public record ParameterDeclaration(SourceSpan Span, int Index, string Name, TypeReference TypeReference, IExpression? Initializer, bool IsCompilerGenerated = false)
    : LocalVariableDeclaration(Span, Name, TypeReference, Initializer, IsCompilerGenerated)
{
    public override TResult? Accept<TResult>(IAstVisitor<TResult> visitor) where TResult : class => visitor.Visit(this);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> Children => Enumerable.Empty<IAstNode>();
}
public record AssignmentExpression(SourceSpan Span, string Name, IExpression? AssignmentValue) : BaseNode(Span), IExpression
{
    public override TResult? Accept<TResult>(IAstVisitor<TResult> visitor) where TResult : class => visitor.Visit(this);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> Children
    {
        get
        {
            if (AssignmentValue != null) yield return AssignmentValue;
        }
    }
}
public record Prototype(SourceSpan Span, string Name, bool IsExtern, IReadOnlyList<ParameterDeclaration> Parameters, TypeReference ReturnType, bool IsCompilerGenerated = false) : BaseNode(Span)
{
    public override TResult? Accept<TResult>(IAstVisitor<TResult> visitor) where TResult : class => visitor.Visit(this);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> Children => Parameters;
}
public record FunctionCallExpression(SourceSpan Span, Prototype Callee, List<IExpression> Arguments) : BaseNode(Span), IExpression
{
    /// <inheritdoc />
    public override TResult? Accept<TResult>(IAstVisitor<TResult> visitor) where TResult : class => visitor.Visit(this);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> Children
    {
        get { yield return Callee; }
    }
}
public record FunctionDefinition(SourceSpan Span, Prototype Signature, IAstNode Body, bool IsAnonymous = false) : BaseNode(Span)
{
    /// <inheritdoc />
    public override TResult? Accept<TResult>(IAstVisitor<TResult> visitor) where TResult : class => visitor.Visit(this);

    public override IEnumerable<IAstNode> Children
    {
        get
        {
            yield return Signature;
            yield return Body;
        }
    }

    public IReadOnlyList<ParameterDeclaration> Parameters => Signature.Parameters;

    public IReadOnlyList<LocalVariableDeclaration> LocalVariables { get; }
}
public record AttributeEaSt(SourceSpan Span, string Name, IAstNode? Operand, IReadOnlyList<IExpression>? Arguments) : BaseNode(Span)
{
    /// <inheritdoc />
    public override TResult? Accept<TResult>(IAstVisitor<TResult> visitor) where TResult : class => visitor.Visit(this);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> Children
    {
        get
        {
            if (Operand is not null)
                yield return Operand;
            if (Arguments is not null)
            {
                foreach (var argument in Arguments)
                {
                    yield return argument;
                }
            }
        }
    }
}
public record IfExpression(
    SourceSpan Span, IExpression Condition, IAstNode ThenExpression, IAstNode? ElseExpression,
    // compiler generated result variable supports building conditional
    // expressions without the need for SSA form by using mutable variables
    // The result is assigned a value from both sides of the branch. In
    // pure SSA form this isn't needed as a PHI node would be used instead.
    LocalVariableDeclaration? ResultVar) : BaseNode(Span), IExpression
{
    /// <inheritdoc />
    public override TResult? Accept<TResult>(IAstVisitor<TResult> visitor) where TResult : class => visitor.Visit(this);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> Children
    {
        get
        {
            yield return Condition;
            yield return ThenExpression;
            yield return ElseExpression;
        }
    }
}
