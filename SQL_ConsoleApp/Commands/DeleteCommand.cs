using System.Text.RegularExpressions;

namespace SQL_ConsoleApp.Commands
{
    /// <summary>
    /// Команда DELETE — логическое удаление записей из таблицы.
    /// Синтаксис: DELETE FROM &lt;имя_таблицы&gt; [WHERE &lt;условие&gt;];
    /// </summary>
    public class DeleteCommand : ICommand
    {
        private const string Pattern =
            @"(?im)^\s*DELETE\s+FROM\s+(?<tableName>\w+)\s*" +
            @"(?:WHERE\s+(?<logicCommand>[^;]+))?\s*;$";

        private static readonly Regex DeleteRegex = new(Pattern, RegexOptions.Compiled);

        private readonly Match _match;
        private readonly LogicExpressionParser _whereParser;

        /// <summary>
        /// Разбирает команду DELETE. Выбрасывает исключение при неверном синтаксисе.
        /// </summary>
        /// <param name="command">Строка SQL-команды.</param>
        public DeleteCommand(string command)
        {
            _match = DeleteRegex.Match(command);
            if (!_match.Success)
                throw new System.Exception("Неверный синтаксис команды DELETE");

            string whereClause = _match.Groups["logicCommand"].Value;
            if (!string.IsNullOrWhiteSpace(whereClause))
                _whereParser = new LogicExpressionParser(whereClause);
        }

        /// <summary>Возвращает имя таблицы.</summary>
        public string GetTableName() => _match.Groups["tableName"].Value;

        /// <summary>Проверяет наличие WHERE-условия.</summary>
        public bool HasWhereCondition() => _whereParser != null;

        /// <summary>Возвращает парсер WHERE-условия или null, если условие отсутствует.</summary>
        public LogicExpressionParser GetWhereParser() => _whereParser;
    }
}