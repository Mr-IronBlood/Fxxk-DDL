using FxxkDDL.Core.Common;
using System;
using System.Collections.Generic;
using System.Windows;

namespace FxxkDDL.Core.Navigation
{
    /// <summary>
    /// 导航目标类型
    /// </summary>
    public enum NavigationTarget
    {
        /// <summary>
        /// 输入页面
        /// </summary>
        Input,

        /// <summary>
        /// 日历页面
        /// </summary>
        Calendar,

        /// <summary>
        /// 任务管理页面
        /// </summary>
        Tasks,

        /// <summary>
        /// 设置页面
        /// </summary>
        Settings,

        /// <summary>
        /// 欢迎页面
        /// </summary>
        Welcome
    }

    /// <summary>
    /// 导航事件参数
    /// </summary>
    public class NavigationEventArgs : EventArgs
    {
        /// <summary>
        /// 导航目标
        /// </summary>
        public NavigationTarget Target { get; }

        /// <summary>
        /// 导航参数（可选）
        /// </summary>
        public object Parameter { get; }

        /// <summary>
        /// 初始化导航事件参数
        /// </summary>
        /// <param name="target">导航目标</param>
        /// <param name="parameter">导航参数</param>
        public NavigationEventArgs(NavigationTarget target, object parameter = null)
        {
            Target = target;
            Parameter = parameter;
        }
    }

    /// <summary>
    /// 导航服务，管理应用程序页面导航
    /// </summary>
    public class NavigationService : ObservableObject
    {
        private static NavigationService _instance;
        private object _currentContent;
        private NavigationTarget _currentTarget;
        private readonly Stack<NavigationTarget> _navigationStack = new Stack<NavigationTarget>();

        /// <summary>
        /// 单例实例
        /// </summary>
        public static NavigationService Instance => _instance ??= new NavigationService();

        /// <summary>
        /// 当前显示的内容
        /// </summary>
        public object CurrentContent
        {
            get => _currentContent;
            private set => SetProperty(ref _currentContent, value);
        }

        /// <summary>
        /// 当前导航目标
        /// </summary>
        public NavigationTarget CurrentTarget
        {
            get => _currentTarget;
            private set => SetProperty(ref _currentTarget, value);
        }

        /// <summary>
        /// 是否可以后退
        /// </summary>
        public bool CanGoBack => _navigationStack.Count > 0;

        /// <summary>
        /// 导航事件
        /// </summary>
        public event EventHandler<NavigationEventArgs> Navigating;

        /// <summary>
        /// 导航完成事件
        /// </summary>
        public event EventHandler<NavigationEventArgs> Navigated;

        private NavigationService()
        {
            // 私有构造函数确保单例
        }

        /// <summary>
        /// 导航到指定目标
        /// </summary>
        /// <param name="target">导航目标</param>
        /// <param name="parameter">导航参数</param>
        public void NavigateTo(NavigationTarget target, object parameter = null)
        {
            var args = new NavigationEventArgs(target, parameter);
            OnNavigating(args);

            // 保存当前导航状态
            if (CurrentTarget != NavigationTarget.Input && CurrentTarget != NavigationTarget.Welcome) // 输入页面和欢迎页面不加入后退栈
            {
                _navigationStack.Push(CurrentTarget);
            }

            // 创建新的内容
            CurrentContent = CreateContentForTarget(target);
            CurrentTarget = target;

            OnPropertyChanged(nameof(CanGoBack));
            OnNavigated(args);
        }

        /// <summary>
        /// 导航到指定类型的内容
        /// </summary>
        /// <param name="content">要显示的内容</param>
        /// <param name="target">关联的导航目标</param>
        public void NavigateToContent(object content, NavigationTarget target)
        {
            var args = new NavigationEventArgs(target);
            OnNavigating(args);

            if (CurrentTarget != NavigationTarget.Input && CurrentTarget != NavigationTarget.Welcome)
            {
                _navigationStack.Push(CurrentTarget);
            }

            CurrentContent = content;
            CurrentTarget = target;

            OnPropertyChanged(nameof(CanGoBack));
            OnNavigated(args);
        }

        /// <summary>
        /// 后退到上一个页面
        /// </summary>
        public void GoBack()
        {
            if (_navigationStack.Count == 0) return;

            var previousTarget = _navigationStack.Pop();
            NavigateTo(previousTarget);
        }

        /// <summary>
        /// 清除导航历史
        /// </summary>
        public void ClearHistory()
        {
            _navigationStack.Clear();
            OnPropertyChanged(nameof(CanGoBack));
        }

        /// <summary>
        /// 根据导航目标创建内容
        /// </summary>
        private object CreateContentForTarget(NavigationTarget target)
        {
            try
            {
                var viewFactory = new DefaultViewFactory();
                return viewFactory.CreateView(target);
            }
            catch
            {
                return CreateDefaultContent(target);
            }
        }

        /// <summary>
        /// 创建默认内容（备用方案）
        /// </summary>
        private object CreateDefaultContent(NavigationTarget target)
        {
            // 如果没有注册ViewFactory，使用反射创建
            var viewNamespace = "FxxkDDL.Views";
            string viewTypeName = target switch
            {
                NavigationTarget.Input => $"{viewNamespace}.InputPage",
                NavigationTarget.Calendar => $"{viewNamespace}.CalendarPage",
                NavigationTarget.Tasks => $"{viewNamespace}.TasksPage",
                NavigationTarget.Settings => $"{viewNamespace}.SettingsPage",
                _ => throw new ArgumentOutOfRangeException(nameof(target), target, null)
            };

            try
            {
                var viewType = Type.GetType(viewTypeName);
                if (viewType != null)
                {
                    return Activator.CreateInstance(viewType);
                }
            }
            catch
            {
                // 忽略异常
            }

            return new System.Windows.Controls.TextBlock
            {
                Text = $"无法加载页面: {target}",
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            };
        }

        /// <summary>
        /// 触发导航中事件
        /// </summary>
        protected virtual void OnNavigating(NavigationEventArgs e)
        {
            Navigating?.Invoke(this, e);
        }

        /// <summary>
        /// 触发导航完成事件
        /// </summary>
        protected virtual void OnNavigated(NavigationEventArgs e)
        {
            Navigated?.Invoke(this, e);
        }
    }

    /// <summary>
    /// 视图工厂接口
    /// </summary>
    public interface IViewFactory
    {
        /// <summary>
        /// 根据导航目标创建视图
        /// </summary>
        /// <param name="target">导航目标</param>
        /// <returns>创建的视图对象</returns>
        object CreateView(NavigationTarget target);
    }
}