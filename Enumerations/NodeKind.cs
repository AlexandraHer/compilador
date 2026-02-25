namespace MyLangCompiler.Enumerations;

public enum NodeKind
{
    Program,
    UseDeclaration,
    Function,
    Block,

    VariableDeclaration,
    Assignment,
    Return,

    BinaryExpression,
    UnaryExpression,
    Literal,
    Identifier,
    CallExpression
}
