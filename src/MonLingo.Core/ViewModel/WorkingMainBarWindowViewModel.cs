using System;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows;
using System.Threading.Tasks;
using System.Linq;
using NLog;
using MonLingo.View.Windows;
using MonLingo.Core.View.Windows;
using MonLingo.Core.Models;
using MonLingo.Core.Service;
using MonLingo.Core.Infrastructure;
using MonLingo.Core.Commands;
using System.Collections.ObjectModel;

namespace MonLingo.ViewModel
{
    /// <summary>
    /// 工作版 MainBarWindowViewModel - 啟用基本按鈕功能
    /// </summary>
    public class WorkingMainBarWindowViewModel : INotifyPropertyChanged
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private Window _mainBarWindow;
        private readonly ILanguageConfigService _languageConfigService;
    private readonly ITranslateService _translateService;
        
        public WorkingMainBarWindowViewModel(Window mainBarWindow = null)
        {
            Logger.Info("🔧 WorkingMainBarWindowViewModel 建構函數開始");
            Logger.Debug($"📋 傳入的主視窗: {(mainBarWindow != null ? mainBarWindow.GetType().Name : "null")}");
            _mainBarWindow = mainBarWindow;
            
            // 初始化語言配置服務
            _languageConfigService = Phase5ServiceContainer.GetService<ILanguageConfigService>();
            _translateService = Phase5ServiceContainer.GetService<ITranslateService>();
            
            // 異步初始化語言設置
            _ = InitializeLanguageSettingsAsync();
            
            // 訂閱語言設定變更事件，確保 7 號按鈕即時更新
            if (_languageConfigService != null)
            {
                _languageConfigService.LanguageConfigChanged += OnLanguageConfigChanged;
            }
            
            // 初始化可選引擎清單與當前引擎
            InitializeEngines();

            Logger.Info("✅ WorkingMainBarWindowViewModel 建構完成");
        }

        /// <summary>
        /// 異步初始化語言設置
        /// </summary>
        private async Task InitializeLanguageSettingsAsync()
        {
            try
            {
                if (_languageConfigService != null)
                {
                    var sourceLanguage = await _languageConfigService.GetSourceLanguageAsync();
                    var targetLanguage = await _languageConfigService.GetTargetLanguageAsync();
                    
                    // 將語言代碼轉換為顯示文字
                    SourceLanguage = ConvertLanguageCodeToDisplay(sourceLanguage);
                    TargetLanguage = ConvertLanguageCodeToDisplay(targetLanguage);
                    
                    Logger.Info($"語言設置初始化完成: {SourceLanguage} → {TargetLanguage}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "初始化語言設置時發生錯誤");
                // 使用默認值
                SourceLanguage = "EN";
                TargetLanguage = "中文";
            }
        }

        /// <summary>
        /// 將語言代碼轉換為顯示文字
        /// </summary>
        private string ConvertLanguageCodeToDisplay(string languageCode)
        {
            return languageCode switch
            {
                "auto" => "AUTO",
                "en" => "EN",
                "zh-cn" => "中文",
                "zh-tw" => "繁體",
                "ja" => "日語",
                "ko" => "韓語",
                "fr" => "FR",
                "de" => "DE",
                "es" => "ES",
                "ru" => "RU",
                _ => languageCode?.ToUpper() ?? "EN"
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #region 顯示屬性

        private string _selectedEngine = "Google";
        public string SelectedEngine
        {
            get => _selectedEngine;
            set
            {
                _selectedEngine = value;
                OnPropertyChanged(nameof(SelectedEngine));
            }
        }

        // 引擎清單供 #5 下拉選用
        public ObservableCollection<string> AvailableEngines { get; } = new ObservableCollection<string>();

        // 引擎選擇命令（參數為引擎名稱）
        private ICommand _selectEngineCommand;
    public ICommand SelectEngineCommand => _selectEngineCommand ??= new RelayCommand<string>(engineName =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(engineName)) return;
        if (Enum.TryParse<MonLingo.Core.Service.TranslationEngine>(engineName, out var engine))
                {
                    _translateService?.SetTranslationEngine(engine);
                    SelectedEngine = engine.ToString();
                    Logger.Info($"[Button5] 已切換翻譯引擎為: {SelectedEngine}");
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, $"[Button5] 切換引擎失敗: {engineName}");
            }
        });

        private int _remainingTranslations = 999;
        public int RemainingTranslations
        {
            get => _remainingTranslations;
            set
            {
                _remainingTranslations = value;
                OnPropertyChanged(nameof(RemainingTranslations));
            }
        }

        private int _coinBalance = 1250;
        public int CoinBalance
        {
            get => _coinBalance;
            set
            {
                _coinBalance = value;
                OnPropertyChanged(nameof(CoinBalance));
            }
        }

        private string _sourceLanguage = "EN";
        public string SourceLanguage
        {
            get => _sourceLanguage;
            set
            {
                _sourceLanguage = value;
                OnPropertyChanged(nameof(SourceLanguage));
            }
        }

        private string _targetLanguage = "中文";
        public string TargetLanguage
        {
            get => _targetLanguage;
            set
            {
                _targetLanguage = value;
                OnPropertyChanged(nameof(TargetLanguage));
            }
        }

        private bool _isCoverModeEnabled = true;
        public bool IsCoverModeEnabled
        {
            get => _isCoverModeEnabled;
            set
            {
                _isCoverModeEnabled = value;
                OnPropertyChanged(nameof(IsCoverModeEnabled));
            }
        }

        private bool _isAutoTranslateEnabled = false;
        public bool IsAutoTranslateEnabled
        {
            get => _isAutoTranslateEnabled;
            set
            {
                _isAutoTranslateEnabled = value;
                OnPropertyChanged(nameof(IsAutoTranslateEnabled));
            }
        }

        private bool _isComicModeEnabled = false;
        public bool IsComicModeEnabled
        {
            get => _isComicModeEnabled;
            set
            {
                _isComicModeEnabled = value;
                OnPropertyChanged(nameof(IsComicModeEnabled));
            }
        }

        private bool _isCollapsed = false;
        public bool IsCollapsed
        {
            get => _isCollapsed;
            set
            {
                _isCollapsed = value;
                OnPropertyChanged(nameof(IsCollapsed));
            }
        }

        #endregion

        #region 命令

        // 1. 開始翻譯
        private ICommand _startTranslationCommand;
        public ICommand StartTranslationCommand =>
            _startTranslationCommand ??= new AsyncRelayCommand(async () =>
            {
                try
                {
                    Logger.Info("▶️ StartTranslationCommand 觸發：收集既有區域並開始翻譯");

                    // 觸發 UI 轉動動畫（如果視圖端有實作 Storyboard，透過事件聚合或直接調方法）
                    TrySpinStartIconOnce();

                    // 取得目前框選區域（10/11 號建立），包含隱藏框
                    var regionsInfo = MonLingo.Core.View.Windows.SelectionBoxOverlay.Instance.GetAllRegions(includeHidden: true);
                    if (regionsInfo == null || regionsInfo.Count == 0)
                    {
                        MessageBox.Show("尚未建立任何翻譯區域，請先使用 10 或 11 號按鈕框選區域。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    // 轉為 Rect 清單（忽略編號，順序已排序）
                    var regions = new System.Collections.Generic.List<System.Windows.Rect>();
                    foreach (var (rect, number) in regionsInfo)
                    {
                        regions.Add(rect);
                    }

                    // 準備/重用快速翻譯服務（共用字幕視窗）
                    if (_globalQuickTranslationService == null)
                    {
                        _globalQuickTranslationService = new MonLingo.Core.Service.QuickTranslationService(_mainBarWindow);
                    }

                    await _globalQuickTranslationService.StartRegionTranslationAsync(regions);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "StartTranslationCommand 執行失敗");
                    MessageBox.Show($"開始翻譯時發生錯誤：{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });

        // 嘗試讓 1 號按鈕動畫轉一圈（視圖端可透過名稱查找並啟動 Storyboard，或發事件讓視圖端處理）
        private void TrySpinStartIconOnce()
        {
            try
            {
                if (_mainBarWindow is MonLingo.Core.View.Windows.MainBarWindow view)
                {
                    (view as MonLingo.Core.View.Windows.MainBarWindow)?.SpinStartButtonOnce();
                }
            }
            catch { }
        }

        private void InitializeEngines()
        {
            try
            {
                // 填充 enum 名稱作為顯示
                AvailableEngines.Clear();
                foreach (var name in Enum.GetNames(typeof(MonLingo.Core.Service.TranslationEngine)))
                {
                    AvailableEngines.Add(name);
                }

                // 同步 SelectedEngine 與服務當前值
                var current = _translateService?.CurrentEngine.ToString();
                if (!string.IsNullOrWhiteSpace(current))
                {
                    SelectedEngine = current;
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "初始化翻譯引擎清單失敗");
            }
        }

        // 2. 升級到PRO
        private ICommand _upgradeToProCommand;
        public ICommand UpgradeToProCommand =>
            _upgradeToProCommand ??= new SimpleRelayCommand(() =>
            {
                MessageBox.Show("升級到PRO功能被點擊！\n正在打開升級頁面...", "升級PRO", MessageBoxButton.OK, MessageBoxImage.Information);
            });

        // 3. 每日簽到
        private ICommand _dailyCheckInCommand;
        public ICommand DailyCheckInCommand =>
            _dailyCheckInCommand ??= new SimpleRelayCommand(() =>
            {
                RemainingTranslations += 10; // 模擬獲得翻譯次數
                MessageBox.Show($"每日簽到完成！\n獲得 10 次翻譯機會\n當前剩餘: {RemainingTranslations} 次", "每日簽到", MessageBoxButton.OK, MessageBoxImage.Information);
            });

        // 4. 購買硬幣
        private ICommand _purchaseCoinsCommand;
        public ICommand PurchaseCoinsCommand =>
            _purchaseCoinsCommand ??= new SimpleRelayCommand(() =>
            {
                MessageBox.Show("購買硬幣功能被點擊！\n正在打開硬幣商店...", "硬幣商店", MessageBoxButton.OK, MessageBoxImage.Information);
            });

        // 5. 語言設置
        private ICommand _languageSettingsCommand;
        public ICommand LanguageSettingsCommand =>
            _languageSettingsCommand ??= new SimpleRelayCommand(() =>
            {
                try
                {
                    Logger.Info("[Button7] 打開/聚焦設定，跳轉至『翻譯語言』頁");

                    // 嘗試尋找已開啟的設定視窗
                    var existing = Application.Current.Windows
                        .OfType<SettingMainWindow>()
                        .FirstOrDefault();

                    if (existing != null)
                    {
                        // 已存在：還原並置頂到前景，並跳轉到翻譯語言
                        if (existing.WindowState == WindowState.Minimized)
                            existing.WindowState = WindowState.Normal;
                        existing.Activate();
                        existing.Topmost = true; // 暫時置頂以確保可見
                        existing.Topmost = false;
                        existing.OpenTranslationLanguagePage();
                        Logger.Debug("[Button7] 已聚焦現有設定視窗並跳轉頁面");
                        return;
                    }

                    // 不存在：建立新的設定視窗
                    var mainWindow = Application.Current.MainWindow as MainBarWindow;
                    if (mainWindow != null) mainWindow.Topmost = false; // 避免遮擋

                    var settingsWindow = new SettingMainWindow
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterScreen
                    };
                    settingsWindow.OpenTranslationLanguagePage();
                    settingsWindow.Closed += (s, e) =>
                    {
                        if (mainWindow != null) mainWindow.Topmost = true;
                    };
                    settingsWindow.Show();
                }
                catch (Exception ex)
                {
                    var mainWindow = Application.Current.MainWindow as MainBarWindow;
                    if (mainWindow != null)
                    {
                        mainWindow.Topmost = true;
                    }
                    MessageBox.Show($"打開語言設定時發生錯誤：{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });

        /// <summary>
        /// 刷新語言顯示
        /// </summary>
        private async Task RefreshLanguageDisplayAsync()
        {
            try
            {
                if (_languageConfigService != null)
                {
                    var sourceLanguage = await _languageConfigService.GetSourceLanguageAsync();
                    var targetLanguage = await _languageConfigService.GetTargetLanguageAsync();
                    
                    // 將語言代碼轉換為顯示文字
                    var src = ConvertLanguageCodeToDisplay(sourceLanguage);
                    var tgt = ConvertLanguageCodeToDisplay(targetLanguage);

                    // 確保在 UI 執行緒更新
                    if (Application.Current?.Dispatcher != null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            SourceLanguage = src;
                            TargetLanguage = tgt;
                        });
                    }
                    else
                    {
                        SourceLanguage = src;
                        TargetLanguage = tgt;
                    }
                    
                    Logger.Info($"語言顯示已刷新: {SourceLanguage} → {TargetLanguage}");
                }
                else
                {
                    // 如果服務不可用，觸發屬性更新
                    OnPropertyChanged(nameof(SourceLanguage));
                    OnPropertyChanged(nameof(TargetLanguage));
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"刷新語言顯示失敗: {ex.Message}");
                // 觸發屬性更新以確保UI響應
                OnPropertyChanged(nameof(SourceLanguage));
                OnPropertyChanged(nameof(TargetLanguage));
            }
        }

        /// <summary>
        /// 語言設定變更事件處理：即時更新 7 號按鈕顯示
        /// </summary>
        private async void OnLanguageConfigChanged(object sender, EventArgs e)
        {
            try
            {
                await RefreshLanguageDisplayAsync();
            }
            catch (Exception ex)
            {
                Logger.Warn($"OnLanguageConfigChanged 更新顯示失敗: {ex.Message}");
            }
        }

        // 依據語言顯示名稱 → 語言代碼 → 轉短標籤 (給 7 號按鈕 UI 使用)
        private string ToShortLabelFromDisplay(string displayName, bool isSource)
        {
            try
            {
                var code = LanguageSettings.GetLanguageCodeByDisplayName(displayName, isSource);
                if (string.IsNullOrWhiteSpace(code)) return displayName;

                code = code.ToLowerInvariant();
                // 特殊映射
                if (code == "auto") return "AUTO";
                if (code.StartsWith("zh")) return "中文"; // 包含 zh, zh-tw
                if (code == "uk") return "UA"; // 烏克蘭語

                // 兩段式代碼轉為較短顯示，例如 zh-tw -> ZH-TW (已由中文特判處理)
                if (code.Contains('-'))
                    return code.ToUpperInvariant();

                // 否則直接回傳大寫語言代碼
                return code.ToUpperInvariant();
            }
            catch
            {
                // 回退到原字串（完整顯示名）
                return displayName;
            }
        }

        // 6. 翻譯引擎對比
        private ICommand _compareEnginesCommand;
        public ICommand CompareEnginesCommand =>
            _compareEnginesCommand ??= new SimpleRelayCommand(() =>
            {
                MessageBox.Show("翻譯引擎對比功能被點擊！\n正在比較不同翻譯引擎的結果...", "引擎對比", MessageBoxButton.OK, MessageBoxImage.Information);
            });

        // 8. 自定義詞典
        private ICommand _openDictionaryCommand;
        public ICommand OpenDictionaryCommand =>
            _openDictionaryCommand ??= new SimpleRelayCommand(() =>
            {
                MessageBox.Show("自定義詞典功能被點擊！\n正在打開詞典設定...", "詞典功能", MessageBoxButton.OK, MessageBoxImage.Information);
            });

        // 9. 快速截圖翻譯
        private ICommand _quickScreenshotCommand;
        private static MonLingo.Core.Service.QuickTranslationService _globalQuickTranslationService;
        
        public ICommand QuickScreenshotCommand =>
            _quickScreenshotCommand ??= new AsyncRelayCommand(async () =>
            {
                Logger.Info("🖱️ QuickScreenshotCommand 被觸發");
                Logger.Debug($"📋 命令執行時狀態: _mainBarWindow={((_mainBarWindow == null) ? "null" : _mainBarWindow.GetType().Name)}, _globalQuickTranslationService={((_globalQuickTranslationService == null) ? "null" : "已存在")}");
                
                try
                {
                    // 使用靜態服務實例，確保字幕視窗可以重複使用
                    if (_globalQuickTranslationService == null)
                    {
                        Logger.Info("🔨 創建新的 QuickTranslationService 實例");
                        Logger.Debug($"📋 傳入主視窗: {((_mainBarWindow == null) ? "null" : _mainBarWindow.GetType().Name)}");
                        _globalQuickTranslationService = new MonLingo.Core.Service.QuickTranslationService(_mainBarWindow);
                        Logger.Info("✅ QuickTranslationService 實例創建完成");
                    }
                    else
                    {
                        Logger.Info("♻️ 重複使用現有的 QuickTranslationService 實例");
                    }
                    
                    Logger.Info("🚀 開始執行快速翻譯");
                    await _globalQuickTranslationService.StartQuickTranslationAsync();
                    Logger.Info("✅ 快速翻譯執行完成");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "❌ 快速翻譯過程中發生錯誤");
                    MessageBox.Show($"快速翻譯出錯: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });

        // 10. 翻譯區域選擇
        private ICommand _selectRegionCommand;
        private bool _isRegion1Processing = false;
        public ICommand SelectRegionCommand =>
            _selectRegionCommand ??= new SimpleRelayCommand(async () =>
            {
                try
                {
                    // 防抖：避免連續點擊導致重複處理
                    if (_isRegion1Processing)
                    {
                        Logger.Info("[Toolbar10] 正在處理中，忽略重複點擊");
                        return;
                    }
                    _isRegion1Processing = true;

                    Logger.Info("[Toolbar10] SelectRegionCommand 觸發");

                    var overlayHost = MonLingo.Core.View.Windows.SelectionBoxOverlay.Instance;
                    var allRegions = overlayHost?.GetAllRegions(includeHidden: true) ?? new System.Collections.Generic.List<(System.Windows.Rect rect, int number)>();
                    Logger.Info($"[Toolbar10] 目前已存在框數量(含隱藏)={allRegions.Count}; 編號列表=[{string.Join(",", allRegions.ConvertAll(r => r.number.ToString()))}]");

                    bool has1 = overlayHost != null && overlayHost.HasBox(1);
                    Logger.Info($"[Toolbar10] HasBox(1)={has1}");
                    if (has1)
                    {
                        bool visible1 = overlayHost.IsBoxVisible(1);
                        Logger.Info($"[Toolbar10] IsBoxVisible(1)={visible1}");
                        if (visible1)
                        {
                            Logger.Info("[Toolbar10] 分支=Visible→Flash");
                            await overlayHost.FlashBoxByNumberAsync(1);
                            return;
                        }
                        bool showOk = overlayHost.ShowBoxByNumber(1);
                        Logger.Info($"[Toolbar10] 分支=Hidden→Show 結果={showOk}");
                        if (showOk)
                        {
                            return;
                        }
                        else
                        {
                            Logger.Warn("[Toolbar10] Hidden→Show 失敗，將回退為創建新框");
                        }
                    }

                    // 否則創建1號區域選擇窗口
                    Logger.Info("[Toolbar10] 分支=Create 新建 1 號框（進入十字游標模式）");
                    var overlay = new RegionSelectionOverlay(1);
                    
                    // 訂閱區域選擇事件
                    overlay.RegionSelected += (sender, args) =>
                    {
                        var (region, number) = args;
                        Logger.Info($"[Toolbar10] 區域{number}選擇完成: X={region.X}, Y={region.Y}, Width={region.Width}, Height={region.Height}");
                        
                        // 這裡可以保存區域信息或觸發其他處理
                        // TODO: 整合實際的翻譯區域處理邏輯
                        
                        // 注意：不需要手動關閉窗口，RegionSelectionOverlay 會在選擇完成後自動關閉
                    };
                    
                    // 顯示選擇窗口
                    overlay.Show();
                    overlay.Activate();
                    overlay.Focus();
                    Logger.Info("[Toolbar10] RegionSelectionOverlay 窗口已顯示");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "啟動區域選擇時發生錯誤");
                    MessageBox.Show($"啟動區域選擇失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    _isRegion1Processing = false;
                }
            });

        // 11. 翻譯區域2選擇
        private ICommand _selectRegion2Command;
        private bool _isRegion2Processing = false;
        public ICommand SelectRegion2Command =>
            _selectRegion2Command ??= new SimpleRelayCommand(async () =>
            {
                try
                {
                    // 防抖：避免連續點擊導致重複處理
                    if (_isRegion2Processing)
                    {
                        Logger.Info("[Toolbar11] 正在處理中，忽略重複點擊");
                        return;
                    }
                    _isRegion2Processing = true;

                    Logger.Info("[Toolbar11] SelectRegion2Command 觸發");

                    var overlayHost = MonLingo.Core.View.Windows.SelectionBoxOverlay.Instance;
                    var allRegions = overlayHost?.GetAllRegions(includeHidden: true) ?? new System.Collections.Generic.List<(System.Windows.Rect rect, int number)>();
                    Logger.Info($"[Toolbar11] 目前已存在框數量(含隱藏)={allRegions.Count}; 編號列表=[{string.Join(",", allRegions.ConvertAll(r => r.number.ToString()))}]");

                    bool has2 = overlayHost != null && overlayHost.HasBox(2);
                    Logger.Info($"[Toolbar11] HasBox(2)={has2}");
                    if (has2)
                    {
                        bool visible2 = overlayHost.IsBoxVisible(2);
                        Logger.Info($"[Toolbar11] IsBoxVisible(2)={visible2}");
                        if (visible2)
                        {
                            Logger.Info("[Toolbar11] 分支=Visible→Flash");
                            await overlayHost.FlashBoxByNumberAsync(2);
                            return;
                        }
                        bool showOk = overlayHost.ShowBoxByNumber(2);
                        Logger.Info($"[Toolbar11] 分支=Hidden→Show 結果={showOk}");
                        if (showOk)
                        {
                            return;
                        }
                        else
                        {
                            Logger.Warn("[Toolbar11] Hidden→Show 失敗，將回退為創建新框");
                        }
                    }

                    // 否則創建2號區域選擇窗口
                    Logger.Info("[Toolbar11] 分支=Create 新建 2 號框（進入十字游標模式）");
                    var overlay = new RegionSelectionOverlay(2);
                    
                    // 訂閱區域選擇事件
                    overlay.RegionSelected += (sender, args) =>
                    {
                        var (region, number) = args;
                        Logger.Info($"[Toolbar11] 區域{number}選擇完成: X={region.X}, Y={region.Y}, Width={region.Width}, Height={region.Height}");
                        
                        // 這裡可以保存區域信息或觸發其他處理
                        // TODO: 整合實際的翻譯區域處理邏輯
                        
                        // 注意：不需要手動關閉窗口，RegionSelectionOverlay 會在選擇完成後自動關閉
                    };
                    
                    // 顯示選擇窗口
                    overlay.Show();
                    overlay.Activate();
                    overlay.Focus();
                    Logger.Info("[Toolbar11] RegionSelectionOverlay 窗口已顯示");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "啟動區域選擇2時發生錯誤");
                    MessageBox.Show($"啟動區域選擇2失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    _isRegion2Processing = false;
                }
            });

        // 12. 漫畫翻譯優化
        private ICommand _comicModeCommand;
        public ICommand ComicModeCommand =>
            _comicModeCommand ??= new SimpleRelayCommand(() =>
            {
                IsComicModeEnabled = !IsComicModeEnabled;
                MessageBox.Show($"漫畫翻譯模式: {(IsComicModeEnabled ? "已開啟" : "已關閉")}\n優化漫畫翻譯效果", "漫畫模式", MessageBoxButton.OK, MessageBoxImage.Information);
            });

        // 15. 編輯窗口
        private ICommand _openEditorCommand;
        public ICommand OpenEditorCommand =>
            _openEditorCommand ??= new SimpleRelayCommand(() =>
            {
                Logger.Info("[Button15] 編輯窗口功能被點擊");
                ExecuteOpenEditorCommand();
            });

        // 16. 更多工具
        private ICommand _showMoreToolsCommand;
        public ICommand ShowMoreToolsCommand =>
            _showMoreToolsCommand ??= new SimpleRelayCommand(() =>
            {
                MessageBox.Show("更多工具功能被點擊！\n顯示額外工具選項...", "更多工具", MessageBoxButton.OK, MessageBoxImage.Information);
            });

        // 17. 設置
        private ICommand _openSettingsCommand;
        public ICommand OpenSettingsCommand =>
            _openSettingsCommand ??= new SimpleRelayCommand(() =>
            {
                try
                {
                    Logger.Info("⚙️ 設定按鈕被點擊，正在打開設定頁面...");
                    
                    // 獲取主窗口參考以管理 Topmost 狀態
                    var mainWindow = Application.Current.MainWindow as MainBarWindow;
                    if (mainWindow != null)
                    {
                        // 暫時禁用工具條的 Topmost 設定，避免與設置窗口衝突
                        mainWindow.Topmost = false;
                        Logger.Info("🔽 暫時禁用工具條 Topmost 設定");
                    }
                    
                    var settingsWindow = new SettingMainWindow();
                    Logger.Debug("✅ SettingMainWindow 實例已創建");
                    
                    // 當設置窗口關閉時，恢復工具條的 Topmost 設定
                    settingsWindow.Closed += (s, args) =>
                    {
                        if (mainWindow != null)
                        {
                            mainWindow.Topmost = true;
                            Logger.Info("🔼 恢復工具條 Topmost 設定");
                        }
                    };
                    
                    // 使用 Show() 而不是 ShowDialog() 以避免阻塞工具條
                    settingsWindow.Show();
                    Logger.Info("✅ 設定頁面已顯示，工具條保持可用");
                }
                catch (Exception ex)
                {
                    // 發生錯誤時也要恢復 Topmost 設定
                    var mainWindow = Application.Current.MainWindow as MainBarWindow;
                    if (mainWindow != null)
                    {
                        mainWindow.Topmost = true;
                    }
                    Logger.Error(ex, "❌ 打開設定頁面時發生錯誤");
                    MessageBox.Show($"設置功能無法載入！\n錯誤詳情：{ex.Message}", "設置", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });

        // 18. 最小化
        private ICommand _minimizeCommand;
        public ICommand MinimizeCommand =>
            _minimizeCommand ??= new SimpleRelayCommand(() =>
            {
                try
                {
                    // 使用事件讓視圖端 (MainBarWindow) 處理實際的最小化/隱藏行為
                    OnMinimizeRequested();
                    Logger.Info("MinimizeCommand: MinimizeRequested event raised");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "MinimizeCommand 執行失敗");
                }
            });

        // 19. 關閉
        private ICommand _exitCommand;
        public ICommand ExitCommand =>
            _exitCommand ??= new SimpleRelayCommand(() =>
            {
                try
                {
                    if (MessageBox.Show("確定要退出 MonLingo 嗎？", "退出確認", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        // 使用事件讓 MainBarWindow 處理乾淨的關閉流程
                        OnExitRequested();
                        Logger.Info("ExitCommand: ExitRequested event raised");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "ExitCommand 執行失敗");
                }
            });

        // 20. 收縮工具條
        private ICommand _collapseCommand;
        public ICommand CollapseCommand =>
            _collapseCommand ??= new SimpleRelayCommand(() =>
            {
                IsCollapsed = !IsCollapsed;
                
                // 觸發收縮切換事件，讓 View 處理實際的收縮邏輯
                CollapseRequested?.Invoke(this, new CollapseEventArgs { IsCollapsed = IsCollapsed });
            });

        #endregion

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Window Events
        
        /// <summary>
        /// 截圖請求事件
        /// </summary>
        public event EventHandler CaptureRequested;
        
        /// <summary>
        /// 設定請求事件  
        /// </summary>
        public event EventHandler SettingsRequested;
        
        /// <summary>
        /// 退出請求事件
        /// </summary>
        public event EventHandler ExitRequested;
        
        /// <summary>
        /// 最小化請求事件
        /// </summary>
        public event EventHandler MinimizeRequested;
        
        /// <summary>
        /// 收縮切換請求事件
        /// </summary>
        public event EventHandler<CollapseEventArgs> CollapseRequested;

        protected virtual void OnCaptureRequested()
        {
            CaptureRequested?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnSettingsRequested()
        {
            SettingsRequested?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnExitRequested()
        {
            ExitRequested?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnMinimizeRequested()
        {
            MinimizeRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 執行編輯窗口命令 (按鈕15)
        /// </summary>
    private void ExecuteOpenEditorCommand()
        {
            try
            {
                Logger.Info("[Button15] 開始執行編輯窗口命令");

                // 獲取當前翻譯結果
                var currentResults = GetCurrentTranslationResults();
                if (!currentResults.HasValue || (string.IsNullOrEmpty(currentResults.Value.OriginalText) && string.IsNullOrEmpty(currentResults.Value.TranslatedText)))
                {
                    Logger.Warn("[Button15] 沒有找到當前回合的翻譯結果");
                    MessageBox.Show("沒有可編輯的翻譯結果。\n請先執行翻譯功能（按鈕1）。", "編輯器", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                Logger.Info($"[Button15] 找到翻譯結果 - 原文長度: {currentResults.Value.OriginalText?.Length ?? 0}, 譯文長度: {currentResults.Value.TranslatedText?.Length ?? 0}");

                // 使用當前 VM 顯示在 7 號按鈕上的語言（已由語言服務維護）
                var srcLangDisplay = this.SourceLanguage;
                var tgtLangDisplay = this.TargetLanguage;

                // 建立編輯窗口（模型視窗），不阻擋工具條
                var editWindow = new MonLingo.Core.View.Windows.EditWindow(
                    currentResults.Value.OriginalText ?? string.Empty,
                    currentResults.Value.TranslatedText ?? string.Empty,
                    srcLangDisplay,
                    tgtLangDisplay
                );

                // 監聽確認事件以回寫結果
                editWindow.EditConfirmed += (s, e) =>
                {
                    Logger.Info("[Button15] 用戶確認編輯（非模態）");
                    UpdateTranslationResults(e.OriginalText, e.TranslatedText);
                    Logger.Info("[Button15] 編輯結果已更新到字幕窗口");
                };

                Logger.Info("[Button15] 顯示編輯窗口（非模態）");
                editWindow.Show();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "[Button15] 執行編輯窗口命令時發生錯誤");
                MessageBox.Show($"編輯功能發生錯誤：\n{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 獲取當前翻譯結果
        /// </summary>
        private (string OriginalText, string TranslatedText)? GetCurrentTranslationResults()
        {
            try
            {
                Logger.Debug("[Button15] 嘗試獲取當前翻譯結果");

                // 檢查當前是否有活動的字幕窗口和內容
                var subtitleWindow = Application.Current.Windows.Cast<Window>().OfType<MonLingo.Core.View.Windows.SubtitleWindow>().FirstOrDefault();
                if (subtitleWindow?.DataContext is MonLingo.Core.ViewModel.SubtitleViewModel vm)
                {
                    Logger.Debug($"[Button15] 找到字幕窗口，當前有 {vm.SubtitleLines.Count} 行字幕");
                    
                    if (vm.SubtitleLines.Count > 0)
                    {
                        // 獲取最後一行（最新的翻譯結果）
                        var lastLine = vm.SubtitleLines.Last();
                        Logger.Debug($"[Button15] 最後一行 - 原文: '{lastLine.OriginalText}', 譯文: '{lastLine.TranslatedText}'");
                        return (lastLine.OriginalText, lastLine.TranslatedText);
                    }
                }

                Logger.Debug("[Button15] 沒有找到有效的翻譯結果");
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "[Button15] 獲取當前翻譯結果時發生錯誤");
                return null;
            }
        }

        /// <summary>
        /// 更新翻譯結果到字幕窗口
        /// </summary>
        private void UpdateTranslationResults(string originalText, string translatedText)
        {
            try
            {
                Logger.Info($"[Button15] 更新翻譯結果 - 原文: '{originalText}', 譯文: '{translatedText}'");

                // 查找字幕窗口並更新最後一行
                var subtitleWindow = Application.Current.Windows.Cast<Window>().OfType<MonLingo.Core.View.Windows.SubtitleWindow>().FirstOrDefault();
                if (subtitleWindow?.DataContext is MonLingo.Core.ViewModel.SubtitleViewModel vm && vm.SubtitleLines.Count > 0)
                {
                    Logger.Debug("[Button15] 更新字幕窗口的最後一行");
                    
                    // 以替換方式更新最後一行，確保 UI 收到 Collection 變更通知
                    var lastIndex = vm.SubtitleLines.Count - 1;
                    var oldItem = vm.SubtitleLines[lastIndex];
                    var newItem = new MonLingo.Core.ViewModel.SubtitleLineItem
                    {
                        OriginalText = originalText,
                        TranslatedText = translatedText,
                        Timestamp = oldItem.Timestamp
                    };
                    vm.SubtitleLines[lastIndex] = newItem;

                    Logger.Info("[Button15] 字幕窗口內容已更新");
                }
                else
                {
                    Logger.Warn("[Button15] 沒有找到可更新的字幕窗口");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "[Button15] 更新翻譯結果時發生錯誤");
            }
        }

        #endregion
    }

    /// <summary>
    /// 簡單的 RelayCommand 實現，避免衝突
    /// </summary>
    public class SimpleRelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public SimpleRelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object parameter)
        {
            _execute();
        }
    }

    /// <summary>
    /// 支援異步操作的RelayCommand
    /// </summary>
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool> _canExecute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<Task> execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter)
        {
            return !_isExecuting && (_canExecute == null || _canExecute());
        }

        public async void Execute(object parameter)
        {
            _isExecuting = true;
            RaiseCanExecuteChanged();

            try
            {
                await _execute();
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        private void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
