using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using MonLingo.View.Windows;

namespace MonLingo.ViewModel
{
    /// <summary>
    /// CaptureRegionWindow 的 ViewModel
    /// 負責處理螢幕擷取區域選擇的邏輯
    /// </summary>
    public class CaptureRegionViewModel : INotifyPropertyChanged, IDisposable
    {
        private bool _isDisposed = false;

        #region 屬性

        private bool _isSelecting = false;
        /// <summary>
        /// 是否正在選擇區域
        /// </summary>
        public bool IsSelecting
        {
            get => _isSelecting;
            set => SetProperty(ref _isSelecting, value);
        }

        private Point _startPoint;
        /// <summary>
        /// 選擇開始點
        /// </summary>
        public Point StartPoint
        {
            get => _startPoint;
            set => SetProperty(ref _startPoint, value);
        }

        private Point _currentPoint;
        /// <summary>
        /// 當前滑鼠位置
        /// </summary>
        public Point CurrentPoint
        {
            get => _currentPoint;
            set 
            { 
                SetProperty(ref _currentPoint, value);
                OnPropertyChanged(nameof(SelectionSize));
            }
        }

        private Rect _selectedRegion;
        /// <summary>
        /// 選擇的區域
        /// </summary>
        public Rect SelectedRegion
        {
            get => _selectedRegion;
            set => SetProperty(ref _selectedRegion, value);
        }

        /// <summary>
        /// 選擇區域的大小 (用於顯示)
        /// </summary>
        public Size SelectionSize
        {
            get
            {
                if (IsSelecting && StartPoint != default && CurrentPoint != default)
                {
                    var width = Math.Abs(CurrentPoint.X - StartPoint.X);
                    var height = Math.Abs(CurrentPoint.Y - StartPoint.Y);
                    return new Size(width, height);
                }
                return new Size(0, 0);
            }
        }

        private string _statusText = "拖曳滑鼠選擇翻譯區域";
        /// <summary>
        /// 狀態文字
        /// </summary>
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        #endregion

        #region 事件

        /// <summary>
        /// 取消擷取事件
        /// </summary>
        public event EventHandler CancelRequested;

        /// <summary>
        /// 擷取完成事件
        /// </summary>
        public event EventHandler<CaptureCompletedEventArgs> CaptureCompleted;

        #endregion

        #region 公開方法

        /// <summary>
        /// 開始選擇區域
        /// </summary>
        /// <param name="startPoint">開始點</param>
        public void StartSelection(Point startPoint)
        {
            StartPoint = startPoint;
            CurrentPoint = startPoint;
            IsSelecting = true;
            StatusText = "拖曳以選擇區域...";
        }

        /// <summary>
        /// 更新選擇區域
        /// </summary>
        /// <param name="currentPoint">當前點</param>
        public void UpdateSelection(Point currentPoint)
        {
            CurrentPoint = currentPoint;
            
            // 計算當前選擇的矩形
            var rect = GetSelectionRect();
            SelectedRegion = rect;
            
            // 更新狀態文字
            if (rect.Width > 0 && rect.Height > 0)
            {
                StatusText = $"選擇區域: {rect.Width:F0} × {rect.Height:F0}";
            }
        }

        /// <summary>
        /// 更新選擇區域 (重載方法)
        /// </summary>
        /// <param name="rect">選擇的矩形</param>
        public void UpdateSelection(Rect rect)
        {
            SelectedRegion = rect;
            
            if (rect.Width > 0 && rect.Height > 0)
            {
                StatusText = $"選擇區域: {rect.Width:F0} × {rect.Height:F0}";
            }
        }

        /// <summary>
        /// 完成擷取
        /// </summary>
        /// <param name="selectedRect">選擇的區域</param>
        public void CompleteCapture(Rect selectedRect)
        {
            try
            {
                IsSelecting = false;
                StatusText = "處理擷取...";
                
                // 驗證選擇區域
                if (selectedRect.Width < 10 || selectedRect.Height < 10)
                {
                    StatusText = "選擇區域太小，請重新選擇";
                    return;
                }

                // 觸發擷取完成事件
                CaptureCompleted?.Invoke(this, new CaptureCompletedEventArgs(selectedRect));
            }
            catch (Exception ex)
            {
                StatusText = "擷取失敗";
                System.Diagnostics.Debug.WriteLine($"完成擷取錯誤: {ex.Message}");
            }
        }

        /// <summary>
        /// 取消擷取
        /// </summary>
        public void CancelCapture()
        {
            try
            {
                IsSelecting = false;
                StatusText = "已取消";
                
                // 觸發取消事件
                CancelRequested?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"取消擷取錯誤: {ex.Message}");
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 計算選擇的矩形
        /// </summary>
        /// <returns>選擇的矩形</returns>
        private Rect GetSelectionRect()
        {
            if (StartPoint == default || CurrentPoint == default)
                return new Rect();

            var x = Math.Min(StartPoint.X, CurrentPoint.X);
            var y = Math.Min(StartPoint.Y, CurrentPoint.Y);
            var width = Math.Abs(CurrentPoint.X - StartPoint.X);
            var height = Math.Abs(CurrentPoint.Y - StartPoint.Y);

            return new Rect(x, y, width, height);
        }

        #endregion

        #region INotifyPropertyChanged 實現

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
        {
            if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion

        #region IDisposable 實現

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // 清理託管資源
                    CancelRequested = null;
                    CaptureCompleted = null;
                }

                _isDisposed = true;
            }
        }

        #endregion
    }
}
