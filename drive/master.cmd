@ECHO OFF
  
    IF NOT "%1" == "exit" CALL :INIT

    IF NOT DEFINED VT_LETTER GOTO:EOF

    net session >nul 2>&1
    IF NOT %errorLevel% == 0 (
        mshta "javascript:new ActiveXObject('WScript.Shell').Popup('This program must be executed as administrator!', 10, 'Warning', 48); close();"
        GOTO:EOF
    )
    GOTO MAIN

:INIT
    SET VT_LETTER=B
    SET VT_NAME=%~n0
    SET VT_SCRIPT=%cd%\%~n0%~x0
    SET VT_HOME=%cd%
    SET VT_OUTPUT=%VT_HOME%\%VT_NAME%.log
    GOTO:EOF

:MAIN
    IF EXIST "%VT_OUTPUT%" DEL "%VT_OUTPUT%"
    SET DISK=%VT_HOME%\%VT_NAME%.vhd
    IF EXIST "%VT_HOME%\%VT_NAME%.vhdx" SET DISK=%VT_HOME%\%VT_NAME%.vhdx
  
    IF NOT EXIST "%DISK%" (
        mshta "javascript:new ActiveXObject('WScript.Shell').Popup('No corresponding virtual disk found!', 10, 'Warning', 48); close();"
        GOTO:EOF
    )
  
    CALL :SCRIPTS
 
    IF EXIST %VT_LETTER%:\ (
        DiskPart /s %VT_NAME%.detach >> "%VT_OUTPUT%"
    )
  
    IF "%1" == "detach" (
        CALL:CLEAN
        GOTO:EOF
    )

    IF "%1" == "exit" (
        CALL:CLEAN
        SET "VT_LETTER="
        SET "VT_NAME="
        SET "VT_SCRIPT="
        SET "VT_HOME="
        SET "VT_OUTPUT="
        GOTO:EOF
    )

    IF "%1" == "compact" (
        DiskPart /s %VT_NAME%.compact >> "%VT_OUTPUT%"
        CALL:CLEAN
        GOTO:EOF
    )

    DiskPart /s %VT_NAME%.attach >> "%VT_OUTPUT%"
  
    DiskPart /s %VT_NAME%.list >> "%VT_OUTPUT%"
  
    SET number=X
    FOR /f "tokens=*" %%a IN ('type "%VT_OUTPUT%"') DO (
        CALL :LOOKUP %%a
    )
  
    IF "%number%" == "X" (
        DiskPart /s %VT_NAME%.detach >> "%VT_OUTPUT%"
        CALL :CLEAN
        mshta "javascript:new ActiveXObject('WScript.Shell').Popup('Volume for \'%VT_NAME%\' was not found in:\r\n\tDiskPart /s %VT_NAME%.list\r\nPlease check the virtual disk, it must exist and use the name of the environment!', 10, 'Warning', 48); close();"
        GOTO:EOF
    )
  
    ECHO. >> "%VT_OUTPUT%"
    ECHO Volume number %number% found for '%VT_NAME%'. >> "%VT_OUTPUT%"
  
    ECHO select volume %number% >> %VT_NAME%.assign  
    ECHO assign letter=%VT_LETTER% >> %VT_NAME%.assign 
    ECHO exit >> %VT_NAME%.assign
  
    DiskPart /s %VT_NAME%.assign >> "%VT_OUTPUT%"
  
    CALL :CLEAN
  
    ping -n 3 127.0.0.1 > NUL
    CALL %VT_LETTER%:\Startup.exe

:LOOKUP
    IF /I NOT "%1" == "Volume" GOTO:EOF
    IF /I NOT "%3" == "%VT_NAME%" (
        IF /I NOT "%4" == "%VT_NAME%" GOTO:EOF
    )

    SET "var="&FOR /f "delims=0123456789" %%i IN ("%2") DO SET var=%%i
    IF DEFINED var GOTO:EOF
    SET number=%2

    GOTO:EOF

:SCRIPTS
    CALL :CLEAN

    ECHO select vdisk file="%DISK%" >> %VT_NAME%.attach
    ECHO attach vdisk >> %VT_NAME%.attach
    ECHO exit >> %VT_NAME%.attach 

    ECHO select vdisk file="%DISK%" >> %VT_NAME%.detach
    ECHO detach vdisk >> %VT_NAME%.detach
    ECHO exit >> %VT_NAME%.detach

    ECHO list volume >> %VT_NAME%.list
    ECHO exit >> %VT_NAME%.list

    ECHO select vdisk file="%DISK%" >> %VT_NAME%.compact
    ECHO attach vdisk readonly >> %VT_NAME%.compact
    ECHO compact vdisk >> %VT_NAME%.compact
    ECHO detach vdisk >> %VT_NAME%.compact
    ECHO exit >> %VT_NAME%.compact

    GOTO:EOF
 
:CLEAN
    IF EXIST %VT_NAME%.attach DEL %VT_NAME%.attach
    IF EXIST %VT_NAME%.detach DEL %VT_NAME%.detach
    IF EXIST %VT_NAME%.list DEL %VT_NAME%.list
    IF EXIST %VT_NAME%.assign DEL %VT_NAME%.assign
    IF EXIST %VT_NAME%.compact DEL %VT_NAME%.compact
    GOTO:EOF
