using MyLangCompiler.Enumerations;

namespace MyLangCompiler.Nodes;

public sealed class ArrayAccessNode : ExprNode
{
    public override NodeKind Kind => NodeKind.IndexExpression;

    public string ArrayName { get; }
    public ExprNode Index { get; }

    public ArrayAccessNode(SourceSpan span, string arrayName, ExprNode index)
        : base(span)
    {
        ArrayName = arrayName;
        Index = index;
    }
}