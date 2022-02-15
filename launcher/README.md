# Launcher

TODO:

## Settings XML

TODO:

```xml
<?xml version="1.0" encoding="utf-8"?>
<settings>
    <!-- 
        System-wide hot-key to show the launcher (decimal modifiers:virtual key code)
        https://docs.microsoft.com/en-us/windows/win32/inputdev/wm-hotkey#parameters
        https://docs.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
        
        Proven standards:
        - Release: Win + Esc
        - Development: Alt + Esc
        
        Default value: 8 + 27 
    -->
    <hotKey>1 + 27</hotKey>

    <!-- Cell size in pixels (height and width) -->
    <gridSize>99</gridSize>
    <!-- Distance between cells in pixels -->
    <gridGap>25</gridGap>
    <!-- Distance from the border in the cells in pixels -->
    <gridPadding>10</gridPadding>

    <!-- 
        Opacity of background in percents (100 - 0)
        Less than 50 makes no sense, because then the UI is difficult to see.
        
        Default value: 90
    -->
    <opacity>90</opacity>

    <!--
        BackgroundImage is optionally a path relative to the working directory.
        The image is centered and scaled to fit.
    -->
    <backgroundImage></backgroundImage>

    <!--
        Coloring
        
            backgroundColor
        General background color
        Default value: #000000

            foregroundColor
        General foreground color
        Default value: #C8C8C8

            borderColor
        Border color from grid
        Default value: #424242
        
            highlightColor 
        Color for highlighting (e.g. navigation)
        Default value: #FAB400
    -->
    
    <!-- Dark Coloring (default) -->
    <backgroundColor>#000000</backgroundColor>
    <foregroundColor>#C8C8C8</foregroundColor>
    <borderColor>#424242</borderColor>
    <highlightColor>#FAB400</highlightColor>

    <!--
        Font size in the tiles.
        
        Default value: Size of the system font
    -->
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
