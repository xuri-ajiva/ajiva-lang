using ajivac_lib.AST;
using ajivac_lib.Semantics;

namespace ajivac_lib;

public class Diagnostics
{
    public Diagnostics(Action<string> writer, Sensitivity minSensitivity)
    {
        Writer = writer;
        MinSensitivity = minSensitivity;
    }

    public int Count { get; private set; }
    public Action<String> Writer { get; }
    public Sensitivity MinSensitivity { get; }

    public virtual void ReportError(SourceSpan location, string message, Sensitivity sensitivity)
    {
        if (sensitivity >= MinSensitivity)
        {
            Count++;
            Writer?.Invoke($"[{sensitivity}] {location.FullLocation()}: {message}");
        }
    }

    public static Diagnostics Console { get; } = new Diagnostics(System.Console.WriteLine, Sensitivity.Info);

    public void ReportInvalidCharacter(SourceSpan currentChar)
    {
        ReportError(currentChar, "Invalid character", Sensitivity.Warning);
    }

    public void ReportUnexpectedToken(Token token, string expected)
    {
        ReportError(token.Span, $"Unexpected token '{token.Span.GetValue()}'. Expected {expected}", Sensitivity.Error);
    }

    public void ReportUndefinedPreprocessor(string identifier, SourceSpan preprocessorSpan)
    {
        ReportError(preprocessorSpan, $"Undefined preprocessor '{identifier}'", Sensitivity.Error);
    }

    public void ReportVariableNotDefined(string nodeIdentifier, SourceSpan pos)
    {
        ReportError(pos, $"Variable '{nodeIdentifier}' not defined", Sensitivity.Error);
    }

    public void ReportIdentifierNotFound(string nodeIdentifier, SourceSpan pos)
    {
        ReportError(pos, $"Identifier '{nodeIdentifier}' not found", Sensitivity.Error);
    }

    public void ReportRefMissing(IAstNode node, string property)
    {
        ReportError(node.Span, $"Missing reference '{property}'", Sensitivity.Error);
    }

    public void ReportWrongNumberOfArguments(FunctionCallExpression node, int parametersCount, int argumentsCount)
    {
        ReportError(node.Span, $"Wrong number of arguments. Expected {parametersCount}, got {argumentsCount}", Sensitivity.Error);
    }

    public void ReportFunctionNotFound(FunctionCallExpression node, string nodeCalleeName, List<TypeReference> argTypes)
    {
        ReportError(node.Span, $"Function '{nodeCalleeName}({string.Join(", ", argTypes.Select(t => t.ToString()))})' not found", Sensitivity.Error);
    }

    public void ReportCannotAssign(IAstNode node, TypeReference leftType, TypeReference rightType)
    {
        ReportError(node.Span, $"Cannot assign '{rightType.Kind}' to '{leftType.Kind}'", Sensitivity.Error);
    }

    public void ReportInvalidConditionType(IfExpression node, TypeReference conditionType)
    {
        ReportError(node.Span, $"Invalid condition type '{conditionType.Kind}'", Sensitivity.Error);
    }

    public void ReportVariableAlreadyDeclared(SourceSpan variableSpan, string variableName)
    {
        ReportError(variableSpan, $"Variable '{variableName}' already declared in this scope", Sensitivity.Error);
    }

    public void ReportNativeFunctionNotFound(string name, string eMessage)
    {
        ReportError(SourceSpan.Empty, $"Native function '{name}' not found: {eMessage}", Sensitivity.Error);
    }

    public void ReportInternalError(string error)
    {
        ReportError(SourceSpan.Empty, $"Internal error: {error}", Sensitivity.Error);
    }

    public void ReportConversionError(TypeKind from, TypeKind to)
    {
        ReportError(SourceSpan.Empty, $"Cannot convert from '{from}' to '{to}'", Sensitivity.Error);
    }

    public void ReportOperatorNotSupported(SourceSpan sourceSpan, BinaryOperator nodeOperator, TypeReference leftType, TypeReference rightType, string? reason = null)
    {
        ReportError(sourceSpan, $"Operator '{nodeOperator}' not supported for types '{leftType.Kind}' and '{rightType.Kind}'{reason}", Sensitivity.Error);
    }

    public void ReportOperatorNotSupported(SourceSpan sourceSpan, UnaryOperator nodeOperator, TypeReference operandType, string? reason = null)
    {
        ReportError(sourceSpan, $"Operator '{nodeOperator}' not supported for type '{operandType.Kind}'{reason}", Sensitivity.Error);
    }

    public void ReportWrongArgumentType(SourceSpan pos, TypeReference typeReference, TypeReference argType)
    {
        ReportError(pos, $"Wrong argument type. Expected '{typeReference.Kind}', got '{argType.Kind}'", Sensitivity.Error);
    }
}
public enum Sensitivity
{
    Info,
    Warning,
    Error,
}
