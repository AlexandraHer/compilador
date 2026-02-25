using MyLangCompiler.Nodes;

namespace MyLangCompiler.Semantic;

public static class TypeResolver
{
    public static TypeSymbol Resolve(TypeRefNode typeRef)
    {
        return typeRef.Name switch
        {
            "i" => BuiltInTypeSymbol.Int,
            "f" => BuiltInTypeSymbol.Float,
            "b" => BuiltInTypeSymbol.Bool,
            "s" => BuiltInTypeSymbol.String,
            _ => throw new Exception($"Unknown type '{typeRef.Name}'")
        };
    }
}
