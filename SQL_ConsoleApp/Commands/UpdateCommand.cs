using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SQL_ConsoleApp.Commands
{
    public class UpdateCommand : ICommand
    {
        private static readonly Regex UPDATE_TABLE = new Regex(
            @"(?im)^\s*UPDATE\s+(?<tableName>\w+)\s+SET\s+(?<setClause>[^;]+?)\s*" +
            @"(?:WHERE\s+(?<logicCommand>[^;]+))?\s*;$",
            RegexOptions.Compiled
        );

        private static readonly Regex SET_PATTERN = new Regex(
            @"^\s*(?<field>\w+)\s*=\s*(?<value>" +
            @"\d+(?:\.\d+)?|TRUE|FALSE|T|F|N|Y|\?|NULL|""[^""]*""|@[^\s]+|"+
            @"\d\d(\.|,|\\|/)\d\d\1\d\d\d\d|\d\d\d\d(\.|,|\\|/)\d\d(\.|,|\\|/)\d\d)\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );
        private readonly Match _regex;
        private readonly List<(string Field, string Value)> _values;
        private readonly LogicExpressionParser _whereParser;

        public UpdateCommand(string command)
        {
            _values = new List<(string, string)>();

            _regex = UPDATE_TABLE.Match(command);
            if (!_regex.Success)
                throw new Exception("Неверный синтаксис команды UPDATE");

            string setClause = _regex.Groups["setClause"].Value;
            string[] setParts = setClause.Split(',');

            foreach (string part in setParts)
            {
                Match match = SET_PATTERN.Match(part);
                if (!match.Success)
                    throw new Exception($"Неверный синтаксис в SET: {part}");

                string field = match.Groups["field"].Value;
                string value = match.Groups["value"].Value;
                string normalizedValue = NormalizeValue(value);

                _values.Add((field, normalizedValue));
            }

            string logicCommand = _regex.Groups["logicCommand"].Value;
            if (!string.IsNullOrWhiteSpace(logicCommand))
                _whereParser = new LogicExpressionParser(logicCommand);
        }
        private string NormalizeValue(string value)
        {
            string upperValue = value.ToUpperInvariant();

            // Нормализуем логические значения (без кавычек)
            if (upperValue == "TRUE" || upperValue == "T" || upperValue == "Y" || upperValue == "1")
                return "TRUE";
            if (upperValue == "FALSE" || upperValue == "F" || upperValue == "N" || upperValue == "0")
                return "FALSE";
            if (upperValue == "NULL")
                return "";
            return value;
        }
        public string GetTableName() => _regex.Groups["tableName"].Value;
        public List<(string Field, string Value)> GetValues() => _values;
        public bool HasWhereCondition() => _whereParser != null;
        public LogicExpressionParser GetWhereParser() => _whereParser;
    }
}