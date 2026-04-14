using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
        // ── Controls ────────────────────────────────────────────────────────
        private SplitContainer splitMain;

        // Left panel
        private DataGridView dgvSongs;
        private Label lblSongsHeader;

        // Right panel
        private DataGridView dgvAliases;
        private Label lblAliasesHeader;
        private FlowLayoutPanel rightButtonsPanel;
        private Button btnAddAlias;
        private Button btnRemoveAlias;

        // Bottom bar
        private Panel bottomPanel;
        private FlowLayoutPanel bottomButtonsPanel;
        private Button btnAutoScan;
        private Button btnClose;

        // ── State ────────────────────────────────────────────────────────────
        private MusicEntry _selectedSong;
        private List<ArtistAliasEntry> _artistEntries = new List<ArtistAliasEntry>();

        /// <summary>
        /// Path of the file that tracks music IDs whose featured-artists were
        /// edited manually (auto-scan will skip these songs).
        /// </summary>
        private static string ManuallyEditedFilePath =>
            DbcManager.GetFilePath("ManuallyEditedAliases.txt");

        private HashSet<int> _manuallyEditedIds = new HashSet<int>();

        // ────────────────────────────────────────────────────────────────────

        public ArtistAliasManagerForm()
        {
            InitializeComponents();
            ApplyTheme();
            LoadManuallyEditedIds();
            LoadArtistEntries();
            LoadSongs();
        }

        // ── Initialization ───────────────────────────────────────────────────

        private void InitializeComponents()
        {
            this.Text = "🎤 " + LanguageManager.GetString("ArtistAlias.Title", "Gestione Alias Artisti");
            this.Size = new Size(1000, 600);
            this.MinimumSize = new Size(800, 480);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;

            // ── Bottom bar ──────────────────────────────────────────────────
            bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 46,
                Padding = new Padding(6, 6, 6, 6)
            };

            btnAutoScan = CreateButton(
                "🔍 " + LanguageManager.GetString("ArtistAlias.AutoScan", "Auto-Scan"),
                AppTheme.BgLight);
            btnClose = CreateButton(
                "✖ " + LanguageManager.GetString("ArtistAlias.Close", "Chiudi"),
                AppTheme.BgLight);

            btnAutoScan.Click += BtnAutoScan_Click;
            btnClose.Click += (s, e) => this.Close();

            bottomButtonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            bottomButtonsPanel.Controls.Add(btnAutoScan);
            bottomButtonsPanel.Controls.Add(btnClose);
            bottomPanel.Controls.Add(bottomButtonsPanel);

            // ── SplitContainer ──────────────────────────────────────────────
            splitMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 500,
                SplitterWidth = 5,
                IsSplitterFixed = false,
                Panel1MinSize = 200,
                Panel2MinSize = 200
            };

            // ── Left panel: songs with featured artists ─────────────────────
            lblSongsHeader = new Label
            {
                Text = LanguageManager.GetString("ArtistAlias.TracksWithAliases", "Brani con Alias Artisti"),
                Dock = DockStyle.Top,
                Height = 24,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(4, 0, 0, 0)
            };

            dgvSongs = new DataGridView
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
            dgvSongs.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colArtist",
                HeaderText = LanguageManager.GetString("ArtistAlias.Artist", "Artista"),
                FillWeight = 30
            });
            dgvSongs.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colTitle",
                HeaderText = LanguageManager.GetString("MusicEditor.Title", "Titolo"),
                FillWeight = 40
            });
            dgvSongs.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colFeatured",
                HeaderText = LanguageManager.GetString("ArtistAlias.ArtistAliases", "Alias Artisti del Brano"),
                FillWeight = 30
            });
            dgvSongs.SelectionChanged += DgvSongs_SelectionChanged;

            splitMain.Panel1.Controls.Add(dgvSongs);
            splitMain.Panel1.Controls.Add(lblSongsHeader);

            // ── Right panel: aliases for selected song ──────────────────────
            lblAliasesHeader = new Label
            {
                Text = LanguageManager.GetString("ArtistAlias.ArtistAliases", "Alias Artisti del Brano"),
                Dock = DockStyle.Top,
                Height = 24,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(4, 0, 0, 0)
            };

            dgvAliases = new DataGridView
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
            dgvAliases.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colAliasArtist",
                HeaderText = LanguageManager.GetString("ArtistAlias.Artist", "Artista"),
                FillWeight = 40
            });
            dgvAliases.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colCanonical",
                HeaderText = LanguageManager.GetString("ArtistAlias.ArtistName", "Nome Artista Canonico"),
                FillWeight = 35
            });
            dgvAliases.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colAliasesKnown",
                HeaderText = LanguageManager.GetString("ArtistAlias.Aliases", "Alias (separati da ;)"),
                FillWeight = 25
            });

            rightButtonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(4, 6, 4, 6),
                Margin = new Padding(0)
            };

            btnAddAlias = CreateButton(
                "➕ " + LanguageManager.GetString("ArtistAlias.AddAlias", "Aggiungi Alias"),
                AppTheme.Primary);
            btnRemoveAlias = CreateButton(
                "🗑️ " + LanguageManager.GetString("ArtistAlias.RemoveAlias", "Rimuovi"),
                Color.IndianRed);

            btnAddAlias.Click += BtnAddAlias_Click;
            btnRemoveAlias.Click += BtnRemoveAlias_Click;

            rightButtonsPanel.Controls.Add(btnAddAlias);
            rightButtonsPanel.Controls.Add(btnRemoveAlias);

            splitMain.Panel2.Controls.Add(dgvAliases);
            splitMain.Panel2.Controls.Add(lblAliasesHeader);
            splitMain.Panel2.Controls.Add(rightButtonsPanel);

            // ── Assembly ────────────────────────────────────────────────────
            this.Controls.Add(splitMain);
            this.Controls.Add(bottomPanel);

            this.Shown += (s, e) => UpdateSplitDistance();
            this.Resize += (s, e) => UpdateSplitDistance();
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

        // ── Theme ────────────────────────────────────────────────────────────

        private void ApplyTheme()
        {
            this.BackColor = AppTheme.BgDark;
            bottomPanel.BackColor = AppTheme.BgDark;
            bottomButtonsPanel.BackColor = AppTheme.BgDark;
            rightButtonsPanel.BackColor = AppTheme.BgDark;
            splitMain.BackColor = AppTheme.BgDark;

            lblSongsHeader.BackColor = AppTheme.BgLight;
            lblSongsHeader.ForeColor = AppTheme.TextPrimary;
            lblAliasesHeader.BackColor = AppTheme.BgLight;
            lblAliasesHeader.ForeColor = AppTheme.TextPrimary;

            ApplyGridTheme(dgvSongs);
            ApplyGridTheme(dgvAliases);
        }

        private void ApplyGridTheme(DataGridView dgv)
        {
            dgv.BackgroundColor = AppTheme.BgDark;
            dgv.GridColor = AppTheme.BorderLight;
            dgv.DefaultCellStyle.BackColor = AppTheme.Surface;
            dgv.DefaultCellStyle.ForeColor = AppTheme.TextPrimary;
            dgv.DefaultCellStyle.SelectionBackColor = AppTheme.Primary;
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.BgLight;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = AppTheme.TextPrimary;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dgv.EnableHeadersVisualStyles = false;
        }

        // ── Data loading ─────────────────────────────────────────────────────

        private void LoadManuallyEditedIds()
        {
            _manuallyEditedIds.Clear();
            try
            {
                string path = ManuallyEditedFilePath;
                if (File.Exists(path))
                {
                    foreach (var line in File.ReadAllLines(path))
                    {
                        if (int.TryParse(line.Trim(), out int id))
                            _manuallyEditedIds.Add(id);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ArtistAliasManager] ⚠️ Errore caricamento ManuallyEditedAliases: {ex.Message}");
            }
        }

        private void SaveManuallyEditedIds()
        {
            try
            {
                File.WriteAllLines(ManuallyEditedFilePath,
                    _manuallyEditedIds.Select(id => id.ToString()));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ArtistAliasManager] ⚠️ Errore salvataggio ManuallyEditedAliases: {ex.Message}");
            }
        }

        private void LoadArtistEntries()
        {
            try
            {
                _artistEntries = DbcManager.LoadFromCsv<ArtistAliasEntry>("Artists.dbc");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ArtistAliasManager] ⚠️ Errore caricamento Artists.dbc: {ex.Message}");
                _artistEntries = new List<ArtistAliasEntry>();
            }
        }

        private void LoadSongs()
        {
            try
            {
                int? previousId = (_selectedSong != null) ? (int?)_selectedSong.ID : null;

                dgvSongs.Rows.Clear();
                _selectedSong = null;

                var allMusic = DbcManager.LoadFromCsv<MusicEntry>("Music.dbc");
                var songsWithAliases = allMusic
                    .Where(m => !string.IsNullOrWhiteSpace(m.FeaturedArtists))
                    .OrderBy(m => m.Artist)
                    .ThenBy(m => m.Title)
                    .ToList();

                foreach (var m in songsWithAliases)
                {
                    int rowIdx = dgvSongs.Rows.Add(m.Artist, m.Title, m.FeaturedArtists);
                    dgvSongs.Rows[rowIdx].Tag = m;
                }

                // Restore previous selection if possible
                if (previousId.HasValue)
                {
                    foreach (DataGridViewRow row in dgvSongs.Rows)
                    {
                        if (row.Tag is MusicEntry me && me.ID == previousId.Value)
                        {
                            row.Selected = true;
                            dgvSongs.CurrentCell = row.Cells[0];
                            break;
                        }
                    }
                }

                RefreshAliasPanel();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ArtistAliasManager] ⚠️ Errore caricamento brani: {ex.Message}");
            }
        }

        private void RefreshAliasPanel()
        {
            dgvAliases.Rows.Clear();

            if (_selectedSong == null)
            {
                lblAliasesHeader.Text = LanguageManager.GetString(
                    "ArtistAlias.ArtistAliases", "Alias Artisti del Brano");
                return;
            }

            lblAliasesHeader.Text = LanguageManager.GetString("ArtistAlias.ArtistAliases", "Alias Artisti del Brano");

            var (primaryArtist, parsedFeatured) = ArtistParsingService.ParseArtists(
                _selectedSong.Artist, _selectedSong.Title, _artistEntries);

            var songArtists = new List<string>();
            AddIfMissing(songArtists, primaryArtist);
            foreach (var detected in parsedFeatured)
                AddIfMissing(songArtists, detected);

            foreach (var explicitArtist in (_selectedSong.FeaturedArtists ?? "")
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(n => n.Trim())
                .Where(n => !string.IsNullOrWhiteSpace(n)))
            {
                AddIfMissing(songArtists, explicitArtist);
            }

            foreach (var name in songArtists)
            {
                // Find canonical name and known aliases from Artists.dbc
                var entry = _artistEntries.FirstOrDefault(a =>
                    string.Equals(a.ArtistName?.Trim(), name, StringComparison.OrdinalIgnoreCase)
                    || (a.Aliases ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Any(alias => string.Equals(alias.Trim(), name, StringComparison.OrdinalIgnoreCase)));

                string canonical = entry?.ArtistName ?? name;
                string aliases = entry?.Aliases ?? "";
                dgvAliases.Rows.Add(name, canonical, aliases);
            }
        }

        // ── Event handlers ────────────────────────────────────────────────────

        private void DgvSongs_SelectionChanged(object sender, EventArgs e)
        {
            _selectedSong = null;
            if (dgvSongs.CurrentRow?.Tag is MusicEntry me)
                _selectedSong = me;

            RefreshAliasPanel();
        }

        private void BtnAddAlias_Click(object sender, EventArgs e)
        {
            if (_selectedSong == null)
            {
                MessageBox.Show(
                    LanguageManager.GetString("ArtistAlias.SelectSongFirst", "Seleziona prima un brano."),
                    LanguageManager.GetString("ArtistAlias.Title", "Gestione Alias Artisti"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string newArtist = Microsoft.VisualBasic.Interaction.InputBox(
                LanguageManager.GetString("ArtistAlias.EnterArtistName", "Inserisci il nome dell'artista da aggiungere:"),
                LanguageManager.GetString("ArtistAlias.AddAlias", "Aggiungi Alias"),
                "");

            if (string.IsNullOrWhiteSpace(newArtist)) return;

            newArtist = newArtist.Trim();

            // Check if already present
            var existing = (_selectedSong.FeaturedArtists ?? "")
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(n => n.Trim());

            if (existing.Any(n => string.Equals(n, newArtist, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show(
                    LanguageManager.GetString("ArtistAlias.ArtistAlreadyPresent", "L'artista è già presente."),
                    LanguageManager.GetString("ArtistAlias.Title", "Gestione Alias Artisti"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                // Update FeaturedArtists in Music.dbc
                var updatedList = existing.ToList();
                updatedList.Add(newArtist);
                _selectedSong.FeaturedArtists = string.Join(";", updatedList);
                DbcManager.Update("Music.dbc", _selectedSong);

                // Mark as manually edited
                _manuallyEditedIds.Add(_selectedSong.ID);
                SaveManuallyEditedIds();

                // Persist artist in Artists.dbc if unknown
                PersistArtistEntry(newArtist);

                LoadArtistEntries();
                LoadSongs();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{LanguageManager.GetString("Common.Error", "Errore")}: {ex.Message}",
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnRemoveAlias_Click(object sender, EventArgs e)
        {
            if (_selectedSong == null || dgvAliases.CurrentRow == null) return;

            string artistToRemove = dgvAliases.CurrentRow.Cells["colAliasArtist"].Value?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(artistToRemove)) return;

            var confirmResult = MessageBox.Show(
                string.Format(
                    LanguageManager.GetString("ArtistAlias.ConfirmDelete",
                        "Eliminare l'alias per '{0}'?"), artistToRemove),
                LanguageManager.GetString("Common.Confirm", "Conferma"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirmResult != DialogResult.Yes) return;

            try
            {
                var updatedList = (_selectedSong.FeaturedArtists ?? "")
                    .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(n => n.Trim())
                    .Where(n => !string.Equals(n, artistToRemove, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                _selectedSong.FeaturedArtists = string.Join(";", updatedList);
                DbcManager.Update("Music.dbc", _selectedSong);

                // Mark as manually edited
                _manuallyEditedIds.Add(_selectedSong.ID);
                SaveManuallyEditedIds();

                LoadSongs();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{LanguageManager.GetString("Common.Error", "Errore")}: {ex.Message}",
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnAutoScan_Click(object sender, EventArgs e)
        {
            try
            {
                LoadArtistEntries();
                var allMusic = DbcManager.LoadFromCsv<MusicEntry>("Music.dbc");
                int newArtistsAdded = 0;

                foreach (var m in allMusic)
                {
                    // Skip songs manually edited by the user
                    if (_manuallyEditedIds.Contains(m.ID)) continue;

                    var (primary, detected) = ArtistParsingService.ParseArtists(m.Artist, m.Title, _artistEntries);

                    var detectedAll = new List<string>();
                    AddIfMissing(detectedAll, primary);
                    foreach (var detectedArtist in detected)
                        AddIfMissing(detectedAll, detectedArtist);

                    if (detectedAll.Count == 0) continue;

                    // Merge detected with existing FeaturedArtists (don't overwrite custom ones)
                    var existing = (m.FeaturedArtists ?? "")
                        .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(n => n.Trim())
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .ToList();
                    var existingSet = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);

                    bool changed = false;
                    foreach (var d in detectedAll)
                    {
                        if (existingSet.Add(d))
                        {
                            existing.Add(d);
                            changed = true;
                            newArtistsAdded++;
                        }
                    }

                    if (changed)
                    {
                        m.FeaturedArtists = string.Join(";", existing);
                        DbcManager.Update("Music.dbc", m);
                        // Persist any new artists in Artists.dbc
                        foreach (var d in detectedAll)
                            PersistArtistEntry(d);
                    }
                }

                LoadArtistEntries();
                LoadSongs();

                var message = newArtistsAdded > 0
                    ? string.Format(
                        LanguageManager.GetString("ArtistAlias.AutoScanResult",
                            "Auto-Scan completato: {0} nuovi artisti aggiunti."), newArtistsAdded)
                    : LanguageManager.GetString("ArtistAlias.NoNewArtists", "Nessun nuovo artista trovato.");

                MessageBox.Show(
                    message,
                    LanguageManager.GetString("ArtistAlias.AutoScan", "Auto-Scan"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{LanguageManager.GetString("Common.Error", "Errore")}: {ex.Message}",
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void UpdateSplitDistance()
        {
            if (splitMain == null || splitMain.ClientSize.Width <= 0) return;

            int target = splitMain.ClientSize.Width / 2;
            int min = splitMain.Panel1MinSize;
            int max = splitMain.ClientSize.Width - splitMain.Panel2MinSize;
            splitMain.SplitterDistance = Math.Max(min, Math.Min(target, max));
        }

        private static void AddIfMissing(List<string> list, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            string trimmed = value.Trim();
            if (!list.Any(v => string.Equals(v, trimmed, StringComparison.OrdinalIgnoreCase)))
                list.Add(trimmed);
        }

        private void PersistArtistEntry(string artistName)
        {
            try
            {
                bool alreadyExists = _artistEntries.Any(a =>
                    string.Equals(a.ArtistName?.Trim(), artistName, StringComparison.OrdinalIgnoreCase));

                if (!alreadyExists)
                {
                    var newEntry = new ArtistAliasEntry { ArtistName = artistName, Aliases = "" };
                    DbcManager.Insert("Artists.dbc", newEntry);
                    _artistEntries.Add(newEntry);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ArtistAliasManager] ⚠️ Errore salvataggio artista: {ex.Message}");
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
                ? LanguageManager.GetString("ArtistAlias.AddArtist", "Aggiungi Artista")
                : LanguageManager.GetString("ArtistAlias.EditArtist", "Modifica Artista");
            this.Size = new Size(420, 200);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = AppTheme.BgDark;

            var lblName = new Label
            {
                Text = LanguageManager.GetString("ArtistAlias.ArtistName", "Nome Artista Canonico"),
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
                Text = LanguageManager.GetString("ArtistAlias.Aliases", "Alias (separati da ;)"),
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
                Text = "✔ " + (isNew
                    ? LanguageManager.GetString("ArtistAlias.Add", "Aggiungi")
                    : LanguageManager.GetString("ArtistAlias.Edit", "Modifica")),
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
                Text = "✖ " + LanguageManager.GetString("Common.Cancel", "Annulla"),
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
