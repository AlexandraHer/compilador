using MyLangCompiler.Enumerations;

namespace MyLangCompiler.Nodes;

public abstract class AstNode
{
    public SourceSpan Span { get; }

    public abstract NodeKind Kind { get; }

    protected AstNode(SourceSpan span)
    {
        Span = span;
    }
}