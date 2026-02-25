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
	entry func Main():i
	{
		declare z:s;
		declare x:i = 5;
		declare y:i = 3;

		loop (declare j:i = 0; j < 10; set j = j + 1)
		{
			show(j + y);
		}

		check (x > y)
		{
			show(""La variable x es mas grande que la variable y"");
		}
		otherwise
		{
			show(""La variable y es mas grande que la variable x"");
		}

		repeat (x < 7)
		{
			show(""While loop"");
			set x = x + 1;
		}

		show(""Suma:"");
		show(x + y);

		show(""Resta:"");
		show(x - y);

		show(""Multiplicación:"");
		show(x * y);

		show(""División:"");
		show(x / y);

		show(""Módulo:"");
		show(x % y);

		show(""Ingresa un dato:"");
		ask(z);

		show(""Ingresaste:"");
		show(z);
		
		declare obj:Math = Math();

		obj.suma(x, y);

		declare test:i = obj.suma(x, y);

		set x = obj.suma(x, y);
		
		show(""Factorial:"");
		show(obj.factorial(x));
		
		gives 0;
	}
}

object Math
{
	func suma(a:i, c:i):i
	{
		gives a + c;
	}
	
	func factorial(num:i):i
	{
		check (num == 1)
		{
			gives num;
		}
		
		gives num * factorial(num - 1);
	}
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
