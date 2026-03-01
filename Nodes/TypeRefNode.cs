using MyLangCompiler.Enumerations;

namespace MyLangCompiler.Nodes;

public sealed class TypeRefNode : AstNode
{
    public override NodeKind Kind => NodeKind.TypeReference;

    public string Name { get; }

    public TypeRefNode(SourceSpan span, string name)
        : base(span)
    {
        Name = name;
    }

    public override string ToString() => Name;
}