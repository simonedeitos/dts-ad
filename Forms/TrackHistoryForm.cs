using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using AirDirector.Services.Database;
using AirDirector.Services.Localization;

namespace AirDirector.Forms
{
    /// <summary>
    /// Mostra lo storico dei passaggi di un brano, letti dal file Report.dbc.
    /// Prevede esportazione in CSV.
    /// </summary>
    public class TrackHistoryForm : Form
    {
        private DataGridView _dgv;
        private string _artist;
        private string _title;

        public TrackHistoryForm(string artist, string title)
        {
            _artist = artist ?? "";
            _title = title ?? "";

            this.Text = $"📋 {LanguageManager.GetString("TrackHistory.Title", "Storico Passaggi")} - {_artist} - {_title}";
            this.Size = new Size(750, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(500, 300);
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.White;

            const int HEADER_HEIGHT = 45;
            const int BUTTON_PANEL_HEIGHT = 50;

            // ✅ Header label - posizionamento manuale
            Label lblHeader = new Label
            {
                Text = $"🎵 {_artist} - {_title}",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(30, 30, 30),
                Location = new Point(0, 0),
                Size = new Size(this.ClientSize.Width, HEADER_HEIGHT),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            this.Controls.Add(lblHeader);

            // ✅ Button panel - posizionamento manuale dal basso
            Panel pnlButtons = new Panel
            {
                Location = new Point(0, this.ClientSize.Height - BUTTON_PANEL_HEIGHT),
                Size = new Size(this.ClientSize.Width, BUTTON_PANEL_HEIGHT),
                BackColor = Color.FromArgb(40, 40, 40),
                Padding = new Padding(10, 5, 10, 5),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            this.Controls.Add(pnlButtons);

            Button btnExport = new Button
            {
                Text = "📥 " + LanguageManager.GetString("TrackHistory.ExportCsv", "Esporta CSV"),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(130, 32),
                Location = new Point(10, 9),
                Cursor = Cursors.Hand
            };
            btnExport.FlatAppearance.BorderSize = 0;
            btnExport.Click += BtnExport_Click;
            pnlButtons.Controls.Add(btnExport);

            Label lblCount = new Label
            {
                Name = "lblCount",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.LightGray,
                Location = new Point(160, 15),
                AutoSize = true
            };
            pnlButtons.Controls.Add(lblCount);

            Button btnClose = new Button
            {
                Text = LanguageManager.GetString("Common.Close", "Chiudi"),
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(80, 32),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();
            pnlButtons.Controls.Add(btnClose);

            // Posiziona btnClose a destra
            Action positionCloseButton = () =>
            {
                btnClose.Location = new Point(pnlButtons.Width - btnClose.Width - 10, 9);
            };
            positionCloseButton();
            pnlButtons.Resize += (s, e) => positionCloseButton();

            // ✅ DataGridView - posizionamento manuale tra header e buttons
            _dgv = new DataGridView
            {
                Location = new Point(0, HEADER_HEIGHT),
                Size = new Size(this.ClientSize.Width, this.ClientSize.Height - HEADER_HEIGHT - BUTTON_PANEL_HEIGHT),
                BackgroundColor = Color.FromArgb(25, 25, 25),
                ForeColor = Color.White,
                GridColor = Color.FromArgb(60, 60, 60),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(50, 50, 50),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    SelectionBackColor = Color.FromArgb(50, 50, 50)
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(30, 30, 30),
                    ForeColor = Color.White,
                    SelectionBackColor = Color.FromArgb(0, 100, 180),
                    SelectionForeColor = Color.White
                },
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(38, 38, 38),
                    ForeColor = Color.White,
                    SelectionBackColor = Color.FromArgb(0, 100, 180),
                    SelectionForeColor = Color.White
                },
                EnableHeadersVisualStyles = false
            };

            _dgv.Columns.Add("Date", LanguageManager.GetString("TrackHistory.ColumnDate", "Data"));
            _dgv.Columns.Add("StartTime", LanguageManager.GetString("TrackHistory.ColumnStartTime", "Ora Inizio"));
            _dgv.Columns.Add("EndTime", LanguageManager.GetString("TrackHistory.ColumnEndTime", "Ora Fine"));
            _dgv.Columns.Add("PlayDuration", LanguageManager.GetString("TrackHistory.ColumnPlayDuration", "Durata Play"));
            _dgv.Columns.Add("Type", LanguageManager.GetString("TrackHistory.ColumnType", "Tipo"));

            this.Controls.Add(_dgv);

            // Load data
            LoadHistory(lblCount);
        }

        private void LoadHistory(Label lblCount)
        {
            try
            {
                var entries = ReportManager.LoadTrackHistory(_artist, _title);

                _dgv.Rows.Clear();
                foreach (var entry in entries.OrderByDescending(e => e.Date).ThenByDescending(e => e.StartTime))
                {
                    _dgv.Rows.Add(
                        entry.Date.ToString("yyyy-MM-dd"),
                        entry.StartTime,
                        entry.EndTime,
                        entry.PlayDuration,
                        entry.Type
                    );
                }

                lblCount.Text = string.Format(LanguageManager.GetString("TrackHistory.TotalEntries", "Totale passaggi: {0}"), entries.Count);
            }
            catch (Exception ex)
            {
                lblCount.Text = string.Format(LanguageManager.GetString("Common.Error", "Errore") + ": {0}", ex.Message);
            }
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            try
            {
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Filter = "CSV files (*.csv)|*.csv";
                    sfd.FileName = $"Storico_{_artist}_{_title}.csv".Replace(" ", "_");
                    sfd.Title = LanguageManager.GetString("TrackHistory.ExportTitle", "Esporta Storico Passaggi");

                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine("Data;Ora Inizio;Ora Fine;Durata Play;Tipo;Artista;Titolo");

                        foreach (DataGridViewRow row in _dgv.Rows)
                        {
                            if (row.IsNewRow) continue;
                            sb.AppendLine($"{row.Cells[0].Value};{row.Cells[1].Value};{row.Cells[2].Value};{row.Cells[3].Value};{row.Cells[4].Value};{_artist};{_title}");
                        }

                        File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                        MessageBox.Show(string.Format(LanguageManager.GetString("TrackHistory.ExportSuccess", "✅ Esportati {0} passaggi in:\n{1}"), _dgv.Rows.Count, sfd.FileName),
                            LanguageManager.GetString("TrackHistory.ExportCompleted", "Export Completato"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(LanguageManager.GetString("TrackHistory.ExportError", "❌ Errore esportazione:\n{0}"), ex.Message), LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
