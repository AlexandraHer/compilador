using System.Collections.Generic;

namespace MyLangCompiler.Nodes;

// Contenedor simple para devolver múltiples DeclNode desde el visitor
public sealed class DeclListNode : DeclNode
{
    public List<DeclNode> Decls { get; } = new();

    public DeclListNode(SourceSpan span) : base(span) { }
}