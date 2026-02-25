using System.Collections.Generic;

namespace MyLangCompiler.Nodes;

public sealed class BlockNode : StmtNode
{
    public List<StmtNode> Statements { get; } = new();

    public BlockNode(SourceSpan span) : base(span)
    {
    }
}
