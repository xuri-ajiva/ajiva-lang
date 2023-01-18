using System.Diagnostics;
using System.Runtime.CompilerServices;
using ajivac_lib.AST;
using BinaryExpression = ajivac_lib.AST.BinaryExpression;
using UnaryExpression = ajivac_lib.AST.UnaryExpression;

namespace ajivac_lib.Visitor;

public class AstAllSubVisitorBase<TResult, TArg>
    : AstVisitorBase<TResult, TArg>
    where TResult : struct where TArg : struct
{
    protected override TResult VisitChildren(IAstNode? node, ref TArg arg)
    {
        if (node is null)
            return default!;

        var result = default(TResult);
        var children = node.Children;
        foreach (var child in children)
        {
            result = child.Accept(this, ref arg);
        }

        return result;
    }
}
