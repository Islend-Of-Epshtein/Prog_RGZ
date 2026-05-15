using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SQL_ConsoleApp.Commands
{
    public class SelectCommand : ICommand
    {
        private static readonly Regex SELECT_TABLE = new Regex(
            @"(?im)^\s*SELECT\s+(?<columns>\*|[\w\s,]+)\s+FROM\s+(?<tableName>\w+)\s*" +
            @"(?:WHERE\s+(?<logicCommand>[^;]+))?\s*;$",
            RegexOptions.Compiled
        );

        private readonly Match _regex;
        private readonly List<string> _columns;
        private readonly LogicExpressionParser _whereParser;

        public SelectCommand(string command)
        {
            _columns = new List<string>();

            _regex = SELECT_TABLE.Match(command);
            if (!_regex.Success)
                throw new Exception("Неверный синтаксис команды SELECT");

            string columnsStr = _regex.Groups["columns"].Value.Trim();
            if (columnsStr == "*")
                _columns.Add("*");
            else
                foreach (string col in columnsStr.Split(','))
                    _columns.Add(col.Trim());

            string logicCommand = _regex.Groups["logicCommand"].Value;
            if (!string.IsNullOrWhiteSpace(logicCommand))
                _whereParser = new LogicExpressionParser(logicCommand);
        }

        public string GetTableName() => _regex.Groups["tableName"].Value;
        public List<string> GetColumns() => _columns;
        public bool IsSelectAll() => _columns.Count == 1 && _columns[0] == "*";
        public bool HasWhereCondition() => _whereParser != null;
        public LogicExpressionParser GetWhereParser() => _whereParser;
    }
}