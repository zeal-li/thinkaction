@echo off
setlocal enabledelayedexpansion

::::统一切到 936 代码页，保证本脚本（GBK）里的中文正常显示
chcp 936 >nul

::::用法: local_server_operate.bat <功能名> <项目目录> [服务器名] [服务器ID] [服务器类型]
::::功能名: build（编译）/ rebuild（重编）/ clean（清理）/ publish（发布）
::::        start（开服）/ stop（关服）/ status（查看）
::::项目目录 = 检出根目录\项目名，由调用方传进来，脚本里不写死项目路径
::::服务器名 / 服务器ID / 服务器类型只对 start / stop / status 有意义，对应 server_tool.sh 的 -n / -s / -m
::::服务器类型一次只传一个（如 dbserver），选 allserver 时由调用方拆开逐个调用本脚本
set "action=%~1"
set "project_dir=%~2"
set "server_name=%~3"
set "server_id=%~4"
set "server_model=%~5"

if "!action!"=="" (
    call :print_usage
    exit /b 1
)

if /i "!action!"=="build" (
    call :build_project
    goto :finish
)

if /i "!action!"=="rebuild" (
    call :rebuild_project
    goto :finish
)

if /i "!action!"=="clean" (
    call :clean_project
    goto :finish
)

if /i "!action!"=="publish" (
    call :publish_project
    goto :finish
)

if /i "!action!"=="start" (
    call :start_server
    goto :finish
)

if /i "!action!"=="stop" (
    call :stop_server
    goto :finish
)

if /i "!action!"=="status" (
    call :status_server
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
echo 用法: %~nx0 ^<功能名^> ^<项目目录^> [服务器名] [服务器ID] [服务器类型]
echo 可用功能:
echo   build   - 编译：登录 WSL，进入「项目目录\server\skynet」用 sudo 执行 ./server_tool.sh -o build
echo   rebuild - 重编：同上，-o rebuild
echo   clean   - 清理：同上，-o clean
echo   publish - 发布：同上，-o release，发布成功后打开 TortoiseSVN 提交界面提交「项目目录\common\release\ubuntu」
echo   start   - 开服：-o start -m 类型 -n 服务器名 -s 服务器ID，一次只处理一个类型
echo   stop    - 关服：-o stop -m 类型 -n 服务器名 -s 服务器ID，一次只处理一个类型
echo   status  - 查看：-o status -m 类型 -n 服务器名 -s 服务器ID，一次只处理一个类型
exit /b 0

::::------------------------------------------------------------
::::build：编译
::::------------------------------------------------------------
:build_project
call :check_skynet_dir
if errorlevel 1 exit /b 1

call :to_wsl_path "!skynet_dir!"

set "ssh_commond=cd '!wsl_path!' && sudo ./server_tool.sh -o build"
call "%~dp0sshcall_wsl.bat" "!ssh_commond!"
exit /b !errorlevel!

::::------------------------------------------------------------
::::rebuild：重编
::::------------------------------------------------------------
:rebuild_project
call :check_skynet_dir
if errorlevel 1 exit /b 1

call :to_wsl_path "!skynet_dir!"

set "ssh_commond=cd '!wsl_path!' && sudo ./server_tool.sh -o rebuild"
call "%~dp0sshcall_wsl.bat" "!ssh_commond!"
exit /b !errorlevel!

::::------------------------------------------------------------
::::clean：清理
::::------------------------------------------------------------
:clean_project
call :check_skynet_dir
if errorlevel 1 exit /b 1

call :to_wsl_path "!skynet_dir!"

set "ssh_commond=cd '!wsl_path!' && sudo ./server_tool.sh -o clean"
call "%~dp0sshcall_wsl.bat" "!ssh_commond!"
exit /b !errorlevel!

::::------------------------------------------------------------
::::publish：发布，成功后打开 Svn 提交界面提交「项目目录\common\release\ubuntu」
::::------------------------------------------------------------
:publish_project
call :check_skynet_dir
if errorlevel 1 exit /b 1

call :to_wsl_path "!skynet_dir!"

set "ssh_commond=cd '!wsl_path!' && sudo ./server_tool.sh -o release"
call "%~dp0sshcall_wsl.bat" "!ssh_commond!"
set "publish_result=!errorlevel!"
if not "!publish_result!"=="0" (
    echo [错误] 发布失败，退出码 !publish_result!，不打开Svn提交界面
    exit /b !publish_result!
)

call :open_svn_commit
exit /b !errorlevel!

::::------------------------------------------------------------
::::start：开服
::::------------------------------------------------------------
:start_server
call :check_skynet_dir
if errorlevel 1 exit /b 1

call :to_wsl_path "!skynet_dir!"

::::命令交给 sshcall_wsl.bat 登录后执行；整体用双引号包住，命令里的两个与号不会被 cmd 当成命令分隔符
::::sudo 加在 server_tool.sh 前面，不能加在 cd 前面（cd 是 shell 内建命令，sudo 找不到它）
echo "[操作] 开服 服务器类型: !server_model!"
set "ssh_commond=cd '!wsl_path!' && sudo ./server_tool.sh -o start -m !server_model! -n !server_name! -s !server_id!"
call "%~dp0sshcall_wsl.bat" "!ssh_commond!"
exit /b !errorlevel!

::::------------------------------------------------------------
::::stop：关服
::::------------------------------------------------------------
:stop_server
call :check_skynet_dir
if errorlevel 1 exit /b 1

call :to_wsl_path "!skynet_dir!"

echo "[操作] 关服 服务器类型: !server_model!"
set "ssh_commond=cd '!wsl_path!' && sudo ./server_tool.sh -o stop -m !server_model! -n !server_name! -s !server_id!"
call "%~dp0sshcall_wsl.bat" "!ssh_commond!"
exit /b !errorlevel!

::::------------------------------------------------------------
::::status：查看
::::------------------------------------------------------------
:status_server
call :check_skynet_dir
if errorlevel 1 exit /b 1

call :to_wsl_path "!skynet_dir!"

echo "[操作] 查看 服务器类型: !server_model!"
set "ssh_commond=cd '!wsl_path!' && sudo ./server_tool.sh -o status -m !server_model! -n !server_name! -s !server_id!"
call "%~dp0sshcall_wsl.bat" "!ssh_commond!"
exit /b !errorlevel!

::::------------------------------------------------------------
::::公共接口：校验项目目录并定位 server_tool.sh 所在目录
::::  入参: 无，直接读全局变量 project_dir（由调用方传入的第 2 个参数）
::::  出参: 变量 skynet_dir，如 C:\data\ikun_01_cn\server\skynet
::::  失败: 打印错误并 exit /b 1，调用方接 if errorlevel 1 exit /b 1
::::  例:   call :check_skynet_dir
::::         if errorlevel 1 exit /b 1
::::------------------------------------------------------------
:check_skynet_dir
if "!project_dir!"=="" (
    echo [错误] 未指定项目目录
    exit /b 1
)

set "skynet_dir=!project_dir!\server\skynet"
if not exist "!skynet_dir!\server_tool.sh" (
    echo [错误] 未找到 server_tool.sh: !skynet_dir!\server_tool.sh
    exit /b 1
)
exit /b 0

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


::::------------------------------------------------------------
::::发布成功后打开 TortoiseSVN 提交界面，提交「项目目录\common\release\ubuntu」
::::TortoiseProc.exe 优先用 PATH 里的，没有就按默认安装目录找（跟 svn_operate.bat 一致）
::::------------------------------------------------------------
:open_svn_commit
set "release_dir=!project_dir!\common\release\ubuntu"
set "tortoise_proc=TortoiseProc.exe"
where TortoiseProc.exe >nul 2>nul || set "tortoise_proc=%ProgramFiles%\TortoiseSVN\bin\TortoiseProc.exe"

echo "[提交] 打开 Svn 提交界面: !release_dir!"
start "" "!tortoise_proc!" /command:commit /path:"!release_dir!" /closeonend:0
exit /b 0
