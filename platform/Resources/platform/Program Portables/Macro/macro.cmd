@ECHO OFF

SET WORKDIR=%~dp0
SET MACRO=%1
SET MACRO=%MACRO:"=%.cmd
SET MACROSDIR=%WORKDIR%\macros

IF NOT EXIST "%MACROSDIR%" MKDIR "%MACROSDIR%"

IF NOT [%1] == [] (
    IF EXIST "%MACROSDIR%\%MACRO%" (
         PUSHD "%MACROSDIR%" & "%MACRO%" & POPD
         ENDLOCAL   
         GOTO:EOF
    )
)

SETLOCAL ENABLEEXTENSIONS

ECHO Virtual Environment Macro [Version 1.0.0 20221208]
ECHO Copyright (C) 2022 Seanox Software Solutions
ECHO Simplification of repeated processes
ECHO.

ECHO Usage: %~nx0 [macro]
ECHO.

SET MACROSEXISTS=0
FOR %%f IN ("%MACROSDIR%\*.cmd") DO (
    IF NOT [%%~nxf] == [%~nx0] (
        SET MACROSEXISTS=1
    )
)
IF [%MACROSEXISTS%] == [1] (
    ECHO Available Macros:
    FOR %%f IN ("%MACROSDIR%\*.cmd") DO (
        IF NOT [%%~nxf] == [%~nx0] (
            ECHO   %%~nf
        )
    )
) ELSE (
    ECHO no macros available
)

ENDLOCAL
