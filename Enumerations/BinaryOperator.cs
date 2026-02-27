namespace MyLangCompiler.Enumerations;

public enum BinaryOperator
{
    // Aritméticos
    Add,
    Subtract,
    Multiply,
    Divide,
    Modulo,

    // Lógicos
    And,
    Or,

    // Relacionales
    Equal,
    NotEqual,
    Less,
    LessOrEqual,
    Greater,
    GreaterOrEqual,

    // Unario
    Negate
}