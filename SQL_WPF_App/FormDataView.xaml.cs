using SQL_ConsoleApp.Model;
using System;
using System.Data;
using System.Windows;

namespace SQL_WPF_App
{
    public partial class FormDataView : Window
    {
        private DatabaseModel _model;
        private string _tableName;

        public event Action DataChanged;

        public FormDataView(DatabaseModel model, string tableName)
        {
            InitializeComponent();
            _model = model;
            _tableName = tableName;
            this.Title = $"Данные таблицы: {_tableName}";
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                string result = _model.ExecuteCommand($"SELECT * FROM {_tableName};");
                dgvData.ItemsSource = ParseResultToDataTable(result).DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            // Логика добавления (аналогично WinForms)
            MessageBox.Show("Добавление записи");
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            // Логика редактирования
            MessageBox.Show("Редактирование записи");
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            // Логика удаления
            MessageBox.Show("Удаление записи");
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
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