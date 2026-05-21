using System.IO;
using System.Text.RegularExpressions;

namespace SQL_ConsoleApp.Commands
{
    /// <summary>
    /// Команда OPEN — открытие существующей таблицы из .dbf файла.
    /// Синтаксис: OPEN &lt;путь_к_файлу&gt;;
    /// </summary>
    public class OpenCommand : ICommand
    {
        private const string Pattern = @"(?im)^\s*OPEN\s+(?<filePath>[^\s;]+)\s*;$";

        private static readonly Regex OpenRegex = new(Pattern, RegexOptions.Compiled);

        private readonly Match _match;

        /// <summary>
        /// Разбирает команду OPEN. Выбрасывает исключение при неверном синтаксисе.
        /// </summary>
        /// <param name="command">Строка SQL-команды.</param>
        public OpenCommand(string command)
        {
            _match = OpenRegex.Match(command);
            if (!_match.Success)
                throw new System.Exception("Неверный синтаксис команды OPEN");
        }

        /// <summary>Возвращает полный путь к файлу из команды.</summary>
        public string GetPath() => _match.Groups["filePath"].Value;

        /// <summary>Возвращает имя таблицы (имя файла без расширения).</summary>
        public string GetTableName() => Path.GetFileNameWithoutExtension(GetPath());

        /// <summary>Проверяет, что файл имеет расширение .dbf.</summary>
        public bool CheckExtension() => GetPath().EndsWith(".dbf");
    }
}