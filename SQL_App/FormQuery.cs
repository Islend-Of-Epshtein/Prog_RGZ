using System;
using System.Data;
using System.Windows.Forms;
using SQL_ConsoleApp.Model;

namespace SQL_App
{
    public partial class FormQuery : Form
    {
        private DatabaseModel _model;
        private RichTextBox rtbInput;
        private DataGridView dgvOutput;
        private Button btnExecute, btnClear, btnClose;

        public event Action QueryExecuted;

        public FormQuery(DatabaseModel model)
        {
            _model = model;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Интерпретатор SQL - выполнение SQL-запроса";
            this.Size = new System.Drawing.Size(800, 600);
            this.StartPosition = FormStartPosition.CenterParent;

            // Верхняя панель с кнопками как на Рисунке 5
            var panelTop = new Panel() { Dock = DockStyle.Top, Height = 40 };

            btnExecute = new Button() { Text = "Выполнить", Location = new System.Drawing.Point(12, 8), Width = 100 };
            btnClear = new Button() { Text = "Очистить", Location = new System.Drawing.Point(120, 8), Width = 100 };
            btnClose = new Button() { Text = "Закрыть", Location = new System.Drawing.Point(228, 8), Width = 100 };

            btnExecute.Click += BtnExecute_Click;
            btnClear.Click += (s, e) => rtbInput.Clear();
            btnClose.Click += (s, e) => this.Close();

            panelTop.Controls.Add(btnExecute);
            panelTop.Controls.Add(btnClear);
            panelTop.Controls.Add(btnClose);

            // Поле ввода SQL (как на рисунке)
            rtbInput = new RichTextBox()
            {
                Dock = DockStyle.Top,
                Height = 200,
                Font = new System.Drawing.Font("Consolas", 11F),
                BackColor = System.Drawing.Color.White
            };

            // DataGridView для вывода результатов
            dgvOutput = new DataGridView()
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = System.Drawing.Color.White
            };

            this.Controls.Add(dgvOutput);
            this.Controls.Add(rtbInput);
            this.Controls.Add(panelTop);
        }

        private void BtnExecute_Click(object sender, EventArgs e)
        {
            string sql = rtbInput.Text.Trim();
            if (string.IsNullOrEmpty(sql))
            {
                MessageBox.Show("Введите SQL-запрос", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                    dgvOutput.DataSource = dt;
                    dgvOutput.AutoResizeColumns();
                }
                else
                {
                    MessageBox.Show(result, "Результат", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dgvOutput.DataSource = null;
                }

                QueryExecuted?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка выполнения запроса:\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
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