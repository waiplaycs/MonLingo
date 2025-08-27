# 使用 PaddleOCR 套件自動下載 PP-OCRv5 模型
import os
import sys
import shutil
from pathlib import Path

def setup_ppocr_v5_models():
    """使用 PaddleOCR 套件自動下載並提取 PP-OCRv5 模型"""
    
    print("🚀 開始設置 PP-OCRv5 模型...")
    
    try:
        # 安裝 PaddleOCR 套件（如果尚未安裝）
        import paddleocr
        print("✅ PaddleOCR 套件已安裝")
    except ImportError:
        print("📦 安裝 PaddleOCR 套件...")
        os.system("pip install paddleocr")
        import paddleocr
    
    try:
        # 初始化 PaddleOCR，這會自動下載 v5 模型
        print("📦 初始化 PaddleOCR (這會自動下載 PP-OCRv5 模型)...")
        ocr = paddleocr.PaddleOCR(lang='ch')
        print("✅ PaddleOCR 初始化成功，模型已下載")
        
        # 找到下載的模型文件位置
        import paddleocr
        paddle_home = os.path.expanduser("~/.paddleocr")
        
        print(f"🔍 在 {paddle_home} 中尋找模型文件...")
        
        # 列出所有下載的模型
        if os.path.exists(paddle_home):
            for root, dirs, files in os.walk(paddle_home):
                if any(file.endswith('.pdmodel') for file in files):
                    print(f"📁 發現模型目錄: {root}")
                    for file in files:
                        if file.endswith(('.pdmodel', '.pdiparams')):
                            file_path = os.path.join(root, file)
                            file_size = os.path.getsize(file_path) / (1024*1024)  # MB
                            print(f"   📄 {file}: {file_size:.1f} MB")
        
        # 複製到我們的 models 目錄
        project_models_dir = Path("c:/Users/User/Desktop/MonLingo2/models")
        
        # 尋找 PP-OCRv5 相關的模型目錄
        v5_det_dirs = []
        v5_rec_dirs = []
        
        if os.path.exists(paddle_home):
            for root, dirs, files in os.walk(paddle_home):
                dir_name = os.path.basename(root)
                if 'v5' in dir_name.lower() and 'det' in dir_name.lower():
                    v5_det_dirs.append(root)
                elif 'v5' in dir_name.lower() and 'rec' in dir_name.lower():
                    v5_rec_dirs.append(root)
                elif 'PP-OCRv5' in dir_name and 'det' in dir_name:
                    v5_det_dirs.append(root)
                elif 'PP-OCRv5' in dir_name and 'rec' in dir_name:
                    v5_rec_dirs.append(root)
        
        # 複製檢測模型
        if v5_det_dirs:
            src_det = v5_det_dirs[0]
            dst_det = project_models_dir / "ch_PP-OCRv5_det_infer"
            print(f"📋 複製檢測模型: {src_det} -> {dst_det}")
            if dst_det.exists():
                shutil.rmtree(dst_det)
            shutil.copytree(src_det, dst_det)
            print("✅ 檢測模型複製完成")
        
        # 複製識別模型
        if v5_rec_dirs:
            src_rec = v5_rec_dirs[0]
            dst_rec = project_models_dir / "ch_PP-OCRv5_rec_infer"
            print(f"📋 複製識別模型: {src_rec} -> {dst_rec}")
            if dst_rec.exists():
                shutil.rmtree(dst_rec)
            shutil.copytree(src_rec, dst_rec)
            print("✅ 識別模型複製完成")
        
        # 驗證模型
        print("\n📊 驗證 PP-OCRv5 模型:")
        
        det_model = project_models_dir / "ch_PP-OCRv5_det_infer" / "inference.pdmodel"
        rec_model = project_models_dir / "ch_PP-OCRv5_rec_infer" / "inference.pdmodel"
        
        if det_model.exists():
            det_size = det_model.stat().st_size / (1024*1024)
            print(f"   ✅ 檢測模型: {det_size:.1f} MB")
        else:
            print("   ❌ 檢測模型: 未找到")
        
        if rec_model.exists():
            rec_size = rec_model.stat().st_size / (1024*1024)
            print(f"   ✅ 識別模型: {rec_size:.1f} MB")
        else:
            print("   ❌ 識別模型: 未找到")
        
        print("\n🎉 PP-OCRv5 模型設置完成!")
        return True
        
    except Exception as e:
        print(f"❌ 錯誤: {e}")
        return False

if __name__ == "__main__":
    setup_ppocr_v5_models()
