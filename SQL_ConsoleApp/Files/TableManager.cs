using Microsoft.VisualBasic.FileIO;
using SQL_ConsoleApp.Commands;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace SQL_ConsoleApp.Files
{
    /// <summary>
    /// Управляет таблицей DBF: создание, открытие, сохранение, CRUD-операции над записями и столбцами.
    /// </summary>
    public class TableManager
    {
        private readonly string _filePath;
        private DbfHeader? _header;
        private List<DbfRecord>? _records;
        private DbtManager? _dbtManager;

        /// <summary>
        /// Инициализирует менеджер таблицы по указанному пути к .dbf файлу.
        /// </summary>
        /// <param name="filePath">Путь к файлу .dbf.</param>
        public TableManager(string filePath) => _filePath = filePath;

        // ─────────────────────────────── Статические фабричные методы ───────────────────────────────

        /// <summary>
        /// Создаёт новую таблицу с указанными столбцами и сохраняет её.
        /// </summary>
        /// <param name="tableName">Имя таблицы (без расширения).</param>
        /// <param name="rows">Определения столбцов.</param>
        /// <returns>Менеджер созданной таблицы.</returns>
        public static TableManager Create(string tableName, RowDefinition[] rows)
        {
            string filePath = $"{tableName}.dbf";
            var manager = new TableManager(filePath)
            {
                _header = DbfHeader.Create(rows),
                _records = new List<DbfRecord>(),
                _dbtManager = HasMemoFieldStatic(rows) ? DbtManager.Create(tableName) : null
            };
            manager.Save();
            return manager;
        }

        /// <summary>
        /// Открывает существующую таблицу из указанного .dbf файла.
        /// </summary>
        /// <param name="filePath">Путь к .dbf файлу.</param>
        /// <returns>Менеджер открытой таблицы.</returns>
        public static TableManager Open(string filePath)
        {
            if (!File.Exists(filePath))
                throw new Exception($"Файл {filePath} не найден");

            var manager = new TableManager(filePath);
            manager.Load();
            return manager;
        }

        /// <summary>
        /// Удаляет таблицу и связанные файлы (.dbf и .dbt) по имени.
        /// </summary>
        /// <param name="tableName">Имя таблицы (без расширения).</param>
        public static void Drop(string tableName)
        {
            DeleteFile($"{tableName}.dbf");
            DeleteFile($"{tableName}.dbt");
        }

        // ──────────────────────────────────── Публичные методы ────────────────────────────────────

        /// <summary>Возвращает структуру полей таблицы.</summary>
        public List<(string Name, char Type, int Length, int Precision, bool NotNull)> GetFields() =>
            _header.Fields.Select(f => (f.Name.TrimEnd('\0'), f.Type, f.Length, (int)f.DecimalCount, f.NotNull)).ToList();

        /// <summary>Возвращает количество записей в таблице.</summary>
        public int GetRecordCount() => _records?.Count ?? 0;

        /// <summary>Сохраняет изменения в .dbf и .dbt файлы.</summary>
        public void Save()
        {
            using var writer = new BinaryWriter(File.Create(_filePath), Encoding.ASCII);
            _header.Write(writer);
            DbfRecord.WriteAll(writer, _records, _header);
            writer.Write((byte)0x1A);
            _dbtManager?.Save();
        }

        /// <summary>Закрывает таблицу, предварительно сохранив изменения.</summary>
        public void Close()
        {
            Save();
            _records = null;
            _header = null;
            _dbtManager = null;
        }

        /// <summary>
        /// Вставляет новую запись с указанными значениями. Отсутствующие NOT NULL поля заполняются значениями по умолчанию.
        /// </summary>
        /// <param name="columns">Список имён столбцов.</param>
        /// <param name="values">Список значений (в том же порядке).</param>
        public void Insert(List<string> columns, List<string> values)
        {
            var record = new DbfRecord(_header.Fields.Count);

            for (int i = 0; i < _header.Fields.Count; i++)
            {
                var field = _header.Fields[i];
                int colIndex = columns.FindIndex(c =>
                    c.Equals(field.Name.TrimEnd('\0'), StringComparison.OrdinalIgnoreCase));

                string value = colIndex >= 0 && !IsNull(values[colIndex])
                    ? values[colIndex]
                    : field.NotNull
                        ? throw new Exception($"Поле '{field.Name}' не может быть NULL")
                        : GetDefaultValue(field);

                record.SetValue(i, value, field, _dbtManager);
            }

            _records.Add(record);
            _header.RecordCount = _records.Count;
        }

        /// <summary>Добавляет новый столбец в таблицу.</summary>
        /// <param name="add">Параметры добавляемого столбца.</param>
        public void AddColumn(AlterAdd add)
        {
            var newField = CreateFieldFromAlter(add);
            _header.Fields.Add(newField);

            if (newField.Type == 'M')
                EnableMemoSupport();

            _header.HeaderLength = DbfHeader.CalculateHeaderLength(_header.Fields.Count);
            _header.RecordLength = DbfHeader.CalculateRecordLength(_header.Fields);

            foreach (var record in _records)
                record.AddField(GetDefaultValue(newField), newField, _dbtManager);

            Save();
        }

        /// <summary>Удаляет столбец с указанным именем.</summary>
        /// <param name="columnName">Имя удаляемого столбца.</param>
        public void RemoveColumn(string columnName)
        {
            int index = FindFieldIndex(columnName);
            bool isMemo = _header.Fields[index].Type == 'M';

            _header.Fields.RemoveAt(index);
            _header.HeaderLength = DbfHeader.CalculateHeaderLength(_header.Fields.Count);
            _header.RecordLength = DbfHeader.CalculateRecordLength(_header.Fields);

            foreach (var record in _records)
                record.RemoveField(index);

            if (isMemo)
                DisableMemoSupport();

            Save();
        }

        /// <summary>Переименовывает столбец.</summary>
        /// <param name="oldName">Текущее имя столбца.</param>
        /// <param name="newName">Новое имя столбца.</param>
        public void RenameColumn(string oldName, string newName)
        {
            var field = _header.Fields.Find(f =>
                f.Name.TrimEnd('\0').Equals(oldName, StringComparison.OrdinalIgnoreCase))
                ?? throw new Exception($"Поле '{oldName}' не найдено");

            field.Name = newName.PadRight(11, '\0');
            Save();
        }

        /// <summary>Изменяет тип и/или размеры существующего столбца с преобразованием значений.</summary>
        /// <param name="update">Параметры изменения столбца.</param>
        public void UpdateColumn(AlterUpdate update)
        {
            int index = FindFieldIndex(update.GetRowName());
            var oldField = _header.Fields[index];
            var newField = CreateFieldFromAlter(update);

            if (newField.NotNull)
                ValidateNotNullConstraint(index, oldField, newField, update.GetRowName());

            foreach (var record in _records)
            {
                string oldValue;
                if (oldField.Type != 'M')
                {
                    oldValue = record.GetDisplayValue(index, oldField, _dbtManager);
                }
                else
                {
                    oldValue = record.GetValue(index);
                }
                string newValue = ConvertValue(oldValue, oldField, newField);
                record.SetValue(index, newValue, newField, _dbtManager);
            }

            _header.Fields[index] = newField;
            _header.RecordLength = DbfHeader.CalculateRecordLength(_header.Fields);
            Save();
        }

        /// <summary>Обновляет записи, соответствующие условию WHERE. Возвращает количество обновлённых.</summary>
        public int Update(UpdateCommand update) => ModifyRecords(
            update.GetWhereParser(),
            (record, _) =>
            {
                foreach (var set in update.GetValues())
                {
                    int idx = FindFieldIndex(set.Field);
                    record.SetValue(idx, set.Value, _header.Fields[idx], _dbtManager);
                }
            });

        /// <summary>Помечает записи как удалённые по условию WHERE. Возвращает количество помеченных.</summary>
        public int Delete(DeleteCommand delete) => ModifyRecords(
            delete.GetWhereParser(),
            (record, _) => record.IsDeleted = true);

        /// <summary>Физически удаляет все помеченные записи. Возвращает количество удалённых.</summary>
        public int Truncate()
        {
            int count = _records.RemoveAll(r => r.IsDeleted);
            _header.RecordCount = _records.Count;
            Save();
            return count;
        }

        /// <summary>Восстанавливает помеченные записи по условию WHERE. Возвращает количество восстановленных.</summary>
        public int Restore(RestoreCommand restore) => ModifyRecords(
            restore.GetWhereParser(),
            (record, _) => record.IsDeleted = false,
            onlyDeleted: true);

        /// <summary>
        /// Выполняет SELECT-запрос и возвращает типизированные данные.
        /// </summary>
        /// <param name="select">Параметры SELECT-запроса.</param>
        /// <returns>Список строк, где каждая строка — массив типизированных значений.</returns>
        public List<object[]> Select(SelectCommand select)
        {
            var parser = select.GetWhereParser();
            var selectedRecords = _records.Where(r => !r.IsDeleted);

            if (parser != null)
                selectedRecords = selectedRecords.Where(r =>
                    parser.Evaluate(r.ToDictionary(_header.Fields), _dbtManager));

            var columns = select.IsSelectAll()
                ? _header.Fields.Select(f => f.Name.TrimEnd('\0')).ToList()
                : select.GetColumns();

            var columnInfos = columns.Select(col => (Index: FindFieldIndex(col), Field: _header.Fields[FindFieldIndex(col)])).ToList();

            return selectedRecords.Select(record =>
            {
                object[] row = new object[columns.Count];
                for (int i = 0; i < columnInfos.Count; i++)
                {
                    var (index, field) = columnInfos[i];
                    row[i] = ParseFieldValue(record.GetValue(index), field);
                }
                return row;
            }).ToList();
        }

        // ─────────────────────────────────── Приватные методы ───────────────────────────────────

        private void Load()
        {
            using var reader = new BinaryReader(File.OpenRead(_filePath), Encoding.ASCII);
            _header = DbfHeader.Read(reader);
            _records = DbfRecord.ReadAll(reader, _header);

            if (HasMemoField())
                _dbtManager = DbtManager.Open(Path.ChangeExtension(_filePath, null));
        }

        private bool HasMemoField() => _header.Fields.Any(f => f.Type == 'M');

        private static bool HasMemoFieldStatic(RowDefinition[] rows) => rows.Any(r => r.Type == 'M');

        /// <summary>Выполняет модификацию записей с условием WHERE. Возвращает количество затронутых записей.</summary>
        private int ModifyRecords(LogicExpressionParser parser, Action<DbfRecord, int> action, bool onlyDeleted = false)
        {
            int count = 0;
            for (int i = 0; i < _records.Count; i++)
            {
                var record = _records[i];
                if (record.IsDeleted != onlyDeleted) continue;
                if (parser != null && !parser.Evaluate(record.ToDictionary(_header.Fields), _dbtManager)) continue;

                action(record, i);
                count++;
            }
            Save();
            return count;
        }

        /// <summary>Находит индекс поля по имени и выбрасывает исключение, если не найдено.</summary>
        private int FindFieldIndex(string fieldName)
        {
            int index = _header.Fields.FindIndex(f =>
                f.Name.TrimEnd('\0').Equals(fieldName, StringComparison.OrdinalIgnoreCase));
            if (index == -1)
                throw new Exception($"Поле '{fieldName}' не найдено");
            return index;
        }

        /// <summary>Создаёт объект DbfField из команды ALTER TABLE.</summary>
        private static DbfField CreateFieldFromAlter(AlterAdd add) => new()
        {
            Name = add.GetRowName().PadRight(11, '\0'),
            Type = add.GetType(),
            Length = FieldLength(add.GetType(), add.GetWidth()),
            DecimalCount = add.GetType() == 'N' ? byte.Parse(add.GetPrecision()) : (byte)0,
            NotNull = add.IsNotNull()
        };

        /// <summary>Создаёт объект DbfField из команды ALTER TABLE UPDATE.</summary>
        private static DbfField CreateFieldFromAlter(AlterUpdate update) => new()
        {
            Name = update.GetRowName().PadRight(11, '\0'),
            Type = update.GetType(),
            Length = FieldLength(update.GetType(), update.GetWidth()),
            DecimalCount = update.GetType() == 'N' ? byte.Parse(update.GetPrecision()) : (byte)0,
            NotNull = update.IsNotNull()
        };

        /// <summary>Возвращает длину поля в зависимости от типа.</summary>
        private static int FieldLength(char type, string width) => type switch
        {
            'C' or 'N' => int.Parse(width),
            'D' => 8,
            'L' => 1,
            'M' => 10,
            _ => 0
        };

        /// <summary>Значение по умолчанию для поля заданного типа.</summary>
        private static string GetDefaultValue(DbfField field) => field.Type switch
        {
            'C' => new string(' ', field.Length),
            'N' => new string(' ', field.Length),
            'D' => "        ",
            'L' => "?",
            'M' => "          ",
            _ => ""
        };

        private void EnableMemoSupport()
        {
            if (_dbtManager != null) throw new Exception("Memo поле уже существует");
            _dbtManager = DbtManager.Create(_filePath);
            _header.Version = 0x83;
        }

        private void DisableMemoSupport()
        {
            _header.Version = 0x03;
            _dbtManager = null;
            string dbtPath = Path.ChangeExtension(_filePath, ".dbt");
            if (File.Exists(dbtPath))
            {
                try { File.Delete(dbtPath); }
                catch (Exception ex) { throw new Exception($"Не удалось удалить MEMO файл: {ex.Message}"); }
            }
        }

        /// <summary>Проверяет, что значения в NOT NULL поле останутся непустыми после преобразования.</summary>
        private void ValidateNotNullConstraint(int index, DbfField oldField, DbfField newField, string fieldName)
        {
            int recordNum = 1;
            foreach (var record in _records)
            {
                if (record.IsDeleted) { recordNum++; continue; }

                string converted = TryConvertValue(record.GetValue(index), oldField, newField);
                if (string.IsNullOrWhiteSpace(converted) || converted.Trim() == new string(' ', newField.Length))
                    throw new Exception($"Поле '{fieldName}' NOT NULL не может быть пустым после преобразования. " +
                                        $"Запись #{recordNum} содержит недопустимое значение.");
                recordNum++;
            }
        }

        private string TryConvertValue(string oldValue, DbfField oldField, DbfField newField)
        {
            try { return ConvertValue(oldValue, oldField, newField); }
            catch { return GetDefaultValue(newField); }
        }

        /// <summary>Преобразует значение из одного типа поля в другой.</summary>
        private string ConvertValue(string oldValue, DbfField oldField, DbfField newField)
        {
            string trimmed = oldValue?.Trim() ?? "";

            if (oldField.Type == 'M' && newField.Type != 'M' || newField.Type == 'M' && oldField.Type != 'M')
                throw new Exception("Нельзя преобразовывать MEMO поля!");

            if (newField.Type == oldField.Type)
                return ConvertSameType(trimmed, oldField, newField);

            return ConvertDifferentType(trimmed, oldField, newField);
        }

        private string ConvertSameType(string trimmed, DbfField oldField, DbfField newField) => newField.Type switch
        {
            'C' => PadOrTruncate(trimmed, newField.Length, padRight: true),
            'M' => _dbtManager?.GetText(trimmed) ?? GetDefaultValue(oldField),
            _ => trimmed
        };

        private string ConvertDifferentType(string trimmed, DbfField oldField, DbfField newField) => newField.Type switch
        {
            'N' => ConvertToNumeric(trimmed, oldField, newField),
            'C' => ConvertToString(trimmed, oldField, newField),
            'L' => ConvertToLogical(trimmed, oldField),
            'D' => ConvertToDate(trimmed, oldField),
            _ => GetDefaultValue(newField)
        };

        private string ConvertToNumeric(string trimmed, DbfField oldField, DbfField newField)
        {
            double? num = oldField.Type switch
            {
                'C' => TryParseDouble(trimmed),
                'L' => trimmed == "T" ? 1.0 : trimmed == "F" ? 0.0 : null,
                'N' => TryParseDouble(trimmed),
                _ => null
            };
            return num.HasValue
                ? num.Value.ToString($"F{newField.DecimalCount}").PadLeft(newField.Length)
                : new string(' ', newField.Length);
        }

        private static string ConvertToString(string trimmed, DbfField oldField, DbfField newField)
        {
            string result = oldField.Type switch
            {
                'N' => trimmed,
                'L' => trimmed == "T" ? "TRUE" : "FALSE",
                _ => trimmed
            };
            return PadOrTruncate(result, newField.Length, padRight: true);
        }

        private static string ConvertToLogical(string trimmed, DbfField oldField) => oldField.Type switch
        {
            'N' => TryParseDouble(trimmed) > 0 ? "T" : "F",
            'C' => trimmed.ToUpperInvariant() is "TRUE" or "T" or "Y" or "1" ? "T" : "F",
            _ => "F"
        };

        private static string ConvertToDate(string trimmed, DbfField oldField) =>
            oldField.Type == 'C' && DateTime.TryParse(trimmed, out DateTime date)
                ? date.ToString("yyyyMMdd")
                : "        ";

        /// <summary>Обрезает или дополняет строку до указанной длины.</summary>
        private static string PadOrTruncate(string value, int length, bool padRight) =>
            value.Length > length
                ? value[..length]
                : padRight ? value.PadRight(length) : value.PadLeft(length);

        /// <summary>Парсит значение поля в типизированный объект для SELECT.</summary>
        private object ParseFieldValue(string rawValue, DbfField field) => field.Type switch
        {
            'C' => rawValue.TrimEnd(),
            'N' => ParseNumeric(rawValue, (int)field.DecimalCount),
            'L' => ParseLogical(rawValue),
            'D' => ParseDate(rawValue),
            'M' => _dbtManager?.GetText(rawValue),
            _ => rawValue.Trim()
        };

        private static double? ParseNumeric(string value, int decimalCount)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            if (double.TryParse(value.Trim().Replace('.', ','), out double result))
                return double.Parse(result.ToString($"F{decimalCount}"));
            return null;
        }

        private static bool? ParseLogical(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Trim().Equals("?")) return null;
            return value.Trim().ToUpperInvariant() is "T" or "Y" or "1" or "TRUE";
        }

        private static DateTime? ParseDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            string trimmed = value.Trim();
            if (trimmed.Length == 0)
                return null;

            return DateTime.TryParseExact(trimmed, "yyyyMMdd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date) ? date : null;
        }

        private static double? TryParseDouble(string value) =>
            double.TryParse(value, out double result) ? result : null;

        private static bool IsNull(string value) =>
            value.Trim().Equals("NULL", StringComparison.OrdinalIgnoreCase);

        private static void DeleteFile(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
