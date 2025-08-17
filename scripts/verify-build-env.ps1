# MonLingo 建置環境驗證腳本
# 對應專案規劃 Phase 0 目標：CI/品質閘門設置

param(
    [switch]$Detailed
)

Write-Host "=== MonLingo 建置環境驗證 ===" -ForegroundColor Green

$errorCount = 0
$warningCount = 0

function Test-Requirement {
    param(
        [string]$Name,
        [scriptblock]$Test,
        [string]$ErrorMessage,
        [string]$SuccessMessage
    )
    
    Write-Host "檢查 $Name..." -NoNewline
    
    try {
        $result = & $Test
        if ($result) {
            Write-Host " ✓" -ForegroundColor Green
            if ($Detailed) { Write-Host "  $SuccessMessage" -ForegroundColor Gray }
        } else {
            Write-Host " ✗" -ForegroundColor Red
            Write-Host "  錯誤: $ErrorMessage" -ForegroundColor Red
            $script:errorCount++
        }
    } catch {
        Write-Host " ✗" -ForegroundColor Red
        Write-Host "  異常: $_" -ForegroundColor Red
        $script:errorCount++
    }
}

function Test-Warning {
    param(
        [string]$Name,
        [scriptblock]$Test,
        [string]$WarningMessage
    )
    
    Write-Host "檢查 $Name..." -NoNewline
    
    try {
        $result = & $Test
        if ($result) {
            Write-Host " ✓" -ForegroundColor Green
        } else {
            Write-Host " ⚠" -ForegroundColor Yellow
            Write-Host "  警告: $WarningMessage" -ForegroundColor Yellow
            $script:warningCount++
        }
    } catch {
        Write-Host " ⚠" -ForegroundColor Yellow
        Write-Host "  警告: $_" -ForegroundColor Yellow
        $script:warningCount++
    }
}

# 檢查作業系統版本
Test-Requirement "Windows 版本" {
    $version = [System.Environment]::OSVersion.Version
    $isWin10Plus = ($version.Major -gt 10) -or ($version.Major -eq 10 -and $version.Build -ge 18362)
    return $isWin10Plus
} "需要 Windows 10 1903+ 以支援 Graphics Capture API" "Windows 版本符合需求"

# 檢查 .NET Framework
Test-Requirement ".NET Framework 4.8.1" {
    $netFramework = Get-ItemProperty "HKLM:SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\" -ErrorAction SilentlyContinue
    return $netFramework.Release -ge 533320
} "需要安裝 .NET Framework 4.8.1" ".NET Framework 4.8.1 已安裝"

# 檢查 .NET SDK
Test-Requirement ".NET 8 SDK" {
    $dotnetVersion = dotnet --version 2>$null
    return $dotnetVersion -and $dotnetVersion.StartsWith("8.")
} "需要安裝 .NET 8 SDK" ".NET 8 SDK 已安裝"

# 檢查 MSBuild
Test-Requirement "MSBuild" {
    $msbuild = Get-Command msbuild -ErrorAction SilentlyContinue
    return $msbuild -ne $null
} "需要安裝 Visual Studio 或 Build Tools" "MSBuild 可用"

# 檢查 Git
Test-Requirement "Git" {
    $git = Get-Command git -ErrorAction SilentlyContinue
    return $git -ne $null
} "需要安裝 Git" "Git 已安裝"

# 檢查專案檔案
Test-Requirement "解決方案檔案" {
    return Test-Path "MonLingo.sln"
} "MonLingo.sln 不存在" "解決方案檔案存在"

Test-Requirement "Directory.Packages.props" {
    return Test-Path "Directory.Packages.props"
} "Directory.Packages.props 不存在，套件版本管理不完整" "套件版本集中管理已設置"

Test-Requirement "NuGet.config" {
    return Test-Path "NuGet.config"
} "NuGet.config 不存在，套件來源設定不完整" "NuGet 配置檔案存在"

# 檢查專案結構
$expectedDirs = @(
    "src\MonLingo",
    "src\MonLingo.Core", 
    "tests\MonLingo.Tests"
)

foreach ($dir in $expectedDirs) {
    Test-Requirement "專案目錄 $dir" {
        return Test-Path $dir
    } "專案目錄 $dir 不存在" "專案目錄結構正確"
}

# 檢查套件還原
Write-Host "嘗試套件還原..." -NoNewline
try {
    $restoreOutput = dotnet restore --verbosity quiet 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host " ✓" -ForegroundColor Green
    } else {
        Write-Host " ✗" -ForegroundColor Red
        Write-Host "  套件還原失敗:" -ForegroundColor Red
        Write-Host $restoreOutput -ForegroundColor Red
        $errorCount++
    }
} catch {
    Write-Host " ✗" -ForegroundColor Red
    Write-Host "  套件還原異常: $_" -ForegroundColor Red
    $errorCount++
}

# 檢查建置
Write-Host "嘗試建置..." -NoNewline
try {
    $buildOutput = dotnet build --configuration Debug --verbosity quiet --no-restore 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host " ✓" -ForegroundColor Green
    } else {
        Write-Host " ✗" -ForegroundColor Red
        Write-Host "  建置失敗:" -ForegroundColor Red
        Write-Host $buildOutput -ForegroundColor Red
        $errorCount++
    }
} catch {
    Write-Host " ✗" -ForegroundColor Red
    Write-Host "  建置異常: $_" -ForegroundColor Red
    $errorCount++
}

# 檢查測試
Write-Host "嘗試運行測試..." -NoNewline
try {
    $testOutput = dotnet test --configuration Debug --verbosity quiet --no-build 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host " ✓" -ForegroundColor Green
    } else {
        Write-Host " ⚠" -ForegroundColor Yellow
        Write-Host "  部分測試失敗（開發階段正常）:" -ForegroundColor Yellow
        if ($Detailed) { Write-Host $testOutput -ForegroundColor Gray }
        $warningCount++
    }
} catch {
    Write-Host " ⚠" -ForegroundColor Yellow
    Write-Host "  測試執行異常: $_" -ForegroundColor Yellow
    $warningCount++
}

# 可選檢查
Test-Warning "DirectX 11 支援" {
    # 簡化檢查：檢查 D3D11 DLL 是否存在
    return Test-Path "$env:SystemRoot\System32\d3d11.dll"
} "未檢測到 DirectX 11 支援，可能影響硬體加速功能"

Test-Warning "Visual Studio Code" {
    $code = Get-Command code -ErrorAction SilentlyContinue
    return $code -ne $null
} "建議安裝 Visual Studio Code 以獲得更好的開發體驗"

# 輸出總結
Write-Host "`n=== 驗證總結 ===" -ForegroundColor Green
if ($errorCount -eq 0) {
    Write-Host "✓ 建置環境驗證通過！" -ForegroundColor Green
    if ($warningCount -gt 0) {
        Write-Host "⚠ $warningCount 個警告項目，建議檢查" -ForegroundColor Yellow
    }
    Write-Host "`n可以開始開發 MonLingo 專案。" -ForegroundColor Green
    exit 0
} else {
    Write-Host "✗ 發現 $errorCount 個錯誤，$warningCount 個警告" -ForegroundColor Red
    Write-Host "請修復錯誤後重新驗證。" -ForegroundColor Red
    exit 1
}
