using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using NAudio.Wave;
using AirDirector.Services.Database;
using AirDirector.Services.Licensing;
using AirDirector.Services.Localization;
using AirDirector.Forms;
using AirDirector.Themes;

namespace AirDirector.Controls
{
    public partial class ArchiveControl : UserControl
    {
        private string _archiveType;
        private ArchiveDataGridView dgvArchive;
        private Panel headerPanel;
        private Label lblHeader;
        private TextBox txtSearch;
        private ComboBox cmbGenreFilter;
        private ComboBox cmbCategoryFilter;
        private Button btnClearFilters;
        private Button btnZoomIn;
        private Button btnZoomOut;
        private Button btnImport;
        private Button btnEdit;
        private Button btnDelete;
        private Button btnRefresh;
        private List<MusicEntry> _allMusicData;
        private List<ClipEntry> _allClipsData;
        private float _currentFontSize = 10F;

        private Panel pnlMiniPlayer;
        private Button btnPlayStop;
        private TrackBar trackPosition;
        private Label lblTimeCounter;
        private Label lblPreviewTitle;
        private WaveOutEvent _previewPlayer;
        private AudioFileReader _previewAudioFile;
        private System.Windows.Forms.Timer _previewTimer;
        private bool _isPreviewPlaying = false;
        private bool _userIsDraggingSlider = false;

        private int _previewSessionCounter = 0;
        private readonly object _previewLock = new object();

        private bool _isDragging = false;

        private Services.Core.DailyLogger _dailyLogger;
        private Point _dragStartPoint;

        // Batch progress overlay
        private Panel _batchProgressPanel;
        private Label _lblBatchProgress;
        private ProgressBar _pbBatch;

        public event EventHandler<string> StatusChanged;

        private bool _isInitializing = true;

        public ArchiveControl(string archiveType)
        {
            InitializeComponent();
            _archiveType = archiveType;
            _allMusicData = new List<MusicEntry>();
            _allClipsData = new List<ClipEntry>();
            try { _dailyLogger = new Services.Core.DailyLogger("Archive"); } catch { }
            InitializeUI();
            InitializeMiniPlayer();

            _isInitializing = false;

            ApplyLanguage();
            RefreshArchive();

            LanguageManager.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            if (btnImport != null)
                btnImport.Text = "📥 " + LanguageManager.GetString("Archive.Import", "Importa");

            if (btnEdit != null)
                btnEdit.Text = "✏️ " + LanguageManager.GetString("Archive.Edit", "Modifica");

            if (btnDelete != null)
                btnDelete.Text = "🗑️ " + LanguageManager.GetString("Archive.Delete", "Elimina");

            if (txtSearch != null)
                txtSearch.PlaceholderText = "🔍 " + LanguageManager.GetString("Archive.SearchPlaceholder", "Cerca artista o titolo...");

            if (btnClearFilters != null)
                btnClearFilters.Text = "✖ " + LanguageManager.GetString("Archive.Reset", "Reset");

            if (cmbGenreFilter != null && cmbGenreFilter.Items.Count > 0)
            {
                int selectedIndex = cmbGenreFilter.SelectedIndex;
                string firstItem = LanguageManager.GetString("Archive.AllGenres", "Tutti i Generi");
                cmbGenreFilter.Items[0] = firstItem;
                if (selectedIndex >= 0)
                    cmbGenreFilter.SelectedIndex = selectedIndex;
            }

            if (cmbCategoryFilter != null && cmbCategoryFilter.Items.Count > 0)
            {
                int selectedIndex = cmbCategoryFilter.SelectedIndex;
                string firstItem = LanguageManager.GetString("Archive.AllCategories", "Tutte le Categorie");
                cmbCategoryFilter.Items[0] = firstItem;
                if (selectedIndex >= 0)
                    cmbCategoryFilter.SelectedIndex = selectedIndex;
            }

            if (lblPreviewTitle != null && lblPreviewTitle.Text.Contains("Nessun"))
                lblPreviewTitle.Text = LanguageManager.GetString("Archive.NoPreview", "Nessun file in preascolto");

            if (dgvArchive?.ContextMenuStrip != null)
            {
                foreach (ToolStripItem item in dgvArchive.ContextMenuStrip.Items)
                {
                    if (item is ToolStripMenuItem mi)
                    {
                        switch (mi.Tag as string)
                        {
                            case "ctx_preview":     mi.Text = "🎧 " + LanguageManager.GetString("Archive.Preview", "Preascolto"); break;
                            case "ctx_associate":   mi.Text = "🎬 " + LanguageManager.GetString("Archive.AssociateVideo", "Associa Video..."); break;
                            case "ctx_remove":      mi.Text = "🗑️ " + LanguageManager.GetString("Archive.RemoveVideo", "Rimuovi Video"); break;
                            case "ctx_edit":        mi.Text = "✏️ " + LanguageManager.GetString("Archive.EditGenreCategory", "Modifica Genere/Categoria"); break;
                            case "ctx_history":     mi.Text = "📋 " + LanguageManager.GetString("Archive.ShowPlayHistory", "Mostra Storico Passaggi"); break;
                            case "ctx_show_folder": mi.Text = "📁 " + LanguageManager.GetString("Archive.ShowFileFolder", "Mostra cartella del file"); break;
                            case "ctx_export":      mi.Text = "📤 " + LanguageManager.GetString("Archive.ExportSelectedFiles", "Esporta file selezionati"); break;
                        }
                    }
                }
            }

            UpdateHeaderCount(_archiveType == "Music" ? _allMusicData.Count : _allClipsData.Count);

            if (!_isInitializing)
                UpdateColumnHeaders();
        }

        private string FormatDurationMs(double durationMs)
        {
            double seconds = durationMs / 1000.0;
            int minutes = (int)(seconds / 60);
            int secs = (int)(seconds % 60);
            return $"{minutes: 00}:{secs:00}";
        }

        private void InitializeUI()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = AppTheme.BgLight;
            this.Padding = new Padding(0);

            headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = AppTheme.BgDark,
                Padding = new Padding(15, 10, 15, 10)
            };

            lblHeader = new Label
            {
                Text = $"📂 ARCHIVIO {_archiveType.ToUpper()} • 0 elementi",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 12),
                AutoSize = true
            };
            headerPanel.Controls.Add(lblHeader);

            btnImport = new Button
            {
                Text = "📥 Importa",
                Location = new Point(15, 45),
                Size = new Size(110, 30),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnImport.FlatAppearance.BorderSize = 0;
            btnImport.Click += BtnImport_Click;
            headerPanel.Controls.Add(btnImport);

            btnEdit = new Button
            {
                Text = "✏️ Modifica",
                Location = new Point(135, 45),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(255, 140, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnEdit.FlatAppearance.BorderSize = 0;
            btnEdit.Click += BtnEdit_Click;
            headerPanel.Controls.Add(btnEdit);

            btnDelete = new Button
            {
                Text = "🗑️ Elimina",
                Location = new Point(245, 45),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnDelete.FlatAppearance.BorderSize = 0;
            btnDelete.Click += BtnDelete_Click;
            headerPanel.Controls.Add(btnDelete);

            btnRefresh = new Button
            {
                Text = "🔄",
                Location = new Point(355, 45),
                Size = new Size(40, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12),
                Cursor = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (s, e) => RefreshArchive();
            headerPanel.Controls.Add(btnRefresh);

            txtSearch = new TextBox
            {
                PlaceholderText = "🔍 Cerca artista o titolo...",
                Font = new Font("Segoe UI", 10),
                Size = new Size(300, 25)
            };
            txtSearch.TextChanged += (s, e) => ApplyFilters();
            headerPanel.Controls.Add(txtSearch);

            btnZoomOut = new Button
            {
                Text = "➖ A",
                Size = new Size(45, 25),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnZoomOut.FlatAppearance.BorderSize = 0;
            btnZoomOut.Click += (s, e) =>
            {
                if (_currentFontSize > 8)
                {
                    _currentFontSize -= 1;
                    UpdateFontSize();
                }
            };
            headerPanel.Controls.Add(btnZoomOut);

            btnZoomIn = new Button
            {
                Text = "➕ A",
                Size = new Size(45, 25),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnZoomIn.FlatAppearance.BorderSize = 0;
            btnZoomIn.Click += (s, e) =>
            {
                if (_currentFontSize < 16)
                {
                    _currentFontSize += 1;
                    UpdateFontSize();
                }
            };
            headerPanel.Controls.Add(btnZoomIn);

            cmbGenreFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9),
                Size = new Size(130, 40)
            };
            cmbGenreFilter.Items.Add("Tutti i Generi");
            cmbGenreFilter.SelectedIndex = 0;
            cmbGenreFilter.SelectedIndexChanged += (s, e) => ApplyFilters();
            headerPanel.Controls.Add(cmbGenreFilter);

            cmbCategoryFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9),
                Size = new Size(130, 40)
            };
            cmbCategoryFilter.Items.Add("Tutte le Categorie");
            cmbCategoryFilter.SelectedIndex = 0;
            cmbCategoryFilter.SelectedIndexChanged += (s, e) => ApplyFilters();
            headerPanel.Controls.Add(cmbCategoryFilter);

            btnClearFilters = new Button
            {
                Text = "✖ Reset",
                Size = new Size(70, 25),
                BackColor = Color.Gray,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClearFilters.FlatAppearance.BorderSize = 0;
            btnClearFilters.Click += (s, e) =>
            {
                txtSearch.Text = "";
                cmbGenreFilter.SelectedIndex = 0;
                cmbCategoryFilter.SelectedIndex = 0;
            };
            headerPanel.Controls.Add(btnClearFilters);

            headerPanel.Resize += (s, e) => RepositionHeaderControls();
            RepositionHeaderControls();

            this.Controls.Add(headerPanel);

            dgvArchive = new ArchiveDataGridView
            {
                Location = new Point(0, 80),
                Size = new Size(this.Width, this.Height - 80 - 70), // ✅ Spazio per mini player
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackgroundColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                GridColor = Color.FromArgb(60, 60, 60),
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight = 40,
                RowTemplate = { Height = 35 },
                Font = new Font("Segoe UI", _currentFontSize),
                AllowUserToResizeRows = false,
                ScrollBars = ScrollBars.Both,
                AllowDrop = false
            };

            dgvArchive.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(50, 50, 50);
            dgvArchive.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvArchive.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            dgvArchive.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvArchive.ColumnHeadersDefaultCellStyle.Padding = new Padding(5);

            dgvArchive.DefaultCellStyle.BackColor = Color.FromArgb(40, 40, 40);
            dgvArchive.DefaultCellStyle.ForeColor = Color.White;
            dgvArchive.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 215);
            dgvArchive.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvArchive.DefaultCellStyle.Padding = new Padding(5);

            dgvArchive.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(35, 35, 35);
            dgvArchive.EnableHeadersVisualStyles = false;

            dgvArchive.MouseDown += DgvArchive_MouseDown;
            dgvArchive.MouseMove += DgvArchive_MouseMove;

            if (_archiveType == "Music")
            {
                var colVideo = new DataGridViewTextBoxColumn
                {
                    Name = "Video",
                    HeaderText = "🎬",
                    Width = 40,
                    MinimumWidth = 40,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                    DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
                };
                dgvArchive.Columns.Add(colVideo);

                dgvArchive.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "Artist",
                    HeaderText = "Artista",
                    FillWeight = 20,
                    DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft }
                });

                dgvArchive.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "Title",
                    HeaderText = "Titolo",
                    FillWeight = 30,
                    DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft }
                });

                dgvArchive.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "Genre",
                    HeaderText = "Genere",
                    FillWeight = 12,
                    DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
                });

                dgvArchive.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "Year",
                    HeaderText = "Anno",
                    FillWeight = 8,
                    DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
                });

                dgvArchive.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "Duration",
                    HeaderText = "Durata",
                    FillWeight = 8,
                    DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
                });

                dgvArchive.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "Intro",
                    HeaderText = "Intro",
                    FillWeight = 7,
                    DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
                });

                dgvArchive.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "Category",
                    HeaderText = "Categoria",
                    FillWeight = 12,
                    DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
                });

                dgvArchive.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "AddedDate",
                    HeaderText = "Aggiunto",
                    FillWeight = 10,
                    DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
                });
            }
            else
            {
                var colVideo = new DataGridViewTextBoxColumn
                {
                    Name = "Video",
                    HeaderText = "🎬",
                    Width = 40,
                    MinimumWidth = 40,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                    DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
                };
                dgvArchive.Columns.Add(colVideo);

                dgvArchive.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "Title",
                    HeaderText = "Titolo",
                    FillWeight = 35,
                    DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft }
                });

                dgvArchive.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "Genre",
                    HeaderText = "Genere",
                    FillWeight = 15,
                    DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
                });

                dgvArchive.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "Duration",
                    HeaderText = "Durata",
                    FillWeight = 10,
                    DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
                });

                dgvArchive.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "Intro",
                    HeaderText = "Intro",
                    FillWeight = 8,
                    DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
                });

                dgvArchive.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "Category",
                    HeaderText = "Categoria",
                    FillWeight = 15,
                    DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
                });

                dgvArchive.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "AddedDate",
                    HeaderText = "Aggiunto",
                    FillWeight = 12,
                    DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
                });
            }

            ContextMenuStrip contextMenu = new ContextMenuStrip();
            var miCtxPreview = new ToolStripMenuItem("🎧 " + LanguageManager.GetString("Archive.Preview", "Preascolto"), null, MenuPreview_Click) { Tag = "ctx_preview" };
            contextMenu.Items.Add(miCtxPreview);
            contextMenu.Items.Add(new ToolStripSeparator() { Tag = "ctx_sep1" });

            if (_archiveType == "Music")
            {
                var miCtxAssociate = new ToolStripMenuItem("🎬 " + LanguageManager.GetString("Archive.AssociateVideo", "Associa Video..."), null, MenuAssociateVideo_Click) { Tag = "ctx_associate" };
                contextMenu.Items.Add(miCtxAssociate);
            }
            else // Clips
            {
                var miCtxAssociate = new ToolStripMenuItem("🎬 " + LanguageManager.GetString("Archive.AssociateVideo", "Associa Video..."), null, MenuAssociateVideo_Click) { Tag = "ctx_associate" };
                contextMenu.Items.Add(miCtxAssociate);
            }

            var miCtxRemove = new ToolStripMenuItem("🗑️ " + LanguageManager.GetString("Archive.RemoveVideo", "Rimuovi Video"), null, MenuRemoveVideo_Click) { Tag = "ctx_remove" };
            contextMenu.Items.Add(miCtxRemove);
            contextMenu.Items.Add(new ToolStripSeparator() { Tag = "ctx_sep2" });
            var miCtxEdit = new ToolStripMenuItem("✏️ " + LanguageManager.GetString("Archive.EditGenreCategory", "Modifica Genere/Categoria"), null, MenuBatchEdit_Click) { Tag = "ctx_edit" };
            contextMenu.Items.Add(miCtxEdit);
            contextMenu.Items.Add(new ToolStripSeparator() { Tag = "ctx_sep3" });
            var miCtxHistory = new ToolStripMenuItem("📋 " + LanguageManager.GetString("Archive.ShowPlayHistory", "Mostra Storico Passaggi"), null, MenuShowPlayHistory_Click) { Tag = "ctx_history" };
            contextMenu.Items.Add(miCtxHistory);
            contextMenu.Items.Add(new ToolStripSeparator() { Tag = "ctx_sep4" });
            var miCtxShowFolder = new ToolStripMenuItem("📁 " + LanguageManager.GetString("Archive.ShowFileFolder", "Mostra cartella del file"), null, MenuShowFileFolder_Click) { Tag = "ctx_show_folder" };
            contextMenu.Items.Add(miCtxShowFolder);
            var miCtxExport = new ToolStripMenuItem("📤 " + LanguageManager.GetString("Archive.ExportSelectedFiles", "Esporta file selezionati"), null, MenuExportFiles_Click) { Tag = "ctx_export" };
            contextMenu.Items.Add(miCtxExport);
            contextMenu.Opening += ContextMenu_Opening;
            dgvArchive.ContextMenuStrip = contextMenu;

            dgvArchive.CellDoubleClick += DgvArchive_CellDoubleClick;
            dgvArchive.KeyDown += DgvArchive_KeyDown;

            this.Controls.Add(dgvArchive);
        }

        private void InitializeMiniPlayer()
        {
            pnlMiniPlayer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 70,
                BackColor = Color.FromArgb(30, 30, 30),
                Visible = false
            };
            this.Controls.Add(pnlMiniPlayer);
            pnlMiniPlayer.BringToFront();

            lblPreviewTitle = new Label
            {
                Text = LanguageManager.GetString("Archive.NoPreview", "Nessun file in preascolto"),
                Location = new Point(10, 8),
                Size = new Size(400, 20),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White
            };
            pnlMiniPlayer.Controls.Add(lblPreviewTitle);

            btnPlayStop = new Button
            {
                Text = "▶",
                Location = new Point(10, 32),
                Size = new Size(50, 30),
                BackColor = Color.FromArgb(0, 200, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnPlayStop.FlatAppearance.BorderSize = 0;
            btnPlayStop.Click += BtnPlayStop_Click;
            pnlMiniPlayer.Controls.Add(btnPlayStop);

            trackPosition = new TrackBar
            {
                Location = new Point(70, 32),
                Size = new Size(pnlMiniPlayer.Width - 250, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Minimum = 0,
                Maximum = 1000,
                TickFrequency = 50,
                TickStyle = TickStyle.None,
                SmallChange = 1,
                LargeChange = 10
            };

            trackPosition.MouseDown += TrackPosition_MouseDown;
            trackPosition.MouseUp += TrackPosition_MouseUp;
            trackPosition.ValueChanged += TrackPosition_ValueChanged;

            pnlMiniPlayer.Controls.Add(trackPosition);

            lblTimeCounter = new Label
            {
                Text = "00:00 / 00:00",
                Location = new Point(pnlMiniPlayer.Width - 170, 38),
                Size = new Size(150, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleRight
            };
            pnlMiniPlayer.Controls.Add(lblTimeCounter);

            Button btnClose = new Button
            {
                Text = "✖",
                Location = new Point(pnlMiniPlayer.Width - 35, 5),
                Size = new Size(25, 25),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.FromArgb(200, 0, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => StopPreview();
            pnlMiniPlayer.Controls.Add(btnClose);

            _previewTimer = new System.Windows.Forms.Timer { Interval = 100 };
            _previewTimer.Tick += PreviewTimer_Tick;
        }

        private void UpdateColumnHeaders()
        {
            if (dgvArchive == null || dgvArchive.Columns.Count == 0) return;

            if (dgvArchive.Columns.Contains("Video"))
                dgvArchive.Columns["Video"].HeaderText = "🎬";

            if (_archiveType == "Music")
            {
                if (dgvArchive.Columns.Contains("Artist"))
                    dgvArchive.Columns["Artist"].HeaderText = LanguageManager.GetString("Archive.ColumnArtist", "Artista");

                if (dgvArchive.Columns.Contains("Title"))
                    dgvArchive.Columns["Title"].HeaderText = LanguageManager.GetString("Archive.ColumnTitle", "Titolo");

                if (dgvArchive.Columns.Contains("Genre"))
                    dgvArchive.Columns["Genre"].HeaderText = LanguageManager.GetString("Archive.ColumnGenre", "Genere");

                if (dgvArchive.Columns.Contains("Year"))
                    dgvArchive.Columns["Year"].HeaderText = LanguageManager.GetString("Archive.ColumnYear", "Anno");

                if (dgvArchive.Columns.Contains("Duration"))
                    dgvArchive.Columns["Duration"].HeaderText = LanguageManager.GetString("Archive.ColumnDuration", "Durata");

                if (dgvArchive.Columns.Contains("Intro"))
                    dgvArchive.Columns["Intro"].HeaderText = LanguageManager.GetString("Archive.ColumnIntro", "Intro");

                if (dgvArchive.Columns.Contains("Category"))
                    dgvArchive.Columns["Category"].HeaderText = LanguageManager.GetString("Archive.ColumnCategory", "Categoria");

                if (dgvArchive.Columns.Contains("AddedDate"))
                    dgvArchive.Columns["AddedDate"].HeaderText = LanguageManager.GetString("Archive.ColumnAdded", "Aggiunto");
            }
            else
            {
                if (dgvArchive.Columns.Contains("Title"))
                    dgvArchive.Columns["Title"].HeaderText = LanguageManager.GetString("Archive.ColumnTitle", "Titolo");

                if (dgvArchive.Columns.Contains("Genre"))
                    dgvArchive.Columns["Genre"].HeaderText = LanguageManager.GetString("Archive.ColumnGenre", "Genere");

                if (dgvArchive.Columns.Contains("Duration"))
                    dgvArchive.Columns["Duration"].HeaderText = LanguageManager.GetString("Archive.ColumnDuration", "Durata");

                if (dgvArchive.Columns.Contains("Intro"))
                    dgvArchive.Columns["Intro"].HeaderText = LanguageManager.GetString("Archive.ColumnIntro", "Intro");

                if (dgvArchive.Columns.Contains("Category"))
                    dgvArchive.Columns["Category"].HeaderText = LanguageManager.GetString("Archive.ColumnCategory", "Categoria");

                if (dgvArchive.Columns.Contains("AddedDate"))
                    dgvArchive.Columns["AddedDate"].HeaderText = LanguageManager.GetString("Archive.ColumnAdded", "Aggiunto");
            }
        }

        private void RepositionHeaderControls()
        {
            const int MARGIN = 15;
            int panelWidth = headerPanel.Width;

            int x1 = panelWidth - btnZoomIn.Width - MARGIN;
            btnZoomIn.Location = new Point(x1, 14);

            x1 -= (btnZoomOut.Width + 5);
            btnZoomOut.Location = new Point(x1, 14);

            x1 -= (txtSearch.Width + 15);
            txtSearch.Location = new Point(x1, 12);

            int x2 = panelWidth - btnClearFilters.Width - MARGIN;
            btnClearFilters.Location = new Point(x2, 48);

            x2 -= (cmbCategoryFilter.Width + 10);
            cmbCategoryFilter.Location = new Point(x2, 48);

            x2 -= (cmbGenreFilter.Width + 10);
            cmbGenreFilter.Location = new Point(x2, 48);
        }

        private void UpdateFontSize()
        {
            dgvArchive.Font = new Font("Segoe UI", _currentFontSize);
            dgvArchive.RowTemplate.Height = (int)(_currentFontSize * 3.5);
            dgvArchive.Refresh();
        }

        private void UpdateHeaderCount(int count)
        {
            string elementsText = LanguageManager.GetString("Archive.Elements", "elementi");
            lblHeader.Text = $"📂 {LanguageManager.GetString("Archive.Archive", "ARCHIVIO")} {_archiveType.ToUpper()} • {count} {elementsText}";
            lblHeader.ForeColor = count > 0 ? Color.White : Color.Gray;
        }

        private string GetVideoIcon(object entry)
        {
            if (_archiveType == "Music" && entry is MusicEntry musicEntry)
            {
                if (musicEntry.VideoSource == VideoSourceType.StaticVideo && !string.IsNullOrEmpty(musicEntry.VideoFilePath))
                    return "🎬";
                else if (musicEntry.VideoSource == VideoSourceType.BufferVideo)
                    return "🖼️";
                else
                    return "🎵";
            }
            else if (_archiveType == "Clips" && entry is ClipEntry clipEntry)
            {
                if (clipEntry.VideoSource == VideoSourceType.StaticVideo && !string.IsNullOrEmpty(clipEntry.VideoFilePath))
                    return "🎬";
                else if (clipEntry.VideoSource == VideoSourceType.BufferVideo)
                    return "🖼️";
                else
                    return "🎵";
            }

            return "🎵";
        }

        private void DgvArchive_MouseDown(object sender, MouseEventArgs e)
{
    var hitTest = dgvArchive.HitTest(e.X, e.Y);

    // ✅ Tasto destro: seleziona la riga sotto il cursore PRIMA di mostrare il menu
    if (e.Button == MouseButtons.Right)
    {
        if (hitTest.RowIndex >= 0 && hitTest.RowIndex < dgvArchive.Rows.Count)
        {
            // Se la riga cliccata NON è già nella selezione, seleziona solo quella
            if (!dgvArchive.Rows[hitTest.RowIndex].Selected)
            {
                dgvArchive.ClearSelection();
                dgvArchive.Rows[hitTest.RowIndex].Selected = true;
                dgvArchive.CurrentCell = dgvArchive.Rows[hitTest.RowIndex].Cells[0]; // ✅ solo se nuova riga
            }
            // Se è già selezionata (es. multi-selezione), lascia la selezione com'è
            // NON aggiornare CurrentCell per evitare il reset della multi-selezione
        }
        return;
    }

    // ✅ Tasto sinistro: drag & drop (invariato)
    if (e.Button == MouseButtons.Left)
    {
        if (hitTest.RowIndex >= 0 && hitTest.RowIndex < dgvArchive.Rows.Count)
        {
            _dragStartPoint = e.Location;
            _isDragging = false;
        }
    }
}

        private void DgvArchive_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && !_isDragging)
            {
                if (Math.Abs(e.X - _dragStartPoint.X) > SystemInformation.DragSize.Width ||
                    Math.Abs(e.Y - _dragStartPoint.Y) > SystemInformation.DragSize.Height)
                {
                    StartDragOperation();
                }
            }
        }

        private void StartDragOperation()
        {
            if (dgvArchive.SelectedRows.Count == 0)
                return;

            _isDragging = true;

            try
            {
                var selectedItems = new List<object>();

                foreach (DataGridViewRow row in dgvArchive.SelectedRows)
                {
                    if (row.Tag != null)
                    {
                        selectedItems.Add(row.Tag);
                    }
                }

                if (selectedItems.Count == 0)
                {
                    _isDragging = false;
                    return;
                }

                DataObject dragData = new DataObject();

                if (_archiveType == "Music")
                {
                    var musicList = selectedItems.OfType<MusicEntry>().ToList();
                    dragData.SetData("MusicEntryList", musicList);

                    Log($"[ArchiveControl] Drag avviato:  {musicList.Count} brani musicali");
                }
                else
                {
                    var clipsList = selectedItems.OfType<ClipEntry>().ToList();
                    dragData.SetData("ClipEntryList", clipsList);

                    Log($"[ArchiveControl] Drag avviato: {clipsList.Count} clips");
                }

                DragDropEffects result = dgvArchive.DoDragDrop(dragData, DragDropEffects.Copy);

                if (result == DragDropEffects.Copy)
                {
                    Log("[ArchiveControl] ✅ Drag completato con successo");

                    StatusChanged?.Invoke(this,
                        string.Format(LanguageManager.GetString("Archive.ItemsAdded", "{0} elementi aggiunti alla playlist"),
                        selectedItems.Count));
                }
                else
                {
                    Log($"[ArchiveControl] ⚠️ Drag annullato o fallito:  {result}");
                }
            }
            catch (Exception ex)
            {
                Log($"[ArchiveControl] ❌ Errore drag: {ex.Message}");
                MessageBox.Show(
                    $"Errore durante il trascinamento:\n{ex.Message}",
                    "Errore Drag & Drop",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _isDragging = false;
            }
        }

        // ═══════════════════════════════════════════════════════════
        // CONTEXT MENU
        // ═══════════════════════════════════════════════════════════

        private void MenuPreview_Click(object sender, EventArgs e)
        {
            if (dgvArchive.SelectedRows.Count == 0) return;

            DataGridViewRow selectedRow = dgvArchive.SelectedRows[0];
            object entryData = selectedRow.Tag;

            string filePath = "";
            string title = "";

            if (entryData is MusicEntry musicEntry)
            {
                filePath = musicEntry.FilePath;
                title = $"{musicEntry.Artist} - {musicEntry.Title}";
            }
            else if (entryData is ClipEntry clipEntry)
            {
                filePath = clipEntry.FilePath;
                title = clipEntry.Title;
            }

            Log("");
            Log("========================================");
            Log("[ArchiveControl] 🎧 PREVIEW RICHIESTO");
            Log($"[ArchiveControl] File: {filePath}");
            Log($"[ArchiveControl] Titolo: {title}");
            Log($"[ArchiveControl] File esiste: {File.Exists(filePath)}");
            Log($"[ArchiveControl] Player attivo: {_previewPlayer != null}");
            Log($"[ArchiveControl] In riproduzione: {_isPreviewPlaying}");
            Log($"[ArchiveControl] Panel visibile: {pnlMiniPlayer.Visible}");
            Log("========================================");

            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                StartPreviewFromExternal(filePath, title);
            }
            else
            {
                MessageBox.Show(
                    LanguageManager.GetString("Archive.FileNotFound", "File non trovato! "),
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void MenuAssociateVideo_Click(object sender, EventArgs e)
        {
            if (dgvArchive.SelectedRows.Count == 0) return;

            DataGridViewRow row = dgvArchive.SelectedRows[0];

            if (row.Tag is MusicEntry musicEntry)
            {
                using (var dialog = new VideoAssociationDialog(musicEntry, false))
                {
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        musicEntry.VideoSource = dialog.SelectedVideoSource;
                        musicEntry.VideoFilePath = dialog.SelectedVideoPath;
                        musicEntry.NDISourceName = "";

                        if (DbcManager.Update("Music.dbc", musicEntry))
                        {
                            RefreshArchive();
                            StatusChanged?.Invoke(this, "Associazione video aggiornata");

                            MessageBox.Show(
                                "✅ Associazione video salvata con successo!",
                                "Successo",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show(
                                "❌ Errore durante il salvataggio! ",
                                "Errore",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                        }
                    }
                }
            }
            else if (row.Tag is ClipEntry clipEntry)
            {
                using (var dialog = new VideoAssociationDialog(clipEntry, false))
                {
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        clipEntry.VideoSource = dialog.SelectedVideoSource;
                        clipEntry.VideoFilePath = dialog.SelectedVideoPath;
                        clipEntry.NDISourceName = "";

                        if (DbcManager.Update("Clips.dbc", clipEntry))
                        {
                            RefreshArchive();
                            StatusChanged?.Invoke(this, "Associazione video aggiornata");

                            MessageBox.Show(
                                "✅ Associazione video salvata con successo!",
                                "Successo",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show(
                                "❌ Errore durante il salvataggio!",
                                "Errore",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }

        private void MenuRemoveVideo_Click(object sender, EventArgs e)
        {
            if (dgvArchive.SelectedRows.Count == 0) return;

            var result = MessageBox.Show(
                $"Rimuovere l'associazione video da {dgvArchive.SelectedRows.Count} elemento/i?",
                "Conferma Rimozione",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                foreach (DataGridViewRow row in dgvArchive.SelectedRows)
                {
                    if (_archiveType == "Music" && row.Tag is MusicEntry musicEntry)
                    {
                        musicEntry.VideoFilePath = "";
                        musicEntry.NDISourceName = "";
                        musicEntry.VideoSource = VideoSourceType.None;
                        DbcManager.Update("Music.dbc", musicEntry);
                    }
                    else if (_archiveType == "Clips" && row.Tag is ClipEntry clipEntry)
                    {
                        clipEntry.VideoFilePath = "";
                        clipEntry.NDISourceName = "";
                        clipEntry.VideoSource = VideoSourceType.None;
                        DbcManager.Update("Clips.dbc", clipEntry);
                    }
                }

                RefreshArchive();
                StatusChanged?.Invoke(this, "Associazioni video rimosse");
            }
        }

        private async void MenuBatchEdit_Click(object sender, EventArgs e)
        {
            if (dgvArchive.SelectedRows.Count == 0)
            {
                MessageBox.Show(
                    LanguageManager.GetString("Archive.SelectAtLeastOne", "Seleziona almeno un elemento"),
                    LanguageManager.GetString("Common.Warning", "Attenzione"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            using (var batchForm = new BatchEditForm(_archiveType))
            {
                if (batchForm.ShowDialog() == DialogResult.OK)
                {
                    await ApplyBatchEditAsync(batchForm.ModifyGenre, batchForm.NewGenre, batchForm.ModifyCategory, batchForm.NewCategory);
                }
            }
        }

        private void MenuShowPlayHistory_Click(object sender, EventArgs e)
        {
            if (dgvArchive.SelectedRows.Count == 0) return;

            var row = dgvArchive.SelectedRows[0];
            string artist = "", title = "";

            if (_archiveType == "Music" && row.Tag is MusicEntry musicEntry)
            {
                artist = musicEntry.Artist ?? "";
                title = musicEntry.Title ?? "";
            }
            else if (_archiveType == "Clips" && row.Tag is ClipEntry clipEntry)
            {
                artist = "";
                title = clipEntry.Title ?? "";
            }

            if (string.IsNullOrWhiteSpace(artist) && string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("Nessun artista/titolo disponibile per questo brano.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var historyForm = new TrackHistoryForm(artist, title);
            historyForm.ShowDialog(this.FindForm());
        }

        private void ContextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (dgvArchive.SelectedRows.Count == 0)
            {
                e.Cancel = true;
                return;
            }

            bool multiSelection = dgvArchive.SelectedRows.Count > 1;

            foreach (ToolStripItem item in dgvArchive.ContextMenuStrip.Items)
            {
                switch (item.Tag as string)
                {
                    case "ctx_preview":
                    case "ctx_sep1":
                    case "ctx_associate":
                    case "ctx_remove":
                    case "ctx_sep2":
                    case "ctx_sep3":
                    case "ctx_history":
                        item.Visible = !multiSelection;
                        break;
                    default:
                        item.Visible = true;
                        break;
                }
            }
        }

        private void MenuShowFileFolder_Click(object sender, EventArgs e)
        {
            if (dgvArchive.SelectedRows.Count == 0) return;

            DataGridViewRow row = dgvArchive.SelectedRows[0];
            string filePath = "";

            if (row.Tag is MusicEntry musicEntry)
                filePath = musicEntry.FilePath;
            else if (row.Tag is ClipEntry clipEntry)
                filePath = clipEntry.FilePath;

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                MessageBox.Show(
                    LanguageManager.GetString("Archive.FileNotFound", "File non trovato! "),
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{filePath}\"");
        }

        private void MenuExportFiles_Click(object sender, EventArgs e)
        {
            if (dgvArchive.SelectedRows.Count == 0) return;

            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = LanguageManager.GetString("Archive.ExportSelectFolder", "Seleziona la cartella di destinazione");
                if (folderDialog.ShowDialog() != DialogResult.OK) return;

                string destFolder = folderDialog.SelectedPath;
                int copied = 0;
                int errors = 0;

                this.Cursor = Cursors.WaitCursor;
                try
                {
                    foreach (DataGridViewRow row in dgvArchive.SelectedRows)
                    {
                        string filePath = "";
                        if (row.Tag is MusicEntry me)
                            filePath = me.FilePath;
                        else if (row.Tag is ClipEntry ce)
                            filePath = ce.FilePath;

                        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                        {
                            errors++;
                            continue;
                        }

                        try
                        {
                            string fileName = Path.GetFileNameWithoutExtension(filePath);
                            string ext = Path.GetExtension(filePath);
                            string destFile = Path.Combine(destFolder, Path.GetFileName(filePath));
                            int counter = 1;
                            while (File.Exists(destFile))
                            {
                                destFile = Path.Combine(destFolder, $"{fileName} ({counter}){ext}");
                                counter++;
                            }
                            File.Copy(filePath, destFile, false);
                            copied++;
                        }
                        catch (IOException ex)
                        {
                            Log($"[ArchiveControl] Export error for '{filePath}': {ex.Message}");
                            errors++;
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            Log($"[ArchiveControl] Export access denied for '{filePath}': {ex.Message}");
                            errors++;
                        }
                    }
                }
                finally
                {
                    this.Cursor = Cursors.Default;
                }

                MessageBox.Show(
                    string.Format(LanguageManager.GetString("Archive.ExportComplete", "✅ Esportati: {0}\n❌ Errori: {1}"), copied, errors),
                    LanguageManager.GetString("Archive.ExportCompleteTitle", "Esportazione Completata"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private async Task ApplyBatchEditAsync(bool modifyGenre, string newGenre, bool modifyCategory, string newCategory)
        {
            if (!modifyGenre && !modifyCategory)
            {
                MessageBox.Show(
                    LanguageManager.GetString("Archive.NoModificationSelected", "Nessuna modifica selezionata! "),
                    LanguageManager.GetString("Common.Warning", "Attenzione"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // Capture selected rows on UI thread before Task.Run
            var rows = dgvArchive.SelectedRows.Cast<DataGridViewRow>().ToList();
            int total = rows.Count;
            string archiveType = _archiveType;

            SetBatchEditButtonsEnabled(false);
            this.Cursor = Cursors.WaitCursor;
            ShowBatchProgressPanel(total);

            int updated = 0, errors = 0;

            var progress = new Progress<int>(current =>
            {
                UpdateBatchProgressPanel(current, total);
            });

            try
            {
                (updated, errors) = await Task.Run(() =>
                {
                    int updatedCount = 0, errorCount = 0;
                    int current = 0;
                    foreach (var row in rows)
                    {
                        current++;
                        ((IProgress<int>)progress).Report(current);
                        try
                        {
                            if (archiveType == "Music" && row.Tag is MusicEntry musicEntry)
                            {
                                bool changed = false;

                                if (modifyGenre && !string.IsNullOrWhiteSpace(newGenre))
                                {
                                    if (UpdateGenreInFile(musicEntry.FilePath, newGenre))
                                    {
                                        musicEntry.Genre = newGenre;
                                        changed = true;
                                    }
                                }

                                if (modifyCategory && !string.IsNullOrWhiteSpace(newCategory))
                                {
                                    musicEntry.Categories = newCategory;
                                    changed = true;
                                }

                                if (changed && DbcManager.Update("Music.dbc", musicEntry))
                                {
                                    updatedCount++;
                                }
                            }
                            else if (archiveType == "Clips" && row.Tag is ClipEntry clipEntry)
                            {
                                bool changed = false;

                                if (modifyGenre && !string.IsNullOrWhiteSpace(newGenre))
                                {
                                    if (UpdateGenreInFile(clipEntry.FilePath, newGenre))
                                    {
                                        clipEntry.Genre = newGenre;
                                        changed = true;
                                    }
                                }

                                if (modifyCategory && !string.IsNullOrWhiteSpace(newCategory))
                                {
                                    clipEntry.Categories = newCategory;
                                    changed = true;
                                }

                                if (changed && DbcManager.Update("Clips.dbc", clipEntry))
                                {
                                    updatedCount++;
                                }
                            }
                        }
                        catch
                        {
                            errorCount++;
                        }
                    }
                    return (updatedCount, errorCount);
                });
            }
            finally
            {
                HideBatchProgressPanel();
                SetBatchEditButtonsEnabled(true);
                this.Cursor = Cursors.Default;
            }

            RefreshArchive();

            MessageBox.Show(
                string.Format(LanguageManager.GetString("Archive.BatchEditComplete", "✅ Aggiornati:  {0}\n❌ Errori: {1}"), updated, errors),
                LanguageManager.GetString("Archive.BatchEditCompletedTitle", "Modifica Batch Completata"),
                MessageBoxButtons.OK,
                errors > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }

        private void SetBatchEditButtonsEnabled(bool enabled)
        {
            if (btnImport != null) btnImport.Enabled = enabled;
            if (btnEdit != null) btnEdit.Enabled = enabled;
            if (btnDelete != null) btnDelete.Enabled = enabled;
        }

        private void ShowBatchProgressPanel(int total)
        {
            if (_batchProgressPanel == null)
            {
                _batchProgressPanel = new Panel
                {
                    BackColor = Color.FromArgb(50, 50, 50),
                    BorderStyle = BorderStyle.FixedSingle,
                    Padding = new Padding(10)
                };

                var lblTitle = new Label
                {
                    Text = "⏳ " + LanguageManager.GetString("Archive.BatchInProgress", "Modifica batch in corso..."),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                    AutoSize = true,
                    Location = new Point(10, 10)
                };

                _lblBatchProgress = new Label
                {
                    ForeColor = Color.LightGray,
                    Font = new Font("Segoe UI", 9f),
                    AutoSize = true,
                    Location = new Point(10, 38)
                };

                _pbBatch = new ProgressBar
                {
                    Location = new Point(10, 62),
                    Size = new Size(220, 18),
                    Minimum = 0,
                    Style = ProgressBarStyle.Continuous
                };

                _batchProgressPanel.Controls.Add(lblTitle);
                _batchProgressPanel.Controls.Add(_lblBatchProgress);
                _batchProgressPanel.Controls.Add(_pbBatch);
                _batchProgressPanel.Size = new Size(242, 92);

                dgvArchive.Controls.Add(_batchProgressPanel);
            }

            _pbBatch.Maximum = total > 0 ? total : 1;
            _pbBatch.Value = 0;
            _lblBatchProgress.Text = string.Format(
                LanguageManager.GetString("Archive.BatchFileProgress", "File 0 di {0}"), total);

            // Center over the grid
            _batchProgressPanel.Location = new Point(
                (dgvArchive.Width - _batchProgressPanel.Width) / 2,
                (dgvArchive.Height - _batchProgressPanel.Height) / 2);

            _batchProgressPanel.BringToFront();
            _batchProgressPanel.Visible = true;
        }

        private void UpdateBatchProgressPanel(int current, int total)
        {
            if (_batchProgressPanel == null || !_batchProgressPanel.Visible) return;
            if (_pbBatch != null && current <= _pbBatch.Maximum)
                _pbBatch.Value = current;
            if (_lblBatchProgress != null)
                _lblBatchProgress.Text = string.Format(
                    LanguageManager.GetString("Archive.BatchFileProgressCurrent", "File {0} di {1}"), current, total);
        }

        private void HideBatchProgressPanel()
        {
            if (_batchProgressPanel != null)
                _batchProgressPanel.Visible = false;
        }

        private bool UpdateGenreInFile(string filePath, string newGenre)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                var tagFile = TagLib.File.Create(filePath);
                tagFile.Tag.Genres = new[] { newGenre };
                tagFile.Save();
                tagFile.Dispose();

                return true;
            }
            catch
            {
                return false;
            }
        }

        // ═══════════════════════════════════════════════════════════
        // MINI PLAYER - CORRETTO
        // ═══════════════════════════════════════════════════════════

        public void StartPreviewFromExternal(string filePath, string title)
        {
            if (!File.Exists(filePath))
            {
                MessageBox.Show(
                    LanguageManager.GetString("Archive.FileNotFound", "File non trovato!"),
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            StartPreview(filePath, title);
        }

        private void StartPreview(string filePath, string title)
        {
            // ✅ FIX 1: Stoppa SEMPRE preview precedente
            try
            {
                StopPreview();
            }
            catch (Exception ex)
            {
                Log($"[ArchiveControl] ⚠️ Errore stop preview: {ex.Message}");
            }

            int session;
            lock (_previewLock)
            {
                _previewSessionCounter++;
                session = _previewSessionCounter;
            }

            try
            {
                int deviceNumber = ConfigurationControl.GetPreviewOutputDeviceNumber();

                _previewAudioFile = new AudioFileReader(filePath);
                _previewPlayer = new WaveOutEvent();

                if (deviceNumber >= 0)
                    _previewPlayer.DeviceNumber = deviceNumber;

                _previewPlayer.PlaybackStopped += (s, e) =>
                {
                    lock (_previewLock)
                    {
                        if (session != _previewSessionCounter)
                            return;
                    }

                    if (this.IsHandleCreated)
                    {
                        this.BeginInvoke(new Action(() =>
                        {
                            Log("[ArchiveControl] ⏹️ Playback terminato");
                            _isPreviewPlaying = false;
                            btnPlayStop.Text = "▶";
                            btnPlayStop.BackColor = Color.FromArgb(0, 200, 0);
                            _previewTimer.Stop();
                            pnlMiniPlayer.Visible = false;
                            trackPosition.Value = 0;
                            lblTimeCounter.Text = "00:00 / 00:00";
                        }));
                    }
                };

                _previewPlayer.Init(_previewAudioFile);
                _previewPlayer.Play();

                _isPreviewPlaying = true;
                btnPlayStop.Text = "⏸";
                btnPlayStop.BackColor = Color.FromArgb(200, 150, 0);
                lblPreviewTitle.Text = title;

                int totalSeconds = Math.Max(1, (int)_previewAudioFile.TotalTime.TotalSeconds);
                trackPosition.Maximum = totalSeconds;
                trackPosition.Minimum = 0;
                trackPosition.Value = 0;

                // ✅ FIX 2: MOSTRA PLAYER
                pnlMiniPlayer.Visible = true;
                pnlMiniPlayer.BringToFront();

                _previewTimer.Start();

                Log($"[ArchiveControl] ▶️ Preview avviato:  {title}");
            }
            catch (Exception ex)
            {
                Log($"[ArchiveControl] ❌ Errore avvio preview: {ex.Message}");
                MessageBox.Show(
                    string.Format(LanguageManager.GetString("Archive.PreviewError", "Errore preascolto:\n{0}"), ex.Message),
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                StopPreview();
            }
        }

        private void StopPreview()
        {
            try
            {
                Log("[ArchiveControl] 🛑 StopPreview chiamato");

                lock (_previewLock)
                {
                    _previewSessionCounter++;
                }

                _previewTimer?.Stop();

                if (_previewPlayer != null)
                {
                    try
                    {
                        if (_previewPlayer.PlaybackState == PlaybackState.Playing ||
                            _previewPlayer.PlaybackState == PlaybackState.Paused)
                        {
                            _previewPlayer.Stop();
                            Log("[ArchiveControl]   ⏹️ Player fermato");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"[ArchiveControl]   ⚠️ Errore stop player: {ex.Message}");
                    }

                    try
                    {
                        _previewPlayer.Dispose();
                        Log("[ArchiveControl]   ♻️ Player disposed");
                    }
                    catch (Exception ex)
                    {
                        Log($"[ArchiveControl]   ⚠️ Errore dispose player: {ex.Message}");
                    }

                    _previewPlayer = null;
                }

                if (_previewAudioFile != null)
                {
                    try
                    {
                        _previewAudioFile.Dispose();
                        Log("[ArchiveControl]   ♻️ AudioFile disposed");
                    }
                    catch (Exception ex)
                    {
                        Log($"[ArchiveControl]   ⚠️ Errore dispose audiofile: {ex.Message}");
                    }

                    _previewAudioFile = null;
                }

                _isPreviewPlaying = false;
                _userIsDraggingSlider = false;
                btnPlayStop.Text = "▶";
                btnPlayStop.BackColor = Color.FromArgb(0, 200, 0);
                trackPosition.Value = 0;
                lblTimeCounter.Text = "00:00 / 00:00";
                lblPreviewTitle.Text = LanguageManager.GetString("Archive.NoPreview", "Nessun file in preascolto");

                pnlMiniPlayer.Visible = false;

                Log("[ArchiveControl] ✅ StopPreview completato");
            }
            catch (Exception ex)
            {
                Log($"[ArchiveControl] ❌ Errore critico in StopPreview: {ex.Message}");
            }
        }

        private void BtnPlayStop_Click(object sender, EventArgs e)
        {
            if (_previewPlayer == null) return;

            try
            {
                if (_isPreviewPlaying)
                {
                    _previewPlayer.Pause();
                    _isPreviewPlaying = false;
                    btnPlayStop.Text = "▶";
                    btnPlayStop.BackColor = Color.FromArgb(0, 200, 0);
                    _previewTimer.Stop();
                }
                else
                {
                    _previewPlayer.Play();
                    _isPreviewPlaying = true;
                    btnPlayStop.Text = "⏸";
                    btnPlayStop.BackColor = Color.FromArgb(200, 150, 0);
                    _previewTimer.Start();
                }
            }
            catch
            {
                StopPreview();
            }
        }

        private void TrackPosition_MouseDown(object sender, MouseEventArgs e)
        {
            if (_previewAudioFile == null) return;

            _userIsDraggingSlider = true;

            TrackBar track = (TrackBar)sender;
            double percentage = (double)e.X / track.Width;
            int newValue = (int)(percentage * track.Maximum);
            newValue = Math.Max(track.Minimum, Math.Min(track.Maximum, newValue));

            track.Value = newValue;

            double seconds = newValue;
            _previewAudioFile.CurrentTime = TimeSpan.FromSeconds(seconds);
        }

        private void TrackPosition_MouseUp(object sender, MouseEventArgs e)
        {
            if (_previewAudioFile == null) return;

            _userIsDraggingSlider = false;

            TrackBar track = (TrackBar)sender;
            double seconds = track.Value;
            _previewAudioFile.CurrentTime = TimeSpan.FromSeconds(seconds);
        }

        private void TrackPosition_ValueChanged(object sender, EventArgs e)
        {
            if (_previewAudioFile == null) return;

            if (_userIsDraggingSlider)
            {
                TrackBar track = (TrackBar)sender;
                int currentSeconds = track.Value;
                int totalSeconds = (int)_previewAudioFile.TotalTime.TotalSeconds;

                lblTimeCounter.Text = $"{FormatTime(currentSeconds)} / {FormatTime(totalSeconds)}";
            }
        }

        private void PreviewTimer_Tick(object sender, EventArgs e)
        {
            if (_previewAudioFile != null && !_userIsDraggingSlider)
            {
                try
                {
                    double currentSeconds = _previewAudioFile.CurrentTime.TotalSeconds;
                    double totalSeconds = _previewAudioFile.TotalTime.TotalSeconds;

                    int newValue = (int)Math.Round(currentSeconds);

                    if (newValue < trackPosition.Minimum) newValue = trackPosition.Minimum;
                    if (newValue > trackPosition.Maximum) newValue = trackPosition.Maximum;

                    if (Math.Abs(trackPosition.Value - newValue) > 0)
                        trackPosition.Value = newValue;

                    lblTimeCounter.Text = $"{FormatTime((int)currentSeconds)} / {FormatTime((int)totalSeconds)}";
                }
                catch
                {
                    _previewTimer.Stop();
                }
            }
        }

        private string FormatTime(int totalSeconds)
        {
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return $"{minutes:00}:{seconds:00}";
        }

        // ═══════════════════════════════════════════════════════════
        // GRID EVENTS
        // ═══════════════════════════════════════════════════════════

        private void DgvArchive_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            DataGridViewRow selectedRow = dgvArchive.Rows[e.RowIndex];
            object entryData = selectedRow.Tag;

            if (entryData is MusicEntry musicEntry)
                OpenMusicEditor(musicEntry);
            else if (entryData is ClipEntry clipEntry)
                OpenClipEditor(clipEntry);
        }

        private void DgvArchive_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
                BtnDelete_Click(null, null);
        }

        // ═══════════════════════════════════════════════════════════
        // BUTTON ACTIONS
        // ═══════════════════════════════════════════════════════════

        // ─────────────────────────────────────────────────────────────────────────────
        // REPLACE  BtnImport_Click  with this version
        // ─────────────────────────────────────────────────────────────────────────────
        private void BtnImport_Click(object sender, EventArgs e)
        {
            if (LicenseManager.IsDemoMode())
            {
                int currentCount = _archiveType == "Music" ? _allMusicData.Count : _allClipsData.Count;
                int maxAllowed = _archiveType == "Music" ? DemoLimits.MAX_MUSIC_TRACKS : DemoLimits.MAX_CLIPS;

                if (currentCount >= maxAllowed)
                {
                    MessageBox.Show(
                        string.Format(LanguageManager.GetString("Archive.DemoLimitReached",
                            "⚠️ MODALITÀ DEMO\n\nLimite raggiunto: {0}/{1} elementi.\n\nPer importare più file, attiva la licenza completa."),
                            currentCount, maxAllowed),
                        LanguageManager.GetString("Archive.DemoLimitTitle", "Limite Demo Raggiunto"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }
            }

            bool isRadioTVMode = ConfigurationControl.IsRadioTVMode();

            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = $"{LanguageManager.GetString("Archive.SelectFiles", "Seleziona file")} {_archiveType}";

                if (isRadioTVMode)
                    ofd.Filter = "Audio/Video Files|*.mp3;*.wav;*.flac;*.m4a;*.wma;*.mp4;*.avi;*.mkv;*.mov;*.wmv;*.ts;*.mts;*.m2ts;*.webm|Audio Files|*.mp3;*.wav;*.flac;*.m4a;*.wma|Video Files|*.mp4;*.avi;*.mkv;*.mov;*.wmv;*.ts;*.mts;*.m2ts;*.webm|All Files|*.*";
                else
                    ofd.Filter = "Audio Files|*.mp3;*.wav;*.flac;*.m4a;*.wma|All Files|*.*";

                ofd.Multiselect = true;

                if (ofd.ShowDialog() != DialogResult.OK) return;

                string[] selectedFiles = ofd.FileNames;

                // ── Separate audio from video ───────────────────
                var videoExtensions = new HashSet<string>(
                    new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".ts", ".mts", ".m2ts", ".webm" },
                    StringComparer.OrdinalIgnoreCase);

                var audioFiles = selectedFiles
                    .Where(f => !videoExtensions.Contains(Path.GetExtension(f)))
                    .ToArray();

                var videoFiles = selectedFiles
                    .Where(f => videoExtensions.Contains(Path.GetExtension(f)))
                    .ToArray();

                // audio files → open conversion form (handles conversion + direct import for compatible files)
                if (audioFiles.Length > 0)
                    OpenVideoConversionForm(audioFiles);

                // video files → open conversion form (non-modal) – only in RadioTV mode
                if (videoFiles.Length > 0)
                {
                    if (isRadioTVMode)
                        OpenVideoConversionForm(videoFiles);
                    else
                        ImportFiles(videoFiles); // shouldn't happen in Radio mode, but handle gracefully
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // NEW METHOD:  OpenVideoConversionForm
        // ─────────────────────────────────────────────────────────────────────────────
        private void OpenVideoConversionForm(string[] videoFiles)
        {
            var convForm = new VideoConversionForm(videoFiles);

            // When conversion finishes, import the resulting mp4 files into the archive.
            // This callback is raised on the UI thread (see VideoConversionForm).
            convForm.ConversionCompleted += (convertedPaths) =>
            {
                if (convertedPaths != null && convertedPaths.Count > 0)
                    ImportFiles(convertedPaths.ToArray(), convForm);
            };

            // Non-modal: user can keep working in other forms
            convForm.Show(this);
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // REPLACE  ImportFiles  with this version
        // (only change: accepts the already-converted .mp4 paths that come from the
        //  conversion form — all video handling now routes through VideoConversionForm,
        //  so here we can always treat .mp4 as a regular video import)
        // ─────────────────────────────────────────────────────────────────────────────
        private void ImportFiles(string[] filePaths, VideoConversionForm convForm = null)
        {
            if (filePaths.Length == 0) return;

            if (LicenseManager.IsDemoMode())
            {
                int currentCount = _archiveType == "Music" ? _allMusicData.Count : _allClipsData.Count;
                int maxAllowed = _archiveType == "Music" ? DemoLimits.MAX_MUSIC_TRACKS : DemoLimits.MAX_CLIPS;
                int remainingSlots = maxAllowed - currentCount;

                if (remainingSlots <= 0)
                {
                    MessageBox.Show(
                        string.Format(LanguageManager.GetString("Archive.DemoLimitReached",
                            "⚠️ MODALITÀ DEMO\n\nLimite raggiunto: {0}/{1} elementi.\n\nImpossibile importare altri file."),
                            currentCount, maxAllowed),
                        LanguageManager.GetString("Archive.DemoLimitTitle", "Limite Demo Raggiunto"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                if (filePaths.Length > remainingSlots)
                {
                    var res = MessageBox.Show(
                        string.Format(LanguageManager.GetString("Archive.DemoLimitPartial",
                            "⚠️ MODALITÀ DEMO\n\nHai selezionato {0} file, ma puoi importarne solo {1}.\n\nVuoi importare i primi {1} file?"),
                            filePaths.Length, remainingSlots),
                        LanguageManager.GetString("Archive.DemoLimitTitle", "Limite Demo"),
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (res == DialogResult.No) return;
                    Array.Resize(ref filePaths, remainingSlots);
                }
            }

            this.Cursor = Cursors.WaitCursor;
            int imported = 0, errors = 0, skipped = 0;

            var videoExtensions = new HashSet<string>(
                new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".ts", ".mts", ".m2ts", ".webm" },
                StringComparer.OrdinalIgnoreCase);

            try
            {
                foreach (string filePath in filePaths)
                {
                    if (LicenseManager.IsDemoMode())
                    {
                        int currentCount = _archiveType == "Music" ? _allMusicData.Count : _allClipsData.Count;
                        int maxAllowed = _archiveType == "Music" ? DemoLimits.MAX_MUSIC_TRACKS : DemoLimits.MAX_CLIPS;

                        if (currentCount + imported >= maxAllowed)
                        {
                            skipped = filePaths.Length - imported - errors;
                            break;
                        }
                    }

                    try
                    {
                        string originalPath = filePath; // ✅ SALVA PATH ORIGINALE PRIMA DEL RENAME
                        string currentPath = filePath;
                        string extension = Path.GetExtension(currentPath).ToLower();
                        bool isVideo = videoExtensions.Contains(extension);

                        // ✅ FIX: Pre-editing usa originalPath per recuperare i marker
                        var (markerIn, markerOut) = convForm?.IsPreEditingEnabled == true
                            ? convForm.GetPreEditMarkers(originalPath)
                            : (-1, -1);

                        // ✅ DEBUG LOG: verifica valori marker letti
                        if (markerIn >= 0 || markerOut >= 0)
                        {
                            Log($"[ImportFiles] 🎯 Marker pre-editing per {Path.GetFileName(originalPath)}: IN={markerIn}ms OUT={markerOut}ms");
                        }

                        // Rename file on disk if rename mode is active
                        if (convForm != null && convForm.RenameMode != 0)
                        {
                            try
                            {
                                var (renArtist, renTitle) = convForm.GetArtistTitleForFile(currentPath);
                                renArtist = convForm.ApplyRenameCase(renArtist);
                                renTitle = convForm.ApplyRenameCase(renTitle);

                                string newName;
                                if (!string.IsNullOrWhiteSpace(renArtist) && !string.IsNullOrWhiteSpace(renTitle))
                                    newName = $"{renArtist} - {renTitle}{extension}";
                                else if (!string.IsNullOrWhiteSpace(renTitle))
                                    newName = $"{renTitle}{extension}";
                                else
                                    newName = convForm.ApplyRenameCase(Path.GetFileNameWithoutExtension(currentPath)) + extension;

                                string dir = Path.GetDirectoryName(currentPath) ?? "";
                                string newPath = Path.Combine(dir, newName);
                                if (!string.Equals(currentPath, newPath, StringComparison.Ordinal))
                                {
                                    // Case-only change on Windows: use temp file to avoid IOException
                                    if (string.Equals(currentPath, newPath, StringComparison.OrdinalIgnoreCase))
                                    {
                                        string tmpPath = currentPath + ".tmp_rename";
                                        File.Move(currentPath, tmpPath);
                                        File.Move(tmpPath, newPath);
                                    }
                                    else if (!File.Exists(newPath))
                                    {
                                        File.Move(currentPath, newPath);
                                    }
                                    currentPath = newPath; // ✅ currentPath cambia, originalPath NO
                                }
                            }
                            catch (Exception ex)
                            {
                                Log($"[ArchiveControl] ⚠️ Errore rename {currentPath}: {ex.Message}");
                            }
                        }

                        // Get artist/title from conversion form settings (tag source + rename mode)
                        string importArtist = null, importTitle = null;
                        if (convForm != null)
                        {
                            var (a, t) = convForm.GetArtistTitleForFile(currentPath);
                            importArtist = convForm.ApplyRenameCase(a);
                            importTitle = convForm.ApplyRenameCase(t);
                        }

                        if (_archiveType == "Music")
                        {
                            var musicEntry = isVideo
                                ? CreateMusicEntryFromVideo(currentPath)
                                : CreateMusicEntryFromFile(currentPath);

                            // Override artist/title with conversion form settings
                            if (!string.IsNullOrWhiteSpace(importArtist)) musicEntry.Artist = importArtist;
                            if (!string.IsNullOrWhiteSpace(importTitle)) musicEntry.Title = importTitle;

                            // ✅ FIX: Apply pre-editing markers if detected
                            if (markerIn >= 0)
                            {
                                musicEntry.MarkerIN = markerIn;
                                musicEntry.MarkerINTRO = markerIn; // ✅ INTRO parte dall'IN
                            }

                            if (markerOut >= 0)
                            {
                                musicEntry.MarkerOUT = markerOut;
                                musicEntry.MarkerMIX = markerOut; // ✅ MIX parte dall'OUT
                            }

                            // ✅ DEBUG LOG: verifica valori marker applicati
                            Log($"[ImportFiles] 💾 Salvataggio {musicEntry.Artist} - {musicEntry.Title}: " +
                                $"IN={musicEntry.MarkerIN}ms INTRO={musicEntry.MarkerINTRO}ms " +
                                $"MIX={musicEntry.MarkerMIX}ms OUT={musicEntry.MarkerOUT}ms");

                            if (DbcManager.Insert("Music.dbc", musicEntry)) imported++;
                            else errors++;
                        }
                        else
                        {
                            var clipEntry = isVideo
                                ? CreateClipEntryFromVideo(currentPath)
                                : CreateClipEntryFromFile(currentPath);

                            // Override title with conversion form settings (clips have no artist)
                            if (!string.IsNullOrWhiteSpace(importTitle)) clipEntry.Title = importTitle;

                            // ✅ FIX: Apply pre-editing markers if detected
                            if (markerIn >= 0)
                            {
                                clipEntry.MarkerIN = markerIn;
                                clipEntry.MarkerINTRO = markerIn;
                            }

                            if (markerOut >= 0)
                            {
                                clipEntry.MarkerOUT = markerOut;
                                clipEntry.MarkerMIX = markerOut;
                            }

                            // ✅ DEBUG LOG
                            Log($"[ImportFiles] 💾 Salvataggio {clipEntry.Title}: " +
                                $"IN={clipEntry.MarkerIN}ms OUT={clipEntry.MarkerOUT}ms");

                            if (DbcManager.Insert("Clips.dbc", clipEntry)) imported++;
                            else errors++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"[ArchiveControl] ❌ Errore import {filePath}: {ex.Message}");
                        errors++;
                    }
                }

                RefreshArchive();

                string message = string.Format(
                    LanguageManager.GetString("Archive.ImportComplete", "✅ Importati:  {0}\n❌ Errori: {1}"),
                    imported, errors);

                if (skipped > 0)
                    message += string.Format("\n" + LanguageManager.GetString("Archive.Skipped",
                        "⚠️ Saltati: {0} (limite demo raggiunto)"), skipped);

                if (LicenseManager.IsDemoMode())
                {
                    int currentCount = _archiveType == "Music" ? _allMusicData.Count : _allClipsData.Count;
                    int maxAllowed = _archiveType == "Music" ? DemoLimits.MAX_MUSIC_TRACKS : DemoLimits.MAX_CLIPS;
                    message += $"\n\n📊 Totale:  {currentCount}/{maxAllowed}";
                }

                MessageBox.Show(message, "Import Completato", MessageBoxButtons.OK,
                    errors > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private MusicEntry CreateMusicEntryFromFile(string filePath)
        {
            string artist = "Unknown Artist";
            string title = Path.GetFileNameWithoutExtension(filePath);
            int year = DateTime.Now.Year;
            string genre = "Unknown";
            int duration = 0;

            try
            {
                TagLib.File tagFile = TagLib.File.Create(filePath);

                if (!string.IsNullOrEmpty(tagFile.Tag.FirstPerformer))
                    artist = tagFile.Tag.FirstPerformer;

                if (!string.IsNullOrEmpty(tagFile.Tag.Title))
                    title = tagFile.Tag.Title;

                if (tagFile.Tag.Year >= 1900 && tagFile.Tag.Year <= 2200)
                    year = (int)tagFile.Tag.Year;

                if (!string.IsNullOrEmpty(tagFile.Tag.FirstGenre))
                    genre = tagFile.Tag.FirstGenre;

                duration = (int)tagFile.Properties.Duration.TotalMilliseconds;

                tagFile.Dispose();
            }
            catch
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                if (fileName.Contains(" - "))
                {
                    string[] parts = fileName.Split(new[] { " - " }, 2, StringSplitOptions.None);
                    artist = parts[0].Trim();
                    title = parts[1].Trim();
                }

                try
                {
                    using (var reader = new AudioFileReader(filePath))
                        duration = (int)reader.TotalTime.TotalMilliseconds;
                }
                catch { duration = 180000; }
            }

            int mixDuration = ConfigurationControl.GetMixDuration();

            return new MusicEntry
            {
                FilePath = filePath,
                Artist = artist,
                Title = title,
                Album = "",
                Genre = genre,
                Year = year,
                Duration = duration,
                FileSize = 0,
                Format = "",
                Bitrate = 0,
                SampleRate = 0,
                Channels = 0,
                Categories = "",
                MarkerIN = 0,
                MarkerINTRO = 0,
                MarkerMIX = Math.Max(0, duration - mixDuration),
                MarkerOUT = duration,
                ValidMonths = "1;2;3;4;5;6;7;8;9;10;11;12",
                ValidDays = "Monday;Tuesday;Wednesday;Thursday;Friday;Saturday;Sunday",
                ValidHours = "0;1;2;3;4;5;6;7;8;9;10;11;12;13;14;15;16;17;18;19;20;21;22;23",
                ValidFrom = DateTime.Now.ToString("yyyy-MM-dd"),
                ValidTo = DateTime.Now.AddYears(100).ToString("yyyy-MM-dd"),
                AddedDate = DateTime.Now.ToString("yyyy-MM-dd"),
                LastPlayed = "",
                PlayCount = 0,
                VideoFilePath = "",
                NDISourceName = "",
                VideoSource = VideoSourceType.None
            };
        }

        // ✅ NUOVO:  Crea MusicEntry da file video MP4
        private MusicEntry CreateMusicEntryFromVideo(string filePath)
        {
            string artist = "Unknown Artist";
            string title = Path.GetFileNameWithoutExtension(filePath);
            int year = DateTime.Now.Year;
            string genre = "";
            int duration = 180000; // Default 3 minuti in ms

            // ✅ Estrai Artista e Titolo dal nome file "Artista - Titolo.mp4"
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            if (fileName.Contains(" - "))
            {
                string[] parts = fileName.Split(new[] { " - " }, 2, StringSplitOptions.None);
                artist = parts[0].Trim();
                title = parts[1].Trim();
            }

            // ✅ Prova a leggere metadata video
            try
            {
                TagLib.File tagFile = TagLib.File.Create(filePath);

                if (!string.IsNullOrEmpty(tagFile.Tag.FirstPerformer))
                    artist = tagFile.Tag.FirstPerformer;

                if (!string.IsNullOrEmpty(tagFile.Tag.Title))
                    title = tagFile.Tag.Title;

                if (tagFile.Tag.Year >= 1900 && tagFile.Tag.Year <= 2200)
                    year = (int)tagFile.Tag.Year;

                if (!string.IsNullOrEmpty(tagFile.Tag.FirstGenre))
                    genre = tagFile.Tag.FirstGenre;

                duration = (int)tagFile.Properties.Duration.TotalMilliseconds;

                tagFile.Dispose();
            }
            catch (Exception ex)
            {
                Log($"[ArchiveControl] ⚠️ Impossibile leggere metadata video: {ex.Message}");
            }

            int mixDuration = ConfigurationControl.GetMixDuration();

            return new MusicEntry
            {
                FilePath = filePath,
                Artist = artist,
                Title = title,
                Album = "",
                Genre = genre,
                Year = year,
                Duration = duration,
                FileSize = 0,
                Format = "MP4",
                Bitrate = 0,
                SampleRate = 0,
                Channels = 0,
                Categories = "",
                MarkerIN = 0,
                MarkerINTRO = 0,
                MarkerMIX = Math.Max(0, duration - mixDuration),
                MarkerOUT = duration,
                ValidMonths = "1;2;3;4;5;6;7;8;9;10;11;12",
                ValidDays = "Monday;Tuesday;Wednesday;Thursday;Friday;Saturday;Sunday",
                ValidHours = "0;1;2;3;4;5;6;7;8;9;10;11;12;13;14;15;16;17;18;19;20;21;22;23",
                ValidFrom = DateTime.Now.ToString("yyyy-MM-dd"),
                ValidTo = DateTime.Now.AddYears(100).ToString("yyyy-MM-dd"),
                AddedDate = DateTime.Now.ToString("yyyy-MM-dd"),
                LastPlayed = "",
                PlayCount = 0,
                VideoFilePath = filePath, // ✅ Associa video automaticamente
                NDISourceName = "",
                VideoSource = VideoSourceType.StaticVideo // ✅ Imposta come video statico
            };
        }

        private ClipEntry CreateClipEntryFromFile(string filePath)
        {
            string title = Path.GetFileNameWithoutExtension(filePath);
            string genre = "Jingle";
            int duration = 0;

            try
            {
                TagLib.File tagFile = TagLib.File.Create(filePath);

                if (!string.IsNullOrEmpty(tagFile.Tag.Title))
                    title = tagFile.Tag.Title;

                if (!string.IsNullOrEmpty(tagFile.Tag.FirstGenre))
                    genre = tagFile.Tag.FirstGenre;

                duration = (int)tagFile.Properties.Duration.TotalMilliseconds;
                tagFile.Dispose();
            }
            catch
            {
                try
                {
                    using (var reader = new AudioFileReader(filePath))
                        duration = (int)reader.TotalTime.TotalMilliseconds;
                }
                catch { duration = 30000; }
            }

            int mixDuration = ConfigurationControl.GetMixDuration();

            return new ClipEntry
            {
                FilePath = filePath,
                Title = title,
                Genre = genre,
                Duration = duration,
                Categories = "",
                MarkerIN = 0,
                MarkerINTRO = 0,
                MarkerMIX = Math.Max(0, duration - mixDuration),
                MarkerOUT = duration,
                ValidMonths = "1;2;3;4;5;6;7;8;9;10;11;12",
                ValidDays = "Monday;Tuesday;Wednesday;Thursday;Friday;Saturday;Sunday",
                ValidHours = "0;1;2;3;4;5;6;7;8;9;10;11;12;13;14;15;16;17;18;19;20;21;22;23",
                ValidFrom = DateTime.Now.ToString("yyyy-MM-dd"),
                ValidTo = DateTime.Now.AddYears(100).ToString("yyyy-MM-dd"),
                AddedDate = DateTime.Now.ToString("yyyy-MM-dd"),
                LastPlayed = "",
                PlayCount = 0,
                VideoFilePath = "",
                NDISourceName = "",
                VideoSource = VideoSourceType.None
            };
        }

        // ✅ NUOVO: Crea ClipEntry da file video MP4
        private ClipEntry CreateClipEntryFromVideo(string filePath)
        {
            string title = Path.GetFileNameWithoutExtension(filePath);
            string genre = "Video";
            int duration = 30000; // Default 30 secondi in ms

            // ✅ Prova a leggere metadata video
            try
            {
                TagLib.File tagFile = TagLib.File.Create(filePath);

                if (!string.IsNullOrEmpty(tagFile.Tag.Title))
                    title = tagFile.Tag.Title;

                if (!string.IsNullOrEmpty(tagFile.Tag.FirstGenre))
                    genre = tagFile.Tag.FirstGenre;

                duration = (int)tagFile.Properties.Duration.TotalMilliseconds;

                tagFile.Dispose();
            }
            catch (Exception ex)
            {
                Log($"[ArchiveControl] ⚠️ Impossibile leggere metadata video:  {ex.Message}");
            }

            int mixDuration = ConfigurationControl.GetMixDuration();

            return new ClipEntry
            {
                FilePath = filePath,
                Title = title,
                Genre = genre,
                Duration = duration,
                Categories = "",
                MarkerIN = 0,
                MarkerINTRO = 0,
                MarkerMIX = Math.Max(0, duration - mixDuration),
                MarkerOUT = duration,
                ValidMonths = "1;2;3;4;5;6;7;8;9;10;11;12",
                ValidDays = "Monday;Tuesday;Wednesday;Thursday;Friday;Saturday;Sunday",
                ValidHours = "0;1;2;3;4;5;6;7;8;9;10;11;12;13;14;15;16;17;18;19;20;21;22;23",
                ValidFrom = DateTime.Now.ToString("yyyy-MM-dd"),
                ValidTo = DateTime.Now.AddYears(100).ToString("yyyy-MM-dd"),
                AddedDate = DateTime.Now.ToString("yyyy-MM-dd"),
                LastPlayed = "",
                PlayCount = 0,
                VideoFilePath = filePath, // ✅ Associa video automaticamente
                NDISourceName = "",
                VideoSource = VideoSourceType.StaticVideo // ✅ Imposta come video statico
            };
        }

        private void OpenMusicEditor(MusicEntry entry)
        {
            if (entry.ID <= 0)
            {
                MessageBox.Show(
                    LanguageManager.GetString("Archive.InvalidID", "❌ ERRORE: Entry non ha un ID valido! "),
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            using (var editorForm = new MusicEditorForm(entry))
            {
                if (editorForm.ShowDialog() == DialogResult.OK)
                {
                    RefreshArchive();
                    StatusChanged?.Invoke(this, LanguageManager.GetString("Archive.TrackUpdated", "Brano aggiornato"));
                }
            }
        }

        private void OpenClipEditor(ClipEntry entry)
        {
            if (entry.ID <= 0)
            {
                MessageBox.Show(
                    LanguageManager.GetString("Archive.InvalidID", "❌ ERRORE: Entry non ha un ID valido!"),
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            var musicEntry = new MusicEntry
            {
                ID = entry.ID,
                FilePath = entry.FilePath,
                Artist = "Jingle",
                Title = entry.Title,
                Album = "",
                Genre = entry.Genre,
                Categories = entry.Categories,
                Year = DateTime.Now.Year,
                Duration = entry.Duration,
                FileSize = 0,
                Format = "",
                Bitrate = 0,
                SampleRate = 0,
                Channels = 0,
                MarkerIN = entry.MarkerIN,
                MarkerINTRO = entry.MarkerINTRO,
                MarkerMIX = entry.MarkerMIX,
                MarkerOUT = entry.MarkerOUT,
                ValidMonths = entry.ValidMonths,
                ValidDays = entry.ValidDays,
                ValidHours = entry.ValidHours,
                ValidFrom = entry.ValidFrom,
                ValidTo = entry.ValidTo,
                AddedDate = entry.AddedDate,
                LastPlayed = entry.LastPlayed,
                PlayCount = entry.PlayCount,
                VideoFilePath = entry.VideoFilePath,
                NDISourceName = entry.NDISourceName,
                VideoSource = entry.VideoSource
            };

            using (var editorForm = new MusicEditorForm(musicEntry, isClip: true))
            {
                if (editorForm.ShowDialog() == DialogResult.OK)
                {
                    entry.Title = musicEntry.Title;
                    entry.Genre = musicEntry.Genre;
                    entry.Categories = musicEntry.Categories;
                    entry.Duration = musicEntry.Duration;
                    entry.MarkerIN = musicEntry.MarkerIN;
                    entry.MarkerINTRO = musicEntry.MarkerINTRO;
                    entry.MarkerMIX = musicEntry.MarkerMIX;
                    entry.MarkerOUT = musicEntry.MarkerOUT;
                    entry.ValidMonths = musicEntry.ValidMonths;
                    entry.ValidDays = musicEntry.ValidDays;
                    entry.ValidHours = musicEntry.ValidHours;
                    entry.ValidFrom = musicEntry.ValidFrom;
                    entry.ValidTo = musicEntry.ValidTo;
                    entry.VideoFilePath = musicEntry.VideoFilePath;
                    entry.NDISourceName = musicEntry.NDISourceName;
                    entry.VideoSource = musicEntry.VideoSource;

                    DbcManager.Update("Clips.dbc", entry);

                    RefreshArchive();
                    StatusChanged?.Invoke(this, LanguageManager.GetString("Archive.JingleUpdated", "Jingle aggiornato"));
                }
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (dgvArchive.SelectedRows.Count == 0)
            {
                MessageBox.Show(
                    LanguageManager.GetString("Archive.SelectToEdit", "Seleziona almeno un elemento da modificare"),
                    LanguageManager.GetString("Common.Warning", "Attenzione"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (dgvArchive.SelectedRows.Count == 1)
                DgvArchive_CellDoubleClick(null, new DataGridViewCellEventArgs(0, dgvArchive.SelectedRows[0].Index));
            else
                MenuBatchEdit_Click(null, null);
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (dgvArchive.SelectedRows.Count == 0)
            {
                MessageBox.Show(
                    LanguageManager.GetString("Archive.SelectToDelete", "Seleziona almeno un elemento da eliminare"),
                    LanguageManager.GetString("Common.Warning", "Attenzione"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                string.Format(LanguageManager.GetString("Archive.ConfirmDelete", "Eliminare {0} elemento/i selezionato/i?"), dgvArchive.SelectedRows.Count),
                LanguageManager.GetString("Archive.ConfirmDeleteTitle", "Conferma eliminazione"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                int deleted = 0;

                foreach (DataGridViewRow row in dgvArchive.SelectedRows)
                {
                    object entryData = row.Tag;
                    int id = 0;

                    if (entryData is MusicEntry musicEntry)
                        id = musicEntry.ID;
                    else if (entryData is ClipEntry clipEntry)
                        id = clipEntry.ID;

                    if (id > 0)
                    {
                        if (_archiveType == "Music")
                        {
                            if (DbcManager.Delete<MusicEntry>("Music.dbc", id))
                                deleted++;
                        }
                        else
                        {
                            if (DbcManager.Delete<ClipEntry>("Clips.dbc", id))
                                deleted++;
                        }
                    }
                }

                RefreshArchive();
                StatusChanged?.Invoke(this, string.Format(LanguageManager.GetString("Archive.ElementsDeleted", "{0} elementi eliminati"), deleted));
            }
        }

        // ═══════════════════════════════════════════════════════════
        // REFRESH & FILTERS
        // ═══════════════════════════════════════════════════════════

        public void RefreshArchive()
        {
            try
            {
                // ✅ Salva posizione scroll e selezione corrente
                int savedScrollIndex = dgvArchive.FirstDisplayedScrollingRowIndex;
                var savedSelectedIds = new List<int>();

                foreach (DataGridViewRow row in dgvArchive.SelectedRows)
                {
                    if (row.Tag is MusicEntry me)
                        savedSelectedIds.Add(me.ID);
                    else if (row.Tag is ClipEntry ce)
                        savedSelectedIds.Add(ce.ID);
                }

                dgvArchive.Rows.Clear();

                if (_archiveType == "Music")
                {
                    _allMusicData = DbcManager.LoadFromCsv<MusicEntry>("Music.dbc");

                    if (LicenseManager.IsDemoMode() && _allMusicData.Count > 50)
                    {
                        StatusChanged?.Invoke(this, string.Format(LanguageManager.GetString("Archive.DemoShowingMusic", "⚠️ DEMO: Mostrando solo 50/{0} brani (attiva licenza per vedere tutti)"), _allMusicData.Count));
                        _allMusicData = _allMusicData.Take(50).ToList();
                    }

                    LoadGenresAndCategories(_allMusicData);
                }
                else
                {
                    _allClipsData = DbcManager.LoadFromCsv<ClipEntry>("Clips.dbc");

                    if (LicenseManager.IsDemoMode() && _allClipsData.Count > DemoLimits.MAX_CLIPS)
                    {
                        StatusChanged?.Invoke(this, string.Format(LanguageManager.GetString("Archive.DemoShowingClips", "⚠️ DEMO: Mostrando solo {0}/{1} clips (attiva licenza per vedere tutti)"), DemoLimits.MAX_CLIPS, _allClipsData.Count));
                        _allClipsData = _allClipsData.Take(DemoLimits.MAX_CLIPS).ToList();
                    }

                    LoadGenresAndCategoriesClips(_allClipsData);
                }

                ApplyFilters();

                // ✅ Ripristina posizione scroll
                if (savedScrollIndex >= 0 && savedScrollIndex < dgvArchive.Rows.Count)
                {
                    dgvArchive.FirstDisplayedScrollingRowIndex = savedScrollIndex;
                }

                // ✅ Ripristina selezione per ID
                if (savedSelectedIds.Count > 0)
                {
                    dgvArchive.ClearSelection();
                    foreach (DataGridViewRow row in dgvArchive.Rows)
                    {
                        int rowId = 0;
                        if (row.Tag is MusicEntry me)
                            rowId = me.ID;
                        else if (row.Tag is ClipEntry ce)
                            rowId = ce.ID;

                        if (rowId > 0 && savedSelectedIds.Contains(rowId))
                        {
                            row.Selected = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(LanguageManager.GetString("Archive.LoadError", "Errore caricamento {0}: {1}"), _archiveType, ex.Message),
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void LoadGenresAndCategories(List<MusicEntry> data)
        {
            var genres = data.Select(m => m.Genre ?? "").Where(g => !string.IsNullOrEmpty(g)).Distinct().OrderBy(g => g).ToList();
            var categories = data.Select(m => m.Categories ?? "").Where(c => !string.IsNullOrEmpty(c)).Distinct().OrderBy(c => c).ToList();

            string currentGenre = cmbGenreFilter.SelectedItem?.ToString();
            string currentCategory = cmbCategoryFilter.SelectedItem?.ToString();

            cmbGenreFilter.Items.Clear();
            cmbGenreFilter.Items.Add(LanguageManager.GetString("Archive.AllGenres", "Tutti i Generi"));
            foreach (var genre in genres)
                cmbGenreFilter.Items.Add(genre);

            cmbCategoryFilter.Items.Clear();
            cmbCategoryFilter.Items.Add(LanguageManager.GetString("Archive.AllCategories", "Tutte le Categorie"));
            foreach (var category in categories)
                cmbCategoryFilter.Items.Add(category);

            cmbGenreFilter.SelectedItem = currentGenre ?? LanguageManager.GetString("Archive.AllGenres", "Tutti i Generi");
            cmbCategoryFilter.SelectedItem = currentCategory ?? LanguageManager.GetString("Archive.AllCategories", "Tutte le Categorie");

            if (cmbGenreFilter.SelectedIndex < 0) cmbGenreFilter.SelectedIndex = 0;
            if (cmbCategoryFilter.SelectedIndex < 0) cmbCategoryFilter.SelectedIndex = 0;
        }

        private void LoadGenresAndCategoriesClips(List<ClipEntry> data)
        {
            var genres = data.Select(c => c.Genre ?? "").Where(g => !string.IsNullOrEmpty(g)).Distinct().OrderBy(g => g).ToList();
            var categories = data.Select(c => c.Categories ?? "").Where(c => !string.IsNullOrEmpty(c)).Distinct().OrderBy(c => c).ToList();

            cmbGenreFilter.Items.Clear();
            cmbGenreFilter.Items.Add(LanguageManager.GetString("Archive.AllGenres", "Tutti i Generi"));
            foreach (var genre in genres)
                cmbGenreFilter.Items.Add(genre);

            cmbCategoryFilter.Items.Clear();
            cmbCategoryFilter.Items.Add(LanguageManager.GetString("Archive.AllCategories", "Tutte le Categorie"));
            foreach (var category in categories)
                cmbCategoryFilter.Items.Add(category);

            cmbGenreFilter.SelectedIndex = 0;
            cmbCategoryFilter.SelectedIndex = 0;
        }

        private void ApplyFilters()
        {
            dgvArchive.Rows.Clear();

            string searchText = txtSearch.Text.ToLower();
            string selectedGenre = cmbGenreFilter.SelectedItem?.ToString() ?? LanguageManager.GetString("Archive.AllGenres", "Tutti i Generi");
            string selectedCategory = cmbCategoryFilter.SelectedItem?.ToString() ?? LanguageManager.GetString("Archive.AllCategories", "Tutte le Categorie");

            string allGenresText = LanguageManager.GetString("Archive.AllGenres", "Tutti i Generi");
            string allCategoriesText = LanguageManager.GetString("Archive.AllCategories", "Tutte le Categorie");

            if (_archiveType == "Music")
            {
                var filtered = _allMusicData.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(searchText))
                    filtered = filtered.Where(m => (m.Artist ?? "").ToLower().Contains(searchText) || (m.Title ?? "").ToLower().Contains(searchText));

                if (selectedGenre != allGenresText)
                    filtered = filtered.Where(m => m.Genre == selectedGenre);

                if (selectedCategory != allCategoriesText)
                    filtered = filtered.Where(m => m.Categories == selectedCategory);

                foreach (var entry in filtered)
                {
                    int displayDurationMs = entry.MarkerMIX > entry.MarkerIN
                        ? entry.MarkerMIX - entry.MarkerIN
                        : entry.Duration - entry.MarkerIN;
                    int introMs = Math.Max(0, entry.MarkerINTRO - entry.MarkerIN);
                    int rowIndex = dgvArchive.Rows.Add(
                        GetVideoIcon(entry),
                        entry.Artist ?? "",
                        entry.Title ?? "",
                        entry.Genre ?? "",
                        entry.Year.ToString(),
                        FormatDurationMs(displayDurationMs),
                        $"{introMs / 1000}s",
                        entry.Categories ?? "",
                        entry.AddedDate ?? ""
                    );

                    dgvArchive.Rows[rowIndex].Tag = entry;
                }

                UpdateHeaderCount(filtered.Count());
                StatusChanged?.Invoke(this, $"Music: {filtered.Count()} / {_allMusicData.Count} {LanguageManager.GetString("Archive.Elements", "elementi")}");
            }
            else
            {
                var filtered = _allClipsData.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(searchText))
                    filtered = filtered.Where(c => (c.Title ?? "").ToLower().Contains(searchText));

                if (selectedGenre != allGenresText)
                    filtered = filtered.Where(c => c.Genre == selectedGenre);

                if (selectedCategory != allCategoriesText)
                    filtered = filtered.Where(c => c.Categories == selectedCategory);

                foreach (var entry in filtered)
                {
                    int displayDurationMs = entry.MarkerMIX > entry.MarkerIN
                        ? entry.MarkerMIX - entry.MarkerIN
                        : entry.Duration - entry.MarkerIN;
                    int introMs = Math.Max(0, entry.MarkerINTRO - entry.MarkerIN);
                    int rowIndex = dgvArchive.Rows.Add(
                        GetVideoIcon(entry),
                        entry.Title ?? "",
                        entry.Genre ?? "",
                        FormatDurationMs(displayDurationMs),
                        $"{introMs / 1000}s",
                        entry.Categories ?? "",
                        entry.AddedDate ?? ""
                    );

                    dgvArchive.Rows[rowIndex].Tag = entry;
                }

                UpdateHeaderCount(filtered.Count());
                StatusChanged?.Invoke(this, $"Clips: {filtered.Count()} / {_allClipsData.Count} {LanguageManager.GetString("Archive.Elements", "elementi")}");
            }
        }

        private void Log(string m) { _dailyLogger?.Log(m); }
        private void LogErr(string m, Exception ex) { _dailyLogger?.LogErr(m, ex); }
        private void LogErr(string m) { _dailyLogger?.LogErr(m); }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                LanguageManager.SaveMissingKeysToFile();
                LanguageManager.LanguageChanged -= OnLanguageChanged;
                StopPreview();
                try { _dailyLogger?.Dispose(); } catch { }
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// DataGridView subclass that preserves multi-row selection on right-click.
        /// Without this, the DataGridView internal OnMouseDown processing resets
        /// the selection even when the right-clicked row is already selected.
        /// </summary>
        private class ArchiveDataGridView : DataGridView
        {
            protected override void OnMouseDown(MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Right)
                {
                    var hitTest = HitTest(e.X, e.Y);
                    if (hitTest.RowIndex >= 0 && hitTest.RowIndex < Rows.Count
                        && Rows[hitTest.RowIndex].Selected)
                    {
                        // Right-clicked on an already-selected row: skip base processing
                        // to prevent DataGridView from resetting the multi-selection.
                        // The context menu still opens via WM_CONTEXTMENU on mouse-up.
                        return;
                    }
                }

                base.OnMouseDown(e);
            }
        }
    }
}
