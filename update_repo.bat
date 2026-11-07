@echo off
cd /d %~dp0
set "BRANCH=Config_separation"

echo Checking for local changes...
git diff --quiet
if errorlevel 1 goto dirty

git diff --cached --quiet
if errorlevel 1 goto dirty

for /f "delims=" %%i in ('git ls-files --others --exclude-standard') do goto dirty

echo Downloading the latest changes from GitHub...
git fetch origin %BRANCH%
if errorlevel 1 exit /b 1

git checkout %BRANCH%
if errorlevel 1 git checkout -b %BRANCH% --track origin/%BRANCH%
if errorlevel 1 exit /b 1

git pull --ff-only origin %BRANCH%
if errorlevel 1 exit /b 1

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
cd build
start "" Discord_Bot.exe

echo Done!
exit /b 0

:dirty
echo ERROR: Local changes or untracked files found. Commit or stash them before updating.
exit /b 1