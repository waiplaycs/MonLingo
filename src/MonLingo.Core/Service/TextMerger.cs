using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using MonLingo.Core.Service;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// 文字合併器 - 將零散的 OCR 文字塊合併成連貫的句子
    /// 實現字幕模式的特殊邏輯，根據文檔中描述的算法
    /// </summary>
    public static class TextMerger
    {
        /// <summary>
        /// 為字幕模式合併文字
        /// 將空間上相近的文字塊組合成一個單一的字串
        /// </summary>
        /// <param name="ocrResult">OCR 識別結果</param>
        /// <returns>合併後的文字</returns>
        public static string MergeForSubtitle(OcrResult ocrResult)
        {
            if (ocrResult == null || ocrResult.Lines == null || !ocrResult.Lines.Any())
            {
                return string.Empty;
            }
            
            try
            {
                // 1. 按置信度過濾文字行
                var filteredLines = FilterByConfidence(ocrResult.Lines);
                
                if (!filteredLines.Any())
                {
                    return string.Empty;
                }
                
                // 2. 按空間位置排序
                var sortedLines = SortByPosition(filteredLines);
                
                // 3. 合併相近的文字行
                var mergedGroups = GroupNearbyLines(sortedLines);
                
                // 4. 拼接最終文字
                var finalText = JoinTextGroups(mergedGroups);
                
                return finalText.Trim();
            }
            catch (Exception)
            {
                // 如果合併失敗，返回簡單拼接結果
                return string.Join(" ", ocrResult.Lines.Select(line => line.Text).Where(text => !string.IsNullOrWhiteSpace(text)));
            }
        }
        
        /// <summary>
        /// 按置信度過濾文字行
        /// </summary>
        private static List<OcrLine> FilterByConfidence(OcrLine[] lines, double minConfidence = 0.7)
        {
            return lines.Where(line => 
                !string.IsNullOrWhiteSpace(line.Text) && 
                line.Confidence >= minConfidence
            ).ToList();
        }
        
        /// <summary>
        /// 按位置排序文字行（從上到下，從左到右）
        /// </summary>
        private static List<OcrLine> SortByPosition(List<OcrLine> lines)
        {
            return lines.OrderBy(line => GetCenterY(line.BoundingBox))
                       .ThenBy(line => GetCenterX(line.BoundingBox))
                       .ToList();
        }
        
        /// <summary>
        /// 獲取邊界框的中心 X 座標
        /// </summary>
        private static double GetCenterX(Rectangle boundingBox)
        {
            return boundingBox.X + boundingBox.Width / 2.0;
        }
        
        /// <summary>
        /// 獲取邊界框的中心 Y 座標
        /// </summary>
        private static double GetCenterY(Rectangle boundingBox)
        {
            return boundingBox.Y + boundingBox.Height / 2.0;
        }
        
        /// <summary>
        /// 將相近的文字行分組
        /// </summary>
        private static List<List<OcrLine>> GroupNearbyLines(List<OcrLine> sortedLines)
        {
            var groups = new List<List<OcrLine>>();
            
            if (!sortedLines.Any())
            {
                return groups;
            }
            
            var currentGroup = new List<OcrLine> { sortedLines[0] };
            
            for (int i = 1; i < sortedLines.Count; i++)
            {
                var currentLine = sortedLines[i];
                var lastLineInGroup = currentGroup.Last();
                
                // 檢查是否應該加入當前組
                if (ShouldGroupTogether(lastLineInGroup, currentLine))
                {
                    currentGroup.Add(currentLine);
                }
                else
                {
                    // 開始新組
                    groups.Add(currentGroup);
                    currentGroup = new List<OcrLine> { currentLine };
                }
            }
            
            // 添加最後一組
            if (currentGroup.Any())
            {
                groups.Add(currentGroup);
            }
            
            return groups;
        }
        
        /// <summary>
        /// 判斷兩個文字行是否應該分在同一組
        /// 優化邏輯以更好地處理複雜佈局（如 YouTube 評論、列表結構）
        /// </summary>
        private static bool ShouldGroupTogether(OcrLine line1, OcrLine line2)
        {
            // 🎯 特殊檢測：如果是列表結構（以 * 或 # 開頭），應該保持獨立行
            if (IsListItem(line1.Text) || IsListItem(line2.Text))
            {
                return false; // 列表項目不應該與其他行合併
            }
            
            // 計算垂直距離
            var verticalDistance = Math.Abs(GetCenterY(line2.BoundingBox) - GetCenterY(line1.BoundingBox));
            
            // 計算水平距離  
            var horizontalDistance = Math.Abs(GetCenterX(line2.BoundingBox) - GetCenterX(line1.BoundingBox));
            
            // 計算平均行高
            var avgHeight = (line1.BoundingBox.Height + line2.BoundingBox.Height) / 2.0;
            
            // 🎯 優化：使用更嚴格的垂直閾值，避免不同段落文字被錯誤合併
            var verticalThreshold = avgHeight * 1.0; // 進一步降低到1.0，更嚴格的行分離
            
            // 🎯 優化：水平閾值也更保守，避免距離較遠的文字被強制合併
            var maxWidth = Math.Max(line1.BoundingBox.Width, line2.BoundingBox.Width);
            var horizontalThreshold = maxWidth * 1.2; // 進一步降低到1.2
            
            // 🎯 新增：如果兩行文字在水平方向上重疊度很低，不應該合併
            var line1Right = line1.BoundingBox.X + line1.BoundingBox.Width;
            var line1Left = line1.BoundingBox.X;
            var line2Right = line2.BoundingBox.X + line2.BoundingBox.Width;
            var line2Left = line2.BoundingBox.X;
            
            // 計算水平重疊區間
            var overlapLeft = Math.Max(line1Left, line2Left);
            var overlapRight = Math.Min(line1Right, line2Right);
            var overlapWidth = Math.Max(0, overlapRight - overlapLeft);
            var minWidth = Math.Min(line1.BoundingBox.Width, line2.BoundingBox.Width);
            var overlapRatio = minWidth > 0 ? overlapWidth / minWidth : 0;
            
            // 🎯 新增：檢測是否為獨立的文件名或配置項
            if (IsConfigurationItem(line1.Text) || IsConfigurationItem(line2.Text))
            {
                return false; // 配置項應該保持獨立
            }
            
            // 如果垂直距離很小且有足夠水平重疊，認為是同一行
            var isCloseVertically = verticalDistance < verticalThreshold;
            var hasReasonableHorizontalGap = horizontalDistance < horizontalThreshold;
            var hasSignificantOverlap = overlapRatio > 0.3; // 提高重疊要求到30%
            
            return isCloseVertically && hasReasonableHorizontalGap && hasSignificantOverlap;
        }
        
        /// <summary>
        /// 檢測是否為列表項目（以 * 或 # 開頭）
        /// </summary>
        private static bool IsListItem(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;
                
            var trimmed = text.Trim();
            return trimmed.StartsWith("*") || trimmed.StartsWith("#") || 
                   trimmed.StartsWith("-.") || trimmed.StartsWith("•");
        }
        
        /// <summary>
        /// 檢測是否為配置項目（文件擴展名、設置項等）
        /// </summary>
        private static bool IsConfigurationItem(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;
                
            var trimmed = text.Trim();
            
            // 檢測文件擴展名模式
            if (trimmed.Contains("*.") && trimmed.Length < 50)
                return true;
                
            // 檢測項目設置文件模式
            if (trimmed.Contains("project.") || trimmed.Contains(".cache") || 
                trimmed.Contains(".json") || trimmed.Contains(".props") ||
                trimmed.Contains(".targets") || trimmed.Contains(".config"))
                return true;
                
            return false;
        }
        
        /// <summary>
        /// 拼接文字組
        /// 針對不同類型的內容使用不同的拼接策略
        /// </summary>
        private static string JoinTextGroups(List<List<OcrLine>> groups)
        {
            var lines = new List<string>();
            
            foreach (var group in groups)
            {
                // 對組內的文字按水平位置排序
                var sortedGroup = group.OrderBy(line => GetCenterX(line.BoundingBox)).ToList();
                
                // 檢查是否為列表或配置內容
                var isListContent = sortedGroup.Any(line => IsListItem(line.Text) || IsConfigurationItem(line.Text));
                
                if (isListContent)
                {
                    // 對於列表內容，每個文字塊應該是獨立的一行
                    foreach (var line in sortedGroup)
                    {
                        var cleanText = line.Text.Trim();
                        if (!string.IsNullOrWhiteSpace(cleanText))
                        {
                            lines.Add(cleanText);
                        }
                    }
                }
                else
                {
                    // 對於普通文字內容，可以在同一行合併
                    var groupText = string.Join(" ", sortedGroup.Select(line => line.Text.Trim()));
                    
                    if (!string.IsNullOrWhiteSpace(groupText))
                    {
                        lines.Add(groupText);
                    }
                }
            }
            
            // 用換行符連接不同組
            return string.Join(Environment.NewLine, lines);
        }
    }
}
