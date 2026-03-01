using System.Collections.Generic;
using MyLangCompiler.Enumerations;

namespace MyLangCompiler.Nodes;

public sealed class SyntheticDeclContainerNode : DeclNode
{
    public List<DeclNode> Decls { get; } = new();
    public override NodeKind Kind => NodeKind.DeclarationList;

    public SyntheticDeclContainerNode(SourceSpan span) : base(span) { }
}