@echo off
setlocal enabledelayedexpansion

::保存当前目录
set cur_work_path=%cd%
::参数
::ssh_commond ssh命令
set ssh_type=%1
set ssh_commond=%2

::切换到脚本目录
cd %~dp0

::执行ssh命令
if "!ssh_type!" == "centos" (
    call :ssh_opt_centos %ssh_commond%
) else if "!ssh_type!" == "wsl" (
    call :ssh_opt_wsl %ssh_commond%
) else (
    echo 未定义SSH操作类型:%ssh_type%
    exit /b 1
)

::还原当前目录
cd %cur_work_path%
exit /b %errorlevel%

::本地WSL系统SSH接口配置检测
:ssh_opt_wsl_check_config
    ::检查SSH user
    if not defined ssh_local_user (
        echo 未配置SSH ssh_user,请前往配置%config_path%
        exit /b 1
    )

    ::检查SSH host
    if not defined ssh_local_host (
        echo 未配置SSH ssh_host,请前往配置%config_path%
        exit /b 2
    )

    ::检查SSH port
    if not defined ssh_local_port (
        echo 未配置SSH ssh_port,请前往配置%config_path%
        exit /b 3
    )

    ::检查SSH key file
    if not defined ssh_local_user_key (
        echo 未配置SSH ssh_user_key,请前往配置%config_path%
        exit /b 4
    )

    exit /b 0
    goto:EOF

::远程centos7服务器SSH接口配置检测
:ssh_opt_centos_check_config
    ::检查SSH user
    if not defined ssh_remote_user (
        echo 未配置SSH ssh_user,请前往配置%config_path%
        exit /b 1
    )

    ::检查SSH host
    if not defined ssh_remote_host (
        echo 未配置SSH ssh_host,请前往配置%config_path%
        exit /b 2
    )

    ::检查SSH port
    if not defined ssh_remote_port (
        echo 未配置SSH ssh_port,请前往配置%config_path%
        exit /b 3
    )

    ::检查SSH key file
    if not defined ssh_remote_user_key (
        echo 未配置SSH ssh_user_key,请前往配置%config_path%
        exit /b 4
    )

    exit /b 0
    goto:EOF

::本地WSL系统SSH接口 %1[ssh_commond]
:ssh_opt_wsl
    ::SSH参数检测
    call :ssh_opt_wsl_check_config
    if %errorlevel% neq 0 (
        exit /b 0
    )

    set wsl_ssh_commond=%1
    echo "[开始执行ssh命令] ssh -i %ssh_local_user_key% %ssh_local_user%@%ssh_local_host% -p %ssh_local_port% %wsl_ssh_commond%"
    ssh -i %ssh_local_user_key% %ssh_local_user%@%ssh_local_host% -p %ssh_local_port% %wsl_ssh_commond%
    set opt_result=%errorlevel%
    chcp 936 > nul
    if %opt_result% neq 0 (
        exit /b %opt_result%
    )
    goto:EOF

::远程centos7服务器SSH接口 %1[ssh_commond]
:ssh_opt_centos
    ::SSH参数检测
    call :ssh_opt_centos_check_config
    if %errorlevel% neq 0 (
        exit /b 0
    )

    set centos_ssh_commond=%1
    echo "[开始执行ssh命令] ssh -i %ssh_remote_user_key% %ssh_remote_user%@%ssh_remote_host% -p %ssh_remote_port% %centos_ssh_commond%"
    ssh -i %ssh_remote_user_key% %ssh_remote_user%@%ssh_remote_host% -p %ssh_remote_port% %centos_ssh_commond%
    set opt_result=%errorlevel%
    chcp 936 > nul
    if %opt_result% neq 0 (
        exit /b %opt_result%
    )
    goto:EOF

