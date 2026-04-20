using System;
using System.Collections.Generic;
using System.Linq;

namespace MlogSharp
{
    public class Compiler
    {
        private int _tempVarCounter, _callIdCounter, _labelCounter;
        private List<string> _instructions = new();
        private Dictionary<string, FunctionDeclaration> _functionDefs = new();
        private List<FunctionGenerationTask> _functionTasks = new();

        private class FunctionGenerationTask
        {
            public FunctionDeclaration Definition = null!;
            public int CopyId;
            public string ReturnLabel = "", ResultVar = "";
            public Dictionary<string, string> ArgMapping = new();
        }

        private string GetNewLabel() => $"@label_{_labelCounter++}";
        private string GetTempVar() => $"__t{_tempVarCounter++}";

        public string Compile(ProgramNode program)
        {
            _instructions.Clear(); _functionDefs.Clear(); _functionTasks.Clear();
            _tempVarCounter = _callIdCounter = _labelCounter = 0;

            foreach (var stmt in program.Statements)
                if (stmt is FunctionDeclaration funcDecl)
                    _functionDefs[funcDecl.Name] = funcDecl;

            foreach (var stmt in program.Statements.Where(s => s is not FunctionDeclaration))
                GenerateStatement(stmt);

            _instructions.Add("end");
            foreach (var task in _functionTasks) GenerateFunctionBody(task);
            return string.Join("\n", _instructions);
        }

        private void GenerateStatement(AstNode node)
        {
            switch (node)
            {
                case PrintStatement print: HandlePrint(print, _instructions, null); break;
                case IfStatement ifStmt: GenerateIf(ifStmt, _instructions, null); break;
                case WhileStatement whileStmt: GenerateWhile(whileStmt, _instructions, null); break;
                case ForStatement forStmt: GenerateFor(forStmt, _instructions, null); break;
<<<<<<< проект-обзор-и-анализ-4601b
                case ArrayDeclaration arrayDecl: GenerateArrayDeclaration(arrayDecl); break;
                case ArrayAssignmentStatement arrayAssign: GenerateArrayAssignment(arrayAssign); break;
=======
>>>>>>> master
                case Parser.ExpressionStatement exprStmt: GenerateExpression(exprStmt.Expr, _instructions, null); break;
                default: throw new Exception($"Unknown statement: {node.GetType()}");
            }
        }

        private void GenerateWhile(WhileStatement stmt, List<string> target, Dictionary<string, string>? map)
        {
            string start = GetNewLabel(), end = GetNewLabel();
            target.Add($"{start}:");
            string cond = GenerateExpression(stmt.Condition, target, map);
            target.Add($"jump {end} equal {cond} 0");
            foreach (var s in stmt.Body) GenerateStatementInternal(s, target, map);
            target.Add($"jump {start} always 0 0");
            target.Add($"{end}:");
        }

        private void GenerateFor(ForStatement stmt, List<string> target, Dictionary<string, string>? map)
        {
            if (stmt.Init != null) GenerateStatementInternal(stmt.Init, target, map);
            string start = GetNewLabel(), end = GetNewLabel();
            target.Add($"{start}:");
            if (stmt.Condition != null)
            {
                string cond = GenerateExpression(stmt.Condition, target, map);
                target.Add($"jump {end} equal {cond} 0");
            }
            foreach (var s in stmt.Body) GenerateStatementInternal(s, target, map);
            if (stmt.Update != null) GenerateStatementInternal(stmt.Update, target, map);
            target.Add($"jump {start} always 0 0");
            target.Add($"{end}:");
        }

        private void GenerateIf(IfStatement stmt, List<string> target, Dictionary<string, string>? map)
        {
            string elseLabel = GetNewLabel(), endLabel = GetNewLabel();
            string cond = GenerateExpression(stmt.Condition, target, map);
            target.Add($"jump {elseLabel} equal {cond} 0");
            foreach (var s in stmt.ThenBody) GenerateStatementInternal(s, target, map);
            if (stmt.ElseBody != null && stmt.ElseBody.Count > 0)
                target.Add($"jump {endLabel} always 0 0");
            target.Add($"{elseLabel}:");
            if (stmt.ElseBody != null && stmt.ElseBody.Count > 0)
                foreach (var s in stmt.ElseBody) GenerateStatementInternal(s, target, map);
            target.Add($"{endLabel}:");
        }

        private void GenerateStatementInternal(AstNode node, List<string> target, Dictionary<string, string>? map)
        {
            switch (node)
            {
                case PrintStatement print: HandlePrint(print, target, map); break;
                case IfStatement ifStmt: GenerateIf(ifStmt, target, map); break;
                case WhileStatement whileStmt: GenerateWhile(whileStmt, target, map); break;
                case ForStatement forStmt: GenerateFor(forStmt, target, map); break;
<<<<<<< проект-обзор-и-анализ-4601b
                case ArrayDeclaration arrayDecl: GenerateArrayDeclaration(arrayDecl); break;
                case ArrayAssignmentStatement arrayAssign: GenerateArrayAssignment(arrayAssign); break;
=======
>>>>>>> master
                case Parser.ExpressionStatement exprStmt: GenerateExpression(exprStmt.Expr, target, map); break;
                case AsmBlockStatement asm:
                    foreach (var line in asm.RawCode.Split(new[] { ';', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                        if (!string.IsNullOrWhiteSpace(line)) target.Add(line.Trim());
                    break;
                case ReturnStatement ret:
                    throw new Exception("Return inside control flow is complex. Keep it at function top level.");
                default: throw new Exception($"Unknown statement: {node.GetType()}");
            }
        }

        private void HandlePrint(PrintStatement print, List<string> target, Dictionary<string, string>? map)
        {
            string value = GenerateExpression(print.Value, target, map);
            string dest = print.Destination != null ? GenerateExpression(print.Destination, target, map) : "message1";
            target.Add($"print {value}");
            target.Add($"printflush {dest}");
        }

        private string GenerateExpression(Expression expr, List<string> target, Dictionary<string, string>? map)
        {
            map ??= new Dictionary<string, string>();
            return expr switch
            {
                NumberLiteral num => num.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                StringLiteral str => $"\"{str.Value}\"",
                VariableReference vr => map.ContainsKey(vr.Name) ? map[vr.Name] : vr.Name,
<<<<<<< проект-обзор-и-анализ-4601b
                ArrayAccessExpression arrAccess => HandleArrayAccess(arrAccess, target, map),
=======
>>>>>>> master
                Assignment assign => HandleAssignment(assign, target, map),
                BinaryOperation bin when IsComparison(bin.Operator) => GenerateComparison(bin, target, map),
                BinaryOperation bin => HandleArithmetic(bin, target, map),
                FunctionCall call => GenerateFunctionCall(call),
                _ => throw new Exception($"Unknown expression: {expr.GetType()}")
            };
        }

        private string HandleAssignment(Assignment assign, List<string> target, Dictionary<string, string> map)
        {
            string val = GenerateExpression(assign.Value, target, map);
            target.Add($"set {assign.VariableName} {val}");
            return assign.VariableName;
        }
<<<<<<< проект-обзор-и-анализ-4601b

        private void GenerateArrayDeclaration(ArrayDeclaration arrayDecl)
        {
            // Array declaration is a no-op at runtime; memory cell is assumed to exist.
            // The syntax is: array name(size, cell);
            // We don't generate any code here, just validate.
        }

        private void GenerateArrayAssignment(ArrayAssignmentStatement arrayAssign)
        {
            string index = GenerateExpression(arrayAssign.Index, _instructions, null);
            string value = GenerateExpression(arrayAssign.Value, _instructions, null);
            _instructions.Add($"write {value} {arrayAssign.ArrayName} {index}");
        }

        private string HandleArrayAccess(ArrayAccessExpression arrAccess, List<string> target, Dictionary<string, string> map)
        {
            string index = GenerateExpression(arrAccess.Index, target, map);
            string resultVar = GetTempVar();
            target.Add($"read {resultVar} {arrAccess.ArrayName} {index}");
            return resultVar;
        }
=======
>>>>>>> master

        private string HandleArithmetic(BinaryOperation bin, List<string> target, Dictionary<string, string> map)
        {
            string left = GenerateExpression(bin.Left, target, map);
            string right = GenerateExpression(bin.Right, target, map);
            string temp = GetTempVar();
            string op = bin.Operator switch { "+" => "add", "-" => "sub", "*" => "mul", "/" => "div", _ => throw new Exception($"Unknown operator: {bin.Operator}") };
            target.Add($"op {op} {temp} {left} {right}");
            return temp;
        }

        private bool IsComparison(string op) => op is "==" or "!=" or "<" or ">" or "<=" or ">=";

        private string GenerateComparison(BinaryOperation bin, List<string> target, Dictionary<string, string> map)
        {
            string left = GenerateExpression(bin.Left, target, map);
            string right = GenerateExpression(bin.Right, target, map);
            string mlogOp = bin.Operator switch { "==" => "equal", "!=" => "notEqual", "<" => "lessThan", ">" => "greaterThan", "<=" => "lessThanEq", ">=" => "greaterThanEq", _ => throw new Exception($"Unknown comparison: {bin.Operator}") };
            string result = GetTempVar();
            string labelTrue = GetNewLabel(), labelEnd = GetNewLabel();
            target.Add($"set {result} 0");
            target.Add($"jump {labelTrue} {mlogOp} {left} {right}");
            target.Add($"jump {labelEnd} always 0 0");
            target.Add($"{labelTrue}:");
            target.Add($"set {result} 1");
            target.Add($"{labelEnd}:");
            return result;
        }

        private string GenerateFunctionCall(FunctionCall call)
        {
            if (!_functionDefs.TryGetValue(call.FunctionName, out var funcDef))
                throw new Exception($"Function {call.FunctionName} not defined");

            int copyId = _callIdCounter++;
            string funcLabel = $"@{call.FunctionName}_copy{copyId}";
            string returnLabel = $"@return_{call.FunctionName}_copy{copyId}";
            string resultVar = $"__result_{call.FunctionName}_copy{copyId}";
            var argMapping = new Dictionary<string, string>();

            for (int i = 0; i < call.Arguments.Count; i++)
            {
                string argVal = GenerateExpression(call.Arguments[i], _instructions, null);
                string mlogArg = $"__arg_{call.FunctionName}_copy{copyId}_{i}";
                _instructions.Add($"set {mlogArg} {argVal}");
                if (i < funcDef.Parameters.Count) argMapping[funcDef.Parameters[i]] = mlogArg;
            }

            _instructions.Add($"jump {funcLabel} always 0 0");
            _instructions.Add($"{returnLabel}:");
            _functionTasks.Add(new FunctionGenerationTask { Definition = funcDef, CopyId = copyId, ReturnLabel = returnLabel, ResultVar = resultVar, ArgMapping = argMapping });
            return resultVar;
        }

        private void GenerateFunctionBody(FunctionGenerationTask task)
        {
            _instructions.Add($"@{task.Definition.Name}_copy{task.CopyId}:");
            foreach (var stmt in task.Definition.Body)
            {
                if (stmt is ReturnStatement ret)
                {
                    string val = GenerateExpression(ret.Value, _instructions, task.ArgMapping);
                    _instructions.Add($"set {task.ResultVar} {val}");
                    _instructions.Add($"jump {task.ReturnLabel} always 0 0");
                }
                else if (stmt is PrintStatement print) HandlePrint(print, _instructions, task.ArgMapping);
                else if (stmt is Parser.ExpressionStatement expr) GenerateExpression(expr.Expr, _instructions, task.ArgMapping);
                else throw new Exception($"Unsupported statement in function: {stmt.GetType()}");
            }
        }
    }
}
