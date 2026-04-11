using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;
using AirDirector.Services.Database;
using AirDirector.Services.Licensing;
using AirDirector.Services.Localization;
using AirDirector.Services;
using System.Linq;
using System.IO;

namespace AirDirector.Controls
{
    public partial class OverviewControl : UserControl
    {
        private System.Windows.Forms.Timer _updateTimer;
        private PlaylistQueueControl _playlistQueue;
        private EncodersControl _encodersControl;
        private RecordersControl _recordersControl;

        // Header
        private Panel headerPanel;
        private PictureBox picLogo;
        private Label lblDateTime;
        private Label lblStationName;
        private Label lblDemoTag;

        // Video Preview (RadioTV mode)
        private Panel pnlVideoPreview;
        private PictureBox picVideoPreview;
        private Label lblVideoPreviewHeader;
        private Label lblVideoStatus;
        private bool _isRadioTVMode = false;

        // NDI Preview Receiver
        private NDIPreviewReceiver _ndiPreviewReceiver;
        private System.Windows.Forms.Timer _previewRetryTimer;
        private string _ndiSourceName = "";

        // Now Playing
        private Panel pnlNowPlaying;
        private Label lblNowPlayingArtist;
        private Label lblNowPlayingTitle;
        private Label lblNowPlayingTime;
        private Label lblNowPlayingHeader;

        // Next Track
        private Panel pnlNextTrack;
        private Label lblNextArtist;
        private Label lblNextTitle;
        private Label lblNextDuration;
        private Label lblNextTrackHeader;

        // Last Played
        private Panel pnlLastPlayed;
        private Label lblLastArtist;
        private Label lblLastTitle;
        private Label lblLastPlayedHeader;

        // Clock Active
        private Panel pnlClockActive;
        private Label lblClockName;
        private Label lblClockActiveHeader;

        // Next Schedule
        private Panel pnlNextSchedule;
        private Label lblNextScheduleInfo;
        private Label lblCountdown;
        private Label lblNextScheduleHeader;

        // Next Ad
        private Panel pnlNextAd;
        private Label lblNextAdInfo;
        private Label lblNextAdCountdown;
        private Label lblNextAdHeader;

        // Encoders
        private Panel pnlEncoders;
        private Label lblEncodersInfo;
        private Label lblEncodersHeader;

        // Recorders
        private Panel pnlRecorders;
        private Label lblRecordersInfo;
        private Label lblRecordersHeader;

        // System Info
        private Panel pnlSystemInfo;
        private Label lblSystemInfo;

        private PlaylistQueueItem _currentMusic = null;
        private PlaylistQueueItem _lastMusic = null;
        private bool _blinkState = false;

        // ADV Cache
        private List<AirDirectorPlaylistItem> _cachedAdvItems = new List<AirDirectorPlaylistItem>();
        private DateTime _advCacheDate = DateTime.MinValue;
        private DateTime _lastAdvReloadTime = DateTime.MinValue;

        public OverviewControl()
        {
            InitializeComponent();
            _isRadioTVMode = ConfigurationControl.IsRadioTVMode();
            InitializeCustomUI();
            ApplyLanguage();
            LoadAdvCache();
            StartTimer();

            if (_isRadioTVMode)
            {
                StartNDIPreview();
            }

            LanguageManager.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            if (lblStationName != null)
                lblStationName.Text = ConfigurationControl.GetStationName();

            if (lblDemoTag != null)
                lblDemoTag.Text = "🔸 " + LanguageManager.GetString("Overview.Demo", "DEMO");

            if (lblVideoPreviewHeader != null)
                lblVideoPreviewHeader.Text = "📺 " + LanguageManager.GetString("Overview.VideoPreview", "VIDEO PREVIEW");

            if (lblNowPlayingHeader != null)
                lblNowPlayingHeader.Text = "🎵 " + LanguageManager.GetString("Overview.OnAir", "IN ONDA");

            if (lblNextTrackHeader != null)
                lblNextTrackHeader.Text = "⏭️ " + LanguageManager.GetString("Overview.Next", "PROSSIMO");

            if (lblLastPlayedHeader != null)
                lblLastPlayedHeader.Text = "⏮️ " + LanguageManager.GetString("Overview.LastPlayed", "APPENA SUONATO");

            if (lblClockActiveHeader != null)
                lblClockActiveHeader.Text = "🕐 " + LanguageManager.GetString("Overview.ActiveClock", "CLOCK ATTIVO");

            if (lblNextScheduleHeader != null)
                lblNextScheduleHeader.Text = "⏰ " + LanguageManager.GetString("Overview.NextSchedule", "PROSSIMA SCHEDULAZIONE");

            if (lblNextAdHeader != null)
                lblNextAdHeader.Text = "📢 " + LanguageManager.GetString("Overview.NextAd", "PROSSIMA PUBBLICITÀ");

            if (lblEncodersHeader != null)
                lblEncodersHeader.Text = "📡 " + LanguageManager.GetString("Overview.Encoders", "ENCODERS");

            if (lblRecordersHeader != null)
                lblRecordersHeader.Text = "🎙️ " + LanguageManager.GetString("Overview.Recorders", "RECORDERS");
        }

        public void SetReferences(PlaylistQueueControl playlistQueue, EncodersControl encoders, RecordersControl recorders)
        {
            _playlistQueue = playlistQueue;
            _encodersControl = encoders;
            _recordersControl = recorders;
        }

        // ═══════════════════════════════════════════════════════════
        // NDI PREVIEW
        // ═══════════════════════════════════════════════════════════

        private void StartNDIPreview()
        {
            try
            {
                _ndiSourceName = ConfigurationControl.GetNDISourceName();

                if (string.IsNullOrEmpty(_ndiSourceName))
                {
                    Console.WriteLine("[OverviewNDI] ⚠️ NDI source name not configured");
                    ShowPreviewPlaceholder(LanguageManager.GetString("Overview.NDINotConfigured", "NDI source not configured"));
                    return;
                }

                Console.WriteLine($"[OverviewNDI] 🎬 Starting NDI preview for:  {_ndiSourceName}");

                _ndiPreviewReceiver = new NDIPreviewReceiver();
                _ndiPreviewReceiver.FrameReceived += OnNDIFrameReceived;

                _previewRetryTimer = new System.Windows.Forms.Timer { Interval = 3000 };
                _previewRetryTimer.Tick += PreviewRetryTimer_Tick;
                _previewRetryTimer.Start();

                ShowPreviewPlaceholder(LanguageManager.GetString("Overview.ConnectingNDI", "Connecting to NDI... ") + $"\n{_ndiSourceName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OverviewNDI] ❌ Error starting preview: {ex.Message}");
                ShowPreviewPlaceholder(LanguageManager.GetString("Overview.NDIError", "NDI Error"));
            }
        }

        private void PreviewRetryTimer_Tick(object sender, EventArgs e)
        {
            if (_ndiPreviewReceiver == null) return;

            if (!_ndiPreviewReceiver.IsConnected)
            {
                Console.WriteLine($"[OverviewNDI] 🔄 Attempting to connect to:  {_ndiSourceName}");
                bool connected = _ndiPreviewReceiver.Start(_ndiSourceName);

                if (connected)
                {
                    Console.WriteLine("[OverviewNDI] ✅ Connected!");
                    UpdateVideoStatus("🟢 " + LanguageManager.GetString("Overview.Connected", "Connected"), Color.FromArgb(0, 200, 0));
                    _previewRetryTimer.Interval = 10000;
                }
                else
                {
                    ShowPreviewPlaceholder(LanguageManager.GetString("Overview.WaitingNDI", "Waiting for NDI... ") + $"\n{_ndiSourceName}");
                    UpdateVideoStatus("🔴 " + LanguageManager.GetString("Overview.Disconnected", "Disconnected"), Color.FromArgb(200, 0, 0));
                }
            }
            else
            {
                UpdateVideoStatus("🟢 " + LanguageManager.GetString("Overview.Connected", "Connected"), Color.FromArgb(0, 200, 0));
            }
        }

        private void OnNDIFrameReceived(Bitmap frame)
        {
            if (picVideoPreview == null || picVideoPreview.IsDisposed || frame == null)
                return;

            try
            {
                if (picVideoPreview.InvokeRequired)
                {
                    picVideoPreview.BeginInvoke(new Action(() => UpdatePreviewImage(frame)));
                }
                else
                {
                    UpdatePreviewImage(frame);
                }
            }
            catch { }
        }

        private void UpdatePreviewImage(Bitmap frame)
        {
            try
            {
                if (picVideoPreview == null || picVideoPreview.IsDisposed)
                    return;

                int previewWidth = picVideoPreview.Width;
                int previewHeight = picVideoPreview.Height;

                if (previewWidth <= 0 || previewHeight <= 0)
                    return;

                Bitmap resized = new Bitmap(previewWidth, previewHeight);
                using (Graphics g = Graphics.FromImage(resized))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;
                    g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
                    g.DrawImage(frame, 0, 0, previewWidth, previewHeight);
                }

                var oldImage = picVideoPreview.Image;
                picVideoPreview.Image = resized;
                oldImage?.Dispose();
            }
            catch { }
        }

        private void ShowPreviewPlaceholder(string message)
        {
            if (picVideoPreview == null || picVideoPreview.IsDisposed)
                return;

            try
            {
                if (picVideoPreview.InvokeRequired)
                {
                    picVideoPreview.BeginInvoke(new Action(() => ShowPreviewPlaceholder(message)));
                    return;
                }

                int w = picVideoPreview.Width > 0 ? picVideoPreview.Width : 400;
                int h = picVideoPreview.Height > 0 ? picVideoPreview.Height : 200;

                Bitmap placeholder = new Bitmap(w, h);
                using (Graphics g = Graphics.FromImage(placeholder))
                {
                    g.Clear(Color.FromArgb(20, 20, 25));

                    using (Font iconFont = new Font("Segoe UI", 28))
                    using (SolidBrush iconBrush = new SolidBrush(Color.FromArgb(50, 50, 60)))
                    {
                        string icon = "📺";
                        SizeF iconSize = g.MeasureString(icon, iconFont);
                        g.DrawString(icon, iconFont, iconBrush, (w - iconSize.Width) / 2, h / 2 - 40);
                    }

                    using (Font msgFont = new Font("Segoe UI", 9))
                    using (SolidBrush msgBrush = new SolidBrush(Color.FromArgb(100, 100, 110)))
                    {
                        StringFormat sf = new StringFormat
                        {
                            Alignment = StringAlignment.Center,
                            LineAlignment = StringAlignment.Center
                        };
                        RectangleF textRect = new RectangleF(10, h / 2, w - 20, 40);
                        g.DrawString(message, msgFont, msgBrush, textRect, sf);
                    }
                }

                var oldImage = picVideoPreview.Image;
                picVideoPreview.Image = placeholder;
                oldImage?.Dispose();
            }
            catch { }
        }

        private void UpdateVideoStatus(string status, Color color)
        {
            if (lblVideoStatus == null || lblVideoStatus.IsDisposed)
                return;

            try
            {
                if (lblVideoStatus.InvokeRequired)
                {
                    lblVideoStatus.BeginInvoke(new Action(() => UpdateVideoStatus(status, color)));
                    return;
                }

                lblVideoStatus.Text = status;
                lblVideoStatus.ForeColor = color;
            }
            catch { }
        }

        // ═══════════════════════════════════════════════════════════
        // UI INITIALIZATION
        // ═══════════════════════════════════════════════════════════

        private void InitializeCustomUI()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(15, 15, 15);
            this.Padding = new Padding(20);

            // Header Panel
            headerPanel = new Panel
            {
                Location = new Point(20, 20),
                Size = new Size(this.Width - 40, 100),
                BackColor = Color.FromArgb(25, 25, 25),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            this.Controls.Add(headerPanel);

            picLogo = new PictureBox
            {
                Location = new Point(20, 15),
                Size = new Size(120, 70),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };
            headerPanel.Controls.Add(picLogo);

            string logoPath = ConfigurationControl.GetLogoPath();
            int textStartX = 20;

            if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
            {
                try
                {
                    picLogo.Image = Image.FromFile(logoPath);
                    picLogo.Visible = true;
                    textStartX = 150;
                }
                catch
                {
                    picLogo.Visible = false;
                }
            }
            else
            {
                picLogo.Visible = false;
            }

            lblStationName = new Label
            {
                Text = ConfigurationControl.GetStationName(),
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(textStartX, 15),
                AutoSize = true
            };
            headerPanel.Controls.Add(lblStationName);

            lblDateTime = new Label
            {
                Text = DateTime.Now.ToString("dddd, dd MMMM yyyy - HH:mm:ss"),
                Font = new Font("Segoe UI", 14, FontStyle.Regular),
                ForeColor = Color.FromArgb(200, 200, 200),
                Location = new Point(textStartX, 58),
                AutoSize = true
            };
            headerPanel.Controls.Add(lblDateTime);

            if (LicenseManager.IsDemoMode())
            {
                lblDemoTag = new Label
                {
                    Text = "🔸 DEMO",
                    Font = new Font("Segoe UI", 14, FontStyle.Bold),
                    ForeColor = Color.White,
                    BackColor = Color.FromArgb(255, 140, 0),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Size = new Size(100, 40),
                    Location = new Point(headerPanel.Width - 120, 30),
                    Anchor = AnchorStyles.Top | AnchorStyles.Right
                };
                headerPanel.Controls.Add(lblDemoTag);
            }

            CreatePanels();
            this.Resize += OverviewControl_Resize;
        }

        private void OverviewControl_Resize(object sender, EventArgs e)
        {
            RepositionPanels();
        }

        private void CreatePanels()
        {
            // ═══════════════════════════════════════════════════════════
            // ROW 1: Now Playing, Next, Last Played
            // ═══════════════════════════════════════════════════════════
            pnlNowPlaying = CreateInfoPanel("🎵 IN ONDA", 150, Color.FromArgb(0, 180, 0), out lblNowPlayingHeader);
            lblNowPlayingArtist = CreateContentLabel("---", 35, Color.White, 10, FontStyle.Bold, 20);
            lblNowPlayingTitle = CreateContentLabel("---", 55, Color.FromArgb(220, 220, 220), 10, FontStyle.Regular, 20);
            pnlNowPlaying.Controls.Add(lblNowPlayingArtist);
            pnlNowPlaying.Controls.Add(lblNowPlayingTitle);
            pnlNowPlaying.Controls.Add(lblNowPlayingTime);
            this.Controls.Add(pnlNowPlaying);

            pnlNextTrack = CreateInfoPanel("⏭️ PROSSIMO", 150, Color.FromArgb(0, 120, 215), out lblNextTrackHeader);
            lblNextArtist = CreateContentLabel("---", 35, Color.White, 10, FontStyle.Bold, 20);
            lblNextTitle = CreateContentLabel("---", 55, Color.FromArgb(220, 220, 220), 10, FontStyle.Regular, 20);
            pnlNextTrack.Controls.Add(lblNextArtist);
            pnlNextTrack.Controls.Add(lblNextTitle);
            pnlNextTrack.Controls.Add(lblNextDuration);
            this.Controls.Add(pnlNextTrack);

            pnlLastPlayed = CreateInfoPanel("⏮️ APPENA SUONATO", 150, Color.FromArgb(80, 80, 80), out lblLastPlayedHeader);
            lblLastArtist = CreateContentLabel("---", 35, Color.White, 10, FontStyle.Bold, 20);
            lblLastTitle = CreateContentLabel("---", 55, Color.FromArgb(220, 220, 220), 10, FontStyle.Regular, 20);
            pnlLastPlayed.Controls.Add(lblLastArtist);
            pnlLastPlayed.Controls.Add(lblLastTitle);
            this.Controls.Add(pnlLastPlayed);

            // ═══════════════════════════════════════════════════════════
            // Video Preview (solo RadioTV)
            // ═══════════════════════════════════════════════════════════
            if (_isRadioTVMode)
            {
                pnlVideoPreview = CreateInfoPanel("📺 VIDEO PREVIEW", 250, Color.FromArgb(0, 150, 200), out lblVideoPreviewHeader);

                lblVideoStatus = new Label
                {
                    Text = "🔴 Disconnected",
                    Font = new Font("Segoe UI", 8),
                    ForeColor = Color.FromArgb(200, 0, 0),
                    Location = new Point(10, 10),
                    AutoSize = true,
                    Anchor = AnchorStyles.Top | AnchorStyles.Right
                };
                pnlVideoPreview.Controls.Add(lblVideoStatus);

                picVideoPreview = new PictureBox
                {
                    Location = new Point(10, 35),
                    Size = new Size(380, 214),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.Black,
                    BorderStyle = BorderStyle.FixedSingle
                };
                pnlVideoPreview.Controls.Add(picVideoPreview);
                this.Controls.Add(pnlVideoPreview);
            }

            // ═══════════════════════════════════════════════════════════
            // Clock, Schedule, Ad
            // ═══════════════════════════════════════════════════════════
            pnlClockActive = CreateInfoPanel("🕐 CLOCK ATTIVO", 100, Color.FromArgb(138, 43, 226), out lblClockActiveHeader);
            lblClockName = new Label
            {
                Text = "---",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 50),
                Size = new Size(200, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoEllipsis = true
            };
            pnlClockActive.Controls.Add(lblClockName);
            this.Controls.Add(pnlClockActive);

            pnlNextSchedule = CreateInfoPanelWithCountdown("⏰ PROSSIMA SCHEDULAZIONE", 100, Color.FromArgb(255, 140, 0), out lblNextScheduleHeader, out lblCountdown);
            lblNextScheduleInfo = new Label
            {
                Text = "--:--:-- - ---",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 50),
                Size = new Size(200, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoEllipsis = true
            };
            pnlNextSchedule.Controls.Add(lblNextScheduleInfo);
            this.Controls.Add(pnlNextSchedule);

            pnlNextAd = CreateInfoPanelWithCountdown("📢 PROSSIMA PUBBLICITÀ", 100, Color.FromArgb(255, 87, 34), out lblNextAdHeader, out lblNextAdCountdown);
            lblNextAdInfo = new Label
            {
                Text = "---",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 50),
                Size = new Size(200, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoEllipsis = true
            };
            pnlNextAd.Controls.Add(lblNextAdInfo);
            this.Controls.Add(pnlNextAd);

            // ═══════════════════════════════════════════════════════════
            // Encoders, Recorders, System
            // ═══════════════════════════════════════════════════════════
            pnlEncoders = CreateInfoPanel("📡 ENCODERS", 110, Color.FromArgb(0, 150, 136), out lblEncodersHeader);
            lblEncodersInfo = new Label
            {
                Text = "0 / 0 attivi",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 50),
                Size = new Size(200, 40),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoEllipsis = true
            };
            pnlEncoders.Controls.Add(lblEncodersInfo);
            this.Controls.Add(pnlEncoders);

            pnlRecorders = CreateInfoPanel("🎙️ RECORDERS", 110, Color.FromArgb(233, 30, 99), out lblRecordersHeader);
            lblRecordersInfo = new Label
            {
                Text = "0 / 0 attivi",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 50),
                Size = new Size(200, 40),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoEllipsis = true
            };
            pnlRecorders.Controls.Add(lblRecordersInfo);
            this.Controls.Add(pnlRecorders);

            pnlSystemInfo = CreateInfoPanelNoTitle(110, Color.FromArgb(96, 125, 139));
            lblSystemInfo = new Label
            {
                Text = "Caricamento.. .",
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = Color.White,
                Location = new Point(15, 15),
                Size = new Size(200, 80),
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlSystemInfo.Controls.Add(lblSystemInfo);
            this.Controls.Add(pnlSystemInfo);

            RepositionPanels();
        }

        // ═══════════════════════════════════════════════════════════
        // LAYOUT - FISSO, SOLO RIDIMENSIONAMENTO PROPORZIONALE
        // ═══════════════════════════════════════════════════════════

        private void RepositionPanels()
        {
            int availableWidth = this.Width - 40;
            if (availableWidth < 100) return;

            int margin = 20;
            int yPos = 130;

            if (_isRadioTVMode)
            {
                RepositionRadioTV(margin, yPos, availableWidth);
            }
            else
            {
                RepositionRadioOnly(margin, yPos, availableWidth);
            }
        }

        /// <summary>
        /// Layout RadioTV - FISSO:  Row1 (3 pannelli), Row2 (Video + 3 pannelli laterali), Row3 (3 pannelli)
        /// </summary>
        private void RepositionRadioTV(int margin, int yPos, int availableWidth)
        {
            int thirdWidth = (availableWidth - margin * 2) / 3;

            // ═══════════════════════════════════════════════════════════
            // ROW 1: Now Playing | Next | Last Played
            // ═══════════════════════════════════════════════════════════
            pnlNowPlaying.Location = new Point(20, yPos);
            pnlNowPlaying.Size = new Size(thirdWidth, 100);
            CenterContentLabels(pnlNowPlaying, thirdWidth);

            pnlNextTrack.Location = new Point(20 + thirdWidth + margin, yPos);
            pnlNextTrack.Size = new Size(thirdWidth, 100);
            CenterContentLabels(pnlNextTrack, thirdWidth);

            pnlLastPlayed.Location = new Point(20 + (thirdWidth + margin) * 2, yPos);
            pnlLastPlayed.Size = new Size(thirdWidth, 100);
            CenterContentLabels(pnlLastPlayed, thirdWidth);

            yPos += 100 + margin;

            // ═══════════════════════════════════════════════════════════
            // ROW 2: Video (sinistra 60%) | Clock/Schedule/Ad (destra 40%)
            // ═══════════════════════════════════════════════════════════
            int videoWidth = (int)((availableWidth - margin) * 0.55);
            int sidePanelWidth = availableWidth - videoWidth - margin;
            int videoHeight = (int)(videoWidth * 9.0 / 16.0);
            int videoPanelHeight = videoHeight + 45;
            int sidePanelHeight = (videoPanelHeight - margin * 2) / 3;

            pnlVideoPreview.Location = new Point(20, yPos);
            pnlVideoPreview.Size = new Size(videoWidth, videoPanelHeight);
            picVideoPreview.Location = new Point(10, 35);
            picVideoPreview.Size = new Size(videoWidth - 20, videoHeight);
            lblVideoStatus.Location = new Point(videoWidth - 115, 12);

            int rightX = 20 + videoWidth + margin;

            pnlClockActive.Location = new Point(rightX, yPos);
            pnlClockActive.Size = new Size(sidePanelWidth, sidePanelHeight);
            CenterContentLabels(pnlClockActive, sidePanelWidth);

            pnlNextSchedule.Location = new Point(rightX, yPos + sidePanelHeight + margin);
            pnlNextSchedule.Size = new Size(sidePanelWidth, sidePanelHeight);
            CenterContentLabels(pnlNextSchedule, sidePanelWidth);
            lblCountdown.Location = new Point(sidePanelWidth - 95, 12);
            lblCountdown.Width = 85;

            pnlNextAd.Location = new Point(rightX, yPos + (sidePanelHeight + margin) * 2);
            pnlNextAd.Size = new Size(sidePanelWidth, sidePanelHeight);
            CenterContentLabels(pnlNextAd, sidePanelWidth);
            lblNextAdCountdown.Location = new Point(sidePanelWidth - 95, 12);
            lblNextAdCountdown.Width = 85;

            yPos += videoPanelHeight + margin;

            // ═══════════════════════════════════════════════════════════
            // ROW 3: Encoders | Recorders | System
            // ═══════════════════════════════════════════════════════════
            pnlEncoders.Location = new Point(20, yPos);
            pnlEncoders.Size = new Size(thirdWidth, 100);
            CenterContentLabels(pnlEncoders, thirdWidth);

            pnlRecorders.Location = new Point(20 + thirdWidth + margin, yPos);
            pnlRecorders.Size = new Size(thirdWidth, 100);
            CenterContentLabels(pnlRecorders, thirdWidth);

            pnlSystemInfo.Location = new Point(20 + (thirdWidth + margin) * 2, yPos);
            pnlSystemInfo.Size = new Size(thirdWidth, 100);
            CenterContentLabels(pnlSystemInfo, thirdWidth);
        }

        /// <summary>
        /// Layout Radio Only - FISSO: 3 righe da 3 pannelli (come prima)
        /// </summary>
        private void RepositionRadioOnly(int margin, int yPos, int availableWidth)
        {
            int thirdWidth = (availableWidth - margin * 2) / 3;

            // ROW 1: Now Playing | Next | Last Played
            pnlNowPlaying.Location = new Point(20, yPos);
            pnlNowPlaying.Size = new Size(thirdWidth, 150);
            CenterContentLabels(pnlNowPlaying, thirdWidth);

            pnlNextTrack.Location = new Point(20 + thirdWidth + margin, yPos);
            pnlNextTrack.Size = new Size(thirdWidth, 150);
            CenterContentLabels(pnlNextTrack, thirdWidth);

            pnlLastPlayed.Location = new Point(20 + (thirdWidth + margin) * 2, yPos);
            pnlLastPlayed.Size = new Size(thirdWidth, 150);
            CenterContentLabels(pnlLastPlayed, thirdWidth);

            yPos += 170;

            // ROW 2: Clock | Schedule | Ad
            int mediumPanelWidth = (availableWidth - margin * 2) / 3;

            pnlClockActive.Location = new Point(20, yPos);
            pnlClockActive.Size = new Size(mediumPanelWidth, 100);
            CenterContentLabels(pnlClockActive, mediumPanelWidth);

            pnlNextSchedule.Location = new Point(20 + mediumPanelWidth + margin, yPos);
            pnlNextSchedule.Size = new Size(mediumPanelWidth, 100);
            CenterContentLabels(pnlNextSchedule, mediumPanelWidth);
            lblCountdown.Width = 90;
            lblCountdown.Location = new Point(mediumPanelWidth - 100, 10);

            pnlNextAd.Location = new Point(20 + (mediumPanelWidth + margin) * 2, yPos);
            pnlNextAd.Size = new Size(mediumPanelWidth, 100);
            CenterContentLabels(pnlNextAd, mediumPanelWidth);
            lblNextAdCountdown.Width = 90;
            lblNextAdCountdown.Location = new Point(mediumPanelWidth - 100, 10);

            yPos += 120;

            // ROW 3: Encoders | Recorders | System
            int smallPanelWidth = (availableWidth - margin * 2) / 3;

            pnlEncoders.Location = new Point(20, yPos);
            pnlEncoders.Size = new Size(smallPanelWidth, 110);
            CenterContentLabels(pnlEncoders, smallPanelWidth);

            pnlRecorders.Location = new Point(20 + smallPanelWidth + margin, yPos);
            pnlRecorders.Size = new Size(smallPanelWidth, 110);
            CenterContentLabels(pnlRecorders, smallPanelWidth);

            pnlSystemInfo.Location = new Point(20 + (smallPanelWidth + margin) * 2, yPos);
            pnlSystemInfo.Size = new Size(smallPanelWidth, 110);
            CenterContentLabels(pnlSystemInfo, smallPanelWidth);
        }

        /// <summary>
        /// Centra solo le label di contenuto (non gli header)
        /// </summary>
        private void CenterContentLabels(Panel panel, int panelWidth)
        {
            foreach (Control c in panel.Controls)
            {
                if (c is Label lbl && c.Location.Y >= 35)
                {
                    lbl.Width = panelWidth - 30;
                    lbl.Location = new Point(15, lbl.Location.Y);
                    lbl.TextAlign = ContentAlignment.MiddleCenter;
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        // HELPER METHODS
        // ═══════════════════════════════════════════════════════════

        private Panel CreateInfoPanel(string title, int height, Color accentColor, out Label headerLabel)
        {
            Panel panel = new Panel
            {
                BackColor = Color.FromArgb(30, 30, 30),
                BorderStyle = BorderStyle.FixedSingle,
                Height = height
            };

            Panel accentBar = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(1000, 5),
                BackColor = accentColor,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            panel.Controls.Add(accentBar);

            headerLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = accentColor,
                Location = new Point(15, 15),
                AutoSize = true
            };
            panel.Controls.Add(headerLabel);

            return panel;
        }

        private Panel CreateInfoPanelWithCountdown(string title, int height, Color accentColor, out Label headerLabel, out Label countdownLabel)
        {
            Panel panel = new Panel
            {
                BackColor = Color.FromArgb(30, 30, 30),
                BorderStyle = BorderStyle.FixedSingle,
                Height = height
            };

            Panel accentBar = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(1000, 5),
                BackColor = accentColor,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            panel.Controls.Add(accentBar);

            headerLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = accentColor,
                Location = new Point(15, 15),
                AutoSize = true
            };
            panel.Controls.Add(headerLabel);

            countdownLabel = new Label
            {
                Text = "--:--:--",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Location = new Point(0, 12),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleRight,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            panel.Controls.Add(countdownLabel);

            return panel;
        }

        private Panel CreateInfoPanelNoTitle(int height, Color accentColor)
        {
            Panel panel = new Panel
            {
                BackColor = Color.FromArgb(30, 30, 30),
                BorderStyle = BorderStyle.FixedSingle,
                Height = height
            };

            Panel accentBar = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(1000, 5),
                BackColor = accentColor,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            panel.Controls.Add(accentBar);

            return panel;
        }

        private Label CreateContentLabel(string text, int y, Color color, int fontSize, FontStyle style, int height = 20)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", fontSize, style),
                ForeColor = color,
                Location = new Point(15, y),
                AutoEllipsis = true,
                AutoSize = false,
                Size = new Size(250, height),
                TextAlign = ContentAlignment.MiddleCenter
            };
        }

        // ═══════════════════════════════════════════════════════════
        // TIMER & DATA UPDATES
        // ═══════════════════════════════════════════════════════════

        private void StartTimer()
        {
            _updateTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            lblDateTime.Text = DateTime.Now.ToString("dddd, dd MMMM yyyy - HH:mm:ss");

            DateTime now = DateTime.Now;

            bool shouldReload = false;

            if (_cachedAdvItems.Count == 0)
                shouldReload = true;
            else if (_advCacheDate != now.Date)
                shouldReload = true;
            else if (now.Hour == 23 && now.Minute == 59 && now.Second >= 55)
                shouldReload = true;
            else if (now.Hour == 0 && now.Minute == 0 && now.Second >= 5 && now.Second <= 10)
                shouldReload = true;
            else if (now.Minute == 0 && now.Second >= 5 && now.Second <= 10 && (now - _lastAdvReloadTime).TotalMinutes > 1)
                shouldReload = true;

            if (shouldReload)
                LoadAdvCache();

            UpdateNowPlaying();
            UpdateNextTrack();
            UpdateLastPlayed();
            UpdateClockActive();
            UpdateNextSchedule();
            UpdateNextAd();
            UpdateEncodersInfo();
            UpdateRecordersInfo();
            UpdateSystemInfo();
        }

        private void LoadAdvCache()
        {
            try
            {
                string databasePath = "";

                try
                {
                    using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AirDirector"))
                    {
                        if (key != null)
                            databasePath = key.GetValue("DatabasePath") as string;
                    }
                }
                catch { }

                if (string.IsNullOrEmpty(databasePath))
                    databasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database");

                string advFilePath = Path.Combine(databasePath, "ADV_AirDirector.dbc");

                if (!File.Exists(advFilePath))
                {
                    _cachedAdvItems.Clear();
                    _advCacheDate = DateTime.MinValue;
                    _lastAdvReloadTime = DateTime.Now;
                    return;
                }

                var lines = File.ReadAllLines(advFilePath);
                _cachedAdvItems.Clear();

                var italianCulture = new System.Globalization.CultureInfo("it-IT");

                for (int i = 1; i < lines.Length; i++)
                {
                    try
                    {
                        string line = lines[i].Trim().TrimStart('\uFEFF');
                        if (string.IsNullOrEmpty(line)) continue;
                        if (line.StartsWith("\"") && line.EndsWith("\"") && line.Length >= 2)
                        	line = line.Substring(1, line.Length - 2);
                        var parts = line.Split(new[] { "\";\""  }, StringSplitOptions.None);
                        if (parts.Length < 9) continue;

                        // Pulisci eventuali virgolette residue su ogni campo
                        for (int p = 0; p < parts.Length; p++)
                            parts[p] = parts[p].Trim('"').Trim();

                        var item = new AirDirectorPlaylistItem
                        {
                            ID = int.Parse(parts[0]),
                            Date = DateTime.Parse(parts[1], italianCulture),
                            SlotTime = parts[2],
                            SequenceOrder = int.Parse(parts[3]),
                            FileType = parts[4],
                            FilePath = parts[5],
                            Duration = int.Parse(parts[6]),
                            ClientName = parts[7],
                            SpotTitle = parts[8],
                            CampaignName = "",
                            CategoryName = "",
                            IsActive = true
                        };

                        _cachedAdvItems.Add(item);
                    }
                    catch { }
                }

                _advCacheDate = DateTime.Now.Date;
                _lastAdvReloadTime = DateTime.Now;
            }
            catch
            {
                _cachedAdvItems.Clear();
                _advCacheDate = DateTime.MinValue;
                _lastAdvReloadTime = DateTime.Now;
            }
        }

        private void UpdateNowPlaying()
        {
            if (_playlistQueue == null) return;

            var currentItem = _playlistQueue.GetCurrentPlayingItem();

            if (currentItem != null)
            {
                if (currentItem.Type == PlaylistItemType.Music)
                {
                    if (_currentMusic == null || _currentMusic != currentItem)
                    {
                        if (_currentMusic != null)
                            _lastMusic = _currentMusic;
                        _currentMusic = currentItem;
                    }

                    lblNowPlayingArtist.Text = string.IsNullOrEmpty(currentItem.Artist) ? currentItem.Title : currentItem.Artist;
                    lblNowPlayingTitle.Text = string.IsNullOrEmpty(currentItem.Artist) ? "" : currentItem.Title;
                }
                else
                {
                    lblNowPlayingArtist.Text = $"⚡ {currentItem.Title}";
                    lblNowPlayingTitle.Text = "(" + LanguageManager.GetString("Overview.ClipJingle", "Clip / Jingle") + ")";
                }
            }
            else
            {
                lblNowPlayingArtist.Text = "---";
                lblNowPlayingTitle.Text = LanguageManager.GetString("Overview.NoPlaying", "Nessun elemento in riproduzione");
            }
        }

        private void UpdateNextTrack()
        {
            if (_playlistQueue == null) return;

            var allItems = _playlistQueue.GetAllItems();
            var currentIndex = allItems.FindIndex(i => i == _playlistQueue.GetCurrentPlayingItem());

            PlaylistQueueItem nextMusicItem = null;

            if (currentIndex >= 0)
            {
                for (int i = currentIndex + 1; i < allItems.Count; i++)
                {
                    if (allItems[i].Type == PlaylistItemType.Music)
                    {
                        nextMusicItem = allItems[i];
                        break;
                    }
                }
            }

            if (nextMusicItem != null)
            {
                lblNextArtist.Text = string.IsNullOrEmpty(nextMusicItem.Artist) ? nextMusicItem.Title : nextMusicItem.Artist;
                lblNextTitle.Text = string.IsNullOrEmpty(nextMusicItem.Artist) ? "" : nextMusicItem.Title;
            }
            else
            {
                lblNextArtist.Text = "---";
                lblNextTitle.Text = LanguageManager.GetString("Overview.NoNextTrack", "Nessun brano successivo");
            }
        }

        private void UpdateLastPlayed()
        {
            if (_lastMusic != null)
            {
                lblLastArtist.Text = string.IsNullOrEmpty(_lastMusic.Artist) ? _lastMusic.Title : _lastMusic.Artist;
                lblLastTitle.Text = string.IsNullOrEmpty(_lastMusic.Artist) ? "" : _lastMusic.Title;
            }
            else
            {
                lblLastArtist.Text = "---";
                lblLastTitle.Text = LanguageManager.GetString("Overview.NoPreviousTrack", "Nessun brano precedente");
            }
        }

        private void UpdateClockActive()
        {
            if (_playlistQueue == null) return;

            string clockName = _playlistQueue.GetCurrentClockName();
            lblClockName.Text = string.IsNullOrEmpty(clockName) ? LanguageManager.GetString("Overview.NoActiveClock", "Nessun clock attivo") : clockName;
        }

        private void UpdateNextSchedule()
        {
            try
            {
                DateTime now = DateTime.Now;
                int currentDayOfWeek = (int)now.DayOfWeek;

                var schedules = DbcManager.LoadFromCsv<ScheduleEntry>("Schedules.dbc");
                var activeSchedules = schedules.Where(s => s.IsEnabled == 1 && IsDayEnabled(s, currentDayOfWeek)).ToList();

                ScheduleEntry nextSchedule = null;
                DateTime nextScheduleTime = DateTime.MaxValue;

                foreach (var schedule in activeSchedules)
                {
                    if (string.IsNullOrEmpty(schedule.Times)) continue;

                    var times = schedule.Times.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var timeStr in times)
                    {
                        if (TimeSpan.TryParse(timeStr.Trim(), out TimeSpan scheduleTime))
                        {
                            DateTime scheduleDateTime = now.Date.Add(scheduleTime);

                            if (scheduleDateTime > now && scheduleDateTime < nextScheduleTime)
                            {
                                nextSchedule = schedule;
                                nextScheduleTime = scheduleDateTime;
                            }
                        }
                    }
                }

                if (nextSchedule != null)
                {
                    lblNextScheduleInfo.Text = $"{nextScheduleTime:HH:mm:ss} - {nextSchedule.Name}";

                    TimeSpan remaining = nextScheduleTime - now;
                    int totalSeconds = (int)remaining.TotalSeconds;
                    int hours = totalSeconds / 3600;
                    int minutes = (totalSeconds % 3600) / 60;
                    int seconds = totalSeconds % 60;

                    lblCountdown.Text = $"{hours:00}:{minutes:00}:{seconds:00}";

                    if (totalSeconds <= 120)
                    {
                        _blinkState = !_blinkState;
                        lblCountdown.ForeColor = _blinkState ? Color.Red : Color.White;
                    }
                    else
                    {
                        lblCountdown.ForeColor = Color.White;
                    }
                }
                else
                {
                    lblNextScheduleInfo.Text = LanguageManager.GetString("Overview.NoSchedule", "Nessuna schedulazione prevista");
                    lblCountdown.Text = "--:--:--";
                    lblCountdown.ForeColor = Color.White;
                }
            }
            catch
            {
                lblNextScheduleInfo.Text = LanguageManager.GetString("Overview.ScheduleError", "Errore lettura schedulazioni");
                lblCountdown.Text = "--:--:--";
                lblCountdown.ForeColor = Color.White;
            }
        }

        private void UpdateNextAd()
        {
            try
            {
                DateTime now = DateTime.Now;

                if (_cachedAdvItems.Count == 0)
                {
                    lblNextAdInfo.Text = LanguageManager.GetString("Overview.NoAdsScheduled", "Nessuna pubblicità programmata");
                    lblNextAdCountdown.Text = "--:--:--";
                    lblNextAdCountdown.ForeColor = Color.White;
                    return;
                }

                var todaySlots = _cachedAdvItems
                    .Where(a => a.Date.Date == now.Date && a.IsActive)
                    .GroupBy(a => a.SlotTime)
                    .Select(g => new
                    {
                        SlotTime = g.Key,
                        Items = g.OrderBy(x => x.SequenceOrder).ToList()
                    })
                    .OrderBy(s => s.SlotTime)
                    .ToList();

                DateTime? nextAdDateTime = null;
                int spotCount = 0;
                int totalDuration = 0;

                foreach (var slot in todaySlots)
                {
                    if (TimeSpan.TryParse(slot.SlotTime, out TimeSpan slotTime))
                    {
                        DateTime slotDateTime = now.Date.Add(slotTime);

                        if (slotDateTime > now)
                        {
                            nextAdDateTime = slotDateTime;
                            spotCount = slot.Items.Count(i => i.FileType == "SPOT");
                            totalDuration = slot.Items.Sum(i => i.Duration);
                            break;
                        }
                    }
                }

                if (nextAdDateTime.HasValue)
                {
                    int mins = totalDuration / 60;
                    int secs = totalDuration % 60;
                    string durationStr = $"{mins:00}:{secs:00}";

                    lblNextAdInfo.Text = $"{nextAdDateTime.Value:HH:mm} - {spotCount} Spot - {durationStr}";

                    TimeSpan remaining = nextAdDateTime.Value - now;
                    int totalSeconds = (int)remaining.TotalSeconds;
                    int hours = totalSeconds / 3600;
                    int minutes = (totalSeconds % 3600) / 60;
                    int seconds = totalSeconds % 60;

                    lblNextAdCountdown.Text = $"{hours:00}:{minutes:00}:{seconds:00}";

                    if (totalSeconds <= 120)
                    {
                        _blinkState = !_blinkState;
                        lblNextAdCountdown.ForeColor = _blinkState ? Color.Red : Color.White;
                    }
                    else
                    {
                        lblNextAdCountdown.ForeColor = Color.White;
                    }
                }
                else
                {
                    lblNextAdInfo.Text = LanguageManager.GetString("Overview.NoAdsScheduled", "Nessuna pubblicità programmata");
                    lblNextAdCountdown.Text = "--:--:--";
                    lblNextAdCountdown.ForeColor = Color.White;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UpdateNextAd] Errore: {ex.Message}");
                lblNextAdInfo.Text = LanguageManager.GetString("Overview.AdError", "Errore lettura pubblicità");
                lblNextAdCountdown.Text = "--:--:--";
                lblNextAdCountdown.ForeColor = Color.White;
            }
        }

        private bool IsDayEnabled(ScheduleEntry schedule, int dayOfWeek)
        {
            switch (dayOfWeek)
            {
                case 0: return schedule.Sunday == 1;
                case 1: return schedule.Monday == 1;
                case 2: return schedule.Tuesday == 1;
                case 3: return schedule.Wednesday == 1;
                case 4: return schedule.Thursday == 1;
                case 5: return schedule.Friday == 1;
                case 6: return schedule.Saturday == 1;
                default: return false;
            }
        }

        private void UpdateEncodersInfo()
        {
            if (_encodersControl == null) return;

            int total = _encodersControl.GetTotalEncoders();
            int active = _encodersControl.GetActiveEncoders();

            lblEncodersInfo.Text = string.Format(LanguageManager.GetString("Overview.Active", "{0} / {1} attivi"), active, total);
            lblEncodersInfo.ForeColor = active > 0 ? Color.FromArgb(0, 255, 0) : Color.Gray;
        }

        private void UpdateRecordersInfo()
        {
            if (_recordersControl == null) return;

            int total = _recordersControl.GetTotalRecorders();
            int active = _recordersControl.GetActiveRecorders();

            lblRecordersInfo.Text = string.Format(LanguageManager.GetString("Overview.Active", "{0} / {1} attivi"), active, total);
            lblRecordersInfo.ForeColor = active > 0 ? Color.FromArgb(255, 0, 0) : Color.Gray;
        }

        private void UpdateSystemInfo()
        {
            try
            {
                int mixDuration = ConfigurationControl.GetMixDuration();
                int hourlySep = ConfigurationControl.GetHourlySeparation();
                int artistSep = ConfigurationControl.GetArtistSeparation();
                bool autoStart = ConfigurationControl.GetAutoStartMode();

                string mixLabel = LanguageManager.GetString("Overview.Mix", "Mix");
                string trackSepLabel = LanguageManager.GetString("Overview.TrackSeparation", "Separazione Brano");
                string artistSepLabel = LanguageManager.GetString("Overview.ArtistSeparation", "Separazione Artista");
                string autoLabel = LanguageManager.GetString("Overview.Auto", "Auto");

                lblSystemInfo.Text =
                    $"{mixLabel}:  {mixDuration}ms\n" +
                    $"{trackSepLabel}: {hourlySep}h\n" +
                    $"{artistSepLabel}: {artistSep}h\n" +
                    $"{autoLabel}: {(autoStart ? "ON" : "OFF")}";
            }
            catch
            {
                lblSystemInfo.Text = LanguageManager.GetString("Overview.LoadError", "Errore\ncaricamento");
            }
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

                _updateTimer?.Stop();
                _updateTimer?.Dispose();

                _previewRetryTimer?.Stop();
                _previewRetryTimer?.Dispose();

                if (_ndiPreviewReceiver != null)
                {
                    _ndiPreviewReceiver.FrameReceived -= OnNDIFrameReceived;
                    _ndiPreviewReceiver.Dispose();
                    _ndiPreviewReceiver = null;
                }

                picVideoPreview?.Image?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}