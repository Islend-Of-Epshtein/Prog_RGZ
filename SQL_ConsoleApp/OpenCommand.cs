using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SQL_ConsoleApp
{
    internal sealed class OpenCommand
    {
        static string OPEN_COMMAND = @"(?im)^\s*OPEN\s+(?<filePath>[^\s;]+)\s*;$";
        private Match regex;
        public OpenCommand(string command)
        {
            regex = Regex.Match(command, OPEN_COMMAND);
            if (!regex.Success) { throw new Exception("Неверный синтаксис команды"); }
        }
        public string getPath()
        {
            return regex.Success ? regex.Groups["filePath"].Value:"";
        }
        public bool CheckPath()
        {
            if (!regex.Success) { return false; }
            if (!regex.Groups["filePath"].Value.ToString().EndsWith(".dpf")) { return false; }
            return true;
        }
    }
}
