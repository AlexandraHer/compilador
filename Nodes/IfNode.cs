using MyLangCompiler.Enumerations;

namespace MyLangCompiler.Nodes;

public sealed class IfNode : StmtNode
{
    public override NodeKind Kind => NodeKind.If; // ✅

    public ExprNode Condition { get; }
    public BlockNode ThenBlock { get; }
    public BlockNode? ElseBlock { get; }

    public IfNode(SourceSpan span, ExprNode condition, BlockNode thenBlock, BlockNode? elseBlock)
        : base(span)
    {
        Condition = condition;
        ThenBlock = thenBlock;
        ElseBlock = elseBlock;
    }
}