@echo off
title FIT-TRACK PRO — Installer
color 1F
cls

echo.
echo  =====================================================
echo   FIT-TRACK PRO — Installation Wizard
echo   Universal Health ^& Fitness Management System
echo  =====================================================
echo.

:: ── Step 1: Check Administrator ──────────────────────────
net session >nul 2>&1
if %errorLevel% NEQ 0 (
    echo  [ERROR] Please right-click this file and choose
    echo          "Run as administrator" then try again.
    echo.
    pause
    exit /b 1
)

echo  [1/6] Checking administrator rights ... OK
echo.

:: ── Step 2: Check .NET 8 ─────────────────────────────────
dotnet --version >nul 2>&1
if %errorLevel% NEQ 0 (
    echo  [ERROR] .NET 8 Runtime is not installed.
    echo.
    echo  Please download and install it from:
    echo  https://dotnet.microsoft.com/download/dotnet/8.0
    echo.
    echo  Choose: .NET 8.0  ^>  Windows  ^>  x64 Installer
    echo  After installing, run this installer again.
    echo.
    pause
    exit /b 1
)

echo  [2/6] .NET Runtime detected ... OK
echo.

:: ── Step 3: Check SQL Server ──────────────────────────────
sc query MSSQL$SQLEXPRESS >nul 2>&1
if %errorLevel% NEQ 0 (
    sc query MSSQLSERVER >nul 2>&1
    if %errorLevel% NEQ 0 (
        echo  [WARNING] SQL Server service not detected.
        echo.
        echo  Please install SQL Server Express (free) from:
        echo  https://www.microsoft.com/sql-server/sql-server-downloads
        echo.
        echo  Choose "Basic" installation type.
        echo  Then run this installer again.
        echo.
        pause
        exit /b 1
    )
)

echo  [3/6] SQL Server detected ... OK
echo.

:: ── Step 4: Set install location ─────────────────────────
set INSTALL_DIR=%LOCALAPPDATA%\FitTrackPro
set SOURCE_DIR=%~dp0FitTrack

echo  [4/6] Installing to: %INSTALL_DIR%
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"
echo.

:: ── Step 5: Publish self-contained exe ───────────────────
echo  [5/6] Building application (this takes 1-2 minutes) ...
echo.

dotnet publish "%SOURCE_DIR%\FitTrack.csproj" ^
    --configuration Release ^
    --runtime win-x64 ^
    --self-contained true ^
    --output "%INSTALL_DIR%" ^
    /p:PublishSingleFile=true ^
    /p:IncludeNativeLibrariesForSelfExtract=true ^
    >"%INSTALL_DIR%\install_log.txt" 2>&1

if %errorLevel% NEQ 0 (
    echo  [ERROR] Build failed. See log: %INSTALL_DIR%\install_log.txt
    echo.
    type "%INSTALL_DIR%\install_log.txt"
    echo.
    pause
    exit /b 1
)

echo  Build complete.
echo.

:: ── Step 6: Copy schema.sql ───────────────────────────────
copy /Y "%SOURCE_DIR%\Database\schema.sql" "%INSTALL_DIR%\schema.sql" >nul
copy /Y "%~dp0README.md"                   "%INSTALL_DIR%\README.md"  >nul 2>&1

echo  [6/6] Creating desktop shortcut ...

:: Create shortcut via PowerShell
powershell -NoProfile -Command ^
    "$ws = New-Object -ComObject WScript.Shell; ^
     $sc = $ws.CreateShortcut([System.IO.Path]::Combine([System.Environment]::GetFolderPath('Desktop'), 'FitTrack Pro.lnk')); ^
     $sc.TargetPath = '%INSTALL_DIR%\FitTrack.exe'; ^
     $sc.WorkingDirectory = '%INSTALL_DIR%'; ^
     $sc.Description = 'FIT-TRACK PRO - Health and Fitness Manager'; ^
     $sc.Save()"

echo.
echo  =====================================================
echo   Installation Complete!
echo  =====================================================
echo.
echo   App installed to: %INSTALL_DIR%
echo   Desktop shortcut: "FitTrack Pro" created
echo.
echo   IMPORTANT — Before running the app:
echo   1. Open SSMS (SQL Server Management Studio)
echo   2. Connect to your SQL Server instance
echo   3. Open and run: %INSTALL_DIR%\schema.sql
echo   4. Then double-click "FitTrack Pro" on your desktop
echo.
echo   Full setup guide: See the included PDF manual.
echo.
pause
