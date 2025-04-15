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