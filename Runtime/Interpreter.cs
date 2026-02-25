using MyLangCompiler.Nodes;
using MyLangCompiler.Enumerations;

namespace MyLangCompiler.Runtime;

public sealed class Interpreter
{
    private RuntimeScope _scope = new RuntimeScope(null);
    private ProgramNode? _program;

    public object? Execute(ProgramNode program)
    {
        _program = program;

        var mainFunction = program.Declarations
            .OfType<FunctionNode>()
            .FirstOrDefault(f => f.Name == "main");

        if (mainFunction == null)
            throw new Exception("No main function found.");

        return ExecuteFunction(mainFunction, new List<object?>());
    }

    // ===================== FUNCTION =====================

    private object? ExecuteFunction(FunctionNode function, List<object?> args)
    {
        var previous = _scope;
        _scope = new RuntimeScope(null);

        try
        {
            // Registrar parámetros
            for (int i = 0; i < function.Parameters.Count; i++)
            {
                var paramName = function.Parameters[i].Name;
                var value = i < args.Count ? args[i] : null;

                _scope.Declare(paramName, value);
            }

            return ExecuteBlock(function.Body);
        }
        finally
        {
            _scope = previous;
        }
    }

    // ===================== BLOCK =====================

    private object? ExecuteBlock(BlockNode block)
    {
        var previous = _scope;
        _scope = new RuntimeScope(previous);

        try
        {
            foreach (var stmt in block.Statements)
            {
                var (hasReturn, value) = ExecuteStatement(stmt);

                if (hasReturn)
                    return value;
            }

            return null;
        }
        finally
        {
            _scope = previous;
        }
    }

    // ===================== STATEMENTS =====================

    private (bool hasReturn, object? value) ExecuteStatement(StmtNode stmt)
    {
        switch (stmt)
        {
            case VarDeclNode varDecl:
                ExecuteVarDecl(varDecl);
                return (false, null);

            case AssignNode assign:
                ExecuteAssign(assign);
                return (false, null);

            case ReturnNode ret:
                return (true, EvaluateExpression(ret.Value));

            case BlockNode nestedBlock:
                var result = ExecuteBlock(nestedBlock);
                return (result != null, result);

            default:
                throw new Exception($"Unsupported statement '{stmt.GetType().Name}'.");
        }
    }

    // ===================== VARIABLES =====================

    private void ExecuteVarDecl(VarDeclNode varDecl)
    {
        object? value = null;

        if (varDecl.Initializer != null)
            value = EvaluateExpression(varDecl.Initializer);

        if (!_scope.Declare(varDecl.Name, value))
            throw new Exception($"Variable '{varDecl.Name}' already declared in this scope (runtime).");
    }

    private void ExecuteAssign(AssignNode assign)
    {
        var identifier = assign.Target as IdentifierNode
            ?? throw new Exception("Assignment target must be identifier.");

        var value = EvaluateExpression(assign.Value);

        if (!_scope.Assign(identifier.Name, value))
            throw new Exception($"Variable '{identifier.Name}' not declared (runtime).");
    }

    // ===================== EXPRESSIONS =====================

    private object? EvaluateExpression(ExprNode expr)
    {
        switch (expr)
        {
            case LiteralNode lit:
                return lit.Value;

            case IdentifierNode id:
                if (!_scope.TryGet(id.Name, out var value))
                    throw new Exception($"Variable '{id.Name}' not declared (runtime).");
                return value;

            case BinaryExprNode bin:
                return EvaluateBinary(bin);

            case CallExprNode call:
                return EvaluateCall(call);

            default:
                throw new Exception($"Unsupported expression '{expr.GetType().Name}'.");
        }
    }

    // ===================== FUNCTION CALL =====================

    private object? EvaluateCall(CallExprNode call)
    {
        var function = _program!.Declarations
            .OfType<FunctionNode>()
            .FirstOrDefault(f => f.Name == call.FunctionName);

        if (function == null)
            throw new Exception($"Function '{call.FunctionName}' not found (runtime).");

        var args = call.Arguments
            .Select(a => EvaluateExpression(a))
            .ToList();

        return ExecuteFunction(function, args);
    }

    // ===================== BINARY =====================

    private object? EvaluateBinary(BinaryExprNode bin)
    {
        var left = EvaluateExpression(bin.Left);
        var right = EvaluateExpression(bin.Right);

        switch (bin.Operator)
        {
            case BinaryOperator.Add:
                return (int)left! + (int)right!;

            case BinaryOperator.Subtract:
                return (int)left! - (int)right!;

            case BinaryOperator.Multiply:
                return (int)left! * (int)right!;

            case BinaryOperator.Divide:
                return (int)left! / (int)right!;

            case BinaryOperator.Modulo:
                return (int)left! % (int)right!;

            case BinaryOperator.Equal:
                return Equals(left, right);

            case BinaryOperator.NotEqual:
                return !Equals(left, right);

            case BinaryOperator.And:
                return (bool)left! && (bool)right!;

            case BinaryOperator.Or:
                return (bool)left! || (bool)right!;

            case BinaryOperator.Negate:
                return -(int)right!;

            default:
                throw new Exception($"Unsupported operator '{bin.Operator}'.");
        }
    }
}
