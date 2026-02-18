using FxxkDDL.Core.Common;
using FxxkDDL.Core.Interfaces;
using FxxkDDL.Models;
using System;
using System.Windows.Input;

namespace FxxkDDL.Core.ViewModels
{
    /// <summary>
    /// 设置页面ViewModel
    /// </summary>
    public class SettingsViewModel : ViewModelBase
    {
        private readonly IConfigService _configService;
        private readonly IDeepSeekService _deepSeekService;
        private string _apiKey;
        private string _statusText;
        private bool _isTesting;

        /// <summary>
        /// API密钥
        /// </summary>
        public string ApiKey
        {
            get => _apiKey;
            set => SetProperty(ref _apiKey, value);
        }

        /// <summary>
        /// 状态文本
        /// </summary>
        public string StatusText
        {
            get => _statusText;
            private set => SetProperty(ref _statusText, value);
        }

        /// <summary>
        /// 是否正在测试
        /// </summary>
        public bool IsTesting
        {
            get => _isTesting;
            private set => SetProperty(ref _isTesting, value);
        }

        /// <summary>
        /// 掩码后的API密钥（用于显示）
        /// </summary>
        public string MaskedApiKey => _configService.GetMaskedApiKey();

        /// <summary>
        /// 是否已配置API密钥
        /// </summary>
        public bool HasApiKey => _configService.HasApiKey();

        /// <summary>
        /// 保存API密钥命令
        /// </summary>
        public ICommand SaveApiKeyCommand { get; }

        /// <summary>
        /// 测试API密钥命令
        /// </summary>
        public ICommand TestApiKeyCommand { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public SettingsViewModel()
        {
            _configService = ServiceLocator.GetService<IConfigService>();
            _deepSeekService = ServiceLocator.GetService<IDeepSeekService>();

            // 初始化命令
            SaveApiKeyCommand = new RelayCommand(ExecuteSaveApiKey, () => !string.IsNullOrWhiteSpace(ApiKey));
            TestApiKeyCommand = new RelayCommand(ExecuteTestApiKey, () => !string.IsNullOrWhiteSpace(ApiKey) && !IsTesting);

            // 加载已保存的配置
            LoadSavedConfig();
        }

        /// <summary>
        /// 加载已保存的配置
        /// </summary>
        private void LoadSavedConfig()
        {
            try
            {
                var config = _configService.GetConfig();
                ApiKey = config.DeepSeekApiKey;
                UpdateStatusText();
            }
            catch (Exception ex)
            {
                SetError($"加载配置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新状态文本
        /// </summary>
        private void UpdateStatusText()
        {
            if (_configService.HasApiKey())
            {
                StatusText = $"✅ API密钥已配置\n密钥: {_configService.GetMaskedApiKey()}";
            }
            else
            {
                StatusText = "❌ 未配置API密钥";
            }

            OnPropertyChanged(nameof(HasApiKey));
            OnPropertyChanged(nameof(MaskedApiKey));
            ((RelayCommand)SaveApiKeyCommand).RaiseCanExecuteChanged();
            ((RelayCommand)TestApiKeyCommand).RaiseCanExecuteChanged();
        }

        /// <summary>
        /// 执行保存API密钥
        /// </summary>
        private void ExecuteSaveApiKey()
        {
            ExecuteWithBusyAsync(async () =>
            {
                try
                {
                    _configService.UpdateApiKey(ApiKey);
                    UpdateStatusText();
                }
                catch (ArgumentException ex)
                {
                    SetError($"输入错误: {ex.Message}");
                }
                catch (Exception ex)
                {
                    SetError($"保存失败: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 执行测试API密钥
        /// </summary>
        private async void ExecuteTestApiKey()
        {
            await ExecuteWithBusyAsync(async () =>
            {
                IsTesting = true;
                ((RelayCommand)TestApiKeyCommand).RaiseCanExecuteChanged();

                try
                {
                    // 先保存密钥
                    _configService.UpdateApiKey(ApiKey);

                    // 测试连接
                    var result = await _deepSeekService.TestApiConnectionAsync();

                    if (result.Success)
                    {
                        StatusText = $"✅ {result.Message}";
                    }
                    else
                    {
                        StatusText = $"❌ {result.Message}";
                    }
                }
                catch (Exception ex)
                {
                    SetError($"测试失败: {ex.Message}");
                }
                finally
                {
                    IsTesting = false;
                    ((RelayCommand)TestApiKeyCommand).RaiseCanExecuteChanged();
                    UpdateStatusText();
                }
            });
        }

        /// <summary>
        /// 刷新配置
        /// </summary>
        public void Refresh()
        {
            LoadSavedConfig();
        }
    }
}