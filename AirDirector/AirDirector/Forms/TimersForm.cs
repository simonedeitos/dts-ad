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

        // Row 0 – In Onda
        private Panel _pnlOnAir;
        private Label _lblOnAirHeader;
        private Label _lblOnAirName;
        private TableLayoutPanel _pnlCountdownsRow;
        private Panel _pnlIntroSide;
        private Label _lblIntroHeader;
        private Label _lblIntroCountdown;
        private Panel _pnlMixSide;
        private Label _lblMixHeader;
        private Label _lblMixCountdown;

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

        private void ApplyLanguage()
        {
            this.Text = LanguageManager.GetString("TimersForm.Title", "Timers");
            _lblOnAirHeader.Text = LanguageManager.GetString("TimersForm.OnAir", "🎙 IN ONDA");
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

            _lblOnAirHeader = new Label
            {
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(100, 200, 255),
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                Height = 40,
                BackColor = Color.Transparent
            };

            _lblOnAirName = new Label
            {
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                Height = 40,
                AutoEllipsis = true,
                BackColor = Color.Transparent,
                Text = "--"
            };

            // Countdown sub-panel split 50/50
            _pnlCountdownsRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 2,
                BackColor = Color.Transparent
            };
            _pnlCountdownsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            _pnlCountdownsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            _pnlCountdownsRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // Intro side
            _pnlIntroSide = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(60, 0, 0) };
            _lblIntroHeader = new Label
            {
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                Height = 40,
                BackColor = Color.Transparent
            };
            _lblIntroCountdown = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font(_countdownFontFamily, 32f, FontStyle.Bold),
                Text = "--:--:--"
            };
            _pnlIntroSide.Controls.Add(_lblIntroCountdown);
            _pnlIntroSide.Controls.Add(_lblIntroHeader);

            // Mix side
            _pnlMixSide = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(25, 25, 30) };
            _lblMixHeader = new Label
            {
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                Height = 40,
                BackColor = Color.Transparent
            };
            _lblMixCountdown = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Red,
                BackColor = Color.Transparent,
                Font = new Font(_countdownFontFamily, 32f, FontStyle.Bold),
                Text = "--:--:--"
            };
            _pnlMixSide.Controls.Add(_lblMixCountdown);
            _pnlMixSide.Controls.Add(_lblMixHeader);

            _pnlCountdownsRow.Controls.Add(_pnlIntroSide, 0, 0);
            _pnlCountdownsRow.Controls.Add(_pnlMixSide, 1, 0);

            // Add in reverse order so DockStyle.Top builds top→bottom correctly
            _pnlOnAir.Controls.Add(_pnlCountdownsRow);
            _pnlOnAir.Controls.Add(_lblOnAirName);
            _pnlOnAir.Controls.Add(_lblOnAirHeader);

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

            float rowH = Math.Max(60f, _mainLayout.Height / 3.0f);
            float countdown = Math.Max(12f, Math.Min(80f, rowH * 0.32f));
            float name     = Math.Max(9f,  Math.Min(22f, rowH * 0.12f));

            ScaleCountdown(_lblIntroCountdown, countdown * 0.8f);
            ScaleCountdown(_lblMixCountdown, countdown * 0.8f);
            ScaleCountdown(_lblScheduleCountdown, countdown);
            ScaleCountdown(_lblAdCountdown, countdown);

            ScaleName(_lblOnAirName, name);
            ScaleName(_lblScheduleName, name);
            ScaleName(_lblAdInfo, name);
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
                _lblOnAirName.Text = "--";
                SetNoIntro();
                SetNoMix();
                return;
            }

            var current = _playlistQueue.GetCurrentPlayingItem();
            if (current == null)
            {
                _lblOnAirName.Text = LanguageManager.GetString("TimersForm.NoOnAir", "Nessun elemento in onda");
                SetNoIntro();
                SetNoMix();
                return;
            }

            _lblOnAirName.Text = !string.IsNullOrEmpty(current.Artist)
                ? $"{current.Artist} – {current.Title}"
                : current.Title ?? "--";

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
                _pnlIntroSide.BackColor = Color.Red;
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
        }

        private void SetNoIntro()
        {
            _lblIntroCountdown.Text = "--:--:--";
            _lblIntroCountdown.ForeColor = Color.FromArgb(120, 120, 120);
            _pnlIntroSide.BackColor = Color.FromArgb(60, 0, 0);
        }

        private void SetNoMix()
        {
            _lblMixCountdown.Text = "--:--:--";
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
                    _lblScheduleCountdown.Text = $"{sec / 3600:00}:{(sec % 3600) / 60:00}:{sec % 60:00}";

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
                    _lblAdCountdown.Text = $"{sec / 3600:00}:{(sec % 3600) / 60:00}:{sec % 60:00}";

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
            return $"{s / 3600:00}:{(s % 3600) / 60:00}:{s % 60:00}";
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
                        var p = lines[i].Split(',');
                        if (p.Length < 12) continue;
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
                            CampaignName  = p[9],
                            CategoryName  = p[10],
                            IsActive      = bool.Parse(p[11])
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
