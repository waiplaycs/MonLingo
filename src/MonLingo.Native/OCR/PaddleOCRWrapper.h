#pragma once
#include <opencv2/opencv.hpp>
#include <vector>
#include <string>
#include <memory>

// 暫時使用模擬的 PaddleOCR 接口，直到真正的庫整合完成
// TODO: 替換為真正的 #include <paddle_inference_api.h>

namespace PaddleOCR {
    
    // 模擬 PaddleOCR 配置結構
    struct OCRConfig {
        bool use_gpu = false;
        int gpu_id = 0;
        int gpu_mem = 4000;
        int cpu_math_library_num_threads = 6;
        bool use_mkldnn = true;
        std::string det_model_dir;
        std::string rec_model_dir;
        std::string cls_model_dir;
        std::string char_list_file;
        bool use_angle_cls = true;
        int rec_batch_num = 6;
        
        // 多語言支援
        enum class Language {
            Chinese = 0,
            English = 1,
            Japanese = 2,
            Korean = 3
        };
        Language language = Language::Chinese;
    };
    
    // OCR 結果結構
    struct TextBox {
        std::vector<cv::Point2f> box;  // 四個角點
        std::string text;
        float confidence;
    };
    
    // 主要 PaddleOCR 引擎類
    class PPOCR {
    public:
        explicit PPOCR(const OCRConfig& config);
        ~PPOCR();
        
        // 執行完整 OCR 流程 (檢測 + 識別)
        std::vector<TextBox> ocr(const cv::Mat& image, 
                                bool det = true, 
                                bool rec = true, 
                                bool cls = true);
        
        // 單獨執行文字檢測
        std::vector<std::vector<cv::Point2f>> det(const cv::Mat& image);
        
        // 單獨執行文字識別
        std::pair<std::string, float> rec(const cv::Mat& image);
        
        // 單獨執行文字方向分類
        std::pair<cv::Mat, float> cls(const cv::Mat& image);
        
        // 檢查是否已初始化
        bool IsInitialized() const { return m_initialized; }
        
    private:
        bool m_initialized;
        OCRConfig m_config;
        
        // TODO: 實際的 PaddleOCR 模型指標
        // std::unique_ptr<paddle_infer::Predictor> m_detPredictor;
        // std::unique_ptr<paddle_infer::Predictor> m_recPredictor;
        // std::unique_ptr<paddle_infer::Predictor> m_clsPredictor;
        
        // 初始化模型
        bool InitializeModels();
        
        // 輔助函數
        cv::Mat NormalizeImage(const cv::Mat& image);
        std::vector<cv::Point2f> PostProcessDetection(const cv::Mat& prediction);
        std::string PostProcessRecognition(const cv::Mat& prediction);
        cv::Mat ExtractROI(const cv::Mat& image, const std::vector<cv::Point2f>& box);
    };
    
} // namespace PaddleOCR
