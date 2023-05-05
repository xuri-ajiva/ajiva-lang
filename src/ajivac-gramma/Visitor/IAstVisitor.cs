using ajivac_lib.AST;

namespace ajivac_lib.Visitor;

public interface IAstVisitor<TResult, TArg>
    where TResult : struct where TArg : struct
{
    TResult Visit(CompoundStatement node, ref TArg arg);
    TResult Visit(LiteralExpression node, ref TArg arg);
    TResult Visit(IdentifierExpression node, ref TArg arg);
    TResult Visit(Prototype node, ref TArg arg);
    TResult Visit(LocalVariableDeclaration node, ref TArg arg);
    TResult Visit(BinaryExpression node, ref TArg arg);
    TResult Visit(UnaryExpression node, ref TArg arg);
    TResult Visit(FunctionCallExpression node, ref TArg arg);
    TResult Visit(FunctionDefinition node, ref TArg arg);
    TResult Visit(ParameterDeclaration node, ref TArg arg);
    TResult Visit(AttributeEaSt node, ref TArg arg);
    TResult Visit(IfExpression node, ref TArg arg);
    TResult Visit(AssignmentExpression node, ref TArg arg);
    TResult Visit(BreakStatement node, ref TArg arg);
    TResult Visit(ReturnStatement node, ref TArg arg);
    TResult Visit(WhileStatement node, ref TArg arg);
    TResult Visit(ForStatement node, ref TArg arg);
    TResult Visit(ContinueStatement node, ref TArg arg);
    TResult Default(ref TArg arg);
}
