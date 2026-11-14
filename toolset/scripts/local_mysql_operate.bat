@echo off
setlocal enabledelayedexpansion

::::统一切到 936 代码页，保证本脚本（GBK）里的中文能正确传给 PowerShell 解析 JSON
chcp 936 >nul

::::用法: local_mysql_operate.bat <功能名>
::::功能名与下面的标签一一对应
set "action=%~1"

::::首次初始化 data 目录后给 root 设的密码
set "root_password=123456"

if "!action!"=="" (
    call :print_usage
    exit /b 1
)

if /i "!action!"=="config" (
    call :config_service
    goto :finish
)

if /i "!action!"=="register" (
    call :register_service
    goto :finish
)

if /i "!action!"=="remove" (
    call :remove_service
    goto :finish
)

if /i "!action!"=="start" (
    call :start_service
    goto :finish
)

if /i "!action!"=="stop" (
    call :stop_service
    goto :finish
)

echo [错误] 未定义的功能: !action!
call :print_usage
exit /b 1

:finish
::::提权出来的副本带 elevated 参数，跑完 pause 停住，日志才看得见，否则窗口一闪而过
set "opt_result=!errorlevel!"
if /i "%~2"=="elevated" pause
exit /b !opt_result!

::::------------------------------------------------------------
::::用法
::::------------------------------------------------------------
:print_usage
echo 用法: %~nx0 ^<功能名^>
echo 可用功能:
echo   config   - 配置服务（拷 envconf\mysql\my.ini 到 dirpath 并替换 CFG_BASE_DIR / CFG_DATA_DIR / CFG_PORT）
echo   register - 注册服务（install 后自动启动，首次注册会初始化 data 目录、root 密码设成 123456，并按配置的 username/password 加一个权限等同 root 的用户；需先执行 config）
echo   remove   - 移除服务（运行中先 net stop ^<servicename^>，再 sc delete，不删数据目录）
echo   start    - 启动服务（net start ^<servicename^>）
echo   stop     - 停止服务（net stop ^<servicename^>）
exit /b 0

::::------------------------------------------------------------
::::从 exe 同级的 config/config.json 读取 servicename / dirpath / port / username / password
::::------------------------------------------------------------
:load_config
set "config_path=%~dp0..\config\config.json"

::::解析用的临时文件放在脚本同级的 tmp 目录，用前清理、用完删除
set "config_tmp=%~dp0tmp\local_mysql_config_tmp.txt"
if not exist "%~dp0tmp" mkdir "%~dp0tmp"
if exist "%config_tmp%" del /q "%config_tmp%" >nul 2>nul

powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference = 'Stop'; $j = ConvertFrom-Json (Get-Content -Raw -LiteralPath '%config_path%' -Encoding UTF8); $s = $j.'环境部署'.'Local_MySql_配置'; 'servicename=' + $s.servicename; 'dirpath=' + $s.dirpath; 'port=' + $s.port; 'username=' + $s.username; 'password=' + $s.password" > "%config_tmp%"

if errorlevel 1 (
    for %%i in ("%config_path%") do echo [错误] 读取配置失败，请检查 %%~fi 是否为合法 JSON
    del /q "%config_tmp%" >nul 2>nul
    exit /b 1
)

for /f "usebackq tokens=1,* delims==" %%a in ("%config_tmp%") do (
    if /i "%%a"=="servicename" set "mysql_service_name=%%b"
    if /i "%%a"=="dirpath" set "mysql_dirpath=%%b"
    if /i "%%a"=="port" set "mysql_port=%%b"
    if /i "%%a"=="username" set "mysql_username=%%b"
    if /i "%%a"=="password" set "mysql_password=%%b"
)
del /q "%config_tmp%" >nul 2>nul
exit /b 0

::::------------------------------------------------------------
::::配置服务：拷贝 envconf\mysql\my.ini 到 dirpath\my.ini，并把模板里的
::::          CFG_BASE_DIR / CFG_DATA_DIR / CFG_PORT 替换成实际值
::::          basedir=dirpath，datadir=dirpath/data，port 取配置里的 port
::::------------------------------------------------------------
:config_service
call :load_config
if errorlevel 1 exit /b 1

if "!mysql_dirpath!"=="" (
    echo [错误] 未配置 dirpath，请在界面「环境部署 / Local_MySql_配置」填写并点确认
    exit /b 1
)

if "!mysql_port!"=="" (
    echo [错误] 未配置 port，请在界面「环境部署 / Local_MySql_配置」填写并点确认
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
    set "line=!line:CFG_BASE_DIR=%ini_base_dir%!"
    set "line=!line:CFG_DATA_DIR=%ini_data_dir%!"
    set "line=!line:CFG_PORT=%mysql_port%!"
    echo !line!>>"!output_ini!"
)

if not exist "!output_ini!" (
    echo [错误] 生成 my.ini 失败，请检查 dirpath 是否存在: !mysql_dirpath!
    exit /b 1
)

echo "[配置服务] 完成"
exit /b 0

::::------------------------------------------------------------
::::注册服务：注册 Windows 服务 -> 启动服务 -> 设 root 密码 -> 按配置建业务用户
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

::::MySQL 5.7 不会自动建 data 目录，没有它服务一启动就 Aborting，所以先初始化
::::--defaults-file 必须是第一个参数，放后面会被忽略，datadir 就对不上了
if not exist "!mysql_dirpath!\data" (
    echo "[注册服务] 初始化数据目录 !mysql_dirpath!\data"
    mysqld --defaults-file="!mysql_dirpath!\my.ini" --initialize-insecure
    if errorlevel 1 (
        echo [错误] 初始化数据目录失败，请检查 dirpath 是否可写
        popd
        exit /b 1
    )
)

echo "[注册服务] 启动服务 !mysql_service_name!"
net start "!mysql_service_name!"
if errorlevel 1 (
    echo [错误] 启动服务失败，请查看上面的服务报错信息
    popd
    exit /b 1
)

::::探测 root 密码状态：空密码能连上说明还没设过（刚初始化或之前没设成功），先设成 root_password
mysql -h 127.0.0.1 -P !mysql_port! -u root -e "SELECT 1" >nul 2>nul
if not errorlevel 1 (
    echo "[注册服务] 设置 root 密码"
    mysql -h 127.0.0.1 -P !mysql_port! -u root -e "ALTER USER 'root'@'localhost' IDENTIFIED BY '!root_password!';"
    if errorlevel 1 (
        echo [错误] 设置 root 密码失败
        popd
        exit /b 1
    )
)

::::按配置的 username / password 建一个权限等同 root 的用户
echo "[注册服务] 添加用户 !mysql_username!"
::::主机位 %% 在批处理里代表一个百分号，写 '%' 会被当成变量名吞掉
mysql -h 127.0.0.1 -P !mysql_port! -u root -p!root_password! -e "CREATE USER IF NOT EXISTS '!mysql_username!'@'%%' IDENTIFIED BY '!mysql_password!'; GRANT ALL PRIVILEGES ON *.* TO '!mysql_username!'@'%%' WITH GRANT OPTION; FLUSH PRIVILEGES;"
if errorlevel 1 (
    echo [错误] 添加用户失败，请确认 root 密码是 !root_password!
    popd
    exit /b 1
)

popd

echo "[注册服务] 完成，服务已启动，root 密码 !root_password!，业务用户 !mysql_username!"
exit /b 0

::::------------------------------------------------------------
::::移除服务：先停服务再卸载，不动 dirpath 下的数据和 my.ini
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

::::正在运行就先停掉，net stop 会等到服务真正停下才返回
sc query "!mysql_service_name!" | findstr /i "RUNNING" >nul 2>nul
if not errorlevel 1 (
    echo "[移除服务] 停止服务 !mysql_service_name!"
    net stop "!mysql_service_name!"
    if errorlevel 1 (
        echo [错误] 停止服务失败，请手动确认服务状态后再移除
        exit /b 1
    )
)

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
::::注册/移除/启动/停止服务都需要管理员权限：没有就提权重启自己
::::  提权副本带 elevated 参数，跑完在 :finish 里 pause，日志看得见
::::  当前窗口随后 exit 直接关掉，不留一个只能看到「正在提权」的空窗口
::::------------------------------------------------------------
:ensure_admin
net session >nul 2>&1
if not errorlevel 1 exit /b 0

echo "[!action!] 需要管理员权限，正在以管理员身份重新启动..."
powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -ArgumentList '!action!','elevated' -Verb RunAs"
if errorlevel 1 (
    echo [错误] 获取管理员权限失败
    exit /b 1
)

::::exit 而不是 exit /b：跳过外层的 pause，把本窗口直接关掉，只留提权窗口
exit
