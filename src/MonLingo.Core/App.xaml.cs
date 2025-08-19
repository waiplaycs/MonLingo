using System;
using System.Threading.Tasks;
using System.Windows;
using MonLingo.Core.Infrastructure;
using MonLingo.Core.Services;
using MonLingo.View.Windows;
using MonLingo.ViewModel;
using NLog;

namespace MonLingo.Core
{
    /// <summary>
    /// MonLingo WPF 應用程式主類
    /// 應用程式生命週期管理和服務協調
    /// </summary>
    public partial class App : Application
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private MainBarWindow _mainBarWindow;
        private IHotKeyService _hotKeyService;
        private IEventAggregator _eventAggregator;
        private bool _isInitialized = false;

        /// <summary>
        /// 應用程式啟動事件處理器
        /// </summary>
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            OnStartupSync(e);
        }

        /// <summary>
        /// 應用程式啟動（同步版本）
        /// </summary>
        private void OnStartupSync(StartupEventArgs e)
        {
            try
            {
                // 設定例外處理
                SetupExceptionHandling();

                // 簡化初始化：先建立基本的 UI
                CreateMainWindow();

                // 延遲初始化服務（避免阻塞 UI 啟動）
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ServiceContainer.InitializeAsync();
                        
                        // 在 UI 執行緒上獲取服務
                        Dispatcher.Invoke(() =>
                        {
                            try
                            {
                                _hotKeyService = ServiceContainer.GetService<IHotKeyService>();
                                _eventAggregator = ServiceContainer.GetService<IEventAggregator>();
                                
                                // 訂閱事件
                                SubscribeToEvents();
                                
                                _isInitialized = true;
                                Logger.Info("MonLingo 應用程式啟動完成");
                            }
                            catch (Exception ex)
                            {
                                Logger.Error(ex, "服務初始化失敗");
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "背景服務初始化失敗");
                    }
                });

                Logger.Info("MonLingo UI 已啟動，服務正在背景初始化");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "應用程式啟動失敗");
                MessageBox.Show($"啟動失敗: {ex.Message}", "MonLingo", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        /// <summary>
        /// 應用程式關閉
        /// </summary>
        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                if (_isInitialized)
                {
                    // 儲存設定
                    var configService = ServiceContainer.GetService<IConfigService>();
                    configService?.SaveAsync().Wait(2000); // 最多等待2秒

                    // 清理熱鍵
                    _hotKeyService?.Dispose();

                    // 關閉主視窗
                    _mainBarWindow?.Close();

                    // 清理服務容器
                    ServiceContainer.Dispose();
                }

                Logger.Info("MonLingo 應用程式已關閉");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "應用程式關閉異常");
            }

            base.OnExit(e);
        }

        /// <summary>
        /// 設定全域例外處理
        /// </summary>
        private void SetupExceptionHandling()
        {
            // UI執行緒未處理的例外
            DispatcherUnhandledException += (sender, e) =>
            {
                Logger.Error(e.Exception, "UI執行緒例外");
                
                var notificationService = ServiceContainer.GetService<INotificationService>();
                notificationService?.ShowError($"發生錯誤: {e.Exception.Message}");
                
                e.Handled = true; // 防止應用程式崩潰
            };

            // 應用程式域未處理的例外
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                Logger.Fatal(ex, "應用程式域例外");
            };

            // Task未處理的例外
            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                Logger.Error(e.Exception, "Task未處理例外");
                e.SetObserved(); // 標記為已處理
            };
        }

        /// <summary>
        /// 訂閱應用程式事件
        /// </summary>
        private void SubscribeToEvents()
        {
            // 訂閱翻譯事件
            _eventAggregator.Subscribe<StartTranslationEvent>(async (e) =>
            {
                await HandleStartTranslationAsync();
            });

            // 訂閱語音翻譯事件
            _eventAggregator.Subscribe<StartAudioTranslationEvent>(async (e) =>
            {
                await HandleStartAudioTranslationAsync();
            });

            // 訂閱翻譯完成事件
            _eventAggregator.Subscribe<TranslationCompletedEvent>(async (e) =>
            {
                await HandleTranslationCompletedAsync(e.Result as TranslationResult);
            });

            // 訂閱擷取完成事件
            _eventAggregator.Subscribe<CaptureCompletedEvent>(async (e) =>
            {
                await HandleCaptureCompletedAsync(e.Result as CaptureResult);
            });
        }

        /// <summary>
        /// 建立主視窗
        /// </summary>
        private void CreateMainWindow()
        {
            _mainBarWindow = new MainBarWindow();
            
            // 設定視窗位置
            var configService = ServiceContainer.GetService<IConfigService>();
            if (configService != null)
            {
                var x = configService.GetSetting("Window_MainBar_X", 100.0);
                var y = configService.GetSetting("Window_MainBar_Y", 50.0);
                
                _mainBarWindow.Left = x;
                _mainBarWindow.Top = y;
            }

            // 顯示主視窗
            _mainBarWindow.Show();
        }

        /// <summary>
        /// 處理開始翻譯事件
        /// </summary>
        private async Task HandleStartTranslationAsync()
        {
            try
            {
                Logger.Info("開始文字翻譯流程");

                // 顯示擷取區域選擇視窗
                var captureWindow = new CaptureRegionWindow();
                var result = captureWindow.ShowDialog();

                if (result == true && captureWindow.SelectedRegion != Rect.Empty)
                {
                    var region = captureWindow.SelectedRegion;
                    
                    // 執行螢幕擷取
                    var captureService = ServiceContainer.GetRequiredService<ICaptureService>();
                    var captureResult = await captureService.CaptureRegionAsync(region);

                    if (captureResult.IsSuccess && !string.IsNullOrWhiteSpace(captureResult.ExtractedText))
                    {
                        // 執行翻譯
                        var translateService = ServiceContainer.GetRequiredService<ITranslateService>();
                        var translationResult = await translateService.TranslateAsync(captureResult.ExtractedText);

                        // 發布翻譯完成事件
                        await _eventAggregator.PublishAsync(new TranslationCompletedEvent 
                        { 
                            Result = translationResult 
                        });
                    }
                    else
                    {
                        var notificationService = ServiceContainer.GetService<INotificationService>();
                        notificationService?.ShowWarning("未能從擷取區域識別到文字");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "文字翻譯流程異常");
                var notificationService = ServiceContainer.GetService<INotificationService>();
                notificationService?.ShowError($"翻譯失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 處理開始語音翻譯事件
        /// </summary>
        private async Task HandleStartAudioTranslationAsync()
        {
            try
            {
                Logger.Info("開始語音翻譯流程");

                var audioService = ServiceContainer.GetRequiredService<IAudioService>();
                var notificationService = ServiceContainer.GetService<INotificationService>();

                notificationService?.ShowInfo("開始錄音，按任意鍵停止...");

                // 開始音訊錄製
                var audioResult = await audioService.StartRecordingAsync(); // 開始錄音

                if (audioResult.IsSuccess)
                {
                    // TODO: 實現語音轉文字功能
                    // 現在暫時模擬
                    var recognizedText = "這是模擬的語音識別結果";

                    if (!string.IsNullOrWhiteSpace(recognizedText))
                    {
                        // 執行翻譯
                        var translateService = ServiceContainer.GetRequiredService<ITranslateService>();
                        var translationResult = await translateService.TranslateAsync(recognizedText);

                        // 發布翻譯完成事件
                        await _eventAggregator.PublishAsync(new TranslationCompletedEvent 
                        { 
                            Result = translationResult 
                        });
                    }
                    else
                    {
                        notificationService?.ShowWarning("未能識別語音內容");
                    }
                }
                else
                {
                    notificationService?.ShowError($"錄音失敗: {audioResult.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "語音翻譯流程異常");
                var notificationService = ServiceContainer.GetService<INotificationService>();
                notificationService?.ShowError($"語音翻譯失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 處理翻譯完成事件
        /// </summary>
        private async Task HandleTranslationCompletedAsync(TranslationResult result)
        {
            try
            {
                if (result.IsSuccess)
                {
                    // 顯示翻譯結果
                    var resultWindow = new TranslationPopupWindow(new Rect(100, 100, 400, 300));
                    var viewModel = resultWindow.DataContext as TranslationResultViewModel;
                    
                    if (viewModel != null)
                    {
                        viewModel.OriginalText = result.OriginalText;
                        viewModel.TranslatedText = result.TranslatedText;
                        // viewModel.SourceLanguage = result.SourceLanguage;
                        // viewModel.TargetLanguage = result.TargetLanguage;
                        // viewModel.Engine = result.Engine;
                        // viewModel.Confidence = result.Confidence;
                        viewModel.ProcessingTime = result.ProcessingTime.TotalSeconds;
                    }

                    resultWindow.Show();

                    // 加入歷史記錄
                    var dataService = ServiceContainer.GetService<IDataService>();
                    if (dataService != null)
                    {
                        var translationHistory = new TranslationHistory
                        {
                            OriginalText = result.OriginalText,
                            TranslatedText = result.TranslatedText,
                            SourceLanguage = result.SourceLanguage,
                            TargetLanguage = result.TargetLanguage,
                            Engine = result.Engine,
                            Timestamp = DateTime.Now
                        };

                        await dataService.SaveTranslationHistoryAsync(translationHistory);
                    }

                    Logger.Info($"翻譯完成: {result.OriginalText} -> {result.TranslatedText}");
                }
                else
                {
                    var notificationService = ServiceContainer.GetService<INotificationService>();
                    notificationService?.ShowError($"翻譯失敗: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "處理翻譯結果異常");
            }
        }

        /// <summary>
        /// 處理擷取完成事件
        /// </summary>
        private async Task HandleCaptureCompletedAsync(CaptureResult result)
        {
            try
            {
                if (result.IsSuccess)
                {
                    Logger.Info($"擷取完成: 識別文字 '{result.ExtractedText}'");
                }
                else
                {
                    Logger.Warn($"擷取失敗: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "處理擷取結果異常");
            }

            await Task.CompletedTask;
        }
    }
}
