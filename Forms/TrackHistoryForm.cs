using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using AirDirector.Services.Database;

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

            this.Text = $"📋 Storico Passaggi - {_artist} - {_title}";
            this.Size = new Size(750, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(500, 300);
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.White;

            // Header label
            Label lblHeader = new Label
            {
                Text = $"🎵 {_artist} - {_title}",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 35,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            this.Controls.Add(lblHeader);

            // Button panel
            Panel pnlButtons = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 45,
                BackColor = Color.FromArgb(40, 40, 40),
                Padding = new Padding(10, 5, 10, 5)
            };
            this.Controls.Add(pnlButtons);

            // Spacer to push the table below the header (header height + 5px)
            Panel pnlSpacer = new Panel
            {
                Dock = DockStyle.Top,
                Height = 5,
                BackColor = Color.FromArgb(30, 30, 30)
            };
            this.Controls.Add(pnlSpacer);

            Button btnExport = new Button
            {
                Text = "📥 Esporta CSV",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(130, 32),
                Location = new Point(10, 6),
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
                Location = new Point(160, 12),
                AutoSize = true
            };
            pnlButtons.Controls.Add(lblCount);

            Button btnClose = new Button
            {
                Text = "Chiudi",
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(80, 32),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();
            pnlButtons.Controls.Add(btnClose);
            this.Load += (s, e) =>
            {
                btnClose.Location = new Point(pnlButtons.Width - btnClose.Width - 10, 6);
            };
            pnlButtons.Resize += (s, e) =>
            {
                btnClose.Location = new Point(pnlButtons.Width - btnClose.Width - 10, 6);
            };

            // DataGridView
            _dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
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

            _dgv.Columns.Add("Date", "Data");
            _dgv.Columns.Add("StartTime", "Ora Inizio");
            _dgv.Columns.Add("EndTime", "Ora Fine");
            _dgv.Columns.Add("PlayDuration", "Durata Play");
            _dgv.Columns.Add("Type", "Tipo");

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

                lblCount.Text = $"Totale passaggi: {entries.Count}";
            }
            catch (Exception ex)
            {
                lblCount.Text = $"Errore: {ex.Message}";
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
                    sfd.Title = "Esporta Storico Passaggi";

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
                        MessageBox.Show($"✅ Esportati {_dgv.Rows.Count} passaggi in:\n{sfd.FileName}",
                            "Export Completato", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Errore esportazione:\n{ex.Message}", "Errore",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
