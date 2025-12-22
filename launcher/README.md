# Launcher
A portable program launcher designed for the Seanox Virtual Environment. It
provides a full-screen, tile-based interface and can also be used independently.

<img src="Resources/animation.gif"/>

The launcher is optimized for keyboard-based operation. Programs can be started
with a small number of key presses, including via a global hotkey when the
launcher is not visible.

# Features
- __Full-screen overlay interface__  
  Displayed on the primary screen.
- __Tile-based user interface__  
  Up to 40 configurable tiles.
- __Global hotkey__  
  Shows or hides the launcher.
- __Hotkeys for tiles and programs__  
  Keyboard-optimized access, including support for international keyboard
  layouts.
- __Automatic settings reload__  
  Configuration changes are applied immediately.
- __Environment variable support__  
  Text-based configuration values can reference environment variables.
- __Configurable visual appearance__  
  Colors, opacity, background image, and grid appearance can be adjusted.
- __No taskbar or system tray integration__  
  The launcher runs in the background without adding icons to the shell.  
  Termination can be configured via a tile.
- __Portable application__  
  No installation required.

# System Requirement
- Microsoft Windows 10 or higher
- Microsoft .NET 4.8.x or higher (for runtime)
- [Microsoft .NET 4.8.x Developer Pack or higher](
      https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48) (for development only)

# Download
The launcher is [part of the virtual environment](
    https://github.com/seanox/virtual-environment/tree/main/platform/Program%20Portables/Launcher)
but can also be downloaded and used separately.

https://github.com/seanox/virtual-environment/releases

# Settings
Configuration is stored in a file named `launcher.xml` (based on the application
name) in the working directory. If the file is missing or invalid, the launcher
terminates with an error message.

Example configuration:

```xml
<?xml version="1.0" encoding="utf-8"?>
<settings>
    <!--
        General Information
        - Text values (not numeric / boolean) supported environment variables
        - Invalid or unset values use a defined default value
    -->

    <!-- Hot-key to show the launcher (decimal modifiers:virtual key code) -->
    <hotKey>1 + 27</hotKey>

    <!-- Cell size in pixels (height and width) -->
    <gridSize>99</gridSize>
    <!-- Distance between cells in pixels -->
    <gridGap>25</gridGap>
    <!-- Distance from the border in the cells in pixels -->
    <gridPadding>10</gridPadding>
    <!-- Radius for rounding the corners from the grid in pixels -->
    <gridCornerRadius>4</gridCornerRadius>

    <!-- Opacity of background in percents (100 - 0) -->
    <opacity>95</opacity>

    <!--
        BackgroundImage is optionally a path relative to the working directory.
        The image is centered and scaled to fit. If the image does not exist or
        it uses an unsupported format, the image will be ignored.
    -->
    <backgroundImage></backgroundImage>

    <!-- General background color -->
    <backgroundColor>#000000</backgroundColor>
    <!-- General foreground color -->
    <foregroundColor>#C8C8C8</foregroundColor>
    <!-- Border color from grid -->
    <borderColor>#313131</borderColor>
    <!-- Color for highlighting (e.g. navigation) -->
    <highlightColor>#FAB400</highlightColor>

    <!-- Font size in the tiles -->
    <fontSize>9.75</fontSize>

    <tiles>
        <tile>
            <!-- Number of the tile (1 - 40) -->
            <index>40</index>
            <!-- Application name and tile title -->
            <title>Detach</title>
            <!--
                Icon file, optionally with index separated by colon.
                Supports many image formats and icons in exe, dll, ... files.
            -->
            <icon>%WINDIR%\system32\shell32.dll:27</icon>
            <!-- Path of the application file or `exit` to exit the launcher -->
            <destination>%PLATFORM_APP%</destination>
            <!-- Arguments for the program start -->
            <arguments>%HOMEDRIVE% detach</arguments>
            <!-- Working directory in which the application is executed -->
            <workingDirectory>%PLATFORM_HOME%</workingDirectory>
            <!-- 
                Optionally, environment variables can be added for the action of
                the tile. These also support the syntax of other environment
                variables.
            -->
            <!--
            <environment>
                <variable>
                    <name>USERPROFILE</name>
                    <value>%APPSSETTINGS%</value>
                </variable>
                <variable>
                    <name>PATH</name>
                    <value>%APPSPATH%\example;%PATH%</value>
                </variable>
            </environment>
            -->          
        </tile>
        ...
    </tiles>
</settings>
```

# Changes 
## 1.2.0 20250701  
BF: Launcher: Correction when using custom scaling  
CR: Platform: Optimization and corrections  
CR: Launcher: Optimization and corrections  
CR: Launcher: Added additional environment variables for the tile action  

[Read more](https://raw.githubusercontent.com/seanox/virtual-environment/master/launcher/CHANGES)
