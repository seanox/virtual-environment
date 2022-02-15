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
using System.Windows.Forms;

namespace Seanox.Platform.Launcher.Tiles
{
    internal class MetaTileScreen
    {
        private readonly MetaTile[] _metaTiles;
        private readonly Screen _screen;
        private readonly Settings _settings;

        private readonly Image _activeBorderImage;
        private readonly Image _passiveBorderImage;

        private Point _selection;
        
        private MetaTileScreen(Screen screen, Settings settings, params MetaTile[] metaTiles)
        {
            _metaTiles = metaTiles;
            _screen = screen;
            _settings = settings;

            var metaTileGrid = MetaTileGrid.Create(settings);
            var borderColor = ColorTranslator.FromHtml(settings.BorderColor);
            var highlightColor = ColorTranslator.FromHtml(settings.HighlightColor);

            _passiveBorderImage = new Bitmap(metaTileGrid.Size, metaTileGrid.Size, PixelFormat.Format32bppPArgb);
            using (var passiveBorderImageGraphics = Graphics.FromImage(_passiveBorderImage))
            Utilities.Graphics.DrawRectangleRounded(passiveBorderImageGraphics, new Pen(new SolidBrush(borderColor)),
                    new Rectangle(0, 0, metaTileGrid.Size - 1, metaTileGrid.Size - 1), 1);

            _activeBorderImage = new Bitmap(metaTileGrid.Size, metaTileGrid.Size, PixelFormat.Format32bppPArgb);
            using (var activeBorderImageGraphics = Graphics.FromImage(_activeBorderImage))
                Utilities.Graphics.DrawRectangleRounded(activeBorderImageGraphics, new Pen(new SolidBrush(highlightColor)),
                        new Rectangle(0, 0, metaTileGrid.Size - 1, metaTileGrid.Size - 1), 1);
        }

        internal static MetaTileScreen Create(Screen screen, Settings settings, params MetaTile[] metaTiles)
        {
            return new MetaTileScreen(screen, settings, metaTiles);
        }

        internal MetaTile Locate(string symbol)
        {
            return Array.Find(_metaTiles,
                metaTile => "" + metaTile.Symbol   == symbol);
        }
        
        internal MetaTile Locate(Point point)
        {
            return Array.Find(_metaTiles, metaTile =>
                    point.X >= metaTile.Location.Left
                            && point.X <= metaTile.Location.Right 
                            && point.Y >= metaTile.Location.Top
                            && point.Y <= metaTile.Location.Bottom);
        }

        internal void Select(Graphics graphics, MetaTile metaTile)
        {
            if (metaTile == null)
                return;

            if (_selection != null
                    && _selection.X != 0
                    && _selection.Y != 0)
                graphics.DrawImage(_passiveBorderImage, _selection);
            _selection = new Point(metaTile.Location.X, metaTile.Location.Y);
            graphics.DrawImage(_activeBorderImage, _selection);
        }

        private static Rectangle ImageCenter(Rectangle rectangle, Image image)
        {
            return new Rectangle((rectangle.Width - image.Width) / 2,
                (rectangle.Height - image.Height) / 2, image.Width, image.Height);
        }

        internal void Draw(Graphics graphics)
        {
            var screenRectangle = _screen.Bounds;

            if (!String.IsNullOrWhiteSpace(_settings.BackgroundImage))
                using (var backgroundImage = Utilities.Graphics.ImageOf(_settings.BackgroundImage))
                    if (backgroundImage != null)
                        graphics.DrawImage(backgroundImage, ImageCenter(screenRectangle, backgroundImage));

            foreach (var metaTile in _metaTiles)
                metaTile.Draw(graphics);
        }
    }
}