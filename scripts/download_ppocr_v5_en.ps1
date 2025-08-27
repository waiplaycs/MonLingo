# PP-OCRv5 Real Model Download Script
param(
    [string]$ModelsDir = "c:\Users\User\Desktop\MonLingo2\models"
)

Write-Host "Downloading real PP-OCRv5 models..." -ForegroundColor Green

# PP-OCRv5 official model download URLs
$detUrl = "https://paddleocr.bj.bcebos.com/PP-OCRv5/chinese/ch_PP-OCRv5_det_infer.tar"
$recUrl = "https://paddleocr.bj.bcebos.com/PP-OCRv5/chinese/ch_PP-OCRv5_rec_infer.tar"

Set-Location $ModelsDir

# Download detection model
Write-Host "Downloading PP-OCRv5 detection model..." -ForegroundColor Yellow
try {
    Invoke-WebRequest -Uri $detUrl -OutFile "ch_PP-OCRv5_det_infer.tar" -UseBasicParsing
    Write-Host "Detection model downloaded successfully" -ForegroundColor Green
    
    # Extract
    tar -xf "ch_PP-OCRv5_det_infer.tar"
    Remove-Item "ch_PP-OCRv5_det_infer.tar" -Force
    Write-Host "Detection model extracted successfully" -ForegroundColor Green
}
catch {
    Write-Host "Detection model download failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Download recognition model
Write-Host "Downloading PP-OCRv5 recognition model..." -ForegroundColor Yellow
try {
    Invoke-WebRequest -Uri $recUrl -OutFile "ch_PP-OCRv5_rec_infer.tar" -UseBasicParsing
    Write-Host "Recognition model downloaded successfully" -ForegroundColor Green
    
    # Extract
    tar -xf "ch_PP-OCRv5_rec_infer.tar"
    Remove-Item "ch_PP-OCRv5_rec_infer.tar" -Force
    Write-Host "Recognition model extracted successfully" -ForegroundColor Green
}
catch {
    Write-Host "Recognition model download failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Verify download results
Write-Host ""
Write-Host "Verifying model files:" -ForegroundColor Yellow

if (Test-Path "ch_PP-OCRv5_det_infer\inference.pdmodel") {
    $detSize = (Get-Item "ch_PP-OCRv5_det_infer\inference.pdmodel").Length / 1MB
    Write-Host "Detection model: $($detSize.ToString('F1')) MB - OK" -ForegroundColor Green
} else {
    Write-Host "Detection model: FAILED" -ForegroundColor Red
}

if (Test-Path "ch_PP-OCRv5_rec_infer\inference.pdmodel") {
    $recSize = (Get-Item "ch_PP-OCRv5_rec_infer\inference.pdmodel").Length / 1MB
    Write-Host "Recognition model: $($recSize.ToString('F1')) MB - OK" -ForegroundColor Green
} else {
    Write-Host "Recognition model: FAILED" -ForegroundColor Red
}

Write-Host ""
Write-Host "PP-OCRv5 model deployment completed!" -ForegroundColor Green
