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
        private System.Drawing.Rectangle _targetScreenBounds; // 目標螢幕邊界，用於多螢幕支援

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
            
            // 默認設置為主螢幕全螢幕大小
            WindowState = WindowState.Maximized;
            
            // 創建Canvas
            _debugCanvas = new Canvas();
            Content = _debugCanvas;
            
            // 設置點擊穿透
            IsHitTestVisible = false;
            
            Logger.Info("🔍 OCR調試覆蓋層初始化完成");
        }

        /// <summary>
        /// 設置覆蓋層在指定螢幕上顯示
        /// </summary>
        /// <param name="targetScreen">目標螢幕的工作區域</param>
        public void SetTargetScreen(System.Drawing.Rectangle targetScreen)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    // 保存目標螢幕資訊供座標轉換使用
                    _targetScreenBounds = targetScreen;
                    
                    // 設置視窗位置和大小以覆蓋指定螢幕
                    WindowState = WindowState.Normal;
                    Left = targetScreen.X;
                    Top = targetScreen.Y;
                    Width = targetScreen.Width;
                    Height = targetScreen.Height;
                    
                    Logger.Info($"🖥️ OCR調試覆蓋層已設置到螢幕區域：({targetScreen.X},{targetScreen.Y},{targetScreen.Width},{targetScreen.Height})");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "設置目標螢幕時發生錯誤");
                }
            });
        }

        /// <summary>
        /// 顯示OCR識別結果的調試信息
        /// </summary>
        /// <param name="ocrResult">OCR識別結果</param>
        /// <param name="transform">座標轉換參數</param>
        /// <param name="mergedLineInfo">合併行資訊（可選）</param>
        public void ShowOcrDebugInfo(OcrResult ocrResult, CoordinateTransform transform = null, LayoutAnalysisResult layoutResult = null)
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

                    // 建立合併資訊映射（如果有版面分析結果）
                    var mergedIndices = new HashSet<int>();
                    if (layoutResult?.Success == true && layoutResult.DebugInfo?.MergedOriginalIndices != null)
                    {
                        mergedIndices = layoutResult.DebugInfo.MergedOriginalIndices;
                        Logger.Info($"🔗 檢測到 {mergedIndices.Count} 個合併的原始索引：[{string.Join(", ", mergedIndices)}]");
                    }
                    else
                    {
                        Logger.Info("📝 沒有版面分析結果，所有框將顯示為未合併狀態（細虛線）");
                    }

                    // 繪製每個識別框
                    for (int i = 0; i < sortedLines.Count; i++)
                    {
                        var line = sortedLines[i];
                        var originalIndex = Array.IndexOf(ocrResult.Lines, line);
                        var isMerged = mergedIndices.Contains(originalIndex);
                        
                        Logger.Debug($"🔍 框{i + 1}：原始索引={originalIndex}, 合併狀態={isMerged}, 文字=\"{line.Text?.Trim()}\"");
                        
                        DrawOcrBox(line, i + 1, transform, isMerged); // 傳遞合併狀態
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
            if (ocrResult?.Lines == null || !ocrResult.Lines.Any())
                return;

            Dispatcher.Invoke(() =>
            {
                try
                {
                    // 清除之前的調試元素
                    ClearDebugElements();
                    
                    Logger.Info($"🎯 顯示版面分析結果：欄位聚類虛線框 + 段落框雙重顯示模式");

                    // 第一步：為所有原始OCR文字行繪製虛線框
                    var sortedLines = ocrResult.Lines
                        .Where(line => !string.IsNullOrWhiteSpace(line.Text))
                        .OrderBy(line => GetCenterY(line.BoundingBox))
                        .ThenBy(line => GetCenterX(line.BoundingBox))
                        .ToList();

                    // 建立原始索引到欄位顏色的映射
                    var indexToColumnColorMap = new Dictionary<int, SolidColorBrush>();
                    
                    if (layoutResult?.Layout != null && layoutResult.Layout.Any())
                    {
                        int columnIndex = 0;
                        foreach (var column in layoutResult.Layout)
                        {
                            var columnColor = GetColumnColor(columnIndex);
                            Logger.Info($"📋 欄位 {column.Key}：{column.Value.Count} 個段落，顏色：{columnColor}");

                            // 為這個欄位中的所有行建立顏色映射
                            foreach (var paragraph in column.Value)
                            {
                                foreach (var line in paragraph.Lines)
                                {
                                    indexToColumnColorMap[line.OriginalIndex] = columnColor;
                                    Logger.Debug($"🎨 映射索引{line.OriginalIndex} → 欄位{columnIndex}顏色");
                                }
                            }
                            columnIndex++;
                        }
                    }

                    Logger.Info($"🔸 繪製 {sortedLines.Count} 個按欄位聚類的虛線框");
                    for (int i = 0; i < sortedLines.Count; i++)
                    {
                        var line = sortedLines[i];
                        var originalIndex = Array.IndexOf(ocrResult.Lines, line);
                        
                        // 獲取該行所屬欄位的顏色，如果沒有映射則使用預設顏色
                        var columnColor = indexToColumnColorMap.ContainsKey(originalIndex) 
                            ? indexToColumnColorMap[originalIndex] 
                            : Brushes.Gray; // 未歸類的行使用灰色
                        
                        Logger.Debug($"🔸 虛線框：索引={originalIndex}, 欄位顏色={columnColor}, 文字=\"{line.Text?.Trim()}\"");
                        DrawOcrBoxWithColor(line, columnColor, transform); // 使用欄位顏色繪製虛線框（移除編號）
                    }

                    // 第二步：如果有版面分析結果，繪製段落框包圍合併後的區域
                    if (layoutResult?.Layout != null && layoutResult.Layout.Any())
                    {
                        Logger.Info($"📦 繪製版面分析段落框：{layoutResult.Layout.Count} 個欄位");
                        
                        int columnIndex = 0;
                        foreach (var column in layoutResult.Layout)
                        {
                            var columnColor = GetColumnColor(columnIndex);

                            foreach (var paragraph in column.Value)
                            {
                                // 只為多行段落繪製段落框（單行段落不需要額外的段落框）
                                if (paragraph.Lines.Count > 1)
                                {
                                    DrawParagraphBoundingBox(paragraph, columnColor, transform);
                                    Logger.Info($"📦 繪製段落框：{paragraph.Lines.Count} 行，段落ID={paragraph.ParagraphId}，範圍=({paragraph.BoundingBox.X},{paragraph.BoundingBox.Y},{paragraph.BoundingBox.Width}×{paragraph.BoundingBox.Height})");
                                }
                                else
                                {
                                    Logger.Debug($"⏭️ 跳過單行段落框：段落ID={paragraph.ParagraphId}（已有虛線框顯示）");
                                }
                            }
                            columnIndex++;
                        }
                    }
                    else
                    {
                        Logger.Info("📝 沒有版面分析結果，使用預設顏色顯示虛線框");
                    }

                    Logger.Info($"📊 顯示完成：{sortedLines.Count} 個按欄位聚類的虛線框 + 版面分析段落框");

                    // 顯示視窗
                    Show();
                    Activate();
                    
                    Logger.Info("✅ 版面分析調試視覺化已顯示（雙重框線模式）");
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
        /// <param name="isMerged">是否為合併後的框</param>
        private void DrawOcrBox(OcrLine line, int index, CoordinateTransform transform = null, bool isMerged = false)
        {
            var boundingBox = line.BoundingBox;
            
            // 計算相對於覆蓋層視窗的座標
            System.Windows.Point relativePosition;
            if (transform != null)
            {
                // 先計算絕對螢幕座標
                var absolutePosition = transform.TransformToScreenCoordinates(boundingBox.X, boundingBox.Y);
                
                // 轉換為相對於覆蓋層視窗的座標
                relativePosition = new System.Windows.Point(
                    absolutePosition.X - _targetScreenBounds.X,
                    absolutePosition.Y - _targetScreenBounds.Y
                );
                
                Logger.Debug($"🔍 框{index}座標轉換：絕對({absolutePosition.X:F1},{absolutePosition.Y:F1}) → 相對({relativePosition.X:F1},{relativePosition.Y:F1})");
            }
            else
            {
                // 回退到簡單偏移（舊版本兼容）
                relativePosition = new System.Windows.Point(boundingBox.X, boundingBox.Y);
                Logger.Debug($"🔍 框{index}使用簡單座標：({relativePosition.X:F1},{relativePosition.Y:F1})");
            }
            
            // 創建邊界框，根據合併狀態設置線條樣式
            var rect = new Rectangle
            {
                Width = boundingBox.Width / (transform?.DpiScale ?? 1.0), // 考慮DPI縮放
                Height = boundingBox.Height / (transform?.DpiScale ?? 1.0),
                Stroke = GetColorByIndex(index),
                StrokeThickness = 1, // 細線
                Fill = Brushes.Transparent
            };

            // 根據合併狀態設置線條樣式
            if (isMerged)
            {
                // 合併過的框用細實線
                rect.StrokeDashArray = null;
                Logger.Debug($"🔗 框{index}：合併框-實線樣式，顏色={rect.Stroke}");
            }
            else
            {
                // 沒有合併的框用細虛線
                rect.StrokeDashArray = new DoubleCollection { 5, 3 }; // 虛線樣式：5個單位實線，3個單位空白
                Logger.Debug($"📝 框{index}：未合併框-虛線樣式(5,3)，顏色={rect.Stroke}");
            }

            // 設置相對位置（相對於覆蓋層視窗）
            Canvas.SetLeft(rect, relativePosition.X);
            Canvas.SetTop(rect, relativePosition.Y);

            // 創建編號標籤，根據合併狀態設置不同的外觀
            var label = new Border
            {
                Background = isMerged ? Brushes.DarkGreen : GetColorByIndex(index), // 合併框用深綠色標籤
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(4, 2, 4, 2),
                Child = new TextBlock
                {
                    Text = isMerged ? $"{index}✓" : index.ToString(), // 合併框加勾號
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    FontSize = 12
                }
            };

            // 設置標籤位置（在框的左上角，使用相對座標）
            Canvas.SetLeft(label, relativePosition.X);
            Canvas.SetTop(label, relativePosition.Y - 25);

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
        /// 繪製OCR識別框（按欄位聚類，無編號版本）
        /// </summary>
        /// <param name="line">OCR識別行</param>
        /// <param name="columnColor">欄位顏色</param>
        /// <param name="transform">座標轉換參數</param>
        private void DrawOcrBoxWithColor(OcrLine line, SolidColorBrush columnColor, CoordinateTransform transform = null)
        {
            var boundingBox = line.BoundingBox;
            
            // 計算相對於覆蓋層視窗的座標
            System.Windows.Point relativePosition;
            if (transform != null)
            {
                // 先計算絕對螢幕座標
                var absolutePosition = transform.TransformToScreenCoordinates(boundingBox.X, boundingBox.Y);
                
                // 轉換為相對於覆蓋層視窗的座標
                relativePosition = new System.Windows.Point(
                    absolutePosition.X - _targetScreenBounds.X,
                    absolutePosition.Y - _targetScreenBounds.Y
                );
                
                Logger.Debug($"🔍 欄位虛線框座標轉換：絕對({absolutePosition.X:F1},{absolutePosition.Y:F1}) → 相對({relativePosition.X:F1},{relativePosition.Y:F1})");
            }
            else
            {
                // 回退到簡單偏移（舊版本兼容）
                relativePosition = new System.Windows.Point(boundingBox.X, boundingBox.Y);
                Logger.Debug($"🔍 欄位虛線框使用簡單座標：({relativePosition.X:F1},{relativePosition.Y:F1})");
            }
            
            // 創建邊界框，使用欄位顏色的虛線
            var rect = new Rectangle
            {
                Width = boundingBox.Width / (transform?.DpiScale ?? 1.0), // 考慮DPI縮放
                Height = boundingBox.Height / (transform?.DpiScale ?? 1.0),
                Stroke = columnColor,
                StrokeThickness = 1, // 細線
                Fill = Brushes.Transparent,
                StrokeDashArray = new DoubleCollection { 5, 3 } // 虛線樣式：5個單位實線，3個單位空白
            };

            // 設置相對位置（相對於覆蓋層視窗）
            Canvas.SetLeft(rect, relativePosition.X);
            Canvas.SetTop(rect, relativePosition.Y);

            // 添加到Canvas（不添加編號標籤）
            _debugCanvas.Children.Add(rect);

            // 記錄元素以便後續清除
            _debugElements.Add(rect);
            
            Logger.Debug($"📝 欄位虛線框：顏色={columnColor}，文字=\"{line.Text?.Trim()}\"");
        }

        /// <summary>
        /// 繪製段落邊界框（大框包圍整個段落）
        /// </summary>
        private void DrawParagraphBoundingBox(LayoutParagraph paragraph, SolidColorBrush color, CoordinateTransform transform = null)
        {
            var boundingBox = paragraph.BoundingBox;
            
            // 計算相對於覆蓋層視窗的座標
            System.Windows.Point relativePosition;
            if (transform != null)
            {
                // 先計算絕對螢幕座標
                var absolutePosition = transform.TransformToScreenCoordinates(boundingBox.X, boundingBox.Y);
                
                // 轉換為相對於覆蓋層視窗的座標
                relativePosition = new System.Windows.Point(
                    absolutePosition.X - _targetScreenBounds.X,
                    absolutePosition.Y - _targetScreenBounds.Y
                );
                
                Logger.Debug($"🔍 段落框座標轉換：絕對({absolutePosition.X:F1},{absolutePosition.Y:F1}) → 相對({relativePosition.X:F1},{relativePosition.Y:F1})");
            }
            else
            {
                // 回退到簡單偏移（舊版本兼容）
                relativePosition = new System.Windows.Point(boundingBox.X, boundingBox.Y);
                Logger.Debug($"🔍 段落框使用簡單座標：({relativePosition.X:F1},{relativePosition.Y:F1})");
            }

            // 創建段落邊界框（粗實線框，無填充，無標籤）
            var rect = new Rectangle
            {
                Width = boundingBox.Width / (transform?.DpiScale ?? 1.0),
                Height = boundingBox.Height / (transform?.DpiScale ?? 1.0),
                Stroke = color,
                StrokeThickness = 4, // 粗線表示段落邊界
                Fill = Brushes.Transparent, // 無填充，去除底色
                StrokeDashArray = null // 實線
            };

            // 設置相對位置（相對於覆蓋層視窗）
            Canvas.SetLeft(rect, relativePosition.X);
            Canvas.SetTop(rect, relativePosition.Y);

            // 添加到Canvas（移除段落標籤）
            _debugCanvas.Children.Add(rect);

            // 記錄元素以便後續清除
            _debugElements.Add(rect);
            
            Logger.Debug($"📦 段落框：無標籤，顏色={color}，段落ID={paragraph.ParagraphId}");
        }

        /// <summary>
        /// 繪製文字行框（同一欄使用相同顏色）
        /// </summary>
        private void DrawLineWithColumnColor(LayoutLine line, SolidColorBrush columnColor, CoordinateTransform transform = null)
        {
            var boundingBox = line.BoundingBox;
            
            // 計算相對於覆蓋層視窗的座標
            System.Windows.Point relativePosition;
            if (transform != null)
            {
                // 先計算絕對螢幕座標
                var absolutePosition = transform.TransformToScreenCoordinates(boundingBox.X, boundingBox.Y);
                
                // 轉換為相對於覆蓋層視窗的座標
                relativePosition = new System.Windows.Point(
                    absolutePosition.X - _targetScreenBounds.X,
                    absolutePosition.Y - _targetScreenBounds.Y
                );
            }
            else
            {
                // 回退到簡單偏移（舊版本兼容）
                relativePosition = new System.Windows.Point(boundingBox.X, boundingBox.Y);
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

            // 設置相對位置（相對於覆蓋層視窗）
            Canvas.SetLeft(rect, relativePosition.X);
            Canvas.SetTop(rect, relativePosition.Y);

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
        /// 顯示版面分析V2結果的調試信息 (AI驅動模式)
        /// 只顯示智能分欄結果,每個欄位用不同顏色標示
        /// </summary>
        /// <param name="ocrResult">OCR識別結果</param>
        /// <param name="layoutResultV2">版面分析V2結果</param>
        /// <param name="transform">座標轉換參數</param>
        public void ShowLayoutAnalysisDebugInfoV2(OcrResult ocrResult, LayoutAnalysisResultV2 layoutResultV2, CoordinateTransform transform = null)
        {
            if (ocrResult?.Lines == null || !ocrResult.Lines.Any())
                return;
            
            if (layoutResultV2?.Columns == null || !layoutResultV2.Columns.Any())
            {
                Logger.Warn("⚠️ 沒有分欄結果,無法顯示調試信息");
                return;
            }

            Dispatcher.Invoke(() =>
            {
                try
                {
                    // 清除之前的調試元素
                    ClearDebugElements();
                    
                    Logger.Info($"🎯 顯示AI驅動版面分析結果：{layoutResultV2.Columns.Count} 個欄位,用顏色標示");

                    // 為每個欄位分配顏色並繪製
                    for (int columnIndex = 0; columnIndex < layoutResultV2.Columns.Count; columnIndex++)
                    {
                        var column = layoutResultV2.Columns[columnIndex];
                        var columnColor = GetColumnColor(columnIndex);
                        
                        Logger.Info($"📋 欄位 {columnIndex + 1}：{column.Lines.Count} 行文字，顏色：{columnColor}");

                        // 繪製該欄位中的所有行 (用欄位顏色標示,不帶編號)
                        foreach (var line in column.Lines)
                        {
                            Logger.Debug($"🎨 繪製欄位{columnIndex + 1}的行：文字=\"{line.Text?.Trim()}\"");
                            DrawLayoutLineWithColor(line, columnColor, transform);
                        }
                        
                        // 不繪製欄位邊界框和標籤,只用顏色區分各欄位的文字行
                        // 用戶要求: 移除大包圍框和"欄位1"文字標籤
                    }

                    Logger.Info($"✅ 版面分析V2調試顯示完成：{layoutResultV2.Columns.Count} 個欄位");

                    // 顯示視窗
                    Show();
                    Activate();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "顯示版面分析V2調試信息時發生錯誤");
                }
            });
        }

        /// <summary>
        /// 繪製欄位邊界框 - 已禁用 (用戶要求只顯示顏色標示,不要大框和標籤)
        /// </summary>
        private void DrawColumnBoundingBox(System.Drawing.Rectangle boundingBox, SolidColorBrush color, CoordinateTransform transform, int columnNumber)
        {
            // 🚫 用戶要求: 移除大包圍框和"欄位1"文字標籤
            // 只保留各文字行的顏色標示即可
            Logger.Debug($"🎨 欄位{columnNumber}：僅使用顏色標示,不繪製邊界框");
        }

        /// <summary>
        /// 繪製 LayoutLine（支援 LayoutLine 類型）
        /// </summary>
        private void DrawLayoutLineWithColor(LayoutLine line, SolidColorBrush columnColor, CoordinateTransform transform = null)
        {
            var boundingBox = line.BoundingBox;
            
            // 計算相對於覆蓋層視窗的座標
            System.Windows.Point relativePosition;
            if (transform != null)
            {
                // 先計算絕對螢幕座標
                var absolutePosition = transform.TransformToScreenCoordinates(boundingBox.X, boundingBox.Y);
                
                // 轉換為相對於覆蓋層視窗的座標
                relativePosition = new System.Windows.Point(
                    absolutePosition.X - _targetScreenBounds.X,
                    absolutePosition.Y - _targetScreenBounds.Y
                );
            }
            else
            {
                // 回退到簡單偏移
                relativePosition = new System.Windows.Point(boundingBox.X, boundingBox.Y);
            }
            
            // 創建邊界框，使用欄位顏色的虛線
            var rect = new Rectangle
            {
                Width = boundingBox.Width / (transform?.DpiScale ?? 1.0),
                Height = boundingBox.Height / (transform?.DpiScale ?? 1.0),
                Stroke = columnColor,
                StrokeThickness = 1,
                Fill = Brushes.Transparent,
                StrokeDashArray = new DoubleCollection { 5, 3 } // 虛線樣式
            };

            Canvas.SetLeft(rect, relativePosition.X);
            Canvas.SetTop(rect, relativePosition.Y);
            _debugCanvas.Children.Add(rect);
            _debugElements.Add(rect);
            
            Logger.Debug($"📝 欄位虛線框：顏色={columnColor}，文字=\"{line.Text?.Trim()}\"");
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
