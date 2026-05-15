using System.Text.RegularExpressions;

namespace SQL_ConsoleApp.Commands
{
    public class TruncateCommand : ICommand
    {
        private static readonly Regex TRUNCATE_TABLE = new Regex(
            @"(?im)^\s*TRUNCATE\s+(?<tableName>\w+)\s*;$",
            RegexOptions.Compiled
        );

        private readonly Match _regex;

        public TruncateCommand(string command)
        {
            _regex = TRUNCATE_TABLE.Match(command);
            if (!_regex.Success)
                throw new Exception("Неверный синтаксис команды TRUNCATE");
        }

        public string GetTableName() => _regex.Groups["tableName"].Value;
    }
}