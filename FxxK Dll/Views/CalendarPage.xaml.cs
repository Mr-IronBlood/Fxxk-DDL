using FxxkDDL.Models;
using FxxkDDL.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Documents;

namespace FxxkDDL.Views
{
    public partial class CalendarPage : UserControl
    {
        // ============ 字段声明 ============
        private DateTime _currentDate;
        private DateTime _weekStartDate;
        private int _weekDays = 7;
        private bool _isSyncingScroll; // 防止滚动同步循环的标志
        private ScrollViewer _headerScrollViewer; // 日期头部滚动视图
        private ScrollViewer _taskScrollViewer;   // 任务区域滚动视图

        // 周视图天数属性（用于XAML绑定）
        public int WeekDaysCount
        {
            get { return _weekDays; }
        }

        // 使用属性确保CalendarService始终可用
        private CalendarService _calendarService;
        private CalendarService CalendarService
        {
            get
            {
                if (_calendarService == null)
                {
                    _calendarService = new CalendarService();
                }
                return _calendarService;
            }
        }

        // ============ 数据模型类 ============
        // 月视图日期单元格数据模型
        public class DayCellData
        {
            public int DayNumber { get; set; }
            public DateTime Date { get; set; }
            public bool IsCurrentMonth { get; set; }
            public bool IsToday { get; set; }
            public int EventCount { get; set; }
            public bool HasEvents => EventCount > 0;
            public List<DotData> Dots { get; set; } = new List<DotData>();
            public Brush TextColor => IsCurrentMonth ?
                (IsToday ? Brushes.White : Brushes.Black) :
                Brushes.Gray;
        }

        public class DotData
        {
            public Brush Color { get; set; }
            public string Importance { get; set; }
        }

        // 周视图日期列数据模型
        public class WeekDayData
        {
            public DateTime Date { get; set; }
            public string DayOfWeek { get; set; }
            public string DateString { get; set; }
            public bool IsToday { get; set; }
            public string IsTodayText => IsToday ? "今天" : "";
            public Brush DateBackground { get; set; }
            public Brush DateColor { get; set; }
            public Brush DayOfWeekColor { get; set; }
            public List<WeekTaskData> Events { get; set; } = new List<WeekTaskData>();
        }

        // 周视图任务数据模型（卡片式设计）
        public class WeekTaskData : INotifyPropertyChanged
        {
            public string EventId { get; set; }
            public string TaskDescription { get; set; }
            public DateTime Deadline { get; set; }
            public string TimeString { get; set; }
            public string Importance { get; set; }

            // 卡片样式属性
            public string CardBackgroundColor { get; set; }
            public string CardBorderBrush { get; set; }
            public double CardShadowDepth { get; set; }
            public string TaskDescriptionColor { get; set; }
            public string TimeStringColor { get; set; }
            public string ImportanceColor { get; set; }

            public CalendarEvent OriginalEvent { get; set; }

            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        // ============ 构造函数 ============
        public CalendarPage()
        {
            InitializeComponent();

            _currentDate = DateTime.Today;
            _weekStartDate = DateTime.Today;

            // 初始化月视图
            LoadMonthView();

            // 订阅Loaded事件以初始化滚动同步
            Loaded += CalendarPage_Loaded;
        }

        // ============ 查找可视化子元素 ============
        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;

                var childResult = FindVisualChild<T>(child);
                if (childResult != null)
                    return childResult;
            }

            return null;
        }

        // ============ 月视图方法 ============
        private void BtnMonthView_Click(object sender, RoutedEventArgs e)
        {
            SwitchToMonthView();
        }

        private void BtnWeekView_Click(object sender, RoutedEventArgs e)
        {
            SwitchToWeekView();
        }

        private void BtnPrev_Click(object sender, RoutedEventArgs e)
        {
            // 检查当前是否在周视图
            if (WeekViewContainer.Visibility == Visibility.Visible &&
                WeekViewContainer.IsVisible)
            {
                // 周视图时不响应月份导航
                return;
            }

            // 只有月视图时才执行
            _currentDate = _currentDate.AddMonths(-1);
            LoadMonthView();
        }

        private void BtnToday_Click(object sender, RoutedEventArgs e)
        {
            // 根据当前视图决定行为
            if (WeekViewContainer.Visibility == Visibility.Visible &&
                WeekViewContainer.IsVisible)
            {
                // 周视图：回到本周
                _weekStartDate = DateTime.Today;
                LoadWeekView();
            }
            else
            {
                // 月视图：回到本月
                _currentDate = DateTime.Today;
                LoadMonthView();
            }
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            // 检查当前是否在周视图
            if (WeekViewContainer.Visibility == Visibility.Visible &&
                WeekViewContainer.IsVisible)
            {
                // 周视图时不响应月份导航
                return;
            }

            // 只有月视图时才执行
            _currentDate = _currentDate.AddMonths(1);
            LoadMonthView();
        }
        private void SwitchToMonthView()
        {
            MonthViewContainer.Visibility = Visibility.Visible;
            WeekViewContainer.Visibility = Visibility.Collapsed;

            BtnMonthView.Background = new SolidColorBrush(Color.FromRgb(52, 152, 219));
            BtnMonthView.Foreground = Brushes.White;
            BtnMonthView.BorderThickness = new Thickness(0);

            BtnWeekView.Background = Brushes.Transparent;
            BtnWeekView.Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80));
            BtnWeekView.BorderThickness = new Thickness(1);

            LoadMonthView();
        }

        private void SwitchToWeekView()
        {
            MonthViewContainer.Visibility = Visibility.Collapsed;
            WeekViewContainer.Visibility = Visibility.Visible;

            // 更新按钮样式
            BtnWeekView.Background = new SolidColorBrush(Color.FromRgb(52, 152, 219));
            BtnWeekView.Foreground = Brushes.White;
            BtnWeekView.BorderThickness = new Thickness(0);

            BtnMonthView.Background = Brushes.Transparent;
            BtnMonthView.Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80));
            BtnMonthView.BorderThickness = new Thickness(1);

            // 更新标题为周范围
            var endDate = _weekStartDate.AddDays(_weekDays - 1);
            TxtCurrentPeriod.Text = $"{_weekStartDate:yyyy年MM月dd日} - {endDate:yyyy年MM月dd日}";

            // 确保状态栏也更新
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.StatusText.Text = "周视图 - 查看未来DDL安排";
            }

            LoadWeekView();
        }

        private void LoadMonthView()
        {
            try
            {
                TxtCurrentPeriod.Text = $"{_currentDate:yyyy年MM月}";

                var events = CalendarService.GetEventsForMonth(_currentDate.Year, _currentDate.Month);
                var days = new List<DayCellData>();

                var firstDayOfMonth = new DateTime(_currentDate.Year, _currentDate.Month, 1);
                var startDate = firstDayOfMonth.AddDays(-(int)firstDayOfMonth.DayOfWeek);

                for (int i = 0; i < 42; i++)
                {
                    var date = startDate.AddDays(i);
                    var dayData = new DayCellData
                    {
                        DayNumber = date.Day,
                        Date = date,
                        IsCurrentMonth = date.Month == _currentDate.Month,
                        IsToday = date.Date == DateTime.Today
                    };

                    var dayEvents = events.FindAll(e => e.Date.Date == date.Date);
                    dayData.EventCount = dayEvents.Count;

                    foreach (var ev in dayEvents)
                    {
                        dayData.Dots.Add(new DotData
                        {
                            Color = new SolidColorBrush(ev.GetColorByImportance()),
                            Importance = ev.Task.Importance
                        });
                    }

                    days.Add(dayData);
                }

                MonthDaysControl.ItemsSource = days;
            }
            catch (Exception ex)
            {
                // Silently handle errors
            }
        }

        private void DayCell_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                // 悬停时添加阴影效果和轻微缩放
                border.Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    Opacity = 0.15,
                    BlurRadius = 8,
                    ShadowDepth = 3
                };

                // 轻微放大
                var scaleTransform = new ScaleTransform(1.02, 1.02);
                border.RenderTransform = scaleTransform;
                border.RenderTransformOrigin = new Point(0.5, 0.5);
            }
        }

        private void DayCell_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                // 恢复默认效果
                border.Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    Opacity = 0.08,
                    BlurRadius = 6,
                    ShadowDepth = 2
                };
                border.RenderTransform = null;
            }
        }

        private void DayCell_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is DayCellData dayData)
            {
                if (dayData.HasEvents)
                {
                    ShowDayEvents(dayData.Date);
                }
                else
                {
                    MessageBox.Show($"{dayData.Date:yyyy年MM月dd日}\n\n暂无DDL安排",
                                  "日期详情",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                }
            }
        }

        private void ShowColorPicker(string taskId)
        {
            var colorWindow = new Window
            {
                Title = "选择任务颜色",
                Width = 300,
                Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                ResizeMode = ResizeMode.NoResize
            };

            var stackPanel = new StackPanel { Margin = new Thickness(20) };

            // 颜色选项
            var colors = new[]
            {
        new { Name = "🔴 高重要性", Color = Color.FromRgb(255, 59, 48), Importance = "高" },
        new { Name = "🟡 中重要性", Color = Color.FromRgb(255, 204, 0), Importance = "中" },
        new { Name = "🟢 低重要性", Color = Color.FromRgb(76, 217, 100), Importance = "低" },
        new { Name = "🔵 默认蓝色", Color = Color.FromRgb(52, 152, 219), Importance = "中" },
        new { Name = "🟣 紫色", Color = Color.FromRgb(155, 89, 182), Importance = "中" },
        new { Name = "🟠 橙色", Color = Color.FromRgb(230, 126, 34), Importance = "中" }
    };

            foreach (var colorInfo in colors)
            {
                var colorButton = new Button
                {
                    Content = colorInfo.Name,
                    Height = 35,
                    Margin = new Thickness(0, 0, 0, 8),
                    Background = new SolidColorBrush(colorInfo.Color),
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.SemiBold,
                    BorderThickness = new Thickness(0)
                };

                colorButton.Click += (s, e) =>
                {
                    var taskService = new TaskService();

                    if (colorInfo.Importance == "高" || colorInfo.Importance == "中" || colorInfo.Importance == "低")
                    {
                        // 更新重要性
                        if (taskService.UpdateImportance(taskId, colorInfo.Importance))
                        {
                            MessageBox.Show($"任务颜色已更新为{colorInfo.Name}", "操作成功",
                                MessageBoxButton.OK, MessageBoxImage.Information);

                            // 立即刷新当前视图
                            if (MonthViewContainer.Visibility == Visibility.Visible)
                            {
                                LoadMonthView();
                            }
                            else if (WeekViewContainer.Visibility == Visibility.Visible)
                            {
                                LoadWeekView();
                            }

                            colorWindow.Close();
                        }
                    }
                    else
                    {
                        // 自定义颜色
                        string colorHex = $"#{colorInfo.Color.R:X2}{colorInfo.Color.G:X2}{colorInfo.Color.B:X2}";
                        if (taskService.SetCustomColor(taskId, colorHex))
                        {
                            MessageBox.Show($"任务颜色已更新为{colorInfo.Name}", "操作成功",
                                MessageBoxButton.OK, MessageBoxImage.Information);

                            // 立即刷新当前视图
                            if (MonthViewContainer.Visibility == Visibility.Visible)
                            {
                                LoadMonthView();
                            }
                            else if (WeekViewContainer.Visibility == Visibility.Visible)
                            {
                                LoadWeekView();
                            }

                            colorWindow.Close();
                        }
                    }
                };

                stackPanel.Children.Add(colorButton);
            }

            // 重置按钮
            var resetButton = new Button
            {
                Content = "🔄 重置为默认（基于重要性）",
                Height = 35,
                Margin = new Thickness(0, 10, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(149, 165, 166)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold
            };

            resetButton.Click += (s, e) =>
            {
                var taskService = new TaskService();
                taskService.ResetToDefaultColor(taskId);

                MessageBox.Show("已重置为默认颜色", "操作成功",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                if (MonthViewContainer.Visibility == Visibility.Visible)
                    LoadMonthView();
                else
                    LoadWeekView();

                colorWindow.Close();
            };

            stackPanel.Children.Add(resetButton);
            colorWindow.Content = stackPanel;
            colorWindow.ShowDialog();
        }

        private void ShowDayEvents(DateTime date)
        {
            var events = CalendarService.GetEventsForMonth(date.Year, date.Month)
                .FindAll(e => e.Date.Date == date.Date);

            if (events.Count > 0)
            {
                // 按重要性分组并排序（高->中->低）
                var importanceOrder = new Dictionary<string, int> { { "高", 0 }, { "中", 1 }, { "低", 2 } };
                var sortedEvents = events.OrderBy(e =>
                {
                    var imp = e.Task.Importance ?? "中";
                    return importanceOrder.ContainsKey(imp) ? importanceOrder[imp] : 3;
                }).ThenBy(e => e.StartTime).ToList();

                // 创建自定义窗口显示任务详情
                var dialog = new Window
                {
                    Title = $"📅 {date:yyyy年MM月dd日} 的DDL任务",
                    Width = 700,
                    Height = 550,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Window.GetWindow(this),
                    ResizeMode = ResizeMode.CanResizeWithGrip,
                    Background = new SolidColorBrush(Color.FromRgb(248, 249, 250))
                };

                // 滚动视图容器
                var scrollViewer = new ScrollViewer
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    Padding = new Thickness(15)
                };

                var mainStackPanel = new StackPanel();

                // 按重要性分组显示
                var currentImportance = "";
                StackPanel currentGroupPanel = null;

                foreach (var eventItem in sortedEvents)
                {
                    var task = eventItem.Task;
                    var importance = task.Importance ?? "中";

                    // 如果重要性改变，创建新的分组
                    if (importance != currentImportance)
                    {
                        currentImportance = importance;

                        // 分组标题
                        var groupHeader = new Border
                        {
                            Background = importance switch
                            {
                                "高" => new SolidColorBrush(Color.FromRgb(231, 76, 60)),
                                "中" => new SolidColorBrush(Color.FromRgb(241, 196, 15)),
                                "低" => new SolidColorBrush(Color.FromRgb(46, 204, 113)),
                                _ => new SolidColorBrush(Color.FromRgb(52, 152, 219))
                            },
                            CornerRadius = new CornerRadius(5),
                            Padding = new Thickness(12, 8, 12, 8),
                            Margin = new Thickness(0, 10, 0, 10)
                        };

                        var headerText = new TextBlock
                        {
                            Text = $"❗ {importance}重要度",
                            FontSize = 16,
                            FontWeight = FontWeights.Bold,
                            Foreground = Brushes.White
                        };
                        groupHeader.Child = headerText;
                        mainStackPanel.Children.Add(groupHeader);

                        // 创建分组面板
                        currentGroupPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 15) };
                        mainStackPanel.Children.Add(currentGroupPanel);
                    }

                    // 任务卡片
                    var taskCard = CreateTaskCard(eventItem, dialog);
                    currentGroupPanel.Children.Add(taskCard);
                }

                // 底部关闭按钮
                var closeButton = new Button
                {
                    Content = "关闭",
                    Width = 120,
                    Height = 35,
                    Margin = new Thickness(0, 10, 0, 0),
                    Background = new SolidColorBrush(Color.FromRgb(52, 152, 219)),
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.SemiBold,
                    Cursor = Cursors.Hand,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                closeButton.Click += (s, e) => dialog.Close();
                mainStackPanel.Children.Add(closeButton);

                scrollViewer.Content = mainStackPanel;
                dialog.Content = scrollViewer;
                dialog.ShowDialog();
            }
            else
            {
                MessageBox.Show($"{date:yyyy年MM月dd日}\n\n暂无DDL安排",
                              "日期详情",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// 创建任务卡片
        /// </summary>
        private Border CreateTaskCard(CalendarEvent eventItem, Window dialog)
        {
            var task = eventItem.Task;
            var displayColor = eventItem.EventColor;

            var card = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 10),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Gray,
                    Direction = 270,
                    ShadowDepth = 2,
                    BlurRadius = 10,
                    Opacity = 0.2
                }
            };

            var cardPanel = new StackPanel();

            // 第一行：任务名称和状态
            var firstRow = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            firstRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            firstRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var taskName = new TextBlock
            {
                Text = !string.IsNullOrWhiteSpace(task.TaskName) ? task.TaskName : task.Description,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80)),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetColumn(taskName, 0);
            firstRow.Children.Add(taskName);

            var statusBadge = new Border
            {
                Background = task.IsCompleted
                    ? new SolidColorBrush(Color.FromRgb(46, 204, 113))
                    : new SolidColorBrush(Color.FromRgb(241, 196, 15)),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(10, 0, 0, 0)
            };
            var statusText = new TextBlock
            {
                Text = task.IsCompleted ? "✅已完成" : "⏳待完成",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            };
            statusBadge.Child = statusText;
            Grid.SetColumn(statusBadge, 1);
            firstRow.Children.Add(statusBadge);

            cardPanel.Children.Add(firstRow);

            // 第二行：截止时间
            var timeRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
            timeRow.Children.Add(new TextBlock
            {
                Text = "⏰ ",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(52, 152, 219))
            });
            timeRow.Children.Add(new TextBlock
            {
                Text = eventItem.StartTime.ToString("yyyy-MM-dd HH:mm"),
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(127, 140, 141))
            });
            cardPanel.Children.Add(timeRow);

            // 第三行：任务详情（如果有）
            var taskDetail = !string.IsNullOrWhiteSpace(task.TaskDetail) ? task.TaskDetail : task.OriginalContext;
            if (!string.IsNullOrWhiteSpace(taskDetail))
            {
                var detailText = new TextBlock
                {
                    Text = taskDetail.Length > 100 ? taskDetail.Substring(0, 100) + "..." : taskDetail,
                    FontSize = 13,
                    Foreground = new SolidColorBrush(Color.FromRgb(127, 140, 141)),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                cardPanel.Children.Add(detailText);
            }

            // 第四行：操作按钮
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            // 查看详情按钮
            var detailButton = CreateActionButton("📄 详情", Color.FromRgb(52, 152, 219));
            detailButton.Click += (s, e) =>
            {
                dialog.Hide(); // 先隐藏月视图详情对话框
                var detailDialog = new TaskDetailDialog(task.Id) { Owner = Window.GetWindow(this) };

                // 订阅编辑任务事件
                detailDialog.OnEditTask += (taskId) =>
                {
                    var taskService = new TaskService();
                    var taskToEdit = taskService.GetTask(taskId);
                    if (taskToEdit != null)
                    {
                        ShowEditDialog(taskToEdit);
                        LoadMonthView();
                    }
                };

                // 订阅删除任务事件
                detailDialog.OnDeleteTask += (taskId) =>
                {
                    LoadMonthView();
                    dialog.Close();
                };

                detailDialog.ShowDialog();
                LoadMonthView();
                dialog.Close(); // 详情查看完成后关闭
            };
            buttonPanel.Children.Add(detailButton);

            // 标记完成/取消完成按钮
            var toggleButton = CreateActionButton(
                task.IsCompleted ? "↩️ 恢复" : "✅ 完成",
                task.IsCompleted ? Color.FromRgb(149, 165, 166) : Color.FromRgb(46, 204, 113));
            toggleButton.Click += (s, e) =>
            {
                var taskService = new TaskService();
                if (taskService.MarkAsCompleted(task.Id, !task.IsCompleted))
                {
                    LoadMonthView();
                    dialog.Close();
                }
            };
            buttonPanel.Children.Add(toggleButton);

            // 编辑按钮
            var editButton = CreateActionButton("✏️ 编辑", Color.FromRgb(241, 196, 15));
            editButton.Click += (s, e) =>
            {
                dialog.Hide(); // 先隐藏月视图详情对话框
                ShowEditDialog(task);
                LoadMonthView();
                dialog.Close(); // 编辑完成后关闭
            };
            buttonPanel.Children.Add(editButton);

            // 删除按钮
            var deleteButton = CreateActionButton("🗑️ 删除", Color.FromRgb(231, 76, 60));
            deleteButton.Click += (s, e) =>
            {
                var result = MessageBox.Show("确定要删除这个任务吗？", "确认删除",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    var taskService = new TaskService();
                    if (taskService.DeleteTask(task.Id))
                    {
                        LoadMonthView();
                        dialog.Close();
                    }
                }
            };
            buttonPanel.Children.Add(deleteButton);

            cardPanel.Children.Add(buttonPanel);
            card.Child = cardPanel;

            return card;
        }

        /// <summary>
        /// 创建操作按钮
        /// </summary>
        private Button CreateActionButton(string content, Color color)
        {
            return new Button
            {
                Content = content,
                Width = 70,
                Height = 28,
                Margin = new Thickness(5, 0, 0, 0),
                Padding = new Thickness(0),
                Background = new SolidColorBrush(color),
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
        }


        private string GetColorName(Color color)
        {
            if (color.R == 231 && color.G == 76 && color.B == 60) return "红色(高)";
            if (color.R == 241 && color.G == 196 && color.B == 15) return "黄色(中)";
            if (color.R == 46 && color.G == 204 && color.B == 113) return "绿色(低)";
            return "蓝色(默认)";
        }

        // ============ 周视图方法 ============
        private void LoadWeekView()
        {
            try
            {
                // 清除旧数据和拖尾效果
                WeekHeaderControl.ItemsSource = null;
                TaskTrailCanvas.Children.Clear();
                TaskInteractionCanvas.Children.Clear();

                var endDate = _weekStartDate.AddDays(_weekDays - 1);
                var events = CalendarService.GetEventsForWeek(_weekStartDate, _weekDays);

                // 创建日期头部数据
                var weekHeaderData = new List<WeekDayData>();
                for (int i = 0; i < _weekDays; i++)
                {
                    var date = _weekStartDate.AddDays(i);
                    var isToday = date.Date == DateTime.Today;
                    var isWeekend = date.DayOfWeek == DayOfWeek.Sunday || date.DayOfWeek == DayOfWeek.Saturday;

                    weekHeaderData.Add(new WeekDayData
                    {
                        Date = date,
                        DayOfWeek = GetChineseDayOfWeek(date.DayOfWeek),
                        DateString = date.ToString("MM月dd日"),
                        IsToday = isToday,
                        DateBackground = isToday ? new SolidColorBrush(Color.FromRgb(52, 152, 219)) : new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                        DateColor = isToday ? Brushes.White : (isWeekend ? Brushes.Red : Brushes.Black),
                        DayOfWeekColor = isWeekend ? Brushes.Red : Brushes.Black
                    });
                }

                WeekHeaderControl.ItemsSource = weekHeaderData;
                // 设置ItemsControl宽度以确保所有日期项都能显示
                WeekHeaderControl.Width = _weekDays * 120.0;
                // 设置任务区域Grid宽度以匹配日期头部总宽度
                if (TaskTrailGrid != null)
                {
                    TaskTrailGrid.Width = _weekDays * 120.0;
                    TaskTrailGrid.HorizontalAlignment = HorizontalAlignment.Left;
                }
                WeekHeaderControl.UpdateLayout(); // 强制更新布局，确保日期头部正确显示

                // 找到今天在周中的索引
                int todayIndex = -1;
                for (int i = 0; i < _weekDays; i++)
                {
                    if (_weekStartDate.AddDays(i).Date == DateTime.Today.Date)
                    {
                        todayIndex = i;
                        break;
                    }
                }

                // 收集所有任务并按你的要求排序
                var allTasks = new List<TaskTrailInfo>();
                for (int i = 0; i < _weekDays; i++)
                {
                    var date = _weekStartDate.AddDays(i);
                    var dayEvents = events.FindAll(e => e.Date.Date == date.Date);

                    foreach (var ev in dayEvents)
                    {
                        allTasks.Add(new TaskTrailInfo
                        {
                            Task = ev.Task,
                            Event = ev,
                            DayIndex = i,
                            TaskDate = ev.Date,  // 使用事件的日期（任务截止日期）而不是列的日期
                            TodayIndex = todayIndex
                        });
                    }
                }

                // 按你的要求排序：优先按重要性（高>中>低），同重要性按日期先后（早的在上）
                var sortedTasks = allTasks
                    .Where(t => t.Task != null) // 确保任务存在
                    .OrderByDescending(t => GetImportanceLevel(t.Task.Importance)) // 重要性优先
                    .ThenBy(t => t.TaskDate) // 同重要性按日期早晚
                    .ThenBy(t => t.Task.Description) // 相同日期按名称排序（稳定排序）
                    .ToList();

                // 等待布局完成后绘制拖尾效果
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    DrawTaskTrails(sortedTasks);
                    SetupScrollSync(); // 重新设置滚动同步
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
            catch (Exception ex)
            {
                // Silently handle errors
            }
        }

        // 任务拖尾信息类
        public class TaskTrailInfo
        {
            public DDLTask Task { get; set; }
            public CalendarEvent Event { get; set; }
            public int DayIndex { get; set; }
            public DateTime TaskDate { get; set; }
            public int TodayIndex { get; set; }
        }

        // 获取重要性等级（用于排序）
        private int GetImportanceLevel(string importance)
        {
            return importance switch
            {
                "高" => 3,
                "中" => 2,
                "低" => 1,
                _ => 0
            };
        }

        // 绘制任务拖尾效果
        private void DrawTaskTrails(List<TaskTrailInfo> sortedTasks)
        {
            try
            {
                TaskTrailCanvas.Children.Clear();
                TaskInteractionCanvas.Children.Clear();

                // 绘制日期分界线浅灰色细线
                double columnWidth = 120.0;
                for (int i = 0; i <= _weekDays; i++)
                {
                    double lineX = i * columnWidth;
                    var line = new System.Windows.Shapes.Line
                    {
                        X1 = lineX,
                        Y1 = 0,
                        X2 = lineX,
                        Y2 = 1000, // 足够大的高度
                        Stroke = Brushes.LightGray,
                        StrokeThickness = 0.5,
                        StrokeDashArray = null
                    };
                    // 设置ZIndex较低，确保在背景层
                    Canvas.SetZIndex(line, -1);
                    TaskTrailCanvas.Children.Add(line);
                }

                // 设置Canvas宽度以匹配日期头部总宽度
                TaskTrailCanvas.Width = _weekDays * columnWidth;
                TaskInteractionCanvas.Width = _weekDays * columnWidth;

                // 列宽固定为120px，与日期头部项宽度一致
                const double trailHeight = 90;   // 拖尾条带的高度
                const double verticalSpacing = 90; // 垂直间距改为与拖尾高度相同，消除间隙
                const double minVerticalGap = 0; // 最小垂直间隙设为0，紧密排列

                // 任务框尺寸常量（与DrawTaskBox方法保持一致）
                const double boxWidth = 120; // 列宽
                const double borderWidth = 10; // 边框宽度
                const double borderLeftOffset = boxWidth - borderWidth; // 边框左侧在任务框容器内的偏移量

                // 用于跟踪每列已占用的垂直位置范围
                var columnOccupancy = new Dictionary<int, List<(double y, double height)>>();

                for (int taskIndex = 0; taskIndex < sortedTasks.Count; taskIndex++)
                {
                    var taskInfo = sortedTasks[taskIndex];
                    var task = taskInfo.Task;

                    // 获取重要性对应的颜色（使用与其他页面一致的通用颜色）
                    Color trailColor = task.Importance switch
                    {
                        "高" => Color.FromRgb(231, 76, 60),     // #E74C3C 红色
                        "中" => Color.FromRgb(241, 196, 15),    // #F1C40F 黄色（修正为通用颜色）
                        "低" => Color.FromRgb(46, 204, 113),    // #2ECC71 绿色（修正为通用颜色）
                        _ => Color.FromRgb(149, 165, 166)       // #95A5A6 默认灰色
                    };

                    // 任务所在日期的X坐标
                    double taskColumnX = taskInfo.DayIndex * columnWidth;

                    // 今天的X坐标
                    double todayColumnX = taskInfo.TodayIndex >= 0 ? taskInfo.TodayIndex * columnWidth : -1;

                    // 计算初始垂直位置
                    double yPosition = 10 + taskIndex * verticalSpacing;

                    // 检查拖尾条带涉及的所有列是否重叠
                    if (taskInfo.TodayIndex >= 0 && taskInfo.DayIndex != taskInfo.TodayIndex)
                    {
                        int startCol = Math.Min(taskInfo.DayIndex, taskInfo.TodayIndex);
                        int endCol = Math.Max(taskInfo.DayIndex, taskInfo.TodayIndex);

                        // 尝试寻找不重叠的位置
                        bool positionFound = false;
                        int maxAttempts = 20; // 最多尝试20次
                        int attempt = 0;

                        while (!positionFound && attempt < maxAttempts)
                        {
                            positionFound = true;

                            // 检查所有涉及的列
                            for (int col = startCol; col <= endCol; col++)
                            {
                                if (columnOccupancy.ContainsKey(col))
                                {
                                    foreach (var (occupiedY, occupiedHeight) in columnOccupancy[col])
                                    {
                                        // 检查是否重叠
                                        if (!(yPosition + trailHeight + minVerticalGap <= occupiedY ||
                                              yPosition >= occupiedY + occupiedHeight + minVerticalGap))
                                        {
                                            // 重叠，需要调整位置
                                            positionFound = false;
                                            yPosition = Math.Max(yPosition, occupiedY + occupiedHeight + minVerticalGap);
                                            break;
                                        }
                                    }
                                    if (!positionFound) break;
                                }
                            }

                            if (!positionFound)
                            {
                                attempt++;
                                // 如果仍然找不到，增加y位置
                                yPosition += verticalSpacing;
                            }
                        }
                    }

                    // 记录这个任务占用的列
                    if (taskInfo.TodayIndex >= 0 && taskInfo.DayIndex != taskInfo.TodayIndex)
                    {
                        int startCol = Math.Min(taskInfo.DayIndex, taskInfo.TodayIndex);
                        int endCol = Math.Max(taskInfo.DayIndex, taskInfo.TodayIndex);

                        for (int col = startCol; col <= endCol; col++)
                        {
                            if (!columnOccupancy.ContainsKey(col))
                                columnOccupancy[col] = new List<(double y, double height)>();

                            columnOccupancy[col].Add((yPosition, trailHeight));
                        }
                    }
                    else
                    {
                        // 如果没有拖尾（任务就在今天），只占用任务列
                        int col = taskInfo.DayIndex;
                        if (!columnOccupancy.ContainsKey(col))
                            columnOccupancy[col] = new List<(double y, double height)>();

                        columnOccupancy[col].Add((yPosition, trailHeight));
                    }

                    // 绘制拖尾条带
                    // 无论任务在哪一天，都绘制拖尾，拖尾始终向左延伸到 X=0（第0列的左边界）
                    bool shouldDrawTrail = true;

                    if (shouldDrawTrail)
                    {
                        // 计算边框右侧位置，使拖尾与边框右侧竖线连接
                        double taskBorderRightX = taskColumnX + boxWidth;

                        // 拖尾目标始终是 X=0（第0列的左边界）
                        double trailTargetX = 0;

                        DrawTrailStrip(taskBorderRightX, trailTargetX, yPosition, trailHeight, trailColor, false, taskInfo, taskColumnX);
                    }

                    // 绘制任务所在日期的圆角矩形
                    DrawTaskBox(taskColumnX, yPosition, trailHeight, trailColor, task, taskInfo);
                }
            }
            catch (Exception ex)
            {
                // Silently handle errors
            }
        }

        // 绘制荧光拖尾条带
        // taskX: 任务竖线的右边界X坐标
        // todayX: 目标位置X坐标
        // taskColumnX: 任务列的左边界X坐标（用于文字定位）
        private void DrawTrailStrip(double taskX, double todayX, double y, double height, Color color, bool isFuture, TaskTrailInfo taskInfo = null, double taskColumnX = 0)
        {
            const double borderWidth = 24; // 竖线宽度
            double cornerRadius = height / 4; // 圆角半径

            // 计算拖尾位置：taskX和todayX已经是右边界坐标
            double startX, endX;
            double trailWidth;

            if (isFuture)
            {
                // 未来任务：从今天向右延伸到任务
                startX = todayX;  // 今天列的右边界（已经是正确的）
                endX = taskX;    // 任务竖线的右边界（已经是正确的）
            }
            else
            {
                // 过去任务：从任务向右延伸到今天
                startX = taskX;    // 任务竖线的右边界（已经是正确的）
                endX = todayX;    // 今天列的右边界（已经是正确的）
            }

            trailWidth = Math.Abs(endX - startX);

            if (trailWidth <= 0) return; // 无需绘制拖尾

            // 创建荧光渐变画刷：从任务端（不透明）到今天端（透明）
            var gradientBrush = new LinearGradientBrush();
            gradientBrush.StartPoint = new Point(0, 0.5);
            gradientBrush.EndPoint = new Point(1, 0.5);

            if (isFuture)
            {
                // 未来任务：从今天（透明，Alpha=0）→ 任务（不透明，Alpha=255）
                gradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb((byte)0, color.R, color.G, color.B), 0.0));   // 今天端：完全透明
                gradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb((byte)100, color.R, color.G, color.B), 0.5)); // 中间：半透明
                gradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb((byte)255, color.R, color.G, color.B), 1.0));   // 任务端：完全不透明
            }
            else
            {
                // 过去任务：从左侧（透明，Alpha=0）→ 任务（不透明，Alpha=255）
                gradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb((byte)0, color.R, color.G, color.B), 0.0));     // 左侧（最左端）：完全透明
                gradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb((byte)100, color.R, color.G, color.B), 0.5)); // 中间：半透明
                gradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb((byte)255, color.R, color.G, color.B), 1.0));   // 右侧（任务端）：完全不透明
            }

            // 创建拖尾Border - 只有右侧圆角（任务端），左侧是平的
            var trailBorder = new Border
            {
                Width = trailWidth,
                Height = height,
                Background = gradientBrush,
                CornerRadius = new CornerRadius(0, cornerRadius * 0.6, cornerRadius * 0.6, 0), // 减小圆角弧度
                Cursor = Cursors.Hand,
                Tag = taskInfo,
                Opacity = 1.0
            };

            // 添加荧光发光效果
            trailBorder.Effect = new DropShadowEffect
            {
                Color = color,
                Opacity = 0.4,
                BlurRadius = 8,
                ShadowDepth = 0
            };

            Canvas.SetLeft(trailBorder, Math.Min(startX, endX));
            Canvas.SetTop(trailBorder, y);
            TaskTrailCanvas.Children.Add(trailBorder);

            // 添加拖尾的交互事件（整个拖尾都可点击）
            trailBorder.MouseLeftButtonUp += (s, e) =>
            {
                if (taskInfo != null)
                {
                    ShowTaskDetails(taskInfo.Task);
                }
            };

            trailBorder.MouseEnter += (s, e) =>
            {
                if (taskInfo != null)
                {
                    // 将拖尾置顶
                    Canvas.SetZIndex(trailBorder, 1000);

                    // 拖尾悬停变大效果，以任务端（右侧）为中心
                    var scaleTransform = new ScaleTransform(1.05, 1.05);
                    trailBorder.RenderTransform = scaleTransform;

                    // 根据拖尾方向设置变换原点（始终以任务端/右侧为缩放中心）
            trailBorder.RenderTransformOrigin = new Point(1, 0.5);

                    HighlightTaskBorder(taskInfo, true);
                }
            };

            trailBorder.MouseLeave += (s, e) =>
            {
                if (taskInfo != null)
                {
                    // 恢复拖尾ZIndex
                    Canvas.SetZIndex(trailBorder, 0);

                    // 恢复原始大小
                    trailBorder.RenderTransform = null;

                    HighlightTaskBorder(taskInfo, false);
                }
            };

            // 在任务所在日期端显示任务文字、提交时间和勾选框
            if (taskInfo != null)
            {
                var task = taskInfo.Task;
                bool isCompleted = task.IsCompleted;

                // 创建容器StackPanel来垂直排列文字、时间和勾选框
                var container = new StackPanel
                {
                    Orientation = Orientation.Vertical
                };

                // 第一行：任务名称文字
                var taskText = new TextBlock
                {
                    Text = task.Description,
                    Foreground = Brushes.White,
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Effect = new DropShadowEffect
                    {
                        ShadowDepth = 0,
                        Color = Colors.Black,
                        Opacity = 1,
                        BlurRadius = 2
                    }
                };

                // 第二行：时间和勾选框的容器
                var timeCheckBoxContainer = new StackPanel
                {
                    Orientation = Orientation.Horizontal
                };

                // 提交时间文字
                var deadlineText = new TextBlock
                {
                    Text = task.Deadline.HasValue ? task.Deadline.Value.ToString("MM-dd HH:mm") : "",
                    Foreground = Brushes.White,
                    FontSize = 11,
                    FontWeight = FontWeights.Normal,
                    Effect = new DropShadowEffect
                    {
                        ShadowDepth = 0,
                        Color = Colors.Black,
                        Opacity = 1,
                        BlurRadius = 2
                    }
                };

                // 勾选框
                var checkBox = new CheckBox
                {
                    IsChecked = isCompleted,
                    Margin = new Thickness(5, 0, 0, 0),
                    Tag = taskInfo,
                    Cursor = Cursors.Hand
                };

                // 勾选框点击事件
                checkBox.Click += (s, e) =>
                {
                    if (checkBox.Tag is TaskTrailInfo info)
                    {
                        var taskService = new TaskService();
                        bool newState = checkBox.IsChecked ?? false;

                        if (taskService.MarkAsCompleted(info.Task.Id, newState))
                        {
                            LoadWeekView(); // 重新加载视图
                        }
                    }
                };

                // 添加到第二行容器
                timeCheckBoxContainer.Children.Add(deadlineText);
                timeCheckBoxContainer.Children.Add(checkBox);

                // 添加到主容器
                container.Children.Add(taskText);
                container.Children.Add(timeCheckBoxContainer);

                // 计算容器位置（文字显示在任务列内，靠近左边界）
                double textX = taskColumnX + 5;

                Canvas.SetLeft(container, textX);
                Canvas.SetTop(container, y + height / 2 - 15);

                // 设置初始ZIndex
                Canvas.SetZIndex(container, 0);

                // 为容器添加鼠标事件，同步置顶效果
                container.MouseEnter += (s, e) =>
                {
                    // 置顶容器
                    Canvas.SetZIndex(container, 1002);
                    // 同时高亮拖尾和竖线
                    HighlightTaskBorder(taskInfo, true);
                };

                container.MouseLeave += (s, e) =>
                {
                    // 恢复容器ZIndex
                    Canvas.SetZIndex(container, 0);
                    // 取消高亮
                    HighlightTaskBorder(taskInfo, false);
                };

                TaskInteractionCanvas.Children.Add(container);
            }
        }

        // 绘制任务圆角矩形（右括号形状）
        private void DrawTaskBox(double x, double y, double height, Color color, DDLTask task, TaskTrailInfo taskInfo)
        {
            const double boxWidth = 120; // 列宽
            const double borderWidth = 24; // 边框宽度（增大以确保圆角完整显示）
            double cornerRadius = height / 4 * 0.6; // 减小圆角半径

            // 创建带圆角的右边框（透明背景，只显示边框）
            var rightBorder = new Border
            {
                Width = borderWidth,
                Height = height,
                Background = Brushes.Transparent,
                BorderBrush = new SolidColorBrush(Color.FromArgb((byte)255, color.R, color.G, color.B)),
                BorderThickness = new Thickness(0, 2, 3, 2), // 上、右、下边框
                CornerRadius = new CornerRadius(0, cornerRadius, cornerRadius, 0), // 右侧圆角
                Cursor = Cursors.Hand,
                Tag = taskInfo
            };

            // 添加荧光发光效果
            rightBorder.Effect = new DropShadowEffect
            {
                Color = color,
                Opacity = 0.8,
                BlurRadius = 10,
                ShadowDepth = 0
            };

            // 设置位置：右侧对齐到列边界
            Canvas.SetLeft(rightBorder, x + boxWidth - borderWidth);
            Canvas.SetTop(rightBorder, y);
            TaskInteractionCanvas.Children.Add(rightBorder);

            // 添加交互事件
            rightBorder.MouseLeftButtonUp += TaskBox_Click;
            rightBorder.MouseEnter += TaskBox_MouseEnter;
            rightBorder.MouseLeave += TaskBox_MouseLeave;
        }

        // 高亮任务边框（竖线）
        private void HighlightTaskBorder(TaskTrailInfo taskInfo, bool highlight)
        {
            if (taskInfo == null) return;

            // 在TaskInteractionCanvas中查找对应的竖线Border
            foreach (var child in TaskInteractionCanvas.Children)
            {
                if (child is Border border && border.Tag is TaskTrailInfo borderInfo &&
                    borderInfo == taskInfo)
                {
                    // 设置高亮效果
                    var task = taskInfo.Task;
                    Color color = task.Importance switch
                    {
                        "高" => Color.FromRgb(231, 76, 60),     // #E74C3C 红色
                        "中" => Color.FromRgb(241, 196, 15),    // #F1C40F 黄色（通用颜色）
                        "低" => Color.FromRgb(46, 204, 113),    // #2ECC71 绿色（通用颜色）
                        _ => Color.FromRgb(149, 165, 166)       // #95A5A6 默认灰色
                    };

                    if (highlight)
                    {
                        // 将竖线置顶
                        Canvas.SetZIndex(border, 1001);

                        border.Effect = new DropShadowEffect
                        {
                            Color = color,
                            Opacity = 1.0,
                            BlurRadius = 15,
                            ShadowDepth = 0
                        };

                        // 轻微放大效果
                        var scaleTransform = new ScaleTransform(1.1, 1.1);
                        border.RenderTransform = scaleTransform;
                        border.RenderTransformOrigin = new Point(0.5, 0.5);
                    }
                    else
                    {
                        // 恢复竖线ZIndex
                        Canvas.SetZIndex(border, 0);

                        // 恢复原始效果
                        border.Effect = new DropShadowEffect
                        {
                            Color = color,
                            Opacity = 0.8,
                            BlurRadius = 10,
                            ShadowDepth = 0
                        };
                        border.RenderTransform = null;
                    }
                    return;
                }
            }
        }

        // 高亮任务拖尾
        private void HighlightTaskTrail(TaskTrailInfo taskInfo, bool highlight)
        {
            if (taskInfo == null) return;

            // 在TaskTrailCanvas中查找对应的拖尾Border
            foreach (var child in TaskTrailCanvas.Children)
            {
                if (child is Border trailBorder && trailBorder.Tag is TaskTrailInfo trailInfo &&
                    trailInfo == taskInfo)
                {
                    if (highlight)
                    {
                        // 将拖尾置顶
                        Canvas.SetZIndex(trailBorder, 1000);

                        // 拖尾悬停变大效果
                        var scaleTransform = new ScaleTransform(1.05, 1.05);
                        trailBorder.RenderTransform = scaleTransform;
                        trailBorder.RenderTransformOrigin = new Point(1, 0.5);
                    }
                    else
                    {
                        // 恢复拖尾ZIndex
                        Canvas.SetZIndex(trailBorder, 0);

                        // 恢复原始大小
                        trailBorder.RenderTransform = null;
                    }
                    return;
                }
            }
        }

        // 任务框鼠标进入事件
        private void TaskBox_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border taskBox)
            {
                // 增强荧光发光效果
                if (taskBox.Tag is TaskTrailInfo taskInfo)
                {
                    var task = taskInfo.Task;
                    Color color = task.Importance switch
                    {
                        "高" => Color.FromRgb(231, 76, 60),     // #E74C3C 红色
                        "中" => Color.FromRgb(241, 196, 15),    // #F1C40F 黄色（通用颜色）
                        "低" => Color.FromRgb(46, 204, 113),    // #2ECC71 绿色（通用颜色）
                        _ => Color.FromRgb(149, 165, 166)       // #95A5A6 默认灰色
                    };

                    taskBox.Effect = new DropShadowEffect
                    {
                        Color = color,
                        Opacity = 1.0,
                        BlurRadius = 15,
                        ShadowDepth = 0,
                        Direction = 0
                    };

                    // 同时高亮拖尾
                    HighlightTaskTrail(taskInfo, true);
                }

                // 轻微放大效果
                var scaleTransform = new ScaleTransform(1.1, 1.1);
                taskBox.RenderTransform = scaleTransform;
                taskBox.RenderTransformOrigin = new Point(0.5, 0.5);
            }
        }

        // 任务框鼠标离开事件
        private void TaskBox_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border taskBox && taskBox.Tag is TaskTrailInfo taskInfo)
            {
                // 恢复荧光发光效果
                var task = taskInfo.Task;
                Color color = task.Importance switch
                {
                    "高" => Color.FromRgb(231, 76, 60),     // #E74C3C 红色
                    "中" => Color.FromRgb(241, 196, 15),    // #F1C40F 黄色（通用颜色）
                    "低" => Color.FromRgb(46, 204, 113),    // #2ECC71 绿色（通用颜色）
                    _ => Color.FromRgb(149, 165, 166)       // #95A5A6 默认灰色
                };

                taskBox.Effect = new DropShadowEffect
                {
                    Color = color,
                    Opacity = 0.8,
                    BlurRadius = 10,
                    ShadowDepth = 0,
                    Direction = 0
                };

                // 同时取消高亮拖尾
                HighlightTaskTrail(taskInfo, false);

                // 恢复原始大小
                taskBox.RenderTransform = null;
            }
        }

        // 任务框点击事件
        private void TaskBox_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border taskBox && taskBox.Tag is TaskTrailInfo taskInfo)
            {
                try
                {
                    ShowTaskDetails(taskInfo.Task);
                }
                catch (Exception ex)
                {
                    // Silently handle errors
                }
            }
        }

        // 显示任务详情
        private void ShowTaskDetails(DDLTask task)
        {
            try
            {
                var detailDialog = new Views.TaskDetailDialog(task.Id);
                detailDialog.Owner = Window.GetWindow(this);

                // 订阅编辑任务事件
                detailDialog.OnEditTask += (taskId) =>
                {
                    var taskService = new TaskService();
                    var taskToEdit = taskService.GetTask(taskId);
                    if (taskToEdit != null)
                    {
                        ShowEditDialog(taskToEdit);
                    }
                };

                // 订阅管理关系事件
                detailDialog.OnManageRelations += (taskId) =>
                {
                    var relationDialog = new TaskRelationshipDialog(taskId)
                    {
                        Owner = Window.GetWindow(this)
                    };
                    relationDialog.ShowDialog();
                };

                // 订阅删除任务事件
                detailDialog.OnDeleteTask += (taskId) =>
                {
                    LoadWeekView();
                };

                // 监听任务变化
                var originalImportance = task.Importance;
                var originalCompleted = task.IsCompleted;

                var result = detailDialog.ShowDialog();

                // 如果重要性或完成状态发生变化，重新加载视图
                if (task.Importance != originalImportance || task.IsCompleted != originalCompleted)
                {
                    LoadWeekView();
                }
            }
            catch (Exception ex)
            {
                // Silently handle errors
            }
        }

        /// <summary>
        /// 显示编辑对话框
        /// </summary>
        private void ShowEditDialog(DDLTask task)
        {
            var editWindow = new Window
            {
                Title = "编辑任务",
                Width = 500,
                Height = 650,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                ResizeMode = ResizeMode.CanResize,
                Background = new SolidColorBrush(Color.FromRgb(248, 249, 250))
            };

            // 创建滚动容器
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(0)
            };

            var stackPanel = new StackPanel { Margin = new Thickness(20) };

            // 任务名称
            stackPanel.Children.Add(new TextBlock
            {
                Text = "任务名称:",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 5),
                Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80))
            });

            var taskNameBox = new TextBox
            {
                Text = !string.IsNullOrWhiteSpace(task.TaskName) ? task.TaskName : task.Description,
                Height = 60,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 0, 0, 15),
                Padding = new Thickness(8)
            };
            stackPanel.Children.Add(taskNameBox);

            // 任务详情
            stackPanel.Children.Add(new TextBlock
            {
                Text = "任务详情:",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 5),
                Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80))
            });

            var taskDetailBox = new TextBox
            {
                Text = !string.IsNullOrWhiteSpace(task.TaskDetail) ? task.TaskDetail : task.OriginalContext,
                Height = 100,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 0, 0, 15),
                Padding = new Thickness(8)
            };
            stackPanel.Children.Add(taskDetailBox);

            // 截止时间
            stackPanel.Children.Add(new TextBlock
            {
                Text = "截止时间:",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 5),
                Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80))
            });

            var deadlinePicker = new DatePicker
            {
                SelectedDate = task.Deadline,
                Margin = new Thickness(0, 0, 0, 15),
                Padding = new Thickness(8)
            };
            stackPanel.Children.Add(deadlinePicker);

            // 截止时间（时：分）
            var timePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 15) };
            timePanel.Children.Add(new TextBlock { Text = "时间: ", VerticalAlignment = VerticalAlignment.Center, Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80)) });

            var hourCombo = new ComboBox
            {
                Width = 70,
                Margin = new Thickness(5, 0, 5, 0),
                Padding = new Thickness(5)
            };
            for (int i = 0; i < 24; i++) hourCombo.Items.Add(i.ToString("D2"));
            hourCombo.SelectedIndex = task.Deadline?.Hour ?? 23;
            timePanel.Children.Add(hourCombo);

            timePanel.Children.Add(new TextBlock { Text = ":", VerticalAlignment = VerticalAlignment.Center, Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80)) });

            var minuteCombo = new ComboBox
            {
                Width = 70,
                Margin = new Thickness(5, 0, 5, 0),
                Padding = new Thickness(5)
            };
            for (int i = 0; i < 60; i++) minuteCombo.Items.Add(i.ToString("D2"));
            minuteCombo.SelectedIndex = task.Deadline?.Minute ?? 59;
            timePanel.Children.Add(minuteCombo);

            stackPanel.Children.Add(timePanel);

            // 重要性
            stackPanel.Children.Add(new TextBlock
            {
                Text = "重要性:",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 5),
                Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80))
            });

            var importancePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 15) };
            var highRadio = new RadioButton { Content = "高", Margin = new Thickness(0, 0, 15, 0), Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80)) };
            var mediumRadio = new RadioButton { Content = "中", Margin = new Thickness(0, 0, 15, 0), Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80)) };
            var lowRadio = new RadioButton { Content = "低", Margin = new Thickness(0, 0, 15, 0), Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80)) };

            switch (task.Importance)
            {
                case "高": highRadio.IsChecked = true; break;
                case "中": mediumRadio.IsChecked = true; break;
                case "低": lowRadio.IsChecked = true; break;
                default: mediumRadio.IsChecked = true; break;
            }

            importancePanel.Children.Add(highRadio);
            importancePanel.Children.Add(mediumRadio);
            importancePanel.Children.Add(lowRadio);
            stackPanel.Children.Add(importancePanel);

            // 原文内容
            stackPanel.Children.Add(new TextBlock
            {
                Text = "原文内容:",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 5),
                Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80))
            });

            var originalTextBox = new TextBox
            {
                Text = !string.IsNullOrWhiteSpace(task.OriginalText) ? task.OriginalText : task.SourceText,
                Height = 80,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 0, 0, 15),
                Padding = new Thickness(8),
                IsReadOnly = true
            };
            stackPanel.Children.Add(originalTextBox);

            // 按钮
            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };

            var saveButton = new Button
            {
                Content = "保存",
                Width = 100,
                Height = 35,
                Margin = new Thickness(0, 0, 10, 0),
                Background = new SolidColorBrush(Color.FromRgb(52, 152, 219)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Style = null
            };
            saveButton.Click += (s, e) =>
            {
                try
                {
                    var selectedDate = deadlinePicker.SelectedDate;
                    if (selectedDate.HasValue)
                    {
                        var hour = int.Parse(hourCombo.SelectedItem.ToString());
                        var minute = int.Parse(minuteCombo.SelectedItem.ToString());
                        task.Deadline = selectedDate.Value.AddHours(hour).AddMinutes(minute);
                    }
                    else
                    {
                        task.Deadline = null;
                    }

                    task.TaskName = taskNameBox.Text;
                    task.Description = taskNameBox.Text; // 保持向后兼容
                    task.TaskDetail = taskDetailBox.Text;
                    task.OriginalContext = taskDetailBox.Text; // 保持向后兼容
                    task.Importance = highRadio.IsChecked == true ? "高" : mediumRadio.IsChecked == true ? "中" : "低";

                    var taskService = new TaskService();
                    taskService.UpdateTask(task);
                    editWindow.DialogResult = true;
                    editWindow.Close();
                    LoadWeekView();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            buttonPanel.Children.Add(saveButton);

            var cancelButton = new Button
            {
                Content = "取消",
                Width = 100,
                Height = 35,
                Background = new SolidColorBrush(Color.FromRgb(149, 165, 166)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Style = null
            };
            cancelButton.Click += (s, e) =>
            {
                editWindow.DialogResult = false;
                editWindow.Close();
            };
            buttonPanel.Children.Add(cancelButton);

            stackPanel.Children.Add(buttonPanel);

            // 将面板放入滚动视图，再将滚动视图放入窗口
            scrollViewer.Content = stackPanel;
            editWindow.Content = scrollViewer;
            editWindow.ShowDialog();
        }

        // 公共方法：外部调用刷新周视图
        public void RefreshWeekView()
        {
            LoadWeekView();
        }

        private string GetChineseDayOfWeek(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Sunday => "周日",
                DayOfWeek.Monday => "周一",
                DayOfWeek.Tuesday => "周二",
                DayOfWeek.Wednesday => "周三",
                DayOfWeek.Thursday => "周四",
                DayOfWeek.Friday => "周五",
                DayOfWeek.Saturday => "周六",
                _ => "未知"
            };
        }

        // 周视图按钮事件
        private void BtnWeekPrev_Click(object sender, RoutedEventArgs e)
        {
            _weekStartDate = _weekStartDate.AddDays(-_weekDays);
            LoadWeekView();
        }

        private void BtnWeekToday_Click(object sender, RoutedEventArgs e)
        {
            _weekStartDate = DateTime.Today;
            LoadWeekView();
        }

        private void BtnWeekNext_Click(object sender, RoutedEventArgs e)
        {
            _weekStartDate = _weekStartDate.AddDays(_weekDays);
            LoadWeekView();
        }

        // 7天/14天视图切换
        private void BtnWeekViewToggle_Click(object sender, RoutedEventArgs e)
        {
            if (_weekDays == 7)
            {
                _weekDays = 14;
                if (BtnWeekViewToggle is Button btn)
                {
                    btn.Content = "切换7天视图";
                }
            }
            else
            {
                _weekDays = 7;
                if (BtnWeekViewToggle is Button btn)
                {
                    btn.Content = "切换14天视图";
                }
            }
            LoadWeekView();
        }

        // 获取日期列索引
        private int GetDateColumnIndex(DateTime date)
        {
            for (int i = 0; i < _weekDays; i++)
            {
                if (_weekStartDate.AddDays(i).Date == date.Date)
                {
                    return i;
                }
            }
            return -1;
        }

        // ============ 周视图任务交互事件 ============
        private void WeekTask_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Opacity = 0.8;
            }
        }

        private void WeekTask_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Opacity = 1.0;
            }
        }

        private void WeekTask_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is string eventId)
            {
                ShowWeekTaskDetail(eventId);
            }
        }

        // 显示周视图任务详情
        private void ShowWeekTaskDetail(string eventId)
        {
            try
            {
                var events = CalendarService.GetEventsForWeek(_weekStartDate, _weekDays);
                var ev = events.Find(e => e.Task.Id == eventId);

                if (ev != null)
                {
                    // 使用与任务管理界面相同的任务详情对话框
                    var detailDialog = new TaskDetailDialog(eventId)
                    {
                        Owner = Window.GetWindow(this)
                    };

                    // 订阅事件
                    detailDialog.OnEditTask += (taskId) =>
                    {
                        // 如果需要特殊处理，可以在这里添加
                        LoadWeekView(); // 重新加载以反映更改
                    };

                    detailDialog.OnManageRelations += (taskId) =>
                    {
                        // 如果需要特殊处理，可以在这里添加
                        LoadWeekView(); // 重新加载以反映更改
                    };

                    // 订阅删除任务事件
                    detailDialog.OnDeleteTask += (taskId) =>
                    {
                        LoadWeekView(); // 重新加载以反映更改
                    };

                    detailDialog.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"显示任务详情失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 周视图任务复选框事件处理
        private void WeekTask_CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            HandleCheckBoxStateChange(sender as CheckBox, true);
        }

        private void WeekTask_CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            HandleCheckBoxStateChange(sender as CheckBox, false);
        }

        private void HandleCheckBoxStateChange(CheckBox checkBox, bool isCompleted)
        {
            if (checkBox?.Tag is string eventId)
            {
                var taskService = new TaskService();
                if (taskService.MarkAsCompleted(eventId, isCompleted))
                {
                    LoadWeekView();
                }
            }
        }

        // ============ 滚动同步方法 ============
        private void CalendarPage_Loaded(object sender, RoutedEventArgs e)
        {
            // 延迟执行以确保控件已加载
            Dispatcher.BeginInvoke(new Action(() =>
            {
                SetupScrollSync();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void SetupScrollSync()
        {
            // 尝试直接访问XAML中定义的ScrollViewer字段
            ScrollViewer headerScrollViewer = null;
            ScrollViewer taskScrollViewer = null;

            // 方法1：直接通过字段访问（如果WPF生成了对应字段）
            try
            {
                // 这些字段名应该与XAML中的x:Name一致
                headerScrollViewer = HeaderScrollViewer;
                taskScrollViewer = TaskScrollViewer;
            }
            catch
            {
                // 如果字段不存在，使用可视化树查找
                headerScrollViewer = FindVisualChild<ScrollViewer>(WeekViewMainGrid, "HeaderScrollViewer");
                taskScrollViewer = FindVisualChild<ScrollViewer>(WeekViewMainGrid, "TaskScrollViewer");
            }

            // 如果通过名称查找失败，尝试查找第一个和第二个ScrollViewer
            if (headerScrollViewer == null || taskScrollViewer == null)
            {
                var allScrollViewers = FindVisualChildren<ScrollViewer>(WeekViewMainGrid).ToList();
                if (allScrollViewers.Count >= 2)
                {
                    headerScrollViewer = allScrollViewers[0];
                    taskScrollViewer = allScrollViewers[1];
                }
            }

            // 找到两个ScrollViewer后同步它们的滚动
            if (headerScrollViewer != null && taskScrollViewer != null && headerScrollViewer != taskScrollViewer)
            {
                // 保存引用到字段
                _headerScrollViewer = headerScrollViewer;
                _taskScrollViewer = taskScrollViewer;

                // 清除旧的事件处理器（避免重复绑定）
                _headerScrollViewer.ScrollChanged -= HeaderScrollViewer_ScrollChanged;
                _taskScrollViewer.ScrollChanged -= TaskScrollViewer_ScrollChanged;

                // 添加新的事件处理器
                _headerScrollViewer.ScrollChanged += HeaderScrollViewer_ScrollChanged;
                _taskScrollViewer.ScrollChanged += TaskScrollViewer_ScrollChanged;
            }
        }

        private void HeaderScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_isSyncingScroll) return;
            if (Math.Abs(e.HorizontalChange) < 0.1 && Math.Abs(e.VerticalChange) < 0.1) return;
            if (_taskScrollViewer == null) return;

            _isSyncingScroll = true;
            try
            {
                // 同步水平偏移：日期头部滚动时，任务区域同步滚动
                _taskScrollViewer.ScrollToHorizontalOffset(_headerScrollViewer.HorizontalOffset);
            }
            finally
            {
                _isSyncingScroll = false;
            }
        }

        private void TaskScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_isSyncingScroll) return;
            if (Math.Abs(e.HorizontalChange) < 0.1 && Math.Abs(e.VerticalChange) < 0.1) return;
            if (_headerScrollViewer == null) return;

            _isSyncingScroll = true;
            try
            {
                // 同步水平偏移：任务区域滚动时，日期头部同步滚动
                _headerScrollViewer.ScrollToHorizontalOffset(_taskScrollViewer.HorizontalOffset);
            }
            finally
            {
                _isSyncingScroll = false;
            }
        }

        // 按名称查找可视化子元素
        private T FindVisualChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result && (child as FrameworkElement)?.Name == childName)
                    return result;

                var childResult = FindVisualChild<T>(child, childName);
                if (childResult != null)
                    return childResult;
            }

            return null;
        }

        // 查找所有可视化子元素
        private IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    yield return result;

                foreach (var grandChild in FindVisualChildren<T>(child))
                    yield return grandChild;
            }
        }
    }
}
