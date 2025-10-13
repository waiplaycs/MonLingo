# MonLingo 編譯腳本
$ErrorActionPreference = "Stop"

Write-Host "🔍 搜索 MSBuild..." -ForegroundColor Cyan

# 查找 MSBuild
$msbuildPaths = @(
    "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
)

$msbuild = $null
foreach ($path in $msbuildPaths) {
    if (Test-Path $path) {
        $msbuild = $path
        Write-Host "✅ 找到 MSBuild: $msbuild" -ForegroundColor Green
        break
    }
}

if (-not $msbuild) {
    Write-Host "❌ 找不到 MSBuild.exe" -ForegroundColor Red
    exit 1
}

# 編譯項目
Write-Host "🔨 開始編譯 MonLingo..." -ForegroundColor Cyan
$slnPath = Join-Path $PSScriptRoot "MonLingo.sln"

& $msbuild $slnPath /p:Configuration=Debug /p:Platform=x64 /t:Build /m /clp:ErrorsOnly

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ 編譯成功!" -ForegroundColor Green
} else {
    Write-Host "❌ 編譯失敗，退出碼: $LASTEXITCODE" -ForegroundColor Red
    exit $LASTEXITCODE
}
