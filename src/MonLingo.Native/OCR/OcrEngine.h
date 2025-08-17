#pragma once
#include "../Common/NativeAPI.h"
#include <opencv2/opencv.hpp>
#include <vector>
#include <string>
#include <memory>

// 前向聲明
namespace PaddleOCR {
    class PPOCR;
}

namespace MonLingo {
    namespace Native {

        /// <summary>
        /// PaddleOCR 引擎類
        /// 負責圖像文字識別功能，支援中日韓多語言
        /// </summary>
        class OcrEngine {
        public:
            OcrEngine();
            ~OcrEngine();

            /// <summary>
            /// 處理圖像數據並返回 OCR 結果
            /// </summary>
            /// <param name="imageData">圖像數據</param>
            /// <param name="dataSize">數據大小</param>
            /// <param name="width">圖像寬度</param>
            /// <param name="height">圖像高度</param>
            /// <param name="result">OCR 結果指針</param>
            /// <returns>操作結果代碼</returns>
            int ProcessImage(const unsigned char* imageData, int dataSize, int width, int height, OcrResult** result);

            /// <summary>
            /// 處理屏幕區域並返回 OCR 結果
            /// </summary>
            /// <param name="x">區域 X 座標</param>
            /// <param name="y">區域 Y 座標</param>
            /// <param name="width">區域寬度</param>
            /// <param name="height">區域高度</param>
            /// <param name="result">OCR 結果指針</param>
            /// <returns>操作結果代碼</returns>
            int ProcessScreenRegion(int x, int y, int width, int height, OcrResult** result);

        private:
            // ============ 初始化與清理 ============
            void InitializePaddleOCR();
            void CleanupPaddleOCR();

            // ============ 圖像處理 ============
            cv::Mat ConvertBGRAToMat(const unsigned char* imageData, int width, int height);
            cv::Mat ApplyImagePreprocessing(const cv::Mat& image);

            // ============ PaddleOCR 核心處理 ============
            std::vector<std::vector<cv::Point2f>> RunTextDetection(const cv::Mat& image);
            std::pair<std::string, float> RunTextRecognition(const cv::Mat& image, const std::vector<cv::Point2f>& textBox);

            // ============ 結果轉換 ============
            OcrResult* CreateEmptyOcrResult();
            OcrResult* ConvertToOcrResult(
                const std::vector<std::vector<cv::Point2f>>& detectionResults,
                const std::vector<std::string>& recognizedTexts, 
                const std::vector<float>& confidences,
                int imageWidth, int imageHeight);

            // ============ 成員變量 ============
            bool m_isInitialized;
            
            // PaddleOCR 引擎實例
            std::unique_ptr<PaddleOCR::PPOCR> m_ocrEngine;
            
            // 模型路徑配置
            std::string m_detModelPath;
            std::string m_recModelPath; 
            std::string m_clsModelPath;
            std::string m_keysPath;
        };

    } // namespace Native
} // namespace MonLingo
