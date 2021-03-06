@ECHO OFF

CD /D #[environment.directory]



REM Verification that the correct drive is available
REM ----

SETLOCAL ENABLEDELAYEDEXPANSION
SET STARTUP=%~df0
IF NOT "%STARTUP:~0,2%" == "#[environment.drive]:" (
    mshta "javascript:new ActiveXObject('WScript.Shell').Popup('A virtual drive B: is required for this environment.', 10, 'Warning', 48 +4096); close();"
    ENDLOCAL
    GOTO:EOF
)
ENDLOCAL

startup -s
startup -m waring sdsdfssfsdf
startup -a


REM Environment will be terminated
REM ----

IF "%1" == "exit" (
    CALL cmd /c start #[environment.resources.directory]\message.hta^
        The virtual environment will be detached.^
        \r\nPrograms and services are terminated.

    REM Script when exiting the environment
    REM ---- DETACH
    IF EXIST #[environment.resources.directory]\detach.cmd CALL #[environment.resources.directory]\detach.cmd

    SETLOCAL ENABLEDELAYEDEXPANSION

    SET DRIVE=%~dp0
    SET DRIVE=!DRIVE:~0,3!

    REM Gentle termination of all applications
    SET ACCEPTED=0
    FOR /F "tokens=1,2 delims==" %%A IN ('WMIC path win32_process Get ExecutablePath^,ProcessId /VALUE') DO (
        SET KEY=%%A
        SET VALUE=%%B
        IF NOT "!VALUE!" == "" (
            SET VALUE=!VALUE:~0,-1!
            IF "!KEY!" == "ProcessId" (
                IF "!ACCEPTED!" == "1" (
                    CALL taskkill /t /pid !VALUE!
                )
            )
            SET ACCEPTED=0
            IF "!KEY!" == "ExecutablePath" (
                IF "!VALUE:~0,3!" == "!DRIVE!" (
                    SET ACCEPTED=1
                )
            )
        )
    )

    ping -n 3 127.0.0.1 > NUL

    REM Hard termination of all applications
    SET ACCEPTED=0
    FOR /F "tokens=1,2 delims==" %%A IN ('WMIC path win32_process Get ExecutablePath^,ProcessId /VALUE') DO (
        SET KEY=%%A
        SET VALUE=%%B
        IF NOT "!VALUE!" == "" (
            SET VALUE=!VALUE:~0,-1!
            IF "!KEY!" == "ProcessId" (
                IF "!ACCEPTED!" == "1" (
                    CALL taskkill /f /t /pid !VALUE!
                )
            )
            SET ACCEPTED=0
            IF "!KEY!" == "ExecutablePath" (
                IF "!VALUE:~0,3!" == "!DRIVE!" (
                    SET ACCEPTED=1
                )
            )
        )
    )

    ENDLOCAL

    ping -n 3 127.0.0.1 > NUL
    taskkill /f /t /im mshta.exe

    GOTO:EOF
)



REM Environment will be configured and started
REM ----

CALL cmd /c start #[environment.resources.directory]\message.hta^
    The virtual environment starts.^
    \r\nPrograms and services are established.

SET OS_TEMP=%TEMP%
SET OS_APPDATA=%APPDATA%
SET OS_LOCALAPPDATA=%LOCALAPPDATA%
SET OS_HOMEPATH=%HOMEPATH%
SET OS_USERPROFILE=%USERPROFILE%
SET OS_PUBLIC=%PUBLIC%
SET OS_PATH=%PATH%

SET VT_ROOT=#[environment.drive]
SET VT_DATABASE=%VT_ROOT%\#[environment.database]
SET VT_DOCUMENTS=%VT_ROOT%\#[environment.documents]
SET VT_HOMEPATH=%VT_DOCUMENTS%\Local
SET VT_USERPROFILE=%VT_DOCUMENTS%\Profile
SET VT_PUBLIC=%VT_DOCUMENTS%\Public
SET VT_PROGRAMS=%VT_ROOT%\#[environment.programs]
SET VT_RESOURCES=%ROOT%\#[environment.resources]
SET VT_TEMP=%ROOT%\#[environment.temp]

FOR /F "tokens=1,2 delims==" %%a IN ('WMIC LogicalDisk WHERE DeviceId^="B:" Get VolumeName /Value') DO (
    IF NOT "%%b" == "" SET VT_VOLUME_NAME=%%b
)

SET ROOT=%VT_ROOT%
SET TEMP=%VT_TEMP%
SET TMP=%TEMP%
SET APPDATA=%VT_HOMEPATH%\Roaming
SET LOCALAPPDATA=%VT_HOMEPATH%\Local
SET HOME=%VT_HOMEPATH%
SET PUBLIC=%VT_PUBLIC%

SET DATABASE=%VT_DATABASE%
SET DOCUMENTS=%VT_DOCUMENTS%
SET PROGRAMS=%VT_PROGRAMS%
SET RESOURCES=%VT_RESOURCES%
SET TEMP=%TEMP%

REM Script with common commands
REM ---- COMMONS
IF EXIST %RESOURCES%\commons.cmd CALL %RESOURCES%\commons.cmd

REM Script for initialization, will be executed after common
REM ---- ATTACH
IF EXIST %RESOURCES%\attach.cmd CALL %RESOURCES%\attach.cmd

ping -n 3 127.0.0.1 > NUL
taskkill /f /t /im mshta.exe

REM Script after initialization for program starts
REM ---- STARTUP
IF EXIST %RESOURCES%\startup.cmd CALL %RESOURCES%\startup.cmd
