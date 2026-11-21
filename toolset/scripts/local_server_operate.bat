@echo off
setlocal enabledelayedexpansion

::::统一切到 936 代码页，保证本脚本（GBK）里的中文正常显示
chcp 936 >nul

::::用法: local_server_operate.bat <功能名> <项目目录> [服务器名]
::::功能名: build（编译）/ rebuild（重编）/ clean（清理）/ release（发布）
::::        start（开服）/ stop（关服）/ status（查看）
::::项目目录 = 检出根目录\项目名，由调用方传进来，脚本里不写死项目路径
::::服务器名只对 start / stop / status 有意义，对应 server_tool.sh 的 -n
::::服务器类型（-m）和服ID（-s）都不走参数，start / stop / status 时由脚本在控制台里让玩家选
::::服务器类型选 allserver 时，脚本自己按 server_model_array 的顺序逐个类型执行
:::::------------------------------------------------------------
:::::数据库操作：对本地 Windows MySQL 执行命令
:::::  功能名: create（建库）/ delete（删库）/ clear（清档）/ update（更新）
:::::  第3参数只传中间那段库名（如 lizexian），实际库名按类型拼: dbserver_<数据库名>_<服ID> / logserver_<数据库名>
:::::  SQL 取「项目目录\common\sql」下的 <数据库类型>_table.sql（create）或 <数据库类型>_change.sql（update），clear 不需要 SQL 文件
:::::  连接信息取 exe 同级 config\config.json 的「环境部署/Local_MySql_配置」（dirpath / hostname / port / username / password）
:::::  例: local_server_operate.bat create C:\data\ikun_01_cn lizexian
:::::------------------------------------------------------------

set "action=%~1"
set "project_dir=%~2"
set "server_name=%~3"
set "db_name=%~3"

::::服务器类型固定只有这几种，start / stop / status 执行时由玩家按序号选择
::::顺序即 allserver 时的执行顺序，名字必须和 server_tool.sh 的 -m 取值一致（网关是 gateway，不是 gateserver）
set "server_model_array[1]=allserver"
set "server_model_array[2]=logserver"
set "server_model_array[3]=dbserver"
set "server_model_array[4]=gameserver"
set "server_model_array[5]=loginserver"
set "server_model_array[6]=gateway"

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

if /i "!action!"=="create" (
    call :create_database
    goto :finish
)

if /i "!action!"=="delete" (
    call :delete_database
    goto :finish
)

if /i "!action!"=="clear" (
    call :clear_database
    goto :finish
)

if /i "!action!"=="update" (
    call :update_database
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
echo   create  - 建库：建「实际库名」，再导入 ^<数据库类型^>_table.sql
echo   delete  - 删库：删除「实际库名」
echo   clear   - 清档：保留库和表结构，清空「实际库名」里所有表的数据（TRUNCATE TABLE，不需要 SQL 文件）
echo   update  - 更新：导入 ^<数据库类型^>_change.sql
echo 数据库操作用 第3参数=数据库名，数据库类型和服ID执行时输入（1=!db_type_array[1]!，2=!db_type_array[2]!）:
echo 实际库名按类型拼: dbserver_^<数据库名^>_^<服ID^> / logserver_^<数据库名^>
echo   %~nx0 create C:\data\ikun_01_cn lizexian
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

call :run_server_tool start 开服
exit /b !errorlevel!

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

call :run_server_tool stop 关服
exit /b !errorlevel!

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

call :run_server_tool status 查看
exit /b !errorlevel!

::::------------------------------------------------------------
::::开服 / 关服 / 查看公共接口：让玩家选择服务器类型
::::  出参: 变量 server_model，取值见 server_model_array
::::------------------------------------------------------------
:select_server_model
echo 请选择服务器类型:
echo   【1】!server_model_array[1]!
echo   【2】!server_model_array[2]!
echo   【3】!server_model_array[3]!
echo   【4】!server_model_array[4]!
echo   【5】!server_model_array[5]!
echo   【6】!server_model_array[6]!
choice /c 123456 /n /m "输入序号选择服务器类型: "

::::choice 把选中的序号放在 errorlevel 里，借 for 做一次变量间接取值
for %%i in (!errorlevel!) do set "server_model=!server_model_array[%%i]!"
echo "[选择] 服务器类型: !server_model!"
exit /b 0

::::------------------------------------------------------------
::::开服 / 关服 / 查看公共接口：执行 server_tool.sh
::::  %~1=动作（start / stop / status），%~2=动作中文名（只用于日志）
::::  依赖全局变量 server_model / wsl_path / server_name / server_id
::::  选 allserver 时跳过 server_model_array[1] 自己，按数组顺序逐个类型执行，任意一次失败就中止
::::------------------------------------------------------------
:run_server_tool
if /i "!server_model!"=="allserver" goto :run_server_tool_all

call :exec_server_tool "%~1" "!server_model!" "%~2"
exit /b !errorlevel!

:run_server_tool_all
for %%i in (2 3 4 5 6) do (
    call :exec_server_tool "%~1" "!server_model_array[%%i]!" "%~2"
    if errorlevel 1 exit /b !errorlevel!
)
exit /b 0

::::------------------------------------------------------------
::::开服 / 关服 / 查看公共接口：对单个服务器类型执行一次 server_tool.sh
::::  %~1=动作（start / stop / status），%~2=服务器类型（-m），%~3=动作中文名（只用于日志）
::::  命令交给 sshcall_wsl.bat 登录后执行；整体用双引号包住，命令里的两个与号不会被 cmd 当成命令分隔符
::::  sudo 加在 server_tool.sh 前面，不能加在 cd 前面（cd 是 shell 内建命令，sudo 找不到它）
::::------------------------------------------------------------
:exec_server_tool
echo "[操作] %~3 服务器类型: %~2"
set "ssh_commond=cd '!wsl_path!' && sudo ./server_tool.sh -o %~1 -m %~2 -n !server_name! -s !server_id!"
call "%~dp0sshcall_wsl.bat" "!ssh_commond!"
exit /b !errorlevel!

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
:::::create：建库，再导入「项目目录\common\sql\<数据库类型>_table.sql」
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
:::::delete：删库
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
:::::clear：清档，保留数据库和表结构，只清空库里所有表的数据（TRUNCATE TABLE，不需要 SQL 文件）
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
:::::update：导入「项目目录\common\sql\<数据库类型>_change.sql」
:::::------------------------------------------------------------
:update_database
call :mysql_check_args
if errorlevel 1 exit /b 1

call :select_db_type
call :select_server_id
if errorlevel 1 exit /b 1

call :compose_db_name

set "mysql_sql_file=!project_dir!\common\sql\!db_type!_change.sql"
if not exist "!mysql_sql_file!" (
    echo [错误] 未找到 SQL 文件: !mysql_sql_file!
    exit /b 1
)

call :mysql_database_exists
if errorlevel 1 (
    echo [错误] 数据库不存在: !db_name!
    exit /b 1
)

call :mysql_import "!mysql_sql_file!"
exit /b !errorlevel!


:::::------------------------------------------------------------
:::::数据库操作公共接口：让玩家选择数据库类型（1=logserver，2=dbserver）
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
:::::数据库操作公共接口：让玩家输入服ID（数字且大于 0）
:::::  出参: 变量 server_id
:::::------------------------------------------------------------
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

:::::------------------------------------------------------------
:::::数据库操作公共接口：按类型和服ID把 db_name 拼成实际库名（就地改写 db_name）
:::::  dbserver -> dbserver_<数据库名>_<服ID>，logserver -> logserver_<数据库名>
:::::  与 config_global_vars.sh 的 DBSERVER_MYSQL_DBNAME=dbserver_%s_%d / LOGSERVER_MYSQL_DBNAME=logserver_%s 一致
:::::  入参: 全局变量 db_type / db_name / server_id
:::::------------------------------------------------------------
:compose_db_name
if /i "!db_type!"=="logserver" (
    set "db_name=!db_type!_!db_name!"
) else (
    set "db_name=!db_type!_!db_name!_!server_id!"
)
echo "[库名] !db_name!"
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
