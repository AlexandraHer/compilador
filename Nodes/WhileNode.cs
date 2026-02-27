namespace MyLangCompiler.Nodes;

public sealed class WhileNode : StmtNode
{
    public ExprNode Condition { get; }
    public BlockNode Body { get; }

    public WhileNode(SourceSpan span, ExprNode condition, BlockNode body)
        : base(span)
    {
        Condition = condition;
        Body = body;
    }
}