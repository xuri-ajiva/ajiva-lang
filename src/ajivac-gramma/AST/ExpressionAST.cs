﻿using System.Text;
using ajivac_lib.Semantics;
using ajivac_lib.Visitor;

namespace ajivac_lib.AST;

public abstract record BaseNode(SourceSpan Span) : IAstNode
{
    public void Print(StringBuilder stringBuilder) => PrintMembers(stringBuilder);

    protected bool PrintList(StringBuilder builder, string name, IEnumerable<IAstNode> astNodes)
    {
        builder.Append(name);
        builder.Append(" = [");
        bool first = true;
        foreach (var astNode in astNodes)
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

    public abstract TResult Accept<TResult, TArg>(IAstVisitor<TResult, TArg> visitor, ref TArg arg) where TResult : struct where TArg : struct;

    /// <inheritdoc />
    public abstract IEnumerable<IAstNode> Children { get; }
}
public record CompoundStatement(SourceSpan Span, IEnumerable<IAstNode> Statements) : BaseNode(Span)
{
    /// <inheritdoc />
    public override TResult Accept<TResult, TArg>(IAstVisitor<TResult, TArg> visitor, ref TArg arg) where TResult : struct where TArg : struct => visitor.Visit(this, ref arg);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> Children => Statements;

    protected override bool PrintMembers(StringBuilder builder)
    {
        return PrintList(builder, nameof(Children), Children);
    }
}
public record LiteralExpression(SourceSpan Span, string Value, TypeReference TypeReference) : BaseNode(Span), IExpression, ITypedExpression
{
    /// <inheritdoc />
    public override TResult Accept<TResult, TArg>(IAstVisitor<TResult, TArg> visitor, ref TArg arg) where TResult : struct where TArg : struct => visitor.Visit(this, ref arg);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> Children => Enumerable.Empty<IAstNode>();
}
public record IdentifierExpression(SourceSpan Span, string Identifier) : BaseNode(Span), IExpression, ITypedExpression
{
    /// <inheritdoc />
    public override TResult Accept<TResult, TArg>(IAstVisitor<TResult, TArg> visitor, ref TArg arg) where TResult : struct where TArg : struct => visitor.Visit(this, ref arg);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> Children => Enumerable.Empty<IAstNode>();

    public LocalVariableDeclaration? Definition { get; set; }
    public TypeReference TypeReference => Definition!.TypeReference;
}
public record LocalVariableDeclaration(SourceSpan Span, string Name, TypeReference TypeReference, IExpression? Initializer, bool IsCompilerGenerated = false)
    : BaseNode(Span), IVariableDeclaration, ITypedExpression
{
    /// <inheritdoc />
    public override TResult Accept<TResult, TArg>(IAstVisitor<TResult, TArg> visitor, ref TArg arg) where TResult : struct where TArg : struct => visitor.Visit(this, ref arg);

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
public record BinaryExpression(SourceSpan Span, BinaryOperator Operator, IExpression Left, IExpression Right) : BaseNode(Span), ITypedExpression, IExpression
{
    /// <inheritdoc />
    public override TResult Accept<TResult, TArg>(IAstVisitor<TResult, TArg> visitor, ref TArg arg) where TResult : struct where TArg : struct => visitor.Visit(this, ref arg);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> Children
    {
        get
        {
            yield return Left;
            yield return Right;
        }
    }
    public TypeReference TypeReference { get; set; }
}
public record UnaryExpression(SourceSpan Span, UnaryOperator Operator, IExpression Operand) : BaseNode(Span), ITypedExpression, IExpression
{
    /// <inheritdoc />
    public override TResult Accept<TResult, TArg>(IAstVisitor<TResult, TArg> visitor, ref TArg arg) where TResult : struct where TArg : struct => visitor.Visit(this, ref arg);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> Children
    {
        get { yield return Operand; }
    }
    public TypeReference TypeReference { get; set; }
}
public record ParameterDeclaration(SourceSpan Span, int Index, string Name, TypeReference TypeReference, IExpression? Initializer, bool IsCompilerGenerated = false)
    : LocalVariableDeclaration(Span, Name, TypeReference, Initializer, IsCompilerGenerated), ITypedExpression
{
    public override TResult Accept<TResult, TArg>(IAstVisitor<TResult, TArg> visitor, ref TArg arg) where TResult : struct where TArg : struct => visitor.Visit(this, ref arg);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> Children => Enumerable.Empty<IAstNode>();
}
public record AssignmentExpression(SourceSpan Span, string Name, IExpression? AssignmentValue) : BaseNode(Span), IExpression //todo dose assing return th value?, ITypedExpression
{
    public override TResult Accept<TResult, TArg>(IAstVisitor<TResult, TArg> visitor, ref TArg arg) where TResult : struct where TArg : struct => visitor.Visit(this, ref arg);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> Children
    {
        get
        {
            if (AssignmentValue != null) yield return AssignmentValue;
        }
    }

    public LocalVariableDeclaration? Definition { get; set; }
}
public record Prototype(SourceSpan Span, string Name, bool IsExtern, IReadOnlyList<ParameterDeclaration> Parameters, TypeReference ReturnType, bool IsCompilerGenerated = false) : BaseNode(Span), ITypedExpression
{
    public override TResult Accept<TResult, TArg>(IAstVisitor<TResult, TArg> visitor, ref TArg arg) where TResult : struct where TArg : struct => visitor.Visit(this, ref arg);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> Children => Parameters;

    /// <inheritdoc />
    protected override bool PrintMembers(StringBuilder builder)
    {
        builder.Append(nameof(Name));
        builder.Append(" = ");
        builder.Append(Name);
        builder.Append(", ");
        builder.Append(nameof(IsExtern));
        builder.Append(" = ");
        builder.Append(IsExtern);
        builder.Append(", ");
        PrintList(builder, nameof(Parameters), Parameters);
        builder.Append(", ");
        builder.Append(nameof(ReturnType));
        builder.Append(" = ");
        builder.Append(ReturnType);
        return true;
    }

    public TypeReference TypeReference => ReturnType;
}
public record FunctionCallExpression(SourceSpan Span, string CalleeName, IReadOnlyList<IExpression> Arguments) : BaseNode(Span), ITypedExpression, IExpression
{
    /// <inheritdoc />
    public override TResult Accept<TResult, TArg>(IAstVisitor<TResult, TArg> visitor, ref TArg arg) where TResult : struct where TArg : struct => visitor.Visit(this, ref arg);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> Children
    {
        get
        {
            //todo yield return Callee;
            yield break;
        }
    }

    /// <inheritdoc />
    protected override bool PrintMembers(StringBuilder builder)
    {
        builder.Append(nameof(CalleeName));
        builder.Append(" = ");
        builder.Append(CalleeName);
        builder.Append(", ");
        PrintList(builder, nameof(Arguments), Arguments);
        return true;
    }

    public FunctionDefinition? Definition { get; set; }
    public TypeReference TypeReference => Definition!.TypeReference;
}
public record FunctionDefinition(SourceSpan Span, Prototype Signature, bool IsAnonymous = false) : BaseNode(Span), ITypedExpression
{
    public FunctionDefinition(SourceSpan span, Prototype signature, IAstNode body, bool isAnonymous = false) : this(span, signature, isAnonymous)
    {
        Body = body;
    }

    public IAstNode Body { get; set; }

    /// <inheritdoc />
    public override TResult Accept<TResult, TArg>(IAstVisitor<TResult, TArg> visitor, ref TArg arg) where TResult : struct where TArg : struct => visitor.Visit(this, ref arg);

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
    public TypeReference TypeReference => Signature.TypeReference;
}
public record AttributeEaSt(SourceSpan Span, string Name, IAstNode? Operand, IReadOnlyList<IExpression>? Arguments) : BaseNode(Span)
{
    /// <inheritdoc />
    public override TResult Accept<TResult, TArg>(IAstVisitor<TResult, TArg> visitor, ref TArg arg) where TResult : struct where TArg : struct => visitor.Visit(this, ref arg);

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
    SourceSpan Span, IExpression Condition, IAstNode ThenExpression, IAstNode? ElseExpression) : BaseNode(Span), IExpression
{
    /// <inheritdoc />
    public override TResult Accept<TResult, TArg>(IAstVisitor<TResult, TArg> visitor, ref TArg arg) where TResult : struct where TArg : struct => visitor.Visit(this, ref arg);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> Children
    {
        get
        {
            yield return Condition;
            yield return ThenExpression;
            if (ElseExpression is not null) yield return ElseExpression;
        }
    }
}
public record ReturnStatement(SourceSpan Span, IExpression? Expression) : BaseNode(Span), ITypedExpression
{
    /// <inheritdoc />
    public override TResult Accept<TResult, TArg>(IAstVisitor<TResult, TArg> visitor, ref TArg arg) where TResult : struct where TArg : struct => visitor.Visit(this, ref arg);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> Children
    {
        get
        {
            if (Expression != null) yield return Expression;
        }
    }
    public TypeReference TypeReference { get; set; }

    public FunctionDefinition Function { get; set; }

    protected override bool PrintMembers(StringBuilder builder)
    {
        if (base.PrintMembers(builder))
            builder.Append(", ");
        builder.Append("Expression = ");
        builder.Append((object)this.Expression);
        builder.Append(", TypeReference = ");
        builder.Append(TypeReference.ToString());
        return true;
    }
}
public record WhileStatement(SourceSpan Span, IExpression Condition, IAstNode Body) : BaseNode(Span)
{
    /// <inheritdoc />
    public override TResult Accept<TResult, TArg>(IAstVisitor<TResult, TArg> visitor, ref TArg arg) where TResult : struct where TArg : struct => visitor.Visit(this, ref arg);

    public override IEnumerable<IAstNode> Children
    {
        get
        {
            yield return Condition;
            yield return Body;
        }
    }
}
public record ForStatement(SourceSpan Span, IAstNode Initializer, IExpression Condition, IAstNode Increment, IAstNode Body) : BaseNode(Span)
{
    /// <inheritdoc />
    public override TResult Accept<TResult, TArg>(IAstVisitor<TResult, TArg> visitor, ref TArg arg) where TResult : struct where TArg : struct => visitor.Visit(this, ref arg);

    public override IEnumerable<IAstNode> Children
    {
        get
        {
            yield return Initializer;
            yield return Condition;
            yield return Increment;
            yield return Body;
        }
    }
}
public record BreakStatement(SourceSpan Span) : BaseNode(Span)
{
    /// <inheritdoc />
    public override TResult Accept<TResult, TArg>(IAstVisitor<TResult, TArg> visitor, ref TArg arg) where TResult : struct where TArg : struct => visitor.Visit(this, ref arg);

    public override IEnumerable<IAstNode> Children => Enumerable.Empty<IAstNode>();
}
public record ContinueStatement(SourceSpan Span) : BaseNode(Span)
{
    /// <inheritdoc />
    public override TResult Accept<TResult, TArg>(IAstVisitor<TResult, TArg> visitor, ref TArg arg) where TResult : struct where TArg : struct => visitor.Visit(this, ref arg);

    public override IEnumerable<IAstNode> Children => Enumerable.Empty<IAstNode>();
}
public record EmptyStatement(SourceSpan Span) : BaseNode(Span)
{
    /// <inheritdoc />
    public override TResult Accept<TResult, TArg>(IAstVisitor<TResult, TArg> visitor, ref TArg arg) where TResult : struct where TArg : struct => visitor.Default(ref arg);

    public override IEnumerable<IAstNode> Children => Enumerable.Empty<IAstNode>();
}
