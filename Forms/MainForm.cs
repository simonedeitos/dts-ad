// PORCODDIO
// PORCAMADONNA
// THIS IS MY SIGNATURE DIO INFAMEEEE!!!!

using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.IO.Compression;
using AirDirector.Services;
using AirDirector.Services.Licensing;
using AirDirector.Services.Localization;
using AirDirector.Services.Database;
using AirDirector.Themes;
using AirDirector.Controls;
using AirDirector.Forms;

namespace AirDirector.Forms
{
    public partial class MainForm : Form
    {
        private MenuStrip menuStrip;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblStatus;
        private ToolStripStatusLabel lblLicense;
        private TabControl tabControl;
        private FileWatcherService _fileWatcher;
        private PlaylistQueueControl playlistQueue;

        // ═══════════════════════════════════════════════════════════
        // PLAYER — uno dei due sarà attivo in base alla modalità
        // ═══════════════════════════════════════════════════════════
        private PlayerControl playerControl;           // Modalità Radio
        private PlayerControlVideo playerControlVideo; // Modalità RadioTV
        private bool _isRadioTVMode = false;

        private System.Windows.Forms.Timer _backupTimer;
        private DateTime _lastBackupDate = DateTime.MinValue;

        private OverviewControl overviewControl;
        private ArchiveControl musicArchiveControl;
        private ArchiveControl clipsArchiveControl;
        private SchedulesControl schedulesControl;
        private EncodersControl encodersControl;
        private RecordersControl recordersControl;
        private ConfigurationControl configurationControl;
        private ClocksControl clocksControl;
        private ReportControl reportControl;
        private WhatsAppControl whatsAppControl;

        private Label lblCurrentClock;
        private Label lblQueueTitle;
        private Label lblQueueCount;
        private Label lblClockLabel;
        private Button btnSelectClock;
        private Button btnReloadSchedules;
        private Button btnClearQueue;

        public MainForm()
        {
            ConfigurationControl.RestoreDriveMappingsOnStartup();

            // ═══ Determina modalità PRIMA di costruire la UI ═══
            _isRadioTVMode = ConfigurationControl.IsRadioTVMode();

            InitializeComponent();
            InitializeMainLayout();
            InitializeFileWatcher();
            InitializeBackupTimer();
            UpdateLicenseStatus();
            CleanOldLogs();

            LanguageManager.LanguageChanged += OnLanguageChanged;
            this.FormClosing += MainForm_FormClosing;
        }

        private void CleanOldLogs()
        {
            try
            {
                string logsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

                if (!Directory.Exists(logsPath))
                    return;

                DateTime cutoffDate = DateTime.Now.AddDays(-100);

                foreach (var dir in Directory.GetDirectories(logsPath))
                {
                    try
                    {
                        string folderName = Path.GetFileName(dir);
                        if (DateTime.TryParseExact(folderName, "yyyy-MM-dd",
                            System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None, out DateTime folderDate))
                        {
                            if (folderDate < cutoffDate)
                            {
                                Directory.Delete(dir, true);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[MainForm] ⚠️ Impossibile eliminare cartella log: {Path.GetFileName(dir)} - {ex.Message}");
                    }
                }

                // Rimuovi anche eventuali file sparsi nella root Logs (vecchio formato)
                try
                {
                    foreach (var file in Directory.GetFiles(logsPath, "*.*", SearchOption.TopDirectoryOnly))
                    {
                        var fi = new FileInfo(file);
                        if (fi.LastWriteTime < cutoffDate)
                        {
                            try { fi.Delete(); } catch { }
                        }
                    }
                }
                catch { }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainForm] ⚠️ Errore pulizia log: {ex.Message}");
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Se la chiusura viene da Application.Exit() (già confermata da MenuExit), non chiedere di nuovo
            if (e.CloseReason == CloseReason.ApplicationExitCall)
                return;

            var result = MessageBox.Show(
                LanguageManager.GetString("MainForm.ConfirmExit", "Are you sure you want to exit AirDirector?"),
                LanguageManager.GetString("MainForm.ConfirmExitTitle", "Confirm Exit"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                e.Cancel = true;
            }
            else
            {
                _backupTimer?.Stop();
            }
        }

        // ═══════════════════════════════════════════════════════════
        // METODI HELPER — chiamano il player corretto
        // ═══════════════════════════════════════════════════════════
        private void PlayerSetAutoMode(bool auto)
        {
            if (_isRadioTVMode)
                playerControlVideo?.SetAutoMode(auto);
            else
                playerControl?.SetAutoMode(auto);
        }

        private void PlayerSetAutoStartPending(bool pending)
        {
            if (_isRadioTVMode)
                playerControlVideo?.SetAutoStartPending(pending);
            else
                playerControl?.SetAutoStartPending(pending);
        }

        private void PlayerNotifyQueueItemsAvailable()
        {
            if (_isRadioTVMode)
                playerControlVideo?.NotifyQueueItemsAvailable();
            else
                playerControl?.NotifyQueueItemsAvailable();
        }

        private void PlayerSetManualMode()
        {
            if (_isRadioTVMode)
                playerControlVideo?.SetManualMode();
            else
                playerControl?.SetManualMode();
        }

        private void PlayerLoadTrack(string filePath, string artist, string title, TimeSpan intro,
            int markerIN, int markerINTRO, int markerMIX, int markerOUT, string itemType)
        {
            if (_isRadioTVMode)
                playerControlVideo?.LoadTrack(filePath, artist, title, intro, markerIN, markerINTRO, markerMIX, markerOUT, itemType);
            else
                playerControl?.LoadTrack(filePath, artist, title, intro, markerIN, markerINTRO, markerMIX, markerOUT, itemType);
        }

        private void PlayerPlay()
        {
            if (_isRadioTVMode)
                playerControlVideo?.Play();
            else
                playerControl?.Play();
        }

        // ═══════════════════════════════════════════════════════════
        // LAYOUT — crea il player corretto in base alla modalità
        // ═══════════════════════════════════════════════════════════
        private void InitializeMainLayout()
        {
            this.Text = LanguageManager.GetString("MainForm.Title", "AirDirector - Professional Playout");
            this.Size = new Size(1600, 1000);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = AppTheme.BgLight;
            this.WindowState = FormWindowState.Maximized;
            this.MinimumSize = new Size(1430, 700);

            SplitContainer splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 0,
                BackColor = AppTheme.BorderLight,
                SplitterWidth = 5
            };
            this.Controls.Add(splitContainer);
            splitContainer.SplitterDistance = this.ClientSize.Width / 2;

            Panel playlistPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                Padding = new Padding(0)
            };
            splitContainer.Panel1.Controls.Add(playlistPanel);

            playlistQueue = new PlaylistQueueControl
            {
                Name = "playlistQueue",
                Dock = DockStyle.Fill
            };
            playlistPanel.Controls.Add(playlistQueue);

            Panel playlistHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 45,
                BackColor = AppTheme.BgDark,
                Padding = new Padding(5)
            };
            playlistPanel.Controls.Add(playlistHeader);

            lblQueueTitle = new Label
            {
                Text = LanguageManager.GetString("MainForm.Playlist", "PLAYLIST:"),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(10, 14),
                AutoSize = true
            };
            playlistHeader.Controls.Add(lblQueueTitle);

            lblQueueCount = new Label
            {
                Name = "lblQueueCount",
                Text = "0",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = AppTheme.LEDGreen,
                Location = new Point(95, 14),
                AutoSize = true
            };
            playlistHeader.Controls.Add(lblQueueCount);

            lblClockLabel = new Label
            {
                Text = LanguageManager.GetString("MainForm.Clock", "Clock:"),
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = Color.LightGray,
                Location = new Point(140, 14),
                AutoSize = true
            };
            playlistHeader.Controls.Add(lblClockLabel);

            lblCurrentClock = new Label
            {
                Name = "lblCurrentClock",
                Text = "-",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.Yellow,
                Location = new Point(195, 14),
                AutoSize = true,
                MaximumSize = new Size(200, 0)
            };
            playlistHeader.Controls.Add(lblCurrentClock);

            btnSelectClock = new Button
            {
                Name = "btnSelectClock",
                Text = "🕐 " + LanguageManager.GetString("MainForm.SelectClock", "Seleziona Clock"),
                Size = new Size(130, 35),
                BackColor = Color.FromArgb(138, 43, 226),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Location = new Point(420, 5),
                Cursor = Cursors.Hand
            };
            btnSelectClock.FlatAppearance.BorderSize = 0;
            btnSelectClock.Click += BtnSelectClock_Click;
            playlistHeader.Controls.Add(btnSelectClock);

            btnReloadSchedules = new Button
            {
                Name = "btnReloadSchedules",
                Text = "🔄 " + LanguageManager.GetString("MainForm.ReloadSchedules", "Aggiorna Schedulazioni"),
                Size = new Size(180, 35),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnReloadSchedules.FlatAppearance.BorderSize = 0;
            btnReloadSchedules.Click += BtnReloadSchedules_Click;
            playlistHeader.Controls.Add(btnReloadSchedules);

            btnClearQueue = new Button
            {
                Name = "btnClearQueue",
                Text = LanguageManager.GetString("MainForm.ClearQueue", "Svuota"),
                Size = new Size(90, 35),
                BackColor = AppTheme.Danger,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClearQueue.FlatAppearance.BorderSize = 0;
            btnClearQueue.Click += (s, e) =>
            {
                if (playlistQueue != null && playlistQueue.GetItemCount() > 0)
                {
                    var result = MessageBox.Show(
                        LanguageManager.GetString("MainForm.ConfirmClearQueue", "Vuoi svuotare la playlist queue?"),
                        LanguageManager.GetString("Common.Confirm", "Conferma"),
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);
                    if (result == DialogResult.Yes)
                    {
                        playlistQueue.Clear();
                        UpdateStatus(LanguageManager.GetString("MainForm.QueueCleared", "Playlist queue svuotata"));
                    }
                }
            };
            playlistHeader.Controls.Add(btnClearQueue);

            playlistHeader.Resize += (s, e) => RepositionPlaylistHeaderButtons(playlistHeader, btnSelectClock, btnReloadSchedules, btnClearQueue);
            RepositionPlaylistHeaderButtons(playlistHeader, btnSelectClock, btnReloadSchedules, btnClearQueue);

            Panel tabPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Surface,
                Padding = new Padding(0)
            };
            splitContainer.Panel2.Controls.Add(tabPanel);

            CreateTabControl(tabPanel);

            Panel playerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 180,
                BackColor = AppTheme.BgDark,
                Padding = new Padding(5)
            };
            this.Controls.Add(playerPanel);

            // ═══════════════════════════════════════════════════════
            // SCELTA PLAYER IN BASE ALLA MODALITÀ
            // ═══════════════════════════════════════════════════════
            if (_isRadioTVMode)
            {
                Console.WriteLine("[MainForm] 📺 Modalità RadioTV → PlayerControlVideo");

                playerControlVideo = new PlayerControlVideo
                {
                    Name = "playerControl",
                    Dock = DockStyle.Fill
                };
                playerControlVideo.SetPlaylistQueue(playlistQueue);
                playerControlVideo.PlayRequested += (s, ev) => UpdateStatus(LanguageManager.GetString("MainForm.PlayRequested", "Play richiesto"));
                playerControlVideo.PauseRequested += (s, ev) => UpdateStatus(LanguageManager.GetString("MainForm.Pause", "Pausa"));
                playerControlVideo.StopRequested += (s, ev) => UpdateStatus(LanguageManager.GetString("MainForm.Stop", "Stop"));
                playerControlVideo.NextRequested += (s, ev) => UpdateStatus(LanguageManager.GetString("MainForm.NextRequested", "Next richiesto"));
                playerControlVideo.AutoModeChanged += (s, isAuto) => UpdateStatus(isAuto ?
                    LanguageManager.GetString("MainForm.ModeAuto", "Modalità AUTO") :
                    LanguageManager.GetString("MainForm.ModeManual", "Modalità MANUAL"));
                playerControlVideo.TrackEndedInManualMode += PlayerControl_TrackEndedInManualMode;

                playerPanel.Controls.Add(playerControlVideo);
            }
            else
            {
                Console.WriteLine("[MainForm] 📻 Modalità Radio → PlayerControl");

                playerControl = new PlayerControl
                {
                    Name = "playerControl",
                    Dock = DockStyle.Fill
                };
                playerControl.SetPlaylistQueue(playlistQueue);
                playerControl.PlayRequested += (s, ev) => UpdateStatus(LanguageManager.GetString("MainForm.PlayRequested", "Play richiesto"));
                playerControl.PauseRequested += (s, ev) => UpdateStatus(LanguageManager.GetString("MainForm.Pause", "Pausa"));
                playerControl.StopRequested += (s, ev) => UpdateStatus(LanguageManager.GetString("MainForm.Stop", "Stop"));
                playerControl.NextRequested += (s, ev) => UpdateStatus(LanguageManager.GetString("MainForm.NextRequested", "Next richiesto"));
                playerControl.AutoModeChanged += (s, isAuto) => UpdateStatus(isAuto ?
                    LanguageManager.GetString("MainForm.ModeAuto", "Modalità AUTO") :
                    LanguageManager.GetString("MainForm.ModeManual", "Modalità MANUAL"));
                playerControl.TrackEndedInManualMode += PlayerControl_TrackEndedInManualMode;

                playerPanel.Controls.Add(playerControl);
            }

            CreateMenuBar();
            CreateStatusBar();
        }

        // ═══════════════════════════════════════════════════════════
        // EVENTS — usano i metodi helper
        // ═══════════════════════════════════════════════════════════

        private void MainForm_Load(object sender, EventArgs e)
        {
            if (LicenseManager.IsDemoMode())
                UpdateStatus(LanguageManager.GetString("MainForm.DemoActive", "Modalità Demo attiva - Funzionalità limitate"));
            else
            {
                // Verifica periodica della licenza all'avvio
                if (!LicenseManager.PeriodicCheck(out string checkMsg) && !string.IsNullOrEmpty(checkMsg))
                {
                    UpdateLicenseStatus();
                    MessageBox.Show(checkMsg, LanguageManager.GetString("License_Title", "Licenza"),
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                UpdateStatus(LanguageManager.GetString("MainForm.Ready", "AirDirector pronto - Licenza attiva"));
            }

            playlistQueue.QueueReady += PlaylistQueueControl_QueueReady;
            playlistQueue.QueueCountChanged += PlaylistQueue_QueueCountChanged;
            playlistQueue.PreviewRequested += PlaylistQueue_PreviewRequested;
            playlistQueue.ClockChanged += PlaylistQueue_ClockChanged;
            playlistQueue.ReportUpdated += PlaylistQueue_ReportUpdated;
            playlistQueue.ItemsAdded += PlaylistQueue_ItemsAdded;

            if (ConfigurationControl.GetAutoStartMode())
            {
                UpdateStatus(LanguageManager.GetString("MainForm.AutoStartGenerating", "AutoStart: Generazione playlist in corso..."));
                PlayerSetAutoStartPending(true);
                playlistQueue.GenerateInitialPlaylist();
            }
            else
            {
                UpdateStatus(LanguageManager.GetString("MainForm.ManualMode", "Player in modalità Manual"));
                PlayerSetManualMode();
            }
        }

        private void PlaylistQueueControl_QueueReady(object sender, int count)
        {
            UpdateStatus(string.Format(LanguageManager.GetString("MainForm.PlaylistReady", "Playlist pronta: {0} brani caricati"), count));

            PlayerSetAutoMode(true);

            var items = playlistQueue.GetAllItems();
            if (items.Count > 0)
            {
                var firstItem = items[0];

                UpdateStatus(string.Format(LanguageManager.GetString("MainForm.NowPlaying", "In riproduzione: {0} - {1}"), firstItem.Artist, firstItem.Title));

                PlayerLoadTrack(
                    firstItem.FilePath,
                    firstItem.Artist,
                    firstItem.Title,
                    firstItem.Intro,
                    firstItem.MarkerIN,
                    firstItem.MarkerINTRO,
                    firstItem.MarkerMIX,
                    firstItem.MarkerOUT,
                    firstItem.ItemType
                );

                PlayerPlay();
                playlistQueue.SetCurrentPlaying(0);
                PlayerSetAutoStartPending(false);
            }
            else
            {
                // Coda ancora vuota: attiva _autoStartPending così ItemsAdded farà partire il player
                PlayerSetAutoStartPending(true);
            }
        }

        private void PlaylistQueue_ItemsAdded(object sender, EventArgs e)
        {
            try
            {
                bool isPlaying = _isRadioTVMode
                    ? (playerControlVideo?.IsPlaying ?? false)
                    : (playerControl?.IsPlaying ?? false);

                if (!isPlaying)
                    PlayerNotifyQueueItemsAvailable();
            }
            catch { }
        }

        private void PlayerControl_TrackEndedInManualMode(object sender, EventArgs e)
        {
            try
            {
                playlistQueue.RemoveFinishedTrackInManualMode();
                UpdateStatus(LanguageManager.GetString("MainForm.TrackFinishedManual", "Brano finito - Player in stop (modalità MANUALE)"));
            }
            catch { }
        }

        // ═══════════════════════════════════════════════════════════
        // DISPOSE
        // ═══════════════════════════════════════════════════════════
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                LanguageManager.SaveMissingKeysToFile();
                LanguageManager.LanguageChanged -= OnLanguageChanged;

                _backupTimer?.Stop();
                _backupTimer?.Dispose();
                _fileWatcher?.Dispose();

                // Dispose del player attivo
                playerControl?.Dispose();
                playerControlVideo?.Dispose();
            }
            base.Dispose(disposing);
        }

        // ═══════════════════════════════════════════════════════════
        // TUTTO IL RESTO — invariato (copia dal tuo file originale)
        // BtnSelectClock_Click, BtnReloadSchedules_Click, 
        // CreateMenuBar, CreateTabControl, CreateStatusBar,
        // tutti i menu click, playlist events, ecc.
        // ═══════════════════════════════════════════════════════════

        private void OnLanguageChanged(object sender, EventArgs e) { ApplyLanguage(); }

        private void ApplyLanguage()
        {
            this.Text = LanguageManager.GetString("MainForm.Title", "AirDirector - Professional Playout");
            if (lblQueueTitle != null) lblQueueTitle.Text = LanguageManager.GetString("MainForm.Playlist", "PLAYLIST:");
            if (lblClockLabel != null) lblClockLabel.Text = LanguageManager.GetString("MainForm.Clock", "Clock:");
            if (btnSelectClock != null) btnSelectClock.Text = "🕐 " + LanguageManager.GetString("MainForm.SelectClock", "Seleziona Clock");
            if (btnReloadSchedules != null) btnReloadSchedules.Text = "🔄 " + LanguageManager.GetString("MainForm.ReloadSchedules", "Aggiorna Schedulazioni");
            if (btnClearQueue != null) btnClearQueue.Text = LanguageManager.GetString("MainForm.ClearQueue", "Svuota");
            UpdateMenuBar();
            UpdateTabPages();
        }

        private void UpdateMenuBar()
        {
            if (menuStrip == null || menuStrip.Items.Count < 3) return;
            if (menuStrip.Items[0] is ToolStripMenuItem menuFile) { menuFile.Text = LanguageManager.GetString("MainForm.MenuFile", "File"); if (menuFile.DropDownItems.Count > 0) menuFile.DropDownItems[0].Text = LanguageManager.GetString("MainForm.MenuExit", "Esci"); }
            if (menuStrip.Items[1] is ToolStripMenuItem menuTools) { menuTools.Text = LanguageManager.GetString("MainForm.MenuTools", "Strumenti"); if (menuTools.DropDownItems.Count > 0) { menuTools.DropDownItems[0].Text = LanguageManager.GetString("MainForm.MenuSettings", "Impostazioni"); if (menuTools.DropDownItems.Count > 2) menuTools.DropDownItems[2].Text = "💾 " + LanguageManager.GetString("MainForm.MenuBackup", "Backup Manuale Database"); if (menuTools.DropDownItems.Count > 4) menuTools.DropDownItems[4].Text = LanguageManager.GetString("MainForm.MenuLicense", "Gestione Licenza"); } }
            if (menuStrip.Items[2] is ToolStripMenuItem menuHelp) { menuHelp.Text = LanguageManager.GetString("MainForm.MenuHelp", "Aiuto"); if (menuHelp.DropDownItems.Count > 0) menuHelp.DropDownItems[0].Text = LanguageManager.GetString("MainForm.MenuAbout", "Informazioni"); }
        }

        private void UpdateTabPages()
        {
            if (tabControl == null || tabControl.TabPages.Count == 0) return;
            int idx = 0;
            if (tabControl.TabPages.Count > idx) tabControl.TabPages[idx++].Text = "📊 " + LanguageManager.GetString("MainForm.TabOverview", "Overview");
            if (tabControl.TabPages.Count > idx) tabControl.TabPages[idx++].Text = "🎵 " + LanguageManager.GetString("MainForm.TabMusic", "Music");
            if (tabControl.TabPages.Count > idx) tabControl.TabPages[idx++].Text = "🎬 " + LanguageManager.GetString("MainForm.TabClips", "Clips");
            if (tabControl.TabPages.Count > idx) tabControl.TabPages[idx++].Text = "📅 " + LanguageManager.GetString("MainForm.TabSchedule", "Schedule");
            if (tabControl.TabPages.Count > idx) tabControl.TabPages[idx++].Text = "🕐 " + LanguageManager.GetString("MainForm.TabClock", "Clock");
            if (tabControl.TabPages.Count > idx) tabControl.TabPages[idx++].Text = "🎵 " + LanguageManager.GetString("MainForm.TabPalette", "Palette");
            if (tabControl.TabPages.Count > idx) tabControl.TabPages[idx++].Text = "📥 " + LanguageManager.GetString("MainForm.TabDownload", "Download");
            if (tabControl.TabPages.Count > idx) tabControl.TabPages[idx++].Text = "📡 " + LanguageManager.GetString("MainForm.TabEncoders", "Encoders");
            if (tabControl.TabPages.Count > idx) tabControl.TabPages[idx++].Text = "🎙️ " + LanguageManager.GetString("MainForm.TabRecorders", "Recorders");
            if (tabControl.TabPages.Count > idx) tabControl.TabPages[idx++].Text = "📊 " + LanguageManager.GetString("MainForm.TabReport", "Report");
            for (int i = idx; i < tabControl.TabPages.Count; i++)
            {
                if (tabControl.TabPages[i].Text.Contains("WhatsApp")) tabControl.TabPages[i].Text = "💬 WhatsApp";
                else if (tabControl.TabPages[i].Text.Contains("Config")) tabControl.TabPages[i].Text = "⚙️ " + LanguageManager.GetString("MainForm.TabConfig", "Config");
            }
        }

        private void InitializeBackupTimer() { _backupTimer = new System.Windows.Forms.Timer(); _backupTimer.Interval = 60000; _backupTimer.Tick += BackupTimer_Tick; _backupTimer.Start(); }

        private void BackupTimer_Tick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            string backupTime = ConfigurationControl.GetBackupTime();
            if (!TimeSpan.TryParse(backupTime, out TimeSpan targetTime)) targetTime = new TimeSpan(1, 0, 0);
            if (now.Hour == targetTime.Hours && now.Minute == targetTime.Minutes) { if (_lastBackupDate.Date != now.Date) { _lastBackupDate = now.Date; PerformDailyBackup(); } }
        }

        private void PerformDailyBackup()
        {
            try
            {
                string backupPath = ConfigurationControl.GetBackupPath();
                if (!Directory.Exists(backupPath)) Directory.CreateDirectory(backupPath);
                string backupFileName = $"Database_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.zip";
                string backupFilePath = Path.Combine(backupPath, backupFileName);
                UpdateStatus(LanguageManager.GetString("MainForm.BackupInProgress", "Backup automatico in corso..."));
                if (File.Exists(backupFilePath)) File.Delete(backupFilePath);
                string databasePath = DbcManager.GetDatabasePath();
                if (!Directory.Exists(databasePath)) return;
                ZipFile.CreateFromDirectory(databasePath, backupFilePath, CompressionLevel.Optimal, false);
                if (File.Exists(backupFilePath)) { UpdateStatus(string.Format(LanguageManager.GetString("MainForm.BackupCompleted", "Backup automatico completato: {0}"), backupFileName)); CleanOldBackups(backupPath); }
            }
            catch (Exception ex) { MessageBox.Show(string.Format(LanguageManager.GetString("MainForm.BackupError", "Errore durante il backup:\n{0}"), ex.Message), LanguageManager.GetString("Common.Error", "Errore Backup"), MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void CleanOldBackups(string backupPath) { try { var backupFiles = Directory.GetFiles(backupPath, "Database_*.zip").Select(f => new FileInfo(f)).Where(f => f.CreationTime < DateTime.Now.AddDays(-30)).ToList(); foreach (var oldBackup in backupFiles) oldBackup.Delete(); } catch { } }

        private void RepositionPlaylistHeaderButtons(Panel playlistHeader, Button btnSelectClock, Button btnReloadSchedules, Button btnClearQueue)
        {
            const int MARGIN = 10; const int MIN_SPACING = 5;
            int headerWidth = playlistHeader.Width;
            int clearQueueX = headerWidth - btnClearQueue.Width - MARGIN;
            int reloadSchedulesX = clearQueueX - btnReloadSchedules.Width - MARGIN;
            btnClearQueue.Location = new Point(clearQueueX, 5);
            btnReloadSchedules.Location = new Point(reloadSchedulesX, 5);
            int availableSpaceForSelectClock = reloadSchedulesX - 420 - MIN_SPACING;
            if (availableSpaceForSelectClock > btnSelectClock.Width) btnSelectClock.Location = new Point(420, 5);
            else { int selectClockX = reloadSchedulesX - btnSelectClock.Width - MARGIN; selectClockX = Math.Max(selectClockX, MARGIN); btnSelectClock.Location = new Point(selectClockX, 5); }
        }

        private void BtnReloadSchedules_Click(object sender, EventArgs e)
        {
            try { UpdateStatus("🔄 Ricaricamento schedulazioni e pubblicità in corso..."); playlistQueue.ReloadTodaySchedules(); playlistQueue.ReloadAdvSchedules(); UpdateStatus("✅ Schedulazioni e pubblicità ricaricate con successo!"); }
            catch (Exception ex) { MessageBox.Show($"Errore durante il ricaricamento:\n{ex.Message}", "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void BtnSelectClock_Click(object sender, EventArgs e)
        {
            try
            {
                var clocks = DbcManager.LoadFromCsv<ClockEntry>("Clocks.dbc");
                if (clocks.Count == 0) { MessageBox.Show(LanguageManager.GetString("MainForm.NoClocksAvailable", "Nessun clock disponibile!\n\nCrea prima un clock nella sezione Clock."), LanguageManager.GetString("Common.Warning", "Attenzione"), MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

                Form selectForm = new Form { Text = LanguageManager.GetString("MainForm.ClockSelection", "Selezione Clock"), Size = new Size(550, 600), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, BackColor = Color.FromArgb(25, 25, 25) };
                Panel headerPanel = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(138, 43, 226) };
                selectForm.Controls.Add(headerPanel);
                headerPanel.Controls.Add(new Label { Text = "🕐 " + LanguageManager.GetString("MainForm.ClockSelectionTitle", "SELEZIONE CLOCK"), Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.White, Location = new Point(20, 15), AutoSize = true });
                headerPanel.Controls.Add(new Label { Text = LanguageManager.GetString("MainForm.ClockSelectionSubtitle", "Seleziona un clock da attivare immediatamente"), Font = new Font("Segoe UI", 10, FontStyle.Regular), ForeColor = Color.FromArgb(220, 220, 220), Location = new Point(20, 45), AutoSize = true });

                Panel contentPanel = new Panel { Location = new Point(20, 100), Size = new Size(510, 400), BackColor = Color.FromArgb(35, 35, 35), BorderStyle = BorderStyle.FixedSingle };
                selectForm.Controls.Add(contentPanel);
                contentPanel.Controls.Add(new Label { Text = string.Format(LanguageManager.GetString("MainForm.CurrentlyActiveClock", "Clock attualmente attivo: {0}"), playlistQueue.GetCurrentClockName()), Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.Yellow, Location = new Point(10, 10), Size = new Size(490, 20) });

                ListBox listClocks = new ListBox { Location = new Point(10, 40), Size = new Size(490, 350), Font = new Font("Segoe UI", 11), BackColor = Color.FromArgb(45, 45, 45), ForeColor = Color.White, BorderStyle = BorderStyle.None, ItemHeight = 30, DrawMode = DrawMode.OwnerDrawFixed };
                listClocks.DrawItem += (s, ev) =>
                {
                    if (ev.Index < 0) return;
                    bool isSelected = (ev.State & DrawItemState.Selected) == DrawItemState.Selected;
                    using (SolidBrush bgBrush = new SolidBrush(isSelected ? Color.FromArgb(138, 43, 226) : Color.FromArgb(45, 45, 45))) ev.Graphics.FillRectangle(bgBrush, ev.Bounds);
                    string itemText = listClocks.Items[ev.Index].ToString();
                    string displayText = itemText.Replace(" (Default)", "").Replace(" " + LanguageManager.GetString("MainForm.Active", "(Attivo)"), "").Trim();
                    using (Font font = new Font("Segoe UI", 11, FontStyle.Bold)) using (SolidBrush textBrush = new SolidBrush(Color.White)) ev.Graphics.DrawString(displayText, font, textBrush, ev.Bounds.X + 10, ev.Bounds.Y + 5);
                    ev.DrawFocusRectangle();
                };

                string activeText = LanguageManager.GetString("MainForm.Active", "(Attivo)");
                foreach (var clock in clocks) { string displayText = clock.ClockName; if (clock.IsDefault == 1) displayText += " (Default)"; if (clock.ClockName == playlistQueue.GetCurrentClockName()) displayText += " " + activeText; listClocks.Items.Add(displayText); }
                string currentClock = playlistQueue.GetCurrentClockName();
                if (!string.IsNullOrEmpty(currentClock)) { for (int i = 0; i < listClocks.Items.Count; i++) { if (listClocks.Items[i].ToString().Contains(currentClock)) { listClocks.SelectedIndex = i; break; } } }
                contentPanel.Controls.Add(listClocks);

                Panel buttonPanel = new Panel { Location = new Point(20, 510), Size = new Size(510, 50), BackColor = Color.FromArgb(25, 25, 25) };
                selectForm.Controls.Add(buttonPanel);
                Button btnOk = new Button { Text = "✓ " + LanguageManager.GetString("MainForm.ActivateClock", "ATTIVA CLOCK"), Size = new Size(240, 45), Location = new Point(0, 0), BackColor = Color.FromArgb(0, 180, 0), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 11, FontStyle.Bold), Cursor = Cursors.Hand };
                btnOk.FlatAppearance.BorderSize = 0;
                btnOk.Click += (s, ev) =>
                {
                    if (listClocks.SelectedIndex >= 0)
                    {
                        string selectedText = listClocks.SelectedItem.ToString();
                        string clockName = selectedText.Replace(" (Default)", "").Replace(" " + activeText, "").Trim();
                        if (clockName == playlistQueue.GetCurrentClockName()) { MessageBox.Show(LanguageManager.GetString("MainForm.ClockAlreadyActive", "Questo clock è già attivo!"), LanguageManager.GetString("Common.Info", "Informazione"), MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
                        var result = MessageBox.Show(string.Format(LanguageManager.GetString("MainForm.ConfirmActivateClock", "Vuoi attivare il clock '{0}'?\n\nLa coda verrà svuotata e rigenerata con il nuovo clock."), clockName), LanguageManager.GetString("MainForm.ConfirmClockChange", "Conferma Cambio Clock"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (result == DialogResult.Yes) { playlistQueue.ChangeClockManually(clockName); UpdateStatus(string.Format(LanguageManager.GetString("MainForm.ClockActivated", "Clock '{0}' attivato manualmente"), clockName)); selectForm.DialogResult = DialogResult.OK; selectForm.Close(); }
                    }
                    else { MessageBox.Show(LanguageManager.GetString("MainForm.SelectClockFromList", "Seleziona un clock dalla lista!"), LanguageManager.GetString("Common.Warning", "Attenzione"), MessageBoxButtons.OK, MessageBoxIcon.Warning); }
                };
                buttonPanel.Controls.Add(btnOk);
                Button btnCancel = new Button { Text = "✕ " + LanguageManager.GetString("Common.Cancel", "ANNULLA"), Size = new Size(240, 45), Location = new Point(270, 0), BackColor = Color.FromArgb(80, 80, 80), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 11, FontStyle.Bold), Cursor = Cursors.Hand };
                btnCancel.FlatAppearance.BorderSize = 0;
                btnCancel.Click += (s, ev) => { selectForm.DialogResult = DialogResult.Cancel; selectForm.Close(); };
                buttonPanel.Controls.Add(btnCancel);
                selectForm.ShowDialog();
            }
            catch (Exception ex) { MessageBox.Show(string.Format(LanguageManager.GetString("MainForm.ClockSelectionError", "Errore selezione clock:\n{0}"), ex.Message), LanguageManager.GetString("Common.Error", "Errore"), MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void CreateMenuBar()
        {
            menuStrip = new MenuStrip { BackColor = AppTheme.Surface, Font = new Font("Segoe UI", 10), Dock = DockStyle.Top };
            ToolStripMenuItem menuFile = new ToolStripMenuItem(LanguageManager.GetString("MainForm.MenuFile", "File"));
            menuFile.DropDownItems.Add(LanguageManager.GetString("MainForm.MenuExit", "Esci"), null, MenuExit_Click);
            menuStrip.Items.Add(menuFile);
            ToolStripMenuItem menuTools = new ToolStripMenuItem(LanguageManager.GetString("MainForm.MenuTools", "Strumenti"));
            menuTools.DropDownItems.Add(LanguageManager.GetString("MainForm.MenuSettings", "Impostazioni"), null, MenuSettings_Click);
            menuTools.DropDownItems.Add(new ToolStripSeparator());
            menuTools.DropDownItems.Add("💾 " + LanguageManager.GetString("MainForm.MenuBackup", "Backup Manuale Database"), null, MenuBackup_Click);
            menuTools.DropDownItems.Add(new ToolStripSeparator());
            menuTools.DropDownItems.Add(LanguageManager.GetString("MainForm.MenuLicense", "Gestione Licenza"), null, MenuLicense_Click);
            menuStrip.Items.Add(menuTools);
            ToolStripMenuItem menuHelp = new ToolStripMenuItem(LanguageManager.GetString("MainForm.MenuHelp", "Aiuto"));
            menuHelp.DropDownItems.Add(LanguageManager.GetString("MainForm.MenuAbout", "Informazioni"), null, MenuAbout_Click);
            menuStrip.Items.Add(menuHelp);
            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);
        }

        private void MenuBackup_Click(object sender, EventArgs e) { var result = MessageBox.Show(LanguageManager.GetString("MainForm.ConfirmBackup", "Vuoi creare un backup manuale del database?"), LanguageManager.GetString("MainForm.BackupDatabase", "Backup Database"), MessageBoxButtons.YesNo, MessageBoxIcon.Question); if (result == DialogResult.Yes) PerformDailyBackup(); }

        private void CreateTabControl(Panel parent)
        {
            tabControl = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9, FontStyle.Bold), Padding = new Point(8, 4), Appearance = TabAppearance.Normal, SizeMode = TabSizeMode.Fixed, ItemSize = new Size(140, 28), DrawMode = TabDrawMode.Normal, Multiline = true, HotTrack = true };
            TabPage tabOverview = new TabPage("📊 " + LanguageManager.GetString("MainForm.TabOverview", "Overview")); tabOverview.BackColor = AppTheme.BgLight; overviewControl = new OverviewControl { Dock = DockStyle.Fill }; overviewControl.SetReferences(playlistQueue, encodersControl, recordersControl); tabOverview.Controls.Add(overviewControl); tabControl.TabPages.Add(tabOverview);
            TabPage tabMusic = new TabPage("🎵 " + LanguageManager.GetString("MainForm.TabMusic", "Music")); tabMusic.BackColor = AppTheme.BgLight; musicArchiveControl = new ArchiveControl("Music") { Dock = DockStyle.Fill }; musicArchiveControl.StatusChanged += (s, msg) => UpdateStatus(msg); tabMusic.Controls.Add(musicArchiveControl); tabControl.TabPages.Add(tabMusic);
            TabPage tabClips = new TabPage("🎬 " + LanguageManager.GetString("MainForm.TabClips", "Clips")); tabClips.BackColor = AppTheme.BgLight; clipsArchiveControl = new ArchiveControl("Clips") { Dock = DockStyle.Fill }; clipsArchiveControl.StatusChanged += (s, msg) => UpdateStatus(msg); tabClips.Controls.Add(clipsArchiveControl); tabControl.TabPages.Add(tabClips);
            TabPage tabSchedules = new TabPage("📅 " + LanguageManager.GetString("MainForm.TabSchedule", "Schedule")); tabSchedules.BackColor = AppTheme.BgLight; schedulesControl = new SchedulesControl { Dock = DockStyle.Fill }; schedulesControl.StatusChanged += (s, msg) => UpdateStatus(msg); tabSchedules.Controls.Add(schedulesControl); tabControl.TabPages.Add(tabSchedules);
            TabPage tabClock = new TabPage("🕐 " + LanguageManager.GetString("MainForm.TabClock", "Clock")); tabClock.BackColor = AppTheme.BgLight; clocksControl = new ClocksControl { Dock = DockStyle.Fill }; clocksControl.StatusChanged += (s, msg) => UpdateStatus(msg); tabClock.Controls.Add(clocksControl); tabControl.TabPages.Add(tabClock);
            TabPage tabPalette = new TabPage("🎵 " + LanguageManager.GetString("MainForm.TabPalette", "Palette")); tabPalette.BackColor = AppTheme.BgLight; PaletteControl paletteControl = new PaletteControl { Dock = DockStyle.Fill }; tabPalette.Controls.Add(paletteControl); tabControl.TabPages.Add(tabPalette);
            TabPage tabDownload = new TabPage("📥 " + LanguageManager.GetString("MainForm.TabDownload", "Download")); tabDownload.BackColor = AppTheme.BgLight; DownloadControl downloadControl = new DownloadControl { Dock = DockStyle.Fill }; tabDownload.Controls.Add(downloadControl); tabControl.TabPages.Add(tabDownload);
            TabPage tabEncoders = new TabPage("📡 " + LanguageManager.GetString("MainForm.TabEncoders", "Encoders")); tabEncoders.BackColor = AppTheme.BgLight; encodersControl = new EncodersControl { Dock = DockStyle.Fill }; encodersControl.StatusChanged += (s, msg) => UpdateStatus(msg); tabEncoders.Controls.Add(encodersControl); tabControl.TabPages.Add(tabEncoders);
            TabPage tabRecorders = new TabPage("🎙️ " + LanguageManager.GetString("MainForm.TabRecorders", "Recorders")); tabRecorders.BackColor = AppTheme.BgLight; recordersControl = new RecordersControl { Dock = DockStyle.Fill }; recordersControl.StatusChanged += (s, msg) => UpdateStatus(msg); tabRecorders.Controls.Add(recordersControl); tabControl.TabPages.Add(tabRecorders);
            TabPage tabReport = new TabPage("📊 " + LanguageManager.GetString("MainForm.TabReport", "Report")); tabReport.BackColor = AppTheme.BgLight; reportControl = new ReportControl { Dock = DockStyle.Fill }; tabReport.Controls.Add(reportControl); tabControl.TabPages.Add(tabReport);
            if (ConfigurationControl.GetShowWhatsApp()) { TabPage tabWhatsApp = new TabPage("💬 WhatsApp"); tabWhatsApp.BackColor = AppTheme.BgLight; whatsAppControl = new WhatsAppControl { Dock = DockStyle.Fill }; tabWhatsApp.Controls.Add(whatsAppControl); tabControl.TabPages.Add(tabWhatsApp); }
            TabPage tabConfig = new TabPage("⚙️ " + LanguageManager.GetString("MainForm.TabConfig", "Config")); tabConfig.BackColor = AppTheme.BgLight; configurationControl = new ConfigurationControl { Dock = DockStyle.Fill }; configurationControl.ConfigurationChanged += (s, ev) => UpdateStatus(LanguageManager.GetString("MainForm.ConfigSaved", "Configurazione salvata")); tabConfig.Controls.Add(configurationControl); tabControl.TabPages.Add(tabConfig);
            parent.Controls.Add(tabControl);
            tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;
            tabControl.SelectedIndex = 0;
        }

        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            int configTabIndex = tabControl.TabPages.Count - 1;
            int whatsAppTabIndex = -1;
            for (int i = 0; i < tabControl.TabPages.Count; i++) { if (tabControl.TabPages[i].Text.Contains("WhatsApp")) { whatsAppTabIndex = i; break; } }
            switch (tabControl.SelectedIndex)
            {
                case 0: overviewControl?.Refresh(); UpdateStatus(LanguageManager.GetString("MainForm.StatusOverview", "Overview")); break;
                case 1: musicArchiveControl?.RefreshArchive(); UpdateStatus(LanguageManager.GetString("MainForm.StatusMusic", "Archivio Music")); break;
                case 2: clipsArchiveControl?.RefreshArchive(); UpdateStatus(LanguageManager.GetString("MainForm.StatusClips", "Archivio Clips")); break;
                case 3: schedulesControl?.RefreshSchedules(); UpdateStatus(LanguageManager.GetString("MainForm.StatusSchedules", "Schedulazioni")); break;
                case 4: UpdateStatus(LanguageManager.GetString("MainForm.StatusClock", "Gestione Clock")); break;
                case 5: UpdateStatus(LanguageManager.GetString("MainForm.StatusPalette", "Jingle Palette")); break;
                case 6: UpdateStatus(LanguageManager.GetString("MainForm.StatusDownload", "Download Schedulati")); break;
                case 7: UpdateStatus(LanguageManager.GetString("MainForm.StatusEncoders", "Encoders")); break;
                case 8: UpdateStatus(LanguageManager.GetString("MainForm.StatusRecorders", "Recorders")); break;
                case 9: reportControl?.LoadLast24Hours(); UpdateStatus(LanguageManager.GetString("MainForm.StatusReport", "Report ultime 24 ore")); break;
                default: if (tabControl.SelectedIndex == whatsAppTabIndex && whatsAppTabIndex != -1) UpdateStatus("WhatsApp Web"); else if (tabControl.SelectedIndex == configTabIndex) UpdateStatus(LanguageManager.GetString("MainForm.StatusConfig", "Configurazione")); break;
            }
        }

        private void CreateStatusBar()
        {
            statusStrip = new StatusStrip { BackColor = AppTheme.Surface, Font = new Font("Segoe UI", 9) };
            lblStatus = new ToolStripStatusLabel { Text = LanguageManager.GetString("MainForm.StatusReady", "Pronto"), Spring = true, TextAlign = ContentAlignment.MiddleLeft };
            statusStrip.Items.Add(lblStatus);
            lblLicense = new ToolStripStatusLabel { Text = LanguageManager.GetString("MainForm.DemoMode", "Modalità Demo"), ForeColor = Color.OrangeRed, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            statusStrip.Items.Add(lblLicense);
            this.Controls.Add(statusStrip);
        }

        private void InitializeFileWatcher() { _fileWatcher = new FileWatcherService(); _fileWatcher.FileChanged += (s, fileName) => { BeginInvoke(new Action(() => { UpdateStatus(string.Format(LanguageManager.GetString("MainForm.DatabaseUpdated", "Database aggiornato: {0}"), fileName)); })); }; }
        private void UpdateLicenseStatus() { if (LicenseManager.IsDemoMode()) { lblLicense.Text = LanguageManager.GetString("MainForm.DemoMode", "Modalità Demo"); lblLicense.ForeColor = Color.OrangeRed; } else { var lic = LicenseManager.GetCurrentLicense(); string owner = !string.IsNullOrEmpty(lic.OwnerName) ? lic.OwnerName : lic.SerialKey; lblLicense.Text = owner; lblLicense.ForeColor = Color.Green; } }
        private void UpdateStatus(string message) { if (lblStatus != null) lblStatus.Text = message; }
        private void PlaylistQueue_QueueCountChanged(object sender, int count) { Label lblCount = this.Controls.Find("lblQueueCount", true).FirstOrDefault() as Label; if (lblCount != null) lblCount.Text = count.ToString(); }
        private void PlaylistQueue_ClockChanged(object sender, string clockName) { if (lblCurrentClock != null) lblCurrentClock.Text = clockName; }
        private void PlaylistQueue_ReportUpdated(object sender, EventArgs e) { try { reportControl?.LoadLast24Hours(); } catch { } }

        private void PlaylistQueue_PreviewRequested(object sender, string filePath)
        {
            if (!File.Exists(filePath)) { MessageBox.Show(LanguageManager.GetString("MainForm.FileNotFound", "File non trovato!"), LanguageManager.GetString("Common.Error", "Errore"), MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            string title = Path.GetFileNameWithoutExtension(filePath);
            var items = playlistQueue.GetAllItems();
            var item = items.FirstOrDefault(i => i.FilePath == filePath);
            if (item != null) title = string.IsNullOrEmpty(item.Artist) ? item.Title : $"{item.Artist} - {item.Title}";
            bool isMusic = item != null ? (item.Type == PlaylistItemType.Music) : new[] { ".mp3", ".wav", ".flac", ".aac" }.Contains(Path.GetExtension(filePath).ToLower());
            if (isMusic && musicArchiveControl != null) { tabControl.SelectedIndex = 1; musicArchiveControl.StartPreviewFromExternal(filePath, title); }
            else if (!isMusic && clipsArchiveControl != null) { tabControl.SelectedIndex = 2; clipsArchiveControl.StartPreviewFromExternal(filePath, title); }
        }

        private void MenuExit_Click(object sender, EventArgs e)
        {
            this.Close(); // FormClosing gestirà la conferma
        }
        private void MenuSettings_Click(object sender, EventArgs e) { tabControl.SelectedIndex = tabControl.TabPages.Count - 1; UpdateStatus(LanguageManager.GetString("MainForm.GoToConfig", "Vai alla sezione Configurazione")); }

        private void MenuLicense_Click(object sender, EventArgs e)
        {
            var currentLicense = LicenseManager.GetCurrentLicense();
            if (currentLicense.IsDemoMode)
            {
                LicenseForm licenseForm = new LicenseForm();
                if (licenseForm.ShowDialog() == DialogResult.OK)
                {
                    UpdateLicenseStatus();
                    MessageBox.Show(
                        LanguageManager.GetString("MainForm.LicenseActivatedRestart", "Licenza attivata! Riavvia l'applicazione per applicare le modifiche."),
                        LanguageManager.GetString("Common.Success", "Successo"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            else
            {
                using (var infoForm = new LicenseInfoForm())
                {
                    if (infoForm.ShowDialog(this) == DialogResult.OK && infoForm.LicenseRemoved)
                    {
                        UpdateLicenseStatus();
                        MessageBox.Show(
                            LanguageManager.GetString("MainForm.LicenseRemoved", "Licenza rimossa con successo"),
                            LanguageManager.GetString("Common.Success", "Successo"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                }
            }
        }

        private void MenuAbout_Click(object sender, EventArgs e) { MessageBox.Show(LanguageManager.GetString("MainForm.AboutText", "AirDirector v1.0.0\n\nPlayout Radiofonico e TV Professionale\n\n© 2025 AirDirector\nTutti i diritti riservati."), LanguageManager.GetString("MainForm.AboutTitle", "Informazioni su AirDirector"), MessageBoxButtons.OK, MessageBoxIcon.Information); }
    }

    [Serializable]
    public class DragDropData
    {
        public string EntryType { get; set; }
        public object EntryData { get; set; }
    }
}
