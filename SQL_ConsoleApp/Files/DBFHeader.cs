using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SQL_ConsoleApp.Commands;

namespace SQL_ConsoleApp.Files
{
    public class DbfField
    {
        public string Name;
        public char Type;
        public int Length;
        public byte DecimalCount;
        public bool NotNull;
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
                header.Fields.Add(new DbfField
                {
                    Name = row.Name.PadRight(11, '\0'),
                    Type = row.Type,
                    Length = row.Type == 'C' ? row.Width :
                             row.Type == 'N' ? row.Width : 1,
                    DecimalCount = row.Type == 'N' ? (byte)row.Precision : (byte)0,
                    NotNull = row.IsNotNull
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
                length += (short)field.Length;
            return length;
        }

        public static DbfHeader Read(BinaryReader reader)
        {
            var header = new DbfHeader();
            header.Version = reader.ReadByte();
            header.LastUpdate = new DateTime(
                1900 + reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadByte()
            );
            header.RecordCount = reader.ReadInt32();
            header.HeaderLength = reader.ReadInt16();
            header.RecordLength = reader.ReadInt16();

            reader.ReadBytes(20);

            int fieldCount = (header.HeaderLength - 32 - 1) / 32;
            for (int i = 0; i < fieldCount; i++)
            {
                var field = new DbfField
                {
                    Name = Encoding.ASCII.GetString(reader.ReadBytes(11)).TrimEnd('\0'),
                    Type = (char)reader.ReadByte(),
                    Length = reader.ReadByte(),
                    DecimalCount = reader.ReadByte()
                };
                reader.ReadBytes(15);
                header.Fields.Add(field);
            }

            reader.ReadByte(); // терминатор 0x0D

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
            writer.Write(new byte[20]);

            foreach (var field in Fields)
            {
                writer.Write(Encoding.ASCII.GetBytes(field.Name.PadRight(11, '\0')));
                writer.Write(field.Type);
                writer.Write((byte)field.Length);
                writer.Write(field.DecimalCount);
                writer.Write(new byte[15]);
            }

            writer.Write((byte)0x0D);
        }
    }
}