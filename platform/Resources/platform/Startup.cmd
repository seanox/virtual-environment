@ECHO OFF

REM Environment will be terminated
REM ----
IF "%1" == "exit" (

    REM All the commands required to unmount the virtual environment are
    REM inserted here. In particular, stop all temporary services and remove
    REM them from the system. The environment itself will then gently or, if
    REM unsuccessful, hard terminate all programs that were started from the
    REM virtual drive and are still running.

REM ---- Launcher
start "Virtual Environment Launcher" /min "%APPSPATH%\Launcher\launcherExit.cmd"

    REM Placeholder for automatic module integration
    REM INSERT DETACH

    GOTO:EOF
)





REM Environment will be configured
REM ----

SET OS_APPDATA=%APPDATA%
SET OS_LOCALAPPDATA=%LOCALAPPDATA%
SET OS_HOMEPATH=%HOMEPATH%
SET OS_PATH=%PATH%
SET OS_PUBLIC=%PUBLIC%
SET OS_TEMP=%TEMP%
SET OS_TMP=%TMP%
SET OS_USERPROFILE=%USERPROFILE%

REM Following environment variables are set at startup:
REM - VT_PLATFORM_NAME  Name of the environment, derived from the virtual disk
REM - VT_PLATFORM_HOME  Home directory of the virtual disk  
REM - VT_PLATFORM_DISK  Path of the virtual disc
REM - VT_PLATFORM_APP   Path from the environment startup program 
REM - VT_HOMEDRIVE      Root directory of the started virtual environment

SET VT_HOMEPATH=\Documents
SET VT_USERPROFILE=%VT_HOMEDRIVE%\Settings
SET VT_APPDATA=%VT_USERPROFILE%\Roaming
SET VT_LOCALAPPDATA=%VT_USERPROFILE%\Local
SET VT_PUBLIC=%VT_HOMEDRIVE%\Documents\Public
SET VT_APPSPATH=%VT_HOMEDRIVE%\Program Portables
SET VT_TEMP=%VT_HOMEDRIVE%\Temp

REM Relevant system variables are rewritten
REM Be careful with changing USERPROFILE, it can cause unexpected results. 
SET APPDATA=%VT_APPDATA%
SET APPSPATH=%VT_APPSPATH%
SET APPSSETTINGS=%VT_HOMEDRIVE%\Settings
SET HOME=%APPSSETTINGS%
SET HOMEDRIVE=%VT_HOMEDRIVE%
SET HOMEPATH=%VT_HOMEPATH%
SET LOCALAPPDATA=%VT_LOCALAPPDATA%
SET PUBLIC=%VT_PUBLIC%
SET TEMP=%VT_TEMP%
SET TMP=%VT_TMP%

SET PLATFORM_NAME=%VT_PLATFORM_NAME%
SET PLATFORM_HOME=%VT_PLATFORM_HOME%
SET PLATFORM_DISK=%VT_PLATFORM_DISK%
SET PLATFORM_APP=%VT_PLATFORM_APP%

REM Further environment variables are inserted here.
REM Please do not set program starts here, that will come later.

REM Changes the platform name to uppercase, e.g. as a prefix for services.
REM for /f "usebackq delims=" %%I in (`powershell "\"%PLATFORM_NAME%\".toUpper()"`) do set "PLATFORM_NAME=%%~I"

REM ---- Extensions
SET PATH=%APPSPATH%\Extensions;%PATH%

REM Placeholder for automatic module integration
REM INSERT COMMONS





REM Environment will be prepared
REM ----

REM Programs and service are configured and initialized here, but not started.

REM Placeholder for automatic module integration
REM INSERT ATTACH





REM Environment will be started
REM ----

REM Programs and services are finally started here.
REM It is important that the programs start in such a way that the startup
REM script does not block, because the startup program of the virtual
REM environment waits for the end of the startup script.

REM Basically, a launcher or start menu should be started so that the programs
REM can later use the virtual environment and the required environment
REM variables. This is also important so that detaching works properly.

REM ---- Launcher (to keep the environment alive)
pushd "%APPSPATH%\Launcher"
start launcher.exe
popd

REM Placeholder for automatic module integration
REM INSERT STARTUP
