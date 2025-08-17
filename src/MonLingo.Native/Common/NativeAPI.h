#pragma once
#include "../pch.h"

/// <summary>
/// MonLingo Native API 定義
/// 基於 PRD §2.3 Native.dll 架構規範
/// 完整對應 Gaminik Native.dll 的API介面
/// </summary>

namespace MonLingo {
    namespace Native {
        
        // 結果代碼定義
        enum class ResultCode : int {
            Success = 0,
            ErrorInvalidParameter = -1,
            ErrorNotSupported = -2,
            ErrorOutOfMemory = -3,
            ErrorInitializationFailed = -4,
            ErrorCaptureFailed = -5,
            ErrorOcrFailed = -6
        };

        // OCR 結果結構
        struct OcrLine {
            char* content;
            float confidence;
            int x, y, width, height;
        };

        struct OcrResult {
            OcrLine* lines;
            int lineCount;
            bool isValid;
        };
    }
}

// ===== C API 導出函數 =====
#ifdef __cplusplus
extern "C" {
#endif

// ===== 初始化和清理 API =====

/// <summary>
/// 初始化 Native 庫
/// </summary>
/// <returns>成功返回 0，失敗返回錯誤碼</returns>
MONLINGO_API int native_initialize();

/// <summary>
/// 清理 Native 庫資源
/// </summary>
MONLINGO_API void native_cleanup();

/// <summary>
/// 獲取版本資訊
/// </summary>
/// <returns>版本字串</returns>
MONLINGO_API const char* get_version();

// ===== 螢幕截圖相關 API (基於 PRD §2.3.1) =====

/// <summary>
/// 檢查系統是否支援 Windows Graphics Capture API
/// </summary>
/// <returns>true 如果支援，false 如果不支援</returns>
MONLINGO_API bool graphics_capture_is_supported();

/// <summary>
/// 對指定視窗執行單次截圖
/// 基於 PRD §3.1.3 螢幕擷取管線
/// </summary>
/// <param name="hwnd">目標視窗控制代碼</param>
/// <param name="buffer">輸出緩衝區</param>
/// <param name="bufferSize">緩衝區大小</param>
/// <param name="width">輸出影像寬度</param>
/// <param name="height">輸出影像高度</param>
/// <returns>成功返回 0，失敗返回錯誤碼</returns>
MONLINGO_API int screenshot_window_once(
    HWND hwnd, 
    unsigned char* buffer, 
    int bufferSize, 
    int* width, 
    int* height
);

/// <summary>
/// 啟動基於視窗的連續截圖會話
/// 實現 PRD §2.3.1 的非同步事件驅動模型
/// </summary>
/// <param name="hwnd">目標視窗控制代碼</param>
/// <returns>成功返回 0，失敗返回錯誤碼</returns>
MONLINGO_API int screenshot_window_loop_start(HWND hwnd);

/// <summary>
/// 從連續截圖會話讀取最新幀
/// 實現生產者-消費者模型的消費者端
/// </summary>
/// <param name="buffer">輸出緩衝區</param>
/// <param name="bufferSize">緩衝區大小</param>
/// <param name="actualSize">實際數據大小</param>
/// <param name="width">影像寬度</param>
/// <param name="height">影像高度</param>
/// <returns>成功返回 0，失敗返回錯誤碼</returns>
MONLINGO_API int screenshot_window_loop_read(
    unsigned char* buffer, 
    int bufferSize, 
    int* actualSize, 
    int* width, 
    int* height
);

/// <summary>
/// 停止連續截圖會話並清理資源
/// </summary>
/// <returns>成功返回 0，失敗返回錯誤碼</returns>
MONLINGO_API int screenshot_window_close();

// ===== OCR 相關 API (基於 PRD §2.3.2) =====

/// <summary>
/// 初始化 OCR 引擎
/// 基於 PRD §2.3.2 PaddleOCR 整合
/// </summary>
/// <param name="useModelDelayLoad">是否使用模型延遲載入</param>
/// <returns>成功返回 0，失敗返回錯誤碼</returns>
MONLINGO_API int ocr_init(bool useModelDelayLoad = true);

/// <summary>
/// 銷毀 OCR 引擎並清理資源
/// </summary>
/// <returns>成功返回 0，失敗返回錯誤碼</returns>
MONLINGO_API int ocr_destroy();

/// <summary>
/// 對影像執行完整的 OCR 處理管線
/// 基於 PRD §2.3.2 的分階段處理流程
/// </summary>
/// <param name="imageData">影像數據</param>
/// <param name="dataSize">數據大小</param>
/// <param name="width">影像寬度</param>
/// <param name="height">影像高度</param>
/// <returns>OCR 結果指標，失敗返回 nullptr</returns>
MONLINGO_API void* ocr_run_pipeline(
    const unsigned char* imageData, 
    int dataSize, 
    int width, 
    int height
);

/// <summary>
/// 獲取 OCR 結果中的行數
/// </summary>
/// <param name="result">OCR 結果指標</param>
/// <returns>行數，失敗返回 -1</returns>
MONLINGO_API int ocr_get_line_count(void* result);

/// <summary>
/// 獲取指定行的內容
/// </summary>
/// <param name="result">OCR 結果指標</param>
/// <param name="lineIndex">行索引</param>
/// <returns>行內容字串指標，需要調用 free_native_string 釋放</returns>
MONLINGO_API const char* ocr_get_line_content(void* result, int lineIndex);

/// <summary>
/// 獲取指定行的邊界框
/// </summary>
/// <param name="result">OCR 結果指標</param>
/// <param name="lineIndex">行索引</param>
/// <param name="x">輸出 X 座標</param>
/// <param name="y">輸出 Y 座標</param>
/// <param name="width">輸出寬度</param>
/// <param name="height">輸出高度</param>
/// <returns>成功返回 0，失敗返回錯誤碼</returns>
MONLINGO_API int ocr_get_line_bounding_box(
    void* result, 
    int lineIndex, 
    int* x, 
    int* y, 
    int* width, 
    int* height
);

/// <summary>
/// 獲取指定行中的單詞數量
/// </summary>
/// <param name="result">OCR 結果指標</param>
/// <param name="lineIndex">行索引</param>
/// <returns>單詞數量，失敗返回 -1</returns>
MONLINGO_API int ocr_get_line_word_count(void* result, int lineIndex);

/// <summary>
/// 獲取指定行中指定單詞的內容
/// </summary>
/// <param name="result">OCR 結果指標</param>
/// <param name="lineIndex">行索引</param>
/// <param name="wordIndex">單詞索引</param>
/// <returns>單詞內容，需要調用 free_native_string 釋放</returns>
MONLINGO_API const char* ocr_get_line_word(
    void* result, 
    int lineIndex, 
    int wordIndex
);

/// <summary>
/// 獲取指定單詞的邊界框
/// </summary>
/// <param name="result">OCR 結果指標</param>
/// <param name="lineIndex">行索引</param>
/// <param name="wordIndex">單詞索引</param>
/// <param name="x">輸出 X 座標</param>
/// <param name="y">輸出 Y 座標</param>
/// <param name="width">輸出寬度</param>
/// <param name="height">輸出高度</param>
/// <returns>成功返回 0，失敗返回錯誤碼</returns>
MONLINGO_API int ocr_get_word_bounding_box(
    void* result, 
    int lineIndex, 
    int wordIndex, 
    int* x, 
    int* y, 
    int* width, 
    int* height
);

/// <summary>
/// 釋放 OCR 結果
/// </summary>
/// <param name="result">OCR 結果指標</param>
/// <returns>成功返回 0，失敗返回錯誤碼</returns>
MONLINGO_API int ocr_release_result(void* result);

// ===== 安全與加密 API (基於 PRD §2.3.6) =====

/// <summary>
/// 獲取簽章字串用於完整性檢查
/// </summary>
/// <param name="data">輸入數據</param>
/// <param name="dataSize">數據大小</param>
/// <returns>簽章字串，需要調用 free_native_string 釋放</returns>
MONLINGO_API const char* get_sign_str(const unsigned char* data, int dataSize);

/// <summary>
/// Agent 編碼函式
/// </summary>
/// <param name="input">輸入字串</param>
/// <param name="key">編碼密鑰</param>
/// <returns>編碼結果，需要調用 free_native_string 釋放</returns>
MONLINGO_API const char* agent_encode(const char* input, const char* key);

/// <summary>
/// Agent 解碼函式
/// </summary>
/// <param name="input">編碼字串</param>
/// <param name="key">解碼密鑰</param>
/// <returns>解碼結果，需要調用 free_native_string 釋放</returns>
MONLINGO_API const char* agent_decode(const char* input, const char* key);

/// <summary>
/// ITS 編碼函式 (內部傳輸安全)
/// </summary>
/// <param name="input">輸入字串</param>
/// <param name="key">編碼密鑰</param>
/// <returns>編碼結果，需要調用 free_native_string 釋放</returns>
MONLINGO_API const char* its_encode(const char* input, const char* key);

/// <summary>
/// ITS 解碼函式 (內部傳輸安全)
/// </summary>
/// <param name="input">編碼字串</param>
/// <param name="key">解碼密鑰</param>
/// <returns>解碼結果，需要調用 free_native_string 釋放</returns>
MONLINGO_API const char* its_decode(const char* input, const char* key);

// ===== 記憶體管理 API =====

/// <summary>
/// 釋放由 Native API 分配的字串記憶體
/// 所有返回 const char* 的函式都需要調用此函式釋放記憶體
/// </summary>
/// <param name="str">要釋放的字串指標</param>
MONLINGO_API void free_native_string(const char* str);

#ifdef __cplusplus
}
#endif
