@echo off
setlocal enabledelayedexpansion

:::统一切到 936 代码页，保证本脚本（GBK）里的中文正常显示
chcp 936 >nul

:::用法: svn_operate.bat <checkout|update|commit|cleanup|showlog> <检出根目录> <项目名> [Svn地址]
:::checkout 用 Svn 地址检出到「检出根目录\项目名」，其余命令只对「检出根目录\项目名」执行
:::所有命令都通过 TortoiseProc.exe 打开 TortoiseSVN 的界面：参数校验通过后用 start 把界面拉起来，
:::随即 exit 关掉控制台，不等 TortoiseProc 返回（成功还是报错都由 TortoiseSVN 自己提示）；
:::校验没通过才停在窗口里看提示
set "action=%~1"
set "checkout_root_dir=%~2"
set "project_name=%~3"
set "svn_url=%~4"

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

:::优先用 PATH 里的 TortoiseProc.exe（安装时可勾选加入 PATH），没有就按默认安装目录找
set "tortoise_proc=TortoiseProc.exe"
where TortoiseProc.exe >nul 2>nul
if errorlevel 1 (
    set "tortoise_proc=%ProgramFiles%\TortoiseSVN\bin\TortoiseProc.exe"
    if not exist "!tortoise_proc!" (
        echo [错误] 未找到 TortoiseProc.exe，请确认已安装 TortoiseSVN
        exit /b 1
    )
)

set "project_dir=!checkout_root_dir!\!project_name!"

:::用 goto 而不是 call 分发：各功能块末尾的 exit（不带 /b）要作用在本脚本自身，
:::才能结束 cmd / pause 那个外层窗口，否则只是从子过程返回
if /i "!action!"=="checkout" goto :checkout_project
if /i "!action!"=="update" goto :update_project
if /i "!action!"=="commit" goto :commit_project
if /i "!action!"=="cleanup" goto :cleanup_project
if /i "!action!"=="showlog" goto :showlog_project

echo [错误] 未定义的功能: !action!
call :print_usage
exit /b 1

:::------------------------------------------------------------
:::用法
:::------------------------------------------------------------
:print_usage
echo 用法: %~nx0 ^<功能名^> ^<检出根目录^> ^<项目名^> [Svn地址]
echo 可用功能:
echo   checkout - 在检出根目录下检出项目（目录名取项目名），需要 Svn 地址
echo   update   - 对「检出根目录\项目名」执行 svn update
echo   commit   - 对「检出根目录\项目名」执行 svn commit
echo   cleanup  - 对「检出根目录\项目名」执行 svn cleanup
echo   showlog  - 查看「检出根目录\项目名」的 svn 日志
exit /b 0

:::------------------------------------------------------------
:::checkout：检出到「检出根目录\项目名」
:::------------------------------------------------------------
:checkout_project
if "!svn_url!"=="" (
    echo [错误] 未配置 Svn 地址，请先在卡片里填写
    exit /b 1
)

:::start 拉起 TortoiseProc 后立即返回，紧跟着的 exit 把控制台关掉，不等界面关闭
start "" "!tortoise_proc!" /command:checkout /url:"!svn_url!" /path:"!project_dir!" /closeonend:0
exit

:::------------------------------------------------------------
:::update：更新「检出根目录\项目名」
:::------------------------------------------------------------
:update_project
start "" "!tortoise_proc!" /command:update /path:"!project_dir!" /closeonend:0
exit

:::------------------------------------------------------------
:::commit：提交「检出根目录\项目名」
:::------------------------------------------------------------
:commit_project
start "" "!tortoise_proc!" /command:commit /path:"!project_dir!" /closeonend:0
exit

:::------------------------------------------------------------
:::cleanup：清理「检出根目录\项目名」的锁 / 未完成操作
:::------------------------------------------------------------
:cleanup_project
start "" "!tortoise_proc!" /command:cleanup /path:"!project_dir!" /closeonend:0
exit

:::------------------------------------------------------------
:::showlog：查看「检出根目录\项目名」的日志
:::------------------------------------------------------------
:showlog_project
start "" "!tortoise_proc!" /command:log /path:"!project_dir!" /closeonend:0
exit
