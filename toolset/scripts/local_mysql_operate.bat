@echo off
setlocal enabledelayedexpansion

::::统一切到 936 代码页，保证本脚本（GBK）里的中文能正确传给 PowerShell 解析 JSON
chcp 936 >nul

::::用法: local_mysql_operate.bat <功能名>
::::功能名与下面的标签一一对应
set "action=%~1"

if "!action!"=="" (
    call :print_usage
    exit /b 1
)

if /i "!action!"=="config" (
    call :config_service
    exit /b !errorlevel!
)

if /i "!action!"=="register" (
    call :register_service
    exit /b !errorlevel!
)

if /i "!action!"=="remove" (
    call :remove_service
    exit /b !errorlevel!
)

if /i "!action!"=="start" (
    call :start_service
    exit /b !errorlevel!
)

if /i "!action!"=="stop" (
    call :stop_service
    exit /b !errorlevel!
)

echo [错误] 未定义的功能: !action!
call :print_usage
exit /b 1

::::------------------------------------------------------------
::::用法
::::------------------------------------------------------------
:print_usage
echo 用法: %~nx0 ^<功能名^>
echo 可用功能:
echo   config   - 配置服务（拷 envconf\mysql\my.ini 到 dirpath 并替换 BASE_DIR / DATA_DIR）
echo   register - 注册服务（mysqld --install ^<servicename^> --defaults-file=^<dirpath^>\my.ini，需先执行 config）
echo   remove   - 移除服务（sc delete ^<servicename^>，不删数据目录）
echo   start    - 启动服务（net start ^<servicename^>）
echo   stop     - 停止服务（net stop ^<servicename^>）
exit /b 0

::::------------------------------------------------------------
::::从 exe 同级的 config/config.json 读取 servicename / dirpath
::::------------------------------------------------------------
:load_config
set "config_path=%~dp0..\config\config.json"

::::解析用的临时文件放在脚本同级的 tmp 目录，用前清理、用完删除
set "config_tmp=%~dp0tmp\local_mysql_config_tmp.txt"
if not exist "%~dp0tmp" mkdir "%~dp0tmp"
if exist "%config_tmp%" del /q "%config_tmp%" >nul 2>nul

powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference = 'Stop'; $j = ConvertFrom-Json (Get-Content -Raw -LiteralPath '%config_path%' -Encoding UTF8); $s = $j.'环境部署'.'Local_MySql_配置'; 'servicename=' + $s.servicename; 'dirpath=' + $s.dirpath" > "%config_tmp%"

if errorlevel 1 (
    for %%i in ("%config_path%") do echo [错误] 读取配置失败，请检查 %%~fi 是否为合法 JSON
    del /q "%config_tmp%" >nul 2>nul
    exit /b 1
)

for /f "usebackq tokens=1,* delims==" %%a in ("%config_tmp%") do (
    if /i "%%a"=="servicename" set "mysql_service_name=%%b"
    if /i "%%a"=="dirpath" set "mysql_dirpath=%%b"
)
del /q "%config_tmp%" >nul 2>nul
exit /b 0

::::------------------------------------------------------------
::::配置服务：拷贝 envconf\mysql\my.ini 到 dirpath\my.ini，并把模板里的
::::          BASE_DIR / DATA_DIR 替换成实际路径（basedir=dirpath，datadir=dirpath/data）
::::------------------------------------------------------------
:config_service
call :load_config
if errorlevel 1 exit /b 1

if "!mysql_dirpath!"=="" (
    echo [错误] 未配置 dirpath，请在界面「环境部署 / Local_MySql_配置」填写并点确认
    exit /b 1
)

set "source_ini=%~dp0..\envconf\mysql\my.ini"
if not exist "!source_ini!" (
    echo [错误] 未找到模板文件: !source_ini!
    exit /b 1
)

::::生成 my.ini，basedir 用正斜杠，datadir 为 dirpath/data
set "output_ini=!mysql_dirpath!\my.ini"
set "ini_base_dir=!mysql_dirpath:\=/!"
set "ini_data_dir=%ini_base_dir%/data"

if exist "!output_ini!" del /f /q "!output_ini!" >nul 2>nul

echo "[配置服务] !source_ini! -> !output_ini!"
for /f "usebackq tokens=* delims=" %%a in ("!source_ini!") do (
    set "line=%%a"
    set "line=!line:BASE_DIR=%ini_base_dir%!"
    set "line=!line:DATA_DIR=%ini_data_dir%!"
    echo !line!>>"!output_ini!"
)

if not exist "!output_ini!" (
    echo [错误] 生成 my.ini 失败，请检查 dirpath 是否存在: !mysql_dirpath!
    exit /b 1
)

echo "[配置服务] 完成"
exit /b 0

::::------------------------------------------------------------
::::注册服务：只负责注册 Windows 服务
::::  在 dirpath\bin 下执行 mysqld --install <servicename> --defaults-file=<dirpath>\my.ini
::::  my.ini 由 config 功能生成
::::------------------------------------------------------------
:register_service
call :load_config
if errorlevel 1 exit /b 1

if "!mysql_dirpath!"=="" (
    echo [错误] 未配置 dirpath，请在界面「环境部署 / Local_MySql_配置」填写并点确认
    exit /b 1
)

if "!mysql_service_name!"=="" (
    echo [错误] 未配置 servicename，请在界面「环境部署 / Local_MySql_配置」填写并点确认
    exit /b 1
)

call :ensure_admin
if errorlevel 1 exit /b 1

echo "[注册服务] 安装服务 !mysql_service_name!"
pushd "!mysql_dirpath!\bin"
mysqld --install "!mysql_service_name!" --defaults-file="!mysql_dirpath!\my.ini"
if errorlevel 1 (
    echo [错误] 注册服务失败，请确认 servicename 未被占用且 mysqld 可用
    popd
    exit /b 1
)
popd

echo "[注册服务] 完成"
exit /b 0

::::------------------------------------------------------------
::::移除服务：只卸载 Windows 服务，不动 dirpath 下的数据和 my.ini
::::  sc delete <servicename>，只认服务名，不需要 dirpath
::::------------------------------------------------------------
:remove_service
call :load_config
if errorlevel 1 exit /b 1

if "!mysql_service_name!"=="" (
    echo [错误] 未配置 servicename，请在界面「环境部署 / Local_MySql_配置」填写并点确认
    exit /b 1
)

call :ensure_admin
if errorlevel 1 exit /b 1

echo "[移除服务] 卸载服务 !mysql_service_name!"
sc delete "!mysql_service_name!"
if errorlevel 1 (
    echo [错误] 移除服务失败，请确认服务名是否正确、服务是否已停止
    exit /b 1
)

echo "[移除服务] 完成"
exit /b 0

::::------------------------------------------------------------
::::启动服务：net start <servicename>
::::------------------------------------------------------------
:start_service
call :load_config
if errorlevel 1 exit /b 1

if "!mysql_service_name!"=="" (
    echo [错误] 未配置 servicename，请在界面「环境部署 / Local_MySql_配置」填写并点确认
    exit /b 1
)

call :ensure_admin
if errorlevel 1 exit /b 1

echo "[启动服务] 启动服务 !mysql_service_name!"
net start "!mysql_service_name!"
if errorlevel 1 (
    echo [错误] 启动服务失败，请确认服务已注册且未处于运行状态
    exit /b 1
)

echo "[启动服务] 完成"
exit /b 0

::::------------------------------------------------------------
::::停止服务：net stop <servicename>
::::------------------------------------------------------------
:stop_service
call :load_config
if errorlevel 1 exit /b 1

if "!mysql_service_name!"=="" (
    echo [错误] 未配置 servicename，请在界面「环境部署 / Local_MySql_配置」填写并点确认
    exit /b 1
)

call :ensure_admin
if errorlevel 1 exit /b 1

echo "[停止服务] 停止服务 !mysql_service_name!"
net stop "!mysql_service_name!"
if errorlevel 1 (
    echo [错误] 停止服务失败，请确认服务已注册且未处于停止状态
    exit /b 1
)

echo "[停止服务] 完成"
exit /b 0

::::------------------------------------------------------------
::::注册/移除/启动/停止服务都需要管理员权限：没有就提权重启自己，原进程直接退出
::::------------------------------------------------------------
:ensure_admin
net session >nul 2>&1
if not errorlevel 1 exit /b 0

echo "[!action!] 需要管理员权限，正在以管理员身份重新启动..."
powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -ArgumentList '!action!' -Verb RunAs"
if errorlevel 1 (
    echo [错误] 获取管理员权限失败
    exit /b 1
)

exit /b 1
