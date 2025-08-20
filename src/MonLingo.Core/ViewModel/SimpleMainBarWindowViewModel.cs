using System;
using System.ComponentModel;
using System.Windows.Input;

namespace MonLingo.ViewModel
{
    /// <summary>
    /// 簡化版 MainBarWindowViewModel 用於測試
    /// </summary>
    public class SimpleMainBarWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // 基本屬性
        private string _selectedEngine = "Google";
        public string SelectedEngine
        {
            get => _selectedEngine;
            set
            {
                _selectedEngine = value;
                OnPropertyChanged(nameof(SelectedEngine));
            }
        }

        private int _remainingTranslations = 999;
        public int RemainingTranslations
        {
            get => _remainingTranslations;
            set
            {
                _remainingTranslations = value;
                OnPropertyChanged(nameof(RemainingTranslations));
            }
        }

        private int _coinBalance = 1250;
        public int CoinBalance
        {
            get => _coinBalance;
            set
            {
                _coinBalance = value;
                OnPropertyChanged(nameof(CoinBalance));
            }
        }

        // 簡單命令
        private ICommand _settingsCommand;
        public ICommand OpenSettingsCommand =>
            _settingsCommand ??= new RelayCommand(() =>
            {
                System.Windows.MessageBox.Show("設置按鈕被點擊！", "測試");
            });

        private ICommand _exitCommand;
        public ICommand ExitCommand =>
            _exitCommand ??= new RelayCommand(() =>
            {
                System.Windows.MessageBox.Show("關閉按鈕被點擊！", "測試");
                System.Windows.Application.Current.Shutdown();
            });

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 簡單的 RelayCommand 實現
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object parameter)
        {
            _execute();
        }
    }
}
