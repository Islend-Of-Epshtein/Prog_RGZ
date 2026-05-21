using SQL_ConsoleApp.Model;
using System;
using System.Collections.Generic;
using System.Windows;

namespace SQL_WPF_App
{
    /// <summary>
    /// Окно выполнения произвольных SQL-запросов.
    /// Поддерживает SELECT (с отображением результата в таблице) и прочие команды.
    /// </summary>
    public partial class FormQuery : Window
    {
        private readonly DatabaseModel _model;

        /// <summary>Событие уведомления о выполнении запроса (для обновления родительского окна).</summary>
        public event Action QueryExecuted;

        /// <summary>
        /// Инициализирует окно запросов.
        /// </summary>
        /// <param name="model">Модель базы данных с открытой таблицей.</param>
        public FormQuery(DatabaseModel model)
        {
            InitializeComponent();
            _model = model;
        }

        /// <summary>
        /// Обработчик кнопки выполнения SQL-запроса.
        /// Выполняет команду и отображает результат в DataGrid или в MessageBox.
        /// </summary>
        private void BtnExecute_Click(object sender, RoutedEventArgs e)
        {
            string sql = txtInput.Text.Trim();
            if (string.IsNullOrEmpty(sql))
            {
                ShowWarning("Введите SQL-запрос");
                return;
            }

            if (!sql.EndsWith(";"))
                sql += ";";

            try
            {
                string message = _model.ExecuteCommand(sql);

                // ExecuteCommand возвращает не-null для не-SELECT команд (статус выполнения)
                if (message != null)
                {
                    ShowInfo(message);
                    // После изменения данных обновляем выборку для отображения актуального состояния
                    _model.ExecuteCommand($"SELECT * FROM {_model.GetTableName()};");
                }

                List<object[]> data = _model.GetSelectResult();
                var dt = FormDataView.CreateDataTableFromData(data, _model.GetTableStructure());
                dgvOutput.ItemsSource = dt.DefaultView;
                QueryExecuted?.Invoke();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка выполнения запроса:\n{ex.Message}");
            }
        }

        /// <summary>Очищает поле ввода SQL-запроса.</summary>
        private void BtnClear_Click(object sender, RoutedEventArgs e) => txtInput.Clear();

        /// <summary>Закрывает окно.</summary>
        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        private static void ShowInfo(string message) =>
            MessageBox.Show(message, "Результат", MessageBoxButton.OK, MessageBoxImage.Information);

        private static void ShowWarning(string message) =>
            MessageBox.Show(message, "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);

        private static void ShowError(string message) =>
            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}