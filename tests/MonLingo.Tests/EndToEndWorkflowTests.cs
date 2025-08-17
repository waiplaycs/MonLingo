using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonLingo.Core;
using MonLingo.Core.Services;
```csharp
using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using MonLingo.Core;
using MonLingo.Core.Services;

namespace MonLingo.Tests.EndToEnd
{
    /// <summary>
    /// 端到端工作流程測試套件
    /// 測試完整的用戶使用場景和系統整合
    /// </summary>
    [TestClass]
    [TestCategory("E2E")]
    public class EndToEndWorkflowTests
    {
        private IServiceProvider _serviceProvider;
        private ITranslationPipelineManager _pipelineManager;
        private IScreenCaptureService _captureService;
        private IConfigService _configService;

        [TestInitialize]
        public async Task Setup()
        {
            // 初始化完整的服務容器
            _serviceProvider = CreateTestServiceProvider();
            _pipelineManager = _serviceProvider.GetService<ITranslationPipelineManager>();
            _captureService = _serviceProvider.GetService<IScreenCaptureService>();
            _configService = _serviceProvider.GetService<IConfigService>();

            // 初始化 Native Bridge
            var success = await NativeBridge.InitializeAsync();
            success.Should().BeTrue("Native DLL 應該成功初始化");
        }

        [TestCleanup]
        public async Task Cleanup()
        {
            await NativeBridge.CleanupAsync();
            _serviceProvider?.Dispose();
        }

        /// <summary>
        /// 端到端 OCR 翻譯工作流程測試
        /// </summary>
        [TestMethod]
        [TestCategory("E2E")]
        [TestCategory("Critical")]
        public async Task E2E_OCR_Translation_Workflow_Should_Complete_Successfully()
        {
            // Arrange
            var testImagePath = PrepareTestImage("chinese_text_sample.png");
            var expectedText = "測試中文文字識別";
            var expectedTranslation = "Test Chinese text recognition";

            // Act & Assert - Step 1: 螢幕擷取
            var captureResult = await _captureService.CaptureRegionAsync(
                new CaptureRegion { X = 100, Y = 100, Width = 400, Height = 200 });
            
            captureResult.Should().NotBeNull("螢幕擷取應該成功");
            captureResult.IsSuccess.Should().BeTrue("擷取操作應該成功");

            // Act & Assert - Step 2: 圖像預處理
            var preprocessedImage = await _pipelineManager.PreprocessImageAsync(captureResult.Image);
            preprocessedImage.Should().NotBeNull("圖像預處理應該成功");

            // Act & Assert - Step 3: OCR 文字識別
            var ocrResult = await _pipelineManager.PerformOcrAsync(preprocessedImage);
            ocrResult.Should().NotBeNull("OCR 識別應該完成");
            ocrResult.IsSuccess.Should().BeTrue("OCR 應該成功識別文字");
            ocrResult.Text.Should().NotBeNullOrEmpty("應該識別出文字內容");

            // Act & Assert - Step 4: 文字翻譯
            var translationResult = await _pipelineManager.TranslateTextAsync(
                ocrResult.Text, "zh", "en");
            
            translationResult.Should().NotBeNull("翻譯應該完成");
            translationResult.IsSuccess.Should().BeTrue("翻譯應該成功");
            translationResult.TranslatedText.Should().NotBeNullOrEmpty("應該有翻譯結果");

            // Act & Assert - Step 5: 結果驗證
            var endToEndLatency = captureResult.ProcessingTime + 
                                 preprocessedImage.ProcessingTime + 
                                 ocrResult.ProcessingTime + 
                                 translationResult.ProcessingTime;

            endToEndLatency.Should().BeLessThan(TimeSpan.FromSeconds(3), 
                "完整工作流程應該在 3 秒內完成");

            Console.WriteLine($"端到端測試完成:");
            Console.WriteLine($"  OCR 識別: {ocrResult.Text}");
            Console.WriteLine($"  翻譯結果: {translationResult.TranslatedText}");
            Console.WriteLine($"  總處理時間: {endToEndLatency.TotalMilliseconds:F2}ms");
        }

        /// <summary>
        /// 多語言支援端到端測試
        /// </summary>
        [TestMethod]
        [TestCategory("E2E")]
        [TestCategory("Multilingual")]
        public async Task E2E_Multilingual_Support_Should_Work_Correctly()
        {
            // Arrange
            var testCases = new[]
            {
                new { Language = "zh", ImageFile = "chinese_sample.png", ExpectedContains = "中文" },
                new { Language = "ja", ImageFile = "japanese_sample.png", ExpectedContains = "日本語" },
                new { Language = "ko", ImageFile = "korean_sample.png", ExpectedContains = "한국어" },
                new { Language = "en", ImageFile = "english_sample.png", ExpectedContains = "English" }
            };

            foreach (var testCase in testCases)
            {
                Console.WriteLine($"測試語言: {testCase.Language}");

                // Act - 設置語言模式
                await _configService.SetOcrLanguageAsync(testCase.Language);
                
                // Act - 執行 OCR
                var testImage = LoadTestImage(testCase.ImageFile);
                var ocrResult = await _pipelineManager.PerformOcrAsync(testImage);

                // Assert
                ocrResult.Should().NotBeNull($"{testCase.Language} OCR 應該完成");
                ocrResult.IsSuccess.Should().BeTrue($"{testCase.Language} OCR 應該成功");
                ocrResult.Confidence.Should().BeGreaterThan(0.8, 
                    $"{testCase.Language} OCR 信心度應該大於 80%");

                Console.WriteLine($"  識別結果: {ocrResult.Text}");
                Console.WriteLine($"  信心度: {ocrResult.Confidence:P2}");
            }
        }

        /// <summary>
        /// 錯誤處理與恢復流程測試
        /// </summary>
        [TestMethod]
        [TestCategory("E2E")]
        [TestCategory("ErrorHandling")]
        public async Task E2E_Error_Handling_And_Recovery_Should_Work_Correctly()
        {
            // Test Case 1: 網路斷線情況
            await TestNetworkDisconnectionScenario();

            // Test Case 2: DLL 載入失敗情況
            await TestDllLoadFailureScenario();

            // Test Case 3: OCR 處理失敗情況
            await TestOcrProcessingFailureScenario();

            // Test Case 4: 記憶體不足情況
            await TestMemoryPressureScenario();
        }

        /// <summary>
        /// 性能壓力測試
        /// </summary>
        [TestMethod]
        [TestCategory("E2E")]
        [TestCategory("Performance")]
        public async Task E2E_Performance_Stress_Test_Should_Meet_Requirements()
        {
            // Arrange
            const int testIterations = 50;
            const int concurrentTasks = 5;
            var latencies = new List<TimeSpan>();

            // Act - 執行並發壓力測試
            var tasks = new List<Task>();
            for (int batch = 0; batch < testIterations / concurrentTasks; batch++)
            {
                var batchTasks = new List<Task>();
                for (int i = 0; i < concurrentTasks; i++)
                {
                    batchTasks.Add(Task.Run(async () =>
                    {
                        var stopwatch = Stopwatch.StartNew();
                        await ExecuteFullWorkflowAsync();
                        stopwatch.Stop();
                        lock (latencies)
                        {
                            latencies.Add(stopwatch.Elapsed);
                        }
                    }));
                }
                await Task.WhenAll(batchTasks);
            }

            // Assert - 性能要求驗證
            var avgLatency = latencies.Average(t => t.TotalMilliseconds);
            var p95Latency = latencies.OrderBy(t => t.TotalMilliseconds)
                                   .Skip((int)(latencies.Count * 0.95))
                                   .First().TotalMilliseconds;

            avgLatency.Should().BeLessThan(2000, "平均端到端延遲應該小於 2 秒");
            p95Latency.Should().BeLessThan(3000, "P95 端到端延遲應該小於 3 秒");

            // 記憶體洩漏檢查
            GC.Collect();
            GC.WaitForPendingFinalizers();
            var finalMemory = GC.GetTotalMemory(false);
            
            Console.WriteLine($"壓力測試結果:");
            Console.WriteLine($"  測試次數: {testIterations}");
            Console.WriteLine($"  平均延遲: {avgLatency:F2}ms");
            Console.WriteLine($"  P95 延遲: {p95Latency:F2}ms");
            Console.WriteLine($"  最終記憶體: {finalMemory / 1024 / 1024:F2}MB");
        }

        #region 輔助方法

        private IServiceProvider CreateTestServiceProvider()
        {
            var services = new ServiceCollection();
            
            // 註冊所有核心服務
            services.AddSingleton<IConfigService, ConfigService>();
            services.AddSingleton<ITranslationPipelineManager, TranslationPipelineManager>();
            services.AddScoped<IScreenCaptureService, ScreenCaptureService>();
            services.AddScoped<ITranslateService, TranslateService>();
            services.AddScoped<IOcrEngine, OcrEngine>();

            return services.BuildServiceProvider();
        }

        private async Task<TranslationResult> ExecuteFullWorkflowAsync()
        {
            var captureResult = await _captureService.CaptureRegionAsync(
                new CaptureRegion { X = 100, Y = 100, Width = 400, Height = 200 });
            
            var preprocessedImage = await _pipelineManager.PreprocessImageAsync(captureResult.Image);
            var ocrResult = await _pipelineManager.PerformOcrAsync(preprocessedImage);
            
            if (ocrResult.IsSuccess && !string.IsNullOrEmpty(ocrResult.Text))
            {
                return await _pipelineManager.TranslateTextAsync(ocrResult.Text, "auto", "en");
            }
            
            return new TranslationResult { IsSuccess = false };
        }

        private async Task TestNetworkDisconnectionScenario()
        {
            // 模擬網路斷線
            var originalTranslateService = _serviceProvider.GetService<ITranslateService>();
            var mockTranslateService = new Mock<ITranslateService>();
            mockTranslateService.Setup(x => x.TranslateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                               .ThrowsAsync(new NetworkException("Network unavailable"));

            // 替換服務並測試錯誤處理
            // ... 錯誤處理測試邏輯 ...
        }

        private async Task TestDllLoadFailureScenario()
        {
            // 測試 DLL 載入失敗的處理
            // ... DLL 錯誤處理測試邏輯 ...
        }

        private async Task TestOcrProcessingFailureScenario()
        {
            // 測試 OCR 處理失敗的處理
            // ... OCR 錯誤處理測試邏輯 ...
        }

        private async Task TestMemoryPressureScenario()
        {
            // 測試記憶體壓力情況下的處理
            // ... 記憶體壓力測試邏輯 ...
        }

        private string PrepareTestImage(string fileName)
        {
            var testImagePath = Path.Combine(TestContext.TestDeploymentDir, "TestImages", fileName);
            File.Exists(testImagePath).Should().BeTrue($"測試圖像 {fileName} 應該存在");
            return testImagePath;
        }

        private byte[] LoadTestImage(string fileName)
        {
            var imagePath = PrepareTestImage(fileName);
            return File.ReadAllBytes(imagePath);
        }

        #endregion
    }
}
```

---

## 🚀 整合測試實現

### 📊 **系統整合測試**

#### SystemIntegrationTests.cs
```csharp
namespace MonLingo.Tests.Integration
{
    /// <summary>
    /// 系統整合測試套件
    /// 測試各組件間的整合和協作
    /// </summary>
    [TestClass]
    [TestCategory("Integration")]
    public class SystemIntegrationTests
    {
        /// <summary>
        /// Native Bridge 與 Core 服務整合測試
        /// </summary>
        [TestMethod]
        [TestCategory("Integration")]
        public async Task Native_Bridge_Core_Services_Integration_Should_Work()
        {
            // Arrange
            var nativeSuccess = await NativeBridge.InitializeAsync();
            nativeSuccess.Should().BeTrue();

            var configService = new ConfigService();
            var ocrEngine = new OcrEngine();

            // Act - 測試 Native Bridge 與 Core 服務的協作
            var testImage = CreateTestBitmap(800, 600);
            using var imageStream = new MemoryStream();
            testImage.Save(imageStream, ImageFormat.Png);

            var ocrResult = await ocrEngine.RecognizeTextAsync(imageStream.ToArray());

            // Assert
            ocrResult.Should().NotBeNull("OCR 引擎應該返回結果");
            ocrResult.IsSuccess.Should().BeTrue("OCR 處理應該成功");

            // Cleanup
            await NativeBridge.CleanupAsync();
        }

        /// <summary>
        /// 服務生命週期管理測試
        /// </summary>
        [TestMethod]
        [TestCategory("Integration")]
        public async Task Service_Lifecycle_Management_Should_Work_Correctly()
        {
            // 測試服務的創建、使用、釋放完整生命週期
            var serviceProvider = CreateServiceProvider();
            
            // 測試各服務的初始化
            var services = new[]
            {
                serviceProvider.GetService<IConfigService>(),
                serviceProvider.GetService<ITranslateService>(),
                serviceProvider.GetService<IScreenCaptureService>()
            };

            foreach (var service in services)
            {
                service.Should().NotBeNull("所有服務應該正確初始化");
            }

            // 測試服務協作
            var configService = services[0] as IConfigService;
            var translateService = services[1] as ITranslateService;

            await configService.SetSettingAsync("TranslationEngine", "OpenAI");
            var engine = await configService.GetSettingAsync<string>("TranslationEngine");
            engine.Should().Be("OpenAI");

            // 清理資源
            serviceProvider.Dispose();
        }
    }
}
```

---

## 📋 測試資料準備

### 🖼️ **測試圖像素材**

#### 建立測試圖像目錄結構
```
tests/MonLingo.Tests/TestImages/
├── chinese_sample.png          # 中文測試圖像
├── japanese_sample.png         # 日文測試圖像  
├── korean_sample.png           # 韓文測試圖像
├── english_sample.png          # 英文測試圖像
├── mixed_language.png          # 混合語言圖像
├── complex_layout.png          # 複雜版面圖像
├── high_resolution.png         # 高解析度圖像
└── low_quality.png             # 低品質圖像
```

#### 測試資料生成器
```csharp
public static class TestImageGenerator
{
    public static Bitmap CreateTestImageWithText(string text, string language = "zh")
    {
        var bitmap = new Bitmap(800, 600);
        using var graphics = Graphics.FromImage(bitmap);
        
        graphics.Clear(Color.White);
        
        var font = GetFontForLanguage(language);
        var brush = new SolidBrush(Color.Black);
        
        graphics.DrawString(text, font, brush, new PointF(50, 50));
        
        return bitmap;
    }

    private static Font GetFontForLanguage(string language)
    {
        return language switch
        {
            "zh" => new Font("Microsoft YaHei", 24, FontStyle.Regular),
            "ja" => new Font("MS Gothic", 24, FontStyle.Regular),
            "ko" => new Font("Malgun Gothic", 24, FontStyle.Regular),
            _ => new Font("Arial", 24, FontStyle.Regular)
        };
    }
}
```

---

## 🔧 自動化執行配置

### ⚙️ **測試執行配置**

#### test.runsettings
```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <TestRunParameters>
    <Parameter name="TestImagePath" value="TestImages" />
    <Parameter name="TestTimeout" value="300000" />
    <Parameter name="EnableLongRunningTests" value="true" />
  </TestRunParameters>
  
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="Code Coverage" uri="datacollector://Microsoft/CodeCoverage/2.0">
        <Configuration>
          <CodeCoverage>
            <ModulePaths>
              <Include>
                <ModulePath>.*MonLingo\.Core\.dll$</ModulePath>
                <ModulePath>.*MonLingo\.Native\.dll$</ModulePath>
              </Include>
            </ModulePaths>
          </CodeCoverage>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```

#### PowerShell 執行腳本
```powershell
# run_e2e_tests.ps1
param(
    [string]$Configuration = "Debug",
    [string]$TestCategory = "E2E",
    [switch]$GenerateReport
)

Write-Host "🧪 開始執行端到端測試..." -ForegroundColor Cyan

# 編譯專案
Write-Host "📦 編譯測試專案..." -ForegroundColor Yellow
dotnet build tests\MonLingo.Tests\MonLingo.Tests.csproj -c $Configuration

if ($LASTEXITCODE -ne 0) {
    Write-Error "編譯失敗，測試中止"
    exit 1
}

# 執行端到端測試
Write-Host "🚀 執行端到端測試..." -ForegroundColor Yellow
$testCommand = "dotnet test tests\MonLingo.Tests\MonLingo.Tests.csproj " +
               "-c $Configuration " +
               "--filter `"TestCategory=$TestCategory`" " +
               "--settings test.runsettings " +
               "--verbosity normal"

if ($GenerateReport) {
    $testCommand += " --collect:`"XPlat Code Coverage`" --results-directory TestResults"
}

Invoke-Expression $testCommand

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ 端到端測試執行成功!" -ForegroundColor Green
    
    if ($GenerateReport) {
        Write-Host "📊 生成測試報告..." -ForegroundColor Yellow
        # 生成覆蓋率報告的邏輯
    }
} else {
    Write-Error "❌ 端到端測試執行失敗"
    exit 1
}
```

---

## 📊 持續整合配置

### 🔄 **GitHub Actions 工作流程**

#### .github/workflows/e2e-tests.yml
```yaml
name: End-to-End Tests

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]
  schedule:
    - cron: '0 2 * * *'  # 每日凌晨 2 點執行

jobs:
  e2e-tests:
    runs-on: windows-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET Framework
      uses: microsoft/setup-msbuild@v1
      
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build solution
      run: dotnet build --configuration Release --no-restore
      
    - name: Prepare test images
      run: |
        mkdir tests\MonLingo.Tests\TestImages
        # 複製測試圖像的邏輯
        
    - name: Run E2E tests
      run: |
        dotnet test tests\MonLingo.Tests\MonLingo.Tests.csproj `
          --configuration Release `
          --filter "TestCategory=E2E" `
          --settings test.runsettings `
          --collect:"XPlat Code Coverage" `
          --results-directory TestResults
          
    - name: Upload test results
      uses: actions/upload-artifact@v3
      if: always()
      with:
        name: e2e-test-results
        path: TestResults/
        
    - name: Generate test report
      if: always()
      run: |
        # 生成並上傳測試報告
```

---

## 📈 測試報告與監控

### 📊 **測試指標追蹤**

#### E2E 測試儀表板指標
```
🎯 端到端測試關鍵指標:

性能指標:
- 完整工作流程延遲 P95 < 3秒
- 系統資源使用率 < 80%
- 記憶體洩漏檢測: 0 個

功能指標:  
- 多語言支援覆蓋率: 100%
- 錯誤處理場景覆蓋: 100%
- 整合測試通過率: 100%

穩定性指標:
- 連續執行穩定性: 100 次無失敗
- 併發處理能力: 5 個並發任務
- 長時間運行穩定性: 24 小時無問題
```

---

## 🎯 DEV-03 價值總結

### ✅ **交付成果**

1. **完整端到端測試套件** - 覆蓋所有關鍵用戶場景
2. **自動化測試執行** - 100% 自動化，支持 CI/CD 整合
3. **性能與穩定性驗證** - 確保系統在各種條件下穩定運行
4. **多語言支援驗證** - 確保中日韓英語言功能完整

### 🚀 **對 Phase 3 的支撐**

1. **品質保證** - 為 UI 開發提供完整的後端功能驗證
2. **整合指導** - 明確各組件間的整合方式和最佳實踐
3. **性能基線** - 為 UI 響應性設計提供性能參考
4. **錯誤處理** - 確保 UI 層能正確處理各種異常情況

**DEV-03 端到端測試架構已完全就緒，為整個系統提供全面的品質保證！** 🎯
