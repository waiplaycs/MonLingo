# PP-OCRv5 真實模型下載腳本
# 下載官方 PP-OCRv5 模型文件

param(
    [string]$ModelsDir = "c:\Users\User\Desktop\MonLingo2\models"
)

Write-Host "🚀 開始下載真正的 PP-OCRv5 模型..." -ForegroundColor Green

# PP-OCRv5 官方模型下載鏈接
$models = @{
    "ch_PP-OCRv5_det_infer" = @{
        "url" = "https://paddleocr.bj.bcebos.com/PP-OCRv5/chinese/ch_PP-OCRv5_det_infer.tar"
        "desc" = "PP-OCRv5 檢測模型"
    }
    "ch_PP-OCRv5_rec_infer" = @{
        "url" = "https://paddleocr.bj.bcebos.com/PP-OCRv5/chinese/ch_PP-OCRv5_rec_infer.tar"
        "desc" = "PP-OCRv5 識別模型"
    }
}

# 建立模型目錄
if (-not (Test-Path $ModelsDir)) {
    New-Item -ItemType Directory -Path $ModelsDir -Force | Out-Null
}

Set-Location $ModelsDir

foreach ($modelName in $models.Keys) {
    $model = $models[$modelName]
    $url = $model.url
    $desc = $model.desc
    $tarFile = "$modelName.tar"
    
    Write-Host "📦 下載 $desc..." -ForegroundColor Yellow
    
    try {
        # 下載 tar 文件
        if (Test-Path $tarFile) {
            Remove-Item $tarFile -Force
        }
        
        Write-Host "   -> 從 $url 下載..." -ForegroundColor Gray
        Invoke-WebRequest -Uri $url -OutFile $tarFile -UseBasicParsing
        
        if (Test-Path $tarFile) {
            $fileSize = (Get-Item $tarFile).Length / 1MB
            Write-Host "   -> 下載完成: $($fileSize.ToString('F1')) MB" -ForegroundColor Green
            
            # 檢查是否已存在目錄，如果存在則刪除
            if (Test-Path $modelName) {
                Remove-Item -Recurse -Force $modelName
            }
            
            # 解壓縮 tar 文件
            Write-Host "   -> 解壓縮中..." -ForegroundColor Gray
            
            # 使用 tar 命令解壓縮 (Windows 10 1903+ 內建支援)
            if (Get-Command tar -ErrorAction SilentlyContinue) {
                tar -xf $tarFile
                Write-Host "   -> 解壓縮完成" -ForegroundColor Green
            } else {
                Write-Host "   -> 系統沒有 tar 命令，嘗試使用 7-Zip..." -ForegroundColor Yellow
                
                # 嘗試使用 7-Zip
                $sevenZipPaths = @(
                    "${env:ProgramFiles}\7-Zip\7z.exe",
                    "${env:ProgramFiles(x86)}\7-Zip\7z.exe"
                )
                
                $sevenZip = $sevenZipPaths | Where-Object { Test-Path $_ } | Select-Object -First 1
                
                if ($sevenZip) {
                    & $sevenZip x $tarFile -y
                    Write-Host "   -> 解壓縮完成" -ForegroundColor Green
                } else {
                    Write-Host "   -> 錯誤: 無法找到解壓縮工具 (tar 或 7-Zip)" -ForegroundColor Red
                    continue
                }
            }
            
            # 刪除 tar 文件
            Remove-Item $tarFile -Force
            
            # 驗證模型文件
            if (Test-Path "$modelName\inference.pdmodel") {
                $modelSize = (Get-Item "$modelName\inference.pdmodel").Length / 1MB
                Write-Host "   -> 模型驗證: $($modelSize.ToString('F1')) MB ✅" -ForegroundColor Green
            } else {
                Write-Host "   -> 警告: 模型文件不完整" -ForegroundColor Red
            }
        } else {
            Write-Host "   -> 錯誤: 下載失敗" -ForegroundColor Red
        }
    }
    catch {
        Write-Host "   -> 錯誤: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    Write-Host ""
}

Write-Host "🎉 PP-OCRv5 模型下載完成！" -ForegroundColor Green
Write-Host "📍 模型位置: $ModelsDir" -ForegroundColor Cyan

# 顯示下載結果
Write-Host "`n📊 下載結果摘要:" -ForegroundColor Yellow
foreach ($modelName in $models.Keys) {
    if (Test-Path "$ModelsDir\$modelName") {
        $files = Get-ChildItem "$ModelsDir\$modelName" | Measure-Object -Property Length -Sum
        $totalSize = $files.Sum / 1MB
        Write-Host "   ✅ $modelName ($($totalSize.ToString('F1')) MB)" -ForegroundColor Green
    } else {
        Write-Host "   ❌ $modelName (下載失敗)" -ForegroundColor Red
    }
}
