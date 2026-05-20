using SQL_ConsoleApp.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace SQL_ConsoleApp.Commands
{
    public enum CompareOperator
    {
        Equal, NotEqual, LessThan, GreaterThan, LessOrEqual, GreaterOrEqual
    }

    public enum LogicalOperator { And, Or, Xor, Not }

    public struct ElementaryExpression
    {
        public string RowName;
        public CompareOperator CompareOperator;
        public string Value;

        public ElementaryExpression(string rowName, string operator_, string value)
        {
            RowName = rowName;
            CompareOperator = operator_ switch
            {
                "=" => CompareOperator.Equal,
                "<>" => CompareOperator.NotEqual,
                "<" => CompareOperator.LessThan,
                ">" => CompareOperator.GreaterThan,
                "<=" => CompareOperator.LessOrEqual,
                ">=" => CompareOperator.GreaterOrEqual,
                _ => throw new ArgumentException($"Неизвестный оператор: {operator_}")
            };
            Value = value;
        }
        public void setValue(string value)
        {
            this.Value = value;
        }
    }

    public class LogicalExpressionNode
    {
        public LogicalOperator? Operator { get; set; }
        public ElementaryExpression? ElementaryExpression { get; set; }
        public LogicalExpressionNode Left { get; set; }
        public LogicalExpressionNode Right { get; set; }
        public bool IsElementary => ElementaryExpression.HasValue;
        public bool IsNot => Operator == LogicalOperator.Not;
    }

    public class LogicExpressionParser
    {
        private static readonly Regex TOKEN_PATTERN = new Regex(
            @"(AND|OR|XOR|NOT|\(|\)|TRUE|NULL|FALSE|""[^""]*""|" +
            @"\d{2}[.,\\\/\-]\d{2}[.,\\\/\-]\d{4}|\d{4}[.,\\\/\-]\d{2}[.,\\\/\-]\d{2}|" +
            @"\d+(?:\.\d+)?|\w+|[=<>]+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        private static readonly Regex EXPRESSION_PATTERN = new Regex(
            @"^\s*(?<field>\w+)\s*(?<op>=|<>|<|>|<=|>=)\s*(?<value>" +
            @"\d\d[\.\\\/\-]\d\d[\.\\/-]\d\d\d\d|\d\d\d\d[\.\\/-]\d\d[\.\\/-]\d\d" +
            @"|\d+(?:\.\d+)?|TRUE|NULL|FALSE|""[^""]*"")\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        private readonly List<ElementaryExpression> _expressions;
        public LogicalExpressionNode Root { get; private set; }

        public LogicExpressionParser(string expression)
        {
            _expressions = new List<ElementaryExpression>();
            Root = Parse(expression);
        }

        public List<ElementaryExpression> GetAllExpressions() => _expressions;
        private LogicalExpressionNode Parse(string expression)
        {
            expression = expression.Trim();
            var tokens = Tokenize(expression);

            if (!AreParenthesesValid(tokens))
                throw new Exception("Несбалансированные скобки в логическом выражении");

            var rpn = ConvertToRPN(tokens);
            return BuildTreeFromRPN(rpn);
        }

        private List<string> Tokenize(string expression)
        {
            var tokens = new List<string>();
            var matches = TOKEN_PATTERN.Matches(expression);

            foreach (Match match in matches)
            {
                string token = match.Value;

                if (token.Equals("AND", StringComparison.OrdinalIgnoreCase) ||
                    token.Equals("OR", StringComparison.OrdinalIgnoreCase) ||
                    token.Equals("XOR", StringComparison.OrdinalIgnoreCase))
                    tokens.Add(token.ToUpper());
                else if (token.Equals("NOT", StringComparison.OrdinalIgnoreCase))
                    tokens.Add("NOT");
                else if (token.Equals("TRUE", StringComparison.OrdinalIgnoreCase))
                    tokens.Add("TRUE");
                else if (token.Equals("FALSE", StringComparison.OrdinalIgnoreCase))
                    tokens.Add("FALSE");
                else if (token.StartsWith("\""))
                    tokens.Add(token);
                else if (token == "(" || token == ")")
                    tokens.Add(token);
                else if (token == "=" || token == "<>" || token == "<" || token == ">" || token == "<=" || token == ">=")
                    tokens.Add(token);
                else
                    tokens.Add(token.ToUpper());
            }
            return tokens;
        }
        private bool AreParenthesesValid(List<string> tokens)
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

        private List<string> ConvertToRPN(List<string> tokens)
        {
            var output = new List<string>();
            var operators = new Stack<string>();
            var precedence = new Dictionary<string, int> { ["NOT"] = 3, ["AND"] = 2, ["XOR"] = 1, ["OR"] = 1 };

            for (int i = 0; i < tokens.Count; i++)
            {
                string token = tokens[i];
                if (token == "(")
                    operators.Push(token);
                else if (token == ")")
                {
                    while (operators.Count > 0 && operators.Peek() != "(")
                        output.Add(operators.Pop());
                    if (operators.Count > 0 && operators.Peek() == "(")
                        operators.Pop();
                }
                else if (token == "AND" || token == "OR" || token == "XOR" || token == "NOT")
                {
                    if (token == "NOT")
                        operators.Push(token);
                    else
                    {
                        while (operators.Count > 0 && operators.Peek() != "(" &&
                               precedence[operators.Peek()] >= precedence[token])
                            output.Add(operators.Pop());
                        operators.Push(token);
                    }
                }
                else
                {
                    string fullExpr = token;
                    while (i + 2 < tokens.Count && IsComparisonOperator(tokens[i + 1]))
                    {
                        fullExpr += tokens[i + 1] + tokens[i + 2];
                        i += 2;
                    }

                    if (!EXPRESSION_PATTERN.IsMatch(fullExpr))
                        throw new Exception($"Неверное выражение: {fullExpr}");

                    ParseAndAddExpression(fullExpr);
                    output.Add(fullExpr);
                }
            }

            while (operators.Count > 0)
                output.Add(operators.Pop());

            return output;
        }

        private bool IsComparisonOperator(string token)
        {
            return token == "=" || token == "<>" || token == "<" || token == ">" || token == "<=" || token == ">=";
        }

        private void ParseAndAddExpression(string expr)
        {
            Match match = EXPRESSION_PATTERN.Match(expr);
            if (match.Success)
            {
                var expression = new ElementaryExpression(
                    match.Groups["field"].Value,
                    match.Groups["op"].Value,
                    match.Groups["value"].Value
                );
                _expressions.Add(expression);
            }
        }

        private LogicalExpressionNode BuildTreeFromRPN(List<string> rpn)
        {
            var stack = new Stack<LogicalExpressionNode>();

            foreach (string token in rpn)
            {
                if (token == "AND" || token == "OR" || token == "XOR")
                {
                    var right = stack.Pop();
                    var left = stack.Pop();
                    stack.Push(new LogicalExpressionNode
                    {
                        Operator = token == "AND" ? LogicalOperator.And : token == "OR" ? LogicalOperator.Or : LogicalOperator.Xor,
                        Left = left,
                        Right = right
                    });
                }
                else if (token == "NOT")
                {
                    var operand = stack.Pop();
                    stack.Push(new LogicalExpressionNode { Operator = LogicalOperator.Not, Left = operand });
                }
                else
                {
                    var expr = _expressions.First(e => $"{e.RowName}{GetOperatorString(e.CompareOperator)}{e.Value}" == token ||
                                                        $"{e.RowName} {GetOperatorString(e.CompareOperator)} {e.Value}" == token);
                    stack.Push(new LogicalExpressionNode { ElementaryExpression = expr });
                }
            }

            if (stack.Count != 1)
                throw new Exception("Некорректное логическое выражение");

            return stack.Pop();
        }

        private string GetOperatorString(CompareOperator op)
        {
            return op switch
            {
                CompareOperator.Equal => "=",
                CompareOperator.NotEqual => "<>",
                CompareOperator.LessThan => "<",
                CompareOperator.GreaterThan => ">",
                CompareOperator.LessOrEqual => "<=",
                CompareOperator.GreaterOrEqual => ">=",
                _ => "="
            };
        }

        public bool Evaluate(Dictionary<string, (object, char)> row, DbtManager dbtManager = null)
        {
            
            return EvaluateNode(Root, row, dbtManager);
        }

        private bool EvaluateNode(LogicalExpressionNode node, Dictionary<string, (object Value, char Type)> row, DbtManager dbtManager)
        {
            if (node.IsElementary)
            {
                var expr = node.ElementaryExpression.Value;
                if (!row.ContainsKey(expr.RowName))
                    throw new Exception($"Поле '{expr.RowName}' не найдено");

                var (fieldValue, fieldType) = row[expr.RowName];

                // Если поле мемо, получаем текст из файла
                if (fieldType == 'M' && dbtManager != null)
                {
                    fieldValue = dbtManager.GetText(fieldValue?.ToString() ?? "");
                }

                object compareValue = ParseValue(expr.Value, fieldType);

                // Сравниваем значения с учетом типа
                return CompareTypedValues(fieldValue, compareValue, fieldType, expr.CompareOperator);
            }

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

        private bool CompareTypedValues(object fieldValue, object compareValue, char type, CompareOperator compareOperator)
        {
            switch (type)
            {
                case 'N': // Числа

                    double num1 = fieldValue is double d1 ? d1 :
                                 (double.TryParse(fieldValue?.ToString()?.Replace('.',',').Trim(), out double parsed1) ? parsed1 : 0.0);
                    double num2 = compareValue is double d2 ? d2 :
                                 (double.TryParse(compareValue?.ToString()?.Replace('.', ',').Trim(), out double parsed2) ? parsed2 : 0.0);

                    return compareOperator switch
                    {
                        CompareOperator.Equal => num1 == num2,
                        CompareOperator.NotEqual => num1 != num2,
                        CompareOperator.LessThan => num1 < num2,
                        CompareOperator.GreaterThan => num1 > num2,
                        CompareOperator.LessOrEqual => num1 <= num2,
                        CompareOperator.GreaterOrEqual => num1 >= num2,
                        _ => false
                    };

                case 'L': // Логические
                    bool bool1 = fieldValue is bool b1 ? b1 :
                                (fieldValue?.ToString()?.Trim().ToUpperInvariant() == "T" ||
                                 fieldValue?.ToString()?.Trim().ToUpperInvariant() == "TRUE");
                    bool bool2 = compareValue is bool b2 ? b2 :
                                (compareValue?.ToString()?.Trim().ToUpperInvariant() == "T" ||
                                 compareValue?.ToString()?.Trim().ToUpperInvariant() == "TRUE");

                    return compareOperator switch
                    {
                        CompareOperator.Equal => bool1 == bool2,
                        CompareOperator.NotEqual => bool1 != bool2,
                        CompareOperator.LessThan => !bool1 && bool2,     // false < true
                        CompareOperator.GreaterThan => bool1 && !bool2,  // true > false
                        CompareOperator.LessOrEqual => !bool1 || bool2,  // false <= true
                        CompareOperator.GreaterOrEqual => bool1 || !bool2, // true >= false
                        _ => false
                    };

                case 'D': // Даты
                    DateTime? date1 = fieldValue is DateTime dt1 ? dt1 :
                                    (DateTime.TryParseExact(fieldValue?.ToString()?.Trim(), "yyyyMMdd",
                                     System.Globalization.CultureInfo.InvariantCulture,
                                     System.Globalization.DateTimeStyles.None, out DateTime parsedDt1) ? parsedDt1 : (DateTime?)null);
                    DateTime? date2 = compareValue is DateTime dt2 ? dt2 :
                                    (DateTime.TryParseExact(compareValue?.ToString()?.Trim(), "yyyyMMdd",
                                     System.Globalization.CultureInfo.InvariantCulture,
                                     System.Globalization.DateTimeStyles.None, out DateTime parsedDt2) ? parsedDt2 : (DateTime?)null);

                    return compareOperator switch
                    {
                        CompareOperator.Equal => date1 == date2,
                        CompareOperator.NotEqual => date1 != date2,
                        CompareOperator.LessThan => date1 < date2,
                        CompareOperator.GreaterThan => date1 > date2,
                        CompareOperator.LessOrEqual => date1 <= date2,
                        CompareOperator.GreaterOrEqual => date1 >= date2,
                        _ => false
                    };

                case 'C': // Строки
                case 'M': // Memo как строки
                default:  // Строковое сравнение по умолчанию
                    string str1 = fieldValue?.ToString()?.Trim() ?? "";
                    string str2 = compareValue?.ToString()?.Trim() ?? "";

                    return compareOperator switch
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
        }

        private object ParseValue(string value, char type)
        {
            string upperValue = value.ToUpperInvariant();

            // Логические значения
            if (type == 'L')
            {
                if (upperValue == "TRUE" || upperValue == "T" || upperValue == "Y" )
                    return true;
                if (upperValue == "FALSE" || upperValue == "F" || upperValue == "N")
                    return false;
                if (upperValue == "NULL" || upperValue == "?")
                    return false;
            }
            if (upperValue.Equals("NULL"))
                return "";
            if (type == 'D' && DateTime.TryParse(value, out DateTime dateRes))
                return dateRes.ToString("yyyyMMdd");

            if (type == 'N' && double.TryParse(value.Replace('.', ','), out double numRes))
                return numRes;

            if (value.StartsWith("\""))
                return DbfRecord.ClearQuotes(value);

            return value;
        }
    }
}