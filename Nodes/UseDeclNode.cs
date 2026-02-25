namespace MyLangCompiler.Nodes;

public sealed class UseDeclNode : DeclNode
{
    public string ModuleName { get; }

    public UseDeclNode(SourceSpan span, string moduleName)
        : base(span)
    {
        ModuleName = moduleName;
    }
}
