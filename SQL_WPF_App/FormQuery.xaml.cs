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
                    var dt = FormDataView.ParseResultToDataTable(result, _model.GetTableStructure());
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
    }
}