using System;
using System.Windows.Media;

namespace FxxkDDL.Models
{
    public class DDLTask
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // 新的数据结构
        public string TaskName { get; set; } = "";           // AI提炼的任务名称（简短）
        public string TaskDetail { get; set; } = "";          // AI总结概括的任务详情（包含所有要点）
        public string OriginalText { get; set; } = "";         // 完整的原文内容

        // 保留旧字段以兼容（标记为过时）
        [Obsolete("请使用TaskName替代")]
        public string Description { get => TaskName; set => TaskName = value; }

        public DateTime? Deadline { get; set; }
        public string DeadlineString { get; set; } = "";
        public string Importance { get; set; } = "中"; // 高/中/低

        [Obsolete("请使用OriginalText替代")]
        public string SourceText { get; set; } = "";

        [Obsolete("请使用TaskDetail或OriginalText替代")]
        public string OriginalContext { get; set; } = "";

        public double Confidence { get; set; } = 0.8;
        public bool IsCompleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }

        // 新增：自定义颜色（如果用户想覆盖重要性颜色）
        public string CustomColor { get; set; } = ""; // 格式：#RRGGBB

        // 新增：任务关系字段（支持子任务、母任务、依赖关系）
        public string ParentTaskId { get; set; } = "";          // 父任务ID（母任务）
        public List<string> SubTaskIds { get; set; } = new();   // 子任务ID列表
        public List<string> DependencyIds { get; set; } = new(); // 依赖任务ID列表（前置任务）
        public int TaskOrder { get; set; } = 0;                 // 任务排序（用于子任务排序）
        public bool IsRootTask { get; set; } = true;           // 是否为根任务（无父任务）

        // 新增：获取实际显示的颜色
        public Color GetDisplayColor()
        {
            // 1. 绝对优先使用自定义颜色（如果有）
            if (!string.IsNullOrWhiteSpace(CustomColor))
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(CustomColor);
                    return color;
                }
                catch (Exception ex)
                {
                    // 继续使用重要性颜色
                }
            }

            // 2. 根据重要性返回颜色（使用更标准的红黄绿）
            Color result = Importance switch
            {
                "高" => Color.FromRgb(255, 59, 48),    // 标准红色
                "中" => Color.FromRgb(255, 204, 0),    // 标准黄色
                "低" => Color.FromRgb(76, 217, 100),   // 标准绿色
                _ => Color.FromRgb(52, 152, 219)       // 蓝色
            };

            return result;
        }



        // 新增：获取透明度颜色（用于已完成的半透明效果）
        public Color GetCompletedColor()
        {
            var color = GetDisplayColor();
            return Color.FromArgb(128, color.R, color.G, color.B); // 50%透明度
        }

        // 解析截止时间字符串
        public void ParseDeadline()
        {
            if (!string.IsNullOrWhiteSpace(DeadlineString))
            {
                if (DateTime.TryParse(DeadlineString, out var date))
                {
                    Deadline = date;
                }
            }
        }
    }
}
