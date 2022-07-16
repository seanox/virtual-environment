@ECHO OFF

ECHO Virtual Environment Switch [Version 0.0.0 00000000]
ECHO Copyright (C) 0000 Seanox Software Solutions
ECHO Temporary change of the command line configuration
ECHO.

SETLOCAL ENABLEEXTENSIONS
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

SET PATCHESEXISTS=0
FOR %%f IN (.\*.cmd) DO (
    IF NOT [%%~nxf] == [%~nx0] (
        SET PATCHESEXISTS=1
    )
)
IF [%PATCHESEXISTS%] == [1] (
    ECHO Available Patches:
    FOR %%f IN (.\*.cmd) DO (
        IF NOT [%%~nxf] == [%~nx0] (
            ECHO   %%~nf
        )
    )
) ELSE (
    ECHO no patches available
)

ENDLOCAL
