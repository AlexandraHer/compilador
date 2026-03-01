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
    CallExpression,
    ArrayLiteral,
    IndexExpression,
    MethodCallExpression,
    TypeReference,
    ExpressionStatement,
    If,
    For,
    While,
    Parameter,
    DeclarationList,
    ClassDeclaration,
}
