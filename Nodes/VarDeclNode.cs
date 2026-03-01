using MyLangCompiler.Enumerations;

namespace MyLangCompiler.Nodes;

public sealed class VarDeclNode : StmtNode
{
    public override NodeKind Kind => NodeKind.VariableDeclaration;

    public string Name { get; }
    public TypeRefNode Type { get; }
    public ExprNode? Initializer { get; }

    public VarDeclNode(SourceSpan span, string name, TypeRefNode type, ExprNode? initializer)
        : base(span)
    {
        Name = name;
        Type = type;
        Initializer = initializer;
    }
}