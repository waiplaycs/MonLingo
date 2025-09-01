using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using MonLingo.Core.Service;
using NLog;
using Brushes = System.Windows.Media.Brushes;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace MonLingo.Core.View.Windows
{
    /// <summary>
    /// OCR調試視窗 - 用於可視化OCR識別到的文字框
    /// 顯示每個識別框的位置、編號和處理順序
    /// </summary>
    public partial class OcrDebugOverlay : Window
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private Canvas _debugCanvas;
        private List<FrameworkElement> _debugElements = new List<FrameworkElement>();

        public OcrDebugOverlay()
        {
            InitializeComponent();
            SetupWindow();
        }

        private void SetupWindow()
        {
            // 設置視窗屬性
            Title = "OCR 調試覆蓋層";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            ResizeMode = ResizeMode.NoResize;
            
            // 設置全螢幕大小
            WindowState = WindowState.Maximized;
            
            // 創建Canvas
            _debugCanvas = new Canvas();
            Content = _debugCanvas;
            
            // 設置點擊穿透
            IsHitTestVisible = false;
            
            Logger.Info("🔍 OCR調試覆蓋層初始化完成");
        }

        /// <summary>
        /// 顯示OCR識別結果的調試信息
        /// </summary>
        /// <param name="ocrResult">OCR識別結果</param>
        /// <param name="transform">座標轉換參數</param>
        public void ShowOcrDebugInfo(OcrResult ocrResult, CoordinateTransform transform = null)
        {
            if (ocrResult?.Lines == null || !ocrResult.Lines.Any())
                return;

            Dispatcher.Invoke(() =>
            {
                try
                {
                    // 清除之前的調試元素
                    ClearDebugElements();
                    
                    // 按位置排序（與TextMerger中的邏輯相同）
                    var sortedLines = ocrResult.Lines
                        .Where(line => !string.IsNullOrWhiteSpace(line.Text))
                        .OrderBy(line => GetCenterY(line.BoundingBox))
                        .ThenBy(line => GetCenterX(line.BoundingBox))
                        .ToList();

                    Logger.Info($"🎯 顯示 {sortedLines.Count} 個OCR識別框");

                    // 繪製每個識別框
                    for (int i = 0; i < sortedLines.Count; i++)
                    {
                        var line = sortedLines[i];
                        DrawOcrBox(line, i + 1, transform); // 編號從1開始，傳遞座標轉換
                    }

                    // 顯示視窗
                    Show();
                    Activate();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "顯示OCR調試信息時發生錯誤");
                }
            });
        }

        /// <summary>
        /// 顯示OCR識別結果的版面分析調試信息
        /// </summary>
        /// <param name="ocrResult">OCR識別結果</param>
        /// <param name="layoutResult">版面分析結果</param>
        /// <param name="transform">座標轉換參數</param>
        public void ShowLayoutAnalysisDebugInfo(OcrResult ocrResult, LayoutAnalysisResult layoutResult, CoordinateTransform transform = null)
        {
            if (ocrResult?.Lines == null || !ocrResult.Lines.Any() || layoutResult?.Layout == null)
                return;

            Dispatcher.Invoke(() =>
            {
                try
                {
                    // 清除之前的調試元素
                    ClearDebugElements();
                    
                    Logger.Info($"🎯 顯示版面分析結果：{layoutResult.Layout.Count} 個欄位");

                    int columnIndex = 0;
                    foreach (var column in layoutResult.Layout)
                    {
                        var columnColor = GetColumnColor(columnIndex);
                        Logger.Info($"📋 欄位 {column.Key}：{column.Value.Count} 個段落，顏色：{columnColor}");

                    foreach (var paragraph in column.Value)
                    {
                        // 根據用戶需求：如果是同一段落(符合階段1,2,3)，用一個大框包圍原文代替識別框
                        DrawParagraphBoundingBox(paragraph, columnColor, transform);
                        
                        // 大框內不需要有小框，所以註釋掉個別文字行的繪製
                        // foreach (var line in paragraph.Lines)
                        // {
                        //     DrawLineWithColumnColor(line, columnColor, transform);
                        // }
                    }                        columnIndex++;
                    }

                    // 顯示視窗
                    Show();
                    Activate();
                    
                    Logger.Info("✅ 版面分析調試視覺化已顯示");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "顯示版面分析調試信息時發生錯誤");
                }
            });
        }

        /// <summary>
        /// 繪製OCR識別框
        /// </summary>
        /// <param name="line">OCR識別行</param>
        /// <param name="index">編號</param>
        /// <param name="transform">座標轉換參數</param>
        private void DrawOcrBox(OcrLine line, int index, CoordinateTransform transform = null)
        {
            var boundingBox = line.BoundingBox;
            
            // 計算絕對螢幕座標
            System.Windows.Point screenPosition;
            if (transform != null)
            {
                // 使用座標轉換器計算正確的螢幕座標
                screenPosition = transform.TransformToScreenCoordinates(boundingBox.X, boundingBox.Y);
            }
            else
            {
                // 回退到簡單偏移（舊版本兼容）
                screenPosition = new System.Windows.Point(boundingBox.X, boundingBox.Y);
            }
            
            var absoluteX = screenPosition.X;
            var absoluteY = screenPosition.Y;
            
            // 創建邊界框（虛線樣式）
            var rect = new Rectangle
            {
                Width = boundingBox.Width / (transform?.DpiScale ?? 1.0), // 考慮DPI縮放
                Height = boundingBox.Height / (transform?.DpiScale ?? 1.0),
                Stroke = GetColorByIndex(index),
                StrokeThickness = 2,
                Fill = Brushes.Transparent,
                StrokeDashArray = new DoubleCollection { 5, 3 } // 虛線樣式：5個單位實線，3個單位空白
            };

            // 設置絕對位置
            Canvas.SetLeft(rect, absoluteX);
            Canvas.SetTop(rect, absoluteY);

            // 創建編號標籤
            var label = new Border
            {
                Background = GetColorByIndex(index),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(4, 2, 4, 2),
                Child = new TextBlock
                {
                    Text = index.ToString(),
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    FontSize = 12
                }
            };

            // 設置標籤位置（在框的左上角，使用絕對座標）
            Canvas.SetLeft(label, absoluteX);
            Canvas.SetTop(label, absoluteY - 25);

            // 添加到Canvas（只添加框和編號標籤，移除文字預覽）
            _debugCanvas.Children.Add(rect);
            _debugCanvas.Children.Add(label);

            // 記錄元素以便後續清除
            _debugElements.Add(rect);
            _debugElements.Add(label);
        }

        /// <summary>
        /// 根據索引獲取顏色
        /// </summary>
        private SolidColorBrush GetColorByIndex(int index)
        {
            var colors = new[]
            {
                Brushes.Red,
                Brushes.Blue,
                Brushes.Green,
                Brushes.Orange,
                Brushes.Purple,
                Brushes.Brown,
                Brushes.Pink,
                Brushes.Cyan,
                Brushes.Magenta,
                Brushes.Yellow
            };

            return colors[(index - 1) % colors.Length] as SolidColorBrush;
        }

        /// <summary>
        /// 根據欄位索引獲取欄位顏色
        /// </summary>
        private SolidColorBrush GetColumnColor(int columnIndex)
        {
            var colors = new[]
            {
                Brushes.Red,        // 紅色 - Column 1
                Brushes.Green,      // 綠色 - Column 2  
                Brushes.Blue,       // 藍色 - Column 3
                Brushes.Orange,     // 橙色 - Column 4
                Brushes.Purple,     // 紫色 - Column 5
                Brushes.Pink,       // 粉紅色 - Column 6
                Brushes.Cyan,       // 青色 - Column 7
                Brushes.Yellow,     // 黃色 - Column 8
            };

            return colors[columnIndex % colors.Length] as SolidColorBrush;
        }

        /// <summary>
        /// 繪製段落邊界框（大框包圍整個段落）
        /// </summary>
        private void DrawParagraphBoundingBox(LayoutParagraph paragraph, SolidColorBrush color, CoordinateTransform transform = null)
        {
            var boundingBox = paragraph.BoundingBox;
            
            // 計算絕對螢幕座標
            System.Windows.Point screenPosition;
            if (transform != null)
            {
                screenPosition = transform.TransformToScreenCoordinates(boundingBox.X, boundingBox.Y);
            }
            else
            {
                screenPosition = new System.Windows.Point(boundingBox.X, boundingBox.Y);
            }

            // 創建段落邊界框（粗實線框，無填充）
            var rect = new Rectangle
            {
                Width = boundingBox.Width / (transform?.DpiScale ?? 1.0),
                Height = boundingBox.Height / (transform?.DpiScale ?? 1.0),
                Stroke = color,
                StrokeThickness = 4, // 粗線表示段落邊界
                Fill = Brushes.Transparent, // 無填充，去除底色
                StrokeDashArray = null // 實線
            };

            // 設置絕對位置
            Canvas.SetLeft(rect, screenPosition.X);
            Canvas.SetTop(rect, screenPosition.Y);

            // 創建段落標籤
            var label = new Border
            {
                Background = color,
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(6, 3, 6, 3),
                Child = new TextBlock
                {
                    Text = $"段落 {paragraph.ParagraphId}",
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    FontSize = 14
                }
            };

            // 設置標籤位置（在框的左上角外側）
            Canvas.SetLeft(label, screenPosition.X);
            Canvas.SetTop(label, screenPosition.Y - 30);

            // 添加到Canvas
            _debugCanvas.Children.Add(rect);
            _debugCanvas.Children.Add(label);

            // 記錄元素以便後續清除
            _debugElements.Add(rect);
            _debugElements.Add(label);
        }

        /// <summary>
        /// 繪製文字行框（同一欄使用相同顏色）
        /// </summary>
        private void DrawLineWithColumnColor(LayoutLine line, SolidColorBrush columnColor, CoordinateTransform transform = null)
        {
            var boundingBox = line.BoundingBox;
            
            // 計算絕對螢幕座標
            System.Windows.Point screenPosition;
            if (transform != null)
            {
                screenPosition = transform.TransformToScreenCoordinates(boundingBox.X, boundingBox.Y);
            }
            else
            {
                screenPosition = new System.Windows.Point(boundingBox.X, boundingBox.Y);
            }

            // 創建文字行邊界框（細虛線框）
            var rect = new Rectangle
            {
                Width = boundingBox.Width / (transform?.DpiScale ?? 1.0),
                Height = boundingBox.Height / (transform?.DpiScale ?? 1.0),
                Stroke = columnColor,
                StrokeThickness = 1, // 細線表示文字行
                Fill = Brushes.Transparent,
                StrokeDashArray = new DoubleCollection { 3, 2 } // 虛線樣式
            };

            // 設置絕對位置
            Canvas.SetLeft(rect, screenPosition.X);
            Canvas.SetTop(rect, screenPosition.Y);

            // 添加到Canvas
            _debugCanvas.Children.Add(rect);

            // 記錄元素以便後續清除
            _debugElements.Add(rect);
        }

        /// <summary>
        /// 清除調試元素
        /// </summary>
        private void ClearDebugElements()
        {
            foreach (var element in _debugElements)
            {
                _debugCanvas.Children.Remove(element);
            }
            _debugElements.Clear();
        }

        /// <summary>
        /// 隱藏調試覆蓋層
        /// </summary>
        public void HideDebugOverlay()
        {
            Dispatcher.Invoke(() =>
            {
                ClearDebugElements();
                Hide();
                Logger.Info("🙈 OCR調試覆蓋層已隱藏");
            });
        }

        /// <summary>
        /// 獲取邊界框的中心X座標
        /// </summary>
        private double GetCenterX(System.Drawing.Rectangle boundingBox)
        {
            return boundingBox.X + boundingBox.Width / 2.0;
        }

        /// <summary>
        /// 獲取邊界框的中心Y座標
        /// </summary>
        private double GetCenterY(System.Drawing.Rectangle boundingBox)
        {
            return boundingBox.Y + boundingBox.Height / 2.0;
        }

        protected override void OnClosed(EventArgs e)
        {
            ClearDebugElements();
            base.OnClosed(e);
        }
    }
}
