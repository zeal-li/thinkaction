@echo off
setlocal
cd /d %~dp0

echo ==========================================================
echo   ToolSet - Build Debug
echo ==========================================================
echo.

if not exist "build" mkdir "build"

dotnet build src\ToolSet.csproj -c Debug -o build --nologo

if errorlevel 1 (
    echo.
    echo [ERROR] Build failed, see the message above.
    pause
    exit /b 1
)

echo.
echo Copying scripts...
if exist "build\scripts" rd /s /q "build\scripts"
xcopy "scripts" "build\scripts" /e /i /y >nul
if errorlevel 1 (
    echo [ERROR] Copy scripts failed.
    pause
    exit /b 1
)

echo.
echo Copying ssh_key_block...
if exist "build\ssh_key_block" rd /s /q "build\ssh_key_block"
xcopy "ssh_key_block" "build\ssh_key_block" /e /i /y >nul
if errorlevel 1 (
    echo [ERROR] Copy ssh_key_block failed.
    pause
    exit /b 1
)

echo.
echo [OK] Output: %~dp0build\ToolSet.exe
pause
