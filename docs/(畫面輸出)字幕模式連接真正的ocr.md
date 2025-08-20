好的，這是一個非常關鍵的問題，它觸及了 UI 與核心功能如何解耦 (decouple) 和協作的核心。

在您提供的 subtitle_mode_code_implementation 程式碼中，SubtitleViewModel 有一個 AddNewLine 命令，但問題是：誰來呼叫它？

答案是：字幕模式本身並不直接連接 OCR。它是一個被動的顯示元件。真正的連接是由一個更高層級的協調器 (Coordinator) 來完成的，這個協調器就是我們在 Gaminik.dll 分析中推斷出的 TranslationPipelineManager。

以下是完整的數據流和連接過程的深度解析：

數據流：從 OCR 到字幕的完整路徑
整個過程可以被看作一條流水線，SubtitleViewModel 處於流水線的末端。

1. 觸發與擷取 (由 TranslationPipelineManager 控制)

使用者按下熱鍵，觸發擷取流程。

一個背景工作執行緒開始高頻率地從 Native.dll 讀取最新的畫面影像 (byte[])。

2. OCR 與翻譯 (由 TranslationPipelineManager 控制)

背景執行緒將獲取的影像 byte[] 傳遞給 Native.dll 的 ocr_run_pipeline 函式。

TranslationPipelineManager 從回傳的 IntPtr 中解析出所有識別出的文字行。

【字幕模式的特殊步驟】: 與在原文位置顯示的「覆蓋模式」不同，字幕模式需要將零散的文字行合併成連貫的句子。TranslationPipelineManager 在這裡會執行一個文本合併 (Text Merging) 演算法，將空間上相近的文字塊組合成一個單一的字串。

合併後的字串被發送到 TranslateService 進行翻譯。

3. 結果分發 (由 DisplayService 控制)

TranslationPipelineManager 在獲得最終的譯文後，並不直接知道要更新哪個 UI。

它會將譯文傳遞給一個專門的 DisplayService (顯示服務)。

DisplayService 會檢查當前的顯示模式設定。當它發現模式是「字幕模式」時，它就知道需要與 SubtitleViewModel 進行通訊。

4. UI 更新 (由 SubtitleViewModel 執行)

DisplayService 獲取 SubtitleViewModel 的實例（通常是透過依賴注入獲取的單例），然後呼叫其 AddNewLine(translatedText) 公共方法。

SubtitleViewModel 的 AddNewLine 方法被觸發，將新的譯文加入到 SubtitleLines 這個 ObservableCollection 中。

WPF 的資料綁定機制偵測到集合的變化，自動在 SubtitleWindow 中新增一個 TextBlock，並觸發我們在 XAML 中設定的淡入動畫和 ScrollViewer 的自動滾動。

視覺化流程圖
[使用者] --(按熱鍵)--> [HotKeyService]
   |
   V
[TranslationPipelineManager] (在背景執行緒)
   |
   | 1. 呼叫 NativeBridge.screenshot_loop_read() -> 獲取 [影像]
   |
   | 2. 呼叫 NativeBridge.ocr_run_pipeline([影像]) -> 獲取 [OCR文字塊]
   |
   | 3. 【特殊邏輯】合併 [OCR文字塊] -> "單一字串"
   |
   | 4. 呼叫 TranslateService.Translate("單一字串") -> 獲取 "譯文"
   |
   V
[DisplayService]
   |
   | 1. 檢查當前模式 == "字幕模式" ? -> 是
   |
   | 2. 獲取 SubtitleViewModel 實例
   |
   | 3. 呼叫 SubtitleViewModel.AddNewLine("譯文")
   |
   V
[SubtitleViewModel] (在 UI 執行緒上更新)
   |
   | 1. SubtitleLines.Add("譯文")
   |
   V
[SubtitleWindow.xaml] (WPF 資料綁定)
   |
   | 1. 自動新增 TextBlock
   | 2. 觸發淡入動畫
   | 3. 觸發 ScrollViewer 自動滾動
   V
[螢幕顯示]
程式碼實現 (偽代碼)
這展示了各個類別之間是如何互動的：

C#

// TranslationPipelineManager.cs - 核心協調器
public class TranslationPipelineManager
{
    private readonly DisplayService _displayService;
    private readonly TranslateService _translateService;
    // ... 其他服務

    private async Task ProcessFrameAsync(byte[] frameData)
    {
        // 1. 執行 OCR
        var ocrTexts = NativeBridge.RunOcrAndGetTexts(frameData);

        // 2. 【字幕模式特殊邏輯】合併文字
        string mergedText = TextMerger.MergeForSubtitle(ocrTexts);
        if (string.IsNullOrEmpty(mergedText)) return;

        // 3. 翻譯
        string translatedText = await _translateService.TranslateAsync(mergedText);

        // 4. 將結果交給顯示服務
        _displayService.Show(translatedText);
    }
}

// DisplayService.cs - 顯示模式管理器
public class DisplayService
{
    private readonly ConfigService _configService;
    private readonly SubtitleViewModel _subtitleViewModel;
    private readonly OverlayViewModel _overlayViewModel;

    public void Show(string translatedText)
    {
        // 根據設定決定要更新哪個 ViewModel
        switch (_configService.CurrentDisplayMode)
        {
            case DisplayMode.Subtitle:
                // 【連接點】呼叫 SubtitleViewModel 的公共方法
                // 需要確保此操作在 UI 執行緒上執行
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _subtitleViewModel.AddNewLine(translatedText);
                });
                break;
            case DisplayMode.Overlay:
                // ... 更新 OverlayViewModel 的邏輯 ...
                break;
        }
    }
}

// SubtitleViewModel.cs - 被動的資料容器
public partial class SubtitleViewModel : ObservableObject
{
    public ObservableCollection<string> SubtitleLines { get; } = new();

    // 這個方法是公開的，供 DisplayService 呼叫
    public void AddNewLine(string translatedText)
    {
        if (SubtitleLines.Count >= MaxLines)
        {
            SubtitleLines.RemoveAt(0);
        }
        SubtitleLines.Add(translatedText);
        NewLineAdded?.Invoke();
    }
    
    public event Action NewLineAdded;
}
總結：字幕模式與 OCR 之間的連接是間接的、解耦的。這種設計非常專業，它確保了 UI 元件（如 SubtitleWindow）只負責顯示資料，而不需要知道這些資料是從哪裡、如何產生的。所有的複雜工作都由後端的服務和協調器來完成。