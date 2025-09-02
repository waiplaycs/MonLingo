Gaminik 覆蓋模式技術實現深度解析

1. 核心設計哲學：原地渲染與最小化干擾
覆蓋模式的設計核心是將譯文直接渲染在原文的位置上，創造一種無縫的「本地化」體驗。使用者無需移動視線去尋找翻譯結果，譯文彷彿本來就在那裡。為了達成此目標，其實現必須遵循兩個原則：

1.1 像素級精準定位: 譯文覆蓋層的位置和大小，必須與原始文字在螢幕上的位置精確對應。

1.2. 上下文保留: 覆蓋層必須是半透明的，讓使用者依然能感知到底層的原始圖像上下文，同時又要確保譯文清晰可讀。

2. 技術架構：多視窗協作系統
Gaminik 並非使用單一視窗來實現此功能。它採用了一個由多個專門的 WPF Window 物件組成的協作系統，由 Gaminik.dll 中的一個高階協調器 (DisplayManager 或類似角色的類別) 來統一管理。

系統組成:

2.1 主擷取覆蓋層 (CaptureRegionWindow):

2.1.1 職責: 提供使用者介面以選取翻譯區域。

2.1.2 特性: 全螢幕、極低透明度 (Background="#01000000")，用於接收滑鼠事件。當使用者完成選取後，此視窗會隱藏或關閉。

2.2 翻譯顯示容器 (TranslationOverlayHostWindow):

2.2.1 職責: 作為所有獨立翻譯文字塊的父容器。

2.2.2 特性:

2.2.2.1一個與使用者選取區域完全相同大小和位置的無邊框透明視窗 (WindowStyle="None", AllowsTransparency="True", Background="Transparent")。

2.2.2.2它本身不顯示任何內容，僅作為一個畫布 (Canvas) 來承載和定位真正的譯文視窗。

2.3 獨立譯文視窗 (SingleTranslationWindow):

2.3.1 職責: 顯示單一行的翻譯文字。

2.3.2 特性:

2.3.2.1 每個 SingleTranslationWindow 都對應 OCR 結果中的一個文字行 (OcrLine)。

2.3.2.2 它的 Width 和 Height 由該行文字的邊界框 (Bounding Box) 決定。

2.3.2.3 它的位置是相對於其父容器 TranslationOverlayHostWindow 來設定的。

2.3.2.4 擁有圓角、半透明的背景和文字描邊效果，以確保在任何背景下的可讀性。

2.3.3 關鍵點: 系統會根據 OCR 結果的行數，動態創建數個甚至數十個這樣的獨立小視窗。

為何使用多視窗？
這種看似複雜的設計，是為了極致的性能和靈活性。WPF 在處理大量分散的、獨立更新的小型 Window 物件時，其渲染性能遠高於在單一巨大 Canvas 上不斷重繪數十個 TextBlock。每個小視窗都有自己的渲染執行緒，可以獨立進行淡入淡出動畫，而不會影響其他視窗。

3. 工作流程與數據流
以下是從 OCR 完成到譯文顯示在螢幕上的完整流程：

[TranslationPipelineManager]
   |
   | 1. 執行 OCR，獲取 OcrResult (包含多個 OcrLine)
   |
   V
[DisplayManager]
   |
   | 2. 獲取使用者選取的螢幕區域 Rect (selectionRect)
   |
   | 3. 創建並顯示 TranslationOverlayHostWindow
   |    - HostWindow.Left = selectionRect.Left
   |    - HostWindow.Top = selectionRect.Top
   |    - HostWindow.Width = selectionRect.Width
   |    - HostWindow.Height = selectionRect.Height
   |
   | 4. (For each OcrLine in OcrResult.Lines):
   |    |
   |    | a. 獲取該行的譯文 (translatedText)
   |    |
   |    | b. 獲取該行的邊界框 (lineBoundingBox)
   |    |
   |    | c. 創建一個新的 SingleTranslationWindow 實例
   |    |
   |    | d. 設定其內容與位置
   |    |    - TranslationWindow.ViewModel.Text = translatedText
   |    |    - TranslationWindow.Owner = HostWindow // 設置父視窗
   |    |    - TranslationWindow.Width = lineBoundingBox.Width
   |    |    - TranslationWindow.Height = lineBoundingBox.Height
   |    |    - // 【關鍵】計算相對於父容器的位置
   |    |    - TranslationWindow.Left = HostWindow.Left + lineBoundingBox.Left
   |    |    - TranslationWindow.Top = HostWindow.Top + lineBoundingBox.Top
   |    |
   |    | e. 顯示 TranslationWindow 並觸發淡入動畫
   |
   V
[螢幕顯示]

4. UI 實現細節 (WPF XAML)
TranslationOverlayHostWindow.xaml
<Window x:Class="MonLingo.Interface.Windows.TranslationOverlayHostWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Translation Host"
        WindowStyle="None"
        AllowsTransparency="True"
        Background="Transparent"
        ShowInTaskbar="False"
        Topmost="True">
    <!-- 這個視窗是完全透明的，只作為一個定位的錨點 -->
    <!-- 無需任何內容 -->
</Window>

SingleTranslationWindow.xaml (核心 UI)
這就是使用者真正看到的譯文覆蓋層。

<Window x:Class="MonLingo.Interface.Windows.SingleTranslationWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Single Translation"
        WindowStyle="None"
        AllowsTransparency="True"
        Background="Transparent"
        ShowInTaskbar="False"
        Topmost="True"
        SizeToContent="WidthAndHeight"> <!-- 視窗大小由內容決定 -->

    <Border Background="#CC2E2E2E" <!-- 半透明深灰色背景 -->
            CornerRadius="8">
        
        <Grid>
            <!-- 第一層：用於文字描邊的副本 (黑色) -->
            <TextBlock Text="{Binding TranslatedText}"
                       Foreground="Black"
                       Margin="1,1,0,0"
                       FontWeight="Bold"
                       FontSize="16"/>
            <TextBlock Text="{Binding TranslatedText}"
                       Foreground="Black"
                       Margin="-1,-1,0,0"
                       FontWeight="Bold"
                       FontSize="16"/>
            <TextBlock Text="{Binding TranslatedText}"
                       Foreground="Black"
                       Margin="1,-1,0,0"
                       FontWeight="Bold"
                       FontSize="16"/>
            <TextBlock Text="{Binding TranslatedText}"
                       Foreground="Black"
                       Margin="-1,1,0,0"
                       FontWeight="Bold"
                       FontSize="16"/>
                       
            <!-- 第二層：主要的文字 (白色) -->
            <TextBlock Text="{Binding TranslatedText}"
                       Foreground="White"
                       FontWeight="Bold"
                       FontSize="16"
                       VerticalAlignment="Center"
                       HorizontalAlignment="Center"
                       Padding="8,4"/>
        </Grid>
    </Border>

    <!-- 淡入動畫觸發器 -->
    <Window.Triggers>
        <EventTrigger RoutedEvent="Window.Loaded">
            <BeginStoryboard>
                <Storyboard>
                    <DoubleAnimation Storyboard.TargetProperty="Opacity"
                                     From="0.0" To="1.0"
                                     Duration="0:0:0.3"/>
                </Storyboard>
            </BeginStoryboard>
        </EventTrigger>
    </Window.Triggers>
</Window>

5. 結論
Gaminik 的覆蓋模式是一個精心設計的多視窗系統，其核心技術優勢在於：

架構上: 使用一個宿主視窗 (HostWindow) 來統一管理和定位，確保所有譯文塊的相對位置準確無誤。

渲染上: 為每一行文字創建獨立的 Window 物件，利用 WPF 的渲染機制實現了高性能的並行渲染和獨立動畫。

視覺上: 透過半透明背景和文字描邊等技術，確保了譯文在任何複雜的遊戲背景下都清晰可讀。

要精確重現此功能，您的 MonLingo 專案必須遵循這個多視窗協作的架構，這是實現其流暢、精準的覆蓋體驗的關鍵所在。