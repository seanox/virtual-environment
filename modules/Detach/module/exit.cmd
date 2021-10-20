@ECHO OFF

CALL #[release.drive.letter]:\Startup.cmd exit

IF NOT DEFINED VT_LETTER (
    IF NOT DEFINED VT_SCRIPT (
        GOTO:EOF          
    )
)

CALL "%VT_SCRIPT%" exit
