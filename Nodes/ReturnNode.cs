using MyLangCompiler.Enumerations;

namespace MyLangCompiler.Nodes;

public sealed class ReturnNode : StmtNode
{
    public override NodeKind Kind => NodeKind.Return;

    public ExprNode Value { get; }

    public ReturnNode(SourceSpan span, ExprNode value)
        : base(span)
    {
        Value = value;
    }
}