@ECHO OFF

SET WORKDIR=%cd%
SET HOMEDIR=%~dp0
SET MACRO=%1
SET MACRO=%MACRO:"=%.cmd
SET MACROSDIR=%HOMEDIR%\macros

IF NOT EXIST "%MACROSDIR%" MKDIR "%MACROSDIR%"

IF NOT [%1] == [] (
    IF EXIST "%MACROSDIR%\%MACRO%" (
         PUSHD "%MACROSDIR%" & "%MACRO%" & POPD
         ENDLOCAL   
         GOTO:EOF
    )
)

SETLOCAL ENABLEEXTENSIONS

ECHO Virtual Environment Macros [Version 1.0.0 20230125]
ECHO Copyright (C) 2023 Seanox Software Solutions
ECHO Temporary change of the command line configuration
ECHO.

ECHO usage: %~nx0 [macro]
ECHO.

SET MACROSEXISTS=0
FOR %%f IN ("%MACROSDIR%\*.cmd") DO (
    IF NOT [%%~nxf] == [%~nx0] (
        SET MACROSEXISTS=1
    )
)
IF [%MACROSEXISTS%] == [1] (
    ECHO available macros:
    FOR %%f IN ("%MACROSDIR%\*.cmd") DO (
        IF NOT [%%~nxf] == [%~nx0] (
            ECHO   %%~nf
        )
    )
) ELSE (
    ECHO no macros available
)

ENDLOCAL
