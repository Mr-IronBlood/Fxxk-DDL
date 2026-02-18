using FxxkDDL.Core.ViewModels;
using FxxkDDL.Views;
using System.Windows;
using System.Windows.Controls;

namespace FxxkDDL.Views
{
    /// <summary>
    /// 输入页面视图 - 处理UI交互
    /// 业务逻辑已迁移到InputViewModel
    /// </summary>
    public partial class InputPage : UserControl
    {
        public InputPage()
        {
            InitializeComponent();
            Loaded += InputPage_Loaded;
        }

        private void InputPage_Loaded(object sender, RoutedEventArgs e)
        {
            // 订阅分析完成事件
            if (DataContext is InputViewModel viewModel)
            {
                viewModel.OnAnalysisCompleted += InputViewModel_OnAnalysisCompleted;
            }

            // 确保取消订阅
            Unloaded += InputPage_Unloaded;
        }

        private void InputPage_Unloaded(object sender, RoutedEventArgs e)
        {
            // 取消订阅事件
            if (DataContext is InputViewModel viewModel)
            {
                viewModel.OnAnalysisCompleted -= InputViewModel_OnAnalysisCompleted;
            }
            Unloaded -= InputPage_Unloaded;
            Loaded -= InputPage_Loaded;
        }

        /// <summary>
        /// 处理分析完成事件 - 显示结果
        /// </summary>
        private void InputViewModel_OnAnalysisCompleted(object sender, AnalysisCompletedEventArgs e)
        {
            // 显示美化后的结果对话框
            var dialog = new AnalysisResultDialog(e.Tasks, e.Message)
            {
                Owner = Window.GetWindow(this)
            };

            dialog.ShowDialog();
        }
    }
}