using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SQL_WPF_App
{
    public partial class FormHelp : Window
    {
        private List<CommandInfo> _allCommands;

        public FormHelp()
        {
            InitializeComponent();
            LoadCommands();
            DisplayAllCommands();

            // Добавляем обработчик поиска по Enter
            txtSearch.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                    BtnSearch_Click(null, null);
            };
        }

        private void LoadCommands()
        {
            _allCommands = new List<CommandInfo>
            {
                new CommandInfo
                {
                    Name = "CREATE TABLE",
                    Syntax = "CREATE TABLE <имя> (<поле1> <тип> [NOT NULL], ...);",
                    Description = "Создаёт новую таблицу с указанной структурой",
                    Example = "CREATE TABLE employees (id N(5,0) NOT NULL, name C(50), salary N(10,2));",
                    Category = "DDL"
                },
                new CommandInfo
                {
                    Name = "OPEN",
                    Syntax = "OPEN <путь_к_файлу>;",
                    Description = "Открывает существующую DBF таблицу",
                    Example = "OPEN C:\\data\\employees.dbf;",
                    Category = "DDL"
                },
                new CommandInfo
                {
                    Name = "CLOSE",
                    Syntax = "CLOSE;",
                    Description = "Закрывает текущую открытую таблицу",
                    Example = "CLOSE;",
                    Category = "DDL"
                },
                new CommandInfo
                {
                    Name = "DROP TABLE",
                    Syntax = "DROP TABLE <имя>;",
                    Description = "Удаляет таблицу и все связанные файлы",
                    Example = "DROP TABLE employees;",
                    Category = "DDL"
                },
                new CommandInfo
                {
                    Name = "ALTER TABLE ADD",
                    Syntax = "ALTER TABLE <имя> COLUMN ADD <поле> <тип> [NOT NULL];",
                    Description = "Добавляет новый столбец в таблицу",
                    Example = "ALTER TABLE employees COLUMN ADD email C(100);",
                    Category = "DDL"
                },
                new CommandInfo
                {
                    Name = "ALTER TABLE REMOVE",
                    Syntax = "ALTER TABLE <имя> COLUMN REMOVE <поле>;",
                    Description = "Удаляет столбец из таблицы",
                    Example = "ALTER TABLE employees COLUMN REMOVE email;",
                    Category = "DDL"
                },
                new CommandInfo
                {
                    Name = "ALTER TABLE RENAME",
                    Syntax = "ALTER TABLE <имя> COLUMN RENAME <старое> <новое>;",
                    Description = "Переименовывает столбец",
                    Example = "ALTER TABLE employees COLUMN RENAME salary wage;",
                    Category = "DDL"
                },
                new CommandInfo
                {
                    Name = "ALTER TABLE UPDATE",
                    Syntax = "ALTER TABLE <имя> COLUMN UPDATE <поле> <новый_тип>;",
                    Description = "Изменяет тип данных столбца",
                    Example = "ALTER TABLE employees COLUMN UPDATE salary N(12,2);",
                    Category = "DDL"
                },
                new CommandInfo
                {
                    Name = "INSERT",
                    Syntax = "INSERT INTO <имя> (<поля>) VALUE (<значения>);",
                    Description = "Добавляет новую запись в таблицу",
                    Example = "INSERT INTO employees (id, name, salary) VALUE (1, \"Иван\", 50000.00);",
                    Category = "DML"
                },
                new CommandInfo
                {
                    Name = "SELECT",
                    Syntax = "SELECT <*|поле1,...> FROM <имя> [WHERE <условие>];",
                    Description = "Выбирает данные из таблицы с возможной фильтрацией",
                    Example = "SELECT * FROM employees WHERE salary > 40000 AND active = T;",
                    Category = "DML"
                },
                new CommandInfo
                {
                    Name = "UPDATE",
                    Syntax = "UPDATE <имя> SET <поле>=<значение> [WHERE <условие>];",
                    Description = "Обновляет данные в таблице",
                    Example = "UPDATE employees SET salary = salary * 1.1 WHERE salary < 30000;",
                    Category = "DML"
                },
                new CommandInfo
                {
                    Name = "DELETE",
                    Syntax = "DELETE FROM <имя> [WHERE <условие>];",
                    Description = "Логически удаляет записи (помечает на удаление)",
                    Example = "DELETE FROM employees WHERE active = N;",
                    Category = "DML"
                },
                new CommandInfo
                {
                    Name = "RESTORE",
                    Syntax = "RESTORE <имя> [WHERE <условие>];",
                    Description = "Восстанавливает ранее удалённые записи",
                    Example = "RESTORE employees WHERE active = N;",
                    Category = "DML"
                },
                new CommandInfo
                {
                    Name = "TRUNCATE",
                    Syntax = "TRUNCATE <имя>;",
                    Description = "Физически удаляет все помеченные записи",
                    Example = "TRUNCATE employees;",
                    Category = "DML"
                }
            };
        }

        private void DisplayAllCommands()
        {
            stackAllCommands.Children.Clear();

            var groupedCommands = _allCommands.GroupBy(c => c.Category);

            foreach (var group in groupedCommands)
            {
                // Заголовок категории
                var categoryHeader = new Border
                {
                    Background = (Brush)new BrushConverter().ConvertFrom("#4A90D9"),
                    CornerRadius = new CornerRadius(5),
                    Margin = new Thickness(0, 10, 0, 5),
                    Padding = new Thickness(10, 5, 10, 5)
                };
                categoryHeader.Child = new TextBlock
                {
                    Text = group.Key == "DDL" ? "🏗️ DDL - Команды определения данных" : "📝 DML - Команды манипуляции данными",
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    FontSize = 14
                };
                stackAllCommands.Children.Add(categoryHeader);

                // Команды в категории
                foreach (var cmd in group)
                {
                    var border = CreateCommandCard(cmd);
                    stackAllCommands.Children.Add(border);
                }
            }
        }

        private Border CreateCommandCard(CommandInfo cmd)
        {
            var border = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(6),
                Margin = new Thickness(0, 5, 0, 5),
                Padding = new Thickness(12),
                Cursor = Cursors.Hand,
                Tag = cmd
            };

            // Добавляем эффект тени
            border.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 3,
                ShadowDepth = 1,
                Opacity = 0.1
            };

            // Обработчик клика для копирования
            border.MouseLeftButtonDown += (s, e) =>
            {
                var command = ((Border)s).Tag as CommandInfo;
                if (command != null)
                {
                    Clipboard.SetText(command.Syntax);
                    txtTip.Text = $"✅ Скопировано: {command.Syntax}";
                    txtTip.Foreground = (Brush)new BrushConverter().ConvertFrom("#27AE60");

                    // Анимация подсветки
                    border.Background = (Brush)new BrushConverter().ConvertFrom("#E8F4FD");
                    var timer = new System.Windows.Threading.DispatcherTimer();
                    timer.Interval = System.TimeSpan.FromMilliseconds(300);
                    timer.Tick += (ts, te) =>
                    {
                        border.Background = Brushes.White;
                        timer.Stop();
                        txtTip.Foreground = (Brush)new BrushConverter().ConvertFrom("#BDC3C7");
                        txtTip.Text = "💡 Совет: Нажмите на любую команду в справке, чтобы скопировать её в буфер обмена";
                    };
                    timer.Start();
                }
            };

            var stack = new StackPanel();

            // Верхняя строка с названием и категорией
            var headerPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
            headerPanel.Children.Add(new TextBlock
            {
                Text = cmd.Name,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = (Brush)new BrushConverter().ConvertFrom("#2C3E50")
            });

            var categoryTag = new Border
            {
                Background = cmd.Category == "DDL" ? (Brush)new BrushConverter().ConvertFrom("#27AE60") : (Brush)new BrushConverter().ConvertFrom("#2980B9"),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(8, 2, 8, 2),
                Margin = new Thickness(10, 0, 0, 0)
            };
            categoryTag.Child = new TextBlock
            {
                Text = cmd.Category,
                Foreground = Brushes.White,
                FontSize = 10
            };
            headerPanel.Children.Add(categoryTag);

            stack.Children.Add(headerPanel);

            // Синтаксис
            stack.Children.Add(new TextBlock
            {
                Text = cmd.Syntax,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                Foreground = (Brush)new BrushConverter().ConvertFrom("#E67E22"),
                Margin = new Thickness(0, 0, 0, 5)
            });

            // Описание
            stack.Children.Add(new TextBlock
            {
                Text = cmd.Description,
                Foreground = (Brush)new BrushConverter().ConvertFrom("#7F8C8D"),
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 8),
                TextWrapping = TextWrapping.Wrap
            });

            // Пример
            var examplePanel = new Border
            {
                Background = (Brush)new BrushConverter().ConvertFrom("#F8F9FA"),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8)
            };
            examplePanel.Child = new StackPanel
            {
                Children =
                {
                    new TextBlock
                    {
                        Text = "Пример:",
                        FontWeight = FontWeights.SemiBold,
                        FontSize = 11,
                        Foreground = (Brush)new BrushConverter().ConvertFrom("#4A90D9")
                    },
                    new TextBlock
                    {
                        Text = cmd.Example,
                        FontFamily = new FontFamily("Consolas"),
                        FontSize = 11,
                        Foreground = (Brush)new BrushConverter().ConvertFrom("#2C3E50"),
                        Margin = new Thickness(0, 3, 0, 0)
                    }
                }
            };
            stack.Children.Add(examplePanel);

            border.Child = stack;
            return border;
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            string searchText = txtSearch.Text.Trim().ToLower();
            if (string.IsNullOrEmpty(searchText))
            {
                DisplayAllCommands();
                return;
            }

            stackAllCommands.Children.Clear();

            var filteredCommands = _allCommands.Where(c =>
                c.Name.ToLower().Contains(searchText) ||
                c.Description.ToLower().Contains(searchText) ||
                c.Syntax.ToLower().Contains(searchText)
            ).ToList();

            if (filteredCommands.Count == 0)
            {
                var noResult = new TextBlock
                {
                    Text = $"🔍 Ничего не найдено по запросу \"{searchText}\"",
                    FontSize = 14,
                    Foreground = (Brush)new BrushConverter().ConvertFrom("#E74C3C"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 50, 0, 0)
                };
                stackAllCommands.Children.Add(noResult);
                return;
            }

            var grouped = filteredCommands.GroupBy(c => c.Category);
            foreach (var group in grouped)
            {
                var categoryHeader = new Border
                {
                    Background = (Brush)new BrushConverter().ConvertFrom("#4A90D9"),
                    CornerRadius = new CornerRadius(5),
                    Margin = new Thickness(0, 10, 0, 5),
                    Padding = new Thickness(10, 5, 10, 5)
                };
                categoryHeader.Child = new TextBlock
                {
                    Text = group.Key == "DDL" ? "🏗️ DDL - Команды определения данных" : "📝 DML - Команды манипуляции данными",
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    FontSize = 14
                };
                stackAllCommands.Children.Add(categoryHeader);

                foreach (var cmd in group)
                {
                    var border = CreateCommandCard(cmd);
                    stackAllCommands.Children.Add(border);
                }
            }

            txtTip.Text = $"🔍 Найдено команд: {filteredCommands.Count}";
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Clear();
            DisplayAllCommands();
            txtTip.Text = "💡 Совет: Нажмите на любую команду в справке, чтобы скопировать её в буфер обмена";
        }
    }

    internal class CommandInfo
    {
        public string Name { get; set; }
        public string Syntax { get; set; }
        public string Description { get; set; }
        public string Example { get; set; }
        public string Category { get; set; }
    }
}