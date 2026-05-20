using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using IntegerUpDown = Xceed.Wpf.Toolkit.IntegerUpDown;

namespace SQL_WPF_App
{
    public partial class FormCreateTable : Window
    {
        public event Action<string, RowDefinition[]> TableCreated;
        public event Action<string, string, RowDefinition[]> TableStructureChanged;

        private bool _isEditMode;
        private string _originalTableName;

        public FormCreateTable()
        {
            InitializeComponent();
            AddColumnRow();
            _isEditMode = false;
        }

        public FormCreateTable(string tableName, List<(string Name, char Type, int Length, int Precision, bool NotNull)> fields)
        {
            InitializeComponent();
            _originalTableName = tableName;
            _isEditMode = true;

            txtTableName.Text = tableName;
            txtTableName.IsReadOnly = false;
            this.Title = $"Интерпретатор SQL - структура таблицы {tableName}";

            foreach (var field in fields)
                AddColumnRow(field.Name, field.Type, field.Length, field.Precision, field.NotNull);
        }

        private void AddColumnRow(string name = "", char type = 'C', int length = 0, int precision = 0, bool notNull = false)
        {
            var rowPanel = new Grid();
            rowPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            rowPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(45) });
            rowPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(63) });
            rowPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(63) });
            rowPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            rowPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(95) });

            rowPanel.Margin = new Thickness(0, 5, 0, 5);

            // Имя столбца
            var txtName = new TextBox
            {
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Height = 22,
                Text = name
            };
            Grid.SetColumn(txtName, 0);
            rowPanel.Children.Add(txtName);

            // Тип
            var cmbType = new ComboBox
            {
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Height = 22
            };
            cmbType.Items.Add("C");
            cmbType.Items.Add("N");
            cmbType.Items.Add("M");
            cmbType.Items.Add("D");
            cmbType.Items.Add("L");
            cmbType.SelectedItem = type.ToString();
            cmbType.SelectionChanged += TypeSelectionChanged;
            Grid.SetColumn(cmbType, 1);
            rowPanel.Children.Add(cmbType);

            // Длина
            var txtLength = new TextBox
            {
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Height = 22,
                Text = length > 0 ? length.ToString() : ""
            };
            Grid.SetColumn(txtLength, 2);
            rowPanel.Children.Add(txtLength);

            // Точность
            var txtPrecision = new TextBox
            {
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Height = 22,
                Text = precision > 0 ? precision.ToString() : ""
            };
            Grid.SetColumn(txtPrecision, 3);
            rowPanel.Children.Add(txtPrecision);

            // NOT NULL
            var chkNotNull = new CheckBox
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsChecked = notNull
            };
            Grid.SetColumn(chkNotNull, 4);
            rowPanel.Children.Add(chkNotNull);

            // Позиция (IntegerUpDown)
            var numPosition = new IntegerUpDown
            {
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Height = 22,
                Minimum = -1,
                Maximum = 1,
                Width = 18,
                Value = 0,
                ShowButtonSpinner = true,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1)
            };
            numPosition.ValueChanged += NumPosition_ValueChanged;
            Grid.SetColumn(numPosition, 5);
            rowPanel.Children.Add(numPosition);

            rowPanel.Tag = new List<Control> { txtName, cmbType, txtLength, txtPrecision, chkNotNull, numPosition };
            rowsPanel.Children.Add(rowPanel);

            // Обновляем состояние полей в зависимости от типа
            TypeSelectionChanged(cmbType, null);
        }

        private void AddColumnButton_Click(object sender, RoutedEventArgs e) => AddColumnRow();

        private void NumPosition_ValueChanged(object sender, EventArgs e)
        {
            var num = sender as IntegerUpDown;
            if (num == null) return;
            var rowGrid = FindParent<Grid>(num);
            if (rowGrid == null) return;
            int currentIndex = rowsPanel.Children.IndexOf(rowGrid);
            if (currentIndex < 0) return;
            int delta = num.Value ?? 0;
            if (delta == 0) return;
            int targetIndex = delta == -1 ? currentIndex + 1 : currentIndex - 1;
            if (targetIndex < 0 || targetIndex >= rowsPanel.Children.Count) return;

            num.ValueChanged -= NumPosition_ValueChanged;
            var targetRow = rowsPanel.Children[targetIndex];
            rowsPanel.Children.RemoveAt(currentIndex);
            rowsPanel.Children.Insert(targetIndex, rowGrid);
            num.Value = 0;
            num.ValueChanged += NumPosition_ValueChanged;
        }

        private void TypeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cmb = sender as ComboBox;
            if (cmb == null) return;
            var rowPanel = FindParent<Grid>(cmb);
            var controls = rowPanel?.Tag as List<Control>;
            if (controls == null) return;
            var txtLength = controls[2] as TextBox;
            var txtPrecision = controls[3] as TextBox;
            string selectedType = cmb.SelectedItem?.ToString();

            if (selectedType == "D" || selectedType == "L" || selectedType == "M")
            {
                txtLength.IsEnabled = false;
                txtLength.Text = "";
                txtPrecision.IsEnabled = false;
                txtPrecision.Text = "";
            }
            else
            {
                txtLength.IsEnabled = true;
                txtPrecision.IsEnabled = (selectedType == "N");
                if (selectedType != "N")
                    txtPrecision.Text = "";
            }
        }

        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            while (child != null && !(child is T))
                child = VisualTreeHelper.GetParent(child);
            return child as T;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string newTableName = txtTableName.Text.Trim();
            if (string.IsNullOrEmpty(newTableName))
            {
                MessageBox.Show("Введите имя таблицы", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var rows = new List<RowDefinition>();
            foreach (UIElement element in rowsPanel.Children)
            {
                var rowPanel = element as Grid;
                var controls = rowPanel?.Tag as List<Control>;
                if (controls == null) continue;

                var txtName = controls[0] as TextBox;
                var cmbType = controls[1] as ComboBox;
                var txtLength = controls[2] as TextBox;
                var txtPrecision = controls[3] as TextBox;
                var chkNotNull = controls[4] as CheckBox;

                string name = txtName.Text.Trim();
                if (string.IsNullOrEmpty(name)) continue;

                string typeStr = cmbType.SelectedItem?.ToString();
                if (string.IsNullOrEmpty(typeStr))
                {
                    MessageBox.Show($"Для столбца '{name}' не выбран тип", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                char type = typeStr[0];
                string width = txtLength.Text.Trim();
                string precision = txtPrecision.Text.Trim();
                bool notNull = chkNotNull.IsChecked == true;

                // Валидация
                if ((type == 'C' || type == 'N') && string.IsNullOrEmpty(width))
                {
                    MessageBox.Show($"Для столбца '{name}' типа {type} необходима длина", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (type == 'N' && string.IsNullOrEmpty(precision))
                    precision = "0";

                try
                {
                    rows.Add(new RowDefinition(type, name, notNull, width, precision));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка в столбце '{name}': {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            if (rows.Count == 0)
            {
                MessageBox.Show("Добавьте хотя бы один столбец", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_isEditMode)
                TableStructureChanged?.Invoke(newTableName, _originalTableName, rows.ToArray());
            else
                TableCreated?.Invoke(newTableName, rows.ToArray());

            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
    }

    public struct RowDefinition
    {
        public char Type;
        public string Name;
        public int Width;
        public int Precision;
        public bool IsNotNull;

        public RowDefinition(char type, string name, bool isNotNull, string width = "", string precision = "")
        {
            Type = type;
            Name = name;
            IsNotNull = isNotNull;
            Width = 0;
            Precision = !string.IsNullOrEmpty(precision) ? Precision = int.Parse(precision) : 0;
            Width = type switch
            {
                'C' => !string.IsNullOrEmpty(width) ? int.Parse(width) : 0,
                'N' => !string.IsNullOrEmpty(width) ? int.Parse(width) : 0,
                'L' => Width = 1,
                'D' => Width = 8,
                'M' => Width = 10,
                _ => Width = 0
            };
        }
    }
}