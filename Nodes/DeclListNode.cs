using System.Collections.Generic;
using MyLangCompiler.Enumerations;

namespace MyLangCompiler.Nodes;

public sealed class DeclListNode : DeclNode
{
    public override NodeKind Kind => NodeKind.DeclarationList;

    public List<DeclNode> Decls { get; } = new();

    public DeclListNode(SourceSpan span) : base(span) { }
}