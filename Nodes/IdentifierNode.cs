using MyLangCompiler.Enumerations;

namespace MyLangCompiler.Nodes;

public sealed class IdentifierNode : ExprNode
{
    public override NodeKind Kind => NodeKind.Identifier;

    public string Name { get; }

    public IdentifierNode(SourceSpan span, string name)
        : base(span)
    {
        Name = name;
    }
}