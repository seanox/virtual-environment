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
using System.Windows.Forms;
using launcher;

// TODO: Tiles: Matrix 4x10 with shortcuts from keyboard layout
// TODO: Tiles: Navigation Up, Down, Left, Right
// TODO: OnKeyDown for all shortcuts (according to the keyboard layout)
// TODO: Rebuild when the keyboard layout changes

namespace Launcher
{
    internal partial class Control : Form
    {
        private const int RASTER_SIZE = 99;
        private const int RASTER_GAP = 25;
        private const int RASTER_COLUMNS = 10;
        private const int RASTER_ROWS = 4;

        private const int RASTER_CELL_COUNT = RASTER_COLUMNS * RASTER_ROWS;
            
        private const int RASTER_HEIGHT = ((RASTER_SIZE + RASTER_GAP) * RASTER_ROWS) - RASTER_GAP;
        private const int RASTER_WIDTH = ((RASTER_SIZE + RASTER_GAP) * RASTER_COLUMNS) - RASTER_GAP;
        private const int RASTER_HEIGHT_BORDERED = RASTER_HEIGHT + (RASTER_GAP * 2); 
        private const int RASTER_WIDTH_BORDERED = RASTER_WIDTH + (RASTER_GAP * 2);

        private Settings _settings;

        private int _cursor;

        internal Control(Settings settings)
        {
            _settings = settings;

            for (var index = 1; index <= RASTER_CELL_COUNT; index++)
                AttachTile(index);

            InitializeComponent();

            Message.Font = new Font(SystemFonts.DefaultFont.FontFamily, 24, FontStyle.Regular);

            SizeChanged += OnVisualChange;
            VisibleChanged += OnVisualChange;
            Shown += OnVisualChange;
            KeyDown += OnKeyDown;
            LostFocus += OnLostFocus;
        }

        private void AttachTile(int index)
        {
            // TODO:
        }

        private void SelectTile(int index)
        {
            // TODO:
            // TODO: only for development
            Message.Text = "" + index;
        }
        
        private void OnVisualChange(object sender, EventArgs eventArgs)
        {
            if (!Visible)
                return;

            if (Size.Width < RASTER_WIDTH_BORDERED
                    || Size.Height < RASTER_HEIGHT_BORDERED)
                Message.Text = "The resolution is too low to show the tiles.";
            
            // TODO: Realignment of the tiles according to the current screen resolution.
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
                    if (_cursor <= 0)
                        _cursor = RASTER_CELL_COUNT;
                    _cursor--;
                    break;
                case Keys.Right:
                case Keys.Tab:
                    if (_cursor + 1 >= RASTER_CELL_COUNT)
                        _cursor = -1;
                    _cursor++;
                    break;
                case Keys.Up:
                    if (_cursor < RASTER_COLUMNS
                            && _cursor > 0)
                        _cursor += RASTER_CELL_COUNT - 1;
                    _cursor -= RASTER_COLUMNS;
                    if (_cursor < 0)
                        _cursor = RASTER_CELL_COUNT - 1;
                    break;
                case Keys.Down:
                    if (_cursor >= RASTER_CELL_COUNT -RASTER_COLUMNS
                            && _cursor < RASTER_CELL_COUNT -1)
                        _cursor = (_cursor -RASTER_CELL_COUNT) +1;
                    _cursor += RASTER_COLUMNS;
                    if (_cursor >= RASTER_CELL_COUNT)
                        _cursor = 0;
                    break;
                case Keys.Enter:
                case Keys.Space:
                    break;
            }
            
            SelectTile(_cursor);
        }
    }
}