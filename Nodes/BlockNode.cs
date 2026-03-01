using System.Collections.Generic;
using MyLangCompiler.Enumerations;

namespace MyLangCompiler.Nodes;

public sealed class BlockNode : StmtNode
{
    public override NodeKind Kind => NodeKind.Block;

    public List<StmtNode> Statements { get; } = new();

    public BlockNode(SourceSpan span) : base(span)
    {
    }
}