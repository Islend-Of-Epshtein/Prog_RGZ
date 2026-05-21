using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SQL_ConsoleApp.Commands
{
    /// <summary>
    /// Команда UPDATE — обновление значений в записях таблицы.
    /// Синтаксис: UPDATE &lt;имя_таблицы&gt; SET &lt;поле&gt;=&lt;значение&gt; [,...] [WHERE &lt;условие&gt;];
    /// </summary>
    public class UpdateCommand : ICommand
    {
        private const string DatePattern = @"\d\d(\.|,|\\|/)\d\d\1\d\d\d\d|\d\d\d\d(\.|,|\\|/)\d\d(\.|,|\\|/)\d\d";
        private const string NumberPattern = @"\d+(?:\.\d+)?";
        private const string LogicalPattern = @"TRUE|FALSE|T|F|N|Y|\?|NULL";
        private const string StringPattern = @"""[^""]*""";
        private const string FilePattern = @"@[^\s]+";

        private const string TablePattern =
            @"(?im)^\s*UPDATE\s+(?<tableName>\w+)\s+SET\s+(?<setClause>[^;]+?)\s*" +
            @"(?:WHERE\s+(?<logicCommand>[^;]+))?\s*;$";

        private static readonly string SetValuePattern =
            $@"^\s*(?<field>\w+)\s*=\s*(?<value>{NumberPattern}|{LogicalPattern}|{StringPattern}|{FilePattern}|{DatePattern})\s*$";

        private static readonly Regex UpdateRegex = new(TablePattern, RegexOptions.Compiled);
        private static readonly Regex SetRegex = new(SetValuePattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly Match _match;
        private readonly List<(string Field, string Value)> _values;
        private readonly LogicExpressionParser _whereParser;

        /// <summary>
        /// Разбирает команду UPDATE. Выбрасывает исключение при неверном синтаксисе.
        /// </summary>
        /// <param name="command">Строка SQL-команды.</param>
        public UpdateCommand(string command)
        {
            _match = UpdateRegex.Match(command);
            if (!_match.Success)
                throw new System.Exception("Неверный синтаксис команды UPDATE");

            _values = ParseSetClause(_match.Groups["setClause"].Value);

            string whereClause = _match.Groups["logicCommand"].Value;
            if (!string.IsNullOrWhiteSpace(whereClause))
                _whereParser = new LogicExpressionParser(whereClause);
        }

        /// <summary>Возвращает имя таблицы.</summary>
        public string GetTableName() => _match.Groups["tableName"].Value;

        /// <summary>Возвращает список пар поле-значение из SET-выражения.</summary>
        public List<(string Field, string Value)> GetValues() => _values;

        /// <summary>Проверяет наличие WHERE-условия.</summary>
        public bool HasWhereCondition() => _whereParser != null;

        /// <summary>Возвращает парсер WHERE-условия или null, если условие отсутствует.</summary>
        public LogicExpressionParser GetWhereParser() => _whereParser;

        /// <summary>Разбирает SET-выражение на пары поле=значение.</summary>
        private static List<(string Field, string Value)> ParseSetClause(string setClause)
        {
            var values = new List<(string, string)>();

            foreach (string part in setClause.Split(','))
            {
                Match match = SetRegex.Match(part);
                if (!match.Success)
                    throw new System.Exception($"Неверный синтаксис в SET: {part}");

                values.Add((match.Groups["field"].Value, NormalizeValue(match.Groups["value"].Value)));
            }

            return values;
        }

        /// <summary>Нормализует значение: приводит логические к TRUE/FALSE, NULL к пустой строке.</summary>
        private static string NormalizeValue(string value) => value.ToUpperInvariant() switch
        {
            "TRUE" or "T" or "Y" or "1" => "TRUE",
            "FALSE" or "F" or "N" or "0" => "FALSE",
            "NULL" => "",
            _ => value
        };
    }
}