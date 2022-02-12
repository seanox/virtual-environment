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

namespace Seanox.Platform.Launcher.Tiles
{
    internal class MetaTileMap
    {
        private readonly Image _image;
        
        internal Image Image => _image;
        
        private MetaTileMap(MetaTileGrid metaTileGrid, MetaTile[] metaTiles)
        {
        }

        internal static MetaTileMap Create(MetaTileGrid metaTileGrid, MetaTile[] metaTiles)
        {
            return new MetaTileMap(metaTileGrid, metaTiles);
        }

        internal MetaTile Locate(Point point)
        {
            return null;
        }
    }
}