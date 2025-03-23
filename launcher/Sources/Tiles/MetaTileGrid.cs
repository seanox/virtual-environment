// LICENSE TERMS - Seanox Software Solutions is an open source project,
// hereinafter referred to as Seanox Software Solutions or Seanox for short.
// This software is subject to version 2 of the Apache License.
//
// Virtual Environment Launcher
// Program starter for the virtual environment.
// Copyright (C) 2025 Seanox Software Solutions
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not
// use this file except in compliance with the License. You may obtain a copy of
// the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
// License for the specific language governing permissions and limitations under
// the License.

using System;
using System.Windows.Forms;

namespace VirtualEnvironment.Launcher.Tiles
{
    internal class MetaTileGrid
    {
        internal readonly int Columns = 10;
        internal readonly int Rows    = 4;
        
        internal readonly int Size;
        internal readonly int Gap;
        internal readonly int Padding;
        internal readonly int Radius;
        
        internal readonly float FontSize;

        internal int Count  => Columns * Rows;
        internal int Height => ((Size + Gap) * Rows) - Gap;
        internal int Width  => ((Size + Gap) * Columns) - Gap;
        
        private MetaTileGrid(Settings settings)
        {
            var scaleFactor = 1f;
            if (settings.AutoScale)
            {
                var scaleMinOffset = settings.GridGap * 6;
                var scaleMinHeight = ((settings.GridSize + settings.GridGap) * Rows) - Gap;
                var scaleMinWidth = ((settings.GridSize + settings.GridGap) * Columns) - Gap;

                var scaleMaxOffset = (settings.GridSize + settings.GridGap) * 2; 
                var scaleMaxHeight = scaleMinHeight + scaleMaxOffset;
                var scaleMaxWidth = scaleMinWidth + scaleMaxOffset;

                var screenBounds = Screen.PrimaryScreen.Bounds;
                if (screenBounds.Height >= scaleMaxHeight
                        && screenBounds.Width >= scaleMaxWidth)
                {
                    var scaleFactorHeight = (float)screenBounds.Height / scaleMaxHeight;
                    var scaleFactorWidth = (float)screenBounds.Width / scaleMaxWidth;
                    scaleFactor = Math.Min(scaleFactorHeight, scaleFactorWidth);
                }
                else 
                {
                    var scaleFactorHeight = (float)screenBounds.Height / (scaleMinHeight + scaleMinOffset);
                    var scaleFactorWidth = (float)screenBounds.Width / (scaleMinWidth + scaleMinOffset);
                    scaleFactor = Math.Min(scaleFactorHeight, scaleFactorWidth);
                }
            }

            Func<double, double> ScaleNumber = (number) =>
            {
                var scaleNumberValue = Math.Floor(number * scaleFactor);
                if (number % 2 == 0
                        && scaleNumberValue % 2 != 0)
                    return scaleNumberValue + 1;
                if (number % 2 != 0
                        && scaleNumberValue % 2 == 0)
                    return scaleNumberValue + 1;
                return scaleNumberValue;
            };

            Size    = (int)ScaleNumber(settings.GridSize);
            Gap     = (int)ScaleNumber(settings.GridGap);
            Padding = (int)ScaleNumber(settings.GridPadding);
                
            Radius = Math.Min(Math.Max(settings.GridCornerRadius, 0), Size /2);

            FontSize = settings.FontSize * scaleFactor;
        }

        internal static MetaTileGrid Create(Settings settings)
        {
            return new MetaTileGrid(settings);
        }
    }
}