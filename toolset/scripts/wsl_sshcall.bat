@echo off
setlocal enabledelayedexpansion

:::统一切到 936 代码页，保证本脚本（GBK）里的中文能正确传给 PowerShell 解析 JSON
chcp 936 >nul

:::参数: ssh_commond 要执行的命令
set "ssh_commond=%~1"

:::从 exe 同级的 config/config.json 读取 WSL 连接配置
set "config_path=%~dp0..\config\config.json"

:::解析用的临时文件放在脚本同级的 tmp 目录，用前清理、用完删除
set "config_tmp=%~dp0tmp\wsl_ssh_config_tmp.txt"
if not exist "%~dp0tmp" mkdir "%~dp0tmp"
if exist "%config_tmp%" del /q "%config_tmp%" >nul 2>nul

powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference = 'Stop'; $j = ConvertFrom-Json (Get-Content -Raw -LiteralPath '%config_path%' -Encoding UTF8); $s = $j.'环境部署'.'WSL_SSH_配置'; 'host=' + $s.hostname; 'port=' + $s.port; 'user=' + $s.username; 'key=' + $s.userkey" > "%config_tmp%"

if errorlevel 1 (
    for %%i in ("%config_path%") do echo [错误] 读取配置失败，请检查 %%~fi 是否为合法 JSON
    del /q "%config_tmp%" >nul 2>nul
    exit /b 1
)

for /f "usebackq tokens=1,* delims==" %%a in ("%config_tmp%") do (
    if /i "%%a"=="host" set "ssh_host=%%b"
    if /i "%%a"=="port" set "ssh_port=%%b"
    if /i "%%a"=="user" set "ssh_user=%%b"
    if /i "%%a"=="key"  set "ssh_key=%%b"
)
del /q "%config_tmp%" >nul 2>nul

:::连接配置检查
if "!ssh_host!"=="" (
    echo [错误] 未配置 hostname，请在界面「环境部署 / WSL_SSH_配置」填写并点确认
    exit /b 1
)
if "!ssh_port!"=="" (
    echo [错误] 未配置 port，请在界面「环境部署 / WSL_SSH_配置」填写并点确认
    exit /b 1
)
if "!ssh_user!"=="" (
    echo [错误] 未配置 username，请在界面「环境部署 / WSL_SSH_配置」填写并点确认
    exit /b 1
)
if "!ssh_key!"=="" (
    echo [错误] 未配置 userkey，请在界面「环境部署 / WSL_SSH_配置」填写并点确认
    exit /b 1
)

:::命令检查
if "!ssh_commond!"=="" (
    echo [错误] 未指定要执行的命令
    echo 用法: %~nx0 "命令"
    exit /b 1
)

:::执行ssh命令
echo "[开始执行ssh命令] ssh -i !ssh_key! !ssh_user!@!ssh_host! -p !ssh_port! !ssh_commond!"
ssh -i "!ssh_key!" !ssh_user!@!ssh_host! -p !ssh_port! !ssh_commond!
set "ssh_result=!errorlevel!"

:::ssh 退出后可能把控制台代码页切成 65001，切回来，否则后面的中文提示会乱码
chcp 936 >nul

echo "[ssh命令执行完成] 退出码:!ssh_result!"
exit /b !ssh_result!
