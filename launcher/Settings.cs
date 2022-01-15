// LIZENZBEDINGUNGEN - Seanox Software Solutions ist ein Open-Source-Projekt, im
// Folgenden Seanox Software Solutions oder kurz Seanox genannt.
// Diese Software unterliegt der Version 2 der Apache License.
//
// Virtual Environment Launcher
// Program starter for the virtual environment.
// Copyright (C) 2022 Seanox Software Solutions
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not
// use this file except in compliance with the License. You may obtain a copy of
// the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
// License for the specific language governing permissions and limitations under
// the License.

using System;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace Seanox.Platform.Launcher
{
    [XmlRoot("settings")]
    public class Settings
    {
        internal const string HOT_KEY = "1:27";

        internal const int GRID_SIZE    = 99;
        internal const int GRID_GAP     = 25;
        internal const int GRID_PADDING = 10;
        
        internal const int OPACITY = 90;
        
        internal const string BACKGROUND_COLOR = "#0F0A07";
        internal const string FOREGROUND_COLOR = "#999999";
        internal const string BORDER_COLOR     = "#FFAA44";
        internal const string HIGHLIGHT_COLOR  = "#FFAA44";
        
        internal const float FONT_SIZE = 9.75f;

        public Settings()
        {
            HotKey = HOT_KEY;
            
            GridSize = GRID_SIZE;
            GridGap = GRID_GAP;
            GridPadding = GRID_PADDING;
            
            Opacity = OPACITY;
            
            BackgroundColor = BACKGROUND_COLOR;
            ForegroundColor = FOREGROUND_COLOR;
            BorderColor = BORDER_COLOR;
            HighlightColor = HIGHLIGHT_COLOR;
            
            FontSize = FONT_SIZE;
            
            Tiles = Array.Empty<Tile>();
        }

        [XmlElement("hotKey")]
        public string HotKey;

        [XmlElement("rasterSize")]
        public int GridSize;
        
        [XmlElement("rasterGap")]
        public int GridGap;
        
        [XmlElement("rasterPadding")]
        public int GridPadding;

        [XmlElement("opacity")]
        public int Opacity;

        [XmlElement("backgroundColor")]
        public string BackgroundColor;
        
        [XmlElement("foregroundColor")]
        public string ForegroundColor;
        
        [XmlElement("borderColor")]
        public string BorderColor;
        
        [XmlElement("highlightColor")]
        public string HighlightColor;
        
        [XmlElement("fontSize")]
        public float FontSize;

        [XmlArray("tiles")]
        [XmlArrayItem("tile", typeof(Tile))]
        public Tile[] Tiles;
        
        public class Tile
        {
            [XmlElement("index")]
            public int Index;

            [XmlElement("title")]
            public string Title;

            [XmlElement("icon")]
            public string Icon;

            internal string IconFile {
                get
                {
                    var iconFile = Icon;
                    if (String.IsNullOrWhiteSpace(Icon))
                        iconFile = Destination;
                    else iconFile = new Regex(":.*$").Replace(iconFile, "");
                    return iconFile != null ? iconFile.Trim() : iconFile;
                }
            }
            
            internal int IconIndex {
                get
                {
                    var iconFile = Icon;
                    if (String.IsNullOrWhiteSpace(Icon))
                        return 0;
                    iconFile = new Regex("^.*:").Replace(iconFile, "");
                    int.TryParse(iconFile, out var iconIndex);
                    return iconIndex;
                }
            }

            [XmlElement("destination")]
            public string Destination;

            [XmlElement("arguments")]
            public string Arguments;

            [XmlElement("workingDirectory")]
            public string WorkingDirectory;
        }
    }
}