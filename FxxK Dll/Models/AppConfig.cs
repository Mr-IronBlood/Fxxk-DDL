namespace FxxkDDL.Models
{
    public class AppConfig
    {
        // API 配置
        public string DeepSeekApiKey { get; set; } = "";
        public string Model { get; set; } = "deepseek-reasoner";

        // 分析设置
        public int MaxTokens { get; set; } = 1000;
        public double Temperature { get; set; } = 0.3;
        public bool AutoSave { get; set; } = true;

        // 界面设置
        public string Theme { get; set; } = "light"; // light/dark
        public bool NotificationsEnabled { get; set; } = true;

        // 提醒设置
        public bool RemindBeforeDeadline { get; set; } = true;
        public int RemindDaysBefore { get; set; } = 1; // 提前1天提醒
        public int RemindHoursBefore { get; set; } = 3; // 提前3小时提醒

        // 默认构造函数
        public AppConfig()
        {
            // 设置默认值
        }
    }
}
