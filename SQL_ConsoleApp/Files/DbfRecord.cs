using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace SQL_ConsoleApp.Files
{
    /// <summary>
    /// Представляет одну запись DBF-файла. Хранит строковые значения полей,
    /// флаг удаления и обеспечивает форматирование/чтение/запись.
    /// </summary>
    public class DbfRecord
    {
        private const byte DeletedFlag = 0x2A; // '*'
        private const byte ActiveFlag = 0x20;  // ' '

        private readonly List<string> _values;

        /// <summary>Флаг логического удаления записи.</summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Создаёт пустую запись с заданным количеством полей.
        /// </summary>
        /// <param name="fieldCount">Количество полей в записи.</param>
        public DbfRecord(int fieldCount)
        {
            _values = new List<string>(fieldCount);
            IsDeleted = false;
        }

        // ──────────────────────────────── Управление значениями ────────────────────────────────

        /// <summary>Добавляет новое поле с форматированным значением в конец записи.</summary>
        public void AddField(string value, DbfField field, DbtManager dbtManager) =>
            _values.Add(FormatValue(value, field, dbtManager));

        /// <summary>Удаляет поле по индексу.</summary>
        public void RemoveField(int index) => _values.RemoveAt(index);

        /// <summary>Возвращает сырое значение поля по индексу.</summary>
        public string GetValue(int index) => _values[index];

        /// <summary>
        /// Устанавливает значение поля по индексу. При необходимости расширяет список пустыми значениями.
        /// </summary>
        public void SetValue(int index, string value, DbfField field, DbtManager dbtManager)
        {
            while (_values.Count <= index)
                _values.Add("");
            _values[index] = FormatValue(value, field, dbtManager);
        }

        /// <summary>
        /// Возвращает значение поля в формате для отображения:
        /// MEMO — текст из DBT, даты — dd.MM.yyyy, логические — TRUE/FALSE/NULL.
        /// </summary>
        public string GetDisplayValue(int index, DbfField field, DbtManager dbtManager)
        {
            string value = _values[index];

            if (field.Type == 'M' && dbtManager != null)
                return dbtManager.GetText(value);

            if (field.Type == 'D')
                return TryFormatDate(value, "yyyyMMdd", "dd.MM.yyyy") ?? value?.Trim() ?? "";

            if (field.Type == 'L')
                return FormatLogicalForDisplay(value?.Trim() ?? "");

            return value?.Trim() ?? "";
        }

        // ─────────────────────────────── Сериализация / Десериализация ───────────────────────────────

        /// <summary>Читает все записи из потока в соответствии с заголовком.</summary>
        public static List<DbfRecord> ReadAll(BinaryReader reader, DbfHeader header)
        {
            var records = new List<DbfRecord>(header.RecordCount);

            for (int i = 0; i < header.RecordCount; i++)
            {
                var record = new DbfRecord(header.Fields.Count)
                {
                    IsDeleted = reader.ReadByte() == DeletedFlag
                };

                foreach (var field in header.Fields)
                    record._values.Add(Encoding.ASCII.GetString(reader.ReadBytes(field.Length)));

                records.Add(record);
            }

            return records;
        }

        /// <summary>Записывает все записи в поток.</summary>
        public static void WriteAll(BinaryWriter writer, List<DbfRecord> records, DbfHeader header)
        {
            foreach (var record in records)
            {
                writer.Write(record.IsDeleted ? DeletedFlag : ActiveFlag);

                foreach (var value in record._values)
                    writer.Write(Encoding.ASCII.GetBytes(value ?? ""));
            }
        }

        // ──────────────────────────────── Преобразование в словарь ────────────────────────────────

        /// <summary>
        /// Преобразует запись в словарь (имя_поля → (значение, тип)) для использования в WHERE-условиях.
        /// </summary>
        public Dictionary<string, (object Value, char Type)> ToDictionary(List<DbfField> fields)
        {
            var dict = new Dictionary<string, (object, char)>(fields.Count, StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < fields.Count; i++)
                dict[fields[i].Name.TrimEnd('\0')] = (_values[i].Trim(), fields[i].Type);

            return dict;
        }

        // ─────────────────────────────────── Форматирование ───────────────────────────────────

        /// <summary>Форматирует значение для хранения в DBF в соответствии с типом поля.</summary>
        private static string FormatValue(string value, DbfField field, DbtManager dbtManager)
        {
            value = value.Trim();

            return field.Type switch
            {
                'M' => FormatMemo(value, dbtManager),
                'C' => PadOrTruncate(ClearQuotes(value), field.Length, padRight: true),
                'N' => FormatNumeric(value, field),
                'D' => TryFormatDate(value, null, "yyyyMMdd") ?? new string(' ', 8),
                'L' => FormatLogical(value),
                _ => value.Equals("NULL", StringComparison.OrdinalIgnoreCase) ? "" : value
            };
        }

        /// <summary>Форматирует MEMO-значение: читает из файла, если путь, и сохраняет в DBT.</summary>
        private static string FormatMemo(string value, DbtManager dbtManager)
        {
            if (dbtManager == null)
                throw new Exception("MEMO поле не поддерживается");

            if (string.IsNullOrEmpty(value))
                return new string(' ', 10);

            if (value.StartsWith("@"))
            {
                string filePath = value[1..];
                if (!File.Exists(filePath))
                    throw new Exception($"Файл {filePath} не найден");
                value = File.ReadAllText(filePath, Encoding.UTF8);
            }

            return dbtManager.AddText(ClearQuotes(value));
        }

        /// <summary>Форматирует числовое значение с заданной точностью и длиной.</summary>
        private static string FormatNumeric(string value, DbfField field)
        {
            value = value.Replace('.', ',');

            if (!double.TryParse(value, out double num))
                return new string(' ', field.Length);

            string formatted = num.ToString($"F{field.DecimalCount}", CultureInfo.InvariantCulture);
            return PadOrTruncate(formatted, field.Length, padRight: false);
        }

        /// <summary>Форматирует логическое значение: TRUE/T/Y → "T", FALSE/F/N → "F", иначе "?".</summary>
        private static string FormatLogical(string value) => value.ToUpperInvariant() switch
        {
            "TRUE" or "T" or "Y" => "T",
            "FALSE" or "F" or "N" => "F",
            _ => "?"
        };

        /// <summary>Форматирует логическое значение для отображения: T→TRUE, F→FALSE, ?→NULL.</summary>
        private static string FormatLogicalForDisplay(string value) => value switch
        {
            "T" => "TRUE",
            "F" => "FALSE",
            "?" => "NULL",
            _ => value
        };

        /// <summary>Удаляет обрамляющие двойные кавычки, если они есть в начале и конце строки.</summary>
        public static string ClearQuotes(string value)
        {
            if (value.StartsWith('"') && value.EndsWith('"') && value.Length >= 2)
                return value[1..^1];

            return value;
        }

        /// <summary>Обрезает или дополняет строку до указанной длины.</summary>
        private static string PadOrTruncate(string value, int length, bool padRight) =>
            value.Length > length
                ? value[..length]
                : padRight ? value.PadRight(length) : value.PadLeft(length);

        /// <summary>Пытается распарсить дату из формата fromFormat и вернуть в формате toFormat.</summary>
        private static string TryFormatDate(string value, string fromFormat, string toFormat)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            string trimmed = value.Trim();

            // Если указан конкретный формат — используем строгий парсинг
            if (fromFormat != null)
            {
                
                return DateTime.TryParseExact(trimmed, fromFormat, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out DateTime date)
                    ? date.ToString(toFormat)
                    : null;
            }

            // Без указания формата — пробуем стандартный парсинг
            return DateTime.TryParse(trimmed, out DateTime parsed)
                ? parsed.ToString(toFormat)
                : null;
        }
    }
}