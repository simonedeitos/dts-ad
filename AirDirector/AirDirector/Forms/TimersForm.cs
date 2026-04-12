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
        private Label _lblIntroHeader;
        private Label _lblIntroCountdown;
        private Label _lblMixHeader;
        private Label _lblMixCountdown;
        private Panel _pnlProgressBar;
        private float _trackProgress = 0f;

        // Row 1 – Next Schedule
        private Panel _pnlSchedule;
        private Label _lblScheduleHeader;
        private Label _lblScheduleName;
        private Label _lblScheduleCountdown;

        // Row 2 – Next Ad
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
            _lblIntroHeader.Text = LanguageManager.GetString("TimersForm.Intro", "INTRO");
            _lblMixHeader.Text = LanguageManager.GetString("TimersForm.Countdown", "COUNTDOWN");
            _lblScheduleHeader.Text = LanguageManager.GetString("TimersForm.NextSchedule", "📅 PROSSIMA SCHEDULAZIONE");
            _lblAdHeader.Text = LanguageManager.GetString("TimersForm.NextAd", "📢 PROSSIMA PUBBLICITÀ");
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
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33f));
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33f));
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
            _onAirGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f));
            _onAirGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            _onAirGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 40f));
            _onAirGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 40f));
            _onAirGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));

            // Artista (col 0, row 0)
            _lblOnAirArtist = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(255, 215, 0),
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                AutoEllipsis = true,
                BackColor = Color.Transparent,
                Text = "--",
                Padding = new Padding(8, 0, 0, 0)
            };

            // Titolo (col 0, row 1)
            _lblOnAirTitle = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                AutoEllipsis = true,
                BackColor = Color.Transparent,
                Text = "--",
                Padding = new Padding(8, 0, 0, 0)
            };

            // Intro panel (col 1, row 0)
            Panel pnlIntro = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(60, 0, 0) };
            _lblIntroHeader = new Label
            {
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                Height = 20,
                BackColor = Color.Transparent,
                Text = "INTRO"
            };
            _lblIntroCountdown = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font(_countdownFontFamily, 28f, FontStyle.Bold),
                Text = "--:--"
            };
            pnlIntro.Controls.Add(_lblIntroCountdown);
            pnlIntro.Controls.Add(_lblIntroHeader);

            // Mix/Countdown panel (col 1, row 1)
            Panel pnlMix = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(25, 25, 30) };
            _lblMixHeader = new Label
            {
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                Height = 20,
                BackColor = Color.Transparent,
                Text = "COUNTDOWN"
            };
            _lblMixCountdown = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Red,
                BackColor = Color.Transparent,
                Font = new Font(_countdownFontFamily, 28f, FontStyle.Bold),
                Text = "--:--"
            };
            pnlMix.Controls.Add(_lblMixCountdown);
            pnlMix.Controls.Add(_lblMixHeader);

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

            // ── Row 1: Next Schedule ───────────────────────────────────────────
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
                Text = "--"
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

            // ── Row 2: Next Ad ─────────────────────────────────────────────────
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
                Text = "--"
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

            // Assemble main layout
            _mainLayout.Controls.Add(_pnlOnAir, 0, 0);
            _mainLayout.Controls.Add(_pnlSchedule, 0, 1);
            _mainLayout.Controls.Add(_pnlAd, 0, 2);

            this.Controls.Add(_mainLayout);
        }

        // ─── Responsive font scaling ────────────────────────────────────────────
        private void ResizeFonts()
        {
            if (_mainLayout == null) return;

            float formW = Math.Max(352f, this.ClientSize.Width);
            float rowH  = Math.Max(60f,  _mainLayout.Height / 3.0f);

            float countdown = Math.Max(12f, Math.Min(120f, Math.Min(rowH * 0.35f, formW * 0.22f)));

            // Intro/Mix countdown: right column occupies ~35% of width
            float rightW = formW * 0.35f;
            float introMixCountdown = Math.Max(10f, Math.Min(countdown, rightW * 0.22f));

            float artistSize = Math.Max(10f, Math.Min(36f, rowH * 0.16f));
            float titleSize  = Math.Max(9f,  Math.Min(30f, rowH * 0.14f));
            float header = Math.Max(8f, Math.Min(18f, rowH * 0.10f));
            int   labelH = Math.Max(16, Math.Min(50,  (int)(rowH * 0.22f)));

            // Scale header labels
            _lblIntroHeader.Height = Math.Max(14, (int)(rowH * 0.10f));
            ScaleName(_lblIntroHeader, header);
            _lblMixHeader.Height = Math.Max(14, (int)(rowH * 0.10f));
            ScaleName(_lblMixHeader, header);

            _lblScheduleHeader.Height = labelH;
            _lblScheduleName.Height   = labelH;
            _lblAdHeader.Height       = labelH;
            _lblAdInfo.Height         = labelH;

            ScaleName(_lblScheduleHeader, header);
            ScaleName(_lblAdHeader,       header);

            // Scale countdown fonts
            ScaleCountdown(_lblIntroCountdown,    introMixCountdown);
            ScaleCountdown(_lblMixCountdown,      introMixCountdown);
            ScaleCountdown(_lblScheduleCountdown, countdown);
            ScaleCountdown(_lblAdCountdown,       countdown);

            // Scale artist/title
            ScaleName(_lblOnAirArtist, artistSize);
            ScaleName(_lblOnAirTitle,  titleSize);

            // Scale schedule/ad name/info
            float name = Math.Max(8f, Math.Min(30f, rowH * 0.13f));
            ScaleName(_lblScheduleName, name);
            ScaleName(_lblAdInfo,       name);
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
            UpdateNextSchedule();
            UpdateNextAd();
        }

        // ─── On Air ─────────────────────────────────────────────────────────────
        private void UpdateOnAir()
        {
            if (_playlistQueue == null)
            {
                _lblOnAirArtist.Text = "--";
                _lblOnAirTitle.Text = "--";
                SetNoIntro();
                SetNoMix();
                UpdateProgress(0, 0, 0);
                return;
            }

            var current = _playlistQueue.GetCurrentPlayingItem();
            if (current == null)
            {
                _lblOnAirArtist.Text = "--";
                _lblOnAirTitle.Text = LanguageManager.GetString("TimersForm.NoOnAir", "Nessun elemento in onda");
                SetNoIntro();
                SetNoMix();
                UpdateProgress(0, 0, 0);
                return;
            }

            _lblOnAirArtist.Text = !string.IsNullOrEmpty(current.Artist) ? current.Artist : "--";
            _lblOnAirTitle.Text  = !string.IsNullOrEmpty(current.Title)  ? current.Title  : "--";

            int posMs = _isRadioTVMode
                ? (_playerControlVideo?.CurrentPositionMs ?? 0)
                : (_playerControl?.CurrentPositionMs ?? 0);

            // INTRO countdown
            int introMarker = _isRadioTVMode
                ? (_playerControlVideo?.CurrentMarkerINTRO ?? 0)
                : (_playerControl?.CurrentMarkerINTRO ?? 0);

            if (introMarker > 0 && posMs < introMarker)
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

            // MIX countdown
            int mixMarker = _isRadioTVMode
                ? (_playerControlVideo?.CurrentMarkerMIX ?? 0)
                : (_playerControl?.CurrentMarkerMIX ?? 0);

            if (mixMarker > 0 && posMs < mixMarker)
            {
                _lblMixCountdown.Text = FormatMs(mixMarker - posMs);
                _lblMixCountdown.ForeColor = Color.Red;
            }
            else
            {
                SetNoMix();
            }

            // Progress bar
            UpdateProgress(posMs, current.MarkerIN, current.MarkerMIX);
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
                    _lblScheduleName.Text = LanguageManager.GetString("TimersForm.NoSchedule", "Nessuna schedulazione prevista");
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
                    _lblAdInfo.Text = LanguageManager.GetString("TimersForm.NoAd", "Nessuna pubblicità programmata");
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
                    _lblAdInfo.Text = LanguageManager.GetString("TimersForm.NoAd", "Nessuna pubblicità programmata");
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
