using System.Text.RegularExpressions;

namespace SQL_ConsoleApp.Commands
{
    /// <summary>
    /// Базовый класс для команд ALTER TABLE. Выполняет разбор команды по регулярному выражению.
    /// </summary>
    public abstract class AlterCommandBase : ICommand
    {
        /// <summary>Результат сопоставления регулярного выражения с командой.</summary>
        protected readonly Match RegexMatch;

        /// <summary>
        /// Инициализирует базовый класс, проверяя соответствие команды шаблону.
        /// </summary>
        /// <param name="command">Строка SQL-команды.</param>
        /// <param name="pattern">Регулярное выражение для разбора.</param>
        protected AlterCommandBase(string command, string pattern)
        {
            RegexMatch = Regex.Match(command, pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            if (!RegexMatch.Success)
                throw new System.Exception("Неверный синтаксис команды ALTER TABLE");
        }

        /// <summary>Возвращает имя таблицы из команды.</summary>
        public string GetTableName() => RegexMatch.Groups["tableName"].Value;
    }

    /// <summary>
    /// Команда ALTER TABLE ... COLUMN ADD — добавление нового столбца.
    /// </summary>
    public class AlterAdd : AlterCommandBase
    {
        private const string Pattern =
            @"(?im)^\s*ALTER\s+TABLE\s+(?<tableName>[^\s;]+)\s+" +
            @"COLUMN\s+ADD\s+(?<rowName>\w+)\s+" +
            @"(?:(?<type>C)\s*\(\s*(?<width>\d+)\s*\)" +
            @"|(?<type>D)" +
            @"|(?<type>L)" +
            @"|(?<type>N)\s*\(\s*(?<width>\d+)\s*,\s*(?<precision>\d+)\s*\)" +
            @"|(?<type>M))" +
            @"(?:\s+NOT\s+NULL)?\s*;$";

        public AlterAdd(string command) : base(command, Pattern) { }

        /// <summary>Имя добавляемого столбца.</summary>
        public string GetRowName() => RegexMatch.Groups["rowName"].Value;

        /// <summary>Тип столбца: C, N, D, L, M.</summary>
        public char GetType() => RegexMatch.Groups["type"].Value[0];

        /// <summary>Ширина столбца (для C и N).</summary>
        public string GetWidth() => GetOptionalGroup("width");

        /// <summary>Точность столбца (для N).</summary>
        public string GetPrecision() => GetOptionalGroup("precision");

        /// <summary>Флаг NOT NULL.</summary>
        public bool IsNotNull() => RegexMatch.Value.Contains("NOT NULL", System.StringComparison.OrdinalIgnoreCase);

        private string GetOptionalGroup(string groupName) =>
            RegexMatch.Groups[groupName].Success ? RegexMatch.Groups[groupName].Value : "";
    }

    /// <summary>
    /// Команда ALTER TABLE ... COLUMN REMOVE — удаление столбца.
    /// </summary>
    public class AlterRemove : AlterCommandBase
    {
        private const string Pattern =
            @"(?im)^\s*ALTER\s+TABLE\s+(?<tableName>[^\s;]+)\s+" +
            @"COLUMN\s+REMOVE\s+(?<rowName>\w+)\s*;$";

        public AlterRemove(string command) : base(command, Pattern) { }

        /// <summary>Имя удаляемого столбца.</summary>
        public string GetRowName() => RegexMatch.Groups["rowName"].Value;
    }

    /// <summary>
    /// Команда ALTER TABLE ... COLUMN RENAME — переименование столбца.
    /// </summary>
    public class AlterRename : AlterCommandBase
    {
        private const string Pattern =
            @"(?im)^\s*ALTER\s+TABLE\s+(?<tableName>[^\s;]+)\s+" +
            @"COLUMN\s+RENAME\s+(?<oldName>\w+)\s+(?<newName>\w+)\s*;$";

        public AlterRename(string command) : base(command, Pattern) { }

        /// <summary>Текущее имя столбца.</summary>
        public string GetOldName() => RegexMatch.Groups["oldName"].Value;

        /// <summary>Новое имя столбца.</summary>
        public string GetNewName() => RegexMatch.Groups["newName"].Value;
    }

    /// <summary>
    /// Команда ALTER TABLE ... COLUMN UPDATE — изменение типа/размера столбца.
    /// </summary>
    public class AlterUpdate : AlterCommandBase
    {
        private const string Pattern =
            @"(?im)^\s*ALTER\s+TABLE\s+(?<tableName>[^\s;]+)\s+" +
            @"COLUMN\s+UPDATE\s+(?<rowName>\w+)\s+" +
            @"(?:(?<type>C)\s*\(\s*(?<width>\d+)\s*\)" +
            @"|(?<type>D)" +
            @"|(?<type>L)" +
            @"|(?<type>N)\s*\(\s*(?<width>\d+)\s*,\s*(?<precision>\d+)\s*\)" +
            @"|(?<type>M))" +
            @"(?:\s+NOT\s+NULL)?\s*;$";

        public AlterUpdate(string command) : base(command, Pattern) { }

        /// <summary>Имя изменяемого столбца.</summary>
        public string GetRowName() => RegexMatch.Groups["rowName"].Value;

        /// <summary>Новый тип столбца.</summary>
        public char GetType() => RegexMatch.Groups["type"].Value[0];

        /// <summary>Новая ширина (для C и N).</summary>
        public string GetWidth() => GetOptionalGroup("width");

        /// <summary>Новая точность (для N).</summary>
        public string GetPrecision() => GetOptionalGroup("precision");

        /// <summary>Флаг NOT NULL.</summary>
        public bool IsNotNull() => RegexMatch.Value.Contains("NOT NULL", System.StringComparison.OrdinalIgnoreCase);

        private string GetOptionalGroup(string groupName) =>
            RegexMatch.Groups[groupName].Success ? RegexMatch.Groups[groupName].Value : "";
    }
}