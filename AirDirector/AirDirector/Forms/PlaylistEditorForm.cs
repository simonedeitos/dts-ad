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
        private List<StreamingEntry> _streamingEntries;
        private List<CommandEntry> _commandEntries;
        private bool _isRadioTVMode;

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
        private ComboBox _cmbRuleSource;
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
        private GroupBox _grpRuleCommands;
        private Label _lblRuleCommandSelect;
        private ComboBox _cmbRuleCommand;
        private Button _btnInsertCommand;
        private ComboBox _cmbPlaylistStreaming;
        private MaskedTextBox _txtPlaylistStreamDuration;
        private TextBox _txtPlaylistStreamingBuffer;
        private Button _btnBrowsePlaylistStreamingBuffer;
        private Button _btnInsertStreaming;
        private TextBox _txtRuleAudioFile;
        private Button _btnBrowseRuleAudioFile;
        private CheckBox _chkRuleBufferVideo;
        private TextBox _txtRuleBufferVideo;
        private Button _btnBrowseRuleBufferVideo;
        private CheckBox _chkRuleVideoMp4;
        private TextBox _txtRuleVideoMp4;
        private Button _btnBrowseRuleVideoMp4;
        private Button _btnInsertAudio;

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
            _streamingEntries = new List<StreamingEntry>();
            _commandEntries = new List<CommandEntry>();
            _isRadioTVMode = ConfigurationControl.IsRadioTVMode();

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

            try { _streamingEntries = DbcManager.LoadFromCsv<StreamingEntry>("Streaming.dbc"); }
            catch { _streamingEntries = new List<StreamingEntry>(); }

            try { _commandEntries = DbcManager.LoadFromCsv<CommandEntry>("Commands.dbc"); }
            catch { _commandEntries = new List<CommandEntry>(); }
        }

        // ══════════════════════════════════════════════════════════════════
        // INITIALIZE UI
        // ══════════════════════════════════════════════════════════════════

        private void InitializeComponent()
        {
            this.Text = LanguageManager.GetString("PlaylistEditor.Title", "PLAYLIST EDITOR");
            this.Size = new Size(1500, 950);
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
                    mainSplit.SplitterDistance = (int)(mainSplit.Width * 0.55);
                    mainSplit.Panel1MinSize = Math.Min(400, mainSplit.Width / 2);
                    mainSplit.Panel2MinSize = Math.Min(300, mainSplit.Width / 2);
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
            colTime.DefaultCellStyle.ForeColor = Color.White;
            colTime.DefaultCellStyle.BackColor = Color.FromArgb(35, 35, 35);
            _dgvEditor.Columns.Add(colTime);
            _dgvEditor.Columns.Add(new DataGridViewTextBoxColumn { Name = "colType", HeaderText = LanguageManager.GetString("PlaylistEditor.ColumnType", "Tipo"), Width = 42, ReadOnly = true });
            var colType = _dgvEditor.Columns["colType"];
            colType.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
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
            _dgvMusic.Columns.Add(new DataGridViewTextBoxColumn { Name = "Artist", HeaderText = LanguageManager.GetString("PlaylistEditor.ColumnArtist", "Artista"), Width = 180 });
            _dgvMusic.Columns.Add(new DataGridViewTextBoxColumn { Name = "Title", HeaderText = LanguageManager.GetString("PlaylistEditor.ColumnTitle", "Titolo"), AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            _dgvMusic.Columns.Add(new DataGridViewTextBoxColumn { Name = "Genre", HeaderText = LanguageManager.GetString("PlaylistEditor.ColumnGenre", "Genere"), Width = 80 });
            _dgvMusic.Columns.Add(new DataGridViewTextBoxColumn { Name = "Duration", HeaderText = LanguageManager.GetString("PlaylistEditor.ColumnDuration", "Durata"), Width = 60 });
            _dgvMusic.Dock = DockStyle.Fill;
            _dgvMusic.DoubleClick += (s, e) => AddSelectedMusicToPlaylist();
            _dgvMusic.MouseDown += DgvMusic_MouseDown;
            _dgvMusic.MouseMove += DgvMusic_MouseMove;

            // Add button
            _btnAddMusic = CreateButton(LanguageManager.GetString("PlaylistEditor.AddToPlaylist", "+ Aggiungi"), Color.FromArgb(33, 150, 83));
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
            _dgvClips.Columns.Add(new DataGridViewTextBoxColumn { Name = "Title", HeaderText = LanguageManager.GetString("PlaylistEditor.ColumnTitle", "Titolo"), AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            _dgvClips.Columns.Add(new DataGridViewTextBoxColumn { Name = "Genre", HeaderText = LanguageManager.GetString("PlaylistEditor.ColumnGenre", "Genere"), Width = 80 });
            _dgvClips.Columns.Add(new DataGridViewTextBoxColumn { Name = "Duration", HeaderText = LanguageManager.GetString("PlaylistEditor.ColumnDuration", "Durata"), Width = 60 });
            _dgvClips.Dock = DockStyle.Fill;
            _dgvClips.DoubleClick += (s, e) => AddSelectedClipToPlaylist();
            _dgvClips.MouseDown += DgvClips_MouseDown;
            _dgvClips.MouseMove += DgvClips_MouseMove;

            _btnAddClip = CreateButton(LanguageManager.GetString("PlaylistEditor.AddToPlaylist", "+ Aggiungi"), Color.FromArgb(33, 150, 83));
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
            _dgvPlaylists.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = LanguageManager.GetString("PlaylistEditor.ColumnPlaylistName", "Nome"), AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            _dgvPlaylists.Columns.Add(new DataGridViewTextBoxColumn { Name = "Modified", HeaderText = LanguageManager.GetString("PlaylistEditor.ColumnPlaylistModified", "Modificato"), Width = 130 });
            _dgvPlaylists.Columns.Add(new DataGridViewTextBoxColumn { Name = "Items", HeaderText = LanguageManager.GetString("PlaylistEditor.ColumnPlaylistItems", "Elementi"), Width = 70 });
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
            { BackColor = Color.FromArgb(28, 28, 28), Padding = new Padding(20, 20, 6, 6) };

            int y = 20;

            // Source selection (Music or Clips)
            Label lblSource = new Label { Text = LanguageManager.GetString("PlaylistEditor.RuleSource", "Sorgente:"), ForeColor = Color.White, Left = 16, Top = y + 3, Width = 80, Height = 22, Font = new Font("Segoe UI", 10) };
            _cmbRuleSource = new ComboBox { Left = 100, Top = y, Width = 280, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(45, 45, 45), ForeColor = Color.White, Font = new Font("Segoe UI", 10) };
            _cmbRuleSource.Items.Add("🎵 " + LanguageManager.GetString("PlaylistEditor.RuleSourceMusic", "Music"));
            _cmbRuleSource.Items.Add("🔔 " + LanguageManager.GetString("PlaylistEditor.RuleSourceClips", "Clips"));
            _cmbRuleSource.SelectedIndex = 0;
            _cmbRuleSource.SelectedIndexChanged += (s, e) => OnRuleSourceChanged();
            page.Controls.AddRange(new Control[] { lblSource, _cmbRuleSource });
            y += 40;

            // Source type
            _radRuleCategory = new RadioButton { Text = LanguageManager.GetString("PlaylistEditor.RuleCategory", "Categoria"), ForeColor = Color.White, Left = 16, Top = y, AutoSize = true, Checked = true, Font = new Font("Segoe UI", 10) };
            _radRuleGenre = new RadioButton { Text = LanguageManager.GetString("PlaylistEditor.RuleGenre", "Genere"), ForeColor = Color.White, Left = 130, Top = y, AutoSize = true, Font = new Font("Segoe UI", 10) };
            _radRuleCategoryGenre = new RadioButton { Text = LanguageManager.GetString("PlaylistEditor.RuleCategoryGenre", "Categoria + Genere"), ForeColor = Color.White, Left = 210, Top = y, AutoSize = true, Font = new Font("Segoe UI", 10) };
            _radRuleCategory.CheckedChanged += (s, e) => UpdateRuleControls();
            _radRuleGenre.CheckedChanged += (s, e) => UpdateRuleControls();
            _radRuleCategoryGenre.CheckedChanged += (s, e) => UpdateRuleControls();
            page.Controls.AddRange(new Control[] { _radRuleCategory, _radRuleGenre, _radRuleCategoryGenre });
            y += 38;

            // Category combo
            Label lblCat = new Label { Text = LanguageManager.GetString("PlaylistEditor.FilterCategory", "Categoria:"), ForeColor = Color.White, Left = 16, Top = y + 3, Width = 80, Height = 22, Font = new Font("Segoe UI", 10) };
            _cmbRuleCategory = new ComboBox { Left = 100, Top = y, Width = 280, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(45, 45, 45), ForeColor = Color.White, Font = new Font("Segoe UI", 10) };
            _cmbRuleCategory.Items.Add("-- " + LanguageManager.GetString("PlaylistEditor.SelectCategory", "Seleziona") + " --");
            foreach (var c in _musicCategories) _cmbRuleCategory.Items.Add(c);
            _cmbRuleCategory.SelectedIndex = 0;
            _cmbRuleCategory.SelectedIndexChanged += (s, e) => UpdateRuleFoundCount();
            page.Controls.AddRange(new Control[] { lblCat, _cmbRuleCategory });
            y += 40;

            // Genre combo
            Label lblGen = new Label { Text = LanguageManager.GetString("PlaylistEditor.FilterGenre", "Genere:"), ForeColor = Color.White, Left = 16, Top = y + 3, Width = 80, Height = 22, Font = new Font("Segoe UI", 10) };
            _cmbRuleGenre = new ComboBox { Left = 100, Top = y, Width = 280, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(45, 45, 45), ForeColor = Color.White, Font = new Font("Segoe UI", 10) };
            _cmbRuleGenre.Items.Add("-- " + LanguageManager.GetString("PlaylistEditor.SelectGenre", "Seleziona") + " --");
            foreach (var g in _musicGenres) _cmbRuleGenre.Items.Add(g);
            _cmbRuleGenre.SelectedIndex = 0;
            _cmbRuleGenre.SelectedIndexChanged += (s, e) => UpdateRuleFoundCount();
            page.Controls.AddRange(new Control[] { lblGen, _cmbRuleGenre });
            y += 40;

            // Year filter
            _chkRuleYearFilter = new CheckBox { Text = LanguageManager.GetString("PlaylistEditor.YearFilter", "Filtro Anni:"), ForeColor = Color.White, Left = 16, Top = y, AutoSize = true, Font = new Font("Segoe UI", 10) };
            _chkRuleYearFilter.CheckedChanged += (s, e) => { UpdateRuleControls(); UpdateRuleFoundCount(); };
            page.Controls.Add(_chkRuleYearFilter);
            y += 34;

            Label lblFrom = new Label { Text = LanguageManager.GetString("PlaylistEditor.YearFrom", "Da:"), ForeColor = Color.White, Left = 20, Top = y + 3, Width = 60, Height = 22, Font = new Font("Segoe UI", 10) };
            _numRuleYearFrom = new NumericUpDown { Left = 86, Top = y, Width = 90, Minimum = 1900, Maximum = DateTime.Now.Year, Value = 2000, BackColor = Color.FromArgb(45, 45, 45), ForeColor = Color.White, Font = new Font("Segoe UI", 10) };
            _numRuleYearFrom.ValueChanged += (s, e) => UpdateRuleFoundCount();
            Label lblTo = new Label { Text = LanguageManager.GetString("PlaylistEditor.YearTo", "A:"), ForeColor = Color.White, Left = 180, Top = y + 3, Width = 60, Height = 22, Font = new Font("Segoe UI", 10) };
            _numRuleYearTo = new NumericUpDown { Left = 250, Top = y, Width = 90, Minimum = 1900, Maximum = DateTime.Now.Year, Value = DateTime.Now.Year, BackColor = Color.FromArgb(45, 45, 45), ForeColor = Color.White, Font = new Font("Segoe UI", 10) };
            _numRuleYearTo.ValueChanged += (s, e) => UpdateRuleFoundCount();
            page.Controls.AddRange(new Control[] { lblFrom, _numRuleYearFrom, lblTo, _numRuleYearTo });
            y += 40;

            // Found count
            _lblRuleFoundTracks = new Label
            {
                Text = "",
                ForeColor = Color.FromArgb(0, 200, 130),
                Left = 16,
                Top = y,
                Width = 400,
                Height = 44,
                Font = new Font("Segoe UI", 10)
            };
            page.Controls.Add(_lblRuleFoundTracks);
            y += 54;

            // Add rule button
            _btnAddRule = CreateButton(LanguageManager.GetString("PlaylistEditor.AddRuleToPlaylist", "+ Aggiungi Regola"), Color.FromArgb(33, 150, 83));
            _btnAddRule.Left = 16; _btnAddRule.Top = y; _btnAddRule.Width = 220; _btnAddRule.Height = 34;
            _btnAddRule.Click += BtnAddRule_Click;
            page.Controls.Add(_btnAddRule);
            y += 48;

            _grpRuleCommands = new GroupBox
            {
                Text = LanguageManager.GetString("PlaylistEditor.InsertCommand", "Inserisci Comando"),
                ForeColor = Color.White,
                Left = 16,
                Top = y,
                Width = 620,
                Height = 90
            };
            _lblRuleCommandSelect = new Label
            {
                Text = LanguageManager.GetString("PlaylistEditor.SelectCommand", "Comando:"),
                Left = 12,
                Top = 28,
                AutoSize = true,
                ForeColor = Color.White
            };
            _grpRuleCommands.Controls.Add(_lblRuleCommandSelect);

            int commandComboLeft = Math.Max(120, _lblRuleCommandSelect.Right + 12);
            int commandComboWidth = Math.Max(260, _grpRuleCommands.Width - commandComboLeft - 140);
            _cmbRuleCommand = new ComboBox { Left = commandComboLeft, Top = 24, Width = commandComboWidth, DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var command in _commandEntries.OrderBy(c => c.Name))
                _cmbRuleCommand.Items.Add(command);
            if (_cmbRuleCommand.Items.Count > 0) _cmbRuleCommand.SelectedIndex = 0;
            _btnInsertCommand = CreateButton(LanguageManager.GetString("PlaylistEditor.InsertCommandBtn", "Inserisci Comando"), Color.FromArgb(33, 150, 83));
            _btnInsertCommand.Left = _grpRuleCommands.Width - 130; _btnInsertCommand.Top = 22; _btnInsertCommand.Width = 120; _btnInsertCommand.Height = 30;
            _btnInsertCommand.Click += BtnInsertCommand_Click;
            _grpRuleCommands.Controls.AddRange(new Control[] { _cmbRuleCommand, _btnInsertCommand });
            page.Controls.Add(_grpRuleCommands);
            y += _grpRuleCommands.Height + 10;

            GroupBox grpStreaming = new GroupBox
            {
                Text = LanguageManager.GetString("PlaylistEditor.InsertStreaming", "Inserisci Streaming"),
                ForeColor = Color.White,
                Left = 16,
                Top = y,
                Width = 620,
                Height = _isRadioTVMode ? 130 : 95
            };
            grpStreaming.Controls.Add(new Label { Text = LanguageManager.GetString("ScheduleEditor.URLStreaming", "URL Streaming") + ":", Left = 12, Top = 28, Width = 85, ForeColor = Color.White });
            _cmbPlaylistStreaming = new ComboBox { Left = 100, Top = 24, Width = 290, DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var stream in _streamingEntries) _cmbPlaylistStreaming.Items.Add(stream);
            if (_cmbPlaylistStreaming.Items.Count > 0) _cmbPlaylistStreaming.SelectedIndex = 0;
            grpStreaming.Controls.Add(_cmbPlaylistStreaming);
            grpStreaming.Controls.Add(new Label { Text = LanguageManager.GetString("PlaylistEditor.StreamingDuration", "Durata:"), Left = 400, Top = 28, Width = 55, ForeColor = Color.White });
            _txtPlaylistStreamDuration = new MaskedTextBox { Left = 460, Top = 24, Width = 80, Mask = "00:00:00", Text = "010000" };
            grpStreaming.Controls.Add(_txtPlaylistStreamDuration);

            _txtPlaylistStreamingBuffer = new TextBox { Left = 130, Top = 57, Width = 410, Visible = _isRadioTVMode };
            _btnBrowsePlaylistStreamingBuffer = new Button { Text = "📁", Left = 545, Top = 56, Width = 30, Height = 24, Visible = _isRadioTVMode };
            _btnBrowsePlaylistStreamingBuffer.Click += (s, e) => BrowseFileInto(_txtPlaylistStreamingBuffer, LanguageManager.GetString("PlaylistEditor.SelectBufferFile", "Seleziona file buffer video"), "Video|*.mp4;*.mov;*.avi;*.mkv;*.wmv|All files|*.*");
            grpStreaming.Controls.Add(new Label { Text = "File Buffer Video:", Left = 12, Top = 60, Width = 115, ForeColor = Color.White, Visible = _isRadioTVMode });
            grpStreaming.Controls.Add(_txtPlaylistStreamingBuffer);
            grpStreaming.Controls.Add(_btnBrowsePlaylistStreamingBuffer);

            _btnInsertStreaming = CreateButton(LanguageManager.GetString("PlaylistEditor.InsertStreamingBtn", "Inserisci Streaming in Playlist"), Color.FromArgb(33, 150, 83));
            _btnInsertStreaming.Left = 390; _btnInsertStreaming.Top = _isRadioTVMode ? 88 : 56; _btnInsertStreaming.Width = 220; _btnInsertStreaming.Height = 30;
            _btnInsertStreaming.Click += BtnInsertStreaming_Click;
            grpStreaming.Controls.Add(_btnInsertStreaming);
            page.Controls.Add(grpStreaming);
            y += grpStreaming.Height + 10;

            GroupBox grpAudio = new GroupBox
            {
                Text = LanguageManager.GetString("PlaylistEditor.InsertAudio", "Inserisci Audio"),
                ForeColor = Color.White,
                Left = 16,
                Top = y,
                Width = 620,
                Height = _isRadioTVMode ? 170 : 95
            };
            grpAudio.Controls.Add(new Label { Text = LanguageManager.GetString("PlaylistEditor.SelectAudioFile", "Seleziona file audio") + ":", Left = 12, Top = 28, Width = 110, ForeColor = Color.White });
            _txtRuleAudioFile = new TextBox { Left = 125, Top = 24, Width = 415 };
            _btnBrowseRuleAudioFile = new Button { Text = "📁", Left = 545, Top = 24, Width = 30, Height = 24 };
            _btnBrowseRuleAudioFile.Click += (s, e) => BrowseFileInto(_txtRuleAudioFile, LanguageManager.GetString("PlaylistEditor.SelectAudioFile", "Seleziona file audio"), "Audio|*.mp3;*.wav;*.wma;*.aac;*.flac|All files|*.*");
            grpAudio.Controls.AddRange(new Control[] { _txtRuleAudioFile, _btnBrowseRuleAudioFile });

            _chkRuleBufferVideo = new CheckBox { Left = 12, Top = 58, Width = 200, Text = LanguageManager.GetString("PlaylistEditor.AddBufferVideo", "Aggiungi File Buffer Video"), ForeColor = Color.White, Visible = _isRadioTVMode };
            _chkRuleBufferVideo.CheckedChanged += (s, e) => UpdateRuleVideoOptions();
            _txtRuleBufferVideo = new TextBox { Left = 220, Top = 56, Width = 320, Visible = false };
            _btnBrowseRuleBufferVideo = new Button { Text = "📁", Left = 545, Top = 56, Width = 30, Height = 24, Visible = false };
            _btnBrowseRuleBufferVideo.Click += (s, e) => BrowseFileInto(_txtRuleBufferVideo, LanguageManager.GetString("PlaylistEditor.SelectBufferFile", "Seleziona file buffer video"), "Video|*.mp4;*.mov;*.avi;*.mkv;*.wmv|All files|*.*");
            grpAudio.Controls.AddRange(new Control[] { _chkRuleBufferVideo, _txtRuleBufferVideo, _btnBrowseRuleBufferVideo });

            _chkRuleVideoMp4 = new CheckBox { Left = 12, Top = 88, Width = 280, Text = LanguageManager.GetString("PlaylistEditor.AddVideoMP4", "Aggiungi File Video MP4 (non da archivio)"), ForeColor = Color.White, Visible = _isRadioTVMode };
            _chkRuleVideoMp4.CheckedChanged += (s, e) => UpdateRuleVideoOptions();
            _txtRuleVideoMp4 = new TextBox { Left = 300, Top = 86, Width = 240, Visible = false };
            _btnBrowseRuleVideoMp4 = new Button { Text = "📁", Left = 545, Top = 86, Width = 30, Height = 24, Visible = false };
            _btnBrowseRuleVideoMp4.Click += (s, e) => BrowseFileInto(_txtRuleVideoMp4, LanguageManager.GetString("PlaylistEditor.SelectVideoFile", "Seleziona file video"), "MP4|*.mp4|All files|*.*");
            grpAudio.Controls.AddRange(new Control[] { _chkRuleVideoMp4, _txtRuleVideoMp4, _btnBrowseRuleVideoMp4 });

            _btnInsertAudio = CreateButton(LanguageManager.GetString("PlaylistEditor.InsertAudioBtn", "Inserisci Audio in Playlist"), Color.FromArgb(33, 150, 83));
            _btnInsertAudio.Left = 390; _btnInsertAudio.Top = _isRadioTVMode ? 122 : 56; _btnInsertAudio.Width = 220; _btnInsertAudio.Height = 30;
            _btnInsertAudio.Click += BtnInsertAudio_Click;
            grpAudio.Controls.Add(_btnInsertAudio);
            page.Controls.Add(grpAudio);

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
            if (_btnAddMusic != null) _btnAddMusic.Text = LanguageManager.GetString("PlaylistEditor.AddToPlaylist", "+ Aggiungi");
            if (_btnAddClip != null) _btnAddClip.Text = LanguageManager.GetString("PlaylistEditor.AddToPlaylist", "+ Aggiungi");
            if (_btnAddRule != null) _btnAddRule.Text = LanguageManager.GetString("PlaylistEditor.AddRuleToPlaylist", "+ Aggiungi Regola");
            if (_btnDeletePlaylist != null) _btnDeletePlaylist.Text = LanguageManager.GetString("PlaylistEditor.DeletePlaylist", "Elimina");
            if (_grpRuleCommands != null) _grpRuleCommands.Text = LanguageManager.GetString("PlaylistEditor.InsertCommand", "Inserisci Comando");
            if (_lblRuleCommandSelect != null) _lblRuleCommandSelect.Text = LanguageManager.GetString("PlaylistEditor.SelectCommand", "Comando:");
            if (_cmbRuleCommand != null && _btnInsertCommand != null && _grpRuleCommands != null && _lblRuleCommandSelect != null)
            {
                int commandComboLeft = Math.Max(120, _lblRuleCommandSelect.Right + 12);
                int commandComboWidth = Math.Max(260, _grpRuleCommands.Width - commandComboLeft - 140);
                _cmbRuleCommand.Left = commandComboLeft;
                _cmbRuleCommand.Width = commandComboWidth;
                _btnInsertCommand.Left = _grpRuleCommands.Width - 130;
            }
            if (_btnInsertCommand != null) _btnInsertCommand.Text = LanguageManager.GetString("PlaylistEditor.InsertCommandBtn", "Inserisci Comando");
            if (_btnInsertStreaming != null) _btnInsertStreaming.Text = LanguageManager.GetString("PlaylistEditor.InsertStreamingBtn", "Inserisci Streaming in Playlist");
            if (_btnInsertAudio != null) _btnInsertAudio.Text = LanguageManager.GetString("PlaylistEditor.InsertAudioBtn", "Inserisci Audio in Playlist");
            if (_chkRuleBufferVideo != null) _chkRuleBufferVideo.Text = LanguageManager.GetString("PlaylistEditor.AddBufferVideo", "Aggiungi File Buffer Video");
            if (_chkRuleVideoMp4 != null) _chkRuleVideoMp4.Text = LanguageManager.GetString("PlaylistEditor.AddVideoMP4", "Aggiungi File Video MP4 (non da archivio)");
            if (_tabArchive != null && _tabArchive.TabPages.Count >= 4)
            {
                _tabArchive.TabPages[0].Text = LanguageManager.GetString("PlaylistEditor.TabMusic", "🎵 Music");
                _tabArchive.TabPages[1].Text = LanguageManager.GetString("PlaylistEditor.TabClips", "🔔 Clips");
                _tabArchive.TabPages[2].Text = LanguageManager.GetString("PlaylistEditor.TabPlaylists", "📋 Playlist");
                _tabArchive.TabPages[3].Text = LanguageManager.GetString("PlaylistEditor.TabRules", "🔧 Regole");
            }
            // Music archive columns
            if (_dgvMusic != null && _dgvMusic.Columns.Count >= 4)
            {
                _dgvMusic.Columns["Artist"].HeaderText = LanguageManager.GetString("PlaylistEditor.ColumnArtist", "Artista");
                _dgvMusic.Columns["Title"].HeaderText = LanguageManager.GetString("PlaylistEditor.ColumnTitle", "Titolo");
                _dgvMusic.Columns["Genre"].HeaderText = LanguageManager.GetString("PlaylistEditor.ColumnGenre", "Genere");
                _dgvMusic.Columns["Duration"].HeaderText = LanguageManager.GetString("PlaylistEditor.ColumnDuration", "Durata");
            }
            // Clips archive columns
            if (_dgvClips != null && _dgvClips.Columns.Count >= 3)
            {
                _dgvClips.Columns["Title"].HeaderText = LanguageManager.GetString("PlaylistEditor.ColumnTitle", "Titolo");
                _dgvClips.Columns["Genre"].HeaderText = LanguageManager.GetString("PlaylistEditor.ColumnGenre", "Genere");
                _dgvClips.Columns["Duration"].HeaderText = LanguageManager.GetString("PlaylistEditor.ColumnDuration", "Durata");
            }
            // Playlists archive columns
            if (_dgvPlaylists != null && _dgvPlaylists.Columns.Count >= 3)
            {
                _dgvPlaylists.Columns["Name"].HeaderText = LanguageManager.GetString("PlaylistEditor.ColumnPlaylistName", "Nome");
                _dgvPlaylists.Columns["Modified"].HeaderText = LanguageManager.GetString("PlaylistEditor.ColumnPlaylistModified", "Modificato");
                _dgvPlaylists.Columns["Items"].HeaderText = LanguageManager.GetString("PlaylistEditor.ColumnPlaylistItems", "Elementi");
            }
            // Editor columns
            if (_dgvEditor != null && _dgvEditor.Columns.Count >= 5)
            {
                _dgvEditor.Columns["colTime"].HeaderText = LanguageManager.GetString("PlaylistEditor.ColumnTime", "Orario");
                _dgvEditor.Columns["colType"].HeaderText = LanguageManager.GetString("PlaylistEditor.ColumnType", "Tipo");
                _dgvEditor.Columns["colElement"].HeaderText = LanguageManager.GetString("PlaylistEditor.ColumnElement", "Elemento");
                _dgvEditor.Columns["colDuration"].HeaderText = LanguageManager.GetString("PlaylistEditor.ColumnDuration", "Durata");
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

                // Set row text color based on item type
                if (item.Type == AirPlaylistItemType.Track)
                {
                    _dgvEditor.Rows[rowIdx].DefaultCellStyle.ForeColor = Color.FromArgb(255, 215, 0); // Gold - Music
                }
                else if (item.Type == AirPlaylistItemType.Clip)
                {
                    _dgvEditor.Rows[rowIdx].DefaultCellStyle.ForeColor = Color.FromArgb(0, 180, 255); // Azure - Clip
                }
                else if (item.Type == AirPlaylistItemType.Category || item.Type == AirPlaylistItemType.Genre)
                {
                    if (item.RuleSourceType == "Clips")
                        _dgvEditor.Rows[rowIdx].DefaultCellStyle.ForeColor = Color.FromArgb(0, 180, 255); // Azure - Clips rule
                    else
                        _dgvEditor.Rows[rowIdx].DefaultCellStyle.ForeColor = Color.FromArgb(255, 215, 0); // Gold - Music rule
                }
            }

            _dgvEditor.ResumeLayout();
            UpdateTotalDurationLabel();
            _dgvEditor.ClearSelection();
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
                    YearTo = item.YearTo,
                    RuleSourceType = item.RuleSourceType,
                    RuleCategoryName = item.RuleCategoryName,
                    RuleGenreName = item.RuleGenreName,
                    StreamDuration = item.StreamDuration,
                    CommandValue = item.CommandValue,
                    AssociatedBufferPath = item.AssociatedBufferPath,
                    AssociatedVideoPath = item.AssociatedVideoPath
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

        private void BrowseFileInto(TextBox target, string title, string filter)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = title;
                ofd.Filter = filter;
                if (ofd.ShowDialog(this) == DialogResult.OK)
                    target.Text = ofd.FileName;
            }
        }

        private void UpdateRuleVideoOptions()
        {
            if (!_isRadioTVMode)
                return;

            if (_chkRuleBufferVideo != null && _chkRuleVideoMp4 != null)
            {
                if (_chkRuleBufferVideo.Checked && _chkRuleVideoMp4.Checked)
                {
                    if (ReferenceEquals(ActiveControl, _chkRuleBufferVideo))
                        _chkRuleVideoMp4.Checked = false;
                    else
                        _chkRuleBufferVideo.Checked = false;
                }
            }

            bool showBuffer = _chkRuleBufferVideo?.Checked == true;
            bool showVideo = _chkRuleVideoMp4?.Checked == true;
            if (_txtRuleBufferVideo != null) _txtRuleBufferVideo.Visible = showBuffer;
            if (_btnBrowseRuleBufferVideo != null) _btnBrowseRuleBufferVideo.Visible = showBuffer;
            if (_txtRuleVideoMp4 != null) _txtRuleVideoMp4.Visible = showVideo;
            if (_btnBrowseRuleVideoMp4 != null) _btnBrowseRuleVideoMp4.Visible = showVideo;
        }

        private void BtnInsertCommand_Click(object sender, EventArgs e)
        {
            var selectedCommand = _cmbRuleCommand?.SelectedItem as CommandEntry;
            if (selectedCommand == null)
                return;

            AirPlaylistItemType type;
            if (string.Equals(selectedCommand.Type, "LogoShow", StringComparison.OrdinalIgnoreCase))
                type = AirPlaylistItemType.LogoShow;
            else if (string.Equals(selectedCommand.Type, "LogoHide", StringComparison.OrdinalIgnoreCase))
                type = AirPlaylistItemType.LogoHide;
            else if (string.Equals(selectedCommand.Type, "UDP", StringComparison.OrdinalIgnoreCase))
                type = AirPlaylistItemType.CommandUdp;
            else
                type = AirPlaylistItemType.CommandHttp;

            var item = new AirPlaylistItem
            {
                Type = type,
                CommandValue = selectedCommand.CommandString ?? "",
                Title = selectedCommand.Name ?? "",
                DurationSeconds = 0
            };

            _playlist.Items.Add(item);
            MarkChanged();
            RefreshEditorGrid();
        }

        private void BtnInsertStreaming_Click(object sender, EventArgs e)
        {
            var selected = _cmbPlaylistStreaming?.SelectedItem as StreamingEntry;
            if (selected == null)
                return;

            TimeSpan duration = TimeSpan.FromHours(1);
            TimeSpan.TryParse((_txtPlaylistStreamDuration?.Text ?? "01:00:00").Trim(), out duration);

            var item = new AirPlaylistItem
            {
                Type = AirPlaylistItemType.URLStreaming,
                Title = selected.Name,
                FilePath = selected.URL,
                StreamDuration = duration.ToString(@"hh\:mm\:ss"),
                DurationSeconds = (int)duration.TotalSeconds,
                AssociatedBufferPath = _isRadioTVMode ? (_txtPlaylistStreamingBuffer?.Text ?? "") : ""
            };
            _playlist.Items.Add(item);
            MarkChanged();
            RefreshEditorGrid();
        }

        private void BtnInsertAudio_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtRuleAudioFile?.Text))
                return;

            var item = new AirPlaylistItem
            {
                Type = AirPlaylistItemType.ExternalAudio,
                FilePath = _txtRuleAudioFile.Text.Trim(),
                Title = Path.GetFileNameWithoutExtension(_txtRuleAudioFile.Text.Trim()),
                DurationSeconds = 0,
                AssociatedBufferPath = (_chkRuleBufferVideo?.Checked == true) ? (_txtRuleBufferVideo?.Text ?? "") : "",
                AssociatedVideoPath = (_chkRuleVideoMp4?.Checked == true) ? (_txtRuleVideoMp4?.Text ?? "") : ""
            };

            _playlist.Items.Add(item);
            MarkChanged();
            RefreshEditorGrid();
        }

        private void OnRuleSourceChanged()
        {
            bool isClips = _cmbRuleSource?.SelectedIndex == 1;

            // Repopulate category combo
            if (_cmbRuleCategory != null)
            {
                _cmbRuleCategory.Items.Clear();
                _cmbRuleCategory.Items.Add("-- " + LanguageManager.GetString("PlaylistEditor.SelectCategory", "Seleziona") + " --");
                var categories = isClips ? _clipCategories : _musicCategories;
                foreach (var c in categories) _cmbRuleCategory.Items.Add(c);
                _cmbRuleCategory.SelectedIndex = 0;
            }

            // Repopulate genre combo
            if (_cmbRuleGenre != null)
            {
                _cmbRuleGenre.Items.Clear();
                _cmbRuleGenre.Items.Add("-- " + LanguageManager.GetString("PlaylistEditor.SelectGenre", "Seleziona") + " --");
                var genres = isClips ? _clipGenres : _musicGenres;
                foreach (var g in genres) _cmbRuleGenre.Items.Add(g);
                _cmbRuleGenre.SelectedIndex = 0;
            }

            // Disable year filter for Clips (they don't have Year field)
            if (isClips)
            {
                if (_chkRuleYearFilter != null) { _chkRuleYearFilter.Checked = false; _chkRuleYearFilter.Enabled = false; }
                if (_numRuleYearFrom != null) _numRuleYearFrom.Enabled = false;
                if (_numRuleYearTo != null) _numRuleYearTo.Enabled = false;
            }
            else
            {
                if (_chkRuleYearFilter != null) _chkRuleYearFilter.Enabled = true;
                UpdateRuleControls(); // re-enables year fields based on checkbox state
            }

            UpdateRuleFoundCount();
        }

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
                bool isClips = _cmbRuleSource?.SelectedIndex == 1;
                int count;
                double avgMs;

                if (isClips)
                {
                    var filtered = FilterClipsForRule();
                    count = filtered.Count;
                    if (count > 0)
                        avgMs = filtered.Average(c => { int effMs = c.MarkerMIX > c.MarkerIN ? c.MarkerMIX - c.MarkerIN : c.Duration; return (double)effMs; });
                    else
                        avgMs = 0;
                }
                else
                {
                    var filtered = FilterMusicForRule();
                    count = filtered.Count;
                    if (count > 0)
                        avgMs = filtered.Average(m => { int effMs = m.MarkerMIX > m.MarkerIN ? m.MarkerMIX - m.MarkerIN : m.Duration; return (double)effMs; });
                    else
                        avgMs = 0;
                }

                if (count > 0)
                {
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

        private List<ClipEntry> FilterClipsForRule()
        {
            var filtered = _allClipEntries.AsEnumerable();
            bool catFilter = _radRuleCategory?.Checked == true || _radRuleCategoryGenre?.Checked == true;
            bool genFilter = _radRuleGenre?.Checked == true || _radRuleCategoryGenre?.Checked == true;

            if (catFilter && _cmbRuleCategory?.SelectedIndex > 0)
            {
                string selCat = _cmbRuleCategory.SelectedItem.ToString();
                filtered = filtered.Where(c =>
                    !string.IsNullOrEmpty(c.Categories) &&
                    c.Categories.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Any(cat => cat.Trim().Equals(selCat, StringComparison.OrdinalIgnoreCase)));
            }
            if (genFilter && _cmbRuleGenre?.SelectedIndex > 0)
            {
                string selGen = _cmbRuleGenre.SelectedItem.ToString();
                filtered = filtered.Where(c =>
                    !string.IsNullOrEmpty(c.Genre) &&
                    c.Genre.Trim().Equals(selGen, StringComparison.OrdinalIgnoreCase));
            }
            // No year filter for clips
            return filtered.ToList();
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
            bool isClips = _cmbRuleSource?.SelectedIndex == 1;
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

            // Calculate average duration from the correct source
            int avgDurSec = 0;
            if (isClips)
            {
                var filtered = FilterClipsForRule();
                if (filtered.Count > 0)
                {
                    double avgMs = filtered.Average(c => { int effMs = c.MarkerMIX > c.MarkerIN ? c.MarkerMIX - c.MarkerIN : c.Duration; return (double)effMs; });
                    avgDurSec = (int)(avgMs / 1000.0);
                }
            }
            else
            {
                var filtered = FilterMusicForRule();
                if (filtered.Count > 0)
                {
                    double avgMs = filtered.Average(m => { int effMs = m.MarkerMIX > m.MarkerIN ? m.MarkerMIX - m.MarkerIN : m.Duration; return (double)effMs; });
                    avgDurSec = (int)(avgMs / 1000.0);
                }
            }

            AirPlaylistItemType itemType;
            string displayName;
            if (isBoth)
            {
                itemType = isClips ? AirPlaylistItemType.Clip : AirPlaylistItemType.Category;
                displayName = $"{catName} / {genName}";
            }
            else if (isGenre)
            {
                itemType = isClips ? AirPlaylistItemType.Clip : AirPlaylistItemType.Genre;
                displayName = genName;
            }
            else
            {
                itemType = isClips ? AirPlaylistItemType.Clip : AirPlaylistItemType.Category;
                displayName = catName;
            }

            var item = new AirPlaylistItem
            {
                Type = itemType,
                CategoryName = displayName,
                DurationSeconds = avgDurSec,
                YearFilterEnabled = !isClips && (_chkRuleYearFilter?.Checked == true),
                YearFrom = (int)(_numRuleYearFrom?.Value ?? 1900),
                YearTo = (int)(_numRuleYearTo?.Value ?? DateTime.Now.Year),
                RuleSourceType = isClips ? "Clips" : "Music",
                RuleCategoryName = (isCategory || isBoth) ? catName : null,
                RuleGenreName = (isGenre || isBoth) ? genName : null
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
