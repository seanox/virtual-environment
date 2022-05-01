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
using System.IO;
using System.Windows.Forms;

namespace VirtualEnvironment.Launcher.Tiles
{
    internal class MetaTile : IDisposable
    {
        internal readonly Settings.Tile Settings;
        
        internal readonly int       Index;
        internal readonly int       ScanCode;
        internal readonly string    Symbol;
        internal readonly Rectangle Location;

        private readonly MetaTileGrid _metaTileGrid;

        private readonly Color _borderColor;
        private readonly Color _foregroundColor;
        private readonly Color _highlightColor;
        private readonly Font  _textFont;
        private readonly Size  _textMeasure;
        private readonly int   _iconSpace;

        private Image _iconImage;
        private long  _iconImageLastModified;

        private MetaTile(Screen screen, Settings settings, Settings.Tile tile)
        {
            Settings = tile;

            _metaTileGrid = MetaTileGrid.Create(settings);

            // The index for the configuration starts user-friendly with 1,
            // but internally it is technically started with 0. Therefore
            // the index in the configuration is different!
            Index = tile.Index - 1;

            // The following scan codes are used:
            // 0x02 0x03 0x04 0x05 0x06 0x07 0x08 0x09 0x0A 0x0B
            // 0x10 0x11 0x12 0x13 0x14 0x15 0x16 0x17 0x18 0x19
            // 0x1E 0x1F 0x20 0x21 0x22 0x23 0x24 0x25 0x26 0x27
            // 0x2C 0x2D 0x2E 0x2F 0x30 0x31 0x32 0x33 0x34 0x35
            // The ranges are 14 points apart.

            // A1: 0
            // B1: =(ROUNDDOWN(A1/10;0)*14)
            // C1: =B1/14
            // D1: =10*C1
            // E1: =A1-D1
            // F1: =E1+B1+2
            var radix = ((int)Math.Floor(Index / 10d)) * 14;
            ScanCode = radix + (Index - (10 * (radix / 14))) + 2;
            Symbol = Utilities.ScanCode.ToString(ScanCode);

            var screenRectangle = screen.Bounds;
            var tileMapLocation = new Point((screenRectangle.Width / 2) - (_metaTileGrid.Width / 2),
                (screenRectangle.Height / 2) - (_metaTileGrid.Height / 2));

            var tileRasterColumn = Index % _metaTileGrid.Columns;
            var tileRasterRow = (int)Math.Floor((float)Index / _metaTileGrid.Columns);
            var tileStartX = tileRasterColumn * (_metaTileGrid.Size + _metaTileGrid.Gap);
            var tileStartY = tileRasterRow * (_metaTileGrid.Size + _metaTileGrid.Gap);
            Location = new Rectangle(tileStartX +tileMapLocation.X, tileStartY +tileMapLocation.Y, _metaTileGrid.Size, _metaTileGrid.Size);
            
            _borderColor = ColorTranslator.FromHtml(settings.BorderColor);
            _foregroundColor = ColorTranslator.FromHtml(settings.ForegroundColor);
            _highlightColor = ColorTranslator.FromHtml(settings.HighlightColor);

            _textFont = new Font(SystemFonts.DefaultFont.FontFamily, _metaTileGrid.FontSize, FontStyle.Regular);
            _textMeasure = TextRenderer.MeasureText($"{Environment.NewLine}", _textFont);
            _iconSpace = _metaTileGrid.Size - _textMeasure.Height;
            _iconImage = GetIconImage(_iconSpace -(2 * _metaTileGrid.Padding), Settings.IconFile, Settings.IconIndex);

            _iconImageLastModified = -1;
            if (File.Exists(Settings.IconFile))
                _iconImageLastModified = File.GetLastWriteTime(Settings.IconFile).Ticks;
        }

        private static Image GetIconImage(int iconSize, string iconFile, int iconIndex)
        {
            var iconImage = Utilities.Graphics.ImageOf(iconFile, iconIndex);
            if (iconImage == null
                    || iconSize < 16)
                return null;

            iconSize = CalculateIconSize(iconSize);

            // For aesthetic reasons, the scaling of the icons depends on the
            // screen resolution. As a reference, WXGA HD (1366 x 768) is used
            // as the smallest usable resolution in IT. With increasing
            // resolution (total number of pixels) max. 25% scaling of the icon
            // size is calculated in relation to the screen resolution.

            var screenBounds = Screen.PrimaryScreen.Bounds;
            var scaleFactorMax = (float)screenBounds.Width * screenBounds.Height;
            scaleFactorMax = Math.Max(scaleFactorMax / (1366 * 768), 1);
            scaleFactorMax = (scaleFactorMax / 4) +1;
            
            var scaleFactor = Math.Max(iconSize / Math.Min(iconImage.Height, iconImage.Width), 1f);
            scaleFactor = Math.Min(Math.Max(scaleFactor, 1), scaleFactorMax);

            using (iconImage)
                return Utilities.Graphics.ImageResize(iconImage, (int)(iconImage.Width * scaleFactor),
                        (int)(iconImage.Height * scaleFactor));
        }

        internal static MetaTile Create(Screen screen, Settings settings, Settings.Tile tile)
        {
            return new MetaTile(screen, settings, tile);
        }

        private static int CalculateIconSize(int iconSpace)
        {
            var iconSizes = new [] {16, 24, 32, 48, 64, 128, 256, 512};
            Array.Reverse(iconSizes);
            foreach (var iconSize in iconSizes)
                if (iconSpace >= iconSize)
                    return iconSize;
            return -1;
        }

        internal void Draw(Graphics graphics)
        {
            using (var rectanglePen = new Pen(new SolidBrush(_borderColor)))
                Utilities.Graphics.DrawRectangleRounded(graphics, rectanglePen,
                        new Rectangle(Location.X, Location.Y, _metaTileGrid.Size - 1, _metaTileGrid.Size - 1), _metaTileGrid.Radius);

            try
            {
                var iconImageLastModified = File.Exists(Settings.IconFile) ? File.GetLastWriteTime(Settings.IconFile).Ticks : -1;
                if (iconImageLastModified != _iconImageLastModified)
                    _iconImage = GetIconImage(_iconSpace -(2 * _metaTileGrid.Padding), Settings.IconFile, Settings.IconIndex);
                _iconImageLastModified = iconImageLastModified;
            }
            catch (Exception)
            {
            }
           
            if (_iconImage != null)
                graphics.DrawImage(_iconImage, Location.X + (_metaTileGrid.Size /2) -(_iconImage.Width /2),
                        Location.Y + Math.Max((_iconSpace /2) -(_iconImage.Height /2), _metaTileGrid.Padding));

            var stringFormat = new StringFormat();
            stringFormat.Alignment = StringAlignment.Center;
            stringFormat.LineAlignment = StringAlignment.Center;

            var titleRectangle = new Rectangle(Location.X + _metaTileGrid.Padding, Location.Y + _metaTileGrid.Size -_metaTileGrid.Padding -_textMeasure.Height,
                    _metaTileGrid.Size - (2 * _metaTileGrid.Padding), _textMeasure.Height);
            using (var foregroundColorBrush = new SolidBrush(_foregroundColor))
                graphics.DrawString(Settings.Title, _textFont, foregroundColorBrush, titleRectangle, stringFormat);

            using (var highlightColorBrush = new SolidBrush(_highlightColor))
                graphics.DrawString(Symbol.ToUpper(), _textFont, highlightColorBrush,
                        new Point(Location.X + (_metaTileGrid.Padding /2), Location.Y + (_metaTileGrid.Padding /2)));
        }

        public void Dispose()
        {
            _textFont?.Dispose();
            _iconImage?.Dispose();
        }
    }
}