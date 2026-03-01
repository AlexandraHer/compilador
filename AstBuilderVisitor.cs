using System;
using System.Collections.Generic;
using System.Globalization;
using Antlr4.Runtime;
using MyLangCompiler.Nodes;
using MyLangCompiler.Enumerations;

namespace MyLangCompiler;

public sealed class AstBuilderVisitor : MyLangParserBaseVisitor<AstNode>
{
    private static SourceSpan Span(ParserRuleContext ctx)
        => new SourceSpan("input", ctx.Start.Line, ctx.Start.Column);

    private static BinaryOperator ToBinaryOp(IToken token) => token.Type switch
    {
        MyLangLexer.PLUS => BinaryOperator.Add,
        MyLangLexer.MINUS => BinaryOperator.Subtract,
        MyLangLexer.STAR => BinaryOperator.Multiply,
        MyLangLexer.SLASH => BinaryOperator.Divide,
        MyLangLexer.PERCENT => BinaryOperator.Modulo,

        MyLangLexer.AND => BinaryOperator.And,
        MyLangLexer.OR => BinaryOperator.Or,

        MyLangLexer.EQ => BinaryOperator.Equal,
        MyLangLexer.NEQ => BinaryOperator.NotEqual,
        MyLangLexer.LT => BinaryOperator.Less,
        MyLangLexer.LTE => BinaryOperator.LessOrEqual,
        MyLangLexer.GT => BinaryOperator.Greater,
        MyLangLexer.GTE => BinaryOperator.GreaterOrEqual,

        _ => throw new Exception($"Unsupported operator token: {token.Text} (type={token.Type})")
    };

    // ================= START =================

    public override AstNode VisitStart(MyLangParser.StartContext context)
        => Visit(context.program());

    // ================= PROGRAM =================

    public override AstNode VisitProgram(MyLangParser.ProgramContext context)
    {
        var program = new ProgramNode(Span(context));

        foreach (var decl in context.topLevelDecl())
        {
            var visited = Visit(decl);

            if (visited is DeclListNode bag)
            {
                foreach (var d in bag.Decls)
                {
                    Console.WriteLine($"DECL ADDED: {d.GetType().Name}");
                    program.Declarations.Add(d);
                }
                continue;
            }

            if (visited is DeclNode node)
            {
                Console.WriteLine($"DECL ADDED: {node.GetType().Name}");
                program.Declarations.Add(node);
            }
        }

        return program;
    }

    public override AstNode VisitTopLevelDecl(MyLangParser.TopLevelDeclContext context)
    {
        if (context.useDecl() != null) return Visit(context.useDecl());
        if (context.functionDecl() != null) return Visit(context.functionDecl());
        if (context.classDecl() != null) return Visit(context.classDecl()); // ✅
        return base.VisitTopLevelDecl(context);
    }

    // ================= USE =================

    public override AstNode VisitUseDecl(MyLangParser.UseDeclContext context)
        => new UseDeclNode(Span(context), context.ID().GetText());

    // ================= CLASS / OBJECT (APLANAR MÉTODOS) =================

    public override AstNode VisitClassDecl(MyLangParser.ClassDeclContext context)
{
    var bag = new DeclListNode(Span(context));

    var className = context.ID().GetText();
    bag.Decls.Add(new ClassDeclNode(Span(context), className));

    foreach (var member in context.classMember())
    {
        if (member.methodDecl() == null) continue;

        var fn = BuildFunctionFromMethodDecl(member.methodDecl());
        bag.Decls.Add(fn);
    }

    return bag;
}
    private FunctionNode BuildFunctionFromMethodDecl(MyLangParser.MethodDeclContext m)
    {
        var span = Span(m);
        var name = m.ID().GetText();
        var isEntry = m.ENTRY() != null;

        var returnType = (TypeRefNode)Visit(m.typeRef());
        var body = (BlockNode)Visit(m.block());

        var function = new FunctionNode(span, name, isEntry, returnType, body);

        if (m.paramList() != null)
        {
            foreach (var p in m.paramList().param())
            {
                var paramName = p.ID().GetText();
                var typeCtx = p.typeRef();
                var typeNode = (TypeRefNode)Visit(p.typeRef());

                function.Parameters.Add(new ParameterNode(Span(p), paramName, typeNode));
            }
        }

        return function;
    }

    // ================= FUNCTION =================

    public override AstNode VisitFunctionDecl(MyLangParser.FunctionDeclContext context)
    {
        var span = Span(context);
        var name = context.ID().GetText();
        var isEntry = context.ENTRY() != null;

        var returnType = (TypeRefNode)Visit(context.typeRef());
        var body = (BlockNode)Visit(context.block());

        var function = new FunctionNode(span, name, isEntry, returnType, body);

        if (context.paramList() != null)
        {
            foreach (var p in context.paramList().param())
            {
                var paramName = p.ID().GetText();
                var typeCtx = p.typeRef();

                var typeNode = new TypeRefNode(Span(typeCtx), typeCtx.GetText());
                function.Parameters.Add(new ParameterNode(Span(p), paramName, typeNode));
            }
        }

        return function;
    }

    // ================= BLOCK =================

    public override AstNode VisitBlock(MyLangParser.BlockContext context)
    {
        var block = new BlockNode(Span(context));

        foreach (var stmt in context.statement())
        {
            var node = Visit(stmt) as StmtNode;
            if (node != null) block.Statements.Add(node);
        }

        return block;
    }

    // ================= STATEMENTS =================

    public override AstNode VisitStatement(MyLangParser.StatementContext context)
    {
        if (context.varDecl() != null) return Visit(context.varDecl());
        if (context.assignStmt() != null) return Visit(context.assignStmt());
        if (context.ifStmt() != null) return Visit(context.ifStmt());           // ✅ NUEVO
        if (context.loopStmt() != null) return Visit(context.loopStmt());       // ✅ NUEVO
        if (context.repeatStmt() != null) return Visit(context.repeatStmt());   // ✅ NUEVO
        if (context.returnStmt() != null) return Visit(context.returnStmt());
        if (context.exprStmt() != null) return Visit(context.exprStmt());

        return base.VisitStatement(context);
    }

    public override AstNode VisitVarDecl(MyLangParser.VarDeclContext context)
    {
        var span = Span(context);
        var name = context.ID().GetText();
        var type = (TypeRefNode)Visit(context.typeRef());

        ExprNode? initializer = null;
        if (context.initializer() != null)
            initializer = (ExprNode)Visit(context.initializer());

        return new VarDeclNode(span, name, type, initializer);
    }

    public override AstNode VisitAssignStmt(MyLangParser.AssignStmtContext context)
    {
        var span = Span(context);
        var target = BuildLValue(context.lvalue());
        var value = (ExprNode)Visit(context.expression());
        return new AssignNode(span, target, value);
    }

    public override AstNode VisitReturnStmt(MyLangParser.ReturnStmtContext context)
        => new ReturnNode(Span(context), (ExprNode)Visit(context.expression()));

    public override AstNode VisitExprStmt(MyLangParser.ExprStmtContext context)
    {
        var expr = (ExprNode)Visit(context.expression());
        return new ExprStmtNode(Span(context), expr);
    }

    // ================= IF / LOOP / WHILE =================

    public override AstNode VisitIfStmt(MyLangParser.IfStmtContext context)
    {
        var cond = (ExprNode)Visit(context.condition());
        var thenBlock = (BlockNode)Visit(context.block(0));
        BlockNode? elseBlock = null;

        if (context.OTHERWISE() != null)
            elseBlock = (BlockNode)Visit(context.block(1));

        return new IfNode(Span(context), cond, thenBlock, elseBlock);
    }

    public override AstNode VisitRepeatStmt(MyLangParser.RepeatStmtContext context)
    {
        var cond = (ExprNode)Visit(context.condition());
        var body = (BlockNode)Visit(context.block());
        return new WhileNode(Span(context), cond, body);
    }

    public override AstNode VisitLoopStmt(MyLangParser.LoopStmtContext context)
    {
        StmtNode? init = null;
        if (context.loopInit() != null)
            init = (StmtNode)Visit(context.loopInit());

        var cond = (ExprNode)Visit(context.condition());

        StmtNode? action = null;
        if (context.loopAction() != null)
            action = (StmtNode)Visit(context.loopAction());

        var body = (BlockNode)Visit(context.block());

        return new ForNode(Span(context), init, cond, action, body);
    }

    public override AstNode VisitLoopInit(MyLangParser.LoopInitContext context)
    {
        if (context.varDeclNoSemi() != null) return Visit(context.varDeclNoSemi());
        return Visit(context.assignNoSemi());
    }

    public override AstNode VisitLoopAction(MyLangParser.LoopActionContext context)
    {
        if (context.assignNoSemi() != null) return Visit(context.assignNoSemi());

        var expr = (ExprNode)Visit(context.expression());
        return new ExprStmtNode(Span(context), expr);
    }

    public override AstNode VisitVarDeclNoSemi(MyLangParser.VarDeclNoSemiContext context)
    {
        var span = Span(context);
        var name = context.ID().GetText();
        var type = (TypeRefNode)Visit(context.typeRef());

        ExprNode? initializer = null;
        if (context.initializer() != null)
            initializer = (ExprNode)Visit(context.initializer());

        return new VarDeclNode(span, name, type, initializer);
    }

    public override AstNode VisitAssignNoSemi(MyLangParser.AssignNoSemiContext context)
    {
        var span = Span(context);
        var target = BuildLValue(context.lvalue());
        var value = (ExprNode)Visit(context.expression());
        return new AssignNode(span, target, value);
    }

    // ================= EXPRESSIONS =================

    public override AstNode VisitExpression(MyLangParser.ExpressionContext context)
        => Visit(context.orExpr());

    public override AstNode VisitOrExpr(MyLangParser.OrExprContext context)
    {
        var left = (ExprNode)Visit(context.andExpr(0));
        for (int i = 1; i < context.andExpr().Length; i++)
        {
            var right = (ExprNode)Visit(context.andExpr(i));
            left = new BinaryExprNode(Span(context), BinaryOperator.Or, left, right);
        }
        return left;
    }

    public override AstNode VisitAndExpr(MyLangParser.AndExprContext context)
    {
        var left = (ExprNode)Visit(context.notExpr(0));
        for (int i = 1; i < context.notExpr().Length; i++)
        {
            var right = (ExprNode)Visit(context.notExpr(i));
            left = new BinaryExprNode(Span(context), BinaryOperator.And, left, right);
        }
        return left;
    }

    public override AstNode VisitNotExpr(MyLangParser.NotExprContext context)
    {
        var expr = (ExprNode)Visit(context.relExpr());
        if (context.NOT() != null)
            return new UnaryExprNode(Span(context), UnaryOperatorKind.Not, expr);

        return expr;
    }

    public override AstNode VisitRelExpr(MyLangParser.RelExprContext context)
    {
        var left = (ExprNode)Visit(context.addExpr(0));
        if (context.relOp() == null) return left;

        var opText = context.relOp().GetText();
        var right = (ExprNode)Visit(context.addExpr(1));

        var op = opText switch
        {
            "==" => BinaryOperator.Equal,
            "!=" => BinaryOperator.NotEqual,
            "<" => BinaryOperator.Less,
            "<=" => BinaryOperator.LessOrEqual,
            ">" => BinaryOperator.Greater,
            ">=" => BinaryOperator.GreaterOrEqual,
            _ => throw new Exception($"Relational operator '{opText}' not supported.")
        };

        return new BinaryExprNode(Span(context), op, left, right);
    }

    public override AstNode VisitAddExpr(MyLangParser.AddExprContext context)
    {
        var left = (ExprNode)Visit(context.mulExpr(0));
        for (int i = 1; i < context.mulExpr().Length; i++)
        {
            var right = (ExprNode)Visit(context.mulExpr(i));
            var opText = context.GetChild(2 * i - 1).GetText();

            var op = opText switch
            {
                "+" => BinaryOperator.Add,
                "-" => BinaryOperator.Subtract,
                _ => throw new Exception($"Unsupported operator '{opText}' in addExpr.")
            };

            left = new BinaryExprNode(Span(context), op, left, right);
        }
        return left;
    }

    public override AstNode VisitMulExpr(MyLangParser.MulExprContext context)
    {
        var left = (ExprNode)Visit(context.unaryExpr(0));
        for (int i = 1; i < context.unaryExpr().Length; i++)
        {
            var right = (ExprNode)Visit(context.unaryExpr(i));
            var opText = context.GetChild(2 * i - 1).GetText();

            var op = opText switch
            {
                "*" => BinaryOperator.Multiply,
                "/" => BinaryOperator.Divide,
                "%" => BinaryOperator.Modulo,
                _ => throw new Exception($"Unsupported operator '{opText}' in mulExpr.")
            };

            left = new BinaryExprNode(Span(context), op, left, right);
        }
        return left;
    }

    public override AstNode VisitUnaryExpr(MyLangParser.UnaryExprContext context)
    {
        if (context.MINUS() != null)
        {
            var exprNode = (ExprNode)Visit(context.postfixExpr());
            return new BinaryExprNode(Span(context), BinaryOperator.Negate, new LiteralNode(Span(context), 0), exprNode);
        }
        return Visit(context.postfixExpr());
    }

    // ================= POSTFIX =================

    public override AstNode VisitPostfixExpr(MyLangParser.PostfixExprContext context)
    {
        ExprNode current = (ExprNode)Visit(context.primaryPostfix());

        foreach (var suffix in context.postfixSuffix())
        {
            if (suffix.DOT() != null)
            {
                var methodName = suffix.ID().GetText();
                var args = ReadArgs(suffix.argList());
                current = new MethodCallExprNode(Span(suffix), current, methodName, args);
                continue;
            }

            if (suffix.LPAREN() != null)
            {
                var args = ReadArgs(suffix.argList());

                if (current is IdentifierNode id)
                    current = new CallExprNode(Span(suffix), id.Name, args);
                else
                    throw new Exception($"Call '(... )' only supported on identifiers for now. Got: {current.GetType().Name}");

                continue;
            }

            throw new Exception($"Unknown postfix suffix: {suffix.GetText()}");
        }

        return current;
    }

    public override AstNode VisitPrimaryPostfix(MyLangParser.PrimaryPostfixContext context)
    {
        if (context.ID() != null)
            return new IdentifierNode(Span(context), context.ID().GetText());

        if (context.literal() != null)
            return Visit(context.literal());

        if (context.lenExpr() != null) return Visit(context.lenExpr());
        if (context.askExpr() != null) return Visit(context.askExpr());
        if (context.convertExpr() != null) return Visit(context.convertExpr());
        if (context.showExpr() != null) return Visit(context.showExpr());
        if (context.readFileExpr() != null) return Visit(context.readFileExpr());
        if (context.writeFileExpr() != null) return Visit(context.writeFileExpr());
        if (context.arrayLiteral() != null) return Visit(context.arrayLiteral());
        if (context.arrayAccess() != null) return Visit(context.arrayAccess());
        if (context.arrayLiteral() != null) return Visit(context.arrayLiteral());

        if (context.expression() != null)
            return Visit(context.expression());

        throw new Exception($"Invalid primaryPostfix at {context.Start.Line}:{context.Start.Column}. Text='{context.GetText()}'");
    }

    private List<ExprNode> ReadArgs(MyLangParser.ArgListContext? argList)
    {
        var args = new List<ExprNode>();
        if (argList == null) return args;

        foreach (var expr in argList.expression())
            args.Add((ExprNode)Visit(expr));

        return args;
    }

    // ================= LITERALS =================

    public override AstNode VisitLiteral(MyLangParser.LiteralContext context)
    {
        var span = Span(context);

        if (context.INT() != null)
            return new LiteralNode(span, int.Parse(context.INT().GetText(), CultureInfo.InvariantCulture));

        if (context.FLOAT() != null)
            return new LiteralNode(span, double.Parse(context.FLOAT().GetText(), CultureInfo.InvariantCulture));

        if (context.STRING() != null)
        {
            var raw = context.STRING().GetText();
            var unquoted = raw.Substring(1, raw.Length - 2);
            return new LiteralNode(span, unquoted);
        }

        if (context.TRUE() != null) return new LiteralNode(span, true);
        if (context.FALSE() != null) return new LiteralNode(span, false);

        return new LiteralNode(span, null);
    }

    // ================= TYPE =================

    public override AstNode VisitTypeRef(MyLangParser.TypeRefContext context)
        => new TypeRefNode(Span(context), context.GetText());

    // ================= BUILT-IN EXPRESSIONS =================

    public override AstNode VisitShowExpr(MyLangParser.ShowExprContext context)
    {
        // SHOW '(' expression ')'
        var arg = (ExprNode)Visit(context.expression());
        return new CallExprNode(Span(context), "show", new List<ExprNode> { arg });
    }

    public override AstNode VisitAskExpr(MyLangParser.AskExprContext context)
    {
        // ASK '(' ID ')'
        var id = new IdentifierNode(Span(context), context.ID().GetText());
        return new CallExprNode(Span(context), "ask", new List<ExprNode> { id });
    }

    public override AstNode VisitLenExpr(MyLangParser.LenExprContext context)
    {
        // LEN '(' ID ')'
        var id = new IdentifierNode(Span(context), context.ID().GetText());
        return new CallExprNode(Span(context), "len", new List<ExprNode> { id });
    }

    public override AstNode VisitConvertExpr(MyLangParser.ConvertExprContext context)
    {
        // (convertToInt|convertToFloat|convertToBoolean) '(' expression ')'
        var arg = (ExprNode)Visit(context.expression());

        string fnName =
            context.CONV_INT() != null ? "convertToInt" :
            context.CONV_FLOAT() != null ? "convertToFloat" :
            "convertToBoolean";

        return new CallExprNode(Span(context), fnName, new List<ExprNode> { arg });
    }

    public override AstNode VisitReadFileExpr(MyLangParser.ReadFileExprContext context)
    {
        var arg = (ExprNode)Visit(context.expression());
        return new CallExprNode(Span(context), "readfile", new List<ExprNode> { arg });
    }

    public override AstNode VisitWriteFileExpr(MyLangParser.WriteFileExprContext context)
    {
        var a = (ExprNode)Visit(context.expression(0));
        var b = (ExprNode)Visit(context.expression(1));
        return new CallExprNode(Span(context), "writefile", new List<ExprNode> { a, b });
    }

    public override AstNode VisitArrayAccess(MyLangParser.ArrayAccessContext context)
    {
        var target = new IdentifierNode(Span(context), context.ID().GetText());
        var index = (ExprNode)Visit(context.expression());

        // TODO: este nodo debe existir (IndexExprNode o ArrayAccessExprNode)
        return new IndexExprNode(Span(context), target, index);
    }

    public override AstNode VisitArrayLiteral(MyLangParser.ArrayLiteralContext context)
    {
        var items = new List<ExprNode>();

        foreach (var e in context.expression())
            items.Add((ExprNode)Visit(e));

        return new ArrayLiteralNode(Span(context), items);
    }

    private ExprNode BuildLValue(MyLangParser.LvalueContext ctx)
    {
        ExprNode current = new IdentifierNode(Span(ctx), ctx.ID().GetText());

        if (ctx.LBRACK() != null)
        {
            var index = (ExprNode)Visit(ctx.expression());
            current = new IndexExprNode(Span(ctx), current, index);
        }

        return current;
    }
}