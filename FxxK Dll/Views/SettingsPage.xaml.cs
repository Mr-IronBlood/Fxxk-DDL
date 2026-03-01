using FxxkDDL.Services;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FxxkDDL.Views
{
    public partial class SettingsPage : UserControl
    {
        private ConfigService _configService;
        private DeepSeekService _deepSeekService;

        public SettingsPage()
        {
            InitializeComponent();

            _configService = new ConfigService();
            _deepSeekService = new DeepSeekService();

            // 绑定按钮事件
            BtnSaveApiKey.Click += BtnSaveApiKey_Click;
            BtnTestApiKey.Click += BtnTestApiKey_Click;

            // 加载已保存的配置
            LoadSavedConfig();
        }

        private void LoadSavedConfig()
        {
            var config = _configService.GetConfig();
            TxtApiKey.Text = config.DeepSeekApiKey;

            UpdateStatusText();
        }

        private void UpdateStatusText()
        {
            if (_configService.HasApiKey())
            {
                TxtStatus.Text = $"✅ API密钥已配置\n密钥: {_configService.GetMaskedApiKey()}";
                TxtStatus.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                TxtStatus.Text = "❌ 未配置API密钥";
                TxtStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void BtnSaveApiKey_Click(object sender, RoutedEventArgs e)
        {
            string apiKey = TxtApiKey.Text.Trim();

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                MessageBox.Show("请输入API密钥", "提示",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _configService.UpdateApiKey(apiKey);

                MessageBox.Show($"✅ API密钥保存成功！\n\n密钥已安全保存到配置文件",
                              "保存成功",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);

                UpdateStatusText();
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message, "输入错误",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败: {ex.Message}", "错误",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnTestApiKey_Click(object sender, RoutedEventArgs e)
        {
            string apiKey = TxtApiKey.Text.Trim();

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                MessageBox.Show("请先输入API密钥", "提示",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 先保存密钥
            try
            {
                _configService.UpdateApiKey(apiKey);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message, "密钥格式错误",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 创建新的 DeepSeekService 实例以使用最新的 API 密钥
            _deepSeekService = new DeepSeekService();

            // 禁用按钮，显示测试中
            BtnTestApiKey.IsEnabled = false;
            BtnTestApiKey.Content = "测试中...";

            // 测试连接
            var result = await _deepSeekService.TestApiConnectionAsync();

            // 恢复按钮
            BtnTestApiKey.IsEnabled = true;
            BtnTestApiKey.Content = "测试连接";

            // 显示结果
            string message = result.Message;
            if (result.Success)
            {
                message += "\n\n⚠️ 请重启软件以确保新配置生效！";
            }

            MessageBox.Show(message,
                          result.Success ? "连接测试成功" : "连接测试失败",
                          MessageBoxButton.OK,
                          result.Success ? MessageBoxImage.Information : MessageBoxImage.Error);

            UpdateStatusText();
        }
    }
}
