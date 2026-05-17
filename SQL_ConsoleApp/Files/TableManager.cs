using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SQL_ConsoleApp.Commands;

namespace SQL_ConsoleApp.Files
{
    public class TableManager
    {
        private readonly string _filePath;
        private DbfHeader _header;
        private List<DbfRecord> _records;
        private DbtManager _dbtManager;

        public TableManager(string filePath)
        {
            _filePath = filePath;
        }

        public static TableManager Create(string tableName, RowDefinition[] rows)
        {
            string filePath = $"{tableName}.dbf";
            var manager = new TableManager(filePath);
            manager._header = DbfHeader.Create(rows);
            manager._records = new List<DbfRecord>();
            manager._dbtManager = manager.HasMemoField() ? DbtManager.Create(tableName) : null;
            manager.Save();
            return manager;
        }

        public static TableManager Open(string filePath)
        {
            if (!File.Exists(filePath))
                throw new Exception($"Файл {filePath} не найден");

            var manager = new TableManager(filePath);
            manager.Load();
            return manager;
        }

        public static void Drop(string tableName)
        {
            string dbfPath = $"{tableName}.dbf";
            string dbtPath = $"{tableName}.dbt";

            if (File.Exists(dbfPath))
                File.Delete(dbfPath);
            if (File.Exists(dbtPath))
                File.Delete(dbtPath);
        }

        private void Load()
        {
            using (var reader = new BinaryReader(File.OpenRead(_filePath), Encoding.ASCII))
            {
                _header = DbfHeader.Read(reader);
                _records = DbfRecord.ReadAll(reader, _header);
            }

            if (HasMemoField())
                _dbtManager = DbtManager.Open(Path.GetFileNameWithoutExtension(_filePath));
        }

        public void Save()
        {
            using (var writer = new BinaryWriter(File.Create(_filePath), Encoding.ASCII))
            {
                _header.Write(writer);
                DbfRecord.WriteAll(writer, _records, _header);
                writer.Write((byte)0x1A);
            }

            if (_dbtManager != null)
                _dbtManager.Save();
        }

        public void Close()
        {
            Save();
        }

        private bool HasMemoField()
        {
            return _header.Fields.Any(f => f.Type == 'M');
        }
        public List<(string Name, char Type, int Length, int Precision, bool NotNull)> GetFields()
        {
            return _header.Fields.Select(f => (f.Name.TrimEnd('\0'), f.Type, f.Length, (int)f.DecimalCount, f.NotNull)).ToList();
        }
        public int GetRecordCount() => _records.Count;

        public void Insert(List<string> columns, List<string> values)
        {
            var record = new DbfRecord(_header.Fields.Count);

            for (int i = 0; i < _header.Fields.Count; i++)
            {
                var field = _header.Fields[i];
                int colIndex = columns.FindIndex(c => c.Equals(field.Name.TrimEnd('\0'), StringComparison.OrdinalIgnoreCase));

                if (colIndex >= 0)
                {
                    string value = values[colIndex];
                    record.SetValue(i, value, field, _dbtManager);
                }
                else if (field.NotNull)
                {
                    throw new Exception($"Поле '{field.Name}' не может быть NULL");
                }
                else
                {
                    record.SetValue(i, GetDefaultValue(field), field, _dbtManager);
                }
            }

            _records.Add(record);
            _header.RecordCount = _records.Count;
        }

        private string GetDefaultValue(DbfField field)
        {
            return field.Type switch
            {
                'C' => new string(' ', field.Length),
                'N' => new string(' ', field.Length),
                'D' => "        ",
                'L' => "?",
                'M' => "          ",
                _ => ""
            };
        }

        public void AddColumn(AlterAdd add)
        {
            var newField = new DbfField
            {
                Name = add.GetRowName().PadRight(11, '\0'),
                Type = add.GetType(),
                Length = add.GetType() == 'C' ? int.Parse(add.GetWidth()) :
                         add.GetType() == 'N' ? int.Parse(add.GetWidth()) : 1,
                DecimalCount = add.GetType() == 'N' ? byte.Parse(add.GetPrecision()) : (byte)0,
                NotNull = add.IsNotNull()
            };

            _header.Fields.Add(newField);
            _header.HeaderLength = DbfHeader.CalculateHeaderLength(_header.Fields.Count);
            _header.RecordLength = DbfHeader.CalculateRecordLength(_header.Fields);

            foreach (var record in _records)
            {
                string defaultValue = GetDefaultValue(newField);
                record.AddField(defaultValue, newField, _dbtManager);
            }

            Save();
        }

        public void RemoveColumn(string columnName)
        {
            int index = _header.Fields.FindIndex(f => f.Name.TrimEnd('\0').Equals(columnName, StringComparison.OrdinalIgnoreCase));
            if (index == -1)
                throw new Exception($"Поле '{columnName}' не найдено");

            _header.Fields.RemoveAt(index);
            _header.HeaderLength = DbfHeader.CalculateHeaderLength(_header.Fields.Count);
            _header.RecordLength = DbfHeader.CalculateRecordLength(_header.Fields);

            foreach (var record in _records)
                record.RemoveField(index);

            Save();
        }

        public void RenameColumn(string oldName, string newName)
        {
            var field = _header.Fields.Find(f => f.Name.TrimEnd('\0').Equals(oldName, StringComparison.OrdinalIgnoreCase));
            if (field == null)
                throw new Exception($"Поле '{oldName}' не найдено");

            field.Name = newName.PadRight(11, '\0');
            Save();
        }

        public void UpdateColumn(AlterUpdate update)
        {
            int index = _header.Fields.FindIndex(f => f.Name.TrimEnd('\0').Equals(update.GetRowName(), StringComparison.OrdinalIgnoreCase));
            if (index == -1)
                throw new Exception($"Поле '{update.GetRowName()}' не найдено");

            var newField = new DbfField
            {
                Name = update.GetRowName().PadRight(11, '\0'),
                Type = update.GetType(),
                Length = update.GetType() == 'C' ? int.Parse(update.GetWidth()) :
                         update.GetType() == 'N' ? int.Parse(update.GetWidth()) : 1,
                DecimalCount = update.GetType() == 'N' ? byte.Parse(update.GetPrecision()) : (byte)0,
                NotNull = update.IsNotNull()
            };

            foreach (var record in _records)
            {
                string oldValue = record.GetValue(index);
                string newValue = ConvertType(oldValue, _header.Fields[index], newField);
                record.SetValue(index, newValue, newField, _dbtManager);
            }

            _header.Fields[index] = newField;
            _header.RecordLength = DbfHeader.CalculateRecordLength(_header.Fields);
            Save();
        }

        private string ConvertType(string oldValue, DbfField oldField, DbfField newField)
        {
            // Упрощённое преобразование типов
            if (newField.NotNull && string.IsNullOrWhiteSpace(oldValue))
                throw new Exception($"Поле '{newField.Name}' NOT NULL не может быть пустым");

            if (newField.Type == oldField.Type)
                return oldValue;

            // Попытка преобразования
            if (newField.Type == 'N' && oldField.Type == 'C')
            {
                if (double.TryParse(oldValue.Trim(), out double num))
                    return num.ToString().PadLeft(newField.Length);
            }

            return new string(' ', newField.Length);
        }

        public int Update(UpdateCommand update)
        {
            int count = 0;
            var parser = update.GetWhereParser();

            for (int i = 0; i < _records.Count; i++)
            {
                var record = _records[i];
                if (record.IsDeleted) continue;

                if (parser == null || parser.Evaluate(record.ToDictionary(_header.Fields)))
                {
                    foreach (var set in update.GetValues())
                    {
                        int colIndex = _header.Fields.FindIndex(f => f.Name.TrimEnd('\0').Equals(set.Field, StringComparison.OrdinalIgnoreCase));
                        if (colIndex == -1)
                            throw new Exception($"Поле '{set.Field}' не найдено");

                        record.SetValue(colIndex, set.Value, _header.Fields[colIndex], _dbtManager);
                    }
                    count++;
                }
            }

            Save();
            return count;
        }

        public int Delete(DeleteCommand delete)
        {
            int count = 0;
            var parser = delete.GetWhereParser();

            for (int i = 0; i < _records.Count; i++)
            {
                var record = _records[i];
                if (record.IsDeleted) continue;

                if (parser == null || parser.Evaluate(record.ToDictionary(_header.Fields)))
                {
                    record.IsDeleted = true;
                    count++;
                }
            }

            Save();
            return count;
        }

        public int Truncate()
        {
            int count = _records.RemoveAll(r => r.IsDeleted);
            _header.RecordCount = _records.Count;
            Save();
            return count;
        }

        public int Restore(RestoreCommand restore)
        {
            int count = 0;
            var parser = restore.GetWhereParser();

            for (int i = 0; i < _records.Count; i++)
            {
                var record = _records[i];
                if (!record.IsDeleted) continue;

                if (parser == null || parser.Evaluate(record.ToDictionary(_header.Fields)))
                {
                    record.IsDeleted = false;
                    count++;
                }
            }

            Save();
            return count;
        }

        public string Select(SelectCommand select)
        {
            var parser = select.GetWhereParser();
            var result = new StringBuilder();

            var selectedRecords = _records.Where(r => !r.IsDeleted);

            
            if (parser != null)
                selectedRecords = selectedRecords.Where(r => parser.Evaluate(r.ToDictionary(_header.Fields)));

            var columns = select.IsSelectAll() ? _header.Fields.Select(f => f.Name.TrimEnd('\0')).ToList() : select.GetColumns();

            // Заголовок
            foreach (var col in columns)
                result.Append($"{col,-15}");
            result.AppendLine();
            result.AppendLine(new string('-', columns.Count * 15));

            // Данные
            foreach (var record in selectedRecords)
            {
                foreach (var col in columns)
                {
                    int colIndex = _header.Fields.FindIndex(f => f.Name.TrimEnd('\0').Equals(col, StringComparison.OrdinalIgnoreCase));
                    if (colIndex == -1)
                        throw new Exception($"Поле '{col}' не найдено");

                    string value = record.GetDisplayValue(colIndex, _header.Fields[colIndex], _dbtManager);
                    result.Append($"{value,-15}");
                }
                result.AppendLine();
            }

            return result.ToString();
        }
    }
}