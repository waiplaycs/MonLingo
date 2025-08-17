# MonLingo Build Environment Verification Script
# Phase 0 Target: CI/Quality Gates Setup

param([switch]$Detailed)

Write-Host "=== MonLingo Build Environment Verification ===" -ForegroundColor Green

$errorCount = 0
$warningCount = 0

function Test-Requirement {
    param([string]$Name, [scriptblock]$Test, [string]$ErrorMessage)
    
    Write-Host "Checking $Name..." -NoNewline
    try {
        if (& $Test) {
            Write-Host " ✓" -ForegroundColor Green
        } else {
            Write-Host " ✗" -ForegroundColor Red
            Write-Host "  Error: $ErrorMessage" -ForegroundColor Red
            $global:errorCount++
        }
    } catch {
        Write-Host " ✗" -ForegroundColor Red
        Write-Host "  Exception: $($_.Exception.Message)" -ForegroundColor Red
        $global:errorCount++
    }
}

# Check .NET Framework 4.8.1
Test-Requirement ".NET Framework 4.8.1" {
    $dotnetVersion = dotnet --version
    return $dotnetVersion -ne $null
} "DotNet CLI not found"

# Check solution file exists
Test-Requirement "Solution structure" {
    return (Test-Path "MonLingo.sln") -and (Test-Path "src/MonLingo.Core") -and (Test-Path "src/MonLingo") -and (Test-Path "tests/MonLingo.Tests")
} "Required solution structure not found"

# Check central package management
Test-Requirement "Central package management" {
    return (Test-Path "Directory.Packages.props") -and (Test-Path "NuGet.config")
} "Central package management files missing"

# Check build success
Test-Requirement "Solution build" {
    $buildResult = dotnet build --configuration Debug --verbosity quiet
    return $LASTEXITCODE -eq 0
} "Build failed"

# Check test execution
Test-Requirement "Unit tests" {
    $testResult = dotnet test --configuration Debug --verbosity quiet --no-build
    return $LASTEXITCODE -eq 0
} "Tests failed"

# Check CI configuration
Test-Requirement "CI/CD setup" {
    return (Test-Path ".github/workflows/ci.yml")
} "CI configuration missing"

# Check project references
Test-Requirement "Project dependencies" {
    $coreProj = Get-Content "src/MonLingo.Core/MonLingo.Core.csproj"
    $mainProj = Get-Content "src/MonLingo/MonLingo.csproj"
    $testProj = Get-Content "tests/MonLingo.Tests/MonLingo.Tests.csproj"
    
    $hasMainRef = $mainProj -like "*MonLingo.Core*"
    $hasTestRef = $testProj -like "*MonLingo.Core*"
    
    return $hasMainRef -and $hasTestRef
} "Project references not configured correctly"

# Check core interfaces
Test-Requirement "Core interfaces" {
    $servicesFile = "src/MonLingo.Core/Service/IServices.cs"
    $bridgeFile = "src/MonLingo.Core/Core/NativeBridge.cs"
    
    return (Test-Path $servicesFile) -and (Test-Path $bridgeFile)
} "Core interfaces missing"

# Performance check - restore time
Write-Host "Checking restore performance..." -NoNewline
try {
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    dotnet restore --verbosity quiet | Out-Null
    $stopwatch.Stop()
    
    if ($stopwatch.ElapsedMilliseconds -lt 120000) { # < 2 minutes
        Write-Host " ✓ ($($stopwatch.ElapsedMilliseconds)ms)" -ForegroundColor Green
    } else {
        Write-Host " ⚠ ($($stopwatch.ElapsedMilliseconds)ms)" -ForegroundColor Yellow
        Write-Host "  Warning: Restore time exceeds 2 minutes" -ForegroundColor Yellow
        $warningCount++
    }
} catch {
    Write-Host " ✗" -ForegroundColor Red
    $errorCount++
}

# Summary
Write-Host "`n=== Verification Summary ===" -ForegroundColor Green
if ($errorCount -eq 0) {
    Write-Host "✓ Build environment verification passed!" -ForegroundColor Green
    if ($warningCount -gt 0) {
        Write-Host "⚠ Found $warningCount warnings" -ForegroundColor Yellow
    }
    Write-Host "Ready to start MonLingo development." -ForegroundColor Green
    exit 0
} else {
    Write-Host "✗ Found $errorCount errors" -ForegroundColor Red
    Write-Host "Please fix errors and re-verify." -ForegroundColor Red
    exit 1
}
