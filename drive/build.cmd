@ECHO OFF

    IF EXIST "#[workspace.drive.file]" del "#[workspace.drive.file]"

    DiskPart /s diskpart.create
    IF NOT %errorLevel% == 0 (
        ECHO DiskPart /s diskpart.create ^(failed^)
        DiskPart /s diskpart.detach
        EXIT /B 1
    )

    DiskPart /s diskpart.attach
    IF NOT %errorLevel% == 0 (
        ECHO DiskPart /s diskpart.attach ^(failed^)
        DiskPart /s diskpart.detach
        EXIT /B 1
    )

    ECHO list volume > diskpart.list
    ECHO exit >> diskpart.list

    DiskPart /s diskpart.list > diskpart.volumes

    SET number=X
    FOR /f "tokens=*" %%a IN ('type "diskpart.volumes"') DO (
        CALL :LOOKUP %%a
    )

    IF "%number%" == "X" (
        DiskPart /s diskpart.detach
        ECHO Volume for '#[release.name]' was not found in:
        ECHO      diskpart.list
        ECHO Please check the virtual disk, it must exist and use the name of the environment!
        EXIT /B 1
    )

    ECHO Volume number %number% found for '#[release.name]'.

    ECHO select volume %number% > diskpart.assign
    ECHO assign letter=#[workspace.drive.letter] >> diskpart.assign
    ECHO exit >> diskpart.assign

    DiskPart /s diskpart.assign
    IF NOT %errorLevel% == 0 (
        ECHO DiskPart /s diskpart.assign ^(failed^)
        DiskPart /s diskpart.detach
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
