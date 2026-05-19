using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace SQL_ConsoleApp.Files
{
    public class DbfRecord
    {
        private readonly List<string> _values;
        public bool IsDeleted { get; set; }

        public DbfRecord(int fieldCount)
        {
            _values = new List<string>();
            IsDeleted = false;
        }

        public void AddField(string value, DbfField field, DbtManager dbtManager)
        {
            _values.Add(FormatValue(value, field, dbtManager));
        }

        public void RemoveField(int index)
        {
            _values.RemoveAt(index);
        }

        public string GetValue(int index)
        {
            if (index < 0 || index >= _values.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _values[index];
        }

        public string GetDisplayValue(int index, DbfField field, DbtManager dbtManager)
        {
            string value = _values[index];
            if (field.Type == 'M' && dbtManager != null)
                return dbtManager.GetText(value);
            if (field.Type == 'D')
            {
                if (DateTime.TryParseExact(value?.Trim(), "yyyyMMdd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                {
                    return date.ToString("dd.MM.yyyy");
                }
                return value?.Trim() ?? "";
            }
            if (field.Type == 'L')
            {
                string val = value?.Trim() ?? "";
                if (val == "T") return "TRUE";
                if (val == "F") return "FALSE";
                if (val == "?") return "NULL";
                return val;
            }
            if (value.Trim().Equals("NULL", StringComparison.OrdinalIgnoreCase))
            {
                return "";
            }
            return value?.Trim() ?? "";
        }

        public void SetValue(int index, string value, DbfField field, DbtManager dbtManager)
        {
            // Расширяем список до нужного индекса
            while (_values.Count <= index)
            {
                _values.Add(""); // добавляем пустые значения
            }
            _values[index] = FormatValue(value, field, dbtManager);
        }

        private string FormatValue(string value, DbfField field, DbtManager dbtManager)
        {
            value = value.Trim();

            if (field.Type == 'M')
            {
                if (dbtManager == null)
                    throw new Exception("MEMO поле не поддерживается");

                if (string.IsNullOrEmpty(value))
                    return dbtManager.AddText("");

                if (value.StartsWith("@"))
                {
                    string filePath = value.Substring(1);
                    if (File.Exists(filePath))
                        value = File.ReadAllText(filePath, Encoding.UTF8);
                    else
                        throw new Exception($"Файл {filePath} не найден");
                }

                return dbtManager.AddText(value);
            }

            if (field.Type == 'C')
            {
                if (value.StartsWith("\""))
                {
                    int firstIndex = value.IndexOf('"'),
                        lastIndex = value.LastIndexOf('"');

                    if (firstIndex >= 0)
                    {
                        value = value.Remove(firstIndex, 1);
                        // После удаления первого символа, lastIndex смещается на -1
                        if (lastIndex >= 0)
                        {
                            lastIndex--; // корректируем индекс
                            value = value.Remove(lastIndex, 1);
                        }
                    }
                }
                return value.PadRight(field.Length).Substring(0, field.Length);
            }
            if (field.Type == 'N')
            {
                value=value.Replace('.', ',');
                if (double.TryParse(value, out double num))
                {
                    // Используем InvariantCulture для точки в качестве разделителя
                    string formatted = num.ToString($"F{field.DecimalCount}", System.Globalization.CultureInfo.InvariantCulture);
                    
                    if (formatted.Length > field.byteLenght)
                    {
                        formatted = formatted.Substring(0, field.byteLenght);
                    }
                    else
                    {
                        formatted = formatted.PadLeft(field.byteLenght);
                    }
                    return formatted;
                }
                return new string(' ', field.byteLenght);
            }

            if (field.Type == 'D')
            {
                if (DateTime.TryParse(value, out DateTime date))
                    return date.ToString("yyyyMMdd");
                return "        ";
            }

            if (field.Type == 'L')
            {
                string upperValue = value.ToUpperInvariant();

                if (upperValue == "TRUE" || upperValue == "T" || upperValue == "Y")
                    return "T";

                if (upperValue == "FALSE" || upperValue == "F" || upperValue == "N")
                    return "F";

                return "?"; // NULL значение
            }
            if (value.Equals("NULL", StringComparison.OrdinalIgnoreCase))
            {
                return "";
            }
            return value;
        }

        public static List<DbfRecord> ReadAll(BinaryReader reader, DbfHeader header)
        {
            var records = new List<DbfRecord>();

            for (int i = 0; i < header.RecordCount; i++)
            {
                var record = new DbfRecord(header.Fields.Count);
                byte deletedFlag = reader.ReadByte();
                record.IsDeleted = (deletedFlag == 0x2A);

                foreach (var field in header.Fields)
                {
                    byte[] fieldData = reader.ReadBytes(field.byteLenght);
                    string value = Encoding.ASCII.GetString(fieldData);
                    record._values.Add(value);
                }
                records.Add(record);
            }

            return records;
        }

        public static void WriteAll(BinaryWriter writer, List<DbfRecord> records, DbfHeader header)
        {
            foreach (var record in records)
            {
                writer.Write(record.IsDeleted ? (byte)0x2A : (byte)0x20);
                foreach (var value in record._values)
                {
                    string safeValue = value ?? "";    
                    byte[] data = Encoding.ASCII.GetBytes(safeValue);
                    writer.Write(data);
                }
            }
        }

        public Dictionary<string, (object, char)> ToDictionary(List<DbfField> fields)
        {
            var dict = new Dictionary<string, (object, char)>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < fields.Count; i++)
            {
                dict[fields[i].Name.TrimEnd('\0')] = (_values[i].Trim(), fields[i].Type);
            }
            return dict;
        }
    }
}