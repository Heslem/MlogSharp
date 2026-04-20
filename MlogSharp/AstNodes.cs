using System;
using System.Collections.Generic;

namespace MlogSharp
{
    public abstract class AstNode { }
    public abstract class Expression : AstNode { }
    public abstract class Statement : AstNode { }

    public class NumberLiteral : Expression
    {
        public double Value { get; }
        public NumberLiteral(double value) => Value = value;
    }

    public class StringLiteral : Expression
    {
        public string Value { get; }
        public StringLiteral(string value) => Value = value;
    }

    public class VariableReference : Expression
    {
        public string Name { get; }
        public VariableReference(string name) => Name = name;
    }

    public class BinaryOperation : Expression
    {
        public Expression Left { get; }
        public string Operator { get; }
        public Expression Right { get; }
        public BinaryOperation(Expression left, string op, Expression right) =>
            (Left, Operator, Right) = (left, op, right);
    }

    public class FunctionCall : Expression
    {
        public string FunctionName { get; }
        public List<Expression> Arguments { get; }
        public FunctionCall(string name, List<Expression> args) =>
            (FunctionName, Arguments) = (name, args);
    }

    public class Assignment : Expression
    {
        public string VariableName { get; }
        public Expression Value { get; }
        public Assignment(string variableName, Expression value) =>
            (VariableName, Value) = (variableName, value);
    }

    public class PrintStatement : Statement
    {
        public Expression Value { get; }
        public Expression? Destination { get; }
        public PrintStatement(Expression value, Expression? destination = null) =>
            (Value, Destination) = (value, destination);
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
        public FunctionDeclaration(string name, List<string> parameters, List<Statement> body) =>
            (Name, Parameters, Body) = (name, parameters, body);
    }

    public class IfStatement : Statement
    {
        public Expression Condition { get; }
        public List<Statement> ThenBody { get; }
        public List<Statement>? ElseBody { get; }
        public IfStatement(Expression condition, List<Statement> thenBody, List<Statement>? elseBody = null) =>
            (Condition, ThenBody, ElseBody) = (condition, thenBody, elseBody);
    }

    public class WhileStatement : Statement
    {
        public Expression Condition { get; }
        public List<Statement> Body { get; }
        public WhileStatement(Expression condition, List<Statement> body) =>
            (Condition, Body) = (condition, body);
    }

    public class ForStatement : Statement
    {
        public Statement? Init { get; }
        public Expression? Condition { get; }
        public Statement? Update { get; }
        public List<Statement> Body { get; }
        public ForStatement(Statement? init, Expression? condition, Statement? update, List<Statement> body) =>
            (Init, Condition, Update, Body) = (init, condition, update, body);
    }

    public class AsmBlockStatement : Statement
    {
        public string RawCode { get; }
        public AsmBlockStatement(string rawCode) => RawCode = rawCode;
    }

    public class ArrayDeclaration : Statement
    {
        public string Name { get; }
        public Expression Size { get; }
        public string CellName { get; }
        public ArrayDeclaration(string name, Expression size, string cellName) =>
            (Name, Size, CellName) = (name, size, cellName);
    }

    public class ArrayAccessExpression : Expression
    {
        public string ArrayName { get; }
        public Expression Index { get; }
        public ArrayAccessExpression(string arrayName, Expression index) =>
            (ArrayName, Index) = (arrayName, index);
    }

    public class ArrayAssignmentStatement : Statement
    {
        public string ArrayName { get; }
        public Expression Index { get; }
        public Expression Value { get; }
        public ArrayAssignmentStatement(string arrayName, Expression index, Expression value) =>
            (ArrayName, Index, Value) = (arrayName, index, value);
    }

    public class ProgramNode : AstNode
    {
        public List<Statement> Statements { get; }
        public ProgramNode(List<Statement> statements) => Statements = statements;
    }
}
