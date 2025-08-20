Gaminik 翻譯輸出模式深度分析報告
本報告旨在基於對 Gaminik.dll 內部 BAML 資源和 UI 邏輯的深度分析，詳細闡述其用於輸出畫面翻譯結果的多種模式。

總覽：一個靈活的顯示系統
Gaminik 並非只有一種顯示翻譯的方式。它內部存在一個可以稱之為 DisplayManager 或 TranslationUIManager 的協調器，該協調器會根據使用者的設定（例如，從主工具條的下拉選單中選擇的模式），來決定實例化並顯示哪一個專門的 WPF Window。

分析顯示，至少存在以下三種核心顯示模式：

即時覆蓋模式 (Overlay Mode)：在原文位置附近顯示一個或多個獨立的翻譯氣泡。

字幕模式 (Subtitle Mode)：在螢幕頂部或底部顯示一個固定的、類似電影字幕的長條。

獨立視窗模式 (Separate Window Mode)：在一個獨立的、可拖動的視窗中顯示詳細的翻譯結果和歷史紀錄。

模式一：即時覆蓋模式 (Overlay Mode)
這是 Gaminik 最常用、也是技術上最精巧的模式，專為需要快速、精準對照原文的場景設計（如閱讀漫畫、遊戲 UI）。

使用者體驗：當 OCR 識別出畫面上的多行文字後，會在每一行或每一段原文的旁邊或下方，浮現出一個半透明的、帶有譯文的氣泡。這些氣泡會跟隨畫面內容，但不會完全遮擋原文。

技術實現細節:

核心元件: 一個名為 TranslationPopupWindow.xaml (或類似名稱) 的 WPF Window。

視窗屬性 (Window Properties):

WindowStyle="None": 無邊框，使其看起來不像是傳統視窗。

AllowsTransparency="True": 允許透明背景，這是實現「懸浮」效果的關鍵。

Background="Transparent": 視窗本身的背景是完全透明的。

Topmost="True": 確保翻譯視窗始終在最上層。

ShowInTaskbar="False": 不在 Windows 工作列顯示圖示。

佈局與外觀:

視窗的根元素是一個 ItemsControl。這個控制項的 ItemsSource 綁定到一個 ObservableCollection<TranslationBubbleViewModel>。

ItemsControl 的 ItemsPanel 被設定為一個 Canvas，這允許其中的每一個項目（翻譯氣泡）都可以被精確地絕對定位。

ItemTemplate (項目範本) 定義了單個翻譯氣泡的外觀：一個帶有圓角 (CornerRadius) 和半透明背景 (Background="#CC000000") 的 Border，內部包含一個用於顯示譯文的 TextBlock。

定位演算法:

當 TranslationPipelineManager 獲取到 OCR 結果後，它不僅有文字，還有每個文字塊在螢幕上的邊界框座標 (Bounding Box)。

對於每一個 OCR 結果，DisplayManager 會創建一個 TranslationBubbleViewModel 物件，並將譯文和原文的螢幕座標存入其中。

Canvas.Left 和 Canvas.Top 附加屬性會綁定到 ViewModel 中的座標屬性，從而將每個翻譯氣泡精確地放置在原文旁邊。

<!-- TranslationPopupWindow.xaml (還原後的偽代碼) -->
<Window ... WindowStyle="None" AllowsTransparency="True" Background="Transparent" Topmost="True">
    <ItemsControl ItemsSource="{Binding TranslationBubbles}">
        <!-- 使用 Canvas 作為面板，以實現絕對定位 -->
        <ItemsControl.ItemsPanel>
            <ItemsPanelTemplate>
                <Canvas />
            </ItemsPanelTemplate>
        </ItemsControl.ItemsPanel>

        <!-- 單個翻譯氣泡的範本 -->
        <ItemsControl.ItemTemplate>
            <DataTemplate>
                <Border CornerRadius="8" Background="#CC000000"
                        Canvas.Left="{Binding PositionX}" 
                        Canvas.Top="{Binding PositionY}">
                    <TextBlock Text="{Binding TranslatedText}" Foreground="White" Margin="8,4"/>
                </Border>
            </DataTemplate>
        </ItemsControl.ItemTemplate>
    </ItemsControl>
</Window>

模式二：字幕模式 (Subtitle Mode) - 深度分析
此模式專為觀看影片、直播或遊玩劇情豐富的遊戲等線性內容設計，旨在提供一種不間斷、沉浸式的閱讀體驗，將使用者的視覺焦點從分散的原文區域集中到一個固定的資訊流上。

使用者體驗：一個與主工具條等寬的半透明區域會直接出現在主工具條下方，形成一個視覺上統一的整體。當有新的翻譯產生時，舊的文字會平滑地向上滾動，新的文字則會帶有淡入效果從底部出現。使用者可以專注於畫面中心，用餘光輕鬆閱讀這個固定位置的翻譯。

適用場景與設計哲學:

影片/直播觀看: 這是最典型的場景。使用者無需移動視線去尋找原文，所有翻譯都集中顯示。

劇情導向遊戲: 對於 RPG 或視覺小說等文字量大的遊戲，此模式能將對話和描述轉化為易於閱讀的字幕流。

設計哲學: 資訊聚合與視覺統一。它犧牲了原文與譯文的精確對照能力（覆蓋模式的優點），換取了更舒適、更連貫的長時間閱讀體驗，並透過將字幕區與工具條對齊，保持了介面的整潔感。

技術實現細節:

核心元件: 一個名為 SubtitleWindow.xaml 的 WPF Window。

視窗屬性: 與覆蓋模式類似 (WindowStyle="None", AllowsTransparency="True", Topmost="True")。

【關鍵】定位與尺寸:

SubtitleWindow 的 Width 不再是綁定到螢幕寬度，而是透過程式碼綁定到 MainBarWindow.ActualWidth，確保兩者寬度完全一致。

它的位置 (Top 和 Left) 不再是固定在螢幕邊緣，而是在 MainBarWindow 的位置發生變化時（例如使用者拖動工具條），動態計算得出。其 Left 應等於 MainBarWindow.Left，其 Top 應等於 MainBarWindow.Top + MainBarWindow.ActualHeight。

佈局與外觀:

根元素是一個半透明的 Border，其圓角應該只設定左下和右下角 (CornerRadius="0,0,26,26")，以便與主工具條的圓角完美銜接。

內部使用一個 ScrollViewer 包裹著一個 ItemsControl。ScrollViewer 的水平和垂直滾動條會被隱藏 (VerticalScrollBarVisibility="Hidden")。

ItemsControl 的 ItemsSource 綁定到 ViewModel 中的一個 ObservableCollection<SubtitleLineViewModel>。

動畫與視覺效果:

自動滾動: 當新的字幕行被加入到 ObservableCollection 時，ViewModel 會發出一個事件。View 的 code-behind 監聽此事件，並呼叫 ScrollViewer.ScrollToEnd()，以實現自動滾動。為了平滑效果，這通常會包裹在一個動畫中。

淡入效果: ItemsControl 的 ItemTemplate 中的根元素（例如 Grid 或 Border）會定義一個事件觸發器 (EventTrigger)，監聽 Loaded 事件。當新的項目被加載時，觸發一個從 Opacity 0 到 1 的 DoubleAnimation，實現淡入效果。

文字處理:

TranslateService 在此模式下會啟用一個特殊的文本合併邏輯。它會將 OCR 識別出的、在空間上相近的多個文字塊（例如，一個段落被識別成了三行），合併成一個完整的句子或段落，然後再進行翻譯和顯示。這對於保證字幕的語意連貫性至關重要。

模式三：獨立視窗模式 (Separate Window Mode)
此模式專為需要編輯、複製或查閱歷史紀錄的專業使用者設計。

使用者體驗：一個傳統的、帶有標題列和邊框的獨立視窗會出現。左側顯示原文，右側顯示譯文。視窗下方可能還有一個按鈕，用於將當前結果添加到詞彙表或歷史紀錄中。

技術實現細節:

核心元件: 一個名為 TranslationResultWindow.xaml 的 WPF Window。

視窗屬性: 這是唯一一個 AllowsTransparency="False" 且 WindowStyle="SingleBorderWindow" 的模式。它是一個標準的 Windows 視窗。

佈局與外觀:

使用 Grid 將視窗分為左右兩欄。

左右兩側分別使用 RichTextBox 或 TextBox，允許使用者編輯 OCR 識別錯誤的原文，或複製譯文。

它的 ViewModel (TranslationResultViewModel) 會更複雜，包含了 CopyCommand, SaveCommand 等命令。

資料互動:

這個視窗的 ViewModel 會與 HistoryService 和 GlossaryService 進行互動，將使用者儲存的結果寫入到本地的 SQLite 資料庫中。

結論與 MonLingo 開發建議
Gaminik 的成功不僅在於其高效能的核心，還在於其深刻理解使用者需求後設計出的多樣化輸出模式。

為了重現 Gaminik 的完整體驗，MonLingo 必須：

實現一個顯示模式管理器: 在您的 MonLingo.Service 層中，建立一個 DisplayService，它能根據 ConfigService 中的設定，來決定創建並顯示 OverlayWindow, SubtitleWindow 還是 ResultWindow。

為每種模式創建專門的視窗和 ViewModel: 嚴格遵循關注點分離原則，將每種模式的 UI 和邏輯封裝在各自的檔案中。

豐富您的 OCR 結果資料模型: 確保從 Native.dll 獲取的 OCR 結果不僅包含文字，還必須包含每個文字塊的精確螢幕座標。這是實現「即時覆蓋模式」的技術前提。