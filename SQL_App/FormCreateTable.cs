using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SQL_App
{
    public partial class FormCreateTable : Form
    {
        private DataGridView dgvColumns;
        private TextBox txtTableName;
        private Button btnSave, btnCancel, btnAddColumn;

        public event Action<string, RowDefinition[]> TableCreated;

        public FormCreateTable()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Интерпретатор SQL - создание таблицы";
            this.Size = new System.Drawing.Size(700, 450);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Top Buttons
            btnSave = new Button() { Text = "Сохранить", Location = new System.Drawing.Point(12, 12), Width = 80 };
            btnCancel = new Button() { Text = "Отменить", Location = new System.Drawing.Point(100, 12), Width = 80 };
            btnSave.Click += BtnSave_Click;
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnSave);
            this.Controls.Add(btnCancel);

            // Table Name
            Label lblTableName = new Label() { Text = "Имя таблицы", Location = new System.Drawing.Point(12, 50), AutoSize = true };
            txtTableName = new TextBox() { Location = new System.Drawing.Point(120, 47), Width = 150 };
            this.Controls.Add(lblTableName);
            this.Controls.Add(txtTableName);

            // DataGridView for Columns (как на Рисунке 2)
            dgvColumns = new DataGridView();
            dgvColumns.Location = new System.Drawing.Point(12, 80);
            dgvColumns.Size = new System.Drawing.Size(660, 280);
            dgvColumns.AllowUserToAddRows = true;
            dgvColumns.RowHeadersVisible = false;
            dgvColumns.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Столбцы как на рисунке
            DataGridViewTextBoxColumn colName = new DataGridViewTextBoxColumn();
            colName.HeaderText = "Имя столбца";
            colName.Name = "colName";

            DataGridViewComboBoxColumn colType = new DataGridViewComboBoxColumn();
            colType.HeaderText = "Тип";
            colType.Name = "colType";
            colType.Items.AddRange(new object[] { "C", "D", "L", "M", "N" });

            DataGridViewTextBoxColumn colLength = new DataGridViewTextBoxColumn();
            colLength.HeaderText = "Длина";
            colLength.Name = "colLength";

            DataGridViewTextBoxColumn colPrecision = new DataGridViewTextBoxColumn();
            colPrecision.HeaderText = "Точность";
            colPrecision.Name = "colPrecision";

            DataGridViewCheckBoxColumn colNotNull = new DataGridViewCheckBoxColumn();
            colNotNull.HeaderText = "NOT NULL";
            colNotNull.Name = "colNotNull";

            // UpDown колонка для позиции
            DataGridViewTextBoxColumn colPosition = new DataGridViewTextBoxColumn();
            colPosition.HeaderText = "Позиция";
            colPosition.Name = "colPosition";
            colPosition.ReadOnly = true;

            dgvColumns.Columns.AddRange(new DataGridViewColumn[] { colName, colType, colLength, colPrecision, colNotNull, colPosition });

            // Автоматическая нумерация позиций
            dgvColumns.RowsAdded += (s, e) => UpdatePositions();
            dgvColumns.RowsRemoved += (s, e) => UpdatePositions();

            this.Controls.Add(dgvColumns);

            // Add Column Button как на рисунке
            btnAddColumn = new Button() { Text = "Добавить столбец", Location = new System.Drawing.Point(500, 370), Width = 170, Height = 30 };
            btnAddColumn.Click += (s, e) =>
            {
                dgvColumns.Rows.Add();
                UpdatePositions();
            };
            this.Controls.Add(btnAddColumn);

            // Начальная строка
            dgvColumns.Rows.Add();
            UpdatePositions();
        }

        private void UpdatePositions()
        {
            for (int i = 0; i < dgvColumns.Rows.Count; i++)
            {
                if (!dgvColumns.Rows[i].IsNewRow)
                    dgvColumns.Rows[i].Cells["colPosition"].Value = (i + 1).ToString();
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            string tableName = txtTableName.Text.Trim();
            if (string.IsNullOrEmpty(tableName))
            {
                MessageBox.Show("Введите имя таблицы", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var rows = new List<RowDefinition>();

            foreach (DataGridViewRow row in dgvColumns.Rows)
            {
                if (row.IsNewRow) continue;

                string name = row.Cells["colName"].Value?.ToString();
                if (string.IsNullOrEmpty(name)) continue;

                string typeStr = row.Cells["colType"].Value?.ToString();
                if (string.IsNullOrEmpty(typeStr))
                {
                    MessageBox.Show($"Для столбца '{name}' не выбран тип", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                char type = typeStr[0];
                string width = row.Cells["colLength"].Value?.ToString() ?? "";
                string precision = row.Cells["colPrecision"].Value?.ToString() ?? "";
                bool notNull = row.Cells["colNotNull"].Value != null && (bool)row.Cells["colNotNull"].Value;

                // Валидация
                if (type == 'C' && string.IsNullOrEmpty(width))
                {
                    MessageBox.Show($"Для столбца '{name}' типа C необходима длина", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (type == 'N' && (string.IsNullOrEmpty(width) || string.IsNullOrEmpty(precision)))
                {
                    MessageBox.Show($"Для столбца '{name}' типа N необходимы длина и точность", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    rows.Add(new RowDefinition(type, name, notNull, width, precision));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка в столбце '{name}': {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            if (rows.Count == 0)
            {
                MessageBox.Show("Добавьте хотя бы один столбец", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            TableCreated?.Invoke(tableName, rows.ToArray());
            this.Close();
        }
    }

    // Копия RowDefinition из консольного проекта
    public struct RowDefinition
    {
        public char Type;
        public string Name;
        public int Width;
        public int Precision;
        public bool IsNotNull;

        public RowDefinition(char type, string name, bool isNotNull, string width = "", string precision = "")
        {
            Type = type;
            Name = name;
            IsNotNull = isNotNull;
            Width = 0;
            Precision = 0;

            if (type == 'C' && !string.IsNullOrEmpty(width))
                Width = int.Parse(width);
            else if (type == 'N')
            {
                if (!string.IsNullOrEmpty(width)) Width = int.Parse(width);
                if (!string.IsNullOrEmpty(precision)) Precision = int.Parse(precision);
            }
        }
    }
}