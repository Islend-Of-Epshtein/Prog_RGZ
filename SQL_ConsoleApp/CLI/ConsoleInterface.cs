using System.Text;
using SQL_ConsoleApp.Model;

namespace SQL_ConsoleApp.CLI
{
    /// <summary>
    /// Консольный интерфейс SQL-интерпретатора.
    /// Обеспечивает ввод команд, их выполнение и форматированный вывод результатов.
    /// </summary>
    public static class ConsoleInterface
    {
        private static readonly DatabaseModel Model = new();
        private static readonly StringBuilder CommandBuffer = new();
        private static readonly StringBuilder OutputHistory = new();
        private static bool _waitingForMore;

        private const int MinColumnWidth = 10;
        private const int ColumnPadding = 2;

        /// <summary>Запускает главный цикл обработки команд.</summary>
        public static void Run()
        {
            Console.BufferWidth = Math.Max(Console.BufferWidth, 500);
            PrintWelcome();

            while (true)
            {
                string input = ReadCommand();
                if (input == null) continue;

                if (!IsCommandComplete(input))
                {
                    _waitingForMore = true;
                    continue;
                }

                _waitingForMore = false;
                string fullCommand = CommandBuffer.ToString().Trim();
                CommandBuffer.Clear();

                if (string.IsNullOrWhiteSpace(fullCommand))
                    continue;

                if (ProcessSpecialCommand(fullCommand))
                    break;

                try
                {
                    ExecuteAndDisplay(fullCommand);
                }
                catch (Exception ex)
                {
                    WriteError($"Ошибка: {ex.Message}");
                }
            }
        }

        // ──────────────────────────────── Командный цикл ────────────────────────────────

        /// <summary>Выводит приветственное сообщение.</summary>
        private static void PrintWelcome()
        {
            Console.WriteLine(@"
SQL ИНТЕРПРЕТАТОР                   
Консольное приложение для работы с DBF-файлами               
    Программа поддерживает создание, открытие, изменение структуры
    и управление данными таблиц формата dBASE III+ (.dbf) с поддержкой
    MEMO-полей (.dbt). Реализован базовый синтаксис SQL-команд.
- Основные возможности
  • Создание и удаление таблиц (CREATE TABLE, DROP TABLE)
  • Открытие существующих DBF-файлов (OPEN)
  • Просмотр данных (SELECT с WHERE-условиями)
  • Добавление, изменение, удаление записей (INSERT, UPDATE, DELETE)
  • Физическое удаление помеченных записей (TRUNCATE)
  • Восстановление удалённых записей (RESTORE)
  • Изменение структуры таблицы: 
    добавление, удаление, переименование и изменение типа столбцов
    (ALTER TABLE ... COLUMN ADD/REMOVE/RENAME/UPDATE)
- Поддерживаемые типы данных
      C(n)    — Строка фиксированной длины (n символов)
      N(n,d)  — Число: всего n знаков, d знаков после запятой
      D       — Дата (хранится как ГГГГММДД, отображается ДД.ММ.ГГГГ)
      L       — Логический (TRUE/FALSE)
      M       — MEMO-поле (текст до 4 ГБ во внешнем .dbt файле)
- Управление программой
      Все команды завершаются точкой с запятой (;).
      Многострочный ввод поддерживается автоматически.
      Для выхода введите EXIT;
      Для справки по всем командам введите /?
      Для сохранения справки в файл введите /? > имя_файла
");
            Console.WriteLine();
        }

        /// <summary>Читает строку ввода с соответствующим приглашением.</summary>
        private static string ReadCommand()
        {
            Console.Write(_waitingForMore ? "...> " : "SQL> ");
            string input = Console.ReadLine();
            if (input != null)
                CommandBuffer.AppendLine(input);
            return input;
        }

        /// <summary>Проверяет, завершена ли команда (заканчивается на ; или является /?).</summary>
        private static bool IsCommandComplete(string input) =>
            input.Trim().EndsWith(";") || input.Trim().Equals("/?");

        /// <summary>
        /// Обрабатывает специальные команды: /?, EXIT;.
        /// </summary>
        /// <returns>true, если нужно завершить программу.</returns>
        private static bool ProcessSpecialCommand(string command)
        {
            if (command.StartsWith("/?"))
            {
                HandleHelp(command);
                return false;
            }

            if (command.Equals("EXIT;", StringComparison.OrdinalIgnoreCase))
            {
                Model.CloseTable();
                Console.WriteLine("Завершение работы.");
                return true;
            }

            return false;
        }

        /// <summary>Выполняет SQL-команду и отображает результат.</summary>
        private static void ExecuteAndDisplay(string command)
        {
            if (NeedConfirmation(command) && !ConfirmDangerousOperation())
            {
                Console.WriteLine("Операция отменена.");
                return;
            }

            string result = Model.ExecuteCommand(command);

            if (result == null) // SELECT-запрос
            {
                string output = FormatSelectResult(Model.GetSelectResult(), Model.GetTableStructure());
                Console.WriteLine(output);
                OutputHistory.AppendLine(output);
            }
            else
            {
                Console.WriteLine(result);
                OutputHistory.AppendLine(result);
            }
        }

        // ──────────────────────────────── Форматирование SELECT ────────────────────────────────

        /// <summary>
        /// Форматирует результат SELECT в читаемую таблицу с автоматическим подбором ширины колонок.
        /// </summary>
        /// <param name="data">Список строк данных.</param>
        /// <param name="structure">Структура таблицы.</param>
        /// <returns>Отформатированная строка для вывода в консоль.</returns>
        private static string FormatSelectResult(List<object[]> data,
            List<(string Name, char Type, int Length, int Precision, bool NotNull)> structure)
        {
            if (structure == null || structure.Count == 0)
                return "Нет столбцов для отображения.";

            int[] columnWidths = CalculateColumnWidths(data, structure);
            var sb = new StringBuilder();

            AppendRow(sb, structure.Select(s => s.Name), columnWidths);
            AppendSeparator(sb, columnWidths);

            if (data != null)
            {
                foreach (var row in data)
                    AppendRow(sb, FormatRow(row, structure), columnWidths);

                sb.AppendLine($"\nВсего записей: {data.Count}");
            }

            return sb.ToString();
        }

        /// <summary>Вычисляет оптимальную ширину каждой колонки на основе заголовков и данных.</summary>
        private static int[] CalculateColumnWidths(List<object[]> data,
            List<(string Name, char Type, int Length, int Precision, bool NotNull)> structure)
        {
            int[] widths = structure.Select(s => Math.Max(s.Name.Length, MinColumnWidth)).ToArray();

            if (data == null) return widths;

            for (int rowIdx = 0; rowIdx < data.Count; rowIdx++)
            {
                for (int colIdx = 0; colIdx < Math.Min(data[rowIdx].Length, structure.Count); colIdx++)
                {
                    int valueLen = FormatValueForDisplay(data[rowIdx][colIdx], structure[colIdx]).Length;
                    widths[colIdx] = Math.Max(widths[colIdx], valueLen);
                }
            }

            return widths;
        }

        /// <summary>Форматирует строку данных для отображения.</summary>
        private static IEnumerable<string> FormatRow(object[] row,
            List<(string Name, char Type, int Length, int Precision, bool NotNull)> structure)
        {
            for (int i = 0; i < Math.Min(row.Length, structure.Count); i++)
                yield return FormatValueForDisplay(row[i], structure[i]);
        }

        /// <summary>Добавляет строку значений с выравниванием по ширине колонок.</summary>
        private static void AppendRow(StringBuilder sb, IEnumerable<string> values, int[] widths)
        {
            int i = 0;
            foreach (var value in values)
            {
                sb.Append(value.PadRight(widths[i] + ColumnPadding));
                i++;
            }
            sb.AppendLine();
        }

        /// <summary>Добавляет строку-разделитель из дефисов.</summary>
        private static void AppendSeparator(StringBuilder sb, int[] widths)
        {
            foreach (int w in widths)
                sb.Append(new string('-', w) + "  ");
            sb.AppendLine();
        }

        /// <summary>Форматирует одно значение для отображения в зависимости от типа поля.</summary>
        private static string FormatValueForDisplay(object value,
            (string Name, char Type, int Length, int Precision, bool NotNull) field)
        {
            if (value == null) return "NULL";

            return field.Type switch
            {
                'D' => value is DateTime date ? date.ToString("dd.MM.yyyy") : value.ToString(),
                'L' => value is bool b ? (b ? "TRUE" : "FALSE") : value.ToString(),
                'N' => value is double d ? d.ToString($"F{field.Precision}") : value.ToString(),
                _ => value.ToString() ?? ""
            };
        }

        // ──────────────────────────────── Подтверждение операций ────────────────────────────────

        /// <summary>Проверяет, требует ли команда подтверждения (ALTER, DROP, TRUNCATE, DELETE без WHERE).</summary>
        private static bool NeedConfirmation(string command)
        {
            string upper = command.ToUpperInvariant();
            return upper.Contains("ALTER TABLE") ||
                   upper.Contains("DROP TABLE") ||
                   upper.Contains("TRUNCATE") ||
                   (upper.Contains("DELETE FROM") && !upper.Contains("WHERE"));
        }

        /// <summary>Запрашивает подтверждение у пользователя.</summary>
        private static bool ConfirmDangerousOperation()
        {
            Console.Write("Предупреждение: эта операция может изменить структуру таблицы или удалить данные. Продолжить? (Y/N): ");
            string answer = Console.ReadLine()?.Trim().ToUpper();
            return answer == "Y" || answer == "YES";
        }

        // ──────────────────────────────────── Справка ────────────────────────────────────

        /// <summary>Выводит справку или сохраняет её в файл при использовании синтаксиса `/?>имя_файла`.</summary>
        private static void HandleHelp(string command)
        {
            string helpText = @"
SQL COMMANDS REFERENCE

CREATE TABLE <имя> (<поле1> <тип> [NOT NULL], <поле2> <тип> [NOT NULL], ...);
  Создаёт новую таблицу.
  Типы: C(n) - строка, D - дата, L - логический, N(n,d) - число, M - MEMO

OPEN <имя_файла>;
  Открывает существующую таблицу.

CLOSE;
  Закрывает текущую таблицу.

ALTER TABLE <имя> COLUMN ADD <поле> <тип> [NOT NULL];
  Добавляет новый столбец.

ALTER TABLE <имя> COLUMN REMOVE <поле>;
  Удаляет столбец.

ALTER TABLE <имя> COLUMN RENAME <старое> <новое>;
  Переименовывает столбец.

ALTER TABLE <имя> COLUMN UPDATE <поле> <новый_тип> [NOT NULL];
  Изменяет тип столбца.

INSERT INTO <имя> (<поля>) VALUE (<значения>);
  Добавляет новую запись.

UPDATE <имя> SET <поле>=<значение> [WHERE <условие>];
  Обновляет записи.

DELETE FROM <имя> [WHERE <условие>];
  Логически удаляет записи (помечает *).

SELECT <*|поле1,поле2,...> FROM <имя> [WHERE <условие>];
  Выбирает записи.

TRUNCATE <имя>;
  Физически удаляет помеченные записи.

RESTORE <имя> [WHERE <условие>];
  Восстанавливает удалённые записи.

DROP TABLE <имя>;
  Удаляет таблицу и связанные файлы.

EXIT;
  Завершает работу.

Логический тип: true, flase, y, n;
Логические операторы в WHERE: AND, OR, XOR, NOT
Операторы сравнения: =, <>, <, >, <=, >=";

            if (command.Contains('>'))
            {
                string filename = command[(command.IndexOf('>') + 1)..].Trim();
                SaveHelpToFile(filename, helpText);
            }
            else
            {
                Console.WriteLine(helpText);
                OutputHistory.AppendLine(helpText);
            }
        }

        /// <summary>Сохраняет текст справки в файл.</summary>
        private static void SaveHelpToFile(string filename, string helpText)
        {
            try
            {
                File.WriteAllText(filename, helpText, Encoding.UTF8);
                WriteSuccess($"Справка сохранена в файл: {filename}");
            }
            catch (Exception ex)
            {
                WriteError($"Ошибка при сохранении справки: {ex.Message}");
            }
        }

        // ──────────────────────────────── Вывод сообщений ────────────────────────────────

        private static void WriteError(string message)
        {
            Console.WriteLine(message);
            OutputHistory.AppendLine(message);
        }

        private static void WriteSuccess(string message)
        {
            Console.WriteLine(message);
            OutputHistory.AppendLine(message);
        }
    }
}