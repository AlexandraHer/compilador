using System.Collections.Generic;
using MyLangCompiler.Enumerations;

namespace MyLangCompiler.Nodes;

public sealed class ArrayLiteralNode : ExprNode
{
    public override NodeKind Kind => NodeKind.ArrayLiteral; 

    public IReadOnlyList<ExprNode> Items { get; }

    public ArrayLiteralNode(SourceSpan span, List<ExprNode> items)
        : base(span)
    {
        Items = items;
    }
}