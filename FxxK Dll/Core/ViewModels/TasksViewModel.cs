using FxxkDDL.Core.Common;
using FxxkDDL.Core.Interfaces;
using FxxkDDL.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace FxxkDDL.Core.ViewModels
{
    /// <summary>
    /// 任务管理页面ViewModel
    /// </summary>
    public class TasksViewModel : ViewModelBase
    {
        private readonly ITaskService _taskService;
        private List<DDLTask> _allTasks;
        private List<TaskDisplayItem> _displayTasks;
        private string _currentFilter;

        /// <summary>
        /// 任务显示项
        /// </summary>
        public class TaskDisplayItem : System.ComponentModel.INotifyPropertyChanged
        {
            public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

            private string _id;
            private string _description;
            private string _deadlineString;
            private string _importance;
            private bool _isCompleted;
            private DDLTask _originalTask;
            private string _relationshipInfo;
            private Brush _importanceColor;
            private bool _hasDeadline;

            public string Id
            {
                get => _id;
                set { _id = value; OnPropertyChanged(nameof(Id)); }
            }

            public string Description
            {
                get => _description;
                set { _description = value; OnPropertyChanged(nameof(Description)); }
            }

            public string DeadlineString
            {
                get => _deadlineString;
                set { _deadlineString = value; OnPropertyChanged(nameof(DeadlineString)); }
            }

            public string Importance
            {
                get => _importance;
                set { _importance = value; OnPropertyChanged(nameof(Importance)); }
            }

            public bool IsCompleted
            {
                get => _isCompleted;
                set { _isCompleted = value; OnPropertyChanged(nameof(IsCompleted)); }
            }

            public DDLTask OriginalTask
            {
                get => _originalTask;
                set { _originalTask = value; OnPropertyChanged(nameof(OriginalTask)); }
            }

            public string RelationshipInfo
            {
                get => _relationshipInfo;
                set { _relationshipInfo = value; OnPropertyChanged(nameof(RelationshipInfo)); }
            }

            public Brush ImportanceColor
            {
                get => _importanceColor;
                set { _importanceColor = value; OnPropertyChanged(nameof(ImportanceColor)); }
            }

            public bool HasDeadline
            {
                get => _hasDeadline;
                set { _hasDeadline = value; OnPropertyChanged(nameof(HasDeadline)); }
            }

            public Action<bool> OnIsCompletedChanged { get; set; }

            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));

                if (propertyName == nameof(IsCompleted) && OnIsCompletedChanged != null)
                {
                    OnIsCompletedChanged(_isCompleted);
                }
            }
        }

        /// <summary>
        /// 高优先级任务
        /// </summary>
        public ObservableCollection<TaskDisplayItem> HighPriorityTasks { get; } = new ObservableCollection<TaskDisplayItem>();

        /// <summary>
        /// 中优先级任务
        /// </summary>
        public ObservableCollection<TaskDisplayItem> MediumPriorityTasks { get; } = new ObservableCollection<TaskDisplayItem>();

        /// <summary>
        /// 低优先级任务
        /// </summary>
        public ObservableCollection<TaskDisplayItem> LowPriorityTasks { get; } = new ObservableCollection<TaskDisplayItem>();

        /// <summary>
        /// 未指定日期任务
        /// </summary>
        public ObservableCollection<TaskDisplayItem> NoDeadlineTasks { get; } = new ObservableCollection<TaskDisplayItem>();

        /// <summary>
        /// 所有显示的任务
        /// </summary>
        public List<TaskDisplayItem> DisplayTasks
        {
            get => _displayTasks;
            private set => SetProperty(ref _displayTasks, value);
        }

        /// <summary>
        /// 当前筛选类型
        /// </summary>
        public string CurrentFilter
        {
            get => _currentFilter;
            private set => SetProperty(ref _currentFilter, value);
        }

        /// <summary>
        /// 高优先级任务数
        /// </summary>
        public int HighTasksCount => HighPriorityTasks.Count;

        /// <summary>
        /// 中优先级任务数
        /// </summary>
        public int MediumTasksCount => MediumPriorityTasks.Count;

        /// <summary>
        /// 低优先级任务数
        /// </summary>
        public int LowTasksCount => LowPriorityTasks.Count;

        /// <summary>
        /// 未指定日期任务数
        /// </summary>
        public int NoDeadlineTasksCount => NoDeadlineTasks.Count;

        /// <summary>
        /// 高优先级任务文本
        /// </summary>
        public string HighTasksText => $"{HighPriorityTasks.Count(t => !t.IsCompleted)} 待完成";

        /// <summary>
        /// 中优先级任务文本
        /// </summary>
        public string MediumTasksText => $"{MediumPriorityTasks.Count(t => !t.IsCompleted)} 待完成";

        /// <summary>
        /// 低优先级任务文本
        /// </summary>
        public string LowTasksText => $"{LowPriorityTasks.Count(t => !t.IsCompleted)} 待完成";

        /// <summary>
        /// 未指定日期任务文本
        /// </summary>
        public string NoDeadlineTasksText => $"{NoDeadlineTasks.Count(t => !t.IsCompleted)} 待完成";

        private string _taskCountText = "正在加载...";
        /// <summary>
        /// 任务计数文本
        /// </summary>
        public string TaskCountText
        {
            get => _taskCountText;
            private set => SetProperty(ref _taskCountText, value);
        }

        /// <summary>
        /// 显示所有任务命令
        /// </summary>
        public ICommand ShowAllCommand { get; }

        /// <summary>
        /// 显示未完成任务命令
        /// </summary>
        public ICommand ShowPendingCommand { get; }

        /// <summary>
        /// 显示已完成任务命令
        /// </summary>
        public ICommand ShowCompletedCommand { get; }

        /// <summary>
        /// 删除所有已完成任务命令
        /// </summary>
        public ICommand DeleteCompletedTasksCommand { get; }

        /// <summary>
        /// 根据ID删除任务命令
        /// </summary>
        public ICommand DeleteTaskByIdCommand { get; }

        /// <summary>
        /// 根据ID编辑任务命令
        /// </summary>
        public ICommand EditTaskByIdCommand { get; }

        /// <summary>
        /// 根据ID管理任务关系命令
        /// </summary>
        public ICommand ManageRelationshipsCommand { get; }

        /// <summary>
        /// 根据ID显示任务详情命令
        /// </summary>
        public ICommand ShowDetailByIdCommand { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public TasksViewModel()
        {
            _taskService = ServiceLocator.GetService<ITaskService>();

            // 初始化命令
            ShowAllCommand = new RelayCommand(() => LoadTasks("all"));
            ShowPendingCommand = new RelayCommand(() => LoadTasks("pending"));
            ShowCompletedCommand = new RelayCommand(() => LoadTasks("completed"));
            DeleteCompletedTasksCommand = new RelayCommand(DeleteAllCompletedTasks);
            DeleteTaskByIdCommand = new RelayCommand<string>(DeleteTaskById);
            EditTaskByIdCommand = new RelayCommand<string>(EditTaskById);
            ManageRelationshipsCommand = new RelayCommand<string>(ManageRelationshipsById);
            ShowDetailByIdCommand = new RelayCommand<string>(ShowDetailById);

            // 初始加载
            LoadTasks("all");
        }

        /// <summary>
        /// 加载任务
        /// </summary>
        private void LoadTasks(string filterType)
        {
            ExecuteWithBusyAsync(async () =>
            {
                try
                {
                    _allTasks = _taskService.GetAllTasks();
                    CurrentFilter = filterType;

                    // 过滤任务
                    IEnumerable<DDLTask> filteredTasks = filterType switch
                    {
                        "pending" => _allTasks.Where(t => !t.IsCompleted),
                        "completed" => _allTasks.Where(t => t.IsCompleted),
                        _ => _allTasks
                    };

                    // 清空所有集合
                    HighPriorityTasks.Clear();
                    MediumPriorityTasks.Clear();
                    LowPriorityTasks.Clear();
                    NoDeadlineTasks.Clear();

                    // 转换为显示模型并分组
                    foreach (var task in filteredTasks)
                    {
                        var displayItem = CreateDisplayItem(task);

                        // 检查是否为未指定日期的任务
                        if (task.Deadline == null)
                        {
                            NoDeadlineTasks.Add(displayItem);
                        }
                        else
                        {
                            // 按重要性分组
                            string importance = task.Importance ?? "中";
                            switch (importance)
                            {
                                case "高":
                                    HighPriorityTasks.Add(displayItem);
                                    break;
                                case "中":
                                    MediumPriorityTasks.Add(displayItem);
                                    break;
                                case "低":
                                    LowPriorityTasks.Add(displayItem);
                                    break;
                                default:
                                    MediumPriorityTasks.Add(displayItem);
                                    break;
                            }
                        }
                    }

                    // 对每个组内的任务进行排序：按截止日期升序（越早越紧急）
                    SortTasks(HighPriorityTasks);
                    SortTasks(MediumPriorityTasks);
                    SortTasks(LowPriorityTasks);

                    // 未指定日期的任务按创建时间排序
                    SortTasksByCreatedTime(NoDeadlineTasks);

                    UpdateTaskCounts();
                }
                catch (Exception ex)
                {
                    SetError($"加载任务失败: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 创建显示项
        /// </summary>
        private TaskDisplayItem CreateDisplayItem(DDLTask task)
        {
            var displayItem = new TaskDisplayItem
            {
                Id = task.Id,
                Description = task.Description ?? "无描述",
                DeadlineString = task.Deadline?.ToString("yyyy-MM-dd HH:mm") ?? "无截止时间",
                Importance = task.Importance ?? "中",
                IsCompleted = task.IsCompleted,
                OriginalTask = task,
                RelationshipInfo = CalculateRelationshipInfo(task),
                ImportanceColor = GetImportanceColorBrush(task.Importance),
                HasDeadline = task.Deadline != null
            };

            // 设置完成状态更改回调
            displayItem.OnIsCompletedChanged = (completed) =>
            {
                ExecuteWithBusyAsync(async () =>
                {
                    try
                    {
                        var success = _taskService.MarkAsCompleted(displayItem.Id, completed);
                        if (success)
                        {
                            task.IsCompleted = completed;
                            task.CompletedAt = completed ? DateTime.Now : null;
                            UpdateTaskCounts();
                        }
                    }
                    catch (Exception ex)
                    {
                        SetError($"标记任务失败: {ex.Message}");
                    }
                });
            };

            return displayItem;
        }

        /// <summary>
        /// 按截止日期排序任务
        /// </summary>
        private void SortTasks(ObservableCollection<TaskDisplayItem> tasks)
        {
            var sorted = tasks.OrderBy(t => t.OriginalTask?.Deadline ?? DateTime.MaxValue).ToList();
            tasks.Clear();
            foreach (var task in sorted)
            {
                tasks.Add(task);
            }
        }

        /// <summary>
        /// 按创建时间排序任务
        /// </summary>
        private void SortTasksByCreatedTime(ObservableCollection<TaskDisplayItem> tasks)
        {
            var sorted = tasks.OrderByDescending(t => t.OriginalTask?.CreatedAt ?? DateTime.Now).ToList();
            tasks.Clear();
            foreach (var task in sorted)
            {
                tasks.Add(task);
            }
        }

        /// <summary>
        /// 获取重要性对应的颜色
        /// </summary>
        private Brush GetImportanceColorBrush(string importance)
        {
            return importance switch
            {
                "高" => new SolidColorBrush(Color.FromRgb(231, 76, 60)),   // 红色
                "中" => new SolidColorBrush(Color.FromRgb(241, 196, 15)),  // 黄色
                "低" => new SolidColorBrush(Color.FromRgb(46, 204, 113)),  // 绿色
                _ => new SolidColorBrush(Color.FromRgb(52, 152, 219))    // 蓝色
            };
        }

        /// <summary>
        /// 计算任务关系信息字符串
        /// </summary>
        private string CalculateRelationshipInfo(DDLTask task)
        {
            var info = new List<string>();

            // 检查父任务
            var parentTask = _taskService.GetParentTask(task.Id);
            if (parentTask != null)
            {
                info.Add($"父: {parentTask.Description.Substring(0, Math.Min(parentTask.Description.Length, 10))}...");
            }

            // 检查子任务
            var subTasks = _taskService.GetSubTasks(task.Id);
            if (subTasks.Count > 0)
            {
                info.Add($"子{subTasks.Count}");
            }

            // 检查依赖任务
            var dependencies = _taskService.GetDependencies(task.Id);
            if (dependencies.Count > 0)
            {
                info.Add($"依赖{dependencies.Count}");
            }

            if (info.Count == 0)
            {
                return "无";
            }

            return string.Join(" | ", info);
        }

        /// <summary>
        /// 更新任务计数
        /// </summary>
        private void UpdateTaskCounts()
        {
            OnPropertyChanged(nameof(HighTasksCount));
            OnPropertyChanged(nameof(MediumTasksCount));
            OnPropertyChanged(nameof(LowTasksCount));
            OnPropertyChanged(nameof(NoDeadlineTasksCount));
            OnPropertyChanged(nameof(HighTasksText));
            OnPropertyChanged(nameof(MediumTasksText));
            OnPropertyChanged(nameof(LowTasksText));
            OnPropertyChanged(nameof(NoDeadlineTasksText));

            int totalTasks = HighTasksCount + MediumTasksCount + LowTasksCount + NoDeadlineTasksCount;
            int totalPending = HighPriorityTasks.Count(t => !t.IsCompleted) +
                              MediumPriorityTasks.Count(t => !t.IsCompleted) +
                              LowPriorityTasks.Count(t => !t.IsCompleted) +
                              NoDeadlineTasks.Count(t => !t.IsCompleted);

            int totalCompleted = totalTasks - totalPending;

            TaskCountText = $"共 {totalTasks} 个任务（{totalPending} 个待完成，{totalCompleted} 个已完成）";
        }

        /// <summary>
        /// 刷新任务列表
        /// </summary>
        public void Refresh()
        {
            LoadTasks(CurrentFilter);
        }

        /// <summary>
        /// 删除所有已完成任务
        /// </summary>
        private void DeleteAllCompletedTasks()
        {
            ExecuteWithBusyAsync(async () =>
            {
                try
                {
                    var count = _taskService.DeleteCompletedTasks();
                    if (count > 0)
                    {
                        LoadTasks(CurrentFilter);
                    }
                }
                catch (Exception ex)
                {
                    SetError($"删除已完成任务失败: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 根据ID删除任务
        /// </summary>
        private void DeleteTaskById(string taskId)
        {
            ExecuteWithBusyAsync(async () =>
            {
                try
                {
                    var success = _taskService.DeleteTask(taskId);
                    if (success)
                    {
                        LoadTasks(CurrentFilter);
                    }
                }
                catch (Exception ex)
                {
                    SetError($"删除任务失败: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 根据ID编辑任务
        /// </summary>
        private void EditTaskById(string taskId)
        {
            // 由View层处理
        }

        /// <summary>
        /// 根据ID管理任务关系
        /// </summary>
        private void ManageRelationshipsById(string taskId)
        {
            // 由View层处理
        }

        /// <summary>
        /// 根据ID显示任务详情
        /// </summary>
        private void ShowDetailById(string taskId)
        {
            // 由View层处理
        }
    }
}
