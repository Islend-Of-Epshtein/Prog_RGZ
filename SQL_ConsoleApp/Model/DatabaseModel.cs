using System;
using System.Collections.Generic;
using SQL_ConsoleApp.Commands;
using SQL_ConsoleApp.Files;

namespace SQL_ConsoleApp.Model
{
    public class DatabaseModel
    {
        private TableManager _currentTable;
        private string _currentTableName;

        public DatabaseModel()
        {
            _currentTable = null;
            _currentTableName = null;
        }

        public string ExecuteCommand(string command)
        {
            command = command.Trim();

            // CREATE TABLE
            if (command.StartsWith("CREATE TABLE", StringComparison.OrdinalIgnoreCase))
            {
                var create = new CreateCommand(command);
                var tableManager = TableManager.Create(create.GetTableName(), create.GetRows());
                _currentTable = tableManager;
                _currentTableName = create.GetTableName();
                return $"Таблица '{create.GetTableName()}' успешно создана.";
            }

            // OPEN
            if (command.StartsWith("OPEN", StringComparison.OrdinalIgnoreCase))
            {
                var open = new OpenCommand(command);
                _currentTable = TableManager.Open(open.GetPath());
                _currentTableName = open.GetTableName();
                return $"Таблица '{open.GetTableName()}' успешно открыта. Записей: {_currentTable.GetRecordCount()}";
            }

            // CLOSE
            if (command.StartsWith("CLOSE", StringComparison.OrdinalIgnoreCase))
            {
                CloseTable();
                return "Таблица закрыта.";
            }

            // DROP TABLE
            if (command.StartsWith("DROP TABLE", StringComparison.OrdinalIgnoreCase))
            {
                var drop = new DropCommand(command);
                if (_currentTableName != null && _currentTableName.Equals(drop.GetTableName(), StringComparison.OrdinalIgnoreCase))
                {
                    _currentTable = null;
                    _currentTableName = null;
                }
                TableManager.Drop(drop.GetTableName());
                return $"Таблица '{drop.GetTableName()}' удалена.";
            }

            // Проверяем, открыта ли таблица для остальных команд
            if (_currentTable == null)
                throw new Exception("Не открыта ни одна таблица. Используйте OPEN <имя>");

            // ALTER TABLE ADD
            if (command.Contains("COLUMN ADD", StringComparison.OrdinalIgnoreCase))
            {
                var alterAdd = new AlterAdd(command);
                _currentTable.AddColumn(alterAdd);
                return $"Столбец '{alterAdd.GetRowName()}' добавлен.";
            }

            // ALTER TABLE REMOVE
            if (command.Contains("COLUMN REMOVE", StringComparison.OrdinalIgnoreCase))
            {
                var alterRemove = new AlterRemove(command);
                _currentTable.RemoveColumn(alterRemove.GetRowName());
                return $"Столбец '{alterRemove.GetRowName()}' удалён.";
            }

            // ALTER TABLE RENAME
            if (command.Contains("COLUMN RENAME", StringComparison.OrdinalIgnoreCase))
            {
                var alterRename = new AlterRename(command);
                _currentTable.RenameColumn(alterRename.GetOldName(), alterRename.GetNewName());
                return $"Столбец '{alterRename.GetOldName()}' переименован в '{alterRename.GetNewName()}'.";
            }

            // ALTER TABLE UPDATE
            if (command.Contains("COLUMN UPDATE", StringComparison.OrdinalIgnoreCase))
            {
                var alterUpdate = new AlterUpdate(command);
                _currentTable.UpdateColumn(alterUpdate);
                return $"Столбец '{alterUpdate.GetRowName()}' изменён.";
            }

            // INSERT
            if (command.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
            {
                var insert = new InsertCommand(command);
                _currentTable.Insert(insert.GetColumns(), insert.GetValues());
                return "Запись добавлена.";
            }

            // SELECT
            if (command.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            {
                var select = new SelectCommand(command);
                return _currentTable.Select(select);
            }

            // UPDATE
            if (command.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase))
            {
                var update = new UpdateCommand(command);
                int count = _currentTable.Update(update);
                return $"Обновлено записей: {count}";
            }

            // DELETE
            if (command.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase))
            {
                var delete = new DeleteCommand(command);
                int count = _currentTable.Delete(delete);
                return $"Помечено на удаление записей: {count}";
            }

            // TRUNCATE
            if (command.StartsWith("TRUNCATE", StringComparison.OrdinalIgnoreCase))
            {
                var truncate = new TruncateCommand(command);
                int count = _currentTable.Truncate();
                return $"Физически удалено записей: {count}";
            }

            // RESTORE
            if (command.StartsWith("RESTORE", StringComparison.OrdinalIgnoreCase))
            {
                var restore = new RestoreCommand(command);
                int count = _currentTable.Restore(restore);
                return $"Восстановлено записей: {count}";
            }

            throw new Exception("Неизвестная команда");
        }

        public void CloseTable()
        {
            if (_currentTable != null)
            {
                _currentTable.Close();
                _currentTable = null;
                _currentTableName = null;
            }
        }
    }
}