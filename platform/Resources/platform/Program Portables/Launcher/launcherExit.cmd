REM Workaround and part of the concept. In the launcher an exit function can be
REM mapped, but the virtual environment is kept alive by the launcher and
REM therefore it does not exist as a default and so the launcher should be
REM terminated via taskkill on detach.
taskkill /t /im launcher.exe
exit
