namespace MyLangCompiler.Semantic;

public sealed class BuiltInTypeSymbol : TypeSymbol
{
    private BuiltInTypeSymbol(string name) : base(name) { }

    public static readonly BuiltInTypeSymbol Int = new("i");
    public static readonly BuiltInTypeSymbol Float = new("f");
    public static readonly BuiltInTypeSymbol Bool = new("b");
    public static readonly BuiltInTypeSymbol String = new("s");
}
