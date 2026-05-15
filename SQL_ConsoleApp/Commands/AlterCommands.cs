using System.Text.RegularExpressions;

namespace SQL_ConsoleApp.Commands
{
    public abstract class AlterCommandBase : ICommand
    {
        protected readonly Match _regex;
        protected AlterCommandBase(string command, string pattern)
        {
            _regex = Regex.Match(command, pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            if (!_regex.Success)
                throw new Exception($"Неверный синтаксис команды ALTER TABLE");
        }
        public string GetTableName() => _regex.Groups["tableName"].Value;
    }

    public class AlterAdd : AlterCommandBase
    {
        private static readonly string PATTERN = @"(?im)^\s*ALTER\s+TABLE\s+(?<tableName>[^\s;]+)\s+" +
            @"COLUMN\s+ADD\s+(?<rowName>\w+)\s+" +
            @"(?:(?<type>C)\s*\(\s*(?<width>\d+)\s*\)|(?<type>D)|(?<type>L)|(?<type>N)\s*\(\s*(?<width>\d+)\s*,\s*(?<precision>\d+)\s*\)|(?<type>M))" +
            @"(?:\s+NOT\s+NULL)?\s*;$";

        public AlterAdd(string command) : base(command, PATTERN) { }

        public string GetRowName() => _regex.Groups["rowName"].Value;
        public char GetType() => _regex.Groups["type"].Value[0];
        public string GetWidth() => _regex.Groups["width"].Success ? _regex.Groups["width"].Value : "";
        public string GetPrecision() => _regex.Groups["precision"].Success ? _regex.Groups["precision"].Value : "";
        public bool IsNotNull() => _regex.Value.Contains("NOT NULL", System.StringComparison.OrdinalIgnoreCase);
    }

    public class AlterRemove : AlterCommandBase
    {
        private static readonly string PATTERN = @"(?im)^\s*ALTER\s+TABLE\s+(?<tableName>[^\s;]+)\s+" +
            @"COLUMN\s+REMOVE\s+(?<rowName>\w+)\s*;$";

        public AlterRemove(string command) : base(command, PATTERN) { }
        public string GetRowName() => _regex.Groups["rowName"].Value;
    }

    public class AlterRename : AlterCommandBase
    {
        private static readonly string PATTERN = @"(?im)^\s*ALTER\s+TABLE\s+(?<tableName>[^\s;]+)\s+" +
            @"COLUMN\s+RENAME\s+(?<oldName>\w+)\s+(?<newName>\w+)\s*;$";

        public AlterRename(string command) : base(command, PATTERN) { }
        public string GetOldName() => _regex.Groups["oldName"].Value;
        public string GetNewName() => _regex.Groups["newName"].Value;
    }

    public class AlterUpdate : AlterCommandBase
    {
        private static readonly string PATTERN = @"(?im)^\s*ALTER\s+TABLE\s+(?<tableName>[^\s;]+)\s+" +
            @"COLUMN\s+UPDATE\s+(?<rowName>\w+)\s+" +
            @"(?:(?<type>C)\s*\(\s*(?<width>\d+)\s*\)|(?<type>D)|(?<type>L)|(?<type>N)\s*\(\s*(?<width>\d+)\s*,\s*(?<precision>\d+)\s*\)|(?<type>M))" +
            @"(?:\s+NOT\s+NULL)?\s*;$";

        public AlterUpdate(string command) : base(command, PATTERN) { }

        public string GetRowName() => _regex.Groups["rowName"].Value;
        public char GetType() => _regex.Groups["type"].Value[0];
        public string GetWidth() => _regex.Groups["width"].Success ? _regex.Groups["width"].Value : "";
        public string GetPrecision() => _regex.Groups["precision"].Success ? _regex.Groups["precision"].Value : "";
        public bool IsNotNull() => _regex.Value.Contains("NOT NULL", System.StringComparison.OrdinalIgnoreCase);
    }
}