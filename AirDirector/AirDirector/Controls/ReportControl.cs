using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AirDirector.Services.Database;
using AirDirector.Services.Localization;
using AirDirector.Themes;

namespace AirDirector.Controls
{
    public partial class ReportControl : UserControl
    {
        private DataGridView dgvReport;
        private Panel headerPanel;
        private Label lblHeader;
        private Label lblCount;
        private Button btnExport;

        public ReportControl()
        {
            InitializeComponent();
            InitializeUI();
            ApplyLanguage();
            LoadLast24Hours();

            LanguageManager.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            ApplyLanguage();
            LoadLast24Hours();
        }

        private void ApplyLanguage()
        {
            if (lblHeader != null)
                lblHeader.Text = "📊 " + LanguageManager.GetString("Report.Title", "REPORT ULTIME 24 ORE");

            if (btnExport != null)
                btnExport.Text = "💾 " + LanguageManager.GetString("Report.ExportCsv", "Esporta CSV");

            if (dgvReport != null && dgvReport.Columns.Count >= 6)
            {
                dgvReport.Columns["Date"].HeaderText = "📅 " + LanguageManager.GetString("Report.Date", "Data");
                dgvReport.Columns["StartTime"].HeaderText = "🕐 " + LanguageManager.GetString("Report.Start", "Inizio");
                dgvReport.Columns["EndTime"].HeaderText = "🕐 " + LanguageManager.GetString("Report.End", "Fine");
                dgvReport.Columns["Artist"].HeaderText = "🎤 " + LanguageManager.GetString("Report.Artist", "Artista");
                dgvReport.Columns["Title"].HeaderText = "🎶 " + LanguageManager.GetString("Report.Title", "Titolo");
                dgvReport.Columns["Type"].HeaderText = "🎵 " + LanguageManager.GetString("Report.Type", "Tipo");
            }
        }

        private void InitializeUI()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = AppTheme.BgLight;
            this.Padding = new Padding(0);

            headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = AppTheme.BgDark,
                Padding = new Padding(15, 10, 15, 10)
            };

            lblHeader = new Label
            {
                Text = "📊 REPORT ULTIME 24 ORE",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 12),
                AutoSize = true
            };
            headerPanel.Controls.Add(lblHeader);

            lblCount = new Label
            {
                Text = "0 brani",
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = AppTheme.LEDGreen,
                Location = new Point(15, 40),
                AutoSize = true
            };
            headerPanel.Controls.Add(lblCount);

            btnExport = new Button
            {
                Name = "btnExport",
                Text = "💾 Esporta CSV",
                Size = new Size(150, 45),
                BackColor = Color.FromArgb(0, 150, 136),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnExport.FlatAppearance.BorderSize = 0;
            btnExport.Click += BtnExport_Click;
            headerPanel.Controls.Add(btnExport);

            RepositionExportButton();
            headerPanel.Resize += (s, e) => RepositionExportButton();

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
                RowTemplate = { Height = 35 },
                Font = new Font("Segoe UI", 10),
                AllowUserToResizeRows = false,
                ScrollBars = ScrollBars.Both
            };

            dgvReport.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(50, 50, 50);
            dgvReport.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvReport.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            dgvReport.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvReport.ColumnHeadersDefaultCellStyle.Padding = new Padding(5);

            dgvReport.DefaultCellStyle.BackColor = Color.FromArgb(40, 40, 40);
            dgvReport.DefaultCellStyle.ForeColor = Color.White;
            dgvReport.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 215);
            dgvReport.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvReport.DefaultCellStyle.Padding = new Padding(5);

            dgvReport.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(35, 35, 35);
            dgvReport.EnableHeadersVisualStyles = false;

            dgvReport.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Date",
                HeaderText = "📅 Data",
                FillWeight = 12,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvReport.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "StartTime",
                HeaderText = "🕐 Inizio",
                FillWeight = 10,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvReport.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "EndTime",
                HeaderText = "🕐 Fine",
                FillWeight = 10,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvReport.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Artist",
                HeaderText = "🎤 Artista",
                FillWeight = 25,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft }
            });

            dgvReport.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Title",
                HeaderText = "🎶 Titolo",
                FillWeight = 26,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft }
            });

            dgvReport.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Type",
                HeaderText = "🎵 Tipo",
                FillWeight = 10,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            // Add Fill control first, then Top-docked panel last so WinForms docking
            // positions the header above the grid correctly.
            this.Controls.Add(dgvReport);
            this.Controls.Add(headerPanel);
        }

        private void RepositionExportButton()
        {
            const int MARGIN = 15;
            int panelWidth = headerPanel.Width;
            int exportX = panelWidth - btnExport.Width - MARGIN;
            btnExport.Location = new Point(exportX, 12);
        }

        public void LoadLast24Hours()
        {
            try
            {
                dgvReport.Rows.Clear();

                DateTime now = DateTime.Now;
                DateTime from = now.AddHours(-24);

                var reports = ReportManager.LoadReport(from, now);

                var sortedReports = reports
                    .OrderByDescending(r => r.Date)
                    .ThenByDescending(r => r.StartTime)
                    .ToList();

                string musicLabel = "🎵 " + LanguageManager.GetString("Report.Music", "Musica");
                string clipLabel = "⚡ " + LanguageManager.GetString("Report.Clip", "Clip");

                foreach (var report in sortedReports)
                {
                    dgvReport.Rows.Add(
                        report.Date.ToString("dd/MM/yyyy"),
                        report.StartTime,
                        report.EndTime,
                        report.Artist,
                        report.Title,
                        report.Type == "Music" ? musicLabel : clipLabel
                    );
                }

                lblCount.Text = string.Format(LanguageManager.GetString("Report.TracksCount", "{0} brani nelle ultime 24 ore"), sortedReports.Count);
                lblCount.ForeColor = sortedReports.Count > 0 ? AppTheme.LEDGreen : Color.Orange;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(LanguageManager.GetString("Report.LoadError", "Errore caricamento report:\\n{0}"), ex.Message),
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvReport.Rows.Count == 0)
                {
                    MessageBox.Show(
                        LanguageManager.GetString("Report.NoData", "Nessun dato da esportare!"),
                        LanguageManager.GetString("Common.Warning", "Attenzione"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = LanguageManager.GetString("Report.CsvFilter", "File CSV (*.csv)|*.csv"),
                    FileName = $"Report_{DateTime.Now:yyyy-MM-dd_HH-mm}.csv",
                    Title = LanguageManager.GetString("Report.ExportTitle", "Esporta Report CSV")
                };

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    using (var writer = new System.IO.StreamWriter(saveDialog.FileName, false, System.Text.Encoding.UTF8))
                    {
                        string headerDate = LanguageManager.GetString("Report.Date", "Data");
                        string headerStart = LanguageManager.GetString("Report.Start", "Inizio");
                        string headerEnd = LanguageManager.GetString("Report.End", "Fine");
                        string headerArtist = LanguageManager.GetString("Report.Artist", "Artista");
                        string headerTitle = LanguageManager.GetString("Report.TitleColumn", "Titolo");
                        string headerType = LanguageManager.GetString("Report.Type", "Tipo");

                        writer.WriteLine($"{headerDate},{headerStart},{headerEnd},{headerArtist},{headerTitle},{headerType}");

                        foreach (DataGridViewRow row in dgvReport.Rows)
                        {
                            if (row.IsNewRow) continue;

                            string line = string.Join(",",
                                EscapeCsv(row.Cells["Date"].Value?.ToString() ?? ""),
                                EscapeCsv(row.Cells["StartTime"].Value?.ToString() ?? ""),
                                EscapeCsv(row.Cells["EndTime"].Value?.ToString() ?? ""),
                                EscapeCsv(row.Cells["Artist"].Value?.ToString() ?? ""),
                                EscapeCsv(row.Cells["Title"].Value?.ToString() ?? ""),
                                EscapeCsv(row.Cells["Type"].Value?.ToString().Replace("🎵 ", "").Replace("⚡ ", "") ?? "")
                            );
                            writer.WriteLine(line);
                        }
                    }

                    MessageBox.Show(
                        string.Format(LanguageManager.GetString("Report.ExportSuccess", "✅ Report esportato con successo! \\n\\n{0} righe salvate in:\\n{1}"),
                            dgvReport.Rows.Count,
                            saveDialog.FileName),
                        LanguageManager.GetString("Report.ExportCompleted", "Esportazione Completata"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(LanguageManager.GetString("Report.ExportError", "Errore esportazione:\\n{0}"), ex.Message),
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }

            return value;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                LanguageManager.SaveMissingKeysToFile();
                LanguageManager.LanguageChanged -= OnLanguageChanged;
            }
            base.Dispose(disposing);
        }
    }
}