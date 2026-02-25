using System.Collections.Generic;

namespace MyLangCompiler.Nodes;

public sealed class ProgramNode : AstNode
{
    public List<DeclNode> Declarations { get; } = new();

    public ProgramNode(SourceSpan span) : base(span)
    {
    }
}
