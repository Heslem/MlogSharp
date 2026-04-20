using System;
using System.Collections.Generic;
using System.Diagnostics; // Для Debug.WriteLine

namespace MlogSharp
{
    public class Parser
    {
        private readonly List<Token> _tokens;
        private int _position;

        public Parser(List<Token> tokens)
        {
            _tokens = tokens;
            _position = 0;
        }

        private Token Current => _tokens[_position];

        private void Log(string msg)
        {
            // Выводим в консоль отладки. В VS Code / Visual Studio это видно в Output.
            // Или можно использовать Console.Error.WriteLine для вывода в stderr
            Console.Error.WriteLine($"[PARSER] {msg} | Current: {Current.Type} '{Current.Text}'");
        }

        private Token Peek(int offset = 1)
        {
            int idx = _position + offset;
            return idx < _tokens.Count ? _tokens[idx] : _tokens[_tokens.Count - 1];
        }

        private void Advance()
        {
            // Log($"Advance from {Current.Text}");
            _position++;
        }

        private void Expect(TokenType type)
        {
            if (Current.Type != type)
                throw new Exception($"Expected {type} but got {Current.Type} ('{Current.Text}') at line {Current.Line}");
            Advance();
        }

        public ProgramNode Parse()
        {
            Log("Starting Parse");
            var statements = new List<Statement>();
            while (Current.Type != TokenType.EndOfFile)
            {
                if (Current.Type == TokenType.Function)
                {
                    statements.Add(ParseFunctionDeclaration());
                }
                else
                {
                    statements.Add(ParseStatement());
                }
            }
            Log("Parse Finished");
            return new ProgramNode(statements);
        }

        private FunctionDeclaration ParseFunctionDeclaration()
        {
            Log("Parsing Function Declaration");
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
            while (Current.Type != TokenType.CloseBrace)
            {
                body.Add(ParseStatement());
            }
            Expect(TokenType.CloseBrace);

            return new FunctionDeclaration(name, parameters, body);
        }

        private Statement ParseStatement()
        {
            Log($"Parsing Statement: {Current.Type}");

            if (Current.Type == TokenType.Print) return ParsePrintStatement();
            if (Current.Type == TokenType.Return) return ParseReturnStatement();
            if (Current.Type == TokenType.If) return ParseIfStatement();
            if (Current.Type == TokenType.While) return ParseWhileStatement();
            if (Current.Type == TokenType.For) return ParseForStatement();
            if (Current.Type == TokenType.AsmBlock) return ParseAsmStatement();

            var expr = ParseExpression();
            if (Current.Type == TokenType.Semicolon) Advance();
            return new ExpressionStatement(expr);
        }

        private AsmBlockStatement ParseAsmStatement()
        {
            // Current.Text уже содержит весь код внутри asm { ... }
            string rawCode = Current.Text;
            Advance(); // съедаем токен
            return new AsmBlockStatement(rawCode);
        }

        private WhileStatement ParseWhileStatement()
        {
            Expect(TokenType.While);
            Expect(TokenType.OpenParen);
            var condition = ParseExpression();
            Expect(TokenType.CloseParen);

            Expect(TokenType.OpenBrace);
            var body = new List<Statement>();
            while (Current.Type != TokenType.CloseBrace)
            {
                body.Add(ParseStatement());
            }
            Expect(TokenType.CloseBrace);

            return new WhileStatement(condition, body);
        }

        private ForStatement ParseForStatement()
        {
            Expect(TokenType.For);
            Expect(TokenType.OpenParen);

            // 1. Init (может быть пустым)
            Statement? init = null;
            if (Current.Type != TokenType.Semicolon)
            {
                // Init может быть выражением (x=0) или объявлением (пока нет decl)
                init = ParseStatement();
                // ParseStatement съедает точку с запятой, если она есть внутри выражения?
                // Нет, наше выражение не ест ;. А Statement ест.
                // Но в for (int i=0; ...) точка с запятой разделитель.
                // Наш ParseStatement для ExpressionStatement ожидает ; в конце.
                // В for скобки заменяют ;.

                // Хак: если мы распарсили ExpressionStatement, он уже съел ;?
                // Да, в ParseStatement есть: if (Current.Type == TokenType.Semicolon) Advance();
                // Но в for разделитель - тоже ;.
                // Поэтому, если мы вызвали ParseStatement, он съел ;.
                // Проверим, что текущий токен не ;, чтобы не пропустить лишний.
            }
            else
            {
                Advance(); // съедаем ;
            }

            // 2. Condition
            Expression? condition = null;
            if (Current.Type != TokenType.Semicolon)
            {
                condition = ParseExpression();
            }
            Expect(TokenType.Semicolon); // съедаем ; после условия

            // 3. Update
            Statement? update = null;
            if (Current.Type != TokenType.CloseParen)
            {
                update = ParseStatement(); // Аналогично init, съест ; если он там был бы, но в for его нет перед )
                // В for (..; ..; i++) нет точки с запятой после i++ перед )
                // Но наш ParseStatement для выражения НЕ требует ;, если его нет?
                // Нет, в ParseStatement: if (Current.Type == TokenType.Semicolon) Advance();
                // Значит, если мы напишем for(...; ...; i++), то ParseStatement вернет ExprStmt, не съев ничего лишнего, так как следом идет )
            }

            Expect(TokenType.CloseParen);

            Expect(TokenType.OpenBrace);
            var body = new List<Statement>();
            while (Current.Type != TokenType.CloseBrace)
            {
                body.Add(ParseStatement());
            }
            Expect(TokenType.CloseBrace);

            return new ForStatement(init, condition, update, body);
        }

        private IfStatement ParseIfStatement()
        {
            Expect(TokenType.If);
            Expect(TokenType.OpenParen);

            // Условие - это выражение
            var condition = ParseExpression();

            Expect(TokenType.CloseParen);

            Expect(TokenType.OpenBrace);
            var thenBody = new List<Statement>();
            while (Current.Type != TokenType.CloseBrace)
            {
                thenBody.Add(ParseStatement());
            }
            Expect(TokenType.CloseBrace);

            List<Statement>? elseBody = null;

            // Проверяем наличие else
            if (Current.Type == TokenType.Else) // Нужно добавить TokenType.Else в enum
            {
                Advance();

                // Else может быть просто блоком { ... } или другим if (else if)
                if (Current.Type == TokenType.If)
                {
                    // Else If - это просто IfStatement внутри else
                    elseBody = new List<Statement> { ParseIfStatement() };
                }
                else
                {
                    Expect(TokenType.OpenBrace);
                    elseBody = new List<Statement>();
                    while (Current.Type != TokenType.CloseBrace)
                    {
                        elseBody.Add(ParseStatement());
                    }
                    Expect(TokenType.CloseBrace);
                }
            }

            return new IfStatement(condition, thenBody, elseBody);
        }

        public class ExpressionStatement : Statement
        {
            public Expression Expr { get; }
            public ExpressionStatement(Expression expr) => Expr = expr;
        }

        private PrintStatement ParsePrintStatement()
        {
            Log("Parsing Print Statement");
            Expect(TokenType.Print);

            // Проверяем, есть ли скобки. print может быть как print "str", так и print("str")
            Expression? value = null;
            Expression? destination = null;

            if (Current.Type == TokenType.OpenParen)
            {
                // Синтаксис: print(arg1, arg2)
                Advance(); // съедаем (

                // Первый аргумент обязателен
                value = ParseExpression();

                // Если есть запятая, читаем второй аргумент
                if (Current.Type == TokenType.Comma)
                {
                    Advance();
                    destination = ParseExpression();
                }

                Expect(TokenType.CloseParen);
            }
            else
            {
                // Синтаксис: print arg1 (или print arg1, arg2 - но в Mlog обычно printflush отдельно)
                // Для простоты пока поддерживаем print value
                value = ParseExpression();
            }

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

        private Expression ParseExpression()
        {
            return ParseAssignment();
        }

        private Expression ParseAssignment()
        {
            // Сначала парсим правую часть (или левую, если это не присваивание)
            // Например, в x = 5, сначала парсим x как Primary/Variable
            var left = ParseAddition();

            // Если следом идет =, то это присваивание
            if (Current.Type == TokenType.Equals)
            {
                // Левая часть должна быть переменной
                if (left is not VariableReference vr)
                {
                    throw new Exception("Left side of assignment must be a variable");
                }

                Advance(); // съедаем =
                var value = ParseAssignment(); // Рекурсия для поддержки x = y = 5

                return new Assignment(vr.Name, value);
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

            // Проверяем операторы сравнения
            TokenType[] compOps = {
                TokenType.EqualEqual,
                TokenType.NotEqual,
                TokenType.LessThan,
                TokenType.GreaterThan,
                TokenType.LessThanEq,
                TokenType.GreaterThanEq
            };

            if (compOps.Contains(Current.Type))
            {
                string op = Current.Text;
                Advance();
                var right = ParseMultiplication();

                // Нормализуем операторы для удобства в Compiler
                // Например, "==" оставим как "==", но можно сразу мапить на Mlog-опкоды, если хочешь.
                // Давай оставим текстовые обозначения, а в Compiler сделаем switch.

                return new BinaryOperation(left, op, right);
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
                var right = ParsePrimary();
                left = new BinaryOperation(left, op, right);
            }
            return left;
        }

        private Expression ParsePrimary()
        {
            Log($"ParsePrimary: {Current.Type} '{Current.Text}'");

            // 1. Скобки
            if (Current.Type == TokenType.OpenParen)
            {
                Advance();
                var expr = ParseExpression();
                Expect(TokenType.CloseParen);
                return expr;
            }

            // 2. Числа
            if (Current.Type == TokenType.Number)
            {
                double val = double.Parse(Current.Text);
                Advance();
                return new NumberLiteral(val);
            }

            // 3. Строки
            if (Current.Type == TokenType.StringLiteral)
            {
                string val = Current.Text;
                Advance();
                return new StringLiteral(val);
            }

            // 4. Идентификаторы (переменные или вызовы функций)
            if (Current.Type == TokenType.Identifier)
            {
                string name = Current.Text;
                Advance();

                // Если следом идет (, то это вызов функции
                if (Current.Type == TokenType.OpenParen)
                {
                    Advance();
                    var args = new List<Expression>();

                    // Если не закрывающая скобка, значит есть аргументы
                    if (Current.Type != TokenType.CloseParen)
                    {
                        args.Add(ParseExpression());
                        while (Current.Type == TokenType.Comma)
                        {
                            Advance();
                            args.Add(ParseExpression());
                        }
                    }
                    Expect(TokenType.CloseParen);
                    return new FunctionCall(name, args);
                }

                return new VariableReference(name);
            }

            throw new Exception($"Unexpected token in Primary: {Current.Type} ('{Current.Text}') at line {Current.Line}");
        }
    }
}