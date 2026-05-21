using SQL_ConsoleApp.Files;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace SQL_ConsoleApp.Commands
{
    /// <summary>Операторы сравнения в WHERE-условиях.</summary>
    public enum CompareOperator
    {
        Equal, NotEqual, LessThan, GreaterThan, LessOrEqual, GreaterOrEqual
    }

    /// <summary>Логические операторы в WHERE-условиях.</summary>
    public enum LogicalOperator { And, Or, Xor, Not }

    /// <summary>
    /// Представляет элементарное выражение сравнения: поле оператор значение.
    /// </summary>
    public struct ElementaryExpression
    {
        public string RowName;
        public CompareOperator CompareOperator;
        public string Value;

        /// <summary>
        /// Создаёт элементарное выражение из имени поля, строки оператора и значения.
        /// </summary>
        public ElementaryExpression(string rowName, string op, string value)
        {
            RowName = rowName;
            CompareOperator = ParseOperator(op);
            Value = value;
        }

        /// <summary>Устанавливает новое значение выражения (используется для подстановки).</summary>
        public void SetValue(string value) => Value = value;

        /// <summary>Преобразует строковое представление оператора в перечисление.</summary>
        private static CompareOperator ParseOperator(string op) => op switch
        {
            "=" => CompareOperator.Equal,
            "<>" => CompareOperator.NotEqual,
            "<" => CompareOperator.LessThan,
            ">" => CompareOperator.GreaterThan,
            "<=" => CompareOperator.LessOrEqual,
            ">=" => CompareOperator.GreaterOrEqual,
            _ => throw new ArgumentException($"Неизвестный оператор: {op}")
        };
    }

    /// <summary>
    /// Узел дерева логического выражения. Может быть элементарным сравнением
    /// или логической операцией над поддеревьями.
    /// </summary>
    public class LogicalExpressionNode
    {
        public LogicalOperator? Operator { get; set; }
        public ElementaryExpression? ElementaryExpression { get; set; }
        public LogicalExpressionNode Left { get; set; }
        public LogicalExpressionNode Right { get; set; }
        public bool IsElementary => ElementaryExpression.HasValue;
        public bool IsNot => Operator == LogicalOperator.Not;
    }

    /// <summary>
    /// Парсер и вычислитель логических выражений для WHERE-условий.
    /// Поддерживает операторы AND, OR, XOR, NOT и сравнения =, &lt;&gt;, &lt;, &gt;, &lt;=, &gt;=.
    /// </summary>
    public class LogicExpressionParser
    {
        private const string DatePattern = @"\d{2}[.,\\\/\-]\d{2}[.,\\\/\-]\d{4}|\d{4}[.,\\\/\-]\d{2}[.,\\\/\-]\d{2}";
        private const string NumberPattern = @"\d+(?:\.\d+)?";
        private const string StringPattern = @"""[^""]*""";
        private const string WordPattern = @"\w+";
        private const string OpPattern = @"[=<>]+";
        private const string LogicalPattern = @"AND|OR|XOR|NOT|TRUE|NULL|FALSE";

        private static readonly Regex TokenPattern = new(
            $@"({LogicalPattern}|\(|\)|{StringPattern}|{DatePattern}|{NumberPattern}|{WordPattern}|{OpPattern})",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex ExpressionPattern = new(
            $@"^\s*(?<field>\w+)\s*(?<op>=|<>|<|>|<=|>=)\s*(?<value>{DatePattern}|{NumberPattern}|TRUE|NULL|FALSE|{StringPattern})\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Dictionary<string, int> Precedence = new()
        {
            ["NOT"] = 3,
            ["AND"] = 2,
            ["XOR"] = 1,
            ["OR"] = 1
        };

        private static readonly HashSet<string> LogicalOperators = new(
            new[] { "AND", "OR", "XOR", "NOT" }, StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> ComparisonOperators = new(
            new[] { "=", "<>", "<", ">", "<=", ">=" });

        private readonly List<ElementaryExpression> _expressions;

        /// <summary>Корень дерева логического выражения.</summary>
        public LogicalExpressionNode Root { get; }

        /// <summary>
        /// Создаёт парсер и сразу разбирает переданное логическое выражение.
        /// </summary>
        /// <param name="expression">Строка WHERE-условия (без ключевого слова WHERE).</param>
        public LogicExpressionParser(string expression)
        {
            _expressions = new List<ElementaryExpression>();
            Root = Parse(expression);
        }

        /// <summary>Возвращает все элементарные выражения, найденные в условии.</summary>
        public List<ElementaryExpression> GetAllExpressions() => _expressions;

        /// <summary>
        /// Вычисляет логическое выражение для переданной строки данных.
        /// </summary>
        /// <param name="row">Словарь значений полей записи: имя → (значение, тип).</param>
        /// <param name="dbtManager">Менеджер MEMO-полей (опционально).</param>
        /// <returns>Результат вычисления условия.</returns>
        public bool Evaluate(Dictionary<string, (object Value, char Type)> row, DbtManager dbtManager = null) =>
            EvaluateNode(Root, row, dbtManager);

        // ──────────────────────────────── Парсинг ────────────────────────────────

        private LogicalExpressionNode Parse(string expression)
        {
            var tokens = Tokenize(expression.Trim());

            if (!AreParenthesesBalanced(tokens))
                throw new Exception("Несбалансированные скобки в логическом выражении");

            return BuildTree(ConvertToRPN(tokens));
        }

        /// <summary>Разбивает строку выражения на токены.</summary>
        private static List<string> Tokenize(string expression)
        {
            var tokens = new List<string>();

            foreach (Match match in TokenPattern.Matches(expression))
            {
                string token = match.Value;

                if (LogicalOperators.Contains(token))
                    tokens.Add(token.ToUpperInvariant());
                else if (token is "(" or ")")
                    tokens.Add(token);
                else if (token.StartsWith("\"") || ComparisonOperators.Contains(token))
                    tokens.Add(token);
                else
                    tokens.Add(token.ToUpperInvariant());
            }

            return tokens;
        }

        /// <summary>Проверяет баланс скобок в списке токенов.</summary>
        private static bool AreParenthesesBalanced(List<string> tokens)
        {
            int balance = 0;
            foreach (string token in tokens)
            {
                if (token == "(") balance++;
                else if (token == ")") balance--;
                if (balance < 0) return false;
            }
            return balance == 0;
        }

        /// <summary>Преобразует токены в обратную польскую нотацию (RPN) по алгоритму сортировочной станции.</summary>
        private List<string> ConvertToRPN(List<string> tokens)
        {
            var output = new List<string>();
            var operators = new Stack<string>();

            for (int i = 0; i < tokens.Count; i++)
            {
                string token = tokens[i];

                if (token == "(")
                {
                    operators.Push(token);
                }
                else if (token == ")")
                {
                    while (operators.Count > 0 && operators.Peek() != "(")
                        output.Add(operators.Pop());
                    operators.Pop(); // удаляем '('
                }
                else if (Precedence.ContainsKey(token))
                {
                    if (token == "NOT")
                    {
                        operators.Push(token);
                    }
                    else
                    {
                        while (operators.Count > 0 && operators.Peek() != "(" &&
                               Precedence[operators.Peek()] >= Precedence[token])
                            output.Add(operators.Pop());
                        operators.Push(token);
                    }
                }
                else
                {
                    // Собираем полное выражение: поле оператор значение
                    string fullExpr = CollectFullExpression(tokens, ref i);
                    ValidateAndRegisterExpression(fullExpr);
                    output.Add(fullExpr);
                }
            }

            while (operators.Count > 0)
                output.Add(operators.Pop());

            return output;
        }

        /// <summary>Собирает трёхтокенное выражение: поле + оператор + значение.</summary>
        private static string CollectFullExpression(List<string> tokens, ref int index)
        {
            string expr = tokens[index];

            if (index + 2 < tokens.Count && ComparisonOperators.Contains(tokens[index + 1]))
            {
                expr += tokens[index + 1] + tokens[index + 2];
                index += 2;
            }

            return expr;
        }

        /// <summary>Проверяет и регистрирует элементарное выражение.</summary>
        private void ValidateAndRegisterExpression(string expr)
        {
            Match match = ExpressionPattern.Match(expr);
            if (!match.Success)
                throw new Exception($"Неверное выражение: {expr}");

            var elementary = new ElementaryExpression(
                match.Groups["field"].Value,
                match.Groups["op"].Value,
                match.Groups["value"].Value);

            _expressions.Add(elementary);
        }

        /// <summary>Строит дерево логического выражения из RPN.</summary>
        private LogicalExpressionNode BuildTree(List<string> rpn)
        {
            var stack = new Stack<LogicalExpressionNode>();

            foreach (string token in rpn)
            {
                if (Precedence.TryGetValue(token, out _))
                {
                    if (token == "NOT")
                    {
                        stack.Push(new LogicalExpressionNode
                        {
                            Operator = LogicalOperator.Not,
                            Left = stack.Pop()
                        });
                    }
                    else
                    {
                        var right = stack.Pop();
                        var left = stack.Pop();
                        stack.Push(new LogicalExpressionNode
                        {
                            Operator = ParseLogicalOperator(token),
                            Left = left,
                            Right = right
                        });
                    }
                }
                else
                {
                    var expr = FindExpressionByToken(token);
                    stack.Push(new LogicalExpressionNode { ElementaryExpression = expr });
                }
            }

            if (stack.Count != 1)
                throw new Exception("Некорректное логическое выражение");

            return stack.Pop();
        }

        private static LogicalOperator ParseLogicalOperator(string token) => token switch
        {
            "AND" => LogicalOperator.And,
            "OR" => LogicalOperator.Or,
            "XOR" => LogicalOperator.Xor,
            _ => throw new Exception($"Неизвестный логический оператор: {token}")
        };

        /// <summary>Находит зарегистрированное выражение по токену (с пробелом или без).</summary>
        private ElementaryExpression FindExpressionByToken(string token) =>
            _expressions.First(e =>
                $"{e.RowName}{GetOperatorSymbol(e.CompareOperator)}{e.Value}" == token ||
                $"{e.RowName} {GetOperatorSymbol(e.CompareOperator)} {e.Value}" == token);

        private static string GetOperatorSymbol(CompareOperator op) => op switch
        {
            CompareOperator.Equal => "=",
            CompareOperator.NotEqual => "<>",
            CompareOperator.LessThan => "<",
            CompareOperator.GreaterThan => ">",
            CompareOperator.LessOrEqual => "<=",
            CompareOperator.GreaterOrEqual => ">=",
            _ => "="
        };

        // ──────────────────────────────── Вычисление ────────────────────────────────

        private bool EvaluateNode(LogicalExpressionNode node,
            Dictionary<string, (object Value, char Type)> row, DbtManager dbtManager)
        {
            if (node.IsElementary)
                return EvaluateElementary(node.ElementaryExpression.Value, row, dbtManager);

            bool leftVal = EvaluateNode(node.Left, row, dbtManager);
            if (node.IsNot) return !leftVal;

            bool rightVal = EvaluateNode(node.Right, row, dbtManager);
            return node.Operator switch
            {
                LogicalOperator.And => leftVal && rightVal,
                LogicalOperator.Or => leftVal || rightVal,
                LogicalOperator.Xor => leftVal != rightVal,
                _ => false
            };
        }

        private static bool EvaluateElementary(ElementaryExpression expr,
            Dictionary<string, (object Value, char Type)> row, DbtManager dbtManager)
        {
            if (!row.TryGetValue(expr.RowName, out var field))
                throw new Exception($"Поле '{expr.RowName}' не найдено");

            object fieldValue = field.Value;
            char fieldType = field.Type;

            if (fieldType == 'M' && dbtManager != null)
                fieldValue = dbtManager.GetText(fieldValue?.ToString() ?? "");

            object compareValue = ParseValue(expr.Value, fieldType);
            return CompareTypedValues(fieldValue, compareValue, fieldType, expr.CompareOperator);
        }

        private static bool CompareTypedValues(object fieldValue, object compareValue,
            char type, CompareOperator op) => type switch
            {
                'N' => CompareNumeric(fieldValue, compareValue, op),
                'L' => CompareLogical(fieldValue, compareValue, op),
                'D' => CompareDates(fieldValue, compareValue, op),
                _ => CompareStrings(fieldValue, compareValue, op)  // C, M и по умолчанию
            };

        private static bool CompareNumeric(object fieldValue, object compareValue, CompareOperator op)
        {
            double num1 = ToDouble(fieldValue);
            double num2 = ToDouble(compareValue);

            return op switch
            {
                CompareOperator.Equal => num1 == num2,
                CompareOperator.NotEqual => num1 != num2,
                CompareOperator.LessThan => num1 < num2,
                CompareOperator.GreaterThan => num1 > num2,
                CompareOperator.LessOrEqual => num1 <= num2,
                CompareOperator.GreaterOrEqual => num1 >= num2,
                _ => false
            };
        }

        private static bool CompareLogical(object fieldValue, object compareValue, CompareOperator op)
        {
            bool bool1 = ToBool(fieldValue);
            bool bool2 = ToBool(compareValue);

            return op switch
            {
                CompareOperator.Equal => bool1 == bool2,
                CompareOperator.NotEqual => bool1 != bool2,
                CompareOperator.LessThan => !bool1 && bool2,
                CompareOperator.GreaterThan => bool1 && !bool2,
                CompareOperator.LessOrEqual => !bool1 || bool2,
                CompareOperator.GreaterOrEqual => bool1 || !bool2,
                _ => false
            };
        }

        private static bool CompareDates(object fieldValue, object compareValue, CompareOperator op)
        {
            DateTime? date1 = ToDateTime(fieldValue);
            DateTime? date2 = ToDateTime(compareValue);

            return op switch
            {
                CompareOperator.Equal => date1 == date2,
                CompareOperator.NotEqual => date1 != date2,
                CompareOperator.LessThan => date1 < date2,
                CompareOperator.GreaterThan => date1 > date2,
                CompareOperator.LessOrEqual => date1 <= date2,
                CompareOperator.GreaterOrEqual => date1 >= date2,
                _ => false
            };
        }

        private static bool CompareStrings(object fieldValue, object compareValue, CompareOperator op)
        {
            string str1 = fieldValue?.ToString()?.Trim() ?? "";
            string str2 = compareValue?.ToString()?.Trim() ?? "";

            return op switch
            {
                CompareOperator.Equal => string.Equals(str1, str2, StringComparison.OrdinalIgnoreCase),
                CompareOperator.NotEqual => !string.Equals(str1, str2, StringComparison.OrdinalIgnoreCase),
                CompareOperator.LessThan => string.Compare(str1, str2, StringComparison.OrdinalIgnoreCase) < 0,
                CompareOperator.GreaterThan => string.Compare(str1, str2, StringComparison.OrdinalIgnoreCase) > 0,
                CompareOperator.LessOrEqual => string.Compare(str1, str2, StringComparison.OrdinalIgnoreCase) <= 0,
                CompareOperator.GreaterOrEqual => string.Compare(str1, str2, StringComparison.OrdinalIgnoreCase) >= 0,
                _ => false
            };
        }

        // ──────────────────────────────── Конвертация типов ────────────────────────────────

        private static double ToDouble(object value) =>
            value is double d ? d :
            double.TryParse(value?.ToString()?.Replace('.', ',').Trim(), out double parsed) ? parsed : 0.0;

        private static bool ToBool(object value) =>
            value is bool b ? b :
            value?.ToString()?.Trim().ToUpperInvariant() is "T" or "TRUE";

        private static DateTime? ToDateTime(object value) =>
            value is DateTime dt ? dt :
            DateTime.TryParseExact(value?.ToString()?.Trim(), "yyyyMMdd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed) ? parsed : null;

        /// <summary>Преобразует строковое значение из WHERE в типизированное для сравнения.</summary>
        private static object ParseValue(string value, char type)
        {
            string upper = value.ToUpperInvariant();

            if (type == 'L')
            {
                if (upper is "TRUE" or "T" or "Y") return true;
                if (upper is "FALSE" or "F" or "N") return false;
                if (upper is "NULL" or "?") return false;
            }

            if (upper == "NULL")
                return "";

            if (type == 'D' && DateTime.TryParse(value, out DateTime date))
                return date.ToString("yyyyMMdd");

            if (type == 'N' && double.TryParse(value.Replace('.', ','), out double num))
                return num;

            if (value.StartsWith("\""))
                return DbfRecord.ClearQuotes(value);

            return value;
        }
    }
}