namespace MyLangCompiler.Nodes;

public sealed class TypeRefNode : AstNode
{
    public string Name { get; }

    public TypeRefNode(SourceSpan span, string name)
        : base(span)
    {
        Name = name;
    }

    public override string ToString()
    {
        return Name;
    }
}
