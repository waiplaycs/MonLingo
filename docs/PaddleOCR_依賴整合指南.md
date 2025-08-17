# PaddleOCR C++ 依賴整合指南

## 概況
本文檔描述如何將 PaddleOCR C++ 推理庫整合到 MonLingo.Native 專案中。

## 依賴項目

### 1. PaddlePaddle Inference C++
- **版本**: 2.5.1 或更高
- **平台**: Windows x64
- **下載地址**: https://www.paddlepaddle.org.cn/inference/master/guides/install/download_lib.html#windows
- **檔案名**: paddle_inference-win-x64-2.5.1.zip (~150MB)

### 2. OpenCV (已整合)
- **版本**: 4.8.0+
- **狀態**: ✅ 已配置
- **用途**: 圖像預處理和格式轉換

### 3. Visual C++ Redistributable
- **版本**: 2019 或 2022
- **狀態**: ⚠️ 運行時依賴
- **用途**: PaddleOCR 運行時庫支援

## 整合步驟

### 步驟 1: 下載 Paddle Inference 庫
```powershell
# 建立依賴目錄
New-Item -ItemType Directory -Path "third_party\paddle_inference" -Force

# 下載並解壓 Paddle Inference (手動執行)
# 1. 訪問 https://www.paddlepaddle.org.cn/inference/master/guides/install/download_lib.html#windows
# 2. 下載 paddle_inference-win-x64-2.5.1.zip
# 3. 解壓到 third_party\paddle_inference\
```

### 步驟 2: 目錄結構配置
```
MonLingo2/
├── third_party/
│   ├── paddle_inference/
│   │   ├── paddle/
│   │   │   ├── include/          # 標頭檔目錄
│   │   │   │   ├── paddle_inference_api.h
│   │   │   │   └── ...
│   │   │   └── lib/             # 庫文件目錄
│   │   │       ├── paddle_inference.lib
│   │   │       ├── paddle_inference.dll
│   │   │       └── ...
│   │   └── third_party/         # 第三方依賴
│   └── models/                  # PaddleOCR 模型目錄
│       ├── ch_PP-OCRv4_det_infer/
│       ├── ch_PP-OCRv4_rec_infer/
│       └── ...
```

### 步驟 3: Visual Studio 專案配置

#### 3.1 包含目錄設置
```xml
<!-- 在 MonLingo.Native.vcxproj 中添加 -->
<AdditionalIncludeDirectories>
  $(ProjectDir);
  $(SolutionDir)third_party\paddle_inference\paddle\include;
  %(AdditionalIncludeDirectories)
</AdditionalIncludeDirectories>
```

#### 3.2 庫目錄設置
```xml
<AdditionalLibraryDirectories>
  $(SolutionDir)third_party\paddle_inference\paddle\lib;
  %(AdditionalLibraryDirectories)
</AdditionalLibraryDirectories>
```

#### 3.3 連結庫設置
```xml
<AdditionalDependencies>
  d3d11.lib;
  dxgi.lib;
  windowsapp.lib;
  paddle_inference.lib;
  %(AdditionalDependencies)
</AdditionalDependencies>
```

### 步驟 4: 運行時 DLL 配置

#### 4.1 複製 DLL 到輸出目錄
在專案的後置建置事件中添加：
```batch
xcopy "$(SolutionDir)third_party\paddle_inference\paddle\lib\*.dll" "$(OutDir)" /Y /D
```

#### 4.2 相關 DLL 清單
- `paddle_inference.dll` - 主要推理庫
- `mklml.dll` - Intel MKL 數學庫
- `libiomp5md.dll` - OpenMP 運行時
- 其他可能的依賴 DLL

### 步驟 5: 代碼實現

#### 5.1 標頭檔包含
```cpp
// OcrEngine.h
#include <paddle_inference_api.h>
using namespace paddle_infer;
```

#### 5.2 配置結構體
```cpp
// 在 OcrEngine.h 的私有成員中添加
std::unique_ptr<Predictor> m_detPredictor;    // 檢測模型
std::unique_ptr<Predictor> m_recPredictor;    // 識別模型
std::unique_ptr<Predictor> m_clsPredictor;    // 分類模型
```

#### 5.3 初始化實現
```cpp
void OcrEngine::InitializePaddleOCR() {
    // 檢測模型配置
    Config detConfig;
    detConfig.SetModel(m_detModelPath + "/inference.pdmodel",
                      m_detModelPath + "/inference.pdiparams");
    detConfig.EnableUseGpu(0, 500);  // 或 DisableGpu() 使用 CPU
    m_detPredictor = CreatePredictor(detConfig);
    
    // 識別模型配置
    Config recConfig;
    recConfig.SetModel(m_recModelPath + "/inference.pdmodel",
                      m_recModelPath + "/inference.pdiparams");
    recConfig.EnableUseGpu(0, 500);
    m_recPredictor = CreatePredictor(recConfig);
    
    // 分類模型配置
    Config clsConfig;
    clsConfig.SetModel(m_clsModelPath + "/inference.pdmodel",
                      m_clsModelPath + "/inference.pdiparams");
    clsConfig.EnableUseGpu(0, 500);
    m_clsPredictor = CreatePredictor(clsConfig);
}
```

## 測試驗證

### 1. 編譯測試
```batch
# 編譯專案
msbuild MonLingo.sln /p:Configuration=Release /p:Platform=x64
```

### 2. 運行測試
```cpp
// 基本功能測試
OcrEngine engine;
// ... 測試代碼
```

### 3. 效能基準
- **目標**: < 500ms 單張圖像處理時間
- **記憶體**: < 1GB 峰值使用量
- **準確性**: F1 ≥ 0.90

## 常見問題

### Q1: 編譯錯誤 "無法找到 paddle_inference_api.h"
**解決方案**: 檢查包含目錄路徑是否正確配置

### Q2: 連結錯誤 "無法解析的外部符號"
**解決方案**: 確認 paddle_inference.lib 已正確加入連結器依賴

### Q3: 運行時錯誤 "找不到 DLL"
**解決方案**: 確認所有必要的 DLL 都已複製到輸出目錄

### Q4: GPU 初始化失敗
**解決方案**: 改用 CPU 模式或檢查 CUDA 版本相容性

## 更新檢查清單

- [ ] 下載 Paddle Inference 庫
- [ ] 配置 Visual Studio 專案設置
- [ ] 實現 PaddleOCR 初始化代碼
- [ ] 實現檢測和識別 API 調用
- [ ] 配置運行時 DLL 複製
- [ ] 執行編譯和測試驗證
- [ ] 效能基準測試
- [ ] 文檔更新

---
**建立時間**: 2024-08-20  
**維護者**: MonLingo 開發團隊  
**更新頻率**: 依需求更新
