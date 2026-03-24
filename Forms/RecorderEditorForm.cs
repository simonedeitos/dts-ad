using AirDirector.Models;
using AirDirector.Services.Database;
using AirDirector.Services.Localization;
using AirDirector.Themes;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace AirDirector.Forms
{
    public partial class RecorderEditorForm : Form
    {
        private Recorder _recorder;
        private bool _isEditMode;

        public RecorderEditorForm(Recorder recorder = null)
        {
            InitializeComponent();

            _isEditMode = recorder != null;
            _recorder = recorder ?? new Recorder
            {
                ID = RecorderConfigManager.GetNextRecorderId(),
                Name = "Nuovo Recorder",
                Type = Recorder.RecorderType.Manual,
                OutputPath = @"C:\AirDirector\Recordings",
                Format = Recorder.AudioFormat.MP3_128_Stereo,
                RetentionDays = 90,
                AutoDeleteOldFiles = true
            };

            ApplyTheme();
            LoadAudioDevices();
            LoadRecorderData();
            ApplyLanguage();

            LanguageManager.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            this.Text = _isEditMode ?
                LanguageManager.GetString("RecorderEditor.EditRecorder", "Modifica Recorder") :
                LanguageManager.GetString("RecorderEditor.NewRecorder", "Nuovo Recorder");

            if (lblName != null)
                lblName.Text = LanguageManager.GetString("RecorderEditor.RecorderName", "Nome Recorder:");

            if (lblType != null)
                lblType.Text = LanguageManager.GetString("RecorderEditor.Type", "Tipo:");

            if (lblAudioDevice != null)
                lblAudioDevice.Text = LanguageManager.GetString("RecorderEditor.AudioDevice", "Dispositivo Audio:");

            if (lblOutputPath != null)
                lblOutputPath.Text = LanguageManager.GetString("RecorderEditor.SavePath", "Percorso Salvataggio:");

            if (lblFormat != null)
                lblFormat.Text = LanguageManager.GetString("RecorderEditor.AudioFormat", "Formato Audio:");

            // Combo Type
            if (cmbType != null && cmbType.Items.Count == 3)
            {
                int selectedIndex = cmbType.SelectedIndex;
                cmbType.Items.Clear();
                cmbType.Items.Add(LanguageManager.GetString("RecorderEditor.TypeManual", "Manuale"));
                cmbType.Items.Add(LanguageManager.GetString("RecorderEditor.Type90Days", "90 Giorni (file orari)"));
                cmbType.Items.Add(LanguageManager.GetString("RecorderEditor.TypeScheduled", "Schedulato"));
                if (selectedIndex >= 0 && selectedIndex < 3)
                    cmbType.SelectedIndex = selectedIndex;
            }

            // Combo Format
            if (cmbFormat != null && cmbFormat.Items.Count == 8)
            {
                int selectedIndex = cmbFormat.SelectedIndex;
                cmbFormat.Items.Clear();
                cmbFormat.Items.Add("MP3 64kbps " + LanguageManager.GetString("Recorder.Mono", "Mono"));
                cmbFormat.Items.Add("MP3 64kbps " + LanguageManager.GetString("Recorder.Stereo", "Stereo"));
                cmbFormat.Items.Add("MP3 128kbps " + LanguageManager.GetString("Recorder.Mono", "Mono"));
                cmbFormat.Items.Add("MP3 128kbps " + LanguageManager.GetString("Recorder.Stereo", "Stereo"));
                cmbFormat.Items.Add("MP3 256kbps " + LanguageManager.GetString("Recorder.Mono", "Mono"));
                cmbFormat.Items.Add("MP3 256kbps " + LanguageManager.GetString("Recorder.Stereo", "Stereo"));
                cmbFormat.Items.Add("MP3 320kbps " + LanguageManager.GetString("Recorder.Mono", "Mono"));
                cmbFormat.Items.Add("MP3 320kbps " + LanguageManager.GetString("Recorder.Stereo", "Stereo"));
                if (selectedIndex >= 0 && selectedIndex < 8)
                    cmbFormat.SelectedIndex = selectedIndex;
            }

            // Schedule Panel
            if (lblScheduleTitle != null)
                lblScheduleTitle.Text = "⏰ " + LanguageManager.GetString("RecorderEditor.Schedule", "SCHEDULAZIONE");

            if (chkMonday != null)
                chkMonday.Text = LanguageManager.GetString("Download.DayMon", "Lun");

            if (chkTuesday != null)
                chkTuesday.Text = LanguageManager.GetString("Download.DayTue", "Mar");

            if (chkWednesday != null)
                chkWednesday.Text = LanguageManager.GetString("Download.DayWed", "Mer");

            if (chkThursday != null)
                chkThursday.Text = LanguageManager.GetString("Download.DayThu", "Gio");

            if (chkFriday != null)
                chkFriday.Text = LanguageManager.GetString("Download.DayFri", "Ven");

            if (chkSaturday != null)
                chkSaturday.Text = LanguageManager.GetString("Download.DaySat", "Sab");

            if (chkSunday != null)
                chkSunday.Text = LanguageManager.GetString("Download.DaySun", "Dom");

            if (lblStartTime != null)
                lblStartTime.Text = LanguageManager.GetString("RecorderEditor.StartTime", "Ora Inizio:");

            if (lblEndTime != null)
                lblEndTime.Text = LanguageManager.GetString("RecorderEditor.EndTime", "Ora Fine:");

            // 90 Days Panel
            if (lbl90Title != null)
                lbl90Title.Text = "📼 " + LanguageManager.GetString("RecorderEditor.Recording90Days", "REGISTRAZIONE 90 GIORNI");

            if (lblRetention != null)
                lblRetention.Text = LanguageManager.GetString("RecorderEditor.RetentionDays", "Giorni di conservazione:");

            if (chkAutoDelete != null)
                chkAutoDelete.Text = LanguageManager.GetString("RecorderEditor.AutoDeleteOld", "Elimina automaticamente file vecchi");

            // Buttons
            if (btnSave != null)
                btnSave.Text = "💾 " + LanguageManager.GetString("Common.Save", "Salva");

            if (btnCancel != null)
                btnCancel.Text = "❌ " + LanguageManager.GetString("Common.Cancel", "Annulla");
        }

        private void ApplyTheme()
        {
            this.BackColor = AppTheme.BgLight;

            btnSave.BackColor = AppTheme.Success;
            btnSave.ForeColor = AppTheme.TextInverse;
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.FlatAppearance.BorderSize = 0;

            btnCancel.BackColor = AppTheme.Danger;
            btnCancel.ForeColor = AppTheme.TextInverse;
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.FlatAppearance.BorderSize = 0;

            btnBrowse.BackColor = AppTheme.Info;
            btnBrowse.ForeColor = AppTheme.TextInverse;
            btnBrowse.FlatStyle = FlatStyle.Flat;
            btnBrowse.FlatAppearance.BorderSize = 0;

            pnlSchedule.BackColor = AppTheme.Surface;
            pnl90Days.BackColor = AppTheme.Surface;

            lblScheduleTitle.ForeColor = AppTheme.Primary;
            lbl90Title.ForeColor = AppTheme.Primary;
        }

        private void LoadAudioDevices()
        {
            cmbAudioDevice.Items.Clear();

            try
            {
                for (int i = 0; i < WaveIn.DeviceCount; i++)
                {
                    var caps = WaveIn.GetCapabilities(i);
                    cmbAudioDevice.Items.Add(caps.ProductName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(LanguageManager.GetString("RecorderEditor.LoadDevicesError", "Errore caricamento dispositivi audio:\n{0}"), ex.Message),
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }

            if (cmbAudioDevice.Items.Count > 0)
                cmbAudioDevice.SelectedIndex = 0;
        }

        private void LoadRecorderData()
        {
            txtName.Text = _recorder.Name;
            cmbType.SelectedIndex = (int)_recorder.Type;
            txtOutputPath.Text = _recorder.OutputPath;
            cmbFormat.SelectedIndex = (int)_recorder.Format;

            if (!string.IsNullOrEmpty(_recorder.AudioSourceDevice))
            {
                for (int i = 0; i < cmbAudioDevice.Items.Count; i++)
                {
                    if (cmbAudioDevice.Items[i].ToString().Contains(_recorder.AudioSourceDevice))
                    {
                        cmbAudioDevice.SelectedIndex = i;
                        break;
                    }
                }
            }

            chkMonday.Checked = _recorder.Monday;
            chkTuesday.Checked = _recorder.Tuesday;
            chkWednesday.Checked = _recorder.Wednesday;
            chkThursday.Checked = _recorder.Thursday;
            chkFriday.Checked = _recorder.Friday;
            chkSaturday.Checked = _recorder.Saturday;
            chkSunday.Checked = _recorder.Sunday;
            dtpStartTime.Value = DateTime.Today.Add(_recorder.StartTime);
            dtpEndTime.Value = DateTime.Today.Add(_recorder.EndTime);

            numRetentionDays.Value = _recorder.RetentionDays;
            chkAutoDelete.Checked = _recorder.AutoDeleteOldFiles;
        }

        private void CmbType_SelectedIndexChanged(object sender, EventArgs e)
        {
            pnlSchedule.Visible = cmbType.SelectedIndex == 2;
            pnl90Days.Visible = cmbType.SelectedIndex == 1;

            if (pnlSchedule.Visible)
            {
                this.ClientSize = new Size(580, 640);
                btnSave.Location = new Point(350, 580);
                btnCancel.Location = new Point(460, 580);
            }
            else if (pnl90Days.Visible)
            {
                this.ClientSize = new Size(580, 540);
                btnSave.Location = new Point(350, 480);
                btnCancel.Location = new Point(460, 480);
            }
            else
            {
                this.ClientSize = new Size(580, 470);
                btnSave.Location = new Point(350, 410);
                btnCancel.Location = new Point(460, 410);
            }
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = LanguageManager.GetString("RecorderEditor.SelectSaveFolder", "Seleziona cartella di salvataggio");
                fbd.SelectedPath = txtOutputPath.Text;

                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtOutputPath.Text = fbd.SelectedPath;
                }
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    MessageBox.Show(
                        LanguageManager.GetString("RecorderEditor.ErrorRecorderName", "Inserisci un nome per il recorder."),
                        LanguageManager.GetString("Common.Error", "Errore"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    txtName.Focus();
                    return;
                }

                if (string.IsNullOrEmpty(txtOutputPath.Text))
                {
                    MessageBox.Show(
                        LanguageManager.GetString("RecorderEditor.ErrorSavePath", "Seleziona un percorso di salvataggio."),
                        LanguageManager.GetString("Common.Error", "Errore"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                _recorder.Name = txtName.Text.Trim();
                _recorder.Type = (Recorder.RecorderType)cmbType.SelectedIndex;
                _recorder.AudioSourceDevice = cmbAudioDevice.SelectedItem?.ToString() ?? "";
                _recorder.OutputPath = txtOutputPath.Text;
                _recorder.Format = (Recorder.AudioFormat)cmbFormat.SelectedIndex;

                _recorder.Monday = chkMonday.Checked;
                _recorder.Tuesday = chkTuesday.Checked;
                _recorder.Wednesday = chkWednesday.Checked;
                _recorder.Thursday = chkThursday.Checked;
                _recorder.Friday = chkFriday.Checked;
                _recorder.Saturday = chkSaturday.Checked;
                _recorder.Sunday = chkSunday.Checked;
                _recorder.StartTime = dtpStartTime.Value.TimeOfDay;
                _recorder.EndTime = dtpEndTime.Value.TimeOfDay;

                _recorder.RetentionDays = (int)numRetentionDays.Value;
                _recorder.AutoDeleteOldFiles = chkAutoDelete.Checked;

                RecorderConfigManager.SaveRecorder(_recorder);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(LanguageManager.GetString("RecorderEditor.SaveError", "Errore salvataggio recorder:\n{0}"), ex.Message),
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        public Recorder GetRecorder()
        {
            return _recorder;
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