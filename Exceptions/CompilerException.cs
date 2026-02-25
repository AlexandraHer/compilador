namespace MyLangCompiler.Exceptions;

public abstract class CompilerException : Exception
{
    protected CompilerException(string message)
        : base(message)
    {
    }
}
