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
using System.Text.RegularExpressions;
using System.Windows.Forms;

// TODO: MessageBox does not show the correct icon in taskbar

namespace Seanox.Virtual.Environment.Launcher
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
    
    // Worker is just a transparent background overlay for the tile menu     
    // ----
    // The worker shows a dark, opaque overlay on which the modal menu is
    // displayed. Because the opacity also affects the content of the shape and
    // the tiles but they should be displayed with full opacity.
    
    internal partial class Worker : Form
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);
        
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        
        private const int HOTKEY_ID = 0x0;
        private const int WM_HOTKEY = 0x0312;

        private Control  _control;
        private Settings _settings;
        
        internal Worker(Settings settings)
        {
            WindowState = FormWindowState.Normal;
            FormBorderStyle = FormBorderStyle.None;
            Bounds = Screen.PrimaryScreen.Bounds;

            #if DEBUG
            TopMost = false;
            #endif
            
            InitializeComponent();

            _settings = settings;

            if (settings.Opacity > 0)
                Opacity = Math.Max(settings.Opacity, 50) /100d;
            
            try
            {
                var matchGroups = new Regex(@"^\s*(\d+)\s*:\s*(\d+)\s*$").Matches(settings.HotKey)[0].Groups;
                if (!RegisterHotKey(Handle, HOTKEY_ID, Int32.Parse(matchGroups[1].Value) , Int32.Parse(matchGroups[2].Value)))
                    throw new Exception();
            }
            catch (Exception)
            {
                MessageBox.Show("The settings do not contain a usable hot key."
                                + $"{System.Environment.NewLine}Please check the node /settings/hotKey in settings.xml file.",
                    "Virtual Environment Launcher", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                System.Environment.Exit(0);  
            }
            
            _control = new Control(settings);
            _control.VisibleChanged += (sender, eventArgs) =>
            {
                if (!_control.Visible)
                    Visible = _control.Visible;
            };
            
            VisibleChanged += (sender, eventArgs) =>
            {
                if (_control.Modal
                        && _control.Visible != Visible)
                    _control.Visible = Visible;
                if (!_control.Modal
                        && Visible)
                    _control.ShowDialog(this);
            };

            Closing += (sender, eventArgs) =>
            {
                if (_control.Modal)
                    _control.Close();    
                UnregisterHotKey(Handle, HOTKEY_ID);
            };
        }

        protected override void WndProc(ref Message message)
        {
            base.WndProc(ref message);
            if (message.Msg == WM_HOTKEY
                    && message.WParam.ToInt32() == HOTKEY_ID)
                Visible = !Visible;
        }
    }
}