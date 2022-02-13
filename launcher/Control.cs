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
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
using Seanox.Platform.Launcher.Tiles;

// TODO: MessageBox does not show the correct icon in taskbar
// TODO: Check usage dispose for a robust program
// TODO: Global mouse move event, then hide if outside -- for more screen usage

// TODO: Tiles: Matrix 4x10 with shortcuts from keyboard layout
// TODO: Tiles: Navigation Up, Down, Left, Right
// TODO: OnKeyDown for all shortcuts (according to the keyboard layout)
// TODO: Rebuild when the keyboard layout changes
// TODO: Check usage dispose for a robust program
// TODO: Reload if the configuration file changes
// TODO: When resize (OnResize) the hide

namespace Seanox.Platform.Launcher
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
    
    internal partial class Control : Form
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);
        
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        
        private const int HOTKEY_ID = 0x0;
        private const int WM_HOTKEY = 0x0312;

        private readonly Image _backgroundImage;

        private readonly MetaTile[] _metaTiles;
        private readonly MetaTileGrid _metaTileGrid;
        private readonly MetaTileMap _metaTileMap;
        
        private readonly Settings _settings;
        
        private int _cursor = -1;

        internal Control(Settings settings)
        {
            _settings = settings;

            FormBorderStyle = FormBorderStyle.None;
            Bounds = Screen.PrimaryScreen.Bounds;
            WindowState = FormWindowState.Maximized;

            #if DEBUG
            TopMost = false;
            #endif

            InitializeComponent();
            RegisterHotKey();
            
            Visible = false;
            Opacity = 0;
            
            if (!String.IsNullOrWhiteSpace(_settings.BackgroundImage))
                _backgroundImage = Utilities.Graphics.ImageOf(_settings.BackgroundImage);
            BackColor = ColorTranslator.FromHtml(_settings.BackgroundColor);
            
            _metaTileGrid = Tiles.MetaTileGrid.Create(_settings, 10, 4);
                        
            // The index for the configuration starts user-friendly with 1, but
            // internally it is technically started with 0. Therefore the index
            // in the configuration is different!
            _metaTiles = new MetaTile[_metaTileGrid.Count];
            
            for (var index = 0; index < _metaTileGrid.Count; index++)
                _metaTiles[index] = Tiles.MetaTile.Create(_settings, new Settings.Tile() {Index = index +1});
            
            foreach (var tile in _settings.Tiles)
                if (tile.Index <= _metaTileGrid.Count
                        && tile.Index > 0)
                    _metaTiles[tile.Index - 1] = Tiles.MetaTile.Create(_settings, tile);

            Load += OnLoad;
            LostFocus += (sender, eventArgs) => Visible = false;
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
                MessageBox.Show("The settings do not contain a usable hot key."
                        + $"{Environment.NewLine}Please check the node /settings/hotKey in settings.xml file.",
                    "Virtual Environment Launcher", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                Environment.Exit(0);  
            }

            Closing += (sender, eventArgs) => UnregisterHotKey(Handle, HOTKEY_ID);
        }

        protected override void WndProc(ref Message message)
        {
            base.WndProc(ref message);
            if (message.Msg == WM_HOTKEY
                    && message.WParam.ToInt32() == HOTKEY_ID)
                Visible = !Visible;
        }

        protected override void OnPaintBackground(PaintEventArgs eventArgs)
        {
            base.OnPaintBackground(eventArgs);
            if (_backgroundImage == null)
                return;
            base.OnPaintBackground(eventArgs);
            var screenRectangle = Screen.FromControl(this).Bounds;
            var backgroundImage = Utilities.Graphics.ImageScale(_backgroundImage, screenRectangle.Width, screenRectangle.Height);
            var rectangle = new Rectangle((screenRectangle.Width - backgroundImage.Width) / 2,
                    (screenRectangle.Height - backgroundImage.Height) / 2, 
                    backgroundImage.Width, backgroundImage.Height);
            eventArgs.Graphics.DrawImage(backgroundImage, rectangle);
        }        
        
        private void OnLoad(object sender, EventArgs eventArgs)
        {
            // Prevents possible flickering effects when drawing from the
            // background image for the first time.            
            DoubleBuffered = true;
            Thread.Sleep(25);
            Opacity = Math.Min(Math.Max(_settings.Opacity, 0), 100) /100d;
            Visible = true;
        }
    }
}