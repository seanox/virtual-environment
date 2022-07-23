@ECHO OFF

SETLOCAL ENABLEEXTENSIONS
SET WORKDIR=%~dp0
SET SWITCH=%1
SET SWITCH=%SWITCH:"=%.cmd
SET SWITCHESDIR=%WORKDIR%\switches

IF NOT EXIST "%SWITCHESDIR%" MKDIR "%SWITCHESDIR%"

IF NOT [%1] == [] (
    IF EXIST "%SWITCHESDIR%\%SWITCH%" (
         PUSHD "%SWITCHESDIR%" & "%SWITCH%" & POPD
         ENDLOCAL   
         GOTO:EOF
    )
)

ECHO Virtual Environment Switch [Version 1.0.0 20220723]
ECHO Copyright (C) 2022 Seanox Software Solutions
ECHO Temporary change of the command line configuration
ECHO.

ECHO Usage: %~nx0 [switch]
ECHO.

SET SWITCHESEXISTS=0
FOR %%f IN ("%SWITCHESDIR%\*.cmd") DO (
    IF NOT [%%~nxf] == [%~nx0] (
        SET SWITCHESEXISTS=1
    )
)
IF [%SWITCHESEXISTS%] == [1] (
    ECHO Available Switches:
    FOR %%f IN ("%SWITCHESDIR%\*.cmd") DO (
        IF NOT [%%~nxf] == [%~nx0] (
            ECHO   %%~nf
        )
    )
) ELSE (
    ECHO no switches available
)

ENDLOCAL
