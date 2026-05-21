using SQL_ConsoleApp.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;

namespace SQL_WPF_App
{
    public partial class MainWindow : Window
    {
        private readonly DatabaseModel _model;

        public MainWindow()
        {
            InitializeComponent();
            _model = new DatabaseModel();
        }

        /// <summary>
        /// Обработчик создания новой таблицы. Открывает форму создания,
        /// формирует SQL-команду CREATE TABLE и выполняет её.
        /// </summary>
        private void CreateItem_Click(object sender, RoutedEventArgs e)
        {
            var form = new FormCreateTable();
            form.TableCreated += (tableName, rows) =>
            {
                try
                {
                    string command = BuildCreateTableCommand(tableName, rows);
                    string result = _model.ExecuteCommand(command);
                    MessageBox.Show(result, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    RefreshData();
                }
                catch (Exception ex)
                {
                    ShowError(ex.Message);
                }
            };
            form.ShowDialog();
        }

        /// <summary>
        /// Формирует SQL-команду CREATE TABLE на основе имени и определений полей.
        /// </summary>
        /// <param name="tableName">Имя создаваемой таблицы.</param>
        /// <param name="rows">Массив определений полей таблицы.</param>
        /// <returns>Строка SQL-команды CREATE TABLE.</returns>
        private static string BuildCreateTableCommand(string tableName, RowDefinition[] rows)
        {
            var parts = new List<string>();
            foreach (var row in rows)
            {
                string fieldDef = $"{row.Name} {row.Type}";
                if (row.Type == 'C')
                    fieldDef += $"({row.Width})";
                else if (row.Type == 'N')
                    fieldDef += $"({row.Width},{row.Precision})";
                if (row.IsNotNull)
                    fieldDef += " NOT NULL";
                parts.Add(fieldDef);
            }
            return $"CREATE TABLE {tableName} ({string.Join(", ", parts)});";
        }

        /// <summary>
        /// Обработчик открытия существующей таблицы через диалог выбора файла.
        /// </summary>
        private void OpenItem_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "DBF files (*.dbf)|*.dbf|All files (*.*)|*.*",
                Title = "Открыть таблицу"
            };

            if (ofd.ShowDialog() == true)
            {
                try
                {
                    string result = _model.ExecuteCommand($"OPEN {ofd.FileName};");
                    MessageBox.Show(result, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    RefreshData();
                }
                catch (Exception ex)
                {
                    ShowError(ex.Message);
                }
            }
        }

        /// <summary>
        /// Обработчик изменения структуры таблицы. Определяет различия между старой
        /// и новой структурой и выполняет соответствующие ALTER TABLE команды.
        /// </summary>
        private void StructureItem_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureTableOpened()) return;

            try
            {
                var fields = _model.GetTableStructure();
                var form = new FormCreateTable(_model.GetTableName(), fields);
                form.TableStructureChanged += (newName, oldName, newRows) =>
                {
                    try
                    {
                        if (!ConfirmStructureChange(oldName)) return;

                        var oldDict = fields.ToDictionary(f => f.Name);
                        var newDict = newRows.ToDictionary(r => r.Name);
                        var renamedColumns = FindRenamedColumns(oldDict, newRows, newDict);

                        RenameColumns(renamedColumns);
                        AddNewColumns(newRows, oldDict, renamedColumns);
                        UpdateExistingColumns(newRows, oldDict, renamedColumns);
                        RemoveOldColumns(fields, newDict, renamedColumns);
                        RenameTableIfNeeded(newName, oldName);

                        MessageBox.Show($"Структура таблицы '{oldName}' изменена.", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        RefreshData();
                    }
                    catch (Exception ex)
                    {
                        ShowError(ex.Message);
                    }
                };
                form.ShowDialog();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка загрузки структуры: {ex.Message}");
            }
        }

        /// <summary>
        /// Находит столбцы, которые были переименованы (совпадают тип и размеры, но разные имена).
        /// </summary>
        private static List<(string OldName, string NewName)> FindRenamedColumns(
            Dictionary<string, (string Name, char Type, int Length, int Precision, bool NotNull)> oldFields,
            RowDefinition[] newRows,
            Dictionary<string, RowDefinition> newDict)
        {
            var result = new List<(string, string)>();
            foreach (var newRow in newRows)
            {
                if (oldFields.ContainsKey(newRow.Name)) continue;

                var match = oldFields.FirstOrDefault(f =>
                    !newDict.ContainsKey(f.Value.Name) &&
                    f.Value.Type == newRow.Type &&
                    f.Value.Length == newRow.Width &&
                    f.Value.Precision == newRow.Precision);

                if (match.Value.Name != null)
                    result.Add((match.Value.Name, newRow.Name));
            }
            return result;
        }

        /// <summary>
        /// Выполняет переименование столбцов через ALTER TABLE COLUMN RENAME.
        /// </summary>
        private void RenameColumns(List<(string OldName, string NewName)> renamedColumns)
        {
            foreach (var (oldName, newName) in renamedColumns)
                _model.ExecuteCommand($"ALTER TABLE {_model.GetTableName()} COLUMN RENAME {oldName} {newName};");
        }

        /// <summary>
        /// Добавляет новые столбцы, которые отсутствовали в старой структуре и не являются переименованными.
        /// </summary>
        private void AddNewColumns(RowDefinition[] newRows,
            Dictionary<string, (string, char, int, int, bool)> oldDict,
            List<(string OldName, string NewName)> renamedColumns)
        {
            foreach (var newRow in newRows)
            {
                if (oldDict.ContainsKey(newRow.Name) || renamedColumns.Any(r => r.NewName == newRow.Name))
                    continue;

                _model.ExecuteCommand(BuildAlterAddCommand(newRow));
            }
        }

        /// <summary>
        /// Обновляет существующие столбцы, у которых изменились тип, размер или NOT NULL.
        /// </summary>
        private void UpdateExistingColumns(RowDefinition[] newRows,
            Dictionary<string, (string, char, int, int, bool)> oldDict,
            List<(string OldName, string NewName)> renamedColumns)
        {
            foreach (var newRow in newRows)
            {
                string currentName = renamedColumns
                    .FirstOrDefault(r => r.NewName == newRow.Name).OldName ?? newRow.Name;

                if (!oldDict.TryGetValue(currentName, out var oldRow)) continue;

                if (oldRow.Item2 != newRow.Type ||
                    oldRow.Item3 != newRow.Width ||
                    oldRow.Item4 != newRow.Precision ||
                    oldRow.Item5 != newRow.IsNotNull)
                {
                    _model.ExecuteCommand(BuildAlterUpdateCommand(currentName, newRow));
                }
            }
        }

        /// <summary>
        /// Удаляет столбцы, которые отсутствуют в новой структуре и не были переименованы.
        /// </summary>
        private void RemoveOldColumns(
            List<(string Name, char, int, int, bool)> oldFields,
            Dictionary<string, RowDefinition> newDict,
            List<(string OldName, string NewName)> renamedColumns)
        {
            foreach (var oldRow in oldFields)
            {
                if (!newDict.ContainsKey(oldRow.Name) && !renamedColumns.Any(r => r.OldName == oldRow.Name))
                    _model.ExecuteCommand($"ALTER TABLE {_model.GetTableName()} COLUMN REMOVE {oldRow.Name};");
            }
        }

        /// <summary>
        /// Переименовывает таблицу, если имя изменилось.
        /// </summary>
        private void RenameTableIfNeeded(string newName, string oldName)
        {
            if (string.Equals(newName, oldName)) return;

            _model.RenameTable(oldName, newName);
            dgvResult.ItemsSource = null;
        }

        /// <summary>
        /// Формирует SQL-команду ALTER TABLE COLUMN ADD для нового столбца.
        /// </summary>
        private string BuildAlterAddCommand(RowDefinition row)
        {
            string cmd = $"ALTER TABLE {_model.GetTableName()} COLUMN ADD {row.Name} {row.Type}";
            if (row.Type == 'C') cmd += $"({row.Width})";
            else if (row.Type == 'N') cmd += $"({row.Width},{row.Precision})";
            if (row.IsNotNull) cmd += " NOT NULL";
            return cmd + ";";
        }

        /// <summary>
        /// Формирует SQL-команду ALTER TABLE COLUMN UPDATE для изменения типа столбца.
        /// </summary>
        private string BuildAlterUpdateCommand(string columnName, RowDefinition row)
        {
            string cmd = $"ALTER TABLE {_model.GetTableName()} COLUMN UPDATE {columnName} {row.Type}";
            if (row.Type == 'C') cmd += $"({row.Width})";
            else if (row.Type == 'N') cmd += $"({row.Width},{row.Precision})";
            if (row.IsNotNull) cmd += " NOT NULL";
            return cmd + ";";
        }

        /// <summary>
        /// Запрашивает подтверждение перед изменением структуры таблицы.
        /// </summary>
        private static bool ConfirmStructureChange(string tableName)
        {
            return MessageBox.Show(
                $"Вы уверены, что хотите изменить структуру таблицы '{tableName}'?\n\n" +
                "Внимание: Эта операция может привести к потере данных при изменении типов полей!",
                "Подтверждение изменения структуры",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) == MessageBoxResult.Yes;
        }

        /// <summary>
        /// Обработчик физического удаления помеченных записей (TRUNCATE).
        /// </summary>
        private void ShrinkItem_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureTableOpened()) return;

            if (MessageBox.Show(
                    $"Вы уверены, что хотите выполнить TRUNCATE для таблицы '{_model.GetTableName()}'?\n\n" +
                    "Внимание: Эта операция безвозвратно удалит все помеченные записи!",
                    "Подтверждение очистки",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                string resultMsg = _model.ExecuteCommand($"TRUNCATE {_model.GetTableName()};");
                MessageBox.Show(resultMsg, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                RefreshData();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        /// <summary>
        /// Обработчик закрытия текущей таблицы.
        /// </summary>
        private void CloseItem_Click(object sender, RoutedEventArgs e)
        {
            if (!_model.IsTableOpened())
            {
                MessageBox.Show("Таблиц не открыто", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            try
            {
                _model.CloseTable();
                dgvResult.ItemsSource = null;
                MessageBox.Show("Таблица закрыта", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        /// <summary>
        /// Обработчик удаления таблицы (DROP TABLE).
        /// </summary>
        private void DropItem_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureTableOpened()) return;

            if (MessageBox.Show(
                    $"Удалить таблицу '{_model.GetTableName()}'?\n\n" +
                    "Внимание: Эта операция безвозвратно удалит все данные!",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                _model.ExecuteCommand($"DROP TABLE {_model.GetTableName()};");
                dgvResult.ItemsSource = null;
                MessageBox.Show("Таблица удалена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        /// <summary>
        /// Обработчик открытия окна просмотра и редактирования данных таблицы.
        /// </summary>
        private void DataItem_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureTableOpened()) return;

            var form = new FormDataView(_model);
            form.DataChanged += RefreshData;
            form.ShowDialog();
        }

        /// <summary>
        /// Обработчик открытия окна выполнения произвольных SQL-запросов.
        /// </summary>
        private void QueryItem_Click(object sender, RoutedEventArgs e)
        {
            var form = new FormQuery(_model);
            form.QueryExecuted += RefreshData;
            form.ShowDialog();
        }

        /// <summary>
        /// Обработчик выхода из приложения.
        /// </summary>
        private void ExitItem_Click(object sender, RoutedEventArgs e)
        {
            _model.CloseTable();
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Обработчик открытия окна справки.
        /// </summary>
        private void HelpItem_Click(object sender, RoutedEventArgs e)
        {
            var formHelp = new FormHelp { Owner = this };
            formHelp.ShowDialog();
        }

        /// <summary>
        /// Обновляет данные в главном DataGrid, выполняя SELECT * FROM текущей таблицы.
        /// </summary>
        private void RefreshData()
        {
            if (!_model.IsTableOpened()) return;

            try
            {
                _model.ExecuteCommand($"SELECT * FROM {_model.GetTableName()};");
                var data = _model.GetSelectResult();
                var dt = FormDataView.CreateDataTableFromData(data, _model.GetTableStructure());
                dgvResult.ItemsSource = dt.DefaultView;
            }
            catch
            {
                dgvResult.ItemsSource = null;
            }
        }

        /// <summary>
        /// Проверяет, открыта ли таблица, и выводит предупреждение, если нет.
        /// </summary>
        /// <returns>true, если таблица открыта; иначе false.</returns>
        private bool EnsureTableOpened()
        {
            if (_model.IsTableOpened()) return true;
            MessageBox.Show("Сначала откройте таблицу", "Предупреждение",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        /// <summary>
        /// Выводит сообщение об ошибке в MessageBox.
        /// </summary>
        /// <param name="message">Текст ошибки.</param>
        private static void ShowError(string message)
        {
            MessageBox.Show($"Ошибка: {message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}