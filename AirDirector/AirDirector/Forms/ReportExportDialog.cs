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

namespace AirDirector.Forms
{
    public partial class ReportExportDialog : Form
    {
        private readonly List<ReportEntry> _data;

        // Header
        private Panel headerPanel;
        private Label lblTitle;
        private Label lblCount;

        // Columns group
        private GroupBox grpColumns;
        private CheckBox chkDate;
        private CheckBox chkStartTime;
        private CheckBox chkEndTime;
        private CheckBox chkArtist;
        private CheckBox chkTitle;
        private CheckBox chkType;
        private CheckBox chkPlayDuration;
        private CheckBox chkFileDuration;

        // Mode group
        private GroupBox grpMode;
        private RadioButton radStandard;
        private RadioButton radAdvSpots;
        private RadioButton radAdvHourly;

        // Options group
        private GroupBox grpOptions;
        private Label lblDelimiter;
        private ComboBox cmbDelimiter;
        private CheckBox chkIncludeHeader;

        // Buttons
        private Button btnExport;
        private Button btnCancel;

        public ReportExportDialog(List<ReportEntry> data)
        {
            _data = data ?? new List<ReportEntry>();
            InitializeComponent();
            InitializeUI();
            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            this.Text = LanguageManager.GetString("ReportExport.Title", "Esporta Report CSV");
            if (lblTitle != null) lblTitle.Text = "💾 " + LanguageManager.GetString("ReportExport.Title", "Esporta Report CSV");
            if (lblCount != null) lblCount.Text = string.Format(LanguageManager.GetString("ReportExport.Count", "{0} righe da esportare"), _data.Count);
            if (grpColumns != null) grpColumns.Text = LanguageManager.GetString("ReportExport.Columns", "Colonne da includere");
            if (grpMode != null) grpMode.Text = LanguageManager.GetString("ReportExport.Mode", "Modalità Export");
            if (grpOptions != null) grpOptions.Text = LanguageManager.GetString("ReportExport.Options", "Opzioni");
            if (chkDate != null) chkDate.Text = LanguageManager.GetString("Report.Column.Date", "Data");
            if (chkStartTime != null) chkStartTime.Text = LanguageManager.GetString("Report.Column.StartTime", "Ora Inizio");
            if (chkEndTime != null) chkEndTime.Text = LanguageManager.GetString("Report.Column.EndTime", "Ora Fine");
            if (chkArtist != null) chkArtist.Text = LanguageManager.GetString("Report.Column.Artist", "Artista");
            if (chkTitle != null) chkTitle.Text = LanguageManager.GetString("Report.Column.Title", "Titolo");
            if (chkType != null) chkType.Text = LanguageManager.GetString("Report.Column.Type", "Tipo");
            if (chkPlayDuration != null) chkPlayDuration.Text = LanguageManager.GetString("Report.Column.PlayDuration", "Durata Riprod.");
            if (chkFileDuration != null) chkFileDuration.Text = LanguageManager.GetString("Report.Column.FileDuration", "Durata File");
            if (radStandard != null) radStandard.Text = LanguageManager.GetString("ReportExport.Standard", "Standard");
            if (radAdvSpots != null) radAdvSpots.Text = LanguageManager.GetString("ReportExport.AdvSpots", "ADV Spots");
            if (radAdvHourly != null) radAdvHourly.Text = LanguageManager.GetString("ReportExport.AdvHourly", "ADV Orario");
            if (lblDelimiter != null) lblDelimiter.Text = LanguageManager.GetString("ReportExport.Delimiter", "Separatore:");
            if (chkIncludeHeader != null) chkIncludeHeader.Text = LanguageManager.GetString("ReportExport.IncludeHeader", "Includi intestazione");
            if (btnExport != null) btnExport.Text = "💾 " + LanguageManager.GetString("ReportExport.Export", "Esporta");
            if (btnCancel != null) btnCancel.Text = LanguageManager.GetString("Common.Cancel", "Annulla");
        }

        private void InitializeUI()
        {
            this.BackColor = Color.FromArgb(40, 40, 40);
            this.ForeColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            // Header
            headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = AppTheme.BgDark,
                Padding = new Padding(15, 10, 15, 10)
            };

            lblTitle = new Label
            {
                Text = "💾 Esporta Report CSV",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 10),
                AutoSize = true
            };
            headerPanel.Controls.Add(lblTitle);

            lblCount = new Label
            {
                Text = $"{_data.Count} righe da esportare",
                Font = new Font("Segoe UI", 9),
                ForeColor = AppTheme.LEDGreen,
                Location = new Point(15, 38),
                AutoSize = true
            };
            headerPanel.Controls.Add(lblCount);

            this.Controls.Add(headerPanel);

            int y = 75;
            int margin = 15;
            int groupW = this.ClientSize.Width - margin * 2;

            // Columns group
            grpColumns = new GroupBox
            {
                Text = "Colonne da includere",
                Location = new Point(margin, y),
                Size = new Size(groupW, 110),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            chkDate = CreateCheckBox("Data", 10, 25, true);
            chkStartTime = CreateCheckBox("Ora Inizio", 10, 50, true);
            chkEndTime = CreateCheckBox("Ora Fine", 10, 75, true);
            chkArtist = CreateCheckBox("Artista", 160, 25, true);
            chkTitle = CreateCheckBox("Titolo", 160, 50, true);
            chkType = CreateCheckBox("Tipo", 160, 75, true);
            chkPlayDuration = CreateCheckBox("Durata Riprod.", 310, 25, true);
            chkFileDuration = CreateCheckBox("Durata File", 310, 50, false);

            grpColumns.Controls.AddRange(new Control[] { chkDate, chkStartTime, chkEndTime, chkArtist, chkTitle, chkType, chkPlayDuration, chkFileDuration });
            this.Controls.Add(grpColumns);
            y += 120;

            // Mode group
            grpMode = new GroupBox
            {
                Text = "Modalità Export",
                Location = new Point(margin, y),
                Size = new Size(groupW, 90),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            radStandard = new RadioButton { Text = "Standard", Location = new Point(10, 25), AutoSize = true, Checked = true, ForeColor = Color.White, Font = new Font("Segoe UI", 9) };
            radAdvSpots = new RadioButton { Text = "ADV Spots", Location = new Point(130, 25), AutoSize = true, ForeColor = Color.White, Font = new Font("Segoe UI", 9) };
            radAdvHourly = new RadioButton { Text = "ADV Orario", Location = new Point(250, 25), AutoSize = true, ForeColor = Color.White, Font = new Font("Segoe UI", 9) };

            var lblModeDesc = new Label { Text = "Standard: esporta colonne selezionate. ADV Spots/Orario: formato specifico per spot pubblicitari.", Location = new Point(10, 55), Size = new Size(groupW - 30, 25), ForeColor = Color.Silver, Font = new Font("Segoe UI", 8) };

            grpMode.Controls.AddRange(new Control[] { radStandard, radAdvSpots, radAdvHourly, lblModeDesc });
            this.Controls.Add(grpMode);
            y += 100;

            // Options group
            grpOptions = new GroupBox
            {
                Text = "Opzioni",
                Location = new Point(margin, y),
                Size = new Size(groupW, 70),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            lblDelimiter = new Label { Text = "Separatore:", Location = new Point(10, 28), AutoSize = true, ForeColor = Color.White, Font = new Font("Segoe UI", 9) };
            cmbDelimiter = new ComboBox
            {
                Location = new Point(100, 25),
                Size = new Size(80, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            cmbDelimiter.Items.AddRange(new object[] { "Punto e virgola (;)", "Virgola (,)", "Tab" });
            cmbDelimiter.SelectedIndex = 0;

            chkIncludeHeader = new CheckBox { Text = "Includi intestazione", Location = new Point(210, 28), AutoSize = true, Checked = true, ForeColor = Color.White, Font = new Font("Segoe UI", 9) };

            grpOptions.Controls.AddRange(new Control[] { lblDelimiter, cmbDelimiter, chkIncludeHeader });
            this.Controls.Add(grpOptions);
            y += 80;

            // Buttons
            btnExport = new Button
            {
                Text = "💾 Esporta",
                Location = new Point(this.ClientSize.Width - 240, y + 5),
                Size = new Size(110, 35),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnExport.FlatAppearance.BorderSize = 0;
            btnExport.Click += BtnExport_Click;
            this.Controls.Add(btnExport);

            btnCancel = new Button
            {
                Text = "Annulla",
                Location = new Point(this.ClientSize.Width - 120, y + 5),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10),
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            this.Controls.Add(btnCancel);

            this.ClientSize = new Size(600, y + 50);
            this.CancelButton = btnCancel;
        }

        private CheckBox CreateCheckBox(string text, int x, int y, bool isChecked)
        {
            return new CheckBox
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                Checked = isChecked,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9)
            };
        }

        private char GetDelimiterChar()
        {
            switch (cmbDelimiter.SelectedIndex)
            {
                case 1: return ',';
                case 2: return '\t';
                default: return ';';
            }
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog
            {
                Filter = "File CSV (*.csv)|*.csv|Tutti i file (*.*)|*.*",
                FileName = $"Report_{DateTime.Now:yyyy-MM-dd_HH-mm}.csv",
                Title = "Esporta Report CSV"
            };

            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                if (radStandard.Checked)
                    ExportToCSV(dlg.FileName);
                else if (radAdvSpots.Checked)
                    ExportAdvSpotsToCSV(dlg.FileName);
                else
                    ExportAdvHourlyToCSV(dlg.FileName);

                MessageBox.Show(
                    string.Format(LanguageManager.GetString("ReportExport.Success", "✅ Esportazione completata!\n{0} righe salvate in:\n{1}"), _data.Count, dlg.FileName),
                    LanguageManager.GetString("ReportExport.SuccessTitle", "Esportazione completata"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(LanguageManager.GetString("ReportExport.Error", "Errore durante l'esportazione:\n{0}"), ex.Message),
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportToCSV(string filePath)
        {
            char sep = GetDelimiterChar();
            var columns = new List<(string Header, Func<ReportEntry, string> Getter)>();

            if (chkDate.Checked) columns.Add(("Data", e => e.Date.ToString("yyyy-MM-dd")));
            if (chkStartTime.Checked) columns.Add(("Ora Inizio", e => e.StartTime));
            if (chkEndTime.Checked) columns.Add(("Ora Fine", e => e.EndTime));
            if (chkArtist.Checked) columns.Add(("Artista", e => e.Artist));
            if (chkTitle.Checked) columns.Add(("Titolo", e => e.Title));
            if (chkType.Checked) columns.Add(("Tipo", e => e.Type));
            if (chkPlayDuration.Checked) columns.Add(("Durata Riprod.", e => e.PlayDuration));
            if (chkFileDuration.Checked) columns.Add(("Durata File", e => e.FileDuration));

            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                if (chkIncludeHeader.Checked)
                    writer.WriteLine(string.Join(sep.ToString(), columns.Select(c => EscapeField(c.Header, sep))));

                foreach (var entry in _data)
                    writer.WriteLine(string.Join(sep.ToString(), columns.Select(c => EscapeField(c.Getter(entry), sep))));
            }
        }

        private void ExportAdvSpotsToCSV(string filePath)
        {
            char sep = GetDelimiterChar();
            var spots = _data.Where(e => string.Equals(e.Type, "clip", StringComparison.OrdinalIgnoreCase) ||
                                          string.Equals(e.Type, "clips", StringComparison.OrdinalIgnoreCase)).ToList();

            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                if (chkIncludeHeader.Checked)
                    writer.WriteLine(string.Join(sep.ToString(), "Data", "Ora", "Spot", "Durata"));

                foreach (var entry in spots)
                {
                    writer.WriteLine(string.Join(sep.ToString(),
                        EscapeField(entry.Date.ToString("yyyy-MM-dd"), sep),
                        EscapeField(entry.StartTime, sep),
                        EscapeField($"{entry.Artist} - {entry.Title}", sep),
                        EscapeField(entry.PlayDuration, sep)));
                }
            }
        }

        private void ExportAdvHourlyToCSV(string filePath)
        {
            char sep = GetDelimiterChar();
            var spots = _data.Where(e => string.Equals(e.Type, "clip", StringComparison.OrdinalIgnoreCase) ||
                                          string.Equals(e.Type, "clips", StringComparison.OrdinalIgnoreCase)).ToList();

            var hourlyGroups = spots.GroupBy(e =>
            {
                if (TimeSpan.TryParse(e.StartTime, out TimeSpan t))
                    return t.Hours;
                return 0;
            }).OrderBy(g => g.Key);

            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                if (chkIncludeHeader.Checked)
                    writer.WriteLine(string.Join(sep.ToString(), "Ora", "N. Spot", "Durata Totale"));

                foreach (var group in hourlyGroups)
                {
                    TimeSpan totalDur = TimeSpan.Zero;
                    foreach (var e in group)
                    {
                        if (TimeSpan.TryParse(e.PlayDuration, out TimeSpan d))
                            totalDur += d;
                    }
                    writer.WriteLine(string.Join(sep.ToString(),
                        EscapeField($"{group.Key:00}:00", sep),
                        group.Count().ToString(),
                        EscapeField(totalDur.ToString(@"hh\:mm\:ss"), sep)));
                }
            }
        }

        private static string EscapeField(string value, char separator)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(separator) || value.Contains('"') || value.Contains('\n'))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }
    }
}
