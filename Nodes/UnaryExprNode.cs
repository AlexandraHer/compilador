using MyLangCompiler.Enumerations;

namespace MyLangCompiler.Nodes;

public sealed class UnaryExprNode : ExprNode
{
    public override NodeKind Kind => NodeKind.UnaryExpression; // ✅

    public UnaryOperatorKind Operator { get; }
    public ExprNode Operand { get; }

    public UnaryExprNode(SourceSpan span, UnaryOperatorKind op, ExprNode operand)
        : base(span)
    {
        Operator = op;
        Operand = operand;
    }
}