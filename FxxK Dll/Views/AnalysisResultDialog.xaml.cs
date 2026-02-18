using FxxkDDL.Models;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FxxkDDL.Views
{
    /// <summary>
    /// 分析结果对话框
    /// </summary>
    public partial class AnalysisResultDialog : Window
    {
        private List<DDLTask> _tasks;
        private string _message;

        /// <summary>
        /// 构造函数
        /// </summary>
        public AnalysisResultDialog(List<DDLTask> tasks, string message)
        {
            InitializeComponent();
            _tasks = tasks;
            _message = message;

            Loaded += AnalysisResultDialog_Loaded;
            InitializeDialog();
        }

        private void AnalysisResultDialog_Loaded(object sender, RoutedEventArgs e)
        {
            // 设置对话框所有者
            if (Owner == null && Application.Current.MainWindow != this)
            {
                Owner = Application.Current.MainWindow;
            }
        }

        /// <summary>
        /// 初始化对话框
        /// </summary>
        private void InitializeDialog()
        {
            // 设置结果标题和消息
            if (_message.Contains("成功"))
            {
                ResultTitle.Text = "✅ 分析成功";
                ResultTitle.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"));
            }
            else if (_message.Contains("失败"))
            {
                ResultTitle.Text = "❌ 分析失败";
                ResultTitle.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
            }
            else
            {
                ResultTitle.Text = "⚠️ 分析完成";
                ResultTitle.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F39C12"));
            }

            ResultMessage.Text = _message;
            TaskCount.Text = _tasks.Count.ToString();

            // 创建任务显示项列表
            var taskItems = new List<TaskDisplayItem>();
            foreach (var task in _tasks)
            {
                taskItems.Add(new TaskDisplayItem(task));
            }

            // 绑定到ItemsControl
            TasksItemsControl.ItemsSource = taskItems;

            // 如果没有任务，显示提示
            if (_tasks.Count == 0)
            {
                var emptyTextBlock = new TextBlock
                {
                    Text = "📭 未提取到明确的DDL任务\n\n可能是文本中没有明确的截止日期信息，\n或者格式不符合AI识别规则。",
                    FontSize = 13,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7F8C8D")),
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    Padding = new Thickness(20),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                // 清除原有内容，添加提示
                TasksItemsControl.ItemsSource = null;
                var grid = TasksItemsControl.Parent as Grid;
                if (grid != null)
                {
                    grid.Children.Remove(TasksItemsControl);
                    grid.Children.Add(emptyTextBlock);
                }
            }
        }

        /// <summary>
        /// 关闭按钮点击事件
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// 任务显示项（用于数据绑定）
        /// </summary>
        private class TaskDisplayItem
        {
            public string Description { get; set; }
            public string DeadlineText { get; set; }
            public string Importance { get; set; }
            public Brush ImportanceColor { get; set; }

            public TaskDisplayItem(DDLTask task)
            {
                Description = task.Description;

                // 格式化截止时间
                if (task.Deadline.HasValue)
                {
                    DeadlineText = task.Deadline.Value.ToString("yyyy-MM-dd HH:mm");
                }
                else if (!string.IsNullOrWhiteSpace(task.DeadlineString))
                {
                    DeadlineText = task.DeadlineString;
                }
                else
                {
                    DeadlineText = "未指定";
                }

                Importance = task.Importance;

                // 根据重要度设置颜色
                ImportanceColor = task.Importance switch
                {
                    "高" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C")), // 红色
                    "中" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F39C12")), // 橙色
                    "低" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60")), // 绿色
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#95A5A6"))   // 灰色
                };
            }
        }
    }
}