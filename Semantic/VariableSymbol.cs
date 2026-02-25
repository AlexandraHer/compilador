using MyLangCompiler.Semantic;

namespace MyLangCompiler.Semantic;

public sealed class VariableSymbol : Symbol
{
    public TypeSymbol Type { get; }

    public VariableSymbol(string name, TypeSymbol type)
        : base(name)
    {
        Type = type;
    }
}
