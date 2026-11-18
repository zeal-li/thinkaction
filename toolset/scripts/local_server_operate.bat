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
echo   publish - 发布：登录 WSL，进入「项目目录\server\skynet」用 sudo 执行 ./server_tool.sh -o release
exit /b 0

::::------------------------------------------------------------
::::publish：登录 WSL 到「项目目录\server\skynet」用 sudo 执行 ./server_tool.sh -o release
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

::::Windows 路径转 WSL 路径统一走 :to_wsl_path，结果放在 !wsl_path! 里
call :to_wsl_path "!skynet_dir!"
set "wsl_skynet_dir=!wsl_path!"

::::命令交给 sshcall_wsl.bat 登录后执行；整体用双引号包住，命令里的两个与号不会被 cmd 当成命令分隔符
::::sudo 加在 server_tool.sh 前面，不能加在 cd 前面（cd 是 shell 内建命令，sudo 找不到它）
set "ssh_commond=cd '!wsl_skynet_dir!' && sudo ./server_tool.sh -o release"
call "%~dp0sshcall_wsl.bat" "!ssh_commond!"
exit /b !errorlevel!


::::------------------------------------------------------------
::::公共接口：Windows 路径转 WSL 路径，后续功能都调它，不要各写一份
::::  入参: %~1 Windows 路径，如 C:\data\ikun_01_cn\server\skynet
::::  出参: 变量 wsl_path，如 /mnt/c/data/ikun_01_cn/server/skynet（调用方直接用 !wsl_path!）
::::  例:   call :to_wsl_path "!skynet_dir!"
::::         set "ssh_commond=cd '!wsl_path!' && sudo ./server_tool.sh -o release"
::::------------------------------------------------------------
:to_wsl_path
set "wsl_path="
if "%~1"=="" (
    echo [错误] 路径转换未传入 Windows 路径
    exit /b 1
)

set "to_wsl_raw=%~1"
set "to_wsl_drive=!to_wsl_raw:~0,1!"
set "to_wsl_rest=!to_wsl_raw:~2!"

::::盘符统一转小写：/mnt/c 而不是 /mnt/C
for %%d in (a b c d e f g h i j k l m n o p q r s t u v w x y z) do (
    if /i "!to_wsl_drive!"=="%%d" set "to_wsl_drive=%%d"
)

set "to_wsl_rest=!to_wsl_rest:\=/!"
set "wsl_path=/mnt/!to_wsl_drive!!to_wsl_rest!"
exit /b 0
