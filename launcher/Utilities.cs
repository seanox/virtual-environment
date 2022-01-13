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
using System.Runtime.InteropServices;
using System.Text;

// TODO: Check usage dispose for a robust program

namespace Seanox.Platform.Launcher
{
    internal static class Utilities
    {
        internal static class ScanCode
        {
            [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
            private static extern int ToUnicodeEx(uint wVirtKey, uint wScanCode, byte[] lpKeyState,
                    StringBuilder pwszBuff, int cchBuff, uint wFlags, IntPtr dwhkl);

            [DllImport("user32.dll", ExactSpelling = true)]
            private static extern IntPtr GetKeyboardLayout(uint threadId);

            private enum MapType : uint
            {
                MapvkVkToVsc   = 0x0,
                MapvkVscToVk   = 0x1,
                MapvkVkToChar  = 0x2,
                MapvkVscToVkEx = 0x3,
            }

            [DllImport("user32.dll")]
            private static extern uint MapVirtualKey(uint uCode, MapType uMapType);
        
            internal static string ToString(int scanCode)
            {
                var stringBuilder = new StringBuilder(10);
                var virtualKey = MapVirtualKey((uint)scanCode, MapType.MapvkVscToVk);
                ToUnicodeEx(virtualKey, (uint)scanCode, new byte[256], stringBuilder, stringBuilder.Capacity, 0, GetKeyboardLayout(0));
                return stringBuilder.ToString().ToUpper().Trim();
            }
        }
        
        internal static class Graphics
        {
            [DllImport("Shell32.dll", EntryPoint = "ExtractIconExW", CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
            private static extern int ExtractIconEx(string lpszFile, int nIconIndex, out IntPtr phiconLarge, out IntPtr phiconSmall, int nIcons);

            public static Icon ExtractIcon(string file, int number, bool largeIcon)
            {
                IntPtr large;
                IntPtr small;
                ExtractIconEx(file, number, out large, out small, 1);
                try
                {
                    return Icon.FromHandle(largeIcon ? large : small);
                }
                catch
                {
                    return null;
                }
            }
            
            internal static void DrawRoundedRect(System.Drawing.Graphics graphics, Pen pen, Rectangle bounds, int radius)
            {
                var diameter = radius * 2;
                var size = new Size(diameter, diameter);
                var arc = new Rectangle(bounds.Location, size);

                using (var path = new GraphicsPath())
                {
                    if (radius != 0)
                    {
                        // top left arc  
                        path.AddArc(arc, 180, 90);

                        // top right arc  
                        arc.X = bounds.Right - diameter;
                        path.AddArc(arc, 270, 90);

                        // bottom right arc  
                        arc.Y = bounds.Bottom - diameter;
                        path.AddArc(arc, 0, 90);

                        // bottom left arc 
                        arc.X = bounds.Left;
                        path.AddArc(arc, 90, 90);
                
                    } else path.AddRectangle(bounds); 

                    path.CloseFigure();
            
                    graphics.DrawPath(pen, path);
                }
            }
        }
    }
}