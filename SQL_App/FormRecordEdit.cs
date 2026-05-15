using System;
using System.Collections.Generic;
using System.Windows.Forms;
using SQL_ConsoleApp.Model;

namespace SQL_App
{
    public partial class FormRecordEdit : Form
    {
        private DatabaseModel _model;
        private string _tableName;
        private string[] _headers;
        private object[] _oldValues;
        private List<TextBox> _textBoxes;
        private bool _isEdit;

        public FormRecordEdit(DatabaseModel model, string tableName, string[] headers, object[] oldValues)
        {
            _model = model;
            _tableName = tableName;
            _headers = headers;
            _oldValues = oldValues;
            _isEdit = oldValues != null;
            _textBoxes = new List<TextBox>();

            InitializeComponent();

            if (_isEdit)
                this.Text = $"Редактирование записи в таблице: {_tableName}";
            else
                this.Text = $"Добавление записи в таблицу: {_tableName}";
        }

        private void InitializeComponent()
        {
            this.Size = new System.Drawing.Size(450, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            TableLayoutPanel tableLayout = new TableLayoutPanel();
            tableLayout.Dock = DockStyle.Fill;
            tableLayout.ColumnCount = 2;
            tableLayout.RowCount = _headers.Length + 1;
            tableLayout.AutoSize = true;
            tableLayout.Padding = new Padding(10);

            // Настройка колонок
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // Создаём поля для каждого столбца
            for (int i = 0; i < _headers.Length; i++)
            {
                Label lbl = new Label();
                lbl.Text = _headers[i];
                lbl.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
                lbl.Dock = DockStyle.Fill;
                tableLayout.Controls.Add(lbl, 0, i);

                TextBox txt = new TextBox();
                txt.Dock = DockStyle.Fill;
                txt.Name = $"txt_{i}";

                // Заполняем значениями если редактируем
                if (_isEdit && _oldValues != null && i < _oldValues.Length)
                    txt.Text = _oldValues[i]?.ToString() ?? "";

                tableLayout.Controls.Add(txt, 1, i);
                _textBoxes.Add(txt);
            }

            // Кнопки
            FlowLayoutPanel buttonPanel = new FlowLayoutPanel();
            buttonPanel.Dock = DockStyle.Bottom;
            buttonPanel.Height = 40;
            buttonPanel.Padding = new Padding(5);
            buttonPanel.FlowDirection = FlowDirection.RightToLeft;

            Button btnSave = new Button();
            btnSave.Text = "Сохранить";
            btnSave.Size = new System.Drawing.Size(100, 30);
            btnSave.Click += BtnSave_Click;

            Button btnCancel = new Button();
            btnCancel.Text = "Отмена";
            btnCancel.Size = new System.Drawing.Size(100, 30);
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            buttonPanel.Controls.Add(btnCancel);
            buttonPanel.Controls.Add(btnSave);

            // Добавляем строку с кнопками в таблицу
            tableLayout.Controls.Add(buttonPanel, 0, _headers.Length);
            tableLayout.SetColumnSpan(buttonPanel, 2);

            this.Controls.Add(tableLayout);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                // Собираем значения из полей
                string columns = "";
                string values = "";

                for (int i = 0; i < _headers.Length; i++)
                {
                    string val = _textBoxes[i].Text.Trim();
                    columns += _headers[i];

                    // Форматируем значение в зависимости от типа (упрощённо)
                    if (string.IsNullOrEmpty(val))
                        values += "NULL";
                    else if (val.Equals("TRUE", StringComparison.OrdinalIgnoreCase) ||
                             val.Equals("FALSE", StringComparison.OrdinalIgnoreCase))
                        values += val.ToUpper();
                    else if (int.TryParse(val, out _) || double.TryParse(val, out _))
                        values += val;
                    else
                        values += $"\"{val}\"";

                    if (i < _headers.Length - 1)
                    {
                        columns += ", ";
                        values += ", ";
                    }
                }

                if (_isEdit)
                {
                    // UPDATE
                    string setClause = "";
                    for (int i = 0; i < _headers.Length; i++)
                    {
                        string val = _textBoxes[i].Text.Trim();
                        string formattedVal;

                        if (string.IsNullOrEmpty(val))
                            formattedVal = "NULL";
                        else if (val.Equals("TRUE", StringComparison.OrdinalIgnoreCase) ||
                                 val.Equals("FALSE", StringComparison.OrdinalIgnoreCase))
                            formattedVal = val.ToUpper();
                        else if (int.TryParse(val, out _) || double.TryParse(val, out _))
                            formattedVal = val;
                        else
                            formattedVal = $"\"{val}\"";

                        setClause += $"{_headers[i]} = {formattedVal}";
                        if (i < _headers.Length - 1)
                            setClause += ", ";
                    }

                    // Простой WHERE по первому полю (упрощённо)
                    string whereClause = $"{_headers[0]} = \"{_oldValues[0]}\"";
                    string command = $"UPDATE {_tableName} SET {setClause} WHERE {whereClause};";
                    _model.ExecuteCommand(command);
                }
                else
                {
                    // INSERT
                    string command = $"INSERT INTO {_tableName} ({columns}) VALUE ({values});";
                    _model.ExecuteCommand(command);
                }

                MessageBox.Show("Запись сохранена", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}