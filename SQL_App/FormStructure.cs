using System;
using System.Data;
using System.Windows.Forms;

namespace SQL_App
{
    public partial class FormStructure : Form
    {
        private DataGridView dgvStructure;
        private string _tableName;

        public FormStructure(string tableName, string data)
        {
            _tableName = tableName;
            InitializeComponent();
            LoadStructure(data);
        }

        private void InitializeComponent()
        {
            this.Text = $"Структура таблицы: {_tableName}";
            this.Size = new System.Drawing.Size(600, 400);
            this.StartPosition = FormStartPosition.CenterParent;

            dgvStructure = new DataGridView();
            dgvStructure.Dock = DockStyle.Fill;
            dgvStructure.AllowUserToAddRows = false;
            dgvStructure.ReadOnly = true;
            dgvStructure.RowHeadersVisible = false;
            dgvStructure.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            Button btnClose = new Button() { Text = "Закрыть", Dock = DockStyle.Bottom, Height = 30 };
            btnClose.Click += (s, e) => this.Close();

            this.Controls.Add(dgvStructure);
            this.Controls.Add(btnClose);
        }

        private void LoadStructure(string data)
        {
            var dt = new DataTable();
            dt.Columns.Add("Имя поля");
            dt.Columns.Add("Тип");
            dt.Columns.Add("Длина");
            dt.Columns.Add("Точность");
            dt.Columns.Add("NOT NULL");

            if (!string.IsNullOrWhiteSpace(data))
            {
                var lines = data.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length > 0)
                {
                    string[] headers = lines[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string header in headers)
                    {
                        dt.Rows.Add(header, "", "", "", "");
                    }
                }
            }

            dgvStructure.DataSource = dt;
        }
    }
}