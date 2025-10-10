using MonLingo.Core.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonLingo.Core.Service.AI
{
    /// <summary>
    /// AI智能翻譯服務接口
    /// 負責將OCR識別的文字行進行語義分析、段落合併並翻譯
    /// </summary>
    public interface IAITranslationService
    {
        /// <summary>
        /// 智能翻譯：分析文字行語義關係，智能合併段落並翻譯
        /// </summary>
        /// <param name="columnLines">單個欄位中的文字行列表</param>
        /// <param name="sourceLanguage">源語言(auto=自動檢測)</param>
        /// <param name="targetLanguage">目標語言</param>
        /// <returns>AI翻譯結果，包含合併後的段落和翻譯文本</returns>
        Task<AITranslationResult> SmartTranslateAsync(
            List<LayoutLine> columnLines,
            string sourceLanguage = "auto",
            string targetLanguage = "zh-TW");

        /// <summary>
        /// 批量智能翻譯：處理多個欄位
        /// </summary>
        /// <param name="columns">多個欄位的文字行列表</param>
        /// <param name="sourceLanguage">源語言</param>
        /// <param name="targetLanguage">目標語言</param>
        /// <returns>每個欄位的AI翻譯結果</returns>
        Task<List<AITranslationResult>> SmartTranslateBatchAsync(
            List<List<LayoutLine>> columns,
            string sourceLanguage = "auto",
            string targetLanguage = "zh-TW");
    }
}
