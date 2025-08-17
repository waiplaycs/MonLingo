# MonLingo 桌面翻譯軟體

基於 Gaminik.dll 深度架構分析的高效能桌面翻譯解決方案。

## 專案概述

MonLingo 是一款精確重現 Gaminik 軟體底層架構的桌面端翻譯工具，採用雙層 DLL 設計確保模組化、可維護性和高效能。

### 核心特性
- **即時螢幕翻譯**：支援任意區域選擇與實時 OCR 處理
- **離線翻譯支援**：整合 CTranslate2 引擎，無網路依賴
- **音訊轉錄**：WASAPI + Whisper.net 實現語音轉文字
- **高效能架構**：C++ Native.dll + C# Core.dll 雙層設計
- **多語言支援**：40+ 語言 OCR，20+ 語言音訊識別

### 技術棧
- **UI 框架**：WPF + .NET Framework 4.8.1 (x64)
- **架構模式**：MVVM + 依賴注入 + 服務導向
- **OCR 引擎**：PaddleOCR + k-means 聚類演算法
- **音訊處理**：WASAPI + Whisper.net
- **圖像擷取**：Windows Graphics Capture API + DirectX 11

## 快速開始

### 系統需求
- Windows 10 1903+ (支援 Windows Graphics Capture API)
- .NET Framework 4.8.1
- Visual Studio 2022 或 VS Build Tools
- 8GB+ RAM，支援 DirectX 11 的顯卡

### 建置步驟

```powershell
# 1. 克隆專案
git clone [repository-url] MonLingo
cd MonLingo

# 2. 還原 NuGet 套件
dotnet restore

# 3. 建置解決方案
dotnet build --configuration Release --platform x64

# 4. 執行應用程式
.\bin\Release\x64\MonLingo.exe
```

### 開發環境設置

```powershell
# 安裝開發工具
dotnet tool install --global dotnet-format
dotnet tool install --global dotnet-coverage

# 設置 Git hooks
.\scripts\setup-git-hooks.ps1

# 驗證建置環境
.\scripts\verify-build-env.ps1
```

## 專案結構

```
MonLingo/
├── src/
│   ├── MonLingo/                 # 主應用程式 (WPF)
│   ├── MonLingo.Core/            # 核心邏輯層 (C#)
│   │   ├── View/                 # WPF 視窗與控制項
│   │   ├── ViewModel/            # MVVM ViewModels
│   │   ├── Service/              # 服務層
│   │   ├── Core/                 # 核心業務邏輯
│   │   └── Data/                 # 資料模型與 DAL
│   └── MonLingo.Native/          # 原生功能層 (C++)
│       ├── ScreenCapture/        # 螢幕擷取模組
│       ├── OCREngine/            # OCR 處理管線
│       ├── AudioCapture/         # 音訊擷取模組
│       └── Security/             # 加密與安全模組
├── tests/
│   └── MonLingo.Tests/           # 單元測試與整合測試
├── docs/                         # 技術文件
└── scripts/                      # 建置與部署腳本
```

## 核心工作流程

### 即時翻譯流程
1. **觸發**：使用者按下熱鍵 (預設 Ctrl+Q)
2. **選取**：顯示螢幕覆蓋層，拖曳選擇翻譯區域
3. **擷取**：啟動高頻率螢幕擷取 (60+ FPS 生產者)
4. **OCR**：PaddleOCR + k-means 處理影像轉文字
5. **翻譯**：線上 API 或離線模型執行翻譯
6. **顯示**：覆蓋視窗呈現翻譯結果

### 音訊轉錄流程
1. **擷取**：WASAPI loopback 模式擷取系統音訊
2. **識別**：Whisper.net 語音轉文字
3. **翻譯**：複用翻譯服務處理文字
4. **呈現**：專用 UI 顯示轉錄與翻譯結果

## 開發指南

### 程式碼風格
- 遵循 Microsoft C# 編碼慣例
- 使用 EditorConfig 統一格式化
- 所有 public API 需要 XML 文件註解
- 非同步方法須以 Async 結尾

### 測試策略
- 單元測試覆蓋率 ≥ 65%
- 關鍵路徑 100% 測試覆蓋
- 使用 MSTest 框架
- P/Invoke 層需要整合測試

### 效能標準
- 螢幕擷取：≥ 60 FPS (生產者)
- OCR 處理：p95 ≤ 150ms
- 全流程延遲：p95 ≤ 300ms
- 記憶體占用：≤ 450MB (常駐)

## 授權與依賴

### 開源授權
本專案使用多種開源組件，詳細授權資訊請參閱 [NOTICE.md](NOTICE.md)：
- **核心組件**：MIT, Apache-2.0, BSD-3-Clause
- **OCR 引擎**：PaddleOCR (Apache-2.0)
- **音訊處理**：Whisper.net (MIT)
- **UI 框架**：Prism.Core (MIT), CommunityToolkit.Mvvm (MIT)

### 第三方依賴
所有 NuGet 套件版本鎖定於 `Directory.Packages.props`，確保可重現建置。

## 專案狀態

### 當前里程碑
- ✅ M0: 專案初始化與版本鎖定 (2025-08-22)
- 🚧 M1: Native Bridge 與螢幕擷取 (2025-09-05)
- ⏳ M2: OCR 管線與資料結構 (2025-09-19)
- ⏳ M3: 核心 UI/設定/熱鍵 (2025-10-03)
- ⏳ RC: 效能優化與發佈 (2025-10-17)

### 已知限制
- 需要 Windows 10 1903+ 支援 Graphics Capture API
- DirectX 11 硬體需求（GPU 加速）
- 部分防毒軟體可能誤報全域鉤子功能

## 貢獻指南

1. Fork 專案並建立功能分支
2. 確保所有測試通過 (`dotnet test`)
3. 遵循程式碼風格指南
4. 提交 Pull Request 附上詳細說明

## 支援與回饋

- **問題回報**：使用 GitHub Issues
- **功能請求**：標註 `enhancement` 標籤
- **安全漏洞**：私下聯繫維護團隊

---

**注意**：本專案嚴格遵循 Gaminik.dll 原始架構設計，確保技術一致性與效能標準。
