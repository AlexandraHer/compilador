using MyLangCompiler.Nodes;
using MyLangCompiler.Enumerations;
using System;
using System.Linq;

namespace MyLangCompiler.Runtime;

public sealed class Interpreter
{
    private RuntimeScope _scope = new RuntimeScope(null);
    private ProgramNode? _program;

    public object? Execute(ProgramNode program)
    {
        _program = program;

        var entryFunction = program.Declarations
            .OfType<FunctionNode>()
            .FirstOrDefault(f => f.IsEntry);

        if (entryFunction == null)
        {
            // Fallback opcional: por si el archivo no usa 'entry'
            entryFunction = program.Declarations
                .OfType<FunctionNode>()
                .FirstOrDefault(f => string.Equals(f.Name, "main", StringComparison.OrdinalIgnoreCase));
        }

        if (entryFunction == null)
            throw new Exception("No entry function found (expected one marked with 'entry').");

        return ExecuteFunction(entryFunction, new List<object?>());
    }

    // ===================== FUNCTION =====================

    private object? ExecuteFunction(FunctionNode function, List<object?> args)
    {
        var previous = _scope;
        _scope = new RuntimeScope(null);

        try
        {
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

            case ExprStmtNode exprStmt:
                EvaluateExpression(exprStmt.Expression);
                return (false, null);

            case BlockNode nestedBlock:
                var result = ExecuteBlock(nestedBlock);
                return (result != null, result);
            
            case IfNode ifNode:
            {
                var ifResult = ExecuteIf(ifNode);
                if (ifResult != null) return (true, ifResult);
                return (false, null);
            }

            case WhileNode whileNode:
            {
                var whileResult = ExecuteWhile(whileNode);
                if (whileResult != null) return (true, whileResult);
                return (false, null);
            }

            case ForNode forNode:
            {
                var forResult = ExecuteFor(forNode);
                if (forResult != null) return (true, forResult);
                return (false, null);
            }

            default:
                throw new Exception($"Unsupported statement '{stmt.GetType().Name}'.");
        }
    }

    private static bool TryGetArraySize(TypeRefNode typeRef, out int size)
    {
        size = 0;
        var text = typeRef.Name;

        var l = text.IndexOf('[');
        var r = text.IndexOf(']');

        if (l < 0 || r < 0 || r <= l) return false;

        var inside = text.Substring(l + 1, r - l - 1);
        return int.TryParse(inside, out size);
    }

    private static string GetBaseTypeName(TypeRefNode typeRef)
    {
        var text = typeRef.Name;

        // quitar [n]
        var bracket = text.IndexOf('[');
        if (bracket >= 0) text = text.Substring(0, bracket);

        // quitar ?
        if (text.EndsWith("?")) text = text.Substring(0, text.Length - 1);

        return text;
    }

    // ===================== VARIABLES =====================

    private void ExecuteVarDecl(VarDeclNode varDecl)
    {
        object? value = null;

        if (varDecl.Initializer != null)
        {
            value = EvaluateExpression(varDecl.Initializer);
        }
        else
        {
            // declare arr:i[3]; -> crear lista con tamaño 3
            if (TryGetArraySize(varDecl.Type, out var size))
            {
                var baseType = GetBaseTypeName(varDecl.Type);

                object? def = baseType switch
                {
                    "i" => 0,
                    "f" => 0.0,
                    "b" => false,
                    "s" => "",
                    _ => null // clases/objetos: null por defecto
                };

                var list = new List<object?>(size);
                for (int i = 0; i < size; i++) list.Add(def);
                value = list;
            }
        }

        if (!_scope.Declare(varDecl.Name, value))
            throw new Exception($"Variable '{varDecl.Name}' already declared in this scope (runtime).");
    }

    private void ExecuteAssign(AssignNode assign)
    {
        // set arr[0] = ...
        if (assign.Target is IndexExprNode idx)
        {
            var target = EvaluateExpression(idx.Target);
            var index = EvaluateExpression(idx.Index);
            var value = EvaluateExpression(assign.Value);

            if (target is not List<object?> list)
                throw new Exception("Index assignment only supported on arrays (runtime).");

            if (index is not int i)
                throw new Exception("Array index must be int (runtime).");

            list[i] = value;
            return;
        }

        // set x = ...
        var identifier = assign.Target as IdentifierNode
            ?? throw new Exception("Assignment target must be identifier or index.");

        var value2 = EvaluateExpression(assign.Value);

        if (!_scope.Assign(identifier.Name, value2))
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

            case MethodCallExprNode mcall:
                return EvaluateMethodCall(mcall); 
            
            case ArrayLiteralNode arr:
                return arr.Items.Select(EvaluateExpression).ToList();

            case IndexExprNode idx:
            {
                var target = EvaluateExpression(idx.Target);
                var index = EvaluateExpression(idx.Index);

                if (target is not List<object?> list)
                    throw new Exception("Indexing only supported on arrays (List<object?>) runtime.");

                if (index is not int i)
                    throw new Exception("Array index must be int (runtime).");

                return list[i];
            }

            default:
                throw new Exception($"Unsupported expression '{expr.GetType().Name}'.");
        }
    }

    // ===================== FUNCTION CALL =====================

    private object? EvaluateCall(CallExprNode call)
    {
        // ===================== BUILT-INS =====================
        switch (call.FunctionName)
        {
            case "show":
            {
                if (call.Arguments.Count != 1)
                    throw new Exception("show expects 1 argument (runtime).");

                var value = EvaluateExpression(call.Arguments[0]);
                Console.Write(value?.ToString() ?? "null");  // NO salto de línea
                return 0; // dummy
            }

            case "ask":
            {
                if (call.Arguments.Count != 1)
                    throw new Exception("ask expects 1 argument (runtime).");

                if (call.Arguments[0] is not IdentifierNode id)
                    throw new Exception("ask expects an identifier (runtime).");

                var input = Console.ReadLine() ?? "";

                // ask(var) -> mete string en la variable
                if (!_scope.Assign(id.Name, input))
                    throw new Exception($"Variable '{id.Name}' not declared (runtime).");

                return input; // retorna string, por si lo usan en expresiones
            }

            case "len":
            {
                if (call.Arguments.Count != 1)
                    throw new Exception("len expects 1 argument (runtime).");

                var v = EvaluateExpression(call.Arguments[0]);

                // Si luego implementas arrays como List<object?> o similar, ajustas aquí
                if (v is string s) return s.Length;
                if (v is System.Collections.ICollection c) return c.Count;

                throw new Exception("len supports string or collection (runtime).");
            }

            case "convertToInt":
            {
                if (call.Arguments.Count != 1)
                    throw new Exception("convertToInt expects 1 argument (runtime).");

                var v = EvaluateExpression(call.Arguments[0])?.ToString() ?? "";
                if (int.TryParse(v, out var i)) return i;
                throw new Exception("convertToInt: invalid int value (runtime).");
            }

            case "convertToFloat":
            {
                if (call.Arguments.Count != 1)
                    throw new Exception("convertToFloat expects 1 argument (runtime).");

                var v = EvaluateExpression(call.Arguments[0])?.ToString() ?? "";
                if (double.TryParse(v, out var d)) return d;
                throw new Exception("convertToFloat: invalid float value (runtime).");
            }

            case "convertToBoolean":
            {
                if (call.Arguments.Count != 1)
                    throw new Exception("convertToBoolean expects 1 argument (runtime).");

                var v = (EvaluateExpression(call.Arguments[0])?.ToString() ?? "").Trim().ToLowerInvariant();
                if (v == "true") return true;
                if (v == "false") return false;
                throw new Exception("convertToBoolean: invalid boolean value (runtime).");
            }
        }

        // ===================== CONSTRUCTOR (ClassName()) =====================
        var isClass = _program!.Declarations
            .OfType<ClassDeclNode>()
            .Any(c => c.Name == call.FunctionName);

        if (isClass)
        {
            if (call.Arguments.Count != 0)
                throw new Exception($"Constructor '{call.FunctionName}' expects 0 arguments (runtime).");

            // Representación mínima del objeto (por ahora)
            return new Dictionary<string, object?> { ["__class"] = call.FunctionName };
        }
        
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
    private object? EvaluateMethodCall(MethodCallExprNode call)
    {
        // Evaluamos receiver solo para validar que existe (obj)
        _ = EvaluateExpression(call.Receiver);

        // Como ustedes aplanaron métodos a funciones globales,
        // tratamos obj.suma(...) como suma(...)
        var function = _program!.Declarations
            .OfType<FunctionNode>()
            .FirstOrDefault(f => f.Name == call.MethodName);

        if (function == null)
            throw new Exception($"Method/function '{call.MethodName}' not found (runtime).");

        var args = call.Arguments
            .Select(a => EvaluateExpression(a))
            .ToList();

        return ExecuteFunction(function, args);
    }

    private object? ExecuteIf(IfNode node)
    {
        var cond = EvaluateExpression(node.Condition);
        if (cond is not bool b)
            throw new Exception("Condition in 'check' must evaluate to bool (runtime).");

        if (b)
            return ExecuteBlock(node.ThenBlock);

        if (node.ElseBlock != null)
            return ExecuteBlock(node.ElseBlock);

        return null;
    }

    private object? ExecuteWhile(WhileNode node)
    {
        while (true)
        {
            var cond = EvaluateExpression(node.Condition);
            if (cond is not bool b)
                throw new Exception("Condition in 'repeat' must evaluate to bool (runtime).");

            if (!b) break;

            var result = ExecuteBlock(node.Body);
            if (result != null) return result; // propagate return
        }

        return null;
    }

    private object? ExecuteFor(ForNode node)
    {
        // Scope propio del loop (igual que hiciste en semántica)
        var previous = _scope;
        _scope = new RuntimeScope(previous);

        try
        {
            if (node.Init != null)
            {
                var (hasReturn, value) = ExecuteStatement(node.Init);
                if (hasReturn) return value;
            }

            while (true)
            {
                var cond = EvaluateExpression(node.Condition);
                if (cond is not bool b)
                    throw new Exception("Condition in 'loop' must evaluate to bool (runtime).");

                if (!b) break;

                var bodyResult = ExecuteBlock(node.Body);
                if (bodyResult != null) return bodyResult; // propagate return

                if (node.Action != null)
                {
                    var (hasReturn, value) = ExecuteStatement(node.Action);
                    if (hasReturn) return value;
                }
            }

            return null;
        }
        finally
        {
            _scope = previous;
        }
    }

    // ===================== HELPERS =====================

    private static bool IsNumber(object? v) => v is int || v is double;

    private static double ToDouble(object v)
        => v is int i ? i : (double)v;

    // ===================== BINARY =====================

    private object? EvaluateBinary(BinaryExprNode bin)
    {
        var left = EvaluateExpression(bin.Left);
        var right = EvaluateExpression(bin.Right);

        switch (bin.Operator)
        {
            // --------- Arithmetic (int/double) ---------
            case BinaryOperator.Add:
                if (left is string || right is string)
                    return $"{left}{right}";
                if (IsNumber(left) && IsNumber(right))
                {
                    if (left is double || right is double)
                        return ToDouble(left!) + ToDouble(right!);
                    return (int)left! + (int)right!;
                }
                throw new Exception("Add supports numbers or string concatenation.");

            case BinaryOperator.Subtract:
                if (IsNumber(left) && IsNumber(right))
                {
                    if (left is double || right is double)
                        return ToDouble(left!) - ToDouble(right!);
                    return (int)left! - (int)right!;
                }
                throw new Exception("Subtract supports numbers only.");

            case BinaryOperator.Multiply:
                if (IsNumber(left) && IsNumber(right))
                {
                    if (left is double || right is double)
                        return ToDouble(left!) * ToDouble(right!);
                    return (int)left! * (int)right!;
                }
                throw new Exception("Multiply supports numbers only.");

            case BinaryOperator.Divide:
                if (IsNumber(left) && IsNumber(right))
                {
                    // división: si cualquiera es double, devuelve double
                    if (left is double || right is double)
                        return ToDouble(left!) / ToDouble(right!);
                    return (int)left! / (int)right!;
                }
                throw new Exception("Divide supports numbers only.");

            case BinaryOperator.Modulo:
                if (left is int li && right is int ri)
                    return li % ri;
                throw new Exception("Modulo supports int only.");

            // --------- Relational ---------
            case BinaryOperator.Equal:
                return Equals(left, right);

            case BinaryOperator.NotEqual:
                return !Equals(left, right);

            case BinaryOperator.Less:
                if (IsNumber(left) && IsNumber(right))
                    return ToDouble(left!) < ToDouble(right!);
                throw new Exception("'<': supports numbers only.");

            case BinaryOperator.LessOrEqual:
                if (IsNumber(left) && IsNumber(right))
                    return ToDouble(left!) <= ToDouble(right!);
                throw new Exception("'<=': supports numbers only.");

            case BinaryOperator.Greater:
                if (IsNumber(left) && IsNumber(right))
                    return ToDouble(left!) > ToDouble(right!);
                throw new Exception("'>' supports numbers only.");

            case BinaryOperator.GreaterOrEqual:
                if (IsNumber(left) && IsNumber(right))
                    return ToDouble(left!) >= ToDouble(right!);
                throw new Exception("'>=' supports numbers only.");

            // --------- Logical ---------
            case BinaryOperator.And:
                return (bool)left! && (bool)right!;

            case BinaryOperator.Or:
                return (bool)left! || (bool)right!;

            // --------- Unary modeled as 0 - expr ---------
            case BinaryOperator.Negate:
                if (right == null) throw new Exception("Negate requires right operand.");
                if (right is int ri2) return -ri2;
                if (right is double rd2) return -rd2;
                throw new Exception("Negate supports numbers only.");

            default:
                throw new Exception($"Unsupported operator '{bin.Operator}'.");
        }
    }
}