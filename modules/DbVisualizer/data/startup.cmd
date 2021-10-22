@echo off

rem Script to launch DbVisualizer by manually invoking Java

rem Please note that it's *not* recommended to launch DbVisualizer
rem with this script. Instead use the "dbvis.exe" launcher.

set DBVIS_HOME=#[module.environment]
set JAVA_EXEC=#[environment.programs.directory]\Java-11\bin\javaw

set CP=%DBVIS_HOME%\resources
set CP=%CP%;%DBVIS_HOME%\lib\*

set DBVIS_OPT=-Xmx512M
set DBVIS_OPT=%DBVIS_OPT% -XX:MaxPermSize=192M
set DBVIS_OPT=%DBVIS_OPT% -Dsun.locale.formatasdefault=true
set DBVIS_OPT=%DBVIS_OPT% -XX:CompileCommand=exclude,javax/swing/text/GlyphView,getBreakSpot
set DBVIS_OPT=%DBVIS_OPT% -Ddbvis.prefsdir=%DBVIS_HOME%\settings
set DBVIS_OPT=%DBVIS_OPT% -splash:"%DBVIS_HOME%\resources\splash-animated.gif"
set DBVIS_OPT=%DBVIS_OPT% -Ddbvis.home="%DBVIS_HOME%"
set DBVIS_OPT=%DBVIS_OPT% -Duser.home=#[environment.documents.directory]\Local

start %JAVA_EXEC% %DBVIS_OPT% -cp "%CP%" com.onseven.dbvis.DbVisualizerGUI
