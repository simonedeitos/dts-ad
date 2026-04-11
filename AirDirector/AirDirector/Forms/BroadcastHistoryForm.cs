using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AirDirector.Services.Database;
using AirDirector.Services.Localization;

namespace AirDirector.Forms
{
    public partial class BroadcastHistoryForm : Form
    {
        private List<ReportEntry> _currentData;
        private List<ReportEntry> _filteredData;

        public BroadcastHistoryForm()
        {
            InitializeComponent();

            // Set date values that depend on DateTime.Today
            dtpFrom.Value = DateTime.Today.AddDays(-7);
            dtpTo.Value = DateTime.Today;

            // Wire up event handlers
            cmbType.SelectedIndexChanged += (s, e) => ApplyFilter();
            txtSearch.TextChanged += (s, e) => ApplyFilter();
            btnRefresh.Click += (s, e) => LoadHistory();
            btnExport.Click += BtnExport_Click;
            btnStatistics.Click += BtnStatistics_Click;
            statsPanel.Resize += (s, e) => btnStatistics.Location = new Point(statsPanel.Width - btnStatistics.Width - 10, 9);
            dgvHistory.CellFormatting += DgvHistory_CellFormatting;

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
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void lblSearch_Click(object sender, EventArgs e)
        {

        }
    }
}
