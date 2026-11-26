@echo off
setlocal enabledelayedexpansion

::::统一切到 936 代码页，保证本脚本（GBK）里的中文正常显示
chcp 936 >nul

::::用法: local_server_operate.bat <功能名> <项目目录> [服务器名/数据库名]
::::功能名: build（编译）/ rebuild（重编）/ clean（清理）/ release（发布）
::::        start（开服）/ stop（关服）/ status（查看）
::::项目目录 = 检出根目录\项目名，由调用方传进来，脚本里不写死项目路径
::::第3参数：start / stop / status 时是服务器名（server_tool.sh 的 -n），数据库操作时是库名中间段
::::服务器类型（-m）和服ID（-s）都不走参数，start / stop / status 时由脚本在控制台里让玩家选
::::服务器类型选 allserver 时，脚本遍历所选分组（游戏服/公共服）已定义的类型逐个执行
:::::------------------------------------------------------------
:::::数据库操作：对本地 Windows MySQL 执行命令
:::::  功能名: db_create（建库）/ db_delete（删库）/ db_clear（清档）/ db_update（更新）
:::::       db_export_structure（导出结构）/ db_export_def（导出def）/ db_query（查询）/ db_export（导出）/ db_import（导入）
:::::  第3参数只传中间那段库名（如 lizexian），实际库名按类型拼: <数据库类型>_<数据库名>_<服ID>
:::::  SQL 取「项目目录\common\sql」下的 <数据库类型>_table.sql（db_create / db_update），db_clear 不需要 SQL 文件
:::::  db_update 调「项目目录\common\sql\mysqldiff\dist\mysqldiff.exe」，对比实际库结构和 <数据库类型>_table.sql 生成差异 SQL 再执行
:::::  连接信息取 exe 同级 config\config.json 的「环境部署/Local_MySql_配置」（dirpath / hostname / port / username / password）
:::::  例: local_server_operate.bat db_create C:\data\ikun_01_cn lizexian
:::::------------------------------------------------------------

set "action=%~1"
set "project_dir=%~2"
set "db_name=%~3"

::::服务器类型分两组（游戏服 game_server_model_array / 公共服 global_server_model_array），start / stop / status 执行时由玩家按序号选择
::::加减服务器类型只改这里，start / stop / status 会自动遍历；值必须和 server_tool.sh 的 -m 取值一致
::::选 allserver 时按变量名的字母序遍历，索引多于 9 个时建议补零（[01]…[10]），否则 10 会排在 2 前面
set "game_server_model_array[0]=allserver"
set "game_server_model_array[1]=dbserver"
set "game_server_model_array[2]=gameserver"
set "game_server_model_array[3]=loginserver"
set "game_server_model_array[4]=gateway"

set "global_server_model_array[100]=allserver"
set "global_server_model_array[101]=logserver"

:::::数据库类型固定只有两种，create / delete / clear / update 执行时由玩家按序号选择
set "db_type_array[1]=dbserver"
set "db_type_array[2]=logserver"

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

if /i "!action!"=="release" (
    call :release_project
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

if /i "!action!"=="db_create" (
    call :create_database
    goto :finish
)

if /i "!action!"=="db_delete" (
    call :delete_database
    goto :finish
)

if /i "!action!"=="db_clear" (
    call :clear_database
    goto :finish
)

if /i "!action!"=="db_update" (
    call :update_database
    goto :finish
)

if /i "!action!"=="db_export_structure" (
    call :export_structure
    goto :finish
)

if /i "!action!"=="db_export_def" (
    call :export_def
    goto :finish
)

if /i "!action!"=="db_query" (
    call :query_database
    goto :finish
)

if /i "!action!"=="db_export" (
    call :export_database
    goto :finish
)

if /i "!action!"=="db_import" (
    call :import_database
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
echo 用法: %~nx0 ^<功能名^> ^<项目目录^> [服务器名/数据库名]
echo 可用功能:
echo   build   - 编译：登录 WSL，进入「项目目录\server\skynet」用 sudo 执行 ./server_tool.sh -o build
echo   rebuild - 重编：同上，-o rebuild
echo   clean   - 清理：同上，-o clean
echo   release - 发布：同上，-o release，发布成功后打开 TortoiseSVN 提交界面提交「项目目录\common\release\ubuntu」
echo   start   - 开服：进入「项目目录\common\release\ubuntu」执行 -o start -m 类型 -n 服务器名 -s 服ID（类型和服ID执行时选择/输入），选 allserver 时逐个类型执行
echo   stop    - 关服：同上，-o stop
echo   status  - 查看：同上，-o status
echo.
echo 数据库操作（本地 Windows MySQL，SQL 取 项目目录\common\sql）:
echo   db_create  - 建库：建「实际库名」，再导入 ^<数据库类型^>_table.sql
echo   db_delete  - 删库：删除「实际库名」
echo   db_clear   - 清档：保留库和表结构，清空「实际库名」里所有表的数据（TRUNCATE TABLE，不需要 SQL 文件）
echo   db_update  - 更新：调 common\sql\mysqldiff\dist\mysqldiff.exe，对比「实际库名」结构和 ^<数据库类型^>_table.sql，生成差异 SQL 并执行
echo   db_export_structure - 导出结构：选服务器后用 mysqldump --no-data 把「实际库名」表结构导到 ^<数据库类型^>_table.sql（覆盖）
echo   db_export_def - 导出def：调 common\tools\tabledefcreator\dist\creatormain.exe，把「实际库名」导出成 server\skynet\^<模块^>\src\define\MysqlTableDefine.lua
echo   db_query   - 查询：列出所有数据库，按 数据库名 模糊匹配并打印匹配到的库名（不选类型和服ID）
echo   db_export  - 导出：用 mysqldump 把「实际库名」的表结构和数据导到 项目目录\common\sql\backup\^<实际库名^>_^<yyyyMMddHHmm^>.sql
echo   db_import  - 导入：选服务器后输入 项目目录\common\sql\backup 下的备份文件名，导入「实际库名」
echo 数据库操作用 第3参数=数据库名，数据库类型和服ID执行时输入（1=!db_type_array[1]!，2=!db_type_array[2]!）:
echo 实际库名按类型拼: ^<数据库类型^>_^<数据库名^>_^<服ID^>
echo   %~nx0 db_create C:\data\ikun_01_cn lizexian
exit /b 0

::::------------------------------------------------------------
::::build：编译
::::------------------------------------------------------------
:build_project
call :check_source_skynet_dir
if errorlevel 1 exit /b 1

call :to_wsl_path "!skynet_dir!"

set "ssh_commond=cd '!wsl_path!' && sudo ./server_tool.sh -o build"
call "%~dp0sshcall_wsl.bat" "!ssh_commond!"
exit /b !errorlevel!

::::------------------------------------------------------------
::::rebuild：重编
::::------------------------------------------------------------
:rebuild_project
call :check_source_skynet_dir
if errorlevel 1 exit /b 1

call :to_wsl_path "!skynet_dir!"

set "ssh_commond=cd '!wsl_path!' && sudo ./server_tool.sh -o rebuild"
call "%~dp0sshcall_wsl.bat" "!ssh_commond!"
exit /b !errorlevel!

::::------------------------------------------------------------
::::clean：清理
::::------------------------------------------------------------
:clean_project
call :check_source_skynet_dir
if errorlevel 1 exit /b 1

call :to_wsl_path "!skynet_dir!"

set "ssh_commond=cd '!wsl_path!' && sudo ./server_tool.sh -o clean"
call "%~dp0sshcall_wsl.bat" "!ssh_commond!"
exit /b !errorlevel!

::::------------------------------------------------------------
::::release：发布，成功后打开 Svn 提交界面提交「项目目录\common\release\ubuntu」
::::------------------------------------------------------------
:release_project
call :check_source_skynet_dir
if errorlevel 1 exit /b 1

call :to_wsl_path "!skynet_dir!"

set "ssh_commond=cd '!wsl_path!' && sudo ./server_tool.sh -o release"
call "%~dp0sshcall_wsl.bat" "!ssh_commond!"
set "release_result=!errorlevel!"
if not "!release_result!"=="0" (
    echo [错误] 发布失败，退出码 !release_result!，不打开Svn提交界面
    exit /b !release_result!
)

call :open_svn_commit
exit /b !errorlevel!

::::------------------------------------------------------------
::::start：开服
::::------------------------------------------------------------
:start_server
call :check_release_skynet_dir
if errorlevel 1 exit /b 1

call :select_server_model
if errorlevel 1 exit /b 1

call :select_server_id
if errorlevel 1 exit /b 1

call :to_wsl_path "!skynet_dir!"

::::没选 allserver 就只执行选中的那个类型
if /i not "!server_model!"=="allserver" (
    echo "[操作] 开服 服务器类型: !server_model!"
    set "ssh_commond=cd '!wsl_path!' && sudo ./server_tool.sh -o start -m !server_model! -n !db_name! -s !server_id!"
    call "%~dp0sshcall_wsl.bat" "!ssh_commond!"
    exit /b !errorlevel!
)

set "model_array_prefix=!server_group!_server_model_array"
for /f "tokens=1,* delims==" %%a in ('set !model_array_prefix![') do (
    if /i not "%%b"=="allserver" (
        echo "[操作] 开服 服务器类型: %%b"
        set "ssh_commond=cd '!wsl_path!' && sudo ./server_tool.sh -o start -m %%b -n !db_name! -s !server_id!"
        call "%~dp0sshcall_wsl.bat" "!ssh_commond!"
        if errorlevel 1 exit /b !errorlevel!
    )
)
exit /b 0

::::------------------------------------------------------------
::::stop：关服
::::------------------------------------------------------------
:stop_server
call :check_release_skynet_dir
if errorlevel 1 exit /b 1

call :select_server_model
if errorlevel 1 exit /b 1

call :select_server_id
if errorlevel 1 exit /b 1

call :to_wsl_path "!skynet_dir!"

::::没选 allserver 就只执行选中的那个类型
if /i not "!server_model!"=="allserver" (
    echo "[操作] 关服 服务器类型: !server_model!"
    set "ssh_commond=cd '!wsl_path!' && sudo ./server_tool.sh -o stop -m !server_model! -n !db_name! -s !server_id!"
    call "%~dp0sshcall_wsl.bat" "!ssh_commond!"
    exit /b !errorlevel!
)

set "model_array_prefix=!server_group!_server_model_array"
for /f "tokens=1,* delims==" %%a in ('set !model_array_prefix![') do (
    if /i not "%%b"=="allserver" (
        echo "[操作] 关服 服务器类型: %%b"
        set "ssh_commond=cd '!wsl_path!' && sudo ./server_tool.sh -o stop -m %%b -n !db_name! -s !server_id!"
        call "%~dp0sshcall_wsl.bat" "!ssh_commond!"
        if errorlevel 1 exit /b !errorlevel!
    )
)
exit /b 0

::::------------------------------------------------------------
::::status：查看
::::------------------------------------------------------------
:status_server
call :check_release_skynet_dir
if errorlevel 1 exit /b 1

call :select_server_model
if errorlevel 1 exit /b 1

call :select_server_id
if errorlevel 1 exit /b 1

call :to_wsl_path "!skynet_dir!"

::::没选 allserver 就只执行选中的那个类型
if /i not "!server_model!"=="allserver" (
    echo "[操作] 查看 服务器类型: !server_model!"
    set "ssh_commond=cd '!wsl_path!' && sudo ./server_tool.sh -o status -m !server_model! -n !db_name! -s !server_id!"
    call "%~dp0sshcall_wsl.bat" "!ssh_commond!"
    exit /b !errorlevel!
)

set "model_array_prefix=!server_group!_server_model_array"
for /f "tokens=1,* delims==" %%a in ('set !model_array_prefix![') do (
    if /i not "%%b"=="allserver" (
        echo "[操作] 查看 服务器类型: %%b"
        set "ssh_commond=cd '!wsl_path!' && sudo ./server_tool.sh -o status -m %%b -n !db_name! -s !server_id!"
        call "%~dp0sshcall_wsl.bat" "!ssh_commond!"
        if errorlevel 1 exit /b !errorlevel!
    )
)
exit /b 0

::::------------------------------------------------------------
::::开服 / 关服 / 查看公共接口：让玩家选择服务器类型
::::  出参: 变量 server_model（选中的服务器类型）、server_group（game=游戏服 / global=公共服）
::::------------------------------------------------------------
:select_server_model
echo 请选择服务器类型:
echo 游戏服:
echo   【0】!game_server_model_array[0]!
echo   【1】!game_server_model_array[1]!
echo   【2】!game_server_model_array[2]!
echo   【3】!game_server_model_array[3]!
echo   【4】!game_server_model_array[4]!
echo 公共服:
echo   【100】!global_server_model_array[100]!
echo   【101】!global_server_model_array[101]!
set /p "server_select=输入序号选择服务器类型: "

::::校验：必须是数字
echo !server_select!|findstr /r /c:"^[0-9][0-9]*$" >nul
if errorlevel 1 (
    echo [错误] 序号必须是数字: !server_select!
    exit /b 1
)

::::按 100 分界选分组，再从对应数组取下标对应的类型，取不到（空）就是无效序号
if !server_select! geq 100 (
    set "server_group=global"
) else (
    set "server_group=game"
)

if "!server_group!"=="global" (
    for %%i in (!server_select!) do set "server_model=!global_server_model_array[%%i]!"
) else (
    for %%i in (!server_select!) do set "server_model=!game_server_model_array[%%i]!"
)

if "!server_model!"=="" (
    echo [错误] 无效的服务器类型序号: !server_select!
    exit /b 1
)

echo "[选择] 服务器类型: !server_model!"
exit /b 0

::::------------------------------------------------------------
::::公共接口：让玩家输入服ID（数字且大于 0），开关服 / 数据库操作共用
::::  出参: 变量 server_id
::::------------------------------------------------------------
:select_server_id
set "server_id="
set /p "server_id=请输入服ID（大于 0 的数字，如 1）: "
echo !server_id!|findstr /r /c:"^[0-9][0-9]*$" >nul
if errorlevel 1 (
    echo [错误] 服ID必须是数字: !server_id!
    exit /b 1
)

if !server_id! leq 0 (
    echo [错误] 服ID必须大于0: !server_id!
    exit /b 1
)

echo "[选择] 服ID: !server_id!"
exit /b 0

::::------------------------------------------------------------
::::公共接口：校验项目目录并定位「源码」里的 server_tool.sh，build / rebuild / clean / release 用
::::  目标目录: !project_dir!\server\skynet
::::  入参: 无，直接读全局变量 project_dir（由调用方传入的第 2 个参数）
::::  出参: 变量 skynet_dir，如 C:\data\ikun_01_cn\server\skynet
::::  失败: 打印错误并 exit /b 1，调用方接 if errorlevel 1 exit /b 1
::::  例:   call :check_source_skynet_dir
::::         if errorlevel 1 exit /b 1
::::------------------------------------------------------------
:check_source_skynet_dir
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
::::公共接口：校验项目目录并定位「发布」目录里的 server_tool.sh，start / stop / status 用
::::  目标目录: !project_dir!\common\release\ubuntu
::::  入参: 无，直接读全局变量 project_dir（由调用方传入的第 2 个参数）
::::  出参: 变量 skynet_dir，如 C:\data\ikun_01_cn\common\release\ubuntu
::::  失败: 打印错误并 exit /b 1，调用方接 if errorlevel 1 exit /b 1
::::  例:   call :check_release_skynet_dir
::::         if errorlevel 1 exit /b 1
::::------------------------------------------------------------
:check_release_skynet_dir
if "!project_dir!"=="" (
    echo [错误] 未指定项目目录
    exit /b 1
)

set "skynet_dir=!project_dir!\common\release\ubuntu"
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


:::::------------------------------------------------------------
:::::db_create：建库，再导入「项目目录\common\sql\<数据库类型>_table.sql」
:::::------------------------------------------------------------
:create_database
call :mysql_check_args
if errorlevel 1 exit /b 1

call :select_db_type
call :select_server_id
if errorlevel 1 exit /b 1

call :compose_db_name

set "mysql_sql_file=!project_dir!\common\sql\!db_type!_table.sql"
if not exist "!mysql_sql_file!" (
    echo [错误] 未找到 SQL 文件: !mysql_sql_file!
    exit /b 1
)

call :mysql_database_exists
if not errorlevel 1 (
    echo [错误] 数据库已存在: !db_name!
    exit /b 1
)

call :mysql_execute "CREATE DATABASE IF NOT EXISTS `!db_name!`;"
if errorlevel 1 exit /b 1

call :mysql_import "!mysql_sql_file!"
exit /b !errorlevel!

:::::------------------------------------------------------------
:::::db_delete：删库
:::::------------------------------------------------------------
:delete_database
call :mysql_check_args
if errorlevel 1 exit /b 1

call :select_db_type
call :select_server_id
if errorlevel 1 exit /b 1

call :compose_db_name

call :mysql_database_exists
if errorlevel 1 (
    echo [错误] 数据库不存在: !db_name!
    exit /b 1
)

call :mysql_execute "DROP DATABASE IF EXISTS `!db_name!`;"
exit /b !errorlevel!

:::::------------------------------------------------------------
:::::db_clear：清档，保留数据库和表结构，只清空库里所有表的数据（TRUNCATE TABLE，不需要 SQL 文件）
:::::------------------------------------------------------------
:clear_database
call :mysql_check_args
if errorlevel 1 exit /b 1

call :select_db_type
call :select_server_id
if errorlevel 1 exit /b 1

call :compose_db_name

call :mysql_database_exists
if errorlevel 1 (
    echo [错误] 数据库不存在: !db_name!
    exit /b 1
)

echo "[清档] 清空数据库 !db_name! 里所有表的数据"
call :mysql_truncate_tables
exit /b !errorlevel!

:::::------------------------------------------------------------
:::::db_update：更新，调「项目目录\common\sql\mysqldiff\dist\mysqldiff.exe」，
:::::           对比「实际库名」当前结构和 <数据库类型>_table.sql，生成差异 SQL 后执行到「实际库名」
:::::  调用约定与 db_create / db_delete 一致：第2参数=项目目录，第3参数=库名中间段，类型和服ID执行时选
:::::  mysqldiff.exe 用 -d file 模式比两个 SQL 文件：先把实际库当前结构用 mysqldump --no-data 导出成文件，
:::::  再 -s=table.sql（参考结构）-t=当前结构文件 -o=差异 SQL，对比完删两份临时文件
:::::  退出码: 0=生成成功；非 0=生成失败，不执行更新
:::::------------------------------------------------------------
:update_database
call :mysql_check_args
if errorlevel 1 exit /b 1

call :select_db_type
call :select_server_id
if errorlevel 1 exit /b 1

call :compose_db_name

set "mysql_sql_file=!project_dir!\common\sql\!db_type!_table.sql"
if not exist "!mysql_sql_file!" (
    echo [错误] 未找到 SQL 文件: !mysql_sql_file!
    exit /b 1
)

set "mysqldiff_dir=!project_dir!\common\sql\mysqldiff"
set "mysqldiff_exe=!mysqldiff_dir!\dist\mysqldiff.exe"
if not exist "!mysqldiff_exe!" (
    echo [错误] 未找到差异工具: !mysqldiff_exe!
    exit /b 1
)

set "mysqldump_exe=!mysql_dirpath!\bin\mysqldump.exe"
if not exist "!mysqldump_exe!" (
    echo [错误] 未找到 mysqldump.exe: !mysqldump_exe!
    exit /b 1
)

call :mysql_database_exists
if errorlevel 1 (
    echo [错误] 数据库不存在: !db_name!
    exit /b 1
)

if not exist "%~dp0tmp" mkdir "%~dp0tmp"
set "mysql_diff_file=%~dp0tmp\local_server_db_update_diff.sql"
set "mysql_structure_file=%~dp0tmp\local_server_db_update_current.sql"
if exist "!mysql_diff_file!" del /q "!mysql_diff_file!" >nul 2>nul
if exist "!mysql_structure_file!" del /q "!mysql_structure_file!" >nul 2>nul

echo "[对比] 导出 !db_name! 当前表结构"
"!mysqldump_exe!" -h!mysql_hostname! -P!mysql_port! -u!mysql_username! -p!mysql_password! --no-data --result-file="!mysql_structure_file!" !db_name!
if errorlevel 1 (
    del /q "!mysql_structure_file!" >nul 2>nul
    echo [错误] 导出当前库结构失败
    exit /b 1
)

echo "[对比] !mysql_sql_file! ^<--^> !db_name!"
"!mysqldiff_exe!" -d file -s "!mysql_sql_file!" -t "!mysql_structure_file!" -o "!mysql_diff_file!"
set "mysqldiff_result=!errorlevel!"
del /q "!mysql_structure_file!" >nul 2>nul
if not "!mysqldiff_result!"=="0" (
    del /q "!mysql_diff_file!" >nul 2>nul
    echo [错误] 生成差异 SQL 失败，不执行更新
    exit /b 1
)

findstr /i /c:"ALTER TABLE" /c:"CREATE TABLE" /c:"DROP TABLE" "!mysql_diff_file!" >nul
if errorlevel 1 (
    del /q "!mysql_diff_file!" >nul 2>nul
    echo "[更新] 结构和 !db_type!_table.sql 一致，无需更新"
    exit /b 0
)

echo "[更新] 执行差异 SQL: !mysql_diff_file!"
call :mysql_import "!mysql_diff_file!"
set "mysql_update_result=!errorlevel!"
del /q "!mysql_diff_file!" >nul 2>nul
exit /b !mysql_update_result!

:::::------------------------------------------------------------
:::::db_export_structure：导出结构，选服务器后用 mysqldump --no-data 把「实际库名」的表结构
:::::           导到 项目目录\common\sql\<数据库类型>_table.sql（覆盖原文件，只导结构不导数据）
:::::  调用约定与 db_create / db_update 一致：第2参数=项目目录，第3参数=库名中间段，类型和服ID执行时选
:::::------------------------------------------------------------
:export_structure
call :mysql_check_args
if errorlevel 1 exit /b 1

call :select_db_type
call :select_server_id
if errorlevel 1 exit /b 1

call :compose_db_name

set "mysqldump_exe=!mysql_dirpath!\bin\mysqldump.exe"
if not exist "!mysqldump_exe!" (
    echo [错误] 未找到 mysqldump.exe: !mysqldump_exe!
    exit /b 1
)

call :mysql_database_exists
if errorlevel 1 (
    echo [错误] 数据库不存在: !db_name!
    exit /b 1
)

set "mysql_sql_file=!project_dir!\common\sql\!db_type!_table.sql"
echo "[导出结构] !db_name! -> !mysql_sql_file!"
"!mysqldump_exe!" -h!mysql_hostname! -P!mysql_port! -u!mysql_username! -p!mysql_password! --no-data --result-file="!mysql_sql_file!" !db_name!
if errorlevel 1 (
    echo [错误] 导出结构失败: !mysql_sql_file!
    exit /b 1
)
echo "[导出结构] 完成: !mysql_sql_file!"
exit /b 0

::::::------------------------------------------------------------
::::::db_export_def：导出def，调「项目目录\common\tools\tabledefcreator\dist\creatormain.exe」，
::::::           把「实际库名」的表结构导出成 server\skynet\<模块>\src\define\MysqlTableDefine.lua
::::::  调用约定与 db_create / db_update 一致：第2参数=项目目录，第3参数=库名中间段，类型和服ID执行时选
::::::  creatormain.exe 参数（跟 common\tools\tabledefcreator\tabledefcreator.bat 一致）:
::::::      host port user password dbname 导出文件路径
::::::  creatormain.exe 内部吞掉异常、退出码固定为 0，所以导出后检查文件是否存在且完整（结尾是 return M）
::::::------------------------------------------------------------
:export_def
call :mysql_check_args
if errorlevel 1 exit /b 1

call :select_db_type
call :select_server_id
if errorlevel 1 exit /b 1

call :compose_db_name

set "creatormain_exe=!project_dir!\common\tools\tabledefcreator\dist\creatormain.exe"
if not exist "!creatormain_exe!" (
    echo [错误] 未找到导出工具: !creatormain_exe!
    exit /b 1
)

call :find_def_dir
if errorlevel 1 exit /b 1
set "def_file=!def_dir!\MysqlTableDefine.lua"

call :mysql_database_exists
if errorlevel 1 (
    echo [错误] 数据库不存在: !db_name!
    exit /b 1
)

echo "[导出def] !db_name! -> !def_file!"
"!creatormain_exe!" !mysql_hostname! !mysql_port! !mysql_username! !mysql_password! !db_name! "!def_file!"

if not exist "!def_file!" (
    echo [错误] 导出def失败，未生成文件: !def_file!
    exit /b 1
)
findstr /c:"return M" "!def_file!" >nul
if errorlevel 1 (
    echo [错误] 导出def失败，文件不完整: !def_file!
    exit /b 1
)
echo "[导出def] 完成: !def_file!"
exit /b 0

::::::------------------------------------------------------------
::::::公共接口：定位服务端 define 目录（MysqlTableDefine.lua 的输出位置）
::::::  server\skynet 下按模块分目录，取其中带 src\define 的那个，如 server\skynet\ikun\src\define
::::::  出参: 变量 def_dir
::::::------------------------------------------------------------
:find_def_dir
set "def_dir="
if "!project_dir!"=="" (
    echo [错误] 未指定项目目录
    exit /b 1
)

for /d %%d in ("!project_dir!\server\skynet\*") do (
    if exist "%%d\src\define" set "def_dir=%%d\src\define"
)
if "!def_dir!"=="" (
    echo [错误] 未找到 define 目录: !project_dir!\server\skynet\<模块>\src\define
    exit /b 1
)
exit /b 0

:::::------------------------------------------------------------
:::::db_query：查询，列出所有数据库并按库名模糊匹配，打印匹配到的数据库名
:::::  调用约定与其它数据库操作一致：第2参数=项目目录，第3参数=库名中间段（模糊匹配用）
:::::  例: db_query C:\data\ikun_01_cn lizexian 匹配出 dbserver_lizexian_1 / logserver_lizexian_1
:::::------------------------------------------------------------
:query_database
call :mysql_check_args
if errorlevel 1 exit /b 1

echo "[查询] 模糊匹配数据库名: !db_name!"
"!mysql_exe!" -h!mysql_hostname! -P!mysql_port! -u!mysql_username! -p!mysql_password! -N -B -e "SHOW DATABASES LIKE '%%!db_name!%%';"
if errorlevel 1 (
    echo [错误] 查询数据库失败: !db_name!
    exit /b 1
)
exit /b 0

:::::------------------------------------------------------------
:::::db_export：导出，用 mysqldump 把「实际库名」的表结构和数据导到 项目目录\common\sql\backup
:::::  文件名: <实际库名>_<yyyyMMddHHmm>.sql，如 dbserver_lizexian_1_202611231155.sql
:::::  调用约定与 db_create / db_update 一致：第2参数=项目目录，第3参数=库名中间段，类型和服ID执行时选
:::::------------------------------------------------------------
:export_database
call :mysql_check_args
if errorlevel 1 exit /b 1

call :select_db_type
call :select_server_id
if errorlevel 1 exit /b 1

call :compose_db_name

set "mysqldump_exe=!mysql_dirpath!\bin\mysqldump.exe"
if not exist "!mysqldump_exe!" (
    echo [错误] 未找到 mysqldump.exe: !mysqldump_exe!
    exit /b 1
)

call :get_datetime_stamp
if errorlevel 1 exit /b 1

set "backup_dir=!project_dir!\common\sql\backup"
if not exist "!backup_dir!" mkdir "!backup_dir!"
set "backup_file=!backup_dir!\!db_name!_!datetime_stamp!.sql"

call :mysql_database_exists
if errorlevel 1 (
    echo [错误] 数据库不存在: !db_name!
    exit /b 1
)

echo "[导出] 表结构和数据: !db_name! -> !backup_file!"
"!mysqldump_exe!" -h!mysql_hostname! -P!mysql_port! -u!mysql_username! -p!mysql_password! --result-file="!backup_file!" !db_name!
if errorlevel 1 (
    echo [错误] 导出失败: !backup_file!
    exit /b 1
)
exit /b 0

:::::------------------------------------------------------------
:::::db_import：导入，选服务器后把 项目目录\common\sql\backup 下的备份文件导进「实际库名」
:::::  调用约定与 db_create / db_update 一致：第2参数=项目目录，第3参数=库名中间段，类型和服ID执行时选
:::::  备份文件名执行时输入（只填文件名，不带目录）
:::::------------------------------------------------------------
:import_database
call :mysql_check_args
if errorlevel 1 exit /b 1

call :select_db_type
call :select_server_id
if errorlevel 1 exit /b 1

call :compose_db_name

set /p "backup_file_name=请输入备份文件名（common\sql\backup 下，如 dbserver_lizexian_1_202611231155.sql）: "
set "backup_file=!project_dir!\common\sql\backup\!backup_file_name!"
if not exist "!backup_file!" (
    echo [错误] 未找到备份文件: !backup_file!
    exit /b 1
)

call :mysql_import "!backup_file!"
exit /b !errorlevel!


:::::------------------------------------------------------------
:::::数据库操作公共接口：让玩家选择数据库类型（1=dbserver，2=logserver）
:::::  出参: 变量 db_type
:::::------------------------------------------------------------
:select_db_type
echo 请选择数据库类型:
echo   【1】!db_type_array[1]!
echo   【2】!db_type_array[2]!
choice /c 12 /n /m "输入序号选择数据库类型: "
if errorlevel 2 (
    set "db_type=!db_type_array[2]!"
) else (
    set "db_type=!db_type_array[1]!"
)
echo "[选择] 数据库类型: !db_type!"
exit /b 0

:::::------------------------------------------------------------
:::::数据库操作公共接口：按类型和服ID把 db_name 拼成实际库名（就地改写 db_name）
:::::  统一拼接: <数据库类型>_<数据库名>_<服ID>，如 dbserver_lizexian_1 / logserver_lizexian_1
:::::  入参: 全局变量 db_type / db_name / server_id
:::::------------------------------------------------------------
:compose_db_name
set "db_name=!db_type!_!db_name!_!server_id!"
echo "[库名] !db_name!"
exit /b 0

:::::------------------------------------------------------------
:::::公共接口：取当前时间戳 yyyyMMddHHmm，如 202611231155，用于备份文件名
:::::  %date% / %time% 跟着系统区域格式走，这里用 PowerShell 输出固定格式，不解析本地格式
:::::  出参: 变量 datetime_stamp
:::::------------------------------------------------------------
:get_datetime_stamp
set "datetime_stamp="
for /f "usebackq delims=" %%i in (`powershell -NoProfile -Command "Get-Date -Format yyyyMMddHHmm"`) do set "datetime_stamp=%%i"
if "!datetime_stamp!"=="" (
    echo [错误] 获取当前时间戳失败
    exit /b 1
)
exit /b 0

:::::------------------------------------------------------------
:::::数据库操作公共接口：校验项目目录 / 数据库名，读取 MySQL 配置并定位 mysql.exe
:::::  入参: 全局变量 project_dir（第2个参数）、db_name（第3个参数）
:::::  出参: mysql_exe / mysql_hostname / mysql_port / mysql_username / mysql_password
:::::  失败: 打印错误并 exit /b 1，调用方接 if errorlevel 1 exit /b 1
:::::------------------------------------------------------------
:mysql_check_args
if "!project_dir!"=="" (
    echo [错误] 未指定项目目录
    exit /b 1
)

if "!db_name!"=="" (
    echo [错误] 未指定数据库名
    exit /b 1
)

call :mysql_load_config
if errorlevel 1 exit /b 1

if "!mysql_dirpath!"=="" (
    echo [错误] 未配置 dirpath，请在界面「环境部署 / Local_MySql_配置」填写并点确认
    exit /b 1
)

if "!mysql_hostname!"=="" (
    echo [错误] 未配置 hostname，请在界面「环境部署 / Local_MySql_配置」填写并点确认
    exit /b 1
)

if "!mysql_port!"=="" (
    echo [错误] 未配置 port，请在界面「环境部署 / Local_MySql_配置」填写并点确认
    exit /b 1
)

if "!mysql_username!"=="" (
    echo [错误] 未配置 username，请在界面「环境部署 / Local_MySql_配置」填写并点确认
    exit /b 1
)

if "!mysql_password!"=="" (
    echo [错误] 未配置 password，请在界面「环境部署 / Local_MySql_配置」填写并点确认
    exit /b 1
)

set "mysql_exe=!mysql_dirpath!\bin\mysql.exe"
if not exist "!mysql_exe!" (
    echo [错误] 未找到 mysql.exe: !mysql_exe!
    exit /b 1
)

exit /b 0

:::::------------------------------------------------------------
:::::数据库操作公共接口：从 exe 同级的 config/config.json 读取「环境部署/Local_MySql_配置」
:::::  出参: mysql_dirpath / mysql_hostname / mysql_port / mysql_username / mysql_password
:::::------------------------------------------------------------
:mysql_load_config
set "mysql_config_path=%~dp0..\config\config.json"

:::::解析用的临时文件放在脚本同级的 tmp 目录，用前清理、用完删除
set "mysql_config_tmp=%~dp0tmp\local_server_mysql_config_tmp.txt"
if not exist "%~dp0tmp" mkdir "%~dp0tmp"
if exist "!mysql_config_tmp!" del /q "!mysql_config_tmp!" >nul 2>nul

powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference = 'Stop'; $j = ConvertFrom-Json (Get-Content -Raw -LiteralPath '%mysql_config_path%' -Encoding UTF8); $s = $j.'环境部署'.'Local_MySql_配置'; 'dirpath=' + $s.dirpath; 'hostname=' + $s.hostname; 'port=' + $s.port; 'username=' + $s.username; 'password=' + $s.password" > "!mysql_config_tmp!"

if errorlevel 1 (
    for %%i in ("%mysql_config_path%") do echo [错误] 读取配置失败，请检查 %%~fi 是否为合法 JSON
    del /q "!mysql_config_tmp!" >nul 2>nul
    exit /b 1
)

for /f "usebackq tokens=1,* delims==" %%a in ("!mysql_config_tmp!") do (
    if /i "%%a"=="dirpath" set "mysql_dirpath=%%b"
    if /i "%%a"=="hostname" set "mysql_hostname=%%b"
    if /i "%%a"=="port" set "mysql_port=%%b"
    if /i "%%a"=="username" set "mysql_username=%%b"
    if /i "%%a"=="password" set "mysql_password=%%b"
)
del /q "!mysql_config_tmp!" >nul 2>nul
exit /b 0

:::::------------------------------------------------------------
:::::数据库操作公共接口：判断 !db_name! 是否存在，存在 exit /b 0，不存在 exit /b 1
:::::------------------------------------------------------------
:mysql_database_exists
"!mysql_exe!" -h!mysql_hostname! -P!mysql_port! -u!mysql_username! -p!mysql_password! -e "USE `!db_name!`;" >nul 2>nul
if errorlevel 1 exit /b 1
exit /b 0

:::::------------------------------------------------------------
:::::数据库操作公共接口：执行一条 SQL  %~1=要执行的 SQL
:::::------------------------------------------------------------
:mysql_execute
echo "[执行] %~1"
"!mysql_exe!" -h!mysql_hostname! -P!mysql_port! -u!mysql_username! -p!mysql_password! -e "%~1"
if errorlevel 1 (
    echo [错误] SQL 执行失败: %~1
    exit /b 1
)
exit /b 0

:::::------------------------------------------------------------
:::::数据库操作公共接口：把 SQL 文件导入 !db_name! 库  %~1=SQL 文件全路径
:::::------------------------------------------------------------
:mysql_import
echo "[导入] %~1"
"!mysql_exe!" -h!mysql_hostname! -P!mysql_port! -u!mysql_username! -p!mysql_password! -D !db_name! < "%~1"
if errorlevel 1 (
    echo [错误] 导入 SQL 失败: %~1
    exit /b 1
)
exit /b 0


:::::------------------------------------------------------------
:::::数据库操作公共接口：清空 !db_name! 里所有表的数据
:::::  先从 information_schema 取表名，拼成 TRUNCATE 脚本再导入；
:::::  前后关/开外键检查，避免带外键的表 TRUNCATE 失败
:::::------------------------------------------------------------
:mysql_truncate_tables
set "mysql_tables_tmp=%~dp0tmp\local_server_mysql_tables_tmp.txt"
set "mysql_truncate_tmp=%~dp0tmp\local_server_mysql_truncate_tmp.sql"
if not exist "%~dp0tmp" mkdir "%~dp0tmp"
if exist "!mysql_tables_tmp!" del /q "!mysql_tables_tmp!" >nul 2>nul

"!mysql_exe!" -h!mysql_hostname! -P!mysql_port! -u!mysql_username! -p!mysql_password! -N -B -e "SELECT table_name FROM information_schema.tables WHERE table_schema = '!db_name!' AND table_type = 'BASE TABLE';" > "!mysql_tables_tmp!"
if errorlevel 1 (
    echo [错误] 查询表名失败: !db_name!
    del /q "!mysql_tables_tmp!" >nul 2>nul
    exit /b 1
)

set "mysql_tables_count=0"
> "!mysql_truncate_tmp!" echo SET FOREIGN_KEY_CHECKS = 0;
for /f "usebackq delims=" %%t in ("!mysql_tables_tmp!") do (
    >> "!mysql_truncate_tmp!" echo TRUNCATE TABLE `%%t`;
    set /a mysql_tables_count+=1
)
>> "!mysql_truncate_tmp!" echo SET FOREIGN_KEY_CHECKS = 1;

echo "[清档] 表数量 !mysql_tables_count!"
call :mysql_import "!mysql_truncate_tmp!"
set "mysql_truncate_result=!errorlevel!"
del /q "!mysql_tables_tmp!" "!mysql_truncate_tmp!" >nul 2>nul
exit /b !mysql_truncate_result!
