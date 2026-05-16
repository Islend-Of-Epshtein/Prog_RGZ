
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace SQL_App
{
    partial class FormCreateTable
    {
        public event Action<string, RowDefinition[]> TableCreated;

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            menuStrip1 = new MenuStrip();
            сохранитьToolStripMenuItem = new ToolStripMenuItem();
            отменитьToolStripMenuItem = new ToolStripMenuItem();
            label1 = new Label();
            textBox1 = new System.Windows.Forms.TextBox();
            comboBox1 = new System.Windows.Forms.ComboBox();
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            label5 = new Label();
            label6 = new Label();
            label7 = new Label();
            AddColumnBtn = new System.Windows.Forms.Button();
            tableLayoutPanel1 = new TableLayoutPanel();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.BackColor = SystemColors.Control;
            menuStrip1.Items.AddRange(new ToolStripItem[] { сохранитьToolStripMenuItem, отменитьToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(500, 25);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            // 
            // сохранитьToolStripMenuItem
            // 
            сохранитьToolStripMenuItem.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 204);
            сохранитьToolStripMenuItem.Name = "сохранитьToolStripMenuItem";
            сохранитьToolStripMenuItem.Size = new Size(83, 21);
            сохранитьToolStripMenuItem.Text = "Сохранить";
            // 
            // отменитьToolStripMenuItem
            // 
            отменитьToolStripMenuItem.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 204);
            отменитьToolStripMenuItem.Name = "отменитьToolStripMenuItem";
            отменитьToolStripMenuItem.Size = new Size(77, 21);
            отменитьToolStripMenuItem.Text = "Отменить";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 204);
            label1.Location = new Point(12, 37);
            label1.Name = "label1";
            label1.Size = new Size(88, 17);
            label1.TabIndex = 1;
            label1.Text = "Имя таблицы";
            label1.Click += label1_Click;
            // 
            // textBox1
            // 
            textBox1.BorderStyle = BorderStyle.FixedSingle;
            textBox1.Location = new Point(106, 36);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(120, 25);
            textBox1.TabIndex = 2;
            // 
            // comboBox1
            // 
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox1.FormattingEnabled = true;
            comboBox1.Items.AddRange(new object[] { "Сохранить в регистр" });
            comboBox1.Location = new Point(246, 36);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(167, 25);
            comboBox1.TabIndex = 3;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 204);
            label2.Location = new Point(12, 80);
            label2.Name = "label2";
            label2.Size = new Size(86, 17);
            label2.TabIndex = 5;
            label2.Text = "Имя столбца";
            label2.Click += label2_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 204);
            label3.Location = new Point(145, 80);
            label3.Name = "label3";
            label3.Size = new Size(29, 17);
            label3.TabIndex = 6;
            label3.Text = "Тип";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 204);
            label4.Location = new Point(188, 80);
            label4.Name = "label4";
            label4.Size = new Size(52, 17);
            label4.TabIndex = 7;
            label4.Text = "Длинна\r\n";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 204);
            label5.Location = new Point(256, 80);
            label5.Name = "label5";
            label5.Size = new Size(63, 17);
            label5.TabIndex = 8;
            label5.Text = "Точность";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 204);
            label6.Location = new Point(325, 80);
            label6.Name = "label6";
            label6.Size = new Size(69, 17);
            label6.TabIndex = 9;
            label6.Text = "NOT NULL";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 204);
            label7.Location = new Point(400, 80);
            label7.Name = "label7";
            label7.Size = new Size(59, 17);
            label7.TabIndex = 10;
            label7.Text = "Позиция";
            // 
            // AddColumnBtn
            // 
            AddColumnBtn.BackColor = Color.Gainsboro;
            AddColumnBtn.BackgroundImageLayout = ImageLayout.Center;
            AddColumnBtn.FlatStyle = FlatStyle.Flat;
            AddColumnBtn.Location = new Point(256, 271);
            AddColumnBtn.Name = "AddColumnBtn";
            AddColumnBtn.Size = new Size(232, 33);
            AddColumnBtn.TabIndex = 11;
            AddColumnBtn.Text = "Добавить столбец";
            AddColumnBtn.UseVisualStyleBackColor = false;
            AddColumnBtn.Click += button1_Click;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 6;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            tableLayoutPanel1.Location = new Point(12, 100);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 2;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.Size = new Size(476, 165);
            tableLayoutPanel1.TabIndex = 12;
            // 
            // FormCreateTable
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.ControlLight;
            ClientSize = new Size(500, 308);
            Controls.Add(tableLayoutPanel1);
            Controls.Add(AddColumnBtn);
            Controls.Add(label7);
            Controls.Add(label6);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(comboBox1);
            Controls.Add(textBox1);
            Controls.Add(label1);
            Controls.Add(menuStrip1);
            Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 204);
            MainMenuStrip = menuStrip1;
            Name = "FormCreateTable";
            Text = "FormCreateTable";
            Load += FormCreateTable_Load;
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip menuStrip1;
        private ToolStripMenuItem сохранитьToolStripMenuItem;
        private ToolStripMenuItem отменитьToolStripMenuItem;
        private Label label1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.ComboBox comboBox1;
        private Label label2;
        private Label label3;
        private Label label4;
        private Label label5;
        private Label label6;
        private Label label7;
        private System.Windows.Forms.Button AddColumnBtn;
        private void InitializeLogic()
        {
            // Настройка DataGridView
            SetupDataGridView();

            // Привязка событий
            сохранитьToolStripMenuItem.Click += SaveButton_Click;
            отменитьToolStripMenuItem.Click += CancelButton_Click;
            AddColumnBtn.Click += AddColumnButton_Click;

            UpdatePositions();
        }

        private void SetupDataGridView()
        {
            // Разрешаем добавление и удаление строк
            dataGridView1.AllowUserToAddRows = true;
            dataGridView1.AllowUserToDeleteRows = true;

            // События для обновления позиций
            //dataGridView1.RowsAdded += (s, e) => UpdatePositions();
            //dataGridView1.RowsRemoved += (s, e) => UpdatePositions();

            // Обработка изменения типа поля
            dataGridView1.CellValueChanged += DataGridView1_CellValueChanged;
        }

        private void DataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
        }

        private void UpdatePositions()
        {
            /*
            int position = 1;
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (!row.IsNewRow && colNot != null)
                {
                    row.Cells["colNot"].Value = position.ToString();
                    position++;
                }
            }
            */
        }

        private void AddColumnButton_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.Add();
            UpdatePositions();
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            string tableName = textBox1.Text.Trim();
            if (string.IsNullOrEmpty(tableName))
            {
                MessageBox.Show("Введите имя таблицы", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var rows = new List<RowDefinition>();

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.IsNewRow) continue;

                string name = row.Cells["colName"]?.Value?.ToString();
                if (string.IsNullOrEmpty(name)) continue;

                string typeStr = row.Cells["colType"]?.Value?.ToString();
                if (string.IsNullOrEmpty(typeStr))
                {
                    MessageBox.Show($"Для столбца '{name}' не выбран тип", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                char type = typeStr[0];
                string width = row.Cells["colLength"]?.Value?.ToString() ?? "";
                bool notNull = row.Cells["colNotNull"]?.Value != null && (bool)row.Cells["colNotNull"].Value;

                // Валидация
                if (type == 'C' && string.IsNullOrEmpty(width))
                {
                    MessageBox.Show($"Для столбца '{name}' типа C необходима длина", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (type == 'N' && string.IsNullOrEmpty(width))
                {
                    MessageBox.Show($"Для столбца '{name}' типа N необходима длина", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int widthInt = 0;
                if (!string.IsNullOrEmpty(width) && !int.TryParse(width, out widthInt))
                {
                    MessageBox.Show($"Для столбца '{name}' длина должна быть числом", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    rows.Add(new RowDefinition(type, name, notNull, width, ""));
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

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        private TableLayoutPanel tableLayoutPanel1;
    }

    // Структура RowDefinition для передачи данных
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