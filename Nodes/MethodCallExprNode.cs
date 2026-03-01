using System.Collections.Generic;
using MyLangCompiler.Enumerations;

namespace MyLangCompiler.Nodes;

public sealed class MethodCallExprNode : ExprNode
{
    public override NodeKind Kind => NodeKind.MethodCallExpression; 

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