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
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;

namespace Seanox.Platform.Launcher.Tiles
{
    internal class MetaTileMap
    {
        private readonly MetaTile[] _metaTiles;
        
        internal readonly Image Image;
        internal readonly Image PassiveBorderImage;
        internal readonly Image ActiveBorderImage;

        private MetaTileMap(Settings settings, MetaTile[] metaTiles)
        {
            var metaTileGrid = MetaTileGrid.Create(settings);
            
            _metaTiles = metaTiles;
            Image = new Bitmap(metaTileGrid.Width, metaTileGrid.Height, PixelFormat.Format32bppPArgb);
            using (var imageGraphics = Graphics.FromImage(Image))
                foreach (var metaTile in metaTiles.Where(metaTile => metaTile.Image != null))
                    imageGraphics.DrawImage(metaTile.Image, metaTile.Location.Left, metaTile.Location.Top);
            
            var borderColor = ColorTranslator.FromHtml(settings.BorderColor);
            var highlightColor = ColorTranslator.FromHtml(settings.HighlightColor);

            PassiveBorderImage = new Bitmap(metaTileGrid.Size, metaTileGrid.Size, PixelFormat.Format32bppPArgb);
            using (var passiveBorderImageGraphics = Graphics.FromImage(PassiveBorderImage))
            Utilities.Graphics.DrawRectangleRounded(passiveBorderImageGraphics, new Pen(new SolidBrush(borderColor)),
                    new Rectangle(0, 0, metaTileGrid.Size - 1, metaTileGrid.Size - 1), 1);

            ActiveBorderImage = new Bitmap(metaTileGrid.Size, metaTileGrid.Size, PixelFormat.Format32bppPArgb);
            using (var activeBorderImageGraphics = Graphics.FromImage(ActiveBorderImage))
                Utilities.Graphics.DrawRectangleRounded(activeBorderImageGraphics, new Pen(new SolidBrush(highlightColor)),
                        new Rectangle(0, 0, metaTileGrid.Size - 1, metaTileGrid.Size - 1), 1);
        }

        internal static MetaTileMap Create(Settings settings, MetaTile[] metaTiles)
        {
            return new MetaTileMap(settings, metaTiles);
        }

        internal MetaTile Locate(string symbol)
        {
            return Array.Find(_metaTiles, metaTile => metaTile.Symbol == symbol);
        }
        
        internal MetaTile Locate(Point point)
        {
            return Array.Find(_metaTiles, metaTile =>
                    point.X >= metaTile.Location.Left
                          && point.X <= metaTile.Location.Right 
                          && point.Y >= metaTile.Location.Top
                          && point.Y <= metaTile.Location.Bottom);
        }
    }
}