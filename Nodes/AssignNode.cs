using MyLangCompiler.Enumerations;

namespace MyLangCompiler.Nodes;

public sealed class AssignNode : StmtNode
{
    public override NodeKind Kind => NodeKind.Assignment;

    public ExprNode Target { get; }
    public ExprNode Value { get; }

    public AssignNode(SourceSpan span, ExprNode target, ExprNode value)
        : base(span)
    {
        Target = target;
        Value = value;
    }
}