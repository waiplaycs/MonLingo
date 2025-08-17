# PaddleOCR 快速設置腳本
param([switch]$Force = $false)

$BaseDir = Split-Path -Parent $PSScriptRoot
$ModelsDir = Join-Path $BaseDir "models"
$ThirdPartyDir = Join-Path $BaseDir "third_party"

Write-Host "=== PaddleOCR 快速設置 ===" -ForegroundColor Green

# 創建目錄
@($ModelsDir, $ThirdPartyDir) | ForEach-Object {
    if (-not (Test-Path $_)) {
        New-Item -ItemType Directory -Path $_ -Force
        Write-Host "已創建目錄: $_" -ForegroundColor Yellow
    }
}

# 下載模型
$Models = @{
    "ch_PP-OCRv4_det_infer" = "https://paddleocr.bj.bcebos.com/PP-OCRv4/chinese/ch_PP-OCRv4_det_infer.tar"
    "ch_PP-OCRv4_rec_infer" = "https://paddleocr.bj.bcebos.com/PP-OCRv4/chinese/ch_PP-OCRv4_rec_infer.tar"
    "ch_ppocr_mobile_v2.0_cls_infer" = "https://paddleocr.bj.bcebos.com/dygraph_v2.0/ch/ch_ppocr_mobile_v2.0_cls_infer.tar"
}

foreach ($model in $Models.GetEnumerator()) {
    $ModelPath = Join-Path $ModelsDir $model.Key
    if ((Test-Path $ModelPath) -and -not $Force) {
        Write-Host "模型已存在: $($model.Key)" -ForegroundColor Green
        continue
    }
    
    $TarFile = Join-Path $ModelsDir "$($model.Key).tar"
    try {
        Write-Host "正在下載: $($model.Key)" -ForegroundColor Cyan
        Invoke-WebRequest -Uri $model.Value -OutFile $TarFile -UseBasicParsing -TimeoutSec 300
        
        # 解壓
        if (Get-Command tar -ErrorAction SilentlyContinue) {
            Push-Location $ModelsDir
            tar -xf $TarFile
            Pop-Location
            Remove-Item $TarFile -Force
            Write-Host "✓ $($model.Key) 下載完成" -ForegroundColor Green
        } else {
            Write-Warning "請手動解壓: $TarFile"
        }
    }
    catch {
        Write-Warning "下載失敗: $($model.Key) - $_"
    }
}

# 下載字典文件
$KeysFile = Join-Path $ModelsDir "ppocr_keys_v1.txt"
if (-not (Test-Path $KeysFile) -or $Force) {
    try {
        Write-Host "正在下載字典文件..." -ForegroundColor Cyan
        Invoke-WebRequest -Uri "https://raw.githubusercontent.com/PaddlePaddle/PaddleOCR/release/2.6/ppocr/utils/ppocr_keys_v1.txt" -OutFile $KeysFile -UseBasicParsing
        Write-Host "✓ 字典文件下載完成" -ForegroundColor Green
    }
    catch {
        Write-Warning "字典文件下載失敗: $_"
    }
}

Write-Host "`n=== 設置狀態檢查 ===" -ForegroundColor Green
$CheckList = @(
    @{ Path = $ModelsDir; Name = "模型目錄" }
    @{ Path = (Join-Path $ModelsDir "ch_PP-OCRv4_det_infer"); Name = "檢測模型" }
    @{ Path = (Join-Path $ModelsDir "ch_PP-OCRv4_rec_infer"); Name = "識別模型" }
    @{ Path = (Join-Path $ModelsDir "ch_ppocr_mobile_v2.0_cls_infer"); Name = "分類模型" }
    @{ Path = (Join-Path $ModelsDir "ppocr_keys_v1.txt"); Name = "字典文件" }
)

$AllOK = $true
foreach ($item in $CheckList) {
    if (Test-Path $item.Path) {
        Write-Host "✓ $($item.Name)" -ForegroundColor Green
    } else {
        Write-Host "✗ $($item.Name)" -ForegroundColor Red
        $AllOK = $false
    }
}

if ($AllOK) {
    Write-Host "`n🎉 模型設置完成！" -ForegroundColor Green
} else {
    Write-Host "`n⚠️ 部分模型缺失，請檢查網路連線" -ForegroundColor Yellow
}

Write-Host "`n下一步: 手動下載 Paddle Inference 庫" -ForegroundColor Cyan
Write-Host "參考: docs\PaddleOCR_依賴整合指南.md" -ForegroundColor Gray
