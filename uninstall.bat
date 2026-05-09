@echo off
title FIT-TRACK PRO — Uninstaller
color 4F
cls

echo.
echo  =====================================================
echo   FIT-TRACK PRO — Uninstaller
echo  =====================================================
echo.
echo  This will remove the application files.
echo  Your database and data in SQL Server will NOT be deleted.
echo.
set /p CONFIRM= Type YES to confirm uninstall: 

if /I "%CONFIRM%" NEQ "YES" (
    echo  Uninstall cancelled.
    pause
    exit /b 0
)

set INSTALL_DIR=%LOCALAPPDATA%\FitTrackPro

:: Remove desktop shortcut
del /f /q "%USERPROFILE%\Desktop\FitTrack Pro.lnk" >nul 2>&1

:: Remove install folder
if exist "%INSTALL_DIR%" (
    rmdir /s /q "%INSTALL_DIR%"
    echo  Application files removed.
) else (
    echo  Install folder not found — already removed.
)

echo.
echo  Uninstall complete.
echo  Note: Your FitTrackOOP database in SQL Server is still intact.
echo        To remove it, open SSMS and run: DROP DATABASE FitTrackOOP;
echo.
pause
