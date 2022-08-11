namespace ajivac_lib.AST;

public interface IAstVisitor<out TResult>
    where TResult : class
{
    TResult Visit(RootNode node);
    TResult Visit<T>(ValueExpression<T> node);
    TResult Visit(IdentifierExpression node);
    TResult Visit(Prototype node);
    TResult Visit(LocalVariableDeclaration node);
    TResult Visit(BinaryExpression node);
    TResult Visit(UnaryExpression node);
    TResult Visit(FunctionCallExpression node);
    TResult Visit(FunctionDefinition node);
    TResult Visit(ParameterDeclaration node);
    TResult Visit(AttributeEaSt node);
    TResult Visit(IfExpression node);
    TResult Visit(AssignmentExpression node);
    TResult Visit(BreakStatement node);
    TResult Visit(ReturnStatement node);
    TResult Visit(WhileStatement node);
    TResult Visit(ForStatement node);
    TResult Visit(ContinueStatement node);
}
