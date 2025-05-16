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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
using VirtualEnvironment.Launcher.Tiles;

namespace VirtualEnvironment.Launcher
{
    // System Tray Icon (NotifyIcon) + menu for show + exit
    // ----
    // Omitted so that there is always a child process of the virtual
    // environment that can start other processes in the virtual environment.
    // In general, the launcher should run in the background as long as the
    // virtual environment IDE is active. Therefore there is no exit function.
    // The Launcher is terminated with the Detach of the virtual environment
    // via Kill -- sounds hard, but that's the concept.
    
    // Global HotKey / KeyEvent 
    // ----
    // At runtime, the launcher is opened via a system-wide HotKey combination.
    // The launcher is hidden when the focus is lost or the ESC key is pressed.
    
    // Navigation
    // ----
    // Mouse, arrow keys as well as tab and backslash are supported. In
    // combination with Shift inverts the behavior of the buttons.
    
    // Loss of focus
    // ----
    // The launcher is implemented as an overlay from the primary screen. When
    // the launcher is no longer in the foreground, it becomes invisible.
    // Exceptions are windows/message boxes that are opened in the context of
    // the launcher.
    
    // Too low screen resolution
    // ----
    // If the screen resolution is too low, an error message is displayed as an
    // overlay instead of the launcher.
    
    // Changes in user settings, keyboard layout or screen resolution
    // ----
    // In both cases, the launcher becomes invisible so that it can be redrawn
    // by pressing the host key again.
    
    internal partial class Control : Form
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);
        
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool ShutdownBlockReasonCreate(IntPtr hWnd, string pwszReason);
        
        private const int HOTKEY_ID = 0x0;
        
        // Keyboard Input Notifications
        // https://docs.microsoft.com/de-de/windows/win32/inputdev/keyboard-input-notifications
        private const int WM_KEYUP    = 0x0101;
        private const int WM_CHAR     = 0x0102;
        private const int WM_SYSKEYUP = 0x0105;
        private const int WM_SYSCHAR  = 0x0106;
        private const int WM_HOTKEY   = 0x0312;

        private const int ERROR_CANCELLED = 0x4C7;

        private readonly MetaTile[] _metaTiles;
        private readonly MetaTileGrid _metaTileGrid;
        private readonly MetaTileScreen _metaTileScreen;
        
        private readonly Settings _settings;
        
        private readonly System.Threading.Timer _timer;
        
        private int _cursor = -1;

        private long _inputSignalTiming;
        private bool _inputEventLock;
        private bool _visible;

        private static bool _initial = true;

        internal Control(Settings settings, bool visible = true)
        {
            // Handle the Windows ShutdownBlockReason, but only if the launcher
            // is running in the context of the virtual environment. It is not
            // necessary to undo this, as the launcher ends and Windows then
            // automatically cleans up the ShutdownBlockReason. 
            var applicationDrive = Path.GetPathRoot(Assembly.GetExecutingAssembly().Location).Substring(0, 2);
            var platformDrive = Environment.GetEnvironmentVariable("PLATFORM_HOMEDRIVE");
            if (string.Equals(applicationDrive, platformDrive, StringComparison.OrdinalIgnoreCase))
                ShutdownBlockReasonCreate(Handle, "Virtual environment must be shut down.");
            
            _settings = settings;
            _visible  = visible;

            FormBorderStyle = FormBorderStyle.None;
            Bounds = Screen.PrimaryScreen.Bounds;

            // FormWindowState.Maximized can miscalculate when using custom
            // scaling. This error is corrected here.
            var devMode = Utilities.Graphics.GetDisplaySettings();
            if (devMode.HasValue)
            {
                var primaryScreenBounds = devMode.Value;
                var displayScalingFactor = Utilities.Graphics.GetDisplayScalingFactor() /100;
                var boundsWidthDiff = primaryScreenBounds.dmPelsWidth - Math.Floor(Bounds.Width * displayScalingFactor);
                if (boundsWidthDiff != 0)
                    Bounds = new Rectangle(Bounds.X, Bounds.Y, Bounds.Width +(int)boundsWidthDiff, Bounds.Height);
                var boundsHeightDiff = primaryScreenBounds.dmPelsHeight - Math.Floor(Bounds.Height * displayScalingFactor);
                if (boundsHeightDiff != 0)
                    Bounds = new Rectangle(Bounds.X, Bounds.Y, Bounds.Width +(int)boundsHeightDiff, Bounds.Height);
            }
            
            WindowState = FormWindowState.Minimized;

            #if DEBUG
            TopMost = false;
            #endif

            InitializeComponent();
            RegisterHotKey();
            
            Visible = false;
            Opacity = 0;

            Message.Font = new Font(SystemFonts.DefaultFont.FontFamily, 24, FontStyle.Regular);
            Message.ForeColor = ColorTranslator.FromHtml(_settings.ForegroundColor);
                
            BackColor = ColorTranslator.FromHtml(_settings.BackgroundColor);
            
            _metaTileGrid = MetaTileGrid.Create(_settings);
                        
            // The index for the configuration starts user-friendly with 1, but
            // internally it is technically started with 0. Therefore the index
            // in the configuration is different!
            _metaTiles = new MetaTile[_metaTileGrid.Count];

            var screen = Screen.FromControl(this);
            for (var index = 0; index < _metaTileGrid.Count; index++)
                _metaTiles[index] = MetaTile.Create(screen, _settings, new Settings.Tile() {Index = index +1});
            
            foreach (var tile in _settings.Tiles)
                if (tile.Index <= _metaTileGrid.Count
                        && tile.Index > 0)
                    _metaTiles[tile.Index - 1] = MetaTile.Create(screen, _settings, tile);

            _metaTileScreen = MetaTileScreen.Create(screen, _settings, _metaTiles);

            Closing += OnClosing; 
            KeyDown += OnKeyDown;
            Load += OnLoad;
            LostFocus += OnLostFocus;
            MouseClick += OnMouseClick;
            VisibleChanged += OnVisibleChanged;

            SystemEvents.UserPreferenceChanging += (sender, eventArgs) => Visible = false;
            SystemEvents.DisplaySettingsChanged += (sender, eventArgs) => Visible = false;

            var bounds = Screen.FromControl(this).Bounds; 
            
            _timer = new System.Threading.Timer((state) =>
            {
                if (!Settings.IsUpdateAvailable()
                        && Screen.FromControl(this).Bounds.Equals(bounds))
                    return;
                
                Invoke((MethodInvoker)delegate
                {
                    _timer?.Change(Timeout.Infinite, Timeout.Infinite);
                    _timer?.Dispose();
                    
                    Close();
                    Dispose();
                });
            }, null, 1000, 1000);
        }

        private void RegisterHotKey()
        {
            try
            {
                var matchGroups = new Regex(@"^\s*(\d+)\s*\+\s*(\d+)\s*$").Matches(_settings.HotKey)[0].Groups;
                if (!RegisterHotKey(Handle, HOTKEY_ID, Int32.Parse(matchGroups[1].Value) , Int32.Parse(matchGroups[2].Value)))
                    throw new Exception();
            }
            catch (Exception)
            {
                Messages.Push(Messages.Type.Error,
                        "The settings do not contain a usable hot key.",
                        $"Please check the node /settings/hotKey in file {Settings.FILE.Replace(" ", "\u00A0")}.");
                if (_initial)
                    Messages.Push(Messages.Type.Exit);
            }
        }

        private void SelectMetaTile(MetaTile metaTile)
        {
            if (metaTile == null)
                return;
            _metaTileScreen.Select(CreateGraphics(), metaTile);
            _cursor = Array.IndexOf(_metaTiles, metaTile);
        }

        private void UseMetaTile(MetaTile metaTile)
        {
            if (Message.Visible)
                return;
            SelectMetaTile(metaTile);
            if (metaTile == null
                    || String.IsNullOrWhiteSpace(metaTile.Settings?.Destination))
                return;

            if (metaTile.Settings.Destination.Trim().ToLower().Equals("exit"))
                Messages.Push(Messages.Type.Exit);

            // For a short time, TopMost must be abandoned. It may be that
            // Windows asks for authorization or similar when starting programs
            // and these questions should then also be in the foreground.
            TopMost = false;

            try
            {
                metaTile.Settings.Start();
            }
            catch (Exception exception)
            {
                // Exception when canceling by the user (UAC) is ignored
                if (exception is Win32Exception
                        && ((Win32Exception)exception).NativeErrorCode == ERROR_CANCELLED)
                    return;
                
                Messages.Push(Messages.Type.Error,
                        $"Error opening action: {metaTile.Settings.Destination}",
                        exception.Message,
                        exception.InnerException?.Message ?? "");

                return;
            }
            finally
            {
                #if !DEBUG
                TopMost = true;
                #endif
            }
            Visible = false;
        }
        
        protected override void WndProc(ref Message message)
        {
            // The use of the HotKey is damped so that persistent signal input
            // does not cause high-frequency redrawing. It flickers and does
            // not look nice. For this the InputSignalTiming is used, assuming
            // the signal inputs <= 75ms without interruption from a KeyUp is a
            // held key. But the behavior is only used for the HotKey. 
            
            base.WndProc(ref message);
            if (message.Msg == WM_KEYUP
                    || message.Msg == WM_SYSKEYUP)
                _inputSignalTiming = 0;
            if (message.Msg != WM_HOTKEY
                    || message.WParam.ToInt32() != HOTKEY_ID)
                return;
            if (DateTimeOffset.Now.ToUnixTimeMilliseconds() -_inputSignalTiming >= 75)
                Visible = !Visible;
            _inputSignalTiming = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }
        
        private void OnClosing(object sender, EventArgs eventArgs)
        {
            UnregisterHotKey(Handle, HOTKEY_ID);
            _initial = false;
            _metaTileScreen?.Dispose();
        }

        private void OnLoad(object sender, EventArgs eventArgs)
        {
            // Prevents possible flickering effects when drawing from the
            // background image for the first time.            
            DoubleBuffered = true;
            Thread.Sleep(25);
            Opacity = Math.Min(Math.Max(_settings.Opacity, 0), 100) /100d;
            Visible = _visible;
        }
        
        private static Process GetForegroundProcess()
        {
            GetWindowThreadProcessId(GetForegroundWindow(), out var processId);
            return Process.GetProcessById(Convert.ToInt32(processId));
        }
        
        private void OnLostFocus(object sender, EventArgs eventArgs)
        {
            // In case of an error message when opening a tile, the Launcher
            // should continue to be displayed. If other programs get the
            // focus, the Launcher should be invisible.
            var process = Process.GetCurrentProcess();
            if (!process.Id.Equals(GetForegroundProcess()?.Id))
                Visible = false;
        }

        protected override bool ProcessKeyMessage(ref Message message)
        {
            if (message.Msg == WM_KEYUP
                    || message.Msg == WM_SYSKEYUP)
                _inputSignalTiming = 0;
            if (message.Msg == WM_CHAR
                    || message.Msg == WM_SYSCHAR)
                UseMetaTile(_metaTileScreen.Locate(Char.ToString((char)message.WParam)));
            return base.ProcessKeyMessage(ref message);
        }
        
        private void OnKeyDown(object sender, KeyEventArgs keyEventArgs)
        {
            if (Message.Visible)
                return;
            
            if (_cursor < 0
                    && (new List<Keys> {Keys.Left, Keys.Back, Keys.Up}).Contains(keyEventArgs.KeyCode))
                _cursor = 0;
            if (_cursor < 0
                    && (new List<Keys> {Keys.Right, Keys.Tab, Keys.Down}).Contains(keyEventArgs.KeyCode))
                _cursor = _metaTileGrid.Count;
            
            // When the key is held down, the input signals are very fast and
            // the cursor is barely visible. Therefore, the signal processing
            // is artificially slowed down. When the lock is active, new input
            // signals are ignored, which concerns only the navigation buttons

            var navigationKeys = (new List<Keys> {Keys.Left, Keys.Back, Keys.Right, Keys.Tab, Keys.Up, Keys.Down}); 
            if (navigationKeys.Contains(keyEventArgs.KeyCode)
                    && _inputEventLock)
                return;
            if (navigationKeys.Contains(keyEventArgs.KeyCode))
                _inputEventLock = true;
            
            // Key combinations with Shift invert the key functions for
            // navigation. Escape, Enter and Space are excluded from this.

            switch (keyEventArgs.KeyCode)
            {
                case (Keys.Escape):
                    Visible = false;
                    break;
                case Keys.Left when !keyEventArgs.Shift:
                case Keys.Back when !keyEventArgs.Shift:
                case Keys.Right when keyEventArgs.Shift:
                case Keys.Tab when keyEventArgs.Shift:
                    if (_cursor <= 0)
                        _cursor = _metaTileGrid.Count;
                    _cursor--;
                    break;
                case Keys.Right when !keyEventArgs.Shift:
                case Keys.Tab when !keyEventArgs.Shift:
                case Keys.Left when keyEventArgs.Shift:
                case Keys.Back when keyEventArgs.Shift:
                    if (_cursor + 1 >= _metaTileGrid.Count)
                        _cursor = -1;
                    _cursor++;
                    break;
                case Keys.Up when !keyEventArgs.Shift:
                case Keys.Down when keyEventArgs.Shift:
                    if (_cursor < _metaTileGrid.Columns
                            && _cursor > 0)
                        _cursor += _metaTileGrid.Count - 1;
                    _cursor -= _metaTileGrid.Columns;
                    if (_cursor < 0)
                        _cursor = _metaTileGrid.Count - 1;
                    break;
                case Keys.Down when !keyEventArgs.Shift:
                case Keys.Up when keyEventArgs.Shift:
                    if (_cursor >= _metaTileGrid.Count -_metaTileGrid.Columns
                            && _cursor < _metaTileGrid.Count - 1)
                        _cursor = (_cursor - _metaTileGrid.Count) + 1;
                    _cursor += _metaTileGrid.Columns;
                    if (_cursor >= _metaTileGrid.Count)
                        _cursor = 0;
                    break;
                case Keys.Enter:
                case Keys.Space:
                    if (_cursor >= 0)
                        UseMetaTile(_metaTiles[_cursor]);
                    break;
            }

            if (_cursor >= 0)
                SelectMetaTile(_metaTiles[_cursor]);
            
            if (!navigationKeys.Contains(keyEventArgs.KeyCode)
                    || !_inputEventLock)
                return;
            Thread.Sleep(50);
            _inputEventLock = false;
        }

        private void OnMouseClick(object sender, MouseEventArgs mouseEventArgs)
        {
            if (Message.Visible)
                return;
            var location = new Point(mouseEventArgs.X, mouseEventArgs.Y);
            var metaTile = _metaTileScreen.Locate(location);
            SelectMetaTile(metaTile);
            if ((mouseEventArgs.Button & MouseButtons.Left) == 0
                    || metaTile == null
                    || metaTile.Settings == null
                    || String.IsNullOrWhiteSpace(metaTile.Settings.Destination))
                return;
            UseMetaTile(metaTile);
        }

        protected override void OnPaintBackground(PaintEventArgs eventArgs)
        {
            base.OnPaintBackground(eventArgs);
            Message.Text = "";
            if (Screen.FromControl(this).Bounds.Width < _metaTileGrid.Width + _metaTileGrid.Gap 
                    || Screen.FromControl(this).Bounds.Height < _metaTileGrid.Height + _metaTileGrid.Gap)
                Message.Text = "The resolution is too low to show the tiles.";
            else _metaTileScreen.Draw(eventArgs.Graphics);
            Message.Visible = !String.IsNullOrWhiteSpace(Message.Text);

            if (!Visible)
                return;

            BringToFront();
            Activate();
            Focus();
        }

        private void OnVisibleChanged(object sender, EventArgs eventArgs)
        {
            WindowState = Visible ? FormWindowState.Normal : FormWindowState.Minimized;
        }
    }
}