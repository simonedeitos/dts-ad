using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using AirDirector.Models;
using AirDirector.Services.Database;
using AirDirector.Services.Localization;

namespace AirDirector.Forms
{
    public partial class LoadPlaylistOnAirForm : Form
    {
        // ── Properties ──────────────────────────────────────────────────────
        public AirPlaylist SelectedPlaylist { get; private set; }
        public bool IsImmediate { get; private set; }

        // ── Controls ────────────────────────────────────────────────────────
        private TextBox _txtSearch;
        private DataGridView _dgvPlaylists;
        private Button _btnImmediate;
        private Button _btnEnqueue;

        // ── Data ────────────────────────────────────────────────────────────
        private List<PlaylistEntry> _allEntries = new List<PlaylistEntry>();

        private class PlaylistEntry
        {
            public string FilePath { get; set; }
            public string Name { get; set; }
            public DateTime Modified { get; set; }
            public int ItemCount { get; set; }
        }

        // ── Constructor ─────────────────────────────────────────────────────
        public LoadPlaylistOnAirForm()
        {
            InitializeForm();
            LoadPlaylists();
        }

        private void InitializeForm()
        {
            this.Text = LanguageManager.GetString("LoadPlaylistOnAir.Title", "Carica Playlist OnAir");
            this.Size = new Size(550, 500);
            this.MinimumSize = new Size(450, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(25, 25, 25);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9f);
            this.FormBorderStyle = FormBorderStyle.Sizable;

            // ── Header panel ──────────────────────────────────────────────
            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 45,
                BackColor = Color.FromArgb(0, 150, 180)
            };
            Label lblTitle = new Label
            {
                Text = "📻 " + LanguageManager.GetString("LoadPlaylistOnAir.Header", "CARICA PLAYLIST ON AIR"),
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0)
            };
            headerPanel.Controls.Add(lblTitle);

            // ── Search row ────────────────────────────────────────────────
            Panel searchPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 38,
                BackColor = Color.FromArgb(35, 35, 35),
                Padding = new Padding(8, 6, 8, 4)
            };
            _txtSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.FromArgb(150, 150, 150),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9f),
                Text = LanguageManager.GetString("LoadPlaylistOnAir.SearchPlaceholder", "Cerca playlist...")
            };
            _txtSearch.GotFocus += (s, e) =>
            {
                if (_txtSearch.ForeColor == Color.FromArgb(150, 150, 150))
                {
                    _txtSearch.Text = "";
                    _txtSearch.ForeColor = Color.White;
                }
            };
            _txtSearch.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(_txtSearch.Text))
                {
                    _txtSearch.Text = LanguageManager.GetString("LoadPlaylistOnAir.SearchPlaceholder", "Cerca playlist...");
                    _txtSearch.ForeColor = Color.FromArgb(150, 150, 150);
                }
            };
            _txtSearch.TextChanged += (s, e) => FilterGrid();
            searchPanel.Controls.Add(_txtSearch);

            // ── Grid ──────────────────────────────────────────────────────
            _dgvPlaylists = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.FromArgb(30, 30, 30),
                GridColor = Color.FromArgb(50, 50, 50),
                BorderStyle = BorderStyle.None,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(40, 40, 40),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    SelectionBackColor = Color.FromArgb(40, 40, 40)
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(35, 35, 35),
                    ForeColor = Color.White,
                    SelectionBackColor = Color.FromArgb(0, 120, 150),
                    SelectionForeColor = Color.White
                },
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 28,
                RowTemplate = { Height = 28 }
            };
            _dgvPlaylists.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Name",
                HeaderText = LanguageManager.GetString("LoadPlaylistOnAir.ColName", "Nome"),
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });
            _dgvPlaylists.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Modified",
                HeaderText = LanguageManager.GetString("LoadPlaylistOnAir.ColModified", "Data Modifica"),
                Width = 130
            });
            _dgvPlaylists.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Items",
                HeaderText = LanguageManager.GetString("LoadPlaylistOnAir.ColItems", "Elementi"),
                Width = 70
            });
            _dgvPlaylists.DoubleClick += DgvPlaylists_DoubleClick;

            // ── Bottom buttons panel ───────────────────────────────────────
            Panel btnPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                BackColor = Color.FromArgb(35, 35, 35),
                Padding = new Padding(10, 8, 10, 8)
            };
            _btnImmediate = new Button
            {
                Text = "▶ " + LanguageManager.GetString("LoadPlaylistOnAir.BtnImmediate", "Subito in Onda"),
                BackColor = Color.FromArgb(33, 150, 83),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Height = 34,
                Width = 180,
                Left = btnPanel.Padding.Left,
                Top = btnPanel.Padding.Top
            };
            _btnImmediate.FlatAppearance.BorderSize = 0;
            _btnImmediate.Click += BtnImmediate_Click;

            _btnEnqueue = new Button
            {
                Text = "➕ " + LanguageManager.GetString("LoadPlaylistOnAir.BtnEnqueue", "Accoda"),
                BackColor = Color.FromArgb(33, 100, 183),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Height = 34,
                Width = 140,
                Top = btnPanel.Padding.Top
            };
            _btnEnqueue.FlatAppearance.BorderSize = 0;
            _btnEnqueue.Click += BtnEnqueue_Click;

            // Position buttons
            btnPanel.Controls.Add(_btnImmediate);
            btnPanel.Controls.Add(_btnEnqueue);
            btnPanel.Resize += (s, e) =>
            {
                _btnImmediate.Left = btnPanel.Padding.Left;
                _btnEnqueue.Left = _btnImmediate.Right + 10;
            };

            // ── Assemble ──────────────────────────────────────────────────
            this.Controls.Add(_dgvPlaylists);
            this.Controls.Add(searchPanel);
            this.Controls.Add(headerPanel);
            this.Controls.Add(btnPanel);
        }

        private void LoadPlaylists()
        {
            _allEntries.Clear();
            try
            {
                string folder = Path.Combine(DbcManager.GetDatabasePath(), "Playlist");
                if (!Directory.Exists(folder))
                    return;

                foreach (string filePath in Directory.GetFiles(folder, "*.airpls"))
                {
                    try
                    {
                        var playlist = AirPlaylist.Load(filePath);
                        _allEntries.Add(new PlaylistEntry
                        {
                            FilePath = filePath,
                            Name = playlist.Name ?? Path.GetFileNameWithoutExtension(filePath),
                            Modified = File.GetLastWriteTime(filePath),
                            ItemCount = playlist.Items?.Count ?? 0
                        });
                    }
                    catch
                    {
                        // Skip broken files
                    }
                }

                _allEntries = _allEntries.OrderBy(e => e.Name).ToList();
            }
            catch
            {
                // Ignore if folder not accessible
            }

            FilterGrid();
        }

        private void FilterGrid()
        {
            string search = _txtSearch.ForeColor == Color.FromArgb(150, 150, 150)
                ? ""
                : (_txtSearch.Text ?? "").Trim();

            _dgvPlaylists.Rows.Clear();
            var filtered = string.IsNullOrEmpty(search)
                ? _allEntries
                : _allEntries.Where(e => e.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            foreach (var entry in filtered)
            {
                int rowIdx = _dgvPlaylists.Rows.Add(
                    entry.Name,
                    entry.Modified.ToString("dd/MM/yyyy HH:mm"),
                    entry.ItemCount.ToString()
                );
                _dgvPlaylists.Rows[rowIdx].Tag = entry;
            }
        }

        private PlaylistEntry GetSelectedEntry()
        {
            if (_dgvPlaylists.SelectedRows.Count == 0)
                return null;
            return _dgvPlaylists.SelectedRows[0].Tag as PlaylistEntry;
        }

        private bool TryLoadSelected(out AirPlaylist playlist)
        {
            playlist = null;
            var entry = GetSelectedEntry();
            if (entry == null)
                return false;
            try
            {
                playlist = AirPlaylist.Load(entry.FilePath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void BtnImmediate_Click(object sender, EventArgs e)
        {
            if (!TryLoadSelected(out AirPlaylist pl))
                return;
            SelectedPlaylist = pl;
            IsImmediate = true;
            this.DialogResult = DialogResult.Yes;
            this.Close();
        }

        private void BtnEnqueue_Click(object sender, EventArgs e)
        {
            if (!TryLoadSelected(out AirPlaylist pl))
                return;
            SelectedPlaylist = pl;
            IsImmediate = false;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void DgvPlaylists_DoubleClick(object sender, EventArgs e)
        {
            BtnEnqueue_Click(sender, e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                components?.Dispose();
            base.Dispose(disposing);
        }
    }
}
