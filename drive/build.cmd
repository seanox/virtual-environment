@ECHO OFF

    IF EXIST "#[workspace.drive.file]" del "#[workspace.drive.file]"

    ECHO DiskPart /s diskpart.create
    DiskPart /s diskpart.create > NUL
    IF NOT %errorLevel% == 0 (
        ECHO An unexpected error has occurred
        DiskPart /s diskpart.detach > NUL
        EXIT /B 1
    )

    ECHO DiskPart /s diskpart.attach
    DiskPart /s diskpart.attach > NUL
    IF NOT %errorLevel% == 0 (
        ECHO An unexpected error has occurred
        DiskPart /s diskpart.detach > NUL
        EXIT /B 1
    )

    ECHO list volume > diskpart.list
    ECHO exit >> diskpart.list

    ECHO DiskPart /s diskpart.list
    DiskPart /s diskpart.list > diskpart.volumes

    SET number=X
    FOR /f "tokens=*" %%a IN ('type "diskpart.volumes"') DO (
        CALL :LOOKUP %%a
    )

    IF "%number%" == "X" (
        DiskPart /s diskpart.detach > NUL
        ECHO Volume for '#[release.name]' was not found in:
        ECHO      diskpart.list
        ECHO Please check the virtual disk, it must exist and use the name of the environment!
        EXIT /B 1
    )

    ECHO select volume %number% > diskpart.assign
    ECHO assign letter=#[workspace.drive.letter] >> diskpart.assign
    ECHO exit >> diskpart.assign

    ECHO DiskPart /s diskpart.assign
    DiskPart /s diskpart.assign > NUL
    IF NOT %errorLevel% == 0 (
        ECHO An unexpected error has occurred
        DiskPart /s diskpart.detach > NUL
        EXIT /B 1
    )

    GOTO:EOF

:LOOKUP
    IF /I NOT "%1" == "Volume" GOTO:EOF
    IF /I NOT "%3" == "#[release.name]" (
        IF /I NOT "%4" == "#[release.name]" GOTO:EOF
    )

    SET "var="&FOR /f "delims=0123456789" %%i IN ("%2") DO SET var=%%i
    IF DEFINED var GOTO:EOF
    SET number=%2

    GOTO:EOF
