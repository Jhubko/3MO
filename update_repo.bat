@echo off
cd /d %~dp0

echo Stashing local changes...
git reset --hard
git clean -fd

echo Downloading the latest changes from GitHub...
git pull origin main

echo Deleting old build folder...
if exist build rmdir /s /q build

echo Building the project...
dotnet publish Discord_Bot2.0.sln -c Release -r win-x64 --self-contained true -o build

echo Building complete!
echo The EXE file is located in the folder: build

set CONFIG_PATH=C:\bot\config.json

echo Copying config file from %CONFIG_PATH%...
if exist "%CONFIG_PATH%" (
    copy /Y "%CONFIG_PATH%" build\
    echo Config file copied successfully!
) else (
    echo ERROR: Config file not found at %CONFIG_PATH%!
)

echo Starting Discord_Bot.exe...
start "" build\Discord_Bot.exe

echo Done!
exit
