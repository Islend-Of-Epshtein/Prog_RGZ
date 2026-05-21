using System.Text.RegularExpressions;

namespace SQL_ConsoleApp.Commands
{
    /// <summary>
    /// Команда TRUNCATE — физическое удаление всех помеченных записей.
    /// Синтаксис: TRUNCATE &lt;имя_таблицы&gt;;
    /// </summary>
    public class TruncateCommand : ICommand
    {
        private const string Pattern = @"(?im)^\s*TRUNCATE\s+(?<tableName>\w+)\s*;$";

        private static readonly Regex TruncateRegex = new(Pattern, RegexOptions.Compiled);

        private readonly Match _match;

        /// <summary>
        /// Разбирает команду TRUNCATE. Выбрасывает исключение при неверном синтаксисе.
        /// </summary>
        /// <param name="command">Строка SQL-команды.</param>
        public TruncateCommand(string command)
        {
            _match = TruncateRegex.Match(command);
            if (!_match.Success)
                throw new System.Exception("Неверный синтаксис команды TRUNCATE");
        }

        /// <summary>Возвращает имя таблицы.</summary>
        public string GetTableName() => _match.Groups["tableName"].Value;
    }
}