# MonLingo PaddleOCR 自動化環境設置
param(
    [switch]$DownloadModels = $true,
    [switch]$ConfigureProject = $true,
    [switch]$Force = $false
)

$ErrorActionPreference = "Stop"

# 基本路徑設置
$BaseDir = Split-Path -Parent $PSScriptRoot
$ThirdPartyDir = Join-Path $BaseDir "third_party"
$ModelsDir = Join-Path $BaseDir "models"
$PaddleDir = Join-Path $ThirdPartyDir "paddle_inference"

Write-Host "=== MonLingo PaddleOCR 環境設置 ===" -ForegroundColor Green
Write-Host "基礎目錄: $BaseDir" -ForegroundColor Cyan

# 創建必要目錄
@($ThirdPartyDir, $ModelsDir, $PaddleDir) | ForEach-Object {
    if (-not (Test-Path $_)) {
        New-Item -ItemType Directory -Path $_ -Force
        Write-Host "已創建目錄: $_" -ForegroundColor Yellow
    }
}

# 1. 下載並配置 PaddleOCR 模型
if ($DownloadModels) {
    Write-Host "`n=== 第 1 步: 下載 PaddleOCR 模型 ===" -ForegroundColor Green
    
    $ModelConfigs = @{
        "ch_PP-OCRv4_det_infer" = "https://paddleocr.bj.bcebos.com/PP-OCRv4/chinese/ch_PP-OCRv4_det_infer.tar"
        "ch_PP-OCRv4_rec_infer" = "https://paddleocr.bj.bcebos.com/PP-OCRv4/chinese/ch_PP-OCRv4_rec_infer.tar"
        "ch_ppocr_mobile_v2.0_cls_infer" = "https://paddleocr.bj.bcebos.com/dygraph_v2.0/ch/ch_ppocr_mobile_v2.0_cls_infer.tar"
    }
    
    foreach ($model in $ModelConfigs.GetEnumerator()) {
        $ModelPath = Join-Path $ModelsDir $model.Key
        $TarFile = Join-Path $ModelsDir "$($model.Key).tar"
        
        if ((Test-Path $ModelPath) -and -not $Force) {
            Write-Host "模型已存在，跳過: $($model.Key)" -ForegroundColor Yellow
            continue
        }
        
        try {
            Write-Host "正在下載: $($model.Key)" -ForegroundColor Green
            Invoke-WebRequest -Uri $model.Value -OutFile $TarFile -UseBasicParsing
            
            # 解壓 tar 檔案
            if (Get-Command tar -ErrorAction SilentlyContinue) {
                Push-Location $ModelsDir
                tar -xf $TarFile
                Pop-Location
                Remove-Item $TarFile -Force
                Write-Host "✓ $($model.Key) 下載完成" -ForegroundColor Green
            } else {
                Write-Warning "未找到 tar 命令，請手動解壓: $TarFile"
            }
        }
        catch {
            Write-Error "下載失敗: $($model.Key) - $_"
        }
    }
    
    # 下載字典文件
    $KeysFile = Join-Path $ModelsDir "ppocr_keys_v1.txt"
    if (-not (Test-Path $KeysFile) -or $Force) {
        try {
            Write-Host "正在下載字典文件..." -ForegroundColor Green
            Invoke-WebRequest -Uri "https://raw.githubusercontent.com/PaddlePaddle/PaddleOCR/release/2.6/ppocr/utils/ppocr_keys_v1.txt" -OutFile $KeysFile -UseBasicParsing
            Write-Host "✓ 字典文件下載完成" -ForegroundColor Green
        }
        catch {
            Write-Error "字典文件下載失敗: $_"
        }
    }
}

# 2. 下載 Paddle Inference 庫 (手動步驟指導)
Write-Host "`n=== 第 2 步: Paddle Inference 庫設置 ===" -ForegroundColor Green

$PaddleLibPath = Join-Path $PaddleDir "paddle"
if (-not (Test-Path $PaddleLibPath)) {
    Write-Host "🔴 需要手動下載 Paddle Inference 庫" -ForegroundColor Red
    Write-Host ""
    Write-Host "請執行以下步驟:" -ForegroundColor Yellow
    Write-Host "1. 訪問: https://www.paddlepaddle.org.cn/inference/master/guides/install/download_lib.html#windows" -ForegroundColor White
    Write-Host "2. 下載: paddle_inference-win-x64-2.5.1.zip (約 150MB)" -ForegroundColor White
    Write-Host "3. 解壓到: $PaddleDir" -ForegroundColor White
    Write-Host "4. 確認結構: $PaddleLibPath\include\ 和 $PaddleLibPath\lib\" -ForegroundColor White
    Write-Host ""
    Write-Warning "完成手動下載後重新運行此腳本"
} else {
    Write-Host "✓ Paddle Inference 庫已存在" -ForegroundColor Green
}

# 3. 配置 Visual Studio 專案
if ($ConfigureProject -and (Test-Path $PaddleLibPath)) {
    Write-Host "`n=== 第 3 步: 配置 Visual Studio 專案 ===" -ForegroundColor Green
    
    $VcxprojPath = Join-Path $BaseDir "src\MonLingo.Native\MonLingo.Native.vcxproj"
    
    if (Test-Path $VcxprojPath) {
        try {
            # 讀取專案文件
            [xml]$vcxproj = Get-Content $VcxprojPath
            
            # 檢查是否已包含 PaddleOCR 配置
            $hasConfig = $vcxproj.SelectNodes("//AdditionalIncludeDirectories") | 
                         Where-Object { $_.InnerText -like "*paddle_inference*" }
            
            if (-not $hasConfig -or $Force) {
                Write-Host "正在更新專案配置..." -ForegroundColor Yellow
                
                # 備份原始文件
                Copy-Item $VcxprojPath "$VcxprojPath.backup" -Force
                
                # TODO: 這裡需要更詳細的 XML 操作來更新專案配置
                # 由於 XML 操作複雜，建議手動配置或使用專門的工具
                
                Write-Host "⚠️  請手動更新專案配置，參考:" -ForegroundColor Yellow
                Write-Host "   docs\PaddleOCR_依賴整合指南.md" -ForegroundColor White
            } else {
                Write-Host "✓ 專案已包含 PaddleOCR 配置" -ForegroundColor Green
            }
        }
        catch {
            Write-Warning "專案配置更新失敗，請手動配置: $_"
        }
    } else {
        Write-Warning "找不到專案文件: $VcxprojPath"
    }
}

# 4. 驗證設置
Write-Host "`n=== 第 4 步: 驗證設置 ===" -ForegroundColor Green

$CheckList = @(
    @{ Path = $ModelsDir; Name = "模型目錄" }
    @{ Path = (Join-Path $ModelsDir "ch_PP-OCRv4_det_infer"); Name = "檢測模型" }
    @{ Path = (Join-Path $ModelsDir "ch_PP-OCRv4_rec_infer"); Name = "識別模型" }
    @{ Path = (Join-Path $ModelsDir "ch_ppocr_mobile_v2.0_cls_infer"); Name = "分類模型" }
    @{ Path = (Join-Path $ModelsDir "ppocr_keys_v1.txt"); Name = "字典文件" }
    @{ Path = $PaddleLibPath; Name = "Paddle Inference 庫" }
    @{ Path = (Join-Path $PaddleLibPath "include"); Name = "Paddle 標頭檔" }
    @{ Path = (Join-Path $PaddleLibPath "lib"); Name = "Paddle 庫文件" }
)

$AllOK = $true
foreach ($item in $CheckList) {
    if (Test-Path $item.Path) {
        Write-Host "✓ $($item.Name)" -ForegroundColor Green
    } else {
        Write-Host "✗ $($item.Name) (缺失: $($item.Path))" -ForegroundColor Red
        $AllOK = $false
    }
}

# 總結報告
Write-Host "`n=== 設置完成報告 ===" -ForegroundColor Green

if ($AllOK) {
    Write-Host "🎉 所有組件設置完成！" -ForegroundColor Green
    Write-Host ""
    Write-Host "下一步操作:" -ForegroundColor Yellow
    Write-Host "1. 檢查專案配置 (參考 docs\PaddleOCR_依賴整合指南.md)" -ForegroundColor White
    Write-Host "2. 編譯專案: msbuild MonLingo.sln /p:Configuration=Release" -ForegroundColor White
    Write-Host "3. 執行測試驗證" -ForegroundColor White
} else {
    Write-Host "⚠️  設置未完全成功，請檢查上述缺失項目" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "常見解決方案:" -ForegroundColor Yellow
    Write-Host "1. 確認網路連線正常" -ForegroundColor White
    Write-Host "2. 手動下載 Paddle Inference 庫" -ForegroundColor White
    Write-Host "3. 檢查權限設置" -ForegroundColor White
}

Write-Host "`n=== 腳本執行完成 ===" -ForegroundColor Green
Write-Host "執行時間: $(Get-Date)" -ForegroundColor Gray
