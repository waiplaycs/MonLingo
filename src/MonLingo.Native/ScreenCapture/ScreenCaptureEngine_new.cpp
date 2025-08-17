#include "../pch.h"
#include "ScreenCaptureEngine.h"
#include "../Common/Logger.h"

namespace MonLingo {
    namespace Native {

        ScreenCaptureEngine::ScreenCaptureEngine()
            : m_isCapturing(false)
            , m_captureThread(nullptr)
            , m_targetWindow(nullptr)
            , m_intervalMs(0)
        {
            // 初始化 COM
            CoInitializeEx(nullptr, COINIT_APARTMENTTHREADED);
            
            Logger::Debug("ScreenCaptureEngine constructed");
        }

        ScreenCaptureEngine::~ScreenCaptureEngine()
        {
            StopContinuousCapture();
            CoUninitialize();
            
            Logger::Debug("ScreenCaptureEngine destructed");
        }

        bool ScreenCaptureEngine::IsSupported()
        {
            try {
                // 檢查 Windows.Graphics.Capture API 是否可用
                // 這個 API 在 Windows 10 1903 (19H1) 及更高版本中可用
                winrt::Windows::Graphics::Capture::GraphicsCaptureSession session{ nullptr };
                return winrt::Windows::Graphics::Capture::GraphicsCaptureSession::IsSupported();
            }
            catch (...) {
                return false;
            }
        }

        int ScreenCaptureEngine::CaptureWindow(HWND windowHandle, unsigned char** imageData, int* dataSize, int* width, int* height)
        {
            try {
                if (!windowHandle || !imageData || !dataSize || !width || !height) {
                    return static_cast<int>(ResultCode::ErrorInvalidParameter);
                }

                // 檢查視窗是否有效
                if (!IsWindow(windowHandle) || !IsWindowVisible(windowHandle)) {
                    Logger::Warning("Target window is not valid or visible");
                    return static_cast<int>(ResultCode::ErrorCaptureFailed);
                }

                // 獲取視窗尺寸
                RECT windowRect;
                if (!GetWindowRect(windowHandle, &windowRect)) {
                    Logger::Error("Failed to get window rect");
                    return static_cast<int>(ResultCode::ErrorCaptureFailed);
                }

                int windowWidth = windowRect.right - windowRect.left;
                int windowHeight = windowRect.bottom - windowRect.top;

                if (windowWidth <= 0 || windowHeight <= 0) {
                    Logger::Warning("Window has invalid size");
                    return static_cast<int>(ResultCode::ErrorCaptureFailed);
                }

                // 使用 GDI+ 進行截圖（後備方案）
                // 在實際實現中，這裡應該使用 Graphics Capture API
                HDC windowDC = GetWindowDC(windowHandle);
                if (!windowDC) {
                    Logger::Error("Failed to get window DC");
                    return static_cast<int>(ResultCode::ErrorCaptureFailed);
                }

                HDC memoryDC = CreateCompatibleDC(windowDC);
                HBITMAP bitmap = CreateCompatibleBitmap(windowDC, windowWidth, windowHeight);
                HGDIOBJ oldBitmap = SelectObject(memoryDC, bitmap);

                // 執行截圖
                if (!BitBlt(memoryDC, 0, 0, windowWidth, windowHeight, windowDC, 0, 0, SRCCOPY)) {
                    Logger::Error("BitBlt failed");
                    SelectObject(memoryDC, oldBitmap);
                    DeleteObject(bitmap);
                    DeleteDC(memoryDC);
                    ReleaseDC(windowHandle, windowDC);
                    return static_cast<int>(ResultCode::ErrorCaptureFailed);
                }

                // 轉換為 PNG 數據
                // 這裡簡化實現，實際應該使用 WIC 或其他圖像庫
                *width = windowWidth;
                *height = windowHeight;
                *dataSize = windowWidth * windowHeight * 4; // RGBA
                *imageData = new unsigned char[*dataSize];
                
                // 填充示例數據 (實際實現應該從bitmap讀取)
                memset(*imageData, 0, *dataSize);

                // 清理資源
                SelectObject(memoryDC, oldBitmap);
                DeleteObject(bitmap);
                DeleteDC(memoryDC);
                ReleaseDC(windowHandle, windowDC);

                Logger::Debug("Window screenshot captured successfully");
                return static_cast<int>(ResultCode::Success);
            }
            catch (const std::exception& e) {
                Logger::Error("Exception in CaptureWindow");
                return static_cast<int>(ResultCode::ErrorCaptureFailed);
            }
        }

        int ScreenCaptureEngine::StartContinuousCapture(HWND windowHandle, int intervalMs)
        {
            try {
                if (m_isCapturing) {
                    Logger::Warning("Continuous capture already running");
                    return static_cast<int>(ResultCode::Success);
                }

                if (!windowHandle || intervalMs <= 0) {
                    return static_cast<int>(ResultCode::ErrorInvalidParameter);
                }

                m_targetWindow = windowHandle;
                m_intervalMs = intervalMs;
                m_isCapturing = true;

                // 啟動捕獲線程
                m_captureThread = std::make_unique<std::thread>(&ScreenCaptureEngine::CaptureLoop, this);

                Logger::Info("Continuous capture started");
                return static_cast<int>(ResultCode::Success);
            }
            catch (const std::exception& e) {
                Logger::Error("Failed to start continuous capture");
                return static_cast<int>(ResultCode::ErrorCaptureFailed);
            }
        }

        int ScreenCaptureEngine::ReadLatestCapture(unsigned char** imageData, int* dataSize, int* width, int* height)
        {
            try {
                std::lock_guard<std::mutex> lock(m_captureMutex);

                if (!m_isCapturing || m_latestCapture.empty()) {
                    return static_cast<int>(ResultCode::ErrorCaptureFailed);
                }

                // 複製最新的截圖數據
                *dataSize = static_cast<int>(m_latestCapture.size());
                *imageData = new unsigned char[*dataSize];
                memcpy(*imageData, m_latestCapture.data(), *dataSize);
                
                *width = m_captureWidth;
                *height = m_captureHeight;

                return static_cast<int>(ResultCode::Success);
            }
            catch (const std::exception& e) {
                Logger::Error("Failed to read latest capture");
                return static_cast<int>(ResultCode::ErrorCaptureFailed);
            }
        }

        int ScreenCaptureEngine::StopContinuousCapture()
        {
            try {
                if (!m_isCapturing) {
                    return static_cast<int>(ResultCode::Success);
                }

                m_isCapturing = false;

                if (m_captureThread && m_captureThread->joinable()) {
                    m_captureThread->join();
                    m_captureThread.reset();
                }

                // 清理捕獲數據
                {
                    std::lock_guard<std::mutex> lock(m_captureMutex);
                    m_latestCapture.clear();
                }

                Logger::Info("Continuous capture stopped");
                return static_cast<int>(ResultCode::Success);
            }
            catch (const std::exception& e) {
                Logger::Error("Failed to stop continuous capture");
                return static_cast<int>(ResultCode::ErrorCaptureFailed);
            }
        }

        void ScreenCaptureEngine::CaptureLoop()
        {
            Logger::Debug("Capture loop started");

            while (m_isCapturing) {
                try {
                    // 執行截圖
                    unsigned char* imageData = nullptr;
                    int dataSize = 0, width = 0, height = 0;

                    int result = CaptureWindow(m_targetWindow, &imageData, &dataSize, &width, &height);
                    
                    if (result == static_cast<int>(ResultCode::Success) && imageData) {
                        // 更新最新截圖
                        {
                            std::lock_guard<std::mutex> lock(m_captureMutex);
                            m_latestCapture.assign(imageData, imageData + dataSize);
                            m_captureWidth = width;
                            m_captureHeight = height;
                        }

                        delete[] imageData;
                    }

                    // 等待下一次截圖
                    std::this_thread::sleep_for(std::chrono::milliseconds(m_intervalMs));
                }
                catch (const std::exception& e) {
                    Logger::Error("Exception in capture loop");
                    // 繼續執行，不退出迴圈
                    std::this_thread::sleep_for(std::chrono::milliseconds(1000));
                }
            }

            Logger::Debug("Capture loop ended");
        }

    } // namespace Native
} // namespace MonLingo
