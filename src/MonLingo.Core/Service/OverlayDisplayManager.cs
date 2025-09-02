using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using MonLingo.Core.View.Windows;
using MonLingo.Core.Service;
using NLog;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// 覆蓋顯示管理器 - 根據 Gaminik 覆蓋模式實現
    /// 管理多視窗協作系統，實現原地渲染翻譯效果
    /// </summary>
    public class OverlayDisplayManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        
        private TranslationOverlayHostWindow _hostWindow;
        private readonly List<SingleTranslationWindow> _translationWindows = new();
        private Rect _currentRegion;
        
        /// <summary>
        /// 顯示翻譯覆蓋
        /// </summary>
        /// <param name="ocrResult">OCR 結果</param>
        /// <param name="translatedTexts">對應的翻譯文字列表</param>
        /// <param name="region">顯示區域</param>
        public void ShowOverlay(OcrResult ocrResult, List<string> translatedTexts, Rect region)
        {
            try
            {
                Logger.Info($"[OverlayDisplayManager] 開始顯示覆蓋模式: {ocrResult?.Lines?.Length} 行 OCR 結果");
                
                // 清理現有覆蓋
                ClearOverlay();
                
                if (ocrResult?.Lines == null || translatedTexts == null) return;
                
                _currentRegion = region;
                
                // 創建宿主視窗
                CreateHostWindow(region);
                
                // 為每個 OCR 行創建翻譯視窗
                CreateTranslationWindows(ocrResult.Lines, translatedTexts);
                
                Logger.Info($"[OverlayDisplayManager] 覆蓋模式顯示完成，共 {_translationWindows.Count} 個翻譯視窗");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "[OverlayDisplayManager] 顯示覆蓋模式失敗");
                ClearOverlay();
            }
        }
        
        /// <summary>
        /// 清理所有覆蓋視窗
        /// </summary>
        public void ClearOverlay()
        {
            try
            {
                Logger.Info("[OverlayDisplayManager] 開始清理覆蓋視窗");
                
                // 清理子視窗
                ClearTranslationWindows();
                
                // 關閉宿主視窗
                if (_hostWindow != null)
                {
                    try
                    {
                        _hostWindow.Close();
                        _hostWindow = null;
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(ex, "[OverlayDisplayManager] 關閉宿主視窗失敗");
                    }
                }
                
                Logger.Info("[OverlayDisplayManager] 覆蓋視窗清理完成");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "[OverlayDisplayManager] 清理覆蓋視窗時發生錯誤");
            }
        }
        
        /// <summary>
        /// 只清理翻譯子視窗，不關閉宿主視窗
        /// </summary>
        private void ClearTranslationWindows()
        {
            // 關閉所有翻譯視窗
            foreach (var window in _translationWindows.ToList())
            {
                try
                {
                    window.Close(); // 直接關閉，不需要動畫
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "[OverlayDisplayManager] 關閉翻譯視窗失敗");
                }
            }
            _translationWindows.Clear();
        }
        
        /// <summary>
        /// 創建宿主視窗 - 主要的翻譯框
        /// </summary>
        private void CreateHostWindow(Rect region)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _hostWindow = new TranslationOverlayHostWindow();
                _hostWindow.SetBounds(region.Left, region.Top, region.Width, region.Height);
                
                // 訂閱宿主視窗關閉事件
                _hostWindow.HostWindowClosing += (sender, e) =>
                {
                    Logger.Info("[OverlayDisplayManager] 宿主視窗關閉，清理所有子視窗");
                    ClearTranslationWindows();
                };
                
                _hostWindow.Show();
                Logger.Info($"[OverlayDisplayManager] 翻譯框已顯示: {region.Left},{region.Top} {region.Width}x{region.Height}");
            });
        }
        
        /// <summary>
        /// 創建翻譯視窗 - 相對於宿主視窗定位
        /// </summary>
        private void CreateTranslationWindows(OcrLine[] ocrLines, List<string> translatedTexts)
        {
            if (ocrLines == null || translatedTexts == null) return;
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                for (int i = 0; i < Math.Min(ocrLines.Length, translatedTexts.Count); i++)
                {
                    var ocrLine = ocrLines[i];
                    var translatedText = translatedTexts[i];
                    
                    if (string.IsNullOrWhiteSpace(translatedText)) continue;
                    
                    // 創建單個翻譯視窗
                    var translationWindow = new SingleTranslationWindow();
                    
                    // 計算相對於宿主視窗的位置
                    // OCR結果的坐標是相對於選擇區域的，所以直接加上宿主視窗的位置
                    double left = _hostWindow.Left + ocrLine.BoundingBox.X;
                    double top = _hostWindow.Top + ocrLine.BoundingBox.Y;
                    
                    // 設置父視窗為宿主視窗
                    translationWindow.Owner = _hostWindow;
                    
                    // 顯示翻譯
                    translationWindow.ShowTranslation(
                        translatedText, 
                        left, 
                        top, 
                        ocrLine.BoundingBox.Width, 
                        ocrLine.BoundingBox.Height
                    );
                    
                    _translationWindows.Add(translationWindow);
                    
                    Logger.Debug($"[OverlayDisplayManager] 子翻譯視窗 {i}: '{translatedText}' at ({left},{top})");
                }
            });
        }
        
        /// <summary>
        /// 釋放資源
        /// </summary>
        public void Dispose()
        {
            ClearOverlay();
        }
    }
}
