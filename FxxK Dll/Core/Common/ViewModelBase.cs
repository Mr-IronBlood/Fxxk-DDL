using System;

namespace FxxkDDL.Core.Common
{
    /// <summary>
    /// ViewModel基类，提供ViewModel通用功能
    /// </summary>
    public abstract class ViewModelBase : ObservableObject, IDisposable
    {
        private bool _disposed;

        /// <summary>
        /// ViewModel显示名称（用于UI显示）
        /// </summary>
        public virtual string DisplayName { get; protected set; } = string.Empty;

        /// <summary>
        /// 指示ViewModel是否处于忙碌状态（用于显示加载指示器）
        /// </summary>
        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            protected set => SetProperty(ref _isBusy, value);
        }

        /// <summary>
        /// 错误消息
        /// </summary>
        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            protected set => SetProperty(ref _errorMessage, value);
        }

        /// <summary>
        /// 是否发生错误
        /// </summary>
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        /// <summary>
        /// 设置错误消息
        /// </summary>
        /// <param name="message">错误消息</param>
        protected void SetError(string message)
        {
            ErrorMessage = message;
            OnPropertyChanged(nameof(HasError));
        }

        /// <summary>
        /// 清除错误消息
        /// </summary>
        protected void ClearError()
        {
            ErrorMessage = string.Empty;
            OnPropertyChanged(nameof(HasError));
        }

        /// <summary>
        /// 设置忙碌状态
        /// </summary>
        /// <param name="busy">是否忙碌</param>
        protected void SetBusy(bool busy)
        {
            IsBusy = busy;
        }

        /// <summary>
        /// 在忙碌状态下执行异步操作
        /// </summary>
        /// <param name="asyncAction">异步操作</param>
        protected async Task ExecuteWithBusyAsync(Func<Task> asyncAction)
        {
            if (IsBusy) return;

            try
            {
                SetBusy(true);
                ClearError();
                await asyncAction();
            }
            catch (Exception ex)
            {
                SetError($"操作失败: {ex.Message}");
            }
            finally
            {
                SetBusy(false);
            }
        }

        /// <summary>
        /// 在忙碌状态下执行异步操作并返回结果
        /// </summary>
        /// <typeparam name="T">结果类型</typeparam>
        /// <param name="asyncAction">异步操作</param>
        /// <returns>操作结果</returns>
        protected async Task<T> ExecuteWithBusyAsync<T>(Func<Task<T>> asyncAction)
        {
            if (IsBusy) return default;

            try
            {
                SetBusy(true);
                ClearError();
                return await asyncAction();
            }
            catch (Exception ex)
            {
                SetError($"操作失败: {ex.Message}");
                return default;
            }
            finally
            {
                SetBusy(false);
            }
        }

        /// <summary>
        /// ViewModel初始化方法（在View加载后调用）
        /// </summary>
        public virtual Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 释放ViewModel占用的资源
        /// </summary>
        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="disposing">是否正在释放托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 释放托管资源
                }

                _disposed = true;
            }
        }
    }
}