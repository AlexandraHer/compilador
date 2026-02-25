namespace MyLangCompiler.Nodes;

public sealed class IdentifierNode : ExprNode
{
    public string Name { get; }

    public IdentifierNode(SourceSpan span, string name)
        : base(span)
    {
        Name = name;
    }
}
