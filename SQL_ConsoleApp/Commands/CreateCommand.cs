using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SQL_ConsoleApp.Commands
{
    /// <summary>
    /// Определение столбца таблицы: тип, имя, размеры и флаг NOT NULL.
    /// </summary>
    public struct RowDefinition
    {
        public char Type;
        public string Name;
        public int Width;
        public int Precision;
        public bool IsNotNull;

        /// <summary>
        /// Создаёт определение столбца с автоматическим расчётом ширины в зависимости от типа.
        /// </summary>
        /// <param name="type">Тип столбца (C, N, D, L, M).</param>
        /// <param name="name">Имя столбца.</param>
        /// <param name="isNotNull">Флаг NOT NULL.</param>
        /// <param name="width">Ширина (обязательна для C и N).</param>
        /// <param name="precision">Точность (обязательна для N).</param>
        public RowDefinition(char type, string name, bool isNotNull, string width = "", string precision = "")
        {
            Type = type;
            Name = name;
            IsNotNull = isNotNull;
            (Width, Precision) = CalculateDimensions(type, width, precision);
        }

        /// <summary>Вычисляет ширину и точность столбца на основе типа и переданных параметров.</summary>
        private static (int width, int precision) CalculateDimensions(char type, string width, string precision) => type switch
        {
            'C' => (ParseRequired(width, "ширина"), 0),
            'N' => (ParseRequired(width, "ширина"), ParseRequired(precision, "точность")),
            'D' => (8, 0),
            'L' => (1, 0),
            'M' => (10, 0),
            _ => (0, 0)
        };

        /// <summary>Парсит обязательный строковый параметр в int. Выбрасывает исключение при неудаче.</summary>
        private static int ParseRequired(string value, string paramName)
        {
            if (string.IsNullOrEmpty(value))
                throw new Exception($"Для типа C/N необходима {paramName}");
            return int.Parse(value);
        }
    }

    /// <summary>
    /// Команда CREATE TABLE — создание новой таблицы с заданными столбцами.
    /// Синтаксис: CREATE TABLE &lt;имя&gt; (&lt;столбец1&gt; &lt;тип&gt; [NOT NULL], ...);
    /// </summary>
    public class CreateCommand : ICommand
    {
        private const string TablePattern =
            @"(?im)^\s*CREATE\s+TABLE\s+(?<tableName>\w+)\s*\((?<rowsDescription>.*?)\)\s*;$";

        private const string RowPattern =
            @"(?im)\s*(?<rowName>\w+)\s+" +
            @"(?:(?<type>C)\s*\(\s*(?<width>\d+)\s*\)" +
            @"|(?<type>D)" +
            @"|(?<type>L)" +
            @"|(?<type>N)\s*\(\s*(?<width>\d+)\s*,\s*(?<precision>\d+)\s*\)" +
            @"|(?<type>M))" +
            @"(?:\s+NOT\s+NULL)?";

        private static readonly Regex CreateRegex = new(TablePattern, RegexOptions.Compiled);
        private static readonly Regex RowRegex = new(RowPattern, RegexOptions.Compiled);

        private readonly Match _match;
        private readonly RowDefinition[] _rows;

        /// <summary>
        /// Разбирает команду CREATE TABLE. Выбрасывает исключение при неверном синтаксисе.
        /// </summary>
        /// <param name="command">Строка SQL-команды.</param>
        public CreateCommand(string command)
        {
            _match = CreateRegex.Match(command);
            if (!_match.Success)
                throw new System.Exception("Неверный синтаксис команды CREATE TABLE");

            _rows = ParseRows(_match.Groups["rowsDescription"].Value);
        }

        /// <summary>Возвращает имя создаваемой таблицы.</summary>
        public string GetTableName() => _match.Groups["tableName"].Value;

        /// <summary>Возвращает массив определений столбцов.</summary>
        public RowDefinition[] GetRows() => _rows;

        /// <summary>Разбирает описание столбцов, разделённых запятыми (с учётом вложенных скобок).</summary>
        private static RowDefinition[] ParseRows(string rowsDescription)
        {
            string[] rowStrings = SplitRespectingParentheses(rowsDescription);
            var rows = new RowDefinition[rowStrings.Length];

            for (int i = 0; i < rowStrings.Length; i++)
            {
                Match match = RowRegex.Match(rowStrings[i]);
                if (!match.Success)
                    throw new System.Exception($"Ошибка синтаксиса в описании столбца: {rowStrings[i]}");

                rows[i] = new RowDefinition(
                    match.Groups["type"].Value[0],
                    match.Groups["rowName"].Value,
                    match.Value.Contains("NOT NULL", StringComparison.OrdinalIgnoreCase),
                    GetOptionalGroup(match, "width"),
                    GetOptionalGroup(match, "precision")
                );
            }

            return rows;
        }

        /// <summary>Разделяет строку по запятым, игнорируя запятые внутри скобок.</summary>
        private static string[] SplitRespectingParentheses(string str)
        {
            var result = new List<string>();
            int depth = 0, start = 0;

            for (int i = 0; i < str.Length; i++)
            {
                switch (str[i])
                {
                    case '(': depth++; break;
                    case ')': depth--; break;
                    case ',' when depth == 0:
                        result.Add(str[start..i].Trim());
                        start = i + 1;
                        break;
                }
            }

            result.Add(str[start..].Trim());
            return result.Where(s => !string.IsNullOrEmpty(s)).ToArray();
        }

        /// <summary>Возвращает значение именованной группы, если она найдена, иначе пустую строку.</summary>
        private static string GetOptionalGroup(Match match, string groupName) =>
            match.Groups[groupName].Success ? match.Groups[groupName].Value : "";
    }
}