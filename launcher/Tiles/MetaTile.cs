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
    internal class MetaTile
    {
        internal readonly int Index;
        internal readonly int ScanCode;
        internal readonly string Symbol;
        internal readonly Image Image;
        internal readonly Rectangle Location;

        private readonly MetaTileGrid _metaTileGrid;
        
        private MetaTile(Settings settings, Settings.Tile tile)
        {
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

            var borderColor = ColorTranslator.FromHtml(settings.BorderColor);
            var foregroundColor = ColorTranslator.FromHtml(settings.ForegroundColor);
            var highlightColor = ColorTranslator.FromHtml(settings.HighlightColor);

            var textFont = new Font(SystemFonts.DefaultFont.FontFamily, settings.FontSize, FontStyle.Regular);
            var textMeasure = TextRenderer.MeasureText($"{Environment.NewLine}", textFont);

            Image = new Bitmap(settings.GridSize, settings.GridSize, PixelFormat.Format32bppPArgb);
            using (var imageGraphics = Graphics.FromImage(Image))
            {
                Utilities.Graphics.DrawRectangleRounded(imageGraphics, new Pen(new SolidBrush(borderColor)),
                        new Rectangle(0, 0, settings.GridSize - 1, settings.GridSize - 1), 1);
                var iconSize = settings.GridSize - (3 * settings.GridPadding) - textMeasure.Height; 
                var iconFile = Environment.ExpandEnvironmentVariables(tile.IconFile ?? "");
                using (var iconImage = GetIconImage(iconSize, iconFile, tile.IconIndex))
                    if (iconImage != null)
                        imageGraphics.DrawImage(iconImage, (settings.GridSize /2) -(iconImage.Width /2),
                                (int)(settings.GridPadding + Math.Max(0, iconSize - iconImage.Height)));

                var stringFormat = new StringFormat();
                stringFormat.Alignment = StringAlignment.Center;
                stringFormat.LineAlignment = StringAlignment.Center;

                var titleRectangle = new Rectangle(settings.GridPadding, settings.GridSize -settings.GridPadding -textMeasure.Height, settings.GridSize - (2 * settings.GridPadding), textMeasure.Height);
                imageGraphics.DrawString(tile.Title, textFont, new SolidBrush(foregroundColor), titleRectangle, stringFormat);
                
                imageGraphics.DrawString(Symbol, textFont, new SolidBrush(highlightColor), new Point(settings.GridPadding /2, settings.GridPadding /2));
            }
            
            textFont.Dispose();

            var tileRasterColumn = Index % MetaTileGrid.Columns;
            var tileRasterRow = (int)Math.Floor((float)Index / MetaTileGrid.Columns);
            var tileStartX = ((tileRasterColumn * (_metaTileGrid.Size + _metaTileGrid.Gap)));
            var tileStartY = ((tileRasterRow * (_metaTileGrid.Size + _metaTileGrid.Gap)));
            Location = new Rectangle(tileStartX, tileStartY, _metaTileGrid.Size, _metaTileGrid.Size);
        }

        private Image GetIconImage(int iconSize, string iconFile, int iconIndex)
        {
            var iconImage = Utilities.Graphics.ImageOf(iconFile, iconIndex);
            if (iconImage == null)
                return null;
            var scaleFactor = 1f;
            scaleFactor = Math.Min(iconSize / (float)iconImage.Height, scaleFactor);
            scaleFactor = Math.Min(iconSize / (float)iconImage.Width, scaleFactor);
            if (scaleFactor >= 1)
                return iconImage;
            using (iconImage)
                return Utilities.Graphics.ImageResize(iconImage, (int)(iconImage.Width * scaleFactor),
                        (int)(iconImage.Height * scaleFactor));
        }

        internal static MetaTile Create(Settings settings, Settings.Tile tile)
        {
            return new MetaTile(settings, tile);
        }
    }
}