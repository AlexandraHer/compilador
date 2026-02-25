using System;
using System.Collections.Generic;
using System.Globalization;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using MyLangCompiler.Nodes;
using MyLangCompiler.Enumerations;

namespace MyLangCompiler;

public sealed class AstBuilderVisitor : MyLangParserBaseVisitor<AstNode>
{
    private static SourceSpan Span(ParserRuleContext ctx)
        => new SourceSpan("input", ctx.Start.Line, ctx.Start.Column);

    // ✅ token.Type viene del LEXER (MyLangLexer.*), no del parser.
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
            var node = Visit(decl) as DeclNode;
            if (node != null)
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
        // classDecl por ahora no lo construimos
        return base.VisitTopLevelDecl(context);
    }

    // ================= USE =================

    public override AstNode VisitUseDecl(MyLangParser.UseDeclContext context)
    {
        return new UseDeclNode(
            Span(context),
            context.ID().GetText()
        );
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
                if (p == null)
                    continue;

                var idToken = p.ID();
                var typeCtx = p.typeRef();

                if (idToken == null || typeCtx == null)
                    continue; // ignoramos parámetros mal construidos

                var paramName = idToken.GetText();

                var typeNode = new TypeRefNode(
                    Span(typeCtx),
                    typeCtx.GetText()
                );

                var param = new ParameterNode(
                    Span(p),
                    paramName,
                    typeNode
                );

                function.Parameters.Add(param);
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
            if (node != null)
                block.Statements.Add(node);
        }

        return block;
    }

    // ================= STATEMENTS =================

    public override AstNode VisitStatement(MyLangParser.StatementContext context)
    {
        if (context.varDecl() != null) return Visit(context.varDecl());
        if (context.assignStmt() != null) return Visit(context.assignStmt());
        if (context.returnStmt() != null) return Visit(context.returnStmt());
        if (context.exprStmt() != null) return Visit(context.exprStmt());

        // if/loop/repeat todavía no implementados → dejamos base
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

        var target = new IdentifierNode(
            span,
            context.lvalue().ID().GetText()
        );

        var value = (ExprNode)Visit(context.expression());

        return new AssignNode(span, target, value);
    }

    public override AstNode VisitReturnStmt(MyLangParser.ReturnStmtContext context)
    {
        return new ReturnNode(
            Span(context),
            (ExprNode)Visit(context.expression())
        );
    }

    public override AstNode VisitExprStmt(MyLangParser.ExprStmtContext context)
        => Visit(context.expression());

    // ================= EXPRESSIONS =================

    public override AstNode VisitExpression(MyLangParser.ExpressionContext context)
        => Visit(context.orExpr());

    public override AstNode VisitOrExpr(MyLangParser.OrExprContext context)
    {
        // orExpr : andExpr (OR andExpr)*
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
        // andExpr : notExpr (AND notExpr)*
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
        // notExpr : NOT? relExpr
        // NOT aún no lo representas en AST -> devolvemos relExpr
        return Visit(context.relExpr());
    }

    public override AstNode VisitRelExpr(MyLangParser.RelExprContext context)
    {
        // relExpr : addExpr (relOp addExpr)?
        var left = (ExprNode)Visit(context.addExpr(0));

        if (context.relOp() == null)
            return left;

        var opTokenText = context.relOp().GetText();
        var right = (ExprNode)Visit(context.addExpr(1));

        var op = opTokenText switch
        {
            "==" => BinaryOperator.Equal,
            "!=" => BinaryOperator.NotEqual,
            _ => throw new Exception($"Relational operator '{opTokenText}' not supported yet.")
        };

        return new BinaryExprNode(Span(context), op, left, right);
    }

    public override AstNode VisitAddExpr(MyLangParser.AddExprContext context)
    {
        var left = (ExprNode)Visit(context.mulExpr(0));

        for (int i = 1; i < context.mulExpr().Length; i++)
        {
            var right = (ExprNode)Visit(context.mulExpr(i));

            var operatorToken = context.GetChild(2 * i - 1).GetText();

            var op = operatorToken switch
            {
                "+" => BinaryOperator.Add,
                "-" => BinaryOperator.Subtract,
                _ => throw new Exception($"Unsupported operator '{operatorToken}' in addExpr.")
            };

            left = new BinaryExprNode(
                Span(context),
                op,
                left,
                right
            );
        }

        return left;
    }


    public override AstNode VisitMulExpr(MyLangParser.MulExprContext context)
    {
        var left = (ExprNode)Visit(context.unaryExpr(0));

        for (int i = 1; i < context.unaryExpr().Length; i++)
        {
            var right = (ExprNode)Visit(context.unaryExpr(i));

            var operatorToken = context.GetChild(2 * i - 1).GetText();

            var op = operatorToken switch
            {
                "*" => BinaryOperator.Multiply,
                "/" => BinaryOperator.Divide,
                "%" => BinaryOperator.Modulo,
                _ => throw new Exception($"Unsupported operator '{operatorToken}' in mulExpr.")
            };

            left = new BinaryExprNode(
                Span(context),
                op,
                left,
                right
            );
        }

        return left;
    }

    public override AstNode VisitUnaryExpr(MyLangParser.UnaryExprContext context)
    {
        // Si hay operador menos unario
        if (context.MINUS() != null)
        {
            var exprNode = Visit(context.GetChild(context.ChildCount - 1)) as ExprNode
                           ?? throw new Exception("Unary minus without expression.");

            return new BinaryExprNode(
                Span(context),
                BinaryOperator.Negate,
                new LiteralNode(Span(context), 0),
                exprNode
            );
        }

        // Caso normal: delegar al único hijo real
        if (context.ChildCount == 1)
            return Visit(context.GetChild(0));

        throw new Exception($"Invalid unary expression at {context.Start.Line}:{context.Start.Column}");
    }

    public override AstNode VisitPrimary(MyLangParser.PrimaryContext context)
    {
        if (context.literal() != null) return Visit(context.literal());
        if (context.callExpr() != null) return Visit(context.callExpr());

        if (context.ID() != null)
            return new IdentifierNode(Span(context), context.ID().GetText());

        if (context.expression() != null)
            return Visit(context.expression());

        throw new Exception($"Invalid primary expression at {context.Start.Line}:{context.Start.Column}. Text='{context.GetText()}'");
    }

    public override AstNode VisitCallExpr(MyLangParser.CallExprContext context)
    {
        // callExpr : ID LPAREN argList? RPAREN
        var span = Span(context);
        var name = context.ID().GetText();

        var args = new List<ExprNode>();

        if (context.argList() != null)
        {
            foreach (var arg in context.argList().expression())
                args.Add((ExprNode)Visit(arg));
        }

        return new CallExprNode(span, name, args);
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
    {
        return new TypeRefNode(
            Span(context),
            context.GetText()
        );
    }
}
