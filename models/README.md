# MonLingo Models Directory
# 這個資料夾包含 PaddleOCR 模型檔案
# 大型模型檔案不會被 Git 追蹤，但資料夾結構會被保護

## ✅ PP-OCRv5 模型已成功安裝！

本專案現在使用最新的 **PP-OCRv5** 模型，具有以下優勢：
- 📈 相比 PP-OCRv4 準確率提升 13%
- 🌐 支援簡體中文、繁體中文、英文、日文、拼音
- 🔥 更強的多語言混合文檔識別能力
- ⚡ 更好的推理性能和魯棒性

## 模型檔案結構

```
models/
├── ppocr_keys.txt                    # 字符集檔案
├── ch_PP-OCRv5_det_infer/           # **PP-OCRv5 檢測模型（最新）**
├── ch_PP-OCRv5_rec_infer/           # **PP-OCRv5 識別模型（最新）**
├── ch_PP-OCRv3_det_infer/           # PP-OCRv3 檢測模型（備用）
├── ch_PP-OCRv3_rec_infer/           # PP-OCRv3 識別模型（備用）
└── ch_ppocr_mobile_v2.0_cls_infer/  # 文本方向分類模型
```

## 模型版本資訊

### 🚀 PP-OCRv5 (推薦使用)
- **版本時間**: 2025年6月
- **模型檔案**: 87MB (det) + 84MB (rec)
- **特點**: 最新算法，最高準確率
- **來源**: PaddleOCR 3.x 官方緩存

### 🔄 PP-OCRv3 (備用)
- **版本時間**: 2022年5月
- **模型檔案**: 較小但性能較低
- **用途**: 備用或特殊場景

## 模型下載方式

模型會透過以下方式自動管理：
1. 使用 PaddleOCR API 初始化時自動下載到系統緩存
2. 複製到本地 models/ 目錄供 MonLingo 使用
3. Git 保護目錄結構但忽略大型模型文件

## 重要提醒
- ✅ 此 README.md 檔案確保 models/ 資料夾不會被 git reset --hard 刪除
- 🚫 模型檔案在 .gitignore 中被忽略以節省倉庫空間
- 🔄 在新環境中模型會自動重新下載
- 🎯 MonLingo 應用程式已配置使用 PP-OCRv5 模型
