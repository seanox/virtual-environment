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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace VirtualEnvironment.Launcher.Utilities
{
    internal static class Graphics
    {
        [DllImport("Shell32.dll", EntryPoint = "ExtractIconExW",
                CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int ExtractIconEx(string lpszFile, int nIconIndex, out IntPtr phiconLarge, out IntPtr phiconSmall, int nIcons);

        private static readonly Regex IMAGE_FIlE_PATTERN = new Regex(@"^.*\.(bmp|dib|gif|jpg|jpeg|jpe|jfif|png|tif|tiff|heic|webp)$", RegexOptions.IgnoreCase);
        
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
                if (!File.Exists(file))
                    return null;
                if (String.IsNullOrWhiteSpace(file))
                    return null;
                if (IMAGE_FIlE_PATTERN.IsMatch(file))
                    return Image.FromFile(file);
                using (var icon = ExtractIcon(file, number, large))
                    if (icon != null)
                        return icon.ToBitmap();
                using (var icon = Icon.ExtractAssociatedIcon(file))
                    if (icon != null)
                        return icon.ToBitmap();
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
        
        internal static Image ImageResize(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height, PixelFormat.Format32bppPArgb);

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
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        internal static Image ImageScale(Image image, int width, int height)
        {
            if (image.Height == height
                    && image.Width == width)
                return image;
            var factor = Math.Max((double)height / image.Height, (double)width / image.Width);
            return ImageResize(image, (int)(image.Width * factor), (int)(image.Height * factor));
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

        [DllImport("shcore.dll")]
        private static extern int GetScaleFactorForMonitor(IntPtr hMonitor, out int pScale);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        [DllImport("user32.dll")]
        private static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

        // Constants of the Windows-GDI API
        private const int LOGPIXELSX = 88;
        private const int LOGPIXELSY = 90;

        private static int GetDpiForPrimaryMonitor()
        {
            var hdc = GetDC(IntPtr.Zero);
            var dpiX = GetDeviceCaps(hdc, LOGPIXELSX);
            var dpiY = GetDeviceCaps(hdc, LOGPIXELSY);
            ReleaseDC(IntPtr.Zero, hdc);
            return (int)Math.Round((dpiX + dpiY) / 2.0);
        }

        private static float GetScalingFactorFromRegistry()
        {
            const string userRoot = "HKEY_CURRENT_USER";
            const string subkey = @"Control Panel\Desktop";
            const string keyName = userRoot + "\\" + subkey;

            var defaultDpi = GetDpiForPrimaryMonitor();
            var registryValue = Registry.GetValue(keyName, "LogPixels", defaultDpi);
            var dpi = (registryValue != null) ? (int)registryValue : defaultDpi;
            
            return dpi / (float)defaultDpi * 100;
        }

        private static float GetScalingFactorForPrimaryMonitor()
        {
            var primaryMonitor = MonitorFromWindow(IntPtr.Zero, 0);
            GetScaleFactorForMonitor(primaryMonitor, out var scaleFactor);
            return scaleFactor;
        }   
        
        internal static float GetDisplayScalingFactor()
        {
            var scalingFactor = GetScalingFactorFromRegistry();
            if (Math.Abs(scalingFactor - 100) < 0.01)
                scalingFactor = GetScalingFactorForPrimaryMonitor();
            return scalingFactor;
        }
        
        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        private static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        private static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct DISPLAY_DEVICE
        {
            [MarshalAs(UnmanagedType.U4)]
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            [MarshalAs(UnmanagedType.U4)]
            public int StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        internal struct DEVMODE
        {
            private const int CCHDEVICENAME = 32;
            private const int CCHFORMNAME = 32;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
            public string dmDeviceName;
            [MarshalAs(UnmanagedType.U2)]
            public ushort dmSpecVersion;
            [MarshalAs(UnmanagedType.U2)]
            public ushort dmDriverVersion;
            [MarshalAs(UnmanagedType.U2)]
            public ushort dmSize;
            [MarshalAs(UnmanagedType.U2)]
            public ushort dmDriverExtra;
            [MarshalAs(UnmanagedType.U4)]
            public uint dmFields;
            [MarshalAs(UnmanagedType.I4)]
            public int dmPositionX;
            [MarshalAs(UnmanagedType.I4)]
            public int dmPositionY;
            [MarshalAs(UnmanagedType.U4)]
            public uint dmDisplayOrientation;
            [MarshalAs(UnmanagedType.U4)]
            public uint dmDisplayFixedOutput;
            [MarshalAs(UnmanagedType.I2)]
            public short dmColor;
            [MarshalAs(UnmanagedType.I2)]
            public short dmDuplex;
            [MarshalAs(UnmanagedType.I2)]
            public short dmYResolution;
            [MarshalAs(UnmanagedType.I2)]
            public short dmTTOption;
            [MarshalAs(UnmanagedType.I2)]
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
            public string dmFormName;
            [MarshalAs(UnmanagedType.U2)]
            public ushort dmLogPixels;
            [MarshalAs(UnmanagedType.U4)]
            public uint dmBitsPerPel;
            [MarshalAs(UnmanagedType.U4)]
            public uint dmPelsWidth;
            [MarshalAs(UnmanagedType.U4)]
            public uint dmPelsHeight;
            [MarshalAs(UnmanagedType.U4)]
            public uint dmDisplayFlags;
            [MarshalAs(UnmanagedType.U4)]
            public uint dmDisplayFrequency;
            [MarshalAs(UnmanagedType.U4)]
            public uint dmICMMethod;
            [MarshalAs(UnmanagedType.U4)]
            public uint dmICMIntent;
            [MarshalAs(UnmanagedType.U4)]
            public uint dmMediaType;
            [MarshalAs(UnmanagedType.U4)]
            public uint dmDitherType;
            [MarshalAs(UnmanagedType.U4)]
            public uint dmReserved1;
            [MarshalAs(UnmanagedType.U4)]
            public uint dmReserved2;
            [MarshalAs(UnmanagedType.U4)]
            public uint dmPanningWidth;
            [MarshalAs(UnmanagedType.U4)]
            public uint dmPanningHeight;
        }

        internal static DEVMODE? GetDisplaySettings()
        {
            var displayDevice = new DISPLAY_DEVICE();
            displayDevice.cb = Marshal.SizeOf(displayDevice);
            if (!EnumDisplayDevices(null, 0, ref displayDevice, 0))
                return null;
            var devMode = new DEVMODE();
            if (EnumDisplaySettings(displayDevice.DeviceName, -1, ref devMode))
                return devMode;
            return null;
        }
    }
}