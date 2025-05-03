@ECHO OFF

REM Environment termination
REM ----
IF "%1" == "exit" (

    REM All the commands required to unmount the virtual environment are
    REM inserted here. In particular, stop all temporary services and remove
    REM them from the system. The environment itself will then gently or, if
    REM unsuccessful, hard terminate all programs that were started from the
    REM virtual drive and are still running.

REM ---- Launcher

    REM Part of the concept: The environment is based on a command line with
    REM customized standard environment variables for Windows and the
    REM applications. This command line is kept alive by the launcher, as it can
    REM start the programs on the basis of its command line and thus on the
    REM basis of the environment variables available there. The launcher
    REM therefore has no implemented function for terminating and must be
    REM terminated hard via taskkill on detach, which the platform does itself.

REM ---- Custom

    REM Placeholder for automatic module integration
    REM INSERT DETACH

    GOTO:EOF
)





REM Environment configuration
REM ----

SET HOST_APPDATA=%APPDATA%
SET HOST_LOCALAPPDATA=%LOCALAPPDATA%
SET HOST_HOMEPATH=%HOMEPATH%
SET HOST_PATH=%PATH%
SET HOST_PUBLIC=%PUBLIC%
SET HOST_TEMP=%TEMP%
SET HOST_TMP=%TMP%
SET HOST_USERPROFILE=%USERPROFILE%

REM Following environment variables are set at startup:
REM - PLATFORM_NAME         Name of the environment, derived from the virtual disk
REM - PLATFORM_HOME         Home directory of the virtual disk  
REM - PLATFORM_DISK         Path of the virtual disc
REM - PLATFORM_APP          Path from the environment startup program 
REM - PLATFORM_HOMEDRIVE    Root directory of the started virtual environment

SET PLATFORM_HOMEPATH=\Documents
SET PLATFORM_USERPROFILE=%PLATFORM_HOMEDRIVE%\Settings
SET PLATFORM_APPDATA=%PLATFORM_USERPROFILE%\Roaming
SET PLATFORM_LOCALAPPDATA=%PLATFORM_USERPROFILE%\Local
SET PLATFORM_PUBLIC=%PLATFORM_HOMEDRIVE%\Documents\Public
SET PLATFORM_APPSPATH=%PLATFORM_HOMEDRIVE%\Programs
SET PLATFORM_TEMP=%PLATFORM_HOMEDRIVE%\Temp

REM Relevant system variables are rewritten
REM Be careful with changing USERPROFILE, it can cause unexpected results. 
SET APPDATA=%PLATFORM_APPDATA%
SET APPSPATH=%PLATFORM_APPSPATH%
SET APPSSETTINGS=%PLATFORM_HOMEDRIVE%\Settings
SET HOME=%APPSSETTINGS%
SET HOMEDRIVE=%PLATFORM_HOMEDRIVE%
SET HOMEPATH=%PLATFORM_HOMEPATH%
SET LOCALAPPDATA=%PLATFORM_LOCALAPPDATA%
SET PUBLIC=%PLATFORM_PUBLIC%
SET TEMP=%PLATFORM_TEMP%
SET TMP=%PLATFORM_TMP%

SET PLATFORM_NAME=%PLATFORM_PLATFORM_NAME%
SET PLATFORM_HOME=%PLATFORM_PLATFORM_HOME%
SET PLATFORM_DISK=%PLATFORM_PLATFORM_DISK%
SET PLATFORM_APP=%PLATFORM_PLATFORM_APP%

REM Further environment variables are inserted here.
REM Please do not set program starts here, that will come later.

REM Changes the platform name to uppercase, e.g. as a prefix for services.
REM for /f "usebackq delims=" %%I in (`powershell "\"%PLATFORM_NAME%\".toUpper()"`) do set "PLATFORM_NAME=%%~I"

REM ---- Platform
SET PATH=%APPSPATH%\Platform;%PATH%

REM ---- Macros
SET PATH=%APPSPATH%\Macros;%PATH%;%APPSPATH%\Macros\macros

REM Placeholder for automatic module integration
REM INSERT COMMONS





REM Environment preparation
REM ----

REM Programs and service are configured and initialized here, but not started.

REM Placeholder for automatic module integration
REM INSERT ATTACH





REM Environment starting
REM ----

REM Programs and services are finally started here.
REM It is important that the programs start in such a way that the startup
REM script does not block, because the startup program of the virtual
REM environment waits for the end of the startup script.

REM Basically, a launcher or start menu should be started so that the programs
REM can later use the virtual environment and the required environment
REM variables. This is also important so that detaching works properly.

REM ---- Launcher (to keep the environment alive)
pushd "%APPSPATH%\Platform"
start launcher.exe
popd

REM Placeholder for automatic module integration
REM INSERT STARTUP
