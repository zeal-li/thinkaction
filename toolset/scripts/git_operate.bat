@echo off
setlocal enabledelayedexpansion

:::统一切到 936 代码页，保证本脚本（GBK）里的中文正常显示
chcp 936 >nul

:::用法: git_operate.bat <clone|pull|push> <检出根目录> <项目名> [Github地址]
:::clone 用 Github 地址检出到「检出根目录\项目名」，pull / push 只用「检出根目录\项目名」
set "action=%~1"
set "checkout_root_dir=%~2"
set "project_name=%~3"
set "github_url=%~4"

if "!action!"=="" (
    call :print_usage
    exit /b 1
)

if "!checkout_root_dir!"=="" (
    echo [错误] 未指定检出根目录
    exit /b 1
)

if "!project_name!"=="" (
    echo [错误] 未指定项目名
    exit /b 1
)

set "project_dir=!checkout_root_dir!\!project_name!"

if /i "!action!"=="clone" (
    call :clone_project
    goto :finish
)

if /i "!action!"=="pull" (
    call :pull_project
    goto :finish
)

if /i "!action!"=="push" (
    call :push_project
    goto :finish
)

echo [错误] 未定义的功能: !action!
call :print_usage
exit /b 1

:finish
exit /b !errorlevel!

:::------------------------------------------------------------
:::用法
:::------------------------------------------------------------
:print_usage
echo 用法: %~nx0 ^<功能名^> ^<检出根目录^> ^<项目名^> [Github地址]
echo 可用功能:
echo   clone - 在检出根目录下检出项目（目录名取项目名），需要 Github 地址
echo   pull  - 对「检出根目录\项目名」执行 git pull
echo   push  - 对「检出根目录\项目名」执行 git push
exit /b 0

:::------------------------------------------------------------
:::clone：检出到「检出根目录\项目名」
:::------------------------------------------------------------
:clone_project
if "!github_url!"=="" (
    echo [错误] 未配置 Github 地址，请先在卡片里填写
    exit /b 1
)

echo "[clone] git clone !github_url! !project_dir!"
git clone "!github_url!" "!project_dir!"
exit /b !errorlevel!

:::------------------------------------------------------------
:::pull：进入项目目录执行 git pull
:::------------------------------------------------------------
:pull_project
call :enter_project_dir
if errorlevel 1 exit /b 1

echo "[pull] git pull"
git pull
set "git_result=!errorlevel!"
popd
exit /b !git_result!

:::------------------------------------------------------------
:::push：进入项目目录执行 git push
:::------------------------------------------------------------
:push_project
call :enter_project_dir
if errorlevel 1 exit /b 1

echo "[push] git push"
git push
set "git_result=!errorlevel!"
popd
exit /b !git_result!

:::------------------------------------------------------------
:::进入项目目录，进不去就报错退出（否则后面的 git 命令会跑在错误的目录下）
:::------------------------------------------------------------
:enter_project_dir
pushd "!project_dir!"
if errorlevel 1 (
    echo [错误] 项目目录不存在: !project_dir!
    exit /b 1
)
exit /b 0
