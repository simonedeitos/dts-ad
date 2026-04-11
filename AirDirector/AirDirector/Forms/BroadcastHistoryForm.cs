using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AirDirector.Services.Database;
using AirDirector.Services.Localization;
using AirDirector.Themes;

namespace AirDirector.Forms
{
    public partial class BroadcastHistoryForm : Form
    {
        // Filter panel controls
        private Panel filterPanel;
        private Label lblFrom;
        private Label lblTo;
        private DateTimePicker dtpFrom;
        private DateTimePicker dtpTo;
        private Label lblType;
        private ComboBox cmbType;
        private Label lblSearch;
        private TextBox txtSearch;
        private Button btnRefresh;
        private Button btnExport;

        // DataGridView
        private DataGridView dgvHistory;

        // Stats panel
        private Panel statsPanel;
        private Label lblTotal;
        private Label lblTotalDuration;
        private Label lblTopArtist;
        private Label lblTopTrack;
        private Button btnStatistics;

        private List<ReportEntry> _currentData;
        private List<ReportEntry> _filteredData;

        public BroadcastHistoryForm()
        {
            InitializeComponent();
            InitializeUI();
            ApplyLanguage();
            _currentData = new List<ReportEntry>();
            _filteredData = new List<ReportEntry>();
            LanguageManager.LanguageChanged += OnLanguageChanged;
            this.Load += (s, e) => LoadHistory();
        }

        private void OnLanguageChanged(object sender, EventArgs e) => ApplyLanguage();

        private void ApplyLanguage()
        {
            this.Text = "📻 " + LanguageManager.GetString("BroadcastHistory.Title", "Storico Trasmesso");
            if (lblFrom != null) lblFrom.Text = LanguageManager.GetString("Report.From", "Da:");
            if (lblTo != null) lblTo.Text = LanguageManager.GetString("Report.To", "A:");
            if (lblType != null) lblType.Text = LanguageManager.GetString("BroadcastHistory.Type", "Tipo:");
            if (lblSearch != null) lblSearch.Text = LanguageManager.GetString("BroadcastHistory.Search", "Cerca:");
            if (btnRefresh != null) btnRefresh.Text = "🔄 " + LanguageManager.GetString("BroadcastHistory.Refresh", "Aggiorna");
            if (btnExport != null) btnExport.Text = "💾 " + LanguageManager.GetString("Report.ExportCSV", "Esporta CSV");
            if (btnStatistics != null) btnStatistics.Text = "📊 " + LanguageManager.GetString("BroadcastHistory.Statistics", "Statistiche");
            UpdateGridHeaders();
        }

        private void UpdateGridHeaders()
        {
            if (dgvHistory == null || dgvHistory.Columns.Count < 6) return;
            dgvHistory.Columns["colDate"].HeaderText = "📅 " + LanguageManager.GetString("Report.Column.Date", "Data");
            dgvHistory.Columns["colTime"].HeaderText = "🕐 " + LanguageManager.GetString("Report.Column.StartTime", "Ora");
            dgvHistory.Columns["colType"].HeaderText = "🎵 " + LanguageManager.GetString("Report.Column.Type", "Tipo");
            dgvHistory.Columns["colArtist"].HeaderText = "🎤 " + LanguageManager.GetString("Report.Column.Artist", "Artista");
            dgvHistory.Columns["colTitle"].HeaderText = "🎶 " + LanguageManager.GetString("Report.Column.Title", "Titolo");
            dgvHistory.Columns["colDuration"].HeaderText = "⏱️ " + LanguageManager.GetString("Report.Column.PlayDuration", "Durata");
        }

        private static Color GetArchiveColor(string type)
        {
            if (string.IsNullOrEmpty(type)) return Color.FromArgb(0, 150, 136);
            string lower = type.ToLowerInvariant();
            if (lower == "music") return Color.FromArgb(76, 175, 80);
            if (lower == "clip" || lower == "clips") return Color.FromArgb(255, 152, 0);
            return Color.FromArgb(0, 150, 136);
        }

        private void InitializeUI()
        {
            this.BackColor = Color.FromArgb(28, 28, 28);
            this.ForeColor = Color.White;
            this.MinimumSize = new Size(900, 600);

            // Filter panel
            filterPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Color.FromArgb(40, 40, 40),
                Padding = new Padding(10, 8, 10, 8)
            };

            int x = 10;
            lblFrom = new Label { Text = "Da:", ForeColor = Color.White, Font = new Font("Segoe UI", 9), Location = new Point(x, 17), AutoSize = true };
            filterPanel.Controls.Add(lblFrom);
            x += 30;

            dtpFrom = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-7), Location = new Point(x, 13), Size = new Size(110, 25) };
            filterPanel.Controls.Add(dtpFrom);
            x += 120;

            lblTo = new Label { Text = "A:", ForeColor = Color.White, Font = new Font("Segoe UI", 9), Location = new Point(x, 17), AutoSize = true };
            filterPanel.Controls.Add(lblTo);
            x += 25;

            dtpTo = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today, Location = new Point(x, 13), Size = new Size(110, 25) };
            filterPanel.Controls.Add(dtpTo);
            x += 120;

            lblType = new Label { Text = "Tipo:", ForeColor = Color.White, Font = new Font("Segoe UI", 9), Location = new Point(x, 17), AutoSize = true };
            filterPanel.Controls.Add(lblType);
            x += 40;

            cmbType = new ComboBox
            {
                Location = new Point(x, 13),
                Size = new Size(100, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            cmbType.Items.AddRange(new object[] { "Tutti", "Music", "Clip" });
            cmbType.SelectedIndex = 0;
            cmbType.SelectedIndexChanged += (s, e) => ApplyFilter();
            filterPanel.Controls.Add(cmbType);
            x += 110;

            lblSearch = new Label { Text = "Cerca:", ForeColor = Color.White, Font = new Font("Segoe UI", 9), Location = new Point(x, 17), AutoSize = true };
            filterPanel.Controls.Add(lblSearch);
            x += 50;

            txtSearch = new TextBox
            {
                Location = new Point(x, 13),
                Size = new Size(150, 25),
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            txtSearch.TextChanged += (s, e) => ApplyFilter();
            filterPanel.Controls.Add(txtSearch);
            x += 160;

            btnRefresh = new Button
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
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (s, e) => LoadHistory();
            filterPanel.Controls.Add(btnRefresh);
            x += 110;

            btnExport = new Button
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
            btnExport.FlatAppearance.BorderSize = 0;
            btnExport.Click += BtnExport_Click;
            filterPanel.Controls.Add(btnExport);

            // Stats panel (bottom)
            statsPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Color.FromArgb(35, 35, 35),
                Padding = new Padding(10, 8, 10, 8)
            };

            lblTotal = new Label { Text = "Totale: 0", ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold), Location = new Point(10, 15), AutoSize = true };
            statsPanel.Controls.Add(lblTotal);

            lblTotalDuration = new Label { Text = "Durata: 00:00:00", ForeColor = AppTheme.LEDGreen, Font = new Font("Segoe UI", 9), Location = new Point(110, 15), AutoSize = true };
            statsPanel.Controls.Add(lblTotalDuration);

            lblTopArtist = new Label { Text = "Top Artista: -", ForeColor = Color.FromArgb(0, 150, 136), Font = new Font("Segoe UI", 9), Location = new Point(280, 15), AutoSize = true };
            statsPanel.Controls.Add(lblTopArtist);

            lblTopTrack = new Label { Text = "Top Brano: -", ForeColor = Color.FromArgb(0, 150, 136), Font = new Font("Segoe UI", 9), Location = new Point(500, 15), AutoSize = true };
            statsPanel.Controls.Add(lblTopTrack);

            btnStatistics = new Button
            {
                Text = "📊 Statistiche",
                Size = new Size(130, 32),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            btnStatistics.FlatAppearance.BorderSize = 0;
            btnStatistics.Click += BtnStatistics_Click;
            statsPanel.Controls.Add(btnStatistics);
            statsPanel.Resize += (s, e) => btnStatistics.Location = new Point(statsPanel.Width - btnStatistics.Width - 10, 9);

            // DataGridView
            dgvHistory = new DataGridView
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
                MultiSelect = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight = 40,
                RowTemplate = { Height = 32 },
                Font = new Font("Segoe UI", 10),
                AllowUserToResizeRows = false,
                ScrollBars = ScrollBars.Both
            };

            dgvHistory.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(50, 50, 50);
            dgvHistory.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvHistory.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            dgvHistory.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvHistory.DefaultCellStyle.BackColor = Color.FromArgb(40, 40, 40);
            dgvHistory.DefaultCellStyle.ForeColor = Color.White;
            dgvHistory.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 215);
            dgvHistory.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvHistory.DefaultCellStyle.Padding = new Padding(5);
            dgvHistory.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(35, 35, 35);
            dgvHistory.EnableHeadersVisualStyles = false;

            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate", HeaderText = "📅 Data", FillWeight = 12, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTime", HeaderText = "🕐 Ora", FillWeight = 10, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "colType", HeaderText = "🎵 Tipo", FillWeight = 10, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "colArtist", HeaderText = "🎤 Artista", FillWeight = 25, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft } });
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTitle", HeaderText = "🎶 Titolo", FillWeight = 25, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft } });
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDuration", HeaderText = "⏱️ Durata", FillWeight = 10, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });

            dgvHistory.CellFormatting += DgvHistory_CellFormatting;

            // Add controls - in WinForms the LAST added control is processed FIRST by the layout engine
            // So we add Fill first, then Bottom, then Top
            this.Controls.Add(dgvHistory);    // Fill - added first (processed last, fills remaining space)
            this.Controls.Add(statsPanel);    // Bottom - processed second
            this.Controls.Add(filterPanel);   // Top - added last (processed first, takes top space)
        }

        private void DgvHistory_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgvHistory.Rows.Count) return;
            var row = dgvHistory.Rows[e.RowIndex];
            if (row.Cells["colType"].Value is string type)
            {
                Color typeColor = GetArchiveColor(type);
                if (dgvHistory.Columns[e.ColumnIndex].Name == "colType")
                {
                    e.CellStyle.ForeColor = typeColor;
                    e.CellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
                }
            }
        }

        private void LoadHistory()
        {
            try
            {
                _currentData = ReportManager.LoadReport(dtpFrom.Value, dtpTo.Value);
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, LanguageManager.GetString("Common.Error", "Errore"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyFilter()
        {
            if (_currentData == null) return;

            var filtered = _currentData.AsEnumerable();

            // Type filter
            if (cmbType.SelectedIndex > 0)
            {
                string typeFilter = cmbType.SelectedItem.ToString();
                filtered = filtered.Where(e => string.Equals(e.Type, typeFilter, StringComparison.OrdinalIgnoreCase));
            }

            // Search filter
            string search = txtSearch.Text?.Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(search))
            {
                filtered = filtered.Where(e =>
                    (e.Artist?.ToLowerInvariant().Contains(search) ?? false) ||
                    (e.Title?.ToLowerInvariant().Contains(search) ?? false));
            }

            _filteredData = filtered.OrderByDescending(r => r.Date).ThenByDescending(r => r.StartTime).ToList();
            PopulateGrid();
            UpdateStats();
        }

        private void PopulateGrid()
        {
            dgvHistory.Rows.Clear();
            foreach (var entry in _filteredData)
            {
                dgvHistory.Rows.Add(
                    entry.Date.ToString("dd/MM/yyyy"),
                    entry.StartTime,
                    entry.Type,
                    entry.Artist,
                    entry.Title,
                    entry.PlayDuration
                );
            }
        }

        private void UpdateStats()
        {
            int total = _filteredData.Count;
            lblTotal.Text = $"Totale: {total}";

            // Total duration
            TimeSpan totalDuration = TimeSpan.Zero;
            foreach (var entry in _filteredData)
            {
                if (TimeSpan.TryParse(entry.PlayDuration, out TimeSpan dur))
                    totalDuration += dur;
            }
            lblTotalDuration.Text = $"Durata: {(int)totalDuration.TotalHours:D2}:{totalDuration.Minutes:D2}:{totalDuration.Seconds:D2}";

            // Top artist
            if (_filteredData.Count > 0)
            {
                var topArtist = _filteredData
                    .Where(e => !string.IsNullOrEmpty(e.Artist))
                    .GroupBy(e => e.Artist)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault();
                lblTopArtist.Text = topArtist != null ? $"Top Artista: {topArtist.Key} ({topArtist.Count()})" : "Top Artista: -";

                var topTrack = _filteredData
                    .Where(e => !string.IsNullOrEmpty(e.Title))
                    .GroupBy(e => new { e.Artist, e.Title })
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault();
                lblTopTrack.Text = topTrack != null ? $"Top Brano: {topTrack.Key.Title} ({topTrack.Count()})" : "Top Brano: -";
            }
            else
            {
                lblTopArtist.Text = "Top Artista: -";
                lblTopTrack.Text = "Top Brano: -";
            }
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            if (_filteredData == null || _filteredData.Count == 0)
            {
                MessageBox.Show(
                    LanguageManager.GetString("Report.Error.NoDataToExport", "Nessun dato da esportare."),
                    LanguageManager.GetString("Report.Title.NoData", "Nessun dato"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            using (var exportForm = new ReportExportDialog(_filteredData))
            {
                exportForm.ShowDialog(this);
            }
        }

        private void BtnStatistics_Click(object sender, EventArgs e)
        {
            using (var form = new MusicStatisticsForm())
            {
                form.ShowDialog(this);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                LanguageManager.LanguageChanged -= OnLanguageChanged;
            }
            base.Dispose(disposing);
        }
    }
}
