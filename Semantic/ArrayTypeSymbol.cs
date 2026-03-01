namespace MyLangCompiler.Semantic;

public sealed class ArrayTypeSymbol : TypeSymbol
{
    public TypeSymbol ElementType { get; }

    public ArrayTypeSymbol(TypeSymbol elementType)
        : base($"{elementType.Name}[]")
    {
        ElementType = elementType;
    }
}