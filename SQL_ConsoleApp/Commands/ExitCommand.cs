using System.Text.RegularExpressions;

namespace SQL_ConsoleApp.Commands
{
    /// <summary>
    /// Команда EXIT — завершение работы интерпретатора.
    /// Синтаксис: EXIT;
    /// </summary>
    public class ExitCommand : ICommand
    {
        private const string Pattern = @"(?im)^\s*EXIT\s*;$";

        private static readonly Regex ExitRegex = new(Pattern, RegexOptions.Compiled);

        private readonly Match _match;

        /// <summary>
        /// Разбирает команду EXIT. Выбрасывает исключение при неверном синтаксисе.
        /// </summary>
        /// <param name="command">Строка SQL-команды.</param>
        public ExitCommand(string command)
        {
            _match = ExitRegex.Match(command);
            if (!_match.Success)
                throw new System.Exception("Неверный синтаксис команды EXIT");
        }

        /// <summary>Возвращает пустую строку (команда не привязана к таблице).</summary>
        public string GetTableName() => "";
    }
}