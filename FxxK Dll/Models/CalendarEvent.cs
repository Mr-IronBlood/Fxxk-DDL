using System;
using System.Windows.Media;

namespace FxxkDDL.Models
{
    public class CalendarEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DDLTask Task { get; set; }
        public DateTime Date { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsAllDay { get; set; } = true;
        public Color EventColor { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsPinned { get; set; }

        // 根据重要性获取颜色（考虑自定义颜色）
        public Color GetColorByImportance()
        {
            if (Task != null)
            {
                return Task.GetDisplayColor();
            }
            return Color.FromRgb(52, 152, 219); // 默认蓝色
        }

        // 获取透明度颜色（用于线条）
        public Color GetTransparentColor()
        {
            var color = GetColorByImportance();
            return Color.FromArgb(100, color.R, color.G, color.B);
        }
    }
}
