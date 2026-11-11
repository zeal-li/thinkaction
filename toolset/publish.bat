@echo off
setlocal
cd /d %~dp0

echo ==========================================================
echo   ToolSet - 发布
echo ==========================================================
echo.

rem 目标平台，可传参覆盖：publish.bat win-arm64
set "RID=%~1"
if "%RID%"=="" set "RID=win-x64"

where dotnet >nul 2>nul
if errorlevel 1 (
    echo [ERROR] dotnet not found in PATH, please install .NET SDK.
    pause
    exit /b 1
)

if not exist "src\ToolSet.csproj" (
    echo [ERROR] Project not found: src\ToolSet.csproj
    pause
    exit /b 1
)

echo Publishing (%RID%, self-contained, single file)...
echo.
dotnet publish src\ToolSet.csproj ^
    -c Release ^
    -r %RID% ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:EnableCompressionInSingleFile=true ^
    -p:DebugType=None ^
    -p:DebugSymbols=false ^
    -o publish

if errorlevel 1 (
    echo.
    echo [ERROR] Publish failed, see the message above.
    pause
    exit /b 1
)

echo.
echo Copying scripts...
if exist "publish\scripts" rd /s /q "publish\scripts"
xcopy "scripts" "publish\scripts" /e /i /y >nul
if errorlevel 1 (
    echo.
    echo [ERROR] Copy scripts failed, see the message above.
    pause
    exit /b 1
)

echo.
echo Copying ssh_key_block...
if exist "publish\ssh_key_block" rd /s /q "publish\ssh_key_block"
xcopy "ssh_key_block" "publish\ssh_key_block" /e /i /y >nul
if errorlevel 1 (
    echo.
    echo [ERROR] Copy ssh_key_block failed, see the message above.
    pause
    exit /b 1
)

echo.
echo [OK] Output: %~dp0publish\ToolSet.exe
for %%f in ("publish\ToolSet.exe") do echo      Size: %%~zf bytes
echo      Self-contained, run directly on any Windows 10/11 %RID% machine.
pause
