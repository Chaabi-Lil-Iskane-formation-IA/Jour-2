@echo off
echo ========================================
echo Whisper Model Downloader (Base Model)
echo ========================================
echo.

REM Check if model already exists
if exist "ggml-medium.bin" (
    echo Model already exists: ggml-medium.bin
    echo.
    set /p overwrite="Overwrite? (y/n): "
    if /i not "%overwrite%"=="y" (
        echo Download cancelled.
        pause
        exit
    )
)

echo Downloading ggml-medium.bin (~140MB)...
echo This may take a few minutes...
echo.

powershell -Command "Invoke-WebRequest -Uri 'https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-medium.bin' -OutFile 'ggml-medium.bin'"

if exist "ggml-medium.bin" (
    echo.
    echo ✓ Download completed successfully!
    echo Model saved to: ggml-medium.bin
    echo.
    echo You can now run: dotnet run
) else (
    echo.
    echo × Download failed!
    echo.
    echo Try downloading manually from:
    echo https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-medium.bin
)

echo.
pause