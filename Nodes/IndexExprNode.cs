using MyLangCompiler.Enumerations;

namespace MyLangCompiler.Nodes;

public sealed class IndexExprNode : ExprNode
{
    public override NodeKind Kind => NodeKind.IndexExpression;

    public ExprNode Target { get; }
    public ExprNode Index { get; }

    public IndexExprNode(SourceSpan span, ExprNode target, ExprNode index)
        : base(span)
    {
        Target = target;
        Index = index;
    }
}