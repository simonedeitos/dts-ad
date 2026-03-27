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

        private CheckBox chkModifyGenre;
        private ComboBox cmbGenre;
        private CheckBox chkModifyCategory;
        private ComboBox cmbCategory;
        private Label lblTitle;
        private Button btnOK;
        private Button btnCancel;
        private string _archiveType;

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

            if (btnOK != null)
                btnOK.Text = "✓ " + LanguageManager.GetString("BatchEdit.Apply", "Applica");

            if (btnCancel != null)
                btnCancel.Text = "✖ " + LanguageManager.GetString("Common.Cancel", "Annulla");
        }

        private void InitializeComponent()
        {
            this.Size = new Size(450, 250);
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
            chkModifyCategory.CheckedChanged += (s, e) => cmbCategory.Enabled = chkModifyCategory.Checked;
            this.Controls.Add(chkModifyCategory);

            cmbCategory = new ComboBox
            {
                Location = new Point(180, 98),
                Size = new Size(230, 25),
                Font = new Font("Segoe UI", 9),
                Enabled = false,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems
            };
            this.Controls.Add(cmbCategory);

            btnOK = new Button
            {
                Text = "✓ Applica",
                Location = new Point(180, 160),
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
                Location = new Point(300, 160),
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

        private void LoadExistingData()
        {
            try
            {
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

                    var categories = allMusic
                        .Select(m => m.Categories)
                        .Where(c => !string.IsNullOrWhiteSpace(c))
                        .Distinct()
                        .OrderBy(c => c)
                        .ToList();

                    cmbCategory.Items.Clear();
                    foreach (var category in categories)
                    {
                        cmbCategory.Items.Add(category);
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

                    var categories = allClips
                        .Select(c => c.Categories)
                        .Where(c => !string.IsNullOrWhiteSpace(c))
                        .Distinct()
                        .OrderBy(c => c)
                        .ToList();

                    cmbCategory.Items.Clear();
                    foreach (var category in categories)
                    {
                        cmbCategory.Items.Add(category);
                    }
                }
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
            NewCategory = cmbCategory.Text.Trim();

            if (!ModifyGenre && !ModifyCategory)
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