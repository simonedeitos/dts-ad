using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AirDirector.Services.Database;
using AirDirector.Services.Localization;
using AirDirector.Themes;

namespace AirDirector.Forms
{
    public partial class BatchEditForm : Form
    {
        public bool ModifyGenre { get; private set; }
        public string NewGenre { get; private set; }
        public bool ModifyCategory { get; private set; }
        public string NewCategory { get; private set; }
        public bool ModifyYear { get; private set; }
        public int? NewYear { get; private set; }

        private CheckBox chkModifyGenre;
        private ComboBox cmbGenre;
        private CheckBox chkModifyCategory;
        private TextBox txtCategoriesDisplay;
        private Button btnCategoriesDropdown;
        private CheckBox chkModifyYear;
        private NumericUpDown numYear;
        private Label lblTitle;
        private Button btnOK;
        private Button btnCancel;
        private string _archiveType;

        private List<string> _allCategories = new List<string>();
        private HashSet<string> _checkedCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public BatchEditForm(string archiveType)
        {
            _archiveType = archiveType;

            InitializeComponent();
            ApplyLanguage();
            LoadExistingData();

            LanguageManager.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            this.Text = string.Format(LanguageManager.GetString("BatchEdit.Title", "Modifica Genere/Categoria - {0}"), _archiveType);

            if (lblTitle != null)
                lblTitle.Text = LanguageManager.GetString("BatchEdit.SelectWhat", "Seleziona cosa modificare:");

            if (chkModifyGenre != null)
                chkModifyGenre.Text = LanguageManager.GetString("BatchEdit.ModifyGenre", "Modifica Genere:");

            if (chkModifyCategory != null)
                chkModifyCategory.Text = LanguageManager.GetString("BatchEdit.ModifyCategory", "Modifica Categoria:");

            if (chkModifyYear != null)
                chkModifyYear.Text = LanguageManager.GetString("Archive.ColumnYear", "Anno") + ":";

            if (btnOK != null)
                btnOK.Text = "✓ " + LanguageManager.GetString("BatchEdit.Apply", "Applica");

            if (btnCancel != null)
                btnCancel.Text = "✖ " + LanguageManager.GetString("Common.Cancel", "Annulla");
        }

        private void InitializeComponent()
        {
            this.Size = new Size(500, 310);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = AppTheme.BgLight;
            this.Padding = new Padding(20, 15, 20, 15);

            lblTitle = new Label
            {
                Text = "Seleziona cosa modificare:",
                Dock = DockStyle.Top,
                Height = 28,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Padding = new Padding(0, 0, 0, 6)
            };

            // --- Content panel con TableLayout ---
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 3,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34));

            // --- Genere (riga 0) ---
            chkModifyGenre = new CheckBox
            {
                Text = "Modifica Genere:",
                AutoSize = true,
                Font = new Font("Segoe UI", 9),
                Margin = new Padding(0, 6, 8, 6),
                Anchor = AnchorStyles.Left
            };
            chkModifyGenre.CheckedChanged += (s, e) => cmbGenre.Enabled = chkModifyGenre.Checked;

            cmbGenre = new ComboBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9),
                Enabled = false,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                Margin = new Padding(0, 6, 0, 6)
            };

            tbl.Controls.Add(chkModifyGenre, 0, 0);
            tbl.Controls.Add(cmbGenre, 1, 0);
            tbl.SetColumnSpan(cmbGenre, 2);

            // --- Categorie (riga 1) ---
            chkModifyCategory = new CheckBox
            {
                Text = "Modifica Categoria:",
                AutoSize = true,
                Font = new Font("Segoe UI", 9),
                Margin = new Padding(0, 6, 8, 6),
                Anchor = AnchorStyles.Left
            };
            chkModifyCategory.CheckedChanged += (s, e) =>
            {
                txtCategoriesDisplay.Enabled = chkModifyCategory.Checked;
                btnCategoriesDropdown.Enabled = chkModifyCategory.Checked;
            };

            txtCategoriesDisplay = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9),
                Enabled = false,
                ReadOnly = true,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 6, 0, 6)
            };
            txtCategoriesDisplay.Click += (s, e) => { if (txtCategoriesDisplay.Enabled) ShowCategoryPopup(); };

            btnCategoriesDropdown = new Button
            {
                Text = "▼",
                Size = new Size(30, 25),
                Font = new Font("Segoe UI", 9),
                FlatStyle = FlatStyle.Flat,
                Enabled = false,
                Margin = new Padding(4, 6, 0, 6)
            };
            btnCategoriesDropdown.Click += (s, e) => ShowCategoryPopup();

            tbl.Controls.Add(chkModifyCategory, 0, 1);
            tbl.Controls.Add(txtCategoriesDisplay, 1, 1);
            tbl.Controls.Add(btnCategoriesDropdown, 2, 1);

            // --- Anno (riga 2) ---
            chkModifyYear = new CheckBox
            {
                Text = "Anno:",
                AutoSize = true,
                Font = new Font("Segoe UI", 9),
                Margin = new Padding(0, 6, 8, 6),
                Anchor = AnchorStyles.Left
            };
            chkModifyYear.CheckedChanged += (s, e) => numYear.Enabled = chkModifyYear.Checked;

            numYear = new NumericUpDown
            {
                Font = new Font("Segoe UI", 9),
                Enabled = false,
                Minimum = 1900,
                Maximum = 2100,
                Value = DateTime.Now.Year,
                Size = new Size(100, 25),
                Margin = new Padding(0, 6, 0, 6),
                Anchor = AnchorStyles.Left
            };

            tbl.Controls.Add(chkModifyYear, 0, 2);
            tbl.Controls.Add(numYear, 1, 2);

            // --- Bottoni ---
            var pnlButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Padding = new Padding(0, 10, 0, 0)
            };

            btnCancel = new Button
            {
                Text = "✖ Annulla",
                Size = new Size(120, 38),
                BackColor = AppTheme.Danger,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 0, 0)
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            btnOK = new Button
            {
                Text = "✓ Applica",
                Size = new Size(120, 38),
                BackColor = AppTheme.Success,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 8, 0)
            };
            btnOK.FlatAppearance.BorderSize = 0;
            btnOK.Click += BtnOK_Click;

            pnlButtons.Controls.Add(btnCancel);
            pnlButtons.Controls.Add(btnOK);

            this.Controls.Add(tbl);
            this.Controls.Add(pnlButtons);
            this.Controls.Add(lblTitle);
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
                Text = LanguageManager.GetString("BatchEdit.ModifyCategory", "Modifica Categoria:"),
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

            var screenPos = txtCategoriesDisplay.PointToScreen(new Point(0, txtCategoriesDisplay.Height));
            popup.Location = screenPos;

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
                Console.WriteLine($"[BatchEdit] ⚠️ Errore salvataggio nuova categoria: {ex.Message}");
            }
        }

        private void LoadExistingData()
        {
            try
            {
                var allCats = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                if (_archiveType == "Music")
                {
                    var allMusic = DbcManager.LoadFromCsv<MusicEntry>("Music.dbc");

                    var genres = allMusic
                        .Select(m => m.Genre)
                        .Where(g => !string.IsNullOrWhiteSpace(g))
                        .Distinct()
                        .OrderBy(g => g)
                        .ToList();

                    cmbGenre.Items.Clear();
                    foreach (var genre in genres)
                    {
                        cmbGenre.Items.Add(genre);
                    }

                    foreach (var m in allMusic)
                    {
                        if (string.IsNullOrWhiteSpace(m.Categories)) continue;
                        foreach (var part in m.Categories.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            string trimmed = part.Trim();
                            if (!string.IsNullOrWhiteSpace(trimmed))
                                allCats.Add(trimmed);
                        }
                    }
                }
                else
                {
                    var allClips = DbcManager.LoadFromCsv<ClipEntry>("Clips.dbc");

                    var genres = allClips
                        .Select(c => c.Genre)
                        .Where(g => !string.IsNullOrWhiteSpace(g))
                        .Distinct()
                        .OrderBy(g => g)
                        .ToList();

                    cmbGenre.Items.Clear();
                    foreach (var genre in genres)
                    {
                        cmbGenre.Items.Add(genre);
                    }

                    foreach (var c in allClips)
                    {
                        if (string.IsNullOrWhiteSpace(c.Categories)) continue;
                        foreach (var part in c.Categories.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            string trimmed = part.Trim();
                            if (!string.IsNullOrWhiteSpace(trimmed))
                                allCats.Add(trimmed);
                        }
                    }
                }

                // Aggiungi categorie da Categories.dbc
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
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(LanguageManager.GetString("BatchEdit.LoadError", "Errore caricamento generi/categorie:\n{0}"), ex.Message),
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            ModifyGenre = chkModifyGenre.Checked;
            NewGenre = cmbGenre.Text.Trim();
            ModifyCategory = chkModifyCategory.Checked;
            ModifyYear = chkModifyYear.Checked;

            // Categorie: join degli elementi selezionati con punto e virgola
            NewCategory = _checkedCategories.Count > 0
                ? string.Join(";", _checkedCategories.OrderBy(c => c))
                : "";

            // Anno: valore dal NumericUpDown
            if (ModifyYear)
            {
                NewYear = (int)numYear.Value;
            }

            if (!ModifyGenre && !ModifyCategory && !ModifyYear)
            {
                MessageBox.Show(
                    LanguageManager.GetString("BatchEdit.SelectAtLeastOne", "Seleziona almeno una modifica!"),
                    LanguageManager.GetString("Common.Warning", "Attenzione"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (ModifyGenre && string.IsNullOrWhiteSpace(NewGenre))
            {
                MessageBox.Show(
                    LanguageManager.GetString("BatchEdit.EnterGenre", "Inserisci il nuovo genere!"),
                    LanguageManager.GetString("Common.Warning", "Attenzione"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (ModifyCategory && _checkedCategories.Count == 0)
            {
                MessageBox.Show(
                    LanguageManager.GetString("BatchEdit.EnterCategory", "Inserisci la nuova categoria!"),
                    LanguageManager.GetString("Common.Warning", "Attenzione"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            this.DialogResult = DialogResult.OK;
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
