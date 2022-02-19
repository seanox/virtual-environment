# Launcher
TODO:

## Features
- Fullscreen overlay interface  
  The tile interface is displayed as a fullscreen overlay on the primary screen.
- Tile interface
  Interface with 40 freely configurable tiles.
- Global HotKey combination  
  A global HotKey combination is used to show and hide the tile interface.
- HotKey for tiles and programs  
  Quick access via the keyboard, optimized for the used keyboard layout.  
- Automatic settings update  
  Changes in the settings are used immediately, if the settings are incorrect
  the last settings are kept.
- No icons and functions in the taskbar or system tray  
  The program is optimized for the virtual environment. So that the shell is
  not lost, the program is always present in the background and cannot be
  simply terminated.

TODO:

## Settings XML
TODO:

```xml
<?xml version="1.0" encoding="utf-8"?>
<settings>
    <!--
        General Instructions
        - Text values (not numeric / boolean) supported environment variables.
        - Invalid or unset values use a defined default value.  
    -->

    <!-- Hot-key to show the launcher (decimal modifiers:virtual key code) -->
    <hotKey>1 + 27</hotKey>

    <!-- Cell size in pixels (height and width) -->
    <gridSize>99</gridSize>
    <!-- Distance between cells in pixels -->
    <gridGap>25</gridGap>
    <!-- Distance from the border in the cells in pixels -->
    <gridPadding>10</gridPadding>

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
    <borderColor>#646464</borderColor>
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
            <!-- Path of the application file -->
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
