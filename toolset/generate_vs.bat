@echo off
chcp 65001 > nul
setlocal
cd /d %~dp0

echo ==========================================================
echo  ToolSet - 生成 Visual Studio 工程
echo ==========================================================
echo.

where dotnet >nul 2>nul
if errorlevel 1 (
    echo [FAIL] 未找到 dotnet 命令，请确认已安装 .NET SDK
    pause
    exit /b 1
)

if not exist "src\ToolSet.csproj" (
    echo [FAIL] 找不到工程文件: src\ToolSet.csproj
    pause
    exit /b 1
)

rem 老 SDK(5/6/7/8/9) 没有 --format 参数，只有 .NET 10+ 才需要显式指定 sln
set SDKM=0
set SLNFMT=
for /f "tokens=1 delims=." %%v in ('dotnet --version') do set SDKM=%%v
if %SDKM% geq 10 set SLNFMT=--format sln
echo 当前 .NET SDK 主版本: %SDKM%

rem src 下的 .cs / .xaml 由 SDK 风格工程自动收录，新增文件无需手工添加到这里
echo [1/2] 重新生成解决方案 src\ToolSet.sln ...
dotnet new sln --force %SLNFMT% -n ToolSet -o src > nul
if errorlevel 1 (
    echo [FAIL] 生成 src\ToolSet.sln 失败
    pause
    exit /b 1
)

echo [2/2] 将 ToolSet.csproj 加入解决方案 ...
dotnet sln src\ToolSet.sln add src\ToolSet.csproj
if errorlevel 1 (
    echo [FAIL] 加入项目失败
    pause
    exit /b 1
)

echo.
echo [OK] 完成，用 VS 打开: %~dp0src\ToolSet.sln
echo      提示: src 下新增的 .cs / .xaml 会自动纳入工程，重新打开解决方案即可看到
pause
