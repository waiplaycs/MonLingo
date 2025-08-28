using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using PaddleOCRSharp;
using System.Reflection;
using MonLingo.Core.Infrastructure;
using System.Collections.Generic;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// 基於PaddleOCR的真實OCR服務實現
    /// 替換MockOcrService，連接真正的PaddleOCR引擎
    /// 支援用戶語言配置，記住輸入語言設定
    /// </summary>
    public class RealOcrService : IOcrService, IDisposable
    {
        private PaddleOCREngine _ocrEngine;
        private bool _isInitialized = false;
        private bool _disposed = false;
        private ILanguageConfigService _languageConfigService;
        private string _lastUsedSourceLanguage = "auto";

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized => _isInitialized;

        public RealOcrService()
        {
            // 延遲初始化語言配置服務，避免循環依賴
        }

        /// <summary>
        /// 初始化語言配置服務
        /// </summary>
        private void EnsureLanguageConfigService()
        {
            if (_languageConfigService == null)
            {
                try
                {
                    _languageConfigService = Phase5ServiceContainer.GetService<ILanguageConfigService>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ 語言配置服務初始化失敗: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 初始化PaddleOCR引擎 (async版本)
        /// </summary>
        public async Task<bool> InitializeAsync()
        {
            if (_isInitialized) return true;

            try
            {
                return await Task.Run(() =>
                {
                    // 若專案的 models 目錄下有 PP-OCRv5 模型，先部署到執行時的 inference 目錄
                    TryDeployPPOCRv5Models();

                    // 初始化PaddleOCR引擎，配置為CPU模式
                    var parameter = new OCRParameter
                    {
                        use_gpu = false,
                        cpu_math_library_num_threads = Environment.ProcessorCount,
                        enable_mkldnn = true,
                        det_db_thresh = 0.3f,
                        det_db_box_thresh = 0.5f,
                        det_db_unclip_ratio = 1.6f,
                        cls_thresh = 0.9f
                    };

                    _ocrEngine = new PaddleOCREngine(null, parameter);
                    _isInitialized = true;
                    
                    Console.WriteLine("✅ PaddleOCR引擎初始化成功");
                    return true;
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ PaddleOCR引擎初始化失敗: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 檢查並在可用時部署 PP-OCRv5 模型到執行時 inference 目錄。
        /// 不存在時安靜跳過，沿用套件內建模型。
        /// </summary>
        private void TryDeployPPOCRv5Models()
        {
            try
            {
                // 1) 探測 models 目錄（優先環境變數、其次相對於執行目錄、再嘗試開發模式路徑）
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string modelsDir = Environment.GetEnvironmentVariable("MONLINGO_MODELS_DIR");
                if (string.IsNullOrWhiteSpace(modelsDir))
                {
                    var candidate1 = Path.Combine(baseDir, "models");
                    var candidate2 = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "models"));
                    if (Directory.Exists(candidate1)) modelsDir = candidate1;
                    else if (Directory.Exists(candidate2)) modelsDir = candidate2;
                }

                if (string.IsNullOrWhiteSpace(modelsDir) || !Directory.Exists(modelsDir))
                {
                    return; // 沒有 models 目錄，直接返回
                }

                // 2) 確認 PP-OCRv5 模型資料夾是否存在（檢/識），分類模型沿用 v2.0（若存在則一併部署）
                string detDir = Path.Combine(modelsDir, "ch_PP-OCRv5_det_infer");
                string recDir = Path.Combine(modelsDir, "ch_PP-OCRv5_rec_infer");
                string clsDir = Path.Combine(modelsDir, "ch_ppocr_mobile_v2.0_cls_infer");
                string keysFile = Path.Combine(modelsDir, "ppocr_keys.txt");
                
                if (!File.Exists(keysFile))
                {
                    // 兼容常見命名
                    var alt = Path.Combine(modelsDir, "ppocr_keys_v1.txt");
                    if (File.Exists(alt)) keysFile = alt;
                }

                bool hasV5 = IsValidPPOCRv5Model(detDir, recDir);

                // 2.1) 若找不到 v5，嘗試使用 v3 作為臨時備援
                string detDirV3 = Path.Combine(modelsDir, "ch_PP-OCRv3_det_infer");
                string recDirV3 = Path.Combine(modelsDir, "ch_PP-OCRv3_rec_infer");
                bool hasV3 = Directory.Exists(detDirV3) && Directory.Exists(recDirV3);

                if (!hasV5 && !hasV3)
                {
                    // 沒有可用模型，跳過
                    return;
                }

                // 3) 部署到執行時 inference 目錄（PaddleOCRSharp 預設從此處載入）
                string inferenceDir = Path.Combine(baseDir, "inference");
                Directory.CreateDirectory(inferenceDir);

                // 複製 keys 檔（如存在）
                if (File.Exists(keysFile))
                {
                    var destKeys = Path.Combine(inferenceDir, "ppocr_keys.txt");
                    SafeCopyFile(keysFile, destKeys);
                }

                if (hasV5)
                {
                    // 複製 v5 det/rec/cls 模型資料夾
                    SafeCopyDirectory(detDir, Path.Combine(inferenceDir, Path.GetFileName(detDir)));
                    SafeCopyDirectory(recDir, Path.Combine(inferenceDir, Path.GetFileName(recDir)));
                    if (Directory.Exists(clsDir))
                    {
                        SafeCopyDirectory(clsDir, Path.Combine(inferenceDir, Path.GetFileName(clsDir)));
                    }
                    Console.WriteLine("📦 已部署 PP-OCRv5 模型至執行目錄的 inference 資料夾");
                }
                else if (hasV3)
                {
                    // 備援：複製 v3 模型，以確保可立即運行（待日後換成 v5）
                    SafeCopyDirectory(detDirV3, Path.Combine(inferenceDir, Path.GetFileName(detDirV3)));
                    SafeCopyDirectory(recDirV3, Path.Combine(inferenceDir, Path.GetFileName(recDirV3)));
                    if (Directory.Exists(clsDir))
                    {
                        SafeCopyDirectory(clsDir, Path.Combine(inferenceDir, Path.GetFileName(clsDir)));
                    }
                    Console.WriteLine("📦 未找到 v5，已暫時部署 PP-OCRv3 模型至 inference 作為備援");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ 部署 PP-OCRv5 模型時發生錯誤：{ex.Message}");
            }
        }

        private static void SafeCopyDirectory(string sourceDir, string destDir)
        {
            if (!Directory.Exists(sourceDir)) return;
            if (Directory.Exists(destDir))
            {
                // 先嘗試刪除舊資料夾，避免殘留舊版本檔案
                try { Directory.Delete(destDir, true); } catch { /* ignore */ }
            }
            Directory.CreateDirectory(destDir);

            foreach (var file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                // .NET Framework 4.8 無 Path.GetRelativePath，改用 Uri 方式計算相對路徑
                var baseUri = new Uri(AppendDirectorySeparatorChar(sourceDir));
                var fileUri = new Uri(file);
                var relative = Uri.UnescapeDataString(baseUri.MakeRelativeUri(fileUri).ToString().Replace('/', Path.DirectorySeparatorChar));
                var target = Path.Combine(destDir, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                SafeCopyFile(file, target);
            }
        }

        private static void SafeCopyFile(string sourceFile, string destFile)
        {
            try
            {
                File.Copy(sourceFile, destFile, true);
            }
            catch
            {
                // 忽略單檔複製錯誤，避免影響整體流程
            }
        }

        private static string AppendDirectorySeparatorChar(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            char lastChar = path[path.Length - 1];
            if (lastChar == Path.DirectorySeparatorChar || lastChar == Path.AltDirectorySeparatorChar)
                return path;
            return path + Path.DirectorySeparatorChar;
        }

        /// <summary>
        /// 使用用戶配置的語言進行文字識別
        /// </summary>
        public async Task<OcrResult> RecognizeTextWithConfigAsync(byte[] imageData, int width, int height)
        {
            // 確保語言配置服務可用
            EnsureLanguageConfigService();
            
            // 獲取用戶設定的源語言
            string sourceLanguage = "auto";
            if (_languageConfigService != null)
            {
                try
                {
                    sourceLanguage = await _languageConfigService.GetSourceLanguageAsync();
                    
                    // 如果語言設定有變更，記錄下來
                    if (_lastUsedSourceLanguage != sourceLanguage)
                    {
                        Console.WriteLine($"🔄 OCR 語言設定變更: {_lastUsedSourceLanguage} → {sourceLanguage}");
                        _lastUsedSourceLanguage = sourceLanguage;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ 獲取源語言設定失敗，使用預設: {ex.Message}");
                }
            }
            
            // 執行 OCR 識別 (目前 PaddleOCR 暫不支援動態語言切換，但我們記錄設定)
            Console.WriteLine($"🔍 使用源語言設定進行 OCR 識別: {sourceLanguage}");
            return await RecognizeTextAsync(imageData, width, height);
        }

        /// <summary>
        /// 識別圖片中的文字 (async版本)
        /// </summary>
        public async Task<OcrResult> RecognizeTextAsync(byte[] imageData, int width, int height)
        {
            if (!_isInitialized || _ocrEngine == null)
            {
                throw new InvalidOperationException("OCR not initialized. Call InitializeAsync first.");
            }

            try
            {
                return await Task.Run(() =>
                {
                    // 處理原始像素數據：來自Native.dll的是RGBA格式
                    using var bitmap = ConvertRawDataToBitmap(imageData, width, height);
                    
                    // 執行OCR識別
                    var ocrResult = _ocrEngine.DetectText(bitmap);
                    
                    // 轉換為我們的OcrResult格式（加入幾何排序，確保閱讀順序：自上而下、由左至右）
                    if (ocrResult?.TextBlocks != null && ocrResult.TextBlocks.Count > 0)
                    {
                        // 1) 將 TextBlocks 轉成中間結構並計算穩定的 BoundingBox（由四個點取 min/max）
                        var blocks = new List<(string Text, float Score, System.Drawing.Rectangle Rect)>();
                        foreach (var tb in ocrResult.TextBlocks)
                        {
                            // BoxPoints 可能不是固定順序，取四點的 min/max 生成軸對齊矩形
                            int minX = (int)Math.Floor(tb.BoxPoints.Min(p => (double)p.X));
                            int minY = (int)Math.Floor(tb.BoxPoints.Min(p => (double)p.Y));
                            int maxX = (int)Math.Ceiling(tb.BoxPoints.Max(p => (double)p.X));
                            int maxY = (int)Math.Ceiling(tb.BoxPoints.Max(p => (double)p.Y));
                            var rect = new System.Drawing.Rectangle(minX, minY, Math.Max(1, maxX - minX), Math.Max(1, maxY - minY));
                            blocks.Add((tb.Text, tb.Score, rect));
                        }

                        if (blocks.Count == 0)
                        {
                            return new OcrResult
                            {
                                Text = string.Empty,
                                Confidence = 0.0,
                                Lines = new OcrLine[0],
                                BoundingBox = new System.Drawing.Rectangle()
                            };
                        }

                        // 2) 估算平均行高，用於同列（row）分組閾值
                        double avgHeight = blocks.Select(b => (double)b.Rect.Height).OrderBy(h => h).Skip(Math.Max(0, blocks.Count / 4)).Take(Math.Max(1, blocks.Count / 2)).DefaultIfEmpty(20).Average();
                        double rowThreshold = Math.Max(10.0, avgHeight * 0.6); // 同一行的中心Y相差不超過該值

                        // 3) 先按 Y 初步排序，再做行分組（以中心Y接近或垂直重疊判斷）
                        var prelim = blocks.OrderBy(b => b.Rect.Top).ThenBy(b => b.Rect.Left).ToList();
                        var rows = new List<List<(string Text, float Score, System.Drawing.Rectangle Rect)>>();

                        bool IsSameRow(System.Drawing.Rectangle a, System.Drawing.Rectangle b)
                        {
                            double cyA = a.Top + a.Height / 2.0;
                            double cyB = b.Top + b.Height / 2.0;
                            if (Math.Abs(cyA - cyB) <= rowThreshold) return true;
                            // 補充：若垂直重疊比例足夠，也視為同一行
                            int top = Math.Max(a.Top, b.Top);
                            int bottom = Math.Min(a.Bottom, b.Bottom);
                            int overlap = Math.Max(0, bottom - top);
                            double minH = Math.Max(1.0, Math.Min(a.Height, b.Height));
                            return (overlap / minH) >= 0.5;
                        }

                        foreach (var blk in prelim)
                        {
                            bool placed = false;
                            for (int i = 0; i < rows.Count; i++)
                            {
                                // 與當前行的代表框比較（用該行的中位數中心Y更穩定）
                                var row = rows[i];
                                var mid = row[row.Count / 2].Rect; // 行中位框
                                if (IsSameRow(mid, blk.Rect))
                                {
                                    row.Add(blk);
                                    placed = true;
                                    break;
                                }
                            }
                            if (!placed)
                            {
                                rows.Add(new List<(string Text, float Score, System.Drawing.Rectangle Rect)> { blk });
                            }
                        }

                        // 4) 依行自上而下排序；行內由左至右排序
                        rows = rows
                            .OrderBy(r => r.Min(b => b.Rect.Top))
                            .Select(r => r.OrderBy(b => b.Rect.Left).ToList())
                            .ToList();

                        // 5) 生成 OcrLine（維持一個 TextBlock 一行，保證穩定的輸出順序）
                        var ordered = rows.SelectMany(r => r).ToList();
                        var lines = new OcrLine[ordered.Count];
                        var allText = new System.Text.StringBuilder();
                        double totalConfidence = 0.0;

                        for (int i = 0; i < ordered.Count; i++)
                        {
                            var b = ordered[i];
                            var words = new OcrWord[]
                            {
                                new OcrWord { Text = b.Text, Confidence = b.Score, BoundingBox = b.Rect }
                            };
                            lines[i] = new OcrLine
                            {
                                Text = b.Text,
                                Confidence = b.Score,
                                BoundingBox = b.Rect,
                                Words = words
                            };
                            allText.AppendLine(b.Text);
                            totalConfidence += b.Score;
                        }

                        var result = new OcrResult
                        {
                            Text = allText.ToString().Trim(),
                            Confidence = totalConfidence / Math.Max(1, ordered.Count),
                            Lines = lines,
                            BoundingBox = CalculateOverallBoundingBox(lines)
                        };

                        Console.WriteLine($"🔍 OCR識別完成: 找到 {lines.Length} 行文字（已依行排序）");
                        return result;
                    }
                    else
                    {
                        return new OcrResult 
                        { 
                            Text = string.Empty, 
                            Confidence = 0.0,
                            Lines = new OcrLine[0],
                            BoundingBox = new System.Drawing.Rectangle()
                        };
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ OCR識別失敗: {ex.Message}");
                throw new InvalidOperationException($"OCR recognition failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 將原始RGBA數據轉換為Bitmap
        /// </summary>
        private Bitmap ConvertRawDataToBitmap(byte[] rawData, int width, int height)
        {
            Console.WriteLine($"[DEBUG] ConvertRawDataToBitmap: rawData.Length={rawData?.Length}, width={width}, height={height}");
            
            // 驗證參數
            if (width <= 0 || height <= 0)
            {
                Console.WriteLine($"[ERROR] Invalid dimensions: width={width}, height={height}");
                return new Bitmap(1, 1); // 返回最小的有效Bitmap
            }
            
            if (rawData == null || rawData.Length == 0)
            {
                Console.WriteLine("[DEBUG] Raw data is null or empty, creating blank bitmap");
                return new Bitmap(width, height);
            }
            
            // 檢查數據大小是否符合預期
            int expectedSize = width * height * 4; // RGBA = 4 bytes per pixel
            
            if (rawData.Length < expectedSize)
            {
                // 如果數據太小，可能是其他格式，嘗試作為圖片流處理
                try
                {
                    using var ms = new MemoryStream(rawData);
                    return new Bitmap(ms);
                }
                catch
                {
                    // 如果都失敗，創建一個空白圖片
                    Console.WriteLine($"⚠️ 圖片數據格式不符，創建空白圖片 ({width}x{height})");
                    return new Bitmap(width, height);
                }
            }

            // 創建Bitmap並複製像素數據
            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            try
            {
                // 複製數據到Bitmap
                Marshal.Copy(rawData, 0, bitmapData.Scan0, Math.Min(rawData.Length, expectedSize));
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }

            return bitmap;
        }

        /// <summary>
        /// 計算整體邊界框
        /// </summary>
        private System.Drawing.Rectangle CalculateOverallBoundingBox(OcrLine[] lines)
        {
            if (lines == null || lines.Length == 0)
                return new System.Drawing.Rectangle();

            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;

            foreach (var line in lines)
            {
                var rect = line.BoundingBox;
                minX = Math.Min(minX, rect.X);
                minY = Math.Min(minY, rect.Y);
                maxX = Math.Max(maxX, rect.X + rect.Width);
                maxY = Math.Max(maxY, rect.Y + rect.Height);
            }

            return new System.Drawing.Rectangle(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// 驗證 PP-OCRv5 模型是否有效
        /// </summary>
        private static bool IsValidPPOCRv5Model(string detDir, string recDir)
        {
            try
            {
                // 檢查目錄是否存在
                if (!Directory.Exists(detDir) || !Directory.Exists(recDir))
                    return false;

                // 檢查必要的模型文件是否存在 (新版 PaddleX 格式)
                string[] requiredDetFiles = { "inference.pdiparams", "inference.json" };
                string[] requiredRecFiles = { "inference.pdiparams", "inference.json" };

                // 檢查檢測模型文件
                foreach (string file in requiredDetFiles)
                {
                    if (!File.Exists(Path.Combine(detDir, file)))
                    {
                        // 嘗試舊格式
                        string oldFormatFile = file.Replace(".json", ".pdmodel");
                        if (!File.Exists(Path.Combine(detDir, oldFormatFile)))
                            return false;
                    }
                }

                // 檢查識別模型文件
                foreach (string file in requiredRecFiles)
                {
                    if (!File.Exists(Path.Combine(recDir, file)))
                    {
                        // 嘗試舊格式
                        string oldFormatFile = file.Replace(".json", ".pdmodel");
                        if (!File.Exists(Path.Combine(recDir, oldFormatFile)))
                            return false;
                    }
                }

                // 檢查模型文件大小，v5 模型通常比 v3 大
                var detParamsFile = Path.Combine(detDir, "inference.pdiparams");
                var recParamsFile = Path.Combine(recDir, "inference.pdiparams");
                
                if (!File.Exists(detParamsFile) || !File.Exists(recParamsFile))
                    return false;

                var detFileInfo = new FileInfo(detParamsFile);
                var recFileInfo = new FileInfo(recParamsFile);

                // PP-OCRv5 的檢測模型通常 > 50MB，識別模型通常 > 50MB
                // 更新閾值以適應真正的 v5 模型大小
                bool detSizeCheck = detFileInfo.Length > 50 * 1024 * 1024; // > 50MB
                bool recSizeCheck = recFileInfo.Length > 50 * 1024 * 1024; // > 50MB

                if (detSizeCheck && recSizeCheck)
                {
                    Console.WriteLine($"✅ 發現真正的 PP-OCRv5 模型: det={detFileInfo.Length / (1024*1024):F1}MB, rec={recFileInfo.Length / (1024*1024):F1}MB");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ 檢驗 PP-OCRv5 模型時發生錯誤: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 釋放OCR引擎資源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    if (_ocrEngine != null)
                    {
                        _ocrEngine.Dispose();
                        _ocrEngine = null;
                        Console.WriteLine("🔄 PaddleOCR引擎資源已釋放");
                    }
                    _isInitialized = false;
                    _disposed = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ 釋放OCR資源時發生錯誤: {ex.Message}");
                }
            }
        }
    }
}
