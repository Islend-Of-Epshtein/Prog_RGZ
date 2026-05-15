using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SQL_ConsoleApp.Commands
{
    public class InsertCommand : ICommand
    {
        private static readonly Regex INSERT_TABLE = new Regex(
            @"(?im)^\s*INSERT\s+INTO\s+(?<tableName>\w+)\s*" +
            @"\((?<columns>[^)]+)\)\s+VALUE\s*\((?<values>[^)]+)\)\s*;$",
            RegexOptions.Compiled
        );

        private readonly Match _regex;
        private readonly List<string> _columns;
        private readonly List<string> _values;

        public InsertCommand(string command)
        {
            _columns = new List<string>();
            _values = new List<string>();

            _regex = INSERT_TABLE.Match(command);
            if (!_regex.Success)
                throw new Exception("Неверный синтаксис команды INSERT");

            string columnsStr = _regex.Groups["columns"].Value;
            foreach (string col in columnsStr.Split(','))
                _columns.Add(col.Trim());

            string valuesStr = _regex.Groups["values"].Value;
            var valueParts = SplitValues(valuesStr);

            if (_columns.Count != valueParts.Count)
                throw new Exception("Количество полей не соответствует количеству значений");

            foreach (string part in valueParts)
                _values.Add(part.Trim());
        }

        public string GetTableName() => _regex.Groups["tableName"].Value;
        public List<string> GetColumns() => _columns;
        public List<string> GetValues() => _values;

        private List<string> SplitValues(string valuesStr)
        {
            var result = new List<string>();
            int depth = 0;
            int start = 0;
            bool inQuotes = false;

            for (int i = 0; i < valuesStr.Length; i++)
            {
                if (valuesStr[i] == '"')
                    inQuotes = !inQuotes;
                else if (!inQuotes && valuesStr[i] == '(')
                    depth++;
                else if (!inQuotes && valuesStr[i] == ')')
                    depth--;
                else if (!inQuotes && valuesStr[i] == ',' && depth == 0)
                {
                    result.Add(valuesStr.Substring(start, i - start));
                    start = i + 1;
                }
            }
            result.Add(valuesStr.Substring(start));

            return result;
        }
    }
}