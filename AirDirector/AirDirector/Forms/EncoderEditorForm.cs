using AirDirector.Services.Database;
using AirDirector.Services.Localization;
using AirDirector.Themes;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AirDirector.Forms
{
    public partial class EncoderEditorForm : Form
    {
        private EncoderEntry _encoder;
        private bool _isNewEncoder;

        public EncoderEditorForm(EncoderEntry encoder = null)
        {
            InitializeComponent();
            _isNewEncoder = encoder == null;
            _encoder = encoder ?? new EncoderEntry();

            LanguageManager.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            this.Text = _isNewEncoder ?
                LanguageManager.GetString("EncoderEditor.NewEncoder", "Nuovo Encoder") :
                string.Format(LanguageManager.GetString("EncoderEditor.EditEncoder", "Modifica Encoder - {0}"), _encoder.Name);

            if (lblName != null)
                lblName.Text = LanguageManager.GetString("EncoderEditor.EncoderName", "Nome Encoder:");

            if (lblStationName != null)
                lblStationName.Text = LanguageManager.GetString("EncoderEditor.StationName", "Nome Stazione:");

            if (lblAudioSource != null)
                lblAudioSource.Text = LanguageManager.GetString("EncoderEditor.AudioSource", "Sorgente Audio:");

            if (lblServerUrl != null)
                lblServerUrl.Text = LanguageManager.GetString("EncoderEditor.ServerUrl", "Server URL:");

            if (lblServerPort != null)
                lblServerPort.Text = LanguageManager.GetString("EncoderEditor.Port", "Porta:");

            if (lblUsername != null)
                lblUsername.Text = LanguageManager.GetString("EncoderEditor.Username", "Username:");

            if (lblPassword != null)
                lblPassword.Text = LanguageManager.GetString("EncoderEditor.Password", "Password:");

            if (lblMountPoint != null)
                lblMountPoint.Text = LanguageManager.GetString("EncoderEditor.MountPoint", "Mount Point:");

            if (lblBitrate != null)
                lblBitrate.Text = LanguageManager.GetString("EncoderEditor.Bitrate", "Bitrate:");

            if (btnSetLocalServer != null)
                btnSetLocalServer.Text = "🖥️ " + LanguageManager.GetString("EncoderEditor.LocalServer", "Server Locale");

            if (chkEnableAGC != null)
                chkEnableAGC.Text = LanguageManager.GetString("EncoderEditor.EnableAGC", "Abilita AGC + Limiter");

            if (lblAGCTarget != null)
                lblAGCTarget.Text = LanguageManager.GetString("EncoderEditor.AGCTarget", "AGC Target:");

            if (lblAGCAttack != null)
                lblAGCAttack.Text = LanguageManager.GetString("EncoderEditor.AGCAttack", "AGC Attack:");

            if (lblAGCRelease != null)
                lblAGCRelease.Text = LanguageManager.GetString("EncoderEditor.AGCRelease", "AGC Release:");

            if (lblLimiterThreshold != null)
                lblLimiterThreshold.Text = LanguageManager.GetString("EncoderEditor.LimiterThreshold", "Limiter Threshold:");

            if (btnResetAGC != null)
                btnResetAGC.Text = "🔄 " + LanguageManager.GetString("EncoderEditor.ResetDefault", "Reset Default");

            if (btnSave != null)
                btnSave.Text = "💾 " + LanguageManager.GetString("Common.Save", "Salva");

            if (btnCancel != null)
                btnCancel.Text = "❌ " + LanguageManager.GetString("Common.Cancel", "Annulla");

            // Aggiorna combo audio source
            if (cboAudioSource != null && cboAudioSource.Items.Count > 0)
            {
                int selectedIndex = cboAudioSource.SelectedIndex;
                string firstItem = LanguageManager.GetString("EncoderEditor.SelectDevice", "(Seleziona dispositivo)");
                if (cboAudioSource.Items.Count > 0)
                {
                    cboAudioSource.Items[0] = firstItem;
                    if (selectedIndex >= 0)
                        cboAudioSource.SelectedIndex = selectedIndex;
                }
            }
        }

        private void EncoderEditorForm_Load(object sender, EventArgs e)
        {
            LoadAudioDevices();
            LoadBitrates();

            if (!_isNewEncoder)
            {
                txtName.Text = _encoder.Name;
                txtStationName.Text = _encoder.StationName;
                txtServerUrl.Text = _encoder.ServerUrl;
                txtServerPort.Text = _encoder.ServerPort.ToString();
                txtUsername.Text = _encoder.Username;
                txtPassword.Text = _encoder.Password;
                txtMountPoint.Text = _encoder.MountPoint;

                SelectAudioDevice(_encoder.AudioSourceDevice);
                SelectBitrate(_encoder.Bitrate);

                chkEnableAGC.Checked = _encoder.EnableAGC;
                trackAGCTarget.Value = (int)(_encoder.AGCTargetLevel * 100);
                trackAGCAttack.Value = (int)(_encoder.AGCAttackTime * 10);
                trackAGCRelease.Value = (int)(_encoder.AGCReleaseTime * 10);
                trackLimiterThreshold.Value = (int)(_encoder.LimiterThreshold * 100);
            }
            else
            {
                txtServerUrl.Text = "127.0.0.1";
                txtServerPort.Text = "8000";
                txtUsername.Text = "source";
                txtPassword.Text = "source";

                chkEnableAGC.Checked = false;
                trackAGCTarget.Value = 20;
                trackAGCAttack.Value = 5;
                trackAGCRelease.Value = 30;
                trackLimiterThreshold.Value = 95;
            }

            UpdateAGCControlLabels();
            EnableAGCControls(chkEnableAGC.Checked);
            ApplyLanguage();
        }

        private void LoadAudioDevices()
        {
            cboAudioSource.Items.Clear();
            cboAudioSource.Items.Add(LanguageManager.GetString("EncoderEditor.SelectDevice", "(Seleziona dispositivo)"));

            for (int i = 0; i < WaveIn.DeviceCount; i++)
            {
                string deviceName = WaveIn.GetCapabilities(i).ProductName;
                cboAudioSource.Items.Add(deviceName);
            }

            using (var enumerator = new MMDeviceEnumerator())
            {
                foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
                {
                    string deviceName = "WASAPI - " + device.FriendlyName;
                    cboAudioSource.Items.Add(deviceName);
                    device.Dispose();
                }
            }

            if (cboAudioSource.Items.Count > 0)
                cboAudioSource.SelectedIndex = 0;
        }

        private void LoadBitrates()
        {
            cboBitrate.Items.Clear();
            cboBitrate.Items.Add("128 kbps");
            cboBitrate.Items.Add("256 kbps");
            cboBitrate.Items.Add("320 kbps");
            cboBitrate.SelectedIndex = 0;
        }

        private void SelectAudioDevice(string deviceName)
        {
            if (string.IsNullOrEmpty(deviceName))
                return;

            for (int i = 0; i < cboAudioSource.Items.Count; i++)
            {
                if (cboAudioSource.Items[i].ToString() == deviceName)
                {
                    cboAudioSource.SelectedIndex = i;
                    return;
                }
            }
        }

        private void SelectBitrate(int bitrate)
        {
            switch (bitrate)
            {
                case 128:
                    cboBitrate.SelectedIndex = 0;
                    break;
                case 256:
                    cboBitrate.SelectedIndex = 1;
                    break;
                case 320:
                    cboBitrate.SelectedIndex = 2;
                    break;
                default:
                    cboBitrate.SelectedIndex = 0;
                    break;
            }
        }

        private void UpdateAGCControlLabels()
        {
            lblAGCTargetValue.Text = $"{trackAGCTarget.Value}%";
            lblAGCAttackValue.Text = $"{trackAGCAttack.Value / 10.0:F1}s";
            lblAGCReleaseValue.Text = $"{trackAGCRelease.Value / 10.0:F1}s";
            lblLimiterThresholdValue.Text = $"{trackLimiterThreshold.Value}%";
        }

        private void EnableAGCControls(bool enable)
        {
            trackAGCTarget.Enabled = enable;
            trackAGCAttack.Enabled = enable;
            trackAGCRelease.Enabled = enable;
            trackLimiterThreshold.Enabled = enable;
            lblAGCTarget.Enabled = enable;
            lblAGCAttack.Enabled = enable;
            lblAGCRelease.Enabled = enable;
            lblLimiterThreshold.Enabled = enable;
            lblAGCTargetValue.Enabled = enable;
            lblAGCAttackValue.Enabled = enable;
            lblAGCReleaseValue.Enabled = enable;
            lblLimiterThresholdValue.Enabled = enable;
        }

        private void chkEnableAGC_CheckedChanged(object sender, EventArgs e)
        {
            EnableAGCControls(chkEnableAGC.Checked);
        }

        private void trackAGCTarget_ValueChanged(object sender, EventArgs e)
        {
            UpdateAGCControlLabels();
        }

        private void trackAGCAttack_ValueChanged(object sender, EventArgs e)
        {
            UpdateAGCControlLabels();
        }

        private void trackAGCRelease_ValueChanged(object sender, EventArgs e)
        {
            UpdateAGCControlLabels();
        }

        private void trackLimiterThreshold_ValueChanged(object sender, EventArgs e)
        {
            UpdateAGCControlLabels();
        }

        private void btnResetAGC_Click(object sender, EventArgs e)
        {
            trackAGCTarget.Value = 20;
            trackAGCAttack.Value = 5;
            trackAGCRelease.Value = 30;
            trackLimiterThreshold.Value = 95;
            UpdateAGCControlLabels();
        }

        private void btnSetLocalServer_Click(object sender, EventArgs e)
        {
            txtServerUrl.Text = "127.0.0.1";
            txtServerPort.Text = "8000";
            txtUsername.Text = "source";
            txtPassword.Text = "source";
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show(
                    LanguageManager.GetString("EncoderEditor.ErrorEncoderName", "Inserire un nome per l'encoder"),
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtStationName.Text))
            {
                MessageBox.Show(
                    LanguageManager.GetString("EncoderEditor.ErrorStationName", "Inserire il nome della stazione"),
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (cboAudioSource.SelectedIndex <= 0)
            {
                MessageBox.Show(
                    LanguageManager.GetString("EncoderEditor.ErrorSelectDevice", "Selezionare un dispositivo audio"),
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtMountPoint.Text))
            {
                MessageBox.Show(
                    LanguageManager.GetString("EncoderEditor.ErrorMountPoint", "Inserire un mountpoint valido"),
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (_isNewEncoder)
            {
                _encoder.ID = EncoderConfigManager.GetNextEncoderId();
            }

            _encoder.Name = txtName.Text.Trim();
            _encoder.StationName = txtStationName.Text.Trim();
            _encoder.AudioSourceDevice = cboAudioSource.SelectedItem.ToString();
            _encoder.ServerUrl = txtServerUrl.Text.Trim();

            if (int.TryParse(txtServerPort.Text, out int port))
            {
                _encoder.ServerPort = port;
                _encoder.Port = port;
            }

            _encoder.Username = txtUsername.Text.Trim();
            _encoder.Password = txtPassword.Text.Trim();
            _encoder.MountPoint = txtMountPoint.Text.Trim();
            _encoder.Format = "MP3";

            switch (cboBitrate.SelectedIndex)
            {
                case 0:
                    _encoder.Bitrate = 128;
                    break;
                case 1:
                    _encoder.Bitrate = 256;
                    break;
                case 2:
                    _encoder.Bitrate = 320;
                    break;
            }

            _encoder.EnableAGC = chkEnableAGC.Checked;
            _encoder.AGCTargetLevel = trackAGCTarget.Value / 100f;
            _encoder.AGCAttackTime = trackAGCAttack.Value / 10f;
            _encoder.AGCReleaseTime = trackAGCRelease.Value / 10f;
            _encoder.LimiterThreshold = trackLimiterThreshold.Value / 100f;

            EncoderConfigManager.SaveEncoder(_encoder);

            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        public EncoderEntry GetEncoder()
        {
            return _encoder;
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