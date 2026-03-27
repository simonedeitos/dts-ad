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
        public int NewYear { get; private set; }

        private CheckBox chkModifyGenre;
        private ComboBox cmbGenre;
        private CheckBox chkModifyCategory;
        private TextBox txtCategory;
        private Button btnCategoryDropdown;
        private CheckBox chkModifyYear;
        private NumericUpDown numYear;
        private Label lblTitle;
        private Button btnOK;
        private Button btnCancel;
        private string _archiveType;
        private List<string> _allCategoryNames = new List<string>();

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
                chkModifyYear.Text = LanguageManager.GetString("BatchEdit.ModifyYear", "Modifica Anno:");

            if (btnOK != null)
                btnOK.Text = "✓ " + LanguageManager.GetString("BatchEdit.Apply", "Applica");

            if (btnCancel != null)
                btnCancel.Text = "✖ " + LanguageManager.GetString("Common.Cancel", "Annulla");
        }

        private void InitializeComponent()
        {
            this.Size = new Size(450, 300);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = AppTheme.BgLight;

            lblTitle = new Label
            {
                Text = "Seleziona cosa modificare:",
                Location = new Point(20, 20),
                Size = new Size(400, 20),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            this.Controls.Add(lblTitle);

            chkModifyGenre = new CheckBox
            {
                Text = "Modifica Genere:",
                Location = new Point(20, 60),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 9)
            };
            chkModifyGenre.CheckedChanged += (s, e) => cmbGenre.Enabled = chkModifyGenre.Checked;
            this.Controls.Add(chkModifyGenre);

            cmbGenre = new ComboBox
            {
                Location = new Point(180, 58),
                Size = new Size(230, 25),
                Font = new Font("Segoe UI", 9),
                Enabled = false,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems
            };
            this.Controls.Add(cmbGenre);

            chkModifyCategory = new CheckBox
            {
                Text = "Modifica Categoria:",
                Location = new Point(20, 100),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 9)
            };
            chkModifyCategory.CheckedChanged += (s, e) =>
            {
                txtCategory.Enabled = chkModifyCategory.Checked;
                btnCategoryDropdown.Enabled = chkModifyCategory.Checked;
            };
            this.Controls.Add(chkModifyCategory);

            txtCategory = new TextBox
            {
                Location = new Point(180, 98),
                Size = new Size(200, 25),
                Font = new Font("Segoe UI", 9),
                Enabled = false
            };
            this.Controls.Add(txtCategory);

            btnCategoryDropdown = new Button
            {
                Text = "▼",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                Size = new Size(25, txtCategory.Height),
                Location = new Point(txtCategory.Right + 2, txtCategory.Top),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnCategoryDropdown.FlatAppearance.BorderSize = 1;
            btnCategoryDropdown.FlatAppearance.BorderColor = Color.Gray;
            btnCategoryDropdown.Click += BtnCategoryDropdown_Click;
            this.Controls.Add(btnCategoryDropdown);

            // ✅ Anno
            chkModifyYear = new CheckBox
            {
                Text = "Modifica Anno:",
                Location = new Point(20, 140),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 9)
            };
            chkModifyYear.CheckedChanged += (s, e) => numYear.Enabled = chkModifyYear.Checked;
            this.Controls.Add(chkModifyYear);

            numYear = new NumericUpDown
            {
                Location = new Point(180, 138),
                Size = new Size(100, 25),
                Font = new Font("Segoe UI", 9),
                Enabled = false,
                Minimum = 1900,
                Maximum = 2200,
                Value = DateTime.Now.Year
            };
            this.Controls.Add(numYear);

            btnOK = new Button
            {
                Text = "✓ Applica",
                Location = new Point(180, 200),
                Size = new Size(110, 35),
                BackColor = AppTheme.Success,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnOK.FlatAppearance.BorderSize = 0;
            btnOK.Click += BtnOK_Click;
            this.Controls.Add(btnOK);

            btnCancel = new Button
            {
                Text = "✖ Annulla",
                Location = new Point(300, 200),
                Size = new Size(110, 35),
                BackColor = AppTheme.Danger,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            this.Controls.Add(btnCancel);
        }

        private void BtnCategoryDropdown_Click(object sender, EventArgs e)
        {
            var popup = new ToolStripDropDown();
            popup.AutoClose = true;

            var panel = new Panel
            {
                BackColor = Color.FromArgb(45, 45, 45),
                Size = new Size(txtCategory.Width + 27, Math.Min(200, Math.Max(50, _allCategoryNames.Count * 22 + 10)))
            };

            var clb = new CheckedListBox
            {
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F),
                BorderStyle = BorderStyle.None,
                CheckOnClick = true,
                Dock = DockStyle.Fill
            };

            var currentCats = (txtCategory.Text ?? "")
                .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim())
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var cat in _allCategoryNames)
            {
                bool isChecked = currentCats.Contains(cat);
                clb.Items.Add(cat, isChecked);
            }

            clb.ItemCheck += (s2, ev2) =>
            {
                this.BeginInvoke((Action)(() =>
                {
                    var selected = new List<string>();
                    for (int i = 0; i < clb.Items.Count; i++)
                    {
                        bool check = clb.GetItemChecked(i);
                        if (i == ev2.Index)
                            check = (ev2.NewValue == CheckState.Checked);
                        if (check)
                            selected.Add(clb.Items[i].ToString());
                    }
                    txtCategory.Text = string.Join(";", selected);
                }));
            };

            panel.Controls.Add(clb);

            var host = new ToolStripControlHost(panel);
            host.AutoSize = true;
            host.Margin = Padding.Empty;
            popup.Items.Add(host);

            popup.Show(txtCategory, new Point(0, txtCategory.Height));
        }

        private void LoadExistingData()
        {
            try
            {
                List<string> allCategoryValues;

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

                    allCategoryValues = allMusic
                        .Select(m => m.Categories)
                        .Where(c => !string.IsNullOrWhiteSpace(c))
                        .ToList();
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

                    allCategoryValues = allClips
                        .Select(c => c.Categories)
                        .Where(c => !string.IsNullOrWhiteSpace(c))
                        .ToList();
                }

                // ✅ Splitta categorie separate da ; o , per ottenere singole categorie
                var catSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var catField in allCategoryValues)
                {
                    var parts = catField.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in parts)
                    {
                        string trimmed = part.Trim();
                        if (!string.IsNullOrWhiteSpace(trimmed))
                            catSet.Add(trimmed);
                    }
                }
                _allCategoryNames = catSet.OrderBy(c => c).ToList();
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
            NewCategory = txtCategory.Text.Trim();
            ModifyYear = chkModifyYear.Checked;
            NewYear = (int)numYear.Value;

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

            if (ModifyCategory && string.IsNullOrWhiteSpace(NewCategory))
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