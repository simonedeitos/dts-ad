using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AirDirector.Services.Database;
using AirDirector.Services.Localization;
using AirDirector.Services;
using AirDirector.Themes;

namespace AirDirector.Forms
{
    public class ArtistAliasManagerForm : Form
    {
        private DataGridView dgvArtists;
        private Button btnAdd;
        private Button btnEdit;
        private Button btnDelete;
        private Button btnAutoScan;
        private Button btnClose;

        public ArtistAliasManagerForm()
        {
            InitializeComponents();
            ApplyTheme();
            LoadData();
        }

        private void InitializeComponents()
        {
            this.Text = "🎤 " + LanguageManager.GetString("ArtistAliasManager.Title", "Gestione Alias Artisti");
            this.Size = new Size(700, 500);
            this.MinimumSize = new Size(600, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;

            // DataGridView
            dgvArtists = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 9F)
            };
            dgvArtists.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colArtist",
                HeaderText = LanguageManager.GetString("ArtistAliasManager.ArtistName", "Nome Artista Canonico"),
                FillWeight = 40
            });
            dgvArtists.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colAliases",
                HeaderText = LanguageManager.GetString("ArtistAliasManager.Aliases", "Alias (separati da ;)"),
                FillWeight = 60
            });

            // Buttons panel
            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                Padding = new Padding(4, 6, 4, 6),
                FlowDirection = FlowDirection.LeftToRight
            };

            btnAdd = CreateButton("➕ " + LanguageManager.GetString("Common.Add", "Aggiungi"), AppTheme.Primary);
            btnEdit = CreateButton("✏️ " + LanguageManager.GetString("Common.Edit", "Modifica"), AppTheme.BgLight);
            btnDelete = CreateButton("🗑️ " + LanguageManager.GetString("Common.Delete", "Elimina"), Color.IndianRed);
            btnAutoScan = CreateButton("🔍 " + LanguageManager.GetString("ArtistAliasManager.AutoScan", "Auto-Scan"), AppTheme.BgLight);
            btnClose = CreateButton("✖ " + LanguageManager.GetString("Common.Close", "Chiudi"), AppTheme.BgLight);

            btnAdd.Click += BtnAdd_Click;
            btnEdit.Click += BtnEdit_Click;
            btnDelete.Click += BtnDelete_Click;
            btnAutoScan.Click += BtnAutoScan_Click;
            btnClose.Click += (s, e) => this.Close();

            btnPanel.Controls.Add(btnAdd);
            btnPanel.Controls.Add(btnEdit);
            btnPanel.Controls.Add(btnDelete);
            btnPanel.Controls.Add(btnAutoScan);
            btnPanel.Controls.Add(btnClose);

            this.Controls.Add(dgvArtists);
            this.Controls.Add(btnPanel);
        }

        private Button CreateButton(string text, Color backColor)
        {
            return new Button
            {
                Text = text,
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F),
                BackColor = backColor,
                ForeColor = AppTheme.TextPrimary,
                Margin = new Padding(3, 0, 3, 0),
                Cursor = Cursors.Hand,
                Height = 30
            };
        }

        private void ApplyTheme()
        {
            this.BackColor = AppTheme.BgDark;
            dgvArtists.BackgroundColor = AppTheme.BgDark;
            dgvArtists.GridColor = AppTheme.BorderLight;
            dgvArtists.DefaultCellStyle.BackColor = AppTheme.Surface;
            dgvArtists.DefaultCellStyle.ForeColor = AppTheme.TextPrimary;
            dgvArtists.DefaultCellStyle.SelectionBackColor = AppTheme.Primary;
            dgvArtists.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvArtists.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.BgLight;
            dgvArtists.ColumnHeadersDefaultCellStyle.ForeColor = AppTheme.TextPrimary;
            dgvArtists.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dgvArtists.EnableHeadersVisualStyles = false;
        }

        private void LoadData()
        {
            try
            {
                dgvArtists.Rows.Clear();
                var entries = DbcManager.LoadFromCsv<ArtistAliasEntry>("Artists.dbc");
                foreach (var e in entries.OrderBy(x => x.ArtistName))
                {
                    dgvArtists.Rows.Add(e.ArtistName, e.Aliases ?? "");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ArtistAliasManager] ⚠️ Errore caricamento: {ex.Message}");
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using (var dlg = new ArtistAliasEditDialog(new ArtistAliasEntry()))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        DbcManager.Insert("Artists.dbc", dlg.Entry);
                        LoadData();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Errore: {ex.Message}", "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (dgvArtists.CurrentRow == null) return;

            string artistName = dgvArtists.CurrentRow.Cells["colArtist"].Value?.ToString() ?? "";
            string aliases = dgvArtists.CurrentRow.Cells["colAliases"].Value?.ToString() ?? "";

            var existing = DbcManager.LoadFromCsv<ArtistAliasEntry>("Artists.dbc");
            var entry = existing.FirstOrDefault(x =>
                string.Equals(x.ArtistName, artistName, StringComparison.OrdinalIgnoreCase));

            if (entry == null) return;

            using (var dlg = new ArtistAliasEditDialog(entry))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        DbcManager.Update("Artists.dbc", dlg.Entry);
                        LoadData();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Errore: {ex.Message}", "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (dgvArtists.CurrentRow == null) return;

            string artistName = dgvArtists.CurrentRow.Cells["colArtist"].Value?.ToString() ?? "";
            var result = MessageBox.Show(
                string.Format(LanguageManager.GetString("ArtistAliasManager.ConfirmDelete", "Eliminare l'alias per '{0}'?"), artistName),
                LanguageManager.GetString("Common.Confirm", "Conferma"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            try
            {
                var existing = DbcManager.LoadFromCsv<ArtistAliasEntry>("Artists.dbc");
                var entry = existing.FirstOrDefault(x =>
                    string.Equals(x.ArtistName, artistName, StringComparison.OrdinalIgnoreCase));

                if (entry != null)
                {
                    DbcManager.Delete<ArtistAliasEntry>("Artists.dbc", entry.ID);
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore: {ex.Message}", "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnAutoScan_Click(object sender, EventArgs e)
        {
            try
            {
                var existingAliases = DbcManager.LoadFromCsv<ArtistAliasEntry>("Artists.dbc");
                var existingNames = new HashSet<string>(
                    existingAliases.Select(x => x.ArtistName?.Trim()),
                    StringComparer.OrdinalIgnoreCase);

                var allMusic = DbcManager.LoadFromCsv<MusicEntry>("Music.dbc");
                var newArtists = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var m in allMusic)
                {
                    // Estrai artisti da Artist e FeaturedArtists
                    var (primary, featured) = ArtistParsingService.ParseArtists(m.Artist, m.Title, existingAliases);

                    if (!string.IsNullOrWhiteSpace(primary) && !existingNames.Contains(primary))
                        newArtists.Add(primary);

                    foreach (var fa in featured)
                    {
                        if (!string.IsNullOrWhiteSpace(fa) && !existingNames.Contains(fa))
                            newArtists.Add(fa);
                    }

                    // Aggiungi anche FeaturedArtists esistenti
                    if (!string.IsNullOrWhiteSpace(m.FeaturedArtists))
                    {
                        foreach (var fa in m.FeaturedArtists.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            string trimmed = fa.Trim();
                            if (!string.IsNullOrWhiteSpace(trimmed) && !existingNames.Contains(trimmed))
                                newArtists.Add(trimmed);
                        }
                    }
                }

                if (newArtists.Count == 0)
                {
                    MessageBox.Show(
                        LanguageManager.GetString("ArtistAliasManager.NoNewArtists", "Nessun nuovo artista trovato."),
                        LanguageManager.GetString("ArtistAliasManager.AutoScan", "Auto-Scan"),
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                int added = 0;
                foreach (var name in newArtists.OrderBy(n => n))
                {
                    try
                    {
                        DbcManager.Insert("Artists.dbc", new ArtistAliasEntry
                        {
                            ArtistName = name,
                            Aliases = ""
                        });
                        added++;
                    }
                    catch { }
                }

                LoadData();
                MessageBox.Show(
                    string.Format(LanguageManager.GetString("ArtistAliasManager.AutoScanResult",
                        "Auto-Scan completato: {0} nuovi artisti aggiunti."), added),
                    LanguageManager.GetString("ArtistAliasManager.AutoScan", "Auto-Scan"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore Auto-Scan: {ex.Message}", "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    /// <summary>
    /// Dialog per aggiungere/modificare un alias artista
    /// </summary>
    public class ArtistAliasEditDialog : Form
    {
        public ArtistAliasEntry Entry { get; private set; }

        private TextBox txtArtistName;
        private TextBox txtAliases;

        public ArtistAliasEditDialog(ArtistAliasEntry entry)
        {
            Entry = entry ?? new ArtistAliasEntry();
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            bool isNew = Entry.ID == 0;
            this.Text = isNew
                ? LanguageManager.GetString("ArtistAliasManager.AddArtist", "Aggiungi Artista")
                : LanguageManager.GetString("ArtistAliasManager.EditArtist", "Modifica Artista");
            this.Size = new Size(420, 200);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = AppTheme.BgDark;

            var lblName = new Label
            {
                Text = LanguageManager.GetString("ArtistAliasManager.ArtistName", "Nome Canonico:"),
                Location = new Point(10, 15),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = AppTheme.TextPrimary
            };
            txtArtistName = new TextBox
            {
                Location = new Point(150, 12),
                Size = new Size(240, 23),
                Font = new Font("Segoe UI", 9F),
                Text = Entry.ArtistName ?? "",
                BackColor = AppTheme.Surface,
                ForeColor = AppTheme.TextPrimary
            };

            var lblAliases = new Label
            {
                Text = LanguageManager.GetString("ArtistAliasManager.Aliases", "Alias (sep. da ;):"),
                Location = new Point(10, 50),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = AppTheme.TextPrimary
            };
            txtAliases = new TextBox
            {
                Location = new Point(150, 47),
                Size = new Size(240, 23),
                Font = new Font("Segoe UI", 9F),
                Text = Entry.Aliases ?? "",
                BackColor = AppTheme.Surface,
                ForeColor = AppTheme.TextPrimary
            };

            var btnOk = new Button
            {
                Text = "✔ OK",
                DialogResult = DialogResult.OK,
                Location = new Point(230, 130),
                Size = new Size(80, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = AppTheme.Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnOk.FlatAppearance.BorderSize = 0;

            var btnCancel = new Button
            {
                Text = "✖ Annulla",
                DialogResult = DialogResult.Cancel,
                Location = new Point(318, 130),
                Size = new Size(80, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = AppTheme.BgLight,
                ForeColor = AppTheme.TextPrimary,
                Font = new Font("Segoe UI", 9F)
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            btnOk.Click += (s, e) =>
            {
                Entry.ArtistName = txtArtistName.Text.Trim();
                Entry.Aliases = txtAliases.Text.Trim();
            };

            this.Controls.AddRange(new Control[] { lblName, txtArtistName, lblAliases, txtAliases, btnOk, btnCancel });
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }
    }
}
