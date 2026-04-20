namespace MlogSharp
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: MlogCompiler <input_file>");
                return;
            }

            string filePath = args[0];
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"File not found: {filePath}");
                return;
            }

            try
            {
                string source = File.ReadAllText(filePath);

                // 1. Lexing
                Lexer lexer = new Lexer(source);
                var tokens = lexer.Tokenize();

                // Debug: print tokens
                // foreach (var t in tokens) Console.WriteLine(t);

                // 2. Parsing
                Parser parser = new Parser(tokens);
                var ast = parser.Parse();

                // 3. Compiling
                Compiler compiler = new Compiler();
                string mlogCode = compiler.Compile(ast);

                Console.WriteLine("Compiled Mlog Code:");
                Console.WriteLine("-------------------");
                Console.WriteLine(mlogCode);

                // Optional: save to file
                File.WriteAllText(Path.ChangeExtension(filePath, ".mlog"), mlogCode);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            Console.ReadKey();
        }
    }
}
