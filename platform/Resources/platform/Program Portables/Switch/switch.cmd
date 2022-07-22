@ECHO OFF

ECHO Virtual Environment Switch [Version 1.0.0 20220722]
ECHO Copyright (C) 2022 Seanox Software Solutions
ECHO Temporary change of the command line configuration
ECHO.

SETLOCAL ENABLEEXTENSIONS
SET WORKDIR=%~dp0
SET WORKPATCH=%1
SET WORKPATCH=%WORKPATCH:"=%.cmdx
SET WORKTEMP=%WORKDIR%\Temp

PUSHD "%WORKDIR%"
IF NOT [%1] == [] (
    IF EXIST "%WORKDIR%\%WORKPATCH%" (
         IF NOT EXIST "%WORKTEMP%" MKDIR "%WORKTEMP%"
         PUSHD "%WORKTEMP%" & "%1.cmd" & POPD 
         COPY /Y "%WORKPATCH%" "%WORKTEMP%\%1.cmd" > NUL
         CALL "%WORKTEMP%\%1.cmd" 
         ENDLOCAL   
         GOTO:EOF
    )
)

ECHO Usage: %~nx0 [patch]
ECHO.

SET PATCHESEXISTS=0
FOR %%f IN (.\*.cmdx) DO (
    IF NOT [%%~nxf] == [%~nx0] (
        SET PATCHESEXISTS=1
    )
)
IF [%PATCHESEXISTS%] == [1] (
    ECHO Available Patches:
    FOR %%f IN (.\*.cmdx) DO (
        IF NOT [%%~nxf] == [%~nx0] (
            ECHO   %%~nf
        )
    )
) ELSE (
    ECHO no patches available
)

ENDLOCAL
