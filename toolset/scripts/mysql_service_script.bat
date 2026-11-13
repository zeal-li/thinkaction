@echo off
setlocal enabledelayedexpansion


::获取管理员权限
net session >nul 2>&1
if %errorlevel% neq 0 (
    powershell -Command "Start-Process '%~f0' -ArgumentList '%*' -Verb RunAs"
    exit /b
)

net session >nul 2>&1
if %errorlevel% neq 0 (
    echo 获取管理员权限失败
    pause
    exit /b
) else (
    echo 获取管理员权限成功
)

::保存当前目录
set cur_work_path=%cd%

::获取参数
set mysql_name=%1
set service_command=%2
set script_path=%~dp0

::切换到脚本目录
cd /d %script_path%

if "%service_command%" == "register" (
    call :service_register
) else if "%service_command%" == "remove" (
    call :service_remove
) else if "%service_command%" == "start" (
    call :service_start
) else if "%service_command%" == "stop" (
    call :service_stop
) else (
    echo 未定义MySQL Service Command:%service_command%
    exit /b 1
)

::还原当前目录
cd %cur_work_path%
exit /b 0

::Mysql服务注册
:service_register
    set mysql_dir_path=!script_path!!mysql_name!
    set input_file=!script_path!my.ini
    set output_file=!mysql_dir_path!\my.ini

    if exist %output_file% del /f/s/q %output_file%

    ::生成my.ini文件
    set ini_path=!mysql_dir_path:\=/!
    for /f "tokens=*" %%a in (%input_file%) do (
        set line=%%a
        set line=!line:BASE_DIR=%ini_path%!
        set line=!line:DATA_DIR=%ini_path%/data!
        echo !line!>>%output_file%
    )

    cd ./!mysql_name!/bin
    mysqld --install MySQL_ikun_01 --defaults-file="!output_file!"
    mysqld --initialize-insecure
    goto:EOF

::Mysql服务移除
:service_remove
    set mysql_data_dir=!script_path!!mysql_name!\data\
    if exist !mysql_data_dir! rd /s/q !mysql_data_dir!

    cd ./!mysql_name!/bin/
    mysqld --remove MySQL_ikun_01
    goto:EOF

::Mysql服务启动
:service_start
    net start MySQL_ikun_01
    goto:EOF

::Mysql服务停止
:service_stop
    net stop MySQL_ikun_01
    goto:EOF
