using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SQL_ConsoleApp.Files
{
    public class DbtManager
    {
        private const long MAX_DBT_SIZE = 4L * 1024 * 1024 * 1024; // 4 гб
        private const Int16 BLOCK_SIZE = (Int16)512;
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
        public void CheckSizeBeforeAdd(string text)
        {
            // Текущий размер всех данных в блоках
            long currentSize = GetCurrentSize();

            // Размер нового текста в байтах
            int newTextSize = string.IsNullOrEmpty(text) ? 0 : Encoding.ASCII.GetByteCount(text);

            // Размер нового блока с учетом заголовка (8 байт) и выравнивания до BLOCK_SIZE
            int newBlockSize = 8 + newTextSize;
            int alignedBlockSize = ((newBlockSize + BLOCK_SIZE - 1) / BLOCK_SIZE) * BLOCK_SIZE;

            // Общий размер после добавления
            long totalSize = currentSize + alignedBlockSize;

            if (totalSize > MAX_DBT_SIZE)
                throw new Exception($"Невозможно добавить MEMO поле. Превышен максимальный размер DBT файла (4 ГБ).");
        }
        public long GetCurrentSize()
        {
            if (_blocks.Count <= 1)
                return 512; // Только заголовок, данных нет

            long totalSize = BLOCK_SIZE;
            // Размер блоков с данными
            for (int i = 1; i < _blocks.Count; i++)
            {
                string text = _blocks[i] ?? "";
                int textSize = Encoding.ASCII.GetByteCount(text);
                int blockDataSize = 8 + textSize;
                int alignedSize = ((blockDataSize + BLOCK_SIZE - 1) / BLOCK_SIZE) * BLOCK_SIZE;
                totalSize += alignedSize;
            }

            return totalSize;
        }
        public bool CanAddText(string text)
        {
            try
            {
                CheckSizeBeforeAdd(text);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public static DbtManager Open(string tableName)
        {
            string filePath = $"{tableName}.dbt";
            var manager = new DbtManager(filePath);

            if (!File.Exists(filePath))
                return Create(tableName);

            using (var reader = new BinaryReader(File.OpenRead(filePath), Encoding.ASCII))
            {
                // Заголовок
                int nextFreeBlock = reader.ReadInt32();
                reader.ReadInt16();
                int blockSize = reader.ReadInt16();
                if (blockSize != BLOCK_SIZE)
                    throw new Exception($"Неверный размер блока DBT: {blockSize}");
                reader.ReadBytes(BLOCK_SIZE - 8);
                manager._blocks.Add("");
                // Блоки с данными
                int blockNumber = 1;
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    int signature = reader.ReadInt32();
                    if (signature != 1) break;

                    int length = reader.ReadInt32();
                    byte[] textData = reader.ReadBytes(length);
                    manager._blocks.Add(Encoding.ASCII.GetString(textData));

                    blockNumber++;

                    // Выравнивание до BLOCK_SIZE
                    int remaining = BLOCK_SIZE - (8 + length);
                    if (remaining > 0)
                        reader.ReadBytes(remaining);
                }

                // Если есть свободные блоки, добавляем заглушки
                while (blockNumber < nextFreeBlock)
                {
                    manager._blocks.Add(""); // Пустой блок
                    blockNumber++;
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
                // Заголовок
                // Байты 0-3: номер свободного блока
                if (GetCurrentSize() > MAX_DBT_SIZE)
                {
                    writer.Write((Int32)0);
                }
                else { writer.Write((Int32)_blocks.Count); }
                    
                // Байты 4-5: не используются
                writer.Write((Int16)0);

                // Байты 6-7: размер блока в байтах (512)
                writer.Write((Int16)BLOCK_SIZE);

                // Байты 8-511: резерв (заполняем нулями)
                byte[] reserved = new byte[BLOCK_SIZE - 8];
                writer.Write(reserved);

                // данные
                for (int i = 1; i < _blocks.Count; i++)
                {
                    string text = _blocks[i] ?? "";
                    byte[] textBytes = Encoding.ASCII.GetBytes(text);
                    int length = textBytes.Length;

                    // Сигнатура блока (4 байта) = 1
                    writer.Write(1);

                    // Размер текста в байтах (4 байта)
                    writer.Write(length);

                    // Текст
                    writer.Write(textBytes);

                    // Выравнивание до BLOCK_SIZE (512 байт)
                    int remaining = BLOCK_SIZE - (8 + length);
                    if (remaining > 0)
                        writer.Write(new byte[remaining]);
                }
            }
        }
    }
}