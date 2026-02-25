namespace MyLangCompiler.Nodes;


public abstract class AstNode
{
    public SourceSpan Span { get; }

    protected AstNode(SourceSpan span)
    {
        Span = span;
    }
}
