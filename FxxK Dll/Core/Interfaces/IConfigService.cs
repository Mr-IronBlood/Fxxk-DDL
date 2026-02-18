using FxxkDDL.Models;

namespace FxxkDDL.Core.Interfaces
{
    /// <summary>
    /// 配置管理服务接口
    /// </summary>
    public interface IConfigService
    {
        /// <summary>
        /// 获取当前配置
        /// </summary>
        /// <returns>应用程序配置</returns>
        AppConfig GetConfig();

        /// <summary>
        /// 保存配置
        /// </summary>
        void SaveConfig();

        /// <summary>
        /// 更新API密钥
        /// </summary>
        /// <param name="apiKey">DeepSeek API密钥</param>
        /// <exception cref="ArgumentException">API密钥为空或格式不正确时抛出</exception>
        void UpdateApiKey(string apiKey);

        /// <summary>
        /// 更新整个配置
        /// </summary>
        /// <param name="newConfig">新的配置对象</param>
        void UpdateConfig(AppConfig newConfig);

        /// <summary>
        /// 检查是否已配置API密钥
        /// </summary>
        /// <returns>如果已配置API密钥返回true</returns>
        bool HasApiKey();

        /// <summary>
        /// 获取掩码后的API密钥（用于安全显示）
        /// </summary>
        /// <returns>掩码后的API密钥字符串</returns>
        string GetMaskedApiKey();
    }
}