using System.Collections.Generic;
using MyLangCompiler.Enumerations;

namespace MyLangCompiler.Nodes;

public sealed class ProgramNode : AstNode
{
    public override NodeKind Kind => NodeKind.Program;

    public List<DeclNode> Declarations { get; } = new();

    public ProgramNode(SourceSpan span) : base(span)
    {
    }
}