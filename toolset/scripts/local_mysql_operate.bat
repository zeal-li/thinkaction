@echo off
setlocal enabledelayedexpansion

::::统一切到 936 代码页，保证本脚本（GBK）里的中文能正确传给 PowerShell 解析 JSON
chcp 936 >nul

::::用法: local_mysql_operate.bat <功能名>
::::功能名与下面的标签一一对应，目前只有 config（配置服务）
set "action=%~1"

if "!action!"=="" (
    echo [错误] 未指定要执行的功能
    echo 用法: %~nx0 ^<功能名^>
    echo 可用功能: config - 配置服务
    exit /b 1
)

if /i "!action!"=="config" (
    call :config_service
    exit /b !errorlevel!
)

echo [错误] 未定义的功能: !action!
echo 可用功能: config - 配置服务
exit /b 1

::::------------------------------------------------------------
::::配置服务：把 exe 同级 envconf\mysql\my.ini 拷贝到配置的 dirpath
::::------------------------------------------------------------
:config_service
set "source_ini=%~dp0..\envconf\mysql\my.ini"
set "config_path=%~dp0..\config\config.json"

::::解析用的临时文件放在脚本同级的 tmp 目录，用前清理、用完删除
set "config_tmp=%~dp0tmp\local_mysql_config_tmp.txt"
if not exist "%~dp0tmp" mkdir "%~dp0tmp"
if exist "%config_tmp%" del /q "%config_tmp%" >nul 2>nul

powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference = 'Stop'; $j = ConvertFrom-Json (Get-Content -Raw -LiteralPath '%config_path%' -Encoding UTF8); $s = $j.'环境部署'.'Local_MySql_配置'; 'dirpath=' + $s.dirpath" > "%config_tmp%"

if errorlevel 1 (
    for %%i in ("%config_path%") do echo [错误] 读取配置失败，请检查 %%~fi 是否为合法 JSON
    del /q "%config_tmp%" >nul 2>nul
    exit /b 1
)

for /f "usebackq tokens=1,* delims==" %%a in ("%config_tmp%") do (
    if /i "%%a"=="dirpath" set "mysql_dirpath=%%b"
)
del /q "%config_tmp%" >nul 2>nul

::::配置检查
if "!mysql_dirpath!"=="" (
    echo [错误] 未配置 dirpath，请在界面「环境部署 / Local_MySql_配置」填写并点确认
    exit /b 1
)

if not exist "!source_ini!" (
    echo [错误] 未找到模板文件: !source_ini!
    exit /b 1
)

::::拷贝 my.ini 到 dirpath
echo "[配置服务] !source_ini! -> !mysql_dirpath!\my.ini"
copy /y "!source_ini!" "!mysql_dirpath!\my.ini" >nul

if errorlevel 1 (
    echo [错误] 拷贝 my.ini 失败，请检查 dirpath 是否存在: !mysql_dirpath!
    exit /b 1
)

echo "[配置服务] 完成"
exit /b 0
