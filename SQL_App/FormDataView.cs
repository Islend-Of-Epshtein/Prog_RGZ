using System;
using System.Data;
using System.Windows.Forms;
using SQL_ConsoleApp.Model;

namespace SQL_App
{
    public partial class FormDataView : Form
    {
        private DatabaseModel _model;
        private string _tableName;
        private DataGridView dgvData;
        private Button btnAdd, btnEdit, btnDelete, btnRefresh, btnClose;

        public event Action DataChanged;

        public FormDataView(DatabaseModel model, string tableName)
        {
            _model = model;
            _tableName = tableName;
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = $"Данные таблицы: {_tableName}";
            this.Size = new System.Drawing.Size(800, 500);
            this.StartPosition = FormStartPosition.CenterParent;

            // Панель с кнопками как на Рисунке 4
            var panel = new Panel() { Dock = DockStyle.Top, Height = 40 };

            btnAdd = new Button() { Text = "Добавить", Location = new System.Drawing.Point(12, 8), Width = 100 };
            btnEdit = new Button() { Text = "Изменить", Location = new System.Drawing.Point(120, 8), Width = 100 };
            btnDelete = new Button() { Text = "Удалить", Location = new System.Drawing.Point(228, 8), Width = 100 };
            btnRefresh = new Button() { Text = "Обновить", Location = new System.Drawing.Point(336, 8), Width = 100 };
            btnClose = new Button() { Text = "Закрыть", Location = new System.Drawing.Point(444, 8), Width = 100 };

            btnAdd.Click += BtnAdd_Click;
            btnEdit.Click += BtnEdit_Click;
            btnDelete.Click += BtnDelete_Click;
            btnRefresh.Click += (s, e) => LoadData();
            btnClose.Click += (s, e) => this.Close();

            panel.Controls.Add(btnAdd);
            panel.Controls.Add(btnEdit);
            panel.Controls.Add(btnDelete);
            panel.Controls.Add(btnRefresh);
            panel.Controls.Add(btnClose);

            // DataGridView
            dgvData = new DataGridView()
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                MultiSelect = false
            };

            this.Controls.Add(dgvData);
            this.Controls.Add(panel);
        }

        private void LoadData()
        {
            try
            {
                string result = _model.ExecuteCommand($"SELECT * FROM {_tableName};");
                dgvData.DataSource = ParseResultToDataTable(result);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            // Получаем структуру таблицы для создания формы ввода
            string structure = _model.ExecuteCommand($"SELECT * FROM {_tableName} WHERE 1=0;"); // Только заголовки
            var headers = GetHeaders(structure);

            var form = new FormRecordEdit(_model, _tableName, headers, null);
            if (form.ShowDialog() == DialogResult.OK)
            {
                LoadData();
                DataChanged?.Invoke();
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (dgvData.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите запись для редактирования", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Получаем значения выбранной строки
            var row = dgvData.SelectedRows[0];
            var values = new object[row.Cells.Count];
            for (int i = 0; i < row.Cells.Count; i++)
                values[i] = row.Cells[i].Value;

            string structure = _model.ExecuteCommand($"SELECT * FROM {_tableName} WHERE 1=0;");
            var headers = GetHeaders(structure);

            var form = new FormRecordEdit(_model, _tableName, headers, values);
            if (form.ShowDialog() == DialogResult.OK)
            {
                LoadData();
                DataChanged?.Invoke();
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (dgvData.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите запись для удаления", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show("Удалить выбранную запись?", "Подтверждение",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // Получаем значения для идентификации записи
                var row = dgvData.SelectedRows[0];
                string whereClause = "";

                // Используем первое поле как идентификатор (упрощённо)
                string fieldName = dgvData.Columns[0].HeaderText;
                object fieldValue = row.Cells[0].Value;

                if (fieldValue is string strVal)
                    whereClause = $"{fieldName} = \"{strVal}\"";
                else
                    whereClause = $"{fieldName} = {fieldValue}";

                try
                {
                    string command = $"DELETE FROM {_tableName} WHERE {whereClause};";
                    _model.ExecuteCommand(command);
                    MessageBox.Show("Запись удалена", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadData();
                    DataChanged?.Invoke();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private string[] GetHeaders(string result)
        {
            if (string.IsNullOrWhiteSpace(result))
                return new string[0];

            var lines = result.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0)
                return new string[0];

            return lines[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
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