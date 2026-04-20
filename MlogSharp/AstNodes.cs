using System;
using System.Collections.Generic;
using System.Text;

namespace MlogSharp
{
    // Базовый класс для всех узлов
    public abstract class AstNode { }

    // Выражения
    public abstract class Expression : AstNode { }

    public class NumberLiteral : Expression
    {
        public double Value { get; }
        public NumberLiteral(double value) => Value = value;
    }

    public class VariableReference : Expression
    {
        public string Name { get; }
        public VariableReference(string name) => Name = name;
    }

    public class BinaryOperation : Expression
    {
        public Expression Left { get; }
        public string Operator { get; } // "+", "-", "*", "/"
        public Expression Right { get; }
        public BinaryOperation(Expression left, string op, Expression right)
        {
            Left = left;
            Operator = op;
            Right = right;
        }
    }

    public class FunctionCall : Expression
    {
        public string FunctionName { get; }
        public List<Expression> Arguments { get; }
        public FunctionCall(string name, List<Expression> args)
        {
            FunctionName = name;
            Arguments = args;
        }
    }

    // Инструкции (Statements)
    public abstract class Statement : AstNode { }

    public class PrintStatement : Statement
    {
        public Expression Value { get; }
        public Expression? Destination { get; } // Опциональный адресат
        public PrintStatement(Expression value, Expression? destination = null)
        {
            Value = value;
            Destination = destination;
        }
    }

    public class ReturnStatement : Statement
    {
        public Expression Value { get; }
        public ReturnStatement(Expression value) => Value = value;
    }

    public class FunctionDeclaration : Statement
    {
        public string Name { get; }
        public List<string> Parameters { get; }
        public List<Statement> Body { get; }
        public FunctionDeclaration(string name, List<string> parameters, List<Statement> body)
        {
            Name = name;
            Parameters = parameters;
            Body = body;
        }
    }

    public class ProgramNode : AstNode
    {
        public List<Statement> Statements { get; }
        public ProgramNode(List<Statement> statements) => Statements = statements;
    }

    public class StringLiteral : Expression
    {
        public string Value { get; }
        public StringLiteral(string value) => Value = value;
    }

    public class Assignment : Expression
    {
        public string VariableName { get; }
        public Expression Value { get; }

        public Assignment(string variableName, Expression value)
        {
            VariableName = variableName;
            Value = value;
        }
    }

    public class IfStatement : Statement
    {
        public Expression Condition { get; }
        public List<Statement> ThenBody { get; }
        public List<Statement>? ElseBody { get; } // null если else нет

        public IfStatement(Expression condition, List<Statement> thenBody, List<Statement>? elseBody = null)
        {
            Condition = condition;
            ThenBody = thenBody;
            ElseBody = elseBody;
        }
    }

    public class WhileStatement : Statement
    {
        public Expression Condition { get; }
        public List<Statement> Body { get; }

        public WhileStatement(Expression condition, List<Statement> body)
        {
            Condition = condition;
            Body = body;
        }
    }

    public class ForStatement : Statement
    {
        public Statement? Init { get; }      // Инициализация (может быть null)
        public Expression? Condition { get; } // Условие (может быть null, тогда бесконечный цикл)
        public Statement? Update { get; }     // Шаг (может быть null)
        public List<Statement> Body { get; }

        public ForStatement(Statement? init, Expression? condition, Statement? update, List<Statement> body)
        {
            Init = init;
            Condition = condition;
            Update = update;
            Body = body;
        }
    }

    public class AsmBlockStatement : Statement
    {
        public string RawCode { get; }

        public AsmBlockStatement(string rawCode)
        {
            RawCode = rawCode;
        }
    }
}
