using System.Collections.Generic;
using MyLangCompiler.Enumerations;

namespace MyLangCompiler.Nodes;

public sealed class CallExprNode : ExprNode
{
    public override NodeKind Kind => NodeKind.CallExpression;

    public string FunctionName { get; }
    public List<ExprNode> Arguments { get; }

    public CallExprNode(SourceSpan span, string functionName, List<ExprNode> arguments)
        : base(span)
    {
        FunctionName = functionName;
        Arguments = arguments;
    }
}