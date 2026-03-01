using FxxkDDL.Core.Common;
using FxxkDDL.Core.Interfaces;
using FxxkDDL.Models;
using System;
using System.Windows;
using System.Windows.Controls;

namespace FxxkDDL.Views
{
    /// <summary>
    /// 手动添加任务对话框
    /// </summary>
    public partial class AddTaskDialog : Window
    {
        private ITaskService _taskService;

        // 用于 XAML 绑定的静态属性
        public static DateTime TodayPlusSeven => DateTime.Today.AddDays(7);

        /// <summary>
        /// 创建新任务事件（任务创建成功后触发）
        /// </summary>
        public event Action<DDLTask> OnTaskCreated;

        public AddTaskDialog()
        {
            InitializeComponent();
            InitializeDialog();
        }

        private void InitializeDialog()
        {
            try
            {
                _taskService = ServiceLocator.GetService<ITaskService>();

                // 初始化时间下拉框（30分钟间隔）
                var timeOptions = GetTimeOptions();
                CmbTime.ItemsSource = timeOptions;
                CmbTime.SelectedItem = "23:59";

                // 初始化重要性下拉框
                CmbImportance.ItemsSource = new[] { "高", "中", "低" };
                CmbImportance.SelectedItem = "中";

                // 设置默认焦点
                Loaded += (s, e) => TxtTaskName.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"对话框初始化失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 生成时间选项（30分钟间隔）
        /// </summary>
        private static string[] GetTimeOptions()
        {
            var times = new string[48]; // 24小时 * 2
            int index = 0;
            for (int hour = 0; hour < 24; hour++)
            {
                for (int minute = 0; minute < 60; minute += 30)
                {
                    times[index++] = $"{hour:D2}:{minute:D2}";
                }
            }
            return times;
        }

        /// <summary>
        /// 保存按钮点击事件
        /// </summary>
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 获取输入值，空值使用默认值
                string taskName = TxtTaskName.Text.Trim();
                if (string.IsNullOrWhiteSpace(taskName))
                {
                    taskName = "无标题";
                }

                string taskDetail = TxtTaskDetail.Text.Trim();
                if (string.IsNullOrWhiteSpace(taskDetail))
                {
                    taskDetail = taskName; // 如果没有详情，使用任务名
                }

                string originalText = TxtOriginalText.Text.Trim();

                // 创建新任务
                var newTask = new DDLTask
                {
                    Id = Guid.NewGuid().ToString(),
                    TaskName = taskName,
                    TaskDetail = taskDetail,
                    OriginalText = originalText,
                    Importance = CmbImportance.SelectedItem?.ToString() ?? "中",
                    IsCompleted = false,
                    CreatedAt = DateTime.Now
                };

                // 设置截止时间
                if (DpDeadline.SelectedDate.HasValue && CmbTime.SelectedItem != null)
                {
                    var timeStr = CmbTime.SelectedItem.ToString();
                    if (DateTime.TryParse($"{DpDeadline.SelectedDate.Value:yyyy-MM-dd} {timeStr}", out var deadline))
                    {
                        newTask.Deadline = deadline;
                        newTask.DeadlineString = deadline.ToString("yyyy-MM-dd HH:mm");
                    }
                }

                // 保存到数据库
                _taskService.AddTask(newTask);

                MessageBox.Show($"任务已添加!\n\n任务: {newTask.TaskName}", "操作成功",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // 触发事件
                OnTaskCreated?.Invoke(newTask);

                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 取消按钮点击事件
        /// </summary>
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
