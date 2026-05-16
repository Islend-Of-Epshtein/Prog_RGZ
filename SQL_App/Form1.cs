using System;
using System.Windows.Forms;
using SQL_ConsoleApp.Model;

namespace SQL_App
{
    public partial class Form1 : Form
    {
        private DatabaseModel _model;
        private DataGridView dgvResult;
        private string _currentTableName;

        public Form1()
        {
            InitializeComponent();
            _model = new DatabaseModel();
            CustomizeInterface();
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            // 
            // Form1
            // 
            BackColor = SystemColors.ActiveCaption;
            ClientSize = new Size(800, 25);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Интерпретатор SQL";
            Load += Form1_Load;
            ResumeLayout(false);
        }

        private void CustomizeInterface()
        {
            // MenuStrip
            MenuStrip menuStrip = new MenuStrip();

            // Пункты меню как на Рисунке 1 методички
            ToolStripMenuItem commandsMenu = new ToolStripMenuItem("Команды");

            ToolStripMenuItem createItem = new ToolStripMenuItem("Создать");
            ToolStripMenuItem openItem = new ToolStripMenuItem("Открыть");
            ToolStripMenuItem structureItem = new ToolStripMenuItem("Структура");
            ToolStripMenuItem shrinkItem = new ToolStripMenuItem("Сжать");
            ToolStripMenuItem closeItem = new ToolStripMenuItem("Закрыть");
            ToolStripMenuItem dropItem = new ToolStripMenuItem("Удалить");
            ToolStripMenuItem dataItem = new ToolStripMenuItem("Данные");
            ToolStripMenuItem queryItem = new ToolStripMenuItem("Запрос");
            ToolStripMenuItem exitItem = new ToolStripMenuItem("Выход");
            ToolStripMenuItem helpItem = new ToolStripMenuItem("?");

            // ========== Создаём дубликаты для подменю "Команды" ==========
            ToolStripMenuItem createItemMenu = new ToolStripMenuItem("Создать");
            ToolStripMenuItem openItemMenu = new ToolStripMenuItem("Открыть");
            ToolStripMenuItem structureItemMenu = new ToolStripMenuItem("Структура");
            ToolStripMenuItem shrinkItemMenu = new ToolStripMenuItem("Сжать");
            ToolStripMenuItem closeItemMenu = new ToolStripMenuItem("Закрыть");
            ToolStripMenuItem dropItemMenu = new ToolStripMenuItem("Удалить");
            ToolStripMenuItem dataItemMenu = new ToolStripMenuItem("Данные");
            ToolStripMenuItem queryItemMenu = new ToolStripMenuItem("Запрос");
            ToolStripMenuItem exitItemMenu = new ToolStripMenuItem("Выход");

            // Attach click events
            createItem.Click += CreateItem_Click;
            openItem.Click += OpenItem_Click;
            structureItem.Click += StructureItem_Click;
            shrinkItem.Click += ShrinkItem_Click;
            closeItem.Click += CloseItem_Click;
            dropItem.Click += DropItem_Click;
            dataItem.Click += DataItem_Click;
            queryItem.Click += QueryItem_Click;
            exitItem.Click += (s, e) => { _model.CloseTable(); Application.Exit(); };
            helpItem.Click += HelpItem_Click;
            //ТЕ ЖЕ САМЫЕ
            createItemMenu.Click += CreateItem_Click;
            openItemMenu.Click += OpenItem_Click;
            structureItemMenu.Click += StructureItem_Click;
            shrinkItemMenu.Click += ShrinkItem_Click;
            closeItemMenu.Click += CloseItem_Click;
            dropItemMenu.Click += DropItem_Click;
            dataItemMenu.Click += DataItem_Click;
            queryItemMenu.Click += QueryItem_Click;
            exitItemMenu.Click += (s, e) => { _model.CloseTable(); Application.Exit(); };

            commandsMenu.DropDownItems.Add(createItemMenu);
            commandsMenu.DropDownItems.Add(openItemMenu);
            commandsMenu.DropDownItems.Add(structureItemMenu);
            commandsMenu.DropDownItems.Add(shrinkItemMenu);
            commandsMenu.DropDownItems.Add(closeItemMenu);
            commandsMenu.DropDownItems.Add(dropItemMenu);
            commandsMenu.DropDownItems.Add(dataItemMenu);
            commandsMenu.DropDownItems.Add(queryItemMenu);
            commandsMenu.DropDownItems.Add(exitItemMenu);

            menuStrip.Items.Add(commandsMenu);
            menuStrip.Items.Add(createItem);
            menuStrip.Items.Add(openItem);
            menuStrip.Items.Add(structureItem);
            menuStrip.Items.Add(shrinkItem);
            menuStrip.Items.Add(closeItem);
            menuStrip.Items.Add(dropItem);
            menuStrip.Items.Add(dataItem);
            menuStrip.Items.Add(queryItem);
            menuStrip.Items.Add(exitItem);
            menuStrip.Items.Add(helpItem);

            menuStrip.BackColor = Color.FromArgb(215, 228, 242);

            this.Controls.Add(menuStrip);
            this.MainMenuStrip = menuStrip;

            // DataGridView для отображения результатов
            dgvResult = new DataGridView();
            dgvResult.Dock = DockStyle.Fill;
            dgvResult.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvResult.AllowUserToAddRows = false;
            dgvResult.ReadOnly = true;
            dgvResult.RowHeadersVisible = false;
            dgvResult.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvResult.BackgroundColor = System.Drawing.Color.White;

            this.Controls.Add(dgvResult);
        }

        private void CreateItem_Click(object sender, EventArgs e)
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
                    MessageBox.Show(result, "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    RefreshData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            form.ShowDialog();
        }

        private void OpenItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "DBF files (*.dbf)|*.dbf|All files (*.*)|*.*";
                ofd.Title = "Открыть таблицу";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string command = $"OPEN {ofd.FileName};";
                        string result = _model.ExecuteCommand(command);
                        _currentTableName = System.IO.Path.GetFileNameWithoutExtension(ofd.FileName);
                        MessageBox.Show(result, "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        RefreshData();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void StructureItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentTableName))
            {
                MessageBox.Show("Сначала откройте таблицу", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Получаем структуру таблицы через SELECT *
                string result = _model.ExecuteCommand($"SELECT * FROM {_currentTableName};");
                var form = new FormStructure(_currentTableName, result);
                form.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShrinkItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentTableName))
            {
                MessageBox.Show("Сначала откройте таблицу", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string command = $"TRUNCATE {_currentTableName};";
                string result = _model.ExecuteCommand(command);
                MessageBox.Show(result, "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CloseItem_Click(object sender, EventArgs e)
        {
            try
            {
                _model.CloseTable();
                _currentTableName = null;
                dgvResult.DataSource = null;
                MessageBox.Show("Таблица закрыта", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DropItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentTableName))
            {
                MessageBox.Show("Сначала откройте таблицу", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show($"Удалить таблицу '{_currentTableName}'?", "Подтверждение",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    string command = $"DROP TABLE {_currentTableName};";
                    _model.ExecuteCommand(command);
                    _currentTableName = null;
                    dgvResult.DataSource = null;
                    MessageBox.Show("Таблица удалена", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void DataItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentTableName))
            {
                MessageBox.Show("Сначала откройте таблицу", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var form = new FormDataView(_model, _currentTableName);
            form.DataChanged += RefreshData;
            form.ShowDialog();
        }

        private void QueryItem_Click(object sender, EventArgs e)
        {
            var form = new FormQuery(_model);
            form.QueryExecuted += RefreshData;
            form.ShowDialog();
        }

        private void HelpItem_Click(object sender, EventArgs e)
        {
            string helpText = @"SQL Interpreter - Справка

Команды:
CREATE TABLE <имя> (<поле1> <тип> [NOT NULL], ...);
OPEN <имя_файла>;
CLOSE;
ALTER TABLE <имя> COLUMN ADD <поле> <тип> [NOT NULL];
ALTER TABLE <имя> COLUMN REMOVE <поле>;
ALTER TABLE <имя> COLUMN RENAME <старое> <новое>;
INSERT INTO <имя> (<поля>) VALUE (<значения>);
UPDATE <имя> SET <поле>=<значение> [WHERE <условие>];
DELETE FROM <имя> [WHERE <условие>];
SELECT *|поля FROM <имя> [WHERE <условие>];
TRUNCATE <имя>;
RESTORE <имя> [WHERE <условие>];
DROP TABLE <имя>;
EXIT;

Типы данных:
C(n) - строка длиной n
D - дата (DD.MM.YYYY)
L - логический (TRUE/FALSE)
N(n,d) - число (n цифр всего, d после запятой)
M - MEMO (текст)

Логические операторы: AND, OR, XOR, NOT";

            MessageBox.Show(helpText, "Справка", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                dgvResult.DataSource = dt;
            }
            catch (Exception ex)
            {
                dgvResult.DataSource = null;
            }
        }

        private System.Data.DataTable ParseResultToDataTable(string result)
        {
            var dt = new System.Data.DataTable();
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

        private void Form1_Load(object sender, EventArgs e)
        {
        }
    }
}