using System;
using System.Windows.Input;

namespace MonLingo.Core.Commands
{
    /// <summary>
    /// RelayCommand 實現 - MVVM 模式中的通用命令實現
    /// 支援同步和非同步操作，以及條件執行
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        /// <summary>
        /// 建構函式
        /// </summary>
        /// <param name="execute">執行的動作</param>
        /// <param name="canExecute">是否可以執行的條件</param>
        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        #region ICommand 實現

        /// <summary>
        /// 命令是否可以執行的變更事件
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// 檢查命令是否可以執行
        /// </summary>
        /// <param name="parameter">命令參數</param>
        /// <returns>是否可以執行</returns>
        public bool CanExecute(object parameter)
        {
            return _canExecute?.Invoke() ?? true;
        }

        /// <summary>
        /// 執行命令
        /// </summary>
        /// <param name="parameter">命令參數</param>
        public void Execute(object parameter)
        {
            if (CanExecute(parameter))
            {
                _execute.Invoke();
            }
        }

        #endregion

        #region 公開方法

        /// <summary>
        /// 手動觸發 CanExecuteChanged 事件
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }

        #endregion
    }

    /// <summary>
    /// 泛型 RelayCommand 實現 - 支援強型別參數
    /// </summary>
    /// <typeparam name="T">參數型別</typeparam>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        /// <summary>
        /// 建構函式
        /// </summary>
        /// <param name="execute">執行的動作</param>
        /// <param name="canExecute">是否可以執行的條件</param>
        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        #region ICommand 實現

        /// <summary>
        /// 命令是否可以執行的變更事件
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// 檢查命令是否可以執行
        /// </summary>
        /// <param name="parameter">命令參數</param>
        /// <returns>是否可以執行</returns>
        public bool CanExecute(object parameter)
        {
            if (parameter is T typedParameter)
            {
                return _canExecute?.Invoke(typedParameter) ?? true;
            }
            return false;
        }

        /// <summary>
        /// 執行命令
        /// </summary>
        /// <param name="parameter">命令參數</param>
        public void Execute(object parameter)
        {
            if (parameter is T typedParameter && CanExecute(parameter))
            {
                _execute.Invoke(typedParameter);
            }
        }

        #endregion

        #region 公開方法

        /// <summary>
        /// 手動觸發 CanExecuteChanged 事件
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }

        #endregion
    }

    /// <summary>
    /// 非同步 RelayCommand 實現
    /// </summary>
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<System.Threading.Tasks.Task> _execute;
        private readonly Func<bool> _canExecute;
        private bool _isExecuting;

        /// <summary>
        /// 建構函式
        /// </summary>
        /// <param name="execute">非同步執行的動作</param>
        /// <param name="canExecute">是否可以執行的條件</param>
        public AsyncRelayCommand(Func<System.Threading.Tasks.Task> execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        #region ICommand 實現

        /// <summary>
        /// 命令是否可以執行的變更事件
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// 檢查命令是否可以執行
        /// </summary>
        /// <param name="parameter">命令參數</param>
        /// <returns>是否可以執行</returns>
        public bool CanExecute(object parameter)
        {
            return !_isExecuting && (_canExecute?.Invoke() ?? true);
        }

        /// <summary>
        /// 執行命令
        /// </summary>
        /// <param name="parameter">命令參數</param>
        public async void Execute(object parameter)
        {
            if (CanExecute(parameter))
            {
                try
                {
                    _isExecuting = true;
                    RaiseCanExecuteChanged();
                    
                    await _execute.Invoke();
                }
                catch (Exception ex)
                {
                    // 記錄錯誤，但不重新拋出以避免應用程式崩潰
                    System.Diagnostics.Debug.WriteLine($"AsyncRelayCommand 執行錯誤: {ex.Message}");
                }
                finally
                {
                    _isExecuting = false;
                    RaiseCanExecuteChanged();
                }
            }
        }

        #endregion

        #region 公開方法

        /// <summary>
        /// 手動觸發 CanExecuteChanged 事件
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// 是否正在執行
        /// </summary>
        public bool IsExecuting => _isExecuting;

        #endregion
    }
}
