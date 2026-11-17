@echo off
setlocal enabledelayedexpansion

::统一切到 936 代码页，保证本脚本（GBK）里的中文正常显示
chcp 936 >nul

::前端 / 后端导出格式，导表工具支持的取值：csv、lua、xml、json
set "client_form=lua"
set "server_form=lua"

::用法: export_config.bat <前端|后端> <配置文件全路径或Excel目录> <输出目录> <项目目录>
::配置文件传 Excel 目录时，导表工具导出该目录下的全部配置（与导表工具.bat 里的 all 一致）
::项目目录 = 检出根目录\项目名，导表工具固定在这个目录下的 common\tools\ExportTool\dist\exportmain.exe
set "target=%~1"
set "input_path=%~2"
set "output_dir=%~3"
set "project_dir=%~4"

if "!target!"=="" (
    call :print_usage
    exit /b 1
)

if "!input_path!"=="" (
    echo [错误] 未指定要导出的配置文件
    exit /b 1
)

if "!output_dir!"=="" (
    echo [错误] 未配置导出目录
    exit /b 1
)

if "!project_dir!"=="" (
    echo [错误] 未选择项目，无法定位导表工具
    exit /b 1
)

::按导出目标取对应格式
if /i "!target!"=="前端" (
    set "export_option=1"
    set "export_form=!client_form!"
) else if /i "!target!"=="后端" (
    set "export_option=2"
    set "export_form=!server_form!"
) else (
    echo [错误] 未定义的导出目标: !target!
    call :print_usage
    exit /b 1
)

::导表工具位置：项目目录\common\tools\ExportTool\dist\exportmain.exe
set "export_tool=!project_dir!\common\tools\ExportTool\dist\exportmain.exe"
if not exist "!export_tool!" (
    echo [错误] 未找到导表工具 !export_tool!
    exit /b 1
)

::格式名转成导表工具的参数：csv:0 lua:1 xml:2 json:3
if /i "!export_form!"=="csv" (
    set "form_option=0"
) else if /i "!export_form!"=="lua" (
    set "form_option=1"
) else if /i "!export_form!"=="xml" (
    set "form_option=2"
) else if /i "!export_form!"=="json" (
    set "form_option=3"
) else (
    echo [错误] 未定义的导出格式: !export_form!
    exit /b 1
)

echo 导出目标: !target!
echo 导出目录: !output_dir!
echo 导出格式: !export_form!

::传目录就是导出全部配置，传单个文件只导出该文件，由导表工具自己判断
if exist "!input_path!\" (
    echo 配置文件: !input_path!（导出该目录下的全部配置）
) else (
    echo 配置文件: !input_path!
)
echo.

::直接前台调用导表工具，日志打在本窗口，退出码原样回传
"!export_tool!" !export_option! "!input_path!" !form_option! "!output_dir!"
set "export_code=%errorlevel%"

if not "!export_code!"=="0" (
    echo.
    echo [错误] 导表工具执行失败，退出码 !export_code!
    exit /b !export_code!
)

echo.
echo 导出完成
exit /b 0

::------------------------------------------------------------
::用法
::------------------------------------------------------------
:print_usage
echo 用法: %~nx0 ^<前端^|后端^> ^<配置文件全路径或Excel目录^> ^<输出目录^> ^<项目目录^>
echo   前端 / 后端: 导出前端配置或后端配置
echo   配置文件全路径或Excel目录: 传目录时导出该目录下的全部配置
echo   项目目录: 检出根目录\项目名，导表工具在该目录的 common\tools\ExportTool\dist 下
exit /b 1
