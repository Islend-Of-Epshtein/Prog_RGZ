using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SQL_ConsoleApp.Files
{
    /// <summary>
    /// Управляет DBT-файлом (MEMO-поля): создание, чтение, запись текстовых блоков.
    /// Файл состоит из заголовка (512 байт) и блоков данных с сигнатурой и текстом.
    /// </summary>
    public class DbtManager
    {
        private const long MaxDbtSize = 4L * 1024 * 1024 * 1024; // 4 ГБ
        private const short BlockSize = 512;
        private const int HeaderSize = 8; // байт в заголовке блока (сигнатура + длина)

        private readonly string _filePath;
        private readonly List<string> _blocks;

        /// <summary>
        /// Инициализирует менеджер DBT-файла.
        /// </summary>
        /// <param name="filePath">Путь к .dbt файлу.</param>
        private DbtManager(string filePath)
        {
            _filePath = filePath;
            _blocks = new List<string>();
        }

        // ─────────────────────────────── Статические фабричные методы ───────────────────────────────

        /// <summary>
        /// Создаёт новый DBT-файл с пустым заголовочным блоком.
        /// </summary>
        /// <param name="tableName">Имя таблицы (без расширения).</param>
        /// <returns>Менеджер созданного DBT-файла.</returns>
        public static DbtManager Create(string tableName)
        {
            var manager = new DbtManager($"{tableName}.dbt");
            manager._blocks.Add(""); // Блок 0 — заголовок
            manager.Save();
            return manager;
        }

        /// <summary>
        /// Открывает существующий DBT-файл или создаёт новый, если файл не найден.
        /// </summary>
        /// <param name="tableName">Имя таблицы (без расширения).</param>
        /// <returns>Менеджер открытого DBT-файла.</returns>
        public static DbtManager Open(string tableName)
        {
            string filePath = $"{tableName}.dbt";

            if (!File.Exists(filePath))
                return Create(tableName);

            return Load(filePath);
        }

        // ──────────────────────────────────── Публичные методы ────────────────────────────────────

        /// <summary>
        /// Добавляет текст в новый блок и возвращает его номер в формате 10 символов с ведущими нулями.
        /// </summary>
        /// <param name="text">Текст для сохранения в MEMO-поле.</param>
        /// <returns>Строковый номер блока (10 символов).</returns>
        public string AddText(string text)
        {
            _blocks.Add(text ?? "");
            return (_blocks.Count - 1).ToString().PadLeft(10, '0');
        }

        /// <summary>
        /// Извлекает текст из блока по его строковому номеру.
        /// </summary>
        /// <param name="blockNumber">Номер блока в строковом формате (10 символов).</param>
        /// <returns>Текст блока или пустая строка, если блок не найден.</returns>
        public string GetText(string blockNumber)
        {
            if (string.IsNullOrWhiteSpace(blockNumber))
                return "";

            return int.TryParse(blockNumber.Trim(), out int index) && index > 0 && index < _blocks.Count
                ? _blocks[index]
                : "";
        }

        /// <summary>
        /// Проверяет, не превысит ли размер файла 4 ГБ после добавления текста.
        /// </summary>
        /// <param name="text">Предполагаемый текст для добавления.</param>
        public void CheckSizeBeforeAdd(string text)
        {
            if (GetCurrentSize() + CalculateAlignedBlockSize(text) > MaxDbtSize)
                throw new Exception("Невозможно добавить MEMO поле. Превышен максимальный размер DBT файла (4 ГБ).");
        }

        /// <summary>
        /// Проверяет возможность добавления текста без превышения лимита размера.
        /// </summary>
        /// <param name="text">Предполагаемый текст для добавления.</param>
        /// <returns>true, если добавление возможно.</returns>
        public bool CanAddText(string text)
        {
            try { CheckSizeBeforeAdd(text); return true; }
            catch { return false; }
        }

        /// <summary>
        /// Вычисляет текущий размер DBT-файла в байтах.
        /// </summary>
        public long GetCurrentSize()
        {
            if (_blocks.Count <= 1)
                return BlockSize; // только заголовок

            long totalSize = BlockSize; // заголовок файла (512 байт)
            for (int i = 1; i < _blocks.Count; i++)
                totalSize += CalculateAlignedBlockSize(_blocks[i]);

            return totalSize;
        }

        /// <summary>
        /// Сохраняет все блоки в DBT-файл.
        /// </summary>
        public void Save()
        {
            using var writer = new BinaryWriter(File.Create(_filePath), Encoding.ASCII);
            WriteHeader(writer);
            WriteDataBlocks(writer);
        }

        // ─────────────────────────────────── Приватные методы ───────────────────────────────────

        /// <summary>Загружает DBT-файл с диска.</summary>
        private static DbtManager Load(string filePath)
        {
            var manager = new DbtManager(filePath);

            using var reader = new BinaryReader(File.OpenRead(filePath), Encoding.ASCII);
            int nextFreeBlock = reader.ReadInt32();
            reader.ReadInt16(); // пропускаем резервные байты 4-5
            int blockSize = reader.ReadInt16();

            if (blockSize != BlockSize)
                throw new Exception($"Неверный размер блока DBT: {blockSize}");

            reader.ReadBytes(BlockSize - HeaderSize); // пропускаем оставшийся заголовок
            manager._blocks.Add(""); // блок 0 — заголовок

            // Читаем блоки данных
            int blockNumber = 1;
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                int signature = reader.ReadInt32();
                if (signature != 1) break;

                int length = reader.ReadInt32();
                byte[] textData = reader.ReadBytes(length);
                manager._blocks.Add(Encoding.ASCII.GetString(textData));

                // Пропускаем байты выравнивания до границы блока
                int remaining = BlockSize - HeaderSize - length;
                if (remaining > 0)
                    reader.ReadBytes(remaining);

                blockNumber++;
            }

            // Заполняем пустыми блоками до nextFreeBlock
            while (blockNumber < nextFreeBlock)
            {
                manager._blocks.Add("");
                blockNumber++;
            }

            return manager;
        }

        /// <summary>Записывает заголовок DBT-файла (512 байт).</summary>
        private void WriteHeader(BinaryWriter writer)
        {
            writer.Write((int)_blocks.Count);          // байты 0-3: номер следующего свободного блока
            writer.Write((short)0);                    // байты 4-5: резерв
            writer.Write(BlockSize);                   // байты 6-7: размер блока
            writer.Write(new byte[BlockSize - HeaderSize]); // байты 8-511: резерв
        }

        /// <summary>Записывает все блоки данных, начиная с индекса 1.</summary>
        private void WriteDataBlocks(BinaryWriter writer)
        {
            for (int i = 1; i < _blocks.Count; i++)
                WriteBlock(writer, _blocks[i] ?? "");
        }

        /// <summary>Записывает один блок данных: сигнатура (1), длина, текст, выравнивание до 512 байт.</summary>
        private static void WriteBlock(BinaryWriter writer, string text)
        {
            byte[] textBytes = Encoding.ASCII.GetBytes(text);
            int length = textBytes.Length;

            writer.Write(1);                    // сигнатура блока
            writer.Write(length);               // длина текста в байтах
            writer.Write(textBytes);            // текст
            WriteAlignment(writer, length);     // выравнивание
        }

        /// <summary>Записывает нулевые байты для выравнивания блока до BlockSize.</summary>
        private static void WriteAlignment(BinaryWriter writer, int dataLength)
        {
            int remaining = BlockSize - HeaderSize - dataLength;
            if (remaining > 0)
                writer.Write(new byte[remaining]);
        }

        /// <summary>Вычисляет размер блока с учётом заголовка и выравнивания.</summary>
        private static int CalculateAlignedBlockSize(string text)
        {
            int dataSize = string.IsNullOrEmpty(text) ? 0 : Encoding.ASCII.GetByteCount(text);
            return ((HeaderSize + dataSize + BlockSize - 1) / BlockSize) * BlockSize;
        }
    }
}