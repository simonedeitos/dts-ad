using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;
using AirDirector.Controls;
using AirDirector.Models;
using AirDirector.Services.Localization;
using AirDirector.Services.RemoteControl;
using AirDirector.Themes;

namespace AirDirector.Forms
{
    public partial class RemoteControlForm : Form
    {
        private const string REGISTRY_KEY = @"SOFTWARE\AirDirector";

        // ── Services ────────────────────────────────────────────────────────────
        private RemoteControlService _remoteService;
        private RemoteAudioService _audioService;
        private System.Windows.Forms.Timer _stateTimer;

        // ── Player references (set by MainForm) ─────────────────────────────────
        private PlayerControl _playerControl;
        private PlayerControlVideo _playerControlVideo;
        private PlaylistQueueControl _playlistQueue;
        private bool _isRadioTVMode;

        // ── UI controls ─────────────────────────────────────────────────────────

        // Connection
        private Label _lblTokenHeader;
        private TextBox _txtToken;
        private Button _btnConnect;
        private Panel _pnlStatus;
        private Label _lblStatusDot;
        private Label _lblStatusText;

        // Options
        private CheckBox _chkAutoOpen;
        private CheckBox _chkMinimizeToTray;

        // Audio source
        private Label _lblAudioSource;
        private ComboBox _cmbAudioSource;
        private Label _lblAudioQuality;
        private ComboBox _cmbAudioQuality;
        private Label _lblAudioOutput;
        private ComboBox _cmbAudioOutput;

        // Users
        private Label _lblUsers;
        private ListView _listUsers;

        // Save
        private Button _btnSave;

        // ── Constructor ─────────────────────────────────────────────────────────

        public RemoteControlForm()
        {
            InitializeComponent();

            _remoteService = new RemoteControlService();
            _audioService = new RemoteAudioService(_remoteService);

            _isRadioTVMode = ConfigurationControl.IsRadioTVMode();

            BuildUI();
            PopulateAudioDevices();
            LoadSettings();
            ApplyLanguage();

            _remoteService.ConnectionStateChanged += OnConnectionStateChanged;
            _remoteService.ConnectedUsersChanged += OnConnectedUsersChanged;
            _remoteService.CommandReceived += OnCommandReceived;

            _stateTimer = new System.Windows.Forms.Timer { Interval = 500 };
            _stateTimer.Tick += StateTimer_Tick;

            LanguageManager.LanguageChanged += OnLanguageChanged;
            this.FormClosing += RemoteControlForm_FormClosing;
        }

        public void SetReferences(PlaylistQueueControl queue, PlayerControl player, PlayerControlVideo playerVideo)
        {
            _playlistQueue = queue;
            _playerControl = player;
            _playerControlVideo = playerVideo;
        }

        // ── Build UI ─────────────────────────────────────────────────────────────

        private void BuildUI()
        {
            this.BackColor = AppTheme.BgLight;
            this.Font = new Font("Segoe UI", 9);
            this.Text = "Remote Control";

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                RowCount = 7,
                ColumnCount = 1,
                AutoSize = false,
                BackColor = AppTheme.BgLight
            };
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));   // Token + connect
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));   // Status
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));   // Checkboxes
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));   // Audio source + quality
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));   // Audio output
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // Users list
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));   // Save button

            // ── Row 0: Token + Connect ────────────────────────────────────────
            var pnlToken = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.BgLight };

            _lblTokenHeader = new Label
            {
                Text = "Token",
                Location = new Point(0, 0),
                AutoSize = true,
                ForeColor = AppTheme.TextPrimary
            };
            pnlToken.Controls.Add(_lblTokenHeader);

            _txtToken = new TextBox
            {
                Location = new Point(0, 20),
                Width = 360,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 10),
                ForeColor = AppTheme.TextPrimary,
                BackColor = Color.White
            };
            pnlToken.Controls.Add(_txtToken);

            _btnConnect = new Button
            {
                Text = "Connetti",
                Location = new Point(370, 18),
                Width = 110,
                Height = 28,
                FlatStyle = FlatStyle.Flat,
                BackColor = AppTheme.Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnConnect.FlatAppearance.BorderSize = 0;
            _btnConnect.Click += BtnConnect_Click;
            pnlToken.Controls.Add(_btnConnect);

            mainPanel.Controls.Add(pnlToken, 0, 0);

            // ── Row 1: Status ─────────────────────────────────────────────────
            _pnlStatus = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.BgLight };

            _lblStatusDot = new Label
            {
                Text = "●",
                ForeColor = AppTheme.Danger,
                Location = new Point(0, 6),
                AutoSize = true,
                Font = new Font("Segoe UI", 12, FontStyle.Bold)
            };
            _pnlStatus.Controls.Add(_lblStatusDot);

            _lblStatusText = new Label
            {
                Text = "Disconnesso",
                ForeColor = AppTheme.TextSecondary,
                Location = new Point(22, 8),
                AutoSize = true,
                Font = new Font("Segoe UI", 9)
            };
            _pnlStatus.Controls.Add(_lblStatusText);

            mainPanel.Controls.Add(_pnlStatus, 0, 1);

            // ── Row 2: Checkboxes ─────────────────────────────────────────────
            var pnlChecks = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.BgLight };

            _chkAutoOpen = new CheckBox
            {
                Text = "Apertura automatica all'avvio",
                Location = new Point(0, 4),
                AutoSize = true,
                ForeColor = AppTheme.TextPrimary
            };
            pnlChecks.Controls.Add(_chkAutoOpen);

            _chkMinimizeToTray = new CheckBox
            {
                Text = "Riduci a icona all'avvio",
                Location = new Point(0, 28),
                AutoSize = true,
                ForeColor = AppTheme.TextPrimary
            };
            pnlChecks.Controls.Add(_chkMinimizeToTray);

            mainPanel.Controls.Add(pnlChecks, 0, 2);

            // ── Row 3: Audio source + quality ─────────────────────────────────
            var pnlAudio = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                BackColor = AppTheme.BgLight
            };
            pnlAudio.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            pnlAudio.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            pnlAudio.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
            pnlAudio.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

            _lblAudioSource = new Label { Text = "Sorgente audio invio", AutoSize = true, ForeColor = AppTheme.TextPrimary, Anchor = AnchorStyles.Left | AnchorStyles.Bottom };
            _cmbAudioSource = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };

            _lblAudioQuality = new Label { Text = "Qualità invio", AutoSize = true, ForeColor = AppTheme.TextPrimary, Anchor = AnchorStyles.Left | AnchorStyles.Bottom };
            _cmbAudioQuality = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };

            pnlAudio.Controls.Add(_lblAudioSource, 0, 0);
            pnlAudio.Controls.Add(_lblAudioQuality, 1, 0);
            pnlAudio.Controls.Add(_cmbAudioSource, 0, 1);
            pnlAudio.Controls.Add(_cmbAudioQuality, 1, 1);

            mainPanel.Controls.Add(pnlAudio, 0, 3);

            // ── Row 4: Audio output ───────────────────────────────────────────
            var pnlOutput = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.BgLight };

            _lblAudioOutput = new Label
            {
                Text = "Periferica uscita audio ricevuto",
                Location = new Point(0, 0),
                AutoSize = true,
                ForeColor = AppTheme.TextPrimary
            };
            pnlOutput.Controls.Add(_lblAudioOutput);

            _cmbAudioOutput = new ComboBox
            {
                Location = new Point(0, 20),
                Width = 480,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            pnlOutput.Controls.Add(_cmbAudioOutput);

            mainPanel.Controls.Add(pnlOutput, 0, 4);

            // ── Row 5: Connected users ────────────────────────────────────────
            var pnlUsers = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.BgLight };

            _lblUsers = new Label
            {
                Text = "Utenti collegati",
                Location = new Point(0, 0),
                AutoSize = true,
                ForeColor = AppTheme.TextPrimary,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            pnlUsers.Controls.Add(_lblUsers);

            _listUsers = new ListView
            {
                Location = new Point(0, 22),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BackColor = Color.White,
                ForeColor = AppTheme.TextPrimary,
                Font = new Font("Segoe UI", 9),
                BorderStyle = BorderStyle.FixedSingle
            };
            _listUsers.Columns.Add("Name", 280);
            _listUsers.Columns.Add("Mic", 80);
            pnlUsers.Controls.Add(_listUsers);
            pnlUsers.Resize += (s, e) =>
            {
                _listUsers.Size = new Size(pnlUsers.Width, pnlUsers.Height - 22);
            };

            mainPanel.Controls.Add(pnlUsers, 0, 5);

            // ── Row 6: Save button ────────────────────────────────────────────
            _btnSave = new Button
            {
                Text = "💾 Salva impostazioni",
                Dock = DockStyle.Right,
                Width = 180,
                Height = 34,
                FlatStyle = FlatStyle.Flat,
                BackColor = AppTheme.Success,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnSave.FlatAppearance.BorderSize = 0;
            _btnSave.Click += BtnSave_Click;
            mainPanel.Controls.Add(_btnSave, 0, 6);

            this.Controls.Add(mainPanel);
        }

        // ── Audio device population ───────────────────────────────────────────

        private void PopulateAudioDevices()
        {
            // Audio source
            _cmbAudioSource.Items.Clear();
            _cmbAudioSource.Items.Add("airdirector");
            foreach (string dev in RemoteAudioService.GetInputDevices())
                _cmbAudioSource.Items.Add(dev);

            // Audio quality
            _cmbAudioQuality.Items.Clear();
            _cmbAudioQuality.Items.Add("low");
            _cmbAudioQuality.Items.Add("medium");
            _cmbAudioQuality.Items.Add("high");
            _cmbAudioQuality.Items.Add("studio");
            _cmbAudioQuality.SelectedIndex = 1;

            // Audio output
            _cmbAudioOutput.Items.Clear();
            _cmbAudioOutput.Items.Add("default");
            foreach (string dev in RemoteAudioService.GetOutputDevices())
                _cmbAudioOutput.Items.Add(dev);
        }

        // ── Load / Save settings ──────────────────────────────────────────────

        private void LoadSettings()
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(REGISTRY_KEY);

                _txtToken.Text = key.GetValue("RC_Token", "")?.ToString() ?? "";
                _chkAutoOpen.Checked = Convert.ToBoolean(key.GetValue("RC_AutoOpen", 0));
                _chkMinimizeToTray.Checked = Convert.ToBoolean(key.GetValue("RC_MinimizeToTray", 0));

                string audioSource = key.GetValue("RC_AudioSource", "airdirector")?.ToString() ?? "airdirector";
                SelectComboItem(_cmbAudioSource, audioSource);

                string audioQuality = key.GetValue("RC_AudioQuality", "medium")?.ToString() ?? "medium";
                SelectComboItem(_cmbAudioQuality, audioQuality);

                string audioOutput = key.GetValue("RC_AudioOutput", "default")?.ToString() ?? "default";
                SelectComboItem(_cmbAudioOutput, audioOutput);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RemoteControlForm] ⚠️ LoadSettings error: {ex.Message}");
            }
        }

        private void SaveSettings()
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(REGISTRY_KEY);

                key.SetValue("RC_Token", _txtToken.Text.Trim());
                key.SetValue("RC_AutoOpen", _chkAutoOpen.Checked ? 1 : 0);
                key.SetValue("RC_MinimizeToTray", _chkMinimizeToTray.Checked ? 1 : 0);
                key.SetValue("RC_AudioSource", _cmbAudioSource.SelectedItem?.ToString() ?? "airdirector");
                key.SetValue("RC_AudioQuality", _cmbAudioQuality.SelectedItem?.ToString() ?? "medium");
                key.SetValue("RC_AudioOutput", _cmbAudioOutput.SelectedItem?.ToString() ?? "default");

                MessageBox.Show(
                    LanguageManager.GetString("RemoteControl.SettingsSaved", "Impostazioni salvate"),
                    LanguageManager.GetString("RemoteControl.Title", "Remote Control"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RemoteControlForm] ❌ SaveSettings error: {ex.Message}");
            }
        }

        public static bool GetAutoOpenOnStartup()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY);
                return Convert.ToBoolean(key?.GetValue("RC_AutoOpen", 0) ?? 0);
            }
            catch { return false; }
        }

        public static bool GetMinimizeToTrayOnStartup()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY);
                return Convert.ToBoolean(key?.GetValue("RC_MinimizeToTray", 0) ?? 0);
            }
            catch { return false; }
        }

        // ── Event handlers ────────────────────────────────────────────────────

        private async void BtnConnect_Click(object sender, EventArgs e)
        {
            if (_remoteService.State == ConnectionState.Disconnected)
            {
                string token = _txtToken.Text.Trim();
                if (string.IsNullOrEmpty(token))
                {
                    MessageBox.Show(
                        LanguageManager.GetString("RemoteControl.TokenRequired", "Inserire un token valido"),
                        LanguageManager.GetString("RemoteControl.Title", "Remote Control"),
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string audioSource = _cmbAudioSource.SelectedItem?.ToString() ?? "airdirector";
                string audioQuality = _cmbAudioQuality.SelectedItem?.ToString() ?? "medium";
                string audioOutput = _cmbAudioOutput.SelectedItem?.ToString() ?? "default";
                _audioService.Configure(audioSource, audioQuality, audioOutput);

                await _remoteService.ConnectAsync(token);

                if (_remoteService.State == ConnectionState.Connected)
                {
                    _audioService.StartCapture();
                    _audioService.StartPlayback();
                    _stateTimer.Start();
                }
            }
            else
            {
                _stateTimer.Stop();
                _audioService.StopCapture();
                _audioService.StopPlayback();
                _remoteService.Disconnect();
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void StateTimer_Tick(object sender, EventArgs e)
        {
            UpdatePlayerState();
        }

        private void UpdatePlayerState()
        {
            try
            {
                if (_isRadioTVMode && _playerControlVideo != null)
                {
                    _remoteService.PlayerStatus = _playerControlVideo.IsPlaying ? "playing" : "stopped";
                    _remoteService.PlayerTrack = _playerControlVideo.CurrentArtist + " - " + _playerControlVideo.CurrentTitle;
                    _remoteService.PlayerPosition = _playerControlVideo.CurrentPositionMs / 1000;
                    _remoteService.PlayerDuration = _playerControlVideo.CurrentDurationMs / 1000;
                }
                else if (_playerControl != null)
                {
                    _remoteService.PlayerStatus = _playerControl.IsPlaying ? "playing" : "stopped";
                    _remoteService.PlayerTrack = _playerControl.CurrentArtist + " - " + _playerControl.CurrentTitle;
                    _remoteService.PlayerPosition = _playerControl.CurrentPositionMs / 1000;
                    _remoteService.PlayerDuration = _playerControl.CurrentDurationMs / 1000;
                }
            }
            catch { }
        }

        private void OnConnectionStateChanged(object sender, ConnectionState state)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnConnectionStateChanged(sender, state)));
                return;
            }

            switch (state)
            {
                case ConnectionState.Connected:
                    _lblStatusDot.ForeColor = AppTheme.Success;
                    _lblStatusText.Text = LanguageManager.GetString("RemoteControl.Status_Connected", "Connesso");
                    _lblStatusText.ForeColor = AppTheme.Success;
                    _btnConnect.Text = LanguageManager.GetString("RemoteControl.Disconnect", "Disconnetti");
                    _btnConnect.BackColor = AppTheme.Danger;
                    break;

                case ConnectionState.Connecting:
                    _lblStatusDot.ForeColor = AppTheme.Warning;
                    _lblStatusText.Text = LanguageManager.GetString("RemoteControl.Status_Connecting", "Connessione in corso...");
                    _lblStatusText.ForeColor = AppTheme.Warning;
                    _btnConnect.Enabled = false;
                    break;

                case ConnectionState.Disconnected:
                    _lblStatusDot.ForeColor = AppTheme.Danger;
                    _lblStatusText.Text = LanguageManager.GetString("RemoteControl.Status_Disconnected", "Disconnesso");
                    _lblStatusText.ForeColor = AppTheme.TextSecondary;
                    _btnConnect.Text = LanguageManager.GetString("RemoteControl.Connect", "Connetti");
                    _btnConnect.BackColor = AppTheme.Primary;
                    _btnConnect.Enabled = true;
                    _listUsers.Items.Clear();
                    break;
            }
        }

        private void OnConnectedUsersChanged(object sender, List<ConnectedUser> users)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnConnectedUsersChanged(sender, users)));
                return;
            }

            _listUsers.Items.Clear();
            foreach (var user in users)
            {
                var item = new ListViewItem(user.Name);
                item.SubItems.Add(user.MicActive ? "🔴 " + LanguageManager.GetString("RemoteControl.MicActive", "Attivo") : LanguageManager.GetString("RemoteControl.MicInactive", "Spento"));
                _listUsers.Items.Add(item);
            }
        }

        private void OnCommandReceived(object sender, string command)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnCommandReceived(sender, command)));
                return;
            }

            try
            {
                switch (command)
                {
                    case "play":
                        if (_isRadioTVMode) _playerControlVideo?.Play();
                        else _playerControl?.Play();
                        break;
                    case "stop":
                        if (_isRadioTVMode) _playerControlVideo?.Stop();
                        else _playerControl?.Stop();
                        break;
                    case "pause":
                        if (_isRadioTVMode) _playerControlVideo?.Pause();
                        else _playerControl?.Pause();
                        break;
                    case "skip":
                        if (_isRadioTVMode) _playerControlVideo?.Skip();
                        else _playerControl?.Skip();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RemoteControlForm] ⚠️ Command error: {ex.Message}");
            }
        }

        private void OnLanguageChanged(object sender, EventArgs e) => ApplyLanguage();

        private void ApplyLanguage()
        {
            this.Text = LanguageManager.GetString("RemoteControl.Title", "Remote Control");
            _lblTokenHeader.Text = LanguageManager.GetString("RemoteControl.Token", "Token");
            _chkAutoOpen.Text = LanguageManager.GetString("RemoteControl.AutoOpen", "Apertura automatica all'avvio");
            _chkMinimizeToTray.Text = LanguageManager.GetString("RemoteControl.MinimizeToTray", "Riduci a icona all'avvio");
            _lblAudioSource.Text = LanguageManager.GetString("RemoteControl.AudioSource", "Sorgente audio invio");
            _lblAudioQuality.Text = LanguageManager.GetString("RemoteControl.AudioQuality", "Qualità invio");
            _lblAudioOutput.Text = LanguageManager.GetString("RemoteControl.AudioOutput", "Periferica uscita audio ricevuto");
            _lblUsers.Text = LanguageManager.GetString("RemoteControl.ConnectedUsers", "Utenti collegati");
            _btnSave.Text = "💾 " + LanguageManager.GetString("RemoteControl.SaveSettings", "Salva impostazioni");

            // Update column headers
            if (_listUsers.Columns.Count >= 2)
            {
                _listUsers.Columns[0].Text = LanguageManager.GetString("RemoteControl.UserName", "Nome");
                _listUsers.Columns[1].Text = LanguageManager.GetString("RemoteControl.MicStatus", "Microfono");
            }

            // Update connect/disconnect button if not in connecting state
            if (_remoteService.State != ConnectionState.Connecting)
            {
                if (_remoteService.State == ConnectionState.Connected)
                    _btnConnect.Text = LanguageManager.GetString("RemoteControl.Disconnect", "Disconnetti");
                else
                    _btnConnect.Text = LanguageManager.GetString("RemoteControl.Connect", "Connetti");
            }

            // Update status label
            OnConnectionStateChanged(this, _remoteService.State);
        }

        private void RemoteControlForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_remoteService.State != ConnectionState.Disconnected)
            {
                _stateTimer.Stop();
                _audioService.StopCapture();
                _audioService.StopPlayback();
                _remoteService.Disconnect();
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SelectComboItem(ComboBox cmb, string value)
        {
            for (int i = 0; i < cmb.Items.Count; i++)
            {
                if (string.Equals(cmb.Items[i]?.ToString(), value, StringComparison.OrdinalIgnoreCase))
                {
                    cmb.SelectedIndex = i;
                    return;
                }
            }
            if (cmb.Items.Count > 0)
                cmb.SelectedIndex = 0;
        }
    }
}
