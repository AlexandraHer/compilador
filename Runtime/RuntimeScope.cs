namespace MyLangCompiler.Runtime;

public sealed class RuntimeScope
{
    private readonly Dictionary<string, object?> _values = new();

    public RuntimeScope? Parent { get; }

    public RuntimeScope(RuntimeScope? parent)
    {
        Parent = parent;
    }

    public bool Declare(string name, object? value)
    {
        if (_values.ContainsKey(name))
            return false;

        _values[name] = value;
        return true;
    }

    public bool TryGet(string name, out object? value)
    {
        if (_values.TryGetValue(name, out value))
            return true;

        if (Parent != null)
            return Parent.TryGet(name, out value);

        value = null;
        return false;
    }

    public bool Assign(string name, object? value)
    {
        if (_values.ContainsKey(name))
        {
            _values[name] = value;
            return true;
        }

        if (Parent != null)
            return Parent.Assign(name, value);

        return false;
    }
}
