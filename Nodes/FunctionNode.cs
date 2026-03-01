using System.Collections.Generic;
using MyLangCompiler.Enumerations;

namespace MyLangCompiler.Nodes;

public sealed class FunctionNode : DeclNode
{
    public override NodeKind Kind => NodeKind.Function;

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