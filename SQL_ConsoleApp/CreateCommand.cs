using SQL_ConsoleApp;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace SQL_ConsoleApp
{
    /*
     * CREATE TABLE, OPEN, CLOSE, ALTER TABLE (ADD/REMOVE/RENAME/UPDATE), INSERT, UPDATE, DELETE, SELECT, TRUNCATE, RESTORE, DROP, EXIT
     */
    public struct Row
    {
        public char Type;
        public string Name;
        public int Width;
        public int Precision;
        public bool isNotNull;
        public Row(char type, string name,bool isNotNull, string width="", string Precision="") { 
            this.Type = type;
            this.Name = name;
            switch (type)
            {
                case ('C'):
                    if (width == "") throw new Exception("для С необходим параметр Width");
                    int.TryParse(width,out int a);
                    this.Width = a;
                    break;
                case ('N'):
                    if (width == ""|| Precision == "") throw new Exception("для N необходимы параметры Width и Precision");
                    int.TryParse(width, out int b);
                    int.TryParse(width, out int c);
                    this.Width = b;
                    this.Precision =c;
                    break;
                default:
                    break;
            }
        }
    }
    public class CreateCommand
    {
        static string CREATE_TABLE = @"(?im)^\s*CREATE\s+TABLE\s+"+
                            @"(?<tableName>\w+)\s*\((?<RowsDiscription>.*?)\)\s*;$";
        static string CREATE_TABLE_ROWS = @"(?im)\s*(?<RowName>\w+)\s+"+
                                    @"(?<Discription>C\s*\(\s*(?<Widht>\d+)\s*\)|D|L|N\s*\((?<Width>\s*\d+\s*),(?<Precision>\s*\d+\s*)\)|M)"+
                                        @"(?<IsNotNull>\s+NOT\s+NULL\s*)?";
        private Match regex;
        private Row[] rows;
        public CreateCommand(string command) {
            regex = Regex.Match(command, CREATE_TABLE);
            if (!regex.Success) { throw new Exception("Неверный синтаксис команды"); }
            command = regex.Groups["RowsDiscription"].Value;
            string[] regexRows = SplitByComma(command);
            foreach (string s in regexRows)
            {
                Match match = Regex.Match(s, CREATE_TABLE_ROWS);
                if (!match.Success)
                {
                    throw new Exception("Ошибка синтаксиса, столбцы описаны неверно");
                }
            }
            rows = new Row[regexRows.Length];
            int i = 0;
            foreach (string s in regexRows)
            {
                Match match = Regex.Match(s, CREATE_TABLE_ROWS);
                switch (match.Groups["Discription"].Value[0])
                {
                    case ('C'):
                        rows[i] = new Row('C', match.Groups["RowName"].Value,
                            match.Groups["isNotNull"].Value!=null, match.Groups["Width"].Value);
                        break;
                    case ('N'):
                        rows[i] = new Row('N', match.Groups["RowName"].Value,
                            match.Groups["isNotNull"].Value != null, match.Groups["Width"].Value, match.Groups["Precision"].Value);
                        break;
                    default:
                        rows[i] = new Row(match.Groups["Discription"].Value[0], match.Groups["RowName"].Value,
                            match.Groups["isNotNull"].Value != null);
                        break;
                }
                i++;
            }
            
        }
        public string getTableName()
        {
            return regex.Success ? regex.Groups["tableName"].Value : "";
        }
        public Row[] GetRows()
        {
            return rows;
        }
        public static void Main(string[] args)
        {
            /*string command = "CREAte TABLE employee (name C(20) NOT NULL, salary N(8,2));";

            Match match = Regex.Match(command, CREATE_TABLE);
            if(!match.Success)
            {
                throw new Exception("Не соответствует структуре (match)");
            }
            command = match.Groups["RowsDescription"].Value;
            foreach(string s in SplitByComma(command))
            {
                Match match2 = Regex.Match(s, CREATE_TABLE_ROWS);
                if (!match2.Success)
                {
                    throw new Exception("Не соответствует структуре (match2)");
                }
                Console.WriteLine(match2.Value);
            }*/
            string command = "OPEN a.dpf;";
            OpenCommand open = new OpenCommand(command);
            //Console.WriteLine(open.CheckPath().ToString() + " - " + open.getPath().ToString());
            
        }
        /// <summary>
        /// Метод для разбиения по запятой для паттерна CREATE_TABLE_ROWS
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static string[] SplitByComma(string str)
        {
            string[] splited = str.Split(',');
            int i = 0, count = 0 ;
            foreach (string s in splited)
            {
                
                if (s.IndexOf('(') != -1 && s.IndexOf(')') == -1) {
                    splited[i] = s + ',' + splited[i+1];
                    splited[i + 1] = "";
                    count--;
                }
                i++;
                count++;
            }
            string[] result = new string[count];
            i = 0;
            foreach (string s in splited)
            {
                if (s != "")
                {
                    result[i] = s;
                    i++;
                }
            }
            return result;
        }
    }
}