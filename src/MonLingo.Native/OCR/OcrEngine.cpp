#include "../pch.h"
#include "OcrEngine.h"
#include "PaddleOCRWrapper.h"
#include "../Common/Logger.h"
#include <opencv2/opencv.hpp>
#include <vector>
#include <memory>
#include <filesystem>

// TODO: 添加真正的 PaddleOCR 頭文件 (當庫整合完成後)
// #include "paddle_inference_api.h"

namespace MonLingo {
    namespace Native {

        OcrEngine::OcrEngine()
        {
            Logger::Debug("OcrEngine constructed - Initializing PaddleOCR");
            InitializePaddleOCR();
        }

        OcrEngine::~OcrEngine()
        {
            Logger::Debug("OcrEngine destructed - Cleaning up PaddleOCR resources");
            CleanupPaddleOCR();
        }

        int OcrEngine::ProcessImage(const unsigned char* imageData, int dataSize, int width, int height, OcrResult** result)
        {
            try {
                if (!imageData || dataSize <= 0 || width <= 0 || height <= 0 || !result) {
                    return static_cast<int>(ResultCode::ErrorInvalidParameter);
                }

                Logger::Debug("Starting PaddleOCR processing - Image: {}x{}, DataSize: {}", width, height, dataSize);

                // ============ PHASE 1: 圖像數據轉換 ============
                // 將 BGRA 格式轉換為 OpenCV Mat (BGR)
                cv::Mat image = ConvertBGRAToMat(imageData, width, height);
                if (image.empty()) {
                    Logger::Error("Failed to convert image data to cv::Mat");
                    return static_cast<int>(ResultCode::ErrorInvalidParameter);
                }

                // ============ PHASE 2: 圖像前處理 ============
                cv::Mat preprocessedImage = ApplyImagePreprocessing(image);
                
                // ============ PHASE 3: PaddleOCR 文字檢測 ============
                auto detectionResults = RunTextDetection(preprocessedImage);
                if (detectionResults.empty()) {
                    Logger::Debug("No text regions detected");
                    // 返回空結果但不報錯
                    *result = CreateEmptyOcrResult();
                    return static_cast<int>(ResultCode::Success);
                }

                // ============ PHASE 4: PaddleOCR 文字識別 ============
                std::vector<std::string> recognizedTexts;
                std::vector<float> confidences;
                
                for (const auto& textBox : detectionResults) {
                    auto [text, confidence] = RunTextRecognition(preprocessedImage, textBox);
                    recognizedTexts.push_back(text);
                    confidences.push_back(confidence);
                }

                // ============ PHASE 5: 結果結構化 ============
                *result = ConvertToOcrResult(detectionResults, recognizedTexts, confidences, width, height);

                Logger::Debug("PaddleOCR processing completed successfully - Found {} text regions", recognizedTexts.size());
                return static_cast<int>(ResultCode::Success);
            }
            catch (const std::exception& e) {
                Logger::Error("Exception in PaddleOCR ProcessImage: {}", e.what());
                return static_cast<int>(ResultCode::ErrorOcrFailed);
            }
        }

        int OcrEngine::ProcessScreenRegion(int x, int y, int width, int height, OcrResult** result)
        {
            try {
                if (width <= 0 || height <= 0 || !result) {
                    return static_cast<int>(ResultCode::ErrorInvalidParameter);
                }

                // 截取屏幕區域
                HDC screenDC = GetDC(nullptr);
                if (!screenDC) {
                    Logger::Error("Failed to get screen DC");
                    return static_cast<int>(ResultCode::ErrorCaptureFailed);
                }

                HDC memoryDC = CreateCompatibleDC(screenDC);
                HBITMAP bitmap = CreateCompatibleBitmap(screenDC, width, height);
                HGDIOBJ oldBitmap = SelectObject(memoryDC, bitmap);

                // 執行截圖
                if (!BitBlt(memoryDC, 0, 0, width, height, screenDC, x, y, SRCCOPY)) {
                    Logger::Error("BitBlt failed for screen region");
                    SelectObject(memoryDC, oldBitmap);
                    DeleteObject(bitmap);
                    DeleteDC(memoryDC);
                    ReleaseDC(nullptr, screenDC);
                    return static_cast<int>(ResultCode::ErrorCaptureFailed);
                }

                // 轉換為圖像數據並進行 OCR
                // 這裡簡化實現，實際應該提取bitmap數據
                unsigned char* imageData = new unsigned char[width * height * 4];
                memset(imageData, 0, width * height * 4);

                int ocrResult = ProcessImage(imageData, width * height * 4, width, height, result);

                // 清理資源
                delete[] imageData;
                SelectObject(memoryDC, oldBitmap);
                DeleteObject(bitmap);
                DeleteDC(memoryDC);
                ReleaseDC(nullptr, screenDC);

                return ocrResult;
            }
            catch (const std::exception& e) {
                Logger::Error("Exception in ProcessScreenRegion");
                return static_cast<int>(ResultCode::ErrorOcrFailed);
            }
        }

        // ============ PaddleOCR 初始化與清理 ============
        void OcrEngine::InitializePaddleOCR()
        {
            try {
                Logger::Debug("Initializing PaddleOCR engines...");
                
                // 設置模型路徑 (相對於執行檔位置)
                std::filesystem::path basePath = std::filesystem::current_path().parent_path();
                m_detModelPath = (basePath / "models" / "ch_PP-OCRv4_det_infer").string();
                m_recModelPath = (basePath / "models" / "ch_PP-OCRv4_rec_infer").string();
                m_clsModelPath = (basePath / "models" / "ch_ppocr_mobile_v2.0_cls_infer").string();
                m_keysPath = (basePath / "models" / "ppocr_keys_v1.txt").string();
                
                // 創建 PaddleOCR 配置
                PaddleOCR::OCRConfig config;
                config.use_gpu = false;  // 暫時使用 CPU 模式
                config.gpu_id = 0;
                config.gpu_mem = 4000;
                config.cpu_math_library_num_threads = 6;
                config.use_mkldnn = true;
                config.det_model_dir = m_detModelPath;
                config.rec_model_dir = m_recModelPath;
                config.cls_model_dir = m_clsModelPath;
                config.char_list_file = m_keysPath;
                config.use_angle_cls = true;
                config.rec_batch_num = 6;
                config.language = PaddleOCR::OCRConfig::Language::Chinese;
                
                // 創建 PaddleOCR 引擎實例
                m_ocrEngine = std::make_unique<PaddleOCR::PPOCR>(config);
                
                if (m_ocrEngine && m_ocrEngine->IsInitialized()) {
                    Logger::Info("PaddleOCR models loaded successfully:");
                    Logger::Info("  Detection: {}", m_detModelPath);
                    Logger::Info("  Recognition: {}", m_recModelPath);
                    Logger::Info("  Classification: {}", m_clsModelPath);
                    
                    m_isInitialized = true;
                    Logger::Info("PaddleOCR initialization completed successfully");
                } else {
                    Logger::Error("Failed to create PaddleOCR engine instance");
                    m_isInitialized = false;
                }
                
            }
            catch (const std::exception& e) {
                Logger::Error("Failed to initialize PaddleOCR: {}", e.what());
                m_isInitialized = false;
            }
        }

        void OcrEngine::CleanupPaddleOCR()
        {
            try {
                if (m_isInitialized && m_ocrEngine) {
                    // 清理 PaddleOCR 資源
                    m_ocrEngine.reset();
                    
                    Logger::Debug("PaddleOCR resources cleaned up");
                    m_isInitialized = false;
                }
            }
            catch (const std::exception& e) {
                Logger::Error("Error during PaddleOCR cleanup: {}", e.what());
            }
        }

        // ============ 圖像處理輔助函數 ============
        cv::Mat OcrEngine::ConvertBGRAToMat(const unsigned char* imageData, int width, int height)
        {
            try {
                // 假設輸入是 BGRA 格式 (4 bytes per pixel)
                cv::Mat bgraImage(height, width, CV_8UC4, (void*)imageData);
                cv::Mat bgrImage;
                cv::cvtColor(bgraImage, bgrImage, cv::COLOR_BGRA2BGR);
                return bgrImage;
            }
            catch (const std::exception& e) {
                Logger::Error("Failed to convert BGRA to BGR: {}", e.what());
                return cv::Mat();
            }
        }

        cv::Mat OcrEngine::ApplyImagePreprocessing(const cv::Mat& image)
        {
            try {
                cv::Mat processed = image.clone();
                
                // TODO: 整合 ImagePreprocessor 的功能
                // 1. 角度校正
                // 2. K-means 聚類
                // 3. 對比度增強
                // 4. 去噪處理
                
                Logger::Debug("Image preprocessing completed");
                return processed;
            }
            catch (const std::exception& e) {
                Logger::Error("Image preprocessing failed: {}", e.what());
                return image; // 返回原圖作為後備
            }
        }

        // ============ PaddleOCR 核心處理 ============
        std::vector<std::vector<cv::Point2f>> OcrEngine::RunTextDetection(const cv::Mat& image)
        {
            try {
                std::vector<std::vector<cv::Point2f>> detectionResults;
                
                if (!m_isInitialized || !m_ocrEngine) {
                    Logger::Error("PaddleOCR not initialized");
                    return detectionResults;
                }

                // 使用 PaddleOCR 包裝類進行文字檢測
                detectionResults = m_ocrEngine->det(image);
                
                Logger::Debug("Text detection completed - Found {} regions", detectionResults.size());
                return detectionResults;
            }
            catch (const std::exception& e) {
                Logger::Error("Text detection failed: {}", e.what());
                return {};
            }
        }

        std::pair<std::string, float> OcrEngine::RunTextRecognition(const cv::Mat& image, const std::vector<cv::Point2f>& textBox)
        {
            try {
                if (!m_isInitialized || !m_ocrEngine) {
                    Logger::Error("PaddleOCR not initialized");
                    return {"", 0.0f};
                }

                // 從 textBox 提取 ROI
                cv::Mat roi = m_ocrEngine->ExtractROI(image, textBox);
                
                // 使用 PaddleOCR 包裝類進行文字識別
                auto [text, confidence] = m_ocrEngine->rec(roi);
                
                Logger::Debug("Text recognition completed: '{}' (confidence: {:.2f})", text, confidence);
                return {text, confidence};
            }
            catch (const std::exception& e) {
                Logger::Error("Text recognition failed: {}", e.what());
                return {"", 0.0f};
            }
        }

        // ============ 結果轉換 ============
        OcrResult* OcrEngine::CreateEmptyOcrResult()
        {
            OcrResult* result = new OcrResult();
            result->lineCount = 0;
            result->lines = nullptr;
            result->isValid = true;
            return result;
        }

        OcrResult* OcrEngine::ConvertToOcrResult(
            const std::vector<std::vector<cv::Point2f>>& detectionResults,
            const std::vector<std::string>& recognizedTexts, 
            const std::vector<float>& confidences,
            int imageWidth, int imageHeight)
        {
            try {
                OcrResult* result = new OcrResult();
                result->lineCount = static_cast<int>(recognizedTexts.size());
                result->lines = new OcrLine[result->lineCount];
                result->isValid = true;

                for (int i = 0; i < result->lineCount; i++) {
                    const auto& box = detectionResults[i];
                    
                    // 計算邊界框
                    float minX = box[0].x, maxX = box[0].x;
                    float minY = box[0].y, maxY = box[0].y;
                    for (const auto& point : box) {
                        minX = std::min(minX, point.x);
                        maxX = std::max(maxX, point.x);
                        minY = std::min(minY, point.y);
                        maxY = std::max(maxY, point.y);
                    }

                    // 分配並複製文字內容
                    const std::string& text = recognizedTexts[i];
                    char* content = new char[text.length() + 1];
                    strcpy_s(content, text.length() + 1, text.c_str());

                    // 填充 OcrLine 結構
                    result->lines[i].content = content;
                    result->lines[i].confidence = confidences[i];
                    result->lines[i].x = static_cast<int>(minX);
                    result->lines[i].y = static_cast<int>(minY);
                    result->lines[i].width = static_cast<int>(maxX - minX);
                    result->lines[i].height = static_cast<int>(maxY - minY);
                }

                Logger::Debug("OCR result conversion completed - {} lines", result->lineCount);
                return result;
            }
            catch (const std::exception& e) {
                Logger::Error("Failed to convert OCR results: {}", e.what());
                return CreateEmptyOcrResult();
            }
        }

    } // namespace Native
} // namespace MonLingo

 
 
// End of file
