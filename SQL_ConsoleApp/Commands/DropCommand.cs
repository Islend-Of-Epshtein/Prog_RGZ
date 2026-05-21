using System.Text.RegularExpressions;

namespace SQL_ConsoleApp.Commands
{
    /// <summary>
    /// Команда DROP TABLE — безвозвратное удаление таблицы и связанных файлов.
    /// Синтаксис: DROP TABLE &lt;имя_таблицы&gt;;
    /// </summary>
    public class DropCommand : ICommand
    {
        private const string Pattern = @"(?im)^\s*DROP\s+TABLE\s+(?<tableName>\w+)\s*;$";

        private static readonly Regex DropRegex = new(Pattern, RegexOptions.Compiled);

        private readonly Match _match;

        /// <summary>
        /// Разбирает команду DROP TABLE. Выбрасывает исключение при неверном синтаксисе.
        /// </summary>
        /// <param name="command">Строка SQL-команды.</param>
        public DropCommand(string command)
        {
            _match = DropRegex.Match(command);
            if (!_match.Success)
                throw new System.Exception("Неверный синтаксис команды DROP");
        }

        /// <summary>Возвращает имя удаляемой таблицы.</summary>
        public string GetTableName() => _match.Groups["tableName"].Value;
    }
}