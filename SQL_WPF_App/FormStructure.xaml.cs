
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace SQL_WPF_App
{
    public partial class FormStructure : Window
    {
        private string _tableName;
        private List<RowDefinition> _rows = new List<RowDefinition>();

        public FormStructure(string tableName, string data)
        {
            InitializeComponent();
            _tableName = tableName;
            this.Title = $"Структура таблицы: {_tableName}";
            LoadStructure(data);
        }

        private void LoadStructure(string data)
        {
            _rows.Clear();

            if (!string.IsNullOrWhiteSpace(data))
            {
                var lines = data.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length > 0)
                {
                    string[] headers = lines[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string header in headers)
                    {
                        _rows.Add(new RowDefinition('C', header, false, "10", "0"));
                    }
                }
            }

            dgvStructure.ItemsSource = _rows;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            _rows.Add(new RowDefinition('C', "new_field", false, "10", "0"));
            dgvStructure.ItemsSource = null;
            dgvStructure.ItemsSource = _rows;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (dgvStructure.SelectedItem is RowDefinition selectedRow)
            {
                _rows.Remove(selectedRow);
                dgvStructure.ItemsSource = null;
                dgvStructure.ItemsSource = _rows;
            }
            else
            {
                MessageBox.Show("Выберите строку для удаления", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    public struct RowDefinition
    {
        public char Type;
        public string Name;
        public int Width;
        public int Precision;
        public bool IsNotNull;
        public int Position; // <-- Добавьте это поле для WPF

        public RowDefinition(char type, string name, bool isNotNull, string width = "", string precision = "")
        {
            Type = type;
            Name = name;
            IsNotNull = isNotNull;
            Width = 0;
            Precision = 0;
            Position = 0; // <-- Инициализация

            if (type == 'C' && !string.IsNullOrEmpty(width))
                Width = int.Parse(width);
            else if (type == 'N')
            {
                if (!string.IsNullOrEmpty(width)) Width = int.Parse(width);
                if (!string.IsNullOrEmpty(precision)) Precision = int.Parse(precision);
            }
        }
    }
}