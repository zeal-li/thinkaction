@echo off
setlocal
cd /d "%~dp0"

echo ============================
echo   Git Push
echo ============================
echo.

where git >nul 2>nul
if errorlevel 1 (
    echo [ERROR] git not found in PATH.
    pause
    exit /b 1
)

rem 本地仓库未配置 user.name / user.email 时补上（只写入当前仓库配置）
git config user.name >nul 2>&1
if errorlevel 1 (
    echo Setting local git user.name...
    git config user.name "zeal-li"
)
git config user.email >nul 2>&1
if errorlevel 1 (
    echo Setting local git user.email...
    git config user.email "64367160@qq.com"
)

for /f "delims=" %%b in ('git rev-parse --abbrev-ref HEAD 2^>nul') do set "BRANCH=%%b"
echo Branch: %BRANCH%
echo.
git status

echo.
set "confirm="
set /p "confirm=Proceed with commit & push? (Y/N): "
if /i not "%confirm%"=="Y" (
    echo Cancelled, only Y is accepted.
    pause
    exit /b 0
)

echo.
echo Adding changes...
git add -A .

rem git diff --cached --quiet：退出码 0 表示没有暂存内容，1 表示有改动
git diff --cached --quiet
if not errorlevel 1 (
    echo [ERROR] Nothing to commit, no staged changes.
    pause
    exit /b 1
)

echo.
echo Files to be committed:
git diff --cached --name-only

echo.
set "msg="
set /p "msg=Enter commit message: "
if "%msg%"=="" (
    echo [ERROR] Commit message cannot be empty.
    pause
    exit /b 1
)
if not "%msg%"=="%msg:"=%" (
    echo [ERROR] Commit message cannot contain double quotes.
    pause
    exit /b 1
)

echo.
echo Committing...
git commit -m "%msg%"
if errorlevel 1 (
    echo [ERROR] Commit failed, see the message above.
    pause
    exit /b 1
)

echo.
echo Pushing to remote...
git rev-parse --abbrev-ref --symbolic-full-name "@{u}" >nul 2>&1
if errorlevel 1 (
    echo No upstream branch configured, pushing with -u origin HEAD...
    git push -u origin HEAD
) else (
    git push
)
if errorlevel 1 (
    echo [ERROR] Push failed. Check network / credentials, or run "git pull --rebase" first.
    pause
    exit /b 1
)

echo.
echo Done!
pause
