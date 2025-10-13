# OpenRouter 集成指南 (未來優化方案)

## 🌟 什麼是 OpenRouter?

OpenRouter 是一個 **AI API 聚合服務**,提供統一的 API 訪問 200+ AI 模型,包括:
- OpenAI (GPT-4, GPT-4o-mini)
- Anthropic (Claude)
- Google (Gemini)
- DeepSeek
- Meta (Llama)
- 等等...

---

## 💡 為什麼考慮 OpenRouter?

### 當前方式 vs OpenRouter

| 功能 | 當前實現 | OpenRouter 方式 |
|------|---------|----------------|
| **API Keys** | ❌ 每個提供商需要單獨申請 | ✅ **只需1個 Key** |
| **端點配置** | ⚠️ 需要為每個提供商手動配置 | ✅ **統一端點** |
| **切換模型** | ⚠️ 修改 Provider + Model + Endpoint | ✅ **只改模型名稱** |
| **新增提供商** | ❌ 需要修改代碼 | ✅ **即開即用** |
| **成本追蹤** | ⚠️ 需要自己計算 | ✅ **統一儀表板** |
| **額外成本** | 無 | 約 10-20% 加價 |

---

## 🚀 如何使用 OpenRouter (未來計劃)

### 步驟 1: 獲取 OpenRouter API Key

1. 訪問 [OpenRouter](https://openrouter.ai/)
2. 註冊帳號
3. 創建 API Key (格式: `sk-or-v1-...`)

### 步驟 2: 配置 MonLingo

**方式 1: 環境變數**
```powershell
$env:MONLINGO_OPENROUTER_API_KEY = "sk-or-v1-your-key"
```

**方式 2: appsettings.json**
```json
{
  "AITranslation": {
    "Provider": "OpenRouter",
    "Model": "deepseek/deepseek-chat",
    "ApiKey": "",
    "ApiEndpoint": "https://openrouter.ai/api/v1"
  }
}
```

### 步驟 3: 切換模型超簡單!

只需修改 `Model` 欄位:

```json
// DeepSeek (便宜)
"Model": "deepseek/deepseek-chat"

// GPT-4o-mini (OpenAI)
"Model": "openai/gpt-4o-mini"

// Gemini Flash (Google)
"Model": "google/gemini-1.5-flash"

// Claude Haiku (Anthropic)
"Model": "anthropic/claude-3-haiku"

// Llama 3.1 (Meta)
"Model": "meta-llama/llama-3.1-70b-instruct"
```

**不需要修改任何代碼!** 🎉

---

## 📊 成本對比 (100頁漫畫翻譯)

| 模型 | 直接調用 | 通過 OpenRouter | 差異 |
|------|---------|----------------|------|
| DeepSeek | $0.024 | $0.026 | +8% |
| GPT-4o-mini | $0.044 | $0.048 | +9% |
| Gemini Flash | $0.022 | $0.024 | +9% |

**結論**: OpenRouter 加價約 8-10%,但換來極大的便利性!

---

## 🔧 實現 OpenRouter 支援需要的修改

### 1. 更新 AIProvider 枚舉

```csharp
public enum AIProvider
{
    OpenAI,
    DeepSeek,
    Gemini,
    OpenRouter  // 新增
}
```

### 2. 更新 GetApiEndpoint()

```csharp
public string GetApiEndpoint()
{
    if (!string.IsNullOrWhiteSpace(ApiEndpoint))
        return ApiEndpoint;

    return Provider switch
    {
        AIProvider.OpenRouter => "https://openrouter.ai/api/v1",
        AIProvider.DeepSeek => "https://api.deepseek.com",
        AIProvider.OpenAI => "https://api.openai.com/v1",
        _ => null
    };
}
```

### 3. 更新環境變數名稱

```csharp
public string GetEnvironmentVariableName()
{
    return Provider switch
    {
        AIProvider.OpenRouter => "MONLINGO_OPENROUTER_API_KEY",
        AIProvider.DeepSeek => "MONLINGO_DEEPSEEK_API_KEY",
        AIProvider.OpenAI => "MONLINGO_OPENAI_API_KEY",
        AIProvider.Gemini => "MONLINGO_GEMINI_API_KEY",
        _ => "MONLINGO_API_KEY"
    };
}
```

### 4. OpenRouter 特殊配置

OpenRouter 需要在 HTTP Header 中添加額外信息:

```csharp
// 需要在 CreateChatClient() 中添加
if (_config.Provider == AIProvider.OpenRouter)
{
    // OpenRouter 需要 HTTP-Referer 和 X-Title headers
    var openAiClientOptions = new OpenAIClientOptions
    {
        Endpoint = new Uri(_config.GetApiEndpoint()),
        // 需要添加自定義 headers (可能需要使用 HttpClient)
    };
}
```

---

## 🎯 推薦方案

### 短期 (當前)
✅ **使用直接 API** (DeepSeek/OpenAI)
- 成本最低
- 完全控制
- 已經實現

### 中期 (如果需要頻繁切換)
✅ **添加 OpenRouter 支援**
- 統一管理
- 快速實驗不同模型
- 輕微成本增加

### 選擇建議:

| 場景 | 推薦方式 |
|------|---------|
| 大量生產翻譯 | ✅ 直接 DeepSeek API (成本最低) |
| 測試多種模型 | ✅ OpenRouter (最方便) |
| 只用一個模型 | ✅ 直接 API (沒必要用 OpenRouter) |
| 企業級部署 | ✅ 直接 API (更穩定) |

---

## 📚 OpenRouter 支援的模型列表

**翻譯推薦模型:**

| 模型 | OpenRouter 名稱 | 適用場景 |
|------|----------------|---------|
| DeepSeek Chat | `deepseek/deepseek-chat` | 中文翻譯,成本最低 |
| GPT-4o-mini | `openai/gpt-4o-mini` | 平衡性能和成本 |
| Claude Haiku | `anthropic/claude-3-haiku` | 快速翻譯 |
| Gemini Flash | `google/gemini-1.5-flash` | 大上下文 |
| Llama 3.1 70B | `meta-llama/llama-3.1-70b-instruct` | 開源,便宜 |

完整列表: https://openrouter.ai/models

---

## ⚠️ 注意事項

1. **成本**: OpenRouter 會加價約 10%
2. **延遲**: 可能比直接調用稍慢
3. **依賴**: 增加了對第三方服務的依賴
4. **Headers**: 需要正確設置 HTTP headers

---

## 🔄 遷移步驟 (如果決定使用)

1. ✅ 註冊 OpenRouter 帳號
2. ✅ 獲取 API Key
3. ✅ 修改代碼支援 OpenRouter Provider
4. ✅ 更新配置文件
5. ✅ 測試各種模型
6. ✅ 監控成本

---

## 📖 相關資源

- OpenRouter 官網: https://openrouter.ai/
- OpenRouter 文檔: https://openrouter.ai/docs
- 模型列表: https://openrouter.ai/models
- 定價: https://openrouter.ai/docs#models

---

## 💭 結論

**當前建議**: 
- ✅ 繼續使用直接 DeepSeek API (已配置完成)
- ✅ 成本最優,性能最佳
- ✅ 如果未來需要頻繁切換模型,再考慮添加 OpenRouter 支援

**OpenRouter 適合**:
- 需要測試多種模型的開發階段
- 不確定哪個模型最適合的場景
- 願意付出 10% 額外成本換取便利性

---

**您現在的配置已經很好了!** DeepSeek 直接 API 是性價比最高的選擇! 💰✨
