using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SQL_ConsoleApp
{
    internal class ExitCommand
    {
        static string EXIT_COMMAND = @"(?im)^\s*EXIT\s*;$";
        private Match regex;
        public ExitCommand(string command)
        {
            regex = Regex.Match(command, EXIT_COMMAND);
            if (!regex.Success) { throw new Exception("Неверный синтаксис команды"); }
        }
    }
}
