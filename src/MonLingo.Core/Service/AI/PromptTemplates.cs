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
        /// 系統提示詞 - 標準版 (禁用思考模式以提升速度)
        /// </summary>
        public const string SYSTEM_PROMPT = @"You are a professional OCR text processing and translation assistant.

CRITICAL INSTRUCTIONS:
- Respond IMMEDIATELY without thinking process
- Output JSON ONLY, no explanations
- Be FAST and concise

Your tasks:
1. Analyze semantic relationships
2. Merge paragraphs intelligently
3. Translate accurately

Output strict JSON format only.";

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
            
            // 標準版任務說明
            prompt.AppendLine("**任務**: ");
            prompt.AppendLine($"1. 分析以下{columns.Count}個欄位的OCR識別文字");
            prompt.AppendLine("2. **每個欄位完全獨立處理**,不要跨欄位合併段落");
            prompt.AppendLine("3. **⚠️ 智能合併段落 - 語義優先,間距輔助**");
            prompt.AppendLine("   - **優先考慮語義**: 語義連貫的行應該合併(如 \"FROM THE\" + \"COLLECTIONS\" = \"FROM THE COLLECTIONS\")");
            prompt.AppendLine("   - **間距作為提示**: 文字行後標記 [大間距↓] 或 [中間距↓] 僅作為視覺排版參考");
            prompt.AppendLine("   - **智能判斷**: 即使有間距標記,若語義強相關(如標題片段、句子延續)也應合併");
            prompt.AppendLine($"4. 翻譯成{GetLanguageName(targetLang)}");
            prompt.AppendLine("5. 返回結構化結果");
            prompt.AppendLine();
            prompt.AppendLine("**重要**: 每個【欄位X】之間是完全獨立的,絕對不要混淆或合併不同欄位的內容!");
            prompt.AppendLine();
            prompt.AppendLine("---");
            prompt.AppendLine();
            prompt.AppendLine($"**多欄位文字** (共{columns.Count}個欄位):");
            prompt.AppendLine();

            // 構建欄位內容 - 標準版格式
            for (int colIdx = 0; colIdx < columns.Count; colIdx++)
            {
                var column = columns[colIdx];
                if (column == null || column.Count == 0) continue;
                
                prompt.AppendLine($"【欄位{colIdx + 1}開始】");
                
                // 計算該欄位的平均行高
                var avgLineHeight = column.Average(l => l.LineHeight);
                
                // 標準版間距標記
                for (int i = 0; i < column.Count; i++)
                {
                    var line = column[i];
                    prompt.Append($"{i}: {line.Text}");
                    
                    // 詳細的間距標記
                    if (i < column.Count - 1)
                    {
                        var nextLine = column[i + 1];
                        var verticalGap = nextLine.BoundingBox.Top - line.BoundingBox.Bottom;
                        var gapRatio = verticalGap / avgLineHeight;
                        
                        if (gapRatio > 1.0)
                        {
                            prompt.Append($" [大間距↓ {gapRatio:F1}x行高]");
                        }
                        else if (gapRatio > 0.5)
                        {
                            prompt.Append($" [中間距↓ {gapRatio:F1}x行高]");
                        }
                    }
                    
                    prompt.AppendLine();
                }
                
                prompt.AppendLine($"【欄位{colIdx + 1}結束】");
                prompt.AppendLine();
            }

            prompt.AppendLine("---");
            prompt.AppendLine();
            
            // 標準版處理規則
            prompt.AppendLine("**處理規則**:");
            prompt.AppendLine("1. **欄位獨立性** (⚠️ 最高優先級):");
            prompt.AppendLine("   - 每個【欄位X】是完全獨立的內容區域");
            prompt.AppendLine("   - 絕對不要跨欄位合併段落");
            prompt.AppendLine("   - 各欄位之間沒有任何語義關聯");
            prompt.AppendLine("   - 欄位1的最後一行和欄位2的第一行永遠不會合併");
            prompt.AppendLine("   - 必須為每個欄位生成獨立的paragraphs數組");
            prompt.AppendLine();
            prompt.AppendLine("2. **智能合併策略** (欄位內部) - ⚠️ 語義優先:");
            prompt.AppendLine("   - **語義連貫性 > 視覺間距**: 優先根據語義判斷是否合併");
            prompt.AppendLine("   - **標題片段**: \"FROM THE\" + \"COLLECTIONS\" → 合併為完整標題");
            prompt.AppendLine("   - **句子延續**: \"It's goodbye from...\" + \"closing and...\" → 合併為完整句子");
            prompt.AppendLine("   - **列表項目**: 有序號、符號或明確獨立性的保持分開");
            prompt.AppendLine("   - **大間距例外**: 只有當大間距(>1.0x)且語義不相關時才強制分段");
            prompt.AppendLine();
            prompt.AppendLine("3. **視覺間距參考** (輔助判斷):");
            prompt.AppendLine("   - **[大間距↓ X.Xx行高]** (>1.0x): 通常表示新段落,但若語義強相關仍可合併");
            prompt.AppendLine("   - **[中間距↓ X.Xx行高]** (0.5-1.0x): 僅作參考,語義連貫時應合併");
            prompt.AppendLine("   - **無標記**: 行距正常(<0.5x行高),通常應合併");
            prompt.AppendLine();
            prompt.AppendLine("4. **段落類型識別**:");
            prompt.AppendLine("   - Heading: 標題(可能跨多行,如 \"FROM THE COLLECTIONS\")");
            prompt.AppendLine("   - Body: 正文段落,語義連貫的多行文字");
            prompt.AppendLine("   - ListItem: 列表項目,有序號、符號或明顯的獨立性");
            prompt.AppendLine("   - Quote: 引用或注釋");
            prompt.AppendLine();
            prompt.AppendLine("5. **翻譯要求**:");
            prompt.AppendLine("   - 保持原文的語氣和風格");
            prompt.AppendLine("   - 專有名詞保持原樣或音譯");
            prompt.AppendLine("   - 保留格式標記(如**粗體**、*斜體*)");
            prompt.AppendLine();
            
            // 標準版JSON格式示例
            prompt.AppendLine("**輸出格式**(嚴格JSON,不要添加```json標記):");
            prompt.AppendLine("{");
            prompt.AppendLine("  \"detectedLanguage\": \"語言代碼(如en、ja、zh-CN)\",");
            prompt.AppendLine("  \"columns\": [");
            prompt.AppendLine("    {");
            prompt.AppendLine("      \"columnIndex\": 0,");
            prompt.AppendLine("      \"paragraphs\": [");
            prompt.AppendLine("        {");
            prompt.AppendLine("          \"lineIndices\": [0, 1, 2],");
            prompt.AppendLine("          \"originalText\": \"合併後的原文\",");
            prompt.AppendLine("          \"translatedText\": \"翻譯結果\",");
            prompt.AppendLine("          \"type\": \"Heading|Body|ListItem|Quote|Other\",");
            prompt.AppendLine("          \"confidence\": 0.95");
            prompt.AppendLine("        }");
            prompt.AppendLine("      ]");
            prompt.AppendLine("    },");
            prompt.AppendLine("    {");
            prompt.AppendLine("      \"columnIndex\": 1,");
            prompt.AppendLine("      \"paragraphs\": [");
            prompt.AppendLine("        {");
            prompt.AppendLine("          \"lineIndices\": [0, 1],");
            prompt.AppendLine("          \"originalText\": \"另一欄位的原文\",");
            prompt.AppendLine("          \"translatedText\": \"另一欄位的翻譯\",");
            prompt.AppendLine("          \"type\": \"Body\",");
            prompt.AppendLine("          \"confidence\": 0.9");
            prompt.AppendLine("        }");
            prompt.AppendLine("      ]");
            prompt.AppendLine("    }");
            prompt.AppendLine("  ]");
            prompt.AppendLine("}");
            prompt.AppendLine();
            prompt.AppendLine("**合併示例** (理解語義優先原則):");
            prompt.AppendLine("```");
            prompt.AppendLine("錯誤示例 (機械按間距分段):");
            prompt.AppendLine("  0: FROM THE");
            prompt.AppendLine("  1: COLLECTIONS [中間距↓ 0.9x]");
            prompt.AppendLine("  → 錯誤: 分成兩段 \"FROM THE\" 和 \"COLLECTIONS\"");
            prompt.AppendLine();
            prompt.AppendLine("正確示例 (語義合併):");
            prompt.AppendLine("  0: FROM THE");
            prompt.AppendLine("  1: COLLECTIONS [中間距↓ 0.9x]");
            prompt.AppendLine("  → 正確: 合併為 \"FROM THE COLLECTIONS\" (完整標題)");
            prompt.AppendLine("  → lineIndices: [0, 1]");
            prompt.AppendLine();
            prompt.AppendLine("另一個正確示例:");
            prompt.AppendLine("  3: It's goodbye from the Wellcome...");
            prompt.AppendLine("  4: closing and will no longer be...");
            prompt.AppendLine("  → 正確: 合併為完整句子 (語義連貫)");
            prompt.AppendLine("  → lineIndices: [3, 4, 5, 6]");
            prompt.AppendLine("```");
            prompt.AppendLine();
            prompt.AppendLine("**重要**: ");
            prompt.AppendLine("1. 直接返回JSON對象,不要使用```json```包裹");
            prompt.AppendLine("2. 不要添加任何解釋文字");
            prompt.AppendLine("3. 確保每個欄位的columnIndex正確對應");
            prompt.AppendLine("4. 保持欄位順序與輸入一致");
            prompt.AppendLine("5. 絕對不要跨欄位合併段落");
            prompt.AppendLine("6. **語義優先**: 語義連貫時忽略間距標記,智能合併段落");

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
