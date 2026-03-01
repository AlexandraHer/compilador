using MyLangCompiler.Enumerations;

namespace MyLangCompiler.Nodes;

public sealed class LiteralNode : ExprNode
{
    public override NodeKind Kind => NodeKind.Literal;

    public object? Value { get; }

    public LiteralNode(SourceSpan span, object? value)
        : base(span)
    {
        Value = value;
    }
}