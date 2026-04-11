using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using AirDirector.Services.Database;
using AirDirector.Services.Localization;
using AirDirector.Themes;

namespace AirDirector.Forms
{
    public partial class MusicStatisticsForm : Form
    {
        // Header controls
        private Panel headerPanel;
        private Label lblTitle;
        private DateTimePicker dtpFrom;
        private DateTimePicker dtpTo;
        private Button btnLast7;
        private Button btnLast15;
        private Button btnLast30;
        private Button btnLastMonth;
        private Button btnUpdate;
        private Button btnExportCsv;

        // Tab control
        private TabControl tabStats;
        private TabPage tabTopTracks;
        private TabPage tabTopArtists;
        private TabPage tabDailyTrend;
        private TabPage tabHourly;
        private TabPage tabWeekday;
        private TabPage tabAvgDuration;
        private TabPage tabRotation;
        private TabPage tabSummary;
        private TabPage tabSingleTrack;
        private TabPage tabSingleArtist;

        // Single track tab
        private ComboBox cmbTrack;
        private Panel pnlTrackDetails;

        // Single artist tab
        private ComboBox cmbArtist;
        private Panel pnlArtistDetails;

        private List<ReportEntry> _data;

        public MusicStatisticsForm()
        {
            InitializeComponent();
            InitializeUI();
            ApplyLanguage();
            _data = new List<ReportEntry>();
            LanguageManager.LanguageChanged += OnLanguageChanged;
            this.Load += (s, e) => LoadAndUpdate();
        }

        private void OnLanguageChanged(object sender, EventArgs e) => ApplyLanguage();

        private void ApplyLanguage()
        {
            this.Text = "📊 " + LanguageManager.GetString("MusicStatistics.Title", "Statistiche Musica");
            if (lblTitle != null) lblTitle.Text = "📊 " + LanguageManager.GetString("MusicStatistics.Title", "Statistiche Musica");
            if (btnUpdate != null) btnUpdate.Text = "🔄 " + LanguageManager.GetString("MusicStatistics.Update", "Aggiorna");
            if (btnExportCsv != null) btnExportCsv.Text = "💾 " + LanguageManager.GetString("MusicStatistics.ExportCsv", "Esporta CSV");
            if (btnLast7 != null) btnLast7.Text = "7gg";
            if (btnLast15 != null) btnLast15.Text = "15gg";
            if (btnLast30 != null) btnLast30.Text = "30gg";
            if (btnLastMonth != null) btnLastMonth.Text = LanguageManager.GetString("MusicStatistics.LastMonth", "Mese scorso");
            UpdateTabTitles();
        }

        private void UpdateTabTitles()
        {
            if (tabStats == null) return;
            if (tabTopTracks != null) tabTopTracks.Text = LanguageManager.GetString("MusicStatistics.TopTracks", "Top Brani");
            if (tabTopArtists != null) tabTopArtists.Text = LanguageManager.GetString("MusicStatistics.TopArtists", "Top Artisti");
            if (tabDailyTrend != null) tabDailyTrend.Text = LanguageManager.GetString("MusicStatistics.DailyTrend", "Trend Giornaliero");
            if (tabHourly != null) tabHourly.Text = LanguageManager.GetString("MusicStatistics.HourlyDist", "Distribuzione Oraria");
            if (tabWeekday != null) tabWeekday.Text = LanguageManager.GetString("MusicStatistics.WeekdayDist", "Distribuzione Settimana");
            if (tabAvgDuration != null) tabAvgDuration.Text = LanguageManager.GetString("MusicStatistics.AvgDuration", "Durata Media/Ora");
            if (tabRotation != null) tabRotation.Text = LanguageManager.GetString("MusicStatistics.Rotation", "Indice Rotazione");
            if (tabSummary != null) tabSummary.Text = LanguageManager.GetString("MusicStatistics.Summary", "Riepilogo");
            if (tabSingleTrack != null) tabSingleTrack.Text = LanguageManager.GetString("MusicStatistics.SingleTrack", "Singolo Brano");
            if (tabSingleArtist != null) tabSingleArtist.Text = LanguageManager.GetString("MusicStatistics.SingleArtist", "Singolo Artista");
        }

        private void InitializeUI()
        {
            this.BackColor = Color.FromArgb(28, 28, 28);
            this.ForeColor = Color.White;
            this.MinimumSize = new Size(1000, 700);
            this.WindowState = FormWindowState.Maximized;

            // Header panel
            headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = AppTheme.BgDark,
                Padding = new Padding(10, 8, 10, 8)
            };

            lblTitle = new Label
            {
                Text = "📊 Statistiche Musica",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(10, 12),
                AutoSize = true
            };
            headerPanel.Controls.Add(lblTitle);

            int x = 280;
            var lblFrom = new Label { Text = "Da:", ForeColor = Color.White, Font = new Font("Segoe UI", 9), Location = new Point(x, 17), AutoSize = true };
            headerPanel.Controls.Add(lblFrom);
            x += 30;

            dtpFrom = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30), Location = new Point(x, 13), Size = new Size(110, 25) };
            headerPanel.Controls.Add(dtpFrom);
            x += 120;

            var lblTo = new Label { Text = "A:", ForeColor = Color.White, Font = new Font("Segoe UI", 9), Location = new Point(x, 17), AutoSize = true };
            headerPanel.Controls.Add(lblTo);
            x += 25;

            dtpTo = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today, Location = new Point(x, 13), Size = new Size(110, 25) };
            headerPanel.Controls.Add(dtpTo);
            x += 120;

            btnLast7 = CreateSmallButton("7gg", x, () => SetRange(7)); x += 55;
            btnLast15 = CreateSmallButton("15gg", x, () => SetRange(15)); x += 60;
            btnLast30 = CreateSmallButton("30gg", x, () => SetRange(30)); x += 65;
            btnLastMonth = CreateSmallButton("Mese scorso", x, SetLastMonth); x += 110;

            headerPanel.Controls.Add(btnLast7);
            headerPanel.Controls.Add(btnLast15);
            headerPanel.Controls.Add(btnLast30);
            headerPanel.Controls.Add(btnLastMonth);

            btnUpdate = new Button
            {
                Text = "🔄 Aggiorna",
                Location = new Point(x, 11),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnUpdate.FlatAppearance.BorderSize = 0;
            btnUpdate.Click += (s, e) => LoadAndUpdate();
            headerPanel.Controls.Add(btnUpdate);
            x += 110;

            btnExportCsv = new Button
            {
                Text = "💾 Esporta CSV",
                Location = new Point(x, 11),
                Size = new Size(130, 30),
                BackColor = Color.FromArgb(0, 150, 136),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnExportCsv.FlatAppearance.BorderSize = 0;
            btnExportCsv.Click += BtnExportCsv_Click;
            headerPanel.Controls.Add(btnExportCsv);

            this.Controls.Add(headerPanel);

            // TabControl
            tabStats = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9),
                Padding = new Point(10, 5)
            };

            tabTopTracks = new TabPage("Top Brani") { BackColor = Color.FromArgb(30, 30, 30) };
            tabTopArtists = new TabPage("Top Artisti") { BackColor = Color.FromArgb(30, 30, 30) };
            tabDailyTrend = new TabPage("Trend Giornaliero") { BackColor = Color.FromArgb(30, 30, 30) };
            tabHourly = new TabPage("Distribuzione Oraria") { BackColor = Color.FromArgb(30, 30, 30) };
            tabWeekday = new TabPage("Distribuzione Settimana") { BackColor = Color.FromArgb(30, 30, 30) };
            tabAvgDuration = new TabPage("Durata Media/Ora") { BackColor = Color.FromArgb(30, 30, 30) };
            tabRotation = new TabPage("Indice Rotazione") { BackColor = Color.FromArgb(30, 30, 30) };
            tabSummary = new TabPage("Riepilogo") { BackColor = Color.FromArgb(30, 30, 30) };
            tabSingleTrack = new TabPage("Singolo Brano") { BackColor = Color.FromArgb(30, 30, 30) };
            tabSingleArtist = new TabPage("Singolo Artista") { BackColor = Color.FromArgb(30, 30, 30) };

            tabStats.TabPages.AddRange(new[] { tabTopTracks, tabTopArtists, tabDailyTrend, tabHourly, tabWeekday, tabAvgDuration, tabRotation, tabSummary, tabSingleTrack, tabSingleArtist });

            // Setup Single Track tab
            SetupSingleTrackTab();
            SetupSingleArtistTab();

            this.Controls.Add(tabStats);
        }

        private Button CreateSmallButton(string text, int x, Action onClick)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, 13),
                Size = new Size(50, 28),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private void SetRange(int days)
        {
            dtpFrom.Value = DateTime.Today.AddDays(-days);
            dtpTo.Value = DateTime.Today;
            LoadAndUpdate();
        }

        private void SetLastMonth()
        {
            var now = DateTime.Today;
            dtpFrom.Value = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
            dtpTo.Value = new DateTime(now.Year, now.Month, 1).AddDays(-1);
            LoadAndUpdate();
        }

        private void SetupSingleTrackTab()
        {
            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 45, BackColor = Color.FromArgb(35, 35, 35), Padding = new Padding(10, 8, 10, 8) };
            var lblTrack = new Label { Text = "Brano:", ForeColor = Color.White, Font = new Font("Segoe UI", 9), Location = new Point(10, 13), AutoSize = true };
            cmbTrack = new ComboBox { Location = new Point(60, 10), Size = new Size(400, 25), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            cmbTrack.SelectedIndexChanged += CmbTrack_SelectedIndexChanged;
            pnlTop.Controls.Add(lblTrack);
            pnlTop.Controls.Add(cmbTrack);
            tabSingleTrack.Controls.Add(pnlTop);

            pnlTrackDetails = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(30, 30, 30), Padding = new Padding(10) };
            tabSingleTrack.Controls.Add(pnlTrackDetails);
        }

        private void SetupSingleArtistTab()
        {
            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 45, BackColor = Color.FromArgb(35, 35, 35), Padding = new Padding(10, 8, 10, 8) };
            var lblArtist = new Label { Text = "Artista:", ForeColor = Color.White, Font = new Font("Segoe UI", 9), Location = new Point(10, 13), AutoSize = true };
            cmbArtist = new ComboBox { Location = new Point(65, 10), Size = new Size(300, 25), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            cmbArtist.SelectedIndexChanged += CmbArtist_SelectedIndexChanged;
            pnlTop.Controls.Add(lblArtist);
            pnlTop.Controls.Add(cmbArtist);
            tabSingleArtist.Controls.Add(pnlTop);

            pnlArtistDetails = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(30, 30, 30), Padding = new Padding(10) };
            tabSingleArtist.Controls.Add(pnlArtistDetails);
        }

        private void LoadAndUpdate()
        {
            try
            {
                _data = ReportManager.LoadReport(dtpFrom.Value, dtpTo.Value)
                    .Where(e => string.Equals(e.Type, "music", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                BuildTopTracksChart();
                BuildTopArtistsChart();
                BuildDailyTrendChart();
                BuildHourlyDistChart();
                BuildWeekdayDistChart();
                BuildAvgDurationChart();
                BuildRotationChart();
                BuildSummaryPanel();
                PopulateCombos();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, LanguageManager.GetString("Common.Error", "Errore"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BuildTopTracksChart()
        {
            tabTopTracks.Controls.Clear();
            var topTracks = _data
                .GroupBy(e => new { e.Artist, e.Title })
                .Select(g => new { Label = $"{g.Key.Artist} - {g.Key.Title}", Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(20)
                .ToList();

            var chart = new ChartPanel(
                topTracks.Select(t => t.Label).ToList(),
                topTracks.Select(t => (double)t.Count).ToList(),
                LanguageManager.GetString("MusicStatistics.TopTracks", "Top 20 Brani"),
                Color.FromArgb(33, 150, 243),
                ChartType.HorizontalBar
            ) { Dock = DockStyle.Fill };
            tabTopTracks.Controls.Add(chart);
        }

        private void BuildTopArtistsChart()
        {
            tabTopArtists.Controls.Clear();
            var topArtists = _data
                .GroupBy(e => e.Artist)
                .Select(g => new { Label = g.Key ?? "?", Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(20)
                .ToList();

            var chart = new ChartPanel(
                topArtists.Select(t => t.Label).ToList(),
                topArtists.Select(t => (double)t.Count).ToList(),
                LanguageManager.GetString("MusicStatistics.TopArtists", "Top 20 Artisti"),
                Color.FromArgb(76, 175, 80),
                ChartType.HorizontalBar
            ) { Dock = DockStyle.Fill };
            tabTopArtists.Controls.Add(chart);
        }

        private void BuildDailyTrendChart()
        {
            tabDailyTrend.Controls.Clear();
            var daily = _data
                .GroupBy(e => e.Date.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToList();

            var chart = new ChartPanel(
                daily.Select(d => d.Date.ToString("dd/MM")).ToList(),
                daily.Select(d => (double)d.Count).ToList(),
                LanguageManager.GetString("MusicStatistics.DailyTrend", "Brani per Giorno"),
                Color.FromArgb(255, 152, 0),
                ChartType.Line
            ) { Dock = DockStyle.Fill };
            tabDailyTrend.Controls.Add(chart);
        }

        private void BuildHourlyDistChart()
        {
            tabHourly.Controls.Clear();
            var hourly = Enumerable.Range(0, 24).Select(h => new
            {
                Hour = h,
                Count = _data.Count(e =>
                {
                    if (TimeSpan.TryParse(e.StartTime, out TimeSpan t)) return t.Hours == h;
                    return false;
                })
            }).ToList();

            var chart = new ChartPanel(
                hourly.Select(h => $"{h.Hour:00}:00").ToList(),
                hourly.Select(h => (double)h.Count).ToList(),
                LanguageManager.GetString("MusicStatistics.HourlyDist", "Distribuzione Oraria"),
                Color.FromArgb(156, 39, 176),
                ChartType.Bar
            ) { Dock = DockStyle.Fill };
            tabHourly.Controls.Add(chart);
        }

        private void BuildWeekdayDistChart()
        {
            tabWeekday.Controls.Clear();
            string[] weekdays = { "Dom", "Lun", "Mar", "Mer", "Gio", "Ven", "Sab" };
            var weekdayData = Enumerable.Range(0, 7).Select(d => new
            {
                Day = d,
                Count = _data.Count(e => (int)e.Date.DayOfWeek == d)
            }).ToList();

            var chart = new ChartPanel(
                weekdayData.Select(d => weekdays[d.Day]).ToList(),
                weekdayData.Select(d => (double)d.Count).ToList(),
                LanguageManager.GetString("MusicStatistics.WeekdayDist", "Distribuzione per Giorno"),
                Color.FromArgb(0, 188, 212),
                ChartType.Bar
            ) { Dock = DockStyle.Fill };
            tabWeekday.Controls.Add(chart);
        }

        private void BuildAvgDurationChart()
        {
            tabAvgDuration.Controls.Clear();
            var avgByHour = Enumerable.Range(0, 24).Select(h =>
            {
                var entries = _data.Where(e =>
                {
                    if (TimeSpan.TryParse(e.StartTime, out TimeSpan t)) return t.Hours == h;
                    return false;
                }).ToList();

                double avgSec = 0;
                if (entries.Count > 0)
                {
                    var durations = entries
                        .Where(e => TimeSpan.TryParse(e.PlayDuration, out _))
                        .Select(e => { TimeSpan.TryParse(e.PlayDuration, out TimeSpan d); return d.TotalSeconds; });
                    if (durations.Any()) avgSec = durations.Average();
                }
                return new { Hour = h, AvgSec = avgSec };
            }).ToList();

            var chart = new ChartPanel(
                avgByHour.Select(h => $"{h.Hour:00}:00").ToList(),
                avgByHour.Select(h => h.AvgSec).ToList(),
                LanguageManager.GetString("MusicStatistics.AvgDuration", "Durata Media per Ora (sec)"),
                Color.FromArgb(255, 193, 7),
                ChartType.Bar
            ) { Dock = DockStyle.Fill };
            tabAvgDuration.Controls.Add(chart);
        }

        private void BuildRotationChart()
        {
            tabRotation.Controls.Clear();
            var rotation = _data
                .GroupBy(e => new { e.Artist, e.Title })
                .Select(g =>
                {
                    var dates = g.Select(e => e.Date.Date).Distinct().OrderBy(d => d).ToList();
                    double avgGapDays = 0;
                    if (dates.Count > 1)
                    {
                        var gaps = new List<double>();
                        for (int i = 1; i < dates.Count; i++)
                            gaps.Add((dates[i] - dates[i - 1]).TotalDays);
                        avgGapDays = gaps.Average();
                    }
                    return new { Label = $"{g.Key.Artist} - {g.Key.Title}", Count = g.Count(), AvgGap = avgGapDays };
                })
                .Where(x => x.Count >= 3)
                .OrderBy(x => x.AvgGap)
                .Take(20)
                .ToList();

            var chart = new ChartPanel(
                rotation.Select(r => r.Label).ToList(),
                rotation.Select(r => r.AvgGap).ToList(),
                LanguageManager.GetString("MusicStatistics.Rotation", "Indice Rotazione - Gap Medio (giorni)"),
                Color.FromArgb(244, 67, 54),
                ChartType.HorizontalBar
            ) { Dock = DockStyle.Fill };
            tabRotation.Controls.Add(chart);
        }

        private void BuildSummaryPanel()
        {
            tabSummary.Controls.Clear();
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(30, 30, 30), AutoScroll = true, Padding = new Padding(20) };

            int totalTracks = _data.Count;
            int uniqueTracks = _data.Select(e => e.Artist + "|" + e.Title).Distinct().Count();
            int uniqueArtists = _data.Select(e => e.Artist).Distinct().Count();
            TimeSpan totalDuration = TimeSpan.Zero;
            foreach (var e in _data)
            {
                if (TimeSpan.TryParse(e.PlayDuration, out TimeSpan d)) totalDuration += d;
            }

            var topArtist = _data.GroupBy(e => e.Artist).OrderByDescending(g => g.Count()).FirstOrDefault();
            var topTrack = _data.GroupBy(e => new { e.Artist, e.Title }).OrderByDescending(g => g.Count()).FirstOrDefault();

            var lines = new[]
            {
                ("📊 Periodo:", $"{dtpFrom.Value:dd/MM/yyyy} - {dtpTo.Value:dd/MM/yyyy}"),
                ("🎵 Totale passaggi:", totalTracks.ToString()),
                ("🎶 Brani unici:", uniqueTracks.ToString()),
                ("🎤 Artisti unici:", uniqueArtists.ToString()),
                ("⏱️ Durata totale:", totalDuration.ToString(@"d\d\ hh\:mm\:ss")),
                ("⏱️ Durata media:", totalTracks > 0 ? TimeSpan.FromSeconds(totalDuration.TotalSeconds / totalTracks).ToString(@"mm\:ss") : "-"),
                ("🏆 Top Artista:", topArtist != null ? $"{topArtist.Key} ({topArtist.Count()} passaggi)" : "-"),
                ("🏆 Top Brano:", topTrack != null ? $"{topTrack.Key.Artist} - {topTrack.Key.Title} ({topTrack.Count()} passaggi)" : "-"),
            };

            int y = 20;
            foreach (var (label, value) in lines)
            {
                var lblKey = new Label { Text = label, Location = new Point(20, y), AutoSize = true, ForeColor = Color.Silver, Font = new Font("Segoe UI", 11) };
                var lblVal = new Label { Text = value, Location = new Point(250, y), AutoSize = true, ForeColor = Color.White, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
                pnl.Controls.Add(lblKey);
                pnl.Controls.Add(lblVal);
                y += 35;
            }

            tabSummary.Controls.Add(pnl);
        }

        private void PopulateCombos()
        {
            // Tracks combo
            cmbTrack.Items.Clear();
            var tracks = _data.Select(e => $"{e.Artist} - {e.Title}").Distinct().OrderBy(s => s).ToArray();
            cmbTrack.Items.AddRange(tracks);
            if (cmbTrack.Items.Count > 0) cmbTrack.SelectedIndex = 0;

            // Artists combo
            cmbArtist.Items.Clear();
            var artists = _data.Select(e => e.Artist).Where(a => !string.IsNullOrEmpty(a)).Distinct().OrderBy(s => s).ToArray();
            cmbArtist.Items.AddRange(artists);
            if (cmbArtist.Items.Count > 0) cmbArtist.SelectedIndex = 0;
        }

        private void CmbTrack_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbTrack.SelectedItem == null) return;
            string selected = cmbTrack.SelectedItem.ToString();
            var parts = selected.Split(new[] { " - " }, 2, StringSplitOptions.None);
            if (parts.Length < 2) return;

            string artist = parts[0];
            string title = parts[1];

            var entries = _data
                .Where(x => string.Equals(x.Artist, artist, StringComparison.OrdinalIgnoreCase) &&
                             string.Equals(x.Title, title, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.StartTime)
                .ToList();

            BuildTrackDetailsPanel(entries, artist, title);
        }

        private void BuildTrackDetailsPanel(List<ReportEntry> entries, string artist, string title)
        {
            pnlTrackDetails.Controls.Clear();

            var lblInfo = new Label
            {
                Text = $"🎵 {artist} - {title}  |  {entries.Count} passaggi",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 35,
                Padding = new Padding(5)
            };
            pnlTrackDetails.Controls.Add(lblInfo);

            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                GridColor = Color.FromArgb(60, 60, 60),
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight = 35,
                RowTemplate = { Height = 30 },
                Font = new Font("Segoe UI", 10),
                EnableHeadersVisualStyles = false
            };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(50, 50, 50);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.DefaultCellStyle.BackColor = Color.FromArgb(40, 40, 40);
            dgv.DefaultCellStyle.ForeColor = Color.White;
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 215);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(35, 35, 35);

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "Data", FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "StartTime", HeaderText = "Ora", FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "PlayDuration", HeaderText = "Durata", FillWeight = 15 });

            foreach (var entry in entries)
                dgv.Rows.Add(entry.Date.ToString("dd/MM/yyyy"), entry.StartTime, entry.PlayDuration);

            pnlTrackDetails.Controls.Add(dgv);
        }

        private void CmbArtist_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbArtist.SelectedItem == null) return;
            string artist = cmbArtist.SelectedItem.ToString();

            var entries = _data
                .Where(x => string.Equals(x.Artist, artist, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.StartTime)
                .ToList();

            BuildArtistDetailsPanel(entries, artist);
        }

        private void BuildArtistDetailsPanel(List<ReportEntry> entries, string artist)
        {
            pnlArtistDetails.Controls.Clear();

            int uniqueTracks = entries.Select(e => e.Title).Distinct().Count();
            var lblInfo = new Label
            {
                Text = $"🎤 {artist}  |  {entries.Count} passaggi  |  {uniqueTracks} brani unici",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 35,
                Padding = new Padding(5)
            };
            pnlArtistDetails.Controls.Add(lblInfo);

            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                GridColor = Color.FromArgb(60, 60, 60),
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight = 35,
                RowTemplate = { Height = 30 },
                Font = new Font("Segoe UI", 10),
                EnableHeadersVisualStyles = false
            };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(50, 50, 50);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.DefaultCellStyle.BackColor = Color.FromArgb(40, 40, 40);
            dgv.DefaultCellStyle.ForeColor = Color.White;
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 215);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(35, 35, 35);

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Title", HeaderText = "Titolo", FillWeight = 40 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "Data", FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "StartTime", HeaderText = "Ora", FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "PlayDuration", HeaderText = "Durata", FillWeight = 15 });

            foreach (var entry in entries)
                dgv.Rows.Add(entry.Title, entry.Date.ToString("dd/MM/yyyy"), entry.StartTime, entry.PlayDuration);

            pnlArtistDetails.Controls.Add(dgv);
        }

        private void BtnExportCsv_Click(object sender, EventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Filter = "File CSV (*.csv)|*.csv",
                FileName = $"Statistiche_{DateTime.Now:yyyy-MM-dd_HH-mm}.csv",
                Title = "Esporta Statistiche CSV"
            };

            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                string csv = BuildCsvForCurrentTab();
                File.WriteAllText(dlg.FileName, csv, Encoding.UTF8);
                MessageBox.Show(
                    $"✅ Esportazione completata!\n{dlg.FileName}",
                    "Esportazione completata",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, LanguageManager.GetString("Common.Error", "Errore"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string BuildCsvForCurrentTab()
        {
            var sb = new StringBuilder();
            int tabIdx = tabStats.SelectedIndex;

            if (tabIdx == 0) // Top Tracks
            {
                sb.AppendLine("Artista;Titolo;Passaggi");
                var data = _data.GroupBy(e => new { e.Artist, e.Title })
                    .Select(g => new { g.Key.Artist, g.Key.Title, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(20);
                foreach (var row in data) sb.AppendLine($"{row.Artist};{row.Title};{row.Count}");
            }
            else if (tabIdx == 1) // Top Artists
            {
                sb.AppendLine("Artista;Passaggi");
                var data = _data.GroupBy(e => e.Artist)
                    .Select(g => new { Artist = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(20);
                foreach (var row in data) sb.AppendLine($"{row.Artist};{row.Count}");
            }
            else if (tabIdx == 2) // Daily Trend
            {
                sb.AppendLine("Data;Passaggi");
                var data = _data.GroupBy(e => e.Date.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .OrderBy(x => x.Date);
                foreach (var row in data) sb.AppendLine($"{row.Date:dd/MM/yyyy};{row.Count}");
            }
            else if (tabIdx == 3) // Hourly
            {
                sb.AppendLine("Ora;Passaggi");
                for (int h = 0; h < 24; h++)
                {
                    int count = _data.Count(e => { if (TimeSpan.TryParse(e.StartTime, out TimeSpan t)) return t.Hours == h; return false; });
                    sb.AppendLine($"{h:00}:00;{count}");
                }
            }
            else // Summary
            {
                sb.AppendLine("Dato;Valore");
                sb.AppendLine($"Totale passaggi;{_data.Count}");
                sb.AppendLine("Brani unici;" + _data.Select(e => e.Artist + "|" + e.Title).Distinct().Count());
                sb.AppendLine($"Artisti unici;{_data.Select(e => e.Artist).Distinct().Count()}");
            }

            return sb.ToString();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                LanguageManager.LanguageChanged -= OnLanguageChanged;
            }
            base.Dispose(disposing);
        }

        // ===== Inner class: ChartPanel =====
        private enum ChartType { Bar, HorizontalBar, Line }

        private class ChartPanel : Panel
        {
            private readonly List<string> _labels;
            private readonly List<double> _values;
            private readonly string _title;
            private readonly Color _color;
            private readonly ChartType _chartType;

            public ChartPanel(List<string> labels, List<double> values, string title, Color color, ChartType chartType)
            {
                _labels = labels ?? new List<string>();
                _values = values ?? new List<double>();
                _title = title;
                _color = color;
                _chartType = chartType;
                this.DoubleBuffered = true;
                this.BackColor = Color.FromArgb(30, 30, 30);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                if (_values.Count == 0)
                {
                    DrawNoData(g);
                    return;
                }

                switch (_chartType)
                {
                    case ChartType.Bar: DrawBarChart(g); break;
                    case ChartType.HorizontalBar: DrawHorizontalBarChart(g); break;
                    case ChartType.Line: DrawLineChart(g); break;
                }
            }

            private void DrawNoData(Graphics g)
            {
                string msg = "Nessun dato disponibile";
                using (var font = new Font("Segoe UI", 14))
                using (var brush = new SolidBrush(Color.Silver))
                {
                    var size = g.MeasureString(msg, font);
                    g.DrawString(msg, font, brush, (Width - size.Width) / 2, (Height - size.Height) / 2);
                }
            }

            private void DrawBarChart(Graphics g)
            {
                if (_values.Count == 0) return;
                double maxVal = _values.Max();
                if (maxVal <= 0) maxVal = 1;

                int margin = 40;
                int bottomMargin = 60;
                int topMargin = 40;
                int chartW = Width - margin * 2;
                int chartH = Height - topMargin - bottomMargin;
                int n = _values.Count;
                float barW = Math.Max(2, (float)(chartW - n) / n);

                // Title
                using (var titleFont = new Font("Segoe UI", 11, FontStyle.Bold))
                using (var titleBrush = new SolidBrush(Color.White))
                    g.DrawString(_title, titleFont, titleBrush, margin, 10);

                // Bars
                using (var barBrush = new SolidBrush(_color))
                using (var labelFont = new Font("Segoe UI", 7))
                using (var labelBrush = new SolidBrush(Color.Silver))
                using (var valueBrush = new SolidBrush(Color.White))
                {
                    for (int i = 0; i < n; i++)
                    {
                        float x = margin + i * (barW + 1);
                        float barH = (float)(_values[i] / maxVal * chartH);
                        float y = topMargin + (chartH - barH);
                        g.FillRectangle(barBrush, x, y, barW, barH);

                        if (i < _labels.Count)
                        {
                            string lbl = _labels[i].Length > 8 ? _labels[i].Substring(0, 7) + "…" : _labels[i];
                            var lblSize = g.MeasureString(lbl, labelFont);
                            g.DrawString(lbl, labelFont, labelBrush, x + barW / 2 - lblSize.Width / 2, topMargin + chartH + 5);
                        }

                        if (_values[i] > 0)
                        {
                            string valStr = _values[i] % 1 == 0 ? ((int)_values[i]).ToString() : _values[i].ToString("F1");
                            var valSize = g.MeasureString(valStr, labelFont);
                            g.DrawString(valStr, labelFont, valueBrush, x + barW / 2 - valSize.Width / 2, y - 14);
                        }
                    }
                }

                // Axes
                using (var axisPen = new Pen(Color.FromArgb(80, 80, 80)))
                {
                    g.DrawLine(axisPen, margin, topMargin, margin, topMargin + chartH);
                    g.DrawLine(axisPen, margin, topMargin + chartH, margin + chartW, topMargin + chartH);
                }
            }

            private void DrawHorizontalBarChart(Graphics g)
            {
                if (_values.Count == 0) return;
                double maxVal = _values.Max();
                if (maxVal <= 0) maxVal = 1;

                int labelW = 200;
                int margin = 10;
                int topMargin = 40;
                int chartW = Width - labelW - margin * 2 - 60;
                int n = _values.Count;
                int barH = Math.Max(8, (Height - topMargin - margin) / Math.Max(n, 1) - 2);

                using (var titleFont = new Font("Segoe UI", 11, FontStyle.Bold))
                using (var titleBrush = new SolidBrush(Color.White))
                    g.DrawString(_title, titleFont, titleBrush, margin, 10);

                using (var barBrush = new SolidBrush(_color))
                using (var labelFont = new Font("Segoe UI", 8))
                using (var labelBrush = new SolidBrush(Color.White))
                using (var valueBrush = new SolidBrush(Color.LightGray))
                {
                    for (int i = 0; i < n; i++)
                    {
                        float y = topMargin + i * (barH + 3);
                        float barW2 = (float)(_values[i] / maxVal * chartW);

                        // Label
                        string lbl = i < _labels.Count ? _labels[i] : "";
                        if (lbl.Length > 28) lbl = lbl.Substring(0, 27) + "…";
                        g.DrawString(lbl, labelFont, labelBrush, margin, y + barH / 2 - 7);

                        // Bar
                        g.FillRectangle(barBrush, labelW, y, barW2, barH);

                        // Value
                        string valStr = _values[i] % 1 == 0 ? ((int)_values[i]).ToString() : _values[i].ToString("F1");
                        g.DrawString(valStr, labelFont, valueBrush, labelW + barW2 + 4, y + barH / 2 - 7);
                    }
                }
            }

            private void DrawLineChart(Graphics g)
            {
                if (_values.Count < 2) { DrawBarChart(g); return; }
                double maxVal = _values.Max();
                if (maxVal <= 0) maxVal = 1;

                int margin = 50;
                int bottomMargin = 60;
                int topMargin = 40;
                int chartW = Width - margin * 2;
                int chartH = Height - topMargin - bottomMargin;

                using (var titleFont = new Font("Segoe UI", 11, FontStyle.Bold))
                using (var titleBrush = new SolidBrush(Color.White))
                    g.DrawString(_title, titleFont, titleBrush, margin, 10);

                var points = new PointF[_values.Count];
                for (int i = 0; i < _values.Count; i++)
                {
                    float x = margin + (float)i / (_values.Count - 1) * chartW;
                    float y = topMargin + (float)(1 - _values[i] / maxVal) * chartH;
                    points[i] = new PointF(x, y);
                }

                using (var linePen = new Pen(_color, 2))
                    g.DrawLines(linePen, points);

                using (var dotBrush = new SolidBrush(_color))
                    foreach (var pt in points)
                        g.FillEllipse(dotBrush, pt.X - 3, pt.Y - 3, 6, 6);

                using (var labelFont = new Font("Segoe UI", 7))
                using (var labelBrush = new SolidBrush(Color.Silver))
                using (var axisPen = new Pen(Color.FromArgb(80, 80, 80)))
                {
                    g.DrawLine(axisPen, margin, topMargin, margin, topMargin + chartH);
                    g.DrawLine(axisPen, margin, topMargin + chartH, margin + chartW, topMargin + chartH);

                    int step = Math.Max(1, _values.Count / 20);
                    for (int i = 0; i < _values.Count; i += step)
                    {
                        if (i < _labels.Count)
                        {
                            string lbl = _labels[i].Length > 6 ? _labels[i].Substring(0, 5) + "…" : _labels[i];
                            g.DrawString(lbl, labelFont, labelBrush, points[i].X - 15, topMargin + chartH + 5);
                        }
                    }
                }
            }
        }
    }
}
