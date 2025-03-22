// LICENSE TERMS - Seanox Software Solutions is an open source project,
// hereinafter referred to as Seanox Software Solutions or Seanox for short.
// This software is subject to version 2 of the Apache License.
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
using System.Runtime.InteropServices;
using System.Text;

namespace VirtualEnvironment.Launcher.Utilities
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
            ToUnicodeEx(virtualKey, (uint)scanCode, new byte[256], stringBuilder, stringBuilder.Capacity, 0,
                    GetKeyboardLayout(0));
            return stringBuilder.ToString().Trim();
        }
    }
}