@echo off
setlocal EnableExtensions
cd /d "%~dp0"

echo ====================================================
echo ImageEditor AI Server Setup for Python 3.11.9
echo Run this file only once.
echo ====================================================

set "PYTHON_LAUNCHER="
if exist ".python311\python.exe" (
    ".python311\python.exe" -c "import struct, sys; raise SystemExit(0 if sys.version_info[:3] == (3, 11, 9) and struct.calcsize('P') == 8 else 1)" >nul 2>&1
    if not errorlevel 1 set PYTHON_LAUNCHER="%~dp0.python311\python.exe"
)

if not defined PYTHON_LAUNCHER (
    py -3.11 -c "import struct, sys; raise SystemExit(0 if sys.version_info[:3] == (3, 11, 9) and struct.calcsize('P') == 8 else 1)" >nul 2>&1
    if not errorlevel 1 set "PYTHON_LAUNCHER=py -3.11"
)

if not defined PYTHON_LAUNCHER (
    python -c "import struct, sys; raise SystemExit(0 if sys.version_info[:3] == (3, 11, 9) and struct.calcsize('P') == 8 else 1)" >nul 2>&1
    if not errorlevel 1 set "PYTHON_LAUNCHER=python"
)

if not defined PYTHON_LAUNCHER goto python_missing

echo [1/6] Python 3.11.9 64-bit found:
%PYTHON_LAUNCHER% -c "import sys; print(sys.version)"
if errorlevel 1 goto failed

if exist ".venv311\Scripts\python.exe" (
    ".venv311\Scripts\python.exe" -c "import sys; raise SystemExit(0 if sys.version_info[:3] == (3, 11, 9) else 1)" >nul 2>&1
    if errorlevel 1 (
        echo Removing an incompatible virtual environment...
        rmdir /s /q ".venv311"
        if errorlevel 1 goto failed
    )
)

if exist ".venv311" if not exist ".venv311\Scripts\python.exe" (
    echo Removing an incomplete virtual environment...
    rmdir /s /q ".venv311"
    if errorlevel 1 goto failed
)

if not exist ".venv311\Scripts\python.exe" (
    echo [2/6] Creating the Python 3.11.9 virtual environment...
    %PYTHON_LAUNCHER% -m venv .venv311
    if errorlevel 1 goto failed
) else (
    echo [2/6] Using the existing Python 3.11.9 virtual environment.
)

set "SERVER_PYTHON=%~dp0.venv311\Scripts\python.exe"

echo [3/6] Updating pip...
"%SERVER_PYTHON%" -m pip install --upgrade pip setuptools wheel
if errorlevel 1 goto failed

echo [4/6] Installing CPU-only PyTorch for Python 3.11.9...
echo This is a large download and can take a while.
"%SERVER_PYTHON%" -m pip install torch==2.12.1 torchvision==0.27.1 --index-url https://download.pytorch.org/whl/cpu
if errorlevel 1 goto failed

echo [5/6] Installing Python 3.11 compatible AI packages...
"%SERVER_PYTHON%" -m pip install --prefer-binary -r requirements.txt
if errorlevel 1 goto failed

echo Keeping only one OpenCV package to prevent cv2 conflicts...
"%SERVER_PYTHON%" -m pip uninstall -y opencv-python opencv-python-headless opencv-contrib-python opencv-contrib-python-headless >nul 2>&1
"%SERVER_PYTHON%" -m pip install --no-deps opencv-contrib-python-headless==4.13.0.92
if errorlevel 1 goto failed

echo [6/6] Checking installed packages...
"%SERVER_PYTHON%" -c "import os, struct, sys; assert sys.version_info[:3] == (3, 11, 9) and struct.calcsize('P') == 8; import cv2, fastapi, mediapipe, numpy, onnxruntime, PIL, requests, torch, uvicorn; from ultralytics import SAM; import server; assert hasattr(cv2, 'dnn_superres'); assert hasattr(server, 'save_replicate_api_token'); assert 'u2netp.onnx' in server.MODEL_FILES; assert 'blaze_face_full_range.tflite' in server.MODEL_FILES; assert os.name != 'nt' or str(server.MODEL_DIR).isascii(); print('Package check OK - Python', sys.version.split()[0])"
if errorlevel 1 goto failed

echo.
echo ====================================================
echo SETUP COMPLETED SUCCESSFULLY
echo For high-quality photo restoration, run 2_set_replicate_token.bat.
echo Then run 3_start_server.bat.
echo ====================================================
pause
exit /b 0

:python_missing
echo.
echo ERROR: Python 3.11.9 64-bit was not found.
echo Your Python must be exactly 3.11.9 and 64-bit.
echo If Python is already installed, close and reopen Windows Explorer.
echo During Python installation, select:
echo Add python.exe to PATH
echo Then run this file again.
pause
exit /b 1

:failed
echo.
echo ERROR: Package installation failed.
echo Check the error shown above and your internet connection.
echo If you send a screenshot, include the first red ERROR line.
pause
exit /b 1
