using SQL_ConsoleApp.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SQL_WPF_App
{
    /// <summary>
    /// Окно просмотра, добавления, редактирования и удаления данных таблицы.
    /// </summary>
    public partial class FormDataView : Window
    {
        private readonly DatabaseModel _model;
        private List<ColumnInfo> _columns;
        private DataTable _dataTable;

        /// <summary>Событие уведомления об изменении данных (для обновления родительского окна).</summary>
        public event Action DataChanged;

        /// <summary>
        /// Инициализирует окно, загружает структуру и данные таблицы.
        /// </summary>
        /// <param name="model">Модель базы данных с открытой таблицей.</param>
        public FormDataView(DatabaseModel model)
        {
            InitializeComponent();
            _model = model;
            Title = $"Интерпретатор SQL - Данные таблицы: {_model.GetTableName()}";
            LoadStructure();
            LoadData();
        }

        /// <summary>
        /// Загружает структуру таблицы из модели и сохраняет в _columns.
        /// </summary>
        private void LoadStructure()
        {
            try
            {
                _columns = _model.GetTableStructure()
                    .Select(f => new ColumnInfo
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
                ShowWarning($"Не удалось получить структуру таблицы: {ex.Message}. " +
                             "Будет использовано упрощённое отображение.");
            }
        }

        /// <summary>
        /// Выполняет SELECT * и отображает данные в DataGrid.
        /// </summary>
        private void LoadData()
        {
            try
            {
                _model.ExecuteCommand($"SELECT * FROM {_model.GetTableName()};");
                _dataTable = CreateDataTableFromData(_model.GetSelectResult(), _model.GetTableStructure());
                dgvData.ItemsSource = _dataTable.DefaultView;
                dgvData.AutoGenerateColumns = true;
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        /// <summary>
        /// Создаёт DataTable из типизированных данных с форматированием значений для отображения.
        /// </summary>
        /// <param name="data">Список строк данных (массивы значений).</param>
        /// <param name="structure">Структура таблицы: имя, тип, длина, точность, NOT NULL.</param>
        /// <returns>DataTable с отформатированными строковыми значениями.</returns>
        public static DataTable CreateDataTableFromData(List<object[]> data,
            List<(string Name, char Type, int Length, int Precision, bool NotNull)> structure)
        {
            var dt = new DataTable();

            if (structure == null || structure.Count == 0)
                return dt;

            foreach (var field in structure)
            {
                dt.Columns.Add(new DataColumn(field.Name, typeof(string))
                {
                    AllowDBNull = !field.NotNull
                });
            }

            if (data == null)
                return dt;

            foreach (var row in data)
            {
                var dataRow = dt.NewRow();
                for (int i = 0; i < Math.Min(row.Length, dt.Columns.Count); i++)
                {
                    dataRow[i] = row[i] is null or DBNull
                        ? DBNull.Value
                        : FormatDisplayValue(row[i], structure[i].Type);
                }
                dt.Rows.Add(dataRow);
            }

            return dt;
        }

        /// <summary>Форматирует значение для отображения в DataTable в зависимости от типа поля.</summary>
        private static string FormatDisplayValue(object value, char type) => type switch
        {
            'D' => value is DateTime date ? date.ToString("dd.MM.yyyy") : value.ToString(),
            'L' => value is bool b ? (b ? "TRUE" : "FALSE") : value.ToString(),
            'N' => value is double d ? d.ToString() : value.ToString(),
            _ => value.ToString()
        };

        /// <summary>Обработчик кнопки добавления новой записи.</summary>
        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureStructureLoaded()) return;

            var dialog = CreateEditDialog(null);
            if (dialog.ShowDialog() != true) return;

            var values = (Dictionary<string, object>)dialog.Tag;
            if (!ValidateAndShowError(values)) return;

            string columns = string.Join(", ", values.Keys);
            string vals = string.Join(", ", values.Values.Select(FormatValue));
            ExecuteAndRefresh($"INSERT INTO {_model.GetTableName()} ({columns}) VALUE ({vals});", "Запись добавлена");
        }

        /// <summary>Обработчик кнопки редактирования выбранной записи.</summary>
        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var oldValues = GetSelectedRowValues();
            if (oldValues == null) return;

            var dialog = CreateEditDialog(oldValues);
            if (dialog.ShowDialog() != true) return;

            var newValues = (Dictionary<string, object>)dialog.Tag;
            if (!ValidateAndShowError(newValues)) return;

            string setClause = string.Join(", ", newValues.Select(kv => $"{kv.Key} = {FormatValue(kv.Value)}"));
            string whereClause = BuildWhereClause(oldValues);
            ExecuteAndRefresh($"UPDATE {_model.GetTableName()} SET {setClause} WHERE {whereClause};", "Запись обновлена");
        }

        /// <summary>Обработчик кнопки удаления выбранной записи.</summary>
        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var values = GetSelectedRowValues();
            if (values == null) return;

            if (MessageBox.Show("Удалить выбранную запись?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            string whereClause = BuildWhereClause(values);
            ExecuteAndRefresh($"DELETE FROM {_model.GetTableName()} WHERE {whereClause};", "Запись удалена");
        }

        /// <summary>Обработчик кнопки обновления данных.</summary>
        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => LoadData();

        /// <summary>Обработчик кнопки закрытия окна.</summary>
        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        /// <summary>
        /// Извлекает значения выбранной строки DataGrid в словарь имя_колонки -> значение.
        /// </summary>
        /// <returns>Словарь значений или null, если строка не выбрана.</returns>
        private Dictionary<string, object> GetSelectedRowValues()
        {
            if (dgvData.SelectedItem is not DataRowView rowView)
            {
                ShowWarning("Выберите запись для редактирования");
                return null;
            }

            var values = new Dictionary<string, object>();
            foreach (DataColumn col in _dataTable.Columns)
                values[col.ColumnName] = rowView[col.ColumnName];
            return values;
        }

        /// <summary>
        /// Выполняет SQL-команду, показывает сообщение об успехе, перезагружает данные и уведомляет об изменении.
        /// </summary>
        private void ExecuteAndRefresh(string command, string successMessage)
        {
            try
            {
                _model.ExecuteCommand(command);
                ShowInfo(successMessage);
                LoadData();
                DataChanged?.Invoke();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка: {ex.Message}");
            }
        }

        /// <summary>
        /// Создаёт диалоговое окно с полями ввода для добавления или редактирования записи.
        /// </summary>
        /// <param name="oldValues">Существующие значения для редактирования или null для добавления.</param>
        /// <returns>Настроенное диалоговое окно.</returns>
        private Window CreateEditDialog(Dictionary<string, object> oldValues)
        {
            bool isEdit = oldValues != null;
            var dialog = new Window
            {
                Title = $"{(isEdit ? "Редактирование" : "Добавление")} записи в {_model.GetTableName()}",
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = Brushes.White,
                Padding = new Thickness(10)
            };

            var stack = new StackPanel { Margin = new Thickness(5) };
            var fields = new Dictionary<string, TextBox>();

            foreach (var col in _columns)
            {
                stack.Children.Add(CreateFieldLabel(col));
                var tb = CreateFieldTextBox(col, oldValues);
                stack.Children.Add(tb);
                fields[col.Name] = tb;
            }

            stack.Children.Add(CreateNotNullInfo());
            stack.Children.Add(CreateDialogButtons(dialog, fields));

            dialog.Content = stack;
            return dialog;
        }

        /// <summary>Создаёт панель с именем поля и звёздочкой для NOT NULL полей.</summary>
        private static StackPanel CreateFieldLabel(ColumnInfo col)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            panel.Children.Add(new Label
            {
                Content = col.Name,
                Margin = new Thickness(0, 5, 0, 0),
                FontWeight = col.IsNotNull ? FontWeights.Bold : FontWeights.Normal
            });

            if (col.IsNotNull)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = "*",
                    Foreground = Brushes.Red,
                    Margin = new Thickness(2, 5, 0, 0)
                });
            }
            return panel;
        }

        /// <summary>Создаёт текстовое поле с предзаполненным значением (при редактировании).</summary>
        private static TextBox CreateFieldTextBox(ColumnInfo col, Dictionary<string, object> oldValues)
        {
            var tb = new TextBox { Width = 250, Margin = new Thickness(0, 0, 0, 5) };

            if (oldValues != null && oldValues.TryGetValue(col.Name, out object value) && value != null)
            {
                tb.Text = value switch
                {
                    DateTime date => date.ToString("dd.MM.yyyy"),
                    bool b => b ? "TRUE" : "FALSE",
                    double d => d.ToString(),
                    _ => value.ToString()
                };
            }
            return tb;
        }

        /// <summary>Создаёт информационную метку о NOT NULL полях.</summary>
        private static Label CreateNotNullInfo() => new()
        {
            Content = "* - NotNull поле",
            Margin = new Thickness(5, 2, 2, 2),
            Foreground = Brushes.Red
        };

        /// <summary>Создаёт панель с кнопками OK и Отмена для диалога.</summary>
        private static StackPanel CreateDialogButtons(Window dialog, Dictionary<string, TextBox> fields)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var okBtn = new Button { Content = "OK", Width = 75, Margin = new Thickness(5) };
            var cancelBtn = new Button { Content = "Отмена", Width = 75, Margin = new Thickness(5) };

            okBtn.Click += (_, _) =>
            {
                dialog.Tag = fields.ToDictionary(kv => kv.Key, kv => (object)kv.Value.Text.Trim());
                dialog.DialogResult = true;
                dialog.Close();
            };
            cancelBtn.Click += (_, _) =>
            {
                dialog.DialogResult = false;
                dialog.Close();
            };

            panel.Children.Add(okBtn);
            panel.Children.Add(cancelBtn);
            return panel;
        }

        /// <summary>
        /// Проверяет введённые значения на соответствие типам и ограничениям.
        /// </summary>
        /// <param name="values">Словарь имя_поля -> значение.</param>
        /// <returns>Сообщение об ошибке или null, если всё корректно.</returns>
        private string ValidateValues(Dictionary<string, object> values)
        {
            foreach (var col in _columns)
            {
                string strValue = (values.GetValueOrDefault(col.Name) as string)?.Trim() ?? "";

                if (col.IsNotNull && string.IsNullOrWhiteSpace(strValue))
                    return $"Поле '{col.Name}' не может быть пустым (NOT NULL).";

                if (string.IsNullOrWhiteSpace(strValue))
                    continue;

                string error = col.Type switch
                {
                    'C' when strValue.Length > col.Length =>
                        $"Поле '{col.Name}' (C) не может быть длиннее {col.Length} символов.",
                    'N' when !double.TryParse(strValue.Replace('.', ','), out _) =>
                        $"Поле '{col.Name}' (N) должно быть числом.",
                    'D' when !DateTime.TryParse(strValue, out _) =>
                        $"Поле '{col.Name}' (D) должно быть датой в формате ДД.ММ.ГГГГ.",
                    'L' when !IsValidLogical(strValue) =>
                        $"Поле '{col.Name}' (L) должно быть TRUE/FALSE, T/F, Y/N или ?",
                    _ => null
                };

                if (error != null) return error;
            }
            return null;
        }

        /// <summary>Проверяет, является ли строка допустимым логическим значением.</summary>
        private static bool IsValidLogical(string value)
        {
            string upper = value.ToUpperInvariant();
            return upper is "TRUE" or "T" or "FALSE" or "F" or "Y" or "N" or "?";
        }

        /// <summary>Валидирует значения и показывает ошибку, если она есть.</summary>
        /// <returns>true, если ошибок нет.</returns>
        private bool ValidateAndShowError(Dictionary<string, object> values)
        {
            string error = ValidateValues(values);
            if (error == null) return true;
            ShowWarning(error);
            return false;
        }

        /// <summary>
        /// Форматирует значение для использования в SQL-команде.
        /// </summary>
        private static string FormatValue(object val)
        {
            if (val == null || string.IsNullOrEmpty(val.ToString()))
                return "NULL";

            string str = val.ToString().Trim();

            if (str.StartsWith('@')) return str;
            if (IsNumeric(str)) return str.Replace(',', '.');
            if (IsLogical(str) || str.Equals("NULL", StringComparison.OrdinalIgnoreCase)) return str;
            if (DateTime.TryParse(str, out _)) return str;

            return $"\"{str.Replace("\"", "")}\"";
        }

        /// <summary>Проверяет, является ли строка числом.</summary>
        private static bool IsNumeric(string value) =>
            double.TryParse(value.Replace('.', ','), out _);

        /// <summary>Проверяет, является ли строка логическим значением.</summary>
        private static bool IsLogical(string value)
        {
            string upper = value.ToUpperInvariant();
            return upper is "TRUE" or "T" or "Y" or "FALSE" or "F" or "N" or "?";
        }

        /// <summary>
        /// Строит WHERE-условие для идентификации записи по всем её значениям.
        /// </summary>
        /// <param name="values">Словарь имя_поля -> значение.</param>
        /// <returns>Строка WHERE-условия.</returns>
        private static string BuildWhereClause(Dictionary<string, object> values)
        {
            var conditions = values.Select(kv =>
            {
                if (kv.Value is null or DBNull || string.IsNullOrEmpty(kv.Value.ToString()))
                    return $"{kv.Key} = NULL";

                string formatted = kv.Value switch
                {
                    DateTime date => date.ToString("dd.MM.yyyy"),
                    bool b => b ? "TRUE" : "FALSE",
                    double d => d.ToString(),
                    _ => kv.Value.ToString()
                };

                return $"{kv.Key} = {FormatValue(formatted)}";
            });

            return string.Join(" AND ", conditions);
        }

        /// <summary>Проверяет, загружена ли структура таблицы, и показывает предупреждение, если нет.</summary>
        private bool EnsureStructureLoaded()
        {
            if (_columns != null && _columns.Count > 0) return true;
            ShowError("Не удалось определить структуру таблицы");
            return false;
        }

        private static void ShowInfo(string message) =>
            MessageBox.Show(message, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

        private static void ShowWarning(string message) =>
            MessageBox.Show(message, "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);

        private static void ShowError(string message) =>
            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

        /// <summary>Информация о столбце таблицы.</summary>
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