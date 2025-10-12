# DeepSeek API 配置指南

## 🌟 為什麼選擇 DeepSeek?

DeepSeek 是一個高性價比的 AI 服務提供商,特別適合翻譯應用:

| 特性 | DeepSeek | OpenAI (GPT-4o-mini) | 優勢 |
|------|----------|---------------------|------|
| **成本** | $0.14/1M input<br>$0.28/1M output | $0.15/1M input<br>$0.60/1M output | **降低 50% 輸出成本** |
| **中文支援** | ⭐⭐⭐⭐⭐ 專為中文優化 | ⭐⭐⭐⭐ 良好 | 更好的中文翻譯質量 |
| **速度** | 快速推理 | 快速 | 相當 |
| **API 兼容性** | 完全兼容 OpenAI API | 原生 | 無縫切換 |

### 成本對比示例

假設翻譯 **100 頁漫畫** (每頁約 500 tokens 輸入 + 600 tokens 輸出):

| 提供商 | 輸入成本 | 輸出成本 | 總成本 |
|--------|---------|---------|--------|
| **DeepSeek** | $0.007 | $0.017 | **$0.024** |
| OpenAI GPT-4o-mini | $0.0075 | $0.036 | **$0.044** |
| **節省** | - | - | **45%** 💰 |

---

## 📋 快速開始 (5分鐘)

### 步驟 1: 獲取 DeepSeek API Key

1. 訪問 [DeepSeek 官網](https://platform.deepseek.com/)
2. 註冊帳號並登入
3. 進入 **API Keys** 頁面
4. 點擊 **Create API Key**
5. 複製生成的 API Key (格式: `sk-...`)

> 💡 **提示**: DeepSeek 提供免費額度,適合測試使用

---

### 步驟 2: 配置方式 (三選一)

#### ✅ 方式 1: 環境變數 (推薦)

**Windows PowerShell:**
```powershell
# 設置當前會話
$env:MONLINGO_DEEPSEEK_API_KEY = "sk-your-deepseek-api-key"

# 永久設置 (系統環境變數)
[System.Environment]::SetEnvironmentVariable('MONLINGO_DEEPSEEK_API_KEY', 'sk-your-deepseek-api-key', 'User')
```

**Windows CMD:**
```cmd
set MONLINGO_DEEPSEEK_API_KEY=sk-your-deepseek-api-key
```

**Linux/Mac:**
```bash
export MONLINGO_DEEPSEEK_API_KEY="sk-your-deepseek-api-key"

# 加入 ~/.bashrc 或 ~/.zshrc 永久生效
echo 'export MONLINGO_DEEPSEEK_API_KEY="sk-your-deepseek-api-key"' >> ~/.bashrc
```

---

#### ✅ 方式 2: appsettings.json 配置文件

創建或編輯 `appsettings.json`:

```json
{
  "AITranslation": {
    "Provider": "DeepSeek",
    "Model": "deepseek-chat",
    "ApiKey": "sk-your-deepseek-api-key",
    "Temperature": 0.3,
    "MaxTokens": 2000,
    "MaxRetries": 3,
    "TimeoutSeconds": 30,
    "DailyQuotaLimit": 1000,
    "EnableVerboseLogging": false,
    "EnableCache": true,
    "CacheExpirationHours": 24
  }
}
```

> ⚠️ **安全提示**: 不要將 API Key 提交到 Git 倉庫!

---

#### ✅ 方式 3: 代碼配置

```csharp
var config = new AITranslationConfig
{
    Provider = AIProvider.DeepSeek,
    Model = "deepseek-chat",
    ApiKey = "sk-your-deepseek-api-key",
    Temperature = 0.3,
    MaxTokens = 2000
};

var translationService = new AITranslationService(config);
```

---

## 🔧 配置參數詳解

### Provider (提供商)
- **值**: `"DeepSeek"`, `"OpenAI"`, `"Gemini"`
- **說明**: 選擇 AI 服務提供商
- **推薦**: `"DeepSeek"` (性價比最高)

### Model (模型)
- **DeepSeek**: `"deepseek-chat"` (推薦)
- **OpenAI**: `"gpt-4o-mini"`, `"gpt-4o"`
- **Gemini**: `"gemini-1.5-flash"`, `"gemini-1.5-pro"`

### Temperature (溫度)
- **範圍**: 0.0 - 2.0
- **推薦**: `0.3` (翻譯需要穩定性)
- **說明**: 
  - `0.0-0.5`: 更準確、一致
  - `0.5-1.0`: 平衡
  - `1.0-2.0`: 更有創造性

### MaxTokens (最大輸出長度)
- **推薦**: `2000`
- **說明**: 單次翻譯最大輸出 tokens 數

### MaxRetries (最大重試次數)
- **推薦**: `3`
- **說明**: API 失敗時重試次數

### DailyQuotaLimit (每日配額限制)
- **推薦**: `1000` (可根據需求調整)
- **說明**: 每日最多調用次數,`0` 表示無限制

---

## 🧪 測試配置

### 方法 1: 使用測試程序

```csharp
// DualChannelV4Demo.cs 或創建新的測試文件
var config = AITranslationConfigLoader.LoadFromFile();

Console.WriteLine($"提供商: {config.Provider}");
Console.WriteLine($"模型: {config.Model}");
Console.WriteLine($"API端點: {config.GetApiEndpoint()}");
Console.WriteLine($"環境變數: {config.GetEnvironmentVariableName()}");

var service = new AITranslationService(config);
Console.WriteLine("✅ AITranslationService 初始化成功!");
```

### 方法 2: Phase 4 測試計劃

參考 `Phase4_測試驗證計劃.md` 執行完整測試流程。

---

## 🔄 多提供商切換

MonLingo 支援在不同 AI 提供商之間輕鬆切換:

### 切換到 OpenAI:
```json
{
  "AITranslation": {
    "Provider": "OpenAI",
    "Model": "gpt-4o-mini",
    "ApiKey": "sk-your-openai-api-key"
  }
}
```

環境變數:
```powershell
$env:MONLINGO_OPENAI_API_KEY = "sk-your-openai-api-key"
```

### 切換到 Gemini:
```json
{
  "AITranslation": {
    "Provider": "Gemini",
    "Model": "gemini-1.5-flash",
    "ApiKey": "your-gemini-api-key"
  }
}
```

環境變數:
```powershell
$env:MONLINGO_GEMINI_API_KEY = "your-gemini-api-key"
```

---

## ❓ 常見問題

### Q1: DeepSeek 免費嗎?
**A**: DeepSeek 提供免費試用額度。具體額度請查看官網。

### Q2: DeepSeek 支援哪些語言?
**A**: 支援多種語言,但對中文特別優化,非常適合中日韓文翻譯。

### Q3: 如何查看剩餘額度?
**A**: 登入 [DeepSeek Platform](https://platform.deepseek.com/) 查看帳戶詳情。

### Q4: API Key 安全性?
**A**: 
- ✅ **推薦**: 使用環境變數
- ⚠️ **不推薦**: 直接寫在配置文件中
- ❌ **禁止**: 提交到 Git 倉庫

### Q5: 遇到 API 錯誤怎麼辦?
**A**: 
1. 檢查 API Key 是否正確
2. 確認網絡連接
3. 查看 `EnableVerboseLogging: true` 的詳細日誌
4. 檢查配額是否用盡

---

## 📊 性能監控

啟用詳細日誌以監控性能:

```json
{
  "AITranslation": {
    "EnableVerboseLogging": true
  }
}
```

日誌將顯示:
- ✅ Token 使用量 (輸入/輸出)
- ✅ 調用耗時
- ✅ 估算成本
- ✅ 重試次數

---

## 🎯 最佳實踐

1. **使用環境變數** 保護 API Key
2. **設置合理配額** 避免過度使用
3. **啟用緩存** 減少重複調用
4. **監控日誌** 及時發現問題
5. **測試小批量** 再處理大量數據
6. **備用提供商** 配置多個 API 以防故障

---

## 📚 相關文檔

- [Phase 4 測試驗證計劃](Phase4_測試驗證計劃.md)
- [API 配置完整文檔](API_Configuration.md)
- [DeepSeek 官方文檔](https://platform.deepseek.com/docs)

---

## 💡 需要幫助?

- 查看 [Phase4_測試驗證計劃.md](Phase4_測試驗證計劃.md) 的快速開始指南
- 檢查日誌文件 (啟用 `EnableVerboseLogging`)
- 參考 DeepSeek [官方文檔](https://platform.deepseek.com/docs)

---

**🎉 配置完成後,就可以開始 Phase 4 測試了!**
