using System.Text.RegularExpressions;

namespace SQL_ConsoleApp.Commands
{
    public class RestoreCommand : ICommand
    {
        private static readonly Regex RESTORE_TABLE = new Regex(
            @"(?im)^\s*RESTORE\s+(?<tableName>\w+)\s*" +
            @"(?:WHERE\s+(?<logicCommand>[^;]+))?\s*;$",
            RegexOptions.Compiled
        );

        private readonly Match _regex;
        private readonly LogicExpressionParser _whereParser;

        public RestoreCommand(string command)
        {
            _regex = RESTORE_TABLE.Match(command);
            if (!_regex.Success)
                throw new Exception("Неверный синтаксис команды RESTORE");

            string logicCommand = _regex.Groups["logicCommand"].Value;
            if (!string.IsNullOrWhiteSpace(logicCommand))
                _whereParser = new LogicExpressionParser(logicCommand);
        }

        public string GetTableName() => _regex.Groups["tableName"].Value;
        public bool HasWhereCondition() => _whereParser != null;
        public LogicExpressionParser GetWhereParser() => _whereParser;
    }
}