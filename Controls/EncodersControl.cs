using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Threading.Tasks;
using AirDirector.Services.Database;
using AirDirector.Services.Localization;
using AirDirector.Models;
using AirDirector.Forms;
using AirDirector.Themes;
using Microsoft.Win32;

namespace AirDirector.Controls
{
    public partial class EncodersControl : UserControl
    {
        private FlowLayoutPanel flowPanel;
        private List<StreamEncoder> _encoders;
        private List<EncoderStreamControl> _encoderControls;
        private CheckBox chkAutoStart;
        private bool _isLoadingControl = true;
        private Button btnNew;
        private Button btnStartAll;
        private Button btnStopAll;
        private Button btnRefresh;

        public event EventHandler<string> StatusChanged;

        public EncodersControl()
        {
            InitializeComponent();
            _encoders = new List<StreamEncoder>();
            _encoderControls = new List<EncoderStreamControl>();
            InitializeUI();
            ApplyLanguage();
            LoadEncoders();
            RefreshEncoders();

            _isLoadingControl = false;

            AirDirector.Services.MetadataDispatcher.MetadataUpdated += OnMetadataUpdated;

            LanguageManager.LanguageChanged += OnLanguageChanged;

            if (chkAutoStart.Checked)
            {
                Task.Delay(1000).ContinueWith(_ =>
                {
                    if (InvokeRequired)
                        Invoke(new Action(AutoStartEncoders));
                    else
                        AutoStartEncoders();
                });
            }
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            if (btnNew != null)
                btnNew.Text = "➕ " + LanguageManager.GetString("Encoders.NewEncoder", "Nuovo Encoder");

            if (btnStartAll != null)
                btnStartAll.Text = "▶ " + LanguageManager.GetString("Encoders.StartAll", "Start All");

            if (btnStopAll != null)
                btnStopAll.Text = "⏹ " + LanguageManager.GetString("Encoders.StopAll", "Stop All");

            if (btnRefresh != null)
                btnRefresh.Text = "🔄 " + LanguageManager.GetString("Encoders.Refresh", "Aggiorna");

            if (chkAutoStart != null)
                chkAutoStart.Text = "🚀 " + LanguageManager.GetString("Encoders.AutoStart", "Avvia Automaticamente");
        }

        public int GetTotalEncoders()
        {
            return _encoders?.Count ?? 0;
        }

        public int GetActiveEncoders()
        {
            return _encoders?.Count(e => e.IsActive) ?? 0;
        }

        private void OnMetadataUpdated(object sender, AirDirector.Services.MetadataEventArgs e)
        {
            try
            {
                if (InvokeRequired)
                    Invoke(new Action(() => UpdateAllEncodersMetadata(e.Artist, e.Title)));
                else
                    UpdateAllEncodersMetadata(e.Artist, e.Title);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EncodersControl] ❌ ERRORE:  {ex.Message}");
            }
        }

        private void InitializeUI()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = AppTheme.BgLight;

            flowPanel = new FlowLayoutPanel
            {
                Name = "flowEncoders",
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = AppTheme.BgLight,
                Padding = new Padding(10)
            };
            this.Controls.Add(flowPanel);

            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = AppTheme.Surface
            };
            this.Controls.Add(headerPanel);

            btnNew = new Button
            {
                Text = "➕ Nuovo Encoder",
                Location = new Point(10, 10),
                Size = new Size(150, 30),
                BackColor = AppTheme.Success,
                ForeColor = AppTheme.TextInverse,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnNew.FlatAppearance.BorderSize = 0;
            btnNew.Click += BtnNew_Click;
            headerPanel.Controls.Add(btnNew);

            btnStartAll = new Button
            {
                Text = "▶ Start All",
                Location = new Point(170, 10),
                Size = new Size(100, 30),
                BackColor = AppTheme.Success,
                ForeColor = AppTheme.TextInverse,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnStartAll.FlatAppearance.BorderSize = 0;
            btnStartAll.Click += BtnStartAll_Click;
            headerPanel.Controls.Add(btnStartAll);

            btnStopAll = new Button
            {
                Text = "⏹ Stop All",
                Location = new Point(280, 10),
                Size = new Size(100, 30),
                BackColor = AppTheme.Danger,
                ForeColor = AppTheme.TextInverse,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnStopAll.FlatAppearance.BorderSize = 0;
            btnStopAll.Click += BtnStopAll_Click;
            headerPanel.Controls.Add(btnStopAll);

            btnRefresh = new Button
            {
                Text = "🔄 Aggiorna",
                Location = new Point(390, 10),
                Size = new Size(100, 30),
                BackColor = AppTheme.Info,
                ForeColor = AppTheme.TextInverse,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9),
                Cursor = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (s, e) => RefreshEncoders();
            headerPanel.Controls.Add(btnRefresh);

            chkAutoStart = new CheckBox
            {
                Text = "🚀 Avvia Automaticamente",
                Location = new Point(headerPanel.Width - 220, 15),
                Size = new Size(210, 20),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = AppTheme.TextPrimary,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand,
                Checked = EncoderConfigManager.GetAutoStartEnabled()
            };
            chkAutoStart.CheckedChanged += ChkAutoStart_CheckedChanged;
            headerPanel.Controls.Add(chkAutoStart);
        }

        private void ChkAutoStart_CheckedChanged(object sender, EventArgs e)
        {
            if (_isLoadingControl) return;

            EncoderConfigManager.SetAutoStartEnabled(chkAutoStart.Checked);

            string message = chkAutoStart.Checked
                ? LanguageManager.GetString("Encoders.AutoStartEnabled", "Gli encoder si avvieranno automaticamente al prossimo avvio")
                : LanguageManager.GetString("Encoders.AutoStartDisabled", "Avvio automatico disabilitato");

            StatusChanged?.Invoke(this, message);
        }

        private async void AutoStartEncoders()
        {
            try
            {
                int startedCount = 0;

                foreach (var encoder in _encoders)
                {
                    if (!encoder.IsActive)
                    {
                        if (encoder.Start(out string error))
                        {
                            startedCount++;
                            await Task.Delay(700);
                        }
                    }
                }

                foreach (var control in _encoderControls)
                {
                    control.UpdateStatus();
                }

                StatusChanged?.Invoke(this, string.Format(LanguageManager.GetString("Encoders.AutoStartCompleted", "Avvio automatico:  {0} encoder avviati"), startedCount));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EncodersControl] Errore avvio automatico: {ex.Message}");
            }
        }

        private void LoadEncoders()
        {
            try
            {
                var encoderEntries = EncoderConfigManager.LoadEncoders();

                _encoders.Clear();

                foreach (var entry in encoderEntries)
                {
                    var encoder = new StreamEncoder
                    {
                        EncoderId = entry.ID,
                        Name = entry.Name,
                        StationName = entry.StationName,
                        AudioSourceDevice = entry.AudioSourceDevice,
                        ServerUrl = entry.ServerUrl,
                        ServerPort = entry.ServerPort,
                        ServerUsername = entry.Username,
                        ServerPassword = entry.Password,
                        MountPoint = entry.MountPoint,
                        Bitrate = entry.Bitrate,
                        Format = entry.Format
                    };

                    encoder.SetAGCEnabled(entry.EnableAGC);
                    encoder.UpdateAGCSettings(
                        entry.AGCTargetLevel,
                        entry.AGCAttackTime,
                        entry.AGCReleaseTime,
                        entry.LimiterThreshold
                    );

                    _encoders.Add(encoder);
                }

                StatusChanged?.Invoke(this, string.Format(LanguageManager.GetString("Encoders.LoadedCount", "Caricati {0} encoder"), _encoders.Count));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(LanguageManager.GetString("Encoders.LoadError", "Errore caricamento encoder:\n{0}"), ex.Message),
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void BtnNew_Click(object sender, EventArgs e)
        {
            using (var editorForm = new EncoderEditorForm())
            {
                if (editorForm.ShowDialog() == DialogResult.OK)
                {
                    var newEntry = editorForm.GetEncoder();

                    var newEncoder = new StreamEncoder
                    {
                        EncoderId = newEntry.ID,
                        Name = newEntry.Name,
                        StationName = newEntry.StationName,
                        AudioSourceDevice = newEntry.AudioSourceDevice,
                        ServerUrl = newEntry.ServerUrl,
                        ServerPort = newEntry.ServerPort,
                        ServerUsername = newEntry.Username,
                        ServerPassword = newEntry.Password,
                        MountPoint = newEntry.MountPoint,
                        Bitrate = newEntry.Bitrate,
                        Format = newEntry.Format
                    };

                    newEncoder.SetAGCEnabled(newEntry.EnableAGC);
                    newEncoder.UpdateAGCSettings(
                        newEntry.AGCTargetLevel,
                        newEntry.AGCAttackTime,
                        newEntry.AGCReleaseTime,
                        newEntry.LimiterThreshold
                    );

                    _encoders.Add(newEncoder);
                    AddEncoderControl(newEncoder);

                    StatusChanged?.Invoke(this, LanguageManager.GetString("Encoders.EncoderCreated", "Encoder creato"));
                }
            }
        }

        private void AddEncoderControl(StreamEncoder encoder)
        {
            var encoderControl = new EncoderStreamControl(encoder);
            encoderControl.EditRequested += EncoderControl_EditRequested;
            encoderControl.DeleteRequested += EncoderControl_DeleteRequested;

            flowPanel.Controls.Add(encoderControl);
            _encoderControls.Add(encoderControl);
        }

        private void EncoderControl_EditRequested(object sender, StreamEncoder encoder)
        {
            if (encoder.IsActive)
            {
                MessageBox.Show(
                    LanguageManager.GetString("Encoders.StopBeforeEdit", "Ferma lo streaming prima di modificare le impostazioni."),
                    LanguageManager.GetString("Common.Warning", "Attenzione"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var entry = new EncoderEntry
            {
                ID = encoder.EncoderId,
                Name = encoder.Name,
                StationName = encoder.StationName,
                AudioSourceDevice = encoder.AudioSourceDevice,
                ServerUrl = encoder.ServerUrl,
                ServerPort = encoder.ServerPort,
                Username = encoder.ServerUsername,
                Password = encoder.ServerPassword,
                MountPoint = encoder.MountPoint,
                Bitrate = encoder.Bitrate,
                Format = encoder.Format,
                EnableAGC = encoder.EnableAGC,
                AGCTargetLevel = encoder.AGCTargetLevel,
                AGCAttackTime = encoder.AGCAttackTime,
                AGCReleaseTime = encoder.AGCReleaseTime,
                LimiterThreshold = encoder.LimiterThreshold
            };

            using (var editorForm = new EncoderEditorForm(entry))
            {
                if (editorForm.ShowDialog() == DialogResult.OK)
                {
                    var updatedEntry = editorForm.GetEncoder();

                    encoder.Name = updatedEntry.Name;
                    encoder.StationName = updatedEntry.StationName;
                    encoder.AudioSourceDevice = updatedEntry.AudioSourceDevice;
                    encoder.ServerUrl = updatedEntry.ServerUrl;
                    encoder.ServerPort = updatedEntry.ServerPort;
                    encoder.ServerUsername = updatedEntry.Username;
                    encoder.ServerPassword = updatedEntry.Password;
                    encoder.MountPoint = updatedEntry.MountPoint;
                    encoder.Bitrate = updatedEntry.Bitrate;
                    encoder.Format = updatedEntry.Format;
                    encoder.SetAGCEnabled(updatedEntry.EnableAGC);
                    encoder.UpdateAGCSettings(
                        updatedEntry.AGCTargetLevel,
                        updatedEntry.AGCAttackTime,
                        updatedEntry.AGCReleaseTime,
                        updatedEntry.LimiterThreshold
                    );

                    foreach (var control in _encoderControls)
                    {
                        if (control.Encoder == encoder)
                        {
                            control.UpdateStatus();
                            break;
                        }
                    }

                    StatusChanged?.Invoke(this, LanguageManager.GetString("Encoders.EncoderUpdated", "Encoder aggiornato"));
                }
            }
        }

        private void EncoderControl_DeleteRequested(object sender, StreamEncoder encoder)
        {
            try
            {
                EncoderConfigManager.DeleteEncoder(encoder.EncoderId);

                var controlToRemove = _encoderControls.FirstOrDefault(c => c.Encoder == encoder);
                if (controlToRemove != null)
                {
                    flowPanel.Controls.Remove(controlToRemove);
                    _encoderControls.Remove(controlToRemove);
                    controlToRemove.Dispose();
                }

                _encoders.Remove(encoder);

                StatusChanged?.Invoke(this, LanguageManager.GetString("Encoders.EncoderDeleted", "Encoder eliminato"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(LanguageManager.GetString("Encoders.DeleteError", "Errore eliminazione encoder:\n{0}"), ex.Message),
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private async void BtnStartAll_Click(object sender, EventArgs e)
        {
            int startedCount = 0;

            foreach (var encoder in _encoders)
            {
                if (!encoder.IsActive)
                {
                    if (encoder.Start(out string error))
                    {
                        startedCount++;
                        await Task.Delay(500);
                    }
                }
            }

            foreach (var control in _encoderControls)
            {
                control.UpdateStatus();
            }

            StatusChanged?.Invoke(this, string.Format(LanguageManager.GetString("Encoders.StartedCount", "Avviati {0} encoder"), startedCount));
        }

        private void BtnStopAll_Click(object sender, EventArgs e)
        {
            int stoppedCount = 0;

            foreach (var encoder in _encoders)
            {
                if (encoder.IsActive)
                {
                    encoder.Stop();
                    stoppedCount++;
                }
            }

            foreach (var control in _encoderControls)
            {
                control.UpdateStatus();
            }

            StatusChanged?.Invoke(this, string.Format(LanguageManager.GetString("Encoders.StoppedCount", "Fermati {0} encoder"), stoppedCount));
        }

        public void RefreshEncoders()
        {
            foreach (var control in _encoderControls)
            {
                control.Dispose();
            }

            flowPanel.Controls.Clear();
            _encoderControls.Clear();

            LoadEncoders();

            foreach (var encoder in _encoders)
            {
                AddEncoderControl(encoder);
            }

            StatusChanged?.Invoke(this, string.Format(LanguageManager.GetString("Encoders.ElementsCount", "Encoders: {0} elementi"), _encoders.Count));
        }

        public void UpdateAllEncodersMetadata(string artist, string title)
        {
            if (_encoders == null || _encoders.Count == 0)
                return;

            try
            {
                int updatedCount = 0;

                foreach (var encoder in _encoders)
                {
                    if (encoder.IsActive)
                    {
                        try
                        {
                            encoder.UpdateMetadata(artist, title);
                            updatedCount++;
                        }
                        catch (Exception encEx)
                        {
                            Console.WriteLine($"[EncodersControl] Encoder error: {encEx.Message}");
                        }
                    }
                }

                if (updatedCount > 0)
                {
                    StatusChanged?.Invoke(this, string.Format(LanguageManager.GetString("Encoders.MetadataUpdated", "Metadata aggiornati su {0} encoder"), updatedCount));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EncodersControl] ❌ ERRORE CRITICO: {ex.Message}");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                LanguageManager.SaveMissingKeysToFile();
                LanguageManager.LanguageChanged -= OnLanguageChanged;

                AirDirector.Services.MetadataDispatcher.MetadataUpdated -= OnMetadataUpdated;

                foreach (var encoder in _encoders)
                {
                    if (encoder.IsActive)
                        encoder.Stop();
                    encoder.Dispose();
                }

                foreach (var control in _encoderControls)
                {
                    control.Dispose();
                }

                if (components != null)
                {
                    components.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }
}