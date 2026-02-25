namespace MyLangCompiler.Nodes;

public abstract class ExprNode : AstNode
{
    protected ExprNode(SourceSpan span) : base(span)
    {
    }
}
