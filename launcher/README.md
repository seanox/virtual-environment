# Launcher

TODO:

## Settings XML

TODO:

```xml
<?xml version="1.0" encoding="utf-8"?>
<settings>
    <!-- Opacity of background in percents -->
    <opacity>75</opacity>
    <!-- System-wide hot-key to show the launcher (decimal modifiers:virtual key code) -->
    <!-- https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerhotkey -->
    <!-- https://docs.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes -->
    <hotKey>8:27</hotKey>
    <tiles>
        <tile>
            <!-- Nummber of the tile (1 - 40) -->
            <index>40</index>
            <!-- Application name and tile title -->
            <title>Detach</title>
            <!-- Icon file, optionally with index separated by colon -->
            <iconFile>%WINDIR%\system32\shell32.dll:27</iconFile>
            <!-- Path of the application file -->
            <filename>%PLATFORM_APP%</filename>
            <!-- Arguments for the program start -->
            <arguments>%HOMEDRIVE% detach</arguments>
            <!-- Working directory in which the application is executed --> 
            <workingDirectory>%PLATFORM_HOME%</workingDirectory>
        </tile>
        ...
    </tiles>
</settings>
```
