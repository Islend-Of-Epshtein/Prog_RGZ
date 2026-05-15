using System.Text.RegularExpressions;

namespace SQL_ConsoleApp.Commands
{
    public class ExitCommand : ICommand
    {
        private static readonly Regex EXIT_COMMAND = new Regex(
            @"(?im)^\s*EXIT\s*;$",
            RegexOptions.Compiled
        );

        private readonly Match _regex;

        public ExitCommand(string command)
        {
            _regex = EXIT_COMMAND.Match(command);
            if (!_regex.Success)
                throw new Exception("Неверный синтаксис команды EXIT");
        }

        public string GetTableName() => "";
    }
}