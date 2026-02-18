using FxxkDDL.Core.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace FxxkDDL
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            // 设置窗口最小尺寸
            this.MinHeight = 500;
            this.MinWidth = 800;

            // 创建并设置ViewModel
            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            // 绑定内容区域到导航服务
            var navigationService = Core.Navigation.NavigationService.Instance;
            ContentArea.SetBinding(ContentControl.ContentProperty, "CurrentContent");
        }
    }
}
