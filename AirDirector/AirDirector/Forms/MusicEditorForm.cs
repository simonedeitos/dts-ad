using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using NAudio.Wave;
using AirDirector.Services.Database;
using AirDirector.Services.Localization;
using AirDirector.Services;
using AirDirector.Themes;
using AirDirector.Controls;

namespace AirDirector.Forms
{
    public partial class MusicEditorForm : Form
    {
        private MusicEntry _musicEntry;
        private AudioFileReader _audioReader;
        private WaveOutEvent _waveOut;
        private System.Windows.Forms.Timer _positionTimer;

        private float[] _waveformData;
        private float[] _originalWaveformData; // dati originali senza volume boost
        private Bitmap _waveformBitmap;
        private bool _isLoadingWaveform = false;
        private string _lastLoadedFile = "";
        private int _waveformSamples = 6000;

        private bool _isPlaying = false;
        private bool _isDraggingMarker = false;
        private string _draggingMarkerType = "";

        private CheckBox[] _chkMonths;
        private CheckBox[] _chkDays;
        private CheckBox[] _chkHours;
        private DateTimePicker _dtpValidFrom;
        private DateTimePicker _dtpValidTo;
        private CheckBox _chkEnableValidFrom;
        private CheckBox _chkEnableValidTo;

        private bool _isClip = false;
        private int _originalClipId = 0;

        private Button btnResetIn;
        private Button btnResetIntro;
        private Button btnResetMix;
        private Button btnResetOut;

        // ✅ ZOOM
        private float _zoomLevel = 1.0f;
        private int _scrollOffset = 0; // offset in waveformData samples
        private bool _userScrolled = false; // flag: l'utente ha scrollato manualmente

        // ✅ Backup marker originali (per ripristino su Annulla)
        private int _originalMarkerIN;
        private int _originalMarkerINTRO;
        private int _originalMarkerMIX;
        private int _originalMarkerOUT;

        // ✅ VOLUME BOOST
        private float _volumeBoostDb = 0f;

        // ✅ PENDING VOLUME APPLY (deferred ffmpeg after Save)
        private bool _pendingVolumeApply = false;
        private float _pendingVolumeBoostDb = 0f;
        private string _pendingFfmpegPath = null;
        private string _pendingSourceFile = null;

        // Flag per la visualizzazione dei picchi colorati nella waveform (disattivato di default)
        private bool _showColoredPeaks = false;

        // ✅ VU METER
        private float _vuLevelLeft = 0f;
        private float _vuLevelRight = 0f;
        private float _vuPeakLeft = 0f;
        private float _vuPeakRight = 0f;
        private System.Windows.Forms.Timer _vuDecayTimer;

        // ✅ AUTOCOMPLETE suggerimenti
        private AutoCompleteStringCollection _genreSuggestions;

        // ✅ CATEGORIE – dati interni per popup
        private List<string> _allCategories = new List<string>();
        private HashSet<string> _checkedCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // ✅ ARTISTI SECONDARI – dati interni per popup
        private List<string> _allFeaturedArtists = new List<string>();
        private HashSet<string> _checkedFeaturedArtists = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // ✅ VIDEO PREVIEW (RadioTV mode) – separate preview window
        private string _videoPath;  // resolved video file path (null if no video)
        private int _videoSyncTickCounter;  // rate-limiter for drift correction
        private VideoPreviewForm _videoPreviewForm;
        private Button _btnVideoPreview;

        // ✅ TOOLBAR – countup / countdown / filename labels (created programmatically)
        private Label _lblCountup;
        private Label _lblCountdown;
        private Label _lblOpenFileName;

        // ✅ Supported video file extensions (used in SetupVideoPreview)
        private static readonly string[] _videoFileExtensions =
        {
            ".mp4", ".avi", ".mkv", ".mov", ".wmv",
            ".ts", ".mts", ".m2ts", ".webm"
        };

        public MusicEditorForm(MusicEntry musicEntry, bool isClip = false)
        {
            InitializeComponent();

            _musicEntry = musicEntry;
            _isClip = isClip;

            // Backup marker originali per ripristino su Annulla
            _originalMarkerIN = musicEntry.MarkerIN;
            _originalMarkerINTRO = musicEntry.MarkerINTRO;
            _originalMarkerMIX = musicEntry.MarkerMIX;
            _originalMarkerOUT = musicEntry.MarkerOUT;

            if (_isClip)
            {
                _originalClipId = musicEntry.ID;
            }

            this.BackColor = AppTheme.BgDark;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            Console.WriteLine($"\n[MusicEditor] ========== APERTURA EDITOR ==========");
            Console.WriteLine($"[MusicEditor] ID: {_musicEntry.ID}");
            Console.WriteLine($"[MusicEditor] IsClip: {_isClip}");
            Console.WriteLine($"[MusicEditor] =======================================\n");

            _dtpValidFrom = dtpValidFrom;
            _dtpValidTo = dtpValidTo;
            _chkEnableValidFrom = chkEnableValidFrom;
            _chkEnableValidTo = chkEnableValidTo;

            _chkEnableValidFrom.CheckedChanged += (s, e) =>
            {
                _dtpValidFrom.Enabled = _chkEnableValidFrom.Checked;
            };

            _chkEnableValidTo.CheckedChanged += (s, e) =>
            {
                _dtpValidTo.Enabled = _chkEnableValidTo.Checked;
            };

            ApplyTheme();
            CreateResetButtons();
            CreateValidityControls();
            SetupZoomControls();
            SetupVolumeControls();
            SetupVuMeter();
            SetupVideoPreview();
            SetupToolbarLabels();
            LoadGenreSuggestions();
            LoadCategorySuggestions();
            LoadFeaturedArtistSuggestions();

            // ✅ APPLICA LINGUA
            ApplyLanguage();

            LoadMetadata();
            LoadAudioFile();

            _positionTimer = new System.Windows.Forms.Timer { Interval = 50 };
            _positionTimer.Tick += PositionTimer_Tick;

            // ✅ ASCOLTA CAMBIO LINGUA
            LanguageManager.LanguageChanged += (s, e) => ApplyLanguage();

            // ✅ Ripristina marker originali se l'utente annulla
            this.FormClosing += (s, e) =>
            {
                if (this.DialogResult != DialogResult.OK)
                {
                    _musicEntry.MarkerIN = _originalMarkerIN;
                    _musicEntry.MarkerINTRO = _originalMarkerINTRO;
                    _musicEntry.MarkerMIX = _originalMarkerMIX;
                    _musicEntry.MarkerOUT = _originalMarkerOUT;
                }
            };
        }

        #region ============ GENRE / CATEGORY SUGGESTIONS ============

        private void LoadGenreSuggestions()
        {
            try
            {
                _genreSuggestions = new AutoCompleteStringCollection();

                // ✅ Carica SOLO generi presenti in altri brani dell'archivio
                string dbcFile = _isClip ? "Clips.dbc" : "Music.dbc";
                var existingGenres = GetDistinctFieldValues(dbcFile, "Genre");

                foreach (var genre in existingGenres)
                {
                    if (!string.IsNullOrWhiteSpace(genre))
                    {
                        _genreSuggestions.Add(genre);
                    }
                }

                cmbGenre.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                cmbGenre.AutoCompleteSource = AutoCompleteSource.CustomSource;
                cmbGenre.AutoCompleteCustomSource = _genreSuggestions;

                // ✅ Popola dropdown SOLO con generi presenti nell'archivio
                cmbGenre.Items.Clear();
                var allGenres = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (string g in existingGenres)
                {
                    if (!string.IsNullOrWhiteSpace(g))
                        allGenres.Add(g);
                }
                cmbGenre.Items.AddRange(allGenres.OrderBy(g => g).ToArray());

                Console.WriteLine($"[MusicEditor] ✅ Caricati {allGenres.Count} generi da {dbcFile} (solo brani esistenti)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MusicEditor] ⚠️ Errore LoadGenreSuggestions: {ex.Message}");
                // ✅ Fallback: dropdown vuoto, l'utente può scrivere liberamente
                cmbGenre.Items.Clear();
            }
        }

        private void LoadCategorySuggestions()
        {
            try
            {
                string dbcFile = _isClip ? "Clips.dbc" : "Music.dbc";
                var existingCategories = GetDistinctFieldValues(dbcFile, "Categories");

                // Le categorie possono essere separate da virgola o punto e virgola
                var allCats = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var catField in existingCategories)
                {
                    if (string.IsNullOrWhiteSpace(catField)) continue;
                    var parts = catField.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in parts)
                    {
                        string trimmed = part.Trim();
                        if (!string.IsNullOrWhiteSpace(trimmed))
                            allCats.Add(trimmed);
                    }
                }

                // Aggiungi anche categorie da Categories.dbc
                try
                {
                    var categoryEntries = DbcManager.LoadFromCsv<CategoryEntry>("Categories.dbc");
                    foreach (var ce in categoryEntries)
                    {
                        if (!string.IsNullOrWhiteSpace(ce.CategoryName))
                            allCats.Add(ce.CategoryName.Trim());
                    }
                }
                catch { }

                _allCategories = allCats.OrderBy(c => c).ToList();

                Console.WriteLine($"[MusicEditor] ✅ Caricate {allCats.Count} categorie da {dbcFile}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MusicEditor] ⚠️ Errore LoadCategorySuggestions: {ex.Message}");
            }
        }

        private void UpdateCategoryDisplay()
        {
            txtCategoriesDisplay.Text = _checkedCategories.Count > 0
                ? string.Join("; ", _checkedCategories.OrderBy(c => c))
                : "";
        }

        private void ShowCategoryPopup()
        {
            var popup = new Form
            {
                FormBorderStyle = FormBorderStyle.FixedToolWindow,
                StartPosition = FormStartPosition.Manual,
                ShowInTaskbar = false,
                Text = LanguageManager.GetString("MusicEditor.Categories", "Categorie:"),
                Size = new Size(280, 260),
                BackColor = this.BackColor
            };

            var clb = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                CheckOnClick = true,
                Font = new Font("Segoe UI", 9F),
                BorderStyle = BorderStyle.None
            };

            // Popola con tutte le categorie (incluse quelle checked non presenti in _allCategories)
            var allItems = new HashSet<string>(_allCategories, StringComparer.OrdinalIgnoreCase);
            foreach (var c in _checkedCategories)
                allItems.Add(c);

            foreach (var cat in allItems.OrderBy(c => c))
            {
                int idx = clb.Items.Add(cat);
                if (_checkedCategories.Contains(cat))
                    clb.SetItemChecked(idx, true);
            }

            // Pannello in basso per aggiungere nuova categoria
            var addPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 36,
                BackColor = AppTheme.BgLight,
                Padding = new Padding(4, 4, 4, 4)
            };
            var txtNew = new TextBox
            {
                Location = new Point(4, 6),
                Size = new Size(168, 24),
                Font = new Font("Segoe UI", 9F),
                BackColor = AppTheme.Surface,
                ForeColor = AppTheme.TextPrimary
            };
            var btnAdd = new Button
            {
                Text = "+ " + LanguageManager.GetString("Download.Add", "Aggiungi"),
                Location = new Point(176, 4),
                Size = new Size(90, 26),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                BackColor = AppTheme.Primary,
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnAdd.FlatAppearance.BorderSize = 0;
            txtNew.KeyDown += (sk, ek) => { if (ek.KeyCode == Keys.Enter) { ek.SuppressKeyPress = true; btnAdd.PerformClick(); } };
            btnAdd.Click += (s2, e2) =>
            {
                string newCat = txtNew.Text.Trim();
                if (string.IsNullOrWhiteSpace(newCat)) return;

                bool exists = false;
                for (int i = 0; i < clb.Items.Count; i++)
                {
                    if (string.Equals(clb.Items[i].ToString(), newCat, StringComparison.OrdinalIgnoreCase))
                    {
                        clb.SetItemChecked(i, true);
                        exists = true;
                        break;
                    }
                }
                if (!exists)
                {
                    int idx = clb.Items.Add(newCat);
                    clb.SetItemChecked(idx, true);
                    if (!_allCategories.Contains(newCat))
                    {
                        _allCategories.Add(newCat);
                        PersistNewCategory(newCat);
                    }
                }
                txtNew.Text = "";
            };
            addPanel.Controls.Add(txtNew);
            addPanel.Controls.Add(btnAdd);

            popup.Controls.Add(clb);
            popup.Controls.Add(addPanel);

            // Posiziona sotto il TextBox
            var screenPos = txtCategoriesDisplay.PointToScreen(new Point(0, txtCategoriesDisplay.Height));
            popup.Location = screenPos;

            // Quando il popup si chiude, aggiorna i dati
            popup.FormClosed += (s2, e2) =>
            {
                _checkedCategories.Clear();
                for (int i = 0; i < clb.Items.Count; i++)
                {
                    if (clb.GetItemChecked(i))
                        _checkedCategories.Add(clb.Items[i].ToString());
                }
                UpdateCategoryDisplay();
            };

            popup.Show(this);
        }

        private void PersistNewCategory(string categoryName)
        {
            try
            {
                var existing = DbcManager.LoadFromCsv<CategoryEntry>("Categories.dbc");
                bool alreadyExists = existing.Any(c =>
                    string.Equals(c.CategoryName?.Trim(), categoryName, StringComparison.OrdinalIgnoreCase));

                if (!alreadyExists)
                {
                    DbcManager.Insert("Categories.dbc", new CategoryEntry
                    {
                        CategoryName = categoryName,
                        Color = "#607D8B",
                        IgnoreHourlySeparation = 0
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MusicEditor] ⚠️ Errore salvataggio nuova categoria: {ex.Message}");
            }
        }

        // ✅ ARTISTI SECONDARI – metodi

        private void LoadFeaturedArtistSuggestions()
        {
            try
            {
                var allArtists = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Da Artists.dbc (nomi canonici)
                try
                {
                    var artistEntries = DbcManager.LoadFromCsv<ArtistAliasEntry>("Artists.dbc");
                    foreach (var ae in artistEntries)
                    {
                        if (!string.IsNullOrWhiteSpace(ae.ArtistName))
                            allArtists.Add(ae.ArtistName.Trim());
                    }
                }
                catch { }

                // Da tutti i brani (artisti principali)
                if (!_isClip)
                {
                    try
                    {
                        var allMusic = DbcManager.LoadFromCsv<MusicEntry>("Music.dbc");
                        foreach (var m in allMusic)
                        {
                            if (!string.IsNullOrWhiteSpace(m.Artist))
                                allArtists.Add(m.Artist.Trim());
                            if (!string.IsNullOrWhiteSpace(m.FeaturedArtists))
                            {
                                foreach (var fa in m.FeaturedArtists.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                                {
                                    string trimmed = fa.Trim();
                                    if (!string.IsNullOrWhiteSpace(trimmed))
                                        allArtists.Add(trimmed);
                                }
                            }
                        }
                    }
                    catch { }
                }

                _allFeaturedArtists = allArtists.OrderBy(a => a).ToList();
                Console.WriteLine($"[MusicEditor] ✅ Caricati {_allFeaturedArtists.Count} artisti per suggerimenti feat.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MusicEditor] ⚠️ Errore LoadFeaturedArtistSuggestions: {ex.Message}");
            }
        }

        private void UpdateFeaturedArtistsDisplay()
        {
            txtFeaturedArtistsDisplay.Text = _checkedFeaturedArtists.Count > 0
                ? string.Join("; ", _checkedFeaturedArtists.OrderBy(a => a))
                : "";
        }

        private void ShowFeaturedArtistsPopup()
        {
            var popup = new Form
            {
                FormBorderStyle = FormBorderStyle.FixedToolWindow,
                StartPosition = FormStartPosition.Manual,
                ShowInTaskbar = false,
                Text = LanguageManager.GetString("MusicEditor.FeaturedArtists", "Artisti Feat.:"),
                Size = new Size(280, 300),
                BackColor = this.BackColor
            };

            var clb = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                CheckOnClick = true,
                Font = new Font("Segoe UI", 9F),
                BorderStyle = BorderStyle.None
            };

            // Popola con tutti gli artisti (inclusi quelli checked non presenti in _allFeaturedArtists)
            var allItems = new HashSet<string>(_allFeaturedArtists, StringComparer.OrdinalIgnoreCase);
            foreach (var a in _checkedFeaturedArtists)
                allItems.Add(a);

            // Rimuovi l'artista principale dalla lista
            string currentArtist = txtArtist.Text?.Trim() ?? "";
            if (!string.IsNullOrWhiteSpace(currentArtist))
                allItems.Remove(currentArtist);

            foreach (var artist in allItems.OrderBy(a => a))
            {
                int idx = clb.Items.Add(artist);
                if (_checkedFeaturedArtists.Contains(artist))
                    clb.SetItemChecked(idx, true);
            }

            // Pannello in basso per aggiungere nuovo artista
            var addPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 66,
                BackColor = AppTheme.BgLight,
                Padding = new Padding(4, 4, 4, 4)
            };
            var txtNew = new TextBox
            {
                Location = new Point(4, 6),
                Size = new Size(168, 24),
                Font = new Font("Segoe UI", 9F),
                BackColor = AppTheme.Surface,
                ForeColor = AppTheme.TextPrimary
            };
            var btnAdd = new Button
            {
                Text = "+ " + LanguageManager.GetString("Download.Add", "Aggiungi"),
                Location = new Point(176, 4),
                Size = new Size(90, 26),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                BackColor = AppTheme.Primary,
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnAdd.FlatAppearance.BorderSize = 0;

            // Bottone Auto-detect
            var btnAutoDetect = new Button
            {
                Text = "🔍 " + LanguageManager.GetString("MusicEditor.AutoDetect", "Auto-detect"),
                Location = new Point(4, 34),
                Size = new Size(262, 26),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                BackColor = AppTheme.BgLight,
                ForeColor = AppTheme.TextPrimary,
                Cursor = Cursors.Hand
            };
            btnAutoDetect.FlatAppearance.BorderSize = 1;

            txtNew.KeyDown += (sk, ek) => { if (ek.KeyCode == Keys.Enter) { ek.SuppressKeyPress = true; btnAdd.PerformClick(); } };
            btnAdd.Click += (s2, e2) =>
            {
                string newArtist = txtNew.Text.Trim();
                if (string.IsNullOrWhiteSpace(newArtist)) return;

                bool exists = false;
                for (int i = 0; i < clb.Items.Count; i++)
                {
                    if (string.Equals(clb.Items[i].ToString(), newArtist, StringComparison.OrdinalIgnoreCase))
                    {
                        clb.SetItemChecked(i, true);
                        exists = true;
                        break;
                    }
                }
                if (!exists)
                {
                    int idx = clb.Items.Add(newArtist);
                    clb.SetItemChecked(idx, true);
                    if (!_allFeaturedArtists.Contains(newArtist))
                    {
                        _allFeaturedArtists.Add(newArtist);
                        PersistNewArtistAlias(newArtist);
                    }
                }
                txtNew.Text = "";
            };

            btnAutoDetect.Click += (s2, e2) =>
            {
                try
                {
                    var aliases = DbcManager.LoadFromCsv<ArtistAliasEntry>("Artists.dbc");
                    var (_, detected) = ArtistParsingService.ParseArtists(
                        txtArtist.Text?.Trim() ?? "",
                        txtTitle.Text?.Trim() ?? "",
                        aliases);

                    foreach (var detectedArtist in detected)
                    {
                        bool found = false;
                        for (int i = 0; i < clb.Items.Count; i++)
                        {
                            if (string.Equals(clb.Items[i].ToString(), detectedArtist, StringComparison.OrdinalIgnoreCase))
                            {
                                clb.SetItemChecked(i, true);
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            int idx = clb.Items.Add(detectedArtist);
                            clb.SetItemChecked(idx, true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MusicEditor] ⚠️ Errore Auto-detect: {ex.Message}");
                }
            };

            addPanel.Controls.Add(txtNew);
            addPanel.Controls.Add(btnAdd);
            addPanel.Controls.Add(btnAutoDetect);

            popup.Controls.Add(clb);
            popup.Controls.Add(addPanel);

            // Posiziona sotto il TextBox
            var screenPos = txtFeaturedArtistsDisplay.PointToScreen(new Point(0, txtFeaturedArtistsDisplay.Height));
            popup.Location = screenPos;

            // Quando il popup si chiude, aggiorna i dati
            popup.FormClosed += (s2, e2) =>
            {
                _checkedFeaturedArtists.Clear();
                for (int i = 0; i < clb.Items.Count; i++)
                {
                    if (clb.GetItemChecked(i))
                        _checkedFeaturedArtists.Add(clb.Items[i].ToString());
                }
                UpdateFeaturedArtistsDisplay();
            };

            popup.Show(this);
        }

        private void PersistNewArtistAlias(string artistName)
        {
            try
            {
                var existing = DbcManager.LoadFromCsv<ArtistAliasEntry>("Artists.dbc");
                bool alreadyExists = existing.Any(a =>
                    string.Equals(a.ArtistName?.Trim(), artistName, StringComparison.OrdinalIgnoreCase));

                if (!alreadyExists)
                {
                    DbcManager.Insert("Artists.dbc", new ArtistAliasEntry
                    {
                        ArtistName = artistName,
                        Aliases = ""
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MusicEditor] ⚠️ Errore salvataggio nuovo artista: {ex.Message}");
            }
        }

        /// <summary>
        /// Legge tutti i valori distinti di un campo dal file DBC specificato.
        /// </summary>
        private List<string> GetDistinctFieldValues(string dbcFile, string fieldName)
        {
            var values = new List<string>();
            try
            {
                if (_isClip)
                {
                    var allClips = DbcManager.LoadFromCsv<ClipEntry>(dbcFile);
                    if (allClips != null)
                    {
                        foreach (var clip in allClips)
                        {
                            string val = fieldName == "Genre" ? clip.Genre : clip.Categories;
                            if (!string.IsNullOrWhiteSpace(val) && !values.Contains(val))
                                values.Add(val);
                        }
                    }
                }
                else
                {
                    var allMusic = DbcManager.LoadFromCsv<MusicEntry>(dbcFile);
                    if (allMusic != null)
                    {
                        foreach (var entry in allMusic)
                        {
                            string val = fieldName == "Genre" ? entry.Genre : entry.Categories;
                            if (!string.IsNullOrWhiteSpace(val) && !values.Contains(val))
                                values.Add(val);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MusicEditor] ⚠️ Errore lettura {dbcFile}/{fieldName}: {ex.Message}");
            }
            return values;
        }

        #endregion

        #region ============ ZOOM ============

        private void SetupZoomControls()
        {
            trkZoom.ValueChanged += TrkZoom_ValueChanged;
            hScrollWaveform.Scroll += HScrollWaveform_Scroll;

            // ✅ Mouse wheel zoom sulla waveform
            picWaveform.MouseWheel += PicWaveform_MouseWheel;
        }

        private void TrkZoom_ValueChanged(object sender, EventArgs e)
        {
            _zoomLevel = trkZoom.Value / 100f;
            lblZoomPercent.Text = $"{trkZoom.Value}%";
            UpdateScrollBarForZoom();
            RecreateWaveformBitmapWithBoost();
            picWaveform.Invalidate();
        }

        private void PicWaveform_MouseWheel(object sender, MouseEventArgs e)
        {
            if (_waveformData == null || _waveformData.Length == 0)
            {
                int delta0 = e.Delta > 0 ? 50 : -50;
                int newVal0 = Math.Max(trkZoom.Minimum, Math.Min(trkZoom.Maximum, trkZoom.Value + delta0));
                trkZoom.Value = newVal0;
                return;
            }

            _userScrolled = true;

            float oldZoom = _zoomLevel;
            int delta = e.Delta > 0 ? 50 : -50;
            int newVal = Math.Max(trkZoom.Minimum, Math.Min(trkZoom.Maximum, trkZoom.Value + delta));
            float newZoom = newVal / 100f;

            // Calculate the sample index under cursor before zoom
            int oldVisible = (int)(_waveformData.Length / oldZoom);
            oldVisible = Math.Max(1, Math.Min(oldVisible, _waveformData.Length));
            float cursorRatio = (float)e.X / picWaveform.Width;
            float sampleUnderCursor = _scrollOffset + cursorRatio * oldVisible;

            // Apply zoom
            _zoomLevel = newZoom;
            trkZoom.ValueChanged -= TrkZoom_ValueChanged;
            trkZoom.Value = newVal;
            trkZoom.ValueChanged += TrkZoom_ValueChanged;
            lblZoomPercent.Text = $"{newVal}%";

            // Adjust scroll offset so the sample under cursor stays at the same screen position
            int newVisible = (int)(_waveformData.Length / newZoom);
            newVisible = Math.Max(1, Math.Min(newVisible, _waveformData.Length));
            int newOffset = (int)(sampleUnderCursor - cursorRatio * newVisible);
            int maxOffset = Math.Max(0, _waveformData.Length - newVisible);
            _scrollOffset = Math.Max(0, Math.Min(newOffset, maxOffset));

            UpdateScrollBarForZoom();
            RecreateWaveformBitmapWithBoost();
            picWaveform.Invalidate();
        }

        private void HScrollWaveform_Scroll(object sender, ScrollEventArgs e)
        {
            if (_waveformData == null || _waveformData.Length == 0) return;

            _userScrolled = true;

            int visibleSamples = (int)(_waveformData.Length / _zoomLevel);
            int maxOffset = Math.Max(0, _waveformData.Length - visibleSamples);
            _scrollOffset = (int)((float)hScrollWaveform.Value / Math.Max(1, hScrollWaveform.Maximum - hScrollWaveform.LargeChange) * maxOffset);
            _scrollOffset = Math.Max(0, Math.Min(_scrollOffset, maxOffset));

            RecreateWaveformBitmapWithBoost();
            picWaveform.Invalidate();
        }

        private void UpdateScrollBarForZoom()
        {
            if (_zoomLevel <= 1.01f)
            {
                hScrollWaveform.Visible = false;
                _scrollOffset = 0;
            }
            else
            {
                hScrollWaveform.Visible = true;
                hScrollWaveform.Maximum = 1000;
                hScrollWaveform.LargeChange = Math.Max(1, (int)(1000 / _zoomLevel));

                // Mantieni posizione relativa
                if (_waveformData != null && _waveformData.Length > 0)
                {
                    int visibleSamples = (int)(_waveformData.Length / _zoomLevel);
                    int maxOffset = Math.Max(0, _waveformData.Length - visibleSamples);
                    float ratio = maxOffset > 0 ? (float)_scrollOffset / maxOffset : 0f;
                    hScrollWaveform.Value = (int)(ratio * (hScrollWaveform.Maximum - hScrollWaveform.LargeChange));
                }
            }
        }

        /// <summary>
        /// Converte una posizione in millisecondi in pixel X sullo schermo della waveform (con zoom e scroll).
        /// </summary>
        private float MsToPixelX(int ms, int totalMs)
        {
            if (totalMs == 0 || _waveformData == null || _waveformData.Length == 0) return 0;

            int visibleSamples = (int)(_waveformData.Length / _zoomLevel);
            visibleSamples = Math.Max(1, Math.Min(visibleSamples, _waveformData.Length));

            float sampleIndex = (float)ms / totalMs * _waveformData.Length;
            float visibleIndex = sampleIndex - _scrollOffset;
            float pixelX = visibleIndex / visibleSamples * picWaveform.Width;
            return pixelX;
        }

        /// <summary>
        /// Converte una posizione pixel X sullo schermo in millisecondi (con zoom e scroll).
        /// </summary>
        private int PixelXToMs(int pixelX, int totalMs)
        {
            if (totalMs == 0 || _waveformData == null || _waveformData.Length == 0) return 0;

            int visibleSamples = (int)(_waveformData.Length / _zoomLevel);
            visibleSamples = Math.Max(1, Math.Min(visibleSamples, _waveformData.Length));

            float sampleIndex = (float)pixelX / picWaveform.Width * visibleSamples + _scrollOffset;
            int ms = (int)(sampleIndex / _waveformData.Length * totalMs);
            return Math.Max(0, Math.Min(ms, totalMs));
        }

        #endregion

        #region ============ VOLUME BOOST (ffmpeg) ============

        private void SetupVolumeControls()
        {
            trkVolume.ValueChanged += TrkVolume_ValueChanged;
            btnApplyVolume.Click += BtnApplyVolume_Click;
        }

        private void ChkColoredPeaks_CheckedChanged(object sender, EventArgs e)
        {
            _showColoredPeaks = chkColoredPeaks.Checked;
            RecreateWaveformBitmapWithBoost();
            picWaveform.Invalidate();
        }

        private void TrkVolume_ValueChanged(object sender, EventArgs e)
        {
            _volumeBoostDb = trkVolume.Value;
            lblVolumeDb.Text = $"{(_volumeBoostDb >= 0 ? "+" : "")}{_volumeBoostDb} dB";

            // ✅ Colore dinamico
            if (_volumeBoostDb > 0)
                lblVolumeDb.ForeColor = Color.FromArgb(255, Math.Max(0, 255 - (int)(_volumeBoostDb * 12)), 0);
            else if (_volumeBoostDb < 0)
                lblVolumeDb.ForeColor = Color.FromArgb(0, Math.Max(100, 255 + (int)(_volumeBoostDb * 8)), 255);
            else
                lblVolumeDb.ForeColor = Color.Lime;

            // ✅ Preview visivo in tempo reale sulla waveform
            RecreateWaveformBitmapWithBoost();
            picWaveform.Invalidate();
        }

        /// <summary>
        /// Ricrea la bitmap della waveform applicando il boost visivo e lo zoom.
        /// </summary>
        private void RecreateWaveformBitmapWithBoost()
        {
            if (_originalWaveformData == null || _originalWaveformData.Length == 0) return;

            try
            {
                float linearGain = (float)Math.Pow(10.0, _volumeBoostDb / 20.0);

                // Applica gain ai dati
                _waveformData = new float[_originalWaveformData.Length];
                for (int i = 0; i < _originalWaveformData.Length; i++)
                {
                    _waveformData[i] = Math.Min(1.0f, _originalWaveformData[i] * linearGain);
                }

                // Calcola la porzione visibile (zoom + scroll)
                int visibleSamples = (int)(_waveformData.Length / _zoomLevel);
                visibleSamples = Math.Max(1, Math.Min(visibleSamples, _waveformData.Length));
                int startSample = Math.Max(0, Math.Min(_scrollOffset, _waveformData.Length - visibleSamples));

                int width = Math.Max(1, picWaveform.Width);
                int height = Math.Max(1, picWaveform.Height > 0 ? picWaveform.Height : 350);

                _waveformBitmap?.Dispose();
                _waveformBitmap = new Bitmap(width, height);

                using (Graphics g = Graphics.FromImage(_waveformBitmap))
                {
                    g.Clear(Color.FromArgb(10, 10, 10));
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.CompositingQuality = CompositingQuality.HighQuality;

                    int midY = height / 2;

                    for (int px = 0; px < width; px++)
                    {
                        // Mappa pixel -> sample nello zoom
                        int sampleIdx = startSample + (int)((float)px / width * visibleSamples);
                        sampleIdx = Math.Max(0, Math.Min(sampleIdx, _waveformData.Length - 1));

                        float amplitude = _waveformData[sampleIdx] * (height / 2) * 0.98f;

                        // Colore basato sull'ampiezza (solo se _showColoredPeaks è attivo)
                        Color colorTop, colorBottom;
                        if (_showColoredPeaks)
                        {
                            if (_waveformData[sampleIdx] >= 0.98f)
                            {
                                colorTop = Color.FromArgb(255, 40, 40);
                                colorBottom = Color.FromArgb(220, 30, 30);
                            }
                            else if (_waveformData[sampleIdx] >= 0.8f)
                            {
                                colorTop = Color.FromArgb(255, 200, 0);
                                colorBottom = Color.FromArgb(220, 170, 0);
                            }
                            else
                            {
                                colorTop = Color.FromArgb(0, 255, 100);
                                colorBottom = Color.FromArgb(0, 200, 80);
                            }
                        }
                        else
                        {
                            colorTop = Color.FromArgb(0, 200, 120);
                            colorBottom = Color.FromArgb(0, 160, 90);
                        }

                        using (Pen penTop = new Pen(colorTop, 1.2f))
                        using (Pen penBottom = new Pen(colorBottom, 1.2f))
                        {
                            g.DrawLine(penTop, px, midY, px, midY - amplitude);
                            g.DrawLine(penBottom, px, midY, px, midY + amplitude);
                        }
                    }

                    // Linea centrale
                    using (Pen penCenter = new Pen(Color.FromArgb(80, 80, 80), 1f))
                    {
                        g.DrawLine(penCenter, 0, midY, width, midY);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MusicEditor] ❌ Errore RecreateWaveformBitmapWithBoost: {ex.Message}");
            }
        }

        private void BtnApplyVolume_Click(object sender, EventArgs e)
        {
            if (_volumeBoostDb == 0)
            {
                MessageBox.Show(
                    LanguageManager.GetString("MusicEditor.VolumeNoChange", "The volume is at 0 dB, no changes to apply."),
                    LanguageManager.GetString("Common.Info", "Info"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
            if (!File.Exists(ffmpegPath))
            {
                MessageBox.Show(
                    LanguageManager.GetString("MusicEditor.FfmpegNotFound", "❌ ffmpeg.exe not found in application folder!"),
                    LanguageManager.GetString("Common.Error", "Error"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string sourceFile = _musicEntry.FilePath;
            if (!File.Exists(sourceFile))
            {
                MessageBox.Show(
                    LanguageManager.GetString("MusicEditor.FileNotFound", "❌ File not found:\n{0}"),
                    LanguageManager.GetString("Common.Error", "Error"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var result = MessageBox.Show(
                string.Format(
                    LanguageManager.GetString("MusicEditor.VolumeConfirm",
                    "Apply {0} dB to the file?\n\n{1}\n\n⚠️ This will overwrite the original file!"),
                    (_volumeBoostDb >= 0 ? "+" : "") + _volumeBoostDb, sourceFile),
                LanguageManager.GetString("MusicEditor.VolumeTitle", "🔊 Apply Gain Volume"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Information);

            if (result != DialogResult.Yes) return;

            // ✅ Salva i parametri pendenti: ffmpeg verrà eseguito al Salva del form
            _pendingVolumeApply = true;
            _pendingVolumeBoostDb = _volumeBoostDb;
            _pendingFfmpegPath = ffmpegPath;
            _pendingSourceFile = sourceFile;
        }

        #endregion

        #region ============ VU METER ============

        private void SetupVuMeter()
        {
            vuMeterPanel.Paint += VuMeterPanel_Paint;

            _vuDecayTimer = new System.Windows.Forms.Timer { Interval = 30 };
            _vuDecayTimer.Tick += VuDecayTimer_Tick;
            _vuDecayTimer.Start();
        }

        // ═════════════════════════════════════════════════════════════════
        // VIDEO PREVIEW (RadioTV mode only)
        // ═════════════════════════════════════════════════════════════════

        private void SetupVideoPreview()
        {
            // Only show video preview button in RadioTV mode
            if (!ConfigurationControl.IsRadioTVMode())
                return;

            // Check if the music entry has an associated video file
            string videoPath = _musicEntry.VideoFilePath;
            bool hasVideo = !string.IsNullOrEmpty(videoPath) && File.Exists(videoPath);

            // Also check if the main file itself is a video
            if (!hasVideo)
            {
                string ext = Path.GetExtension(_musicEntry.FilePath ?? "").ToLowerInvariant();
                if (Array.IndexOf(_videoFileExtensions, ext) >= 0)
                {
                    videoPath = _musicEntry.FilePath;
                    hasVideo = !string.IsNullOrEmpty(videoPath) && File.Exists(videoPath);
                }
            }

            // For clips/spots: look for a video file in the same folder with the same base name
            if (!hasVideo && !string.IsNullOrEmpty(_musicEntry.FilePath))
            {
                string dir = Path.GetDirectoryName(_musicEntry.FilePath);
                string baseName = Path.GetFileNameWithoutExtension(_musicEntry.FilePath);
                if (!string.IsNullOrEmpty(dir) && !string.IsNullOrEmpty(baseName))
                {
                    foreach (string vExt in _videoFileExtensions)
                    {
                        string candidate = Path.Combine(dir, baseName + vExt);
                        if (File.Exists(candidate))
                        {
                            videoPath = candidate;
                            hasVideo = true;
                            break;
                        }
                    }
                }
            }

            if (!hasVideo)
                return;

            _videoPath = videoPath;

            // Add VIDEO PREVIEW button to toolbar (right of grpVolume)
            _btnVideoPreview = new Button
            {
                Text = "📺 VIDEO\nPREVIEW",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 60, 120),
                ForeColor = Color.White,
                Size = new Size(100, 46),
                Location = new Point(grpVolume.Right + 10, 2),
                Cursor = Cursors.Hand
            };
            _btnVideoPreview.FlatAppearance.BorderSize = 1;
            _btnVideoPreview.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 200);
            _btnVideoPreview.Click += BtnVideoPreview_Click;
            toolbarPanel.Controls.Add(_btnVideoPreview);

            // In RadioTV mode, auto-open video preview solo se era aperto in precedenza
            bool videoPreviewWasOpen = true; // default: aperto
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AirDirector"))
                {
                    if (key != null)
                        videoPreviewWasOpen = Convert.ToInt32(key.GetValue("VideoPreviewOpen", 1)) != 0;
                }
            }
            catch { }

            if (videoPreviewWasOpen)
            {
                this.Shown += (s, ev) =>
                {
                    OpenVideoPreviewBottomLeft();
                };
            }
        }

        private void OpenVideoPreviewBottomLeft()
        {
            if (_videoPath == null) return;
            if (_videoPreviewForm != null && !_videoPreviewForm.IsDisposed) return;

            _videoPreviewForm = new VideoPreviewForm(_videoPath);
            _videoPreviewForm.StartPosition = FormStartPosition.Manual;
            var screen = Screen.FromControl(this);
            _videoPreviewForm.Location = new Point(
                screen.WorkingArea.Left,
                screen.WorkingArea.Bottom - _videoPreviewForm.Height);
            _videoPreviewForm.FormClosed += (s2, ev2) => _videoPreviewForm = null;
            _videoPreviewForm.Show(this);
        }

        // ═════════════════════════════════════════════════════════════════
        // TOOLBAR LABELS – countup, countdown, open file name
        // ═════════════════════════════════════════════════════════════════

        private void SetupToolbarLabels()
        {
            // ── Open file name – right of btnStop ──
            _lblOpenFileName = new Label
            {
                Text = Path.GetFileName(_musicEntry.FilePath ?? ""),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(btnStop.Right + 15, btnStop.Top + (btnStop.Height - 20) / 2),
                AutoEllipsis = true
            };
            toolbarPanel.Controls.Add(_lblOpenFileName);

            // ── Countup (elapsed time) – green, placed after the filename label ──
            _lblCountup = new Label
            {
                Text = "00:00",
                Font = new Font("Consolas", 11F, FontStyle.Bold),
                ForeColor = Color.Lime,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(btnStop.Right + 15, btnStop.Top + (btnStop.Height - 20) / 2 + 22)
            };
            toolbarPanel.Controls.Add(_lblCountup);

            // ── Countdown (remaining time) – orange/red, right of countup ──
            _lblCountdown = new Label
            {
                Text = "-00:00",
                Font = new Font("Consolas", 11F, FontStyle.Bold),
                ForeColor = Color.OrangeRed,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(btnStop.Right + 80, btnStop.Top + (btnStop.Height - 20) / 2 + 22)
            };
            toolbarPanel.Controls.Add(_lblCountdown);
        }

        /// <summary>Formats milliseconds as MM:SS (short format for countup/countdown).</summary>
        private string FormatTimeShort(int milliseconds)
        {
            if (milliseconds < 0) milliseconds = 0;
            TimeSpan ts = TimeSpan.FromMilliseconds(milliseconds);
            return $"{(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}";
        }

        private void BtnVideoPreview_Click(object sender, EventArgs e)
        {
            if (_videoPath == null) return;

            if (_videoPreviewForm != null && !_videoPreviewForm.IsDisposed)
            {
                // Already open – bring to front
                _videoPreviewForm.BringToFront();
                _videoPreviewForm.Focus();
                return;
            }

            _videoPreviewForm = new VideoPreviewForm(_videoPath);
            _videoPreviewForm.FormClosed += (s, ev) => _videoPreviewForm = null;
            _videoPreviewForm.Show(this);

            // Sync to current audio state
            if (_isPlaying)
            {
                _videoPreviewForm.SyncPlay();
                if (_audioReader != null)
                    _videoPreviewForm.SyncSeek((int)_audioReader.CurrentTime.TotalMilliseconds);
            }
        }

        private void VuDecayTimer_Tick(object sender, EventArgs e)
        {
            if (_isPlaying && _audioReader != null)
            {
                // ✅ Leggi livelli audio reali dalla posizione corrente
                try
                {
                    if (_waveformData != null && _waveformData.Length > 0)
                    {
                        int totalMs = (int)_audioReader.TotalTime.TotalMilliseconds;
                        int currentMs = (int)_audioReader.CurrentTime.TotalMilliseconds;

                        if (totalMs > 0)
                        {
                            int sampleIdx = (int)((float)currentMs / totalMs * _waveformData.Length);
                            sampleIdx = Math.Max(0, Math.Min(sampleIdx, _waveformData.Length - 1));

                            // Simula stereo con leggera variazione
                            float level = _waveformData[sampleIdx];
                            float variation = (float)(new Random().NextDouble() * 0.05 - 0.025);

                            float targetL = Math.Min(1.0f, level + variation);
                            float targetR = Math.Min(1.0f, level - variation);

                            // Smooth attack/release
                            _vuLevelLeft = _vuLevelLeft * 0.3f + targetL * 0.7f;
                            _vuLevelRight = _vuLevelRight * 0.3f + targetR * 0.7f;

                            // Peak hold
                            if (_vuLevelLeft > _vuPeakLeft) _vuPeakLeft = _vuLevelLeft;
                            else _vuPeakLeft = Math.Max(0, _vuPeakLeft - 0.005f);

                            if (_vuLevelRight > _vuPeakRight) _vuPeakRight = _vuLevelRight;
                            else _vuPeakRight = Math.Max(0, _vuPeakRight - 0.005f);
                        }
                    }
                }
                catch { }
            }
            else
            {
                // Decay quando non in play
                _vuLevelLeft = Math.Max(0, _vuLevelLeft - 0.03f);
                _vuLevelRight = Math.Max(0, _vuLevelRight - 0.03f);
                _vuPeakLeft = Math.Max(0, _vuPeakLeft - 0.008f);
                _vuPeakRight = Math.Max(0, _vuPeakRight - 0.008f);
            }

            if (vuMeterPanel.IsHandleCreated && !vuMeterPanel.IsDisposed)
            {
                vuMeterPanel.Invalidate();
            }
        }

        private void VuMeterPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;

            int w = vuMeterPanel.Width;
            int h = vuMeterPanel.Height;
            int barHeight = (h - 6) / 2; // 2 barre (L + R)
            int yL = 2;
            int yR = yL + barHeight + 2;

            g.Clear(Color.FromArgb(15, 15, 15));

            // ✅ Disegna barra sinistra (L)
            DrawVuBar(g, yL, barHeight, w, _vuLevelLeft, _vuPeakLeft, "L");

            // ✅ Disegna barra destra (R)
            DrawVuBar(g, yR, barHeight, w, _vuLevelRight, _vuPeakRight, "R");
        }

        private void DrawVuBar(Graphics g, int y, int barHeight, int totalWidth, float level, float peak, string channel)
        {
            int labelWidth = 14;
            int barX = labelWidth;
            int barW = totalWidth - labelWidth - 4;

            // Label canale
            using (Font f = new Font("Consolas", 7, FontStyle.Bold))
            using (SolidBrush b = new SolidBrush(Color.Gray))
            {
                g.DrawString(channel, f, b, 1, y);
            }

            // Background barra
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(25, 25, 25)))
            {
                g.FillRectangle(bg, barX, y, barW, barHeight);
            }

            // Segmenti colorati
            int levelWidth = (int)(level * barW);
            int greenEnd = (int)(barW * 0.6f);
            int yellowEnd = (int)(barW * 0.85f);

            if (levelWidth > 0)
            {
                // Green zone
                int greenW = Math.Min(levelWidth, greenEnd);
                if (greenW > 0)
                {
                    using (LinearGradientBrush brush = new LinearGradientBrush(
                        new Rectangle(barX, y, greenW, barHeight),
                        Color.FromArgb(0, 180, 0), Color.FromArgb(0, 255, 60), LinearGradientMode.Horizontal))
                    {
                        g.FillRectangle(brush, barX, y, greenW, barHeight);
                    }
                }

                // Yellow zone
                if (levelWidth > greenEnd)
                {
                    int yellowW = Math.Min(levelWidth - greenEnd, yellowEnd - greenEnd);
                    if (yellowW > 0)
                    {
                        using (LinearGradientBrush brush = new LinearGradientBrush(
                            new Rectangle(barX + greenEnd, y, yellowW, barHeight),
                            Color.FromArgb(255, 255, 0), Color.FromArgb(255, 180, 0), LinearGradientMode.Horizontal))
                        {
                            g.FillRectangle(brush, barX + greenEnd, y, yellowW, barHeight);
                        }
                    }
                }

                // Red zone
                if (levelWidth > yellowEnd)
                {
                    int redW = levelWidth - yellowEnd;
                    using (LinearGradientBrush brush = new LinearGradientBrush(
                        new Rectangle(barX + yellowEnd, y, Math.Max(1, redW), barHeight),
                        Color.FromArgb(255, 80, 0), Color.FromArgb(255, 0, 0), LinearGradientMode.Horizontal))
                    {
                        g.FillRectangle(brush, barX + yellowEnd, y, redW, barHeight);
                    }
                }
            }

            // ✅ Peak indicator (linea sottile)
            int peakX = barX + (int)(peak * barW);
            peakX = Math.Max(barX, Math.Min(peakX, barX + barW - 1));
            Color peakColor = peak >= 0.85f ? Color.Red : (peak >= 0.6f ? Color.Yellow : Color.Lime);
            using (Pen peakPen = new Pen(peakColor, 2f))
            {
                g.DrawLine(peakPen, peakX, y, peakX, y + barHeight);
            }

            // ✅ Graduazione dB (tacche)
            float[] dbMarks = { -30, -20, -10, -6, -3, 0 };
            using (Pen tickPen = new Pen(Color.FromArgb(80, 80, 80), 1f))
            using (Font tickFont = new Font("Consolas", 6))
            using (SolidBrush tickBrush = new SolidBrush(Color.FromArgb(100, 100, 100)))
            {
                foreach (float db in dbMarks)
                {
                    // dB to linear: 10^(dB/20), normalizzato su 0dB = 1.0
                    float linear = (float)Math.Pow(10.0, db / 20.0);
                    int tickX = barX + (int)(linear * barW);
                    if (tickX >= barX && tickX <= barX + barW)
                    {
                        g.DrawLine(tickPen, tickX, y, tickX, y + barHeight);
                    }
                }
            }
        }

        #endregion

        #region ============ LANGUAGE ============

        private void ApplyLanguage()
        {
            try
            {
                // ✅ TITOLO FINESTRA
                string windowTitle = _isClip
                    ? LanguageManager.GetString("MusicEditor.WindowTitleClip", "🎵 Editing Jingle: {0}")
                    : LanguageManager.GetString("MusicEditor.WindowTitle", "🎵 Editing: {0} - {1}");

                if (_isClip)
                {
                    this.Text = string.Format(windowTitle, _musicEntry.Title);
                }
                else
                {
                    this.Text = string.Format(windowTitle, _musicEntry.Artist, _musicEntry.Title);
                }

                // ✅ TOOLBAR
                btnPlay.Text = LanguageManager.GetString("MusicEditor.Play", "▶ PLAY");
                btnStop.Text = LanguageManager.GetString("MusicEditor.Stop", "⏹ STOP");
                btnLoop.Text = LanguageManager.GetString("MusicEditor.Loop", "🔁 LOOP");

                // ✅ MARKER LABELS
                lblMarkerInLabel.Text = LanguageManager.GetString("MusicEditor.MarkerIN", "IN");
                lblMarkerIntroLabel.Text = LanguageManager.GetString("MusicEditor.MarkerINTRO", "INTRO");
                lblMarkerMixLabel.Text = LanguageManager.GetString("MusicEditor.MarkerMIX", "MIX");
                lblMarkerOutLabel.Text = LanguageManager.GetString("MusicEditor.MarkerOUT", "OUT");

                // ✅ METADATA LABELS
                lblTitle.Text = LanguageManager.GetString("MusicEditor.Title", "Titolo:");
                lblArtist.Text = LanguageManager.GetString("MusicEditor.Artist", "Artista:");
                lblAlbum.Text = LanguageManager.GetString("MusicEditor.Album", "Album:");
                lblYear.Text = LanguageManager.GetString("MusicEditor.Year", "Anno:");
                lblGenre.Text = LanguageManager.GetString("MusicEditor.Genre", "Genere:");
                lblCategories.Text = LanguageManager.GetString("MusicEditor.Categories", "Categorie:");
                if (!_isClip) lblFeaturedArtists.Text = LanguageManager.GetString("MusicEditor.FeaturedArtists", "Artisti Feat.:");
                lblFilePath.Text = LanguageManager.GetString("MusicEditor.FilePath", "File Audio:");

                // ✅ GROUPBOX
                grpPeriod.Text = LanguageManager.GetString("MusicEditor.ValidityPeriod", "📅 Periodo Validità");
                grpMonths.Text = LanguageManager.GetString("MusicEditor.AllowedMonths", "📆 Mesi Consentiti");
                grpDays.Text = LanguageManager.GetString("MusicEditor.AllowedDays", "📅 Giorni Consentiti");
                grpHours.Text = LanguageManager.GetString("MusicEditor.AllowedHours", "🕐 Ore Consentite");

                // ✅ CHECKBOX VALIDITÀ
                chkEnableValidFrom.Text = LanguageManager.GetString("MusicEditor.ValidFrom", "Da");
                chkEnableValidTo.Text = LanguageManager.GetString("MusicEditor.ValidTo", "A");

                // ✅ BOTTONI
                btnSave.Text = LanguageManager.GetString("MusicEditor.Save", "💾 Salva");
                btnCancel.Text = LanguageManager.GetString("MusicEditor.Cancel", "✖ Annulla");

                // ✅ VOLUME
                grpVolume.Text = LanguageManager.GetString("MusicEditor.VolumeBoost", "🔊 Volume Boost");
                btnApplyVolume.Text = LanguageManager.GetString("MusicEditor.Apply", "APPLY");

                // ✅ AGGIORNA NOMI MESI
                UpdateMonthNames();

                // ✅ AGGIORNA NOMI GIORNI
                UpdateDayNames();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MusicEditor] Errore ApplyLanguage: {ex.Message}");
            }
        }

        private void UpdateMonthNames()
        {
            try
            {
                string[] monthKeys = {
                    "Common.MonthJan", "Common.MonthFeb", "Common.MonthMar", "Common.MonthApr",
                    "Common.MonthMay", "Common.MonthJun", "Common.MonthJul", "Common.MonthAug",
                    "Common.MonthSep", "Common.MonthOct", "Common.MonthNov", "Common.MonthDec"
                };

                string[] defaultMonths = { "Gen", "Feb", "Mar", "Apr", "Mag", "Giu", "Lug", "Ago", "Set", "Ott", "Nov", "Dic" };

                for (int i = 0; i < 12 && i < _chkMonths.Length; i++)
                {
                    if (_chkMonths[i] != null)
                    {
                        _chkMonths[i].Text = LanguageManager.GetString(monthKeys[i], defaultMonths[i]);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MusicEditor] Errore UpdateMonthNames: {ex.Message}");
            }
        }

        private void UpdateDayNames()
        {
            try
            {
                string[] dayKeys = {
                    "Common.DaySunday", "Common.DayMonday", "Common.DayTuesday", "Common.DayWednesday",
                    "Common.DayThursday", "Common.DayFriday", "Common.DaySaturday"
                };

                string[] defaultDays = { "Dom", "Lun", "Mar", "Mer", "Gio", "Ven", "Sab" };

                for (int i = 0; i < 7 && i < _chkDays.Length; i++)
                {
                    if (_chkDays[i] != null)
                    {
                        _chkDays[i].Text = LanguageManager.GetString(dayKeys[i], defaultDays[i]);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MusicEditor] Errore UpdateDayNames: {ex.Message}");
            }
        }

        #endregion

        #region ============ THEME ============

        private void ApplyTheme()
        {
            toolbarPanel.BackColor = Color.FromArgb(45, 45, 48);
            btnPlay.BackColor = Color.FromArgb(40, 160, 40);
            btnPlay.ForeColor = Color.White;
            btnStop.BackColor = Color.FromArgb(200, 50, 50);
            btnStop.ForeColor = Color.White;
            btnLoop.BackColor = Color.FromArgb(60, 60, 60);
            btnLoop.ForeColor = Color.White;

            leftPanel.BackColor = Color.FromArgb(30, 30, 30);
            lblCurrentPosition.BackColor = Color.Black;
            lblCurrentPosition.ForeColor = Color.Lime;
            lblCurrentPositionMs.BackColor = Color.Black;
            lblCurrentPositionMs.ForeColor = Color.Cyan;
            lblTotalDuration.BackColor = Color.Black;
            lblTotalDuration.ForeColor = Color.Orange;

            txtMarkerIn.BackColor = Color.Black;
            txtMarkerIn.ForeColor = Color.Red;
            txtMarkerIntro.BackColor = Color.Black;
            txtMarkerIntro.ForeColor = Color.Magenta;
            txtMarkerMix.BackColor = Color.Black;
            txtMarkerMix.ForeColor = Color.Yellow;
            txtMarkerOut.BackColor = Color.Black;
            txtMarkerOut.ForeColor = Color.FromArgb(255, 140, 0);

            picWaveform.BackColor = Color.Black;
            bottomPanel.BackColor = Color.FromArgb(240, 240, 240);

            btnSave.BackColor = AppTheme.Success;
            btnSave.ForeColor = Color.White;
            btnSave.FlatAppearance.BorderSize = 0;
            btnCancel.BackColor = AppTheme.Danger;
            btnCancel.ForeColor = Color.White;
            btnCancel.FlatAppearance.BorderSize = 0;
        }

        #endregion

        #region ============ RESET BUTTONS ============

        private void CreateResetButtons()
        {
            btnResetIn = new Button
            {
                Text = "✕",
                Location = new Point(345, 20),
                Size = new Size(25, 28),
                BackColor = Color.FromArgb(200, 50, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnResetIn.FlatAppearance.BorderSize = 0;
            btnResetIn.Click += (s, e) => ResetMarker("In");
            leftPanel.Controls.Add(btnResetIn);

            btnResetIntro = new Button
            {
                Text = "✕",
                Location = new Point(345, 60),
                Size = new Size(25, 28),
                BackColor = Color.FromArgb(200, 50, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnResetIntro.FlatAppearance.BorderSize = 0;
            btnResetIntro.Click += (s, e) => ResetMarker("Intro");
            leftPanel.Controls.Add(btnResetIntro);

            btnResetMix = new Button
            {
                Text = "✕",
                Location = new Point(345, 100),
                Size = new Size(25, 28),
                BackColor = Color.FromArgb(200, 50, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnResetMix.FlatAppearance.BorderSize = 0;
            btnResetMix.Click += (s, e) => ResetMarker("Mix");
            leftPanel.Controls.Add(btnResetMix);

            btnResetOut = new Button
            {
                Text = "✕",
                Location = new Point(345, 140),
                Size = new Size(25, 28),
                BackColor = Color.FromArgb(200, 50, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnResetOut.FlatAppearance.BorderSize = 0;
            btnResetOut.Click += (s, e) => ResetMarker("Out");
            leftPanel.Controls.Add(btnResetOut);
        }

        private void ResetMarker(string markerType)
        {
            int totalMs = _audioReader != null ? (int)_audioReader.TotalTime.TotalMilliseconds : 0;

            switch (markerType)
            {
                case "In":
                    _musicEntry.MarkerIN = 0;
                    txtMarkerIn.Text = FormatTime(0);
                    PlayFromMarker("In");
                    break;

                case "Intro":
                    _musicEntry.MarkerINTRO = 0;
                    txtMarkerIntro.Text = FormatTime(0);
                    PlayFromMarker("Intro");
                    break;

                case "Mix":
                    _musicEntry.MarkerMIX = totalMs;
                    txtMarkerMix.Text = FormatTime(totalMs);
                    _musicEntry.MarkerOUT = totalMs;
                    txtMarkerOut.Text = FormatTime(totalMs);
                    PlayFromMarker("Mix");
                    break;

                case "Out":
                    _musicEntry.MarkerOUT = totalMs;
                    txtMarkerOut.Text = FormatTime(totalMs);
                    PlayFromMarker("Out");
                    break;
            }

            picWaveform.Invalidate();
            Console.WriteLine($"[MusicEditor] ✅ Marker {markerType} resettato");
        }

        #endregion

        #region ============ VALIDITY CONTROLS ============

        private void CreateValidityControls()
        {
            _chkMonths = new CheckBox[12];
            string[] monthNames = { "Gen", "Feb", "Mar", "Apr", "Mag", "Giu", "Lug", "Ago", "Set", "Ott", "Nov", "Dic" };

            for (int i = 0; i < 12; i++)
            {
                _chkMonths[i] = new CheckBox
                {
                    Text = monthNames[i],
                    Location = new Point(8 + (i * 55), 20),
                    Size = new Size(48, 25),
                    Checked = true,
                    Appearance = Appearance.Button,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 8, FontStyle.Bold),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.DodgerBlue,
                    ForeColor = Color.Black
                };

                _chkMonths[i].CheckedChanged += (s, e) =>
                {
                    var chk = s as CheckBox;
                    chk.BackColor = chk.Checked ? Color.DodgerBlue : Color.LightGray;
                    chk.ForeColor = chk.Checked ? Color.Black : Color.DarkGray;
                };

                grpMonths.Controls.Add(_chkMonths[i]);
            }

            _chkDays = new CheckBox[7];
            string[] dayNames = { "Dom", "Lun", "Mar", "Mer", "Gio", "Ven", "Sab" };

            for (int i = 0; i < 7; i++)
            {
                _chkDays[i] = new CheckBox
                {
                    Text = dayNames[i],
                    Location = new Point(8 + (i * 68), 20),
                    Size = new Size(60, 25),
                    Checked = true,
                    Appearance = Appearance.Button,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 8, FontStyle.Bold),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.Green,
                    ForeColor = Color.Black
                };

                _chkDays[i].CheckedChanged += (s, e) =>
                {
                    var chk = s as CheckBox;
                    chk.BackColor = chk.Checked ? Color.Green : Color.LightGray;
                    chk.ForeColor = chk.Checked ? Color.Black : Color.DarkGray;
                };

                grpDays.Controls.Add(_chkDays[i]);
            }

            _chkHours = new CheckBox[24];

            for (int i = 0; i < 24; i++)
            {
                string hourText = string.Format("{0:D2}", i);

                _chkHours[i] = new CheckBox
                {
                    Text = hourText,
                    Location = new Point(8 + (i * 34), 18),
                    Size = new Size(30, 22),
                    Checked = true,
                    Appearance = Appearance.Button,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 8, FontStyle.Bold),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.Purple,
                    ForeColor = Color.Black
                };

                _chkHours[i].CheckedChanged += (s, e) =>
                {
                    var chk = s as CheckBox;
                    chk.BackColor = chk.Checked ? Color.Purple : Color.LightGray;
                    chk.ForeColor = chk.Checked ? Color.Black : Color.DarkGray;
                };

                grpHours.Controls.Add(_chkHours[i]);
            }
        }

        #endregion

        #region ============ LOAD METADATA / AUDIO ============

        private void LoadMetadata()
        {
            txtMarkerIn.Text = FormatTime(_musicEntry.MarkerIN);
            txtMarkerIntro.Text = FormatTime(_musicEntry.MarkerINTRO);
            txtMarkerMix.Text = FormatTime(_musicEntry.MarkerMIX);
            txtMarkerOut.Text = FormatTime(_musicEntry.MarkerOUT);

            txtTitle.Text = _musicEntry.Title ?? "";
            txtArtist.Text = _musicEntry.Artist ?? "";
            txtFilePath.Text = _musicEntry.FilePath ?? "";
            txtAlbum.Text = _musicEntry.Album ?? "";
            numYear.Value = _musicEntry.Year > 0 ? _musicEntry.Year : DateTime.Now.Year;
            cmbGenre.Text = _musicEntry.Genre ?? "";

            // Popola le categorie selezionate dai dati del brano
            _checkedCategories.Clear();
            string currentCats = _musicEntry.Categories ?? "";
            foreach (var part in currentCats.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string trimmed = part.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                    _checkedCategories.Add(trimmed);
            }
            UpdateCategoryDisplay();

            // Popola gli artisti secondari
            _checkedFeaturedArtists.Clear();
            if (!_isClip)
            {
                string currentFeat = _musicEntry.FeaturedArtists ?? "";
                foreach (var part in currentFeat.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string trimmed = part.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmed))
                        _checkedFeaturedArtists.Add(trimmed);
                }
                UpdateFeaturedArtistsDisplay();
            }

            LoadValidityData();
        }

        private void LoadValidityData()
        {
            if (!string.IsNullOrEmpty(_musicEntry.ValidFrom) &&
                DateTime.TryParse(_musicEntry.ValidFrom, out DateTime validFrom) &&
                validFrom.Year > 1900)
            {
                _chkEnableValidFrom.Checked = true;
                _dtpValidFrom.Value = validFrom;
            }

            if (!string.IsNullOrEmpty(_musicEntry.ValidTo) &&
                DateTime.TryParse(_musicEntry.ValidTo, out DateTime validTo) &&
                validTo.Year > 1900)
            {
                _chkEnableValidTo.Checked = true;
                _dtpValidTo.Value = validTo;
            }

            if (!string.IsNullOrEmpty(_musicEntry.ValidMonths))
            {
                var months = _musicEntry.ValidMonths.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                         .Select(m => int.TryParse(m.Trim(), out int month) ? month : 0)
                                         .Where(m => m > 0 && m <= 12)
                                         .ToList();

                for (int i = 0; i < 12; i++)
                {
                    _chkMonths[i].Checked = months.Contains(i + 1);
                }
            }

            if (!string.IsNullOrEmpty(_musicEntry.ValidDays))
            {
                var days = _musicEntry.ValidDays.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                      .Select(d => d.Trim())
                                      .ToList();
                string[] dayMap = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

                for (int i = 0; i < 7; i++)
                {
                    _chkDays[i].Checked = days.Contains(dayMap[i]);
                }
            }

            if (!string.IsNullOrEmpty(_musicEntry.ValidHours))
            {
                var hours = _musicEntry.ValidHours.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(h => int.TryParse(h.Trim(), out int hour) ? hour : -1)
                                        .Where(h => h >= 0 && h < 24)
                                        .ToList();

                for (int i = 0; i < 24; i++)
                {
                    _chkHours[i].Checked = hours.Contains(i);
                }
            }
        }

        private async void LoadAudioFile()
        {
            try
            {
                if (!File.Exists(_musicEntry.FilePath))
                {
                    string errorMsg = LanguageManager.GetString("MusicEditor.FileNotFound", "❌ File non trovato:\n{0}");
                    string errorTitle = LanguageManager.GetString("Common.Error", "Errore");
                    MessageBox.Show(string.Format(errorMsg, _musicEntry.FilePath), errorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                _audioReader = new AudioFileReader(_musicEntry.FilePath);
                _waveOut = new WaveOutEvent();

                _waveOut.PlaybackStopped += (s, e) =>
                {
                    if (this.IsHandleCreated && !this.IsDisposed)
                    {
                        this.Invoke(new Action(() =>
                        {
                            _isPlaying = false;
                            _positionTimer.Stop();
                            btnPlay.Text = LanguageManager.GetString("MusicEditor.Play", "▶ PLAY");
                            btnPlay.BackColor = Color.FromArgb(40, 160, 40);
                            _audioReader.Position = 0;
                            lblCurrentPosition.Text = "00:00:00.000";
                            lblCurrentPositionMs.Text = "0 ms";
                            picWaveform.Invalidate();
                        }));
                    }
                };

                int previewDeviceNumber = ConfigurationControl.GetPreviewOutputDeviceNumber();

                if (previewDeviceNumber >= 0 && previewDeviceNumber < WaveOut.DeviceCount)
                {
                    _waveOut.DeviceNumber = previewDeviceNumber;
                }
                else
                {
                    _waveOut.DeviceNumber = -1;
                }

                _waveOut.Init(_audioReader);

                lblTotalDuration.Text = FormatTime((int)_audioReader.TotalTime.TotalMilliseconds);

                if (_musicEntry.MarkerOUT == 0)
                {
                    _musicEntry.MarkerOUT = (int)_audioReader.TotalTime.TotalMilliseconds;
                }

                txtMarkerOut.Text = FormatTime(_musicEntry.MarkerOUT);

                await GenerateWaveformAsync(_musicEntry.FilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MusicEditor] ❌ Errore caricamento audio: {ex.Message}");
                if (this.IsHandleCreated && !this.IsDisposed)
                {
                    string errorMsg = LanguageManager.GetString("MusicEditor.LoadError", "❌ Errore caricamento audio:\n{0}");
                    string errorTitle = LanguageManager.GetString("Common.Error", "Errore");
                    MessageBox.Show(string.Format(errorMsg, ex.Message), errorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        #endregion

        #region ============ WAVEFORM GENERATION ============

        private async Task GenerateWaveformAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || _isLoadingWaveform)
                return;

            if (_lastLoadedFile == filePath && _waveformBitmap != null)
                return;

            _isLoadingWaveform = true;
            _lastLoadedFile = filePath;

            _waveformBitmap?.Dispose();
            _waveformBitmap = null;

            // ✅ FASE 1: Preview veloce (800 campioni)
            await Task.Run(() =>
            {
                try
                {
                    using (var reader = new AudioFileReader(filePath))
                    {
                        var format = reader.WaveFormat;
                        long totalSamples = reader.Length / (format.BitsPerSample / 8);
                        int quickSamples = 800;
                        long samplesPerPoint = totalSamples / quickSamples;
                        long bytesPerPoint = samplesPerPoint * (format.BitsPerSample / 8);

                        float[] quickData = new float[quickSamples];
                        var sampleProvider = reader.ToSampleProvider();
                        float[] buffer = new float[8192];

                        Parallel.For(0, quickSamples, new ParallelOptions { MaxDegreeOfParallelism = 4 }, i =>
                        {
                            try
                            {
                                long targetByte = i * bytesPerPoint;
                                if (targetByte < reader.Length - buffer.Length * (format.BitsPerSample / 8))
                                {
                                    lock (reader)
                                    {
                                        reader.Position = targetByte;
                                        int samplesRead = sampleProvider.Read(buffer, 0, buffer.Length);
                                        float max = 0f;
                                        for (int j = 0; j < samplesRead; j++)
                                        {
                                            float sample = Math.Abs(buffer[j]);
                                            if (sample > max) max = sample;
                                        }
                                        quickData[i] = max;
                                    }
                                }
                            }
                            catch { quickData[i] = 0f; }
                        });

                        _originalWaveformData = quickData;
                        _waveformData = (float[])quickData.Clone();
                        RecreateWaveformBitmapWithBoost();
                    }
                }
                catch { _waveformBitmap = null; }
            });

            if (this.IsHandleCreated && !this.IsDisposed)
            {
                this.Invoke(new Action(() => picWaveform.Invalidate()));
            }

            // ✅ FASE 2: Waveform completa ad alta risoluzione
            await Task.Run(() =>
            {
                try
                {
                    using (var reader = new AudioFileReader(filePath))
                    {
                        var format = reader.WaveFormat;
                        long totalSamples = reader.Length / (format.BitsPerSample / 8);
                        long samplesPerPoint = totalSamples / _waveformSamples;
                        long bytesPerPoint = samplesPerPoint * (format.BitsPerSample / 8);

                        float[] fullData = new float[_waveformSamples];
                        var sampleProvider = reader.ToSampleProvider();
                        float[] buffer = new float[8192];

                        int batchSize = 50;
                        int batches = (_waveformSamples + batchSize - 1) / batchSize;

                        Parallel.For(0, batches, new ParallelOptions { MaxDegreeOfParallelism = 4 }, batchIndex =>
                        {
                            int start = batchIndex * batchSize;
                            int end = Math.Min(start + batchSize, _waveformSamples);

                            for (int i = start; i < end; i++)
                            {
                                try
                                {
                                    long targetByte = i * bytesPerPoint;
                                    if (targetByte < reader.Length - buffer.Length * (format.BitsPerSample / 8))
                                    {
                                        lock (reader)
                                        {
                                            reader.Position = targetByte;
                                            int samplesRead = sampleProvider.Read(buffer, 0, buffer.Length);
                                            float max = 0f;
                                            for (int j = 0; j < samplesRead; j++)
                                            {
                                                float sample = Math.Abs(buffer[j]);
                                                if (sample > max) max = sample;
                                            }
                                            fullData[i] = max;
                                        }
                                    }
                                }
                                catch { fullData[i] = 0f; }
                            }
                        });

                        _originalWaveformData = fullData;
                        _waveformData = (float[])fullData.Clone();
                        RecreateWaveformBitmapWithBoost();
                    }
                }
                catch { }
                finally
                {
                    _isLoadingWaveform = false;
                }
            });

            if (this.IsHandleCreated && !this.IsDisposed)
            {
                this.Invoke(new Action(() => picWaveform.Invalidate()));
            }
        }

        #endregion

        #region ============ TIME FORMATTING ============

        private string FormatTime(int milliseconds)
        {
            TimeSpan ts = TimeSpan.FromMilliseconds(Math.Abs(milliseconds));
            return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
        }

        private int ParseTime(string timeString)
        {
            try
            {
                string[] parts = timeString.Split(':');
                int hours = int.Parse(parts[0]);
                int minutes = int.Parse(parts[1]);
                string[] secParts = parts[2].Split('.');
                int seconds = int.Parse(secParts[0]);
                int milliseconds = int.Parse(secParts[1]);
                return (hours * 3600000) + (minutes * 60000) + (seconds * 1000) + milliseconds;
            }
            catch
            {
                return 0;
            }
        }

        #endregion

        #region ============ PLAYBACK CONTROLS ============

        private void btnPlay_Click(object sender, EventArgs e)
        {
            if (_isPlaying)
            {
                _waveOut?.Pause();
                _isPlaying = false;
                _positionTimer.Stop();
                btnPlay.Text = LanguageManager.GetString("MusicEditor.Play", "▶ PLAY");
                btnPlay.BackColor = Color.FromArgb(40, 160, 40);

                // Pause video preview
                SyncVideoPause();
            }
            else
            {
                // If stopped (position at 0 or before IN), seek to IN marker first
                if (_audioReader != null && _musicEntry.MarkerIN > 0 && _audioReader.CurrentTime.TotalMilliseconds < _musicEntry.MarkerIN)
                {
                    _audioReader.CurrentTime = TimeSpan.FromMilliseconds(_musicEntry.MarkerIN);
                    SyncVideoSeek(_musicEntry.MarkerIN);
                }

                _waveOut?.Play();
                _isPlaying = true;
                _positionTimer.Start();
                btnPlay.Text = "❚❚ PAUSE";
                btnPlay.BackColor = Color.FromArgb(200, 100, 0);

                // Sync and play video preview
                SyncVideoPlay();
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            _waveOut?.Stop();
            _audioReader.Position = 0;
            _isPlaying = false;
            _positionTimer.Stop();
            _userScrolled = false;
            btnPlay.Text = LanguageManager.GetString("MusicEditor.Play", "▶ PLAY");
            btnPlay.BackColor = Color.FromArgb(40, 160, 40);
            lblCurrentPosition.Text = "00:00:00.000";
            lblCurrentPositionMs.Text = "0 ms";

            // Reset countup / countdown
            if (_lblCountup != null)   _lblCountup.Text   = "00:00";
            if (_lblCountdown != null) _lblCountdown.Text = "-00:00";

            picWaveform.Invalidate();

            // Stop and reset video preview
            SyncVideoStop();
        }

        // ═════════════════════════════════════════════════════════════════
        // VIDEO PREVIEW – sync helpers
        // ═════════════════════════════════════════════════════════════════

        /// <summary>Seek video to the given audio position in milliseconds.</summary>
        private void SyncVideoSeek(int audioMs)
        {
            if (_videoPreviewForm == null || _videoPreviewForm.IsDisposed || _videoPath == null) return;
            try
            {
                _videoPreviewForm.SyncSeek(audioMs);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MusicEditor] ⚠️ Video seek error: {ex.Message}");
            }
        }

        /// <summary>Resume/start video playback synced to current audio position.</summary>
        private void SyncVideoPlay()
        {
            if (_videoPreviewForm == null || _videoPreviewForm.IsDisposed || _videoPath == null) return;
            try
            {
                _videoPreviewForm.SyncPlay();
                // Sync position to audio
                if (_audioReader != null)
                {
                    int audioMs = (int)_audioReader.CurrentTime.TotalMilliseconds;
                    SyncVideoSeek(audioMs);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MusicEditor] ⚠️ Video play error: {ex.Message}");
            }
        }

        /// <summary>Pause video playback.</summary>
        private void SyncVideoPause()
        {
            if (_videoPreviewForm == null || _videoPreviewForm.IsDisposed || _videoPath == null) return;
            try
            {
                _videoPreviewForm.SyncPause();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MusicEditor] ⚠️ Video pause error: {ex.Message}");
            }
        }

        /// <summary>Stop video and seek back to beginning.</summary>
        private void SyncVideoStop()
        {
            if (_videoPreviewForm == null || _videoPreviewForm.IsDisposed || _videoPath == null) return;
            try
            {
                _videoPreviewForm.SyncStop();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MusicEditor] ⚠️ Video stop error: {ex.Message}");
            }
        }

        private void btnSetMarkerIn_Click(object sender, EventArgs e) => SetMarkerToCurrent("In");
        private void btnSetMarkerIntro_Click(object sender, EventArgs e) => SetMarkerToCurrent("Intro");
        private void btnSetMarkerMix_Click(object sender, EventArgs e) => SetMarkerToCurrent("Mix");
        private void btnSetMarkerOut_Click(object sender, EventArgs e) => SetMarkerToCurrent("Out");

        private void btnMarkerInUp_Click(object sender, EventArgs e)
        {
            _musicEntry.MarkerIN += 10;
            txtMarkerIn.Text = FormatTime(_musicEntry.MarkerIN);
            picWaveform.Invalidate();
            PlayFromMarker("In");
        }

        private void btnMarkerInDown_Click(object sender, EventArgs e)
        {
            _musicEntry.MarkerIN = Math.Max(0, _musicEntry.MarkerIN - 10);
            txtMarkerIn.Text = FormatTime(_musicEntry.MarkerIN);
            picWaveform.Invalidate();
            PlayFromMarker("In");
        }

        private void btnMarkerIntroUp_Click(object sender, EventArgs e)
        {
            _musicEntry.MarkerINTRO += 10;
            txtMarkerIntro.Text = FormatTime(_musicEntry.MarkerINTRO);
            picWaveform.Invalidate();
            PlayFromMarker("Intro");
        }

        private void btnMarkerIntroDown_Click(object sender, EventArgs e)
        {
            _musicEntry.MarkerINTRO = Math.Max(0, _musicEntry.MarkerINTRO - 10);
            txtMarkerIntro.Text = FormatTime(_musicEntry.MarkerINTRO);
            picWaveform.Invalidate();
            PlayFromMarker("Intro");
        }

        private void btnMarkerMixUp_Click(object sender, EventArgs e)
        {
            int offset = _musicEntry.MarkerOUT - _musicEntry.MarkerMIX;
            _musicEntry.MarkerMIX += 10;
            txtMarkerMix.Text = FormatTime(_musicEntry.MarkerMIX);
            _musicEntry.MarkerOUT = _musicEntry.MarkerMIX + offset;
            txtMarkerOut.Text = FormatTime(_musicEntry.MarkerOUT);
            picWaveform.Invalidate();
            PlayFromMarker("Mix");
        }

        private void btnMarkerMixDown_Click(object sender, EventArgs e)
        {
            int offset = _musicEntry.MarkerOUT - _musicEntry.MarkerMIX;
            _musicEntry.MarkerMIX = Math.Max(0, _musicEntry.MarkerMIX - 10);
            txtMarkerMix.Text = FormatTime(_musicEntry.MarkerMIX);
            _musicEntry.MarkerOUT = _musicEntry.MarkerMIX + offset;
            txtMarkerOut.Text = FormatTime(_musicEntry.MarkerOUT);
            picWaveform.Invalidate();
            PlayFromMarker("Mix");
        }

        private void btnMarkerOutUp_Click(object sender, EventArgs e)
        {
            _musicEntry.MarkerOUT += 10;
            txtMarkerOut.Text = FormatTime(_musicEntry.MarkerOUT);
            picWaveform.Invalidate();
            PlayFromMarker("Out");
        }

        private void btnMarkerOutDown_Click(object sender, EventArgs e)
        {
            _musicEntry.MarkerOUT = Math.Max(_musicEntry.MarkerMIX, _musicEntry.MarkerOUT - 10);
            txtMarkerOut.Text = FormatTime(_musicEntry.MarkerOUT);
            picWaveform.Invalidate();
            PlayFromMarker("Out");
        }

        private void btnPlayFromIn_Click(object sender, EventArgs e) => PlayFromMarker("In");
        private void btnPlayFromIntro_Click(object sender, EventArgs e) => PlayFromMarker("Intro");
        private void btnPlayFromMix_Click(object sender, EventArgs e) => PlayFromMarker("Mix");
        private void btnPlayFromOut_Click(object sender, EventArgs e) => PlayFromMarker("Out");

        private void SetMarkerToCurrent(string markerType)
        {
            if (_audioReader == null) return;

            int currentMs = (int)_audioReader.CurrentTime.TotalMilliseconds;

            switch (markerType)
            {
                case "In":
                    _musicEntry.MarkerIN = currentMs;
                    txtMarkerIn.Text = FormatTime(currentMs);
                    break;
                case "Intro":
                    _musicEntry.MarkerINTRO = currentMs;
                    txtMarkerIntro.Text = FormatTime(currentMs);
                    break;
                case "Mix":
                    int offset = _musicEntry.MarkerOUT - _musicEntry.MarkerMIX;
                    _musicEntry.MarkerMIX = currentMs;
                    txtMarkerMix.Text = FormatTime(currentMs);
                    _musicEntry.MarkerOUT = currentMs + offset;
                    txtMarkerOut.Text = FormatTime(_musicEntry.MarkerOUT);
                    break;
                case "Out":
                    _musicEntry.MarkerOUT = Math.Max(currentMs, _musicEntry.MarkerMIX);
                    txtMarkerOut.Text = FormatTime(_musicEntry.MarkerOUT);
                    break;
            }

            picWaveform.Invalidate();
        }

        private void PlayFromMarker(string markerType)
        {
            if (_audioReader == null) return;

            int ms = 0;

            switch (markerType.ToUpperInvariant())
            {
                case "IN":
                    ms = _musicEntry.MarkerIN;
                    break;
                case "INTRO":
                    ms = _musicEntry.MarkerINTRO;
                    break;
                case "MIX":
                    ms = _musicEntry.MarkerMIX;
                    break;
                case "OUT":
                    ms = _musicEntry.MarkerOUT;
                    break;
            }

            _audioReader.CurrentTime = TimeSpan.FromMilliseconds(ms);

            // Sync video to this marker position
            SyncVideoSeek(ms);

            if (!_isPlaying)
            {
                btnPlay_Click(null, null);
            }
        }

        private void PositionTimer_Tick(object sender, EventArgs e)
        {
            if (_audioReader != null)
            {
                int currentMs = (int)_audioReader.CurrentTime.TotalMilliseconds;
                int totalMs = (int)_audioReader.TotalTime.TotalMilliseconds;
                lblCurrentPosition.Text = FormatTime(currentMs);
                lblCurrentPositionMs.Text = $"{currentMs} ms";

                // ✅ Update countup / countdown labels
                if (_lblCountup != null)
                    _lblCountup.Text = FormatTimeShort(currentMs);
                if (_lblCountdown != null)
                    _lblCountdown.Text = "-" + FormatTimeShort(Math.Max(0, totalMs - currentMs));

                // ✅ Periodic video sync: re-align if drift > 500ms (check every ~500ms)
                if (_videoPreviewForm != null && !_videoPreviewForm.IsDisposed && _isPlaying)
                {
                    _videoSyncTickCounter++;
                    if (_videoSyncTickCounter >= 10) // timer is 50ms, so 10 ticks = 500ms
                    {
                        _videoSyncTickCounter = 0;
                        SyncVideoSeek(currentMs);
                    }
                }

                // ✅ Auto-scroll zoom: centra la posizione corrente se fuori dalla vista
                // Solo se l'utente non ha scrollato manualmente
                if (!_userScrolled && _zoomLevel > 1.01f && _waveformData != null && _waveformData.Length > 0)
                {
                    int visibleSamples = (int)(_waveformData.Length / _zoomLevel);
                    int currentSample = (int)((float)currentMs / totalMs * _waveformData.Length);

                    if (currentSample < _scrollOffset || currentSample > _scrollOffset + visibleSamples)
                    {
                        _scrollOffset = Math.Max(0, currentSample - visibleSamples / 4);
                        int maxOffset = Math.Max(0, _waveformData.Length - visibleSamples);
                        _scrollOffset = Math.Min(_scrollOffset, maxOffset);

                        // Aggiorna scrollbar
                        float ratio = maxOffset > 0 ? (float)_scrollOffset / maxOffset : 0f;
                        hScrollWaveform.Value = (int)(ratio * (hScrollWaveform.Maximum - hScrollWaveform.LargeChange));

                        RecreateWaveformBitmapWithBoost();
                    }
                }

                picWaveform.Invalidate();
            }
        }

        #endregion

        #region ============ WAVEFORM PAINT & INTERACTION ============

        private void picWaveform_Paint(object sender, PaintEventArgs e)
        {
            if (_waveformBitmap != null)
            {
                e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.DrawImage(_waveformBitmap, 0, 0, picWaveform.Width, picWaveform.Height);
            }

            if (_audioReader != null)
            {
                int totalMs = (int)_audioReader.TotalTime.TotalMilliseconds;

                // ✅ Usa MsToPixelX per posizionamento coerente con zoom
                DrawMarkerWithLabelZoomed(e.Graphics, _musicEntry.MarkerIN, totalMs, Color.FromArgb(255, 50, 50), "IN", 3, false);
                DrawMarkerWithLabelZoomed(e.Graphics, _musicEntry.MarkerINTRO, totalMs, Color.FromArgb(255, 0, 255), "INTRO", picWaveform.Height / 2, false);
                DrawMarkerWithLabelZoomed(e.Graphics, _musicEntry.MarkerMIX, totalMs, Color.FromArgb(255, 255, 0), "MIX", 3, true);
                DrawMarkerWithLabelZoomed(e.Graphics, _musicEntry.MarkerOUT, totalMs, Color.FromArgb(255, 140, 0), "OUT", picWaveform.Height - 25, true);

                // ✅ Cursore posizione corrente
                int currentMs = (int)_audioReader.CurrentTime.TotalMilliseconds;
                float xPos = MsToPixelX(currentMs, totalMs);
                if (xPos >= 0 && xPos <= picWaveform.Width)
                {
                    using (Pen pen = new Pen(Color.White, 2.5f))
                    {
                        e.Graphics.DrawLine(pen, xPos, 0, xPos, picWaveform.Height);
                    }
                }
            }
        }

        /// <summary>
        /// Disegna un marker con label usando le coordinate zoom-aware.
        /// </summary>
        private void DrawMarkerWithLabelZoomed(Graphics g, int markerMs, int totalMs, Color color, string label, int labelYPos, bool labelOnLeft)
        {
            if (totalMs == 0) return;

            float xPos = MsToPixelX(markerMs, totalMs);

            // Disegna solo se visibile
            if (xPos < -50 || xPos > picWaveform.Width + 50) return;

            using (Pen pen = new Pen(color, 2.5f))
            {
                g.DrawLine(pen, xPos, 0, xPos, picWaveform.Height);
            }

            if (!string.IsNullOrEmpty(label))
            {
                using (Font font = new Font("Segoe UI", 8, FontStyle.Bold))
                {
                    SizeF textSize = g.MeasureString(label, font);
                    int labelWidth = (int)textSize.Width + 8;
                    int labelHeight = (int)textSize.Height + 4;

                    float labelX = labelOnLeft ? xPos - labelWidth - 5 : xPos + 5;

                    // Clamp dentro la waveform
                    labelX = Math.Max(0, Math.Min(labelX, picWaveform.Width - labelWidth));

                    RectangleF labelRect = new RectangleF(labelX, labelYPos, labelWidth, labelHeight);
                    using (SolidBrush bgBrush = new SolidBrush(color))
                    {
                        g.FillRectangle(bgBrush, labelRect);
                    }

                    using (Pen borderPen = new Pen(Color.Black, 1))
                    {
                        g.DrawRectangle(borderPen, labelRect.X, labelRect.Y, labelRect.Width, labelRect.Height);
                    }

                    using (SolidBrush textBrush = new SolidBrush(Color.Black))
                    {
                        g.DrawString(label, font, textBrush, labelX + 4, labelYPos + 2);
                    }
                }
            }
        }

        private void picWaveform_MouseDown(object sender, MouseEventArgs e)
        {
            if (_audioReader == null) return;

            int totalMs = (int)_audioReader.TotalTime.TotalMilliseconds;

            // ✅ Check click su marker labels (zoom-aware)
            if (CheckMarkerLabelClickZoomed(e.X, e.Y, totalMs, _musicEntry.MarkerOUT, "OUT", true)) return;
            if (CheckMarkerLabelClickZoomed(e.X, e.Y, totalMs, _musicEntry.MarkerMIX, "MIX", true)) return;
            if (CheckMarkerLabelClickZoomed(e.X, e.Y, totalMs, _musicEntry.MarkerINTRO, "INTRO", false)) return;
            if (CheckMarkerLabelClickZoomed(e.X, e.Y, totalMs, _musicEntry.MarkerIN, "IN", false)) return;

            // ✅ Click su waveform = spostamento posizione (zoom-aware)
            _userScrolled = false;
            int clickMs = PixelXToMs(e.X, totalMs);
            _audioReader.CurrentTime = TimeSpan.FromMilliseconds(clickMs);

            // Sync video to click position
            SyncVideoSeek(clickMs);

            picWaveform.Invalidate();
        }

        private bool CheckMarkerLabelClickZoomed(int mouseX, int mouseY, int totalMs, int markerMs, string markerType, bool labelOnLeft)
        {
            float xPos = MsToPixelX(markerMs, totalMs);

            if (IsInsideLabelZoomed(mouseX, mouseY, xPos, markerType, labelOnLeft))
            {
                _isDraggingMarker = true;
                _draggingMarkerType = markerType;
                picWaveform.Cursor = Cursors.SizeWE;
                Console.WriteLine($"[DRAG START] {markerType} at {markerMs}ms");
                return true;
            }

            return false;
        }

        private bool IsInsideLabelZoomed(int mouseX, int mouseY, float xPos, string markerType, bool labelOnLeft)
        {
            int labelYPos;

            switch (markerType.ToUpper())
            {
                case "IN":
                case "MIX":
                    labelYPos = 3;
                    break;
                case "INTRO":
                    labelYPos = picWaveform.Height / 2;
                    break;
                case "OUT":
                    labelYPos = picWaveform.Height - 25;
                    break;
                default:
                    labelYPos = 3;
                    break;
            }

            using (Font font = new Font("Segoe UI", 8, FontStyle.Bold))
            using (Graphics g = picWaveform.CreateGraphics())
            {
                SizeF textSize = g.MeasureString(markerType.ToUpper(), font);
                int labelWidth = (int)textSize.Width + 8;
                int labelHeight = (int)textSize.Height + 4;

                float labelX = labelOnLeft ? xPos - labelWidth - 5 : xPos + 5;

                RectangleF labelRect = new RectangleF(labelX, labelYPos, labelWidth, labelHeight);
                return labelRect.Contains(mouseX, mouseY);
            }
        }

        private void picWaveform_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDraggingMarker || _audioReader == null) return;

            int totalMs = (int)_audioReader.TotalTime.TotalMilliseconds;

            // ✅ Conversione pixel -> ms zoom-aware
            int newMs = PixelXToMs(e.X, totalMs);
            newMs = Math.Max(0, Math.Min(newMs, totalMs));

            switch (_draggingMarkerType)
            {
                case "IN":
                    _musicEntry.MarkerIN = newMs;
                    txtMarkerIn.Text = FormatTime(newMs);
                    break;

                case "INTRO":
                    _musicEntry.MarkerINTRO = newMs;
                    txtMarkerIntro.Text = FormatTime(newMs);
                    break;

                case "MIX":
                    int offset = _musicEntry.MarkerOUT - _musicEntry.MarkerMIX;
                    _musicEntry.MarkerMIX = newMs;
                    txtMarkerMix.Text = FormatTime(newMs);
                    _musicEntry.MarkerOUT = newMs + offset;
                    txtMarkerOut.Text = FormatTime(_musicEntry.MarkerOUT);
                    break;

                case "OUT":
                    _musicEntry.MarkerOUT = Math.Max(newMs, _musicEntry.MarkerMIX);
                    txtMarkerOut.Text = FormatTime(_musicEntry.MarkerOUT);
                    break;
            }

            picWaveform.Invalidate();
        }

        private void picWaveform_MouseUp(object sender, MouseEventArgs e)
        {
            if (_isDraggingMarker)
            {
                Console.WriteLine($"[DRAG END] {_draggingMarkerType}");
                PlayFromMarker(_draggingMarkerType);
            }

            _isDraggingMarker = false;
            _draggingMarkerType = "";
            picWaveform.Cursor = Cursors.Default;
        }

        #endregion

        #region ============ SAVE ============

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                Console.WriteLine($"\n[MusicEditor] ========== SALVATAGGIO ==========");
                Console.WriteLine($"[MusicEditor] ID originale: {(_isClip ? _originalClipId : _musicEntry.ID)}");
                Console.WriteLine($"[MusicEditor] IsClip: {_isClip}");

                _musicEntry.Title = txtTitle.Text ?? "";
                _musicEntry.Artist = txtArtist.Text ?? "";
                _musicEntry.Album = txtAlbum.Text ?? "";
                _musicEntry.Year = (int)numYear.Value;
                _musicEntry.Genre = cmbGenre.Text ?? "";

                // Categorie: join degli elementi selezionati con punto e virgola
                _musicEntry.Categories = _checkedCategories.Count > 0
                    ? string.Join(";", _checkedCategories.OrderBy(c => c))
                    : "";

                // Artisti secondari
                if (!_isClip)
                {
                    _musicEntry.FeaturedArtists = _checkedFeaturedArtists.Count > 0
                        ? string.Join(";", _checkedFeaturedArtists.OrderBy(a => a))
                        : "";
                }

                _musicEntry.MarkerIN = ParseTime(txtMarkerIn.Text);
                _musicEntry.MarkerINTRO = ParseTime(txtMarkerIntro.Text);
                _musicEntry.MarkerMIX = ParseTime(txtMarkerMix.Text);
                _musicEntry.MarkerOUT = ParseTime(txtMarkerOut.Text);

                _musicEntry.ValidFrom = _chkEnableValidFrom.Checked ? _dtpValidFrom.Value.ToString("yyyy-MM-dd") : "";
                _musicEntry.ValidTo = _chkEnableValidTo.Checked ? _dtpValidTo.Value.ToString("yyyy-MM-dd") : "";

                var selectedMonths = _chkMonths
                    .Select((chk, index) => chk.Checked ? (index + 1).ToString() : null)
                    .Where(m => m != null)
                    .ToList();
                _musicEntry.ValidMonths = selectedMonths.Count > 0 ? string.Join(";", selectedMonths) : "1;2;3;4;5;6;7;8;9;10;11;12";

                string[] dayMap = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
                var selectedDays = _chkDays
                    .Select((chk, index) => chk.Checked ? dayMap[index] : null)
                    .Where(d => d != null)
                    .ToList();
                _musicEntry.ValidDays = selectedDays.Count > 0 ? string.Join(";", selectedDays) : "Sunday;Monday;Tuesday;Wednesday;Thursday;Friday;Saturday";

                var selectedHours = _chkHours
                    .Select((chk, index) => chk.Checked ? index.ToString() : null)
                    .Where(h => h != null)
                    .ToList();
                _musicEntry.ValidHours = selectedHours.Count > 0 ? string.Join(";", selectedHours) : "0;1;2;3;4;5;6;7;8;9;10;11;12;13;14;15;16;17;18;19;20;21;22;23";

                if (_audioReader != null)
                {
                    _musicEntry.Duration = (int)_audioReader.TotalTime.TotalMilliseconds;
                    Console.WriteLine($"[MusicEditor] Duration aggiornata: {_musicEntry.Duration}ms");
                }

                if (!_isClip)
                {
                    try
                    {
                        if (File.Exists(_musicEntry.FilePath))
                        {
                            var tagFile = TagLib.File.Create(_musicEntry.FilePath);
                            tagFile.Tag.Title = _musicEntry.Title;
                            tagFile.Tag.Performers = new[] { _musicEntry.Artist };
                            tagFile.Tag.Album = _musicEntry.Album;
                            tagFile.Tag.Year = (uint)_musicEntry.Year;
                            tagFile.Tag.Genres = new[] { _musicEntry.Genre };
                            tagFile.Save();
                            tagFile.Dispose();
                            Console.WriteLine($"[MusicEditor] ✅ TAG MP3 aggiornati");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[MusicEditor] ⚠️ Errore TAG: {ex.Message}");
                    }
                }

                bool success;

                if (_isClip)
                {
                    var clipEntry = new ClipEntry
                    {
                        ID = _originalClipId,
                        FilePath = _musicEntry.FilePath,
                        Title = _musicEntry.Title,
                        Genre = _musicEntry.Genre,
                        Categories = _musicEntry.Categories,
                        Duration = _musicEntry.Duration,
                        MarkerIN = _musicEntry.MarkerIN,
                        MarkerINTRO = _musicEntry.MarkerINTRO,
                        MarkerMIX = _musicEntry.MarkerMIX,
                        MarkerOUT = _musicEntry.MarkerOUT,
                        ValidMonths = _musicEntry.ValidMonths,
                        ValidDays = _musicEntry.ValidDays,
                        ValidHours = _musicEntry.ValidHours,
                        ValidFrom = _musicEntry.ValidFrom,
                        ValidTo = _musicEntry.ValidTo,
                        AddedDate = _musicEntry.AddedDate,
                        LastPlayed = _musicEntry.LastPlayed,
                        PlayCount = _musicEntry.PlayCount
                    };

                    Console.WriteLine($"[MusicEditor] ========== DATI CLIP DA SALVARE ==========");
                    Console.WriteLine($"  ID: {clipEntry.ID}");
                    Console.WriteLine($"  Title: {clipEntry.Title}");
                    Console.WriteLine($"  Duration: {clipEntry.Duration}ms");
                    Console.WriteLine($"  MarkerIN: {clipEntry.MarkerIN}ms");
                    Console.WriteLine($"  MarkerMIX: {clipEntry.MarkerMIX}ms");
                    Console.WriteLine($"  MarkerOUT: {clipEntry.MarkerOUT}ms");
                    Console.WriteLine($"[MusicEditor] ===========================================");

                    success = DbcManager.Update("Clips.dbc", clipEntry);
                }
                else
                {
                    Console.WriteLine($"[MusicEditor] ========== DATI MUSICA DA SALVARE ==========");
                    Console.WriteLine($"  ID: {_musicEntry.ID}");
                    Console.WriteLine($"  Title: {_musicEntry.Title}");
                    Console.WriteLine($"  Artist: {_musicEntry.Artist}");
                    Console.WriteLine($"  Duration: {_musicEntry.Duration}ms");
                    Console.WriteLine($"  MarkerIN: {_musicEntry.MarkerIN}ms");
                    Console.WriteLine($"  MarkerMIX: {_musicEntry.MarkerMIX}ms");
                    Console.WriteLine($"  MarkerOUT: {_musicEntry.MarkerOUT}ms");
                    Console.WriteLine($"[MusicEditor] =============================================");

                    success = DbcManager.Update("Music.dbc", _musicEntry);
                }

                Console.WriteLine($"[MusicEditor] Risultato DbcManager.Update: {success}");

                if (success)
                {
                    // ✅ Se c'è un volume gain pendente, lancia ffmpeg dopo aver liberato le risorse audio
                    if (_pendingVolumeApply)
                    {
                        // Ferma riproduzione e libera il file
                        _waveOut?.Stop();
                        _isPlaying = false;
                        _positionTimer.Stop();
                        _audioReader?.Dispose();
                        _audioReader = null;
                        _waveOut?.Dispose();
                        _waveOut = null;

                        // Lancia ffmpeg in fire-and-forget (il form si chiude subito, non aspetta)
                        float dbToApply = _pendingVolumeBoostDb;
                        string ffmpegPath = _pendingFfmpegPath;
                        string sourceFile = _pendingSourceFile;

                        Task.Run(() =>
                        {
                            string tempFile = null;
                            try
                            {
                                string ext = Path.GetExtension(sourceFile);
                                tempFile = Path.Combine(Path.GetDirectoryName(sourceFile),
                                    Path.GetFileNameWithoutExtension(sourceFile) + "_boosted" + ext);

                                string volumeFilter = $"volume={dbToApply}dB";
                                string arguments = $"-i \"{sourceFile}\" -af \"{volumeFilter}\" -y \"{tempFile}\"";

                                Console.WriteLine($"[MusicEditor] 🔊 ffmpeg (deferred): {arguments}");

                                bool exited;
                                int exitCode;
                                string stderr;

                                using (var process = new Process
                                {
                                    StartInfo = new ProcessStartInfo
                                    {
                                        FileName = ffmpegPath,
                                        Arguments = arguments,
                                        UseShellExecute = false,
                                        CreateNoWindow = true,
                                        RedirectStandardOutput = false,
                                        RedirectStandardError = true
                                    }
                                })
                                {
                                    process.Start();
                                    stderr = process.StandardError.ReadToEnd();
                                    exited = process.WaitForExit(60000);
                                    exitCode = exited ? process.ExitCode : -1;
                                }

                                Console.WriteLine($"[MusicEditor] ffmpeg output: {stderr}");

                                if (exited && exitCode == 0 && File.Exists(tempFile))
                                {
                                    try
                                    {
                                        File.Delete(sourceFile);
                                        File.Move(tempFile, sourceFile);
                                        tempFile = null; // moved successfully, no cleanup needed
                                        Console.WriteLine($"[MusicEditor] ✅ Volume gain applied successfully (deferred)");
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"[MusicEditor] ❌ File replace error (deferred): {ex.Message}");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"[MusicEditor] ❌ ffmpeg failed (deferred)");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[MusicEditor] ❌ ffmpeg error (deferred): {ex.Message}");
                            }
                            finally
                            {
                                if (tempFile != null && File.Exists(tempFile))
                                    File.Delete(tempFile);
                            }
                        });
                    }

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    string errorMsg = LanguageManager.GetString("MusicEditor.SaveError", "❌ Errore salvataggio nel database.\n\nControlla la console per dettagli!");
                    string errorTitle = LanguageManager.GetString("Common.Error", "Errore");
                    MessageBox.Show(errorMsg, errorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MusicEditor] ❌ ECCEZIONE: {ex.Message}");
                Console.WriteLine($"[MusicEditor] StackTrace: {ex.StackTrace}");

                string errorMsg = LanguageManager.GetString("MusicEditor.SaveException", "❌ Errore:\n{0}");
                string errorTitle = LanguageManager.GetString("Common.Error", "Errore");
                MessageBox.Show(string.Format(errorMsg, ex.Message), errorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region ============ DISPOSE ============

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _positionTimer?.Stop();
                _positionTimer?.Dispose();
                _vuDecayTimer?.Stop();
                _vuDecayTimer?.Dispose();
                _waveOut?.Stop();
                _waveOut?.Dispose();
                _audioReader?.Dispose();
                _waveformBitmap?.Dispose();

                // Salva stato video preview nel registro
                try
                {
                    if (_videoPath != null)
                    {
                        bool isOpen = _videoPreviewForm != null && !_videoPreviewForm.IsDisposed;
                        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AirDirector"))
                        {
                            key?.SetValue("VideoPreviewOpen", isOpen ? 1 : 0);
                        }
                    }
                }
                catch { }

                // Close and dispose VideoPreviewForm if open
                try
                {
                    if (_videoPreviewForm != null && !_videoPreviewForm.IsDisposed)
                    {
                        _videoPreviewForm.Close();
                        _videoPreviewForm.Dispose();
                        _videoPreviewForm = null;
                    }
                }
                catch { }

                if (components != null)
                {
                    components.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}
