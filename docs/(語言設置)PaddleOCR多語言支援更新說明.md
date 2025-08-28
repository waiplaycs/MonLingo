# MonLingo 翻譯語言設置更新說明

## 更新概述
根據 PaddleOCR 的實際支持語種，我們已經更新了 MonLingo 設置界面中的翻譯語言選項，以提供更完整和準確的多語言支持。

## 更新內容

### 1. 新增語言設置模型 (`LanguageSettings.cs`)
- 創建了統一的語言管理類 `MonLingo.Core.Models.LanguageSettings`
- 包含源語言和目標語言的完整映射表
- 提供語言代碼和顯示名稱之間的轉換功能

### 2. 源語言支持 (基於 PaddleOCR)
#### 支持的語種 (25種)：
- 自動檢測 (auto)
- 中文 - Chinese (Simplified) (zh)
- 繁中 - Chinese (Traditional) (zh-tw)
- English - 英文 (en)
- 日本語 - Japanese (ja)
- 한국어 - Korean (ko)
- Français - French (fr)
- Deutsch - German (de)
- Español - Spanish (es)
- Português - Portuguese (pt)
- Italiano - Italian (it)
- Русский - Russian (ru)
- العربية - Arabic (ar)
- Nederlands - Dutch (nl)
- Polski - Polish (pl)
- Bahasa Indonesia - Indonesian (id)
- Tiếng Việt - Vietnamese (vi)
- ไทย - Thai (th)
- हिन्दी - Hindi (hi)
- Svenska - Swedish (sv)
- Čeština - Czech (cs)
- Dansk - Danish (da)
- Română - Romanian (ro)
- Magyar - Hungarian (hu)
- Ελληνικά - Greek (el)

### 3. 目標語言支持 (翻譯引擎)
#### 支持的語種 (24種)：
與源語言相同，但不包含「自動檢測」選項，預設為中文(繁體)

### 4. 更新的設置功能
- **智能語言檢測**：自動識別輸入文本的語言類型
- **記住語言偏好**：保存用戶的語言選擇偏好
- **啟用多語言混合識別**：支持同一圖片中的多語言混合文本識別

## 技術實現

### 文件修改：
1. `src/MonLingo.Core/Models/LanguageSettings.cs` (新增)
   - 語言設置模型類
   - 提供語言代碼和顯示名稱的映射
   - 包含轉換方法

2. `src/MonLingo.Core/View/Windows/SettingMainWindow.xaml.cs` (修改)
   - 更新 `CreateTranslationLanguageContent()` 方法
   - 使用新的語言設置模型
   - 添加多語言混合識別選項

### 核心功能：
```csharp
// 獲取語言列表
string[] sourceLanguages = LanguageSettings.GetSourceLanguageDisplayNames();
string[] targetLanguages = LanguageSettings.GetTargetLanguageDisplayNames();

// 語言代碼轉換
string code = LanguageSettings.GetLanguageCodeByDisplayName(displayName);
string name = LanguageSettings.GetDisplayNameByLanguageCode(code);
```

## 用戶界面改進

### 界面設計參考：
- 左側：源語言設置
- 右側：目標語言設置
- 雙語顯示格式：「本地語言名 - English Name」
- 語言代碼映射：遵循 ISO 639-1 標準

### 增強功能：
1. **更豐富的語言選項**：從原來的8種語言擴展到25種
2. **本地化顯示**：每種語言都以其原生文字和英文名稱顯示
3. **智能設置**：增加多語言混合識別等高級功能

## 使用方式

### 設置步驟：
1. 開啟 MonLingo 應用程式
2. 點擊設置按鈕
3. 選擇左側導航欄中的「翻譯語言」
4. 配置源語言和目標語言
5. 調整語言偏好設置

### 推薦配置：
- **源語言**：保持「自動檢測」以獲得最佳識別效果
- **目標語言**：根據個人需要選擇主要使用的語言
- **智能檢測**：建議開啟，提高識別準確性
- **混合識別**：適合處理多語言文檔時開啟

## 兼容性

### PaddleOCR 版本：
- 支持 PaddleOCR 3.0.0 及以上版本
- 向下兼容現有語言設置

### 翻譯引擎：
- 支持 Google Translate
- 支持 Microsoft Translator
- 支持百度翻譯等主流翻譯服務

## 注意事項

1. **語言檢測準確性**：自動檢測功能依賴於圖片中的文字品質
2. **翻譯品質**：不同語言對之間的翻譯品質可能會有差異
3. **性能考量**：啟用多語言混合識別可能會稍微增加處理時間

## 更新日期
2025年8月27日

## 相關文檔
- PaddleOCR 官方文檔
- MonLingo 使用者手冊
- 語言設置 API 文檔
