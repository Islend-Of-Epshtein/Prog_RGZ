using System;
using System.Text.RegularExpressions;

namespace Model
{
    /*
     * CREATE TABLE, OPEN, CLOSE, ALTER TABLE (ADD/REMOVE/RENAME/UPDATE), INSERT, UPDATE, DELETE, SELECT, TRUNCATE, RESTORE, DROP, EXIT
     */
    public class Commands
    {
        private static string CREATE_TABLE = @"CREATE\sTABLE\s+(?<tableName>\w+)\s*\((.*?)\)\s*;";
        private static string CREATE_TABLE_ROWS = @"";
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
            string pattern = @"CREATE\s+TABLE\s+(?<tableName>\w+)\s*\((.*?)\)\s*;";
            string CREATE_TABLE_ROWS = @"(?<RowName>\w+)\s+([CDL]\(\s+(?<Widht>\d+)\s+\)|[N]\(\s+(?<Widht>\d+)\s+,\s+(?<Precision>\d+)\)|M)?;?";

            string command = "CREATE TABLE employee (name C(20) NOT NULL, salary N(8,2));";

            Match match = Regex.Match(command, pattern, RegexOptions.IgnoreCase);
            Match match2 = Regex.Match(command, CREATE_TABLE_ROWS, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string tableName = match.Groups["tableName"].Value;
                string columnsDef = match.Groups[1].Value; // содержимое между скобками

                Console.WriteLine($"Table: {tableName}");
                Console.WriteLine($"Columns: {columnsDef}");
            }
        }
    }
}