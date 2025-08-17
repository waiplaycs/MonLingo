# PaddleOCR 模型設置腳本
# 用於自動下載和配置 PaddleOCR 推理模型

param(
    [string]$ModelsDir = "models",
    [switch]$Force = $false
)

# 設置變量
$BaseDir = Split-Path -Parent $PSScriptRoot
$ModelsPath = Join-Path $BaseDir $ModelsDir

Write-Host "=== PaddleOCR 模型設置 ===" -ForegroundColor Green
Write-Host "基礎目錄: $BaseDir" -ForegroundColor Cyan
Write-Host "模型目錄: $ModelsPath" -ForegroundColor Cyan

# 創建模型目錄
if (-not (Test-Path $ModelsPath)) {
    New-Item -ItemType Directory -Path $ModelsPath -Force
    Write-Host "已創建模型目錄: $ModelsPath" -ForegroundColor Yellow
}

# 定義模型下載配置
$Models = @{
    "ch_PP-OCRv4_det_infer" = @{
        "url" = "https://paddleocr.bj.bcebos.com/PP-OCRv4/chinese/ch_PP-OCRv4_det_infer.tar"
        "description" = "中文文字檢測模型 v4"
    }
    "ch_PP-OCRv4_rec_infer" = @{
        "url" = "https://paddleocr.bj.bcebos.com/PP-OCRv4/chinese/ch_PP-OCRv4_rec_infer.tar"
        "description" = "中文文字識別模型 v4"
    }
    "ch_ppocr_mobile_v2.0_cls_infer" = @{
        "url" = "https://paddleocr.bj.bcebos.com/dygraph_v2.0/ch/ch_ppocr_mobile_v2.0_cls_infer.tar"
        "description" = "文字方向分類器模型"
    }
}

# 下載字典文件
$KeysFile = @{
    "ppocr_keys_v1.txt" = @{
        "url" = "https://raw.githubusercontent.com/PaddlePaddle/PaddleOCR/release/2.6/ppocr/utils/ppocr_keys_v1.txt"
        "description" = "中文字符字典"
    }
}

# 函數：下載並解壓模型
function Download-Model {
    param(
        [string]$ModelName,
        [string]$Url,
        [string]$Description
    )
    
    $ModelPath = Join-Path $ModelsPath $ModelName
    $TarFile = Join-Path $ModelsPath "$ModelName.tar"
    
    # 檢查模型是否已存在
    if ((Test-Path $ModelPath) -and -not $Force) {
        Write-Host "模型已存在，跳過: $ModelName" -ForegroundColor Yellow
        return
    }
    
    Write-Host "正在下載: $Description ($ModelName)" -ForegroundColor Green
    Write-Host "URL: $Url" -ForegroundColor Gray
    
    try {
        # 下載模型檔案
        Invoke-WebRequest -Uri $Url -OutFile $TarFile -UseBasicParsing
        Write-Host "下載完成: $TarFile" -ForegroundColor Green
        
        # 解壓 tar 檔案 (需要安裝 7-Zip 或使用 Windows 內建的 tar)
        if (Get-Command tar -ErrorAction SilentlyContinue) {
            Set-Location $ModelsPath
            tar -xf $TarFile
            Write-Host "解壓完成: $ModelName" -ForegroundColor Green
        } else {
            Write-Warning "未找到 tar 命令，請手動解壓: $TarFile"
        }
        
        # 清理 tar 檔案
        if (Test-Path $TarFile) {
            Remove-Item $TarFile
            Write-Host "已清理下載檔案: $TarFile" -ForegroundColor Gray
        }
        
    } catch {
        Write-Error "下載失敗: $ModelName - $_"
    }
}

# 函數：下載字典文件
function Download-KeysFile {
    param(
        [string]$FileName,
        [string]$Url,
        [string]$Description
    )
    
    $FilePath = Join-Path $ModelsPath $FileName
    
    if ((Test-Path $FilePath) -and -not $Force) {
        Write-Host "字典文件已存在，跳過: $FileName" -ForegroundColor Yellow
        return
    }
    
    Write-Host "正在下載: $Description ($FileName)" -ForegroundColor Green
    
    try {
        Invoke-WebRequest -Uri $Url -OutFile $FilePath -UseBasicParsing
        Write-Host "下載完成: $FilePath" -ForegroundColor Green
    } catch {
        Write-Error "下載失敗: $FileName - $_"
    }
}

# 執行下載
Write-Host "`n=== 開始下載模型 ===" -ForegroundColor Green

foreach ($model in $Models.GetEnumerator()) {
    Download-Model -ModelName $model.Key -Url $model.Value.url -Description $model.Value.description
}

Write-Host "`n=== 開始下載字典文件 ===" -ForegroundColor Green

foreach ($keys in $KeysFile.GetEnumerator()) {
    Download-KeysFile -FileName $keys.Key -Url $keys.Value.url -Description $keys.Value.description
}

# 驗證下載結果
Write-Host "`n=== 驗證模型文件 ===" -ForegroundColor Green
$AllModels = $Models.Keys + $KeysFile.Keys

foreach ($modelName in $AllModels) {
    $path = if ($modelName.EndsWith('.txt')) { 
        Join-Path $ModelsPath $modelName 
    } else { 
        Join-Path $ModelsPath $modelName 
    }
    
    if (Test-Path $path) {
        Write-Host "✓ $modelName" -ForegroundColor Green
    } else {
        Write-Host "✗ $modelName (缺失)" -ForegroundColor Red
    }
}

Write-Host "`n=== PaddleOCR 模型設置完成 ===" -ForegroundColor Green
Write-Host "模型路徑: $ModelsPath" -ForegroundColor Cyan
Write-Host "請確認所有模型文件都已正確下載後，再進行編譯。" -ForegroundColor Yellow
