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
        private string _currentTableName;

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
                    _currentTableName = tableName;
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
                    _currentTableName = System.IO.Path.GetFileNameWithoutExtension(ofd.FileName);
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
            if (string.IsNullOrEmpty(_currentTableName))
            {
                MessageBox.Show("Сначала откройте таблицу", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Получаем структуру таблицы из DBF (включая NOT NULL)
                var fields = _model.GetTableStructure(_currentTableName);
                var form = new FormCreateTable(_currentTableName, fields);
                form.TableStructureChanged += (newName, oldName, newRows) =>
                {
                    try
                    {
                        // Удаляем старую таблицу
                        _model.ExecuteCommand($"DROP TABLE {oldName};");
                        // Создаём новую с обновлённой структурой
                        string createCmd = $"CREATE TABLE {newName} (";
                        for (int i = 0; i < newRows.Length; i++)
                        {
                            var row = newRows[i];
                            createCmd += $"{row.Name} {row.Type}";
                            if (row.Type == 'C')
                                createCmd += $"({row.Width})";
                            else if (row.Type == 'N')
                                createCmd += $"({row.Width},{row.Precision})";
                            if (row.IsNotNull)
                                createCmd += " NOT NULL";
                            if (i < newRows.Length - 1)
                                createCmd += ", ";
                        }
                        createCmd += ");";
                        _model.ExecuteCommand(createCmd);
                        _currentTableName = newName;
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
            if (string.IsNullOrEmpty(_currentTableName))
            {
                MessageBox.Show("Сначала откройте таблицу", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string command = $"TRUNCATE {_currentTableName};";
                string result = _model.ExecuteCommand(command);
                MessageBox.Show(result, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                RefreshData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _model.CloseTable();
                _currentTableName = null;
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
            if (string.IsNullOrEmpty(_currentTableName))
            {
                MessageBox.Show("Сначала откройте таблицу", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Удалить таблицу '{_currentTableName}'?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    string command = $"DROP TABLE {_currentTableName};";
                    _model.ExecuteCommand(command);
                    _currentTableName = null;
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
            if (string.IsNullOrEmpty(_currentTableName))
            {
                MessageBox.Show("Сначала откройте таблицу", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var form = new FormDataView(_model, _currentTableName);
            form.DataChanged += RefreshData;
            form.ShowDialog();
        }

        private void QueryItem_Click(object sender, RoutedEventArgs e)
        {
            var form = new FormQuery(_model);
            form.QueryExecuted += RefreshData;
            form.ShowDialog();
        }

        private void ExitItem_Click(object sender, RoutedEventArgs e)
        {
            _model.CloseTable();
            Application.Current.Shutdown();
        }
        //СТАРАЯ ВЕРСИЯ 
        /*
        private void HelpItem_Click(object sender, RoutedEventArgs e)
        {
            string helpText = @"
SQL Interpreter - Справка

Команды:
  CREATE TABLE <имя> (<поле1> <тип> [NOT NULL], ...);
  OPEN <имя_файла>;
  CLOSE;
  ALTER TABLE <имя> COLUMN ADD <поле> <тип> [NOT NULL];
  ALTER TABLE <имя> COLUMN REMOVE <поле>;
  ALTER TABLE <имя> COLUMN RENAME <старое> <новое>;
  INSERT INTO <имя> (<поля>) VALUE (<значения>);
  UPDATE <имя> SET <поле>=<значение> [WHERE <условие>];
  DELETE FROM <имя> [WHERE <условие>];
  SELECT *|поля FROM <имя> [WHERE <условие>];
  TRUNCATE <имя>;
  RESTORE <имя> [WHERE <условие>];
  DROP TABLE <имя>;
  EXIT;

        private void HelpItem_Click(object sender, RoutedEventArgs e)
        {
            var formHelp = new FormHelp();
            formHelp.Owner = this;
            formHelp.ShowDialog();
        }

        private void RefreshData()
        {
            if (string.IsNullOrEmpty(_currentTableName))
                return;

            try
            {
                string command = $"SELECT * FROM {_currentTableName};";
                string result = _model.ExecuteCommand(command);
                var dt = ParseResultToDataTable(result);
                dgvResult.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                dgvResult.ItemsSource = null;
            }
        }

        private DataTable ParseResultToDataTable(string result)
        {
            var dt = new DataTable();
            if (string.IsNullOrWhiteSpace(result))
                return dt;

            var lines = result.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2)
                return dt;

            string[] headers = lines[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var header in headers)
                dt.Columns.Add(header);

            for (int i = 2; i < lines.Length; i++)
            {
                var values = lines[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (values.Length == headers.Length)
                    dt.Rows.Add(values);
            }
            return dt;
        }
    }
}