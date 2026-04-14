using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using AirDirector.Services.Database;
using AirDirector.Services.Localization;
using AirDirector.Themes;
using AirDirector.Models;
using Newtonsoft.Json;

namespace AirDirector.Forms
{
    public partial class ClockEditorForm : Form
    {
        private ClockEntry _clock;
        private bool _isNewClock;
        private List<ClockItem> _clockItems;

        private List<string> _availableMusicCategories;
        private List<string> _availableClipCategories;
        private List<string> _availableMusicGenres;
        private List<string> _availableClipGenres;

        private TextBox txtClockName;
        private FlowLayoutPanel flowClockItems;
        private Label lblTotalDuration;

        private Panel pnlSourceGroup;
        private RadioButton radMusic;
        private RadioButton radClips;

        private CheckBox chkFilterCategory;
        private CheckBox chkFilterGenre;
        private ComboBox cmbCategory;
        private ComboBox cmbGenre;
        private Label lblFilterCount;

        private CheckBox chkYearFilter;
        private NumericUpDown numYearFrom;
        private NumericUpDown numYearTo;
        private Button btnAddItem;
        private Button btnSave;
        private Button btnCancel;

        private Label lblHeaderTitle;
        private Label lblName;
        private Label lblAddTitle;
        private Label lblSource;
        private Label lblYearFrom;
        private Label lblYearTo;
        private Label lblItemsTitle;

        private int _dragSourceIndex = -1;
        private bool _isDragging = false;

        public ClockEditorForm(ClockEntry clock)
        {
            _isNewClock = (clock == null);

            if (_isNewClock)
            {
                _clock = new ClockEntry
                {
                    ClockName = "",
                    IsDefault = 0,
                    Items = "[]"
                };
                _clockItems = new List<ClockItem>();
            }
            else
            {
                _clock = clock;
                try
                {
                    _clockItems = JsonConvert.DeserializeObject<List<ClockItem>>(_clock.Items) ?? new List<ClockItem>();
                }
                catch
                {
                    _clockItems = new List<ClockItem>();
                }
            }

            LoadAvailableData();
            InitializeComponent();
            ApplyLanguage();
            LoadData();

            LanguageManager.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            ApplyLanguage();
            RefreshClockItemsList();
        }

        private void ApplyLanguage()
        {
            this.Text = _isNewClock ?
                LanguageManager.GetString("ClockEditor.NewClock", "🕐 Nuovo Clock") :
                LanguageManager.GetString("ClockEditor.EditClock", "✏️ Modifica Clock");

            if (lblHeaderTitle != null)
                lblHeaderTitle.Text = _isNewClock ?
                    LanguageManager.GetString("ClockEditor.NewClockTitle", "🕐 NUOVO CLOCK") :
                    LanguageManager.GetString("ClockEditor.EditClockTitle", "✏️ MODIFICA CLOCK");

            if (lblName != null)
                lblName.Text = LanguageManager.GetString("ClockEditor.ClockName", "Nome Clock:");

            if (lblAddTitle != null)
                lblAddTitle.Text = "➕ " + LanguageManager.GetString("ClockEditor.AddElement", "AGGIUNGI ELEMENTO");

            if (lblSource != null)
                lblSource.Text = LanguageManager.GetString("ClockEditor.Source", "Sorgente:");

            if (radMusic != null)
                radMusic.Text = "🎵 " + LanguageManager.GetString("ClockEditor.Music", "Musica");

            if (radClips != null)
                radClips.Text = "⚡ " + LanguageManager.GetString("ClockEditor.Clips", "Clips");

            if (chkFilterCategory != null)
                chkFilterCategory.Text = "📁 " + LanguageManager.GetString("ClockEditor.Category", "Categoria");

            if (chkFilterGenre != null)
                chkFilterGenre.Text = "🎵 " + LanguageManager.GetString("ClockEditor.Genre", "Genere");

            if (chkYearFilter != null)
                chkYearFilter.Text = "🗓️ " + LanguageManager.GetString("ClockEditor.YearFilter", "Filtro Anni:");

            if (lblYearFrom != null)
                lblYearFrom.Text = LanguageManager.GetString("ClockEditor.From", "Dal:");

            if (lblYearTo != null)
                lblYearTo.Text = LanguageManager.GetString("ClockEditor.To", "Al:");

            if (btnAddItem != null)
                btnAddItem.Text = "➕ " + LanguageManager.GetString("ClockEditor.AddToClock", "AGGIUNGI AL CLOCK  ➤");

            if (lblItemsTitle != null)
                lblItemsTitle.Text = "📋 " + LanguageManager.GetString("ClockEditor.ElementsList", "ELEMENTI DEL CLOCK (trascina per riordinare)");

            if (btnSave != null)
                btnSave.Text = "💾 " + LanguageManager.GetString("Common.Save", "SALVA");

            if (btnCancel != null)
                btnCancel.Text = "✖";

            UpdateFilterCount();
        }

        private double NormalizeDuration(double duration)
        {
            return duration / 1000.0;
        }

        private void LoadAvailableData()
        {
            var musicEntries = DbcManager.LoadFromCsv<MusicEntry>("Music.dbc");
            var musicCategories = new HashSet<string>();
            var musicGenres = new HashSet<string>();

            foreach (var entry in musicEntries)
            {
                if (!string.IsNullOrEmpty(entry.Categories))
                {
                    var cats = entry.Categories.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var cat in cats)
                    {
                        string trimmed = cat.Trim();
                        if (!string.IsNullOrEmpty(trimmed))
                            musicCategories.Add(trimmed);
                    }
                }

                if (!string.IsNullOrEmpty(entry.Genre))
                {
                    string trimmed = entry.Genre.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                        musicGenres.Add(trimmed);
                }
            }

            _availableMusicCategories = musicCategories.OrderBy(c => c).ToList();
            _availableMusicGenres = musicGenres.OrderBy(g => g).ToList();

            var clipEntries = DbcManager.LoadFromCsv<ClipEntry>("Clips.dbc");
            var clipCategories = new HashSet<string>();
            var clipGenres = new HashSet<string>();

            foreach (var entry in clipEntries)
            {
                if (!string.IsNullOrEmpty(entry.Categories))
                {
                    var cats = entry.Categories.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var cat in cats)
                    {
                        string trimmed = cat.Trim();
                        if (!string.IsNullOrEmpty(trimmed))
                            clipCategories.Add(trimmed);
                    }
                }

                if (!string.IsNullOrEmpty(entry.Genre))
                {
                    string trimmed = entry.Genre.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                        clipGenres.Add(trimmed);
                }
            }

            _availableClipCategories = clipCategories.OrderBy(c => c).ToList();
            _availableClipGenres = clipGenres.OrderBy(g => g).ToList();
        }

        private void InitializeComponent()
        {
            this.Text = _isNewClock ? "🕐 Nuovo Clock" : "✏️ Modifica Clock";
            this.Size = new Size(1200, 815);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(25, 25, 25);

            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 90,
                BackColor = Color.FromArgb(138, 43, 226)
            };
            this.Controls.Add(headerPanel);

            lblHeaderTitle = new Label
            {
                Text = _isNewClock ? "🕐 NUOVO CLOCK" : "✏️ MODIFICA CLOCK",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 15),
                AutoSize = true
            };
            headerPanel.Controls.Add(lblHeaderTitle);

            lblName = new Label
            {
                Text = "Nome Clock:",
                Location = new Point(20, 55),
                Size = new Size(110, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White
            };
            headerPanel.Controls.Add(lblName);

            txtClockName = new TextBox
            {
                Location = new Point(135, 52),
                Size = new Size(1030, 30),
                Font = new Font("Segoe UI", 12),
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            headerPanel.Controls.Add(txtClockName);

            Panel pnlLeft = new Panel
            {
                Location = new Point(20, 105),
                Size = new Size(350, 620),
                BackColor = Color.FromArgb(35, 35, 35),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(pnlLeft);

            lblAddTitle = new Label
            {
                Text = "➕ AGGIUNGI ELEMENTO",
                Location = new Point(15, 15),
                Size = new Size(320, 30),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White
            };
            pnlLeft.Controls.Add(lblAddTitle);

            lblSource = new Label
            {
                Text = "Sorgente:",
                Location = new Point(15, 60),
                Size = new Size(90, 28),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White
            };
            pnlLeft.Controls.Add(lblSource);

            pnlSourceGroup = new Panel
            {
                Location = new Point(15, 90),
                Size = new Size(320, 30)
            };
            pnlLeft.Controls.Add(pnlSourceGroup);

            radMusic = new RadioButton
            {
                Text = "🎵 Musica",
                Location = new Point(0, 0),
                Size = new Size(150, 28),
                Checked = true,
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.White
            };
            radMusic.CheckedChanged += RadSource_CheckedChanged;
            pnlSourceGroup.Controls.Add(radMusic);

            radClips = new RadioButton
            {
                Text = "⚡ Clips",
                Location = new Point(160, 0),
                Size = new Size(150, 28),
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.White
            };
            radClips.CheckedChanged += RadSource_CheckedChanged;
            pnlSourceGroup.Controls.Add(radClips);

            chkFilterCategory = new CheckBox
            {
                Text = "📁 Categoria",
                Location = new Point(15, 140),
                Size = new Size(130, 28),
                Checked = true,
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.White
            };
            chkFilterCategory.CheckedChanged += FilterChanged;
            pnlLeft.Controls.Add(chkFilterCategory);

            cmbCategory = new ComboBox
            {
                Location = new Point(15, 175),
                Size = new Size(320, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(55, 55, 55),
                ForeColor = Color.White
            };
            cmbCategory.SelectedIndexChanged += FilterChanged;
            pnlLeft.Controls.Add(cmbCategory);

            chkFilterGenre = new CheckBox
            {
                Text = "🎵 Genere",
                Location = new Point(15, 225),
                Size = new Size(130, 28),
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.White
            };
            chkFilterGenre.CheckedChanged += FilterChanged;
            pnlLeft.Controls.Add(chkFilterGenre);

            cmbGenre = new ComboBox
            {
                Location = new Point(15, 260),
                Size = new Size(320, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(55, 55, 55),
                ForeColor = Color.White,
                Enabled = false
            };
            cmbGenre.SelectedIndexChanged += FilterChanged;
            pnlLeft.Controls.Add(cmbGenre);

            chkYearFilter = new CheckBox
            {
                Text = "🗓️ Filtro Anni:",
                Location = new Point(15, 310),
                Size = new Size(130, 28),
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.White
            };
            chkYearFilter.CheckedChanged += ChkYearFilter_CheckedChanged;
            pnlLeft.Controls.Add(chkYearFilter);

            lblYearFrom = new Label
            {
                Text = "Dal:",
                Location = new Point(10, 348),
                Size = new Size(50, 28),
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.White
            };
            pnlLeft.Controls.Add(lblYearFrom);

            numYearFrom = new NumericUpDown
            {
                Location = new Point(70, 345),
                Size = new Size(90, 30),
                Minimum = 1900,
                Maximum = DateTime.Now.Year,
                Value = 2000,
                Enabled = false,
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(55, 55, 55),
                ForeColor = Color.White
            };
            numYearFrom.ValueChanged += FilterChanged;
            pnlLeft.Controls.Add(numYearFrom);

            lblYearTo = new Label
            {
                Text = "Al:",
                Location = new Point(185, 348),
                Size = new Size(45, 28),
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.White
            };
            pnlLeft.Controls.Add(lblYearTo);

            numYearTo = new NumericUpDown
            {
                Location = new Point(240, 345),
                Size = new Size(90, 30),
                Minimum = 1900,
                Maximum = DateTime.Now.Year,
                Value = DateTime.Now.Year,
                Enabled = false,
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(55, 55, 55),
                ForeColor = Color.White
            };
            numYearTo.ValueChanged += FilterChanged;
            pnlLeft.Controls.Add(numYearTo);

            lblFilterCount = new Label
            {
                Text = "📊 Brani trovati:  0",
                Location = new Point(15, 395),
                Size = new Size(320, 60),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 255, 127)
            };
            pnlLeft.Controls.Add(lblFilterCount);

            btnAddItem = new Button
            {
                Text = "➕ AGGIUNGI AL CLOCK  ➤",
                Location = new Point(15, 490),
                Size = new Size(320, 50),
                BackColor = Color.FromArgb(0, 180, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAddItem.FlatAppearance.BorderSize = 0;
            btnAddItem.Click += BtnAddItem_Click;
            pnlLeft.Controls.Add(btnAddItem);

            Panel pnlRight = new Panel
            {
                Location = new Point(390, 105),
                Size = new Size(790, 620),
                BackColor = Color.FromArgb(35, 35, 35),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(pnlRight);

            lblItemsTitle = new Label
            {
                Text = "📋 ELEMENTI DEL CLOCK (trascina per riordinare)",
                Location = new Point(15, 15),
                Size = new Size(760, 30),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White
            };
            pnlRight.Controls.Add(lblItemsTitle);

            flowClockItems = new FlowLayoutPanel
            {
                Location = new Point(15, 55),
                Size = new Size(760, 510),
                AutoScroll = true,
                BackColor = Color.FromArgb(45, 45, 45),
                BorderStyle = BorderStyle.FixedSingle,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AllowDrop = true
            };
            flowClockItems.DragEnter += FlowClockItems_DragEnter;
            flowClockItems.DragOver += FlowClockItems_DragOver;
            flowClockItems.DragDrop += FlowClockItems_DragDrop;
            flowClockItems.DragLeave += FlowClockItems_DragLeave;
            pnlRight.Controls.Add(flowClockItems);

            lblTotalDuration = new Label
            {
                Text = "⏱️ Durata Totale:  00:00:00",
                Location = new Point(15, 575),
                Size = new Size(760, 30),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 255, 127),
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlRight.Controls.Add(lblTotalDuration);

            btnSave = new Button
            {
                Text = "💾 SALVA",
                Location = new Point(970, 735),
                Size = new Size(100, 36),
                BackColor = Color.FromArgb(0, 180, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            btnCancel = new Button
            {
                Text = "✖",
                Location = new Point(1080, 735),
                Size = new Size(100, 36),
                BackColor = Color.FromArgb(180, 0, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            this.Controls.Add(btnCancel);

            this.CancelButton = btnCancel;
        }

        private void LoadData()
        {
            txtClockName.Text = _clock.ClockName;
            UpdateSourceComboBoxes();
            RefreshClockItemsList();
            UpdateFilterCount();
        }

        private void UpdateSourceComboBoxes()
        {
            cmbCategory.Items.Clear();
            cmbGenre.Items.Clear();

            if (radMusic.Checked)
            {
                foreach (var cat in _availableMusicCategories)
                    cmbCategory.Items.Add(cat);
                foreach (var genre in _availableMusicGenres)
                    cmbGenre.Items.Add(genre);
                chkYearFilter.Enabled = true;
            }
            else
            {
                foreach (var cat in _availableClipCategories)
                    cmbCategory.Items.Add(cat);
                foreach (var genre in _availableClipGenres)
                    cmbGenre.Items.Add(genre);
                chkYearFilter.Enabled = false;
                chkYearFilter.Checked = false;
            }

            if (cmbCategory.Items.Count > 0)
                cmbCategory.SelectedIndex = 0;
            if (cmbGenre.Items.Count > 0)
                cmbGenre.SelectedIndex = 0;
        }

        private void UpdateFilterCount()
        {
            try
            {
                int count = 0;
                double avgDuration = 0;

                if (radMusic.Checked)
                {
                    var musicEntries = DbcManager.LoadFromCsv<MusicEntry>("Music.dbc");
                    var filtered = FilterMusicEntries(musicEntries);
                    count = filtered.Count;

                    if (count > 0)
                    {
                        double totalSeconds = filtered.Sum(e =>
                            e.MarkerMIX > e.MarkerIN ? NormalizeDuration(e.MarkerMIX - e.MarkerIN) : NormalizeDuration(e.Duration));
                        avgDuration = totalSeconds / count;
                    }
                }
                else
                {
                    var clipEntries = DbcManager.LoadFromCsv<ClipEntry>("Clips.dbc");
                    var filtered = FilterClipEntries(clipEntries);
                    count = filtered.Count;

                    if (count > 0)
                    {
                        double totalSeconds = filtered.Sum(e =>
                            e.MarkerMIX > e.MarkerIN ? NormalizeDuration(e.MarkerMIX - e.MarkerIN) : NormalizeDuration(e.Duration));
                        avgDuration = totalSeconds / count;
                    }
                }

                if (count > 0)
                {
                    TimeSpan avgTime = TimeSpan.FromSeconds(avgDuration);
                    lblFilterCount.Text = string.Format(LanguageManager.GetString("ClockEditor.FoundTracks", "📊 Trovati: {0} brani\n\nDurata media: {1}"),
                        count,
                        string.Format("{0:mm\\:ss}", avgTime));
                    lblFilterCount.ForeColor = Color.FromArgb(0, 255, 127);
                }
                else
                {
                    lblFilterCount.Text = LanguageManager.GetString("ClockEditor.NoTracksFound", "⚠️ Nessun brano trovato");
                    lblFilterCount.ForeColor = Color.OrangeRed;
                }
            }
            catch
            {
                lblFilterCount.Text = LanguageManager.GetString("ClockEditor.CalculationError", "❌ Errore calcolo");
                lblFilterCount.ForeColor = Color.Red;
            }
        }

        private List<MusicEntry> FilterMusicEntries(List<MusicEntry> entries)
        {
            var filtered = entries.AsEnumerable();

            if (chkFilterCategory.Checked && cmbCategory.SelectedIndex >= 0)
            {
                string selectedCategory = cmbCategory.SelectedItem.ToString();
                filtered = filtered.Where(e =>
                    !string.IsNullOrEmpty(e.Categories) &&
                    e.Categories.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Any(c => c.Trim().Equals(selectedCategory, StringComparison.OrdinalIgnoreCase))
                );
            }

            if (chkFilterGenre.Checked && cmbGenre.SelectedIndex >= 0)
            {
                string selectedGenre = cmbGenre.SelectedItem.ToString();
                filtered = filtered.Where(e =>
                    !string.IsNullOrEmpty(e.Genre) &&
                    e.Genre.Trim().Equals(selectedGenre, StringComparison.OrdinalIgnoreCase)
                );
            }

            if (chkYearFilter.Checked)
            {
                int yearFrom = (int)numYearFrom.Value;
                int yearTo = (int)numYearTo.Value;
                filtered = filtered.Where(e => e.Year >= yearFrom && e.Year <= yearTo);
            }

            return filtered.ToList();
        }

        private List<ClipEntry> FilterClipEntries(List<ClipEntry> entries)
        {
            var filtered = entries.AsEnumerable();

            if (chkFilterCategory.Checked && cmbCategory.SelectedIndex >= 0)
            {
                string selectedCategory = cmbCategory.SelectedItem.ToString();
                filtered = filtered.Where(e =>
                    !string.IsNullOrEmpty(e.Categories) &&
                    e.Categories.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Any(c => c.Trim().Equals(selectedCategory, StringComparison.OrdinalIgnoreCase))
                );
            }

            if (chkFilterGenre.Checked && cmbGenre.SelectedIndex >= 0)
            {
                string selectedGenre = cmbGenre.SelectedItem.ToString();
                filtered = filtered.Where(e =>
                    !string.IsNullOrEmpty(e.Genre) &&
                    e.Genre.Trim().Equals(selectedGenre, StringComparison.OrdinalIgnoreCase)
                );
            }

            return filtered.ToList();
        }

        private void RefreshClockItemsList()
        {
            flowClockItems.SuspendLayout();
            flowClockItems.Controls.Clear();

            int index = 0;
            double cumulativeDuration = 0;

            foreach (var item in _clockItems)
            {
                double avgDuration = GetAverageItemDuration(item);
                cumulativeDuration += avgDuration;

                Panel card = CreateClockItemCard(item, index, TimeSpan.FromSeconds(cumulativeDuration));
                flowClockItems.Controls.Add(card);
                index++;
            }

            flowClockItems.ResumeLayout();

            TimeSpan totalTime = TimeSpan.FromSeconds(cumulativeDuration);
            lblTotalDuration.Text = string.Format(LanguageManager.GetString("ClockEditor.TotalDuration", "⏱️ Durata Totale: {0}"),
                string.Format("{0:hh\\:mm\\:ss}", totalTime));
        }

        private Panel CreateClockItemCard(ClockItem item, int index, TimeSpan cumulativeTime)
        {
            int cardWidth = flowClockItems.ClientSize.Width - 25;

            bool isMusic = item.Type.StartsWith("Music_");

            Color bgColor = isMusic
                ? Color.FromArgb(255, 255, 100)
                : Color.FromArgb(100, 200, 255);

            Color textColor = Color.Black;

            Panel card = new Panel
            {
                Width = cardWidth,
                Height = 35,
                BackColor = bgColor,
                Margin = new Padding(3),
                Tag = index,
                Cursor = Cursors.SizeAll,
                AllowDrop = true
            };

            card.MouseDown += Card_MouseDown;
            card.MouseMove += Card_MouseMove;
            card.MouseUp += Card_MouseUp;

            string icon = isMusic ? "🎵" : "⚡";
            string sourceType = isMusic ?
                LanguageManager.GetString("ClockEditor.Music", "Musica") :
                LanguageManager.GetString("ClockEditor.Clips", "Clips");
            string value = !string.IsNullOrEmpty(item.Value) ? item.Value : item.CategoryName;

            string detailInfo = GetDetailedFilterInfo(item, sourceType);

            Label lblNumber = new Label
            {
                Text = $"#{index + 1}",
                Location = new Point(5, 8),
                Size = new Size(45, 20),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = textColor,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };
            lblNumber.MouseDown += Card_MouseDown;
            lblNumber.MouseMove += Card_MouseMove;
            lblNumber.MouseUp += Card_MouseUp;
            card.Controls.Add(lblNumber);

            Label lblIcon = new Label
            {
                Text = icon,
                Location = new Point(55, 5),
                Size = new Size(25, 25),
                Font = new Font("Segoe UI", 14),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            lblIcon.MouseDown += Card_MouseDown;
            lblIcon.MouseMove += Card_MouseMove;
            lblIcon.MouseUp += Card_MouseUp;
            card.Controls.Add(lblIcon);

            Label lblValue = new Label
            {
                Text = detailInfo,
                Location = new Point(85, 8),
                Size = new Size(cardWidth - 300, 20),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = textColor,
                AutoEllipsis = true,
                BackColor = Color.Transparent
            };
            lblValue.MouseDown += Card_MouseDown;
            lblValue.MouseMove += Card_MouseMove;
            lblValue.MouseUp += Card_MouseUp;
            card.Controls.Add(lblValue);

            Label lblTime = new Label
            {
                Text = $"[{cumulativeTime.ToString(@"hh\:mm\:ss")}]",  // ✅ FUNZIONA
                Location = new Point(cardWidth - 160, 8),
                Size = new Size(110, 20),
                Font = new Font("Consolas", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 128, 0),
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent
            };
            lblTime.MouseDown += Card_MouseDown;
            lblTime.MouseMove += Card_MouseMove;
            lblTime.MouseUp += Card_MouseUp;
            card.Controls.Add(lblTime);

            Button btnDelete = new Button
            {
                Text = "✖︎",
                Location = new Point(cardWidth - 45, 5),
                Size = new Size(35, 25),
                BackColor = Color.FromArgb(180, 0, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9),
                Cursor = Cursors.Hand,
                Tag = index
            };
            btnDelete.FlatAppearance.BorderSize = 0;
            btnDelete.Click += (s, e) =>
            {
                _clockItems.RemoveAt(index);
                RefreshClockItemsList();
            };
            card.Controls.Add(btnDelete);

            return card;
        }

        private string GetDetailedFilterInfo(ClockItem item, string sourceType)
        {
            List<string> parts = new List<string>();

            parts.Add(sourceType);

            string type = item.Type.Replace("Music_", "").Replace("Clips_", "");

            if (type == "Category")
            {
                parts.Add(item.Value);
                parts.Add(LanguageManager.GetString("ClockEditor.FilterCategory", "Filtro:  Categoria"));
            }
            else if (type == "Genre")
            {
                parts.Add(item.Value);
                parts.Add(LanguageManager.GetString("ClockEditor.FilterGenre", "Filtro:  Genere"));
            }
            else if (type == "Category+Genre")
            {
                string[] values = item.Value.Split(new[] { " + " }, StringSplitOptions.None);
                if (values.Length == 2)
                {
                    parts.Add(values[0]);
                    parts.Add(values[1]);
                    parts.Add(LanguageManager.GetString("ClockEditor.FilterCategoryGenre", "Filtro: Cat+Gen"));
                }
            }

            if (item.YearFilterEnabled)
            {
                parts.Add(string.Format(LanguageManager.GetString("ClockEditor.Years", "Anni: {0}-{1}"), item.YearFrom, item.YearTo));
            }

            return string.Join("   |   ", parts);
        }

        private void Card_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Control ctrl = sender as Control;

                Panel card = null;

                if (ctrl is Panel)
                {
                    card = ctrl as Panel;
                }
                else if (ctrl.Parent is Panel)
                {
                    card = ctrl.Parent as Panel;
                }
                else if (ctrl.Parent?.Parent is Panel)
                {
                    card = ctrl.Parent.Parent as Panel;
                }

                if (card != null && card.Tag != null && card.Tag is int)
                {
                    _dragSourceIndex = (int)card.Tag;
                    _isDragging = false;
                }
            }
        }

        private void Card_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && _dragSourceIndex >= 0 && !_isDragging)
            {
                if (Math.Abs(e.X) > 5 || Math.Abs(e.Y) > 5)
                {
                    _isDragging = true;

                    if (_dragSourceIndex < _clockItems.Count)
                    {
                        flowClockItems.DoDragDrop(_clockItems[_dragSourceIndex], DragDropEffects.Move);
                    }

                    _dragSourceIndex = -1;
                    _isDragging = false;
                }
            }
        }

        private void Card_MouseUp(object sender, MouseEventArgs e)
        {
            _dragSourceIndex = -1;
            _isDragging = false;
        }

        private void FlowClockItems_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(ClockItem)))
            {
                e.Effect = DragDropEffects.Move;
            }
        }

        private void FlowClockItems_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void FlowClockItems_DragLeave(object sender, EventArgs e)
        {
        }

        private void FlowClockItems_DragDrop(object sender, DragEventArgs e)
        {
            if (_dragSourceIndex < 0 || _dragSourceIndex >= _clockItems.Count)
            {
                _dragSourceIndex = -1;
                _isDragging = false;
                return;
            }

            Point clientPoint = flowClockItems.PointToClient(new Point(e.X, e.Y));
            int targetIndex = -1;

            for (int i = 0; i < flowClockItems.Controls.Count; i++)
            {
                Control ctrl = flowClockItems.Controls[i];

                if (clientPoint.Y < ctrl.Bottom)
                {
                    if (ctrl.Tag is int)
                    {
                        targetIndex = (int)ctrl.Tag;
                        break;
                    }
                }
            }

            if (targetIndex < 0)
                targetIndex = _clockItems.Count;

            if (targetIndex != _dragSourceIndex && targetIndex != _dragSourceIndex + 1)
            {
                var item = _clockItems[_dragSourceIndex];
                _clockItems.RemoveAt(_dragSourceIndex);

                if (targetIndex > _dragSourceIndex)
                    targetIndex--;

                _clockItems.Insert(targetIndex, item);
                RefreshClockItemsList();
            }

            _dragSourceIndex = -1;
            _isDragging = false;
        }

        private double GetAverageItemDuration(ClockItem item)
        {
            try
            {
                string source = item.Type.StartsWith("Music_") ? "Music" : "Clips";
                string type = item.Type.Replace("Music_", "").Replace("Clips_", "");

                if (source == "Music")
                {
                    var musicEntries = DbcManager.LoadFromCsv<MusicEntry>("Music.dbc");
                    var filtered = ApplyItemFilterToMusic(musicEntries, item, type);

                    if (filtered.Count > 0)
                    {
                        double totalSeconds = filtered.Sum(e =>
                            e.MarkerMIX > e.MarkerIN ? NormalizeDuration(e.MarkerMIX - e.MarkerIN) : NormalizeDuration(e.Duration));
                        return totalSeconds / filtered.Count;
                    }
                }
                else
                {
                    var clipEntries = DbcManager.LoadFromCsv<ClipEntry>("Clips.dbc");
                    var filtered = ApplyItemFilterToClips(clipEntries, item, type);

                    if (filtered.Count > 0)
                    {
                        double totalSeconds = filtered.Sum(e =>
                            e.MarkerMIX > e.MarkerIN ? NormalizeDuration(e.MarkerMIX - e.MarkerIN) : NormalizeDuration(e.Duration));
                        return totalSeconds / filtered.Count;
                    }
                }

                return 180;
            }
            catch
            {
                return 180;
            }
        }

        private List<MusicEntry> ApplyItemFilterToMusic(List<MusicEntry> entries, ClockItem item, string type)
        {
            var filtered = entries.AsEnumerable();

            if (type == "Category")
            {
                filtered = filtered.Where(e =>
                    !string.IsNullOrEmpty(e.Categories) &&
                    e.Categories.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Any(c => c.Trim().Equals(item.Value, StringComparison.OrdinalIgnoreCase))
                );
            }
            else if (type == "Genre")
            {
                filtered = filtered.Where(e =>
                    !string.IsNullOrEmpty(e.Genre) &&
                    e.Genre.Trim().Equals(item.Value, StringComparison.OrdinalIgnoreCase)
                );
            }
            else if (type == "Category+Genre")
            {
                string[] parts = item.Value.Split(new[] { " + " }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    string category = parts[0];
                    string genre = parts[1];

                    filtered = filtered.Where(e =>
                        !string.IsNullOrEmpty(e.Categories) &&
                        e.Categories.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Any(c => c.Trim().Equals(category, StringComparison.OrdinalIgnoreCase)) &&
                        !string.IsNullOrEmpty(e.Genre) &&
                        e.Genre.Trim().Equals(genre, StringComparison.OrdinalIgnoreCase)
                    );
                }
            }

            if (item.YearFilterEnabled)
            {
                filtered = filtered.Where(e => e.Year >= item.YearFrom && e.Year <= item.YearTo);
            }

            return filtered.ToList();
        }

        private List<ClipEntry> ApplyItemFilterToClips(List<ClipEntry> entries, ClockItem item, string type)
        {
            var filtered = entries.AsEnumerable();

            if (type == "Category")
            {
                filtered = filtered.Where(e =>
                    !string.IsNullOrEmpty(e.Categories) &&
                    e.Categories.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Any(c => c.Trim().Equals(item.Value, StringComparison.OrdinalIgnoreCase))
                );
            }
            else if (type == "Genre")
            {
                filtered = filtered.Where(e =>
                    !string.IsNullOrEmpty(e.Genre) &&
                    e.Genre.Trim().Equals(item.Value, StringComparison.OrdinalIgnoreCase)
                );
            }
            else if (type == "Category+Genre")
            {
                string[] parts = item.Value.Split(new[] { " + " }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    string category = parts[0];
                    string genre = parts[1];

                    filtered = filtered.Where(e =>
                        !string.IsNullOrEmpty(e.Categories) &&
                        e.Categories.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Any(c => c.Trim().Equals(category, StringComparison.OrdinalIgnoreCase)) &&
                        !string.IsNullOrEmpty(e.Genre) &&
                        e.Genre.Trim().Equals(genre, StringComparison.OrdinalIgnoreCase)
                    );
                }
            }

            return filtered.ToList();
        }

        private void FilterChanged(object sender, EventArgs e)
        {
            cmbCategory.Enabled = chkFilterCategory.Checked;
            cmbGenre.Enabled = chkFilterGenre.Checked;
            UpdateFilterCount();
        }

        private void RadSource_CheckedChanged(object sender, EventArgs e)
        {
            UpdateSourceComboBoxes();
            UpdateFilterCount();
        }

        private void ChkYearFilter_CheckedChanged(object sender, EventArgs e)
        {
            numYearFrom.Enabled = chkYearFilter.Checked;
            numYearTo.Enabled = chkYearFilter.Checked;
            UpdateFilterCount();
        }

        private void BtnAddItem_Click(object sender, EventArgs e)
        {
            if (!chkFilterCategory.Checked && !chkFilterGenre.Checked)
            {
                MessageBox.Show(
                    LanguageManager.GetString("ClockEditor.SelectAtLeastOneFilter", "⚠️ Seleziona almeno un filtro"),
                    LanguageManager.GetString("Common.Warning", "Attenzione"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            string source = radMusic.Checked ? "Music" : "Clips";
            string type = "";
            string value = "";

            if (chkFilterCategory.Checked && chkFilterGenre.Checked)
            {
                if (cmbCategory.SelectedIndex < 0 || cmbGenre.SelectedIndex < 0)
                {
                    MessageBox.Show(
                        LanguageManager.GetString("ClockEditor.SelectCategoryAndGenre", "⚠️ Seleziona categoria e genere"),
                        LanguageManager.GetString("Common.Warning", "Attenzione"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }
                type = $"{source}_Category+Genre";
                value = $"{cmbCategory.SelectedItem} + {cmbGenre.SelectedItem}";
            }
            else if (chkFilterCategory.Checked)
            {
                if (cmbCategory.SelectedIndex < 0)
                {
                    MessageBox.Show(
                        LanguageManager.GetString("ClockEditor.SelectCategory", "⚠️ Seleziona una categoria"),
                        LanguageManager.GetString("Common.Warning", "Attenzione"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }
                type = $"{source}_Category";
                value = cmbCategory.SelectedItem.ToString();
            }
            else if (chkFilterGenre.Checked)
            {
                if (cmbGenre.SelectedIndex < 0)
                {
                    MessageBox.Show(
                        LanguageManager.GetString("ClockEditor.SelectGenre", "⚠️ Seleziona un genere"),
                        LanguageManager.GetString("Common.Warning", "Attenzione"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }
                type = $"{source}_Genre";
                value = cmbGenre.SelectedItem.ToString();
            }

            bool yearFilterEnabled = radMusic.Checked && chkYearFilter.Checked;

            if (yearFilterEnabled && numYearFrom.Value > numYearTo.Value)
            {
                MessageBox.Show(
                    LanguageManager.GetString("ClockEditor.InvalidYearRange", "❌ Anno iniziale > anno finale"),
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            var newItem = new ClockItem(
                type,
                value,
                yearFilterEnabled,
                yearFilterEnabled ? (int)numYearFrom.Value : 0,
                yearFilterEnabled ? (int)numYearTo.Value : 0
            );

            _clockItems.Add(newItem);
            RefreshClockItemsList();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtClockName.Text))
            {
                MessageBox.Show(
                    LanguageManager.GetString("ClockEditor.EmptyClockName", "❌ Nome clock vuoto"),
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                txtClockName.Focus();
                return;
            }

            if (_clockItems.Count == 0)
            {
                MessageBox.Show(
                    LanguageManager.GetString("ClockEditor.AddAtLeastOneElement", "❌ Aggiungi almeno un elemento"),
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            _clock.ClockName = txtClockName.Text.Trim();
            _clock.Items = JsonConvert.SerializeObject(_clockItems);

            bool success;
            if (_isNewClock)
            {
                var existingClocks = DbcManager.LoadFromCsv<ClockEntry>("Clocks.dbc");
                if (existingClocks.Count == 0)
                    _clock.IsDefault = 1;

                success = DbcManager.Insert("Clocks.dbc", _clock);
            }
            else
            {
                success = DbcManager.Update("Clocks.dbc", _clock);
            }

            if (success)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show(
                    LanguageManager.GetString("ClockEditor.SaveError", "❌ Errore salvataggio"),
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                LanguageManager.SaveMissingKeysToFile();
                LanguageManager.LanguageChanged -= OnLanguageChanged;
            }
            base.Dispose(disposing);
        }
    }
}