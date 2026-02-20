@echo off
echo ========================================
echo Jewochron Icon Generator
echo ========================================
echo.
echo Choose an option:
echo.
echo 1 - Generate Professional Icons (requires Inkscape)
echo 2 - Create Quick Placeholder Icons
echo 3 - Open Icon Design Instructions
echo 4 - Exit
echo.
set /p choice=Enter your choice (1-4): 

if "%choice%"=="1" (
    echo.
    echo Launching professional icon generator...
    powershell -ExecutionPolicy Bypass -File "%~dp0Generate-Icons.ps1"
) else if "%choice%"=="2" (
    echo.
    echo Launching quick icon generator...
    powershell -ExecutionPolicy Bypass -File "%~dp0Create-Quick-Icon.ps1"
) else if "%choice%"=="3" (
    echo.
    echo Opening instructions...
    start "" "%~dp0README.md"
) else if "%choice%"=="4" (
    echo.
    echo Goodbye!
    timeout /t 2 >nul
    exit
) else (
    echo.
    echo Invalid choice. Please run again and select 1-4.
    timeout /t 3 >nul
)
