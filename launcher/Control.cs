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
using System.Windows.Forms;
using launcher;

// TODO: Tiles: Matrix 4x10 with shortcuts from keyboard layout
// TODO: Tiles: Navigation Up, Down, Left, Right
// TODO: MessageBox does not show the correct icon in taskbar

namespace Launcher
{
    internal partial class Control : Form
    {
        private Settings _settings;
        
        internal Control(Settings settings)
        {
            InitializeComponent();

            _settings = settings;

            SizeChanged += OnVisualChange;
            VisibleChanged += OnVisualChange;
            KeyDown += OnKeyDown;
            LostFocus += OnLostFocus;
        }

        private void OnVisualChange(object sender, EventArgs eventArgs)
        {
            if (!Visible)
                return;
            
            
        }

        private void OnLostFocus(object sender, EventArgs eventArgs)
        {
            Close();
        }
        
        private void OnKeyDown(object sender, KeyEventArgs keyEventArgs)
        {
            switch (keyEventArgs.KeyCode)
            {
                case Keys.Escape:
                    Close();
                    break;
                case Keys.Left:
                case Keys.Back:
                    break;
                case Keys.Right:
                case Keys.Tab:
                    break;
                case Keys.Up:
                    break;
                case Keys.Down:
                    break;
                case Keys.Enter:
                case Keys.Space:
                    break;
            }
        }
    }
}