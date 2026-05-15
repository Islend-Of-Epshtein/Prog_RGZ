using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SQL_ConsoleApp.Files
{
    public class DbtManager
    {
        private const int BLOCK_SIZE = 512;
        private readonly string _filePath;
        private readonly List<string> _blocks;

        private DbtManager(string filePath)
        {
            _filePath = filePath;
            _blocks = new List<string>();
        }

        public static DbtManager Create(string tableName)
        {
            var manager = new DbtManager($"{tableName}.dbt");
            manager._blocks.Add(""); // Блок 0 - заголовок
            manager.Save();
            return manager;
        }

        public static DbtManager Open(string tableName)
        {
            string filePath = $"{tableName}.dbt";
            var manager = new DbtManager(filePath);

            if (!File.Exists(filePath))
                return Create(tableName);

            using (var reader = new BinaryReader(File.OpenRead(filePath), Encoding.ASCII))
            {
                reader.ReadBytes(BLOCK_SIZE); // Пропускаем заголовок

                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    int signature = reader.ReadInt32();
                    if (signature != 1) break;

                    int length = reader.ReadInt32();
                    byte[] textData = reader.ReadBytes(length);
                    manager._blocks.Add(Encoding.ASCII.GetString(textData));

                    // Выравнивание до BLOCK_SIZE
                    int remaining = BLOCK_SIZE - (8 + length);
                    if (remaining > 0)
                        reader.ReadBytes(remaining);
                }
            }

            return manager;
        }

        public string AddText(string text)
        {
            _blocks.Add(text ?? "");
            return (_blocks.Count - 1).ToString().PadLeft(10, '0');
        }

        public string GetText(string blockNumber)
        {
            if (string.IsNullOrWhiteSpace(blockNumber))
                return "";

            if (int.TryParse(blockNumber.Trim(), out int index) && index > 0 && index < _blocks.Count)
                return _blocks[index];

            return "";
        }

        public void Save()
        {
            using (var writer = new BinaryWriter(File.Create(_filePath), Encoding.ASCII))
            {
                // Заголовок (512 байт)
                writer.Write(new byte[BLOCK_SIZE]);

                // Записываем блоки
                for (int i = 1; i < _blocks.Count; i++)
                {
                    string text = _blocks[i] ?? "";
                    byte[] textBytes = Encoding.ASCII.GetBytes(text);
                    int length = textBytes.Length;

                    writer.Write(1); // Сигнатура
                    writer.Write(length);
                    writer.Write(textBytes);

                    // Выравнивание
                    int remaining = BLOCK_SIZE - (8 + length);
                    if (remaining > 0)
                        writer.Write(new byte[remaining]);
                }
            }
        }
    }
}