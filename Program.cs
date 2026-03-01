using Antlr4.Runtime;
using MyLangCompiler.Nodes;
using MyLangCompiler.Semantic;
using MyLangCompiler.Runtime;

namespace MyLangCompiler;

class Program
{
    static void Main(string[] args)
    {
        var input = @"
use System;
use Generics;

object Program
{
	entry func Main():i {
  declare arr:i[3];
  set arr[0] = 4;
  show(len(arr));
  set arr = [1,4];
  show(len(arr));
  gives 0;
}
Explicar
";

        // ======================
        // LEXER & PARSER
        // ======================

        var inputStream = new AntlrInputStream(input);
        var lexer = new MyLangLexer(inputStream);
        var tokens = new CommonTokenStream(lexer);
        var parser = new MyLangParser(tokens);

        var tree = parser.program();

        // ======================
        // AST BUILD
        // ======================

        var visitor = new AstBuilderVisitor();
        var ast = visitor.Visit(tree);

        Console.WriteLine("===== AST =====");
        AstPrinter.Print(ast);

        // ======================
        // SEMANTIC ANALYSIS
        // ======================

        Console.WriteLine();
        Console.WriteLine("===== SEMANTIC ANALYSIS =====");

        try
        {
            var analyzer = new SemanticAnalyzerVisitor();
            analyzer.Analyze((ProgramNode)ast);

            Console.WriteLine("Semantic analysis completed successfully.");

            // ======================
            // EXECUTION (INTERPRETER)
            // ======================

            Console.WriteLine();
            Console.WriteLine("===== EXECUTION =====");

            var interpreter = new Interpreter();
            var result = interpreter.Execute((ProgramNode)ast);

            Console.WriteLine($"Program result: {result}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Semantic error: {ex.Message}");
        }
    }
}
