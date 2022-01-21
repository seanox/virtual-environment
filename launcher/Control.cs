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
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;

// TODO: Tiles: Matrix 4x10 with shortcuts from keyboard layout
// TODO: Tiles: Navigation Up, Down, Left, Right
// TODO: OnKeyDown for all shortcuts (according to the keyboard layout)
// TODO: Rebuild when the keyboard layout changes
// TODO: Check usage dispose for a robust program
// TODO: Reload if the configuration file changes
// TODO: When resize (OnResize) the hide

namespace Seanox.Platform.Launcher
{
    internal partial class Control : Form
    {
        private readonly Settings Settings;

        private readonly MetaTile[] _metaTiles;

        private readonly int GridSize;
        private readonly int GridGap;
        private readonly int GridColumns;
        private readonly int GridRows;
        private readonly int GridPadding;
        
        private int GridCount  => GridColumns * GridRows;
        private int GridHeight => ((GridSize + GridGap) * GridRows) - GridGap;
        private int GridWidth  => ((GridSize + GridGap) * GridColumns) - GridGap;

        private int GridHeightBordered => GridHeight + (GridGap * 2);
        private int GridWidthBordered  => GridWidth + (GridGap * 2);

        internal Control(Settings settings)
        {
            WindowState = FormWindowState.Normal;
            FormBorderStyle = FormBorderStyle.None;
            Bounds = Screen.PrimaryScreen.Bounds;

            #if DEBUG
            TopMost = false;
            #endif

            Settings = settings;
            
            GridSize    = settings.GridSize;
            GridGap     = settings.GridGap;
            GridColumns = 10;
            GridRows    = 4;
            GridPadding = settings.GridPadding;
      
            // The index for the configuration starts user-friendly with 1, but
            // internally it is technically started with 0. Therefore the index
            // in the configuration is different!
            
            _metaTiles = new MetaTile[GridCount];
            for (var index = 0; index < GridCount; index++)
                _metaTiles[index] = MetaTile.Create(this, new Settings.Tile() {Index = index +1});
            
            foreach (var tile in settings.Tiles)
                if (tile.Index <= GridCount
                        && tile.Index > 0)
                    _metaTiles[tile.Index - 1] = MetaTile.Create(this, tile);

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

        private int _cursor = -1;

        private void AttachMetaTile(MetaTile metaTile)
        {
            Controls.Add(metaTile.Controls.ShortcutLabel);
            Controls.Add(metaTile.Controls.IconPictureBox);
            Controls.Add(metaTile.Controls.TitleLabel);
        }

        private void OnVisualSettingsChanged(object sender, EventArgs eventArgs)
        {
            foreach (var metaTiles in _metaTiles)
                metaTiles.Hide();

            Message.Text = "";
            if (Screen.FromControl(this).Bounds.Width < GridWidthBordered
                    || Screen.FromControl(this).Bounds.Height < GridHeightBordered)
                Message.Text = "The resolution is too low to show the tiles.";
            else foreach (var metaTile in _metaTiles)
                metaTile.Show();
        }

        private void OnLostFocus(object sender, EventArgs eventArgs)
        {
            Visible = false;
        }
        
        private void OnClick(object sender, EventArgs eventArgs)
        {
            if ((sender is System.Windows.Forms.Control control)
                    && (control.Tag is MetaTile metaTile))
                SelectMetaTile(metaTile);    
        }
        
        private void SelectMetaTile(MetaTile metaTile)
        {
            _cursor = metaTile.Index;
            foreach (var metaTileEntry in _metaTiles)
                metaTileEntry.Selected = metaTileEntry.Equals(metaTile);
        }
        
        private void OnKeyDown(object sender, KeyEventArgs keyEventArgs)
        {
            if (_cursor < 0
                    && (new Keys[] {Keys.Left, Keys.Back, Keys.Up}).Contains(keyEventArgs.KeyCode))
                _cursor = 0;
            if (_cursor < 0
                    && (new Keys[] {Keys.Right, Keys.Tab, Keys.Down}).Contains(keyEventArgs.KeyCode))
                _cursor = GridCount;

            switch (keyEventArgs.KeyCode)
            {
                case (Keys.Escape):
                    Visible = false;
                    break;
                case Keys.Left:
                case Keys.Back:
                    if (_cursor <= 0)
                        _cursor = GridCount;
                    _cursor--;
                    break;
                case Keys.Right:
                case Keys.Tab:
                    if (_cursor + 1 >= GridCount)
                        _cursor = -1;
                    _cursor++;
                    break;
                case Keys.Up:
                    if (_cursor < GridColumns
                            && _cursor > 0)
                        _cursor += GridCount - 1;
                    _cursor -= GridColumns;
                    if (_cursor < 0)
                        _cursor = GridCount - 1;
                    break;
                case Keys.Down:
                    if (_cursor >= GridCount -GridColumns
                            && _cursor < GridCount - 1)
                        _cursor = (_cursor - GridCount) + 1;
                    _cursor += GridColumns;
                    if (_cursor >= GridCount)
                        _cursor = 0;
                    break;
                case Keys.Enter:
                case Keys.Space:
                    break;
            }

            if (_cursor >= 0)
                SelectMetaTile(_metaTiles[_cursor]);
        }
        
        private class MetaTile
        {
            private readonly Settings.Tile Tile;
            private readonly Control Control;
            private readonly int ScanCode;
            private readonly string Symbol;
            
            internal readonly int Index;
            internal readonly MetaTileControls Controls;

            private bool _selected;

            private readonly Color BackgroundColor;
            private readonly Color BorderColor;
            private readonly Color ForegroundColor;
            private readonly Color HighlightColor;

            private readonly Image ActiveBorderImage;
            private readonly Image PassiveBorderImage;

            internal bool Selected
            {
                get => _selected;
                set
                {
                    if (_selected == value)
                        return;
                    _selected = value;
                    Controls.TitleLabel.BackgroundImage =
                        _selected ? ActiveBorderImage : PassiveBorderImage;
                }
            }

            private MetaTile(Control control, Settings.Tile tile)
            {
                // The index for the configuration starts user-friendly with 1,
                // but internally it is technically started with 0. Therefore
                // the index in the configuration is different!
                
                var index = tile.Index - 1;

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

                var imageSize = (int)Math.Floor(control.GridSize / 2d);
                var imageFile = Environment.ExpandEnvironmentVariables(tile.IconFile ?? "");
                var image = Utilities.Graphics.ImageOf(imageFile, tile.IconIndex);
                if (image != null)
                {
                    var scaleFactor = 1f;
                    scaleFactor = Math.Min(imageSize / (float)image.Height, scaleFactor);
                    scaleFactor = Math.Min(imageSize / (float)image.Width, scaleFactor);
                    if (scaleFactor < 1)
                        image = Utilities.Graphics.ResizeImage(image, (int)(image.Width *scaleFactor), (int)(image.Height *scaleFactor));        
                }

                Control = control;

                BackgroundColor = ColorTranslator.FromHtml(control.Settings.BackgroundColor);
                BorderColor     = ColorTranslator.FromHtml(control.Settings.BorderColor);
                ForegroundColor = ColorTranslator.FromHtml(control.Settings.ForegroundColor);
                HighlightColor  = ColorTranslator.FromHtml(control.Settings.HighlightColor);

                PassiveBorderImage = new Bitmap(Control.GridSize, Control.GridSize); 
                var passiveBorderImageGraphics = Graphics.FromImage(PassiveBorderImage);
                Utilities.Graphics.DrawRectangleRounded(passiveBorderImageGraphics, new Pen(new SolidBrush(BorderColor)),
                    new Rectangle(0, 0,Control.GridSize -1,Control.GridSize -1), 1);

                ActiveBorderImage = new Bitmap(Control.GridSize, Control.GridSize);
                var activeBorderImageGraphics = Graphics.FromImage(ActiveBorderImage);
                Utilities.Graphics.DrawRectangleRounded(activeBorderImageGraphics, new Pen(new SolidBrush(HighlightColor)),
                    new Rectangle(0, 0,Control.GridSize -1,Control.GridSize -1), 1);

                var iconPictureBox = new PictureBox()
                {
                    Width = imageSize,
                    Height = imageSize,
                    SizeMode = PictureBoxSizeMode.CenterImage,
                    Image = image,
                    Tag = this
                };
                iconPictureBox.Click += Control.OnClick;
                
                var titleLabel = new Label()
                {
                    Width = Control.GridSize,
                    Height = Control.GridSize,
                    Padding = new Padding(Control.GridPadding),
                    Font = new Font(SystemFonts.DefaultFont.FontFamily, Control.Settings.FontSize, FontStyle.Regular),
                    ForeColor = ForegroundColor,
                    TextAlign = ContentAlignment.BottomCenter,
                    BackgroundImage = PassiveBorderImage,
                    Text = tile.Title,
                    Tag = this
                };
                titleLabel.Click += Control.OnClick;
                
                var shortcutLabel = new Label()
                {
                    AutoSize = true,
                    Font = new Font(SystemFonts.DefaultFont.FontFamily, 9.75f, FontStyle.Regular),
                    ForeColor = HighlightColor,
                    TextAlign = ContentAlignment.TopLeft,
                    Text = Utilities.ScanCode.ToString(scanCode),
                    Tag = this
                };
                shortcutLabel.Click += Control.OnClick;
                
                Index = index;
                ScanCode = scanCode;
                Tile = tile;
                Symbol = Utilities.ScanCode.ToString(scanCode);
                Controls = MetaTileControls.Create(iconPictureBox, titleLabel, shortcutLabel);
            }

            internal static MetaTile Create(Control control, Settings.Tile tile)
            {
                return new MetaTile(control, tile);
            }
            
            internal class MetaTileControls
            {
                internal readonly PictureBox IconPictureBox;
                internal readonly Label TitleLabel;
                internal readonly Label ShortcutLabel;

                private MetaTileControls(PictureBox iconPictureBox, Label titleLabel, Label shortcutLabel)
                {
                    IconPictureBox = iconPictureBox;
                    TitleLabel = titleLabel;
                    ShortcutLabel = shortcutLabel;
                }

                internal static MetaTileControls Create(PictureBox iconPictureBox, Label titleLabel, Label shortcutLabel)
                {
                    return new MetaTileControls(iconPictureBox, titleLabel, shortcutLabel);
                }
            }

            private Point Location {
                get
                {
                    var rasterStartX = (Screen.FromControl(Control).Bounds.Width - Control.GridWidth) / 2;
                    var rasterStartY = (Screen.FromControl(Control).Bounds.Height - Control.GridHeight) / 2;
                    var tileRasterColumn = Index % Control.GridColumns;
                    var tileRasterRow = (int)Math.Floor((float)Index / Control.GridColumns);

                    var tileStartX = rasterStartX + ((tileRasterColumn * (Control.GridSize + Control.GridGap)));
                    var tileStartY = rasterStartY + ((tileRasterRow * (Control.GridSize + Control.GridGap)));

                    return new Point(tileStartX, tileStartY);
                }
            }

            internal void Show()
            {
                var location = Location;
                Controls.TitleLabel.Location = location;
                Controls.ShortcutLabel.Location = new Point(location.X + 5, location.Y + 5);
                Controls.IconPictureBox.Location = new Point(location.X + ((Control.GridSize -Controls.IconPictureBox.Width) / 2),
                        location.Y + 10);;

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