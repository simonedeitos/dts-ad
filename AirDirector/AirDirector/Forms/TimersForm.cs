using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;
using AirDirector.Controls;
using AirDirector.Services;
using AirDirector.Services.Database;
using AirDirector.Services.Localization;

namespace AirDirector.Forms
{
    public partial class TimersForm : Form
    {
        // References
        private PlaylistQueueControl _playlistQueue;
        private PlayerControl _playerControl;
        private PlayerControlVideo _playerControlVideo;
        private bool _isRadioTVMode;

        // Update timer
        private System.Windows.Forms.Timer _updateTimer;
        private volatile bool _blinkState = false;

        // Countdown font: prefer DSEG7 Classic (digital LCD look), fall back to Consolas
        private static readonly string _countdownFontFamily =
            System.Linq.Enumerable.Any(System.Drawing.FontFamily.Families, f => f.Name == "DSEG7 Classic")
                ? "DSEG7 Classic" : "Consolas";

        // ADV Cache (same pattern as OverviewControl)
        private List<AirDirectorPlaylistItem> _cachedAdvItems = new List<AirDirectorPlaylistItem>();
        private DateTime _advCacheDate = DateTime.MinValue;
        private DateTime _lastAdvReloadTime = DateTime.MinValue;

        // ── Layout ─────────────────────────────────────────────────────────────
        private TableLayoutPanel _mainLayout;

        // Row 0 – In Onda (new layout)
        private Panel _pnlOnAir;
        private TableLayoutPanel _onAirGrid;
        private Label _lblOnAirArtist;
        private Label _lblOnAirTitle;
        private Label _lblIntroCountdown;
        private Label _lblMixCountdown;
        private Panel _pnlProgressBar;
        private float _trackProgress = 0f;

        // Row 1 – Date & Clock (right column)
        private Panel _pnlDateTime;
        private Label _lblDateText;
        private Label _lblClockText;
        private float _dateMaxFontSize = 14f; // updated by ResizeFonts

        // Row 1 – split 50/50 (Queue preview + Date/Clock)
        private TableLayoutPanel _row1Split;
        private Panel _pnlQueueNext;
        private Label[] _lblQueueNext = new Label[3];

        // Row 2 – split 50/50
        private TableLayoutPanel _row2Split;

        // Row 2 Left – Next Schedule
        private Panel _pnlSchedule;
        private Label _lblScheduleHeader;
        private Label _lblScheduleName;
        private Label _lblScheduleCountdown;

        // Row 2 Right – Next Ad
        private Panel _pnlAd;
        private Label _lblAdHeader;
        private Label _lblAdInfo;
        private Label _lblAdCountdown;

        // ─── Constructor ───────────────────────────────────────────────────────
        public TimersForm()
        {
            InitializeComponent();
            _isRadioTVMode = ConfigurationControl.IsRadioTVMode();
            BuildUI();
            ResizeFonts();
            ApplyLanguage();
            LoadAdvCache();

            _updateTimer = new System.Windows.Forms.Timer { Interval = 250 };
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();

            LanguageManager.LanguageChanged += OnLanguageChanged;
            this.Resize += (s, e) => ResizeFonts();
        }

        public void SetReferences(PlaylistQueueControl queue, PlayerControl player, PlayerControlVideo playerVideo)
        {
            _playlistQueue = queue;
            _playerControl = player;
            _playerControlVideo = playerVideo;
        }

        private void OnLanguageChanged(object sender, EventArgs e) => ApplyLanguage();

        protected override void WndProc(ref Message m)
        {
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_MINIMIZE = 0xF020;

            // Impedisci la minimizzazione causata dall'owner
            if (m.Msg == WM_SYSCOMMAND && (m.WParam.ToInt32() & 0xFFF0) == SC_MINIMIZE)
            {
                // Non fare nulla - impedisce la minimizzazione
                return;
            }

            base.WndProc(ref m);
        }

        private void ApplyLanguage()
        {
            this.Text = LanguageManager.GetString("TimersForm.Title", "Timers");
            _lblScheduleHeader.Text = LanguageManager.GetString("TimersForm.NextSchedule", "📅 PROSSIMA SCHEDULAZIONE");
            _lblAdHeader.Text = LanguageManager.GetString("TimersForm.NextAd", "📢 PROSSIMA PUBBLICITÀ");
            _lblIntroCountdown.Parent?.Invalidate();
            _lblMixCountdown.Parent?.Invalidate();
        }

        // ─── UI Construction ────────────────────────────────────────────────────
        private void BuildUI()
        {
            this.BackColor = Color.FromArgb(20, 20, 20);

            _mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                BackColor = Color.FromArgb(20, 20, 20),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
            };
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 34f));
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 30f)); // Row 1 - Date & Clock
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 36f)); // Row 2 - Schedule/Ad
            _mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            // ── Row 0: On Air ──────────────────────────────────────────────────
            _pnlOnAir = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(25, 25, 30),
                Padding = new Padding(6)
            };

            _onAirGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 2,
                BackColor = Color.Transparent
            };
            _onAirGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
            _onAirGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            _onAirGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 40f));
            _onAirGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 40f));
            _onAirGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));

            // Artista (col 0, row 0)
            _lblOnAirArtist = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                BackColor = Color.Transparent,
                Text = "--",
                Padding = Padding.Empty,
                UseMnemonic = false
            };

            // Titolo (col 0, row 1)
            _lblOnAirTitle = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                BackColor = Color.Transparent,
                Text = "--",
                Padding = Padding.Empty,
                UseMnemonic = false
            };

            // Intro panel (col 1, row 0) — header drawn via Paint, countdown fills all space
            Panel pnlIntro = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(60, 0, 0) };
            _lblIntroCountdown = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font(_countdownFontFamily, 28f, FontStyle.Bold),
                Text = "--:--",
                Padding = new Padding(0, 14, 0, 0)
            };
            pnlIntro.Controls.Add(_lblIntroCountdown);
            pnlIntro.Paint += (s, ev) => DrawPanelHeader(ev.Graphics, pnlIntro, LanguageManager.GetString("TimersForm.Intro", "INTRO"));

            // Mix/Countdown panel (col 1, row 1) — header drawn via Paint, countdown fills all space
            Panel pnlMix = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(25, 25, 30) };
            _lblMixCountdown = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Red,
                BackColor = Color.Transparent,
                Font = new Font(_countdownFontFamily, 28f, FontStyle.Bold),
                Text = "--:--",
                Padding = new Padding(0, 14, 0, 0)
            };
            pnlMix.Controls.Add(_lblMixCountdown);
            pnlMix.Paint += (s, ev) => DrawPanelHeader(ev.Graphics, pnlMix, LanguageManager.GetString("TimersForm.Countdown", "COUNTDOWN"));

            // Progress bar (row 2, colspan 2)
            _pnlProgressBar = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(40, 40, 40),
                Margin = new Padding(4, 2, 4, 4)
            };
            _pnlProgressBar.Paint += PnlProgressBar_Paint;

            // Assemble grid
            _onAirGrid.Controls.Add(_lblOnAirArtist, 0, 0);
            _onAirGrid.Controls.Add(pnlIntro, 1, 0);
            _onAirGrid.Controls.Add(_lblOnAirTitle, 0, 1);
            _onAirGrid.Controls.Add(pnlMix, 1, 1);
            _onAirGrid.Controls.Add(_pnlProgressBar, 0, 2);
            _onAirGrid.SetColumnSpan(_pnlProgressBar, 2);

            _pnlOnAir.Controls.Add(_onAirGrid);

            // ── Row 1 Left: Queue preview ──────────────────────────────────────
            _pnlQueueNext = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 22, 30),
                Padding = new Padding(6, 4, 6, 4)
            };

            var queueGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                BackColor = Color.Transparent
            };
            queueGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));
            queueGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));
            queueGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.34f));
            queueGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            for (int i = 0; i < 3; i++)
            {
                _lblQueueNext[i] = new Label
                {
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    ForeColor = Color.White,
                    BackColor = Color.Transparent,
                    Font = new Font("Segoe UI", 11f, FontStyle.Regular),
                    Text = $"| {i + 2} |  --",
                    UseMnemonic = false,
                    AutoEllipsis = true,
                    Padding = new Padding(4, 0, 0, 0)
                };
                queueGrid.Controls.Add(_lblQueueNext[i], 0, i);
            }

            _pnlQueueNext.Controls.Add(queueGrid);

            // ── Row 1 Right: Date & Clock ──────────────────────────────────────
            _pnlDateTime = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(25, 28, 38),
                Padding = new Padding(6)
            };

            _lblDateText = new Label
            {
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                Height = 40,
                BackColor = Color.Transparent,
                UseMnemonic = false,
                Text = "--"
            };

            _lblClockText = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font(_countdownFontFamily, 28f, FontStyle.Bold),
                Text = "--:--:--"
            };

            _pnlDateTime.Controls.Add(_lblClockText);
            _pnlDateTime.Controls.Add(_lblDateText);

            // ── Row 1: Split 50/50 ─────────────────────────────────────────────
            _row1Split = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 2,
                BackColor = Color.FromArgb(20, 20, 20),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
            };
            _row1Split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            _row1Split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            _row1Split.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            _row1Split.Controls.Add(_pnlQueueNext, 0, 0);
            _row1Split.Controls.Add(_pnlDateTime, 1, 0);

            // ── Row 2 Left: Next Schedule ──────────────────────────────────────
            _pnlSchedule = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(25, 28, 38),
                Padding = new Padding(6)
            };

            _lblScheduleHeader = new Label
            {
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(255, 153, 50),
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                Height = 40,
                BackColor = Color.Transparent
            };
            _lblScheduleName = new Label
            {
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Height = 40,
                AutoEllipsis = true,
                BackColor = Color.Transparent,
                Text = "--",
                UseMnemonic = false
            };
            _lblScheduleCountdown = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font(_countdownFontFamily, 28f, FontStyle.Bold),
                Text = "--:--:--"
            };
            _pnlSchedule.Controls.Add(_lblScheduleCountdown);
            _pnlSchedule.Controls.Add(_lblScheduleName);
            _pnlSchedule.Controls.Add(_lblScheduleHeader);

            // ── Row 2 Right: Next Ad ───────────────────────────────────────────
            _pnlAd = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 22, 22),
                Padding = new Padding(6)
            };

            _lblAdHeader = new Label
            {
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(255, 100, 100),
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                Height = 40,
                BackColor = Color.Transparent
            };
            _lblAdInfo = new Label
            {
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Height = 40,
                AutoEllipsis = true,
                BackColor = Color.Transparent,
                Text = "--",
                UseMnemonic = false
            };
            _lblAdCountdown = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font(_countdownFontFamily, 28f, FontStyle.Bold),
                Text = "--:--:--"
            };
            _pnlAd.Controls.Add(_lblAdCountdown);
            _pnlAd.Controls.Add(_lblAdInfo);
            _pnlAd.Controls.Add(_lblAdHeader);

            // ── Row 2: Split 50/50 ─────────────────────────────────────────────
            _row2Split = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 2,
                BackColor = Color.FromArgb(20, 20, 20),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
            };
            _row2Split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            _row2Split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            _row2Split.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            _row2Split.Controls.Add(_pnlSchedule, 0, 0);
            _row2Split.Controls.Add(_pnlAd, 1, 0);

            // Assemble main layout
            _mainLayout.Controls.Add(_pnlOnAir, 0, 0);
            _mainLayout.Controls.Add(_row1Split, 0, 1);
            _mainLayout.Controls.Add(_row2Split, 0, 2);

            this.Controls.Add(_mainLayout);
        }

        // ─── Responsive font scaling ────────────────────────────────────────────
        private void ResizeFonts()
        {
            if (_mainLayout == null) return;

            float formW = Math.Max(352f, this.ClientSize.Width);
            float rowH  = Math.Max(60f,  _mainLayout.Height / 3.0f);

            // Intro/Mix countdown: right column occupies ~40% of width, each panel gets ~40% of row height
            float rightW = formW * 0.40f;
            float counterH = rowH * 0.40f;
            float introMixCountdown = Math.Max(10f, Math.Min(
                rightW * 0.18f,
                counterH * 0.45f
            ));

            float artistSize = Math.Max(10f, Math.Min(36f, rowH * 0.16f));
            float titleSize  = Math.Max(9f,  Math.Min(30f, rowH * 0.14f));
            float header = Math.Max(8f, Math.Min(18f, rowH * 0.10f));
            int   labelH = Math.Max(16, Math.Min(50,  (int)(rowH * 0.22f)));

            _lblScheduleHeader.Height = labelH;
            _lblScheduleName.Height   = labelH;
            _lblAdHeader.Height       = labelH;
            _lblAdInfo.Height         = labelH;

            ScaleName(_lblScheduleHeader, header);
            ScaleName(_lblAdHeader,       header);

            // Dynamic padding for intro/mix countdown to leave room for paint header
            int headerPad = Math.Max(10, (int)(rowH * 0.06f));
            _lblIntroCountdown.Padding = new Padding(0, headerPad, 0, 0);
            _lblMixCountdown.Padding   = new Padding(0, headerPad, 0, 0);

            // Scale countdown fonts
            float row2H = Math.Max(60f, _row2Split != null ? _row2Split.Height : rowH);
            float countdown = Math.Max(12f, Math.Min(120f, Math.Min(row2H * 0.35f, (formW * 0.5f) * 0.22f)));
            ScaleCountdown(_lblIntroCountdown,    introMixCountdown);
            ScaleCountdown(_lblMixCountdown,      introMixCountdown);
            ScaleCountdown(_lblScheduleCountdown, countdown);
            ScaleCountdown(_lblAdCountdown,       countdown);

            // Scale artist/title — re-fit to current text so long strings shrink automatically
            AutoFitLabel(_lblOnAirArtist, _lblOnAirArtist.Text, artistSize);
            AutoFitLabel(_lblOnAirTitle,  _lblOnAirTitle.Text,  titleSize);

            // Scale schedule/ad name/info using AutoFitLabel
            float nameMaxSize = Math.Max(8f, Math.Min(30f, row2H * 0.13f));
            AutoFitLabel(_lblScheduleName, _lblScheduleName.Text, nameMaxSize);
            AutoFitLabel(_lblAdInfo,       _lblAdInfo.Text,       nameMaxSize);

            // Scale date/clock panel
            if (_pnlDateTime != null)
            {
                float dateRowH = Math.Max(40f, _pnlDateTime.Height);
                _dateMaxFontSize = Math.Max(10f, Math.Min(30f, dateRowH * 0.25f));
                AutoFitLabel(_lblDateText, _lblDateText.Text, _dateMaxFontSize);

                float clockSize = Math.Max(12f, Math.Min(80f, Math.Min(dateRowH * 0.40f, formW * 0.18f)));
                ScaleCountdown(_lblClockText, clockSize);

                int dateLabelH = Math.Max(16, Math.Min(50, (int)(dateRowH * 0.35f)));
                _lblDateText.Height = dateLabelH;
            }

            // Scale queue preview labels
            if (_lblQueueNext != null && _pnlQueueNext != null)
            {
                float queueRowH = Math.Max(20f, _pnlQueueNext.Height > 0 ? _pnlQueueNext.Height / 3f : 40f);
                float queueFontSize = Math.Max(7f, Math.Min(16f, queueRowH * 0.35f));
                foreach (var lbl in _lblQueueNext)
                {
                    if (lbl == null) continue;
                    try
                    {
                        var old = lbl.Font;
                        lbl.Font = new Font("Segoe UI", queueFontSize, FontStyle.Regular);
                        old?.Dispose();
                    }
                    catch { }
                }
            }

            // Invalidate intro/mix panels to redraw paint header
            _lblIntroCountdown.Parent?.Invalidate();
            _lblMixCountdown.Parent?.Invalidate();
        }

        private static void ScaleCountdown(Label lbl, float size)
        {
            try
            {
                var old = lbl.Font;
                lbl.Font = new Font(_countdownFontFamily, Math.Max(8f, size), FontStyle.Bold);
                old.Dispose();
            }
            catch { }
        }

        private static void ScaleName(Label lbl, float size)
        {
            try
            {
                var old = lbl.Font;
                lbl.Font = new Font("Segoe UI", Math.Max(8f, size), FontStyle.Bold);
                old.Dispose();
            }
            catch { }
        }

        private static void AutoFitLabel(Label lbl, string text, float maxFontSize, float minFontSize = 7f)
        {
            lbl.AutoEllipsis = false;
            lbl.Text = text;
            if (lbl.Width <= 0) return;

            float fontSize = maxFontSize;
            int availableWidth  = lbl.Width  - 4;
            int availableHeight = lbl.Height > 0 ? lbl.Height - 4 : int.MaxValue;

            while (fontSize > minFontSize)
            {
                using (var testFont = new Font("Segoe UI", fontSize, FontStyle.Bold))
                {
                    var textSize = TextRenderer.MeasureText(text, testFont);
                    if (textSize.Width <= availableWidth && textSize.Height <= availableHeight)
                        break;
                }
                fontSize -= 0.5f;
            }

            fontSize = Math.Max(minFontSize, fontSize);
            var old = lbl.Font;
            lbl.Font = new Font("Segoe UI", fontSize, FontStyle.Bold);
            old?.Dispose();
        }

        private void DrawPanelHeader(Graphics g, Panel panel, string text)
        {
            float headerFontSize = Math.Max(7f, Math.Min(12f, panel.Height * 0.12f));
            using (var font = new Font("Segoe UI", headerFontSize, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.FromArgb(200, 255, 255, 255)))
            {
                var size = g.MeasureString(text, font);
                float x = (panel.Width - size.Width) / 2f;
                float y = 2f;
                g.DrawString(text, font, brush, x, y);
            }
        }

        // ─── Timer Tick ─────────────────────────────────────────────────────────
        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;

            // Blink state is time-based (500 ms on / 500 ms off), independent of timer interval
            _blinkState = now.Millisecond < 500;

            bool reload = _cachedAdvItems.Count == 0
                || _advCacheDate != now.Date
                || ((now - _lastAdvReloadTime).TotalSeconds >= 1 && (
                    (now.Hour == 23 && now.Minute == 59 && now.Second >= 55)
                    || (now.Hour == 0 && now.Minute == 0 && now.Second >= 5 && now.Second <= 10)
                    || (now.Minute == 0 && now.Second >= 5 && now.Second <= 10 && (now - _lastAdvReloadTime).TotalMinutes > 1)));

            if (reload) LoadAdvCache();

            UpdateOnAir();
            UpdateDateTime();
            UpdateQueuePanel();
            UpdateNextSchedule();
            UpdateNextAd();
        }

        // ─── On Air ─────────────────────────────────────────────────────────────
        private void UpdateOnAir()
        {
            if (_playlistQueue == null)
            {
                _lblOnAirArtist.Text = "";
                _lblOnAirTitle.Text = "";
                SetNoIntro();
                SetNoMix();
                UpdateProgress(0, 0, 0);
                return;
            }

            var current = _playlistQueue.GetCurrentPlayingItem();
            if (current == null)
            {
                _lblOnAirArtist.Text = "";
                _lblOnAirTitle.Text = "";
                SetNoIntro();
                SetNoMix();
                UpdateProgress(0, 0, 0);
                return;
            }

            float rowH = _mainLayout != null ? Math.Max(60f, _mainLayout.Height / 3.0f) : 100f;
            float artistMaxSize = Math.Max(10f, Math.Min(36f, rowH * 0.16f));
            float titleMaxSize  = Math.Max(9f,  Math.Min(30f, rowH * 0.14f));

            string artistText = !string.IsNullOrEmpty(current.Artist) ? current.Artist : "--";
            string titleText  = !string.IsNullOrEmpty(current.Title)  ? current.Title  : "--";

            AutoFitLabel(_lblOnAirArtist, artistText, artistMaxSize);
            AutoFitLabel(_lblOnAirTitle,  titleText,  titleMaxSize);

            int posMs = _isRadioTVMode
                ? (_playerControlVideo?.CurrentPositionMs ?? 0)
                : (_playerControl?.CurrentPositionMs ?? 0);

            // INTRO countdown — relativo al MarkerIN (INTRO-IN)
            int introMarker = _isRadioTVMode
                ? (_playerControlVideo?.CurrentMarkerINTRO ?? 0)
                : (_playerControl?.CurrentMarkerINTRO ?? 0);

            int markerIN = _isRadioTVMode
                ? (_playerControlVideo?.CurrentMarkerIN ?? 0)
                : (_playerControl?.CurrentMarkerIN ?? 0);

            int effectiveIntro = introMarker - markerIN; // INTRO-IN

            if (effectiveIntro > 0 && posMs < introMarker)
            {
                _lblIntroCountdown.Text = FormatMs(introMarker - posMs);
                _lblIntroCountdown.ForeColor = Color.White;
                if (_lblIntroCountdown.Parent != null)
                    _lblIntroCountdown.Parent.BackColor = Color.Red;
            }
            else
            {
                SetNoIntro();
            }

            // MIX countdown: get from player (PlayerControl already falls back to total duration when marker is 0)
            int playerMixMarker = _isRadioTVMode
                ? (_playerControlVideo?.CurrentMarkerMIX ?? 0)
                : (_playerControl?.CurrentMarkerMIX ?? 0);

            // Fallback for files without markers (scheduled files, ADV, etc.): use item duration
            int effectiveMixMarker = playerMixMarker > 0 ? playerMixMarker : (int)current.Duration.TotalMilliseconds;

            if (effectiveMixMarker > 0 && posMs < effectiveMixMarker)
            {
                _lblMixCountdown.Text = FormatMs(effectiveMixMarker - posMs);
                _lblMixCountdown.ForeColor = Color.Red;
            }
            else
            {
                SetNoMix();
            }

            // Progress bar: when item has no markers, use full duration as range
            int progressIN  = current.MarkerMIX > 0 ? current.MarkerIN  : 0;
            int progressMIX = current.MarkerMIX > 0 ? current.MarkerMIX : effectiveMixMarker;
            UpdateProgress(posMs, progressIN, progressMIX);
        }

        private void UpdateProgress(int posMs, int markerIN, int markerMIX)
        {
            if (markerMIX > markerIN && markerMIX > 0)
            {
                float progress = (float)(posMs - markerIN) / (markerMIX - markerIN);
                _trackProgress = Math.Max(0f, Math.Min(1f, progress));
            }
            else
            {
                _trackProgress = 0f;
            }
            _pnlProgressBar?.Invalidate();
        }

        private void PnlProgressBar_Paint(object sender, PaintEventArgs e)
        {
            Panel pnl = sender as Panel;
            if (pnl == null) return;

            int barHeight = Math.Max(6, pnl.Height - 4);
            int barY = (pnl.Height - barHeight) / 2;
            int barWidth = pnl.Width - 8;
            int barX = 4;

            using (var bgBrush = new SolidBrush(Color.FromArgb(50, 50, 50)))
                e.Graphics.FillRectangle(bgBrush, barX, barY, barWidth, barHeight);

            if (_trackProgress > 0f)
            {
                int fillWidth = (int)(barWidth * Math.Min(1f, _trackProgress));
                if (fillWidth > 0)
                {
                    using (var fillBrush = new SolidBrush(Color.White))
                        e.Graphics.FillRectangle(fillBrush, barX, barY, fillWidth, barHeight);
                }
            }
        }

        private void SetNoIntro()
        {
            _lblIntroCountdown.Text = "--:--";
            _lblIntroCountdown.ForeColor = Color.FromArgb(120, 120, 120);
            if (_lblIntroCountdown.Parent != null)
                _lblIntroCountdown.Parent.BackColor = Color.FromArgb(60, 0, 0);
        }

        private void SetNoMix()
        {
            _lblMixCountdown.Text = "--:--";
            _lblMixCountdown.ForeColor = Color.Red;
        }

        // ─── Date & Clock ───────────────────────────────────────────────────────
        private void UpdateDateTime()
        {
            try
            {
                DateTime now = DateTime.Now;
                var culture = new System.Globalization.CultureInfo("it-IT");

                string dateText = culture.TextInfo.ToTitleCase(now.ToString("dddd d MMMM, yyyy", culture));
                AutoFitLabel(_lblDateText, dateText, _dateMaxFontSize);

                _lblClockText.Text = now.ToString("HH:mm:ss");
                _lblClockText.ForeColor = Color.White;
            }
            catch
            {
                _lblClockText.Text = "--:--:--";
            }
        }

        // ─── Queue Preview (Row 1 Left) ─────────────────────────────────────────
        private void UpdateQueuePanel()
        {
            if (_lblQueueNext == null) return;

            if (_playlistQueue == null)
            {
                for (int i = 0; i < _lblQueueNext.Length; i++)
                    if (_lblQueueNext[i] != null)
                        _lblQueueNext[i].Text = $"| {i + 2} |  --";
                return;
            }

            var current = _playlistQueue.GetCurrentPlayingItem();
            var allItems = _playlistQueue.GetAllItems();
            int currentIdx = current != null ? allItems.IndexOf(current) : -1;

            for (int i = 0; i < 3; i++)
            {
                if (_lblQueueNext[i] == null) continue;
                int nextIdx = currentIdx + 1 + i;
                if (currentIdx >= 0 && nextIdx < allItems.Count)
                {
                    var item = allItems[nextIdx];
                    string artist = item.Artist ?? "";
                    string title  = item.Title  ?? "";
                    string display = string.IsNullOrEmpty(artist)
                        ? (string.IsNullOrEmpty(title) ? "--" : title)
                        : (string.IsNullOrEmpty(title) ? artist : $"{artist} - {title}");
                    _lblQueueNext[i].Text = $"| {i + 2} |  {display}";
                }
                else
                {
                    _lblQueueNext[i].Text = $"| {i + 2} |  --";
                }
            }
        }

        // ─── Next Schedule ──────────────────────────────────────────────────────
        private void UpdateNextSchedule()
        {
            try
            {
                DateTime now = DateTime.Now;
                int dow = (int)now.DayOfWeek;

                var schedules = DbcManager.LoadFromCsv<ScheduleEntry>("Schedules.dbc");
                var active = schedules.Where(s => s.IsEnabled == 1 && IsDayEnabled(s, dow)).ToList();

                ScheduleEntry next = null;
                DateTime nextTime = DateTime.MaxValue;

                foreach (var s in active)
                {
                    if (string.IsNullOrEmpty(s.Times)) continue;
                    foreach (var t in s.Times.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (TimeSpan.TryParse(t.Trim(), out TimeSpan ts))
                        {
                            DateTime dt = now.Date.Add(ts);
                            if (dt > now && dt < nextTime) { next = s; nextTime = dt; }
                        }
                    }
                }

                if (next != null)
                {
                    _lblScheduleName.Text = $"{nextTime:HH:mm:ss}  –  {next.Name}";
                    int sec = (int)(nextTime - now).TotalSeconds;
                    int secH = sec / 3600, secM = (sec % 3600) / 60, secS = sec % 60;
                    _lblScheduleCountdown.Text = secH > 0 ? $"{secH:00}:{secM:00}:{secS:00}" : $"{secM:00}:{secS:00}";

                    if (sec <= 120)
                    {
                        _lblScheduleCountdown.ForeColor = _blinkState ? Color.Red : Color.White;
                    }
                    else
                    {
                        _lblScheduleCountdown.ForeColor = Color.White;
                    }
                }
                else
                {
                    _lblScheduleName.Text = LanguageManager.GetString("TimersForm.NoSchedule", "");
                    _lblScheduleCountdown.Text = "--:--:--";
                    _lblScheduleCountdown.ForeColor = Color.White;
                }
            }
            catch
            {
                _lblScheduleCountdown.Text = "--:--:--";
            }
        }

        // ─── Next Ad ────────────────────────────────────────────────────────────
        private void UpdateNextAd()
        {
            try
            {
                DateTime now = DateTime.Now;

                if (_cachedAdvItems.Count == 0)
                {
                    _lblAdInfo.Text = LanguageManager.GetString("TimersForm.NoAd", "");
                    _lblAdCountdown.Text = "--:--:--";
                    _lblAdCountdown.ForeColor = Color.White;
                    return;
                }

                var todaySlots = _cachedAdvItems
                    .Where(a => a.Date.Date == now.Date && a.IsActive)
                    .GroupBy(a => a.SlotTime)
                    .Select(g => new { SlotTime = g.Key, Items = g.OrderBy(x => x.SequenceOrder).ToList() })
                    .OrderBy(s => s.SlotTime)
                    .ToList();

                DateTime? nextAdDt = null;
                int spotCount = 0;
                int totalDur = 0;

                foreach (var slot in todaySlots)
                {
                    if (TimeSpan.TryParse(slot.SlotTime, out TimeSpan st))
                    {
                        DateTime slotDt = now.Date.Add(st);
                        if (slotDt > now)
                        {
                            nextAdDt = slotDt;
                            spotCount = slot.Items.Count(i => i.FileType == "SPOT");
                            totalDur = slot.Items.Sum(i => i.Duration);
                            break;
                        }
                    }
                }

                if (nextAdDt.HasValue)
                {
                    _lblAdInfo.Text = $"{nextAdDt.Value:HH:mm}  –  {spotCount} Spot  –  {totalDur / 60:00}:{totalDur % 60:00}";

                    int sec = (int)(nextAdDt.Value - now).TotalSeconds;
                    int secH = sec / 3600, secM = (sec % 3600) / 60, secS = sec % 60;
                    _lblAdCountdown.Text = secH > 0 ? $"{secH:00}:{secM:00}:{secS:00}" : $"{secM:00}:{secS:00}";

                    if (sec <= 120)
                    {
                        _lblAdCountdown.ForeColor = _blinkState ? Color.Red : Color.White;
                    }
                    else
                    {
                        _lblAdCountdown.ForeColor = Color.White;
                    }
                }
                else
                {
                    _lblAdInfo.Text = LanguageManager.GetString("TimersForm.NoAd", "");
                    _lblAdCountdown.Text = "--:--:--";
                    _lblAdCountdown.ForeColor = Color.White;
                }
            }
            catch
            {
                _lblAdCountdown.Text = "--:--:--";
            }
        }

        // ─── Helpers ────────────────────────────────────────────────────────────
        private static string FormatMs(int ms)
        {
            if (ms <= 0) return "--:--:--";
            int s = ms / 1000;
            int h = s / 3600, m = (s % 3600) / 60, sec = s % 60;
            return h > 0 ? $"{h:00}:{m:00}:{sec:00}" : $"{m:00}:{sec:00}";
        }

        private static bool IsDayEnabled(ScheduleEntry schedule, int dow) => dow switch
        {
            0 => schedule.Sunday == 1,
            1 => schedule.Monday == 1,
            2 => schedule.Tuesday == 1,
            3 => schedule.Wednesday == 1,
            4 => schedule.Thursday == 1,
            5 => schedule.Friday == 1,
            6 => schedule.Saturday == 1,
            _ => false
        };

        // ─── ADV Cache (same algorithm as OverviewControl) ──────────────────────
        private void LoadAdvCache()
        {
            try
            {
                string databasePath = "";
                try
                {
                    using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AirDirector"))
                        if (key != null) databasePath = key.GetValue("DatabasePath") as string;
                }
                catch { }

                if (string.IsNullOrEmpty(databasePath))
                    databasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database");

                string advFile = Path.Combine(databasePath, "ADV_AirDirector.dbc");
                _cachedAdvItems.Clear();

                if (!File.Exists(advFile))
                {
                    _advCacheDate = DateTime.MinValue;
                    _lastAdvReloadTime = DateTime.Now;
                    return;
                }

                var lines = File.ReadAllLines(advFile);
                var culture = new System.Globalization.CultureInfo("it-IT");

                for (int i = 1; i < lines.Length; i++)
                {
                    try
                    {
                        string line = lines[i].Trim();
                        if (line.StartsWith("\"") && line.EndsWith("\"") && line.Length >= 2)
                            line = line.Substring(1, line.Length - 2);
                        var p = line.Split(new[] { "\";\""  }, StringSplitOptions.None);
                        if (p.Length < 9) continue;
                        _cachedAdvItems.Add(new AirDirectorPlaylistItem
                        {
                            ID            = int.Parse(p[0]),
                            Date          = DateTime.Parse(p[1], culture),
                            SlotTime      = p[2],
                            SequenceOrder = int.Parse(p[3]),
                            FileType      = p[4],
                            FilePath      = p[5],
                            Duration      = int.Parse(p[6]),
                            ClientName    = p[7],
                            SpotTitle     = p[8],
                            CampaignName  = "",
                            CategoryName  = "",
                            IsActive      = true
                        });
                    }
                    catch { }
                }

                _advCacheDate = DateTime.Now.Date;
                _lastAdvReloadTime = DateTime.Now;
            }
            catch
            {
                _cachedAdvItems.Clear();
                _advCacheDate = DateTime.MinValue;
                _lastAdvReloadTime = DateTime.Now;
            }
        }
    }
}
