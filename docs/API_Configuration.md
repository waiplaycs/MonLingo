# AI翻譯服務配置說明

## 配置API密鑰

### 方式1: 使用appsettings.json (推薦用於開發環境)

編輯 `src/MonLingo.Core/appsettings.json`:

```json
{
  "AITranslation": {
    "ApiKey": "sk-proj-xxxxxxxxxxxxxxxxxxxxxxxx"
  }
}
```

### 方式2: 使用環境變數 (推薦用於生產環境)

設置環境變數:

**Windows PowerShell:**
```powershell
$env:MONLINGO_OPENAI_API_KEY = "sk-proj-xxxxxxxxxxxxxxxxxxxxxxxx"
```

**Windows CMD:**
```cmd
set MONLINGO_OPENAI_API_KEY=sk-proj-xxxxxxxxxxxxxxxxxxxxxxxx
```

**Linux/macOS:**
```bash
export MONLINGO_OPENAI_API_KEY="sk-proj-xxxxxxxxxxxxxxxxxxxxxxxx"
```

### 方式3: 使用用戶機密 (開發環境最佳實踐)

```bash
# 初始化用戶機密
dotnet user-secrets init --project src/MonLingo.Core

# 設置API密鑰
dotnet user-secrets set "AITranslation:ApiKey" "sk-proj-xxxxxxxxxxxxxxxxxxxxxxxx" --project src/MonLingo.Core
```

## 獲取OpenAI API密鑰

1. 訪問 https://platform.openai.com/api-keys
2. 登錄您的OpenAI帳戶
3. 點擊 "Create new secret key"
4. 複製生成的API密鑰
5. 按照上述方式配置到MonLingo中

## 配置參數說明

| 參數 | 默認值 | 說明 |
|------|--------|------|
| `Provider` | OpenAI | AI提供商(OpenAI/Azure/Claude) |
| `Model` | gpt-4o-mini | 使用的模型名稱 |
| `ApiKey` | - | API密鑰(必需) |
| `Temperature` | 0.3 | 創造性參數(0.0-2.0，越低越確定) |
| `MaxTokens` | 2000 | 最大輸出tokens |
| `MaxRetries` | 3 | API失敗重試次數 |
| `EnableCache` | true | 啟用結果緩存 |
| `DailyQuotaLimit` | 1000 | 每日調用配額限制 |

## 成本估算

使用 GPT-4o-mini:
- **定價**: $0.150/1M input tokens, $0.600/1M output tokens
- **單次翻譯**: ~$0.0006 (假設300 input + 200 output tokens)
- **每日100次**: ~$0.06/天 = $1.80/月
- **每日1000次**: ~$0.60/天 = $18/月

## 安全建議

⚠️ **重要**: 
- ❌ 不要將API密鑰提交到版本控制系統
- ✅ 使用環境變數或用戶機密
- ✅ 定期輪換API密鑰
- ✅ 設置合理的每日配額限制
- ✅ 監控API使用量和成本

## 故障排查

### 問題: "API密鑰未配置"
**解決**: 確保已按照上述方式之一配置API密鑰

### 問題: "API調用失敗 401 Unauthorized"
**解決**: 檢查API密鑰是否正確，是否已過期

### 問題: "超出每日配額限制"
**解決**: 調整 `DailyQuotaLimit` 配置或等待次日重置

### 問題: "API調用超時"
**解決**: 增加 `TimeoutSeconds` 配置值
