#!/usr/bin/env bash
# 生成 Debug 版 exe 到 build\ 目录，免开 VS
set -euo pipefail

cd "$(dirname "$0")"

# Git Bash 直接用 PATH 里的 dotnet；WSL 里退回 Windows 安装路径
if command -v dotnet >/dev/null 2>&1; then
    DOTNET=dotnet
else
    DOTNET="/mnt/c/Program Files/dotnet/dotnet.exe"
fi

rm -rf build
"$DOTNET" build src/ToolSet.csproj -c Debug -o build --nologo

echo
echo "[OK] build/ToolSet.exe"
