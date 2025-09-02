using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Animation;
using NLog;

namespace MonLingo.Core.View.Windows
{
    /// <summary>
    /// 單個翻譯文字視窗 - 顯示一行翻譯文字
    /// 根據 Gaminik 覆蓋模式設計實現
    /// </summary>
    public partial class SingleTranslationWindow : Window, INotifyPropertyChanged
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        
        private string _translatedText = "";
        public string TranslatedText
        {
            get => _translatedText;
            set
            {
                _translatedText = value;
                OnPropertyChanged(nameof(TranslatedText));
            }
        }
        
        public SingleTranslationWindow()
        {
            InitializeComponent();
            DataContext = this;
            Logger.Info("[SingleTranslationWindow] 單個翻譯視窗已創建");
        }
        
        /// <summary>
        /// 設置翻譯內容並顯示視窗
        /// </summary>
        /// <param name="translatedText">翻譯文字</param>
        /// <param name="left">左邊位置</param>
        /// <param name="top">頂部位置</param>
        /// <param name="width">建議寬度</param>
        /// <param name="height">建議高度</param>
        public void ShowTranslation(string translatedText, double left, double top, double width = 0, double height = 0)
        {
            TranslatedText = translatedText;
            Left = left;
            Top = top;
            
            // 如果指定了尺寸，設置最小尺寸
            if (width > 0) MinWidth = width;
            if (height > 0) MinHeight = height;
            
            Show();
            Logger.Info($"[SingleTranslationWindow] 顯示翻譯: {translatedText} at {left},{top}");
        }
        
        /// <summary>
        /// 淡出並關閉視窗
        /// </summary>
        /// <summary>
        /// 處理視窗點擊事件 - 點擊後關閉視窗
        /// </summary>
        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Logger.Info("[SingleTranslationWindow] 視窗被點擊，開始關閉");
            FadeOutAndClose();
        }
        
        /// <summary>
        /// 淡出並關閉視窗
        /// </summary>
        public void FadeOutAndClose()
        {
            var fadeOut = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(300));
            fadeOut.Completed += (s, e) => Close();
            BeginAnimation(OpacityProperty, fadeOut);
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
