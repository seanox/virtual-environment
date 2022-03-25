# Launcher
A portable program launcher specially developed for the Seanox Virtual
Environment with a full-screen tile optics that can also be used standalone and
independently.

<img height="450" src="Extras/animation.gif"/>

Focus is fast keyboard usage -- open a program with three keys.  
Through the global hotkey even when the launcher is not visible.


# Features
- Full screen overlay user interface  
  The full screen overlay is displayed on the primary screen.
- User interface in tile optics  
  There are 40 freely configurable tiles available.
- Global HotKey combination  
  A global HotKey combination is used to show and hide the user interface.
- HotKey for tiles and programs  
  Quick access via the keyboard, optimized for the used keyboard layout.  
  Support for international keyboard layouts.
- Automatic settings update  
  Changes in the settings are used immediately.
- Settings supports environment variables  
  Environment variables can be used in text-based values.
- Visual style per settings (themes support)  
  The user interface supports the configuration of colors, opacity, background
  color and image, and the appearance of the grid.
- No icons and functions in the taskbar or system tray  
  The program is optimized for the virtual environment. So that the shell is
  not lost, the program is always present in the background and cannot be
  simply terminated, but the quit can be configured as a function via a tile.  
- Portable application without installation


# System Requirement
- Windows 10 or higher
- .NET 4.7.x or higher


# Download
The launcher is [part of the virtual environment](https://github.com/seanox/virtual-environment/tree/main/platform/Program%20Portables/Launcher)
but can also be downloaded and used separately.

https://github.com/seanox/virtual-environment/tree/main/launcher/Releases


# Settings
For configuration the file `launcher.xml` (depending on the application name)
in the working directory is used. If this file does not exist or is incorrect,
the launcher aborts the start with an error message.

Example of a configuration file:

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
    <opacity>90</opacity>

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

    <!-- Font size in the tiles. -->
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
        </tile>
        ...
    </tiles>
</settings>
```


# Changes (Change Log)
## 1.0.0 20220325 (summary of the current version)  
NT: Release is available  

[Read more](https://raw.githubusercontent.com/seanox/virtual-environment-creator/master/launcher/CHANGES)
