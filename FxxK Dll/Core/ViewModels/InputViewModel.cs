using FxxkDDL.Core.Common;
using FxxkDDL.Core.Interfaces;
using FxxkDDL.Models;
using FxxkDDL.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Input;

namespace FxxkDDL.Core.ViewModels
{
    /// <summary>
    /// 输入页面ViewModel
    /// </summary>
    public class InputViewModel : ViewModelBase
    {
        private readonly IDeepSeekService _deepSeekService;
        private readonly ITaskService _taskService;
        private string _chatText;
        private int _characterCount;
        private string _estimatedTime;
        private bool _isAnalyzing;
        private string _selectedFileName;
        private string _selectedFilePath;
        private bool _hasSelectedFile;
        private string _fileUploadStatus;

        /// <summary>
        /// 聊天记录文本
        /// </summary>
        public string ChatText
        {
            get => _chatText;
            set
            {
                if (SetProperty(ref _chatText, value))
                {
                    UpdateCharacterCount();
                }
            }
        }

        /// <summary>
        /// 字符数
        /// </summary>
        public int CharacterCount
        {
            get => _characterCount;
            private set => SetProperty(ref _characterCount, value);
        }

        /// <summary>
        /// 预估分析时间
        /// </summary>
        public string EstimatedTime
        {
            get => _estimatedTime;
            private set => SetProperty(ref _estimatedTime, value);
        }

        /// <summary>
        /// 是否正在分析
        /// </summary>
        public bool IsAnalyzing
        {
            get => _isAnalyzing;
            private set => SetProperty(ref _isAnalyzing, value);
        }

        /// <summary>
        /// 已选择的文件名
        /// </summary>
        public string SelectedFileName
        {
            get => _selectedFileName;
            private set => SetProperty(ref _selectedFileName, value);
        }

        /// <summary>
        /// 已选择的文件路径
        /// </summary>
        public string SelectedFilePath
        {
            get => _selectedFilePath;
            private set => SetProperty(ref _selectedFilePath, value);
        }

        /// <summary>
        /// 是否有选择的文件
        /// </summary>
        public bool HasSelectedFile
        {
            get => _hasSelectedFile;
            private set
            {
                if (SetProperty(ref _hasSelectedFile, value))
                {
                    // 触发分析按钮文本更新
                    OnPropertyChanged(nameof(AnalyzeButtonText));
                }
            }
        }

        /// <summary>
        /// 文件上传状态
        /// </summary>
        public string FileUploadStatus
        {
            get => _fileUploadStatus;
            private set => SetProperty(ref _fileUploadStatus, value);
        }

        /// <summary>
        /// 分析按钮文本
        /// </summary>
        public string AnalyzeButtonText
        {
            get
            {
                if (HasSelectedFile)
                    return "📄 分析文件";
                else
                    return "🚀 分析文本";
            }
        }

        /// <summary>
        /// 粘贴命令
        /// </summary>
        public ICommand PasteCommand { get; }

        /// <summary>
        /// 导入文件命令
        /// </summary>
        public ICommand ImportCommand { get; }

        /// <summary>
        /// 清空命令
        /// </summary>
        public ICommand ClearCommand { get; }

        /// <summary>
        /// 分析命令
        /// </summary>
        public ICommand AnalyzeCommand { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public InputViewModel()
        {
            // 获取服务实例
            _deepSeekService = ServiceLocator.GetService<IDeepSeekService>();
            _taskService = ServiceLocator.GetService<ITaskService>();

            // 初始化命令
            PasteCommand = new RelayCommand(ExecutePaste);
            ImportCommand = new RelayCommand(ExecuteImport);
            ClearCommand = new RelayCommand(ExecuteClear, () => !string.IsNullOrEmpty(ChatText));
            AnalyzeCommand = new RelayCommand(ExecuteAnalyze, () => CanAnalyze());

            // 初始化属性 - 空字符串，水印会显示提示
            ChatText = string.Empty;
            UpdateCharacterCount();
        }

        /// <summary>
        /// 更新字符数
        /// </summary>
        private void UpdateCharacterCount()
        {
            CharacterCount = string.IsNullOrEmpty(ChatText) ? 0 : ChatText.Length;
            EstimatedTime = CalculateEstimatedTime(CharacterCount);
            ((RelayCommand)ClearCommand).RaiseCanExecuteChanged();
            ((RelayCommand)AnalyzeCommand).RaiseCanExecuteChanged();
        }

        /// <summary>
        /// 计算预估分析时间
        /// </summary>
        private string CalculateEstimatedTime(int charCount)
        {
            if (charCount < 500) return "2-5秒";
            if (charCount < 2000) return "5-10秒";
            if (charCount < 5000) return "10-20秒";
            return "20-30秒";
        }

        /// <summary>
        /// 检查是否可以分析
        /// </summary>
        private bool CanAnalyze()
        {
            // 如果正在分析，则不允许
            if (IsAnalyzing)
                return false;

            // 如果有选择的文件，即使没有文本也可以分析
            if (HasSelectedFile)
                return true;

            // 否则检查文本是否有效
            return !string.IsNullOrWhiteSpace(ChatText) &&
                   !ChatText.Contains("示例对话：") &&
                   ChatText.Length >= 10;
        }

        /// <summary>
        /// 清除文件选择
        /// </summary>
        private void ClearFileSelection()
        {
            SelectedFileName = null;
            SelectedFilePath = null;
            HasSelectedFile = false;
            FileUploadStatus = null;
            ((RelayCommand)AnalyzeCommand).RaiseCanExecuteChanged();
        }

        /// <summary>
        /// 执行粘贴
        /// </summary>
        private void ExecutePaste()
        {
            ExecuteWithBusyAsync(async () =>
            {
                try
                {
                    if (System.Windows.Clipboard.ContainsText())
                    {
                        var clipboardText = System.Windows.Clipboard.GetText();
                        if (!string.IsNullOrWhiteSpace(clipboardText))
                        {
                            ChatText = clipboardText;
                            // 粘贴文本时清除文件选择
                            ClearFileSelection();
                        }
                    }
                }
                catch (Exception ex)
                {
                    SetError($"粘贴失败: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 执行导入
        /// </summary>
        private void ExecuteImport()
        {
            ExecuteWithBusyAsync(async () =>
            {
                try
                {
                    var openFileDialog = new Microsoft.Win32.OpenFileDialog
                    {
                        Filter = "文本文件 (*.txt)|*.txt|PDF文件 (*.pdf)|*.pdf|Word文档 (*.doc;*.docx)|*.doc;*.docx|PPT文件 (*.ppt;*.pptx)|*.ppt;*.pptx|所有文件 (*.*)|*.*",
                        Title = "选择聊天记录或文档文件",
                        Multiselect = false
                    };

                    if (openFileDialog.ShowDialog() == true)
                    {
                        var filePath = openFileDialog.FileName;
                        var extension = Path.GetExtension(filePath).ToLower();

                        // 使用文件解析服务解析文件
                        var fileParser = new FileParserService();
                        var (success, text, message) = fileParser.ParseFile(filePath);

                        if (!success)
                        {
                            // 如果是PDF/Word/PPT，提示用户需要安装额外的库
                            if (new[] { ".pdf", ".doc", ".docx", ".ppt", ".pptx" }.Contains(extension))
                            {
                                FileUploadStatus = $"⚠️ 需要安装解析库: {message}";
                                SetError(message);

                                // 询问用户是否要尝试原始文本方式
                                var result = System.Windows.MessageBox.Show(
                                    $"解析{extension.ToUpper()}文件需要安装额外的NuGet包。\n\n" +
                                    $"是否要将文件内容作为原始文本处理（可能无法正确解析）？\n\n" +
                                    $"建议安装：NPOI或DocumentFormat.OpenXml库",
                                    "需要安装解析库",
                                    System.Windows.MessageBoxButton.YesNo,
                                    System.Windows.MessageBoxImage.Question);

                                if (result == System.Windows.MessageBoxResult.Yes)
                                {
                                    // 尝试读取原始内容
                                    try
                                    {
                                        var fileContent = File.ReadAllText(filePath);
                                        ChatText = fileContent;
                                        ClearFileSelection();
                                    }
                                    catch (Exception ex)
                                    {
                                        SetError($"读取文件失败: {ex.Message}");
                                    }
                                }
                            }
                            else
                            {
                                SetError(message);
                            }
                            return;
                        }

                        // 解析成功，将文本内容显示在输入框
                        ChatText = text;
                        FileUploadStatus = message;

                        // 清除之前的文件选择状态（因为已经解析到文本框了）
                        ClearFileSelection();
                    }
                }
                catch (Exception ex)
                {
                    SetError($"文件导入失败: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 执行清空
        /// </summary>
        private void ExecuteClear()
        {
            // 确认对话框可以在View中处理，这里直接清空
            ChatText = string.Empty;
            ClearFileSelection();
        }

        /// <summary>
        /// 执行分析
        /// </summary>
        private async void ExecuteAnalyze()
        {
            await ExecuteWithBusyAsync(async () =>
            {
                IsAnalyzing = true;
                ((RelayCommand)AnalyzeCommand).RaiseCanExecuteChanged();

                try
                {
                    // 根据是否有选择文件决定分析方式
                    (bool Success, List<DDLTask> Tasks, string Message) result;

                    if (HasSelectedFile && !string.IsNullOrWhiteSpace(SelectedFilePath))
                    {
                        // 文件分析
                        FileUploadStatus = "⏳ 正在分析文件...";
                        result = await _deepSeekService.ExtractDDLFromFileAsync(SelectedFilePath);
                    }
                    else
                    {
                        // 文本分析
                        FileUploadStatus = "⏳ 正在分析文本...";
                        result = await _deepSeekService.ExtractDDLFromTextAsync(ChatText);
                    }

                    if (!result.Success)
                    {
                        FileUploadStatus = $"❌ 分析失败: {result.Message}";
                        SetError($"分析失败: {result.Message}");

                        // 显示失败消息框
                        System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            System.Windows.MessageBox.Show($"分析失败:\n{result.Message}",
                                "分析结果",
                                System.Windows.MessageBoxButton.OK,
                                System.Windows.MessageBoxImage.Warning);
                        });
                        return;
                    }

                    if (result.Tasks == null || result.Tasks.Count == 0)
                    {
                        FileUploadStatus = "⚠️ 分析完成，但未能提取到明确的DDL任务";
                        SetError("分析完成，但未能提取到明确的DDL任务");

                        // 显示未提取到任务的消息框
                        System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            System.Windows.MessageBox.Show("分析完成，但未能提取到明确的DDL任务",
                                "分析结果",
                                System.Windows.MessageBoxButton.OK,
                                System.Windows.MessageBoxImage.Information);
                        });
                        return;
                    }

                    // 保存任务到数据库
                    SaveTasksToDatabase(result.Tasks);

                    // 显示成功消息框
                    System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        System.Windows.MessageBox.Show($"成功提取到 {result.Tasks.Count} 个任务!",
                            "分析成功",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Information);
                    });

                    // 触发分析完成事件
                    OnAnalysisCompleted?.Invoke(this, new AnalysisCompletedEventArgs
                    {
                        Tasks = result.Tasks,
                        Message = result.Message
                    });

                    // 分析完成后清除文件选择
                    ClearFileSelection();
                }
                catch (Exception ex)
                {
                    FileUploadStatus = $"❌ 分析过程发生错误: {ex.Message}";
                    SetError($"分析过程发生错误: {ex.Message}");

                    // 显示错误消息框
                    System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        System.Windows.MessageBox.Show($"分析过程发生错误:\n{ex.Message}",
                            "分析错误",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Error);
                    });
                }
                finally
                {
                    IsAnalyzing = false;
                    ((RelayCommand)AnalyzeCommand).RaiseCanExecuteChanged();
                }
            });
        }

        /// <summary>
        /// 保存任务到数据库
        /// </summary>
        private void SaveTasksToDatabase(List<DDLTask> tasks)
        {
            try
            {
                // 检查并设置任务的必要属性
                foreach (var task in tasks)
                {
                    // 确保任务有ID
                    if (string.IsNullOrWhiteSpace(task.Id))
                    {
                        task.Id = Guid.NewGuid().ToString();
                    }

                    // 确保任务有创建时间
                    if (task.CreatedAt == default)
                    {
                        task.CreatedAt = DateTime.Now;
                    }

                    // 确保任务未完成状态
                    task.IsCompleted = false;
                    task.CompletedAt = null;

                    // 解析截止时间
                    task.ParseDeadline();
                }

                // 保存到数据库
                _taskService.AddTasks(tasks);
            }
            catch (Exception ex)
            {
                SetError($"任务保存失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 分析完成事件
        /// </summary>
        public event EventHandler<AnalysisCompletedEventArgs> OnAnalysisCompleted;
    }

    /// <summary>
    /// 分析完成事件参数
    /// </summary>
    public class AnalysisCompletedEventArgs : EventArgs
    {
        public List<DDLTask> Tasks { get; set; }
        public string Message { get; set; }
    }
}