using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using AirDirector.Services.Database;
using AirDirector.Services.Localization;
using AirDirector.Themes;

namespace AirDirector.Controls
{
    public partial class ReportAdvancedControl : UserControl
    {
        private DataGridView dgvReport;
        private Panel headerPanel;
        private Panel filterPanel;
        private Label lblHeader;
        private Label lblCount;

        private Label lblFrom;
        private Label lblTo;
        private DateTimePicker dtpFromDate;
        private DateTimePicker dtpToDate;
        private Button btnLoad;
        private Button btnToday;
        private Button btnYesterday;
        private Button btnLast7Days;
        private Button btnLast30Days;
        private Button btnExport;

        private List<ReportEntry> _currentData;

        public ReportAdvancedControl()
        {
            InitializeComponent();
            InitializeCustomUI();
            ApplyLanguage();
            _currentData = new List<ReportEntry>();
            LanguageManager.LanguageChanged += (s, e) => ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            if (lblHeader != null) lblHeader.Text = "📊 " + LanguageManager.GetString("Report.HeaderTitle", "REPORT AVANZATO");
            if (lblCount != null && (_currentData == null || _currentData.Count == 0))
                lblCount.Text = LanguageManager.GetString("Report.NoElements", "Nessun elemento");
            if (lblFrom != null) lblFrom.Text = LanguageManager.GetString("Report.From", "Da:");
            if (lblTo != null) lblTo.Text = LanguageManager.GetString("Report.To", "A:");
            if (btnLoad != null) btnLoad.Text = "📊 " + LanguageManager.GetString("Report.Load", "Carica");
            if (btnToday != null) btnToday.Text = LanguageManager.GetString("Report.Today", "Oggi");
            if (btnYesterday != null) btnYesterday.Text = LanguageManager.GetString("Report.Yesterday", "Ieri");
            if (btnLast7Days != null) btnLast7Days.Text = LanguageManager.GetString("Report.Last7Days", "7 Giorni");
            if (btnLast30Days != null) btnLast30Days.Text = LanguageManager.GetString("Report.Last30Days", "30 Giorni");
            if (btnExport != null) btnExport.Text = "💾 " + LanguageManager.GetString("Report.ExportCSV", "Esporta CSV");
            if (dgvReport != null && dgvReport.Columns.Count >= 7)
            {
                dgvReport.Columns["Date"].HeaderText = "📅 " + LanguageManager.GetString("Report.Column.Date", "Data");
                dgvReport.Columns["StartTime"].HeaderText = "🕐 " + LanguageManager.GetString("Report.Column.StartTime", "Inizio");
                dgvReport.Columns["EndTime"].HeaderText = "🕐 " + LanguageManager.GetString("Report.Column.EndTime", "Fine");
                dgvReport.Columns["Artist"].HeaderText = "🎤 " + LanguageManager.GetString("Report.Column.Artist", "Artista");
                dgvReport.Columns["Title"].HeaderText = "🎶 " + LanguageManager.GetString("Report.Column.Title", "Titolo");
                dgvReport.Columns["Type"].HeaderText = "🎵 " + LanguageManager.GetString("Report.Column.Type", "Tipo");
                dgvReport.Columns["PlayDuration"].HeaderText = "⏱️ " + LanguageManager.GetString("Report.Column.PlayDuration", "Durata");
            }
        }

        private void InitializeCustomUI()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(28, 28, 28);

            // Header panel
            headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = AppTheme.BgDark,
                Padding = new Padding(15, 10, 15, 10)
            };

            lblHeader = new Label
            {
                Text = "📊 REPORT AVANZATO",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 12),
                AutoSize = true
            };
            headerPanel.Controls.Add(lblHeader);

            lblCount = new Label
            {
                Text = "Nessun elemento",
                Font = new Font("Segoe UI", 10),
                ForeColor = AppTheme.LEDGreen,
                Location = new Point(15, 38),
                AutoSize = true
            };
            headerPanel.Controls.Add(lblCount);

            btnExport = new Button
            {
                Text = "💾 Esporta CSV",
                Size = new Size(160, 40),
                BackColor = Color.FromArgb(0, 150, 136),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnExport.FlatAppearance.BorderSize = 0;
            btnExport.Click += (s, e) => ShowExportDialog();
            headerPanel.Controls.Add(btnExport);
            headerPanel.Resize += (s, e) => btnExport.Location = new Point(headerPanel.Width - btnExport.Width - 15, 10);

            this.Controls.Add(headerPanel);

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
            x += 35;

            dtpFromDate = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Now.AddDays(-1),
                Location = new Point(x, 13),
                Size = new Size(110, 25),
                CalendarForeColor = Color.White,
                CalendarMonthBackground = Color.FromArgb(50, 50, 50)
            };
            filterPanel.Controls.Add(dtpFromDate);
            x += 120;

            lblTo = new Label { Text = "A:", ForeColor = Color.White, Font = new Font("Segoe UI", 9), Location = new Point(x, 17), AutoSize = true };
            filterPanel.Controls.Add(lblTo);
            x += 25;

            dtpToDate = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Now,
                Location = new Point(x, 13),
                Size = new Size(110, 25),
                CalendarForeColor = Color.White,
                CalendarMonthBackground = Color.FromArgb(50, 50, 50)
            };
            filterPanel.Controls.Add(dtpToDate);
            x += 120;

            btnLoad = new Button
            {
                Text = "📊 Carica",
                Location = new Point(x, 11),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLoad.FlatAppearance.BorderSize = 0;
            btnLoad.Click += (s, e) => LoadReport();
            filterPanel.Controls.Add(btnLoad);
            x += 110;

            x += 10;

            btnToday = new Button
            {
                Text = "Oggi",
                Location = new Point(x, 11),
                Size = new Size(70, 30),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9),
                Cursor = Cursors.Hand
            };
            btnToday.FlatAppearance.BorderSize = 0;
            btnToday.Click += (s, e) => { dtpFromDate.Value = DateTime.Today; dtpToDate.Value = DateTime.Today; LoadReport(); };
            filterPanel.Controls.Add(btnToday);
            x += 80;

            btnYesterday = new Button
            {
                Text = "Ieri",
                Location = new Point(x, 11),
                Size = new Size(60, 30),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9),
                Cursor = Cursors.Hand
            };
            btnYesterday.FlatAppearance.BorderSize = 0;
            btnYesterday.Click += (s, e) => { dtpFromDate.Value = DateTime.Today.AddDays(-1); dtpToDate.Value = DateTime.Today.AddDays(-1); LoadReport(); };
            filterPanel.Controls.Add(btnYesterday);
            x += 70;

            btnLast7Days = new Button
            {
                Text = "7 Giorni",
                Location = new Point(x, 11),
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9),
                Cursor = Cursors.Hand
            };
            btnLast7Days.FlatAppearance.BorderSize = 0;
            btnLast7Days.Click += (s, e) => { dtpFromDate.Value = DateTime.Today.AddDays(-7); dtpToDate.Value = DateTime.Today; LoadReport(); };
            filterPanel.Controls.Add(btnLast7Days);
            x += 90;

            btnLast30Days = new Button
            {
                Text = "30 Giorni",
                Location = new Point(x, 11),
                Size = new Size(85, 30),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9),
                Cursor = Cursors.Hand
            };
            btnLast30Days.FlatAppearance.BorderSize = 0;
            btnLast30Days.Click += (s, e) => { dtpFromDate.Value = DateTime.Today.AddDays(-30); dtpToDate.Value = DateTime.Today; LoadReport(); };
            filterPanel.Controls.Add(btnLast30Days);

            this.Controls.Add(filterPanel);

            // DataGridView
            dgvReport = new DataGridView
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

            dgvReport.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(50, 50, 50);
            dgvReport.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvReport.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            dgvReport.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvReport.DefaultCellStyle.BackColor = Color.FromArgb(40, 40, 40);
            dgvReport.DefaultCellStyle.ForeColor = Color.White;
            dgvReport.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 215);
            dgvReport.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvReport.DefaultCellStyle.Padding = new Padding(5);
            dgvReport.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(35, 35, 35);
            dgvReport.EnableHeadersVisualStyles = false;

            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "📅 Data", FillWeight = 12, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "StartTime", HeaderText = "🕐 Inizio", FillWeight = 10, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "EndTime", HeaderText = "🕐 Fine", FillWeight = 10, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "Artist", HeaderText = "🎤 Artista", FillWeight = 22, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft } });
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "Title", HeaderText = "🎶 Titolo", FillWeight = 22, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft } });
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "🎵 Tipo", FillWeight = 10, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "PlayDuration", HeaderText = "⏱️ Durata", FillWeight = 10, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });

            this.Controls.Add(dgvReport);
        }

        private void LoadReport()
        {
            try
            {
                dgvReport.Rows.Clear();
                var data = ReportManager.LoadReport(dtpFromDate.Value, dtpToDate.Value);
                _currentData = data.OrderByDescending(r => r.Date).ThenByDescending(r => r.StartTime).ToList();

                foreach (var entry in _currentData)
                {
                    dgvReport.Rows.Add(
                        entry.Date.ToString("dd/MM/yyyy"),
                        entry.StartTime,
                        entry.EndTime,
                        entry.Artist,
                        entry.Title,
                        entry.Type,
                        entry.PlayDuration
                    );
                }

                lblCount.Text = string.Format(LanguageManager.GetString("Report.Count", "{0} elementi"), _currentData.Count);
                lblCount.ForeColor = _currentData.Count > 0 ? AppTheme.LEDGreen : Color.Orange;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, LanguageManager.GetString("Common.Error", "Errore"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void LoadDefaultReport()
        {
            dtpFromDate.Value = DateTime.Now.AddDays(-1);
            dtpToDate.Value = DateTime.Now;
            LoadReport();
        }

        public void ShowExportDialog()
        {
            if (_currentData == null || _currentData.Count == 0)
            {
                MessageBox.Show(
                    LanguageManager.GetString("Report.Error.NoDataToExport", "Nessun dato da esportare."),
                    LanguageManager.GetString("Report.Title.NoData", "Nessun dato"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            using (var exportForm = new Forms.ReportExportDialog(_currentData))
            {
                exportForm.ShowDialog();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                LanguageManager.LanguageChanged -= (s, e) => ApplyLanguage();
                components?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
