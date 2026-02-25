namespace MyLangCompiler.Nodes;

public readonly record struct SourceSpan(
    string File,
    int Line,
    int Column
);
