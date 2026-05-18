using SQL_ConsoleApp.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SQL_WPF_App
{
    public partial class FormDataView : Window
    {
        private DatabaseModel _model;
        private string _tableName;
        private List<ColumnInfo> _columns;
        private DataTable _dataTable;

        public event Action DataChanged;

        public FormDataView(DatabaseModel model, string tableName)
        {
            InitializeComponent();
            _model = model;
            _tableName = tableName;
            this.Title = $"Интерпретатор SQL - Данные таблицы: {_tableName}";
            LoadStructure();
            LoadData();
        }

        // Получаем структуру таблицы с типами и NOT NULL
        private void LoadStructure()
        {
            try
            {
                // Предполагаем, что в DatabaseModel есть метод GetTableStructure, возвращающий список с именами, типами, длинами, NOT NULL
                var fields = _model.GetTableStructure(_tableName);
                _columns = fields.Select(f => new ColumnInfo
                {
                    Name = f.Name,
                    Type = f.Type,
                    Length = f.Length,
                    Precision = f.Precision,
                    IsNotNull = f.NotNull
                }).ToList();
            }
            catch (Exception ex)
            {
                // Если метод отсутствует, пытаемся получить структуру через SELECT
                MessageBox.Show($"Не удалось получить структуру таблицы: {ex.Message}. Будет использовано упрощённое отображение.",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                _columns = new List<ColumnInfo>();
                // Пытаемся получить имена колонок через SELECT
                try
                {
                    string result = _model.ExecuteCommand($"SELECT * FROM {_tableName} WHERE 1=0;");
                    var lines = result.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length > 0)
                    {
                        string[] headers = lines[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        _columns = headers.Select(h => new ColumnInfo { Name = h, Type = 'C', Length = 255, IsNotNull = false }).ToList();
                    }
                }
                catch { }
            }
        }

        private void LoadData()
        {
            try
            {
                string result = _model.ExecuteCommand($"SELECT * FROM {_tableName};");
                _dataTable = ParseResultToDataTable(result);
                dgvData.ItemsSource = _dataTable.DefaultView;
                dgvData.AutoGenerateColumns = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Добавление записи
        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (_columns == null || _columns.Count == 0)
            {
                MessageBox.Show("Не удалось определить структуру таблицы", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var dialog = CreateEditDialog(null);
            if (dialog.ShowDialog() == true)
            {
                var values = (Dictionary<string, object>)dialog.Tag;
                // Валидация
                string error = ValidateValues(values);
                if (error != null)
                {
                    MessageBox.Show(error, "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (values.Keys.Count != values.Values.Count) 
                {
                    MessageBox.Show(error, "Количество имен != количеству значений", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                string columns = string.Join(", ", values.Keys);
                string vals = string.Join(", ", values.Values.Select(v => FormatValue(v)));
                string command = $"INSERT INTO {_tableName} ({columns}) VALUE ({vals});";
                try
                {
                    MessageBox.Show($"{command}", "Пол Успеха", MessageBoxButton.OK, MessageBoxImage.Information);
                    _model.ExecuteCommand(command);
                    MessageBox.Show("Запись добавлена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadData();
                    DataChanged?.Invoke();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка добавления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Редактирование выбранной записи
        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (dgvData.SelectedItem == null)
            {
                MessageBox.Show("Выберите запись для редактирования", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var rowView = dgvData.SelectedItem as DataRowView;
            if (rowView == null) return;

            var oldValues = new Dictionary<string, object>();
            foreach (DataColumn col in _dataTable.Columns)
            {
                oldValues[col.ColumnName] = rowView[col.ColumnName];
            }

            var dialog = CreateEditDialog(oldValues);
            if (dialog.ShowDialog() == true)
            {
                var newValues = (Dictionary<string, object>)dialog.Tag;
                string error = ValidateValues(newValues);
                
                if (error != null)
                {
                    MessageBox.Show(error, "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string setClause = string.Join(", ", newValues.Select(kv => $"{kv.Key} = {FormatValue(kv.Value)}"));
                string whereClause = BuildWhereClause(oldValues);
                string command = $"UPDATE {_tableName} SET {setClause} WHERE {whereClause};";
                try
                {
                    
                    _model.ExecuteCommand(command);
                    MessageBox.Show("Запись обновлена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadData();
                    DataChanged?.Invoke();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка обновления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Удаление выбранной записи
        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgvData.SelectedItem == null)
            {
                MessageBox.Show("Выберите запись для удаления", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show("Удалить выбранную запись?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            var rowView = dgvData.SelectedItem as DataRowView;
            if (rowView == null) return;

            var values = new Dictionary<string, object>();
            foreach (DataColumn col in _dataTable.Columns)
            {
                values[col.ColumnName] = rowView[col.ColumnName];
            }

            string whereClause = BuildWhereClause(values);
            string command = $"DELETE FROM {_tableName} WHERE {whereClause};";
            try
            {
                _model.ExecuteCommand(command);
                MessageBox.Show("Запись удалена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
                DataChanged?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Создание диалога для добавления/редактирования
        private Window CreateEditDialog(Dictionary<string, object> oldValues)
        {
            var dialog = new Window
            {
                Title = oldValues == null ? $"Добавление записи в {_tableName}" : $"Редактирование записи в {_tableName}",
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = System.Windows.Media.Brushes.White,
                Padding = new Thickness(10)
            };

            var stack = new StackPanel { Margin = new Thickness(5) };
            var fields = new Dictionary<string, TextBox>();
            var requiredMarks = new Dictionary<string, TextBlock>();

            foreach (var col in _columns)
            {
                var labelPanel = new StackPanel { Orientation = Orientation.Horizontal };
                var label = new Label { Content = col.Name, Margin = new Thickness(0, 5, 0, 0), FontWeight = col.IsNotNull ? FontWeights.Bold : FontWeights.Normal };
                labelPanel.Children.Add(label);
                if (col.IsNotNull)
                {
                    var star = new TextBlock { Text = "*", Foreground = System.Windows.Media.Brushes.Red, Margin = new Thickness(2, 5, 0, 0) };
                    labelPanel.Children.Add(star);
                }
                stack.Children.Add(labelPanel);

                var tb = new TextBox { Width = 250, Margin = new Thickness(0, 0, 0, 5) };
                if (oldValues != null && oldValues.ContainsKey(col.Name))
                    tb.Text = oldValues[col.Name]?.ToString() ?? "";
                stack.Children.Add(tb);
                fields[col.Name] = tb;
            }

           

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
            var okBtn = new Button { Content = "OK", Width = 75, Margin = new Thickness(5) };
            var cancelBtn = new Button { Content = "Отмена", Width = 75, Margin = new Thickness(5) };
            var info = new Label { Content = "* - NotNull поле", Margin = new Thickness(5, 2, 2, 2), Foreground = System.Windows.Media.Brushes.Red };
            buttonPanel.Children.Add(okBtn);
            buttonPanel.Children.Add(cancelBtn);
            stack.Children.Add(info);
            stack.Children.Add(buttonPanel);
            dialog.Content = stack;
           
            okBtn.Click += (s, e) =>
            {
                var result = new Dictionary<string, object>();
                foreach (var kv in fields)
                {
                    result[kv.Key] = kv.Value.Text.Trim();
                }
                dialog.Tag = result;
                dialog.DialogResult = true;
                dialog.Close();
            };
            cancelBtn.Click += (s, e) =>
            {
                dialog.DialogResult = false;
                dialog.Close();
            };

            return dialog;
        }

        // Проверка введённых значений перед INSERT/UPDATE
        private string ValidateValues(Dictionary<string, object> values)
        {
            foreach (var col in _columns)
            {
                object rawValue = values.ContainsKey(col.Name) ? values[col.Name] : null;
                string strValue = rawValue?.ToString() ?? "";

                // Проверка NOT NULL
                if (col.IsNotNull && string.IsNullOrWhiteSpace(strValue))
                    return $"Поле '{col.Name}' не может быть пустым (NOT NULL).";

                if (string.IsNullOrWhiteSpace(strValue))
                    continue;

                // Проверка типа и длины
                switch (col.Type)
                {
                    case 'C':
                        if (strValue.Length > col.Length)
                            return $"Поле '{col.Name}' (C) не может быть длиннее {col.Length} символов.";
                        break;
                    case 'N':
                        if (!decimal.TryParse(strValue, out decimal num))
                            return $"Поле '{col.Name}' (N) должно быть числом.";
                        break;
                    case 'D':
                        if (!DateTime.TryParse(strValue, out _))
                            return $"Поле '{col.Name}' (D) должно быть датой в формате ДД.ММ.ГГГГ.";
                        break;
                    case 'L':
                        string upperVal = strValue.ToUpperInvariant();
                        bool isValid = upperVal == "TRUE" || upperVal == "T" ||
                                       upperVal == "FALSE" || upperVal == "F" ||
                                       upperVal == "Y" || upperVal == "N" ||
                                       upperVal == "?";
                                       
                        if (!isValid)
                            return $"Поле '{col.Name}' (L) должно быть TRUE/FALSE, T/F, Y/N или ?";
                        break;
                    case 'M':
                        if (!strValue.Trim().StartsWith('@'))
                            return $"Поле '{col.Name}' (M) должно начинаться с @";
                        break;
                }
            }
            return null;
        }

        // Форматирование значения для SQL
        private string FormatValue(object val)
        {
            if (val == null || string.IsNullOrEmpty(val.ToString()))
                return "";
            string str = val.ToString().Trim();
            if (double.TryParse(str, out _))
                return str;
            string upperStr = str.ToUpperInvariant();
            if (upperStr == "TRUE" || upperStr == "T" || upperStr == "Y" 
                || upperStr == "FALSE" || upperStr == "F" || upperStr == "N" || upperStr == "?")
                return str;
            if (DateTime.TryParse(str, out _))
                return str;
            // Строки экранируем
            str = str.Replace("\"", "");
            return $"\"{str}\"";
        }

        // Построение WHERE для идентификации записи
        private string BuildWhereClause(Dictionary<string, object> values)
        {
            var conditions = new List<string>();
            foreach (var kv in values)
            {
                if (kv.Value == null || string.IsNullOrEmpty(kv.Value.ToString()))
                    conditions.Add($"{kv.Key} IS NULL");
                else
                    conditions.Add($"{kv.Key} = {FormatValue(kv.Value)}");
            }
            return string.Join(" AND ", conditions);
        }

        private DataTable ParseResultToDataTable(string result)
        {
            var dt = new DataTable();
            if (string.IsNullOrWhiteSpace(result))
                return dt;

            // Получаем структуру таблицы
            var structure = _model.GetTableStructure(_tableName);

            // Разбиваем на строки
            var lines = result.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2)
                return dt;

            // Определяем количество колонок по первой строке (заголовки)
            string headerLine = lines[0];
            int columnWidth = 15;
            int columnCount = headerLine.Length / columnWidth;
            if (headerLine.Length % columnWidth != 0)
                columnCount++;

            // Извлекаем заголовки
            var headers = new List<string>();
            for (int i = 0; i < columnCount; i++)
            {
                int start = i * columnWidth;
                if (start < headerLine.Length)
                {
                    string header = headerLine.Substring(start, Math.Min(columnWidth, headerLine.Length - start)).Trim();
                    if (!string.IsNullOrEmpty(header))
                        headers.Add(header);
                }
            }

            // Добавляем колонки в DataTable
            foreach (var header in headers)
                dt.Columns.Add(header);

            // Обрабатываем строки данных
            for (int i = 2; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var values = new List<string>();

                for (int col = 0; col < headers.Count; col++)
                {
                    int start = col * columnWidth;
                    if (start < line.Length)
                    {
                        string val = line.Substring(start, Math.Min(columnWidth, line.Length - start)).Trim();

                        // Форматируем значение по типу поля
                        if (col < structure.Count)
                        {
                            val = FormatValueByType(val, structure[col].Type);
                        }

                        values.Add(string.IsNullOrEmpty(val) ? "" : val);
                    }
                    else
                    {
                        values.Add("");
                    }
                }

                while (values.Count < headers.Count)
                    values.Add("");

                dt.Rows.Add(values.ToArray());
            }

            return dt;
        }

        private string FormatValueByType(string value, char type)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            switch (type)
            {
                case 'D': // Дата — добавляем разделители
                    if (value.Length == 8)
                        return $"{value.Substring(0, 4)}.{value.Substring(4, 4)}.{value.Substring(6, 6)}";
                    return value;

                case 'C': // Символьный — закавычиваем
                case 'M': // Memo — закавычиваем
                    return $"\"{value}\"";

                default: // N, L и другие — как есть
                    return value;
            }
        }

        private class ColumnInfo
        {
            public string Name { get; set; }
            public char Type { get; set; }
            public int Length { get; set; }
            public int Precision { get; set; }
            public bool IsNotNull { get; set; }
        }
    }
}