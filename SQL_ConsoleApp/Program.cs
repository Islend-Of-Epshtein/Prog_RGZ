using System;
using System.Text.RegularExpressions;

namespace Model
{
    /*
     * CREATE TABLE, OPEN, CLOSE, ALTER TABLE (ADD/REMOVE/RENAME/UPDATE), INSERT, UPDATE, DELETE, SELECT, TRUNCATE, RESTORE, DROP, EXIT
     */
    public class Commands
    {
        static string CREATE_TABLE = @"(?im)^\s*CREATE\s+TABLE\s+"+
                            @"(?<tableName>\w+)\s*\((?<RowsDescription>.*?)\)\s*;$";
        static string CREATE_TABLE_ROWS = @"(?im)\s*(?<RowName>\w+)\s+"+
                                    @"(?:C\s*\(\s*(?<Widht>\d+)\s*\)|D|L|N\s*\((?<Width>\s*\d+\s*),(?<Precision>\s*\d+\s*)\)|M)"+
                                        @"(?<IsNotNull>\s+NOT\s+NULL\s*)?";
        private static string OPEN = @"";
        private static string CLOSE = @"";
        private static string ALTER_TABLE_ADD = @"";
        private static string ALTER_TABLE_REMOVE = @"";
        private static string ALTER_TABLE_RENAME = @"";
        private static string ALTER_TABLE_UPDATE = @"";
        private static string INSERT = @"";
        private static string UPDATE = @"";
        private static string DELETE = @"";
        private static string SELECT = @"";
        private static string TRUNCATE = @"";
        private static string RESTORE = @"";
        private static string DROP = @"";
        private static string EXIT = @"";

        public Commands() { }
        public static void Main(string[] args)
        {
            string command = "CREAte TABLE employee (name C(20) NOT NULL, salary N(8,2));";

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
            }
        
            
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