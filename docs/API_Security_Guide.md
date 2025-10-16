# 🔒 API 密鑰安全配置指南

## ⚠️ 重要安全提醒

**永遠不要將真實的 API 密鑰提交到 Git 倉庫!**

---

## 📝 配置方法

### 方法 1: 使用本地配置文件 (推薦)

1. 複製示例文件:
   ```bash
   copy appsettings.deepseek.example.json appsettings.local.json
   ```

2. 編輯 `appsettings.local.json`,填入你的 API 密鑰:
   ```json
   {
     "AITranslation": {
       "ApiKey": "sk-your-real-api-key-here"
     }
   }
   ```

3. ✅ 這個文件已加入 `.gitignore`,不會被提交到 Git

---

### 方法 2: 使用環境變數

設置環境變數 (Windows PowerShell):
```powershell
$env:MONLINGO_DEEPSEEK_API_KEY = "sk-your-real-api-key-here"
```

設置環境變數 (Windows CMD):
```cmd
set MONLINGO_DEEPSEEK_API_KEY=sk-your-real-api-key-here
```

設置環境變數 (Linux/Mac):
```bash
export MONLINGO_DEEPSEEK_API_KEY="sk-your-real-api-key-here"
```

---

## 🔐 如果 API 密鑰已經暴露

### 立即採取的步驟:

1. **撤銷暴露的密鑰**
   - 登入 DeepSeek 控制台: https://platform.deepseek.com/api_keys
   - 刪除暴露的密鑰
   - 生成新的密鑰

2. **從倉庫中移除密鑰**
   ```bash
   # 從配置文件中清空密鑰
   # 提交修復
   git add .
   git commit -m "security: 移除暴露的 API 密鑰"
   git push
   ```

3. **清理 Git 歷史 (可選但推薦)**
   
   如果密鑰已經在多個提交中:
   ```bash
   # 使用 git-filter-repo (推薦)
   pip install git-filter-repo
   git filter-repo --path src/MonLingo.Core/appsettings.json --invert-paths
   
   # 或使用 BFG Repo-Cleaner
   java -jar bfg.jar --replace-text passwords.txt
   ```

4. **強制推送 (⚠️ 危險操作)**
   ```bash
   git push --force
   ```
   
   ⚠️ 注意: 這會重寫歷史,影響所有協作者

---

## 📂 文件說明

| 文件 | 用途 | Git 追蹤 |
|------|------|----------|
| `appsettings.json` | 公開配置模板 | ✅ 是 (不包含密鑰) |
| `appsettings.deepseek.example.json` | DeepSeek 配置示例 | ✅ 是 |
| `appsettings.local.json` | 本地密鑰配置 | ❌ 否 (.gitignore) |
| `appsettings.*.local.json` | 其他本地配置 | ❌ 否 (.gitignore) |

---

## ✅ 最佳實踐

1. ✅ 使用 `appsettings.local.json` 存儲敏感信息
2. ✅ 在 `appsettings.json` 中使用空字符串作為默認值
3. ✅ 定期輪換 API 密鑰
4. ✅ 為不同環境使用不同的密鑰
5. ✅ 設置 API 密鑰的使用配額
6. ✅ 監控 API 使用情況

---

## 🔍 檢查密鑰是否暴露

在提交前檢查:
```bash
# 檢查暫存區
git diff --cached | grep -i "sk-"

# 檢查所有文件
grep -r "sk-" --include="*.json" --include="*.config"
```

---

## 📞 獲取幫助

- DeepSeek API 文檔: https://platform.deepseek.com/docs
- DeepSeek 控制台: https://platform.deepseek.com/api_keys
- 如有疑問,請查閱 MonLingo 官方文檔
