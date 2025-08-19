# MonLingo 工具條樣式修改教學指南

> 新手友好版本 - 讓您輕鬆自訂 MonLingo 工具條外觀

## 📋 目錄
1. [基礎知識](#基礎知識)
2. [工具與環境準備](#工具與環境準備)
3. [圖標修改教學](#圖標修改教學)
4. [顏色與樣式調整](#顏色與樣式調整)
5. [進階定制技巧](#進階定制技巧)
6. [常見問題解答](#常見問題解答)

---

## 🎯 基礎知識

### 什麼是 WPF 和 XAML？
- **WPF**: Windows Presentation Foundation，微軟的 UI 框架
- **XAML**: 類似 HTML 的標記語言，用來描述 UI 界面
- **向量圖標**: 可無失真放大縮小的圖標，使用數學路徑描述

### 檔案結構說明
```
MonLingo2/
├── src/MonLingo.Core/View/Windows/
│   └── MainBarWindow.xaml           # 🎯 主要修改檔案
├── TestWindow.xaml                  # 📋 參考範本
└── docs/
    └── UI樣式修改教學指南.md        # 📖 本文檔
```

---

## 🛠️ 工具與環境準備

### 必需工具
1. **文字編輯器**: Visual Studio Code / Visual Studio / 記事本
2. **圖標資源**: [Lucide Icons 官網](https://lucide.dev/icons)
3. **顏色選擇器**: Windows 內建 / 線上工具

### 可選工具
- **SVG 編輯器**: Inkscape（免費）/ Adobe Illustrator
- **線上工具**: [SVG to Path 轉換器](https://svg-to-path.com/)

---

## 🎨 圖標修改教學

### 步驟 1: 打開主要檔案

1. 使用任何文字編輯器打開：
   ```
   MonLingo2\src\MonLingo.Core\View\Windows\MainBarWindow.xaml
   ```

2. 找到圖標定義區域（第 15-30 行左右）：
   ```xml
   <Window.Resources>
       <!-- 定義所有 Lucide 圖示的向量路徑數據 -->
       <Geometry x:Key="IconPlay">M6 4L20 12L6 20V4Z</Geometry>
       <Geometry x:Key="IconCrown">M2 4L6 14L12 6L18 14L22 4</Geometry>
       <!-- ... 更多圖標 ... -->
   </Window.Resources>
   ```

### 步驟 2: 理解圖標結構

每個圖標由三部分組成：
```xml
<Geometry x:Key="圖標名稱">路徑數據</Geometry>
```

- **x:Key**: 圖標的唯一名稱（用來引用）
- **路徑數據**: SVG 路徑，描述圖標形狀

### 步驟 3: 修改現有圖標

#### 方法 A: 使用 Lucide Icons

1. 前往 [Lucide Icons](https://lucide.dev/icons)
2. 搜尋想要的圖標（例如 "heart"）
3. 點擊圖標，複製 SVG 路徑
4. 替換現有路徑：

**範例：將播放按鈕改為心形**
```xml
<!-- 原始播放圖標 -->
<Geometry x:Key="IconPlay">M6 4L20 12L6 20V4Z</Geometry>

<!-- 修改為心形圖標 -->
<Geometry x:Key="IconPlay">M20.84 4.61A5.5 5.5 0 0 0 16.5 2.5C14.76 2.5 13.5 3.5 12 5C10.5 3.5 9.24 2.5 7.5 2.5A5.5 5.5 0 0 0 3.16 4.61C1.04 6.73 1.04 10.27 3.16 12.39L12 21.35L20.84 12.39C22.96 10.27 22.96 6.73 20.84 4.61Z</Geometry>
```

#### 方法 B: 創建全新圖標

1. 添加新的圖標定義：
```xml
<Geometry x:Key="IconStar">M12 2L15.09 8.26L22 9.27L17 14.14L18.18 21.02L12 17.77L5.82 21.02L7 14.14L2 9.27L8.91 8.26L12 2Z</Geometry>
```

2. 在按鈕中使用：
```xml
<Button Style="{StaticResource IconButton}" 
        Tag="{StaticResource IconStar}"/>
```

### 步驟 4: 常用圖標路徑資源

以下是一些常用圖標的路徑數據，可直接複製使用：

```xml
<!-- 愛心 -->
<Geometry x:Key="IconHeart">M20.84 4.61A5.5 5.5 0 0 0 16.5 2.5C14.76 2.5 13.5 3.5 12 5C10.5 3.5 9.24 2.5 7.5 2.5A5.5 5.5 0 0 0 3.16 4.61C1.04 6.73 1.04 10.27 3.16 12.39L12 21.35L20.84 12.39C22.96 10.27 22.96 6.73 20.84 4.61Z</Geometry>

<!-- 星星 -->
<Geometry x:Key="IconStar">M12 2L15.09 8.26L22 9.27L17 14.14L18.18 21.02L12 17.77L5.82 21.02L7 14.14L2 9.27L8.91 8.26L12 2Z</Geometry>

<!-- 首頁 -->
<Geometry x:Key="IconHome">M3 9L12 2L21 9V20C21 20.5304 20.7893 21.0391 20.4142 21.4142C20.0391 21.7893 19.5304 22 19 22H5C4.46957 22 3.96086 21.7893 3.58579 21.4142C3.21071 21.0391 3 20.5304 3 20V9Z M9 22V12H15V22</Geometry>

<!-- 音樂 -->
<Geometry x:Key="IconMusic">M9 18V5L21 3V16M9 18C9 19.105 8.105 20 7 20S5 19.105 5 18 5.895 16 7 16 9 16.895 9 18ZM21 16C21 17.105 20.105 18 19 18S17 17.105 17 16 17.895 14 19 14 21 14.895 21 16Z</Geometry>

<!-- 下載 -->
<Geometry x:Key="IconDownload">M21 15V19C21 19.5304 20.7893 21.0391 20.4142 21.4142C20.0391 21.7893 19.5304 22 19 22H5C4.46957 22 3.96086 21.7893 3.58579 21.4142C3.21071 21.0391 3 20.5304 3 20V15 M7 10L12 15M12 15L17 10M12 15V3</Geometry>

<!-- 垃圾桶 -->
<Geometry x:Key="IconTrash">M3 6H5H21 M19 6V20C19 20.5304 18.7893 21.0391 18.4142 21.4142C18.0391 21.7893 17.5304 22 17 22H7C6.46957 22 5.96086 21.7893 5.58579 21.4142C5.21071 21.0391 5 20.5304 5 20V6M8 6V4C8 3.46957 8.21071 2.96086 8.58579 2.58579C8.96086 2.21071 9.46957 2 10 2H14C14.5304 2 15.0391 2.21071 15.4142 2.58579C15.7893 2.96086 16 3.46957 16 4V6</Geometry>

<!-- 郵件 -->
<Geometry x:Key="IconMail">M4 4H20C21.1 4 22 4.9 22 6V18C22 19.1 21.1 20 20 20H4C2.9 20 2 19.1 2 18V6C2 4.9 2.9 4 4 4Z M22 6L12 13L2 6</Geometry>
```

---

## 🎨 顏色與樣式調整

### 修改工具條背景色

找到主容器 Border 標籤（約第 115 行）：
```xml
<Border CornerRadius="26" BorderThickness="1" BorderBrush="#44888888" Padding="12,8">
    <Border.Background>
        <SolidColorBrush Color="#1E1B2E" Opacity="0.95"/>  <!-- 在這裡修改顏色 -->
    </Border.Background>
```

**常用顏色代碼:**
- `#1E1B2E` - 深紫色（現在使用）
- `#000000` - 純黑色
- `#2D2D2D` - 深灰色
- `#1F2937` - 深藍灰
- `#7C3AED` - 紫色
- `#3B82F6` - 藍色

### 修改圖標顏色

找到圖標按鈕樣式（約第 32 行）：
```xml
<Style x:Key="IconButton" TargetType="Button">
    <Setter Property="Foreground" Value="White"/>  <!-- 修改這裡 -->
```

### 修改按鈕大小

```xml
<Style x:Key="IconButton" TargetType="Button">
    <Setter Property="Width" Value="32"/>     <!-- 原始: 28 -->
    <Setter Property="Height" Value="32"/>    <!-- 原始: 28 -->
    <Setter Property="Padding" Value="8"/>    <!-- 原始: 6 -->
```

### 修改懸停效果

找到 IsMouseOver 觸發器：
```xml
<Style.Triggers>
    <Trigger Property="IsMouseOver" Value="True">
        <Setter Property="Background" Value="#4F46E5"/>  <!-- 懸停時的背景色 -->
        <Setter Property="Foreground" Value="White"/>    <!-- 懸停時的圖標色 -->
    </Trigger>
</Style.Triggers>
```

---

## 🚀 進階定制技巧

### 1. 創建發光效果

```xml
<Style x:Key="GlowIconButton" TargetType="Button" BasedOn="{StaticResource IconButton}">
    <Setter Property="Effect">
        <Setter.Value>
            <DropShadowEffect Color="#3B82F6" BlurRadius="15" ShadowDepth="0" Opacity="0.8"/>
        </Setter.Value>
    </Setter>
</Style>
```

使用方法：
```xml
<Button Style="{StaticResource GlowIconButton}" Tag="{StaticResource IconPlay}"/>
```

### 2. 添加旋轉動畫

```xml
<Button.RenderTransform>
    <RotateTransform CenterX="14" CenterY="14"/>
</Button.RenderTransform>
<Button.Triggers>
    <EventTrigger RoutedEvent="Button.Click">
        <BeginStoryboard>
            <Storyboard>
                <DoubleAnimation Storyboard.TargetProperty="RenderTransform.Angle" 
                                To="360" Duration="0:0:0.5"/>
            </Storyboard>
        </BeginStoryboard>
    </EventTrigger>
</Button.Triggers>
```

### 3. 創建漸層色按鈕

```xml
<Style x:Key="GradientIconButton" TargetType="Button" BasedOn="{StaticResource IconButton}">
    <Setter Property="Background">
        <Setter.Value>
            <LinearGradientBrush StartPoint="0,0" EndPoint="1,1">
                <GradientStop Color="#3B82F6" Offset="0"/>
                <GradientStop Color="#1D4ED8" Offset="1"/>
            </LinearGradientBrush>
        </Setter.Value>
    </Setter>
</Style>
```

### 4. 添加按下效果

```xml
<Trigger Property="IsPressed" Value="True">
    <Setter Property="RenderTransform">
        <Setter.Value>
            <ScaleTransform ScaleX="0.9" ScaleY="0.9" CenterX="14" CenterY="14"/>
        </Setter.Value>
    </Setter>
</Trigger>
```

---

## 🔧 實際操作範例

### 範例 1: 將播放按鈕改為暫停按鈕

1. 找到播放圖標定義：
```xml
<Geometry x:Key="IconPlay">M6 4L20 12L6 20V4Z</Geometry>
```

2. 替換為暫停圖標：
```xml
<Geometry x:Key="IconPlay">M6 4H10V20H6V4ZM14 4H18V20H14V4Z</Geometry>
```

### 範例 2: 添加新的自訂按鈕

1. 添加圖標定義：
```xml
<Geometry x:Key="IconCustom">M12 2L13.09 8.26L22 9.27L17 14.14L18.18 21.02L12 17.77L5.82 21.02L7 14.14L2 9.27L8.91 8.26L12 2Z</Geometry>
```

2. 在按鈕區域添加按鈕（找到 StackPanel）：
```xml
<StackPanel Orientation="Horizontal" VerticalAlignment="Center">
    <!-- 現有按鈕... -->
    
    <!-- 新增的自訂按鈕 -->
    <Button x:Name="CustomButton"
            Style="{StaticResource IconButton}"
            Tag="{StaticResource IconCustom}"
            Margin="4,0,0,0"/>
</StackPanel>
```

### 範例 3: 修改整體色彩主題

將工具條改為藍色主題：

1. 修改背景色：
```xml
<Border.Background>
    <SolidColorBrush Color="#1E3A8A" Opacity="0.95"/>  <!-- 深藍色 -->
</Border.Background>
```

2. 修改邊框色：
```xml
<Border CornerRadius="26" BorderThickness="1" BorderBrush="#3B82F6" Padding="12,8">
```

3. 修改懸停效果：
```xml
<Trigger Property="IsMouseOver" Value="True">
    <Setter Property="Background" Value="#2563EB"/>
</Trigger>
```

---

## 🧪 測試與應用

### 保存並測試

1. **保存檔案**: `Ctrl + S`
2. **重新建置專案**:
   ```bash
   cd MonLingo2
   dotnet build src/MonLingo.Core/MonLingo.Core.csproj
   ```
3. **運行應用程式**:
   ```bash
   cd src/MonLingo.Core/bin/x64/Debug/net481
   .\MonLingo.Core.exe
   ```

### 除錯技巧

如果修改後無法顯示：

1. **檢查語法**: 確保 XML 標籤正確閉合
2. **檢查路徑**: 確保 SVG 路徑數據正確
3. **查看錯誤**: 檢查建置輸出中的錯誤訊息
4. **恢復備份**: 如果出問題，複製 `TestWindow.xaml` 內容

---

## ❓ 常見問題解答

### Q: 圖標顯示不出來怎麼辦？
**A**: 檢查以下項目：
- SVG 路徑數據是否正確
- x:Key 名稱是否拼寫正確
- 是否有語法錯誤（遺漏引號、括號等）

### Q: 如何找到更多圖標？
**A**: 推薦資源：
- [Lucide Icons](https://lucide.dev/icons) - 本專案使用
- [Heroicons](https://heroicons.com/)
- [Phosphor Icons](https://phosphoricons.com/)
- [Tabler Icons](https://tabler-icons.io/)

### Q: 如何創建自己的圖標？
**A**: 步驟：
1. 使用 Inkscape 或 AI 創建 SVG
2. 使用線上工具轉換為路徑數據
3. 調整座標系統（通常是 24x24）

### Q: 修改後圖標變形怎麼辦？
**A**: 調整圖標容器大小：
```xml
<Path Width="16" Height="16" Stretch="Uniform"/>
```

### Q: 如何備份原始設計？
**A**: 複製 `TestWindow.xaml` 到 `MainBarWindow.xaml.backup`

---

## 📚 擴展學習資源

### 官方文檔
- [WPF 官方文檔](https://docs.microsoft.com/zh-tw/dotnet/desktop/wpf/)
- [XAML 語法指南](https://docs.microsoft.com/zh-tw/dotnet/desktop/wpf/xaml/)

### 圖標資源
- [Lucide Icons](https://lucide.dev/icons)
- [Heroicons](https://heroicons.com/)
- [Material Design Icons](https://materialdesignicons.com/)

### 線上工具
- [SVG Path Editor](https://yqnn.github.io/svg-path-editor/)
- [SVG to Path Converter](https://svg-to-path.com/)
- [Color Picker](https://htmlcolorcodes.com/color-picker/)

---

## 🎉 結語

通過這個指南，您應該能夠：
- ✅ 理解 MonLingo 工具條的結構
- ✅ 修改現有圖標
- ✅ 添加新圖標
- ✅ 調整顏色和樣式
- ✅ 應用進階效果

記住：**小心修改，經常備份，大膽嘗試！**

如果您有任何問題或需要更多幫助，請隨時詢問！

---

*最後更新: 2025-08-18*
*版本: 1.0.0*
