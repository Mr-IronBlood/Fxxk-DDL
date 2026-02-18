using FxxkDDL.Core.Interfaces;
using FxxkDDL.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FxxkDDL.Services
{
    public class TaskService : ITaskService
    {
        private static readonly string TasksFilePath = "tasks.json";
        private List<DDLTask> _tasks;

        public TaskService()
        {
            LoadTasks();
        }

        public List<DDLTask> GetAllTasks()
        {
            return _tasks.OrderBy(t => t.Deadline ?? DateTime.MaxValue).ToList();
        }

        public List<DDLTask> GetPendingTasks()
        {
            return _tasks
                .Where(t => !t.IsCompleted)
                .OrderBy(t => t.Deadline ?? DateTime.MaxValue)
                .ToList();
        }

        public List<DDLTask> GetCompletedTasks()
        {
            return _tasks
                .Where(t => t.IsCompleted)
                .OrderByDescending(t => t.CompletedAt)
                .ToList();
        }

        /// <summary>
        /// 重新从文件加载任务（用于获取其他进程/服务修改的最新数据）
        /// </summary>
        public void ReloadTasks()
        {
            LoadTasks();
        }

        public void AddTask(DDLTask task)
        {
            _tasks.Add(task);
            SaveTasks();
        }

        public void AddTasks(List<DDLTask> tasks)
        {
            _tasks.AddRange(tasks);
            SaveTasks();
        }
        public DDLTask GetTask(string taskId)
        {
            return _tasks.FirstOrDefault(t => t.Id == taskId);
        }

        public bool MarkAsCompleted(string taskId, bool completed = true)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null)
            {
                task.IsCompleted = completed;
                task.CompletedAt = completed ? DateTime.Now : (DateTime?)null;
                SaveTasks();
                return true;
            }
            return false;
        }

        // 新增：更新任务重要性（改变颜色）
        public bool UpdateImportance(string taskId, string newImportance)
        {
            try
            {
                var task = _tasks.FirstOrDefault(t => t.Id == taskId);
                if (task == null)
                {
                    return false;
                }

                if (newImportance != "高" && newImportance != "中" && newImportance != "低")
                {
                    return false;
                }

                // 更新重要性
                task.Importance = newImportance;
                // 关键：清除自定义颜色，因为现在使用重要性颜色
                task.CustomColor = "";

                SaveTasks();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        // 修改 SetCustomColor 方法
        public bool SetCustomColor(string taskId, string colorHex)
        {
            try
            {
                var task = _tasks.FirstOrDefault(t => t.Id == taskId);
                if (task == null)
                {
                    return false;
                }

                // 关键：只设置 CustomColor，不修改 Importance
                task.CustomColor = colorHex;

                // 保存到文件
                SaveTasks();

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        // 新增：重置为默认颜色（基于重要性）
        public bool ResetToDefaultColor(string taskId)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null)
            {
                task.CustomColor = "";
                SaveTasks();
                return true;
            }
            return false;
        }
        public bool DeleteTask(string taskId)
        {
            try
            {
                var taskToDelete = _tasks.FirstOrDefault(t => t.Id == taskId);
                if (taskToDelete == null)
                {
                    return false;
                }

                // 检查是否可以安全删除
                if (!CanDeleteTaskSafely(taskId))
                {
                    return false;
                }

                // 1. 从父任务的子任务列表中移除
                if (!string.IsNullOrWhiteSpace(taskToDelete.ParentTaskId))
                {
                    var parentTask = _tasks.FirstOrDefault(t => t.Id == taskToDelete.ParentTaskId);
                    if (parentTask != null)
                    {
                        parentTask.SubTaskIds.Remove(taskId);
                    }
                }

                // 2. 从所有依赖此任务的任务中移除依赖关系
                var dependentTasks = _tasks.Where(t => t.DependencyIds.Contains(taskId)).ToList();
                foreach (var dependentTask in dependentTasks)
                {
                    dependentTask.DependencyIds.Remove(taskId);
                }

                // 3. 删除任务本身
                int removedCount = _tasks.RemoveAll(t => t.Id == taskId);
                if (removedCount > 0)
                {
                    SaveTasks();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public int DeleteCompletedTasks()
        {
            try
            {
                // 获取所有已完成且可以安全删除的任务
                var tasksToDelete = _tasks
                    .Where(t => t.IsCompleted && CanDeleteTaskSafely(t.Id))
                    .ToList();

                int count = 0;
                foreach (var task in tasksToDelete)
                {
                    // 使用DeleteTask方法确保正确处理关系
                    if (DeleteTask(task.Id))
                    {
                        count++;
                    }
                }

                return count;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        private void LoadTasks()
        {
            if (File.Exists(TasksFilePath))
            {
                try
                {
                    string json = File.ReadAllText(TasksFilePath);
                    _tasks = JsonConvert.DeserializeObject<List<DDLTask>>(json) ?? new List<DDLTask>();
                }
                catch
                {
                    _tasks = new List<DDLTask>();
                }
            }
            else
            {
                _tasks = new List<DDLTask>();
            }
        }

        private void SaveTasks()
        {
            try
            {
                string json = JsonConvert.SerializeObject(_tasks, Formatting.Indented);
                File.WriteAllText(TasksFilePath, json);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public bool UpdateTask(DDLTask updatedTask)
        {
            var existingTask = _tasks.FirstOrDefault(t => t.Id == updatedTask.Id);
            if (existingTask != null)
            {
                // 更新所有属性
                existingTask.Description = updatedTask.Description;
                existingTask.TaskName = updatedTask.TaskName;
                existingTask.TaskDetail = updatedTask.TaskDetail;
                existingTask.OriginalText = updatedTask.OriginalText;
                existingTask.Deadline = updatedTask.Deadline;
                existingTask.DeadlineString = updatedTask.DeadlineString;
                existingTask.Importance = updatedTask.Importance;
                existingTask.CustomColor = updatedTask.CustomColor;
                existingTask.OriginalContext = updatedTask.OriginalContext;
                existingTask.SourceText = updatedTask.SourceText;

                SaveTasks();
                return true;
            }
            return false;
        }

        // ========== 新增：任务关系管理方法 ==========

        public bool SetParentTask(string taskId, string parentTaskId)
        {
            try
            {
                var childTask = _tasks.FirstOrDefault(t => t.Id == taskId);
                if (childTask == null)
                {
                    return false;
                }

                // 记录旧的父任务ID
                string oldParentId = childTask.ParentTaskId;

                // 更新子任务的父任务
                childTask.ParentTaskId = parentTaskId;
                childTask.IsRootTask = string.IsNullOrWhiteSpace(parentTaskId);

                // 从旧的父任务中移除此子任务
                if (!string.IsNullOrWhiteSpace(oldParentId))
                {
                    var oldParent = _tasks.FirstOrDefault(t => t.Id == oldParentId);
                    if (oldParent != null)
                    {
                        oldParent.SubTaskIds.Remove(taskId);
                    }
                }

                // 添加到新的父任务的子任务列表中
                if (!string.IsNullOrWhiteSpace(parentTaskId))
                {
                    var newParent = _tasks.FirstOrDefault(t => t.Id == parentTaskId);
                    if (newParent == null)
                    {
                        childTask.ParentTaskId = "";
                        childTask.IsRootTask = true;
                        return false;
                    }

                    if (!newParent.SubTaskIds.Contains(taskId))
                    {
                        newParent.SubTaskIds.Add(taskId);
                    }
                }

                SaveTasks();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool AddDependency(string taskId, string dependencyTaskId)
        {
            try
            {
                var task = _tasks.FirstOrDefault(t => t.Id == taskId);
                if (task == null)
                {
                    return false;
                }

                var dependency = _tasks.FirstOrDefault(t => t.Id == dependencyTaskId);
                if (dependency == null)
                {
                    return false;
                }

                // 检查循环依赖
                if (CheckCircularDependency(taskId, dependencyTaskId))
                {
                    return false;
                }

                if (!task.DependencyIds.Contains(dependencyTaskId))
                {
                    task.DependencyIds.Add(dependencyTaskId);
                    SaveTasks();
                    return true;
                }
                else
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool RemoveDependency(string taskId, string dependencyTaskId)
        {
            try
            {
                var task = _tasks.FirstOrDefault(t => t.Id == taskId);
                if (task == null)
                {
                    return false;
                }

                bool removed = task.DependencyIds.Remove(dependencyTaskId);
                if (removed)
                {
                    SaveTasks();
                }

                return removed;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public List<DDLTask> GetSubTasks(string taskId)
        {
            try
            {
                var parentTask = _tasks.FirstOrDefault(t => t.Id == taskId);
                if (parentTask == null)
                {
                    return new List<DDLTask>();
                }

                var subTasks = _tasks
                    .Where(t => parentTask.SubTaskIds.Contains(t.Id))
                    .OrderBy(t => t.TaskOrder)
                    .ToList();

                return subTasks;
            }
            catch (Exception ex)
            {
                return new List<DDLTask>();
            }
        }

        public List<DDLTask> GetDependencies(string taskId)
        {
            try
            {
                var task = _tasks.FirstOrDefault(t => t.Id == taskId);
                if (task == null)
                {
                    return new List<DDLTask>();
                }

                var dependencies = _tasks
                    .Where(t => task.DependencyIds.Contains(t.Id))
                    .ToList();

                return dependencies;
            }
            catch (Exception ex)
            {
                return new List<DDLTask>();
            }
        }

        public DDLTask GetParentTask(string taskId)
        {
            try
            {
                var task = _tasks.FirstOrDefault(t => t.Id == taskId);
                if (task == null || string.IsNullOrWhiteSpace(task.ParentTaskId))
                {
                    return null;
                }

                var parent = _tasks.FirstOrDefault(t => t.Id == task.ParentTaskId);
                return parent;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public List<DDLTask> GetRootTasks()
        {
            try
            {
                var rootTasks = _tasks
                    .Where(t => t.IsRootTask)
                    .OrderBy(t => t.Deadline ?? DateTime.MaxValue)
                    .ToList();

                return rootTasks;
            }
            catch (Exception ex)
            {
                return new List<DDLTask>();
            }
        }

        public bool UpdateSubTaskOrder(string parentTaskId, List<string> subTaskIds)
        {
            try
            {
                var parentTask = _tasks.FirstOrDefault(t => t.Id == parentTaskId);
                if (parentTask == null)
                {
                    return false;
                }

                // 验证所有子任务ID都存在且是父任务的子任务
                foreach (var subTaskId in subTaskIds)
                {
                    var subTask = _tasks.FirstOrDefault(t => t.Id == subTaskId);
                    if (subTask == null)
                    {
                        return false;
                    }

                    if (subTask.ParentTaskId != parentTaskId)
                    {
                        return false;
                    }
                }

                // 更新父任务的子任务列表
                parentTask.SubTaskIds = subTaskIds;

                // 更新每个子任务的TaskOrder
                for (int i = 0; i < subTaskIds.Count; i++)
                {
                    var subTask = _tasks.FirstOrDefault(t => t.Id == subTaskIds[i]);
                    if (subTask != null)
                    {
                        subTask.TaskOrder = i;
                    }
                }

                SaveTasks();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool CanDeleteTaskSafely(string taskId)
        {
            try
            {
                var task = _tasks.FirstOrDefault(t => t.Id == taskId);
                if (task == null)
                {
                    return true; // 不存在的任务可以被"删除"
                }

                // 检查是否有子任务
                if (task.SubTaskIds.Count > 0)
                {
                    return false;
                }

                // 检查是否有其他任务依赖此任务
                var dependentTasks = _tasks.Where(t => t.DependencyIds.Contains(taskId)).ToList();
                if (dependentTasks.Count > 0)
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        // ========== 私有辅助方法 ==========

        private bool CheckCircularDependency(string taskId, string dependencyTaskId)
        {
            // 简单的循环依赖检查：如果依赖的任务又依赖于当前任务，则形成循环
            var dependencyTask = _tasks.FirstOrDefault(t => t.Id == dependencyTaskId);
            if (dependencyTask == null) return false;

            // 检查依赖任务是否直接或间接依赖于当前任务
            var visited = new HashSet<string>();
            var stack = new Stack<string>();
            stack.Push(dependencyTaskId);

            while (stack.Count > 0)
            {
                var currentId = stack.Pop();
                if (currentId == taskId)
                {
                    return true; // 发现循环依赖
                }

                if (visited.Contains(currentId)) continue;
                visited.Add(currentId);

                var currentTask = _tasks.FirstOrDefault(t => t.Id == currentId);
                if (currentTask != null)
                {
                    foreach (var depId in currentTask.DependencyIds)
                    {
                        stack.Push(depId);
                    }
                }
            }

            return false;
        }

    }
}
