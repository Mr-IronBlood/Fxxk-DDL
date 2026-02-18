using FxxkDDL.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FxxkDDL.Core.Interfaces
{
    /// <summary>
    /// DeepSeek AI服务接口
    /// </summary>
    public interface IDeepSeekService
    {
        /// <summary>
        /// 测试API连接是否正常
        /// </summary>
        /// <returns>(是否成功, 消息)</returns>
        Task<(bool Success, string Message)> TestApiConnectionAsync();

        /// <summary>
        /// 从聊天记录中提取DDL任务
        /// </summary>
        /// <param name="chatText">聊天记录文本</param>
        /// <returns>(是否成功, 提取的任务列表, 消息)</returns>
        Task<(bool Success, List<DDLTask> Tasks, string Message)> ExtractDDLFromTextAsync(string chatText);

        /// <summary>
        /// 获取API状态信息
        /// </summary>
        /// <returns>API状态字符串</returns>
        string GetApiStatus();

        /// <summary>
        /// 从文件（PDF、Word、PPT等）中提取DDL任务
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>(是否成功, 提取的任务列表, 消息)</returns>
        Task<(bool Success, List<DDLTask> Tasks, string Message)> ExtractDDLFromFileAsync(string filePath);
    }
}