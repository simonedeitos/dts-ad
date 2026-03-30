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

namespace AirDirector.Controls
{
    public partial class RecordersControl : UserControl
    {
        private FlowLayoutPanel flowPanel;
        private List<Recorder> _recorders;
        private List<RecorderStreamControl> _recorderControls;
        private CheckBox chkAutoStart;
        private bool _isLoadingControl = true;
        private Button btnNew;
        private Button btnStartAll;
        private Button btnStopAll;
        private Button btnRefresh;

        public event EventHandler<string> StatusChanged;

        public RecordersControl()
        {
            InitializeComponent();
            _recorders = new List<Recorder>();
            _recorderControls = new List<RecorderStreamControl>();
            InitializeUI();
            ApplyLanguage();
            LoadRecorders();
            RefreshRecorders();

            _isLoadingControl = false;

            LanguageManager.LanguageChanged += OnLanguageChanged;

            if (chkAutoStart.Checked)
            {
                Task.Delay(1000).ContinueWith(_ =>
                {
                    if (InvokeRequired)
                    {
                        Invoke(new Action(AutoStartRecorders));
                    }
                    else
                    {
                        AutoStartRecorders();
                    }
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
                btnNew.Text = "➕ " + LanguageManager.GetString("Recorders.NewRecorder", "Nuovo Recorder");

            if (btnStartAll != null)
                btnStartAll.Text = "▶ " + LanguageManager.GetString("Recorders.StartAll", "Start All");

            if (btnStopAll != null)
                btnStopAll.Text = "⏹ " + LanguageManager.GetString("Recorders.StopAll", "Stop All");

            if (btnRefresh != null)
                btnRefresh.Text = "🔄 " + LanguageManager.GetString("Recorders.Refresh", "Aggiorna");

            if (chkAutoStart != null)
                chkAutoStart.Text = "🚀 " + LanguageManager.GetString("Recorders.AutoStart", "Avvia Automaticamente");
        }

        private void InitializeUI()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = AppTheme.BgLight;

            flowPanel = new FlowLayoutPanel
            {
                Name = "flowRecorders",
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
                Text = "➕ Nuovo Recorder",
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
            btnRefresh.Click += (s, e) => RefreshRecorders();
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
                Checked = RecorderConfigManager.GetAutoStartEnabled()
            };
            chkAutoStart.CheckedChanged += ChkAutoStart_CheckedChanged;
            headerPanel.Controls.Add(chkAutoStart);
        }

        private void ChkAutoStart_CheckedChanged(object sender, EventArgs e)
        {
            if (_isLoadingControl) return;

            RecorderConfigManager.SetAutoStartEnabled(chkAutoStart.Checked);

            string message = chkAutoStart.Checked
                ? LanguageManager.GetString("Recorders.AutoStartEnabledMessage", "I recorder si avvieranno automaticamente al prossimo avvio")
                : LanguageManager.GetString("Recorders.AutoStartDisabledMessage", "Avvio automatico disabilitato");

            StatusChanged?.Invoke(this, message);
        }

        private async void AutoStartRecorders()
        {
            try
            {
                int startedCount = 0;

                foreach (var control in _recorderControls)
                {
                    var recorder = control.Recorder;

                    if (!recorder.IsActive)
                    {
                        control.StartRecorder();

                        if (recorder.IsActive)
                        {
                            startedCount++;
                            await Task.Delay(500);
                        }
                    }
                }

                foreach (var control in _recorderControls)
                {
                    control.UpdateStatus();
                }

                StatusChanged?.Invoke(this, string.Format(LanguageManager.GetString("Recorders.AutoStartCompleted", "Avvio automatico:  {0} recorder avviati"), startedCount));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RecordersControl] ❌ Errore avvio automatico: {ex.Message}");
            }
        }

        private void LoadRecorders()
        {
            try
            {
                _recorders = RecorderConfigManager.LoadRecorders();

                StatusChanged?.Invoke(this, string.Format(LanguageManager.GetString("Recorders.LoadedCount", "Caricati {0} recorder"), _recorders.Count));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(LanguageManager.GetString("Recorders.LoadError", "Errore caricamento recorder:\n{0}"), ex.Message),
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void BtnNew_Click(object sender, EventArgs e)
        {
            using (var editorForm = new RecorderEditorForm())
            {
                if (editorForm.ShowDialog() == DialogResult.OK)
                {
                    var newRecorder = editorForm.GetRecorder();
                    _recorders.Add(newRecorder);
                    AddRecorderControl(newRecorder);

                    StatusChanged?.Invoke(this, LanguageManager.GetString("Recorders.RecorderCreated", "Recorder creato"));
                }
            }
        }

        private void AddRecorderControl(Recorder recorder)
        {
            var recorderControl = new RecorderStreamControl(recorder);
            recorderControl.EditRequested += RecorderControl_EditRequested;
            recorderControl.DeleteRequested += RecorderControl_DeleteRequested;

            flowPanel.Controls.Add(recorderControl);
            _recorderControls.Add(recorderControl);
        }

        private void RecorderControl_EditRequested(object sender, Recorder recorder)
        {
            if (recorder.IsActive)
            {
                MessageBox.Show(
                    LanguageManager.GetString("Recorders.StopBeforeEdit", "Ferma la registrazione prima di modificare le impostazioni."),
                    LanguageManager.GetString("Common.Warning", "Attenzione"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            using (var editorForm = new RecorderEditorForm(recorder))
            {
                if (editorForm.ShowDialog() == DialogResult.OK)
                {
                    var updatedRecorder = editorForm.GetRecorder();

                    recorder.Name = updatedRecorder.Name;
                    recorder.Type = updatedRecorder.Type;
                    recorder.AudioSourceDevice = updatedRecorder.AudioSourceDevice;
                    recorder.OutputPath = updatedRecorder.OutputPath;
                    recorder.Format = updatedRecorder.Format;
                    recorder.Monday = updatedRecorder.Monday;
                    recorder.Tuesday = updatedRecorder.Tuesday;
                    recorder.Wednesday = updatedRecorder.Wednesday;
                    recorder.Thursday = updatedRecorder.Thursday;
                    recorder.Friday = updatedRecorder.Friday;
                    recorder.Saturday = updatedRecorder.Saturday;
                    recorder.Sunday = updatedRecorder.Sunday;
                    recorder.StartTime = updatedRecorder.StartTime;
                    recorder.EndTime = updatedRecorder.EndTime;
                    recorder.RetentionDays = updatedRecorder.RetentionDays;
                    recorder.AutoDeleteOldFiles = updatedRecorder.AutoDeleteOldFiles;

                    RecorderConfigManager.SaveRecorder(recorder);

                    foreach (var control in _recorderControls)
                    {
                        if (control.Recorder == recorder)
                        {
                            control.UpdateStatus();
                            break;
                        }
                    }

                    StatusChanged?.Invoke(this, LanguageManager.GetString("Recorders.RecorderUpdated", "Recorder aggiornato"));
                }
            }
        }

        private void RecorderControl_DeleteRequested(object sender, Recorder recorder)
        {
            try
            {
                RecorderConfigManager.DeleteRecorder(recorder.ID);

                var controlToRemove = _recorderControls.FirstOrDefault(c => c.Recorder == recorder);
                if (controlToRemove != null)
                {
                    flowPanel.Controls.Remove(controlToRemove);
                    _recorderControls.Remove(controlToRemove);
                    controlToRemove.Dispose();
                }

                _recorders.Remove(recorder);

                StatusChanged?.Invoke(this, LanguageManager.GetString("Recorders.RecorderDeleted", "Recorder eliminato"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(LanguageManager.GetString("Recorders.DeleteError", "Errore eliminazione recorder:\n{0}"), ex.Message),
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private async void BtnStartAll_Click(object sender, EventArgs e)
        {
            int startedCount = 0;

            foreach (var control in _recorderControls)
            {
                var recorder = control.Recorder;

                if (!recorder.IsActive)
                {
                    control.StartRecorder();

                    if (recorder.IsActive)
                    {
                        startedCount++;
                        await Task.Delay(500);
                    }
                }
            }

            foreach (var control in _recorderControls)
            {
                control.UpdateStatus();
            }

            StatusChanged?.Invoke(this, string.Format(LanguageManager.GetString("Recorders.StartedCount", "Avviati {0} recorder"), startedCount));
        }

        private void BtnStopAll_Click(object sender, EventArgs e)
        {
            int stoppedCount = 0;

            foreach (var control in _recorderControls)
            {
                var recorder = control.Recorder;

                if (recorder.IsActive)
                {
                    control.StopRecorder();
                    stoppedCount++;
                }
            }

            foreach (var control in _recorderControls)
            {
                control.UpdateStatus();
            }

            StatusChanged?.Invoke(this, string.Format(LanguageManager.GetString("Recorders.StoppedCount", "Fermati {0} recorder"), stoppedCount));
        }

        public int GetTotalRecorders()
        {
            return _recorders?.Count ?? 0;
        }

        public int GetActiveRecorders()
        {
            return _recorders?.Count(r => r.IsActive) ?? 0;
        }

        public void RefreshRecorders()
        {
            foreach (var control in _recorderControls)
            {
                control.Dispose();
            }

            flowPanel.Controls.Clear();
            _recorderControls.Clear();

            LoadRecorders();

            foreach (var recorder in _recorders)
            {
                AddRecorderControl(recorder);
            }

            StatusChanged?.Invoke(this, string.Format(LanguageManager.GetString("Recorders.ElementsCount", "Recorders: {0} elementi"), _recorders.Count));
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