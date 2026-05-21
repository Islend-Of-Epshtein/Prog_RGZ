using System;
using System.IO;
using System.Text;
using SQL_ConsoleApp.Model;

namespace SQL_ConsoleApp.CLI
{
    public static class ConsoleInterface
    {
        private static readonly DatabaseModel _model = new DatabaseModel();
        private static readonly StringBuilder _commandBuffer = new StringBuilder();
        private static bool _waitingForMore = false;

        // Для сохранения истории вывода
        private static readonly StringBuilder _outputHistory = new StringBuilder();

        public static void Run()
        {
            Console.BufferWidth = Math.Max(Console.BufferWidth, 500);
            Console.WriteLine("SQL Interpreter");
            Console.WriteLine("Type '/?' для вывода списка команд, 'EXIT;' для выхода\nвсе команды заканчиваются (;), кроме \"/?\" - помощь");
            Console.WriteLine();

            while (true)
            {
                if (!_waitingForMore)
                    Console.Write("SQL> ");
                else
                    Console.Write("...> ");

                string input = Console.ReadLine();
                if (input == null) continue;

                _commandBuffer.AppendLine(input);

                if (!input.Trim().EndsWith(";") && !input.Trim().Equals("/?"))
                {
                    _waitingForMore = true;
                    continue;
                }

                _waitingForMore = false;
                string fullCommand = _commandBuffer.ToString().Trim();
                _commandBuffer.Clear();

                if (string.IsNullOrWhiteSpace(fullCommand))
                    continue;

                try
                {
                    if (fullCommand.StartsWith("/?"))
                    {
                        HandleHelp(fullCommand);
                        continue;
                    }

                    if (fullCommand.Equals("EXIT;", StringComparison.OrdinalIgnoreCase))
                    {
                        _model.CloseTable();
                        Console.WriteLine("Завершение работы.");
                        break;
                    }

                    // Проверяем, нужно ли подтверждение для команды
                    if (NeedConfirmation(fullCommand))
                    {
                        Console.Write("Предупреждение: эта операция может изменить структуру таблицы или удалить данные. Продолжить? (Y/N): ");
                        string answer = Console.ReadLine()?.Trim().ToUpper();
                        if (answer != "Y" && answer != "YES")
                        {
                            Console.WriteLine("Операция отменена.");
                            continue;
                        }
                    }
                    string result = _model.ExecuteCommand(fullCommand);
                    // Проверяем, является ли команда SELECT
                    if (result == null)
                    {
                        var data = _model.GetSelectResult();
                        var structure = _model.GetTableStructure();
                        string output = FormatSelectResult(data, structure);
                        Console.WriteLine(output);
                        _outputHistory.AppendLine(output);
                    }
                    else
                    {
                        Console.WriteLine(result);
                        _outputHistory.AppendLine(result);
                    }
                }
                catch (Exception ex)
                {
                    string errorMsg = $"Ошибка: {ex.Message}";
                    Console.WriteLine(errorMsg);
                    _outputHistory.AppendLine(errorMsg);
                }
            }
        }

        private static string FormatSelectResult(List<object[]> data, List<(string Name, char Type, int Length, int Precision, bool NotNull)> structure)
        {
            if (structure == null || structure.Count == 0)
                return "Нет столбцов для отображения.";

            var sb = new StringBuilder();

            // Определяем ширину колонок
            int[] columnWidths = new int[structure.Count];

            // Заголовки
            for (int i = 0; i < structure.Count; i++)
            {
                columnWidths[i] = structure[i].Name.Length;
            }
            if (data != null)
            {
                // Данные
                foreach (var row in data)
                {
                    for (int i = 0; i < Math.Min(row.Length, structure.Count); i++)
                    {
                        string formattedValue = FormatValueForDisplay(row[i], structure[i]);
                        columnWidths[i] = Math.Max(columnWidths[i], formattedValue.Length);
                    }
                }
            }
            // Минимальная ширина колонки
            for (int i = 0; i < columnWidths.Length; i++)
            {
                columnWidths[i] = Math.Max(columnWidths[i], 10);
            }

            // Выводим заголовки
            for (int i = 0; i < structure.Count; i++)
            {
                sb.Append(structure[i].Name.PadRight(columnWidths[i] + 2));
            }
            sb.AppendLine();

            // Разделитель
            for (int i = 0; i < structure.Count; i++)
            {
                sb.Append(new string('-', columnWidths[i]) + "  ");
            }
            sb.AppendLine();
            if(data!=null)
            {
            // Данные
                foreach (var row in data)
                {
                    for (int i = 0; i < Math.Min(row.Length, structure.Count); i++)
                    {
                        string formattedValue = FormatValueForDisplay(row[i], structure[i]);
                        sb.Append(formattedValue.PadRight(columnWidths[i] + 2));
                    }
                    sb.AppendLine();
                }

                sb.AppendLine($"\nВсего записей: {data.Count}");
            }
            return sb.ToString();
        }

        private static string FormatValueForDisplay(object value, (string Name, char Type, int Length, int Precision, bool NotNull) field)
        {
            if (value == null)
                return "NULL";

            return field.Type switch
            {
                'D' => value is DateTime date ? date.ToString("dd.MM.yyyy") : value.ToString(),
                'L' => value is bool boolVal ? (boolVal ? "TRUE" : "FALSE") : value.ToString(),
                'N' => value is double numVal ? numVal.ToString($"F{field.Precision}") : value.ToString(),
                'M' => value?.ToString() ?? "",
                'C' => value?.ToString() ?? "",
                _ => value?.ToString() ?? ""
            };
        }

        private static bool NeedConfirmation(string command)
        {
            string upperCommand = command.ToUpperInvariant();

            // Команды, требующие подтверждения
            return upperCommand.Contains(@"ALTER\s+TABLE") ||
                   upperCommand.Contains(@"DROP\s+TABLE") ||
                   upperCommand.Contains("TRUNCATE") ||
                   (upperCommand.Contains(@"DELETE\s+FROM") && !upperCommand.Contains("WHERE"));
        }

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

Логические операторы в WHERE: AND, OR, XOR, NOT
Операторы сравнения: =, <>, <, >, <=, >=";

            if (command.Contains(">"))
            {
                int idx = command.IndexOf('>');
                string filename = command.Substring(idx + 1).Trim();
                try
                {
                    File.WriteAllText(filename, helpText, Encoding.UTF8);
                    string saveMsg = $"Справка сохранена в файл: {filename}";
                    Console.WriteLine(saveMsg);
                    _outputHistory.AppendLine(saveMsg);
                }
                catch (Exception ex)
                {
                    string errorMsg = $"Ошибка при сохранении справки: {ex.Message}";
                    Console.WriteLine(errorMsg);
                    _outputHistory.AppendLine(errorMsg);
                }
            }
            else
            {
                Console.WriteLine(helpText);
                _outputHistory.AppendLine(helpText);
            }
        }
    }
}