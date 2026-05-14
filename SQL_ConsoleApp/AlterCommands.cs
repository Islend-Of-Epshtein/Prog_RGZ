using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SQL_ConsoleApp
{
    /// <summary>
    /// Класс для проверки комманд типа ALTER_TABLE
    /// </summary>
    internal class AlterCommands
    {
        static string ALTER_TABLE_COMMAND = @"(?im)^\s*ALTER\s+TABLE\s+(?<tableName>[^\s;]+)\s+"+
                                            @"(?<commandType>COLUMN\s+ADD|COLUMN\s+REMOVE|COLUMN\s+RENAME|COLUMN\s+UPDATE)\s+.+;$";
        protected Match regex;
        public AlterCommands(string command) 
        {
            regex = Regex.Match(command, ALTER_TABLE_COMMAND);
            if (!regex.Success) { throw new Exception("Неверный синтаксис команды"); }
            
        }
        public string getCommand()
        {
            return regex.Success ? regex.Groups["commandType"].Value : "";
        }
        public string getTableName()
        {
            return regex.Success ? regex.Groups["tableName"].Value : "";
        }
    }
    /*switch (regex.Groups["commandType"].ToString().ToUpper())
                {
                    case (@"COLUMN\s+ADD"):
                        new AlterAdd(command);
                        break;
                    case (@"COLUMN\s+REMOVE"):
                        new AlterRemove(command);
                        break;
                    case (@"COLUMN\s+RENAME"):
                        new AlterRename(command);
                        break;
                    case (@"COLUMN\s+UPDATE"):
                        new AlterUpdate(command);
                        break;
                    default:
                        throw new Exception("Неопознанная команда");
                }*/

    // Классы команды Alter table

    internal class AlterAdd : AlterCommands {
        static string ALTER_TABLE_ADD_COMMAND = @"(?im)^\s*ALTER\s+TABLE\s+(?<tableName>[^\s;]+)\s+" +
                                            @"COLUMN\s+ADD\s+"+
                                        @"(?<RowName>\w+)\s+" +
                                    @"(?<Type>C\s*\(\s*(?<Width>\d+)\s*\)|D|L|N\s*\((?<Width>\s*\d+\s*),(?<Precision>\s*\d+\s*)\)|M)" +
                                        @"(?<IsNotNull>\s+NOT\s+NULL\s*)?;&";
        internal AlterAdd(string command) : base(command)
        {
            base.regex = Regex.Match(command, ALTER_TABLE_ADD_COMMAND);
            if (!regex.Success) { throw new Exception("Неверный синтаксис команды ADD"); }
        }
        public string getRowName()
        {
            return regex.Success ? regex.Groups["RowName"].Value : "";
        }
        public char getType()
        {
            return regex.Success ? regex.Groups["Type"].Value[0] : ' ';
        }
        public string getWidth()
        {
            switch (this.getType()) {
                case ('C'): 
                case ('N'):
                    return regex.Groups["Width"].Value;
                default:
                    return null;
                    }
        }
        public string getPrecision()
        {
            switch (this.getType())
            {
                case ('N'):
                    return regex.Groups["Precision"].Value;
                default:
                    return null;
            }
        }
        public bool isNotNull()
        {
            return regex.Groups["IsNotNull"]!=null;
        }
    }
    internal class AlterRemove : AlterCommands {
        static string ALTER_TABLE_REMOVE_COMMAND = @"(?im)^\s*ALTER\s+TABLE\s+(?<tableName>[^\s;]+)\s+" +
                                            @"COLUMN\s+REMOVE\s+" +
                                          @"(?<rowName>[^\s;]+)\s*;$";
        internal AlterRemove(string command) : base(command)
        {
            base.regex = Regex.Match(command, ALTER_TABLE_REMOVE_COMMAND);
            if (!regex.Success) { throw new Exception("Неверный синтаксис команды REMOVE"); }
        }
        public string getRowName()
        {
            return regex.Success ? regex.Groups["RowName"].Value : "";
        }
    }
    internal class AlterRename : AlterCommands {
        static string ALTER_TABLE_RENAME_COMMAND = @"(?im)^\s*ALTER\s+TABLE\s+(?<tableName>[^\s;]+)\s+" +
                                            @"COLUMN\s+RENAME\s+" +
                                          @"(?<oldName>[^\s;]+)\s+(?<newName>[^\s;]+);$";
        internal AlterRename(string command) : base(command)
        {
            base.regex = Regex.Match(command, ALTER_TABLE_RENAME_COMMAND);
            if (!regex.Success) { throw new Exception("Неверный синтаксис команды RENAME"); }
        }
        public string getOldName()
        {
            return regex.Success ? regex.Groups["RowName"].Value : "";
        }
        public string getNewName()
        {
            return regex.Success ? regex.Groups["RowName"].Value : "";
        }
    }
    internal class AlterUpdate : AlterCommands {
        static string ALTER_TABLE_RENAME_UPDATE = @"(?im)^\s*ALTER\s+TABLE\s+(?<tableName>[^\s;]+)\s+" +
                                            @"COLUMN\s+RENAME\s+" +
                                          @"(?<rowName>[^\s;]+)\s+"+
                                 @"(?<RowName>[^\s;]+)\s+" +
                                    @"(?:C\s*\(\s*(?<Widht>\d+)\s*\)|D|L|N\s*\((?<Width>\s*\d+\s*),(?<Precision>\s*\d+\s*)\)|M)" +
                                        @"(?<IsNotNull>\s+NOT\s+NULL\s*)?;&";
        internal AlterUpdate(string command) : base(command)
        {
            base.regex = Regex.Match(command, ALTER_TABLE_RENAME_UPDATE);
            if (!regex.Success) { throw new Exception("Неверный синтаксис команды UPDATE"); }
        }
        public string getRowName()
        {
            return regex.Success ? regex.Groups["RowName"].Value : "";
        }
        public char getType()
        {
            return regex.Success ? regex.Groups["Type"].Value[0] : ' ';
        }
        public string getWidth()
        {
            switch (this.getType())
            {
                case ('C'):
                case ('N'):
                    return regex.Groups["Width"].Value;
                default:
                    return null;
            }
        }
        public string getPrecision()
        {
            switch (this.getType())
            {
                case ('N'):
                    return regex.Groups["Precision"].Value;
                default:
                    return null;
            }
        }
        public bool isNotNull()
        {
            return regex.Groups["IsNotNull"] != null;
        }
    }
}
