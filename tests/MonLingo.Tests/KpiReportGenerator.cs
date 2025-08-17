using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MonLingo.Tests
{
    /// <summary>
    /// KPI 測試結果報告生成器
    /// </summary>
    public static class KpiReportGenerator
    {
        /// <summary>
        /// 生成 KPI 驗證報告
        /// </summary>
        public static void GenerateKpiReport(List<KpiTestResult> results, string outputPath)
        {
            var report = new StringBuilder();
            
            // 報告標題
            report.AppendLine("# Phase 2 KPI 達成驗證報告");
            report.AppendLine();
            report.AppendLine($"**生成時間**: {DateTime.Now:yyyy年MM月dd日 HH:mm:ss}");
            report.AppendLine($"**測試版本**: MonLingo Phase 2");
            report.AppendLine($"**測試環境**: .NET Framework 4.8.1 / OpenCvSharp4 4.8.0");
            report.AppendLine();
            report.AppendLine("---");
            report.AppendLine();

            // 執行摘要
            GenerateExecutiveSummary(report, results);
            
            // 詳細測試結果
            GenerateDetailedResults(report, results);
            
            // KPI 達成狀況
            GenerateKpiStatus(report, results);
            
            // 建議和後續步驟
            GenerateRecommendations(report, results);

            // 寫入檔案
            File.WriteAllText(outputPath, report.ToString(), Encoding.UTF8);
        }

        private static void GenerateExecutiveSummary(StringBuilder report, List<KpiTestResult> results)
        {
            report.AppendLine("## 🎯 執行摘要");
            report.AppendLine();

            var passedCount = results.Count(r => r.Passed);
            var totalCount = results.Count;
            var passRate = (double)passedCount / totalCount;

            report.AppendLine($"### 📊 整體 KPI 達成狀況");
            report.AppendLine($"- **總測試項目**: {totalCount} 項");
            report.AppendLine($"- **通過項目**: {passedCount} 項");
            report.AppendLine($"- **失敗項目**: {totalCount - passedCount} 項");
            report.AppendLine($"- **通過率**: {passRate:P1}");
            report.AppendLine();

            // KPI 摘要表格
            report.AppendLine("| KPI 項目 | 目標值 | 實際值 | 狀態 |");
            report.AppendLine("|---------|-------|-------|------|");

            foreach (var result in results)
            {
                var status = result.Passed ? "✅ 通過" : "❌ 失敗";
                report.AppendLine($"| {result.KpiName} | {result.TargetValue} | {result.ActualValue} | {status} |");
            }

            report.AppendLine();
        }

        private static void GenerateDetailedResults(StringBuilder report, List<KpiTestResult> results)
        {
            report.AppendLine("## 📋 詳細測試結果");
            report.AppendLine();

            foreach (var result in results)
            {
                report.AppendLine($"### {result.KpiName}");
                report.AppendLine();
                report.AppendLine($"**目標**: {result.Description}");
                report.AppendLine($"**KPI 要求**: {result.TargetValue}");
                report.AppendLine($"**實際結果**: {result.ActualValue}");
                report.AppendLine($"**狀態**: {(result.Passed ? "✅ 通過" : "❌ 失敗")}");
                
                if (!string.IsNullOrEmpty(result.Details))
                {
                    report.AppendLine();
                    report.AppendLine("**詳細資訊**:");
                    report.AppendLine(result.Details);
                }

                if (result.Metrics.Any())
                {
                    report.AppendLine();
                    report.AppendLine("**性能指標**:");
                    foreach (var metric in result.Metrics)
                    {
                        report.AppendLine($"- {metric.Key}: {metric.Value}");
                    }
                }

                report.AppendLine();
                report.AppendLine("---");
                report.AppendLine();
            }
        }

        private static void GenerateKpiStatus(StringBuilder report, List<KpiTestResult> results)
        {
            report.AppendLine("## 🏆 KPI 達成狀況分析");
            report.AppendLine();

            var categories = results.GroupBy(r => r.Category).ToList();

            foreach (var category in categories)
            {
                var categoryResults = category.ToList();
                var categoryPassRate = (double)categoryResults.Count(r => r.Passed) / categoryResults.Count;

                report.AppendLine($"### {category.Key}");
                report.AppendLine($"**通過率**: {categoryPassRate:P1}");
                report.AppendLine();

                foreach (var result in categoryResults)
                {
                    var icon = result.Passed ? "✅" : "❌";
                    report.AppendLine($"- {icon} {result.KpiName}: {result.ActualValue}");
                }

                report.AppendLine();
            }
        }

        private static void GenerateRecommendations(StringBuilder report, List<KpiTestResult> results)
        {
            report.AppendLine("## 💡 建議和後續步驟");
            report.AppendLine();

            var failedResults = results.Where(r => !r.Passed).ToList();

            if (failedResults.Any())
            {
                report.AppendLine("### 🔧 需要優化的項目");
                report.AppendLine();

                foreach (var result in failedResults)
                {
                    report.AppendLine($"#### {result.KpiName}");
                    report.AppendLine($"**問題**: 實際值 {result.ActualValue} 未達到目標 {result.TargetValue}");
                    
                    if (!string.IsNullOrEmpty(result.Recommendations))
                    {
                        report.AppendLine($"**建議**: {result.Recommendations}");
                    }
                    
                    report.AppendLine();
                }
            }
            else
            {
                report.AppendLine("🎉 **恭喜！所有 KPI 項目均已達成目標！**");
                report.AppendLine();
            }

            // 通用建議
            report.AppendLine("### 🚀 後續發展建議");
            report.AppendLine();
            report.AppendLine("1. **持續監控**: 建立定期的 KPI 監控機制");
            report.AppendLine("2. **性能優化**: 針對接近閾值的指標進行預防性優化");
            report.AppendLine("3. **測試擴展**: 增加更多真實場景的測試案例");
            report.AppendLine("4. **自動化**: 整合到 CI/CD 流程中進行自動驗證");
            report.AppendLine();

            // 技術債務提醒
            report.AppendLine("### ⚠️ 技術債務提醒");
            report.AppendLine();
            report.AppendLine("- **階段二未完成項目**: PaddleOCR 引擎整合、螢幕擷取整合");
            report.AppendLine("- **建議**: 完成剩餘項目後重新執行完整 KPI 驗證");
            report.AppendLine("- **風險**: 後續功能可能影響已達成的 KPI 指標");
            report.AppendLine();
        }
    }

    /// <summary>
    /// KPI 測試結果
    /// </summary>
    public class KpiTestResult
    {
        public string KpiName { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string TargetValue { get; set; }
        public string ActualValue { get; set; }
        public bool Passed { get; set; }
        public string Details { get; set; }
        public string Recommendations { get; set; }
        public Dictionary<string, string> Metrics { get; set; } = new Dictionary<string, string>();
    }
}
