using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using IntegerUpDown = Xceed.Wpf.Toolkit.IntegerUpDown;

namespace SQL_WPF_App
{
    /// <summary>
    /// Окно создания или редактирования структуры таблицы.
    /// Позволяет задать имя таблицы, добавить/удалить/переместить столбцы и указать их параметры.
    /// </summary>
    public partial class FormCreateTable : Window
    {
        /// <summary>Событие создания новой таблицы. Параметры: имя таблицы, определения столбцов.</summary>
        public event Action<string, RowDefinition[]> TableCreated;

        /// <summary>Событие изменения структуры существующей таблицы. Параметры: новое имя, старое имя, определения столбцов.</summary>
        public event Action<string, string, RowDefinition[]> TableStructureChanged;

        private readonly bool _isEditMode;
        private readonly string _originalTableName;

        // Константы для разметки столбцов
        private const int ColName = 0;
        private const int ColType = 1;
        private const int ColLength = 2;
        private const int ColPrecision = 3;
        private const int ColNotNull = 4;
        private const int ColPosition = 5;

        /// <summary>
        /// Конструктор для режима создания новой таблицы.
        /// </summary>
        public FormCreateTable()
        {
            InitializeComponent();
            AddColumnRow();
        }

        /// <summary>
        /// Конструктор для режима редактирования существующей таблицы.
        /// </summary>
        /// <param name="tableName">Имя редактируемой таблицы.</param>
        /// <param name="fields">Список полей таблицы с их параметрами.</param>
        public FormCreateTable(string tableName,
            List<(string Name, char Type, int Length, int Precision, bool NotNull)> fields)
        {
            InitializeComponent();
            _originalTableName = tableName;
            _isEditMode = true;

            txtTableName.Text = tableName;
            txtTableName.IsReadOnly = false;
            Title = $"Интерпретатор SQL - структура таблицы {tableName}";

            foreach (var field in fields)
                AddColumnRow(field.Name, field.Type, field.Length, field.Precision, field.NotNull);
        }

        /// <summary>
        /// Добавляет строку с элементами управления для одного столбца таблицы.
        /// </summary>
        /// <param name="name">Имя столбца (по умолчанию пусто).</param>
        /// <param name="type">Тип столбца: C, N, M, D, L (по умолчанию 'C').</param>
        /// <param name="length">Длина для типов C и N.</param>
        /// <param name="precision">Точность для типа N.</param>
        /// <param name="notNull">Флаг обязательности NOT NULL.</param>
        private void AddColumnRow(string name = "", char type = 'C', int length = 0,
            int precision = 0, bool notNull = false)
        {
            var rowPanel = CreateRowGrid();

            var txtName = CreateTextBox(name);
            var cmbType = CreateTypeComboBox(type);
            var txtLength = CreateTextBox(length > 0 ? length.ToString() : "");
            var txtPrecision = CreateTextBox(precision > 0 ? precision.ToString() : "");
            var chkNotNull = new CheckBox
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsChecked = notNull
            };
            var numPosition = CreatePositionControl();
            numPosition.ValueChanged += NumPosition_ValueChanged;

            PlaceInGrid(rowPanel, txtName, ColName);
            PlaceInGrid(rowPanel, cmbType, ColType);
            PlaceInGrid(rowPanel, txtLength, ColLength);
            PlaceInGrid(rowPanel, txtPrecision, ColPrecision);
            PlaceInGrid(rowPanel, chkNotNull, ColNotNull);
            PlaceInGrid(rowPanel, numPosition, ColPosition);

            rowPanel.Tag = new List<Control> { txtName, cmbType, txtLength, txtPrecision, chkNotNull, numPosition };
            rowsPanel.Children.Add(rowPanel);

            TypeSelectionChanged(cmbType, null);
        }

        /// <summary>Создаёт Grid с предопределёнными колонками для строки столбца.</summary>
        private static Grid CreateRowGrid()
        {
            var grid = new Grid { Margin = new Thickness(0, 5, 0, 5) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(45) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(63) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(63) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(95) });
            return grid;
        }

        /// <summary>Создаёт стандартное текстовое поле для ввода.</summary>
        private static TextBox CreateTextBox(string text) => new()
        {
            Margin = new Thickness(0, 0, 5, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Height = 22,
            Text = text
        };

        /// <summary>Создаёт выпадающий список с доступными типами столбцов.</summary>
        private ComboBox CreateTypeComboBox(char selectedType)
        {
            var cmb = new ComboBox
            {
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Height = 22
            };
            cmb.Items.Add("C");
            cmb.Items.Add("N");
            cmb.Items.Add("M");
            cmb.Items.Add("D");
            cmb.Items.Add("L");
            cmb.SelectedItem = selectedType.ToString();
            cmb.SelectionChanged += TypeSelectionChanged;
            return cmb;
        }

        /// <summary>Создаёт элемент управления для изменения позиции столбца.</summary>
        private static IntegerUpDown CreatePositionControl() => new()
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


        /// <summary>Помещает элемент в указанную колонку Grid и добавляет его в children.</summary>
        private static void PlaceInGrid(Grid grid, UIElement element, int column)
        {
            Grid.SetColumn(element, column);
            grid.Children.Add(element);
        }

        /// <summary>
        /// Обработчик нажатия кнопки добавления нового столбца.
        /// </summary>
        private void AddColumnButton_Click(object sender, RoutedEventArgs e) => AddColumnRow();

        /// <summary>
        /// Обработчик изменения значения позиции столбца. Перемещает строку вверх или вниз.
        /// </summary>
        private void NumPosition_ValueChanged(object sender, EventArgs e)
        {
            var num = sender as IntegerUpDown;
            var rowGrid = FindParent<Grid>(num);
            if (num == null || rowGrid == null) return;

            int currentIndex = rowsPanel.Children.IndexOf(rowGrid);
            if (currentIndex < 0) return;

            int delta = num.Value ?? 0;
            if (delta == 0) return;

            int targetIndex = delta == -1 ? currentIndex + 1 : currentIndex - 1;
            if (targetIndex < 0 || targetIndex >= rowsPanel.Children.Count) return;

            // Отключаем обработчик на время перемещения, чтобы избежать рекурсии
            num.ValueChanged -= NumPosition_ValueChanged;
            rowsPanel.Children.RemoveAt(currentIndex);
            rowsPanel.Children.Insert(targetIndex, rowGrid);
            num.Value = 0;
            num.ValueChanged += NumPosition_ValueChanged;
        }

        /// <summary>
        /// Обработчик изменения выбранного типа столбца.
        /// Блокирует/разблокирует поля длины и точности в зависимости от типа.
        /// </summary>
        private void TypeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cmb = sender as ComboBox;
            var rowPanel = FindParent<Grid>(cmb);
            var controls = rowPanel?.Tag as List<Control>;
            if (cmb == null || controls == null) return;

            var txtLength = controls[ColLength] as TextBox;
            var txtPrecision = controls[ColPrecision] as TextBox;
            string selectedType = cmb.SelectedItem?.ToString();

            bool needsSize = selectedType is "C" or "N";
            bool needsPrecision = selectedType == "N";

            txtLength.IsEnabled = needsSize;
            txtPrecision.IsEnabled = needsPrecision;

            if (!needsSize) txtLength.Text = "";
            if (!needsPrecision) txtPrecision.Text = "";
        }

        /// <summary>Ищет родительский элемент заданного типа в визуальном дереве.</summary>
        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            while (child != null && child is not T)
                child = VisualTreeHelper.GetParent(child);
            return child as T;
        }

        /// <summary>
        /// Обработчик нажатия кнопки сохранения. Собирает определения столбцов,
        /// валидирует их и вызывает соответствующее событие.
        /// </summary>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string tableName = txtTableName.Text.Trim();
            if (string.IsNullOrEmpty(tableName))
            {
                ShowWarning("Введите имя таблицы");
                return;
            }

            var rows = ParseColumnRows();
            if (rows == null) return; // ошибка уже показана в ParseColumnRows

            if (rows.Count == 0)
            {
                ShowWarning("Добавьте хотя бы один столбец");
                return;
            }

            if (_isEditMode)
                TableStructureChanged?.Invoke(tableName, _originalTableName, rows.ToArray());
            else
                TableCreated?.Invoke(tableName, rows.ToArray());

            Close();
        }

        /// <summary>
        /// Извлекает и валидирует определения столбцов из элементов управления.
        /// </summary>
        /// <returns>Список определений столбцов или null при ошибке валидации.</returns>
        private List<RowDefinition> ParseColumnRows()
        {
            var rows = new List<RowDefinition>();

            foreach (UIElement element in rowsPanel.Children)
            {
                var controls = (element as Grid)?.Tag as List<Control>;
                if (controls == null) continue;

                string name = (controls[ColName] as TextBox)?.Text.Trim();
                if (string.IsNullOrEmpty(name)) continue;

                string typeStr = (controls[ColType] as ComboBox)?.SelectedItem?.ToString();
                if (string.IsNullOrEmpty(typeStr))
                {
                    ShowWarning($"Для столбца '{name}' не выбран тип");
                    return null;
                }

                char type = typeStr[0];
                string width = (controls[ColLength] as TextBox)?.Text.Trim();
                string precision = (controls[ColPrecision] as TextBox)?.Text.Trim();
                bool notNull = (controls[ColNotNull] as CheckBox)?.IsChecked == true;

                if ((type == 'C' || type == 'N') && string.IsNullOrEmpty(width))
                {
                    ShowWarning($"Для столбца '{name}' типа {type} необходима длина");
                    return null;
                }

                if (type == 'N' && string.IsNullOrEmpty(precision))
                    precision = "0";

                try
                {
                    rows.Add(new RowDefinition(type, name, notNull, width, precision));
                }
                catch (Exception ex)
                {
                    ShowError($"Ошибка в столбце '{name}': {ex.Message}");
                    return null;
                }
            }

            return rows;
        }

        /// <summary>Обработчик кнопки отмены — закрывает окно.</summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();

        /// <summary>Показывает предупреждение с указанным сообщением.</summary>
        private static void ShowWarning(string message) =>
            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);

        /// <summary>Показывает ошибку с указанным сообщением.</summary>
        private static void ShowError(string message) =>
            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    /// <summary>
    /// Определение столбца таблицы: тип, имя, размеры и флаг NOT NULL.
    /// </summary>
    public struct RowDefinition
    {
        public char Type;
        public string Name;
        public int Width;
        public int Precision;
        public bool IsNotNull;

        /// <summary>
        /// Создаёт определение столбца с автоматическим расчётом ширины в зависимости от типа.
        /// </summary>
        /// <param name="type">Тип столбца (C, N, M, D, L).</param>
        /// <param name="name">Имя столбца.</param>
        /// <param name="isNotNull">Флаг NOT NULL.</param>
        /// <param name="width">Ширина (для C и N).</param>
        /// <param name="precision">Точность (для N).</param>
        public RowDefinition(char type, string name, bool isNotNull, string width = "", string precision = "")
        {
            Type = type;
            Name = name;
            IsNotNull = isNotNull;
            Precision = int.TryParse(precision, out int p) ? p : 0;
            Width = type switch
            {
                'C' or 'N' => int.TryParse(width, out int w) ? w : 0,
                'L' => 1,
                'D' => 8,
                'M' => 10,
                _ => 0
            };
        }
    }
}