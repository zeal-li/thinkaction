@echo off
setlocal enabledelayedexpansion

::统一切到 936 代码页，保证本脚本（GBK）里的中文正常显示
chcp 936 >nul

::用法: export_protocol.bat <前端|后端> <proto协议目录> <输出目录>
::proto协议目录 = 项目里的 common\sproto，里面带 lua.exe、generate_sproto.lua 和 proto 子目录
set "target=%~1"
set "proto_dir=%~2"
set "output_dir=%~3"

if "!target!"=="" (
    call :print_usage
    exit /b 1
)

if "!proto_dir!"=="" (
    echo [错误] 未配置 proto 协议目录
    exit /b 1
)

if "!output_dir!"=="" (
    echo [错误] 未配置生成目录
    exit /b 1
)

::只用 common\sproto 目录下的 lua.exe，不用环境变量里的 lua
if not exist "!proto_dir!\lua.exe" (
    echo [错误] 未找到 !proto_dir!\lua.exe
    exit /b 1
)

::lua.exe 是动态链接版，启动需要同目录的 lua55.dll（可从 server\skynet\3rd\lua\win64 复制）
if not exist "!proto_dir!\lua55.dll" (
    echo [错误] 未找到 !proto_dir!\lua55.dll
    echo   lua.exe 依赖 lua55.dll 才能启动，请把它复制到 proto 协议目录
    exit /b 1
)

if not exist "!proto_dir!\generate_sproto.lua" (
    echo [错误] 未找到 !proto_dir!\generate_sproto.lua
    exit /b 1
)

::按导出目标取生成类型：前端 -> client，后端 -> server
if /i "!target!"=="前端" (
    set "gen_type=client"
) else if /i "!target!"=="后端" (
    set "gen_type=server"
) else (
    echo [错误] 未定义的导出目标: !target!
    call :print_usage
    exit /b 1
)

::去掉目录末尾的斜杠，否则传参时会把引号转义掉
if "!output_dir:~-1!"=="\" set "output_dir=!output_dir:~0,-1!"
if "!output_dir:~-1!"=="/" set "output_dir=!output_dir:~0,-1!"

echo 导出目标: !target!
echo 协议目录: !proto_dir!
echo 生成目录: !output_dir!
echo.

::generate_sproto_cfg.lua 里的协议路径是相对 common\sproto 的，必须先切到该目录再执行
pushd "!proto_dir!"
lua.exe generate_sproto.lua !gen_type! "!output_dir!"
set "gen_code=!errorlevel!"
popd

if not "!gen_code!"=="0" (
    echo.
    echo [错误] 协议生成失败，退出码 !gen_code!
    exit /b !gen_code!
)

echo.
echo 生成完成
exit /b 0

::------------------------------------------------------------
::用法
::------------------------------------------------------------
:print_usage
echo 用法: %~nx0 ^<前端^|后端^> ^<proto协议目录^> ^<输出目录^>
echo   前端 / 后端: 生成前端协议或后端协议
echo   proto协议目录: 项目里的 common\sproto，用其中的 lua.exe 执行 generate_sproto.lua
echo   输出目录: 生成的 .sproto 文件存放目录
exit /b 1
