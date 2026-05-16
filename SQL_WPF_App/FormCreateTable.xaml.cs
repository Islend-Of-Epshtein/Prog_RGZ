using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using IntegerUpDown = Xceed.Wpf.Toolkit.IntegerUpDown;

namespace SQL_WPF_App
{
    public partial class FormCreateTable : Window
    {
        public event Action<string, RowDefinition[]> TableCreated;

        public FormCreateTable()
        {
            InitializeComponent();
            AddColumnButton_Click(null, null);
        }

        private void AddColumnButton_Click(object sender, RoutedEventArgs e)
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
            var txtName = new TextBox { Margin = new Thickness(0, 0, 5, 0), VerticalAlignment = VerticalAlignment.Center, Height = 22 };
            Grid.SetColumn(txtName, 0);
            rowPanel.Children.Add(txtName);

            // Тип (ComboBox)
            var cmbType = new ComboBox { Margin = new Thickness(0, 0, 5, 0), VerticalAlignment = VerticalAlignment.Center, Height = 22 };
            cmbType.Items.Add("C");
            cmbType.Items.Add("N");
            cmbType.Items.Add("M");
            cmbType.Items.Add("D");
            cmbType.Items.Add("L");
            cmbType.SelectedIndex = 0;
            Grid.SetColumn(cmbType, 1);
            rowPanel.Children.Add(cmbType);

            // Длина
            var txtLength = new TextBox { Margin = new Thickness(0, 0, 5, 0), VerticalAlignment = VerticalAlignment.Center, Height = 22 };
            Grid.SetColumn(txtLength, 2);
            rowPanel.Children.Add(txtLength);

            // Точность
            var txtPrecision = new TextBox { Margin = new Thickness(0, 0, 5, 0), VerticalAlignment = VerticalAlignment.Center, Height = 22 };
            Grid.SetColumn(txtPrecision, 3);
            rowPanel.Children.Add(txtPrecision);

            // NOT NULL (CheckBox)
            var chkNotNull = new CheckBox { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(chkNotNull, 4);
            rowPanel.Children.Add(chkNotNull);

            // Позиция (IntegerUpDown из Xceed.Wpf.Toolkit)
            var numPosition = new IntegerUpDown
            {
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Height = 22,
                Minimum = 0,
                Maximum = 1000,
                Width = 18,
                Value = 0,
                ShowButtonSpinner = true, // Показывать стрелочки
                BorderBrush = System.Windows.Media.Brushes.Gray,
                BorderThickness = new Thickness(1)
            };
            Grid.SetColumn(numPosition, 5);
            rowPanel.Children.Add(numPosition);

            // Сохраняем элементы в Tag для извлечения при сохранении
            rowPanel.Tag = new List<Control> { txtName, cmbType, txtLength, txtPrecision, chkNotNull, numPosition };

            rowsPanel.Children.Add(rowPanel);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string tableName = txtTableName.Text.Trim();
            if (string.IsNullOrEmpty(tableName))
            {
                MessageBox.Show("Введите имя таблицы", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var rows = new List<RowDefinition>();

            foreach (UIElement element in rowsPanel.Children)
            {
                if (element is Grid rowPanel)
                {
                    var controls = rowPanel.Tag as List<Control>;
                    if (controls == null) continue;

                    var txtName = controls[0] as TextBox;
                    var cmbType = controls[1] as ComboBox;
                    var txtLength = controls[2] as TextBox;
                    var txtPrecision = controls[3] as TextBox;
                    var chkNotNull = controls[4] as CheckBox;
                    var numPosition = controls[5] as IntegerUpDown;

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
                    bool notNull = chkNotNull.IsChecked == true;

                    // Валидация
                    if (type == 'C' && string.IsNullOrEmpty(width))
                    {
                        MessageBox.Show($"Для столбца '{name}' типа C необходима длина", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    if (type == 'N' && string.IsNullOrEmpty(width))
                    {
                        MessageBox.Show($"Для столбца '{name}' типа N необходима длина", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    int widthInt = 0;
                    if (!string.IsNullOrEmpty(width) && !int.TryParse(width, out widthInt))
                    {
                        MessageBox.Show($"Для столбца '{name}' длина должна быть числом", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    rows.Add(new RowDefinition(type, name, notNull, width, txtPrecision.Text));
                }
            }

            if (rows.Count == 0)
            {
                MessageBox.Show("Добавьте хотя бы один столбец", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TableCreated?.Invoke(tableName, rows.ToArray());
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}