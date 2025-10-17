# API 配置說明

## 🔐 安全提示

**重要**: 本項目的配置文件包含敏感的 API 密鑰,已被 `.gitignore` 排除,不會提交到 Git 倉庫。

## 📋 配置步驟

### 1. 複製示例配置文件

根據您使用的 AI 服務,複製對應的示例配置文件:

**OpenRouter (推薦 - 免費)**:
```bash
# 根目錄
cp appsettings.openrouter.example.json appsettings.openrouter.json

# 或 Core 項目目錄
cp src/MonLingo.Core/appsettings.example.json src/MonLingo.Core/appsettings.json
```

**DeepSeek**:
```bash
cp appsettings.deepseek.example.json appsettings.deepseek.json
```

### 2. 填入您的 API Key

編輯複製的配置文件,將 `YOUR_OPENROUTER_API_KEY_HERE` 替換為您的實際 API Key:

```json
{
  "AITranslation": {
    "Provider": "OpenRouter",
    "Model": "google/gemini-2.0-flash-exp:free",
    "ApiKey": "sk-or-v1-YOUR_ACTUAL_KEY_HERE",  // ← 替換這裡
    "ApiEndpoint": "https://openrouter.ai/api/v1",
    "Temperature": 0.3,
    "MaxTokens": 4000,
    "MaxRetries": 3,
    "EnableVerboseLogging": false
  }
}
```

### 3. 獲取 API Key

#### OpenRouter (免費)
1. 訪問 [OpenRouter](https://openrouter.ai/)
2. 註冊帳號並登入
3. 前往 [Keys 頁面](https://openrouter.ai/keys)
4. 創建新的 API Key
5. 複製 Key (格式: `sk-or-v1-...`)

#### DeepSeek
1. 訪問 [DeepSeek Platform](https://platform.deepseek.com/)
2. 註冊帳號並登入
3. 獲取 API Key

## 🚀 推薦配置

### OpenRouter + Gemini 2.0 Flash (免費)

**優勢**:
- ✅ **完全免費** - 無需付費
- ⚡ **速度快** - 1-2 秒完成翻譯
- 🎯 **質量高** - Google 最新模型
- 💰 **零成本** - 適合個人使用

**配置**:
```json
{
  "AITranslation": {
    "Provider": "OpenRouter",
    "Model": "google/gemini-2.0-flash-exp:free",
    "ApiKey": "sk-or-v1-YOUR_KEY_HERE"
  }
}
```

### 其他可用模型

**OpenRouter 其他免費模型**:
- `google/gemini-2.0-flash-exp:free` - Gemini 2.0 Flash (推薦)
- `meta-llama/llama-3.2-3b-instruct:free` - Llama 3.2 3B
- `qwen/qwen-2-7b-instruct:free` - Qwen 2 7B

**OpenAI (付費)**:
- `gpt-4o-mini` - 快速且經濟
- `gpt-4o` - 最高質量

## 📂 配置文件優先級

應用程序按以下順序查找配置:

1. `src/MonLingo.Core/appsettings.json` (優先)
2. `appsettings.openrouter.json`
3. `appsettings.deepseek.json`
4. 環境變數 `MONLINGO_OPENROUTER_API_KEY`

## ⚠️ 注意事項

1. **不要提交 API Key**: 確保 `.gitignore` 包含:
   ```
   appsettings.json
   appsettings.*.json
   !appsettings.*.example.json
   ```

2. **保護您的 API Key**: 
   - 不要分享到公開場合
   - 定期輪換密鑰
   - 使用環境變數存儲

3. **文件命名**:
   - 實際配置: `appsettings.json` (不提交)
   - 示例配置: `appsettings.*.example.json` (可提交)

## 🔧 環境變數配置 (可選)

您也可以使用環境變數設置 API Key:

**Windows PowerShell**:
```powershell
$env:MONLINGO_OPENROUTER_API_KEY = "sk-or-v1-YOUR_KEY_HERE"
```

**Windows CMD**:
```cmd
set MONLINGO_OPENROUTER_API_KEY=sk-or-v1-YOUR_KEY_HERE
```

**Linux/Mac**:
```bash
export MONLINGO_OPENROUTER_API_KEY=sk-or-v1-YOUR_KEY_HERE
```

## 📊 性能對比

| 提供商 | 模型 | 速度 | 成本 | 推薦度 |
|--------|------|------|------|--------|
| OpenRouter | Gemini 2.0 Flash | ⚡⚡⚡ 1-2秒 | 💰 免費 | ⭐⭐⭐⭐⭐ |
| OpenAI | GPT-4o-mini | ⚡⚡ 2-3秒 | 💰💰 $0.15/1M tokens | ⭐⭐⭐⭐ |
| DeepSeek | DeepSeek V3 | ⚡ 7-10秒 | 💰 $0.27/1M tokens | ⭐⭐⭐ |

## 🆘 故障排除

### 錯誤: "API Key is required"
- 確認配置文件存在且格式正確
- 檢查 API Key 是否已填入
- 驗證 JSON 格式是否有效

### 錯誤: "401 Unauthorized"
- API Key 可能無效或過期
- 前往提供商網站重新生成

### 翻譯速度慢
- 建議切換到 OpenRouter + Gemini 2.0 Flash
- 檢查網絡連接
- 確認使用極簡 Prompt (已優化)

---

**需要幫助?** 請查看 [完整文檔](./Prompt優化對比_標準版vs極簡版.md) 或提交 Issue。
