using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
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
        private System.Windows.Forms.Timer _vuTimer;

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

        // VU meter
        private Panel _pnlVuMeter;
        private float _currentVuLevel = 0f;
        private Label _lblVuHeader;

        // Users
        private Label _lblUsers;
        private Label _lblUserCount;
        private FlowLayoutPanel _pnlUsersList;

        // Buttons
        private Button _btnSave;
        private Button _btnLog;

        // Audio state tracking
        private bool _audioStartedByForm = false;

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

            _audioService.InputLevelChanged += OnInputLevelChanged;

            _stateTimer = new System.Windows.Forms.Timer { Interval = 500 };
            _stateTimer.Tick += StateTimer_Tick;

            _vuTimer = new System.Windows.Forms.Timer { Interval = 50 };
            _vuTimer.Tick += VuTimer_Tick;
            _vuTimer.Start();

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
                Padding = new Padding(14),
                RowCount = 7,
                ColumnCount = 1,
                AutoSize = false,
                BackColor = AppTheme.BgLight
            };
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));  // Connection card
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));   // Options card
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));  // Audio card
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));   // Audio output
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));   // VU meter
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // Users card
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));   // Bottom buttons

            // ── Row 0: Connection card ────────────────────────────────────────
            var pnlConnection = CreateCard();
            pnlConnection.Padding = new Padding(10, 8, 10, 8);

            _lblTokenHeader = new Label
            {
                Text = "🔑 Token",
                Dock = DockStyle.Top,
                Height = 20,
                ForeColor = AppTheme.TextPrimary,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            pnlConnection.Controls.Add(_lblTokenHeader);

            var pnlTokenRow = new Panel { Dock = DockStyle.Top, Height = 34 };
            pnlTokenRow.Padding = new Padding(0, 4, 0, 0);

            _txtToken = new TextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 10),
                ForeColor = AppTheme.TextPrimary,
                BackColor = Color.White
            };
            _btnConnect = new Button
            {
                Dock = DockStyle.Right,
                Text = "Connetti",
                Width = 120,
                FlatStyle = FlatStyle.Flat,
                BackColor = AppTheme.Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(6, 0, 0, 0)
            };
            _btnConnect.FlatAppearance.BorderSize = 0;
            _btnConnect.Click += BtnConnect_Click;

            pnlTokenRow.Controls.Add(_txtToken);
            pnlTokenRow.Controls.Add(_btnConnect);
            pnlConnection.Controls.Add(pnlTokenRow);

            // Status indicator
            _pnlStatus = new Panel { Dock = DockStyle.Bottom, Height = 22 };

            _lblStatusDot = new Label
            {
                Text = "●",
                ForeColor = AppTheme.Danger,
                Location = new Point(0, 2),
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            _pnlStatus.Controls.Add(_lblStatusDot);

            _lblStatusText = new Label
            {
                Text = "Disconnesso",
                ForeColor = AppTheme.TextSecondary,
                Location = new Point(18, 4),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.25f)
            };
            _pnlStatus.Controls.Add(_lblStatusText);
            pnlConnection.Controls.Add(_pnlStatus);

            // Add controls in reverse order for Dock layout
            pnlConnection.Controls.SetChildIndex(_lblTokenHeader, 2);
            pnlConnection.Controls.SetChildIndex(pnlTokenRow, 1);
            pnlConnection.Controls.SetChildIndex(_pnlStatus, 0);

            mainPanel.Controls.Add(pnlConnection, 0, 0);

            // ── Row 1: Options card ───────────────────────────────────────────
            var pnlOpts = CreateCard();
            pnlOpts.Padding = new Padding(10, 8, 10, 4);

            _chkAutoOpen = new CheckBox
            {
                Text = "Apertura automatica all'avvio",
                Dock = DockStyle.Top,
                Height = 22,
                ForeColor = AppTheme.TextPrimary,
                Font = new Font("Segoe UI", 8.25f)
            };

            _chkMinimizeToTray = new CheckBox
            {
                Text = "Riduci a icona all'avvio",
                Dock = DockStyle.Top,
                Height = 22,
                ForeColor = AppTheme.TextPrimary,
                Font = new Font("Segoe UI", 8.25f)
            };

            pnlOpts.Controls.Add(_chkMinimizeToTray);
            pnlOpts.Controls.Add(_chkAutoOpen);
            mainPanel.Controls.Add(pnlOpts, 0, 1);

            // ── Row 2: Audio source + quality card ────────────────────────────
            var pnlAudioCard = CreateCard();
            pnlAudioCard.Padding = new Padding(10, 6, 10, 6);

            var pnlAudio = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            pnlAudio.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            pnlAudio.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            pnlAudio.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            pnlAudio.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            _lblAudioSource = new Label { Text = "🎤 Sorgente audio invio", AutoSize = true, ForeColor = AppTheme.TextPrimary, Anchor = AnchorStyles.Left | AnchorStyles.Bottom, Font = new Font("Segoe UI", 8.25f, FontStyle.Bold) };
            _cmbAudioSource = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 2, 6, 0) };

            _lblAudioQuality = new Label { Text = "📊 Qualità invio", AutoSize = true, ForeColor = AppTheme.TextPrimary, Anchor = AnchorStyles.Left | AnchorStyles.Bottom, Font = new Font("Segoe UI", 8.25f, FontStyle.Bold) };
            _cmbAudioQuality = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 2, 0, 0) };

            pnlAudio.Controls.Add(_lblAudioSource, 0, 0);
            pnlAudio.Controls.Add(_lblAudioQuality, 1, 0);
            pnlAudio.Controls.Add(_cmbAudioSource, 0, 1);
            pnlAudio.Controls.Add(_cmbAudioQuality, 1, 1);

            pnlAudioCard.Controls.Add(pnlAudio);
            mainPanel.Controls.Add(pnlAudioCard, 0, 2);

            // ── Row 3: Audio output ───────────────────────────────────────────
            var pnlOutputCard = CreateCard();
            pnlOutputCard.Padding = new Padding(10, 6, 10, 6);

            _lblAudioOutput = new Label
            {
                Text = "🔊 Periferica uscita audio ricevuto",
                Dock = DockStyle.Top,
                Height = 20,
                ForeColor = AppTheme.TextPrimary,
                Font = new Font("Segoe UI", 8.25f, FontStyle.Bold)
            };

            _cmbAudioOutput = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 2, 0, 0)
            };

            pnlOutputCard.Controls.Add(_cmbAudioOutput);
            pnlOutputCard.Controls.Add(_lblAudioOutput);
            mainPanel.Controls.Add(pnlOutputCard, 0, 3);

            // ── Row 4: VU Meter ───────────────────────────────────────────────
            var pnlVuCard = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 2, 0, 2) };

            _lblVuHeader = new Label
            {
                Text = "🔊 Audio Level",
                Dock = DockStyle.Left,
                Width = 100,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = AppTheme.TextSecondary,
                Font = new Font("Segoe UI", 8f)
            };
            pnlVuCard.Controls.Add(_lblVuHeader);

            _pnlVuMeter = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 20,
                BackColor = Color.FromArgb(40, 40, 40),
                Margin = new Padding(4, 12, 4, 12)
            };
            _pnlVuMeter.Paint += PnlVuMeter_Paint;
            pnlVuCard.Controls.Add(_pnlVuMeter);

            // Ensure label is in front
            _lblVuHeader.BringToFront();

            mainPanel.Controls.Add(pnlVuCard, 0, 4);

            // ── Row 5: Connected users card ───────────────────────────────────
            var pnlUsersCard = CreateCard();
            pnlUsersCard.Padding = new Padding(10, 6, 10, 6);

            var pnlUsersHeader = new Panel { Dock = DockStyle.Top, Height = 24 };

            _lblUsers = new Label
            {
                Text = "👥 Utenti collegati",
                Dock = DockStyle.Left,
                AutoSize = true,
                ForeColor = AppTheme.TextPrimary,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Padding = new Padding(0, 3, 0, 0)
            };
            pnlUsersHeader.Controls.Add(_lblUsers);

            _lblUserCount = new Label
            {
                Text = "0",
                Dock = DockStyle.Right,
                Width = 32,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                BackColor = AppTheme.TextSecondary,
                Font = new Font("Segoe UI", 8, FontStyle.Bold)
            };
            _lblUserCount.Paint += (s, e) =>
            {
                var rect = _lblUserCount.ClientRectangle;
                using var path = CreateRoundedRectPath(rect, 10);
                using var brush = new SolidBrush(_lblUserCount.BackColor);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.FillPath(brush, path);
                TextRenderer.DrawText(e.Graphics, _lblUserCount.Text, _lblUserCount.Font,
                    rect, _lblUserCount.ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            pnlUsersHeader.Controls.Add(_lblUserCount);
            pnlUsersCard.Controls.Add(pnlUsersHeader);

            _pnlUsersList = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.White,
                Padding = new Padding(4)
            };
            pnlUsersCard.Controls.Add(_pnlUsersList);

            // Fix z-order for dock
            pnlUsersCard.Controls.SetChildIndex(_pnlUsersList, 0);
            pnlUsersCard.Controls.SetChildIndex(pnlUsersHeader, 1);

            mainPanel.Controls.Add(pnlUsersCard, 0, 5);

            // ── Row 6: Bottom buttons ─────────────────────────────────────────
            var pnlButtons = new Panel { Dock = DockStyle.Fill };

            _btnLog = new Button
            {
                Text = "📋 Log",
                Dock = DockStyle.Left,
                Width = 100,
                Height = 34,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(55, 71, 79),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnLog.FlatAppearance.BorderSize = 0;
            _btnLog.Click += BtnLog_Click;
            pnlButtons.Controls.Add(_btnLog);

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
            pnlButtons.Controls.Add(_btnSave);

            mainPanel.Controls.Add(pnlButtons, 0, 6);

            this.Controls.Add(mainPanel);
        }

        // ── Card helper ──────────────────────────────────────────────────────

        private static Panel CreateCard()
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Surface,
                Margin = new Padding(0, 2, 0, 2)
            };
            card.Paint += (s, e) =>
            {
                var rect = card.ClientRectangle;
                rect.Inflate(-1, -1);
                using var pen = new Pen(AppTheme.BorderLight, 1);
                using var path = CreateRoundedRectPath(rect, 6);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.DrawPath(pen, path);
            };
            return card;
        }

        private static GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        // ── VU Meter painting ────────────────────────────────────────────────

        private void PnlVuMeter_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var rect = _pnlVuMeter.ClientRectangle;

            // Background
            g.Clear(Color.FromArgb(40, 40, 40));

            if (_currentVuLevel <= 0f) return;

            float level = Math.Min(_currentVuLevel, 1.0f);
            int barWidth = (int)(rect.Width * level);

            if (barWidth <= 0) return;

            // Draw segmented VU bars
            int segW = 4;
            int gap = 1;
            int greenEnd = (int)(rect.Width * 0.6f);
            int yellowEnd = (int)(rect.Width * 0.85f);

            for (int x = 0; x < barWidth; x += segW + gap)
            {
                int w = Math.Min(segW, barWidth - x);
                Color segColor;
                if (x < greenEnd) segColor = AppTheme.VUGreen;
                else if (x < yellowEnd) segColor = AppTheme.VUYellow;
                else segColor = AppTheme.VURed;

                using var brush = new SolidBrush(segColor);
                g.FillRectangle(brush, x + 2, 2, w, rect.Height - 4);
            }
        }

        private void VuTimer_Tick(object sender, EventArgs e)
        {
            // Smoothly decay VU level
            _currentVuLevel *= 0.85f;
            if (_currentVuLevel < 0.005f) _currentVuLevel = 0f;
            _pnlVuMeter?.Invalidate();
        }

        private void OnInputLevelChanged(object sender, float level)
        {
            // Use peak hold: only update if higher
            if (level > _currentVuLevel)
                _currentVuLevel = level;
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
                _remoteService?.LogWarning($"LoadSettings error: {ex.Message}");
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
                _remoteService?.LogError($"SaveSettings error: {ex.Message}");
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

                ConfigureAudio();
                await _remoteService.ConnectAsync(token);

                // Audio start is now handled by OnConnectionStateChanged
            }
            else
            {
                StopAudioAndTimer();
                _remoteService.Disconnect();
            }
        }

        private void ConfigureAudio()
        {
            string audioSource = _cmbAudioSource.SelectedItem?.ToString() ?? "airdirector";
            string audioQuality = _cmbAudioQuality.SelectedItem?.ToString() ?? "medium";
            string audioOutput = _cmbAudioOutput.SelectedItem?.ToString() ?? "default";
            _audioService.Configure(audioSource, audioQuality, audioOutput);
        }

        private void StartAudioAndTimer()
        {
            if (_audioStartedByForm) return;
            _audioStartedByForm = true;
            _audioService.StartCapture();
            _audioService.StartPlayback();
            _stateTimer.Start();
            _remoteService.Log("Audio capture and playback started.");
        }

        private void StopAudioAndTimer()
        {
            if (!_audioStartedByForm) return;
            _audioStartedByForm = false;
            _stateTimer.Stop();
            _audioService.StopCapture();
            _audioService.StopPlayback();
            _remoteService.Log("Audio capture and playback stopped.");
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void BtnLog_Click(object sender, EventArgs e)
        {
            ShowLogDialog();
        }

        private void ShowLogDialog()
        {
            var logForm = new Form
            {
                Text = "📋 Remote Control Log",
                Width = 620,
                Height = 460,
                StartPosition = FormStartPosition.CenterParent,
                MinimumSize = new Size(400, 300),
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.LimeGreen,
                ShowInTaskbar = false
            };

            var txtLog = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                ReadOnly = true,
                Font = new Font("Consolas", 9),
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.LimeGreen,
                WordWrap = false
            };

            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.FromArgb(45, 45, 45)
            };

            var btnClear = new Button
            {
                Text = "🗑️ " + LanguageManager.GetString("RemoteControl.ClearLog", "Pulisci"),
                Dock = DockStyle.Right,
                Width = 100,
                FlatStyle = FlatStyle.Flat,
                BackColor = AppTheme.Danger,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClear.FlatAppearance.BorderSize = 0;
            btnClear.Click += (s, ev) =>
            {
                _remoteService.ClearLog();
                txtLog.Clear();
                txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] Log cleared.\r\n");
            };

            var lblTitle = new Label
            {
                Text = "📡 " + LanguageManager.GetString("RemoteControl.LogTitle", "Connection Log"),
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0)
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(btnClear);

            logForm.Controls.Add(txtLog);
            logForm.Controls.Add(pnlHeader);

            // Populate existing log
            var entries = _remoteService.LogEntries;
            foreach (var entry in entries)
            {
                string icon = entry.Level switch
                {
                    "ERROR" => "❌",
                    "WARN" => "⚠️",
                    _ => "ℹ️"
                };
                txtLog.AppendText($"[{entry.Timestamp:HH:mm:ss}] {icon} {entry.Message}\r\n");
            }

            // Live update handler
            EventHandler<RemoteControlLogEntry> handler = null;
            handler = (s, entry) =>
            {
                if (logForm.IsDisposed) return;
                try
                {
                    logForm.Invoke(new Action(() =>
                    {
                        string icon = entry.Level switch
                        {
                            "ERROR" => "❌",
                            "WARN" => "⚠️",
                            _ => "ℹ️"
                        };
                        txtLog.AppendText($"[{entry.Timestamp:HH:mm:ss}] {icon} {entry.Message}\r\n");
                        txtLog.SelectionStart = txtLog.Text.Length;
                        txtLog.ScrollToCaret();
                    }));
                }
                catch { }
            };

            _remoteService.LogAdded += handler;
            logForm.FormClosed += (s, ev) => _remoteService.LogAdded -= handler;

            // Scroll to end
            if (txtLog.Text.Length > 0)
            {
                txtLog.SelectionStart = txtLog.Text.Length;
                txtLog.ScrollToCaret();
            }

            logForm.Show(this);
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
                    _btnConnect.Enabled = true;
                    // Start audio on (re)connect
                    ConfigureAudio();
                    StartAudioAndTimer();
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
                    // Stop audio on disconnect
                    StopAudioAndTimer();
                    UpdateUsersPanel(new List<ConnectedUser>());
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

            UpdateUsersPanel(users);
        }

        private void UpdateUsersPanel(List<ConnectedUser> users)
        {
            _pnlUsersList.Controls.Clear();
            _lblUserCount.Text = users.Count.ToString();
            _lblUserCount.BackColor = users.Count > 0 ? AppTheme.Success : AppTheme.TextSecondary;
            _lblUserCount.Invalidate();

            foreach (var user in users)
            {
                var userPanel = new Panel
                {
                    Width = _pnlUsersList.Width - 30,
                    Height = 36,
                    BackColor = Color.FromArgb(245, 248, 250),
                    Margin = new Padding(2, 2, 2, 2),
                    Padding = new Padding(8, 0, 8, 0)
                };
                userPanel.Paint += (s, e) =>
                {
                    var rect = userPanel.ClientRectangle;
                    rect.Inflate(-1, -1);
                    using var pen = new Pen(AppTheme.BorderLight, 1);
                    using var path = CreateRoundedRectPath(rect, 4);
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.DrawPath(pen, path);
                };

                var lblIcon = new Label
                {
                    Text = "👤",
                    Dock = DockStyle.Left,
                    Width = 26,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 11)
                };
                userPanel.Controls.Add(lblIcon);

                var lblName = new Label
                {
                    Text = user.Name,
                    Dock = DockStyle.Fill,
                    ForeColor = AppTheme.TextPrimary,
                    Font = new Font("Segoe UI", 9),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(4, 0, 0, 0)
                };
                userPanel.Controls.Add(lblName);

                var lblMic = new Label
                {
                    Text = user.MicActive
                        ? "🔴 " + LanguageManager.GetString("RemoteControl.MicActive", "Attivo")
                        : LanguageManager.GetString("RemoteControl.MicInactive", "Spento"),
                    Dock = DockStyle.Right,
                    Width = 80,
                    ForeColor = user.MicActive ? AppTheme.Danger : AppTheme.TextSecondary,
                    Font = new Font("Segoe UI", 8),
                    TextAlign = ContentAlignment.MiddleRight
                };
                userPanel.Controls.Add(lblMic);

                // Fix z-order for dock layout
                lblName.BringToFront();

                _pnlUsersList.Controls.Add(userPanel);
            }

            if (users.Count == 0)
            {
                var lblEmpty = new Label
                {
                    Text = LanguageManager.GetString("RemoteControl.NoUsers", "Nessun utente collegato"),
                    Dock = DockStyle.Top,
                    Height = 40,
                    ForeColor = AppTheme.TextSecondary,
                    Font = new Font("Segoe UI", 9, FontStyle.Italic),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                _pnlUsersList.Controls.Add(lblEmpty);
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
                _remoteService?.LogWarning($"Command error: {ex.Message}");
            }
        }

        private void OnLanguageChanged(object sender, EventArgs e) => ApplyLanguage();

        private void ApplyLanguage()
        {
            this.Text = LanguageManager.GetString("RemoteControl.Title", "Remote Control");
            _lblTokenHeader.Text = "🔑 " + LanguageManager.GetString("RemoteControl.Token", "Token");
            _chkAutoOpen.Text = LanguageManager.GetString("RemoteControl.AutoOpen", "Apertura automatica all'avvio");
            _chkMinimizeToTray.Text = LanguageManager.GetString("RemoteControl.MinimizeToTray", "Riduci a icona all'avvio");
            _lblAudioSource.Text = "🎤 " + LanguageManager.GetString("RemoteControl.AudioSource", "Sorgente audio invio");
            _lblAudioQuality.Text = "📊 " + LanguageManager.GetString("RemoteControl.AudioQuality", "Qualità invio");
            _lblAudioOutput.Text = "🔊 " + LanguageManager.GetString("RemoteControl.AudioOutput", "Periferica uscita audio ricevuto");
            _lblUsers.Text = "👥 " + LanguageManager.GetString("RemoteControl.ConnectedUsers", "Utenti collegati");
            _btnSave.Text = "💾 " + LanguageManager.GetString("RemoteControl.SaveSettings", "Salva impostazioni");
            _btnLog.Text = "📋 " + LanguageManager.GetString("RemoteControl.Log", "Log");
            _lblVuHeader.Text = "🔊 " + LanguageManager.GetString("RemoteControl.AudioLevel", "Audio Level");

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
                StopAudioAndTimer();
                _remoteService.Disconnect();
            }
            _vuTimer?.Stop();
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
