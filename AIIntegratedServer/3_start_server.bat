@echo off
setlocal EnableExtensions
cd /d "%~dp0"

if not exist ".venv311\Scripts\python.exe" (
    echo ERROR: Server packages are not installed.
    echo Run 1_setup_server.bat first.
    pause
    exit /b 1
)

set "PYTHONUTF8=1"
set "PYTHONUNBUFFERED=1"
"%~dp0.venv311\Scripts\python.exe" -c "import sys; raise SystemExit(0 if sys.version_info[:3] == (3, 11, 9) else 1)" >nul 2>&1
if errorlevel 1 (
    echo ERROR: The server virtual environment is not Python 3.11.9.
    echo Run 1_setup_server.bat again.
    pause
    exit /b 1
)

echo Starting the ImageEditor AI server...
echo Keep this window open while using ImageEditor.
echo Press Ctrl+C to stop the server.
echo.
"%~dp0.venv311\Scripts\python.exe" "%~dp0server.py"

echo.
echo The AI server has stopped.
pause
