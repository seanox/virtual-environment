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

using System.Drawing;
using System.Xml.Serialization;

namespace launcher
{
    [XmlRoot("settings")]
    public class Settings
    {
        [XmlElement("opacity")]
        public int Opacity {get; set;}

        [XmlElement("hotKey")]
        public string HotKey {get; set;}

        [XmlArray("tiles")]
        [XmlArrayItem("tile", typeof(Tile))]
        public Tile[] Tiles {get; set;}
        
        public class Tile
        {
            [XmlElement("index")]
            public int Index {get; set;}

            [XmlElement("title")]
            public string Title {get; set;}

            [XmlElement("icon")]
            public string Icon {get; set;}

            [XmlElement("destination")]
            public string Destination {get; set;}

            [XmlElement("arguments")]
            public string Arguments {get; set;}

            [XmlElement("workingDirectory")]
            public string WorkingDirectory {get; set;}
            
            internal bool Active {get; set;}
            
            internal Point Position {get; set;}
        }
    }
}