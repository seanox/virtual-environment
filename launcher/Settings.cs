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
using System.Drawing;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace Seanox.Platform.Launcher
{
    [XmlRoot("settings")]
    public class Settings
    {
        private string _hotKey;
        private string _backgroundColor;
        private string _backgroundImage;
        private string _foregroundColor;
        private string _borderColor;
        private string _highlightColor;

        private const string HOT_KEY = "1:27";

        private const int GRID_SIZE    = 99;
        private const int GRID_GAP     = 25;
        private const int GRID_PADDING = 10;
        
        private const int OPACITY = 90;
        
        private const string BACKGROUND_COLOR = "#000000";
        private const string FOREGROUND_COLOR = "#C8C8C8";
        private const string BORDER_COLOR     = "#424242";
        private const string HIGHLIGHT_COLOR  = "#FAB400";
        
        private static Regex PATTERN_HOT_KEY = new Regex(@"^0*([1248])\s*\+\s*0*(\d{1,3})$");
        private static Regex PATTERN_BYTE    = new Regex("^0*([0-1]?[0-9]?[0-9]?|2[0-4][0-9]|25[0-5])$");
        private static Regex PATTERN_COLOR   = new Regex("^(#(?:[0-9A-F]{3}){1,2})$", RegexOptions.IgnoreCase);
        private static Regex PATTERN_ICON    = new Regex(@"^(.*?)\s*(?::\s*([^:]*)){0,1}$");

        public Settings()
        {
            _hotKey = HOT_KEY;
            
            GridSize    = GRID_SIZE;
            GridGap     = GRID_GAP;
            GridPadding = GRID_PADDING;
            
            Opacity = OPACITY;
            
            _backgroundColor = BACKGROUND_COLOR;
            _foregroundColor = FOREGROUND_COLOR;
            _borderColor     = BORDER_COLOR;
            _highlightColor  = HIGHLIGHT_COLOR;
            
            FontSize = SystemFonts.DefaultFont.Size;
            
            Tiles = Array.Empty<Tile>();
        }

        private static string NormalizeValue(Regex pattern, string value, string standard)
        {
            if (String.IsNullOrWhiteSpace(value)
                    || !pattern.IsMatch(value))
                value = standard;
            return Environment.ExpandEnvironmentVariables(value ?? "").Trim();
        }
        
        private static string NormalizeValue(string value)
        {
            return Environment.ExpandEnvironmentVariables(value ?? "").Trim();
        }

        [XmlElement("hotKey")]
        public string HotKey
        {
            get => _hotKey;
            set => _hotKey = NormalizeValue(PATTERN_HOT_KEY, value, HOT_KEY);
        }

        [XmlElement("gridSize")]
        public int GridSize;
        
        [XmlElement("gridGap")]
        public int GridGap;
        
        [XmlElement("gridPadding")]
        public int GridPadding;

        [XmlElement("opacity")]
        public int Opacity;

        [XmlElement("backgroundColor")]
        public string BackgroundColor
        {
            get => _backgroundColor;
            set => _backgroundColor = NormalizeValue(PATTERN_COLOR, value, BACKGROUND_COLOR);
        }

        [XmlElement("backgroundImage")]
        public string BackgroundImage
        {
            get => _backgroundImage;
            set => _backgroundImage = NormalizeValue(value);
        }

        [XmlElement("foregroundColor")]
        public string ForegroundColor
        {
            get => _foregroundColor;
            set => _foregroundColor = NormalizeValue(PATTERN_COLOR, value, FOREGROUND_COLOR);
        }

        [XmlElement("borderColor")]
        public string BorderColor
        {
            get => _borderColor;
            set => _borderColor = NormalizeValue(PATTERN_COLOR, value, BORDER_COLOR);
        }
        
        [XmlElement("highlightColor")]
        public string HighlightColor
        {
            get => _highlightColor;
            set => _highlightColor = NormalizeValue(PATTERN_COLOR, value, HIGHLIGHT_COLOR);
        }
        
        [XmlElement("fontSize")]
        public float FontSize;

        [XmlArray("tiles")]
        [XmlArrayItem("tile", typeof(Tile))]
        public Tile[] Tiles;
        
        public class Tile
        {
            private string _title;
            private string _icon;
            private string _destination;
            private string _arguments;
            private string _workingDirectory;
            
            [XmlElement("index")]
            public int Index;
            
            [XmlElement("title")]
            public string Title
            {
                get => _title;
                set => _title = NormalizeValue(value);
            }
            
            [XmlElement("icon")]
            public string Icon
            {
                get => _icon;
                set => _icon = NormalizeValue(value);
            }

            internal string IconFile
            {
                get
                {
                    var iconFile = Icon;
                    if (String.IsNullOrWhiteSpace(Icon))
                        iconFile = Destination;
                    else iconFile = iconFile = PATTERN_ICON.Replace(iconFile, "$1");
                    return iconFile != null ? iconFile.Trim() : null;
                }
            }
            
            internal int IconIndex
            {
                get
                {
                    var iconFile = Icon;
                    if (String.IsNullOrWhiteSpace(Icon))
                        return 0;
                    iconFile = PATTERN_ICON.Replace(iconFile, "$2");
                    int.TryParse(iconFile, out var iconIndex);
                    return iconIndex;
                }
            }

            [XmlElement("destination")]
            public string Destination
            {
                get => _destination;
                set => _destination = NormalizeValue(value);
            }

            [XmlElement("arguments")]
            public string Arguments
            {
                get => _arguments;
                set => _arguments = NormalizeValue(value);
            }

            [XmlElement("workingDirectory")]
            public string WorkingDirectory
            {
                get => _workingDirectory;
                set => _workingDirectory = NormalizeValue(value);
            }
        }
    }
}