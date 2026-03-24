using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using AirDirector.Models;
using AirDirector.Services.Localization;
using AirDirector.Themes;

namespace AirDirector.Controls
{
    public partial class EncoderStreamControl : UserControl
    {
        private StreamEncoder _encoder;
        private System.Windows.Forms.Timer _uiUpdateTimer;
        private float[] _currentLevels = new float[2] { 0, 0 };
        private int _updateCounter = 0;
        private bool _levelUpdatePending = false;
        private readonly object _levelLock = new object();
        private bool _disposed = false;

        public event EventHandler<StreamEncoder> EditRequested;
        public event EventHandler<StreamEncoder> DeleteRequested;

        public StreamEncoder Encoder => _encoder;

        public EncoderStreamControl(StreamEncoder encoder)
        {
            InitializeComponent();

            _encoder = encoder;
            _encoder.LevelMeterUpdated += Encoder_LevelMeterUpdated;
            _encoder.DSPStateChanged += Encoder_DSPStateChanged;

            _uiUpdateTimer = new System.Windows.Forms.Timer { Interval = 150 };
            _uiUpdateTimer.Tick += UiUpdateTimer_Tick;
            _uiUpdateTimer.Start();

            LanguageManager.LanguageChanged += OnLanguageChanged;

            RefreshUI();
            UpdateDSPIndicator();
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            RefreshUI();
        }

        private void Encoder_LevelMeterUpdated(object sender, float[] levels)
        {
            if (_disposed) return;

            lock (_levelLock)
            {
                _currentLevels[0] = Math.Max(_currentLevels[0] * 0.88f, levels[0]);
                _currentLevels[1] = Math.Max(_currentLevels[1] * 0.88f, levels[1]);

                if (!_levelUpdatePending)
                {
                    _levelUpdatePending = true;
                    try
                    {
                        BeginInvoke(new Action(UpdateVuMetersFromCurrentLevels));
                    }
                    catch
                    {
                        _levelUpdatePending = false;
                    }
                }
            }
        }

        private void Encoder_DSPStateChanged(object sender, bool isActive)
        {
            if (_disposed) return;

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action<bool>(HandleDSPStateChanged), isActive);
                }
                catch { }
            }
            else
            {
                HandleDSPStateChanged(isActive);
            }
        }

        private void HandleDSPStateChanged(bool isActive)
        {
            if (_disposed) return;

            try
            {
                if (lblDSP != null)
                {
                    lblDSP.Visible = isActive;
                    lblDSP.BackColor = isActive ? Color.LightGreen : Color.Transparent;
                }
                if (lblFormat != null && _encoder != null)
                    lblFormat.Text = _encoder.GetFormatString();
            }
            catch { }
        }

        private void UpdateVuMetersFromCurrentLevels()
        {
            if (_disposed) return;

            float left, right;

            lock (_levelLock)
            {
                left = _currentLevels[0];
                right = _currentLevels[1];
                _levelUpdatePending = false;
            }

            try
            {
                if (progressLeft != null)
                {
                    progressLeft.Value = (int)Math.Min(100, left * 100);
                    progressLeft.ForeColor = left > 0.9f ? Color.Red : Color.LimeGreen;
                }

                if (progressRight != null)
                {
                    progressRight.Value = (int)Math.Min(100, right * 100);
                    progressRight.ForeColor = right > 0.9f ? Color.Red : Color.LimeGreen;
                }
            }
            catch { }
        }

        private void UiUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (_disposed) return;

            _updateCounter++;

            UpdateVuMetersFromCurrentLevels();

            if (_updateCounter >= 7)
            {
                RefreshUI();
                _updateCounter = 0;
            }
        }

        private void RefreshUI()
        {
            if (_disposed || _encoder == null) return;

            try
            {
                if (lblStationName != null)
                    lblStationName.Text = string.IsNullOrEmpty(_encoder.Name) ? _encoder.StationName : _encoder.Name;

                if (lblFormat != null)
                    lblFormat.Text = _encoder.GetFormatString();

                if (lblStatus != null)
                {
                    lblStatus.Text = _encoder.StatusText;
                    lblStatus.ForeColor = _encoder.StatusTextColor;
                    lblStatus.BackColor = _encoder.StatusBackColor;
                }

                if (lblAudioDevice != null)
                {
                    lblAudioDevice.Text = string.IsNullOrEmpty(_encoder.AudioSourceDevice) ?
                        LanguageManager.GetString("EncoderStream.NoDevice", "Nessun dispositivo") :
                        _encoder.AudioSourceDevice;
                }

                if (lblUptime != null)
                {
                    var uptime = _encoder.Uptime;
                    lblUptime.Text = $"{uptime.Days:D2}, {uptime.Hours:D2}:{uptime.Minutes:D2}:{uptime.Seconds:D2}";
                }

                if (btnStartStop != null)
                {
                    btnStartStop.Text = _encoder.IsActive ?
                        "⏹ " + LanguageManager.GetString("EncoderStream.Stop", "Stop") :
                        "▶ " + LanguageManager.GetString("EncoderStream.Start", "Start");
                    btnStartStop.BackColor = _encoder.IsActive ? AppTheme.Danger : AppTheme.Success;
                }

                if (!_encoder.IsActive)
                {
                    if (progressLeft != null)
                    {
                        progressLeft.Value = 0;
                        progressLeft.ForeColor = Color.LimeGreen;
                    }

                    if (progressRight != null)
                    {
                        progressRight.Value = 0;
                        progressRight.ForeColor = Color.LimeGreen;
                    }

                    lock (_levelLock)
                    {
                        _currentLevels[0] = 0;
                        _currentLevels[1] = 0;
                    }
                }
            }
            catch { }
        }

        private void UpdateDSPIndicator()
        {
            if (_disposed || _encoder == null) return;

            try
            {
                bool dspActive = _encoder.DSPActive;

                if (lblDSP != null)
                {
                    lblDSP.Visible = dspActive;
                    lblDSP.BackColor = dspActive ? Color.LightGreen : Color.Transparent;
                }
            }
            catch { }
        }

        public void UpdateStatus()
        {
            if (_disposed) return;

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(RefreshUI));
                }
                catch { }
                return;
            }

            RefreshUI();
            UpdateDSPIndicator();
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            if (_disposed || _encoder == null) return;

            if (_encoder.IsActive)
            {
                MessageBox.Show(
                    LanguageManager.GetString("EncoderStream.StopBeforeEdit", "Ferma lo streaming prima di modificare le impostazioni dell'encoder."),
                    LanguageManager.GetString("Common.Warning", "Attenzione"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            EditRequested?.Invoke(this, _encoder);
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (_disposed || _encoder == null) return;

            if (_encoder.IsActive)
            {
                MessageBox.Show(
                    LanguageManager.GetString("EncoderStream.StopBeforeDelete", "Ferma lo streaming prima di eliminare l'encoder."),
                    LanguageManager.GetString("Common.Warning", "Attenzione"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                string.Format(LanguageManager.GetString("EncoderStream.ConfirmDelete", "Eliminare l'encoder '{0}'?"), _encoder.StationName),
                LanguageManager.GetString("EncoderStream.ConfirmDeleteTitle", "Conferma eliminazione"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                DeleteRequested?.Invoke(this, _encoder);
            }
        }

        private async void btnStartStop_Click(object sender, EventArgs e)
        {
            if (_disposed || _encoder == null) return;

            if (_encoder.IsActive)
            {
                _encoder.Stop();
                RefreshUI();
                UpdateDSPIndicator();
            }
            else
            {
                if (string.IsNullOrEmpty(_encoder.AudioSourceDevice))
                {
                    MessageBox.Show(
                        LanguageManager.GetString("EncoderStream.SelectDevice", "Seleziona un dispositivo audio prima di avviare lo streaming."),
                        LanguageManager.GetString("EncoderStream.MissingDevice", "Dispositivo mancante"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(_encoder.MountPoint))
                {
                    MessageBox.Show(
                        LanguageManager.GetString("EncoderStream.EmptyMountpoint", "Il mountpoint non può essere vuoto."),
                        LanguageManager.GetString("EncoderStream.WrongConfig", "Configurazione errata"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                Cursor = Cursors.WaitCursor;
                try
                {
                    _encoder.LoadAGCSettings();

                    bool success = _encoder.Start(out string exactError);

                    if (success)
                    {
                        RefreshUI();
                        UpdateDSPIndicator();
                    }
                    else
                    {
                        string errorMsg = string.IsNullOrEmpty(exactError)
                            ? LanguageManager.GetString("EncoderStream.UnknownError", "Errore sconosciuto durante l'avvio")
                            : exactError;

                        MessageBox.Show(
                            string.Format(LanguageManager.GetString("EncoderStream.StartError", "Errore avvio encoder:\n\n{0}"), errorMsg),
                            LanguageManager.GetString("Common.Error", "Errore"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        string.Format(LanguageManager.GetString("EncoderStream.ExceptionUI", "Eccezione nell'UI:\n\n{0}"), ex.Message),
                        LanguageManager.GetString("Common.Error", "Errore"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                finally
                {
                    Cursor = Cursors.Default;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;

                LanguageManager.LanguageChanged -= OnLanguageChanged;

                _uiUpdateTimer?.Stop();
                _uiUpdateTimer?.Dispose();

                if (_encoder != null)
                {
                    _encoder.LevelMeterUpdated -= Encoder_LevelMeterUpdated;
                    _encoder.DSPStateChanged -= Encoder_DSPStateChanged;

                    if (_encoder.IsActive)
                        _encoder.Stop();
                }

                if (components != null)
                {
                    components.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        private void lblAudioDevice_Click(object sender, EventArgs e)
        {

        }
    }
}