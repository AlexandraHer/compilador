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

        DeclareAllFunctions(program);  // Fase 1
        VisitProgram(program);         // Fase 2
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

            // ✅ Por si tu AST está metiendo expresiones como statements (exprStmt)
            // (Ej: CallExprNode, BinaryExprNode, IdentifierNode, LiteralNode, etc.)

            default:
                throw new SemanticException($"Unsupported statement '{stmt.GetType().Name}'.");
        }
    }

    private void VisitVarDecl(VarDeclNode varDecl)
    {
        var type = TypeResolver.Resolve(varDecl.Type);

        var symbol = new VariableSymbol(varDecl.Name, type);
        if (!_currentScope.Declare(symbol))
            throw new SemanticException($"Variable '{varDecl.Name}' already declared in this scope.");

        if (varDecl.Initializer != null)
        {
            var initType = VisitExpression(varDecl.Initializer);
            if (initType != type)
                throw new SemanticException($"Type mismatch in initializer of '{varDecl.Name}'. Expected '{type}', got '{initType}'.");
        }
    }

    private void VisitAssign(AssignNode assign)
    {
        if (assign.Target is not IdentifierNode identifier)
            throw new SemanticException("Assignment target must be an identifier (for now).");

        var symbol = _currentScope.Lookup(identifier.Name) as VariableSymbol;
        if (symbol == null)
            throw new SemanticException($"Variable '{identifier.Name}' not declared.");

        var exprType = VisitExpression(assign.Value);
        if (exprType != symbol.Type)
            throw new SemanticException($"Type mismatch in assignment to '{symbol.Name}'. Expected '{symbol.Type}', got '{exprType}'.");
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

            case BinaryExprNode bin:
                {
                    // ✅ NEGATE lo estás modelando como BinaryExpr con Left=0 y Right=expr (como hiciste en el AST)
                    if (bin.Operator == BinaryOperator.Negate)
                    {
                        var rightType = VisitExpression(bin.Right);

                        if (rightType != BuiltInTypeSymbol.Int && rightType != BuiltInTypeSymbol.Float)
                            throw new SemanticException("Unary '-' only valid for int/float.");

                        return rightType;
                    }

                    var leftType = VisitExpression(bin.Left);
                    var rightType2 = VisitExpression(bin.Right);

                    // Para == y != permitimos comparar mismos tipos
                    if (bin.Operator == BinaryOperator.Equal || bin.Operator == BinaryOperator.NotEqual)
                    {
                        if (leftType != rightType2)
                            throw new SemanticException("Type mismatch in comparison (==/!=).");

                        return BuiltInTypeSymbol.Bool;
                    }

                    if (leftType != rightType2)
                        throw new SemanticException("Type mismatch in binary expression.");

                    return ValidateBinaryOperator(bin.Operator, leftType);
                }

            default:
                throw new SemanticException($"Unsupported expression type '{expr.GetType().Name}'.");
        }
    }

    private TypeSymbol VisitCall(CallExprNode call)
    {
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

    private TypeSymbol ValidateBinaryOperator(BinaryOperator op, TypeSymbol type)
    {
        switch (op)
        {
            case BinaryOperator.Add:
                // si luego quieres concatenación: permitir String + String
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
            throw new SemanticException("Null literal not supported yet.");

        return lit.Value switch
        {
            int => BuiltInTypeSymbol.Int,
            double => BuiltInTypeSymbol.Float,
            bool => BuiltInTypeSymbol.Bool,
            string => BuiltInTypeSymbol.String,
            _ => throw new SemanticException($"Unknown literal type '{lit.Value.GetType().Name}'.")
        };
    }

};
