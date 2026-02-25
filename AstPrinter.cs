using MyLangCompiler.Nodes;
using MyLangCompiler.Enumerations;

namespace MyLangCompiler;

public static class AstPrinter
{
    public static void Print(AstNode? node, int indent = 0)
    {
        if (node == null)
        {
            Console.WriteLine(new string(' ', indent) + "null");
            return;
        }

        var padding = new string(' ', indent);

        switch (node)
        {
            // ===================== PROGRAM =====================

            case ProgramNode program:
                Console.WriteLine($"{padding}Program");
                foreach (var decl in program.Declarations)
                    Print(decl, indent + 2);
                break;

            case UseDeclNode use:
                Console.WriteLine($"{padding}UseDecl: {use.ModuleName}");
                break;

            // ===================== FUNCTION =====================

            case FunctionNode func:
                Console.WriteLine($"{padding}Function: {func.Name}");

                Console.WriteLine($"{padding}  IsEntry: {func.IsEntry}");

                Console.WriteLine($"{padding}  ReturnType:");
                Print(func.ReturnType, indent + 4);

                if (func.Parameters.Count > 0)
                {
                    Console.WriteLine($"{padding}  Parameters:");
                    foreach (var param in func.Parameters)
                        Print(param, indent + 4);
                }

                Console.WriteLine($"{padding}  Body:");
                Print(func.Body, indent + 4);
                break;

            case ParameterNode param:
                Console.WriteLine($"{padding}Parameter: {param.Name}");
                Console.WriteLine($"{padding}  Type:");
                Print(param.Type, indent + 4);
                break;

            case TypeRefNode typeRef:
                Console.WriteLine($"{padding}TypeRef: {typeRef.Name}");
                break;

            // ===================== BLOCK =====================

            case BlockNode block:
                Console.WriteLine($"{padding}Block");
                foreach (var stmt in block.Statements)
                    Print(stmt, indent + 2);
                break;

            // ===================== STATEMENTS =====================

            case VarDeclNode varDecl:
                Console.WriteLine($"{padding}VarDecl: {varDecl.Name}");

                Console.WriteLine($"{padding}  Type:");
                Print(varDecl.Type, indent + 4);

                if (varDecl.Initializer != null)
                {
                    Console.WriteLine($"{padding}  Initializer:");
                    Print(varDecl.Initializer, indent + 4);
                }
                break;

            case AssignNode assign:
                Console.WriteLine($"{padding}Assign");

                Console.WriteLine($"{padding}  Target:");
                Print(assign.Target, indent + 4);

                Console.WriteLine($"{padding}  Value:");
                Print(assign.Value, indent + 4);
                break;

            case ReturnNode ret:
                Console.WriteLine($"{padding}Return");
                Print(ret.Value, indent + 2);
                break;

            // ===================== EXPRESSIONS =====================

            case BinaryExprNode bin:
                Console.WriteLine($"{padding}BinaryExpr: {PrettyOperator(bin.Operator)}");
                Print(bin.Left, indent + 2);
                Print(bin.Right, indent + 2);
                break;

            case CallExprNode call:
                Console.WriteLine($"{padding}CallExpr: {call.FunctionName}");

                if (call.Arguments.Count > 0)
                {
                    Console.WriteLine($"{padding}  Arguments:");
                    foreach (var arg in call.Arguments)
                        Print(arg, indent + 4);
                }
                break;

            case IdentifierNode id:
                Console.WriteLine($"{padding}Identifier: {id.Name}");
                break;

            case LiteralNode lit:
                Console.WriteLine($"{padding}Literal: {lit.Value}");
                break;

            default:
                Console.WriteLine($"{padding}{node.GetType().Name}");
                break;
        }
    }

    // ===================== FORMATO BONITO PARA OPERADORES =====================

    private static string PrettyOperator(BinaryOperator op) => op switch
    {
        BinaryOperator.Add => "+",
        BinaryOperator.Subtract => "-",
        BinaryOperator.Multiply => "*",
        BinaryOperator.Divide => "/",
        BinaryOperator.Modulo => "%",

        BinaryOperator.And => "&&",
        BinaryOperator.Or => "||",

        BinaryOperator.Equal => "==",
        BinaryOperator.NotEqual => "!=",

        BinaryOperator.Negate => "NEG",

        _ => op.ToString()
    };
}
