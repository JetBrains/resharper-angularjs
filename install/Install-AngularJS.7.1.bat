@echo off
setlocal enableextensions

set VERSION=7.1
set PRODUCT=ReSharper
set BASEDIR=JetBrains\%PRODUCT%\v%VERSION%
set PLUGIN=AngularJS

set INSTALL_SOURCEDIR=%~dp0\%PLUGIN%.%VERSION%

REM LOCALAPPDATA is only supported since Windows Vista
if "%LOCALAPPDATA%"=="" set LOCALAPPDATA=%USERPROFILE%\Local Settings\Application Data
if not exist "%LOCALAPPDATA%" (
    echo Unable to find local application data directory
    echo %LOCALAPPDATA%
    echo Please check the LOCALAPPDATA environment variable and try again
    goto :end
)

set PER_USER_PLUGINDIR=%LOCALAPPDATA%\%BASEDIR%\plugins\%PLUGIN%

if exist "%PER_USER_PLUGINDIR%" goto do_clean
mkdir "%PER_USER_PLUGINDIR%"

:do_clean
del /q %PER_USER_PLUGINDIR%\*.* 2> NUL

:do_copy
echo Copying files...
copy /y "%INSTALL_SOURCEDIR%\*.dll" "%PER_USER_PLUGINDIR%"
copy /y "%INSTALL_SOURCEDIR%\*.pdb" "%PER_USER_PLUGINDIR%" 2> NUL
copy /y "%INSTALL_SOURCEDIR%\*.dotSettings" "%PER_USER_PLUGINDIR%" 2> NUL

echo.

REM See https://github.com/citizenmatt/UnblockZoneIdentifier
echo Unblocking downloaded files...
pushd "%PER_USER_PLUGINDIR%"
for /r %%i in (*) do "%~dp0\UnblockZoneIdentifier" "%%i"
popd

:end
pause
