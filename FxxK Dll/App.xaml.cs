using FxxkDDL.Core.Common;
using FxxkDDL.Core.Interfaces;
using FxxkDDL.Core.Navigation;
using System.Windows;

namespace FxxkDDL
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            InitializeServices();
        }

        /// <summary>
        /// 初始化服务注册
        /// </summary>
        private void InitializeServices()
        {
            // 注册服务
            ServiceLocator.Register<ITaskService, Services.TaskService>();
            ServiceLocator.Register<IConfigService, Services.ConfigService>();
            ServiceLocator.Register<IDeepSeekService, Services.DeepSeekService>();
            ServiceLocator.Register<ICalendarService, Services.CalendarService>();
            ServiceLocator.Register<IViewFactory, DefaultViewFactory>();

            // 初始化导航服务（单例会自动创建）
            var navigationService = NavigationService.Instance;
        }
    }
}
