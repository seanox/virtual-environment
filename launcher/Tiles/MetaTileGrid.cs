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

namespace Seanox.Platform.Launcher.Tiles
{
    internal class MetaTileGrid
    {
        internal readonly int Columns = 10;
        internal readonly int Rows    = 4;
        
        internal readonly int Size;
        internal readonly int Gap;
        internal readonly int Padding;
        internal readonly int Radius;

        internal int Count  => Columns * Rows;
        internal int Height => ((Size + Gap) * Rows) - Gap;
        internal int Width  => ((Size + Gap) * Columns) - Gap;
        
        private readonly Settings _settings;

        private MetaTileGrid(Settings settings)
        {
            _settings = settings;
            
            Size    = _settings.GridSize;
            Gap     = _settings.GridGap;
            Padding = _settings.GridPadding;
            
            Radius = Math.Min(Math.Max(_settings.GridCornerRadius, 0), Size /2);
        }

        internal static MetaTileGrid Create(Settings settings)
        {
            return new MetaTileGrid(settings);
        }
    }
}