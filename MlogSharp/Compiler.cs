using System;
using System.Collections.Generic;
using System.Linq;

namespace MlogSharp
{
    public class Compiler
    {
        private int _tempVarCounter = 0;
        private int _callIdCounter = 0;
        private List<string> _instructions;
        private Dictionary<string, FunctionDeclaration> _functionDefs;
        private List<FunctionGenerationTask> _functionTasks;

        private class FunctionGenerationTask
        {
            public FunctionDeclaration Definition;
            public int CopyId;
            public string ReturnLabel;
            public string ResultVar;
            public Dictionary<string, string> ArgMapping;
        }

        private int _labelCounter = 0;

        private string GetNewLabel()
        {
            return $"@label_{_labelCounter++}";
        }

        public string Compile(ProgramNode program)
        {
            _instructions = new List<string>();
            _functionDefs = new Dictionary<string, FunctionDeclaration>();
            _functionTasks = new List<FunctionGenerationTask>();
            _tempVarCounter = 0;
            _callIdCounter = 0;

            foreach (var stmt in program.Statements)
            {
                if (stmt is FunctionDeclaration funcDecl)
                {
                    if (_functionDefs.ContainsKey(funcDecl.Name))
                        throw new Exception($"Duplicate function definition: {funcDecl.Name}");
                    _functionDefs[funcDecl.Name] = funcDecl;
                }
            }

            var globalStatements = program.Statements.Where(s => s is not FunctionDeclaration).ToList();

            foreach (var stmt in globalStatements)
            {
                GenerateStatement(stmt);
            }

            _instructions.Add("end");

            foreach (var task in _functionTasks)
            {
                GenerateFunctionBody(task);
            }

            return string.Join("\n", _instructions);
        }

        private void GenerateStatement(AstNode node)
        {
            switch (node)
            {
                case PrintStatement print: HandlePrint(print, _instructions, null); break;
                case IfStatement ifStmt: GenerateIfStatement(ifStmt, _instructions, null); break;
                case WhileStatement whileStmt: GenerateWhileStatement(whileStmt, _instructions, null); break;
                case ForStatement forStmt: GenerateForStatement(forStmt, _instructions, null); break;
                case Parser.ExpressionStatement exprStmt: GenerateExpression(exprStmt.Expr, _instructions, null); break;
                default: throw new Exception($"Unknown statement: {node.GetType()}");
            }
        }

        private void GenerateWhileStatement(WhileStatement whileStmt, List<string> targetLines, Dictionary<string, string>? paramMapping)
        {
            string startLabel = GetNewLabel();
            string endLabel = GetNewLabel();

            // Метка начала цикла (проверка условия)
            targetLines.Add($"{startLabel}:");

            // Вычисляем условие
            string condVal = GenerateExpression(whileStmt.Condition, targetLines, paramMapping);

            // Если условие ложно (0), выходим
            targetLines.Add($"jump {endLabel} equal {condVal} 0");

            // Тело цикла
            foreach (var stmt in whileStmt.Body)
            {
                GenerateStatementInternal(stmt, targetLines, paramMapping);
            }

            // Прыжок назад к проверке
            targetLines.Add($"jump {startLabel} always 0 0");

            // Метка конца
            targetLines.Add($"{endLabel}:");
        }

        private void GenerateForStatement(ForStatement forStmt, List<string> targetLines, Dictionary<string, string>? paramMapping)
        {
            // For превращаем в While:
            // init;
            // while (condition) {
            //    body;
            //    update;
            // }

            // 1. Init
            if (forStmt.Init != null)
            {
                GenerateStatementInternal(forStmt.Init, targetLines, paramMapping);
            }

            string startLabel = GetNewLabel();
            string endLabel = GetNewLabel();

            targetLines.Add($"{startLabel}:");

            // 2. Condition
            if (forStmt.Condition != null)
            {
                string condVal = GenerateExpression(forStmt.Condition, targetLines, paramMapping);
                targetLines.Add($"jump {endLabel} equal {condVal} 0");
            }
            // Если условия нет, это бесконечный цикл (пока не будет break)

            // 3. Body
            foreach (var stmt in forStmt.Body)
            {
                GenerateStatementInternal(stmt, targetLines, paramMapping);
            }

            // 4. Update
            if (forStmt.Update != null)
            {
                GenerateStatementInternal(forStmt.Update, targetLines, paramMapping);
            }

            // Jump back
            targetLines.Add($"jump {startLabel} always 0 0");

            targetLines.Add($"{endLabel}:");
        }

        private void GenerateIfStatement(IfStatement ifStmt, List<string> targetLines, Dictionary<string, string>? paramMapping)
        {
            string elseLabel = GetNewLabel();
            string endLabel = GetNewLabel();

            // 1. Вычисляем условие
            // Нам нужно сгенерировать код условия и получить имя переменной/значение, которое хранит результат (0 или не 0)
            // Но в Mlog jump проверяет условие сам.
            // Пример: jump @elseLabel equal x 0 (если x == 0, то условие ложно)

            // Проблема: наше условие - это выражение, например, a > b.
            // GenerateExpression вернет нам временную переменную с результатом?
            // Нет, BinaryOperation возвращает результат арифметики.
            // Для сравнений нам нужны отдельные операторы: equal, notEqual, lessThan, etc.

            // Давай расширим BinaryOperation или добавим новый тип ComparisonExpression?
            // Для простоты, давай считать, что условие - это выражение, которое должно быть ИСТИННЫМ (не 0).
            // Но Mlog jump требует конкретный оператор сравнения.

            // Решение:
            // Если условие - это BinaryOperation с оператором сравнения (>, <, ==, !=), мы обрабатываем его специально.
            // Если условие - просто переменная или число, мы считаем, что оно истинно, если не 0.

            // Давай добавим поддержку операторов сравнения в BinaryOperation и Compiler.

            string condVal = GenerateExpression(ifStmt.Condition, targetLines, paramMapping);

            // Теперь у нас есть значение условия в condVal.
            // В Mlog: jump label operator var value
            // Мы хотим прыгнуть на else, если условие ЛОЖНО.
            // Условие истинно, если condVal != 0.
            // Значит, прыгаем на else, если condVal == 0.

            targetLines.Add($"jump {elseLabel} equal {condVal} 0");

            // 2. Генерируем блок THEN
            foreach (var stmt in ifStmt.ThenBody)
            {
                // Рекурсивно генерируем инструкции. 
                // Важно: если внутри есть return, он должен работать корректно.
                // Но у нас GenerateStatement пишет в targetLines.
                // Если мы внутри функции, targetLines это _instructions.
                GenerateStatementInternal(stmt, targetLines, paramMapping);
            }

            // Если есть else, нужно перепрыгнуть его после выполнения if
            if (ifStmt.ElseBody != null && ifStmt.ElseBody.Count > 0)
            {
                targetLines.Add($"jump {endLabel} always 0 0");
            }

            // 3. Метка else
            targetLines.Add($"{elseLabel}:");

            // 4. Генерируем блок ELSE
            if (ifStmt.ElseBody != null && ifStmt.ElseBody.Count > 0)
            {
                foreach (var stmt in ifStmt.ElseBody)
                {
                    GenerateStatementInternal(stmt, targetLines, paramMapping);
                }
            }

            // 5. Метка конца
            targetLines.Add($"{endLabel}:");
        }

        // Вспомогательный метод, чтобы не дублировать код switch из GenerateStatement
        private void GenerateStatementInternal(AstNode node, List<string> targetLines, Dictionary<string, string>? paramMapping)
        {
            switch (node)
            {
                case PrintStatement print: HandlePrint(print, targetLines, paramMapping); break;
                case IfStatement ifStmt: GenerateIfStatement(ifStmt, targetLines, paramMapping); break;
                case WhileStatement whileStmt: GenerateWhileStatement(whileStmt, targetLines, paramMapping); break;
                case ForStatement forStmt: GenerateForStatement(forStmt, targetLines, paramMapping); break;
                case Parser.ExpressionStatement exprStmt: GenerateExpression(exprStmt.Expr, targetLines, paramMapping); break;
                case AsmBlockStatement asm:
                    GenerateAsm(asm, targetLines);
                    break;
                case ReturnStatement ret:
                    // Пока не поддерживаем return внутри циклов/if глубоко
                    throw new Exception("Return inside control flow is complex. Keep it at function top level for now.");
                default: throw new Exception($"Unknown statement: {node.GetType()}");
            }
        }

        private void GenerateAsm(AsmBlockStatement asm, List<string> targetLines)
        {
            // Разбиваем сырой код по точкам с запятой или новым строкам, если нужно.
            // Но Mlog требует одну команду на строку.
            // Если пользователь написал: "sensor r b @copper; draw clear..."
            // Нам нужно разбить это на отдельные строки.

            string[] lines = asm.RawCode.Split(new[] { ';', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    targetLines.Add(trimmed);
                }
            }
        }

        // Новый метод для обработки печати, чтобы не дублировать код
        private void HandlePrint(PrintStatement print, List<string> targetLines, Dictionary<string, string>? paramMapping)
        {
            // Генерируем код для значения
            string valueStr = GenerateExpression(print.Value, targetLines, paramMapping);

            // Определяем адресат
            string destination = "message1"; // По умолчанию
            if (print.Destination != null)
            {
                // Если адресат задан, вычисляем его. 
                // Обычно это имя переменной или строка-идентификатор блока
                destination = GenerateExpression(print.Destination, targetLines, paramMapping);
            }

            targetLines.Add($"print {valueStr}");
            targetLines.Add($"printflush {destination}");
        }

        private string GenerateExpression(Expression expr, List<string> targetLines, Dictionary<string, string>? paramMapping)
        {
            if (paramMapping == null) paramMapping = new Dictionary<string, string>();

            switch (expr)
            {
                case NumberLiteral num:
                    return num.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);

                case StringLiteral str:
                    return $"\"{str.Value}\"";

                case VariableReference vr:
                    if (paramMapping.ContainsKey(vr.Name))
                        return paramMapping[vr.Name];
                    return vr.Name;

                case Assignment assign:
                    // 1. Вычисляем значение правой части
                    string val = GenerateExpression(assign.Value, targetLines, paramMapping);

                    // 2. Определяем имя переменной слева
                    // Если мы внутри функции, нужно проверить, не является ли это локальным параметром?
                    // Нет, параметры только читаются. Присваивание создает/меняет глобальную переменную
                    // или локальную, если бы мы поддерживали локальные переменные.
                    // Пока считаем все переменные глобальными.

                    string varName = assign.VariableName;

                    // 3. Генерируем set
                    targetLines.Add($"set {varName} {val}");

                    // 4. Возвращаем имя переменной (так как присваивание - это выражение)
                    return varName;

                case BinaryOperation binOp:
                    // Проверяем, является ли это операцией сравнения
                    if (IsComparisonOperator(binOp.Operator))
                    {
                        return GenerateComparison(binOp, targetLines, paramMapping);
                    }

                    // Обычная арифметика
                    string left = GenerateExpression(binOp.Left, targetLines, paramMapping);
                    string right = GenerateExpression(binOp.Right, targetLines, paramMapping);
                    string temp = GetTempVar();

                    string opCode = binOp.Operator switch
                    {
                        "+" => "add",
                        "-" => "sub",
                        "*" => "mul",
                        "/" => "div",
                        _ => throw new Exception($"Unknown arithmetic operator: {binOp.Operator}")
                    };

                    targetLines.Add($"op {opCode} {temp} {left} {right}");
                    return temp;

                case FunctionCall call:
                    return GenerateFunctionCall(call);



                default:
                    throw new Exception($"Unknown expression: {expr.GetType()}");
            }
        }

        private string GenerateFunctionCall(FunctionCall call)
        {
            string funcName = call.FunctionName;

            if (!_functionDefs.ContainsKey(funcName))
                throw new Exception($"Function {funcName} not defined");

            var funcDef = _functionDefs[funcName];

            int copyId = _callIdCounter++;

            string funcLabel = $"@{funcName}_copy{copyId}";
            string returnLabel = $"@return_{funcName}_copy{copyId}";
            string resultVar = $"__result_{funcName}_copy{copyId}";

            var argMapping = new Dictionary<string, string>();

            for (int i = 0; i < call.Arguments.Count; i++)
            {
                string argVal = GenerateExpression(call.Arguments[i], _instructions, null);

                string mlogArgName = $"__arg_{funcName}_copy{copyId}_{i}";

                _instructions.Add($"set {mlogArgName} {argVal}");

                if (i < funcDef.Parameters.Count)
                {
                    argMapping[funcDef.Parameters[i]] = mlogArgName;
                }
            }

            _instructions.Add($"jump {funcLabel} always 0 0");
            _instructions.Add($"{returnLabel}:");

            _functionTasks.Add(new FunctionGenerationTask
            {
                Definition = funcDef,
                CopyId = copyId,
                ReturnLabel = returnLabel,
                ResultVar = resultVar,
                ArgMapping = argMapping
            });

            return resultVar;
        }

        private void GenerateFunctionBody(FunctionGenerationTask task)
        {
            string funcLabel = $"@{task.Definition.Name}_copy{task.CopyId}";

            _instructions.Add($"{funcLabel}:");

            foreach (var stmt in task.Definition.Body)
            {
                if (stmt is ReturnStatement ret)
                {
                    string val = GenerateExpression(ret.Value, _instructions, task.ArgMapping);
                    _instructions.Add($"set {task.ResultVar} {val}");
                    _instructions.Add($"jump {task.ReturnLabel} always 0 0");
                }
                else if (stmt is PrintStatement print)
                {
                    // Используем новый универсальный метод HandlePrint
                    HandlePrint(print, _instructions, task.ArgMapping);
                }
                else if (stmt is Parser.ExpressionStatement exprStmt)
                {
                    GenerateExpression(exprStmt.Expr, _instructions, task.ArgMapping);
                }
                else
                {
                    throw new Exception($"Unsupported statement in function: {stmt.GetType()}");
                }
            }
        }

        private bool IsComparisonOperator(string op)
        {
            return op == "==" || op == "!=" || op == "<" || op == ">" || op == "<=" || op == ">=";
        }

        private string GenerateComparison(BinaryOperation binOp, List<string> targetLines, Dictionary<string, string>? paramMapping)
        {
            string left = GenerateExpression(binOp.Left, targetLines, paramMapping);
            string right = GenerateExpression(binOp.Right, targetLines, paramMapping);

            string mlogOp = binOp.Operator switch
            {
                "==" => "equal",
                "!=" => "notEqual",
                "<" => "lessThan",
                ">" => "greaterThan",
                "<=" => "lessThanEq",
                ">=" => "greaterThanEq",
                _ => throw new Exception($"Unknown comparison operator: {binOp.Operator}")
            };

            string resultVar = GetTempVar();
            string labelTrue = GetNewLabel();
            string labelEnd = GetNewLabel();

            // По умолчанию результат 0 (ложь)
            targetLines.Add($"set {resultVar} 0");

            // Если условие истинно, прыгаем на установку 1
            targetLines.Add($"jump {labelTrue} {mlogOp} {left} {right}");

            // Если сюда попали, значит условие ложно. Прыгаем в конец.
            targetLines.Add($"jump {labelEnd} always 0 0");

            // Метка истины
            targetLines.Add($"{labelTrue}:");
            targetLines.Add($"set {resultVar} 1");

            // Конец
            targetLines.Add($"{labelEnd}:");

            return resultVar;
        }

        private string GetTempVar()
        {
            return $"__t{_tempVarCounter++}";
        }
    }
}