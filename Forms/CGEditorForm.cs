using AirDirector.Controls;
using AirDirector.Services;
using Microsoft.Win32;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace AirDirector.Forms
{
    public partial class CGEditorForm : Form
    {
        // ═══════════════════════════════════════════════════════════
        // LOWER THIRD SETTINGS (TRACK TITLE)
        // ═══════════════════════════════════════════════════════════
        private bool _lowerThirdEnabled = true;
        private string _lowerThirdPosition = "BottomLeft";
        private string _lowerThirdAnimation = "SlideLeft";
        private int _lowerThirdDelayStart = 2;
        private int _lowerThirdDuration = 8;
        private bool _lowerThirdShowAtEnd = false;
        private int _lowerThirdEndOffset = 10;
        private int _lowerThirdEndDuration = 5;
        private string _lowerThirdLayout = "SingleLine";
        private Color _lowerThirdBgColor = Color.FromArgb(200, 0, 0, 0);
        private Color _lowerThirdTextColor = Color.White;
        private Color _lowerThirdAccentColor = Color.FromArgb(255, 200, 0);
        private string _lowerThirdFontFamily = "Segoe UI";
        private int _lowerThirdTitleFontSize = 26;
        private int _lowerThirdArtistFontSize = 20;
        private int _lowerThirdMarginX = 50;
        private int _lowerThirdMarginY = 50;

        // ═══════════════════════════════════════════════════════════
        // PERSISTENT INFO BAR (TOP LEFT WITH PROGRESS)
        // ═══════════════════════════════════════════════════════════
        private bool _persistentInfoEnabled = true;
        private Color _progressBarColor = Color.FromArgb(255, 50, 50);
        private int _persistentInfoHideBeforeEnd = 15;
        private int _persistentInfoMarginX = 20;
        private int _persistentInfoMarginY = 20;
        private int _persistentInfoFontSize = 14;

        // ═══════════════════════════════════════════════════════════
        // LOGO SETTINGS
        // ═══════════════════════════════════════════════════════════
        private bool _logoEnabled = true;
        private string _logoPath = "";
        private string _logoPosition = "TopRight";
        private int _logoOpacity = 100;
        private int _logoSize = 150;
        private int _logoMargin = 20;

        // ═══════════════════════════════════════════════════════════
        // CLOCK SETTINGS
        // ═══════════════════════════════════════════════════════════
        private bool _clockEnabled = true;
        private bool _clockUnderLogo = true;
        private Color _clockColor = Color.White;
        private Color _clockBgColor = Color.FromArgb(150, 0, 0, 0);
        private string _clockFontFamily = "Segoe UI";
        private int _clockFontSize = 18;
        private bool _clockBgEnabled = true;

        // ═══════════════════════════════════════════════════════════
        // SPOT/ADV LABEL SETTINGS
        // ═══════════════════════════════════════════════════════════
        private bool _spotLabelEnabled = true;
        private string _spotLabelText = "ADVERTISING";
        private string _spotLabelPosition = "TopLeft";
        private Color _spotLabelBgColor = Color.FromArgb(200, 255, 0, 0);
        private Color _spotLabelTextColor = Color.White;
        private bool _spotLabelBgEnabled = true;
        private int _spotLabelFontSize = 14;
        private int _spotLabelMarginX = 20;
        private int _spotLabelMarginY = 20;

        // UI Controls
        private Panel _previewPanel;
        private TabControl _tabControl;
        private System.Windows.Forms.Timer _previewTimer;

        // Preview state
        private string _previewArtist = "Artist Name";
        private string _previewTitle = "Track Title";
        private float _previewAnimProgress = 1f;
        private float _previewProgressBar = 0.65f;
        private bool _previewShowPersistentInfo = true;
        private bool _previewShowSpotLabel = true;
        private DateTime _animStartTime = DateTime.MinValue;

        // Video dimensions for correct aspect ratio
        private int _videoWidth = 1920;
        private int _videoHeight = 1080;

        public CGEditorForm()
        {
            LoadVideoResolution();
            LoadSettings();
            InitializeComponent();
            StartPreviewTimer();
        }

        private void LoadVideoResolution()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AirDirector", false))
                {
                    if (key != null)
                    {
                        string res = key.GetValue("VideoResolution", "1920x1080")?.ToString() ?? "1920x1080";
                        string[] parts = res.Split('x');
                        if (parts.Length == 2)
                        {
                            int.TryParse(parts[0], out _videoWidth);
                            int.TryParse(parts[1], out _videoHeight);
                        }
                    }
                }
            }
            catch { }

            if (_videoWidth <= 0) _videoWidth = 1920;
            if (_videoHeight <= 0) _videoHeight = 1080;
        }

        private void InitializeComponent()
        {
            this.Text = "CG Editor - Character Generator";
            this.Size = new Size(1050, 820);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.White;

            // ═══════════════════════════════════════════════════════════
            // PREVIEW PANEL (16:9 aspect ratio)
            // ═══════════════════════════════════════════════════════════
            Label lblPreview = new Label
            {
                Text = "PREVIEW",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.Gray,
                Location = new Point(20, 10),
                AutoSize = true
            };
            this.Controls.Add(lblPreview);

            int previewWidth = 900;
            int previewHeight = (int)(previewWidth * ((float)_videoHeight / _videoWidth));

            _previewPanel = new DoubleBufferedPanel
            {
                Location = new Point(20, 35),
                Size = new Size(previewWidth, previewHeight),
                BackColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle
            };
            _previewPanel.Paint += PreviewPanel_Paint;
            this.Controls.Add(_previewPanel);

            // ═══════════════════════════════════════════════════════════
            // TAB CONTROL
            // ═══════════════════════════════════════════════════════════
            int tabTop = _previewPanel.Bottom + 15;

            _tabControl = new TabControl
            {
                Location = new Point(20, tabTop),
                Size = new Size(850, 200),
                Font = new Font("Segoe UI", 9)
            };
            this.Controls.Add(_tabControl);

            CreateLowerThirdTab();
            CreatePersistentInfoTab();
            CreateLogoTab();
            CreateClockTab();
            CreateSpotTab();

            // ═══════════════════════════════════════════════════════════
            // BUTTONS
            // ═══════════════════════════════════════════════════════════
            int btnX = 890;

            Button btnSaveApply = new Button
            {
                Text = "💾 Save & Apply",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Size = new Size(130, 45),
                Location = new Point(btnX, tabTop),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSaveApply.FlatAppearance.BorderSize = 0;
            btnSaveApply.Click += BtnSaveApply_Click;
            this.Controls.Add(btnSaveApply);

            Button btnCancel = new Button
            {
                Text = "Cancel",
                Font = new Font("Segoe UI", 10),
                Size = new Size(130, 40),
                Location = new Point(btnX, tabTop + 55),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);

            Button btnResetPreview = new Button
            {
                Text = "🔄 Reset Preview",
                Font = new Font("Segoe UI", 9),
                Size = new Size(130, 35),
                Location = new Point(btnX, tabTop + 105),
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnResetPreview.FlatAppearance.BorderSize = 0;
            btnResetPreview.Click += (s, e) =>
            {
                _previewAnimProgress = 0f;
                _animStartTime = DateTime.Now;
                _previewProgressBar = 0f;
            };
            this.Controls.Add(btnResetPreview);

            // Toggle Spot Label preview
            CheckBox chkPreviewSpot = new CheckBox
            {
                Text = "Preview ADV",
                Checked = false,
                Location = new Point(btnX, tabTop + 150),
                AutoSize = true,
                ForeColor = Color.White
            };
            chkPreviewSpot.CheckedChanged += (s, e) => { _previewShowSpotLabel = chkPreviewSpot.Checked; _previewPanel.Invalidate(); };
            this.Controls.Add(chkPreviewSpot);
        }

        // ═══════════════════════════════════════════════════════════
        // TAB:  LOWER THIRD (TRACK TITLE)
        // ═══════════════════════════════════════════════════════════
        private void CreateLowerThirdTab()
        {
            TabPage tab = new TabPage("📺 Track Title");
            tab.BackColor = Color.FromArgb(40, 40, 40);
            tab.ForeColor = Color.White;
            tab.Padding = new Padding(5);

            int y = 8;

            // Row 1: Enable, Position, Animation, Layout
            CheckBox chkEnabled = new CheckBox
            {
                Text = "Enabled",
                Checked = _lowerThirdEnabled,
                Location = new Point(10, y),
                AutoSize = true,
                ForeColor = Color.White
            };
            chkEnabled.CheckedChanged += (s, e) => { _lowerThirdEnabled = chkEnabled.Checked; _previewPanel.Invalidate(); };
            tab.Controls.Add(chkEnabled);

            AddLabelAndCombo(tab, "Position:", 100, y, 55, 95,
                new[] { "Bottom Left", "Bottom Center", "Bottom Right" },
                _lowerThirdPosition == "BottomLeft" ? 0 : (_lowerThirdPosition == "BottomCenter" ? 1 : 2),
                (idx) => { _lowerThirdPosition = idx == 0 ? "BottomLeft" : (idx == 1 ? "BottomCenter" : "BottomRight"); _previewPanel.Invalidate(); });

            AddLabelAndCombo(tab, "Animation:", 310, y, 65, 90,
                new[] { "Slide Left", "Slide Right", "Slide Up", "Fade In", "Zoom In" },
                GetAnimationIndex(_lowerThirdAnimation),
                (idx) => { _lowerThirdAnimation = GetAnimationName(idx); _previewAnimProgress = 0f; _animStartTime = DateTime.Now; });

            AddLabelAndCombo(tab, "Layout:", 520, y, 45, 100,
                new[] { "Single Line", "Title Above", "Artist Above" },
                _lowerThirdLayout == "SingleLine" ? 0 : (_lowerThirdLayout == "TitleAbove" ? 1 : 2),
                (idx) => { _lowerThirdLayout = idx == 0 ? "SingleLine" : (idx == 1 ? "TitleAbove" : "ArtistAbove"); _previewPanel.Invalidate(); });

            y += 30;

            // Row 2: Delay, Duration, Margins
            AddLabelAndNumeric(tab, "Delay (s):", 10, y, 60, 50, 0, 30, _lowerThirdDelayStart, (v) => _lowerThirdDelayStart = v);
            AddLabelAndNumeric(tab, "Duration (s):", 140, y, 75, 50, 1, 60, _lowerThirdDuration, (v) => _lowerThirdDuration = v);
            AddLabelAndNumeric(tab, "Margin X:", 290, y, 60, 50, 0, 500, _lowerThirdMarginX, (v) => { _lowerThirdMarginX = v; _previewPanel.Invalidate(); });
            AddLabelAndNumeric(tab, "Margin Y:", 420, y, 60, 50, 0, 500, _lowerThirdMarginY, (v) => { _lowerThirdMarginY = v; _previewPanel.Invalidate(); });

            y += 30;

            // Row 3: Font sizes and font family
            AddLabelAndNumeric(tab, "Title Size:", 10, y, 65, 50, 12, 72, _lowerThirdTitleFontSize, (v) => { _lowerThirdTitleFontSize = v; _previewPanel.Invalidate(); });
            AddLabelAndNumeric(tab, "Artist Size:", 145, y, 65, 50, 12, 72, _lowerThirdArtistFontSize, (v) => { _lowerThirdArtistFontSize = v; _previewPanel.Invalidate(); });

            Label lblFont = new Label { Text = "Font:", Location = new Point(290, y), AutoSize = true, ForeColor = Color.LightGray };
            tab.Controls.Add(lblFont);

            ComboBox cmbFont = new ComboBox
            {
                Location = new Point(330, y - 3),
                Size = new Size(150, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            foreach (FontFamily ff in FontFamily.Families)
            {
                cmbFont.Items.Add(ff.Name);
            }
            cmbFont.SelectedItem = _lowerThirdFontFamily;
            if (cmbFont.SelectedIndex < 0 && cmbFont.Items.Count > 0) cmbFont.SelectedIndex = 0;
            cmbFont.SelectedIndexChanged += (s, e) => { _lowerThirdFontFamily = cmbFont.SelectedItem?.ToString() ?? "Segoe UI"; _previewPanel.Invalidate(); };
            tab.Controls.Add(cmbFont);

            y += 30;

            // Row 4: Colors
            AddLabelAndColorPicker(tab, "Background:", 10, y, 75, _lowerThirdBgColor, (c) => { _lowerThirdBgColor = c; _previewPanel.Invalidate(); }, true);
            AddLabelAndColorPicker(tab, "Text:", 150, y, 35, _lowerThirdTextColor, (c) => { _lowerThirdTextColor = c; _previewPanel.Invalidate(); });
            AddLabelAndColorPicker(tab, "Accent Bar:", 260, y, 70, _lowerThirdAccentColor, (c) => { _lowerThirdAccentColor = c; _previewPanel.Invalidate(); });

            y += 30;

            // Row 5: Show at end options
            CheckBox chkShowEnd = new CheckBox
            {
                Text = "Show also at track end",
                Checked = _lowerThirdShowAtEnd,
                Location = new Point(10, y),
                AutoSize = true,
                ForeColor = Color.White
            };
            chkShowEnd.CheckedChanged += (s, e) => _lowerThirdShowAtEnd = chkShowEnd.Checked;
            tab.Controls.Add(chkShowEnd);

            AddLabelAndNumeric(tab, "Sec.  before end:", 200, y, 100, 50, 5, 60, _lowerThirdEndOffset, (v) => _lowerThirdEndOffset = v);
            AddLabelAndNumeric(tab, "Duration:", 380, y, 55, 50, 1, 30, _lowerThirdEndDuration, (v) => _lowerThirdEndDuration = v);

            _tabControl.TabPages.Add(tab);
        }

        // ═══════════════════════════════════════════════════════════
        // TAB:  PERSISTENT INFO BAR
        // ═══════════════════════════════════════════════════════════
        private void CreatePersistentInfoTab()
        {
            TabPage tab = new TabPage("📊 Persistent Info");
            tab.BackColor = Color.FromArgb(40, 40, 40);
            tab.ForeColor = Color.White;
            tab.Padding = new Padding(5);

            int y = 12;

            CheckBox chkEnabled = new CheckBox
            {
                Text = "Show persistent info bar (Artist - Title with progress bar) after initial display",
                Checked = _persistentInfoEnabled,
                Location = new Point(10, y),
                AutoSize = true,
                ForeColor = Color.White
            };
            chkEnabled.CheckedChanged += (s, e) => { _persistentInfoEnabled = chkEnabled.Checked; _previewPanel.Invalidate(); };
            tab.Controls.Add(chkEnabled);

            y += 35;

            // Row 2: Colors and size
            AddLabelAndColorPicker(tab, "Progress Bar Color:", 10, y, 115, _progressBarColor, (c) => { _progressBarColor = c; _previewPanel.Invalidate(); });
            AddLabelAndNumeric(tab, "Font Size:", 220, y, 60, 50, 10, 36, _persistentInfoFontSize, (v) => { _persistentInfoFontSize = v; _previewPanel.Invalidate(); });

            y += 35;

            // Row 3: Margins
            AddLabelAndNumeric(tab, "Margin X (from left):", 10, y, 120, 60, 0, 500, _persistentInfoMarginX, (v) => { _persistentInfoMarginX = v; _previewPanel.Invalidate(); });
            AddLabelAndNumeric(tab, "Margin Y (from top):", 250, y, 120, 60, 0, 500, _persistentInfoMarginY, (v) => { _persistentInfoMarginY = v; _previewPanel.Invalidate(); });

            y += 35;

            // Row 4: Hide before end
            AddLabelAndNumeric(tab, "Hide before track end (seconds):", 10, y, 195, 60, 0, 60, _persistentInfoHideBeforeEnd, (v) => _persistentInfoHideBeforeEnd = v);

            y += 35;

            Label lblNote = new Label
            {
                Text = "ℹ️ This bar appears in top-left corner after the main title animation finishes,\n" +
                       "    and stays visible until N seconds before track end (or before end title appears).",
                Location = new Point(10, y),
                Size = new Size(700, 35),
                ForeColor = Color.Gray
            };
            tab.Controls.Add(lblNote);

            _tabControl.TabPages.Add(tab);
        }

        // ═══════════════════════════════════════════════════════════
        // TAB: LOGO
        // ═══════════════════════════════════════════════════════════
        private void CreateLogoTab()
        {
            TabPage tab = new TabPage("🏷️ Logo");
            tab.BackColor = Color.FromArgb(40, 40, 40);
            tab.ForeColor = Color.White;
            tab.Padding = new Padding(5);

            int y = 12;

            // Row 1: Enable and browse
            CheckBox chkEnabled = new CheckBox
            {
                Text = "Show Logo",
                Checked = _logoEnabled,
                Location = new Point(10, y),
                AutoSize = true,
                ForeColor = Color.White
            };
            chkEnabled.CheckedChanged += (s, e) => { _logoEnabled = chkEnabled.Checked; _previewPanel.Invalidate(); };
            tab.Controls.Add(chkEnabled);

            Button btnBrowse = new Button
            {
                Text = "📁 Browse...",
                Location = new Point(120, y - 3),
                Size = new Size(90, 25),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnBrowse.FlatAppearance.BorderSize = 0;
            btnBrowse.Click += BtnBrowseLogo_Click;
            tab.Controls.Add(btnBrowse);

            Label lblPath = new Label
            {
                Text = string.IsNullOrEmpty(_logoPath) ? "(no logo selected)" : Path.GetFileName(_logoPath),
                Location = new Point(220, y),
                Size = new Size(250, 20),
                ForeColor = Color.LightGray,
                Name = "lblLogoPath"
            };
            tab.Controls.Add(lblPath);

            y += 35;

            // Row 2: Position, Size, Margin
            AddLabelAndCombo(tab, "Position:", 10, y, 55, 110,
                new[] { "Top Left", "Top Right", "Bottom Left", "Bottom Right" },
                GetLogoPositionIndex(_logoPosition),
                (idx) => { _logoPosition = GetLogoPositionName(idx); _previewPanel.Invalidate(); });

            AddLabelAndNumeric(tab, "Size:", 200, y, 35, 60, 50, 400, _logoSize, (v) => { _logoSize = v; _previewPanel.Invalidate(); });
            AddLabelAndNumeric(tab, "Margin:", 320, y, 50, 60, 0, 200, _logoMargin, (v) => { _logoMargin = v; _previewPanel.Invalidate(); });

            y += 35;

            // Row 3: Opacity
            Label lblOpacity = new Label { Text = "Opacity:", Location = new Point(10, y + 5), AutoSize = true, ForeColor = Color.LightGray };
            tab.Controls.Add(lblOpacity);

            TrackBar trkOpacity = new TrackBar
            {
                Location = new Point(70, y),
                Size = new Size(200, 30),
                Minimum = 0,
                Maximum = 100,
                Value = _logoOpacity,
                TickFrequency = 25
            };
            trkOpacity.ValueChanged += (s, e) => { _logoOpacity = trkOpacity.Value; _previewPanel.Invalidate(); };
            tab.Controls.Add(trkOpacity);

            Label lblOpacityValue = new Label
            {
                Text = $"{_logoOpacity}%",
                Location = new Point(280, y + 5),
                AutoSize = true,
                ForeColor = Color.White
            };
            trkOpacity.ValueChanged += (s, e) => lblOpacityValue.Text = $"{trkOpacity.Value}%";
            tab.Controls.Add(lblOpacityValue);

            _tabControl.TabPages.Add(tab);
        }

        // ═══════════════════════════════════════════════════════════
        // TAB:  CLOCK
        // ═══════════════════════════════════════════════════════════
        private void CreateClockTab()
        {
            TabPage tab = new TabPage("🕐 Clock");
            tab.BackColor = Color.FromArgb(40, 40, 40);
            tab.ForeColor = Color.White;
            tab.Padding = new Padding(5);

            int y = 12;

            // Row 1: Enable and position
            CheckBox chkEnabled = new CheckBox
            {
                Text = "Show Clock (HH:mm)",
                Checked = _clockEnabled,
                Location = new Point(10, y),
                AutoSize = true,
                ForeColor = Color.White
            };
            chkEnabled.CheckedChanged += (s, e) => { _clockEnabled = chkEnabled.Checked; _previewPanel.Invalidate(); };
            tab.Controls.Add(chkEnabled);

            CheckBox chkUnderLogo = new CheckBox
            {
                Text = "Position under Logo",
                Checked = _clockUnderLogo,
                Location = new Point(180, y),
                AutoSize = true,
                ForeColor = Color.White
            };
            chkUnderLogo.CheckedChanged += (s, e) => { _clockUnderLogo = chkUnderLogo.Checked; _previewPanel.Invalidate(); };
            tab.Controls.Add(chkUnderLogo);

            y += 35;

            // Row 2: Colors and size
            AddLabelAndColorPicker(tab, "Text Color:", 10, y, 70, _clockColor, (c) => { _clockColor = c; _previewPanel.Invalidate(); });

            CheckBox chkBgEnabled = new CheckBox
            {
                Text = "Background:",
                Checked = _clockBgEnabled,
                Location = new Point(160, y),
                AutoSize = true,
                ForeColor = Color.White
            };
            chkBgEnabled.CheckedChanged += (s, e) => { _clockBgEnabled = chkBgEnabled.Checked; _previewPanel.Invalidate(); };
            tab.Controls.Add(chkBgEnabled);

            AddLabelAndColorPicker(tab, "", 270, y, 0, _clockBgColor, (c) => { _clockBgColor = c; _previewPanel.Invalidate(); }, true);

            AddLabelAndNumeric(tab, "Font Size:", 350, y, 65, 50, 12, 48, _clockFontSize, (v) => { _clockFontSize = v; _previewPanel.Invalidate(); });

            _tabControl.TabPages.Add(tab);
        }

        // ═══════════════════════════════════════════════════════════
        // TAB:  SPOT/ADVERTISING
        // ═══════════════════════════════════════════════════════════
        private void CreateSpotTab()
        {
            TabPage tab = new TabPage("📢 Advertising");
            tab.BackColor = Color.FromArgb(40, 40, 40);
            tab.ForeColor = Color.White;
            tab.Padding = new Padding(5);

            int y = 12;

            // Row 1: Enable
            CheckBox chkEnabled = new CheckBox
            {
                Text = "Show label during Spot/Advertising playback",
                Checked = _spotLabelEnabled,
                Location = new Point(10, y),
                AutoSize = true,
                ForeColor = Color.White
            };
            chkEnabled.CheckedChanged += (s, e) => { _spotLabelEnabled = chkEnabled.Checked; _previewPanel.Invalidate(); };
            tab.Controls.Add(chkEnabled);

            y += 30;

            // Row 2: Text and position
            Label lblText = new Label { Text = "Label Text:", Location = new Point(10, y), AutoSize = true, ForeColor = Color.LightGray };
            tab.Controls.Add(lblText);

            TextBox txtLabel = new TextBox
            {
                Text = _spotLabelText,
                Location = new Point(80, y - 3),
                Size = new Size(150, 25)
            };
            txtLabel.TextChanged += (s, e) => { _spotLabelText = txtLabel.Text; _previewPanel.Invalidate(); };
            tab.Controls.Add(txtLabel);

            AddLabelAndCombo(tab, "Position:", 260, y, 55, 100,
                new[] { "Top Left", "Top Center", "Top Right" },
                _spotLabelPosition == "TopLeft" ? 0 : (_spotLabelPosition == "TopCenter" ? 1 : 2),
                (idx) => { _spotLabelPosition = idx == 0 ? "TopLeft" : (idx == 1 ? "TopCenter" : "TopRight"); _previewPanel.Invalidate(); });

            AddLabelAndNumeric(tab, "Font Size:", 450, y, 60, 50, 8, 48, _spotLabelFontSize, (v) => { _spotLabelFontSize = v; _previewPanel.Invalidate(); });

            y += 30;

            // Row 3: Margins
            AddLabelAndNumeric(tab, "Margin X:", 10, y, 60, 60, 0, 500, _spotLabelMarginX, (v) => { _spotLabelMarginX = v; _previewPanel.Invalidate(); });
            AddLabelAndNumeric(tab, "Margin Y:", 160, y, 60, 60, 0, 500, _spotLabelMarginY, (v) => { _spotLabelMarginY = v; _previewPanel.Invalidate(); });

            y += 30;

            // Row 4: Colors
            AddLabelAndColorPicker(tab, "Text Color:", 10, y, 70, _spotLabelTextColor, (c) => { _spotLabelTextColor = c; _previewPanel.Invalidate(); });

            CheckBox chkBgEnabled = new CheckBox
            {
                Text = "Background:",
                Checked = _spotLabelBgEnabled,
                Location = new Point(160, y),
                AutoSize = true,
                ForeColor = Color.White
            };
            chkBgEnabled.CheckedChanged += (s, e) => { _spotLabelBgEnabled = chkBgEnabled.Checked; _previewPanel.Invalidate(); };
            tab.Controls.Add(chkBgEnabled);

            AddLabelAndColorPicker(tab, "", 270, y, 0, _spotLabelBgColor, (c) => { _spotLabelBgColor = c; _previewPanel.Invalidate(); }, true);

            _tabControl.TabPages.Add(tab);
        }

        // ═══════════════════════════════════════════════════════════
        // HELPER:  Add Label + ComboBox
        // ═══════════════════════════════════════════════════════════
        private void AddLabelAndCombo(TabPage tab, string labelText, int x, int y, int labelWidth, int comboWidth, string[] items, int selectedIndex, Action<int> onChange)
        {
            if (!string.IsNullOrEmpty(labelText))
            {
                Label lbl = new Label { Text = labelText, Location = new Point(x, y), AutoSize = true, ForeColor = Color.LightGray };
                tab.Controls.Add(lbl);
            }

            ComboBox cmb = new ComboBox
            {
                Location = new Point(x + labelWidth, y - 3),
                Size = new Size(comboWidth, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmb.Items.AddRange(items);
            if (selectedIndex >= 0 && selectedIndex < cmb.Items.Count)
                cmb.SelectedIndex = selectedIndex;
            cmb.SelectedIndexChanged += (s, e) => { onChange(cmb.SelectedIndex); _previewPanel.Invalidate(); };
            tab.Controls.Add(cmb);
        }

        // ═══════════════════════════════════════════════════════════
        // HELPER: Add Label + NumericUpDown
        // ═══════════════════════════════════════════════════════════
        private void AddLabelAndNumeric(TabPage tab, string labelText, int x, int y, int labelWidth, int numWidth, int min, int max, int value, Action<int> onChange)
        {
            Label lbl = new Label { Text = labelText, Location = new Point(x, y), AutoSize = true, ForeColor = Color.LightGray };
            tab.Controls.Add(lbl);

            NumericUpDown num = new NumericUpDown
            {
                Location = new Point(x + labelWidth, y - 3),
                Size = new Size(numWidth, 25),
                Minimum = min,
                Maximum = max,
                Value = Math.Max(min, Math.Min(max, value))
            };
            num.ValueChanged += (s, e) => onChange((int)num.Value);
            tab.Controls.Add(num);
        }

        // ═══════════════════════════════════════════════════════════
        // HELPER:  Add Label + Color Picker
        // ═══════════════════════════════════════════════════════════
        private void AddLabelAndColorPicker(TabPage tab, string labelText, int x, int y, int labelWidth, Color color, Action<Color> onChange, bool includeAlpha = false)
        {
            if (!string.IsNullOrEmpty(labelText))
            {
                Label lbl = new Label { Text = labelText, Location = new Point(x, y), AutoSize = true, ForeColor = Color.LightGray };
                tab.Controls.Add(lbl);
            }

            Panel pnl = new Panel
            {
                Location = new Point(x + labelWidth, y - 2),
                Size = new Size(30, 20),
                BackColor = Color.FromArgb(color.R, color.G, color.B),
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand,
                Tag = color
            };
            pnl.Click += (s, e) =>
            {
                using (ColorDialog cd = new ColorDialog())
                {
                    cd.Color = pnl.BackColor;
                    cd.FullOpen = true;
                    if (cd.ShowDialog() == DialogResult.OK)
                    {
                        Color oldColor = (Color)pnl.Tag;
                        Color newColor = includeAlpha ? Color.FromArgb(oldColor.A, cd.Color.R, cd.Color.G, cd.Color.B) : cd.Color;
                        pnl.BackColor = cd.Color;
                        pnl.Tag = newColor;
                        onChange(newColor);
                    }
                }
            };
            tab.Controls.Add(pnl);
        }

        // ═══════════════════════════════════════════════════════════
        // PREVIEW
        // ═══════════════════════════════════════════════════════════
        private void StartPreviewTimer()
        {
            _previewTimer = new System.Windows.Forms.Timer { Interval = 33 };
            _previewTimer.Tick += (s, e) =>
            {
                bool needsRedraw = false;

                if (_previewAnimProgress < 1f)
                {
                    _previewAnimProgress += 0.04f;
                    if (_previewAnimProgress > 1f) _previewAnimProgress = 1f;
                    needsRedraw = true;
                }

                if (_previewProgressBar < 1f && _persistentInfoEnabled)
                {
                    _previewProgressBar += 0.002f;
                    if (_previewProgressBar > 1f) _previewProgressBar = 1f;
                    needsRedraw = true;
                }

                if (needsRedraw)
                    _previewPanel.Invalidate();
            };
            _previewTimer.Start();
        }

        private void PreviewPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int w = _previewPanel.Width;
            int h = _previewPanel.Height;

            // Simulated video background
            using (LinearGradientBrush bg = new LinearGradientBrush(
                new Rectangle(0, 0, w, h),
                Color.FromArgb(30, 30, 50),
                Color.FromArgb(50, 30, 70),
                45f))
            {
                g.FillRectangle(bg, 0, 0, w, h);
            }

            // Draw elements
            if (_logoEnabled) DrawLogo(g, w, h);
            if (_clockEnabled) DrawClock(g, w, h);
            if (_spotLabelEnabled && _previewShowSpotLabel) DrawSpotLabel(g, w, h);
            if (_persistentInfoEnabled && _previewAnimProgress >= 1f && !_previewShowSpotLabel) DrawPersistentInfo(g, w, h);
            if (_lowerThirdEnabled && !_previewShowSpotLabel) DrawLowerThird(g, w, h, _previewAnimProgress);
        }

        private float GetScale(int w) => (float)w / _videoWidth;

        private void DrawLogo(Graphics g, int w, int h)
        {
            float scale = GetScale(w);
            int logoW = (int)(_logoSize * scale);
            int logoH = (int)(logoW * 0.6f);
            int margin = (int)(_logoMargin * scale);

            int x = 0, y = 0;
            switch (_logoPosition)
            {
                case "TopLeft": x = margin; y = margin; break;
                case "TopRight": x = w - logoW - margin; y = margin; break;
                case "BottomLeft": x = margin; y = h - logoH - margin; break;
                case "BottomRight": x = w - logoW - margin; y = h - logoH - margin; break;
            }

            using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb((int)(_logoOpacity * 2.55f), 255, 255, 255)))
            {
                g.FillRectangle(bgBrush, x, y, logoW, logoH);
            }

            using (Font f = new Font("Segoe UI", 12 * scale, FontStyle.Bold))
            using (SolidBrush tb = new SolidBrush(Color.FromArgb((int)(_logoOpacity * 2.55f), 50, 50, 50)))
            {
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("LOGO", f, tb, new RectangleF(x, y, logoW, logoH), sf);
            }
        }

        private void DrawClock(Graphics g, int w, int h)
        {
            float scale = GetScale(w);
            string time = DateTime.Now.ToString("HH:mm");

            using (Font f = new Font(_clockFontFamily, _clockFontSize * scale, FontStyle.Bold))
            {
                SizeF size = g.MeasureString(time, f);
                int margin = (int)(_logoMargin * scale);
                int x = 0, y = 0;

                if (_clockUnderLogo && _logoEnabled)
                {
                    int logoW = (int)(_logoSize * scale);
                    int logoH = (int)(logoW * 0.6f);

                    switch (_logoPosition)
                    {
                        case "TopRight":
                            x = w - margin - logoW + (logoW - (int)size.Width) / 2;
                            y = margin + logoH + 5;
                            break;
                        case "TopLeft":
                            x = margin + (logoW - (int)size.Width) / 2;
                            y = margin + logoH + 5;
                            break;
                        default:
                            x = w - (int)size.Width - margin;
                            y = margin;
                            break;
                    }
                }
                else
                {
                    x = w - (int)size.Width - 20;
                    y = 20;
                }

                if (_clockBgEnabled)
                {
                    using (SolidBrush bgBrush = new SolidBrush(_clockBgColor))
                    {
                        g.FillRectangle(bgBrush, x - 8, y - 4, size.Width + 16, size.Height + 8);
                    }
                }

                using (SolidBrush textBrush = new SolidBrush(_clockColor))
                {
                    g.DrawString(time, f, textBrush, x, y);
                }
            }
        }

        private void DrawSpotLabel(Graphics g, int w, int h)
        {
            float scale = GetScale(w);

            using (Font f = new Font("Segoe UI", _spotLabelFontSize * scale, FontStyle.Bold))
            {
                SizeF size = g.MeasureString(_spotLabelText, f);
                int padding = (int)(10 * scale);
                int boxW = (int)size.Width + padding * 2;
                int boxH = (int)size.Height + padding;

                int marginX = (int)(_spotLabelMarginX * scale);
                int marginY = (int)(_spotLabelMarginY * scale);

                int x = marginX;
                int y = marginY;

                switch (_spotLabelPosition)
                {
                    case "TopLeft":
                        x = marginX;
                        y = marginY;
                        break;
                    case "TopCenter":
                        x = (w - boxW) / 2;
                        y = marginY;
                        break;
                    case "TopRight":
                        x = w - boxW - marginX;
                        y = marginY;
                        break;
                }

                // Background
                if (_spotLabelBgEnabled)
                {
                    using (SolidBrush bgBrush = new SolidBrush(_spotLabelBgColor))
                    {
                        g.FillRectangle(bgBrush, x, y, boxW, boxH);
                    }
                }

                // Text
                using (SolidBrush textBrush = new SolidBrush(_spotLabelTextColor))
                {
                    g.DrawString(_spotLabelText, f, textBrush, x + padding, y + padding / 2);
                }
            }
        }

        private void DrawPersistentInfo(Graphics g, int w, int h)
        {
            float scale = GetScale(w);
            string text = $"{_previewArtist} - {_previewTitle}";

            using (Font f = new Font(_lowerThirdFontFamily, _persistentInfoFontSize * scale, FontStyle.Bold))
            {
                SizeF size = g.MeasureString(text, f);
                int padding = (int)(10 * scale);
                int barHeight = (int)(4 * scale);
                int x = (int)(_persistentInfoMarginX * scale);
                int y = (int)(_persistentInfoMarginY * scale);
                int boxW = (int)size.Width + padding * 2;
                int boxH = (int)size.Height + padding + barHeight + 4;

                // Background
                using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
                {
                    g.FillRectangle(bgBrush, x, y, boxW, boxH);
                }

                // Text
                using (SolidBrush textBrush = new SolidBrush(Color.White))
                {
                    g.DrawString(text, f, textBrush, x + padding, y + padding / 2);
                }

                // Progress bar background
                int barY = y + (int)size.Height + padding / 2 + 2;
                using (SolidBrush barBgBrush = new SolidBrush(Color.FromArgb(100, 255, 255, 255)))
                {
                    g.FillRectangle(barBgBrush, x + padding, barY, boxW - padding * 2, barHeight);
                }

                // Progress bar fill
                using (SolidBrush barFillBrush = new SolidBrush(_progressBarColor))
                {
                    int fillW = (int)((boxW - padding * 2) * _previewProgressBar);
                    g.FillRectangle(barFillBrush, x + padding, barY, fillW, barHeight);
                }
            }
        }

        private void DrawLowerThird(Graphics g, int w, int h, float progress)
        {
            float scale = GetScale(w);

            string titleText = _previewTitle;
            string artistText = _previewArtist;

            using (Font titleFont = new Font(_lowerThirdFontFamily, _lowerThirdTitleFontSize * scale, FontStyle.Bold))
            using (Font artistFont = new Font(_lowerThirdFontFamily, _lowerThirdArtistFontSize * scale, FontStyle.Regular))
            {
                SizeF titleSize = g.MeasureString(titleText, titleFont);
                SizeF artistSize = g.MeasureString(artistText, artistFont);

                int padding = (int)(20 * scale);
                int accentWidth = (int)(6 * scale);
                int marginX = (int)(_lowerThirdMarginX * scale);
                int marginY = (int)(_lowerThirdMarginY * scale);

                int boxW, boxH;
                float textY1, textY2;

                if (_lowerThirdLayout == "SingleLine")
                {
                    string fullText = $"{artistText} - {titleText}";
                    SizeF fullSize = g.MeasureString(fullText, titleFont);
                    boxW = (int)fullSize.Width + padding * 2 + accentWidth;
                    boxH = (int)fullSize.Height + padding;
                    textY1 = padding / 2;
                    textY2 = 0;
                }
                else
                {
                    boxW = (int)Math.Max(titleSize.Width, artistSize.Width) + padding * 2 + accentWidth;
                    boxH = (int)(titleSize.Height + artistSize.Height) + padding;
                    textY1 = padding / 2;
                    textY2 = padding / 2 + titleSize.Height;
                }

                int baseX = 0, baseY = h - boxH - marginY;

                switch (_lowerThirdPosition)
                {
                    case "BottomLeft": baseX = marginX; break;
                    case "BottomCenter": baseX = (w - boxW) / 2; break;
                    case "BottomRight": baseX = w - boxW - marginX; break;
                }

                // Apply animation
                float drawX = baseX, drawY = baseY;
                float alpha = 1f;

                switch (_lowerThirdAnimation)
                {
                    case "SlideLeft":
                        drawX = baseX - (boxW + baseX) * (1f - progress);
                        break;
                    case "SlideRight":
                        drawX = baseX + (w - baseX) * (1f - progress);
                        break;
                    case "SlideUp":
                        drawY = baseY + (h - baseY) * (1f - progress);
                        break;
                    case "FadeIn":
                        alpha = progress;
                        break;
                    case "ZoomIn":
                        float zoomScale = 0.3f + progress * 0.7f;
                        drawX = baseX + boxW * (1f - zoomScale) / 2;
                        drawY = baseY + boxH * (1f - zoomScale) / 2;
                        boxW = (int)(boxW * zoomScale);
                        boxH = (int)(boxH * zoomScale);
                        alpha = progress;
                        break;
                }

                // Background
                using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(
                    (int)(alpha * _lowerThirdBgColor.A),
                    _lowerThirdBgColor.R, _lowerThirdBgColor.G, _lowerThirdBgColor.B)))
                {
                    g.FillRectangle(bgBrush, drawX, drawY, boxW, boxH);
                }

                // Accent bar
                using (SolidBrush accentBrush = new SolidBrush(Color.FromArgb((int)(alpha * 255), _lowerThirdAccentColor)))
                {
                    g.FillRectangle(accentBrush, drawX, drawY, accentWidth, boxH);
                }

                // Text
                using (SolidBrush textBrush = new SolidBrush(Color.FromArgb((int)(alpha * 255), _lowerThirdTextColor)))
                {
                    if (_lowerThirdLayout == "SingleLine")
                    {
                        string fullText = $"{artistText} - {titleText}";
                        g.DrawString(fullText, titleFont, textBrush, drawX + padding + accentWidth, drawY + textY1);
                    }
                    else if (_lowerThirdLayout == "TitleAbove")
                    {
                        g.DrawString(titleText, titleFont, textBrush, drawX + padding + accentWidth, drawY + textY1);
                        g.DrawString(artistText, artistFont, textBrush, drawX + padding + accentWidth, drawY + textY2);
                    }
                    else
                    {
                        g.DrawString(artistText, artistFont, textBrush, drawX + padding + accentWidth, drawY + textY1);
                        g.DrawString(titleText, titleFont, textBrush, drawX + padding + accentWidth, drawY + textY2);
                    }
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════
        private int GetAnimationIndex(string name)
        {
            switch (name)
            {
                case "SlideLeft": return 0;
                case "SlideRight": return 1;
                case "SlideUp": return 2;
                case "FadeIn": return 3;
                case "ZoomIn": return 4;
                default: return 0;
            }
        }

        private string GetAnimationName(int index)
        {
            switch (index)
            {
                case 0: return "SlideLeft";
                case 1: return "SlideRight";
                case 2: return "SlideUp";
                case 3: return "FadeIn";
                case 4: return "ZoomIn";
                default: return "SlideLeft";
            }
        }

        private int GetLogoPositionIndex(string pos)
        {
            switch (pos)
            {
                case "TopLeft": return 0;
                case "TopRight": return 1;
                case "BottomLeft": return 2;
                case "BottomRight": return 3;
                default: return 1;
            }
        }

        private string GetLogoPositionName(int index)
        {
            switch (index)
            {
                case 0: return "TopLeft";
                case 1: return "TopRight";
                case 2: return "BottomLeft";
                case 3: return "BottomRight";
                default: return "TopRight";
            }
        }

        private void BtnBrowseLogo_Click(object sender, EventArgs e)
        {
            using (System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog())
            {
                ofd.Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif";
                ofd.Title = "Select Logo";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    _logoPath = ofd.FileName;

                    foreach (Control c in _tabControl.TabPages[2].Controls)
                    {
                        if (c.Name == "lblLogoPath")
                        {
                            ((Label)c).Text = Path.GetFileName(_logoPath);
                            break;
                        }
                    }

                    _previewPanel.Invalidate();
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        // LOAD/SAVE SETTINGS
        // ═══════════════════════════════════════════════════════════
        private void LoadSettings()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AirDirector\CG", false))
                {
                    if (key == null) return;

                    _lowerThirdEnabled = GetRegBool(key, "LowerThirdEnabled", true);
                    _lowerThirdPosition = GetRegString(key, "LowerThirdPosition", "BottomLeft");
                    _lowerThirdAnimation = GetRegString(key, "LowerThirdAnimation", "SlideLeft");
                    _lowerThirdDelayStart = GetRegInt(key, "LowerThirdDelayStart", 2);
                    _lowerThirdDuration = GetRegInt(key, "LowerThirdDuration", 8);
                    _lowerThirdShowAtEnd = GetRegBool(key, "LowerThirdShowAtEnd", false);
                    _lowerThirdEndOffset = GetRegInt(key, "LowerThirdEndOffset", 10);
                    _lowerThirdEndDuration = GetRegInt(key, "LowerThirdEndDuration", 5);
                    _lowerThirdLayout = GetRegString(key, "LowerThirdLayout", "SingleLine");
                    _lowerThirdBgColor = Color.FromArgb(GetRegInt(key, "LowerThirdBgColor", Color.FromArgb(200, 0, 0, 0).ToArgb()));
                    _lowerThirdTextColor = Color.FromArgb(GetRegInt(key, "LowerThirdTextColor", Color.White.ToArgb()));
                    _lowerThirdAccentColor = Color.FromArgb(GetRegInt(key, "LowerThirdAccentColor", Color.FromArgb(255, 200, 0).ToArgb()));
                    _lowerThirdFontFamily = GetRegString(key, "LowerThirdFontFamily", "Segoe UI");
                    _lowerThirdTitleFontSize = GetRegInt(key, "LowerThirdTitleFontSize", 26);
                    _lowerThirdArtistFontSize = GetRegInt(key, "LowerThirdArtistFontSize", 20);
                    _lowerThirdMarginX = GetRegInt(key, "LowerThirdMarginX", 50);
                    _lowerThirdMarginY = GetRegInt(key, "LowerThirdMarginY", 50);

                    _persistentInfoEnabled = GetRegBool(key, "PersistentInfoEnabled", true);
                    _progressBarColor = Color.FromArgb(GetRegInt(key, "ProgressBarColor", Color.FromArgb(255, 50, 50).ToArgb()));
                    _persistentInfoHideBeforeEnd = GetRegInt(key, "PersistentInfoHideBeforeEnd", 15);
                    _persistentInfoMarginX = GetRegInt(key, "PersistentInfoMarginX", 20);
                    _persistentInfoMarginY = GetRegInt(key, "PersistentInfoMarginY", 20);
                    _persistentInfoFontSize = GetRegInt(key, "PersistentInfoFontSize", 14);

                    _logoEnabled = GetRegBool(key, "LogoEnabled", true);
                    _logoPath = GetRegString(key, "LogoPath", "");
                    _logoPosition = GetRegString(key, "LogoPosition", "TopRight");
                    _logoOpacity = GetRegInt(key, "LogoOpacity", 100);
                    _logoSize = GetRegInt(key, "LogoSize", 150);
                    _logoMargin = GetRegInt(key, "LogoMargin", 20);

                    _clockEnabled = GetRegBool(key, "ClockEnabled", true);
                    _clockUnderLogo = GetRegBool(key, "ClockUnderLogo", true);
                    _clockColor = Color.FromArgb(GetRegInt(key, "ClockColor", Color.White.ToArgb()));
                    _clockBgColor = Color.FromArgb(GetRegInt(key, "ClockBgColor", Color.FromArgb(150, 0, 0, 0).ToArgb()));
                    _clockFontSize = GetRegInt(key, "ClockFontSize", 18);
                    _clockBgEnabled = GetRegBool(key, "ClockBgEnabled", true);

                    _spotLabelEnabled = GetRegBool(key, "SpotLabelEnabled", true);
                    _spotLabelText = GetRegString(key, "SpotLabelText", "ADVERTISING");
                    _spotLabelPosition = GetRegString(key, "SpotLabelPosition", "TopLeft");
                    _spotLabelBgColor = Color.FromArgb(GetRegInt(key, "SpotLabelBgColor", Color.FromArgb(200, 255, 0, 0).ToArgb()));
                    _spotLabelTextColor = Color.FromArgb(GetRegInt(key, "SpotLabelTextColor", Color.White.ToArgb()));
                    _spotLabelBgEnabled = GetRegBool(key, "SpotLabelBgEnabled", true);
                    _spotLabelFontSize = GetRegInt(key, "SpotLabelFontSize", 14);
                    _spotLabelMarginX = GetRegInt(key, "SpotLabelMarginX", 20);
                    _spotLabelMarginY = GetRegInt(key, "SpotLabelMarginY", 20);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CGEditor] Load error: {ex.Message}");
            }
        }

        private void SaveSettings()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AirDirector\CG"))
                {
                    key.SetValue("LowerThirdEnabled", _lowerThirdEnabled ? 1 : 0);
                    key.SetValue("LowerThirdPosition", _lowerThirdPosition);
                    key.SetValue("LowerThirdAnimation", _lowerThirdAnimation);
                    key.SetValue("LowerThirdDelayStart", _lowerThirdDelayStart);
                    key.SetValue("LowerThirdDuration", _lowerThirdDuration);
                    key.SetValue("LowerThirdShowAtEnd", _lowerThirdShowAtEnd ? 1 : 0);
                    key.SetValue("LowerThirdEndOffset", _lowerThirdEndOffset);
                    key.SetValue("LowerThirdEndDuration", _lowerThirdEndDuration);
                    key.SetValue("LowerThirdLayout", _lowerThirdLayout);
                    key.SetValue("LowerThirdBgColor", _lowerThirdBgColor.ToArgb());
                    key.SetValue("LowerThirdTextColor", _lowerThirdTextColor.ToArgb());
                    key.SetValue("LowerThirdAccentColor", _lowerThirdAccentColor.ToArgb());
                    key.SetValue("LowerThirdFontFamily", _lowerThirdFontFamily);
                    key.SetValue("LowerThirdTitleFontSize", _lowerThirdTitleFontSize);
                    key.SetValue("LowerThirdArtistFontSize", _lowerThirdArtistFontSize);
                    key.SetValue("LowerThirdMarginX", _lowerThirdMarginX);
                    key.SetValue("LowerThirdMarginY", _lowerThirdMarginY);

                    key.SetValue("PersistentInfoEnabled", _persistentInfoEnabled ? 1 : 0);
                    key.SetValue("ProgressBarColor", _progressBarColor.ToArgb());
                    key.SetValue("PersistentInfoHideBeforeEnd", _persistentInfoHideBeforeEnd);
                    key.SetValue("PersistentInfoMarginX", _persistentInfoMarginX);
                    key.SetValue("PersistentInfoMarginY", _persistentInfoMarginY);
                    key.SetValue("PersistentInfoFontSize", _persistentInfoFontSize);

                    key.SetValue("LogoEnabled", _logoEnabled ? 1 : 0);
                    key.SetValue("LogoPath", _logoPath ?? "");
                    key.SetValue("LogoPosition", _logoPosition);
                    key.SetValue("LogoOpacity", _logoOpacity);
                    key.SetValue("LogoSize", _logoSize);
                    key.SetValue("LogoMargin", _logoMargin);

                    key.SetValue("ClockEnabled", _clockEnabled ? 1 : 0);
                    key.SetValue("ClockUnderLogo", _clockUnderLogo ? 1 : 0);
                    key.SetValue("ClockColor", _clockColor.ToArgb());
                    key.SetValue("ClockBgColor", _clockBgColor.ToArgb());
                    key.SetValue("ClockFontSize", _clockFontSize);
                    key.SetValue("ClockBgEnabled", _clockBgEnabled ? 1 : 0);

                    key.SetValue("SpotLabelEnabled", _spotLabelEnabled ? 1 : 0);
                    key.SetValue("SpotLabelText", _spotLabelText ?? "ADVERTISING");
                    key.SetValue("SpotLabelPosition", _spotLabelPosition);
                    key.SetValue("SpotLabelBgColor", _spotLabelBgColor.ToArgb());
                    key.SetValue("SpotLabelTextColor", _spotLabelTextColor.ToArgb());
                    key.SetValue("SpotLabelBgEnabled", _spotLabelBgEnabled ? 1 : 0);
                    key.SetValue("SpotLabelFontSize", _spotLabelFontSize);
                    key.SetValue("SpotLabelMarginX", _spotLabelMarginX);
                    key.SetValue("SpotLabelMarginY", _spotLabelMarginY);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CGEditor] Save error:  {ex.Message}");
            }
        }

        private string GetRegString(RegistryKey key, string name, string defaultValue)
        {
            object val = key.GetValue(name);
            return val != null ? val.ToString() : defaultValue;
        }

        private int GetRegInt(RegistryKey key, string name, int defaultValue)
        {
            object val = key.GetValue(name);
            if (val != null && int.TryParse(val.ToString(), out int result))
                return result;
            return defaultValue;
        }

        private bool GetRegBool(RegistryKey key, string name, bool defaultValue)
        {
            return GetRegInt(key, name, defaultValue ? 1 : 0) == 1;
        }

        private void BtnSaveApply_Click(object sender, EventArgs e)
        {
            SaveSettings();
            CGRenderer.ReloadSettings();

            MessageBox.Show("✅ Settings saved and applied!", "CG Editor",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _previewTimer?.Stop();
            _previewTimer?.Dispose();
            base.OnFormClosing(e);
        }
    }

    // Double-buffered Panel
    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        }
    }
}