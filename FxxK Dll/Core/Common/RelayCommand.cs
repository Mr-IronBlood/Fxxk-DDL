using System;
using System.Windows.Input;

namespace FxxkDDL.Core.Common
{
    /// <summary>
    /// 实现ICommand接口的通用命令类
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        /// <summary>
        /// 初始化一个始终可执行的命令
        /// </summary>
        /// <param name="execute">执行委托</param>
        public RelayCommand(Action execute) : this(execute, null) { }

        /// <summary>
        /// 初始化一个有条件执行的命令
        /// </summary>
        /// <param name="execute">执行委托</param>
        /// <param name="canExecute">可执行性检查委托</param>
        public RelayCommand(Action execute, Func<bool> canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// 检查命令是否可以执行
        /// </summary>
        public bool CanExecute(object parameter)
        {
            return _canExecute?.Invoke() ?? true;
        }

        /// <summary>
        /// 执行命令
        /// </summary>
        public void Execute(object parameter)
        {
            _execute();
        }

        /// <summary>
        /// 当可执行性发生变化时触发
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// 触发CanExecuteChanged事件
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }

    /// <summary>
    /// 带参数版本的RelayCommand
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        /// <summary>
        /// 初始化一个始终可执行的命令
        /// </summary>
        /// <param name="execute">执行委托</param>
        public RelayCommand(Action<T> execute) : this(execute, null) { }

        /// <summary>
        /// 初始化一个有条件执行的命令
        /// </summary>
        /// <param name="execute">执行委托</param>
        /// <param name="canExecute">可执行性检查委托</param>
        public RelayCommand(Action<T> execute, Func<T, bool> canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// 检查命令是否可以执行
        /// </summary>
        public bool CanExecute(object parameter)
        {
            if (_canExecute == null) return true;

            if (parameter is T typedParameter)
                return _canExecute(typedParameter);

            return _canExecute(default);
        }

        /// <summary>
        /// 执行命令
        /// </summary>
        public void Execute(object parameter)
        {
            if (parameter is T typedParameter)
                _execute(typedParameter);
            else
                _execute(default);
        }

        /// <summary>
        /// 当可执行性发生变化时触发
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// 触发CanExecuteChanged事件
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}