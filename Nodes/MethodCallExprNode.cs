using System.Collections.Generic;

namespace MyLangCompiler.Nodes;

public sealed class MethodCallExprNode : ExprNode
{
    public ExprNode Receiver { get; }
    public string MethodName { get; }
    public List<ExprNode> Arguments { get; }

    public MethodCallExprNode(SourceSpan span, ExprNode receiver, string methodName, List<ExprNode> arguments)
        : base(span)
    {
        Receiver = receiver;
        MethodName = methodName;
        Arguments = arguments;
    }
}