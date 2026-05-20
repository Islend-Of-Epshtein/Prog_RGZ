using Microsoft.VisualBasic.FileIO;
using SQL_ConsoleApp.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SQL_ConsoleApp.Files
{
    public class TableManager
    {
        private readonly string _filePath;
        private DbfHeader? _header;
        private List<DbfRecord>? _records;
        private DbtManager? _dbtManager;

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

            _records = null;
            _header = null;
            _dbtManager = null;
        }
        private bool HasMemoField()
        {
            return _header.Fields.Any(f => f.Type == 'M');
        }
        public List<(string Name, char Type, int Length, int Precision, bool NotNull)> GetFields()
        {
            return _header.Fields.Select(f => (f.Name.TrimEnd('\0'), f.Type, f.Length, (int)f.DecimalCount, f.NotNull)).ToList();
        }
        public int GetRecordCount() => _records?.Count ?? 0;
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
                    if ( value.Trim().Equals("NULL", StringComparison.OrdinalIgnoreCase))
                    {
                        value = GetDefaultValue(_header.Fields[colIndex]);
                    }
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
                Length = add.GetType() switch
                {
                    'C' => int.Parse(add.GetWidth()),
                    'N' => int.Parse(add.GetWidth()),
                    'D' => 8,
                    'L' => 1,
                    'M' => 10,
                    _ => 0
                },
                DecimalCount = add.GetType() == 'N' ? byte.Parse(add.GetPrecision()) : (byte)0,
                NotNull = add.IsNotNull(),
            };
            _header.Fields.Add(newField);
            if (newField.Type == 'M') 
            {
                if (_dbtManager != null) throw new Exception("Memo поле уже существует");
                _dbtManager = DbtManager.Create(_filePath);
                _header.Version = 0x83; 
            }
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

            // Проверяем, является ли удаляемое поле MEMO полем
            bool isMemoField = _header.Fields[index].Type == 'M';

            _header.Fields.RemoveAt(index);
            _header.HeaderLength = DbfHeader.CalculateHeaderLength(_header.Fields.Count);
            _header.RecordLength = DbfHeader.CalculateRecordLength(_header.Fields);

            foreach (var record in _records)
                record.RemoveField(index);

            // Если удалили MEMO поле, меняем версию и удаляем DBT файл
            if (isMemoField)
            {
                _header.Version = 0x03;

                // Закрываем и удаляем DBT файл
                if (_dbtManager != null)
                {
                    _dbtManager = null;
                }

                string dbtPath = Path.ChangeExtension(_filePath, ".dbt");
                if (File.Exists(dbtPath))
                {
                    try
                    {
                        File.Delete(dbtPath);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Не удалось удалить MEMO файл: {ex.Message}");
                    }
                }
            }

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

            var oldField = _header.Fields[index];
            var newField = new DbfField
            {
                Name = update.GetRowName().PadRight(11, '\0'),
                Type = update.GetType(),
                Length = update.GetType() switch
                {
                    'C' => int.Parse(update.GetWidth()),
                    'N' => int.Parse(update.GetWidth()),
                    'D' => 8,
                    'L' => 1,
                    'M' => 10,
                    _ => 1
                },
                DecimalCount = update.GetType() == 'N' ? byte.Parse(update.GetPrecision()) : (byte)0,
                NotNull = update.IsNotNull(),
            };

            // Проверка NOT NULL перед преобразованием
            if (newField.NotNull)
            {
                foreach (var record in _records)
                {
                    if (record.IsDeleted) continue;
                    string oldValue = record.GetValue(index);
                    string convertedValue = TryConvertValue(oldValue, oldField, newField);

                    // Если после преобразования значение пустое и поле NOT NULL - ошибка
                    if (string.IsNullOrWhiteSpace(convertedValue) || convertedValue.Trim() == new string(' ', newField.Length))
                    {
                        throw new Exception($"Поле '{update.GetRowName()}' NOT NULL не может быть пустым после преобразования. Запись #{_records.IndexOf(record) + 1} содержит недопустимое значение.");
                    }
                }
            }

            // Преобразуем значения для всех записей
            foreach (var record in _records)
            {
                string oldValue = record.GetDisplayValue(index, oldField, _dbtManager);
                string newValue = ConvertValue(oldValue, oldField, newField);
                record.SetValue(index, newValue, newField, _dbtManager);
            }

            // Обновляем структуру
            _header.Fields[index] = newField;
            _header.RecordLength = DbfHeader.CalculateRecordLength(_header.Fields);
            Save();
        }
        private string TryConvertValue(string oldValue, DbfField oldField, DbfField newField)
        {
            try
            {
                return ConvertValue(oldValue, oldField, newField);
            }
            catch
            {
                // Если преобразование невозможно, возвращаем значение по умолчанию
                return GetDefaultValue(newField);
            }
        }
        private string ConvertValue(string oldValue, DbfField oldField, DbfField newField)
        {
            string trimmedValue = oldValue?.Trim() ?? "";
            if ((newField.Type == 'M' || oldField.Type == 'M' ) && oldField.Type!= newField.Type)
            {
                throw new Exception("Нельзя преобразовывать MEMO поля!");
            }
            // Если тип не меняется, возвращаем как есть
            if (newField.Type == oldField.Type)
            {
                // Но нужно обрезать/дополнить до новой длины
                if (newField.Type == 'C')
                {
                    if (trimmedValue.Length > newField.Length)
                        return trimmedValue.Substring(0, newField.Length);
                    else
                        return trimmedValue.PadRight(newField.Length);
                }
                if (newField.Type == 'M')
                {
                      return _dbtManager?.GetText(oldValue) ?? GetDefaultValue(oldField);
                }
                return oldValue;
            }

            // Преобразования типов согласно методичке
            switch (newField.Type)
            {
                case 'N': // Преобразование в число
                    if (oldField.Type == 'C')
                    {
                        // Строка -> число
                        if (double.TryParse(trimmedValue, out double num))
                        {
                            string formatted = num.ToString("F" + newField.DecimalCount);
                            return formatted.PadLeft(newField.Length);
                        }
                        return new string(' ', newField.Length);
                    }
                    else if (oldField.Type == 'L')
                    {
                        // Логическое -> число (TRUE=1, FALSE=0)
                        if (trimmedValue == "T")
                        {
                            string formatted = "1".PadLeft(newField.Length);
                            return formatted;
                        }
                        else if (trimmedValue == "F")
                        {
                            string formatted = "0".PadLeft(newField.Length);
                            return formatted;
                        }
                        return new string(' ', newField.Length);
                    }
                    else if (oldField.Type == 'N')
                    {
                        // Число -> число (изменение точности)
                        if (double.TryParse(trimmedValue, out double num))
                        {
                            string formatted = num.ToString("F" + newField.DecimalCount);
                            return formatted.PadLeft(newField.Length);
                        }
                        return new string(' ', newField.Length);
                    }
                    break;

                case 'C': // Преобразование в строку
                    if (oldField.Type == 'N')
                    {
                        // Число -> строка
                        string numStr = trimmedValue.Trim();
                        if (numStr.Length > newField.Length)
                            return numStr.Substring(0, newField.Length);
                        else
                            return numStr.PadRight(newField.Length);
                    }
                    else if (oldField.Type == 'L')
                    {
                        // Логическое -> строка
                        string boolStr = trimmedValue == "T" ? "TRUE" : "FALSE";
                        if (boolStr.Length > newField.Length)
                            return boolStr.Substring(0, newField.Length);
                        else
                            return boolStr.PadRight(newField.Length);
                    }
                    break;

                case 'L': // Преобразование в логический тип
                    if (oldField.Type == 'N')
                    {
                        // Число -> логическое (значение>0 = TRUE)
                        if (double.TryParse(trimmedValue, out double num))
                        {
                            return num > 0 ? "T" : "F";
                        }
                        return "F";
                    }
                    else if (oldField.Type == 'C')
                    {
                        // Строка -> логическое
                        string upper = trimmedValue.ToUpperInvariant();
                        if (upper == "TRUE" || upper == "T" || upper == "Y" || upper == "1")
                            return "T";
                        return "F";
                    }
                    break;

                case 'D': // Преобразование в дату
                    if (oldField.Type == 'C')
                    {
                        if (DateTime.TryParse(trimmedValue, out DateTime date))
                            return date.ToString("yyyyMMdd");
                        return "        ";
                    }
                    break;
            }

            // Если преобразование не предусмотрено или не удалось - значение по умолчанию
            return GetDefaultValue(newField);
        }
        public int Update(UpdateCommand update)
        {
            int count = 0;
            var parser = update.GetWhereParser();
            for (int i = 0; i < _records.Count; i++)
            {
                var record = _records[i];
                if (record.IsDeleted) continue;

                if (parser == null || parser.Evaluate(record.ToDictionary(_header.Fields), _dbtManager))
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
                
                if (parser == null || parser.Evaluate(record.ToDictionary(_header.Fields), _dbtManager))
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

                if (parser == null || parser.Evaluate(record.ToDictionary(_header.Fields), _dbtManager))
                {
                    record.IsDeleted = false;
                    count++;
                }
            }

            Save();
            return count;
        }
        public List<object[]> Select(SelectCommand select)
        {
            var parser = select.GetWhereParser();

            var selectedRecords = _records.Where(r => !r.IsDeleted);

            if (parser != null)
            {
                selectedRecords = selectedRecords.Where(r => parser.Evaluate(r.ToDictionary(_header.Fields), _dbtManager));
            }

            var columns = select.IsSelectAll()
                ? _header.Fields.Select(f => f.Name.TrimEnd('\0')).ToList()
                : select.GetColumns();

            // Получаем индексы и типы колонок
            var columnInfos = columns.Select(col =>
            {
                int colIndex = _header.Fields.FindIndex(f =>
                    f.Name.TrimEnd('\0').Equals(col, StringComparison.OrdinalIgnoreCase));

                if (colIndex == -1)
                    throw new Exception($"Поле '{col}' не найдено");

                return (Index: colIndex, Field: _header.Fields[colIndex]);
            }).ToList();

            var result = new List<object[]>();

            foreach (var record in selectedRecords)
            {
                object?[] row = new object[columns.Count];

                for (int i = 0; i < columnInfos.Count; i++)
                {
                    var (index, field) = columnInfos[i];
                    string rawValue = record.GetValue(index);

                    row[i] = field.Type switch
                    {
                        'C' => rawValue.TrimEnd() ?? null, // string
                        'N' => ParseNumeric(rawValue, (int)field.DecimalCount) ?? null, // double
                        'L' => ParseLogical(rawValue) ?? null, // bool
                        'D' => ParseDate(rawValue) ?? null, // DateTime?
                        'M' => _dbtManager?.GetText(rawValue) ?? null, // string from memo
                        _ => rawValue.Trim() ?? null
                    };
                }

                result.Add(row);
            }

            return result;
        }
        private double? ParseNumeric(string value, int decimalCount)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            value = value.Replace('.', ',');
            if (double.TryParse(value.Trim(), out double result))
            {
                // Форматируем с фиксированным количеством знаков после запятой
                string formatted = result.ToString($"F{decimalCount}");
                return double.Parse(formatted);
            }
            return null;
        }
        private bool? ParseLogical(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            string trimmed = value.Trim().ToUpperInvariant();
            return trimmed == "T" || trimmed == "Y" || trimmed == "1" || trimmed == "TRUE";
        }
        private DateTime? ParseDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Trim().Length == 0)
                return null;

            string trimmed = value.Trim();
            if (DateTime.TryParseExact(trimmed, "yyyyMMdd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out DateTime date))
            {
                return date;
            }

            return null;
        }
    }
}