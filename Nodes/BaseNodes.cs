namespace MyLangCompiler.Nodes;

public abstract class DeclNode : AstNode
{
    protected DeclNode(SourceSpan span) : base(span) { }
}

public abstract class StmtNode : AstNode
{
    protected StmtNode(SourceSpan span) : base(span) { }
}
