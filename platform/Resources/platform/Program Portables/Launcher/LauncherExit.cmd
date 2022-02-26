REM Workaround, so that the icon in the system tray is also removed,
REM taskkill is started in its own batch window/process.  
taskkill /t /im Launcher.exe
exit
