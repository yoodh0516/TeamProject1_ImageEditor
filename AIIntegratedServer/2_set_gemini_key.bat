@echo off
setlocal EnableExtensions
cd /d "%~dp0"

echo ====================================================
echo Gemini API Key Setup
echo Skip this file if you do not use music recommendation.
echo ====================================================
echo.

if not exist ".venv311\Scripts\python.exe" (
    echo ERROR: Server packages are not installed.
    echo Run 1_setup_server.bat first.
    pause
    exit /b 1
)

set /p "GEMINI_KEY=Paste your Gemini API key and press Enter: "

if not defined GEMINI_KEY (
    echo No API key was entered. Setup cancelled.
    pause
    exit /b 1
)

set "GEMINI_API_KEY=%GEMINI_KEY%"
"%~dp0.venv311\Scripts\python.exe" -c "import os; from server import save_gemini_api_key; save_gemini_api_key(os.environ['GEMINI_API_KEY']); print('Gemini API key check OK')"
if errorlevel 1 (
    echo Failed to save the API key.
    pause
    exit /b 1
)

set "GEMINI_API_KEY="
set "GEMINI_KEY="
echo.
echo API key saved. You can use music recommendation now.
echo The server does not need to be restarted.
pause
exit /b 0
