using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SQL_ConsoleApp.Files
{
    public class DbfRecord
    {
        private readonly List<string> _values;
        public bool IsDeleted { get; set; }

        public DbfRecord(int fieldCount)
        {
            _values = new List<string>(new string[fieldCount]);
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
            return _values[index];
        }

        public string GetDisplayValue(int index, DbfField field, DbtManager dbtManager)
        {
            string value = _values[index];
            if (field.Type == 'M' && dbtManager != null)
                return dbtManager.GetText(value);
            if (field.Type == 'D' && dbtManager != null)
            {
                return value?.Insert(1, ".").Insert(4,".") ?? "";
            }
            return value?.Trim() ?? "";
        }

        public void SetValue(int index, string value, DbfField field, DbtManager dbtManager)
        {
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
                return value.PadRight(field.Length).Substring(0, field.Length);

            if (field.Type == 'N')
            {
                if (double.TryParse(value, out double num))
                {
                    string formatted = num.ToString("F" + field.DecimalCount);
                    return formatted.PadLeft(field.Length).Substring(0, field.Length);
                }
                return new string(' ', field.Length);
            }

            if (field.Type == 'D')
            {
                if (DateTime.TryParse(value, out DateTime date))
                    return date.ToString("ddMMyyyy");
                return "        ";
            }

            if (field.Type == 'L')
            {
                if (value.Equals("TRUE", StringComparison.OrdinalIgnoreCase))
                    return "T";
                if (value.Equals("FALSE", StringComparison.OrdinalIgnoreCase))
                    return "F";
                return "?";
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
                    byte[] fieldData = reader.ReadBytes(field.Length);
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

        public Dictionary<string, object> ToDictionary(List<DbfField> fields)
        {
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < fields.Count; i++)
            {
                dict[fields[i].Name.TrimEnd('\0')] = _values[i].Trim();
            }
            return dict;
        }
    }
}