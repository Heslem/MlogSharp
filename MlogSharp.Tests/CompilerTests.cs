using Xunit;

namespace MlogSharp.Tests;

public class LexerTests
{
    [Fact]
    public void Tokenize_Numbers()
    {
        var tokens = new Lexer("42 3.14").Tokenize();
        Assert.Equal(TokenType.Number, tokens[0].Type);
        Assert.Equal("42", tokens[0].Text);
        Assert.Equal(TokenType.Number, tokens[1].Type);
        Assert.Equal("3.14", tokens[1].Text);
    }

    [Fact]
    public void Tokenize_IdentifiersAndKeywords()
    {
        var tokens = new Lexer("function x print").Tokenize();
        Assert.Equal(TokenType.Function, tokens[0].Type);
        Assert.Equal(TokenType.Identifier, tokens[1].Type);
        Assert.Equal(TokenType.Print, tokens[2].Type);
    }

    [Fact]
    public void Tokenize_Operators()
    {
        var tokens = new Lexer("+ - * / == != < > <= >=").Tokenize();
        Assert.Equal(TokenType.Plus, tokens[0].Type);
        Assert.Equal(TokenType.Minus, tokens[1].Type);
        Assert.Equal(TokenType.EqualEqual, tokens[4].Type);
        Assert.Equal(TokenType.NotEqual, tokens[5].Type);
    }

    [Fact]
    public void Tokenize_StringLiteral()
    {
        var tokens = new Lexer("\"hello\"").Tokenize();
        Assert.Equal(TokenType.StringLiteral, tokens[0].Type);
        Assert.Equal("hello", tokens[0].Text);
    }

    [Fact]
    public void Tokenize_Comments()
    {
        var tokens = new Lexer("x // comment\ny").Tokenize();
        Assert.Equal(TokenType.Identifier, tokens[0].Type);
        Assert.Equal("x", tokens[0].Text);
        Assert.Equal(TokenType.Identifier, tokens[1].Type);
        Assert.Equal("y", tokens[1].Text);
    }
}

public class ParserTests
{
    [Fact]
    public void Parse_NumberLiteral()
    {
        var ast = new Parser(new Lexer("42;").Tokenize()).Parse();
        var stmt = Assert.IsType<Parser.ExpressionStatement>(ast.Statements[0]);
        var num = Assert.IsType<NumberLiteral>(stmt.Expr);
        Assert.Equal(42, num.Value);
    }

    [Fact]
    public void Parse_Assignment()
    {
        var ast = new Parser(new Lexer("x = 5;").Tokenize()).Parse();
        var stmt = Assert.IsType<Parser.ExpressionStatement>(ast.Statements[0]);
        var assign = Assert.IsType<Assignment>(stmt.Expr);
        Assert.Equal("x", assign.VariableName);
    }

    [Fact]
    public void Parse_FunctionDeclaration()
    {
        var ast = new Parser(new Lexer("function foo(a, b) { return a + b; }").Tokenize()).Parse();
        var func = Assert.IsType<FunctionDeclaration>(ast.Statements[0]);
        Assert.Equal("foo", func.Name);
        Assert.Equal(new[] { "a", "b" }, func.Parameters);
    }

    [Fact]
    public void Parse_IfStatement()
    {
        var ast = new Parser(new Lexer("if (x > 0) { print x; }").Tokenize()).Parse();
        var ifStmt = Assert.IsType<IfStatement>(ast.Statements[0]);
        Assert.IsType<BinaryOperation>(ifStmt.Condition);
    }

    [Fact]
    public void Parse_WhileStatement()
    {
        var ast = new Parser(new Lexer("while (x < 10) { x = x + 1; }").Tokenize()).Parse();
        var whileStmt = Assert.IsType<WhileStatement>(ast.Statements[0]);
        Assert.IsType<BinaryOperation>(whileStmt.Condition);
    }
}

public class CompilerTests
{
    [Fact]
    public void Compile_SimplePrint()
    {
        var ast = new Parser(new Lexer("print \"hello\";").Tokenize()).Parse();
        var result = new Compiler().Compile(ast);
        Assert.Contains("print \"hello\"", result);
        Assert.Contains("printflush message1", result);
    }

    [Fact]
    public void Compile_Assignment()
    {
        var ast = new Parser(new Lexer("x = 5;").Tokenize()).Parse();
        var result = new Compiler().Compile(ast);
        Assert.Contains("set x 5", result);
    }

    [Fact]
    public void Compile_Arithmetic()
    {
        var ast = new Parser(new Lexer("x = 2 + 3;").Tokenize()).Parse();
        var result = new Compiler().Compile(ast);
        Assert.Contains("op add", result);
    }

    [Fact]
    public void Compile_FunctionCall()
    {
        var ast = new Parser(new Lexer("function foo() { return 1; } foo();").Tokenize()).Parse();
        var result = new Compiler().Compile(ast);
        Assert.Contains("@foo_copy0:", result);
        Assert.Contains("jump @foo_copy0", result);
    }

    [Fact]
    public void Compile_IfStatement()
    {
        var ast = new Parser(new Lexer("if (x > 0) { print 1; }").Tokenize()).Parse();
        var result = new Compiler().Compile(ast);
        Assert.Contains("jump @label_", result);
        Assert.Contains("greaterThan", result);
    }

    [Fact]
    public void Compile_EndMarker()
    {
        var ast = new Parser(new Lexer("print 1;").Tokenize()).Parse();
        var result = new Compiler().Compile(ast);
        Assert.EndsWith("\nend", result);
    }
}
