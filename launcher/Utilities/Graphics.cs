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
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

// TODO: Check usage dispose for a robust program

namespace Seanox.Platform.Launcher.Utilities
{
    internal static class Graphics
    {
        [DllImport("Shell32.dll", EntryPoint = "ExtractIconExW",
                CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int ExtractIconEx(string lpszFile, int nIconIndex, out IntPtr phiconLarge, out IntPtr phiconSmall, int nIcons);

        private static readonly Regex IMAGE_FIlE_PATTERN = new Regex(@"^.*\.(bmp|dib|gif|jpg|jpeg|jpe|jfif|png|tif|tiff|ico|heic|webp)$", RegexOptions.IgnoreCase);
        
        private static Icon ExtractIcon(string file, int number = 0, bool large = true)
        {
            try
            {
                ExtractIconEx(file, number, out var largeIcon, out var smallIcon, 1);
                return Icon.FromHandle(large ? largeIcon : smallIcon);
            }
            catch
            {
                return null;
            }
        }

        internal static Image ImageOf(string file, int number = 0, bool large = true)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(file))
                    return null;
                if (IMAGE_FIlE_PATTERN.IsMatch(file))
                    return Image.FromFile(file);
                var icon = ExtractIcon(file, number, large);
                if (icon != null)
                    return Bitmap.FromHicon(icon.Handle);
                icon = Icon.ExtractAssociatedIcon(file);
                var bitmap = new Bitmap(icon.Width, icon.Height);
                using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
                    graphics.DrawIcon(icon, 0, 0);
                return bitmap;
            }
            catch (Exception)
            {
                return null;
            }
        }
        
        internal static Image ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = System.Drawing.Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width,image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        internal static void DrawRectangleRounded(System.Drawing.Graphics graphics, Pen pen, Rectangle bounds, int radius)
        {
            var diameter = radius * 2;
            var size = new Size(diameter, diameter);
            var arc = new Rectangle(bounds.Location, size);

            using (var path = new GraphicsPath())
            {
                if (radius != 0)
                {
                    path.AddArc(arc, 180, 90);

                    arc.X = bounds.Right - diameter;
                    path.AddArc(arc, 270, 90);

                    arc.Y = bounds.Bottom - diameter;
                    path.AddArc(arc, 0, 90);

                    arc.X = bounds.Left;
                    path.AddArc(arc, 90, 90);
            
                } else path.AddRectangle(bounds); 

                path.CloseFigure();
        
                graphics.DrawPath(pen, path);
            }
        }
    }
}