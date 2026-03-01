using MyLangCompiler.Nodes;

namespace MyLangCompiler.Semantic;

public static class TypeResolver
{
    private static readonly Dictionary<string, TypeSymbol> _types = new()
    {
        ["i"] = BuiltInTypeSymbol.Int,
        ["f"] = BuiltInTypeSymbol.Float,
        ["b"] = BuiltInTypeSymbol.Bool,
        ["s"] = BuiltInTypeSymbol.String,
    };

    // ✅ cache: "i" => ArrayTypeSymbol(i), "Math" => ArrayTypeSymbol(Math)
    private static readonly Dictionary<string, ArrayTypeSymbol> _arrayTypes = new();

    public static void RegisterClass(string name)
    {
        if (!_types.ContainsKey(name))
            _types[name] = new ClassTypeSymbol(name);
    }

    public static bool TryResolveName(string name, out TypeSymbol type)
    {
        name = ExtractBaseType(name);
        return _types.TryGetValue(name, out type!);
    }

    public static TypeSymbol Resolve(TypeRefNode typeRef)
    {
        // Detectar si tiene [n]
        var isArray = typeRef.Name.Contains('[');

        var baseName = ExtractBaseType(typeRef.Name);

        if (!_types.TryGetValue(baseName, out var baseType))
            throw new Exception($"Unknown type '{baseName}'");

        if (!isArray)
            return baseType;

        // devolver tipo arreglo cacheado
        if (!_arrayTypes.TryGetValue(baseType.Name, out var arrType))
        {
            arrType = new ArrayTypeSymbol(baseType);
            _arrayTypes[baseType.Name] = arrType;
        }

        return arrType;
    }

    // ✅ para reglas del profe: sacar tamaño cuando viene i[3]
    public static bool TryGetArraySize(TypeRefNode typeRef, out int size)
    {
        size = 0;
        var text = typeRef.Name;

        var l = text.IndexOf('[');
        var r = text.IndexOf(']');

        if (l < 0 || r < 0 || r <= l) return false;

        var inside = text.Substring(l + 1, r - l - 1);
        return int.TryParse(inside, out size);
    }

    // Quita "?" y "[n]"
    private static string ExtractBaseType(string name)
    {
        var bracketIndex = name.IndexOf('[');
        if (bracketIndex >= 0)
            name = name.Substring(0, bracketIndex);

        if (name.EndsWith("?"))
            name = name.Substring(0, name.Length - 1);

        return name;
    }
}