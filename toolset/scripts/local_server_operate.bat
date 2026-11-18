@echo off
setlocal enabledelayedexpansion

::::统一切到 936 代码页，保证本脚本（GBK）里的中文正常显示
chcp 936 >nul

::::用法: local_server_operate.bat <功能名> <项目目录>
::::项目目录 = 检出根目录\项目名，由调用方传进来，脚本里不写死项目路径
set "action=%~1"
set "project_dir=%~2"

if "!action!"=="" (
    call :print_usage
    exit /b 1
)

if /i "!action!"=="publish" (
    call :publish_project
    goto :finish
)

echo [错误] 未定义的功能: !action!
call :print_usage
exit /b 1

:finish
exit /b !errorlevel!

::::------------------------------------------------------------
::::用法
::::------------------------------------------------------------
:print_usage
echo 用法: %~nx0 ^<功能名^> ^<项目目录^>
echo 可用功能:
echo   publish - 发布：登录 WSL，进入「项目目录\server\skynet」执行 ./server_tool.sh -o release
exit /b 0

::::------------------------------------------------------------
::::publish：登录 WSL 到「项目目录\server\skynet」执行 ./server_tool.sh -o release
::::------------------------------------------------------------
:publish_project
if "!project_dir!"=="" (
    echo [错误] 未指定项目目录
    exit /b 1
)

set "skynet_dir=!project_dir!\server\skynet"
if not exist "!skynet_dir!\server_tool.sh" (
    echo [错误] 未找到发布脚本: !skynet_dir!\server_tool.sh
    exit /b 1
)

::::Windows 路径转 WSL 路径：C:\data\ikun_01_cn\server\skynet -> /mnt/c/data/ikun_01_cn/server/skynet
set "drive_letter=!skynet_dir:~0,1!"
set "dir_rest=!skynet_dir:~2!"
for %%d in (a b c d e f g h i j k l m n o p q r s t u v w x y z) do (
    if /i "!drive_letter!"=="%%d" set "drive_letter=%%d"
)
set "dir_rest=!dir_rest:\=/!"
set "wsl_skynet_dir=/mnt/!drive_letter!!dir_rest!"

::::命令交给 sshcall_wsl.bat 登录后执行；整体用双引号包住，命令里的两个与号不会被 cmd 当成命令分隔符
set "ssh_commond=cd '!wsl_skynet_dir!' && ./server_tool.sh -o release"
call "%~dp0sshcall_wsl.bat" "!ssh_commond!"
exit /b !errorlevel!
