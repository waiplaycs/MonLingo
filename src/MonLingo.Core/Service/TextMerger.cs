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
            catch (Exception ex)
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
        /// </summary>
        private static bool ShouldGroupTogether(OcrLine line1, OcrLine line2)
        {
            // 計算垂直距離
            var verticalDistance = Math.Abs(GetCenterY(line2.BoundingBox) - GetCenterY(line1.BoundingBox));
            
            // 計算水平距離
            var horizontalDistance = Math.Abs(GetCenterX(line2.BoundingBox) - GetCenterX(line1.BoundingBox));
            
            // 計算平均行高
            var avgHeight = (line1.BoundingBox.Height + line2.BoundingBox.Height) / 2.0;
            
            // 如果垂直距離小於 1.5 倍行高，且水平距離合理，則認為是同一行
            var verticalThreshold = avgHeight * 1.5;
            var horizontalThreshold = Math.Max(line1.BoundingBox.Width, line2.BoundingBox.Width) * 2;
            
            return verticalDistance < verticalThreshold && horizontalDistance < horizontalThreshold;
        }
        
        /// <summary>
        /// 拼接文字組
        /// </summary>
        private static string JoinTextGroups(List<List<OcrLine>> groups)
        {
            var lines = new List<string>();
            
            foreach (var group in groups)
            {
                // 對組內的文字按水平位置排序
                var sortedGroup = group.OrderBy(line => GetCenterX(line.BoundingBox)).ToList();
                
                // 拼接組內文字
                var groupText = string.Join(" ", sortedGroup.Select(line => line.Text.Trim()));
                
                if (!string.IsNullOrWhiteSpace(groupText))
                {
                    lines.Add(groupText);
                }
            }
            
            // 用換行符連接不同組
            return string.Join(Environment.NewLine, lines);
        }
    }
}
