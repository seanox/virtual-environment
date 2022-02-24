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
// https://www.apache.org/licenses/LICENSE-2.0
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
    internal class MetaTileScreen : IDisposable
    {
        private readonly MetaTile[] _metaTiles;
        private readonly Screen     _screen;
        private readonly Settings   _settings;

        private readonly Image _activeBorderImage;
        private readonly Image _passiveBorderImage;
        private readonly Image _backgroundImage;

        private MetaTile _selection;
        
        private MetaTileScreen(Screen screen, Settings settings, params MetaTile[] metaTiles)
        {
            _metaTiles = metaTiles;
            _screen    = screen;
            _settings  = settings;

            var metaTileGrid   = MetaTileGrid.Create(settings);
            var borderColor    = ColorTranslator.FromHtml(settings.BorderColor);
            var highlightColor = ColorTranslator.FromHtml(settings.HighlightColor);

            _passiveBorderImage = new Bitmap(metaTileGrid.Size, metaTileGrid.Size, PixelFormat.Format32bppPArgb);
            using (var passiveBorderImageGraphics = Graphics.FromImage(_passiveBorderImage))
            Utilities.Graphics.DrawRectangleRounded(passiveBorderImageGraphics, new Pen(new SolidBrush(borderColor)),
                    new Rectangle(0, 0, metaTileGrid.Size - 1, metaTileGrid.Size - 1), metaTileGrid.Radius);

            _activeBorderImage = new Bitmap(metaTileGrid.Size, metaTileGrid.Size, PixelFormat.Format32bppPArgb);
            using (var activeBorderImageGraphics = Graphics.FromImage(_activeBorderImage))
                Utilities.Graphics.DrawRectangleRounded(activeBorderImageGraphics, new Pen(new SolidBrush(highlightColor)),
                        new Rectangle(0, 0, metaTileGrid.Size - 1, metaTileGrid.Size - 1), metaTileGrid.Radius);
            
            if (!String.IsNullOrWhiteSpace(_settings.BackgroundImage))
                using (var backgroundImage = Utilities.Graphics.ImageOf(_settings.BackgroundImage))
                    if (backgroundImage != null)
                        _backgroundImage = Utilities.Graphics.ImageScale(backgroundImage, _screen.Bounds.Width, _screen.Bounds.Height);
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

        internal void Select(Graphics graphics, MetaTile metaTile, bool force = false)
        {
            if ((metaTile == _selection && !force)
                    || metaTile == null)
                return;
            if (_selection != null)
                graphics.DrawImage(_passiveBorderImage, new Point(_selection.Location.X, _selection.Location.Y));
            _selection = metaTile;
            graphics.DrawImage(_activeBorderImage, new Point(_selection.Location.X, _selection.Location.Y));
        }

        private static Rectangle ImageCenter(Rectangle rectangle, Image image)
        {
            return new Rectangle((rectangle.Width - image.Width) / 2,
                (rectangle.Height - image.Height) / 2, image.Width, image.Height);
        }

        internal void Draw(Graphics graphics)
        {
            if (_backgroundImage != null)
                graphics.DrawImage(_backgroundImage, ImageCenter(_screen.Bounds, _backgroundImage));
            foreach (var metaTile in _metaTiles)
                metaTile.Draw(graphics);
            Select(graphics, _selection, true);
        }

        public void Dispose()
        {
            _activeBorderImage?.Dispose();
            _passiveBorderImage?.Dispose();
            _backgroundImage?.Dispose();
        }
    }
}