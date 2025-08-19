Gaminik 核心 DLL 深度分析報告
本報告旨在詳細解析 Gaminik.dll 和 Native.dll 中關於實時翻譯、會員系統、Token 購買系統的具體實現細節。



Gaminik.dll 作為應用程式的「大腦」，包含了所有業務邏輯；而 Native.dll 作為「肌肉」，則負責底層的高性能操作。
這兩個 DLL 的協同工作，實現了 Gaminik 的核心功能。


第一部分：Gaminik.dll (.NET 業務邏輯層)
Gaminik.dll 是一個 .NET Framework 組件，包含了應用程式幾乎所有的上層邏輯。分析其內部結構，我們可以清晰地看到各個商業功能的實現方式。

1. 實時截圖翻譯 (Real-time Screenshot Translation)
此功能是整個應用的核心，由一個精心設計的管線 (Pipeline) 協調完成。

協調核心: Gaminik.Core.TranslationPipelineManager (推斷名稱)

職責: 這是總指揮。它負責接收來自 UI 層的指令（例如使用者按下熱鍵），然後依序調用擷取、OCR、翻譯和顯示服務。

工作流程:

觸發: Gaminik.Service.HotKeyService 監聽到全域熱鍵後，通知 TranslationPipelineManager。

擷取: Manager 調用 Gaminik.Core.NativeBridge 中的 screenshot_window_loop_start() 和 screenshot_window_loop_read() 函式，從 Native.dll 中高頻率獲取畫面影像資料 (byte[])。

OCR: 影像資料被傳遞給 NativeBridge.ocr_run_pipeline()，返回一個指向 OCR 結果的記憶體指標 (IntPtr)。

翻譯: Manager 調用 Gaminik.Service.TranslateService。這個服務內部實現了一個策略模式 (Strategy Pattern)，可以根據使用者設定（免費、Pro、API 金鑰）和網路狀況，動態選擇使用哪個翻譯引擎（如 DeepL, Google, CTranslate2 等）。

顯示: 最終的譯文被傳遞給 Gaminik.Interface.TranslationPopupWindow 的 ViewModel，透過 WPF 的資料綁定機制顯示在螢幕上。

2. 會員系統 (Membership System)
Gaminik 擁有一個完整且複雜的會員與授權系統，其邏輯主要封裝在服務層。

服務核心: Gaminik.Service.LicenseService 和 Gaminik.Service.UserService

具體內容:

使用者模型 (Gaminik.Data.User): 這個類別定義了使用者的所有屬性，包括 email, phone, accountLevel (帳號等級，如 Free, Pro), expireTime (到期時間), coins (硬幣/Token 數量) 等。

登入流程: 存在多個登入視窗 (LoginEmailPage, LoginPhonePage) 和對應的 ViewModel。它們會收集使用者輸入，然後調用 UserService 中的 emailSigninAsync() 或 loginViaSmsCodeAsync() 方法與後端 API 進行驗證。

狀態管理: LicenseService 負責在應用程式啟動時驗證本地儲存的授權 Token，並在整個應用生命週期中管理當前使用者的登入狀態和權限（例如，判斷是否可以存取 Pro-only 的功能）。DLL 中有明確的 IsPro() 檢查函式。

3. 購買 Token (積分系統)
Gaminik 的貨幣化系統設計得非常全面，它不叫「積分」，而是使用兩種貨幣單位：「Points」和「Coins」。

服務核心: Gaminik.Service.ApiService

具體內容:

Points (點數):

定義: 這是免費使用者每日獲得的、有使用期限的基礎貨幣。

來源: Gaminik.dll 中包含了 AddPointsManager 和 addPointsAsync 函式，用於處理使用者透過觀看廣告等方式獲取免費點數的邏輯。日誌字串中也包含 REWARDED_AD_POINTS (激勵廣告點數) 等常量。

消耗: 主要用於基礎的翻譯次數計數。

Coins (硬幣/Token):

定義: 這是 Pro 使用者專用的、需要付費購買的進階貨幣。

購買流程: UI 層有完整的購買流程，從 PurchaseMainPage 開始，引導使用者到 CoinsPayTypeSelectWindow (選擇支付方式)。分析顯示，它整合了多種支付渠道，包括 Alipay, WeixinPay, Coinbase (加密貨幣), Paddle, Lemonsqueezy 等。每個支付渠道都有對應的 ...WebWindow 來處理支付頁面。

消耗: TranslateService 在調用進階翻譯引擎（如 DeepL, ChatGPT）前，會檢查使用者的 Coins 餘額。DLL 中存在明確的 COINS_RATIO_DEEPL 和 COINS_RATIO_GPT_35_INPUT 等常量，定義了不同引擎消耗 Coins 的比例。

第二部分：Native.dll (C++ 核心功能層)
Native.dll 專注於執行性能敏感的任務，為上層的 Gaminik.dll 提供穩定的底層支援。

實時翻譯過程的核心功能實現方法
是的，Native.dll 完整地記錄了實時翻譯前端部分（即擷取和 OCR）的核心實現方法。它採用了現代 Windows API 的非同步事件驅動模型，這是其高性能的關鍵。

核心匯出函式: screenshot_window_loop_start, screenshot_window_loop_read, screenshot_window_loop_close

實現方法:

畫面擷取 (screenshot_window_loop_start):

技術: 它並未使用傳統的 GDI BitBlt，而是呼叫了現代的 Windows Graphics Capture API。從匯入的 d3d11.dll 和 WinRT 相關 API 可以證實這一點。

演算法: 當這個函式被 C# 層調用時，它並不會自己去循環抓圖。相反地，它會建立一個 Direct3D11CaptureFramePool，然後為該物件的 FrameArrived 事件註冊一個 C++ 的回呼函式 (Callback)。之後，它就告訴 Windows：「當這個視窗有新畫面時，請通知我。」

資料生產 (由系統觸發的回呼函式):

技術: 當遊戲或應用程式渲染新的一幀時，Windows 系統會自動、高頻率地調用 Native.dll 中註冊的那個回呼函式。

演算法: 在這個回呼函式內部，DLL 會從 GPU 記憶體中獲取最新的畫面紋理 (texture)，將其複製到 CPU 可讀的記憶體中，然後將這個影像資料放入一個共享的緩衝區。

資料消費 (screenshot_window_loop_read):

技術: C# 層的 DispatcherTimer 會定期調用這個函式。

演算法: 這個函式的作用非常簡單，就是從那個共享緩-衝區中取出最新的一幀影像，並將其回傳給 C#。

這個生產者-消費者模型（Windows API 是生產者，C# 是消費者）完美地分離了高頻率的擷取任務和較低頻率的 OCR/翻譯任務，確保了遊戲的流暢運行和高效的即時翻譯。