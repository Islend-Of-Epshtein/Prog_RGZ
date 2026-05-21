using System.Text.RegularExpressions;

namespace SQL_ConsoleApp.Commands
{
    /// <summary>
    /// Команда CLOSE — закрытие текущей открытой таблицы.
    /// Синтаксис: CLOSE;
    /// </summary>
    public class CloseCommand : ICommand
    {
        private const string Pattern = @"(?im)^\s*CLOSE\s*;$";

        private static readonly Regex CloseRegex = new(Pattern, RegexOptions.Compiled);

        private readonly Match _match;

        /// <summary>
        /// Разбирает команду CLOSE. Выбрасывает исключение при неверном синтаксисе.
        /// </summary>
        /// <param name="command">Строка SQL-команды.</param>
        public CloseCommand(string command)
        {
            _match = CloseRegex.Match(command);
            if (!_match.Success)
                throw new System.Exception("Неверный синтаксис команды CLOSE");
        }

        /// <summary>Возвращает пустую строку (команда не привязана к таблице).</summary>
        public string GetTableName() => "";
    }
}