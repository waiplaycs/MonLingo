#pragma once
#include "../Common/NativeAPI.h"

namespace MonLingo {
    namespace Native {

        /// <summary>
        /// 螢幕截圖引擎類
        /// 負責視窗截圖和連續截圖功能
        /// </summary>
        class ScreenCaptureEngine {
        public:
            ScreenCaptureEngine();
            ~ScreenCaptureEngine();

            /// <summary>
            /// 檢查系統是否支援 Graphics Capture API
            /// </summary>
            /// <returns>true 如果支援，false 如果不支援</returns>
            static bool IsSupported();

            /// <summary>
            /// 對指定視窗執行單次截圖
            /// </summary>
            /// <param name="windowHandle">視窗控制代碼</param>
            /// <param name="imageData">輸出圖像數據</param>
            /// <param name="dataSize">輸出數據大小</param>
            /// <param name="width">輸出圖像寬度</param>
            /// <param name="height">輸出圖像高度</param>
            /// <returns>操作結果代碼</returns>
            int CaptureWindow(HWND windowHandle, unsigned char** imageData, int* dataSize, int* width, int* height);

            /// <summary>
            /// 開始連續截圖
            /// </summary>
            /// <param name="windowHandle">視窗控制代碼</param>
            /// <param name="intervalMs">截圖間隔（毫秒）</param>
            /// <returns>操作結果代碼</returns>
            int StartContinuousCapture(HWND windowHandle, int intervalMs);

            /// <summary>
            /// 讀取最新的截圖
            /// </summary>
            /// <param name="imageData">輸出圖像數據</param>
            /// <param name="dataSize">輸出數據大小</param>
            /// <param name="width">輸出圖像寬度</param>
            /// <param name="height">輸出圖像高度</param>
            /// <returns>操作結果代碼</returns>
            int ReadLatestCapture(unsigned char** imageData, int* dataSize, int* width, int* height);

            /// <summary>
            /// 停止連續截圖
            /// </summary>
            /// <returns>操作結果代碼</returns>
            int StopContinuousCapture();

        private:
            // 私有成員變量
            std::atomic<bool> m_isCapturing;
            std::unique_ptr<std::thread> m_captureThread;
            std::mutex m_captureMutex;
            std::vector<unsigned char> m_latestCapture;
            int m_captureWidth;
            int m_captureHeight;
            HWND m_targetWindow;
            int m_intervalMs;

            // 私有方法
            void CaptureLoop();
        };

    } // namespace Native
} // namespace MonLingo
