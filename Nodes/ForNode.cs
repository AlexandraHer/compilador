using MyLangCompiler.Enumerations;

namespace MyLangCompiler.Nodes;

public sealed class ForNode : StmtNode
{
    public override NodeKind Kind => NodeKind.For;

    public StmtNode? Init { get; }
    public ExprNode Condition { get; }
    public StmtNode? Action { get; }
    public BlockNode Body { get; }

    public ForNode(SourceSpan span, StmtNode? init, ExprNode condition, StmtNode? action, BlockNode body)
        : base(span)
    {
        Init = init;
        Condition = condition;
        Action = action;
        Body = body;
    }
}