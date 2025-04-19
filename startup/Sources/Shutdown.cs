// LICENSE TERMS - Seanox Software Solutions is an open source project,
// hereinafter referred to as Seanox Software Solutions or Seanox for short.
// This software is subject to version 2 of the Apache License.
//
// Virtual Environment Startup
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
using System.Runtime.InteropServices;

namespace VirtualEnvironment.Startup
{
    internal static class Shutdown
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ShutdownBlockReasonCreate(IntPtr hWnd, [MarshalAs(UnmanagedType.LPWStr)] string pwszReason);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ShutdownBlockReasonDestroy(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CreateWindowEx(
            int dwExStyle,
            string lpClassName,
            string lpWindowName,
            int dwStyle,
            int x,
            int y,
            int nWidth,
            int nHeight,
            IntPtr hWndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            IntPtr lpParam);
        
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyWindow(IntPtr hWnd);
        
        private static IntPtr _windowHandle = IntPtr.Zero;

        internal static void Lock(string reason)
        {
            if (_windowHandle != IntPtr.Zero)
                return;
            _windowHandle = CreateWindowEx(
                0,
                "STATIC",
                "HiddenWindow",
                0,
                0, 0, 0, 0,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);
            if (_windowHandle == IntPtr.Zero)
                throw new InvalidOperationException("Shutdown block failed");
            ShutdownBlockReasonCreate(_windowHandle, reason);
        }

        internal static void Unlock()
        {
            if (_windowHandle == IntPtr.Zero)
                return;
            ShutdownBlockReasonDestroy(_windowHandle);
            DestroyWindow(_windowHandle);
            _windowHandle = IntPtr.Zero;
        }
    }
}