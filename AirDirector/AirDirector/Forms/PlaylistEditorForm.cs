using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using AirDirector.Controls;
using AirDirector.Models;
using AirDirector.Services.Database;
using AirDirector.Services.Localization;
using AirDirector.Themes;

namespace AirDirector.Forms
{
    public partial class PlaylistEditorForm : Form
    {
        // ── State ────────────────────────────────────────────────────────────
        private AirPlaylist _playlist;
        private string _currentFilePath;
        private bool _hasUnsavedChanges;

        // ── Archive data ─────────────────────────────────────────────────────
        private List<MusicEntry> _allMusicEntries;
        private List<ClipEntry> _allClipEntries;
        private List<string> _musicCategories;
        private List<string> _musicGenres;
        private List<string> _clipCategories;
        private List<string> _clipGenres;

        // ── Left panel controls ──────────────────────────────────────────────
        private Label _lblHeader;
        private TextBox _txtPlaylistName;
        private MaskedTextBox _txtStartTime;
        private Button _btnSave;
        private Button _btnNew;
        private Button _btnClear;
        private DataGridView _dgvEditor;
        private Button _btnMoveUp;
        private Button _btnMoveDown;
        private Button _btnRemove;
        private Label _lblTotalDuration;

        // ── Right panel controls ─────────────────────────────────────────────
        private TabControl _tabArchive;

        // Music tab
        private TextBox _txtMusicSearch;
        private ComboBox _cmbMusicCategory;
        private ComboBox _cmbMusicGenre;
        private DataGridView _dgvMusic;
        private Button _btnAddMusic;

        // Clips tab
        private TextBox _txtClipSearch;
        private ComboBox _cmbClipCategory;
        private ComboBox _cmbClipGenre;
        private DataGridView _dgvClips;
        private Button _btnAddClip;

        // Playlists tab
        private DataGridView _dgvPlaylists;
        private Button _btnDeletePlaylist;

        // Rules tab
        private RadioButton _radRuleCategory;
        private RadioButton _radRuleGenre;
        private RadioButton _radRuleCategoryGenre;
        private ComboBox _cmbRuleCategory;
        private ComboBox _cmbRuleGenre;
        private CheckBox _chkRuleYearFilter;
        private NumericUpDown _numRuleYearFrom;
        private NumericUpDown _numRuleYearTo;
        private Label _lblRuleFoundTracks;
        private Button _btnAddRule;

        // ── Drag & Drop ──────────────────────────────────────────────────────
        private int _dragSourceRow = -1;
        private int _dragTargetRow = -1;
        private Point _archiveDragStart;
        private const int ArchiveDragThreshold = 5;

        // ── Clipboard ────────────────────────────────────────────────────────
        private List<AirPlaylistItem> _clipboardItems = new List<AirPlaylistItem>();
        private bool _isCutOperation = false;

        // ────────────────────────────────────────────────────────────────────
        public PlaylistEditorForm()
        {
            _playlist = new AirPlaylist();
            _currentFilePath = null;
            _hasUnsavedChanges = false;

            _allMusicEntries = new List<MusicEntry>();
            _allClipEntries = new List<ClipEntry>();
            _musicCategories = new List<string>();
            _musicGenres = new List<string>();
            _clipCategories = new List<string>();
            _clipGenres = new List<string>();

            LoadAvailableData();
            InitializeComponent();
            ApplyLanguage();
            RefreshEditorGrid();

            LanguageManager.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            ApplyLanguage();
        }

        // ══════════════════════════════════════════════════════════════════
        // DATA LOADING
        // ══════════════════════════════════════════════════════════════════

        private void LoadAvailableData()
        {
            try
            {
                _allMusicEntries = DbcManager.LoadFromCsv<MusicEntry>("Music.dbc");
                var musicCats = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var musicGenres = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var m in _allMusicEntries)
                {
                    if (!string.IsNullOrEmpty(m.Categories))
                        foreach (var c in m.Categories.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
                        { string t = c.Trim(); if (!string.IsNullOrEmpty(t)) musicCats.Add(t); }
                    if (!string.IsNullOrEmpty(m.Genre))
                        musicGenres.Add(m.Genre.Trim());
                }
                _musicCategories = musicCats.OrderBy(c => c).ToList();
                _musicGenres = musicGenres.OrderBy(g => g).ToList();
            }
            catch { _allMusicEntries = new List<MusicEntry>(); }

            try
            {
                _allClipEntries = DbcManager.LoadFromCsv<ClipEntry>("Clips.dbc");
                var clipCats = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var clipGenres = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var c in _allClipEntries)
                {
                    if (!string.IsNullOrEmpty(c.Categories))
                        foreach (var cat in c.Categories.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
                        { string t = cat.Trim(); if (!string.IsNullOrEmpty(t)) clipCats.Add(t); }
                    if (!string.IsNullOrEmpty(c.Genre))
                        clipGenres.Add(c.Genre.Trim());
                }
                _clipCategories = clipCats.OrderBy(c => c).ToList();
                _clipGenres = clipGenres.OrderBy(g => g).ToList();
            }
            catch { _allClipEntries = new List<ClipEntry>(); }
        }

        // ══════════════════════════════════════════════════════════════════
        // INITIALIZE UI
        // ══════════════════════════════════════════════════════════════════

        private void InitializeComponent()
        {
            this.Text = LanguageManager.GetString("PlaylistEditor.Title", "PLAYLIST EDITOR");
            this.Size = new Size(1400, 850);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.BackColor = Color.FromArgb(25, 25, 25);
            this.FormClosing += PlaylistEditorForm_FormClosing;

            // ── Header ────────────────────────────────────────────────────
            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Color.FromArgb(33, 150, 243)
            };
            _lblHeader = new Label
            {
                Text = "🎶 " + LanguageManager.GetString("PlaylistEditor.Title", "PLAYLIST EDITOR"),
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(15, 0, 0, 0)
            };
            headerPanel.Controls.Add(_lblHeader);
            // ── Main SplitContainer ────────────────────────────────────────
            SplitContainer mainSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                BackColor = Color.FromArgb(25, 25, 25),
                FixedPanel = FixedPanel.None
            };

            BuildLeftPanel(mainSplit.Panel1);
            BuildRightPanel(mainSplit.Panel2);

            this.Controls.Add(mainSplit);
            this.Controls.Add(headerPanel);

            this.Load += (s, e) =>
            {
                try
                {
                    mainSplit.Panel1MinSize = 400;
                    mainSplit.Panel2MinSize = 300;
                    mainSplit.SplitterDistance = (int)(mainSplit.Width * 0.55);
                }
                catch (InvalidOperationException) { }
            };
        }

        // ── LEFT PANEL ────────────────────────────────────────────────────

        private void BuildLeftPanel(Panel parent)
        {
            parent.BackColor = Color.FromArgb(30, 30, 30);
            parent.Padding = new Padding(10);

            // Name row + start time
            Panel rowName = new Panel { Height = 36, Dock = DockStyle.Top, BackColor = Color.Transparent };

            Label lblName = new Label
            {
                Text = LanguageManager.GetString("PlaylistEditor.PlaylistName", "Nome Playlist:"),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9),
                Width = 110,
                Height = 28,
                Left = 0,
                Top = 4,
                TextAlign = ContentAlignment.MiddleRight
            };

            _txtPlaylistName = new TextBox
            {
                Left = 116,
                Top = 5,
                Width = 280,
                Height = 28,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            _txtPlaylistName.TextChanged += (s, e) => MarkChanged();

            Label lblStart = new Label
            {
                Text = LanguageManager.GetString("PlaylistEditor.StartTime", "Orario Inizio:"),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9),
                Width = 100,
                Height = 28,
                Left = 410,
                Top = 4,
                TextAlign = ContentAlignment.MiddleRight
            };

            _txtStartTime = new MaskedTextBox
            {
                Mask = "00:00:00",
                Left = 516,
                Top = 5,
                Width = 80,
                Height = 28,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.Yellow,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BorderStyle = BorderStyle.FixedSingle
            };
            _txtStartTime.Text = "000000"; // Set before hooking TextChanged to avoid triggering unnecessary refresh during init
            _txtStartTime.TextChanged += TxtStartTime_TextChanged;

            rowName.Controls.AddRange(new Control[] { lblName, _txtPlaylistName, lblStart, _txtStartTime });

            // Toolbar
            Panel toolbar = new Panel { Height = 40, Dock = DockStyle.Top, BackColor = Color.Transparent };
            toolbar.Padding = new Padding(0, 4, 0, 0);

            _btnSave = CreateButton(LanguageManager.GetString("PlaylistEditor.Save", "💾 Salva"), Color.FromArgb(33, 150, 83));
            _btnSave.Left = 0; _btnSave.Top = 4; _btnSave.Click += BtnSave_Click;

            _btnNew = CreateButton(LanguageManager.GetString("PlaylistEditor.New", "📂 Nuova"), Color.FromArgb(33, 100, 183));
            _btnNew.Left = _btnSave.Right + 6; _btnNew.Top = 4; _btnNew.Click += BtnNew_Click;

            _btnClear = CreateButton(LanguageManager.GetString("PlaylistEditor.ClearEditor", "🗑️ Svuota"), Color.FromArgb(150, 60, 60));
            _btnClear.Left = _btnNew.Right + 6; _btnClear.Top = 4; _btnClear.Click += BtnClear_Click;

            toolbar.Controls.AddRange(new Control[] { _btnSave, _btnNew, _btnClear });

            // DataGridView
            _dgvEditor = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.FromArgb(35, 35, 35),
                GridColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                MultiSelect = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9),
                ColumnHeadersHeight = 30,
                RowTemplate = { Height = 26 },
                AllowDrop = true
            };
            _dgvEditor.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(50, 50, 50);
            _dgvEditor.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            _dgvEditor.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            _dgvEditor.DefaultCellStyle.BackColor = Color.FromArgb(35, 35, 35);
            _dgvEditor.DefaultCellStyle.ForeColor = Color.White;
            _dgvEditor.DefaultCellStyle.SelectionBackColor = Color.FromArgb(33, 150, 243);
            _dgvEditor.DefaultCellStyle.SelectionForeColor = Color.White;
            _dgvEditor.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(42, 42, 42);

            // Columns
            _dgvEditor.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNum", HeaderText = "#", Width = 36, ReadOnly = true });
            var colTime = new DataGridViewTextBoxColumn { Name = "colTime", HeaderText = LanguageManager.GetString("PlaylistEditor.ColumnTime", "Orario"), Width = 76, ReadOnly = true };
            colTime.DefaultCellStyle.ForeColor = Color.Yellow;
            colTime.DefaultCellStyle.BackColor = Color.FromArgb(35, 35, 35);
            _dgvEditor.Columns.Add(colTime);
            _dgvEditor.Columns.Add(new DataGridViewTextBoxColumn { Name = "colType", HeaderText = LanguageManager.GetString("PlaylistEditor.ColumnType", "Tipo"), Width = 42, ReadOnly = true });
            _dgvEditor.Columns.Add(new DataGridViewTextBoxColumn { Name = "colElement", HeaderText = LanguageManager.GetString("PlaylistEditor.ColumnElement", "Elemento"), AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true });
            _dgvEditor.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDuration", HeaderText = LanguageManager.GetString("PlaylistEditor.ColumnDuration", "Durata"), Width = 68, ReadOnly = true });

            // Drag & Drop events
            _dgvEditor.MouseDown += DgvEditor_MouseDown;
            _dgvEditor.MouseMove += DgvEditor_MouseMove;
            _dgvEditor.DragEnter += DgvEditor_DragEnter;
            _dgvEditor.DragDrop += DgvEditor_DragDrop;
            _dgvEditor.DragOver += DgvEditor_DragOver;

            // Context menu
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add(LanguageManager.GetString("PlaylistEditor.Copy", "📋 Copia"), null, (s, e) => CopySelectedItems());
            contextMenu.Items.Add(LanguageManager.GetString("PlaylistEditor.Cut", "✂️ Taglia"), null, (s, e) => CutSelectedItems());
            contextMenu.Items.Add(LanguageManager.GetString("PlaylistEditor.Paste", "📌 Incolla"), null, (s, e) => PasteItems());
            _dgvEditor.ContextMenuStrip = contextMenu;

            // Keyboard shortcuts
            _dgvEditor.KeyDown += (s, e) =>
            {
                if (e.Control && e.KeyCode == Keys.C) { CopySelectedItems(); e.Handled = true; }
                else if (e.Control && e.KeyCode == Keys.X) { CutSelectedItems(); e.Handled = true; }
                else if (e.Control && e.KeyCode == Keys.V) { PasteItems(); e.Handled = true; }
            };

            // Bottom toolbar
            Panel bottomBar = new Panel { Height = 36, Dock = DockStyle.Bottom, BackColor = Color.Transparent };

            _btnMoveUp = CreateButton(LanguageManager.GetString("PlaylistEditor.MoveUp", "⬆ Su"), Color.FromArgb(70, 70, 70));
            _btnMoveUp.Left = 0; _btnMoveUp.Top = 3; _btnMoveUp.Width = 80; _btnMoveUp.Click += BtnMoveUp_Click;

            _btnMoveDown = CreateButton(LanguageManager.GetString("PlaylistEditor.MoveDown", "⬇ Giù"), Color.FromArgb(70, 70, 70));
            _btnMoveDown.Left = 86; _btnMoveDown.Top = 3; _btnMoveDown.Width = 80; _btnMoveDown.Click += BtnMoveDown_Click;

            _btnRemove = CreateButton(LanguageManager.GetString("PlaylistEditor.RemoveSelected", "❌ Rimuovi"), Color.FromArgb(150, 60, 60));
            _btnRemove.Left = 172; _btnRemove.Top = 3; _btnRemove.Width = 110; _btnRemove.Click += BtnRemove_Click;

            _lblTotalDuration = new Label
            {
                Text = string.Format(LanguageManager.GetString("PlaylistEditor.TotalDuration", "⏱️ Durata totale: {0}"), "00:00:00"),
                ForeColor = Color.FromArgb(0, 200, 130),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                AutoSize = true,
                Top = 8
            };

            bottomBar.Controls.AddRange(new Control[] { _btnMoveUp, _btnMoveDown, _btnRemove, _lblTotalDuration });
            bottomBar.Controls[3].Left = 292;

            // Add in correct WinForms docking order: Fill first (processed last), then Bottom, then Top
            parent.Controls.Add(_dgvEditor);   // Dock=Fill — added first = processed last = fills remaining space
            parent.Controls.Add(bottomBar);    // Dock=Bottom
            parent.Controls.Add(toolbar);      // Dock=Top
            parent.Controls.Add(rowName);      // Dock=Top
        }

        // ── RIGHT PANEL ───────────────────────────────────────────────────

        private void BuildRightPanel(Panel parent)
        {
            parent.BackColor = Color.FromArgb(28, 28, 28);
            parent.Padding = new Padding(6);

            _tabArchive = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(28, 28, 28)
            };

            _tabArchive.TabPages.Add(BuildMusicTab());
            _tabArchive.TabPages.Add(BuildClipsTab());
            _tabArchive.TabPages.Add(BuildPlaylistsTab());
            _tabArchive.TabPages.Add(BuildRulesTab());

            parent.Controls.Add(_tabArchive);
        }

        private TabPage BuildMusicTab()
        {
            TabPage page = new TabPage(LanguageManager.GetString("PlaylistEditor.TabMusic", "🎵 Music"))
            { BackColor = Color.FromArgb(28, 28, 28), Padding = new Padding(6) };

            // Search bar
            _txtMusicSearch = CreateSearchBox(LanguageManager.GetString("PlaylistEditor.SearchPlaceholder", "Cerca artista o titolo..."));
            _txtMusicSearch.Dock = DockStyle.Top;
            _txtMusicSearch.TextChanged += (s, e) => ApplyMusicFilter();

            // Filter row
            Panel filterRow = new Panel { Height = 30, Dock = DockStyle.Top, BackColor = Color.Transparent };

            Label lblCat = new Label { Text = LanguageManager.GetString("PlaylistEditor.FilterCategory", "Categoria:"), ForeColor = Color.White, Width = 70, Left = 0, Top = 5, Height = 22, TextAlign = ContentAlignment.MiddleLeft };
            _cmbMusicCategory = new ComboBox { Left = 72, Top = 3, Width = 140, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(45, 45, 45), ForeColor = Color.White };
            _cmbMusicCategory.Items.Add(LanguageManager.GetString("PlaylistEditor.AllCategories", "Tutte"));
            foreach (var c in _musicCategories) _cmbMusicCategory.Items.Add(c);
            _cmbMusicCategory.SelectedIndex = 0;
            _cmbMusicCategory.SelectedIndexChanged += (s, e) => ApplyMusicFilter();

            Label lblGen = new Label { Text = LanguageManager.GetString("PlaylistEditor.FilterGenre", "Genere:"), ForeColor = Color.White, Width = 55, Left = 218, Top = 5, Height = 22, TextAlign = ContentAlignment.MiddleLeft };
            _cmbMusicGenre = new ComboBox { Left = 275, Top = 3, Width = 130, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(45, 45, 45), ForeColor = Color.White };
            _cmbMusicGenre.Items.Add(LanguageManager.GetString("PlaylistEditor.AllGenres", "Tutti"));
            foreach (var g in _musicGenres) _cmbMusicGenre.Items.Add(g);
            _cmbMusicGenre.SelectedIndex = 0;
            _cmbMusicGenre.SelectedIndexChanged += (s, e) => ApplyMusicFilter();

            filterRow.Controls.AddRange(new Control[] { lblCat, _cmbMusicCategory, lblGen, _cmbMusicGenre });

            // DataGridView
            _dgvMusic = CreateArchiveDgv();
            _dgvMusic.Columns.Add(new DataGridViewTextBoxColumn { Name = "Artist", HeaderText = "Artista", Width = 180 });
            _dgvMusic.Columns.Add(new DataGridViewTextBoxColumn { Name = "Title", HeaderText = "Titolo", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            _dgvMusic.Columns.Add(new DataGridViewTextBoxColumn { Name = "Genre", HeaderText = "Genere", Width = 80 });
            _dgvMusic.Columns.Add(new DataGridViewTextBoxColumn { Name = "Duration", HeaderText = "Durata", Width = 60 });
            _dgvMusic.Dock = DockStyle.Fill;
            _dgvMusic.DoubleClick += (s, e) => AddSelectedMusicToPlaylist();
            _dgvMusic.MouseDown += DgvMusic_MouseDown;
            _dgvMusic.MouseMove += DgvMusic_MouseMove;

            // Add button
            _btnAddMusic = CreateButton(LanguageManager.GetString("PlaylistEditor.AddToPlaylist", "➤ Aggiungi"), Color.FromArgb(33, 150, 83));
            _btnAddMusic.Dock = DockStyle.Bottom;
            _btnAddMusic.Height = 30;
            _btnAddMusic.Click += (s, e) => AddSelectedMusicToPlaylist();

            // Add in correct WinForms docking order: Fill first (processed last), then Bottom, then Top
            page.Controls.Add(_dgvMusic);          // Dock=Fill
            page.Controls.Add(_btnAddMusic);       // Dock=Bottom
            page.Controls.Add(_txtMusicSearch);    // Dock=Top
            page.Controls.Add(filterRow);          // Dock=Top

            ApplyMusicFilter();
            return page;
        }

        private TabPage BuildClipsTab()
        {
            TabPage page = new TabPage(LanguageManager.GetString("PlaylistEditor.TabClips", "🔔 Clips"))
            { BackColor = Color.FromArgb(28, 28, 28), Padding = new Padding(6) };

            _txtClipSearch = CreateSearchBox(LanguageManager.GetString("PlaylistEditor.SearchPlaceholder", "Cerca artista o titolo..."));
            _txtClipSearch.Dock = DockStyle.Top;
            _txtClipSearch.TextChanged += (s, e) => ApplyClipFilter();

            Panel filterRow = new Panel { Height = 30, Dock = DockStyle.Top, BackColor = Color.Transparent };
            Label lblCat = new Label { Text = LanguageManager.GetString("PlaylistEditor.FilterCategory", "Categoria:"), ForeColor = Color.White, Width = 70, Left = 0, Top = 5, Height = 22, TextAlign = ContentAlignment.MiddleLeft };
            _cmbClipCategory = new ComboBox { Left = 72, Top = 3, Width = 140, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(45, 45, 45), ForeColor = Color.White };
            _cmbClipCategory.Items.Add(LanguageManager.GetString("PlaylistEditor.AllCategories", "Tutte"));
            foreach (var c in _clipCategories) _cmbClipCategory.Items.Add(c);
            _cmbClipCategory.SelectedIndex = 0;
            _cmbClipCategory.SelectedIndexChanged += (s, e) => ApplyClipFilter();

            Label lblGen = new Label { Text = LanguageManager.GetString("PlaylistEditor.FilterGenre", "Genere:"), ForeColor = Color.White, Width = 55, Left = 218, Top = 5, Height = 22, TextAlign = ContentAlignment.MiddleLeft };
            _cmbClipGenre = new ComboBox { Left = 275, Top = 3, Width = 130, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(45, 45, 45), ForeColor = Color.White };
            _cmbClipGenre.Items.Add(LanguageManager.GetString("PlaylistEditor.AllGenres", "Tutti"));
            foreach (var g in _clipGenres) _cmbClipGenre.Items.Add(g);
            _cmbClipGenre.SelectedIndex = 0;
            _cmbClipGenre.SelectedIndexChanged += (s, e) => ApplyClipFilter();

            filterRow.Controls.AddRange(new Control[] { lblCat, _cmbClipCategory, lblGen, _cmbClipGenre });

            _dgvClips = CreateArchiveDgv();
            _dgvClips.Columns.Add(new DataGridViewTextBoxColumn { Name = "Title", HeaderText = "Titolo", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            _dgvClips.Columns.Add(new DataGridViewTextBoxColumn { Name = "Genre", HeaderText = "Genere", Width = 80 });
            _dgvClips.Columns.Add(new DataGridViewTextBoxColumn { Name = "Duration", HeaderText = "Durata", Width = 60 });
            _dgvClips.Dock = DockStyle.Fill;
            _dgvClips.DoubleClick += (s, e) => AddSelectedClipToPlaylist();
            _dgvClips.MouseDown += DgvClips_MouseDown;
            _dgvClips.MouseMove += DgvClips_MouseMove;

            _btnAddClip = CreateButton(LanguageManager.GetString("PlaylistEditor.AddToPlaylist", "➤ Aggiungi"), Color.FromArgb(33, 150, 83));
            _btnAddClip.Dock = DockStyle.Bottom;
            _btnAddClip.Height = 30;
            _btnAddClip.Click += (s, e) => AddSelectedClipToPlaylist();

            // Add in correct WinForms docking order: Fill first (processed last), then Bottom, then Top
            page.Controls.Add(_dgvClips);          // Dock=Fill
            page.Controls.Add(_btnAddClip);        // Dock=Bottom
            page.Controls.Add(_txtClipSearch);     // Dock=Top
            page.Controls.Add(filterRow);          // Dock=Top

            ApplyClipFilter();
            return page;
        }

        private TabPage BuildPlaylistsTab()
        {
            TabPage page = new TabPage(LanguageManager.GetString("PlaylistEditor.TabPlaylists", "📋 Playlist"))
            { BackColor = Color.FromArgb(28, 28, 28), Padding = new Padding(6) };

            _dgvPlaylists = CreateArchiveDgv();
            _dgvPlaylists.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Nome", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            _dgvPlaylists.Columns.Add(new DataGridViewTextBoxColumn { Name = "Modified", HeaderText = "Modificato", Width = 130 });
            _dgvPlaylists.Columns.Add(new DataGridViewTextBoxColumn { Name = "Items", HeaderText = "Elementi", Width = 70 });
            _dgvPlaylists.Dock = DockStyle.Fill;
            _dgvPlaylists.DoubleClick += (s, e) => OpenSelectedPlaylist();

            _btnDeletePlaylist = CreateButton(LanguageManager.GetString("PlaylistEditor.DeletePlaylist", "Elimina"), Color.FromArgb(150, 60, 60));
            _btnDeletePlaylist.Dock = DockStyle.Bottom;
            _btnDeletePlaylist.Height = 30;
            _btnDeletePlaylist.Click += BtnDeletePlaylist_Click;

            // Add in correct WinForms docking order: Fill first (processed last), then Bottom
            page.Controls.Add(_dgvPlaylists);       // Dock=Fill
            page.Controls.Add(_btnDeletePlaylist);  // Dock=Bottom

            RefreshPlaylistList();
            return page;
        }

        private TabPage BuildRulesTab()
        {
            TabPage page = new TabPage(LanguageManager.GetString("PlaylistEditor.TabRules", "🔧 Regole"))
            { BackColor = Color.FromArgb(28, 28, 28), Padding = new Padding(6) };

            int y = 8;

            // Source type
            _radRuleCategory = new RadioButton { Text = LanguageManager.GetString("PlaylistEditor.RuleCategory", "Categoria"), ForeColor = Color.White, Left = 10, Top = y, AutoSize = true, Checked = true, Font = new Font("Segoe UI", 10) };
            _radRuleGenre = new RadioButton { Text = LanguageManager.GetString("PlaylistEditor.RuleGenre", "Genere"), ForeColor = Color.White, Left = 120, Top = y, AutoSize = true, Font = new Font("Segoe UI", 10) };
            _radRuleCategoryGenre = new RadioButton { Text = LanguageManager.GetString("PlaylistEditor.RuleCategoryGenre", "Categoria + Genere"), ForeColor = Color.White, Left = 200, Top = y, AutoSize = true, Font = new Font("Segoe UI", 10) };
            _radRuleCategory.CheckedChanged += (s, e) => UpdateRuleControls();
            _radRuleGenre.CheckedChanged += (s, e) => UpdateRuleControls();
            _radRuleCategoryGenre.CheckedChanged += (s, e) => UpdateRuleControls();
            page.Controls.AddRange(new Control[] { _radRuleCategory, _radRuleGenre, _radRuleCategoryGenre });
            y += 38;

            // Category combo
            Label lblCat = new Label { Text = LanguageManager.GetString("PlaylistEditor.FilterCategory", "Categoria:"), ForeColor = Color.White, Left = 10, Top = y + 3, Width = 80, Height = 22, Font = new Font("Segoe UI", 10) };
            _cmbRuleCategory = new ComboBox { Left = 95, Top = y, Width = 280, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(45, 45, 45), ForeColor = Color.White, Font = new Font("Segoe UI", 10) };
            _cmbRuleCategory.Items.Add("-- " + LanguageManager.GetString("PlaylistEditor.SelectCategory", "Seleziona") + " --");
            foreach (var c in _musicCategories) _cmbRuleCategory.Items.Add(c);
            _cmbRuleCategory.SelectedIndex = 0;
            _cmbRuleCategory.SelectedIndexChanged += (s, e) => UpdateRuleFoundCount();
            page.Controls.AddRange(new Control[] { lblCat, _cmbRuleCategory });
            y += 40;

            // Genre combo
            Label lblGen = new Label { Text = LanguageManager.GetString("PlaylistEditor.FilterGenre", "Genere:"), ForeColor = Color.White, Left = 10, Top = y + 3, Width = 80, Height = 22, Font = new Font("Segoe UI", 10) };
            _cmbRuleGenre = new ComboBox { Left = 95, Top = y, Width = 280, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(45, 45, 45), ForeColor = Color.White, Font = new Font("Segoe UI", 10) };
            _cmbRuleGenre.Items.Add("-- " + LanguageManager.GetString("PlaylistEditor.SelectGenre", "Seleziona") + " --");
            foreach (var g in _musicGenres) _cmbRuleGenre.Items.Add(g);
            _cmbRuleGenre.SelectedIndex = 0;
            _cmbRuleGenre.SelectedIndexChanged += (s, e) => UpdateRuleFoundCount();
            page.Controls.AddRange(new Control[] { lblGen, _cmbRuleGenre });
            y += 40;

            // Year filter
            _chkRuleYearFilter = new CheckBox { Text = LanguageManager.GetString("PlaylistEditor.YearFilter", "Filtro Anni:"), ForeColor = Color.White, Left = 10, Top = y, AutoSize = true, Font = new Font("Segoe UI", 10) };
            _chkRuleYearFilter.CheckedChanged += (s, e) => { UpdateRuleControls(); UpdateRuleFoundCount(); };
            page.Controls.Add(_chkRuleYearFilter);
            y += 34;

            Label lblFrom = new Label { Text = LanguageManager.GetString("PlaylistEditor.YearFrom", "Da:"), ForeColor = Color.White, Left = 20, Top = y + 3, Width = 28, Height = 22, Font = new Font("Segoe UI", 10) };
            _numRuleYearFrom = new NumericUpDown { Left = 52, Top = y, Width = 80, Minimum = 1900, Maximum = DateTime.Now.Year, Value = 2000, BackColor = Color.FromArgb(45, 45, 45), ForeColor = Color.White, Font = new Font("Segoe UI", 10) };
            _numRuleYearFrom.ValueChanged += (s, e) => UpdateRuleFoundCount();
            Label lblTo = new Label { Text = LanguageManager.GetString("PlaylistEditor.YearTo", "A:"), ForeColor = Color.White, Left = 140, Top = y + 3, Width = 20, Height = 22, Font = new Font("Segoe UI", 10) };
            _numRuleYearTo = new NumericUpDown { Left = 162, Top = y, Width = 80, Minimum = 1900, Maximum = DateTime.Now.Year, Value = DateTime.Now.Year, BackColor = Color.FromArgb(45, 45, 45), ForeColor = Color.White, Font = new Font("Segoe UI", 10) };
            _numRuleYearTo.ValueChanged += (s, e) => UpdateRuleFoundCount();
            page.Controls.AddRange(new Control[] { lblFrom, _numRuleYearFrom, lblTo, _numRuleYearTo });
            y += 40;

            // Found count
            _lblRuleFoundTracks = new Label
            {
                Text = "",
                ForeColor = Color.FromArgb(0, 200, 130),
                Left = 10,
                Top = y,
                Width = 400,
                Height = 44,
                Font = new Font("Segoe UI", 10)
            };
            page.Controls.Add(_lblRuleFoundTracks);
            y += 54;

            // Add rule button
            _btnAddRule = CreateButton(LanguageManager.GetString("PlaylistEditor.AddRuleToPlaylist", "➤ Aggiungi Regola"), Color.FromArgb(33, 150, 83));
            _btnAddRule.Left = 10; _btnAddRule.Top = y; _btnAddRule.Width = 220; _btnAddRule.Height = 34;
            _btnAddRule.Click += BtnAddRule_Click;
            page.Controls.Add(_btnAddRule);

            UpdateRuleControls();
            return page;
        }

        // ── HELPER FACTORIES ─────────────────────────────────────────────

        private Button CreateButton(string text, Color backColor)
        {
            return new Button
            {
                Text = text,
                BackColor = backColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Height = 28,
                Width = 110,
                Cursor = Cursors.Hand
            };
        }

        private TextBox CreateSearchBox(string placeholder)
        {
            var tb = new TextBox
            {
                Text = placeholder,
                ForeColor = Color.Gray,
                BackColor = Color.FromArgb(45, 45, 45),
                Height = 26,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9)
            };
            tb.GotFocus += (s, e) => { if (tb.ForeColor == Color.Gray) { tb.Text = ""; tb.ForeColor = Color.White; } };
            tb.LostFocus += (s, e) => { if (string.IsNullOrEmpty(tb.Text)) { tb.Text = placeholder; tb.ForeColor = Color.Gray; } };
            return tb;
        }

        private DataGridView CreateArchiveDgv()
        {
            var dgv = new DataGridView
            {
                BackgroundColor = Color.FromArgb(35, 35, 35),
                GridColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10),
                ColumnHeadersHeight = 34,
                RowTemplate = { Height = 30 }
            };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(50, 50, 50);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            dgv.DefaultCellStyle.BackColor = Color.FromArgb(35, 35, 35);
            dgv.DefaultCellStyle.ForeColor = Color.White;
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(33, 150, 243);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(42, 42, 42);
            return dgv;
        }

        // ══════════════════════════════════════════════════════════════════
        // LANGUAGE
        // ══════════════════════════════════════════════════════════════════

        private void ApplyLanguage()
        {
            this.Text = LanguageManager.GetString("PlaylistEditor.Title", "PLAYLIST EDITOR");
            if (_lblHeader != null)
                _lblHeader.Text = "🎶 " + LanguageManager.GetString("PlaylistEditor.Title", "PLAYLIST EDITOR");
            if (_btnSave != null) _btnSave.Text = LanguageManager.GetString("PlaylistEditor.Save", "💾 Salva");
            if (_btnNew != null) _btnNew.Text = LanguageManager.GetString("PlaylistEditor.New", "📂 Nuova");
            if (_btnClear != null) _btnClear.Text = LanguageManager.GetString("PlaylistEditor.ClearEditor", "🗑️ Svuota");
            if (_btnMoveUp != null) _btnMoveUp.Text = LanguageManager.GetString("PlaylistEditor.MoveUp", "⬆ Su");
            if (_btnMoveDown != null) _btnMoveDown.Text = LanguageManager.GetString("PlaylistEditor.MoveDown", "⬇ Giù");
            if (_btnRemove != null) _btnRemove.Text = LanguageManager.GetString("PlaylistEditor.RemoveSelected", "❌ Rimuovi");
            if (_btnAddMusic != null) _btnAddMusic.Text = LanguageManager.GetString("PlaylistEditor.AddToPlaylist", "➤ Aggiungi");
            if (_btnAddClip != null) _btnAddClip.Text = LanguageManager.GetString("PlaylistEditor.AddToPlaylist", "➤ Aggiungi");
            if (_btnAddRule != null) _btnAddRule.Text = LanguageManager.GetString("PlaylistEditor.AddRuleToPlaylist", "➤ Aggiungi Regola");
            if (_btnDeletePlaylist != null) _btnDeletePlaylist.Text = LanguageManager.GetString("PlaylistEditor.DeletePlaylist", "Elimina");
            if (_tabArchive != null && _tabArchive.TabPages.Count >= 4)
            {
                _tabArchive.TabPages[0].Text = LanguageManager.GetString("PlaylistEditor.TabMusic", "🎵 Music");
                _tabArchive.TabPages[1].Text = LanguageManager.GetString("PlaylistEditor.TabClips", "🔔 Clips");
                _tabArchive.TabPages[2].Text = LanguageManager.GetString("PlaylistEditor.TabPlaylists", "📋 Playlist");
                _tabArchive.TabPages[3].Text = LanguageManager.GetString("PlaylistEditor.TabRules", "🔧 Regole");
            }
            UpdateTotalDurationLabel();
        }

        // ══════════════════════════════════════════════════════════════════
        // EDITOR GRID REFRESH
        // ══════════════════════════════════════════════════════════════════

        private void RefreshEditorGrid()
        {
            _playlist.RecalculateTimings();
            _dgvEditor.SuspendLayout();
            _dgvEditor.Rows.Clear();

            for (int i = 0; i < _playlist.Items.Count; i++)
            {
                var item = _playlist.Items[i];
                int effDur = item.GetEffectiveDuration();
                string durStr = FormatDurationSec(effDur);
                if (item.Type == AirPlaylistItemType.Category || item.Type == AirPlaylistItemType.Genre)
                    durStr = string.Format(LanguageManager.GetString("PlaylistEditor.AverageDuration", "~{0}"), durStr);

                int rowIdx = _dgvEditor.Rows.Add(
                    (i + 1).ToString(),
                    item.ScheduledTime.ToString(@"hh\:mm\:ss"),
                    item.GetTypeIcon(),
                    item.GetDisplayName(),
                    durStr
                );
                _dgvEditor.Rows[rowIdx].Tag = item;
            }

            _dgvEditor.ResumeLayout();
            UpdateTotalDurationLabel();
        }

        private void UpdateTotalDurationLabel()
        {
            if (_lblTotalDuration == null) return;
            TimeSpan total = _playlist?.GetTotalDuration() ?? TimeSpan.Zero;
            _lblTotalDuration.Text = string.Format(
                LanguageManager.GetString("PlaylistEditor.TotalDuration", "⏱️ Durata totale: {0}"),
                total.ToString(@"hh\:mm\:ss"));
        }

        private string FormatDurationSec(int seconds)
        {
            int m = seconds / 60;
            int s = seconds % 60;
            return $"{m:00}:{s:00}";
        }

        // ══════════════════════════════════════════════════════════════════
        // UNSAVED CHANGES
        // ══════════════════════════════════════════════════════════════════

        private void MarkChanged()
        {
            _hasUnsavedChanges = true;
        }

        private bool ConfirmDiscardChanges()
        {
            if (!_hasUnsavedChanges) return true;
            var result = MessageBox.Show(
                LanguageManager.GetString("PlaylistEditor.UnsavedChanges", "Ci sono modifiche non salvate. Continuare?"),
                LanguageManager.GetString("PlaylistEditor.UnsavedChangesTitle", "Modifiche Non Salvate"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            return result == DialogResult.Yes;
        }

        // ══════════════════════════════════════════════════════════════════
        // SAVE / LOAD
        // ══════════════════════════════════════════════════════════════════

        private string GetPlaylistFolder()
        {
            string dbPath = DbcManager.GetDatabasePath();
            return Path.Combine(dbPath, "Playlist");
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            string name = _txtPlaylistName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show(
                    LanguageManager.GetString("PlaylistEditor.EmptyName", "❌ Il nome non può essere vuoto"),
                    "AirDirector", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _playlist.Name = name;
                if (TimeSpan.TryParseExact(_txtStartTime.Text, @"hh\:mm\:ss", null, out TimeSpan st))
                    _playlist.StartTime = st;

                if (string.IsNullOrEmpty(_currentFilePath))
                    _currentFilePath = Path.Combine(GetPlaylistFolder(), $"{name}.airpls");

                _playlist.Save(_currentFilePath);
                _hasUnsavedChanges = false;
                RefreshPlaylistList();
                MessageBox.Show(
                    LanguageManager.GetString("PlaylistEditor.SavedSuccess", "✅ Playlist salvata con successo!"),
                    "AirDirector", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    LanguageManager.GetString("PlaylistEditor.SaveError", "❌ Errore durante il salvataggio") + "\n" + ex.Message,
                    "AirDirector", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnNew_Click(object sender, EventArgs e)
        {
            if (!ConfirmDiscardChanges()) return;
            _playlist = new AirPlaylist();
            _currentFilePath = null;
            _hasUnsavedChanges = false;
            _txtPlaylistName.Text = "";
            _txtStartTime.Text = "00:00:00";
            RefreshEditorGrid();
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                LanguageManager.GetString("PlaylistEditor.ConfirmClear", "Svuotare l'editor? Le modifiche non salvate andranno perse."),
                LanguageManager.GetString("PlaylistEditor.ConfirmClearTitle", "Conferma Svuota"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes) return;
            _playlist.Items.Clear();
            MarkChanged();
            RefreshEditorGrid();
        }

        // ══════════════════════════════════════════════════════════════════
        // START TIME
        // ══════════════════════════════════════════════════════════════════

        private void TxtStartTime_TextChanged(object sender, EventArgs e)
        {
            if (TimeSpan.TryParseExact(_txtStartTime.Text, @"hh\:mm\:ss", null, out TimeSpan st))
            {
                _playlist.StartTime = st;
                RefreshEditorGrid();
                MarkChanged();
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // MOVE / REMOVE
        // ══════════════════════════════════════════════════════════════════

        private int GetSelectedEditorRow()
        {
            if (_dgvEditor.SelectedRows.Count == 0) return -1;
            return _dgvEditor.SelectedRows[0].Index;
        }

        private void BtnMoveUp_Click(object sender, EventArgs e)
        {
            if (_dgvEditor.SelectedRows.Count == 0) return;
            var indices = _dgvEditor.SelectedRows.Cast<DataGridViewRow>()
                .Select(r => r.Index).OrderBy(i => i).ToList();
            if (indices[0] <= 0) return;
            foreach (int idx in indices)
            {
                var tmp = _playlist.Items[idx - 1];
                _playlist.Items[idx - 1] = _playlist.Items[idx];
                _playlist.Items[idx] = tmp;
            }
            MarkChanged();
            RefreshEditorGrid();
            foreach (int idx in indices)
                if (idx - 1 < _dgvEditor.Rows.Count) _dgvEditor.Rows[idx - 1].Selected = true;
        }

        private void BtnMoveDown_Click(object sender, EventArgs e)
        {
            if (_dgvEditor.SelectedRows.Count == 0) return;
            var indices = _dgvEditor.SelectedRows.Cast<DataGridViewRow>()
                .Select(r => r.Index).OrderByDescending(i => i).ToList();
            if (indices[0] >= _playlist.Items.Count - 1) return;
            foreach (int idx in indices)
            {
                var tmp = _playlist.Items[idx + 1];
                _playlist.Items[idx + 1] = _playlist.Items[idx];
                _playlist.Items[idx] = tmp;
            }
            MarkChanged();
            RefreshEditorGrid();
            foreach (int idx in indices)
                if (idx + 1 < _dgvEditor.Rows.Count) _dgvEditor.Rows[idx + 1].Selected = true;
        }

        private void BtnRemove_Click(object sender, EventArgs e)
        {
            if (_dgvEditor.SelectedRows.Count == 0) return;
            var items = _dgvEditor.SelectedRows.Cast<DataGridViewRow>()
                .Select(r => r.Tag as AirPlaylistItem).Where(item => item != null).ToList();
            foreach (var item in items)
                _playlist.Items.Remove(item);
            MarkChanged();
            RefreshEditorGrid();
        }

        // ══════════════════════════════════════════════════════════════════
        // CLIPBOARD — COPY / CUT / PASTE
        // ══════════════════════════════════════════════════════════════════

        private void CopySelectedItems()
        {
            if (_dgvEditor.SelectedRows.Count == 0) return;
            _clipboardItems.Clear();
            _isCutOperation = false;
            foreach (DataGridViewRow row in _dgvEditor.SelectedRows)
            {
                var item = row.Tag as AirPlaylistItem;
                if (item != null) _clipboardItems.Add(item);
            }
            _clipboardItems = _clipboardItems.OrderBy(item => _playlist.Items.IndexOf(item)).ToList();
        }

        private void CutSelectedItems()
        {
            if (_dgvEditor.SelectedRows.Count == 0) return;
            _clipboardItems.Clear();
            _isCutOperation = true;
            foreach (DataGridViewRow row in _dgvEditor.SelectedRows)
            {
                var item = row.Tag as AirPlaylistItem;
                if (item != null) _clipboardItems.Add(item);
            }
            _clipboardItems = _clipboardItems.OrderBy(item => _playlist.Items.IndexOf(item)).ToList();
            foreach (var item in _clipboardItems)
                _playlist.Items.Remove(item);
            MarkChanged();
            RefreshEditorGrid();
        }

        private void PasteItems()
        {
            if (_clipboardItems.Count == 0) return;
            int insertIndex = _dgvEditor.SelectedRows.Count > 0
                ? _dgvEditor.SelectedRows.Cast<DataGridViewRow>().Min(r => r.Index) + 1
                : _playlist.Items.Count;

            foreach (var item in _clipboardItems)
            {
                var newItem = new AirPlaylistItem
                {
                    Type = item.Type,
                    FilePath = item.FilePath,
                    Artist = item.Artist,
                    Title = item.Title,
                    DurationSeconds = item.DurationSeconds,
                    MarkerIN = item.MarkerIN,
                    MarkerMIX = item.MarkerMIX,
                    CategoryName = item.CategoryName,
                    YearFilterEnabled = item.YearFilterEnabled,
                    YearFrom = item.YearFrom,
                    YearTo = item.YearTo
                };
                _playlist.Items.Insert(insertIndex++, newItem);
            }

            if (_isCutOperation)
                _clipboardItems.Clear();

            MarkChanged();
            RefreshEditorGrid();
        }

        // ══════════════════════════════════════════════════════════════════
        // DRAG & DROP IN EDITOR
        // ══════════════════════════════════════════════════════════════════

        private void DgvEditor_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            var hit = _dgvEditor.HitTest(e.X, e.Y);
            if (hit.RowIndex >= 0)
                _dragSourceRow = hit.RowIndex;
        }

        private void DgvEditor_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || _dragSourceRow < 0) return;
            _dgvEditor.DoDragDrop(_dragSourceRow, DragDropEffects.Move);
            _dragSourceRow = -1;
        }

        private void DgvEditor_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(int)))
                e.Effect = DragDropEffects.Move;
            else if (e.Data.GetDataPresent(typeof(DragDropData)))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void DgvEditor_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(int)))
                e.Effect = DragDropEffects.Move;
            else if (e.Data.GetDataPresent(typeof(DragDropData)))
                e.Effect = DragDropEffects.Copy;
            else
                return;
            Point pt = _dgvEditor.PointToClient(new Point(e.X, e.Y));
            var hit = _dgvEditor.HitTest(pt.X, pt.Y);
            _dragTargetRow = hit.RowIndex;
        }

        private void DgvEditor_DragDrop(object sender, DragEventArgs e)
        {
            Point pt = _dgvEditor.PointToClient(new Point(e.X, e.Y));
            var hit = _dgvEditor.HitTest(pt.X, pt.Y);
            int targetRow = hit.RowIndex;

            if (e.Data.GetDataPresent(typeof(int)))
            {
                int sourceRow = (int)e.Data.GetData(typeof(int));
                if (sourceRow < 0 || targetRow < 0 || sourceRow == targetRow) return;
                if (sourceRow >= _playlist.Items.Count || targetRow >= _playlist.Items.Count) return;

                var item = _playlist.Items[sourceRow];
                _playlist.Items.RemoveAt(sourceRow);
                _playlist.Items.Insert(targetRow, item);
                MarkChanged();
                RefreshEditorGrid();
                if (targetRow < _dgvEditor.Rows.Count) _dgvEditor.Rows[targetRow].Selected = true;
            }
            else if (e.Data.GetDataPresent(typeof(DragDropData)))
            {
                var data = e.Data.GetData(typeof(DragDropData)) as DragDropData;
                if (data == null) return;

                AirPlaylistItem item = null;
                if (data.EntryType == "MusicEntry" && data.EntryData is MusicEntry music)
                {
                    int markerMix = music.MarkerMIX;
                    int markerIn = music.MarkerIN;
                    int durationSec = markerMix > markerIn ? (markerMix - markerIn) / 1000 : music.Duration / 1000;
                    item = new AirPlaylistItem
                    {
                        Type = AirPlaylistItemType.Track,
                        FilePath = music.FilePath,
                        Artist = music.Artist ?? "",
                        Title = music.Title ?? "",
                        DurationSeconds = durationSec,
                        MarkerIN = markerIn,
                        MarkerMIX = markerMix
                    };
                }
                else if (data.EntryType == "ClipEntry" && data.EntryData is ClipEntry clip)
                {
                    int markerMix = clip.MarkerMIX;
                    int markerIn = clip.MarkerIN;
                    int durationSec = markerMix > markerIn ? (markerMix - markerIn) / 1000 : clip.Duration / 1000;
                    item = new AirPlaylistItem
                    {
                        Type = AirPlaylistItemType.Clip,
                        FilePath = clip.FilePath,
                        Title = clip.Title ?? "",
                        DurationSeconds = durationSec,
                        MarkerIN = markerIn,
                        MarkerMIX = markerMix
                    };
                }

                if (item == null) return;

                if (targetRow >= 0 && targetRow < _playlist.Items.Count)
                    _playlist.Items.Insert(targetRow, item);
                else
                    _playlist.Items.Add(item);

                MarkChanged();
                RefreshEditorGrid();
                int insertedRow = (targetRow >= 0 && targetRow < _dgvEditor.Rows.Count) ? targetRow : _dgvEditor.Rows.Count - 1;
                if (insertedRow >= 0 && insertedRow < _dgvEditor.Rows.Count)
                    _dgvEditor.Rows[insertedRow].Selected = true;
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // DRAG & DROP FROM ARCHIVE GRIDS
        // ══════════════════════════════════════════════════════════════════

        private void DgvMusic_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                _archiveDragStart = e.Location;
        }

        private void DgvMusic_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (Math.Abs(e.X - _archiveDragStart.X) < ArchiveDragThreshold && Math.Abs(e.Y - _archiveDragStart.Y) < ArchiveDragThreshold) return;
            if (_dgvMusic.SelectedRows.Count == 0) return;
            var entry = _dgvMusic.SelectedRows[0].Tag as MusicEntry;
            if (entry == null) return;
            var data = new DragDropData { EntryType = "MusicEntry", EntryData = entry };
            _dgvMusic.DoDragDrop(data, DragDropEffects.Copy);
        }

        private void DgvClips_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                _archiveDragStart = e.Location;
        }

        private void DgvClips_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (Math.Abs(e.X - _archiveDragStart.X) < ArchiveDragThreshold && Math.Abs(e.Y - _archiveDragStart.Y) < ArchiveDragThreshold) return;
            if (_dgvClips.SelectedRows.Count == 0) return;
            var entry = _dgvClips.SelectedRows[0].Tag as ClipEntry;
            if (entry == null) return;
            var data = new DragDropData { EntryType = "ClipEntry", EntryData = entry };
            _dgvClips.DoDragDrop(data, DragDropEffects.Copy);
        }

        // ══════════════════════════════════════════════════════════════════
        // ADD FROM MUSIC TAB
        // ══════════════════════════════════════════════════════════════════

        private void ApplyMusicFilter()
        {
            if (_dgvMusic == null) return;
            string searchRaw = _txtMusicSearch?.Text ?? "";
            string searchText = (searchRaw == LanguageManager.GetString("PlaylistEditor.SearchPlaceholder", "Cerca artista o titolo...")) ? "" : searchRaw.ToLower();
            string selCat = _cmbMusicCategory?.SelectedItem?.ToString() ?? "";
            string allCats = LanguageManager.GetString("PlaylistEditor.AllCategories", "Tutte");
            string selGen = _cmbMusicGenre?.SelectedItem?.ToString() ?? "";
            string allGens = LanguageManager.GetString("PlaylistEditor.AllGenres", "Tutti");

            _dgvMusic.Rows.Clear();
            var filtered = _allMusicEntries.AsEnumerable();
            if (!string.IsNullOrEmpty(searchText))
                filtered = filtered.Where(m => (m.Artist + " " + m.Title).ToLower().Contains(searchText));
            if (selCat != allCats && !string.IsNullOrEmpty(selCat))
                filtered = filtered.Where(m => !string.IsNullOrEmpty(m.Categories) &&
                    m.Categories.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Any(c => c.Trim().Equals(selCat, StringComparison.OrdinalIgnoreCase)));
            if (selGen != allGens && !string.IsNullOrEmpty(selGen))
                filtered = filtered.Where(m => m.Genre.Trim().Equals(selGen, StringComparison.OrdinalIgnoreCase));

            foreach (var m in filtered)
            {
                int effMs = m.MarkerMIX > m.MarkerIN ? m.MarkerMIX - m.MarkerIN : m.Duration;
                int rowIdx = _dgvMusic.Rows.Add(m.Artist ?? "", m.Title ?? "", m.Genre ?? "", FormatDurationSec(effMs / 1000));
                _dgvMusic.Rows[rowIdx].Tag = m;
            }
        }

        private void AddSelectedMusicToPlaylist()
        {
            if (_dgvMusic.SelectedRows.Count == 0) return;
            var entry = _dgvMusic.SelectedRows[0].Tag as MusicEntry;
            if (entry == null) return;

            int markerMix = entry.MarkerMIX;
            int markerIn = entry.MarkerIN;
            int durationSec = markerMix > markerIn ? (markerMix - markerIn) / 1000 : entry.Duration / 1000;

            var item = new AirPlaylistItem
            {
                Type = AirPlaylistItemType.Track,
                FilePath = entry.FilePath,
                Artist = entry.Artist ?? "",
                Title = entry.Title ?? "",
                DurationSeconds = durationSec,
                MarkerIN = markerIn,
                MarkerMIX = markerMix
            };

            _playlist.Items.Add(item);
            MarkChanged();
            RefreshEditorGrid();
            // Select newly added row
            if (_dgvEditor.Rows.Count > 0)
                _dgvEditor.Rows[_dgvEditor.Rows.Count - 1].Selected = true;
        }

        // ══════════════════════════════════════════════════════════════════
        // ADD FROM CLIPS TAB
        // ══════════════════════════════════════════════════════════════════

        private void ApplyClipFilter()
        {
            if (_dgvClips == null) return;
            string searchRaw = _txtClipSearch?.Text ?? "";
            string searchText = (searchRaw == LanguageManager.GetString("PlaylistEditor.SearchPlaceholder", "Cerca artista o titolo...")) ? "" : searchRaw.ToLower();
            string selCat = _cmbClipCategory?.SelectedItem?.ToString() ?? "";
            string allCats = LanguageManager.GetString("PlaylistEditor.AllCategories", "Tutte");
            string selGen = _cmbClipGenre?.SelectedItem?.ToString() ?? "";
            string allGens = LanguageManager.GetString("PlaylistEditor.AllGenres", "Tutti");

            _dgvClips.Rows.Clear();
            var filtered = _allClipEntries.AsEnumerable();
            if (!string.IsNullOrEmpty(searchText))
                filtered = filtered.Where(c => (c.Title ?? "").ToLower().Contains(searchText));
            if (selCat != allCats && !string.IsNullOrEmpty(selCat))
                filtered = filtered.Where(c => !string.IsNullOrEmpty(c.Categories) &&
                    c.Categories.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Any(cat => cat.Trim().Equals(selCat, StringComparison.OrdinalIgnoreCase)));
            if (selGen != allGens && !string.IsNullOrEmpty(selGen))
                filtered = filtered.Where(c => c.Genre.Trim().Equals(selGen, StringComparison.OrdinalIgnoreCase));

            foreach (var c in filtered)
            {
                int effMs = c.MarkerMIX > c.MarkerIN ? c.MarkerMIX - c.MarkerIN : c.Duration;
                int rowIdx = _dgvClips.Rows.Add(c.Title ?? "", c.Genre ?? "", FormatDurationSec(effMs / 1000));
                _dgvClips.Rows[rowIdx].Tag = c;
            }
        }

        private void AddSelectedClipToPlaylist()
        {
            if (_dgvClips.SelectedRows.Count == 0) return;
            var entry = _dgvClips.SelectedRows[0].Tag as ClipEntry;
            if (entry == null) return;

            int markerMix = entry.MarkerMIX;
            int markerIn = entry.MarkerIN;
            int durationSec = markerMix > markerIn ? (markerMix - markerIn) / 1000 : entry.Duration / 1000;

            var item = new AirPlaylistItem
            {
                Type = AirPlaylistItemType.Clip,
                FilePath = entry.FilePath,
                Title = entry.Title ?? "",
                DurationSeconds = durationSec,
                MarkerIN = markerIn,
                MarkerMIX = markerMix
            };

            _playlist.Items.Add(item);
            MarkChanged();
            RefreshEditorGrid();
            if (_dgvEditor.Rows.Count > 0)
                _dgvEditor.Rows[_dgvEditor.Rows.Count - 1].Selected = true;
        }

        // ══════════════════════════════════════════════════════════════════
        // PLAYLISTS TAB
        // ══════════════════════════════════════════════════════════════════

        private void RefreshPlaylistList()
        {
            if (_dgvPlaylists == null) return;
            _dgvPlaylists.Rows.Clear();
            string folder = GetPlaylistFolder();
            if (!Directory.Exists(folder))
            {
                _dgvPlaylists.Rows.Add(LanguageManager.GetString("PlaylistEditor.NoPlaylistsFound", "Nessuna playlist trovata"), "", "");
                return;
            }
            var files = Directory.GetFiles(folder, "*.airpls");
            if (files.Length == 0)
            {
                _dgvPlaylists.Rows.Add(LanguageManager.GetString("PlaylistEditor.NoPlaylistsFound", "Nessuna playlist trovata"), "", "");
                return;
            }
            foreach (var f in files.OrderByDescending(x => File.GetLastWriteTime(x)))
            {
                try
                {
                    var pl = AirPlaylist.Load(f);
                    int rowIdx = _dgvPlaylists.Rows.Add(pl.Name, pl.ModifiedDate.ToString("dd/MM/yyyy HH:mm"), pl.Items?.Count.ToString() ?? "0");
                    _dgvPlaylists.Rows[rowIdx].Tag = f;
                }
                catch
                {
                    int rowIdx = _dgvPlaylists.Rows.Add(Path.GetFileNameWithoutExtension(f), File.GetLastWriteTime(f).ToString("dd/MM/yyyy HH:mm"), "?");
                    _dgvPlaylists.Rows[rowIdx].Tag = f;
                }
            }
        }

        private void OpenSelectedPlaylist()
        {
            if (_dgvPlaylists.SelectedRows.Count == 0) return;
            string filePath = _dgvPlaylists.SelectedRows[0].Tag as string;
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;

            if (!ConfirmDiscardChanges()) return;

            try
            {
                _playlist = AirPlaylist.Load(filePath);
                _currentFilePath = filePath;
                _hasUnsavedChanges = false;
                _txtPlaylistName.Text = _playlist.Name;
                _txtStartTime.Text = _playlist.StartTime.ToString(@"hh\:mm\:ss");
                RefreshEditorGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    LanguageManager.GetString("PlaylistEditor.LoadError", "❌ Errore durante il caricamento") + "\n" + ex.Message,
                    "AirDirector", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDeletePlaylist_Click(object sender, EventArgs e)
        {
            if (_dgvPlaylists.SelectedRows.Count == 0) return;
            string filePath = _dgvPlaylists.SelectedRows[0].Tag as string;
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;

            string playlistName = Path.GetFileNameWithoutExtension(filePath);
            var result = MessageBox.Show(
                string.Format(LanguageManager.GetString("PlaylistEditor.ConfirmDelete", "Eliminare la playlist '{0}'?"), playlistName),
                LanguageManager.GetString("PlaylistEditor.ConfirmDeleteTitle", "Conferma Eliminazione"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes) return;

            try
            {
                File.Delete(filePath);
                if (_currentFilePath == filePath)
                {
                    _playlist = new AirPlaylist();
                    _currentFilePath = null;
                    _hasUnsavedChanges = false;
                    _txtPlaylistName.Text = "";
                    _txtStartTime.Text = "00:00:00";
                    RefreshEditorGrid();
                }
                RefreshPlaylistList();
                MessageBox.Show(LanguageManager.GetString("PlaylistEditor.PlaylistDeleted", "✅ Playlist eliminata"), "AirDirector", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "AirDirector", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // RULES TAB
        // ══════════════════════════════════════════════════════════════════

        private void UpdateRuleControls()
        {
            if (_cmbRuleCategory == null || _cmbRuleGenre == null) return;
            bool showCat = _radRuleCategory?.Checked == true || _radRuleCategoryGenre?.Checked == true;
            bool showGen = _radRuleGenre?.Checked == true || _radRuleCategoryGenre?.Checked == true;
            _cmbRuleCategory.Enabled = showCat;
            _cmbRuleGenre.Enabled = showGen;
            bool yearEnabled = _chkRuleYearFilter?.Checked == true;
            if (_numRuleYearFrom != null) _numRuleYearFrom.Enabled = yearEnabled;
            if (_numRuleYearTo != null) _numRuleYearTo.Enabled = yearEnabled;
            UpdateRuleFoundCount();
        }

        private void UpdateRuleFoundCount()
        {
            if (_lblRuleFoundTracks == null) return;
            try
            {
                var filtered = FilterMusicForRule();
                int count = filtered.Count;
                if (count > 0)
                {
                    double avgMs = filtered.Average(m =>
                    {
                        int effMs = m.MarkerMIX > m.MarkerIN ? m.MarkerMIX - m.MarkerIN : m.Duration;
                        return (double)effMs;
                    });
                    TimeSpan avgTime = TimeSpan.FromMilliseconds(avgMs);
                    _lblRuleFoundTracks.Text = string.Format(
                        LanguageManager.GetString("PlaylistEditor.FoundTracks", "📊 Trovati: {0} brani - Durata media: {1}"),
                        count,
                        avgTime.ToString(@"mm\:ss"));
                    _lblRuleFoundTracks.ForeColor = Color.FromArgb(0, 200, 130);
                }
                else
                {
                    _lblRuleFoundTracks.Text = LanguageManager.GetString("PlaylistEditor.NoTracksFound", "⚠️ Nessun brano trovato");
                    _lblRuleFoundTracks.ForeColor = Color.OrangeRed;
                }
            }
            catch
            {
                _lblRuleFoundTracks.Text = "";
            }
        }

        private List<MusicEntry> FilterMusicForRule()
        {
            var filtered = _allMusicEntries.AsEnumerable();
            bool catFilter = _radRuleCategory?.Checked == true || _radRuleCategoryGenre?.Checked == true;
            bool genFilter = _radRuleGenre?.Checked == true || _radRuleCategoryGenre?.Checked == true;

            if (catFilter && _cmbRuleCategory?.SelectedIndex > 0)
            {
                string selCat = _cmbRuleCategory.SelectedItem.ToString();
                filtered = filtered.Where(m =>
                    !string.IsNullOrEmpty(m.Categories) &&
                    m.Categories.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Any(c => c.Trim().Equals(selCat, StringComparison.OrdinalIgnoreCase)));
            }
            if (genFilter && _cmbRuleGenre?.SelectedIndex > 0)
            {
                string selGen = _cmbRuleGenre.SelectedItem.ToString();
                filtered = filtered.Where(m =>
                    !string.IsNullOrEmpty(m.Genre) &&
                    m.Genre.Trim().Equals(selGen, StringComparison.OrdinalIgnoreCase));
            }
            if (_chkRuleYearFilter?.Checked == true)
            {
                int yFrom = (int)(_numRuleYearFrom?.Value ?? 1900);
                int yTo = (int)(_numRuleYearTo?.Value ?? DateTime.Now.Year);
                filtered = filtered.Where(m => m.Year >= yFrom && m.Year <= yTo);
            }
            return filtered.ToList();
        }

        private void BtnAddRule_Click(object sender, EventArgs e)
        {
            bool isCategory = _radRuleCategory?.Checked == true;
            bool isGenre = _radRuleGenre?.Checked == true;
            bool isBoth = _radRuleCategoryGenre?.Checked == true;

            string catName = _cmbRuleCategory?.SelectedIndex > 0 ? _cmbRuleCategory.SelectedItem.ToString() : null;
            string genName = _cmbRuleGenre?.SelectedIndex > 0 ? _cmbRuleGenre.SelectedItem.ToString() : null;

            if ((isCategory || isBoth) && string.IsNullOrEmpty(catName))
            {
                MessageBox.Show(LanguageManager.GetString("PlaylistEditor.SelectCategory", "⚠️ Seleziona una categoria"), "AirDirector", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if ((isGenre || isBoth) && string.IsNullOrEmpty(genName))
            {
                MessageBox.Show(LanguageManager.GetString("PlaylistEditor.SelectGenre", "⚠️ Seleziona un genere"), "AirDirector", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Calculate average duration
            var filtered = FilterMusicForRule();
            int avgDurSec = 0;
            if (filtered.Count > 0)
            {
                double avgMs = filtered.Average(m =>
                {
                    int effMs = m.MarkerMIX > m.MarkerIN ? m.MarkerMIX - m.MarkerIN : m.Duration;
                    return (double)effMs;
                });
                avgDurSec = (int)(avgMs / 1000.0);
            }

            AirPlaylistItemType itemType;
            string displayName;
            if (isBoth)
            {
                itemType = AirPlaylistItemType.Category;
                displayName = $"{catName} / {genName}";
            }
            else if (isGenre)
            {
                itemType = AirPlaylistItemType.Genre;
                displayName = genName;
            }
            else
            {
                itemType = AirPlaylistItemType.Category;
                displayName = catName;
            }

            var item = new AirPlaylistItem
            {
                Type = itemType,
                CategoryName = displayName,
                DurationSeconds = avgDurSec,
                YearFilterEnabled = _chkRuleYearFilter?.Checked == true,
                YearFrom = (int)(_numRuleYearFrom?.Value ?? 1900),
                YearTo = (int)(_numRuleYearTo?.Value ?? DateTime.Now.Year)
            };

            _playlist.Items.Add(item);
            MarkChanged();
            RefreshEditorGrid();
            if (_dgvEditor.Rows.Count > 0)
                _dgvEditor.Rows[_dgvEditor.Rows.Count - 1].Selected = true;
        }

        // ══════════════════════════════════════════════════════════════════
        // FORM CLOSING
        // ══════════════════════════════════════════════════════════════════

        private void PlaylistEditorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_hasUnsavedChanges)
            {
                var result = MessageBox.Show(
                    LanguageManager.GetString("PlaylistEditor.UnsavedChanges", "Ci sono modifiche non salvate. Continuare?"),
                    LanguageManager.GetString("PlaylistEditor.UnsavedChangesTitle", "Modifiche Non Salvate"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (result == DialogResult.No) e.Cancel = true;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                LanguageManager.LanguageChanged -= OnLanguageChanged;
                LanguageManager.SaveMissingKeysToFile();
            }
            base.Dispose(disposing);
        }
    }
}
