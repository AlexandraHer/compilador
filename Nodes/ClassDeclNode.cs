using MyLangCompiler.Enumerations;

namespace MyLangCompiler.Nodes;

public sealed class ClassDeclNode : DeclNode
{
    public override NodeKind Kind => NodeKind.ClassDeclaration;

    public string Name { get; }

    public ClassDeclNode(SourceSpan span, string name) : base(span)
    {
        Name = name;
    }
}