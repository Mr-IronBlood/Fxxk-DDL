using FxxkDDL.Core.Common;
using FxxkDDL.Core.Interfaces;
using FxxkDDL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FxxkDDL.Views
{
    /// <summary>
    /// 任务关系管理对话框
    /// </summary>
    public partial class TaskRelationshipDialog : Window
    {
        private readonly ITaskService _taskService;
        private readonly string _taskId;
        private DDLTask _currentTask;
        private List<DDLTask> _availableTasks;
        private List<DDLTask> _availableForParent;
        private List<DDLTask> _availableForDependency;
        private List<DDLTask> _availableForSubTask;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="taskId">要管理关系的任务ID</param>
        public TaskRelationshipDialog(string taskId)
        {
            InitializeComponent();

            _taskService = ServiceLocator.GetService<ITaskService>();
            _taskId = taskId;

            Loaded += TaskRelationshipDialog_Loaded;
            InitializeDialog();
        }

        private void TaskRelationshipDialog_Loaded(object sender, RoutedEventArgs e)
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
            try
            {
                // 获取当前任务
                _currentTask = _taskService.GetTask(_taskId);
                if (_currentTask == null)
                {
                    MessageBox.Show("任务不存在或已被删除", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }

                // 更新任务信息显示
                TxtTaskInfo.Text = $"{_currentTask.Description}";
                UpdateRelationsDisplay();

                // 加载所有可用任务
                LoadAvailableTasks();

                // 加载当前关系
                LoadCurrentRelations();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        /// <summary>
        /// 加载所有可用任务
        /// </summary>
        private void LoadAvailableTasks()
        {
            try
            {
                _availableTasks = _taskService.GetAllTasks();

                // 过滤可用的任务：排除当前任务本身
                _availableForParent = _availableTasks
                    .Where(t => t.Id != _taskId && !IsTaskInHierarchy(t.Id, _taskId))
                    .ToList();

                _availableForDependency = _availableTasks
                    .Where(t => t.Id != _taskId)
                    .ToList();

                _availableForSubTask = _availableTasks
                    .Where(t => t.Id != _taskId && !IsTaskInHierarchy(_taskId, t.Id))
                    .ToList();

                // 绑定到下拉框
                CmbParentTasks.ItemsSource = _availableForParent;
                CmbAvailableTasks.ItemsSource = _availableForDependency;
                CmbAvailableForSubTasks.ItemsSource = _availableForSubTask;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载任务列表失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 检查任务是否在指定任务的层级结构中
        /// </summary>
        private bool IsTaskInHierarchy(string parentTaskId, string childTaskId)
        {
            var task = _taskService.GetTask(childTaskId);
            if (task == null) return false;

            // 检查是否是直接的父任务
            if (task.ParentTaskId == parentTaskId) return true;

            // 递归检查祖先
            if (!string.IsNullOrWhiteSpace(task.ParentTaskId))
            {
                return IsTaskInHierarchy(parentTaskId, task.ParentTaskId);
            }

            return false;
        }

        /// <summary>
        /// 加载当前关系
        /// </summary>
        private void LoadCurrentRelations()
        {
            try
            {
                // 加载父任务
                var parentTask = _taskService.GetParentTask(_taskId);
                if (parentTask != null)
                {
                    TxtCurrentParent.Text = parentTask.Description;
                }
                else
                {
                    TxtCurrentParent.Text = "无";
                    BtnRemoveParent.IsEnabled = false;
                }

                // 加载依赖任务
                var dependencies = _taskService.GetDependencies(_taskId);
                LstDependencies.ItemsSource = dependencies;

                // 加载子任务
                var subTasks = _taskService.GetSubTasks(_taskId);
                LstSubTasks.ItemsSource = subTasks;

                // 更新按钮状态
                UpdateButtonStates();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载关系失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 更新关系显示文本
        /// </summary>
        private void UpdateRelationsDisplay()
        {
            try
            {
                var parentTask = _taskService.GetParentTask(_taskId);
                var dependencies = _taskService.GetDependencies(_taskId);
                var subTasks = _taskService.GetSubTasks(_taskId);

                List<string> relations = new List<string>();

                if (parentTask != null)
                    relations.Add($"父任务: {parentTask.Description}");

                if (dependencies.Count > 0)
                    relations.Add($"依赖任务: {dependencies.Count}个");

                if (subTasks.Count > 0)
                    relations.Add($"子任务: {subTasks.Count}个");

                if (relations.Count == 0)
                    relations.Add("无");

                TxtTaskRelations.Text = $"关系: {string.Join(", ", relations)}";
            }
            catch (Exception ex)
            {
                // Silently handle errors
            }
        }

        /// <summary>
        /// 更新按钮状态
        /// </summary>
        private void UpdateButtonStates()
        {
            var subTasks = _taskService.GetSubTasks(_taskId);
            var selectedSubTask = LstSubTasks.SelectedItem as DDLTask;

            BtnMoveUp.IsEnabled = selectedSubTask != null && selectedSubTask.TaskOrder > 0;
            BtnMoveDown.IsEnabled = selectedSubTask != null && selectedSubTask.TaskOrder < subTasks.Count - 1;
        }

        /// <summary>
        /// 移除父任务按钮点击事件
        /// </summary>
        private void BtnRemoveParent_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("确定要移除父任务关系吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    bool success = _taskService.SetParentTask(_taskId, "");
                    if (success)
                    {
                        TxtCurrentParent.Text = "无";
                        BtnRemoveParent.IsEnabled = false;
                        UpdateRelationsDisplay();
                        MessageBox.Show("父任务关系已移除", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("移除父任务失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"移除父任务失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 设置父任务按钮点击事件
        /// </summary>
        private void BtnSetParent_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedTask = CmbParentTasks.SelectedItem as DDLTask;
                if (selectedTask == null)
                {
                    MessageBox.Show("请选择一个任务作为父任务", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                bool success = _taskService.SetParentTask(_taskId, selectedTask.Id);
                if (success)
                {
                    TxtCurrentParent.Text = selectedTask.Description;
                    BtnRemoveParent.IsEnabled = true;
                    UpdateRelationsDisplay();
                    LoadAvailableTasks(); // 重新加载可用任务列表
                    MessageBox.Show("父任务设置成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("设置父任务失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"设置父任务失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 添加依赖按钮点击事件
        /// </summary>
        private void BtnAddDependency_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedTask = CmbAvailableTasks.SelectedItem as DDLTask;
                if (selectedTask == null)
                {
                    MessageBox.Show("请选择一个任务作为依赖", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                bool success = _taskService.AddDependency(_taskId, selectedTask.Id);
                if (success)
                {
                    LoadCurrentRelations();
                    UpdateRelationsDisplay();
                    MessageBox.Show("依赖关系添加成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("添加依赖关系失败（可能是循环依赖）", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加依赖失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 移除依赖按钮点击事件
        /// </summary>
        private void BtnRemoveDependency_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button?.Tag is string dependencyId)
                {
                    bool success = _taskService.RemoveDependency(_taskId, dependencyId);
                    if (success)
                    {
                        LoadCurrentRelations();
                        UpdateRelationsDisplay();
                        MessageBox.Show("依赖关系已移除", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("移除依赖关系失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"移除依赖失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 添加子任务按钮点击事件
        /// </summary>
        private void BtnAddSubTask_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedTask = CmbAvailableForSubTasks.SelectedItem as DDLTask;
                if (selectedTask == null)
                {
                    MessageBox.Show("请选择一个任务作为子任务", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                bool success = _taskService.SetParentTask(selectedTask.Id, _taskId);
                if (success)
                {
                    LoadCurrentRelations();
                    UpdateRelationsDisplay();
                    LoadAvailableTasks(); // 重新加载可用任务列表
                    MessageBox.Show("子任务添加成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("添加子任务失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加子任务失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 移除子任务按钮点击事件
        /// </summary>
        private void BtnRemoveSubTask_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button?.Tag is string subTaskId)
                {
                    var result = MessageBox.Show("确定要移除这个子任务吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        bool success = _taskService.SetParentTask(subTaskId, "");
                        if (success)
                        {
                            LoadCurrentRelations();
                            UpdateRelationsDisplay();
                            LoadAvailableTasks(); // 重新加载可用任务列表
                            MessageBox.Show("子任务已移除", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("移除子任务失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"移除子任务失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 上移子任务按钮点击事件
        /// </summary>
        private void BtnMoveUp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedTask = LstSubTasks.SelectedItem as DDLTask;
                if (selectedTask == null || selectedTask.TaskOrder <= 0) return;

                var subTasks = _taskService.GetSubTasks(_taskId);
                var newOrder = new List<string>();

                for (int i = 0; i < subTasks.Count; i++)
                {
                    if (i == selectedTask.TaskOrder - 1)
                    {
                        newOrder.Add(selectedTask.Id);
                    }
                    else if (i == selectedTask.TaskOrder)
                    {
                        newOrder.Add(subTasks[i - 1].Id);
                    }
                    else
                    {
                        newOrder.Add(subTasks[i].Id);
                    }
                }

                bool success = _taskService.UpdateSubTaskOrder(_taskId, newOrder);
                if (success)
                {
                    LoadCurrentRelations();
                    UpdateButtonStates();
                    MessageBox.Show("子任务顺序已调整", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"调整顺序失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 下移子任务按钮点击事件
        /// </summary>
        private void BtnMoveDown_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedTask = LstSubTasks.SelectedItem as DDLTask;
                var subTasks = _taskService.GetSubTasks(_taskId);
                if (selectedTask == null || selectedTask.TaskOrder >= subTasks.Count - 1) return;

                var newOrder = new List<string>();

                for (int i = 0; i < subTasks.Count; i++)
                {
                    if (i == selectedTask.TaskOrder)
                    {
                        newOrder.Add(subTasks[i + 1].Id);
                    }
                    else if (i == selectedTask.TaskOrder + 1)
                    {
                        newOrder.Add(selectedTask.Id);
                    }
                    else
                    {
                        newOrder.Add(subTasks[i].Id);
                    }
                }

                bool success = _taskService.UpdateSubTaskOrder(_taskId, newOrder);
                if (success)
                {
                    LoadCurrentRelations();
                    UpdateButtonStates();
                    MessageBox.Show("子任务顺序已调整", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"调整顺序失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 子任务选择变化事件
        /// </summary>
        private void LstSubTasks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtonStates();
        }

        /// <summary>
        /// 保存更改按钮点击事件
        /// </summary>
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 所有更改都是实时保存的，这里只是提示
                MessageBox.Show("所有更改已自动保存", "信息", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 关闭按钮点击事件
        /// </summary>
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}