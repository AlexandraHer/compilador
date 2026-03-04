using System;
using MyLangCompiler.Nodes;
using MyLangCompiler.Exceptions;
using MyLangCompiler.Enumerations;

namespace MyLangCompiler.Semantic;

public sealed class SemanticAnalyzerVisitor
{
    private Scope _currentScope = new Scope(null);

    public void Analyze(ProgramNode program)
    {
        if (program == null) throw new ArgumentNullException(nameof(program));

        var entryCount = program.Declarations
            .OfType<FunctionNode>()
            .Count(f => f.IsEntry);

        if (entryCount != 1)
            throw new SemanticException($"Expected exactly 1 entry function, but found {entryCount}.");

        DeclareAllTypes(program);
        DeclareAllFunctions(program);  // Fase 1
        VisitProgram(program);         // Fase 2
    }

    private void DeclareAllTypes(ProgramNode program)
{
    foreach (var decl in program.Declarations)
    {
        // Si ya tienes ClassDeclNode:
        if (decl is ClassDeclNode cls)
            TypeResolver.RegisterClass(cls.Name);
    }
}

    // ===================== FASE 1 =====================
    // Declarar todas las funciones primero (permite llamadas adelantadas y recursión)
    private void DeclareAllFunctions(ProgramNode program)
    {
        foreach (var decl in program.Declarations)
        {
            if (decl is not FunctionNode fn)
                continue;

            var returnType = TypeResolver.Resolve(fn.ReturnType);

            // ctor: (name, returnType, parameters, declaration)
            var symbol = new FunctionSymbol(fn.Name, returnType, fn.Parameters, fn);

            if (!_currentScope.Declare(symbol))
                throw new SemanticException($"Function '{fn.Name}' already declared.");
        }
    }

    // ===================== FASE 2 =====================
    private void VisitProgram(ProgramNode program)
    {
        foreach (var decl in program.Declarations)
            if (decl is FunctionNode fn)
                VisitFunction(fn);
    }

    private void VisitFunction(FunctionNode function)
    {
        var fnSymbol = _currentScope.Lookup(function.Name) as FunctionSymbol;
        if (fnSymbol == null)
            throw new SemanticException($"Function '{function.Name}' not declared (internal error).");

        var expectedReturnType = fnSymbol.ReturnType;

        var previousScope = _currentScope;
        _currentScope = new Scope(previousScope);

        // Declarar parámetros en el scope de la función
        foreach (var param in function.Parameters)
        {
            var paramType = TypeResolver.Resolve(param.Type);
            var paramSymbol = new VariableSymbol(param.Name, paramType);

            if (!_currentScope.Declare(paramSymbol))
                throw new SemanticException($"Parameter '{param.Name}' already declared.");
        }

        VisitBlock(function.Body, expectedReturnType);

        _currentScope = previousScope;
    }

    private void VisitBlock(BlockNode block, TypeSymbol expectedReturnType)
    {
        var previousScope = _currentScope;
        _currentScope = new Scope(previousScope);

        foreach (var stmt in block.Statements)
            VisitStatement(stmt, expectedReturnType);

        _currentScope = previousScope;
    }

    private void VisitStatement(StmtNode stmt, TypeSymbol expectedReturnType)
    {
        if (stmt == null)
            throw new SemanticException("A statement node was null (AST construction error).");

        switch (stmt)
        {
            case VarDeclNode varDecl:
                VisitVarDecl(varDecl);
                return;

            case AssignNode assign:
                VisitAssign(assign);
                return;

            case ReturnNode ret:
                VisitReturn(ret, expectedReturnType);
                return;

            // ✅ NUEVO: expresiones como statement (show(...);, ask(...);, obj.suma(...);, etc.)
            case ExprStmtNode exprStmt:
                VisitExpression(exprStmt.Expression);
                return;

            // ✅ NUEVO: if/check
            case IfNode ifNode:
                VisitIf(ifNode, expectedReturnType);
                return;

            // ✅ NUEVO: while/repeat
            case WhileNode whileNode:
                VisitWhile(whileNode, expectedReturnType);
                return;

            // ✅ NUEVO: for/loop
            case ForNode forNode:
                VisitFor(forNode, expectedReturnType);
                return;

            default:
                throw new SemanticException($"Unsupported statement '{stmt.GetType().Name}'.");
        }
    }

    private void VisitIf(IfNode node, TypeSymbol expectedReturnType)
    {
        var condType = VisitExpression(node.Condition);
        if (condType != BuiltInTypeSymbol.Bool)
            throw new SemanticException("Condition in 'check' must be bool.");

        VisitBlock(node.ThenBlock, expectedReturnType);

        if (node.ElseBlock != null)
            VisitBlock(node.ElseBlock, expectedReturnType);
    }

    private void VisitWhile(WhileNode node, TypeSymbol expectedReturnType)
    {
        var condType = VisitExpression(node.Condition);
        if (condType != BuiltInTypeSymbol.Bool)
            throw new SemanticException("Condition in 'repeat' must be bool.");

        VisitBlock(node.Body, expectedReturnType);
    }

    private void VisitFor(ForNode node, TypeSymbol expectedReturnType)
    {
        // Creamos un scope propio del for (para init/cond/action)
        var previousScope = _currentScope;
        _currentScope = new Scope(previousScope);

        try
        {
            if (node.Init != null)
                VisitStatement(node.Init, expectedReturnType);

            var condType = VisitExpression(node.Condition);
            if (condType != BuiltInTypeSymbol.Bool)
                throw new SemanticException("Condition in 'loop' must be bool.");

            if (node.Action != null)
                VisitStatement(node.Action, expectedReturnType);

            VisitBlock(node.Body, expectedReturnType);
        }
        finally
        {
            _currentScope = previousScope;
        }
    }

    private void VisitVarDecl(VarDeclNode varDecl)
    {
        var declaredType = TypeResolver.Resolve(varDecl.Type);

        var symbol = new VariableSymbol(varDecl.Name, declaredType);
        if (!_currentScope.Declare(symbol))
            throw new SemanticException($"Variable '{varDecl.Name}' already declared in this scope.");

        if (varDecl.Initializer == null) return;

        // Caso: declare arr:i[3] = [ ... ]
        if (UnwrapNullable(declaredType) is ArrayTypeSymbol declaredArr && varDecl.Initializer is ArrayLiteralNode lit)
        {
            if (TypeResolver.TryGetArraySize(varDecl.Type, out var declaredSize))
            {
                if (lit.Items.Count != declaredSize)
                    throw new SemanticException(
                        $"Array literal size mismatch for '{varDecl.Name}'. Expected {declaredSize} elements, got {lit.Items.Count}.");
            }

            // validar tipo de elementos
            foreach (var item in lit.Items)
            {
                var itemType = VisitExpression(item);
                if (!IsAssignable(declaredArr.ElementType, itemType))
                    throw new SemanticException(
                        $"Type mismatch in array initializer of '{varDecl.Name}'. Expected '{declaredArr.ElementType}', got '{itemType}'.");
            }

            return;
        }

        // Caso general
        var initType = VisitExpression(varDecl.Initializer);
        if (!IsAssignable(declaredType, initType))
            throw new SemanticException(
                $"Type mismatch in initializer of '{varDecl.Name}'. Expected '{declaredType}', got '{initType}'.");
    }

    private void VisitAssign(AssignNode assign)
    {
        // set arr[0] = expr;
        if (assign.Target is IndexExprNode idx)
        {
            var targetType = VisitExpression(idx.Target);
            targetType = UnwrapNullable(targetType);

            if (targetType is not ArrayTypeSymbol arrType)
                throw new SemanticException("Index assignment target must be an array.");

            var indexType = VisitExpression(idx.Index);
            if (indexType != BuiltInTypeSymbol.Int)
                throw new SemanticException("Array index must be int.");

            var valueType = VisitExpression(assign.Value);
            if (!IsAssignable(arrType.ElementType, valueType))
                throw new SemanticException(
                    $"Type mismatch in array element assignment. Expected '{arrType.ElementType}', got '{valueType}'.");

            return;
        }

        // set x = expr;  o set arr = [..]
        if (assign.Target is not IdentifierNode identifier)
            throw new SemanticException("Assignment target must be an identifier or an array index.");

        var symbol = _currentScope.Lookup(identifier.Name) as VariableSymbol;
        if (symbol == null)
            throw new SemanticException($"Variable '{identifier.Name}' not declared.");
        
        if (symbol.Type is ArrayTypeSymbol arrT && assign.Value is ArrayLiteralNode lit)
        {
            foreach (var item in lit.Items)
            {
                var itemType = VisitExpression(item); // puede ser NullTypeSymbol
                if (!IsAssignable(arrT.ElementType, itemType))
                    throw new SemanticException(
                        $"Type mismatch in array assignment to '{symbol.Name}'. Expected elements '{arrT.ElementType}', got '{itemType}'.");
            }
            return;
        }

        var exprType = VisitExpression(assign.Value);
        if (!IsAssignable(symbol.Type, exprType))
            throw new SemanticException(
                $"Type mismatch in assignment to '{symbol.Name}'. Expected '{symbol.Type}', got '{exprType}'.");
    }

    private void VisitReturn(ReturnNode ret, TypeSymbol expectedReturnType)
    {
        var returnType = VisitExpression(ret.Value);

        if (returnType != expectedReturnType)
            throw new SemanticException($"Return type mismatch. Expected '{expectedReturnType}', got '{returnType}'.");
    }

    // ===================== EXPRESSIONS =====================
    private TypeSymbol VisitExpression(ExprNode expr)
    {
        if (expr == null)
            throw new SemanticException("Expression node was null (AST construction error).");

        switch (expr)
        {
            case LiteralNode lit:
                return ResolveLiteralType(lit);

            case IdentifierNode id:
                {
                    var sym = _currentScope.Lookup(id.Name) as VariableSymbol;
                    if (sym == null)
                        throw new SemanticException($"Variable '{id.Name}' not declared.");
                    return sym.Type;
                }

            case CallExprNode call:
                return VisitCall(call);

            case MethodCallExprNode mcall:
                return VisitMethodCall(mcall);

            case BinaryExprNode bin:
                return VisitBinary(bin);
            
            case ArrayLiteralNode arr:
                return VisitArrayLiteral(arr);

            case IndexExprNode idx:
                return VisitIndexExpr(idx);

            case UnaryExprNode un:
                return VisitUnary(un);
            default:
                throw new SemanticException($"Unsupported expression type '{expr.GetType().Name}'.");
        }
    }

    private TypeSymbol VisitBinary(BinaryExprNode bin)
    {
        // NEGATE modelado como BinaryExpr(0, expr)
        if (bin.Operator == BinaryOperator.Negate)
        {
            var rightType = VisitExpression(bin.Right);

            if (rightType != BuiltInTypeSymbol.Int && rightType != BuiltInTypeSymbol.Float)
                throw new SemanticException("Unary '-' only valid for int/float.");

            return rightType;
        }

        var leftType = VisitExpression(bin.Left);
        var rightType2 = VisitExpression(bin.Right);

        // Comparaciones: == != < <= > >=  => bool
        if (bin.Operator == BinaryOperator.Equal || bin.Operator == BinaryOperator.NotEqual)
        {
            if (leftType != rightType2)
                throw new SemanticException("Type mismatch in comparison (==/!=).");

            return BuiltInTypeSymbol.Bool;
        }

        if (bin.Operator == BinaryOperator.Less ||
            bin.Operator == BinaryOperator.LessOrEqual ||
            bin.Operator == BinaryOperator.Greater ||
            bin.Operator == BinaryOperator.GreaterOrEqual)
        {
            if (leftType != rightType2)
                throw new SemanticException("Type mismatch in comparison (<,<=,>,>=).");

            if (leftType != BuiltInTypeSymbol.Int && leftType != BuiltInTypeSymbol.Float)
                throw new SemanticException("Relational operators only valid for int/float.");

            return BuiltInTypeSymbol.Bool;
        }

        if (leftType != rightType2)
            throw new SemanticException("Type mismatch in binary expression.");

        return ValidateBinaryOperator(bin.Operator, leftType);
    }
    private TypeSymbol VisitCall(CallExprNode call)
    {
        // ================= BUILT-INS =================
        // Estos NO están declarados como FunctionNode, así que no deben buscarse en el scope.
        switch (call.FunctionName)
        {
            case "show":
                // show(expr) => permitido para cualquier tipo (solo validar que la expr exista)
                if (call.Arguments.Count != 1)
                    throw new SemanticException("show expects 1 argument.");
                VisitExpression(call.Arguments[0]);
                return BuiltInTypeSymbol.Int; // retorno dummy, porque se usa como statement

            case "ask":
                // ask(ID) => el argumento debe ser variable existente (IdentifierNode)
                if (call.Arguments.Count != 1)
                    throw new SemanticException("ask expects 1 argument.");

                if (call.Arguments[0] is not IdentifierNode id)
                    throw new SemanticException("ask expects an identifier.");

                var v = _currentScope.Lookup(id.Name) as VariableSymbol;
                if (v == null)
                    throw new SemanticException($"Variable '{id.Name}' not declared.");

                // normalmente ask mete un string en la variable
                // si quieres ser estricto: exigir BuiltInTypeSymbol.String
                return BuiltInTypeSymbol.String;

            case "len":
                if (call.Arguments.Count != 1)
                    throw new SemanticException("len expects 1 argument.");

                var tLen = VisitExpression(call.Arguments[0]);
                tLen = UnwrapNullable(tLen);

                if (tLen is not ArrayTypeSymbol)
                    throw new SemanticException("len(...) expects an array.");

                return BuiltInTypeSymbol.Int;

            case "convertToInt":
                if (call.Arguments.Count != 1)
                    throw new SemanticException("convertToInt expects 1 argument.");
                VisitExpression(call.Arguments[0]);
                return BuiltInTypeSymbol.Int;

            case "convertToFloat":
                if (call.Arguments.Count != 1)
                    throw new SemanticException("convertToFloat expects 1 argument.");
                VisitExpression(call.Arguments[0]);
                return BuiltInTypeSymbol.Float;

            case "convertToBoolean":
                if (call.Arguments.Count != 1)
                    throw new SemanticException("convertToBoolean expects 1 argument.");
                VisitExpression(call.Arguments[0]);
                return BuiltInTypeSymbol.Bool;

            case "readfile":
                if (call.Arguments.Count != 1)
                    throw new SemanticException("readfile expects 1 argument.");
                VisitExpression(call.Arguments[0]);
                return BuiltInTypeSymbol.String;

            case "writefile":
                if (call.Arguments.Count != 2)
                    throw new SemanticException("writefile expects 2 arguments.");
                VisitExpression(call.Arguments[0]);
                VisitExpression(call.Arguments[1]);
                return BuiltInTypeSymbol.Bool;
        }

        if (TypeResolver.TryResolveName(call.FunctionName, out var t) && t is ClassTypeSymbol)
        {
            if (call.Arguments.Count != 0)
                throw new SemanticException($"Constructor '{call.FunctionName}' expects 0 arguments (for now).");

            return t;
        }

        // ================= USER FUNCTIONS =================
        var fn = _currentScope.Lookup(call.FunctionName) as FunctionSymbol;
        if (fn == null)
            throw new SemanticException($"Function '{call.FunctionName}' not declared.");

        if (call.Arguments.Count != fn.Parameters.Count)
            throw new SemanticException(
                $"Function '{call.FunctionName}' expects {fn.Parameters.Count} arguments, but got {call.Arguments.Count}.");

        for (int i = 0; i < call.Arguments.Count; i++)
        {
            var argType = VisitExpression(call.Arguments[i]);
            var paramType = TypeResolver.Resolve(fn.Parameters[i].Type);

            if (argType != paramType)
                throw new SemanticException($"Type mismatch in argument {i + 1} of '{call.FunctionName}'. Expected '{paramType}', got '{argType}'.");
        }

        return fn.ReturnType;
    }

    private TypeSymbol VisitMethodCall(MethodCallExprNode call)
    {
        // ✅ Mínimo viable: tratamos obj.suma(...) como llamada a función global "suma"
        // (porque tú estás aplanando los métodos de object al nivel global)
        var fn = _currentScope.Lookup(call.MethodName) as FunctionSymbol;
        if (fn == null)
            throw new SemanticException($"Method/function '{call.MethodName}' not declared.");

        if (call.Arguments.Count != fn.Parameters.Count)
            throw new SemanticException(
                $"'{call.MethodName}' expects {fn.Parameters.Count} arguments, but got {call.Arguments.Count}.");

        for (int i = 0; i < call.Arguments.Count; i++)
        {
            var argType = VisitExpression(call.Arguments[i]);
            var paramType = TypeResolver.Resolve(fn.Parameters[i].Type);

            if (argType != paramType)
                throw new SemanticException($"Type mismatch in argument {i + 1} of '{call.MethodName}'. Expected '{paramType}', got '{argType}'.");
        }

        // También validamos el receiver al menos como expresión válida:
        VisitExpression(call.Receiver);

        return fn.ReturnType;
    }

    private TypeSymbol ValidateBinaryOperator(BinaryOperator op, TypeSymbol type)
    {
        switch (op)
        {
            case BinaryOperator.Add:
                if (type == BuiltInTypeSymbol.Int || type == BuiltInTypeSymbol.Float)
                    return type;
                break;

            case BinaryOperator.Subtract:
            case BinaryOperator.Multiply:
            case BinaryOperator.Divide:
            case BinaryOperator.Modulo:
                if (type == BuiltInTypeSymbol.Int || type == BuiltInTypeSymbol.Float)
                    return type;
                break;

            case BinaryOperator.And:
            case BinaryOperator.Or:
                if (type == BuiltInTypeSymbol.Bool)
                    return type;
                break;
        }

        throw new SemanticException($"Operator '{op}' not supported for type '{type}'.");
    }

    private TypeSymbol ResolveLiteralType(LiteralNode lit)
    {
        if (lit.Value == null)
            return NullTypeSymbol.Instance;

        return lit.Value switch
        {
            int => BuiltInTypeSymbol.Int,
            double => BuiltInTypeSymbol.Float,
            bool => BuiltInTypeSymbol.Bool,
            string => BuiltInTypeSymbol.String,
            _ => throw new SemanticException($"Unknown literal type '{lit.Value.GetType().Name}'.")
        };
    }

    private static TypeSymbol UnwrapNullable(TypeSymbol t)
        => t is NullableTypeSymbol nt ? nt.Underlying : t;

    private static bool IsAssignable(TypeSymbol target, TypeSymbol source)
    {
        if (source is NullTypeSymbol)
            return target is NullableTypeSymbol;

        if (target is NullableTypeSymbol nt)
        {
            if (source == nt) return true;
            return source == nt.Underlying;
        }

        return target == source;
    }

    private TypeSymbol VisitUnary(UnaryExprNode un)
    {
        var t = VisitExpression(un.Operand);

        if (un.Operator == UnaryOperatorKind.Not)
        {
            if (t != BuiltInTypeSymbol.Bool)
                throw new SemanticException("Operator 'not' only valid for bool.");
            return BuiltInTypeSymbol.Bool;
        }

        throw new SemanticException($"Unsupported unary operator '{un.Operator}'.");
    }

    private TypeSymbol VisitArrayLiteral(ArrayLiteralNode arr)
    {
        if (arr.Items.Count == 0)
            throw new SemanticException("Cannot infer type of empty array literal [].");

        // Tipo del primer elemento (sin nullable)
        var first = VisitExpression(arr.Items[0]);
        first = UnwrapNullable(first);

        // No permitimos null dentro del literal por ahora (simple y seguro)
        if (first is NullTypeSymbol)
            throw new SemanticException("Array literal cannot start with null (cannot infer element type).");

        for (int i = 1; i < arr.Items.Count; i++)
        {
            var t = VisitExpression(arr.Items[i]);
            t = UnwrapNullable(t);

            if (t is NullTypeSymbol)
                throw new SemanticException("Array literal cannot contain null elements (for now).");

            if (t != first)
                throw new SemanticException("All elements in array literal must have the same type.");
        }

        // Usa TypeResolver para que el tipo arreglo sea consistente/cached
        var fake = new TypeRefNode(arr.Span, $"{first.Name}[{arr.Items.Count}]");
        return TypeResolver.Resolve(fake);
    }

    private TypeSymbol VisitIndexExpr(IndexExprNode idx)
    {
        var targetType = VisitExpression(idx.Target);
        targetType = UnwrapNullable(targetType);

        if (targetType is not ArrayTypeSymbol arrType)
            throw new SemanticException("Indexing is only valid on arrays.");

        var indexType = VisitExpression(idx.Index);
        if (indexType != BuiltInTypeSymbol.Int)
            throw new SemanticException("Array index must be int.");

        return arrType.ElementType;
    }
}