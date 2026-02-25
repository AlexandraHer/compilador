namespace MyLangCompiler.Exceptions;

public sealed class SemanticException : Exception
{
    public SemanticException(string message)
        : base(message)
    {
    }
}
