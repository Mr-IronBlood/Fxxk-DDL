using System;
using System.Windows;

namespace FxxkDDL.Core.Navigation
{
    /// <summary>
    /// 默认视图工厂实现
    /// </summary>
    public class DefaultViewFactory : IViewFactory
    {
        /// <summary>
        /// 根据导航目标创建视图
        /// </summary>
        public object CreateView(NavigationTarget target)
        {
            try
            {
                return target switch
                {
                    NavigationTarget.Welcome => CreateWelcomePage(),
                    NavigationTarget.Input => CreateInputPage(),
                    NavigationTarget.Calendar => CreateCalendarPage(),
                    NavigationTarget.Tasks => CreateTasksPage(),
                    NavigationTarget.Settings => CreateSettingsPage(),
                    _ => CreateErrorPage($"未知的导航目标: {target}")
                };
            }
            catch (Exception ex)
            {
                return CreateErrorPage($"创建页面失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建输入页面
        /// </summary>
        private object CreateInputPage()
        {
            return new Views.InputPage();
        }

        /// <summary>
        /// 创建日历页面
        /// </summary>
        private object CreateCalendarPage()
        {
            return new Views.CalendarPage();
        }

        /// <summary>
        /// 创建任务页面
        /// </summary>
        private object CreateTasksPage()
        {
            return new Views.TasksPage();
        }

        /// <summary>
        /// 创建设置页面
        /// </summary>
        private object CreateSettingsPage()
        {
            return new Views.SettingsPage();
        }

        /// <summary>
        /// 创建欢迎页面
        /// </summary>
        private object CreateWelcomePage()
        {
            return new Views.WelcomePage();
        }

        /// <summary>
        /// 创建错误页面
        /// </summary>
        private object CreateErrorPage(string message)
        {
            return new System.Windows.Controls.TextBlock
            {
                Text = message,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Foreground = System.Windows.Media.Brushes.Red,
                FontSize = 14
            };
        }
    }
}