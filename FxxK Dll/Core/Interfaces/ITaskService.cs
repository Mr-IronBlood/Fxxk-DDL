using FxxkDDL.Models;
using System.Collections.Generic;

namespace FxxkDDL.Core.Interfaces
{
    /// <summary>
    /// 任务管理服务接口
    /// </summary>
    public interface ITaskService
    {
        /// <summary>
        /// 获取所有任务
        /// </summary>
        /// <returns>按截止时间排序的任务列表</returns>
        List<DDLTask> GetAllTasks();

        /// <summary>
        /// 获取未完成的任务
        /// </summary>
        /// <returns>按截止时间排序的未完成任务列表</returns>
        List<DDLTask> GetPendingTasks();

        /// <summary>
        /// 获取已完成的任务
        /// </summary>
        /// <returns>按完成时间倒序排列的已完成任务列表</returns>
        List<DDLTask> GetCompletedTasks();

        /// <summary>
        /// 根据ID获取任务
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns>找到的任务，如果不存在返回null</returns>
        DDLTask GetTask(string taskId);

        /// <summary>
        /// 添加单个任务
        /// </summary>
        /// <param name="task">要添加的任务</param>
        void AddTask(DDLTask task);

        /// <summary>
        /// 批量添加任务
        /// </summary>
        /// <param name="tasks">要添加的任务列表</param>
        void AddTasks(List<DDLTask> tasks);

        /// <summary>
        /// 更新任务
        /// </summary>
        /// <param name="updatedTask">更新后的任务对象</param>
        /// <returns>是否更新成功</returns>
        bool UpdateTask(DDLTask updatedTask);

        /// <summary>
        /// 标记任务为完成/未完成
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <param name="completed">完成状态，true为完成</param>
        /// <returns>是否成功</returns>
        bool MarkAsCompleted(string taskId, bool completed = true);

        /// <summary>
        /// 更新任务重要性
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <param name="newImportance">新重要性（高/中/低）</param>
        /// <returns>是否成功</returns>
        bool UpdateImportance(string taskId, string newImportance);

        /// <summary>
        /// 设置任务自定义颜色
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <param name="colorHex">颜色Hex值（格式：#RRGGBB）</param>
        /// <returns>是否成功</returns>
        bool SetCustomColor(string taskId, string colorHex);

        /// <summary>
        /// 重置任务颜色为默认（基于重要性）
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns>是否成功</returns>
        bool ResetToDefaultColor(string taskId);

        /// <summary>
        /// 删除任务
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns>是否成功</returns>
        bool DeleteTask(string taskId);

        /// <summary>
        /// 删除所有已完成的任务
        /// </summary>
        /// <returns>删除的任务数量</returns>
        int DeleteCompletedTasks();

        // ========== 新增：任务关系管理方法 ==========

        /// <summary>
        /// 设置任务的父任务（建立母任务-子任务关系）
        /// </summary>
        /// <param name="taskId">子任务ID</param>
        /// <param name="parentTaskId">父任务ID（设置为空字符串可移除父任务）</param>
        /// <returns>是否成功</returns>
        bool SetParentTask(string taskId, string parentTaskId);

        /// <summary>
        /// 添加任务依赖关系（前置任务）
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <param name="dependencyTaskId">依赖的任务ID</param>
        /// <returns>是否成功</returns>
        bool AddDependency(string taskId, string dependencyTaskId);

        /// <summary>
        /// 移除任务依赖关系
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <param name="dependencyTaskId">要移除的依赖任务ID</param>
        /// <returns>是否成功</returns>
        bool RemoveDependency(string taskId, string dependencyTaskId);

        /// <summary>
        /// 获取任务的子任务列表
        /// </summary>
        /// <param name="taskId">父任务ID</param>
        /// <returns>子任务列表</returns>
        List<DDLTask> GetSubTasks(string taskId);

        /// <summary>
        /// 获取任务的依赖任务列表（前置任务）
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns>依赖任务列表</returns>
        List<DDLTask> GetDependencies(string taskId);

        /// <summary>
        /// 获取任务的父任务
        /// </summary>
        /// <param name="taskId">子任务ID</param>
        /// <returns>父任务，如果不存在返回null</returns>
        DDLTask GetParentTask(string taskId);

        /// <summary>
        /// 获取所有根任务（没有父任务的任务）
        /// </summary>
        /// <returns>根任务列表</returns>
        List<DDLTask> GetRootTasks();

        /// <summary>
        /// 更新子任务排序
        /// </summary>
        /// <param name="parentTaskId">父任务ID</param>
        /// <param name="subTaskIds">重新排序后的子任务ID列表</param>
        /// <returns>是否成功</returns>
        bool UpdateSubTaskOrder(string parentTaskId, List<string> subTaskIds);

        /// <summary>
        /// 检查任务删除是否会影响其他任务关系
        /// </summary>
        /// <param name="taskId">要检查的任务ID</param>
        /// <returns>是否可以被安全删除（没有子任务依赖）</returns>
        bool CanDeleteTaskSafely(string taskId);
    }
}