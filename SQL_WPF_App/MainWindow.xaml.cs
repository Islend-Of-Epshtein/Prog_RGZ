using SQL_ConsoleApp.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;

namespace SQL_WPF_App
{
    public partial class MainWindow : Window
    {
        private DatabaseModel _model;

        public MainWindow()
        {
            InitializeComponent();
            _model = new DatabaseModel();
        }

        private void CreateItem_Click(object sender, RoutedEventArgs e)
        {
            var form = new FormCreateTable();
            form.TableCreated += (tableName, rows) =>
            {
                try
                {
                    string command = $"CREATE TABLE {tableName} (";
                    for (int i = 0; i < rows.Length; i++)
                    {
                        var row = rows[i];
                        command += $"{row.Name} {row.Type}";
                        if (row.Type == 'C')
                            command += $"({row.Width})";
                        else if (row.Type == 'N')
                            command += $"({row.Width},{row.Precision})";
                        if (row.IsNotNull)
                            command += " NOT NULL";
                        if (i < rows.Length - 1)
                            command += ", ";
                    }
                    command += ");";

                    string result = _model.ExecuteCommand(command);
                    MessageBox.Show(result, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    RefreshData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            form.ShowDialog();
        }

        private void OpenItem_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new Microsoft.Win32.OpenFileDialog();
            ofd.Filter = "DBF files (*.dbf)|*.dbf|All files (*.*)|*.*";
            ofd.Title = "Открыть таблицу";

            if (ofd.ShowDialog() == true)
            {
                try
                {
                    string command = $"OPEN {ofd.FileName};";
                    string result = _model.ExecuteCommand(command);
                    MessageBox.Show(result, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    RefreshData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void StructureItem_Click(object sender, RoutedEventArgs e)
        {
            if (!_model.isTableOpened())
            {
                MessageBox.Show("Сначала откройте таблицу", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Получаем структуру таблицы из DBF (включая NOT NULL)
                var fields = _model.GetTableStructure();
                var form = new FormCreateTable(_model.GetTableName(), fields);

                form.TableStructureChanged += (newName, oldName, newRows) =>
                {
                    try
                    {
                        // Подтверждение перед изменением структуры
                        var result = MessageBox.Show(
                            $"Вы уверены, что хотите изменить структуру таблицы '{oldName}'?\n\n" +
                            "Внимание: Эта операция может привести к потере данных при изменении типов полей!",
                            "Подтверждение изменения структуры",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                        if (result != MessageBoxResult.Yes)
                            return;

                        var oldDict = fields.ToDictionary(f => f.Name, f => f);
                        var newDict = newRows.ToDictionary(r => r.Name, r => r);

                        // Находим переименованные столбцы (похожие типы, но разные имена)
                        var renamedColumns = new List<(string OldName, string NewName)>();

                        foreach (var newRow in newRows)
                        {
                            if (!oldDict.ContainsKey(newRow.Name))
                            {
                                // Ищем похожий старый столбец с таким же типом и длиной
                                var possibleMatch = fields.FirstOrDefault(f =>
                                    !newDict.ContainsKey(f.Name) &&
                                    f.Type == newRow.Type &&
                                    f.Length == newRow.Width &&
                                    f.Precision == newRow.Precision);

                                if (possibleMatch.Name != null)
                                {
                                    renamedColumns.Add((possibleMatch.Name, newRow.Name));
                                }
                            }
                        }

                        // 1. Переименовываем столбцы
                        foreach (var rename in renamedColumns)
                        {
                            string renameCommand = $"ALTER TABLE {_model.GetTableName()} COLUMN RENAME {rename.OldName} {rename.NewName};";
                            _model.ExecuteCommand(renameCommand);
                        }

                        // 2. Добавляем новые столбцы (те, что не являются переименованными)
                        foreach (var newRow in newRows)
                        {
                            if (!oldDict.ContainsKey(newRow.Name) &&
                                !renamedColumns.Any(r => r.NewName == newRow.Name))
                            {
                                string addCommand = $"ALTER TABLE {_model.GetTableName()} COLUMN ADD {newRow.Name} {newRow.Type}";
                                if (newRow.Type == 'C')
                                    addCommand += $"({newRow.Width})";
                                else if (newRow.Type == 'N')
                                    addCommand += $"({newRow.Width},{newRow.Precision})";
                                if (newRow.IsNotNull)
                                    addCommand += " NOT NULL";
                                addCommand += ";";

                                _model.ExecuteCommand(addCommand);
                            }
                        }

                        // 3. Изменяем существующие столбцы
                        foreach (var newRow in newRows)
                        {
                            string currentName = newRow.Name;

                            // Если столбец был переименован, используем старое имя для UPDATE
                            var renamed = renamedColumns.FirstOrDefault(r => r.NewName == newRow.Name);
                            if (renamed.OldName != null)
                                currentName = renamed.OldName;

                            if (oldDict.TryGetValue(currentName, out var oldRow))
                            {
                                if (oldRow.Type != newRow.Type ||
                                    oldRow.Length != newRow.Width ||
                                    oldRow.Precision != newRow.Precision ||
                                    oldRow.NotNull != newRow.IsNotNull)
                                {
                                    string updateCommand = $"ALTER TABLE {_model.GetTableName()} COLUMN UPDATE {currentName} {newRow.Type}";
                                    if (newRow.Type == 'C')
                                        updateCommand += $"({newRow.Width})";
                                    else if (newRow.Type == 'N')
                                        updateCommand += $"({newRow.Width},{newRow.Precision})";
                                    if (newRow.IsNotNull)
                                        updateCommand += " NOT NULL";
                                    updateCommand += ";";

                                    _model.ExecuteCommand(updateCommand);
                                }
                            }
                        }

                        // 4. Удаляем столбцы, которых нет в новой структуре (и которые не были переименованы)
                        foreach (var oldRow in fields)
                        {
                            if (!newDict.ContainsKey(oldRow.Name) &&
                                !renamedColumns.Any(r => r.OldName == oldRow.Name))
                            {
                                string removeCommand = $"ALTER TABLE {_model.GetTableName()} COLUMN REMOVE {oldRow.Name};";
                                _model.ExecuteCommand(removeCommand);
                            }
                        }

                        // 5. Переименовываем таблицу, если имя изменилось
                        if (!string.Equals(newName, oldName))
                        {
                            try
                            {
                                _model.RenameTable(oldName, newName);
                                dgvResult.ItemsSource = null;
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Ошибка при переименовании таблицы: {ex.Message}",
                                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }
                        }

                        MessageBox.Show($"Структура таблицы '{oldName}' изменена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        RefreshData();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };
                form.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки структуры: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShrinkItem_Click(object sender, RoutedEventArgs e)
        {
            if (!_model.isTableOpened())
            {
                MessageBox.Show("Сначала откройте таблицу", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Подтверждение перед TRUNCATE
            var result = MessageBox.Show(
                $"Вы уверены, что хотите выполнить TRUNCATE для таблицы '{_model.GetTableName()}'?\n\n" +
                "Внимание: Эта операция безвозвратно удалит все помеченные записи!",
                "Подтверждение очистки",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                string command = $"TRUNCATE {_model.GetTableName()};";
                string resultMsg = _model.ExecuteCommand(command);
                MessageBox.Show(resultMsg, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                RefreshData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseItem_Click(object sender, RoutedEventArgs e)
        {
            if (!_model.isTableOpened())
            {
                MessageBox.Show("Таблиц не открыто", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            try
            {
                _model.CloseTable();
                dgvResult.ItemsSource = null;
                MessageBox.Show("Таблица закрыта", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DropItem_Click(object sender, RoutedEventArgs e)
        {
            if (!_model.isTableOpened())
            {
                MessageBox.Show("Сначала откройте таблицу", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Удалить таблицу '{_model.GetTableName()}'?\n\n" +
                "Внимание: Эта операция безвозвратно удалит все данные!",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    string command = $"DROP TABLE {_model.GetTableName()};";
                    _model.ExecuteCommand(command);
                    dgvResult.ItemsSource = null;
                    MessageBox.Show("Таблица удалена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DataItem_Click(object sender, RoutedEventArgs e)
        {
            if (!_model.isTableOpened())
            {
                MessageBox.Show("Сначала откройте таблицу", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var form = new FormDataView(_model);
            form.DataChanged += RefreshData;
            form.ShowDialog();
        }

        private void QueryItem_Click(object sender, RoutedEventArgs e)
        {
            if (!_model.isTableOpened()) {
                MessageBox.Show("Сначала откройте таблицу", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var form = new FormQuery(_model);
            form.QueryExecuted += RefreshData;
            form.ShowDialog();
        }

        private void ExitItem_Click(object sender, RoutedEventArgs e)
        {
            _model.CloseTable();
            Application.Current.Shutdown();
        }
        private void HelpItem_Click(object sender, RoutedEventArgs e)
        {
            var formHelp = new FormHelp();
            formHelp.Owner = this;
            formHelp.ShowDialog();
        }
        private void RefreshData()
        {
            if (!_model.isTableOpened())
                return;
            try
            {
                string command = $"SELECT * FROM {_model.GetTableName()};";
                string result = _model.ExecuteCommand(command);
                var dt = FormDataView.ParseResultToDataTable(result, _model.GetTableStructure());
                dgvResult.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                dgvResult.ItemsSource = null;
            }
        }
    }
}