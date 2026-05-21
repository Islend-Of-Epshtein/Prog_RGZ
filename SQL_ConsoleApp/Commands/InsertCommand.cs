using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SQL_ConsoleApp.Commands
{
    /// <summary>
    /// Команда INSERT — добавление новой записи в таблицу.
    /// Синтаксис: INSERT INTO &lt;имя_таблицы&gt; (&lt;столбцы&gt;) VALUE (&lt;значения&gt;);
    /// </summary>
    public class InsertCommand : ICommand
    {
        private const string Pattern =
            @"(?im)^\s*INSERT\s+INTO\s+(?<tableName>\w+)\s*" +
            @"\((?<columns>[^)]+)\)\s+VALUE\s*\((?<values>[^)]+)\)\s*;$";

        private static readonly Regex InsertRegex = new(Pattern, RegexOptions.Compiled);

        private readonly Match _match;
        private readonly List<string> _columns;
        private readonly List<string> _values;

        /// <summary>
        /// Разбирает команду INSERT. Выбрасывает исключение при неверном синтаксисе
        /// или несоответствии количества полей и значений.
        /// </summary>
        /// <param name="command">Строка SQL-команды.</param>
        public InsertCommand(string command)
        {
            _match = InsertRegex.Match(command);
            if (!_match.Success)
                throw new System.Exception("Неверный синтаксис команды INSERT");

            _columns = ParseList(_match.Groups["columns"].Value);
            _values = ParseList(_match.Groups["values"].Value);

            if (_columns.Count != _values.Count)
                throw new System.Exception("Количество полей не соответствует количеству значений");
        }

        /// <summary>Возвращает имя таблицы.</summary>
        public string GetTableName() => _match.Groups["tableName"].Value;

        /// <summary>Возвращает список имён столбцов.</summary>
        public List<string> GetColumns() => _columns;

        /// <summary>Возвращает список значений.</summary>
        public List<string> GetValues() => _values;

        /// <summary>Разбирает строку с разделителями-запятыми в список обрезанных строк.</summary>
        private static List<string> ParseList(string input) =>
            input.Split(',').Select(s => s.Trim()).ToList();
    }
}