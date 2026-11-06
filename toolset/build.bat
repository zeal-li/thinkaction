@echo off
setlocal
cd /d %~dp0

echo ==========================================================
echo   ToolSet - Build Debug
echo ==========================================================
echo.

if exist "build" rd /s /q "build"

dotnet build src\ToolSet.csproj -c Debug -o build --nologo

if errorlevel 1 (
    echo.
    echo [ERROR] Build failed, see the message above.
    pause
    exit /b 1
)

echo.
echo [OK] Output: %~dp0build\ToolSet.exe
pause
