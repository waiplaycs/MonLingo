using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Data;
using MonLingo.Core.Service;
using MonLingo.Core.Infrastructure;

namespace MonLingo.View.Windows
{
    /// <summary>
    /// SettingMainWindow.xaml 的互動邏輯
    /// 現代化設置界面，採用左側導航和右側內容的佈局
    /// </summary>
    public partial class SettingMainWindow : Window
    {
        private Button _currentSelectedButton;
        // 翻譯語言頁：保存清單參考，便於外部對接事件
        private ListBox _sourceLangListBox;
        private ListBox _targetLangListBox;
        
        // 語言配置服務
        private ILanguageConfigService _languageConfigService;

        // 對外事件：當使用者在翻譯語言頁更改選擇時觸發
        public event Action<string, string> LanguageSelectionChanged;

        public SettingMainWindow()
        {
            InitializeComponent();
            
            // 初始化語言配置服務
            InitializeLanguageConfigService();
            
            // 設置預設選中的按鈕
            SetDefaultSelection();
        }

        /// <summary>
        /// 初始化語言配置服務
        /// </summary>
        private void InitializeLanguageConfigService()
        {
            try
            {
                _languageConfigService = Phase5ServiceContainer.GetService<ILanguageConfigService>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"語言配置服務初始化失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 設置預設選中狀態
        /// </summary>
        private void SetDefaultSelection()
        {
            // 預設選中全局設置按鈕
            var globalButton = FindButtonByTag("全局設置");
            if (globalButton != null)
            {
                SelectNavButton(globalButton);
                LoadGlobalSettings(); // 顯示全局設置內容
            }
        }

        /// <summary>
        /// 查找指定標籤的按鈕
        /// </summary>
        private Button FindButtonByTag(string tag)
        {
            return FindChild<Button>(this, b => b.Tag?.ToString() == tag);
        }

        /// <summary>
        /// 遞歸查找子控件
        /// </summary>
        private T FindChild<T>(DependencyObject parent, Func<T, bool> predicate = null) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is T tChild && (predicate == null || predicate(tChild)))
                {
                    return tChild;
                }

                var foundChild = FindChild<T>(child, predicate);
                if (foundChild != null)
                    return foundChild;
            }

            return null;
        }

        /// <summary>
        /// 導航按鈕點擊事件
        /// </summary>
        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                SelectNavButton(button);
                LoadContent(button.Tag?.ToString());
            }
        }

        /// <summary>
        /// 選中導航按鈕
        /// </summary>
        private void SelectNavButton(Button button)
        {
            // 重置前一個按鈕的樣式
            if (_currentSelectedButton != null)
            {
                _currentSelectedButton.Style = (Style)FindResource("SidebarButtonStyle");
            }

            // 設置當前按鈕為選中狀態
            _currentSelectedButton = button;
            button.Style = (Style)FindResource("SidebarButtonSelectedStyle");
        }

        /// <summary>
        /// 載入對應的內容
        /// </summary>
        private void LoadContent(string contentType)
        {
            switch (contentType)
            {
                case "翻譯語言":
                    LoadTranslationLanguageSettings();
                    break;
                case "全局設置":
                    LoadGlobalSettings();
                    break;
                case "翻譯設置":
                    LoadTranslationSettings();
                    break;
                case "顯示設置":
                    LoadDisplaySettings();
                    break;
                case "個人賬號":
                    LoadAccountSettings();
                    break;
                case "會員":
                    LoadMembershipSettings();
                    break;
                case "硬幣":
                    LoadCoinSettings();
                    break;
                case "快捷鍵":
                    LoadShortcutSettings();
                    break;
                case "聯繫官方":
                    LoadContactOfficial();
                    break;
                case "幫助中心":
                    LoadHelpCenter();
                    break;
                case "關於我們":
                    LoadAboutUs();
                    break;
                default:
                    LoadGlobalSettings();
                    break;
            }
        }

        /// <summary>
        /// 載入全局設置（預設）
        /// </summary>
        private void LoadGlobalSettings()
        {
            // 創建全局設置內容
            CreateSettingsContent("全局設置", "🌐", CreateGlobalSettingsContent());
        }

        /// <summary>
        /// 載入翻譯設置
        /// </summary>
        private void LoadTranslationSettings()
        {
            // 創建翻譯設置內容
            CreateSettingsContent("翻譯設置", "🌐", CreateTranslationSettingsContent());
        }

        /// <summary>
        /// 載入顯示設置
        /// </summary>
        private void LoadDisplaySettings()
        {
            CreateSettingsContent("顯示設置", "🖥️", CreateDisplaySettingsContent());
        }

        /// <summary>
        /// 載入OCR設置
        /// </summary>
        private void LoadOcrSettings()
        {
            CreateSettingsContent("文字識別設置", "📄", CreateOcrSettingsContent());
        }

        /// <summary>
        /// 載入個人賬號設置
        /// </summary>
        private void LoadAccountSettings()
        {
            CreateSettingsContent("個人賬號", "👤", CreateAccountSettingsContent());
        }

        /// <summary>
        /// 載入會員設置
        /// </summary>
        private void LoadMembershipSettings()
        {
            CreateSettingsContent("會員", "👑", CreateMembershipSettingsContent());
        }

        /// <summary>
        /// 載入硬幣設置
        /// </summary>
        private void LoadCoinSettings()
        {
            CreateSettingsContent("硬幣", "🪙", CreateCoinSettingsContent());
        }

        /// <summary>
        /// 載入安全設置
        /// </summary>
        private void LoadSecuritySettings()
        {
            CreateSettingsContent("保護鍵", "🔐", CreateSecuritySettingsContent());
        }

        /// <summary>
        /// 載入聯繫客服
        /// </summary>
        private void LoadContactSupport()
        {
            CreateSettingsContent("聯繫客服", "💬", CreateContactSupportContent());
        }

        /// <summary>
        /// 載入企業設置
        /// </summary>
        private void LoadEnterpriseSettings()
        {
            CreateSettingsContent("企業版", "🏢", CreateEnterpriseSettingsContent());
        }

        /// <summary>
        /// 載入意見反饋
        /// </summary>
        private void LoadFeedbackSettings()
        {
            CreateSettingsContent("意見反饋", "💡", CreateFeedbackSettingsContent());
        }

        /// <summary>
        /// 創建設置內容
        /// </summary>
        private void CreateSettingsContent(string title, string icon, FrameworkElement content)
        {
            // 清空並重新創建動態內容
            ContentPanel.Children.Clear();
            
            // 創建標題
            var titlePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 30) };
            titlePanel.Children.Add(new TextBlock 
            { 
                Text = $"{icon} {title}", 
                FontSize = 24, 
                FontWeight = FontWeights.Bold, 
                Foreground = new SolidColorBrush(Color.FromRgb(31, 41, 55)) 
            });
            
            ContentPanel.Children.Add(titlePanel);
            ContentPanel.Children.Add(content);
        }

        #region 內容創建方法

        private FrameworkElement CreateGlobalSettingsContent()
        {
            var panel = new StackPanel();

            // 頁面標題
            var titleBlock = new TextBlock
            {
                Text = "全局設置",
                Style = (Style)FindResource("SettingTitleStyle"),
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 8)
            };
            panel.Children.Add(titleBlock);

            var subtitleBlock = new TextBlock
            {
                Text = "配置MonLingo的基本行為和偏好設置",
                Style = (Style)FindResource("SettingSubtitleStyle"),
                Margin = new Thickness(0, 0, 0, 24)
            };
            panel.Children.Add(subtitleBlock);

            // 翻譯偵測設置卡片
            var detectionCard = CreateMacOSSettingCard("翻譯偵測設置", "配置文字識別和偵測相關選項");
            var detectionStack = (StackPanel)((Border)detectionCard).Child;

            detectionStack.Children.Add(CreateMacOSComboBoxSetting("偵測模式", "選擇文字偵測的方式", new[] { "區域模式", "全螢幕模式" }));
            detectionStack.Children.Add(CreateMacOSComboBoxSetting("界面語言", "選擇應用程式界面語言", new[] { "繁體中文", "簡體中文", "English" }));
            detectionStack.Children.Add(CreateMacOSComboBoxSetting("神經設置架構", "選擇AI處理模式", new[] { "標準模式", "效能模式", "精確模式" }));

            panel.Children.Add(detectionCard);

            // 翻譯區域設置卡片
            var regionCard = CreateMacOSSettingCard("翻譯區域設置", "配置翻譯範圍和處理方式");
            var regionStack = (StackPanel)((Border)regionCard).Child;

            regionStack.Children.Add(CreateMacOSComboBoxSetting("配置模式範圍", "設定翻譯區域選擇方式", new[] { "自動偵測", "手動選擇" }));
            regionStack.Children.Add(CreateMacOSComboBoxSetting("翻譯型資料模式", "選擇翻譯處理方式", new[] { "即時翻譯", "批次翻譯" }));

            // macOS風格重置按鈕
            var resetButton = new Button
            {
                Content = "重置為預設值",
                Style = (Style)FindResource("ResetButtonStyle"),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 16, 0, 0)
            };
            resetButton.Click += ResetButton_Click;
            regionStack.Children.Add(resetButton);

            panel.Children.Add(regionCard);

            return panel;
        }

        private FrameworkElement CreateTranslationSettingsContent()
        {
            var panel = new StackPanel();
            
            // 翻譯引擎設置
            var engineGroup = CreateSettingGroup("翻譯引擎設置", new StackPanel());
            var engineStack = (StackPanel)((Border)engineGroup).Child;
            
            engineStack.Children.Add(CreateComboBoxSetting("預設翻譯引擎", new[] { "Google 翻譯", "DeepL", "Microsoft Translator", "百度翻譯", "騰訊翻譯", "本地模型" }));
            engineStack.Children.Add(CreateComboBoxSetting("來源語言", new[] { "自動偵測", "繁體中文", "簡體中文", "英文", "日文", "韓文", "法文", "德文", "西班牙文" }));
            engineStack.Children.Add(CreateComboBoxSetting("目標語言", new[] { "繁體中文", "簡體中文", "英文", "日文", "韓文", "法文", "德文", "西班牙文" }));
            
            panel.Children.Add(engineGroup);
            
            // 翻譯品質設置
            var qualityGroup = CreateSettingGroup("翻譯品質設置", new StackPanel());
            var qualityStack = (StackPanel)((Border)qualityGroup).Child;
            
            qualityStack.Children.Add(CreateToggleSetting("啟用多引擎對比", true));
            qualityStack.Children.Add(CreateToggleSetting("智能語言偵測", true));
            qualityStack.Children.Add(CreateSliderSetting("翻譯準確度", 0, 100, 85));
            
            panel.Children.Add(qualityGroup);
            
            return panel;
        }

        private FrameworkElement CreateDisplaySettingsContent()
        {
            var panel = new StackPanel();
            
            // 字幕顯示設置
            var subtitleGroup = CreateSettingGroup("字幕顯示設置", new StackPanel());
            var subtitleStack = (StackPanel)((Border)subtitleGroup).Child;
            
            subtitleStack.Children.Add(CreateSliderSetting("字型大小", 10, 48, 16));
            subtitleStack.Children.Add(CreateComboBoxSetting("字型", new[] { "微軟正黑體", "新細明體", "標楷體", "Arial", "Times New Roman" }));
            subtitleStack.Children.Add(CreateToggleSetting("顯示背景", true));
            subtitleStack.Children.Add(CreateSliderSetting("背景透明度", 0, 100, 80));
            
            panel.Children.Add(subtitleGroup);
            
            // 工具條設置
            var toolbarGroup = CreateSettingGroup("工具條設置", new StackPanel());
            var toolbarStack = (StackPanel)((Border)toolbarGroup).Child;
            
            toolbarStack.Children.Add(CreateToggleSetting("自動隱藏工具條", false));
            toolbarStack.Children.Add(CreateSliderSetting("工具條透明度", 50, 100, 95));
            toolbarStack.Children.Add(CreateToggleSetting("置頂顯示", true));
            
            panel.Children.Add(toolbarGroup);
            
            return panel;
        }

        private FrameworkElement CreateOcrSettingsContent()
        {
            var panel = new StackPanel();
            
            // OCR 引擎設置
            var ocrGroup = CreateSettingGroup("OCR 引擎設置", new StackPanel());
            var ocrStack = (StackPanel)((Border)ocrGroup).Child;
            
            ocrStack.Children.Add(CreateComboBoxSetting("OCR 引擎", new[] { "PaddleOCR", "Tesseract", "Azure OCR", "Google Vision", "百度 OCR" }));
            ocrStack.Children.Add(CreateToggleSetting("啟用 GPU 加速", true));
            ocrStack.Children.Add(CreateSliderSetting("識別精度", 0, 100, 85));
            
            panel.Children.Add(ocrGroup);
            
            return panel;
        }

        private FrameworkElement CreateAccountSettingsContent()
        {
            var panel = new StackPanel();
            
            var accountGroup = CreateSettingGroup("賬號信息", new StackPanel());
            var accountStack = (StackPanel)((Border)accountGroup).Child;
            
            accountStack.Children.Add(CreateTextDisplaySetting("用戶名", "demo_user"));
            accountStack.Children.Add(CreateTextDisplaySetting("郵箱", "demo@example.com"));
            accountStack.Children.Add(CreateTextDisplaySetting("註冊時間", "2024-01-01"));
            
            panel.Children.Add(accountGroup);
            
            return panel;
        }

        private FrameworkElement CreateMembershipSettingsContent()
        {
            var panel = new StackPanel();
            
            var memberGroup = CreateSettingGroup("會員信息", new StackPanel());
            var memberStack = (StackPanel)((Border)memberGroup).Child;
            
            memberStack.Children.Add(CreateTextDisplaySetting("會員等級", "PRO"));
            memberStack.Children.Add(CreateTextDisplaySetting("到期時間", "2024-12-31"));
            memberStack.Children.Add(CreateTextDisplaySetting("剩餘翻譯次數", "無限制"));
            
            panel.Children.Add(memberGroup);
            
            return panel;
        }

        private FrameworkElement CreateCoinSettingsContent()
        {
            var panel = new StackPanel();
            
            var coinGroup = CreateSettingGroup("硬幣信息", new StackPanel());
            var coinStack = (StackPanel)((Border)coinGroup).Child;
            
            coinStack.Children.Add(CreateTextDisplaySetting("當前餘額", "1,250 硬幣"));
            coinStack.Children.Add(CreateTextDisplaySetting("今日獲得", "50 硬幣"));
            coinStack.Children.Add(CreateTextDisplaySetting("累計獲得", "15,750 硬幣"));
            
            panel.Children.Add(coinGroup);
            
            return panel;
        }

        private FrameworkElement CreateSecuritySettingsContent()
        {
            var panel = new StackPanel();
            
            var securityGroup = CreateSettingGroup("安全設置", new StackPanel());
            var securityStack = (StackPanel)((Border)securityGroup).Child;
            
            securityStack.Children.Add(CreateToggleSetting("啟用雙重驗證", false));
            securityStack.Children.Add(CreateToggleSetting("記住登入狀態", true));
            securityStack.Children.Add(CreateToggleSetting("自動鎖定", false));
            
            panel.Children.Add(securityGroup);
            
            return panel;
        }

        private FrameworkElement CreateContactSupportContent()
        {
            var panel = new StackPanel();
            
            var contactGroup = CreateSettingGroup("聯繫方式", new StackPanel());
            var contactStack = (StackPanel)((Border)contactGroup).Child;
            
            contactStack.Children.Add(CreateTextDisplaySetting("客服郵箱", "support@monlingo.com"));
            contactStack.Children.Add(CreateTextDisplaySetting("服務時間", "週一至週五 9:00-18:00"));
            contactStack.Children.Add(CreateTextDisplaySetting("響應時間", "24小時內"));
            
            panel.Children.Add(contactGroup);
            
            return panel;
        }

        private FrameworkElement CreateEnterpriseSettingsContent()
        {
            var panel = new StackPanel();
            
            var enterpriseGroup = CreateSettingGroup("企業功能", new StackPanel());
            var enterpriseStack = (StackPanel)((Border)enterpriseGroup).Child;
            
            enterpriseStack.Children.Add(CreateTextDisplaySetting("版本", "社區版"));
            enterpriseStack.Children.Add(CreateTextDisplaySetting("授權用戶數", "不限制"));
            enterpriseStack.Children.Add(CreateTextDisplaySetting("技術支援", "社區支援"));
            
            panel.Children.Add(enterpriseGroup);
            
            return panel;
        }

        private FrameworkElement CreateFeedbackSettingsContent()
        {
            var panel = new StackPanel();
            
            var feedbackGroup = CreateSettingGroup("意見反饋", new StackPanel());
            var feedbackStack = (StackPanel)((Border)feedbackGroup).Child;
            
            var textBox = new TextBox
            {
                Height = 120,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 10, 0, 0),
                Text = "請描述您的問題或建議..."
            };
            
            feedbackStack.Children.Add(new TextBlock { Text = "反饋內容", Margin = new Thickness(0, 10, 0, 5) });
            feedbackStack.Children.Add(textBox);
            
            var submitButton = new Button
            {
                Content = "提交反饋",
                Background = new SolidColorBrush(Color.FromRgb(79, 70, 229)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(20, 10, 20, 10),
                Margin = new Thickness(0, 15, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            
            feedbackStack.Children.Add(submitButton);
            
            panel.Children.Add(feedbackGroup);
            
            return panel;
        }

        #endregion

        #region UI Helper 方法

        /// <summary>
        /// 創建macOS風格設置卡片
        /// </summary>
        private Border CreateMacOSSettingCard(string title, string subtitle)
        {
            var border = new Border
            {
                Style = (Style)FindResource("SettingCardStyle")
            };

            var mainPanel = new StackPanel();
            
            var titleBlock = new TextBlock
            {
                Text = title,
                Style = (Style)FindResource("SettingTitleStyle")
            };
            
            var subtitleBlock = new TextBlock
            {
                Text = subtitle,
                Style = (Style)FindResource("SettingSubtitleStyle")
            };
            
            mainPanel.Children.Add(titleBlock);
            mainPanel.Children.Add(subtitleBlock);
            
            var contentStack = new StackPanel();
            mainPanel.Children.Add(contentStack);
            
            border.Child = mainPanel;
            return border;
        }

        /// <summary>
        /// 創建macOS風格下拉框設置項
        /// </summary>
        private FrameworkElement CreateMacOSComboBoxSetting(string label, string description, string[] options)
        {
            var mainPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };
            
            // 標籤
            var labelBlock = new TextBlock 
            { 
                Text = label,
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush(Color.FromRgb(31, 41, 55)),
                Margin = new Thickness(0, 0, 0, 4)
            };
            mainPanel.Children.Add(labelBlock);
            
            // 描述
            if (!string.IsNullOrEmpty(description))
            {
                var descBlock = new TextBlock 
                { 
                    Text = description,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                    Margin = new Thickness(0, 0, 0, 8)
                };
                mainPanel.Children.Add(descBlock);
            }
            
            // 下拉框
            var comboBox = new ComboBox
            {
                Style = (Style)FindResource("ModernComboBoxStyle"),
                Width = 280,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            
            foreach (var option in options)
            {
                comboBox.Items.Add(option);
            }
            
            if (comboBox.Items.Count > 0)
                comboBox.SelectedIndex = 0;
                
            mainPanel.Children.Add(comboBox);
            
            return mainPanel;
        }

        /// <summary>
        /// 創建macOS風格切換開關設置項
        /// </summary>
        private FrameworkElement CreateMacOSToggleSetting(string label, string description, bool defaultValue = false)
        {
            var mainPanel = new Grid();
            mainPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            mainPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            
            var textPanel = new StackPanel();
            Grid.SetColumn(textPanel, 0);
            
            var labelBlock = new TextBlock 
            { 
                Text = label,
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush(Color.FromRgb(31, 41, 55))
            };
            textPanel.Children.Add(labelBlock);
            
            if (!string.IsNullOrEmpty(description))
            {
                var descBlock = new TextBlock 
                { 
                    Text = description,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                    Margin = new Thickness(0, 2, 0, 0)
                };
                textPanel.Children.Add(descBlock);
            }
            
            var toggle = new ToggleButton
            {
                Style = (Style)FindResource("ModernToggleStyle"),
                IsChecked = defaultValue,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(toggle, 1);
            
            mainPanel.Children.Add(textPanel);
            mainPanel.Children.Add(toggle);
            
            var wrapper = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };
            wrapper.Children.Add(mainPanel);
            
            return wrapper;
        }

        private Border CreateSettingGroup(string title, Panel content)
        {
            var border = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(10),
                Margin = new Thickness(0, 0, 0, 20),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Color.FromRgb(229, 231, 235),
                    Direction = 270,
                    ShadowDepth = 2,
                    BlurRadius = 8,
                    Opacity = 0.3
                }
            };

            var mainPanel = new StackPanel { Margin = new Thickness(25) };
            
            var titleBlock = new TextBlock
            {
                Text = title,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(55, 65, 81)),
                Margin = new Thickness(0, 0, 0, 15)
            };
            
            mainPanel.Children.Add(titleBlock);
            mainPanel.Children.Add(content);
            
            border.Child = mainPanel;
            return border;
        }

        private FrameworkElement CreateComboBoxSetting(string label, string[] options)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 0) };
            
            panel.Children.Add(new TextBlock 
            { 
                Text = label, 
                VerticalAlignment = VerticalAlignment.Center, 
                Width = 150 
            });
            
            var comboBox = new ComboBox
            {
                Width = 200,
                Height = 35,
                FontSize = 13,
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(209, 213, 219)),
                BorderThickness = new Thickness(1)
            };
            
            foreach (var option in options)
            {
                comboBox.Items.Add(new ComboBoxItem { Content = option });
            }
            
            if (comboBox.Items.Count > 0)
                comboBox.SelectedIndex = 0;
            
            panel.Children.Add(comboBox);
            
            return panel;
        }

        private FrameworkElement CreateToggleSetting(string label, bool defaultValue)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 0) };
            
            panel.Children.Add(new TextBlock 
            { 
                Text = label, 
                VerticalAlignment = VerticalAlignment.Center, 
                Margin = new Thickness(0, 0, 20, 0) 
            });
            
            var toggle = new CheckBox
            {
                IsChecked = defaultValue,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            panel.Children.Add(toggle);
            
            return panel;
        }

        private FrameworkElement CreateSliderSetting(string label, double min, double max, double defaultValue)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 15, 0, 0) };
            
            panel.Children.Add(new TextBlock 
            { 
                Text = label, 
                VerticalAlignment = VerticalAlignment.Center, 
                Width = 150 
            });
            
            var slider = new Slider
            {
                Width = 200,
                Minimum = min,
                Maximum = max,
                Value = defaultValue,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            var valueText = new TextBlock 
            { 
                Text = $"{defaultValue:F0}", 
                VerticalAlignment = VerticalAlignment.Center, 
                Margin = new Thickness(10, 0, 0, 0) 
            };
            
            slider.ValueChanged += (s, e) => valueText.Text = $"{e.NewValue:F0}";
            
            panel.Children.Add(slider);
            panel.Children.Add(valueText);
            
            return panel;
        }

        private FrameworkElement CreateTextDisplaySetting(string label, string value)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 0) };
            
            panel.Children.Add(new TextBlock 
            { 
                Text = label + "：", 
                VerticalAlignment = VerticalAlignment.Center, 
                Width = 150 
            });
            
            panel.Children.Add(new TextBlock 
            { 
                Text = value, 
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.SemiBold
            });
            
            return panel;
        }

        #endregion

        #region 事件處理

        /// <summary>
        /// 關閉按鈕點擊事件
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("確定要重置所有設定為預設值嗎？", "重置設定", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                MessageBox.Show("設定已重置為預設值", "重置完成", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #endregion

        #region 舊版事件保留（向後兼容）

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 儲存設定並關閉視窗
                ApplySettings();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"儲存設定時發生錯誤: {ex.Message}", "錯誤", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"關閉視窗時發生錯誤: {ex.Message}", "錯誤", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            ApplySettings();
        }

        private void ApplySettings()
        {
            // 這裡實作設定儲存邏輯
            MessageBox.Show("設定已套用", "套用設定", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region 新增的導航內容方法

        /// <summary>
        /// 載入翻譯語言設置
        /// </summary>
        private async void LoadTranslationLanguageSettings()
        {
            CreateSettingsContent("翻譯語言", "🌏", await CreateTranslationLanguageContentAsync());
        }

        /// <summary>
        /// 對外公開：直接切換到「翻譯語言」頁
        /// </summary>
        public void OpenTranslationLanguagePage()
        {
            try
            {
                LoadTranslationLanguageSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打開翻譯語言頁失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 載入快捷鍵設置
        /// </summary>
        private void LoadShortcutSettings()
        {
            CreateSettingsContent("快捷鍵", "⌨️", CreateShortcutSettingsContent());
        }

        /// <summary>
        /// 載入聯繫官方
        /// </summary>
        private void LoadContactOfficial()
        {
            CreateSettingsContent("聯繫官方", "📞", CreateContactOfficialContent());
        }

        /// <summary>
        /// 載入幫助中心
        /// </summary>
        private void LoadHelpCenter()
        {
            CreateSettingsContent("幫助中心", "❓", CreateHelpCenterContent());
        }

        /// <summary>
        /// 載入關於我們
        /// </summary>
        private void LoadAboutUs()
        {
            CreateSettingsContent("關於我們", "ℹ️", CreateAboutUsContent());
        }

        /// <summary>
        /// 創建翻譯語言內容（雙欄清單樣式：輸入語言 | 輸出語言）- 異步版本
        /// </summary>
        private async Task<FrameworkElement> CreateTranslationLanguageContentAsync()
        {
            var content = CreateTranslationLanguageContent();
            
            // 載入保存的語言設定
            if (_languageConfigService != null)
            {
                try
                {
                    var config = await _languageConfigService.GetLanguageConfigAsync();
                    
                    // 設定源語言選擇
                    var sourceDisplayName = MonLingo.Core.Models.LanguageSettings.GetDisplayNameByLanguageCode(config.SourceLanguage, true);
                    if (_sourceLangListBox != null && !string.IsNullOrEmpty(sourceDisplayName))
                    {
                        _sourceLangListBox.SelectedItem = sourceDisplayName;
                    }
                    
                    // 設定目標語言選擇
                    var targetDisplayName = MonLingo.Core.Models.LanguageSettings.GetDisplayNameByLanguageCode(config.TargetLanguage, false);
                    if (_targetLangListBox != null && !string.IsNullOrEmpty(targetDisplayName))
                    {
                        _targetLangListBox.SelectedItem = targetDisplayName;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"載入語言設定失敗: {ex.Message}");
                }
            }
            
            return content;
        }

        /// <summary>
        /// 創建翻譯語言內容（雙欄清單樣式：輸入語言 | 輸出語言）
        /// </summary>
        private FrameworkElement CreateTranslationLanguageContent()
        {
            // 外層容器
            var root = new StackPanel();

            // 標題由 CreateSettingsContent 統一生成，此處不再單獨建立

            // 主要區塊：左右雙欄清單（不再是移動項目的 Transfer List，而是各自獨立選擇）
            var grid = new Grid { Margin = new Thickness(0, 12, 0, 16) };
            // 左 | 中(箭頭) | 右
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 標題列
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(360) }); // 清單列

            // 左側：輸入語言（來源語言）
            var leftHeader = new TextBlock
            {
                Text = "輸入語言",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(55, 65, 81)),
                Margin = new Thickness(0, 0, 0, 8)
            };
            Grid.SetColumn(leftHeader, 0);
            Grid.SetRow(leftHeader, 0);
            grid.Children.Add(leftHeader);

            var leftList = new ListBox
            {
                SelectionMode = SelectionMode.Single,
                BorderBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 8, 0)
            };
            
            // 為左側 ListBox 創建 ItemTemplate 來顯示 ✓
            leftList.ItemTemplate = CreateCheckmarkItemTemplate();
            
            // 左側清單：所有可選輸入語言（包含「自動檢測」）
            foreach (var kv in MonLingo.Core.Models.LanguageSettings.SourceLanguages)
            {
                leftList.Items.Add(kv.Value);
            }
            // 預設選擇：自動檢測
            leftList.SelectedIndex = 0;
            // 保存引用並綁定事件
            _sourceLangListBox = leftList;
            leftList.SelectionChanged += (s, e) => RaiseLanguageSelectionChanged();
            Grid.SetColumn(leftList, 0);
            Grid.SetRow(leftList, 1);
            grid.Children.Add(leftList);

            // 中間箭頭
            var middleArrow = new TextBlock
            {
                Text = "->",
                FontSize = 20,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(75, 85, 99)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 8, 0)
            };
            Grid.SetColumn(middleArrow, 1);
            Grid.SetRow(middleArrow, 0);
            Grid.SetRowSpan(middleArrow, 2);
            grid.Children.Add(middleArrow);

            // 右側：輸出語言（翻譯目標語言）
            var rightHeader = new TextBlock
            {
                Text = "輸出語言",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(55, 65, 81)),
                Margin = new Thickness(8, 0, 0, 8)
            };
            Grid.SetColumn(rightHeader, 2);
            Grid.SetRow(rightHeader, 0);
            grid.Children.Add(rightHeader);

            var rightList = new ListBox
            {
                SelectionMode = SelectionMode.Single,
                BorderBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(8, 0, 0, 0)
            };
            
            // 為右側 ListBox 創建 ItemTemplate 來顯示 ✓
            rightList.ItemTemplate = CreateCheckmarkItemTemplate();
            
            // 右側清單：所有可選輸出語言（更多語種）
            foreach (var kv in MonLingo.Core.Models.LanguageSettings.TargetLanguages)
            {
                rightList.Items.Add(kv.Value);
            }
            // 預設選擇：中文(繁體)
            var defaultTarget = MonLingo.Core.Models.LanguageSettings.GetDisplayNameByLanguageCode("zh-tw", false);
            rightList.SelectedItem = defaultTarget;
            // 保存引用並綁定事件
            _targetLangListBox = rightList;
            rightList.SelectionChanged += (s, e) => RaiseLanguageSelectionChanged();

            Grid.SetColumn(rightList, 2);
            Grid.SetRow(rightList, 1);
            grid.Children.Add(rightList);

            root.Children.Add(grid);

            return root;
        }

        /// <summary>
        /// 觸發對外語言變更事件
        /// </summary>
        private async void RaiseLanguageSelectionChanged()
        {
            try
            {
                var sourceDisplayName = _sourceLangListBox?.SelectedItem?.ToString();
                var targetDisplayName = _targetLangListBox?.SelectedItem?.ToString();
                
                if (!string.IsNullOrWhiteSpace(sourceDisplayName) && !string.IsNullOrWhiteSpace(targetDisplayName))
                {
                    // 將顯示名稱轉換為語言代碼
                    var sourceCode = MonLingo.Core.Models.LanguageSettings.GetLanguageCodeByDisplayName(sourceDisplayName, true);
                    var targetCode = MonLingo.Core.Models.LanguageSettings.GetLanguageCodeByDisplayName(targetDisplayName, false);
                    
                    // 保存到語言配置服務
                    if (_languageConfigService != null)
                    {
                        await _languageConfigService.SetSourceLanguageAsync(sourceCode);
                        await _languageConfigService.SetTargetLanguageAsync(targetCode);
                    }
                    
                    // 觸發對外事件
                    LanguageSelectionChanged?.Invoke(sourceDisplayName, targetDisplayName);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"語言選擇變更處理失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 創建帶有選中標記的 ItemTemplate
        /// </summary>
        private static DataTemplate CreateCheckmarkItemTemplate()
        {
            var template = new DataTemplate();
            
            // 創建 StackPanel 來水平排列文字和勾勾
            var stackPanelFactory = new FrameworkElementFactory(typeof(StackPanel));
            stackPanelFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            
            // 語言文字
            var textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
            textBlockFactory.SetBinding(TextBlock.TextProperty, new Binding());
            textBlockFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            stackPanelFactory.AppendChild(textBlockFactory);
            
            // 勾勾標記
            var checkMarkFactory = new FrameworkElementFactory(typeof(TextBlock));
            checkMarkFactory.SetValue(TextBlock.TextProperty, "  ✓");
            checkMarkFactory.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(Color.FromRgb(16, 185, 129)));
            checkMarkFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
            checkMarkFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            
            // 使用 Binding 直接綁定到 IsSelected 屬性，使用 Converter 來控制顯示
            var binding = new Binding("IsSelected")
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(ListBoxItem), 1),
                Converter = new BooleanToVisibilityConverter()
            };
            checkMarkFactory.SetBinding(UIElement.VisibilityProperty, binding);
            
            stackPanelFactory.AppendChild(checkMarkFactory);
            template.VisualTree = stackPanelFactory;
            
            return template;
        }

        /// <summary>
        /// 更新 ListBox 項目，為選中項添加 ✓ 標記
        /// </summary>
        private void UpdateListItemsWithCheckmark(ListBox listBox)
        {
            if (listBox == null) return;

            // 更新所有項目的顯示文字
            for (int i = 0; i < listBox.Items.Count; i++)
            {
                var item = listBox.Items[i].ToString();
                var isSelected = listBox.SelectedIndex == i;
                
                // 移除現有的 ✓ 標記
                var cleanItem = item.Replace("  ✓", "").Trim();
                
                // 為選中項添加 ✓ 標記
                var displayText = isSelected ? cleanItem + "  ✓" : cleanItem;
                
                listBox.Items[i] = displayText;
            }
        }

        /// <summary>
        /// 創建快捷鍵內容
        /// </summary>
        private FrameworkElement CreateShortcutSettingsContent()
        {
            var panel = new StackPanel();

            panel.Children.Add(CreateSubtitleText("全局快捷鍵"));
            panel.Children.Add(CreateTextSetting("快速翻譯", "Ctrl + Shift + T"));
            panel.Children.Add(CreateTextSetting("截圖翻譯", "Ctrl + Shift + S"));
            panel.Children.Add(CreateTextSetting("顯示/隱藏工具條", "Ctrl + Shift + Q"));
            panel.Children.Add(CreateTextSetting("暫停/恢復翻譯", "Ctrl + Shift + P"));

            panel.Children.Add(CreateSubtitleText("工具條快捷鍵"));
            panel.Children.Add(CreateToggleSetting("啟用工具條快捷鍵", true));
            panel.Children.Add(CreateTextSetting("上一頁翻譯", "Ctrl + ←"));
            panel.Children.Add(CreateTextSetting("下一頁翻譯", "Ctrl + →"));

            return panel;
        }

        /// <summary>
        /// 創建聯繫官方內容
        /// </summary>
        private FrameworkElement CreateContactOfficialContent()
        {
            var panel = new StackPanel();

            panel.Children.Add(CreateSubtitleText("聯繫方式"));
            
            var infoPanel = new StackPanel() { Margin = new Thickness(20, 0, 20, 0) };
            
            infoPanel.Children.Add(new TextBlock() 
            { 
                Text = "📧 客服郵箱：support@monlingo.com", 
                FontSize = 14, 
                Margin = new Thickness(0, 10, 0, 10),
                Foreground = new SolidColorBrush(Color.FromRgb(75, 85, 99))
            });
            
            infoPanel.Children.Add(new TextBlock() 
            { 
                Text = "🌐 官方網站：https://www.monlingo.com", 
                FontSize = 14, 
                Margin = new Thickness(0, 10, 0, 10),
                Foreground = new SolidColorBrush(Color.FromRgb(75, 85, 99))
            });
            
            infoPanel.Children.Add(new TextBlock() 
            { 
                Text = "💬 在線客服：週一至週五 9:00-18:00", 
                FontSize = 14, 
                Margin = new Thickness(0, 10, 0, 10),
                Foreground = new SolidColorBrush(Color.FromRgb(75, 85, 99))
            });

            var contactButton = new Button()
            {
                Content = "開啟在線客服",
                Background = new SolidColorBrush(Color.FromRgb(79, 70, 229)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(20, 10, 20, 10),
                Margin = new Thickness(0, 20, 0, 0),
                FontSize = 14
            };

            infoPanel.Children.Add(contactButton);
            panel.Children.Add(infoPanel);

            return panel;
        }

        /// <summary>
        /// 創建幫助中心內容
        /// </summary>
        private FrameworkElement CreateHelpCenterContent()
        {
            var panel = new StackPanel();

            panel.Children.Add(CreateSubtitleText("常見問題"));
            
            var helpPanel = new StackPanel() { Margin = new Thickness(20, 0, 20, 0) };
            
            helpPanel.Children.Add(CreateHelpItem("❓ 如何使用截圖翻譯功能？", 
                "使用快捷鍵 Ctrl+Shift+S 或點擊工具條的截圖按鈕，然後框選需要翻譯的區域。"));
            
            helpPanel.Children.Add(CreateHelpItem("❓ 翻譯結果不準確怎麼辦？", 
                "您可以在翻譯設置中更換翻譯引擎，或者檢查源語言設置是否正確。"));
            
            helpPanel.Children.Add(CreateHelpItem("❓ 如何設置開機自啟動？", 
                "在全局設置中找到「開機自啟動」選項並開啟即可。"));
            
            helpPanel.Children.Add(CreateHelpItem("❓ 如何升級會員？", 
                "點擊左側的「會員」選項，選擇適合的會員套餐進行升級。"));

            panel.Children.Add(helpPanel);

            panel.Children.Add(CreateSubtitleText("使用指南"));
            var guideButton = new Button()
            {
                Content = "查看完整使用指南",
                Background = new SolidColorBrush(Color.FromRgb(79, 70, 229)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(20, 10, 20, 10),
                Margin = new Thickness(20, 10, 20, 0),
                FontSize = 14
            };
            panel.Children.Add(guideButton);

            return panel;
        }

        /// <summary>
        /// 創建關於我們內容
        /// </summary>
        private FrameworkElement CreateAboutUsContent()
        {
            var panel = new StackPanel();

            panel.Children.Add(CreateSubtitleText("軟體資訊"));
            
            var aboutPanel = new StackPanel() { Margin = new Thickness(20, 0, 20, 0) };
            
            aboutPanel.Children.Add(new TextBlock() 
            { 
                Text = "MonLingo 翻譯工具", 
                FontSize = 18, 
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 10, 0, 10),
                Foreground = new SolidColorBrush(Color.FromRgb(79, 70, 229))
            });
            
            aboutPanel.Children.Add(new TextBlock() 
            { 
                Text = "版本：v2.0.1", 
                FontSize = 14, 
                Margin = new Thickness(0, 5, 0, 5),
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128))
            });
            
            aboutPanel.Children.Add(new TextBlock() 
            { 
                Text = "發布日期：2025年8月24日", 
                FontSize = 14, 
                Margin = new Thickness(0, 5, 0, 5),
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128))
            });

            aboutPanel.Children.Add(CreateSubtitleText("功能特色"));
            aboutPanel.Children.Add(new TextBlock() 
            { 
                Text = "• 支援多種翻譯引擎\n• 即時截圖翻譯\n• 智能語言檢測\n• 多語言支援\n• 輕量級設計", 
                FontSize = 14, 
                Margin = new Thickness(0, 10, 0, 10),
                Foreground = new SolidColorBrush(Color.FromRgb(75, 85, 99)),
                LineHeight = 22
            });

            panel.Children.Add(aboutPanel);

            return panel;
        }

        /// <summary>
        /// 創建幫助項目
        /// </summary>
        private FrameworkElement CreateHelpItem(string question, string answer)
        {
            var panel = new StackPanel() { Margin = new Thickness(0, 15, 0, 0) };
            
            panel.Children.Add(new TextBlock() 
            { 
                Text = question, 
                FontSize = 14, 
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(75, 85, 99))
            });
            
            panel.Children.Add(new TextBlock() 
            { 
                Text = answer, 
                FontSize = 13, 
                Margin = new Thickness(0, 5, 0, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 20
            });

            return panel;
        }

        /// <summary>
        /// 創建文字設置項
        /// </summary>
        private FrameworkElement CreateTextSetting(string label, string value)
        {
            var panel = new StackPanel() 
            { 
                Orientation = Orientation.Horizontal, 
                Margin = new Thickness(20, 10, 20, 0),
                Height = 32
            };
            
            panel.Children.Add(new TextBlock() 
            { 
                Text = label, 
                FontSize = 14, 
                VerticalAlignment = VerticalAlignment.Center,
                Width = 150,
                Foreground = new SolidColorBrush(Color.FromRgb(75, 85, 99))
            });
            
            var textBox = new TextBox()
            {
                Text = value,
                Width = 200,
                Height = 30,
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(8, 0, 8, 0),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(209, 213, 219)),
                FontSize = 13
            };
            
            panel.Children.Add(textBox);

            return panel;
        }

        /// <summary>
        /// 創建子標題文字
        /// </summary>
        private FrameworkElement CreateSubtitleText(string title)
        {
            return new TextBlock()
            {
                Text = title,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(75, 85, 99)),
                Margin = new Thickness(20, 20, 20, 10)
            };
        }

        #endregion
    }
}
