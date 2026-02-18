using FxxkDDL.Core.Common;
using FxxkDDL.Core.Interfaces;
using FxxkDDL.Models;
using System;
using System.Windows;
using System.Windows.Media;

namespace FxxkDDL.Views
{
    /// <summary>
    /// 任务详情对话框
    /// </summary>
    public partial class TaskDetailDialog : Window
    {
        private readonly ITaskService _taskService;
        private readonly DDLTask _task;

        public event Action<string> OnEditTask;
        public event Action<string> OnManageRelations;
        public event Action<string> OnDeleteTask;

        public TaskDetailDialog(string taskId)
        {
            InitializeComponent();

            _taskService = ServiceLocator.GetService<ITaskService>();
            _task = _taskService.GetTask(taskId);

            if (_task == null)
            {
                MessageBox.Show("任务不存在或已被删除", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            LoadTaskDetails();
            DataContext = this;
        }

        /// <summary>
        /// 加载任务详情
        /// </summary>
        private void LoadTaskDetails()
        {
            // 标题 - 使用TaskName或Description（兼容旧数据）
            TxtTaskTitle.Text = !string.IsNullOrWhiteSpace(_task.TaskName)
                ? _task.TaskName
                : (_task.Description ?? "无标题");

            // 截止时间
            TxtTaskDeadline.Text = _task.Deadline != null
                ? _task.Deadline.Value.ToString("yyyy年MM月dd日 HH:mm")
                : "未指定截止时间";

            // 重要性
            TxtTaskImportance.Text = _task.Importance ?? "中";

            // 任务名称
            TxtTaskName.Text = !string.IsNullOrWhiteSpace(_task.TaskName)
                ? _task.TaskName
                : (_task.Description ?? "无标题");

            // 任务详情 - 使用TaskDetail或OriginalContext（兼容旧数据）
            TxtTaskDetail.Text = !string.IsNullOrWhiteSpace(_task.TaskDetail)
                ? _task.TaskDetail
                : (_task.OriginalContext ?? _task.Description ?? "无详情");

            // 原文内容 - 使用OriginalText或SourceText（兼容旧数据）
            string originalText = !string.IsNullOrWhiteSpace(_task.OriginalText)
                ? _task.OriginalText
                : _task.SourceText;

            if (!string.IsNullOrWhiteSpace(originalText))
            {
                TxtOriginalText.Text = originalText;
            }
            else
            {
                TxtOriginalText.Text = "无原文内容";
            }

            // 创建时间
            TxtCreatedAt.Text = _task.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");

            // 完成时间
            if (_task.CompletedAt != null)
            {
                TxtCompletedAt.Text = _task.CompletedAt.Value.ToString("yyyy-MM-dd HH:mm:ss");
            }
            else
            {
                TxtCompletedAt.Text = "未完成";
            }

            // 重要性颜色
            ImportanceColor = GetImportanceColor(_task.Importance);

            // 父任务
            var parentTask = _taskService.GetParentTask(_task.Id);
            if (parentTask != null)
            {
                TxtParentTask.Text = !string.IsNullOrWhiteSpace(parentTask.TaskName)
                    ? parentTask.TaskName
                    : parentTask.Description;
                HasParentTask = true;
            }
            else
            {
                HasParentTask = false;
            }

            // 子任务
            var subTasks = _taskService.GetSubTasks(_task.Id);
            if (subTasks.Count > 0)
            {
                LstSubTasks.ItemsSource = subTasks;
                HasSubTasks = true;
            }
            else
            {
                HasSubTasks = false;
            }

            // 依赖任务
            var dependencies = _taskService.GetDependencies(_task.Id);
            if (dependencies.Count > 0)
            {
                LstDependencies.ItemsSource = dependencies;
                HasDependencies = true;
            }
            else
            {
                HasDependencies = false;
            }

            HasAnyRelations = HasParentTask || HasSubTasks || HasDependencies;
        }

        /// <summary>
        /// 获取重要性对应的颜色
        /// </summary>
        private Brush GetImportanceColor(string importance)
        {
            return importance switch
            {
                "高" => new SolidColorBrush(Color.FromRgb(231, 76, 60)),   // 红色
                "中" => new SolidColorBrush(Color.FromRgb(241, 196, 15)),  // 黄色
                "低" => new SolidColorBrush(Color.FromRgb(46, 204, 113)),  // 绿色
                _ => new SolidColorBrush(Color.FromRgb(52, 152, 219))    // 蓝色
            };
        }

        // 绑定属性
        public Brush ImportanceColor { get; private set; }
        public bool HasOriginalContext { get; private set; }
        public bool HasParentTask { get; private set; }
        public bool HasSubTasks { get; private set; }
        public bool HasDependencies { get; private set; }
        public bool HasAnyRelations { get; private set; }

        /// <summary>
        /// 管理关系按钮点击
        /// </summary>
        private void BtnManageRelations_Click(object sender, RoutedEventArgs e)
        {
            OnManageRelations?.Invoke(_task.Id);
        }

        /// <summary>
        /// 编辑任务按钮点击
        /// </summary>
        private void BtnEditTask_Click(object sender, RoutedEventArgs e)
        {
            OnEditTask?.Invoke(_task.Id);
        }

        /// <summary>
        /// 删除任务按钮点击
        /// </summary>
        private void BtnDeleteTask_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                $"确定要删除任务 \"{_task.TaskName ?? _task.Description}\" 吗？\n\n" +
                "此操作不可撤销。",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var success = _taskService.DeleteTask(_task.Id);
                    if (success)
                    {
                        OnDeleteTask?.Invoke(_task.Id);
                        Close();
                    }
                    else
                    {
                        MessageBox.Show(
                            "无法删除该任务。\n可能原因：\n" +
                            "- 任务有子任务\n" +
                            "- 任务被其他任务依赖\n\n" +
                            "请先解除任务关系后再删除。",
                            "删除失败",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"删除任务失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 关闭按钮点击
        /// </summary>
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
