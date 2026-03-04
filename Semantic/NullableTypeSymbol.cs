namespace MyLangCompiler.Semantic;

public sealed class NullableTypeSymbol : TypeSymbol
{
    public TypeSymbol Underlying { get; }

    public NullableTypeSymbol(TypeSymbol underlying)
        : base($"{underlying.Name}?")
    {
        Underlying = underlying;
    }
}