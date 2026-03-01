using MyLangCompiler.Enumerations;

namespace MyLangCompiler.Nodes;

public sealed class ParameterNode : AstNode
{
    public override NodeKind Kind => NodeKind.Parameter; // ✅

    public string Name { get; }
    public TypeRefNode Type { get; }

    public ParameterNode(SourceSpan span, string name, TypeRefNode type)
        : base(span)
    {
        Name = name;
        Type = type;
    }
}