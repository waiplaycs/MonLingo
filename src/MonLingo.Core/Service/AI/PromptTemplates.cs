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
        /// 系統提示詞 - 定義AI的角色和能力
        /// </summary>
        public const string SYSTEM_PROMPT = @"你是一個專業的OCR文字處理和翻譯助手。你擅長:
1. 分析文字行的語義關係
2. 智能合併段落
3. 識別段落類型(標題、正文、列表等)
4. 提供高質量的翻譯

你的回覆必須是嚴格的JSON格式,不要添加任何額外的文字說明。";

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
            // 計算平均行高和行間距
            var avgLineHeight = lines.Average(l => l.LineHeight);
            
            // 構建帶有垂直距離信息的文字行列表
            var linesWithSpacing = new System.Text.StringBuilder();
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                linesWithSpacing.Append($"{i}: {line.Text}");
                
                // 計算與下一行的垂直間距
                if (i < lines.Count - 1)
                {
                    var nextLine = lines[i + 1];
                    var verticalGap = nextLine.BoundingBox.Top - line.BoundingBox.Bottom;
                    var gapRatio = verticalGap / avgLineHeight;
                    
                    // 標記明顯的垂直間距
                    if (gapRatio > 1.0)
                    {
                        linesWithSpacing.Append($" [大間距↓ {gapRatio:F1}x行高]");
                    }
                    else if (gapRatio > 0.5)
                    {
                        linesWithSpacing.Append($" [中間距↓ {gapRatio:F1}x行高]");
                    }
                }
                
                linesWithSpacing.AppendLine();
            }

            return $@"**任務**: 
1. 分析以下OCR識別的文字行
2. 根據語義和**視覺間距**智能合併為段落
3. 翻譯成{GetLanguageName(targetLang)}
4. 返回結構化結果

**文字行列表**(包含行間距信息):
{linesWithSpacing}

**處理規則**:
1. **語義分析**: 分析上下文,判斷哪些行屬於同一段落
2. **視覺間距分析** (⚠️ 關鍵規則):
   - **[大間距↓]** (>1.0x行高): **必須分段**,這表示視覺上有明顯空行
   - **[中間距↓]** (0.5-1.0x行高): 優先分段,除非語義強相關
   - 無標記: 行距正常,可以根據語義合併
3. **段落類型識別**:
   - Heading: 標題通常字體較大、簡短、獨立成段
   - Body: 正文段落,語義連貫的多行文字
   - ListItem: 列表項目,有序號、符號或明顯的獨立性
   - Quote: 引用或注釋,通常有引號或縮排
4. **合併策略** (按優先級):
   - ⚠️ **大間距必須分段** (最高優先級)
   - 標題通常單獨成段
   - 正文段落根據語義連貫性合併
   - 列表項目保持獨立或按邏輯分組
   - 中間距優先分段,除非是明確的句子延續
   - 語義不連貫則分段
5. **翻譯要求**:
   - 保持原文的語氣和風格
   - 專有名詞保持原樣或音譯
   - 保留格式標記(如**粗體**、*斜體*)

**輸出格式**(嚴格JSON,不要添加```json標記):
{{
  ""detectedLanguage"": ""語言代碼(如en、ja、zh-CN)"",
  ""paragraphs"": [
    {{
      ""lineIndices"": [0, 1, 2],
      ""originalText"": ""合併後的原文"",
      ""translatedText"": ""翻譯結果"",
      ""type"": ""Heading|Body|ListItem|Quote|Other"",
      ""confidence"": 0.95
    }}
  ]
}}

**重要**: 直接返回JSON對象,不要使用```json```包裹,不要添加任何解釋文字。";
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
        /// 獲取語言名稱
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
