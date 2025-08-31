# MonLingo Models Directory
# 這個資料夾包含 PaddleOCR 模型檔案
# 大型模型檔案不會被 Git 追蹤，但資料夾結構會被保護

## 模型下載
使用以下腳本下載模型：
- `scripts/setup_ppocr_v5.ps1` - PowerShell 版本
- `scripts/setup_ppocr_v5_auto.py` - Python 版本

## 模型檔案結構
```
models/
├── ppocr_keys.txt                    # 字符集檔案
├── ch_PP-OCRv5_det_infer/           # 檢測模型 (v5)
├── ch_PP-OCRv5_rec_infer/           # 識別模型 (v5)  
├── ch_PP-OCRv3_det_infer/           # 檢測模型 (v3 備用)
├── ch_PP-OCRv3_rec_infer/           # 識別模型 (v3 備用)
└── ch_ppocr_mobile_v2.0_cls_infer/  # 分類模型
```

## 重要提醒
- 此 README.md 檔案確保 models/ 資料夾不會被 git reset --hard 刪除
- 模型檔案在 .gitignore 中被忽略以節省倉庫空間
- 在新環境中請重新下載模型檔案
