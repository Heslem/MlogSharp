using System;

namespace MlogSharp
{
    class Program
    {
        static void Main(string[] args)
        {
            string filePath = args.Length > 0 ? args[0] : "test.high";
            if (!File.Exists(filePath)) { Console.WriteLine($"File not found: {filePath}"); return; }
            
            try
            {
                var tokens = new Lexer(File.ReadAllText(filePath)).Tokenize();
                var ast = new Parser(tokens).Parse();
                string mlog = new Compiler().Compile(ast);
                Console.WriteLine(mlog);
                File.WriteAllText(Path.ChangeExtension(filePath, ".mlog"), mlog);
            }
            catch (Exception ex) { Console.WriteLine($"Error: {ex.Message}"); }
        }
    }
}
