using MyLangCompiler.Nodes;

namespace MyLangCompiler.Semantic;

public sealed class FunctionSymbol : Symbol
{
    public TypeSymbol ReturnType { get; }
    public IReadOnlyList<ParameterNode> Parameters { get; }
    public FunctionNode Declaration { get; }

    public FunctionSymbol(
        string name,
        TypeSymbol returnType,
        IReadOnlyList<ParameterNode> parameters,
        FunctionNode declaration)
        : base(name)
    {
        ReturnType = returnType;
        Parameters = parameters;
        Declaration = declaration;
    }
}
