using SQL_ConsoleApp.Files;
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
        private List<ColumnInfo> _columns;
        private DataTable _dataTable;

        public event Action DataChanged;

        public FormDataView(DatabaseModel model)
        {
            InitializeComponent();
            _model = model;
            this.Title = $"Интерпретатор SQL - Данные таблицы: {_model.GetTableName()}";
            LoadStructure();
            LoadData();
        }

        private void LoadStructure()
        {
            try
            {
                var fields = _model.GetTableStructure();
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
                MessageBox.Show($"Не удалось получить структуру таблицы: {ex.Message}. Будет использовано упрощённое отображение.",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LoadData()
        {
            try
            {
                _model.ExecuteCommand($"SELECT * FROM {_model.GetTableName()};");
                var data = _model.GetSelectResult();
                _dataTable = CreateDataTableFromData(data, _model.GetTableStructure());
                dgvData.ItemsSource = _dataTable.DefaultView;
                dgvData.AutoGenerateColumns = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static DataTable CreateDataTableFromData(List<object[]> data, List<(string Name, char Type, int Length, int Precision, bool NotNull)> structure)
        {
            var dt = new DataTable();

            if (structure == null || structure.Count == 0)
                return dt;

            // Добавляем колонки с правильными типами на основе структуры
            for (int i = 0; i < structure.Count; i++)
            {
                var field = structure[i];
                Type dataType = field.Type switch
                {
                    'N' => typeof(string),  // Меняем на string для контроля формата
                    'L' => typeof(string),  // Меняем на string для отображения TRUE/FALSE
                    'D' => typeof(string),  // Меняем на string для формата даты
                    'M' => typeof(string),
                    'C' => typeof(string),
                    _ => typeof(string)
                };

                var dataColumn = new DataColumn(field.Name, dataType)
                {
                    AllowDBNull = !field.NotNull
                };
                dt.Columns.Add(dataColumn);
            }

            if (data == null)
                return dt;

            // Заполняем данными с форматированием
            foreach (var row in data)
            {
                var dataRow = dt.NewRow();
                for (int i = 0; i < Math.Min(row.Length, dt.Columns.Count); i++)
                {
                    if (row[i] == null || row[i] == DBNull.Value)
                    {
                        dataRow[i] = DBNull.Value;
                    }
                    else
                    {
                        dataRow[i] = structure[i].Type switch
                        {
                            'D' => row[i] is DateTime date ? date.ToString("dd.MM.yyyy") : row[i].ToString(),
                            'L' => row[i] is bool boolVal ? (boolVal ? "TRUE" : "FALSE") : row[i].ToString(),
                            'N' => row[i] is double numVal ? numVal.ToString() : row[i].ToString(),
                            _ => row[i].ToString()
                        };
                    }
                }
                dt.Rows.Add(dataRow);
            }

            return dt;
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
                string error = ValidateValues(values);
                if (error != null)
                {
                    MessageBox.Show(error, "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (values.Keys.Count != values.Values.Count)
                {
                    MessageBox.Show("Количество имен != количеству значений", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                string columns = string.Join(", ", values.Keys);
                string vals = string.Join(", ", values.Values.Select(v => FormatValue(v)));
                string command = $"INSERT INTO {_model.GetTableName()} ({columns}) VALUE ({vals});";
                try
                {
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
                string command = $"UPDATE {_model.GetTableName()} SET {setClause} WHERE {whereClause};";
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
            string command = $"DELETE FROM {_model.GetTableName()} WHERE {whereClause};";
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
                Title = oldValues == null ? $"Добавление записи в {_model.GetTableName()}"
                                          : $"Редактирование записи в {_model.GetTableName()}",
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = System.Windows.Media.Brushes.White,
                Padding = new Thickness(10)
            };

            var stack = new StackPanel { Margin = new Thickness(5) };
            var fields = new Dictionary<string, TextBox>();

            foreach (var col in _columns)
            {
                var labelPanel = new StackPanel { Orientation = Orientation.Horizontal };
                var label = new Label
                {
                    Content = col.Name,
                    Margin = new Thickness(0, 5, 0, 0),
                    FontWeight = col.IsNotNull ? FontWeights.Bold : FontWeights.Normal
                };
                labelPanel.Children.Add(label);
                if (col.IsNotNull)
                {
                    var star = new TextBlock
                    {
                        Text = "*",
                        Foreground = System.Windows.Media.Brushes.Red,
                        Margin = new Thickness(2, 5, 0, 0)
                    };
                    labelPanel.Children.Add(star);
                }
                stack.Children.Add(labelPanel);

                var tb = new TextBox { Width = 250, Margin = new Thickness(0, 0, 0, 5) };
                if (oldValues != null && oldValues.ContainsKey(col.Name))
                {
                    // Форматируем значение для отображения
                    object value = oldValues[col.Name];
                    if (value is DateTime date)
                        tb.Text = date.ToString("dd.MM.yyyy");
                    else if (value is bool boolVal)
                        tb.Text = boolVal ? "TRUE" : "FALSE";
                    else if (value is double numVal)
                        tb.Text = numVal.ToString();
                    else
                        tb.Text = value?.ToString() ?? "";
                }
                stack.Children.Add(tb);
                fields[col.Name] = tb;
            }

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0)
            };
            var okBtn = new Button { Content = "OK", Width = 75, Margin = new Thickness(5) };
            var cancelBtn = new Button { Content = "Отмена", Width = 75, Margin = new Thickness(5) };
            var info = new Label
            {
                Content = "* - NotNull поле",
                Margin = new Thickness(5, 2, 2, 2),
                Foreground = System.Windows.Media.Brushes.Red
            };
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
                        string normalizedValue = strValue.Replace('.', ',');
                        if (!double.TryParse(normalizedValue, out double num))
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
                }
            }
            return null;
        }

        // Форматирование значения для запроса в модель
        private string FormatValue(object val)
        {
            if (val == null || string.IsNullOrEmpty(val.ToString()))
                return "NULL";

            string str = val.ToString().Trim();

            // Число
            string num = str.Replace('.', ',');
            if (double.TryParse(num, out double _))
            {
                num = num.Replace(',', '.');
                return num;
            }

            string upperStr = str.ToUpperInvariant();

            // Логическое
            if (upperStr == "TRUE" || upperStr == "T" || upperStr == "Y" ||
                upperStr == "FALSE" || upperStr == "F" || upperStr == "N" ||
                upperStr == "?" || upperStr == "NULL")
                return str;

            // Дата
            if (DateTime.TryParse(str, out _))
                return str;

            // Проверяем путь к файлу
            if (str.StartsWith('@'))
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
                if (kv.Value == null || kv.Value == DBNull.Value || string.IsNullOrEmpty(kv.Value.ToString()))
                    conditions.Add($"{kv.Key} = NULL");
                else
                {
                    // Форматируем значение в зависимости от типа
                    string formattedValue;
                    if (kv.Value is DateTime date)
                        formattedValue = date.ToString("dd.MM.yyyy");
                    else if (kv.Value is bool boolVal)
                        formattedValue = boolVal ? "TRUE" : "FALSE";
                    else if (kv.Value is double numVal)
                        formattedValue = numVal.ToString();
                    else
                        formattedValue = kv.Value.ToString();

                    conditions.Add($"{kv.Key} = {FormatValue(formattedValue)}");
                }
            }
            return string.Join(" AND ", conditions);
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