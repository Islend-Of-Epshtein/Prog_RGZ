using SQL_ConsoleApp.Model;
using System;
using System.Data;
using System.Windows;


namespace SQL_WPF_App
{
    public partial class FormQuery : Window
    {
        private DatabaseModel _model;

        public event Action QueryExecuted;

        public FormQuery(DatabaseModel model)
        {
            InitializeComponent();
            _model = model;
        }

        private void BtnExecute_Click(object sender, RoutedEventArgs e)
        {
            string sql = txtInput.Text.Trim();
            if (string.IsNullOrEmpty(sql))
            {
                MessageBox.Show("Введите SQL-запрос", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!sql.EndsWith(";"))
                sql += ";";

            try
            {
                string result = _model.ExecuteCommand(sql);

                if (sql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                {
                    var dt = ParseResultToDataTable(result);
                    dgvOutput.ItemsSource = dt.DefaultView;
                }
                else
                {
                    MessageBox.Show(result, "Результат", MessageBoxButton.OK, MessageBoxImage.Information);
                    dgvOutput.ItemsSource = null;
                }

                QueryExecuted?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка выполнения запроса:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            txtInput.Clear();
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