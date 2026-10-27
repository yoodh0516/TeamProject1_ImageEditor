@echo off
setlocal EnableExtensions
cd /d "%~dp0"

echo ====================================================
echo Replicate API Token Setup for Photo Restoration
echo ====================================================
echo.

if not exist ".venv311\Scripts\python.exe" (
    echo ERROR: Server packages are not installed.
    echo Run 1_setup_server.bat first.
    pause
    exit /b 1
)

set /p "REPLICATE_TOKEN=Paste your Replicate API token and press Enter: "

if not defined REPLICATE_TOKEN (
    echo No API token was entered. Setup cancelled.
    pause
    exit /b 1
)

set "REPLICATE_API_TOKEN=%REPLICATE_TOKEN%"
"%~dp0.venv311\Scripts\python.exe" -c "import os; from server import save_replicate_api_token; save_replicate_api_token(os.environ['REPLICATE_API_TOKEN']); print('Replicate API token saved')"
if errorlevel 1 (
    echo Failed to save the API token.
    pause
    exit /b 1
)

set "REPLICATE_API_TOKEN="
set "REPLICATE_TOKEN="
echo.
echo API token saved. Replicate photo restoration is ready.
echo The server does not need to be restarted.
pause
exit /b 0
