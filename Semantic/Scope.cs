using System.Collections.Generic;

namespace MyLangCompiler.Semantic;

public sealed class Scope
{
    private readonly Dictionary<string, Symbol> _symbols = new();

    public Scope? Parent { get; }

    public Scope(Scope? parent)
    {
        Parent = parent;
    }

    public bool Declare(Symbol symbol)
    {
        if (_symbols.ContainsKey(symbol.Name))
            return false;

        _symbols[symbol.Name] = symbol;
        return true;
    }

    public Symbol? Lookup(string name)
    {
        if (_symbols.TryGetValue(name, out var symbol))
            return symbol;

        return Parent?.Lookup(name);
    }
}
