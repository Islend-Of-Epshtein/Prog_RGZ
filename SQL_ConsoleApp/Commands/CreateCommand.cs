using System;
using System.Text.RegularExpressions;

namespace SQL_ConsoleApp.Commands
{
    public struct RowDefinition
    {
        public char Type;
        public string Name;
        public int Width;
        public int Precision;
        public bool IsNotNull;

        public RowDefinition(char type, string name, bool isNotNull, string width = "", string precision = "")
        {
            Type = type;
            Name = name;
            IsNotNull = isNotNull;
            Width = 0;
            Precision = 0;

            switch (type)
            {
                case 'C':
                    if (string.IsNullOrEmpty(width))
                        throw new Exception("Для типа C необходим параметр Width");
                    Width = int.Parse(width);
                    break;
                case 'N':
                    if (string.IsNullOrEmpty(width) || string.IsNullOrEmpty(precision))
                        throw new Exception("Для типа N необходимы параметры Width и Precision");
                    Width = int.Parse(width);
                    Precision = int.Parse(precision);
                    break;
                case 'D':
                    Width = 8;
                    break;
                case 'L':
                    Width = 1;
                    break;
                case 'M':
                    Width = 10;
                    break;
            }
        }
    }

    public class CreateCommand : ICommand
    {
        private static readonly Regex CREATE_TABLE = new Regex(
            @"(?im)^\s*CREATE\s+TABLE\s+(?<tableName>\w+)\s*\((?<rowsDescription>.*?)\)\s*;$",
            RegexOptions.Compiled
        );

        private static readonly Regex CREATE_TABLE_ROWS = new Regex(
            @"(?im)\s*(?<rowName>\w+)\s+" +
            @"(?:(?<type>C)\s*\(\s*(?<width>\d+)\s*\)|(?<type>D)|(?<type>L)|(?<type>N)\s*\(\s*(?<width>\d+)\s*,\s*(?<precision>\d+)\s*\)|(?<type>M))" +
            @"(?:\s+NOT\s+NULL)?",
            RegexOptions.Compiled
        );

        private readonly Match _regex;
        private readonly RowDefinition[] _rows;

        public CreateCommand(string command)
        {
            _regex = CREATE_TABLE.Match(command);
            if (!_regex.Success)
                throw new Exception("Неверный синтаксис команды CREATE TABLE");

            string rowsDescription = _regex.Groups["rowsDescription"].Value;
            string[] rowStrings = SplitByComma(rowsDescription);

            _rows = new RowDefinition[rowStrings.Length];

            for (int i = 0; i < rowStrings.Length; i++)
            {
                Match match = CREATE_TABLE_ROWS.Match(rowStrings[i]);
                if (!match.Success)
                    throw new Exception($"Ошибка синтаксиса в описании столбца: {rowStrings[i]}");

                char type = match.Groups["type"].Value[0];
                string name = match.Groups["rowName"].Value;
                bool isNotNull = match.Value.Contains("NOT NULL", StringComparison.OrdinalIgnoreCase);
                string width = match.Groups["width"].Success ? match.Groups["width"].Value : "";
                string precision = match.Groups["precision"].Success ? match.Groups["precision"].Value : "";

                _rows[i] = new RowDefinition(type, name, isNotNull, width, precision);
            }
        }

        public string GetTableName() => _regex.Groups["tableName"].Value;
        public RowDefinition[] GetRows() => _rows;

        private static string[] SplitByComma(string str)
        {
            var result = new System.Collections.Generic.List<string>();
            int depth = 0;
            int start = 0;

            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == '(') depth++;
                else if (str[i] == ')') depth--;
                else if (str[i] == ',' && depth == 0)
                {
                    result.Add(str.Substring(start, i - start).Trim());
                    start = i + 1;
                }
            }
            result.Add(str.Substring(start).Trim());

            return result.Where(s => !string.IsNullOrEmpty(s)).ToArray();
        }

        private static IEnumerable<string> Where(Func<object, object> value)
        {
            throw new NotImplementedException();
        }
    }
}