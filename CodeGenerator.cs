using LLVMSharp.Interop;
using MyLangCompiler.Enumerations;
using MyLangCompiler.Nodes;
using System.Xml.Linq;

namespace MyLangCompiler.CodeGen;

public sealed class LlvmValueInfo
{
    public LLVMValueRef Pointer { get; set; }
    public LLVMTypeRef Type { get; set; }
    public bool IsArray { get; set; }
    public LLVMTypeRef ElementType { get; set; }
    public int ArrayLength { get; set; }
}

public class CodeGenerator
{
    private readonly LLVMContextRef _context;
    private readonly LLVMModuleRef _module;
    private readonly LLVMBuilderRef _builder;

    private readonly Dictionary<string, LlvmValueInfo> _variables = new();
    private readonly Dictionary<string, LLVMTypeRef> _variableTypes = new();
    private readonly Dictionary<string, LLVMValueRef> _functions = new();
    private readonly Dictionary<string, LLVMTypeRef> _functionTypes = new();
    private readonly HashSet<string> _classNames = new();

    private LLVMValueRef _printfFunction;
    private LLVMTypeRef _printfType;

    private LLVMValueRef _scanfFunction;
    private LLVMTypeRef _scanfType;

    private int _askBufferCounter = 0;
    public CodeGenerator(string moduleName)
    {
        _context = LLVMContextRef.Create();
        _module = _context.CreateModuleWithName(moduleName);
        _builder = _context.CreateBuilder();

        DeclareRuntimeFunctions();
    }
    private LlvmValueInfo AllocateIntArray(string name, int length)
    {
        var arrayType = LLVMTypeRef.CreateArray(LLVMTypeRef.Int32, (uint)length);
        var alloca = _builder.BuildAlloca(arrayType, name);

        return new LlvmValueInfo
        {
            Pointer = alloca,
            Type = arrayType,
            IsArray = true,
            ElementType = LLVMTypeRef.Int32,
            ArrayLength = length
        };
    }
    private void InitializeIntArray(LlvmValueInfo arrayInfo, List<LLVMValueRef> elementValues)
    {
        for (int i = 0; i < elementValues.Count; i++)
        {
            var zero = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0);
            var index = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)i);

            var elementPtr = _builder.BuildInBoundsGEP2(
                arrayInfo.Type,
                arrayInfo.Pointer,
                new[] { zero, index },
                $"arr.elem.ptr.{i}"
            );

            _builder.BuildStore(elementValues[i], elementPtr);
        }
    }
    private LLVMValueRef GetIntArrayElementPointer(LlvmValueInfo arrayInfo, LLVMValueRef index)
    {
        var zero = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0, false);

        return _builder.BuildInBoundsGEP2(
            arrayInfo.Type,
            arrayInfo.Pointer,
            new[] { zero, index },
            "array.elem.ptr"
        );
    }
    private void DeclareRuntimeFunctions()
    {
        var i8PtrType = LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0);

        // printf(const char*, ...) -> i32
        _printfType = LLVMTypeRef.CreateFunction(
            LLVMTypeRef.Int32,
            new[] { i8PtrType },
            true
        );
        _printfFunction = _module.AddFunction("printf", _printfType);

        // scanf(const char*, ...) -> i32
        _scanfType = LLVMTypeRef.CreateFunction(
            LLVMTypeRef.Int32,
            new[] { i8PtrType },
            true
        );
        _scanfFunction = _module.AddFunction("scanf_s", _scanfType);
    }
    public LLVMModuleRef Generate(ProgramNode program)
    {
        _classNames.Clear();

        foreach (var decl in program.Declarations)
        {
            if (decl is ClassDeclNode cls)
                _classNames.Add(cls.Name);
        }

        foreach (var decl in program.Declarations)
        {
            if (decl is FunctionNode fn)
                DeclareFunction(fn);
        }

        foreach (var decl in program.Declarations)
        {
            if (decl is FunctionNode fn)
                GenerateFunctionBody(fn);
        }

        return _module;
    }

    private void DeclareFunction(FunctionNode fn)
    {
        if (_functions.ContainsKey(fn.Name))
            return;

        var returnType = MapType(fn.ReturnType);

        var paramTypes = new List<LLVMTypeRef>();
        if (fn.Parameters != null)
        {
            foreach (var param in fn.Parameters)
                paramTypes.Add(MapType(param.Type));
        }

        var funcType = LLVMTypeRef.CreateFunction(returnType, paramTypes.ToArray(), false);
        var llvmFunctionName = fn.IsEntry ? "main" : fn.Name;
        var function = _module.AddFunction(llvmFunctionName, funcType);

        _functions[fn.Name] = function;
        _functionTypes[fn.Name] = funcType;
    }

    private void GenerateFunctionBody(FunctionNode fn)
    {
        var function = _functions[fn.Name];

        if (function.FirstBasicBlock.Handle != IntPtr.Zero)
            return;

        _variables.Clear();
        _variableTypes.Clear();

        var entry = function.AppendBasicBlock("entry");
        _builder.PositionAtEnd(entry);

        if (fn.Parameters != null)
        {
            for (int i = 0; i < fn.Parameters.Count; i++)
            {
                var param = fn.Parameters[i];
                var paramValue = function.GetParam((uint)i);
                var paramType = MapType(param.Type);

                var alloca = CreateEntryBlockAlloca(function, param.Name, paramType);
                _builder.BuildStore(paramValue, alloca);

                _variables[param.Name] = new LlvmValueInfo
                {
                    Pointer = alloca,
                    Type = paramType,
                    IsArray = false
                };
                _variableTypes[param.Name] = paramType;
            }
        }

        GenerateBlock(fn.Body);

        if (_builder.InsertBlock.Terminator.Handle == IntPtr.Zero)
        {
            var retType = MapType(fn.ReturnType);

            if (retType.Kind == LLVMTypeKind.LLVMIntegerTypeKind)
                _builder.BuildRet(LLVMValueRef.CreateConstInt(retType, 0, false));
            else
                _builder.BuildRet(LLVMValueRef.CreateConstNull(retType));
        }
    }

    private LLVMValueRef CreateEntryBlockAlloca(LLVMValueRef function, string name, LLVMTypeRef type)
    {
        var tempBuilder = _context.CreateBuilder();
        var entry = function.EntryBasicBlock;

        if (entry.FirstInstruction.Handle != IntPtr.Zero)
            tempBuilder.PositionBefore(entry.FirstInstruction);
        else
            tempBuilder.PositionAtEnd(entry);

        return tempBuilder.BuildAlloca(type, name);
    }

    private LLVMTypeRef MapType(TypeRefNode? typeNode)
    {
        if (typeNode == null)
            return LLVMTypeRef.Int32;

        var name = typeNode.Name;

        return name switch
        {
            "i" => LLVMTypeRef.Int32,
            "b" => LLVMTypeRef.Int1,
            "f" => LLVMTypeRef.Float,
            "s" => LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0),
            _ => LLVMTypeRef.Int32
        };
    }

    private void GenerateBlock(BlockNode block)
    {
        foreach (var stmt in block.Statements)
            GenerateStatement(stmt);
    }

    private void GenerateStatement(StmtNode stmt)
    {
        switch (stmt)
        {
            case ReturnNode ret:
                GenerateReturn(ret);
                break;

            case IfNode ifNode:
                GenerateIf(ifNode);
                break;

            case WhileNode whileNode:
                GenerateWhile(whileNode);
                break;

            case ForNode forNode:
                GenerateFor(forNode);
                break;

            case VarDeclNode varDecl:
                GenerateVarDecl(varDecl);
                break;

            case AssignNode assign:
                GenerateAssign(assign);
                break;

            case ExprStmtNode exprStmt:
                GenerateExpression(exprStmt.Expression);
                break;

            default:
                throw new NotImplementedException($"Stmt no soportado: {stmt.GetType().Name}");
        }
    }

    private void GenerateReturn(ReturnNode node)
    {
        var value = GenerateExpression(node.Value);
        _builder.BuildRet(value);
    }

    private void GenerateVarDecl(VarDeclNode node)
    {
        if (node.Initializer is ArrayLiteralNode arrayLiteral)
        {
            var arrayInfo = AllocateIntArray(node.Name, arrayLiteral.Items.Count);

            var elementValues = new List<LLVMValueRef>();
            foreach (var element in arrayLiteral.Items)
            {
                var value = GenerateExpression(element);
                elementValues.Add(value);
            }

            InitializeIntArray(arrayInfo, elementValues);

            _variables[node.Name] = arrayInfo;
            _variableTypes[node.Name] = arrayInfo.Type;

            return;
        }

        var function = _builder.InsertBlock.Parent;
        var llvmType = MapType(node.Type);
        var alloca = CreateEntryBlockAlloca(function, node.Name, llvmType);

        _variables[node.Name] = new LlvmValueInfo
        {
            Pointer = alloca,
            Type = llvmType,
            IsArray = false
        };

        _variableTypes[node.Name] = llvmType;

        if (node.Initializer != null)
        {
            var initValue = GenerateExpression(node.Initializer);
            _builder.BuildStore(initValue, alloca);
        }
        else
        {
            LLVMValueRef defaultValue =
                llvmType.Kind == LLVMTypeKind.LLVMIntegerTypeKind ? LLVMValueRef.CreateConstInt(llvmType, 0) :
                llvmType.Kind == LLVMTypeKind.LLVMFloatTypeKind ? LLVMValueRef.CreateConstReal(llvmType, 0.0) :
                LLVMValueRef.CreateConstNull(llvmType);

            _builder.BuildStore(defaultValue, alloca);
        }
    }

    private void GenerateAssign(AssignNode node)
    {
        if (node.Target is IdentifierNode id)
        {
            if (!_variables.TryGetValue(id.Name, out var variableInfo))
                throw new Exception($"Variable no definida: {id.Name}");

            var value = GenerateExpression(node.Value);
            _builder.BuildStore(value, variableInfo.Pointer);
            return;
        }

        if (node.Target is IndexExprNode indexExpr)
        {
            if (indexExpr.Target is not IdentifierNode arrayId)
                throw new NotSupportedException("Solo se admite asignación a arrays por nombre, por ejemplo: arr[i] = valor");

            if (!_variables.TryGetValue(arrayId.Name, out var arrayInfo))
                throw new Exception($"Variable no definida: {arrayId.Name}");

            if (!arrayInfo.IsArray)
                throw new Exception($"La variable '{arrayId.Name}' no es un array.");

            var indexValue = GenerateExpression(indexExpr.Index);
            var elementPtr = GetIntArrayElementPointer(arrayInfo, indexValue);
            var value = GenerateExpression(node.Value);

            _builder.BuildStore(value, elementPtr);
            return;
        }

        throw new NotImplementedException("Solo se soporta asignación a identificadores o elementos de array.");
    }

    private LLVMValueRef GenerateExpression(ExprNode expr)
    {
        switch (expr)
        {
            case BinaryExprNode bin:
                return GenerateBinary(bin);

            case UnaryExprNode unary:
                return GenerateUnary(unary);

            case LiteralNode lit:
                return GenerateLiteral(lit);

            case IdentifierNode id:
                return LoadVariable(id.Name);

            case CallExprNode call:
                return GenerateCall(call);

            case MethodCallExprNode methodCall:
                return GenerateMethodCall(methodCall);

            case ArrayLiteralNode arrayLiteral:
                return GenerateArrayLiteral(arrayLiteral);
           
            case IndexExprNode indexExpr:
                {
                    // Solo permite algo como arr[0]
                    if (indexExpr.Target is not IdentifierNode id)
                        throw new NotSupportedException("Solo se admite acceso a arrays por nombre, por ejemplo: arr[0]");

                    // Busca el array en la tabla de variables
                    if (!_variables.TryGetValue(id.Name, out var arrayInfo))
                        throw new Exception($"Variable no definida: {id.Name}");

                    // Verifica que sí sea un array
                    if (!arrayInfo.IsArray)
                        throw new Exception($"La variable '{id.Name}' no es un array.");

                    // Genera el índice
                    var indexValue = GenerateExpression(indexExpr.Index);

                    // Obtiene el puntero al elemento arr[i]
                    var elementPtr = GetIntArrayElementPointer(arrayInfo, indexValue);

                    // Carga y retorna el valor del elemento
                    return _builder.BuildLoad2(arrayInfo.ElementType, elementPtr, "array.elem.load");
                }

            default:
                throw new NotImplementedException($"Expr no soportada: {expr.GetType().Name}");
        }
        
    }

    private LLVMValueRef GenerateBinary(BinaryExprNode node)
    {
        var left = GenerateExpression(node.Left);
        var right = GenerateExpression(node.Right);

        return node.Operator switch
        {
            BinaryOperator.Add => _builder.BuildAdd(left, right, "addtmp"),
            BinaryOperator.Subtract => _builder.BuildSub(left, right, "subtmp"),
            BinaryOperator.Multiply => _builder.BuildMul(left, right, "multmp"),
            BinaryOperator.Divide => _builder.BuildSDiv(left, right, "divtmp"),
            BinaryOperator.Modulo => _builder.BuildSRem(left, right, "modtmp"),

            BinaryOperator.And => _builder.BuildAnd(left, right, "andtmp"),
            BinaryOperator.Or => _builder.BuildOr(left, right, "ortmp"),

            BinaryOperator.Equal => _builder.BuildICmp(LLVMIntPredicate.LLVMIntEQ, left, right, "eqtmp"),
            BinaryOperator.NotEqual => _builder.BuildICmp(LLVMIntPredicate.LLVMIntNE, left, right, "netmp"),
            BinaryOperator.Less => _builder.BuildICmp(LLVMIntPredicate.LLVMIntSLT, left, right, "lttmp"),
            BinaryOperator.LessOrEqual => _builder.BuildICmp(LLVMIntPredicate.LLVMIntSLE, left, right, "letmp"),
            BinaryOperator.Greater => _builder.BuildICmp(LLVMIntPredicate.LLVMIntSGT, left, right, "gttmp"),
            BinaryOperator.GreaterOrEqual => _builder.BuildICmp(LLVMIntPredicate.LLVMIntSGE, left, right, "getmp"),

            _ => throw new NotImplementedException($"Operador no soportado: {node.Operator}")
        };
    }

    private LLVMValueRef GenerateUnary(UnaryExprNode node)
    {
        var operand = GenerateExpression(node.Operand);

        return node.Operator switch
        {
            UnaryOperatorKind.Negate => _builder.BuildNeg(operand, "negtmp"),
            UnaryOperatorKind.Not => _builder.BuildNot(operand, "nottmp"),
            _ => throw new NotImplementedException($"Operador unario no soportado: {node.Operator}")
        };
    }

    private LLVMValueRef GenerateLiteral(LiteralNode lit)
    {
        return lit.Value switch
        {
            int i => LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)i, true),
            bool b => LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, b ? 1ul : 0ul, false),
            float f => LLVMValueRef.CreateConstReal(LLVMTypeRef.Float, f),
            double d => LLVMValueRef.CreateConstReal(LLVMTypeRef.Double, d),
            string s => _builder.BuildGlobalStringPtr(s, "strtmp"),
            _ => throw new NotImplementedException($"Literal no soportado: {lit.Value?.GetType().Name ?? "null"}")
        };
    }

    private LLVMValueRef LoadVariable(string name)
    {
        if (!_variables.TryGetValue(name, out var alloca))
            throw new Exception($"Variable no definida: {name}");

        if (!_variableTypes.TryGetValue(name, out var llvmType))
            throw new Exception($"Tipo no registrado para la variable: {name}");

        return _builder.BuildLoad2(llvmType, alloca.Pointer, name);
    }

    private LLVMValueRef GenerateCall(CallExprNode call)
    {
        // ===================== show() =====================
        if (call.FunctionName == "show")
        {
            if (call.Arguments.Count != 1)
                throw new Exception("show expects 1 argument.");

            var value = GenerateExpression(call.Arguments[0]);
            var valueType = value.TypeOf;

            // CASO: bool (i1) → select entre "true"/"false" + %s
            if (valueType.Kind == LLVMTypeKind.LLVMIntegerTypeKind && valueType.IntWidth == 1)
            {
                var trueStr = _builder.BuildGlobalStringPtr("true", "bool.true");
                var falseStr = _builder.BuildGlobalStringPtr("false", "bool.false");
                var selected = _builder.BuildSelect(value, trueStr, falseStr, "bool.sel");

                var format = _builder.BuildGlobalStringPtr("%s\n", "fmtbool");
                _builder.BuildCall2(_printfType, _printfFunction, new[] { format, selected }, "");

                return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0, false);
            }

            // CASO: int (i32) → %d
            if (valueType.Kind == LLVMTypeKind.LLVMIntegerTypeKind)
            {
                var format = _builder.BuildGlobalStringPtr("%d\n", "fmtint");
                _builder.BuildCall2(_printfType, _printfFunction, new[] { format, value }, "");

                return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0, false);
            }

            // CASO: float → promover a double + %f
            if (valueType.Kind == LLVMTypeKind.LLVMFloatTypeKind)
            {
                var promoted = _builder.BuildFPExt(value, LLVMTypeRef.Double, "fext");
                var format = _builder.BuildGlobalStringPtr("%f\n", "fmtfloat");
                _builder.BuildCall2(_printfType, _printfFunction, new[] { format, promoted }, "");

                return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0, false);
            }

            // CASO: double → %f directamente
            if (valueType.Kind == LLVMTypeKind.LLVMDoubleTypeKind)
            {
                var format = _builder.BuildGlobalStringPtr("%f\n", "fmtdouble");
                _builder.BuildCall2(_printfType, _printfFunction, new[] { format, value }, "");

                return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0, false);
            }

            // CASO: string (pointer / i8*) → %s
            if (valueType.Kind == LLVMTypeKind.LLVMPointerTypeKind)
            {
                var format = _builder.BuildGlobalStringPtr("%s\n", "fmtstr");
                _builder.BuildCall2(_printfType, _printfFunction, new[] { format, value }, "");

                return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0, false);
            }

            // Fallback: intentar como entero
            var fallbackFmt = _builder.BuildGlobalStringPtr("%d\n", "fmtfallback");
            _builder.BuildCall2(_printfType, _printfFunction, new[] { fallbackFmt, value }, "");
            return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0, false);
        }

     // ===================== ask() =====================
if (call.FunctionName == "ask")
{
    if (call.Arguments.Count != 1)
        throw new Exception("ask expects 1 argument.");

    if (call.Arguments[0] is not IdentifierNode askId)
        throw new Exception("ask expects an identifier.");

    if (!_variables.TryGetValue(askId.Name, out var varInfo))
        throw new Exception($"Variable no definida: {askId.Name}");

    var varType = varInfo.Type;

    // CASO: int (i32) → scanf("%d", &var)
    if (varType.Kind == LLVMTypeKind.LLVMIntegerTypeKind && varType.IntWidth == 32)
    {
        var scanfFmt = _builder.BuildGlobalStringPtr("%d", $"askfmt.{_askBufferCounter++}");
        _builder.BuildCall2(_scanfType, _scanfFunction, new[] { scanfFmt, varInfo.Pointer }, "");
        return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0, false);
    }

    // CASO: float → scanf("%f", &var)
    if (varType.Kind == LLVMTypeKind.LLVMFloatTypeKind)
    {
        var scanfFmt = _builder.BuildGlobalStringPtr("%f", $"askfmt.{_askBufferCounter++}");
        _builder.BuildCall2(_scanfType, _scanfFunction, new[] { scanfFmt, varInfo.Pointer }, "");
        return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0, false);
    }

    // CASO: string (i8*) → buffer de 256 bytes + scanf("%255s")
    var bufferType = LLVMTypeRef.CreateArray(LLVMTypeRef.Int8, 256);
    var buffer = _builder.BuildAlloca(bufferType, $"ask.buf.{_askBufferCounter++}");

    var zero = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0, false);
    var bufferPtr = _builder.BuildInBoundsGEP2(
        bufferType,
        buffer,
        new[] { zero, zero },
        "ask.buf.ptr"
    );

    var fmt = _builder.BuildGlobalStringPtr("%255s", $"askfmt.{_askBufferCounter++}");
    _builder.BuildCall2(_scanfType, _scanfFunction, new[] { fmt, bufferPtr }, "");
    _builder.BuildStore(bufferPtr, varInfo.Pointer);

    return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0, false);
}

        if (_classNames.Contains(call.FunctionName))
        {
            return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0, false);
        }

        if (!_functions.TryGetValue(call.FunctionName, out var function))
            throw new Exception($"Función no definida: {call.FunctionName}");

        if (!_functionTypes.TryGetValue(call.FunctionName, out var funcType))
            throw new Exception($"Tipo de función no registrado: {call.FunctionName}");

        var args = call.Arguments.Select(GenerateExpression).ToArray();
        return _builder.BuildCall2(funcType, function, args, "calltmp");
    }

    private LLVMValueRef GenerateMethodCall(MethodCallExprNode node)
    {
        if (!_functions.TryGetValue(node.MethodName, out var function))
            throw new Exception($"Método/función no definido: {node.MethodName}");

        if (!_functionTypes.TryGetValue(node.MethodName, out var funcType))
            throw new Exception($"Tipo de función no registrado: {node.MethodName}");

        var args = node.Arguments.Select(GenerateExpression).ToArray();
        return _builder.BuildCall2(funcType, function, args, "methodcalltmp");
    }

    private void GenerateIf(IfNode node)
    {
        var condition = GenerateExpression(node.Condition);
        var function = _builder.InsertBlock.Parent;

        var thenBlock = function.AppendBasicBlock("then");
        var elseBlock = function.AppendBasicBlock("else");
        var mergeBlock = function.AppendBasicBlock("ifend");

        _builder.BuildCondBr(condition, thenBlock, elseBlock);

        _builder.PositionAtEnd(thenBlock);
        GenerateBlock(node.ThenBlock);
        if (_builder.InsertBlock.Terminator.Handle == IntPtr.Zero)
            _builder.BuildBr(mergeBlock);

        _builder.PositionAtEnd(elseBlock);
        if (node.ElseBlock != null)
            GenerateBlock(node.ElseBlock);
        if (_builder.InsertBlock.Terminator.Handle == IntPtr.Zero)
            _builder.BuildBr(mergeBlock);

        _builder.PositionAtEnd(mergeBlock);
    }

    private void GenerateWhile(WhileNode node)
    {
        var function = _builder.InsertBlock.Parent;

        var condBlock = function.AppendBasicBlock("while.cond");
        var bodyBlock = function.AppendBasicBlock("while.body");
        var endBlock = function.AppendBasicBlock("while.end");

        _builder.BuildBr(condBlock);

        _builder.PositionAtEnd(condBlock);
        var condition = GenerateExpression(node.Condition);
        _builder.BuildCondBr(condition, bodyBlock, endBlock);

        _builder.PositionAtEnd(bodyBlock);
        GenerateBlock(node.Body);
        if (_builder.InsertBlock.Terminator.Handle == IntPtr.Zero)
            _builder.BuildBr(condBlock);

        _builder.PositionAtEnd(endBlock);
    }

    private void GenerateFor(ForNode node)
    {
        var function = _builder.InsertBlock.Parent;

        if (node.Init != null)
            GenerateStatement(node.Init);

        var condBlock = function.AppendBasicBlock("for.cond");
        var bodyBlock = function.AppendBasicBlock("for.body");
        var stepBlock = function.AppendBasicBlock("for.step");
        var endBlock = function.AppendBasicBlock("for.end");

        _builder.BuildBr(condBlock);

        _builder.PositionAtEnd(condBlock);
        var condition = node.Condition != null
            ? GenerateExpression(node.Condition)
            : LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, 1, false);

        _builder.BuildCondBr(condition, bodyBlock, endBlock);

        _builder.PositionAtEnd(bodyBlock);
        GenerateBlock(node.Body);
        if (_builder.InsertBlock.Terminator.Handle == IntPtr.Zero)
            _builder.BuildBr(stepBlock);

        _builder.PositionAtEnd(stepBlock);
        if (node.Action != null)
            GenerateStatement(node.Action);
        if (_builder.InsertBlock.Terminator.Handle == IntPtr.Zero)
            _builder.BuildBr(condBlock);

        _builder.PositionAtEnd(endBlock);
    }
    private LLVMValueRef GenerateArrayLiteral(ArrayLiteralNode node)
    {
        // Implementación temporal:
        // tu programa declara el arreglo pero no lo usa después,
        // así que por ahora devolvemos un valor dummy para que LLVM continúe.
        return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0, false);
    }
}