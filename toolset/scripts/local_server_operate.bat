@echo off
setlocal enabledelayedexpansion

::::统一切到 936 代码页，保证本脚本（GBK）里的中文正常显示
chcp 936 >nul

::::用法: local_server_operate.bat <功能名> <项目目录> [服务器名] [服务器ID] [服务器类型列表]
::::功能名: build（编译）/ rebuild（重编）/ clean（清理）/ publish（发布）
::::        start（开服）/ stop（关服）/ status（查看）
::::项目目录 = 检出根目录\项目名，由调用方传进来，脚本里不写死项目路径
::::服务器名 / 服务器ID / 服务器类型只对 start / stop / status 有意义，对应 server_tool.sh 的 -n / -s / -m
::::类型列表用空格分隔（如 dbserver loginserver gateway gameserver），allserver 由调用方展开好再传进来
set "action=%~1"
set "project_dir=%~2"
set "server_name=%~3"
set "server_id=%~4"
set "server_models=%~5"

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
echo 用法: %~nx0 ^<功能名^> ^<项目目录^> [服务器名] [服务器ID] [服务器类型列表]
echo 可用功能:
echo   build   - 编译：登录 WSL，进入「项目目录\server\skynet」用 sudo 执行 ./server_tool.sh -o build
echo   rebuild - 重编：同上，-o rebuild
echo   clean   - 清理：同上，-o clean
echo   publish - 发布：同上，-o release，发布成功后打开 TortoiseSVN 提交界面提交「项目目录\common\release\ubuntu」
echo   start   - 开服：-o start -m 类型 -n 服务器名 -s 服务器ID，类型列表逐个执行
echo   stop    - 关服：-o stop -m 类型 -n 服务器名 -s 服务器ID，类型列表逐个执行
echo   status  - 查看：-o status -m 类型 -n 服务器名 -s 服务器ID，类型列表逐个执行
exit /b 0

::::------------------------------------------------------------
::::build：编译
::::------------------------------------------------------------
:build_project
call :run_server_tool build
exit /b !errorlevel!

::::------------------------------------------------------------
::::rebuild：重编
::::------------------------------------------------------------
:rebuild_project
call :run_server_tool rebuild
exit /b !errorlevel!

::::------------------------------------------------------------
::::clean：清理
::::------------------------------------------------------------
:clean_project
call :run_server_tool clean
exit /b !errorlevel!

::::------------------------------------------------------------
::::publish：发布，成功后打开 Svn 提交界面提交「项目目录\common\release\ubuntu」
::::------------------------------------------------------------
:publish_project
call :run_server_tool release
set "publish_result=!errorlevel!"
if not "!publish_result!"=="0" (
    echo [错误] 发布失败，退出码 !publish_result!，不打开Svn提交界面
    exit /b !publish_result!
)

call :open_svn_commit
exit /b !errorlevel!

::::------------------------------------------------------------
::::start：开服（服务器类型列表逐个开）
::::------------------------------------------------------------
:start_server
call :operate_servers start
exit /b !errorlevel!

::::------------------------------------------------------------
::::stop：关服（服务器类型列表逐个关）
::::------------------------------------------------------------
:stop_server
call :operate_servers stop
exit /b !errorlevel!

::::------------------------------------------------------------
::::status：查看（服务器类型列表逐个查）
::::------------------------------------------------------------
:status_server
call :operate_servers status
exit /b !errorlevel!

::::------------------------------------------------------------
::::公共接口：开服 / 关服 / 查看用，命令一定带 -m / -n / -s
::::  入参: %~1 操作类型（start / stop / status）
::::  server_tool.sh 参数: -m 服务器类型、-n 服务器名、-s 服务器ID
::::------------------------------------------------------------
:operate_servers
if "!project_dir!"=="" (
    echo [错误] 未指定项目目录
    exit /b 1
)
if "!server_name!"=="" (
    echo [错误] 未指定服务器名
    exit /b 1
)
if "!server_id!"=="" (
    echo [错误] 未指定服务器ID
    exit /b 1
)
if "!server_models!"=="" (
    echo [错误] 未指定服务器类型
    exit /b 1
)

set "skynet_dir=!project_dir!\server\skynet"
if not exist "!skynet_dir!\server_tool.sh" (
    echo [错误] 未找到 server_tool.sh: !skynet_dir!\server_tool.sh
    exit /b 1
)

call :to_wsl_path "!skynet_dir!"

for %%m in (!server_models!) do (
    echo "[操作] %~1 服务器类型: %%m"
    set "ssh_commond=cd '!wsl_path!' && sudo ./server_tool.sh -o %~1 -m %%m -n !server_name! -s !server_id!"
    call "%~dp0sshcall_wsl.bat" "!ssh_commond!"
    if errorlevel 1 exit /b !errorlevel!
)
exit /b 0

::::------------------------------------------------------------
::::公共接口：编译 / 重编 / 清理 / 发布用，命令只发 -o 选项
::::  入参: %~1 server_tool.sh 的 -o 选项（build / rebuild / clean / release）
::::------------------------------------------------------------
:run_server_tool
if "!project_dir!"=="" (
    echo [错误] 未指定项目目录
    exit /b 1
)

set "skynet_dir=!project_dir!\server\skynet"
if not exist "!skynet_dir!\server_tool.sh" (
    echo [错误] 未找到 server_tool.sh: !skynet_dir!\server_tool.sh
    exit /b 1
)

call :to_wsl_path "!skynet_dir!"

::::命令交给 sshcall_wsl.bat 登录后执行；整体用双引号包住，命令里的两个与号不会被 cmd 当成命令分隔符
::::sudo 加在 server_tool.sh 前面，不能加在 cd 前面（cd 是 shell 内建命令，sudo 找不到它）
set "ssh_commond=cd '!wsl_path!' && sudo ./server_tool.sh -o %~1"
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
