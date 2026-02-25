namespace MyLangCompiler.Nodes;

public sealed class ReturnNode : StmtNode
{
    public ExprNode Value { get; }

    public ReturnNode(SourceSpan span, ExprNode value)
        : base(span)
    {
        Value = value;
    }
}
