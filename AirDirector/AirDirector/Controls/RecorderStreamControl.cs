using System;
using System.Drawing;
using System.Windows.Forms;
using AirDirector.Models;
using AirDirector.Services.Core;
using AirDirector.Services.Localization;
using AirDirector.Themes;

namespace AirDirector.Controls
{
    public partial class RecorderStreamControl : UserControl
    {
        private Recorder _recorder;
        private RecorderService _recorderService;
        private System.Windows.Forms.Timer _uiUpdateTimer;
        private float[] _currentLevels = new float[2] { 0, 0 };
        private int _updateCounter = 0;
        private bool _levelUpdatePending = false;
        private readonly object _levelLock = new object();
        private bool _disposed = false;

        public event EventHandler<Recorder> EditRequested;
        public event EventHandler<Recorder> DeleteRequested;

        public Recorder Recorder => _recorder;

        public RecorderStreamControl(Recorder recorder)
        {
            InitializeComponent();

            _recorder = recorder;
            _recorderService = new RecorderService(_recorder);

            _recorderService.LogMessage += (s, msg) => Console.WriteLine($"[{_recorder.Name}] {msg}");
            _recorderService.FileSizeUpdated += (s, size) => _recorder.CurrentFileSize = size;
            _recorderService.RecordingStarted += (s, e) =>
            {
                if (InvokeRequired)
                    BeginInvoke(new Action(RefreshUI));
                else
                    RefreshUI();
            };
            _recorderService.RecordingStopped += (s, e) =>
            {
                if (InvokeRequired)
                    BeginInvoke(new Action(RefreshUI));
                else
                    RefreshUI();
            };
            _recorderService.LevelMeterUpdated += RecorderService_LevelMeterUpdated;

            _uiUpdateTimer = new System.Windows.Forms.Timer { Interval = 150 };
            _uiUpdateTimer.Tick += UiUpdateTimer_Tick;
            _uiUpdateTimer.Start();

            LanguageManager.LanguageChanged += OnLanguageChanged;

            RefreshUI();
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            RefreshUI();
        }

        private void RecorderService_LevelMeterUpdated(object sender, float[] levels)
        {
            if (_disposed || levels == null || levels.Length < 2) return;

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
            if (_disposed || _recorder == null) return;

            try
            {
                if (lblRecorderName != null)
                    lblRecorderName.Text = _recorder.Name;

                if (lblFormat != null)
                    lblFormat.Text = _recorder.GetFormatString();

                if (lblType != null)
                {
                    string typeText = "";
                    Color typeColor = Color.Gray;

                    if (_recorder.Type == Recorder.RecorderType.NinetyDays)
                    {
                        typeText = LanguageManager.GetString("RecorderStream.TypeAuto", "Auto");
                        typeColor = Color.Orange;
                    }
                    else if (_recorder.Type == Recorder.RecorderType.Manual)
                    {
                        typeText = LanguageManager.GetString("RecorderStream.TypeManual", "Manual");
                        typeColor = Color.DodgerBlue;
                    }
                    else if (_recorder.Type == Recorder.RecorderType.Scheduled)
                    {
                        typeText = LanguageManager.GetString("RecorderStream.TypeScheduled", "Scheduled");
                        typeColor = Color.Purple;
                    }

                    lblType.Text = typeText;
                    lblType.ForeColor = typeColor;
                }

                if (lblStatus != null)
                {
                    lblStatus.Text = _recorder.StatusText;
                    lblStatus.ForeColor = _recorder.IsRecording ? Color.White : AppTheme.TextSecondary;
                    lblStatus.BackColor = _recorder.IsRecording ? Color.Red : Color.Transparent;
                }

                if (lblAudioDevice != null)
                {
                    string deviceText = string.IsNullOrEmpty(_recorder.AudioSourceDevice) ?
                        LanguageManager.GetString("RecorderStream.NoDevice", "Nessun dispositivo") :
                        _recorder.AudioSourceDevice;

                    if (deviceText.Length > 35)
                        deviceText = deviceText.Substring(0, 32) + "...";

                    lblAudioDevice.Text = deviceText;
                }

                if (lblPath != null)
                {
                    string pathText = _recorder.OutputPath;
                    if (pathText.Length > 40)
                        pathText = "..." + pathText.Substring(pathText.Length - 37);

                    lblPath.Text = $"📁 {pathText}";
                }

                if (lblCurrentFile != null)
                {
                    if (_recorder.IsRecording && !string.IsNullOrEmpty(_recorder.CurrentFileName))
                    {
                        lblCurrentFile.Text = $"📄 {_recorder.CurrentFileName}";
                        lblCurrentFile.ForeColor = Color.Green;
                    }
                    else
                    {
                        lblCurrentFile.Text = LanguageManager.GetString("RecorderStream.NoFile", "Nessun file");
                        lblCurrentFile.ForeColor = Color.Gray;
                    }
                }

                if (lblRecordingInfo != null)
                {
                    if (_recorder.IsRecording)
                    {
                        var duration = _recorder.GetRecordingDuration();
                        string durationStr = $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
                        string sizeStr = _recorder.GetFormattedFileSize();
                        lblRecordingInfo.Text = $"⏱ {durationStr} | 💾 {sizeStr}";
                        lblRecordingInfo.ForeColor = Color.Red;
                    }
                    else
                    {
                        lblRecordingInfo.Text = "00:00:00 | 0 MB";
                        lblRecordingInfo.ForeColor = Color.Gray;
                    }
                }

                if (btnStartStop != null)
                {
                    btnStartStop.Text = _recorder.IsActive ?
                        "⏹ " + LanguageManager.GetString("RecorderStream.Stop", "Stop") :
                        "▶ " + LanguageManager.GetString("RecorderStream.Start", "Start");
                    btnStartStop.BackColor = _recorder.IsActive ? AppTheme.Danger : AppTheme.Success;
                }

                if (!_recorder.IsRecording)
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
            catch (Exception ex)
            {
                Console.WriteLine($"[RecorderStreamControl] ❌ Errore RefreshUI: {ex.Message}");
            }
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
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            if (_disposed || _recorder == null) return;

            if (_recorder.IsActive)
            {
                MessageBox.Show(
                    LanguageManager.GetString("RecorderStream.StopBeforeEdit", "Ferma la registrazione prima di modificare le impostazioni."),
                    LanguageManager.GetString("Common.Warning", "Attenzione"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            EditRequested?.Invoke(this, _recorder);
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (_disposed || _recorder == null) return;

            if (_recorder.IsActive)
            {
                MessageBox.Show(
                    LanguageManager.GetString("RecorderStream.StopBeforeDelete", "Ferma la registrazione prima di eliminare il recorder."),
                    LanguageManager.GetString("Common.Warning", "Attenzione"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                string.Format(LanguageManager.GetString("RecorderStream.ConfirmDelete", "Eliminare il recorder '{0}'?"), _recorder.Name),
                LanguageManager.GetString("RecorderStream.ConfirmDeleteTitle", "Conferma eliminazione"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                DeleteRequested?.Invoke(this, _recorder);
            }
        }

        private void btnStartStop_Click(object sender, EventArgs e)
        {
            if (_disposed || _recorder == null) return;

            if (_recorder.IsActive)
            {
                _recorderService.Stop();
                RefreshUI();
            }
            else
            {
                if (string.IsNullOrEmpty(_recorder.AudioSourceDevice))
                {
                    MessageBox.Show(
                        LanguageManager.GetString("RecorderStream.SelectDevice", "Seleziona un dispositivo audio prima di avviare la registrazione."),
                        LanguageManager.GetString("RecorderStream.MissingDevice", "Dispositivo mancante"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(_recorder.OutputPath))
                {
                    MessageBox.Show(
                        LanguageManager.GetString("RecorderStream.EmptyPath", "Il percorso di salvataggio non può essere vuoto."),
                        LanguageManager.GetString("RecorderStream.WrongConfig", "Configurazione errata"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                Cursor = Cursors.WaitCursor;
                try
                {
                    bool success = _recorderService.Start(out string exactError);

                    if (success)
                    {
                        RefreshUI();
                    }
                    else
                    {
                        string errorMsg = string.IsNullOrEmpty(exactError)
                            ? LanguageManager.GetString("RecorderStream.UnknownError", "Errore sconosciuto durante l'avvio")
                            : exactError;

                        MessageBox.Show(
                            string.Format(LanguageManager.GetString("RecorderStream.StartError", "Errore avvio recorder:\n\n{0}"), errorMsg),
                            LanguageManager.GetString("Common.Error", "Errore"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        string.Format(LanguageManager.GetString("RecorderStream.ExceptionUI", "Eccezione nell'UI:\n\n{0}"), ex.Message),
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

        public void StartRecorder()
        {
            if (_disposed || _recorder == null) return;

            if (_recorder.IsActive)
                return;

            if (string.IsNullOrEmpty(_recorder.AudioSourceDevice))
            {
                Console.WriteLine($"[{_recorder.Name}] ❌ Nessun dispositivo audio configurato");
                return;
            }

            if (string.IsNullOrEmpty(_recorder.OutputPath))
            {
                Console.WriteLine($"[{_recorder.Name}] ❌ Percorso salvataggio non configurato");
                return;
            }

            try
            {
                bool success = _recorderService.Start(out string exactError);

                if (success)
                {
                    Console.WriteLine($"[{_recorder.Name}] ✅ Recorder avviato con VU meter collegati");
                    RefreshUI();
                }
                else
                {
                    Console.WriteLine($"[{_recorder.Name}] ❌ Errore:  {exactError}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{_recorder.Name}] ❌ Eccezione: {ex.Message}");
            }
        }

        public void StopRecorder()
        {
            if (_disposed || _recorder == null) return;

            if (!_recorder.IsActive)
                return;

            try
            {
                _recorderService.Stop();
                RefreshUI();
                Console.WriteLine($"[{_recorder.Name}] ⏹️ Recorder fermato");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{_recorder.Name}] ❌ Errore stop: {ex.Message}");
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

                if (_recorderService != null)
                {
                    if (_recorder?.IsActive == true)
                    {
                        _recorderService.Stop();
                    }
                    _recorderService.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        private void lblStatus_Click(object sender, EventArgs e)
        {

        }

        private void lblVULeft_Click(object sender, EventArgs e)
        {

        }
    }
}