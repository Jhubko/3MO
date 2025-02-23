@echo off
cd /d %~dp0
echo Downloading the latest changes from GitHub...
git pull origin main

echo Building a project...
dotnet publish Discord_Bot2.0.sln -c Release -r win-x64 --self-contained true -o build

echo Building complete!
echo The EXE file is located in the folder: build
pause