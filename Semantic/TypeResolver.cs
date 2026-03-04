using System.Collections.Generic;
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

    private static readonly Dictionary<string, ArrayTypeSymbol> _arrayTypes = new();

    private static readonly Dictionary<string, NullableTypeSymbol> _nullableTypes = new();

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
        var text = typeRef.Name;

        var hasNullable = text.Contains('?');
        var hasArray = text.Contains('[');

        var baseName = ExtractBaseType(text); // quita ? y [n]

        if (!_types.TryGetValue(baseName, out var baseType))
            throw new Exception($"Unknown type '{baseName}'");

        // 1) primero nullable sobre el tipo base
        TypeSymbol elementType = baseType;

        if (hasNullable)
        {
            if (!_nullableTypes.TryGetValue(elementType.Name, out var nt))
            {
                nt = new NullableTypeSymbol(elementType);
                _nullableTypes[elementType.Name] = nt;
            }
            elementType = nt;
        }

        // 2) luego array sobre el elementType (para que i?[3] sea array de i?)
        if (hasArray)
        {
            if (!_arrayTypes.TryGetValue(elementType.Name, out var arr))
            {
                arr = new ArrayTypeSymbol(elementType);
                _arrayTypes[elementType.Name] = arr;
            }
            return arr;
        }

        return elementType;
    }

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

    private static string ExtractBaseType(string name)
    {
        var bracketIndex = name.IndexOf('[');
        if (bracketIndex >= 0)
            name = name.Substring(0, bracketIndex);

        name = name.Replace("?", "");

        return name;
    }
}