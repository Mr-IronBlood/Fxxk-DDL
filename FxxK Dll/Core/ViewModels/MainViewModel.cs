using FxxkDDL.Core.Common;
using FxxkDDL.Core.Navigation;
using System;
using System.Windows.Input;

namespace FxxkDDL.Core.ViewModels
{
    /// <summary>
    /// 主窗口ViewModel
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly NavigationService _navigationService;
        private string _windowTitle;
        private string _statusMessage;
        private string _currentTime;

        /// <summary>
        /// 窗口标题
        /// </summary>
        public string WindowTitle
        {
            get => _windowTitle;
            private set => SetProperty(ref _windowTitle, value);
        }

        /// <summary>
        /// 状态消息
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// 当前时间
        /// </summary>
        public string CurrentTime
        {
            get => _currentTime;
            private set => SetProperty(ref _currentTime, value);
        }

        /// <summary>
        /// 当前显示的内容
        /// </summary>
        public object CurrentContent => _navigationService.CurrentContent;

        /// <summary>
        /// 是否可以后退
        /// </summary>
        public bool CanGoBack => _navigationService.CanGoBack;

        /// <summary>
        /// 导航到输入页面命令
        /// </summary>
        public ICommand NavigateToInputCommand { get; }

        /// <summary>
        /// 导航到日历页面命令
        /// </summary>
        public ICommand NavigateToCalendarCommand { get; }

        /// <summary>
        /// 导航到任务页面命令
        /// </summary>
        public ICommand NavigateToTasksCommand { get; }

        /// <summary>
        /// 导航到设置页面命令
        /// </summary>
        public ICommand NavigateToSettingsCommand { get; }

        /// <summary>
        /// 后退命令
        /// </summary>
        public ICommand GoBackCommand { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public MainViewModel()
        {
            _navigationService = NavigationService.Instance;
            _navigationService.PropertyChanged += OnNavigationServicePropertyChanged;
            _navigationService.Navigated += OnNavigated;

            // 初始化命令
            NavigateToInputCommand = new RelayCommand(() => NavigateTo(NavigationTarget.Input));
            NavigateToCalendarCommand = new RelayCommand(() => NavigateTo(NavigationTarget.Calendar));
            NavigateToTasksCommand = new RelayCommand(() => NavigateTo(NavigationTarget.Tasks));
            NavigateToSettingsCommand = new RelayCommand(() => NavigateTo(NavigationTarget.Settings));
            GoBackCommand = new RelayCommand(GoBack, () => CanGoBack);

            // 初始化属性
            WindowTitle = "Fxxk DDL - 截止日期智能管理器";
            StatusMessage = "就绪";
            CurrentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // 启动时钟
            StartClock();

            // 初始导航到欢迎页面
            NavigateTo(NavigationTarget.Welcome);
        }

        /// <summary>
        /// 导航到指定目标
        /// </summary>
        private void NavigateTo(NavigationTarget target)
        {
            _navigationService.NavigateTo(target);
        }

        /// <summary>
        /// 后退
        /// </summary>
        private void GoBack()
        {
            _navigationService.GoBack();
        }

        /// <summary>
        /// 启动时钟定时器
        /// </summary>
        private void StartClock()
        {
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += (s, e) =>
            {
                CurrentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            };
            timer.Start();
        }

        /// <summary>
        /// 导航服务属性变更事件处理
        /// </summary>
        private void OnNavigationServicePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_navigationService.CurrentContent))
            {
                OnPropertyChanged(nameof(CurrentContent));
            }
            else if (e.PropertyName == nameof(_navigationService.CanGoBack))
            {
                OnPropertyChanged(nameof(CanGoBack));
                ((RelayCommand)GoBackCommand).RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// 导航完成事件处理
        /// </summary>
        private void OnNavigated(object sender, NavigationEventArgs e)
        {
            // 更新状态消息
            StatusMessage = e.Target switch
            {
                NavigationTarget.Welcome => "欢迎使用DDL智能管理器",
                NavigationTarget.Input => "就绪 - 可输入或粘贴聊天记录",
                NavigationTarget.Calendar => "就绪 - 查看和管理DDL日历",
                NavigationTarget.Tasks => "任务管理 - 查看和管理所有DDL",
                NavigationTarget.Settings => "设置页面 - 配置API密钥等",
                _ => "就绪"
            };

            // 更新窗口标题
            var titlePrefix = e.Target switch
            {
                NavigationTarget.Welcome => "👋 欢迎",
                NavigationTarget.Input => "📝 输入聊天记录",
                NavigationTarget.Calendar => "📅 日历视图",
                NavigationTarget.Tasks => "✅ 任务管理",
                NavigationTarget.Settings => "⚙️ 设置",
                _ => "Fxxk DDL"
            };
            WindowTitle = $"{titlePrefix} - 截止日期智能管理器";
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public override void Dispose()
        {
            _navigationService.PropertyChanged -= OnNavigationServicePropertyChanged;
            _navigationService.Navigated -= OnNavigated;
            base.Dispose();
        }
    }
}