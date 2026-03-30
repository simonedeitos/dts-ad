using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using AirDirector.Models;
using AirDirector.Services.Localization;

namespace AirDirector.Forms
{
    public partial class CompositionSettingsForm : Form
    {
        private DownloadTask _task;

        public CompositionSettingsForm(DownloadTask task)
        {
            _task = task;
            InitializeComponent();
            ApplyLanguage();

            LanguageManager.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            this.Text = LanguageManager.GetString("Composition.Title", "Configurazione Composizione Audio");

            if (lblTitle != null)
                lblTitle.Text = LanguageManager.GetString("Composition.Header", "Composizione Audio");

            if (groupBox1 != null)
                groupBox1.Text = LanguageManager.GetString("Composition.OpenerSection", "1.Jingle di Apertura");

            if (chkUseOpener != null)
                chkUseOpener.Text = LanguageManager.GetString("Composition.UseJingle", "Usa jingle");

            if (groupBox2 != null)
                groupBox2.Text = LanguageManager.GetString("Composition.MainSection", "2.File Principale");

            if (label10 != null)
                label10.Text = LanguageManager.GetString("Composition.MainFile", "File principale:");

            if (groupBox3 != null)
                groupBox3.Text = LanguageManager.GetString("Composition.BackgroundSection", "3.Base di Sottofondo");

            if (chkUseBackground != null)
                chkUseBackground.Text = LanguageManager.GetString("Composition.UseBase", "Usa base");

            if (label12 != null)
                label12.Text = LanguageManager.GetString("Composition.Volume", "Volume:");

            if (groupBox4 != null)
                groupBox4.Text = LanguageManager.GetString("Composition.CloserSection", "4.Jingle di Chiusura");

            if (chkUseCloser != null)
                chkUseCloser.Text = LanguageManager.GetString("Composition.UseJingle", "Usa jingle");

            if (groupBox5 != null)
                groupBox5.Text = LanguageManager.GetString("Composition.OutputSection", "File di Output");

            if (label11 != null)
                label11.Text = LanguageManager.GetString("Composition.OutputFile", "File di output:");

            if (chkBoostVolume != null)
                chkBoostVolume.Text = LanguageManager.GetString("Composition.BoostVolume", "Aumenta volume (-0.5db)");

            if (btnSave != null)
                btnSave.Text = LanguageManager.GetString("Common.Save", "Salva");

            if (btnCancel != null)
                btnCancel.Text = LanguageManager.GetString("Common.Cancel", "Annulla");
        }

        private void CompositionSettingsForm_Load(object sender, EventArgs e)
        {
            chkUseOpener.Checked = _task.UseOpener;
            txtOpenerFile.Text = _task.OpenerFilePath;

            txtMainFile.Text = _task.MainFilePath;

            chkUseBackground.Checked = _task.UseBackground;
            txtBackgroundFile.Text = _task.BackgroundFilePath;
            numVolume.Value = _task.BackgroundVolume;

            chkUseCloser.Checked = _task.UseCloser;
            txtCloserFile.Text = _task.CloserFilePath;

            txtOutputFile.Text = _task.OutputFilePath;
            chkBoostVolume.Checked = _task.BoostVolume;

            UpdateUIState();
        }

        private void UpdateUIState()
        {
            txtOpenerFile.Enabled = chkUseOpener.Checked;
            btnBrowseOpener.Enabled = chkUseOpener.Checked;

            txtBackgroundFile.Enabled = chkUseBackground.Checked;
            btnBrowseBackground.Enabled = chkUseBackground.Checked;
            numVolume.Enabled = chkUseBackground.Checked;

            txtCloserFile.Enabled = chkUseCloser.Checked;
            btnBrowseCloser.Enabled = chkUseCloser.Checked;
        }

        private void chkUseOpener_CheckedChanged(object sender, EventArgs e)
        {
            _task.UseOpener = chkUseOpener.Checked;
            UpdateUIState();
        }

        private void chkUseBackground_CheckedChanged(object sender, EventArgs e)
        {
            _task.UseBackground = chkUseBackground.Checked;
            UpdateUIState();
        }

        private void chkUseCloser_CheckedChanged(object sender, EventArgs e)
        {
            _task.UseCloser = chkUseCloser.Checked;
            UpdateUIState();
        }

        private void btnBrowseOpener_Click(object sender, EventArgs e)
        {
            BrowseForAudioFile(txtOpenerFile);
        }

        private void btnBrowseMain_Click(object sender, EventArgs e)
        {
            BrowseForAudioFile(txtMainFile);
        }

        private void btnBrowseBackground_Click(object sender, EventArgs e)
        {
            BrowseForAudioFile(txtBackgroundFile);
        }

        private void btnBrowseCloser_Click(object sender, EventArgs e)
        {
            BrowseForAudioFile(txtCloserFile);
        }

        private void btnBrowseOutput_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = LanguageManager.GetString("Composition.AudioFileMp3", "File audio (*.mp3)|*.mp3|Tutti i file (*.*)|*.*");
                dialog.Title = LanguageManager.GetString("Composition.SelectOutputPath", "Seleziona il percorso del file di output");

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtOutputFile.Text = dialog.FileName;
                }
            }
        }

        private void BrowseForAudioFile(TextBox textBox)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = LanguageManager.GetString("Composition.AudioFiles", "File audio (*.mp3;*.wav)|*.mp3;*.wav|Tutti i file (*.*)|*.*");
                dialog.Title = LanguageManager.GetString("Composition.SelectAudioFile", "Seleziona un file audio");

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    textBox.Text = dialog.FileName;
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtMainFile.Text))
            {
                MessageBox.Show(
                    LanguageManager.GetString("Composition.ErrorMainFile", "Inserisci il percorso del file principale per la composizione."),
                    LanguageManager.GetString("TaskEditor.RequiredField", "Campo richiesto"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                txtMainFile.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtOutputFile.Text))
            {
                MessageBox.Show(
                    LanguageManager.GetString("Composition.ErrorOutputFile", "Inserisci il percorso di output per la composizione."),
                    LanguageManager.GetString("TaskEditor.RequiredField", "Campo richiesto"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                txtOutputFile.Focus();
                return;
            }

            if (_task.UseOpener && string.IsNullOrWhiteSpace(txtOpenerFile.Text))
            {
                MessageBox.Show(
                    LanguageManager.GetString("Composition.ErrorOpenerFile", "Inserisci il percorso del file di apertura per la composizione."),
                    LanguageManager.GetString("TaskEditor.RequiredField", "Campo richiesto"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                txtOpenerFile.Focus();
                return;
            }

            if (_task.UseBackground && string.IsNullOrWhiteSpace(txtBackgroundFile.Text))
            {
                MessageBox.Show(
                    LanguageManager.GetString("Composition.ErrorBackgroundFile", "Inserisci il percorso del file di sottofondo per la composizione."),
                    LanguageManager.GetString("TaskEditor.RequiredField", "Campo richiesto"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                txtBackgroundFile.Focus();
                return;
            }

            if (_task.UseCloser && string.IsNullOrWhiteSpace(txtCloserFile.Text))
            {
                MessageBox.Show(
                    LanguageManager.GetString("Composition.ErrorCloserFile", "Inserisci il percorso del file di chiusura per la composizione."),
                    LanguageManager.GetString("TaskEditor.RequiredField", "Campo richiesto"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                txtCloserFile.Focus();
                return;
            }

            _task.OpenerFilePath = txtOpenerFile.Text;
            _task.MainFilePath = txtMainFile.Text;
            _task.BackgroundFilePath = txtBackgroundFile.Text;
            _task.BackgroundVolume = (int)numVolume.Value;
            _task.CloserFilePath = txtCloserFile.Text;
            _task.OutputFilePath = txtOutputFile.Text;
            _task.BoostVolume = chkBoostVolume.Checked;

            DialogResult = DialogResult.OK;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void numVolume_ValueChanged(object sender, EventArgs e)
        {
            _task.BackgroundVolume = (int)numVolume.Value;
        }

        private void txtMainFile_TextChanged(object sender, EventArgs e)
        {
            _task.MainFilePath = txtMainFile.Text;
        }

        private void txtOpenerFile_TextChanged(object sender, EventArgs e)
        {
            _task.OpenerFilePath = txtOpenerFile.Text;
        }

        private void txtBackgroundFile_TextChanged(object sender, EventArgs e)
        {
            _task.BackgroundFilePath = txtBackgroundFile.Text;
        }

        private void txtCloserFile_TextChanged(object sender, EventArgs e)
        {
            _task.CloserFilePath = txtCloserFile.Text;
        }

        private void txtOutputFile_TextChanged(object sender, EventArgs e)
        {
            _task.OutputFilePath = txtOutputFile.Text;
        }

        private void chkBoostVolume_CheckedChanged(object sender, EventArgs e)
        {
            _task.BoostVolume = chkBoostVolume.Checked;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                LanguageManager.SaveMissingKeysToFile();
                LanguageManager.LanguageChanged -= OnLanguageChanged;

                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}