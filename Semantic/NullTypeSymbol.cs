namespace MyLangCompiler.Semantic;

public sealed class NullTypeSymbol : TypeSymbol
{
    private NullTypeSymbol() : base("null") { }

    public static readonly NullTypeSymbol Instance = new();
}