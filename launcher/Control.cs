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
using Microsoft.Win32;

// TODO: Tiles: Matrix 4x10 with shortcuts from keyboard layout
// TODO: Tiles: Navigation Up, Down, Left, Right
// TODO: OnKeyDown for all shortcuts (according to the keyboard layout)
// TODO: Rebuild when the keyboard layout changes

namespace Seanox.Platform.Launcher
{
    internal partial class Control : Form
    {
        private const int RASTER_SIZE = 99;
        private const int RASTER_GAP = 25;
        private const int RASTER_COLUMNS = 10;
        private const int RASTER_ROWS = 4;

        private const int RASTER_COUNT = RASTER_COLUMNS * RASTER_ROWS;
            
        private const int RASTER_HEIGHT = ((RASTER_SIZE + RASTER_GAP) * RASTER_ROWS) - RASTER_GAP;
        private const int RASTER_WIDTH = ((RASTER_SIZE + RASTER_GAP) * RASTER_COLUMNS) - RASTER_GAP;
        private const int RASTER_HEIGHT_BORDERED = RASTER_HEIGHT + (RASTER_GAP * 2); 
        private const int RASTER_WIDTH_BORDERED = RASTER_WIDTH + (RASTER_GAP * 2);

        private readonly MetaTile[] _metaTiles;

        internal Control(Settings settings)
        {
            WindowState = FormWindowState.Normal;
            FormBorderStyle = FormBorderStyle.None;
            Bounds = Screen.PrimaryScreen.Bounds;            

            #if DEBUG
            TopMost = false;
            #endif
      
            _metaTiles = new MetaTile[RASTER_COUNT];
            for (var index = 0; index < RASTER_COUNT; index++)
                _metaTiles[index] = MetaTile.Create(this, index);
            
            foreach (var tile in settings.Tiles)
                if (tile.Index <= RASTER_COUNT
                        && tile.Index > 0)
                    _metaTiles[tile.Index - 1].Tile = tile;

            foreach (var metaTile in _metaTiles)
                AttachMetaTile(metaTile);

            InitializeComponent();
            
            Message.Font = new Font(SystemFonts.DefaultFont.FontFamily, 24, FontStyle.Regular);

            LostFocus += OnLostFocus;
            KeyDown += OnKeyDown;

            SystemEvents.UserPreferenceChanging += OnVisualSettingsChanged;
            SystemEvents.DisplaySettingsChanged += OnVisualSettingsChanged;

            OnVisualSettingsChanged(null, null);
        }

        private int _cursor;

        private void AttachMetaTile(MetaTile metaTile)
        {
            Controls.Add(metaTile.Controls.ShortcutLabel);
            Controls.Add(metaTile.Controls.TitleLabel);
            Controls.Add(metaTile.Controls.IconPictureBox);
        }

        private void OnVisualSettingsChanged(object sender, EventArgs eventArgs)
        {
            foreach (var metaTiles in _metaTiles)
                metaTiles.Hide();

            Message.Text = "";
            if (Screen.FromControl(this).Bounds.Width < RASTER_WIDTH_BORDERED
                    || Screen.FromControl(this).Bounds.Height < RASTER_HEIGHT_BORDERED)
                Message.Text = "The resolution is too low to show the tiles.";
            else foreach (var metaTile in _metaTiles)
                metaTile.Show();
        }

        private void OnLostFocus(object sender, EventArgs eventArgs)
        {
            Visible = false;
        }
        
        private void SelectMetaTile(MetaTile metaTile)
        {
            foreach (var metaTileEntry in _metaTiles)
                metaTileEntry.Selected = metaTileEntry.Equals(metaTile);
        }
        
        private void OnKeyDown(object sender, KeyEventArgs keyEventArgs)
        {
            switch (keyEventArgs.KeyCode)
            {
                case Keys.Escape:
                    Visible = false;
                    break;
                case Keys.Left:
                case Keys.Back:
                    if (_cursor <= 0)
                        _cursor = RASTER_COUNT;
                    _cursor--;
                    break;
                case Keys.Right:
                case Keys.Tab:
                    if (_cursor + 1 >= RASTER_COUNT)
                        _cursor = -1;
                    _cursor++;
                    break;
                case Keys.Up:
                    if (_cursor < RASTER_COLUMNS
                            && _cursor > 0)
                        _cursor += RASTER_COUNT - 1;
                    _cursor -= RASTER_COLUMNS;
                    if (_cursor < 0)
                        _cursor = RASTER_COUNT - 1;
                    break;
                case Keys.Down:
                    if (_cursor >= RASTER_COUNT -RASTER_COLUMNS
                            && _cursor < RASTER_COUNT -1)
                        _cursor = (_cursor -RASTER_COUNT) +1;
                    _cursor += RASTER_COLUMNS;
                    if (_cursor >= RASTER_COUNT)
                        _cursor = 0;
                    break;
                case Keys.Enter:
                case Keys.Space:
                    break;
            }

            SelectMetaTile(_metaTiles[_cursor]);
        }
        
        private class MetaTile
        {
            internal Settings.Tile Tile {get; set;}

            private Control Control {get; set;}
            
            internal int Index {get; private set;}

            internal int ScanCode {get; private set;}
            
            internal string Symbol {get; private set;}

            internal MetaTileControls Controls {get; private set;}

            private bool _selected;
            
            private static readonly Color PASSIVE_COLOR = ((Func<Color>)(() =>
                    Color.FromArgb(100, 100, 100)))();

            private static readonly Color ACTIVE_COLOR = ((Func<Color>)(() =>
                    Color.FromArgb(250, 180, 0)))();
            
            private static readonly Color SHORTCUT_COLOR = ACTIVE_COLOR;

            private static readonly Color TITLE_COLOR = ((Func<Color>)(() =>
                    Color.FromArgb(200, 200, 200)))();

            private static readonly Image PASSIVE_BORDER_IMAGE = ((Func<Image>)(() =>
            {
                var borderImage = new Bitmap(RASTER_SIZE, RASTER_SIZE);
                var borderImageGraphics = Graphics.FromImage(borderImage);
                Utilities.Graphics.DrawRoundedRect(borderImageGraphics, new Pen(new SolidBrush(PASSIVE_COLOR)),
                        new Rectangle(0, 0,RASTER_SIZE -1,RASTER_SIZE -1), 1);
                return borderImage;
            }))();
                
            private static readonly Image ACTIVE_BORDER_IMAGE = ((Func<Image>)(() =>
            {
                var borderImage = new Bitmap(RASTER_SIZE, RASTER_SIZE);
                var borderImageGraphics = Graphics.FromImage(borderImage);
                Utilities.Graphics.DrawRoundedRect(borderImageGraphics, new Pen(new SolidBrush(ACTIVE_COLOR)),
                        new Rectangle(0, 0,RASTER_SIZE -1,RASTER_SIZE -1), 1);
                return borderImage;
            }))();

            internal bool Selected
            {
                get => _selected;
                set
                {
                    if (_selected == value)
                        return;
                    _selected = value;
                    Controls.TitleLabel.BackgroundImage =
                        _selected ? ACTIVE_BORDER_IMAGE : PASSIVE_BORDER_IMAGE;
                }
            }

            private MetaTile()
            {
            }

            internal static MetaTile Create(Control control, int index)
            {
                // The following scan codes are used:
                // 0x02 0x03 0x04 0x05 0x06 0x07 0x08 0x09 0x0A 0x0B
                // 0x10 0x11 0x12 0x13 0x14 0x15 0x16 0x17 0x18 0x19
                // 0x1E 0x1F 0x20 0x21 0x22 0x23 0x24 0x25 0x26 0x27
                // 0x2C 0x2D 0x2E 0x2F 0x30 0x31 0x32 0x33 0x34 0x35
                // The ranges are 14 points apart.

                // A: 0
                // B: =(ROUNDDOWN(A1/10;0)*14)
                // C: =B1/14
                // D: =10*C1
                // E: =A1-D1
                // F: =E1+B1+2

                var radix = ((int)Math.Floor(index / 10d)) * 14;
                var scanCode = radix + (index - (10 * (radix / 14))) + 2;
                
                return new MetaTile()
                {
                    Control = control,
                    Index = index,
                    ScanCode = scanCode, 
                    Symbol = Utilities.ScanCode.ToString(scanCode),
                    Controls = new MetaTileControls()
                    {
                        IconPictureBox = new PictureBox()
                        {
                            // TODO:
                        },
                        TitleLabel = new Label()
                        {
                            Width = RASTER_SIZE,
                            Height = RASTER_SIZE,
                            Padding = new Padding(10, 10, 10, 10),

                            Font = new Font(SystemFonts.DefaultFont.FontFamily, 9.75f, FontStyle.Regular),
                            ForeColor = TITLE_COLOR,
                            TextAlign = ContentAlignment.BottomCenter,
                            Text = "TODO",

                            BackgroundImage = PASSIVE_BORDER_IMAGE
                        },
                        ShortcutLabel = new Label()
                        {
                            AutoSize = true,

                            Font = new Font(SystemFonts.DefaultFont.FontFamily, 9.75f, FontStyle.Regular),
                            ForeColor = SHORTCUT_COLOR,
                            TextAlign = ContentAlignment.TopLeft,
                            Text = Utilities.ScanCode.ToString(scanCode)
                        }
                    }
                };
            }
            
            internal class MetaTileControls
            {
                internal PictureBox IconPictureBox {get; set;}
                internal Label TitleLabel {set; get;}
                internal Label ShortcutLabel {get; set;}
            }

            private Point Location {
                get
                {
                    var rasterStartX = (Screen.FromControl(Control).Bounds.Width - RASTER_WIDTH) / 2;
                    var rasterStartY = (Screen.FromControl(Control).Bounds.Height - RASTER_HEIGHT) / 2;
                    var tileRasterColumn = Index % RASTER_COLUMNS;
                    var tileRasterRow = (int)Math.Floor((float)Index / RASTER_COLUMNS);

                    var tileStartX = rasterStartX + ((tileRasterColumn * (RASTER_SIZE + RASTER_GAP)));
                    var tileStartY = rasterStartY + ((tileRasterRow * (RASTER_SIZE + RASTER_GAP)));

                    return new Point(tileStartX, tileStartY);
                }
            }

            internal void Show()
            {
                var location = Location;
                Controls.TitleLabel.Location = location;
                Controls.ShortcutLabel.Location = new Point(location.X + 5, location.Y + 5);
                    
                Controls.IconPictureBox.Visible = true;
                Controls.TitleLabel.Visible     = true;
                Controls.ShortcutLabel.Visible  = true;
            }
            
            internal void Hide()
            {
                Controls.IconPictureBox.Visible = false;
                Controls.TitleLabel.Visible     = false;
                Controls.ShortcutLabel.Visible  = false;
            }
        }
    }
}