using System.IO;
using System.Text.RegularExpressions;

namespace SQL_ConsoleApp.Commands
{
    public class OpenCommand : ICommand
    {
        private static readonly Regex OPEN_COMMAND = new Regex(
            @"(?im)^\s*OPEN\s+(?<filePath>[^\s;]+)\s*;$",
            RegexOptions.Compiled
        );

        private readonly Match _regex;

        public OpenCommand(string command)
        {
            _regex = OPEN_COMMAND.Match(command);
            if (!_regex.Success)
                throw new Exception("Неверный синтаксис команды OPEN");
        }

        public string GetPath() => _regex.Groups["filePath"].Value;

        public string GetTableName() => Path.GetFileNameWithoutExtension(GetPath());

        public bool CheckExtension() => GetPath().EndsWith(".dbf");
    }
}