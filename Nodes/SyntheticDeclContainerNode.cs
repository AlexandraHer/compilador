using System.Collections.Generic;

namespace MyLangCompiler.Nodes;

public sealed class SyntheticDeclContainerNode : DeclNode
{
    public List<DeclNode> Decls { get; } = new();

    public SyntheticDeclContainerNode(SourceSpan span) : base(span) { }
}