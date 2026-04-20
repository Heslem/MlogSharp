using System;
using System.Collections.Generic;

namespace MlogSharp
{
    public class Parser
    {
        private readonly List<Token> _tokens;
        private int _position;
        private Token Current => _tokens[_position];
        private Token Peek(int offset = 1) => _position + offset < _tokens.Count ? _tokens[_position + offset] : _tokens[_tokens.Count - 1];
        private void Advance() => _position++;
        private void Expect(TokenType type)
        {
            if (Current.Type != type) throw new Exception($"Expected {type} but got {Current.Type} ('{Current.Text}') at line {Current.Line}");
            Advance();
        }

        public Parser(List<Token> tokens) => (_tokens, _position) = (tokens, 0);

        public ProgramNode Parse()
        {
            var statements = new List<Statement>();
            while (Current.Type != TokenType.EndOfFile)
                statements.Add(Current.Type == TokenType.Function ? ParseFunctionDeclaration() : ParseStatement());
            return new ProgramNode(statements);
        }

        private FunctionDeclaration ParseFunctionDeclaration()
        {
            Expect(TokenType.Function);
            string name = Current.Text;
            Expect(TokenType.Identifier);
            Expect(TokenType.OpenParen);
            var parameters = new List<string>();
            if (Current.Type != TokenType.CloseParen)
            {
                parameters.Add(Current.Text);
                Expect(TokenType.Identifier);
                while (Current.Type == TokenType.Comma)
                {
                    Advance();
                    parameters.Add(Current.Text);
                    Expect(TokenType.Identifier);
                }
            }
            Expect(TokenType.CloseParen);
            Expect(TokenType.OpenBrace);
            var body = new List<Statement>();
            while (Current.Type != TokenType.CloseBrace) body.Add(ParseStatement());
            Expect(TokenType.CloseBrace);
            return new FunctionDeclaration(name, parameters, body);
        }

        private Statement ParseStatement()
        {
            if (Current.Type == TokenType.Print) return ParsePrintStatement();
            if (Current.Type == TokenType.Return) return ParseReturnStatement();
            if (Current.Type == TokenType.If) return ParseIfStatement();
            if (Current.Type == TokenType.While) return ParseWhileStatement();
            if (Current.Type == TokenType.For) return ParseForStatement();
            if (Current.Type == TokenType.AsmBlock) { var asm = new AsmBlockStatement(Current.Text); Advance(); return asm; }
<<<<<<< проект-обзор-и-анализ-4601b
            if (Current.Type == TokenType.Array) return ParseArrayDeclaration();
=======
>>>>>>> master
            var expr = ParseExpression();
            if (Current.Type == TokenType.Semicolon) Advance();
            return new ExpressionStatement(expr);
        }

        private WhileStatement ParseWhileStatement()
        {
            Expect(TokenType.While);
            Expect(TokenType.OpenParen);
            var condition = ParseExpression();
            Expect(TokenType.CloseParen);
            Expect(TokenType.OpenBrace);
            var body = new List<Statement>();
            while (Current.Type != TokenType.CloseBrace) body.Add(ParseStatement());
            Expect(TokenType.CloseBrace);
            return new WhileStatement(condition, body);
        }

        private ForStatement ParseForStatement()
        {
            Expect(TokenType.For);
            Expect(TokenType.OpenParen);
            Statement? init = null;
            if (Current.Type != TokenType.Semicolon) init = ParseStatement();
            else Advance();
            Expression? condition = null;
            if (Current.Type != TokenType.Semicolon) condition = ParseExpression();
            Expect(TokenType.Semicolon);
            Statement? update = null;
            if (Current.Type != TokenType.CloseParen) update = ParseStatement();
            Expect(TokenType.CloseParen);
            Expect(TokenType.OpenBrace);
            var body = new List<Statement>();
            while (Current.Type != TokenType.CloseBrace) body.Add(ParseStatement());
            Expect(TokenType.CloseBrace);
            return new ForStatement(init, condition, update, body);
        }

        private IfStatement ParseIfStatement()
        {
            Expect(TokenType.If);
            Expect(TokenType.OpenParen);
            var condition = ParseExpression();
            Expect(TokenType.CloseParen);
            Expect(TokenType.OpenBrace);
            var thenBody = new List<Statement>();
            while (Current.Type != TokenType.CloseBrace) thenBody.Add(ParseStatement());
            Expect(TokenType.CloseBrace);
            List<Statement>? elseBody = null;
            if (Current.Type == TokenType.Else)
            {
                Advance();
                if (Current.Type == TokenType.If) elseBody = new List<Statement> { ParseIfStatement() };
                else
                {
                    Expect(TokenType.OpenBrace);
                    elseBody = new List<Statement>();
                    while (Current.Type != TokenType.CloseBrace) elseBody.Add(ParseStatement());
                    Expect(TokenType.CloseBrace);
                }
            }
            return new IfStatement(condition, thenBody, elseBody);
        }

        private PrintStatement ParsePrintStatement()
        {
            Expect(TokenType.Print);
            Expression? value = null, destination = null;
            if (Current.Type == TokenType.OpenParen)
            {
                Advance();
                value = ParseExpression();
                if (Current.Type == TokenType.Comma) { Advance(); destination = ParseExpression(); }
                Expect(TokenType.CloseParen);
            }
            else value = ParseExpression();
            if (Current.Type == TokenType.Semicolon) Advance();
            return new PrintStatement(value, destination);
        }

        private ReturnStatement ParseReturnStatement()
        {
            Expect(TokenType.Return);
            var expr = ParseExpression();
            if (Current.Type == TokenType.Semicolon) Advance();
            return new ReturnStatement(expr);
        }

<<<<<<< проект-обзор-и-анализ-4601b
        private ArrayDeclaration ParseArrayDeclaration()
        {
            Expect(TokenType.Array);
            string name = Current.Text;
            Expect(TokenType.Identifier);
            Expect(TokenType.OpenParen);
            var size = ParseExpression();
            Expect(TokenType.Comma);
            string cellName = Current.Text;
            Expect(TokenType.Identifier);
            Expect(TokenType.CloseParen);
            if (Current.Type == TokenType.Semicolon) Advance();
            return new ArrayDeclaration(name, size, cellName);
        }
=======
        private Expression ParseExpression() => ParseAssignment();
>>>>>>> master

        private Expression ParseExpression() => ParseAssignment();

        private Expression ParseAssignment()
        {
            var left = ParseAddition();
            if (Current.Type == TokenType.Equals)
            {
<<<<<<< проект-обзор-и-анализ-4601b
                Advance();
                var right = ParseAssignment();
                if (left is ArrayAccessExpression arrAccess)
                    return new ArrayAssignmentStatement(arrAccess.ArrayName, arrAccess.Index, right);
                if (left is not VariableReference vr) throw new Exception("Left side of assignment must be a variable or array access");
                return new Assignment(vr.Name, right);
=======
                if (left is not VariableReference vr) throw new Exception("Left side of assignment must be a variable");
                Advance();
                return new Assignment(vr.Name, ParseAssignment());
>>>>>>> master
            }
            return left;
        }

        private Expression ParseAddition()
        {
            var left = ParseComparison();
            while (Current.Type == TokenType.Plus || Current.Type == TokenType.Minus)
            {
                var op = Current.Text;
                Advance();
                var right = ParseMultiplication();
                left = new BinaryOperation(left, op, right);
            }
            return left;
        }

        private Expression ParseComparison()
        {
            var left = ParseMultiplication();
            TokenType[] compOps = { TokenType.EqualEqual, TokenType.NotEqual, TokenType.LessThan, TokenType.GreaterThan, TokenType.LessThanEq, TokenType.GreaterThanEq };
            if (compOps.Contains(Current.Type))
            {
                string op = Current.Text;
                Advance();
                return new BinaryOperation(left, op, ParseMultiplication());
            }
            return left;
        }

        private Expression ParseMultiplication()
        {
            var left = ParsePrimary();
            while (Current.Type == TokenType.Star || Current.Type == TokenType.Slash)
            {
                var op = Current.Text;
                Advance();
                left = new BinaryOperation(left, op, ParsePrimary());
            }
            return left;
        }

        private Expression ParsePrimary()
        {
            if (Current.Type == TokenType.OpenParen) { Advance(); var expr = ParseExpression(); Expect(TokenType.CloseParen); return expr; }
            if (Current.Type == TokenType.Number) { double val = double.Parse(Current.Text); Advance(); return new NumberLiteral(val); }
            if (Current.Type == TokenType.StringLiteral) { string val = Current.Text; Advance(); return new StringLiteral(val); }
            if (Current.Type == TokenType.Identifier)
            {
                string name = Current.Text;
                Advance();
                if (Current.Type == TokenType.OpenParen)
                {
                    Advance();
                    var args = new List<Expression>();
                    if (Current.Type != TokenType.CloseParen)
                    {
                        args.Add(ParseExpression());
                        while (Current.Type == TokenType.Comma) { Advance(); args.Add(ParseExpression()); }
                    }
                    Expect(TokenType.CloseParen);
                    return new FunctionCall(name, args);
                }
<<<<<<< проект-обзор-и-анализ-4601b
                if (Current.Type == TokenType.OpenBracket)
                {
                    Advance();
                    var index = ParseExpression();
                    Expect(TokenType.CloseBracket);
                    return new ArrayAccessExpression(name, index);
                }
=======
>>>>>>> master
                return new VariableReference(name);
            }
            throw new Exception($"Unexpected token in Primary: {Current.Type} ('{Current.Text}') at line {Current.Line}");
        }

        public class ExpressionStatement : Statement
        {
            public Expression Expr { get; }
            public ExpressionStatement(Expression expr) => Expr = expr;
        }
    }
}
