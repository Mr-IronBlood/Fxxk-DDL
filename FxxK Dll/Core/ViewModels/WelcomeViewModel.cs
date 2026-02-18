using FxxkDDL.Core.Common;
using System;

namespace FxxkDDL.Core.ViewModels
{
    /// <summary>
    /// 欢迎页面ViewModel - 静态欢迎界面
    /// </summary>
    public class WelcomeViewModel : ViewModelBase
    {
        private string _welcomeText;

        /// <summary>
        /// 欢迎文本（保留属性以兼容XAML绑定）
        /// </summary>
        public string WelcomeText
        {
            get => _welcomeText;
            private set => SetProperty(ref _welcomeText, value);
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public WelcomeViewModel()
        {
            // 初始化欢迎文本
            WelcomeText = "欢迎使用DDL智能管理器";
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
        }
    }
}