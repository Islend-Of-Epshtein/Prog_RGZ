using SQL_ConsoleApp.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SQL_ConsoleApp.Files
{
    /// <summary>
    /// Описание одного поля DBF-файла: имя, тип, длина, точность, флаг NOT NULL.
    /// </summary>
    public class DbfField
    {
        public string Name;
        public char Type;
        public int Length;
        public byte DecimalCount;
        public bool NotNull;
    }

    /// <summary>
    /// Заголовок DBF-файла. Содержит метаданные таблицы и список полей.
    /// Обеспечивает чтение и запись в бинарном формате dBASE.
    /// </summary>
    public class DbfHeader
    {
        // Константы формата dBASE
        private const int FieldDescriptorSize = 32;
        private const int HeaderBaseSize = 32;
        private const int HeaderReservedSize = 20;
        private const int FieldNameSize = 11;
        private const int FieldReserved1Size = 4;
        private const int FieldReserved2Size = 5;
        private const int FieldReserved3Size = 8;
        private const byte HeaderTerminator = 0x0D;
        private const byte NotNullFlag = 0x01;
        private const int BaseYear = 1900;

        /// <summary>Версия DBF-файла (0x03 — без MEMO, 0x83 — с MEMO).</summary>
        public byte Version;

        /// <summary>Дата последнего обновления.</summary>
        public DateTime LastUpdate;

        /// <summary>Количество записей в файле.</summary>
        public int RecordCount;

        /// <summary>Длина заголовка в байтах.</summary>
        public short HeaderLength;

        /// <summary>Длина одной записи в байтах (включая флаг удаления).</summary>
        public short RecordLength;

        /// <summary>Список полей таблицы.</summary>
        public List<DbfField> Fields;

        /// <summary>Создаёт пустой заголовок с настройками по умолчанию.</summary>
        public DbfHeader()
        {
            Version = 0x03;
            LastUpdate = DateTime.Now;
            Fields = new List<DbfField>();
        }

        // ──────────────────────────────── Фабричный метод ────────────────────────────────

        /// <summary>
        /// Создаёт заголовок на основе определений столбцов из команды CREATE TABLE.
        /// </summary>
        /// <param name="rows">Определения столбцов.</param>
        /// <returns>Новый заголовок DBF.</returns>
        public static DbfHeader Create(RowDefinition[] rows)
        {
            var header = new DbfHeader();
            bool hasMemo = false;

            foreach (var row in rows)
            {
                if (hasMemo)
                    throw new Exception("Больше одного MEMO поля недопустимо!");

                if (row.Type == 'M')
                    hasMemo = true;

                header.Fields.Add(CreateField(row));
            }

            if (hasMemo) header.Version = 0x83;
            header.HeaderLength = CalculateHeaderLength(header.Fields.Count);
            header.RecordLength = CalculateRecordLength(header.Fields);
            header.RecordCount = 0;
            return header;
        }

        // ──────────────────────────────── Вычисление размеров ────────────────────────────────

        /// <summary>Вычисляет длину заголовка: базовая часть + дескрипторы полей + терминатор.</summary>
        public static short CalculateHeaderLength(int fieldCount) =>
            (short)(HeaderBaseSize + fieldCount * FieldDescriptorSize + 1);

        /// <summary>Вычисляет длину одной записи: 1 байт флага удаления + сумма длин всех полей.</summary>
        public static short CalculateRecordLength(List<DbfField> fields)
        {
            short length = 1; // флаг удаления
            foreach (var field in fields)
                length += (short)field.Length;
            return length;
        }

        // ──────────────────────────────────── Чтение ────────────────────────────────────

        /// <summary>Читает заголовок DBF из бинарного потока.</summary>
        public static DbfHeader Read(BinaryReader reader)
        {
            var header = new DbfHeader
            {
                Version = reader.ReadByte(),
                LastUpdate = ReadDate(reader),
                RecordCount = reader.ReadInt32(),
                HeaderLength = reader.ReadInt16(),
                RecordLength = reader.ReadInt16()
            };

            reader.ReadBytes(HeaderReservedSize); // резерв

            int fieldCount = (header.HeaderLength - HeaderBaseSize - 1) / FieldDescriptorSize;
            for (int i = 0; i < fieldCount; i++)
                header.Fields.Add(ReadField(reader));

            reader.ReadByte(); // терминатор (0x0D)
            return header;
        }

        // ──────────────────────────────────── Запись ────────────────────────────────────

        /// <summary>Записывает заголовок DBF в бинарный поток.</summary>
        public void Write(BinaryWriter writer)
        {
            writer.Write(Version);
            WriteDate(writer, LastUpdate);
            writer.Write(RecordCount);
            writer.Write(HeaderLength);
            writer.Write(RecordLength);
            writer.Write(new byte[HeaderReservedSize]);

            foreach (var field in Fields)
                WriteField(writer, field);

            writer.Write(HeaderTerminator);
        }

        // ─────────────────────────────── Приватные хелперы ───────────────────────────────

        /// <summary>Создаёт DbfField из RowDefinition.</summary>
        private static DbfField CreateField(RowDefinition row) => new()
        {
            Name = row.Name.PadRight(FieldNameSize, '\0'),
            Type = row.Type,
            Length = row.Width,
            DecimalCount = row.Type == 'N' ? (byte)row.Precision : (byte)0,
            NotNull = row.IsNotNull
        };

        /// <summary>Читает дату из трёх байт (год с 1900, месяц, день).</summary>
        private static DateTime ReadDate(BinaryReader reader)
        {
            int year = reader.ReadByte() + BaseYear;
            int month = reader.ReadByte();
            int day = reader.ReadByte();
            return new DateTime(year, month, day);
        }

        /// <summary>Записывает дату в три байта (год - 1900, месяц, день).</summary>
        private static void WriteDate(BinaryWriter writer, DateTime date)
        {
            writer.Write((byte)(date.Year - BaseYear));
            writer.Write((byte)date.Month);
            writer.Write((byte)date.Day);
        }

        /// <summary>Читает дескриптор одного поля из потока.</summary>
        private static DbfField ReadField(BinaryReader reader)
        {
            byte[] nameBytes = reader.ReadBytes(FieldNameSize);
            var field = new DbfField
            {
                Name = Encoding.ASCII.GetString(nameBytes).TrimEnd('\0'),
                Type = (char)reader.ReadByte()
            };

            reader.ReadBytes(FieldReserved1Size); // адрес в памяти
            field.Length = reader.ReadByte();
            field.DecimalCount = reader.ReadByte();
            reader.ReadBytes(FieldReserved2Size);
            field.NotNull = reader.ReadByte() == NotNullFlag;
            reader.ReadBytes(FieldReserved3Size);

            return field;
        }

        /// <summary>Записывает дескриптор одного поля в поток.</summary>
        private static void WriteField(BinaryWriter writer, DbfField field)
        {
            writer.Write(Encoding.ASCII.GetBytes(field.Name.PadRight(FieldNameSize, '\0')));
            writer.Write(field.Type);
            writer.Write(new byte[FieldReserved1Size]);
            writer.Write((byte)field.Length);
            writer.Write(field.DecimalCount);
            writer.Write(new byte[FieldReserved2Size]);
            writer.Write(field.NotNull ? NotNullFlag : (byte)0x00);
            writer.Write(new byte[FieldReserved3Size]);
        }
    }
}