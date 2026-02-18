using FxxkDDL.Core.Interfaces;
using FxxkDDL.Models;
using Newtonsoft.Json;
using System.IO;
using System;

namespace FxxkDDL.Services
{
    public class ConfigService : IConfigService
    {
        private static readonly string ConfigFilePath = "config.json";
        private AppConfig _config;

        public ConfigService()
        {
            LoadConfig();
        }

        public AppConfig GetConfig()
        {
            return _config;
        }

        /// <summary>
        /// 重新从文件加载配置（用于获取其他进程/服务修改的最新配置）
        /// </summary>
        public void ReloadConfig()
        {
            LoadConfig();
        }

        public void SaveConfig()
        {
            try
            {
                string json = JsonConvert.SerializeObject(_config, Formatting.Indented);
                File.WriteAllText(ConfigFilePath, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"保存配置失败: {ex.Message}");
            }
        }

        public void UpdateApiKey(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("API密钥不能为空");

            if (!apiKey.StartsWith("sk-"))
                throw new ArgumentException("API密钥格式不正确，应以'sk-'开头");

            _config.DeepSeekApiKey = apiKey;
            SaveConfig();
        }

        public void UpdateConfig(AppConfig newConfig)
        {
            _config = newConfig;
            SaveConfig();
        }

        private void LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    _config = JsonConvert.DeserializeObject<AppConfig>(json);

                    if (_config == null)
                    {
                        _config = CreateDefaultConfig();
                        SaveConfig();
                    }
                }
                else
                {
                    _config = CreateDefaultConfig();
                    SaveConfig();
                }
            }
            catch
            {
                _config = CreateDefaultConfig();
            }
        }

        private AppConfig CreateDefaultConfig()
        {
            return new AppConfig
            {
                DeepSeekApiKey = "",
                Model = "deepseek-chat",
                MaxTokens = 2000,
                Temperature = 0.3,
                AutoSave = true,
                Theme = "light",
                NotificationsEnabled = true,
                RemindBeforeDeadline = true,
                RemindDaysBefore = 1,
                RemindHoursBefore = 3
            };
        }

        public bool HasApiKey()
        {
            return !string.IsNullOrWhiteSpace(_config.DeepSeekApiKey);
        }

        public string GetMaskedApiKey()
        {
            if (string.IsNullOrWhiteSpace(_config.DeepSeekApiKey))
                return "未配置";

            if (_config.DeepSeekApiKey.Length <= 8)
                return "***";

            return _config.DeepSeekApiKey.Substring(0, 4) +
                   "..." +
                   _config.DeepSeekApiKey.Substring(_config.DeepSeekApiKey.Length - 4);
        }
    }
}
