namespace MyLangCompiler.Nodes;

public sealed class ParameterNode : AstNode
{
    public string Name { get; }
    public TypeRefNode Type { get; }

    public ParameterNode(SourceSpan span, string name, TypeRefNode type)
        : base(span)
    {
        Name = name;
        Type = type;
    }
}
