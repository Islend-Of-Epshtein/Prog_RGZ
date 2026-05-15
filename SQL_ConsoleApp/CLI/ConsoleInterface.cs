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
            Console.WriteLine("SQL Interpreter");
            Console.WriteLine("Type '/?' для вывода списка команд, 'EXIT;' для выхода");
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

                if (!input.Trim().EndsWith(";"))
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

                    string result = _model.ExecuteCommand(fullCommand);
                    if (!string.IsNullOrEmpty(result))
                    {
                        Console.WriteLine(result);
                        // Сохраняем в историю (опционально)
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