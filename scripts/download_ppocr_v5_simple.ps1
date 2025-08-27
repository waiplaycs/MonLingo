# PP-OCRv5 真實模型下載腳本
param(
    [string]$ModelsDir = "c:\Users\User\Desktop\MonLingo2\models"
)

Write-Host "🚀 開始下載真正的 PP-OCRv5 模型..." -ForegroundColor Green

# PP-OCRv5 官方模型下載鏈接
$detUrl = "https://paddleocr.bj.bcebos.com/PP-OCRv5/chinese/ch_PP-OCRv5_det_infer.tar"
$recUrl = "https://paddleocr.bj.bcebos.com/PP-OCRv5/chinese/ch_PP-OCRv5_rec_infer.tar"

Set-Location $ModelsDir

# 下載檢測模型
Write-Host "📦 下載 PP-OCRv5 檢測模型..." -ForegroundColor Yellow
try {
    Invoke-WebRequest -Uri $detUrl -OutFile "ch_PP-OCRv5_det_infer.tar" -UseBasicParsing
    Write-Host "   -> 檢測模型下載完成" -ForegroundColor Green
    
    # 解壓縮
    tar -xf "ch_PP-OCRv5_det_infer.tar"
    Remove-Item "ch_PP-OCRv5_det_infer.tar" -Force
    Write-Host "   -> 檢測模型解壓縮完成" -ForegroundColor Green
}
catch {
    Write-Host "   -> 檢測模型下載失敗: $($_.Exception.Message)" -ForegroundColor Red
}

# 下載識別模型
Write-Host "📦 下載 PP-OCRv5 識別模型..." -ForegroundColor Yellow
try {
    Invoke-WebRequest -Uri $recUrl -OutFile "ch_PP-OCRv5_rec_infer.tar" -UseBasicParsing
    Write-Host "   -> 識別模型下載完成" -ForegroundColor Green
    
    # 解壓縮
    tar -xf "ch_PP-OCRv5_rec_infer.tar"
    Remove-Item "ch_PP-OCRv5_rec_infer.tar" -Force
    Write-Host "   -> 識別模型解壓縮完成" -ForegroundColor Green
}
catch {
    Write-Host "   -> 識別模型下載失敗: $($_.Exception.Message)" -ForegroundColor Red
}

# 驗證下載結果
Write-Host "`n📊 驗證模型文件:" -ForegroundColor Yellow

if (Test-Path "ch_PP-OCRv5_det_infer\inference.pdmodel") {
    $detSize = (Get-Item "ch_PP-OCRv5_det_infer\inference.pdmodel").Length / 1MB
    Write-Host "   ✅ 檢測模型: $($detSize.ToString('F1')) MB" -ForegroundColor Green
} else {
    Write-Host "   ❌ 檢測模型: 下載失敗" -ForegroundColor Red
}

if (Test-Path "ch_PP-OCRv5_rec_infer\inference.pdmodel") {
    $recSize = (Get-Item "ch_PP-OCRv5_rec_infer\inference.pdmodel").Length / 1MB
    Write-Host "   ✅ 識別模型: $($recSize.ToString('F1')) MB" -ForegroundColor Green
} else {
    Write-Host "   ❌ 識別模型: 下載失敗" -ForegroundColor Red
}

Write-Host "`n🎉 PP-OCRv5 模型部署完成！" -ForegroundColor Green
