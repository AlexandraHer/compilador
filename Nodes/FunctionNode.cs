using System.Collections.Generic;

namespace MyLangCompiler.Nodes;

public sealed class FunctionNode : DeclNode
{
    public string Name { get; }
    public bool IsEntry { get; }

    public List<ParameterNode> Parameters { get; } = new();

    public TypeRefNode ReturnType { get; }

    public BlockNode Body { get; }

    public FunctionNode(
        SourceSpan span,
        string name,
        bool isEntry,
        TypeRefNode returnType,
        BlockNode body
    ) : base(span)
    {
        Name = name;
        IsEntry = isEntry;
        ReturnType = returnType;
        Body = body;
    }
}
