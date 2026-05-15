using System.Text.RegularExpressions;

namespace SQL_ConsoleApp.Commands
{
    public class DeleteCommand : ICommand
    {
        private static readonly Regex DELETE_TABLE = new Regex(
            @"(?im)^\s*DELETE\s+FROM\s+(?<tableName>\w+)\s*" +
            @"(?:WHERE\s+(?<logicCommand>[^;]+))?\s*;$",
            RegexOptions.Compiled
        );

        private readonly Match _regex;
        private readonly LogicExpressionParser _whereParser;

        public DeleteCommand(string command)
        {
            _regex = DELETE_TABLE.Match(command);
            if (!_regex.Success)
                throw new Exception("Неверный синтаксис команды DELETE");

            string logicCommand = _regex.Groups["logicCommand"].Value;
            if (!string.IsNullOrWhiteSpace(logicCommand))
                _whereParser = new LogicExpressionParser(logicCommand);
        }

        public string GetTableName() => _regex.Groups["tableName"].Value;
        public bool HasWhereCondition() => _whereParser != null;
        public LogicExpressionParser GetWhereParser() => _whereParser;
    }
}