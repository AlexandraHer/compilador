namespace MyLangCompiler.Nodes;

public sealed class LiteralNode : ExprNode
{
    public object? Value { get; }

    public LiteralNode(SourceSpan span, object? value)
        : base(span)
    {
        Value = value;
    }
}
