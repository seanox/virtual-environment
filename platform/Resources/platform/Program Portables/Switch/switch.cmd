@ECHO OFF

ECHO Virtual Environment Switch [Version 0.0.0 00000000]
ECHO Copyright (C) 0000 Seanox Software Solutions
ECHO Temporary change of the command line configuration
ECHO.

SETLOCAL
SET WORKDIR=%~dp0
SET WORKPATCH=%1
SET WORKPATCH=%WORKPATCH:"=%.cmd

IF NOT [%1] == [] (
    IF EXIST "%WORKDIR%\%WORKPATCH%" (
         PUSHD "%WORKDIR%" & "%WORKPATCH%" & POPD 
         ENDLOCAL   
         GOTO:EOF
    )
)

ECHO Usage: %~nx0 [patch]
ECHO.

REM TODO: list of available patches (names only)
REM       otherwise: no patches available

ENDLOCAL
GOTO :EOF