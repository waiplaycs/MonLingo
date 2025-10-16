using MonLingo.Core.Model;
using System.Collections.Generic;
using System.Linq;

namespace MonLingo.Core.Service.AI
{
    /// <summary>
    /// AI翻譯Prompt模板
    /// </summary>
    public static class PromptTemplates
    {
        /// <summary>
        /// 系統提示詞 - 極簡版
        /// </summary>
        public const string SYSTEM_PROMPT = @"You are a professional translator. Return JSON only, no explanations.";

        /// <summary>
        /// 構建智能翻譯的Prompt
        /// </summary>
        /// <param name="lines">文字行列表</param>
        /// <param name="sourceLang">源語言</param>
        /// <param name="targetLang">目標語言</param>
        /// <returns>完整的用戶Prompt</returns>
        public static string BuildSmartTranslationPrompt(
            List<LayoutLine> lines,
            string sourceLang,
            string targetLang)
        {
            var avgLineHeight = lines.Average(l => l.LineHeight);
            
            // 極簡文字列表 (只標記大間距)
            var linesText = new System.Text.StringBuilder();
            for (int i = 0; i < lines.Count; i++)
            {
                linesText.Append($"{i}:{lines[i].Text}");
                
                // 只標記必須分段的大間距
                if (i < lines.Count - 1)
                {
                    var verticalGap = lines[i + 1].BoundingBox.Top - lines[i].BoundingBox.Bottom;
                    if (verticalGap / avgLineHeight > 1.0)
                    {
                        linesText.Append(" [GAP]");
                    }
                }
                linesText.AppendLine();
            }

            return $@"Translate to {GetLanguageName(targetLang)}:

{linesText}
Rules: Merge lines by semantic, split at [GAP]

JSON: {{""detectedLanguage"":"""",""paragraphs"":[{{""lineIndices"":[],""originalText"":"""",""translatedText"":"""",""type"":""Body"",""confidence"":0.9}}]}}";
        }

        /// <summary>
        /// 構建漫畫對話優化的Prompt
        /// </summary>
        public static string BuildMangaOptimizedPrompt(List<LayoutLine> lines, string targetLang)
        {
            var linesText = string.Join("\n", 
                lines.Select((l, i) => $"{i}: {l.Text}"));

            return $@"**任務**: 處理漫畫對話氣泡文字

**文字行列表**:
{linesText}

**漫畫特殊規則**:
1. 對話氣泡通常是短句,不要過度合併
2. 感嘆詞和語氣詞(啊、呀、嗯等)保持獨立
3. 不同分格的對話絕對不要合併
4. 音效詞(ドキドキ、バーン等)單獨翻譯
5. 保持對話的節奏感和斷句

**輸出格式**(嚴格JSON):
{{
  ""detectedLanguage"": ""ja"",
  ""paragraphs"": [
    {{
      ""lineIndices"": [0],
      ""originalText"": ""原文"",
      ""translatedText"": ""翻譯"",
      ""type"": ""Body"",
      ""confidence"": 0.95
    }}
  ]
}}";
        }

        /// <summary>
        /// 構建遊戲UI優化的Prompt
        /// </summary>
        public static string BuildGameUIOptimizedPrompt(List<LayoutLine> lines, string targetLang)
        {
            var linesText = string.Join("\n", 
                lines.Select((l, i) => $"{i}: {l.Text}"));

            return $@"**任務**: 處理遊戲UI文字

**文字行列表**:
{linesText}

**遊戲UI特殊規則**:
1. 按鈕文字通常單獨成段(如 Start、Options、Quit)
2. 選項列表保持獨立(選項1、選項2...)
3. 提示文字可能需要合併成完整句子
4. 數值和單位保持在一起(100 HP、50 MP)
5. UI專有術語保持一致性(統一翻譯為開始/啟動等)

**輸出格式**(嚴格JSON):
{{
  ""detectedLanguage"": ""en"",
  ""paragraphs"": [...]
}}";
        }

        /// <summary>
        /// 構建多欄位智能翻譯的Prompt
        /// 使用欄位標記系統,一次性處理多個欄位
        /// 
        /// 調試訊息說明:
        /// - 使用【欄位X開始】和【欄位X結束】明確分隔不同欄位
        /// - 只發送行間距比例 (如 [大間距↓ 2.3x行高]),不發送完整座標 (Left,Top,Right,Bottom)
        /// - 間距標記規則:
        ///   * [大間距↓ Xx行高] - 間距 > 1.0倍行高,AI必須分段
        ///   * [中間距↓ Xx行高] - 間距 0.5~1.0倍行高,AI優先分段
        ///   * 無標記 - 正常行間距,AI可語義合併
        /// 
        /// 示例Prompt輸出:
        /// 【欄位1開始】
        /// 0: 標題文字 [大間距↓ 2.3x行高]
        /// 1: 正文內容
        /// 2: 繼續內容 [中間距↓ 0.8x行高]
        /// 3: 新段落
        /// 【欄位1結束】
        /// 
        /// 【欄位2開始】
        /// 0: 另一欄內容...
        /// 【欄位2結束】
        /// </summary>
        /// <param name="columns">多個欄位的文字行列表</param>
        /// <param name="sourceLang">源語言</param>
        /// <param name="targetLang">目標語言</param>
        /// <returns>完整的多欄位Prompt</returns>
        public static string BuildMultiColumnTranslationPrompt(
            List<List<LayoutLine>> columns,
            string sourceLang,
            string targetLang)
        {
            var prompt = new System.Text.StringBuilder();
            
            // 極簡任務說明
            prompt.AppendLine($"Translate {columns.Count} columns to {GetLanguageName(targetLang)}. Each column is independent.");
            prompt.AppendLine();

            // 構建欄位內容 (移除冗長的標記)
            for (int colIdx = 0; colIdx < columns.Count; colIdx++)
            {
                var column = columns[colIdx];
                if (column == null || column.Count == 0) continue;
                
                prompt.AppendLine($"[Col{colIdx + 1}]");
                
                // 計算該欄位的平均行高
                var avgLineHeight = column.Average(l => l.LineHeight);
                
                // 只標記大間距 (>1.0x),移除中間距標記
                for (int i = 0; i < column.Count; i++)
                {
                    var line = column[i];
                    prompt.Append($"{i}:{line.Text}");
                    
                    // 只標記必須分段的大間距
                    if (i < column.Count - 1)
                    {
                        var nextLine = column[i + 1];
                        var verticalGap = nextLine.BoundingBox.Top - line.BoundingBox.Bottom;
                        var gapRatio = verticalGap / avgLineHeight;
                        
                        if (gapRatio > 1.0)
                        {
                            prompt.Append(" [GAP]");
                        }
                    }
                    
                    prompt.AppendLine();
                }
                
                prompt.AppendLine();
            }

            // 極簡規則 (從5大類減少到2條核心規則)
            prompt.AppendLine("Rules:");
            prompt.AppendLine("1. Merge lines by semantic, split at [GAP]");
            prompt.AppendLine("2. Never merge across columns");
            prompt.AppendLine();
            
            // 清晰的JSON格式示例 (多行但簡潔)
            prompt.AppendLine("JSON format:");
            prompt.AppendLine("{");
            prompt.AppendLine("  \"detectedLanguage\": \"en\",");
            prompt.AppendLine("  \"columns\": [");
            prompt.AppendLine("    {");
            prompt.AppendLine("      \"columnIndex\": 0,");
            prompt.AppendLine("      \"paragraphs\": [");
            prompt.AppendLine("        {");
            prompt.AppendLine("          \"lineIndices\": [0,1],");
            prompt.AppendLine("          \"originalText\": \"merged text\",");
            prompt.AppendLine("          \"translatedText\": \"翻譯文字\",");
            prompt.AppendLine("          \"type\": \"Body\",");
            prompt.AppendLine("          \"confidence\": 0.9");
            prompt.AppendLine("        }");
            prompt.AppendLine("      ]");
            prompt.AppendLine("    }");
            prompt.AppendLine("  ]");
            prompt.AppendLine("}");

            return prompt.ToString();
        }

        /// <summary>
        /// 獲取語言的中文名稱
        /// </summary>
        private static string GetLanguageName(string langCode)
        {
            return langCode?.ToLower() switch
            {
                "zh-tw" or "zh-hant" => "繁體中文",
                "zh-cn" or "zh-hans" => "簡體中文",
                "en" => "英文",
                "ja" => "日文",
                "ko" => "韓文",
                "fr" => "法文",
                "de" => "德文",
                "es" => "西班牙文",
                "ru" => "俄文",
                _ => langCode ?? "目標語言"
            };
        }
    }
}
