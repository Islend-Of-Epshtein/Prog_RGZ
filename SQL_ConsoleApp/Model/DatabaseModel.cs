using System;
using System.Collections.Generic;
using SQL_ConsoleApp.Commands;
using SQL_ConsoleApp.Files;

namespace SQL_ConsoleApp.Model
{
    /// <summary>
    /// Модель базы данных — центральный компонент, управляющий таблицей и выполняющий SQL-команды.
    /// </summary>
    public class DatabaseModel
    {
        private TableManager _currentTable;
        private string _currentTableName;
        private List<object[]> _selectResult;

        /// <summary>
        /// Выполняет переданную SQL-команду.
        /// Для SELECT возвращает null (результат доступен через <see cref="GetSelectResult"/>),
        /// для остальных команд возвращает статус выполнения.
        /// </summary>
        /// <param name="command">Строка SQL-команды.</param>
        /// <returns>Сообщение о результате или null для SELECT.</returns>
        public string ExecuteCommand(string command)
        {
            command = command.Trim();

            // Команды, не требующие открытой таблицы
            if (command.StartsWith("CREATE TABLE", StringComparison.OrdinalIgnoreCase))
                return HandleCreateTable(command);

            if (command.StartsWith("OPEN", StringComparison.OrdinalIgnoreCase))
                return HandleOpen(command);

            if (command.StartsWith("CLOSE", StringComparison.OrdinalIgnoreCase))
                return HandleClose();

            if (command.StartsWith("DROP TABLE", StringComparison.OrdinalIgnoreCase))
                return HandleDropTable(command);

            // Все остальные команды требуют открытой таблицы
            EnsureTableOpened();

            return command switch
            {
                _ when Contains(command, "COLUMN ADD") => HandleAlterAdd(command),
                _ when Contains(command, "COLUMN REMOVE") => HandleAlterRemove(command),
                _ when Contains(command, "COLUMN RENAME") => HandleAlterRename(command),
                _ when Contains(command, "COLUMN UPDATE") => HandleAlterUpdate(command),
                _ when StartsWith(command, "INSERT") => HandleInsert(command),
                _ when StartsWith(command, "SELECT") => HandleSelect(command),
                _ when StartsWith(command, "UPDATE") => HandleUpdate(command),
                _ when StartsWith(command, "DELETE") => HandleDelete(command),
                _ when StartsWith(command, "TRUNCATE") => HandleTruncate(command),
                _ when StartsWith(command, "RESTORE") => HandleRestore(command),
                _ => throw new Exception("Неизвестная команда")
            };
        }

        /// <summary>Возвращает результат последнего SELECT-запроса.</summary>
        public List<object[]> GetSelectResult() => _selectResult;

        /// <summary>
        /// Возвращает структуру текущей таблицы: имя, тип, длина, точность, флаг NOT NULL для каждого поля.
        /// </summary>
        public List<(string Name, char Type, int Length, int Precision, bool NotNull)> GetTableStructure()
        {
            EnsureTableOpened();
            return _currentTable.GetFields();
        }

        /// <summary>Возвращает имя текущей открытой таблицы.</summary>
        public string GetTableName() => _currentTableName;

        /// <summary>Проверяет, открыта ли таблица в данный момент.</summary>
        public bool IsTableOpened() => _currentTable != null;

        /// <summary>
        /// Переименовывает таблицу: закрывает её (если открыта), переименовывает файлы .dbf/.dbt,
        /// и заново открывает с новым именем.
        /// </summary>
        /// <param name="oldName">Текущее имя таблицы.</param>
        /// <param name="newName">Новое имя таблицы.</param>
        public void RenameTable(string oldName, string newName)
        {
            if (string.IsNullOrEmpty(oldName) || string.IsNullOrEmpty(newName))
                throw new Exception("Имя таблицы не может быть пустым");

            if (oldName.Equals(newName))
                return;

            string oldDbf = $"{oldName}.dbf", newDbf = $"{newName}.dbf";
            string oldDbt = $"{oldName}.dbt", newDbt = $"{newName}.dbt";

            if (!System.IO.File.Exists(oldDbf))
                throw new Exception($"Таблица '{oldName}' не найдена");

            if (System.IO.File.Exists(newDbf))
                throw new Exception($"Таблица '{newName}' уже существует");

            bool isCurrent = IsCurrentTable(oldName);

            if (isCurrent)
                CloseTable();

            try
            {
                System.IO.File.Move(oldDbf, newDbf);
                if (System.IO.File.Exists(oldDbt))
                    System.IO.File.Move(oldDbt, newDbt);

                if (isCurrent)
                    OpenTable(newDbf);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при переименовании таблицы: {ex.Message}");
            }
        }

        /// <summary>Закрывает текущую таблицу, сохраняя изменения.</summary>
        public void CloseTable()
        {
            _currentTable?.Close();
            _currentTable = null;
            _currentTableName = null;
        }

        // ─────────────────────────── Приватные обработчики команд ───────────────────────────

        private string HandleCreateTable(string command)
        {
            var create = new CreateCommand(command);
            _currentTable = TableManager.Create(create.GetTableName(), create.GetRows());
            _currentTableName = create.GetTableName();
            return $"Таблица '{create.GetTableName()}' успешно создана.";
        }

        private string HandleOpen(string command)
        {
            var open = new OpenCommand(command);
            OpenTable(open.GetPath());
            return $"Таблица '{open.GetTableName()}' успешно открыта. Записей: {_currentTable.GetRecordCount()}";
        }

        private string HandleClose()
        {
            CloseTable();
            return "Таблица закрыта.";
        }

        private string HandleDropTable(string command)
        {
            var drop = new DropCommand(command);
            if (IsCurrentTable(drop.GetTableName()))
                CloseTable();
            TableManager.Drop(drop.GetTableName());
            return $"Таблица '{drop.GetTableName()}' удалена.";
        }

        private string HandleAlterAdd(string command)
        {
            var alter = new AlterAdd(command);
            _currentTable.AddColumn(alter);
            return $"Столбец '{alter.GetRowName()}' добавлен.";
        }

        private string HandleAlterRemove(string command)
        {
            var alter = new AlterRemove(command);
            _currentTable.RemoveColumn(alter.GetRowName());
            return $"Столбец '{alter.GetRowName()}' удалён.";
        }

        private string HandleAlterRename(string command)
        {
            var alter = new AlterRename(command);
            _currentTable.RenameColumn(alter.GetOldName(), alter.GetNewName());
            return $"Столбец '{alter.GetOldName()}' переименован в '{alter.GetNewName()}'.";
        }

        private string HandleAlterUpdate(string command)
        {
            var alter = new AlterUpdate(command);
            _currentTable.UpdateColumn(alter);
            return $"Столбец '{alter.GetRowName()}' изменён.";
        }

        private string HandleInsert(string command)
        {
            var insert = new InsertCommand(command);
            _currentTable.Insert(insert.GetColumns(), insert.GetValues());
            return "Запись добавлена.";
        }

        /// <summary>Выполняет SELECT и сохраняет результат в _selectResult. Возвращает null.</summary>
        private string HandleSelect(string command)
        {
            var select = new SelectCommand(command);
            _selectResult = _currentTable.Select(select);
            return null;
        }

        private string HandleUpdate(string command)
        {
            int count = _currentTable.Update(new UpdateCommand(command));
            return $"Обновлено записей: {count}";
        }

        private string HandleDelete(string command)
        {
            int count = _currentTable.Delete(new DeleteCommand(command));
            return $"Помечено на удаление записей: {count}";
        }

        private string HandleTruncate(string command)
        {
            int count = _currentTable.Truncate();
            return $"Физически удалено записей: {count}";
        }

        private string HandleRestore(string command)
        {
            int count = _currentTable.Restore(new RestoreCommand(command));
            return $"Восстановлено записей: {count}";
        }

        // ─────────────────────────────── Вспомогательные методы ───────────────────────────────

        /// <summary>Открывает таблицу по указанному пути к .dbf файлу.</summary>
        private void OpenTable(string dbfPath)
        {
            _currentTable = TableManager.Open(dbfPath);
            _currentTableName = System.IO.Path.GetFileNameWithoutExtension(dbfPath);
        }

        /// <summary>Проверяет, является ли переданное имя именем текущей открытой таблицы.</summary>
        private bool IsCurrentTable(string name) =>
            _currentTableName != null &&
            _currentTableName.Equals(name, StringComparison.OrdinalIgnoreCase);

        /// <summary>Проверяет, открыта ли таблица, и выбрасывает исключение, если нет.</summary>
        private void EnsureTableOpened()
        {
            if (_currentTable == null)
                throw new Exception("Не открыта ни одна таблица. Используйте OPEN <имя>");
        }

        /// <summary>Проверяет, начинается ли команда с указанной подстроки (без учёта регистра).</summary>
        private static bool StartsWith(string command, string value) =>
            command.StartsWith(value, StringComparison.OrdinalIgnoreCase);

        /// <summary>Проверяет, содержит ли команда указанную подстроку (без учёта регистра).</summary>
        private static bool Contains(string command, string value) =>
            command.Contains(value, StringComparison.OrdinalIgnoreCase);
    }
}