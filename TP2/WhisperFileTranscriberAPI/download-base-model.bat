@echo off
echo ========================================
echo Whisper Model Downloader (Base Model)
echo ========================================
echo.

REM Check if model already exists
if exist "ggml-base.bin" (
    echo Model already exists: ggml-base.bin
    echo.
    set /p overwrite="Overwrite? (y/n): "
    if /i not "%overwrite%"=="y" (
        echo Download cancelled.
        pause
        exit
    )
)

echo Downloading ggml-base.bin (~140MB)...
echo This may take a few minutes...
echo.

powershell -Command "Invoke-WebRequest -Uri 'https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin' -OutFile 'ggml-base.bin'"

if exist "ggml-base.bin" (
    echo.
    echo ✓ Download completed successfully!
    echo Model saved to: ggml-base.bin
    echo.
    echo You can now run: dotnet run
) else (
    echo.
    echo × Download failed!
    echo.
    echo Try downloading manually from:
    echo https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin
)

echo.
pause