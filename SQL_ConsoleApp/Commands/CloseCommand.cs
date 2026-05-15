using System.Text.RegularExpressions;

namespace SQL_ConsoleApp.Commands
{
    public class CloseCommand : ICommand
    {
        private static readonly Regex CLOSE_COMMAND = new Regex(
            @"(?im)^\s*CLOSE\s*;$",
            RegexOptions.Compiled
        );

        private readonly Match _regex;

        public CloseCommand(string command)
        {
            _regex = CLOSE_COMMAND.Match(command);
            if (!_regex.Success)
                throw new Exception("Неверный синтаксис команды CLOSE");
        }

        public string GetTableName() => "";
    }
}