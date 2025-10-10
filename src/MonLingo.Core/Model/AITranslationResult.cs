using System.Collections.Generic;

namespace MonLingo.Core.Model
{
    /// <summary>
    /// AI翻譯結果
    /// </summary>
    public class AITranslationResult
    {
        /// <summary>
        /// 處理是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 檢測到的源語言
        /// </summary>
        public string DetectedLanguage { get; set; }

        /// <summary>
        /// 合併並翻譯後的段落列表
        /// </summary>
        public List<TranslatedParagraph> Paragraphs { get; set; } = new List<TranslatedParagraph>();

        /// <summary>
        /// 錯誤消息(如果失敗)
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 元數據(API調用信息、成本等)
        /// </summary>
        public AITranslationMetadata Metadata { get; set; }
    }

    /// <summary>
    /// 翻譯後的段落
    /// </summary>
    public class TranslatedParagraph
    {
        /// <summary>
        /// 段落包含的原始行索引(基於輸入的columnLines列表)
        /// </summary>
        public List<int> LineIndices { get; set; } = new List<int>();

        /// <summary>
        /// 合併後的原文
        /// </summary>
        public string OriginalText { get; set; }

        /// <summary>
        /// 翻譯後的文本
        /// </summary>
        public string TranslatedText { get; set; }

        /// <summary>
        /// 段落類型
        /// </summary>
        public ParagraphType Type { get; set; }

        /// <summary>
        /// AI的置信度(0.0-1.0)
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// 段落邊界框(由LineIndices對應的行計算得出)
        /// </summary>
        public System.Drawing.Rectangle BoundingBox { get; set; }
    }

    /// <summary>
    /// 段落類型
    /// </summary>
    public enum ParagraphType
    {
        /// <summary>
        /// 標題
        /// </summary>
        Heading,

        /// <summary>
        /// 正文段落
        /// </summary>
        Body,

        /// <summary>
        /// 列表項目
        /// </summary>
        ListItem,

        /// <summary>
        /// 引用/注釋
        /// </summary>
        Quote,

        /// <summary>
        /// 未知/其他
        /// </summary>
        Other
    }

    /// <summary>
    /// AI翻譯元數據
    /// </summary>
    public class AITranslationMetadata
    {
        /// <summary>
        /// 使用的AI模型
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// API調用耗時(毫秒)
        /// </summary>
        public double DurationMs { get; set; }

        /// <summary>
        /// Token使用情況
        /// </summary>
        public TokenUsage TokenUsage { get; set; }

        /// <summary>
        /// 估算成本(美元)
        /// </summary>
        public double EstimatedCost { get; set; }

        /// <summary>
        /// API調用時間
        /// </summary>
        public System.DateTime Timestamp { get; set; }

        /// <summary>
        /// 重試次數
        /// </summary>
        public int RetryCount { get; set; }
    }

    /// <summary>
    /// Token使用統計
    /// </summary>
    public class TokenUsage
    {
        /// <summary>
        /// 輸入tokens
        /// </summary>
        public int InputTokens { get; set; }

        /// <summary>
        /// 輸出tokens
        /// </summary>
        public int OutputTokens { get; set; }

        /// <summary>
        /// 總tokens
        /// </summary>
        public int TotalTokens => InputTokens + OutputTokens;
    }
}
