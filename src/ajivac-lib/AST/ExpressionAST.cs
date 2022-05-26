using System.Text;

namespace ajivac_lib.AST;

public abstract record EaSt(TokenSource Source)
{
    public void Print(StringBuilder stringBuilder) => PrintMembers(stringBuilder);

    /// <inheritdoc />
    protected virtual bool PrintMembers(StringBuilder builder)
    {
        return false;
    }
}

public record ListOfEaSt<T>(TokenSource Source, IReadOnlyList<T> EaStList)  : EaSt(Source) where T : EaSt
{
    /// <inheritdoc />
    protected override bool PrintMembers(StringBuilder builder)
    {
        if(base.PrintMembers(builder))
        {
            builder.Append(' ');
        }
        builder.Append("[ ");
        
        var first = true;
        foreach(var eaSt in EaStList)
        {
            if(!first)
            {
                builder.Append(", ");
            }
            first = false;
            builder.Append(eaSt);
        }
        builder.Append(" ]");
        return true;
    }

    public static ListOfEaSt<T> Empty(TokenSource source)
    {
        return new ListOfEaSt<T>(source, new List<T>());
    }
}
public record ValueEaSt<T>(TokenSource Source, T Value) : EaSt(Source);
public record I32EaSt(TokenSource Source, int Value) : ValueEaSt<int>(Source, Value);
public record I64EaSt(TokenSource Source, long Value) : ValueEaSt<long>(Source, Value);
public record U32EaSt(TokenSource Source, uint Value) : ValueEaSt<uint>(Source, Value);
public record U64EaSt(TokenSource Source, ulong Value) : ValueEaSt<ulong>(Source, Value);
public record F32EaSt(TokenSource Source, float Value) : ValueEaSt<float>(Source, Value);
public record F64EaSt(TokenSource Source, double Value) : ValueEaSt<double>(Source, Value);
public record ChrEaSt(TokenSource Source, char Value) : ValueEaSt<char>(Source, Value);
public record StrEaSt(TokenSource Source, string Value) : ValueEaSt<string>(Source, Value);
public record BitEaSt(TokenSource Source, bool Value) : ValueEaSt<bool>(Source, Value);
public record IdentifierEaSt(TokenSource Source, string Identifier) : EaSt(Source);
public record TypedIdentifierEaSt(TokenSource Source, IdentifierEaSt Identifier, TypeReference TypeReference) : EaSt(Source);
public record BinaryEaSt(TokenSource Source, BinaryOperator Operator, EaSt Left, EaSt Right) : EaSt(Source);
public record UnaryEaSt(TokenSource Source, UnaryOperator Operator, EaSt Operand) : EaSt(Source);
public record CallEaSt(TokenSource Source, IdentifierEaSt Callee, ListOfEaSt<EaSt> Arguments) : EaSt(Source);
public record FunctionSignatureEaSt(TokenSource Source, IdentifierEaSt Name, ListOfEaSt<TypedIdentifierEaSt> Arguments, TypeReference ReturnType) : EaSt(Source);
public record FunctionEaSt(TokenSource Source, FunctionSignatureEaSt SignatureAst, ListOfEaSt<EaSt> Body) : EaSt(Source);
public record AttributeEaSt(TokenSource Source, IdentifierEaSt Name, ListOfEaSt<EaSt> Arguments) : EaSt(Source);
public record VariableEaSt(TokenSource Source, TypedIdentifierEaSt TypedIdentifier, EaSt? Assignment) : EaSt(Source);

public record IfEaSt(TokenSource Source, ParenthesisEaSt Condition, ListOfEaSt<EaSt> Body) : EaSt(Source);
public record ParenthesisEaSt(TokenSource Source, EaSt? Expression) : EaSt(Source);
