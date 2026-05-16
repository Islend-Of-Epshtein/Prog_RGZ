using SQL_ConsoleApp.Model;
using System;
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
                string result = _model.ExecuteCommand($"SELECT * FROM {_currentTableName};");
                var form = new FormStructure(_currentTableName, result);
                form.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void HelpItem_Click(object sender, RoutedEventArgs e)
        {
            string helpText = @"SQL Interpreter - Справка..."; // Ваш текст справки
            MessageBox.Show(helpText, "Справка", MessageBoxButton.OK, MessageBoxImage.Information);
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