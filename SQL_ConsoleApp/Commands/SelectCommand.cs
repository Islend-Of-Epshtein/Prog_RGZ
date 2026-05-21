using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SQL_ConsoleApp.Commands
{
    /// <summary>
    /// Команда SELECT — выборка данных из таблицы.
    /// Синтаксис: SELECT &lt;*|столбцы&gt; FROM &lt;имя_таблицы&gt; [WHERE &lt;условие&gt;];
    /// </summary>
    public class SelectCommand : ICommand
    {
        private const string Pattern =
            @"(?im)^\s*SELECT\s+(?<columns>\*|[\w\s,]+)\s+FROM\s+(?<tableName>\w+)\s*" +
            @"(?:WHERE\s+(?<logicCommand>[^;]+))?\s*;$";

        private static readonly Regex SelectRegex = new(Pattern, RegexOptions.Compiled);

        private readonly Match _match;
        private readonly List<string> _columns;
        private readonly LogicExpressionParser _whereParser;

        /// <summary>
        /// Разбирает команду SELECT. Выбрасывает исключение при неверном синтаксисе.
        /// </summary>
        /// <param name="command">Строка SQL-команды.</param>
        public SelectCommand(string command)
        {
            _match = SelectRegex.Match(command);
            if (!_match.Success)
                throw new System.Exception("Неверный синтаксис команды SELECT");

            _columns = ParseColumns(_match.Groups["columns"].Value);

            string whereClause = _match.Groups["logicCommand"].Value;
            if (!string.IsNullOrWhiteSpace(whereClause))
                _whereParser = new LogicExpressionParser(whereClause);
        }

        /// <summary>Возвращает имя таблицы.</summary>
        public string GetTableName() => _match.Groups["tableName"].Value;

        /// <summary>Возвращает список запрашиваемых столбцов.</summary>
        public List<string> GetColumns() => _columns;

        /// <summary>Проверяет, выбраны ли все столбцы (SELECT *).</summary>
        public bool IsSelectAll() => _columns.Count == 1 && _columns[0] == "*";

        /// <summary>Проверяет наличие WHERE-условия.</summary>
        public bool HasWhereCondition() => _whereParser != null;

        /// <summary>Возвращает парсер WHERE-условия или null, если условие отсутствует.</summary>
        public LogicExpressionParser GetWhereParser() => _whereParser;

        /// <summary>Разбирает строку столбцов: "*" или список через запятую.</summary>
        private static List<string> ParseColumns(string columnsStr)
        {
            columnsStr = columnsStr.Trim();

            if (columnsStr == "*")
                return new List<string> { "*" };

            var columns = new List<string>();
            foreach (string col in columnsStr.Split(','))
                columns.Add(col.Trim());

            return columns;
        }
    }
}