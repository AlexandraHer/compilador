using MyLangCompiler.Enumerations;

namespace MyLangCompiler.Nodes;

public sealed class UseDeclNode : DeclNode
{
    public override NodeKind Kind => NodeKind.UseDeclaration; // ✅

    public string ModuleName { get; }

    public UseDeclNode(SourceSpan span, string moduleName)
        : base(span)
    {
        ModuleName = moduleName;
    }
}