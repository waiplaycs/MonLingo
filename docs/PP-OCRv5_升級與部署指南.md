# PP-OCRv5 升級與部署指南

本指南說明如何在 MonLingo 中使用 PP-OCRv5 模型（檢測與識別），以及當前程式如何自動部署模型。

## 一、模型放置位置

將以下資料夾與字典檔放入專案根目錄下的 `models/` 目錄（`.gitignore` 已忽略，不會進 Git）：

- models/
  - ch_PP-OCRv5_det_infer/
  - ch_PP-OCRv5_rec_infer/
  - ch_ppocr_mobile_v2.0_cls_infer/ （可選，分類模型）
  - ppocr_keys.txt 或 ppocr_keys_v1.txt

說明：
- det：文字檢測模型（v5）
- rec：文字識別模型（v5）
- cls：角度/分類模型（仍採用 v2.0 版本，若有可放入）
- 字典：ppocr_keys.txt（若使用 v1 亦可）

## 二、自動部署機制

`RealOcrService.InitializeAsync()` 會在初始化前自動執行：
- 探測 `models/` 目錄下是否存在 `ch_PP-OCRv5_det_infer` 與 `ch_PP-OCRv5_rec_infer`。
- 若存在，將這些模型資料夾（以及 `ppocr_keys.txt` 和可選的 `ch_ppocr_mobile_v2.0_cls_infer`）複製到執行目錄下的 `inference/` 資料夾。
- PaddleOCRSharp 會從 `inference/` 讀取模型並載入。

這樣可避免把大型模型檔加入版本控制，同時在本機或 CI/部署時透過放置 `models/` 完成模型切換。

環境變數覆蓋：
- 也可設置 `MONLINGO_MODELS_DIR` 指向模型所在資料夾，程式會優先使用該路徑。

## 三、套件版本

已將 `Directory.Packages.props` 中的 `PaddleOCRSharp` 升級到 `3.0.0` 以對齊解析版本並提升相容性。

## 四、取得 PP-OCRv5 模型

可從 PaddleOCR 官方來源取得 v5 模型。建議保存為上述資料夾名稱：
- ch_PP-OCRv5_det_infer
- ch_PP-OCRv5_rec_infer

（官方下載鏈接可能會更新，請參考 PaddleOCR 官方 README 或發行頁面。若無法直接存取，可透過 ModelScope/HuggingFace 鏡像取得。）

## 五、常見問題

- 啟動後仍載入舊模型？
  - 確認 `inference/` 目錄下的模型資料夾為 v5 名稱且檔案齊全。
  - 刪除舊的 `inference/` 內容後重新執行。
- 模型字典檔缺失？
  - 將 `ppocr_keys.txt` 放入 `models/`，程式會自動複製到 `inference/`。
- GPU 啟用？
  - 目前程式以 CPU 預設；可在 `RealOcrService` 的 `OCRParameter` 調整 `use_gpu` 與其他參數。

## 六、驗證

- 建置並執行應用，第一次初始化時日誌中應看到「已部署 PP-OCRv5 模型至執行目錄的 inference 資料夾」。
- 進行簡單 OCR 測試，確認精度與速度符合預期。
