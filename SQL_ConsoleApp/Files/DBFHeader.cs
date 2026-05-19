using SQL_ConsoleApp.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SQL_ConsoleApp.Files
{
    public class DbfField
    {
        public string Name;
        public char Type;
        public int Length;
        public byte DecimalCount;
        public bool NotNull;
        public int byteLenght;
    }

    public class DbfHeader
    {
        public byte Version;
        public DateTime LastUpdate;
        public int RecordCount;
        public short HeaderLength;
        public short RecordLength;
        public List<DbfField> Fields;

        public DbfHeader()
        {
            Version = 0x03;
            LastUpdate = DateTime.Now;
            Fields = new List<DbfField>();
        }

        public static DbfHeader Create(RowDefinition[] rows)
        {
            var header = new DbfHeader();
            foreach (var row in rows)
            {
                int fieldLength = row.Type == 'C' ? row.Width :
                         (row.Type == 'N' ? row.Width : 0);
                header.Fields.Add(new DbfField
                {
                    Name = row.Name.PadRight(11, '\0'),
                    Type = row.Type,
                    Length = fieldLength,
                    DecimalCount = row.Type == 'N' ? (byte)row.Precision : (byte)0,
                    NotNull = row.IsNotNull,
                    byteLenght = row.Type switch
                    {
                        'C' => fieldLength,
                        'N' => fieldLength,
                        'D' => 8,
                        'L' => 1,
                        'M' => 1,
                        _ => 1
                    }
                });
            }
            header.HeaderLength = CalculateHeaderLength(header.Fields.Count);
            header.RecordLength = CalculateRecordLength(header.Fields);
            header.RecordCount = 0;
            return header;
        }

        public static short CalculateHeaderLength(int fieldCount)
        {
            return (short)(32 + (fieldCount * 32) + 1);
        }

        public static short CalculateRecordLength(List<DbfField> fields)
        {
            short length = 1;
            foreach (var field in fields)
            {
                length += (short)field.byteLenght;
            }
            return length;
        }

        public static DbfHeader Read(BinaryReader reader)
        {
            var header = new DbfHeader();
            header.Version = reader.ReadByte();
            int yy = reader.ReadByte();
            int mm = reader.ReadByte();
            int dd = reader.ReadByte();
            header.LastUpdate = new DateTime(1900 + yy, mm, dd);
            header.RecordCount = reader.ReadInt32();
            header.HeaderLength = reader.ReadInt16();
            header.RecordLength = reader.ReadInt16();

            // Пропускаем 20 байт резерва
            reader.ReadBytes(20);

            int fieldCount = (header.HeaderLength - 32 - 1) / 32;
            for (int i = 0; i < fieldCount; i++)
            {
                var field = new DbfField();
                // Имя поля (11 байт)
                byte[] nameBytes = reader.ReadBytes(11);
                field.Name = Encoding.ASCII.GetString(nameBytes).TrimEnd('\0');
                // Тип
                field.Type = (char)reader.ReadByte();
                // Адрес (4 байта) – пропускаем
                reader.ReadBytes(4);
                // Длина
                field.byteLenght = reader.ReadByte();
                if (field.Type == 'C' || field.Type == 'N')
                {
                    field.Length = field.byteLenght;
                }
                else
                {
                    field.Length = 0;
                }
                // Десятичные
                field.DecimalCount = reader.ReadByte();
                // Резерв (5 байт)
                reader.ReadBytes(5);
                // Флаг NOT NULL (байт 23)
                byte notNullFlag = reader.ReadByte();
                field.NotNull = (notNullFlag == 0x01);
                // Оставшийся резерв (8 байт)
                reader.ReadBytes(8);
                header.Fields.Add(field);
            }

            // Терминатор
            reader.ReadByte(); // должно быть 0x0D
            return header;
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write((byte)(LastUpdate.Year - 1900));
            writer.Write((byte)LastUpdate.Month);
            writer.Write((byte)LastUpdate.Day);
            writer.Write(RecordCount);
            writer.Write(HeaderLength);
            writer.Write(RecordLength);
            writer.Write(new byte[20]); // резерв

            foreach (var field in Fields)
            {
                // Имя поля (11 байт)
                writer.Write(Encoding.ASCII.GetBytes(field.Name.PadRight(11, '\0')));
                writer.Write(field.Type);
                writer.Write(new byte[4]); // адрес
                writer.Write((byte)field.byteLenght);
                writer.Write(field.DecimalCount);
                writer.Write(new byte[5]); // резерв
                // Флаг NOT NULL
                writer.Write(field.NotNull ? (byte)0x01 : (byte)0x00);
                writer.Write(new byte[8]); // резерв
            }
            writer.Write((byte)0x0D);
        }
    }
}