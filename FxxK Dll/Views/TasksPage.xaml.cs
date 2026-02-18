using FxxkDDL.Core.Common;
using FxxkDDL.Core.Interfaces;
using FxxkDDL.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FxxkDDL.Views
{
    public partial class TasksPage : UserControl
    {
        private ITaskService _taskService;
        private string _currentFilter = "all";

        public TasksPage()
        {
            InitializeComponent();
            InitializePage();
        }

        private void InitializePage()
        {
            try
            {
                _taskService = ServiceLocator.GetService<ITaskService>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"页面初始化失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 任务卡片点击事件 - 显示任务详情对话框
        /// </summary>
        private void TaskCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is Border border && border.Tag is string taskId)
                {
                    ShowTaskDetailDialog(taskId);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开任务详情失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 显示任务详情对话框
        /// </summary>
        private void ShowTaskDetailDialog(string taskId)
        {
            try
            {
                var detailDialog = new TaskDetailDialog(taskId)
                {
                    Owner = Window.GetWindow(this)
                };

                // 订阅事件
                detailDialog.OnEditTask += HandleEditTask;
                detailDialog.OnManageRelations += HandleManageRelations;
                detailDialog.OnDeleteTask += HandleDeleteTask;

                detailDialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"显示任务详情失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 处理编辑任务事件
        /// </summary>
        private void HandleEditTask(string taskId)
        {
            try
            {
                var task = _taskService.GetTask(taskId);
                if (task != null)
                {
                    ShowEditDialog(task);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"编辑任务失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 处理管理关系事件
        /// </summary>
        private void HandleManageRelations(string taskId)
        {
            try
            {
                var relationDialog = new TaskRelationshipDialog(taskId)
                {
                    Owner = Window.GetWindow(this)
                };
                relationDialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开关系管理失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 处理删除任务事件
        /// </summary>
        private void HandleDeleteTask(string taskId)
        {
            try
            {
                // 刷新任务列表
                var viewModel = DataContext as Core.ViewModels.TasksViewModel;
                viewModel?.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"刷新任务列表失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
                Height = 450,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(248, 249, 250))
            };

            var stackPanel = new StackPanel { Margin = new Thickness(20) };

            // 任务描述
            stackPanel.Children.Add(new TextBlock
            {
                Text = "任务描述:",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 5),
                Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80))
            });

            var descriptionBox = new TextBox
            {
                Text = task.Description,
                Height = 80,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 0, 0, 15),
                Padding = new Thickness(8)
            };
            stackPanel.Children.Add(descriptionBox);

            // 截止时间
            var timeGrid = new Grid { Margin = new Thickness(0, 0, 0, 15) };
            timeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            timeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            timeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            timeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            timeGrid.Children.Add(new TextBlock
            {
                Text = "截止日期:",
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80))
            });

            var datePicker = new DatePicker
            {
                SelectedDate = task.Deadline?.Date ?? DateTime.Today.AddDays(7),
                Margin = new Thickness(0, 0, 10, 0),
                Width = 120
            };
            Grid.SetColumn(datePicker, 1);

            timeGrid.Children.Add(new TextBlock
            {
                Text = "时间:",
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80))
            });
            Grid.SetColumn(timeGrid.Children[timeGrid.Children.Count - 1], 2);

            var timePicker = new ComboBox
            {
                ItemsSource = GetTimeOptions(),
                SelectedItem = task.Deadline?.ToString("HH:mm") ?? "23:59",
                Width = 80
            };
            Grid.SetColumn(timePicker, 3);

            timeGrid.Children.Add(datePicker);
            timeGrid.Children.Add(timePicker);
            stackPanel.Children.Add(timeGrid);

            // 重要性选择
            stackPanel.Children.Add(new TextBlock
            {
                Text = "重要性:",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 5),
                Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80))
            });

            var importanceCombo = new ComboBox
            {
                ItemsSource = new[] { "高", "中", "低" },
                SelectedItem = task.Importance,
                Width = 100,
                Margin = new Thickness(0, 0, 0, 20)
            };
            stackPanel.Children.Add(importanceCombo);

            // 按钮面板
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var saveButton = new Button
            {
                Content = "💾 保存",
                Padding = new Thickness(15, 8, 15, 8),
                Background = new SolidColorBrush(Color.FromRgb(52, 152, 219)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 10, 0),
                Cursor = Cursors.Hand
            };

            saveButton.Click += (s, e) =>
            {
                try
                {
                    // 更新任务信息
                    task.Description = descriptionBox.Text.Trim();
                    task.Importance = importanceCombo.SelectedItem?.ToString() ?? "中";

                    if (datePicker.SelectedDate.HasValue && timePicker.SelectedItem != null)
                    {
                        var timeStr = timePicker.SelectedItem.ToString();
                        if (DateTime.TryParse($"{datePicker.SelectedDate.Value:yyyy-MM-dd} {timeStr}", out var newDeadline))
                        {
                            task.Deadline = newDeadline;
                            task.DeadlineString = newDeadline.ToString("yyyy-MM-dd HH:mm");
                        }
                    }

                    // 保存到数据库
                    if (_taskService.UpdateTask(task))
                    {
                        MessageBox.Show("任务已更新", "操作成功",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        // 刷新任务列表（通过ViewModel）
                        var viewModel = DataContext as Core.ViewModels.TasksViewModel;
                        viewModel?.Refresh();

                        editWindow.Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"保存失败: {ex.Message}", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            var cancelButton = new Button
            {
                Content = "取消",
                Padding = new Thickness(15, 8, 15, 8),
                Background = new SolidColorBrush(Color.FromRgb(149, 165, 166)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand
            };
            cancelButton.Click += (s, e) => editWindow.Close();

            buttonPanel.Children.Add(saveButton);
            buttonPanel.Children.Add(cancelButton);
            stackPanel.Children.Add(buttonPanel);

            editWindow.Content = stackPanel;
            editWindow.ShowDialog();
        }

        /// <summary>
        /// 时间选项生成
        /// </summary>
        private System.Collections.Generic.List<string> GetTimeOptions()
        {
            var times = new System.Collections.Generic.List<string>();
            for (int hour = 0; hour < 24; hour++)
            {
                for (int minute = 0; minute < 60; minute += 30)
                {
                    times.Add($"{hour:D2}:{minute:D2}");
                }
            }
            return times;
        }

        /// <summary>
        /// 任务复选框点击事件
        /// </summary>
        private void TaskCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            HandleTaskCompleteChange(sender, true);
        }

        /// <summary>
        /// 任务复选框取消勾选事件
        /// </summary>
        private void TaskCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            HandleTaskCompleteChange(sender, false);
        }

        /// <summary>
        /// 处理任务完成状态变更
        /// </summary>
        private void HandleTaskCompleteChange(object sender, bool completed)
        {
            try
            {
                if (sender is CheckBox checkBox && checkBox.Tag is string taskId)
                {
                    bool success = _taskService.MarkAsCompleted(taskId, completed);

                    if (success)
                    {
                        // 刷新任务列表
                        var viewModel = DataContext as Core.ViewModels.TasksViewModel;
                        viewModel?.Refresh();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"更新任务状态失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
