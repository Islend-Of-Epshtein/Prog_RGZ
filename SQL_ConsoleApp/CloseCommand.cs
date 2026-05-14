using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SQL_ConsoleApp
{
    internal class CloseCommand
    {
        static string CLOSE_COMMAND = @"(?im)^\s*CLOSE\s+(?<tableName>[^\s;]+)\s*;$";
        private Match regex;
        public CloseCommand(string command) {
            regex = Regex.Match(command, CLOSE_COMMAND);
            if (!regex.Success) { throw new Exception("Неверный синтаксис команды"); }
        }
        public string getName()
        {
            return regex.Success ? regex.Groups["tableName"].Value : "";
        }
    }
}
