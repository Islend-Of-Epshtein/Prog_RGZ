using System.Text.RegularExpressions;

namespace SQL_ConsoleApp.Commands
{
    public class DropCommand : ICommand
    {
        private static readonly Regex DROP_TABLE = new Regex(
            @"(?im)^\s*DROP\s+TABLE\s+(?<tableName>\w+)\s*;$",
            RegexOptions.Compiled
        );

        private readonly Match _regex;

        public DropCommand(string command)
        {
            _regex = DROP_TABLE.Match(command);
            if (!_regex.Success)
                throw new Exception("Неверный синтаксис команды DROP");
        }

        public string GetTableName() => _regex.Groups["tableName"].Value;
    }
}