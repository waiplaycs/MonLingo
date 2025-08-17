@echo off
echo ========================================
echo MonLingo Native DLL Build Script
echo ========================================

REM 設置環境變數
set VSCMD_START_DIR=%CD%
set VSINSTALLDIR=C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools
set VCINSTALLDIR=%VSINSTALLDIR%\VC
set WindowsSdkDir=C:\Program Files (x86)\Windows Kits\10\
set WindowsSDKVersion=10.0.26100.0\

REM 設置 PATH
set PATH=%VCINSTALLDIR%\Tools\MSVC\14.44.35207\bin\Hostx64\x64;%PATH%
set PATH=%WindowsSdkDir%\bin\%WindowsSDKVersion%\x64;%PATH%

REM 設置 INCLUDE 路徑
set INCLUDE=%VCINSTALLDIR%\Tools\MSVC\14.44.35207\include
set INCLUDE=%INCLUDE%;%WindowsSdkDir%\Include\%WindowsSDKVersion%\ucrt
set INCLUDE=%INCLUDE%;%WindowsSdkDir%\Include\%WindowsSDKVersion%\shared
set INCLUDE=%INCLUDE%;%WindowsSdkDir%\Include\%WindowsSDKVersion%\um
set INCLUDE=%INCLUDE%;%WindowsSdkDir%\Include\%WindowsSDKVersion%\winrt

REM 設置 LIB 路徑
set LIB=%VCINSTALLDIR%\Tools\MSVC\14.44.35207\lib\x64
set LIB=%LIB%;%WindowsSdkDir%\Lib\%WindowsSDKVersion%\ucrt\x64
set LIB=%LIB%;%WindowsSdkDir%\Lib\%WindowsSDKVersion%\um\x64

echo 環境設置完成
echo.

REM 使用 MSBuild 建置
"%VSINSTALLDIR%\MSBuild\Current\Bin\MSBuild.exe" MonLingo.Native.vcxproj /p:Configuration=Debug /p:Platform=x64 /v:minimal

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================
    echo 建置成功！
    echo 輸出檔案: bin\x64\Debug\MonLingo.Native.dll
    echo ========================================
) else (
    echo.
    echo ========================================
    echo 建置失敗！錯誤代碼: %ERRORLEVEL%
    echo ========================================
)

pause
