using MyLangCompiler.Enumerations;

namespace MyLangCompiler.Nodes;

public sealed class BinaryExprNode : ExprNode
{
    public BinaryOperator Operator { get; }
    public ExprNode Left { get; }
    public ExprNode Right { get; }

    public BinaryExprNode(SourceSpan span, BinaryOperator op, ExprNode left, ExprNode right)
        : base(span)
    {
        Operator = op;
        Left = left;
        Right = right;
    }

    public override string ToString() => $"BinaryExpr({Operator})";
}
