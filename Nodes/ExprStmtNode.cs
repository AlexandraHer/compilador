using MyLangCompiler.Enumerations;

namespace MyLangCompiler.Nodes;

public sealed class ExprStmtNode : StmtNode
{
    public override NodeKind Kind => NodeKind.ExpressionStatement;

    public ExprNode Expression { get; }

    public ExprStmtNode(SourceSpan span, ExprNode expression) : base(span)
    {
        Expression = expression;
    }
}