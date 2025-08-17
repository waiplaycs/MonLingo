#include "pch.h"
#include "Common/NativeAPI.h"
#include "Common/Logger.h"
#include "ScreenCapture/ScreenCaptureEngine.h"
#include "OCR/OcrEngine.h"

// 全局變量
static bool g_isInitialized = false;
static std::unique_ptr<MonLingo::Native::ScreenCaptureEngine> g_screenEngine;
static std::unique_ptr<MonLingo::Native::OcrEngine> g_ocrEngine;

using namespace MonLingo::Native;

// ===== 生命週期管理 =====

extern "C" MONLINGO_API int native_initialize()
{
    try {
        if (g_isInitialized) {
            Logger::Warning("Native library already initialized");
            return static_cast<int>(ResultCode::Success);
        }

        // 初始化日誌系統
        Logger::Initialize();
        Logger::Info("MonLingo Native library initializing...");

        // 初始化螢幕截圖引擎
        g_screenEngine = std::make_unique<ScreenCaptureEngine>();
        
        // 初始化OCR引擎
        g_ocrEngine = std::make_unique<OcrEngine>();

        g_isInitialized = true;
        Logger::Info("MonLingo Native library initialized successfully");
        
        return static_cast<int>(ResultCode::Success);
    }
    catch (const std::exception& e) {
        Logger::Error("Failed to initialize native library");
        return static_cast<int>(ResultCode::ErrorInitializationFailed);
    }
}

extern "C" MONLINGO_API void native_cleanup()
{
    try {
        if (!g_isInitialized) {
            return;
        }

        Logger::Info("MonLingo Native library cleaning up...");
        
        // 清理資源
        g_ocrEngine.reset();
        g_screenEngine.reset();
        
        g_isInitialized = false;
        Logger::Info("MonLingo Native library cleanup completed");
        Logger::Cleanup();
    }
    catch (const std::exception& e) {
        // 忽略清理過程中的錯誤
    }
}

extern "C" MONLINGO_API const char* get_version()
{
    return "1.0.0";
}

// ===== 螢幕截圖 API =====

extern "C" MONLINGO_API bool graphics_capture_is_supported()
{
    try {
        return ScreenCaptureEngine::IsSupported();
    }
    catch (const std::exception& e) {
        Logger::Error("Error checking graphics capture support");
        return false;
    }
}

extern "C" MONLINGO_API int screenshot_window_once(void* windowHandle, unsigned char** imageData, int* dataSize, int* width, int* height)
{
    try {
        if (!g_isInitialized || !g_screenEngine) {
            Logger::Error("Native library not initialized");
            return static_cast<int>(ResultCode::ErrorInitializationFailed);
        }

        if (!windowHandle || !imageData || !dataSize || !width || !height) {
            Logger::Error("Invalid parameters for screenshot_window_once");
            return static_cast<int>(ResultCode::ErrorInvalidParameter);
        }

        return g_screenEngine->CaptureWindow(
            static_cast<HWND>(windowHandle), 
            imageData, 
            dataSize, 
            width, 
            height
        );
    }
    catch (const std::exception& e) {
        Logger::Error("Error in screenshot_window_once");
        return static_cast<int>(ResultCode::ErrorCaptureFailed);
    }
}

extern "C" MONLINGO_API int screenshot_window_loop_start(void* windowHandle, int intervalMs)
{
    try {
        if (!g_isInitialized || !g_screenEngine) {
            Logger::Error("Native library not initialized");
            return static_cast<int>(ResultCode::ErrorInitializationFailed);
        }

        if (!windowHandle || intervalMs <= 0) {
            Logger::Error("Invalid parameters for screenshot_window_loop_start");
            return static_cast<int>(ResultCode::ErrorInvalidParameter);
        }

        return g_screenEngine->StartContinuousCapture(
            static_cast<HWND>(windowHandle), 
            intervalMs
        );
    }
    catch (const std::exception& e) {
        Logger::Error("Error in screenshot_window_loop_start");
        return static_cast<int>(ResultCode::ErrorCaptureFailed);
    }
}

extern "C" MONLINGO_API int screenshot_window_loop_read(unsigned char** imageData, int* dataSize, int* width, int* height)
{
    try {
        if (!g_isInitialized || !g_screenEngine) {
            Logger::Error("Native library not initialized");
            return static_cast<int>(ResultCode::ErrorInitializationFailed);
        }

        if (!imageData || !dataSize || !width || !height) {
            Logger::Error("Invalid parameters for screenshot_window_loop_read");
            return static_cast<int>(ResultCode::ErrorInvalidParameter);
        }

        return g_screenEngine->ReadLatestCapture(
            imageData, 
            dataSize, 
            width, 
            height
        );
    }
    catch (const std::exception& e) {
        Logger::Error("Error in screenshot_window_loop_read");
        return static_cast<int>(ResultCode::ErrorCaptureFailed);
    }
}

extern "C" MONLINGO_API int screenshot_window_loop_stop()
{
    try {
        if (!g_isInitialized || !g_screenEngine) {
            return static_cast<int>(ResultCode::Success);
        }

        return g_screenEngine->StopContinuousCapture();
    }
    catch (const std::exception& e) {
        Logger::Error("Error in screenshot_window_loop_stop");
        return static_cast<int>(ResultCode::ErrorCaptureFailed);
    }
}

extern "C" MONLINGO_API void free_image_data(unsigned char* imageData)
{
    if (imageData) {
        delete[] imageData;
    }
}

// ===== OCR API =====

extern "C" MONLINGO_API int ocr_image_data(const unsigned char* imageData, int dataSize, int width, int height, OcrResult** result)
{
    try {
        if (!g_isInitialized || !g_ocrEngine) {
            Logger::Error("Native library not initialized");
            return static_cast<int>(ResultCode::ErrorInitializationFailed);
        }

        if (!imageData || dataSize <= 0 || width <= 0 || height <= 0 || !result) {
            Logger::Error("Invalid parameters for ocr_image_data");
            return static_cast<int>(ResultCode::ErrorInvalidParameter);
        }

        return g_ocrEngine->ProcessImage(imageData, dataSize, width, height, result);
    }
    catch (const std::exception& e) {
        Logger::Error("Error in ocr_image_data");
        return static_cast<int>(ResultCode::ErrorOcrFailed);
    }
}

extern "C" MONLINGO_API int ocr_screen_region(int x, int y, int width, int height, OcrResult** result)
{
    try {
        if (!g_isInitialized || !g_ocrEngine) {
            Logger::Error("Native library not initialized");
            return static_cast<int>(ResultCode::ErrorInitializationFailed);
        }

        if (width <= 0 || height <= 0 || !result) {
            Logger::Error("Invalid parameters for ocr_screen_region");
            return static_cast<int>(ResultCode::ErrorInvalidParameter);
        }

        return g_ocrEngine->ProcessScreenRegion(x, y, width, height, result);
    }
    catch (const std::exception& e) {
        Logger::Error("Error in ocr_screen_region");
        return static_cast<int>(ResultCode::ErrorOcrFailed);
    }
}

extern "C" MONLINGO_API void free_ocr_result(OcrResult* result)
{
    if (result) {
        if (result->lines) {
            for (int i = 0; i < result->lineCount; i++) {
                if (result->lines[i].content) {
                    delete[] result->lines[i].content;
                }
            }
            delete[] result->lines;
        }
        delete result;
    }
}

// ===== 視窗API =====

extern "C" MONLINGO_API int find_window_by_title(const char* windowTitle, void** windowHandle)
{
    try {
        if (!windowTitle || !windowHandle) {
            Logger::Error("Invalid parameters for find_window_by_title");
            return static_cast<int>(ResultCode::ErrorInvalidParameter);
        }

        HWND hwnd = FindWindowA(nullptr, windowTitle);
        if (hwnd == nullptr) {
            return static_cast<int>(ResultCode::ErrorCaptureFailed);
        }

        *windowHandle = hwnd;
        return static_cast<int>(ResultCode::Success);
    }
    catch (const std::exception& e) {
        Logger::Error("Error in find_window_by_title");
        return static_cast<int>(ResultCode::ErrorCaptureFailed);
    }
}

extern "C" MONLINGO_API int find_window_by_class(const char* className, void** windowHandle)
{
    try {
        if (!className || !windowHandle) {
            Logger::Error("Invalid parameters for find_window_by_class");
            return static_cast<int>(ResultCode::ErrorInvalidParameter);
        }

        HWND hwnd = FindWindowA(className, nullptr);
        if (hwnd == nullptr) {
            return static_cast<int>(ResultCode::ErrorCaptureFailed);
        }

        *windowHandle = hwnd;
        return static_cast<int>(ResultCode::Success);
    }
    catch (const std::exception& e) {
        Logger::Error("Error in find_window_by_class");
        return static_cast<int>(ResultCode::ErrorCaptureFailed);
    }
}

extern "C" MONLINGO_API int get_window_rect(void* windowHandle, int* left, int* top, int* right, int* bottom)
{
    try {
        if (!windowHandle || !left || !top || !right || !bottom) {
            Logger::Error("Invalid parameters for get_window_rect");
            return static_cast<int>(ResultCode::ErrorInvalidParameter);
        }

        RECT rect;
        if (!GetWindowRect(static_cast<HWND>(windowHandle), &rect)) {
            Logger::Error("Failed to get window rect");
            return static_cast<int>(ResultCode::ErrorCaptureFailed);
        }

        *left = rect.left;
        *top = rect.top;
        *right = rect.right;
        *bottom = rect.bottom;

        return static_cast<int>(ResultCode::Success);
    }
    catch (const std::exception& e) {
        Logger::Error("Error in get_window_rect");
        return static_cast<int>(ResultCode::ErrorCaptureFailed);
    }
}

extern "C" MONLINGO_API int is_window_visible(void* windowHandle, bool* isVisible)
{
    try {
        if (!windowHandle || !isVisible) {
            Logger::Error("Invalid parameters for is_window_visible");
            return static_cast<int>(ResultCode::ErrorInvalidParameter);
        }

        *isVisible = IsWindowVisible(static_cast<HWND>(windowHandle)) != 0;
        return static_cast<int>(ResultCode::Success);
    }
    catch (const std::exception& e) {
        Logger::Error("Error in is_window_visible");
        return static_cast<int>(ResultCode::ErrorCaptureFailed);
    }
}

// ===== 系統資訊 API =====

extern "C" MONLINGO_API int get_system_info(const char** osVersion, const char** architecture)
{
    try {
        if (!osVersion || !architecture) {
            Logger::Error("Invalid parameters for get_system_info");
            return static_cast<int>(ResultCode::ErrorInvalidParameter);
        }

        static std::string os_ver = "Windows 10+";
        static std::string arch = "x64";

        *osVersion = os_ver.c_str();
        *architecture = arch.c_str();

        return static_cast<int>(ResultCode::Success);
    }
    catch (const std::exception& e) {
        Logger::Error("Error in get_system_info");
        return static_cast<int>(ResultCode::ErrorCaptureFailed);
    }
}

extern "C" MONLINGO_API void debug_output(const char* message)
{
    if (message && g_isInitialized) {
        Logger::Debug(message);
    }
}
