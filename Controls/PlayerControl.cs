using AirDirector.Services;
using AirDirector.Services.Localization;
using AirDirector.Themes;
using LibVLCSharp.Shared;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AirDirector.Controls
{
    public partial class PlayerControl : UserControl
    {
        private PlaylistQueueControl _playlistQueue;

        private IWavePlayer _masterOutput;
        private MixingSampleProvider _mixer;
        private MeteringSampleProvider _meterProvider;

        private AudioFileReader _audioFileA;
        private VolumeSampleProvider _volumeProviderA;

        private AudioFileReader _audioFileB;
        private VolumeSampleProvider _volumeProviderB;

        private LibVLC _libVLC;
        private MediaPlayer _vlcPlayer;

        private float _smoothedVULevelLeft = 0f;
        private float _smoothedVULevelRight = 0f;

        private System.Windows.Forms.Timer _updateTimer;
        private System.Windows.Forms.Timer _mixCheckTimer;
        private System.Windows.Forms.Timer _fadeOutTimer;
        private System.Windows.Forms.Timer _blinkTimer;
        private System.Windows.Forms.Timer _streamDurationTimer;

        private bool _isPlaying = false;
        private bool _isPaused = false;
        private bool _autoMode = true;
        private TimeSpan _totalDuration = TimeSpan.Zero;
        private TimeSpan _currentPosition = TimeSpan.Zero;
        private TimeSpan _introTime = TimeSpan.FromSeconds(10);

        private int _markerIN = 0;
        private int _markerINTRO = 0;
        private int _markerMIX = 0;
        private int _markerOUT = 0;
        private int _mixDuration = 5000;
        private bool _mixRequested = false;

        private bool _isPlayerAActive = true;
        private VolumeSampleProvider _volumeFadingOut = null;
        private AudioFileReader _audioFadingOut = null;
        private int _fadeOutStartMs = 0;
        private int _fadeOutEndMs = 0;

        private float[] _waveformData;
        private int _waveformSamples = 1000;
        private string _lastLoadedFile = "";
        private bool _isLoadingWaveform = false;
        private Bitmap _waveformBitmap;
        private readonly object _waveformLock = new object();

        private float _lastVULevelLeft = 0f;
        private float _lastVULevelRight = 0f;

        private bool _isHoveringWaveform = false;
        private int _hoverX = 0;

        private bool _blinkState = false;

        private bool _isStreamingURL = false;
        private TimeSpan _streamScheduledDuration = TimeSpan.Zero;
        private DateTime _streamStartTime = DateTime.MinValue;

        private Panel waveformPanel;
        private Label lblIntro;
        private Label lblElapsed;
        private Label lblRemaining;
        private Label lblClock;
        private Label lblDate;
        private Label lblArtist;
        private Panel vuMeterLeftPanel;
        private Panel vuMeterRightPanel;

        private Button btnPlay;
        private Button btnPause;
        private Button btnStop;

        private Label _lblElapsedHeader;
        private Label _lblRemainingHeader;

        // ═══════════════════════════════════════════════════════════
        // EVENTI STANDARD
        // ═══════════════════════════════════════════════════════════
        public event EventHandler PlayRequested;
        public event EventHandler PauseRequested;
        public event EventHandler StopRequested;
        public event EventHandler NextRequested;
        public event EventHandler<bool> AutoModeChanged;
        public event EventHandler MixPointReached;
        public event EventHandler TrackEndedInManualMode;

        // ═══════════════════════════════════════════════════════════
        // NUOVI EVENTI PER RADIOTV BRIDGE
        // ═══════════════════════════════════════════════════════════
        private NDIAudioTap _ndiAudioTap;
        public event EventHandler<TrackChangedEventArgs> TrackChanged;
        public event EventHandler<PlayStateChangedEventArgs> PlayStateChanged;

        // Output per NDI (separato)
        private Action<float[], int, int> _ndiAudioCallback;
        private System.Threading.Timer _ndiAudioTimer;
        private float[] _lastMixerBuffer;
        private readonly object _ndiBufferLock = new object();

        // ═══════════════════════════════════════════════════════════
        // PROPRIETÀ PER RADIOTV BRIDGE
        // ═══════════════════════════════════════════════════════════
        public string CurrentFilePath { get; private set; } = "";
        public string CurrentArtist { get; private set; } = "";
        public string CurrentTitle { get; private set; } = "";
        public bool IsCurrentlyPlaying => _isPlaying && !_isPaused;

        private CancellationTokenSource _waveformCts = null;

        private readonly object _waveformCtsLock = new object();

        private Services.Core.DailyLogger _dailyLogger;

        public PlayerControl()
        {
            InitializeComponent();

            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.UserPaint, true);

            InitializePlayerUI();
            InitializeTimer();
            InitializeAudioEngine();
            InitializeVLC();
            try { _dailyLogger = new Services.Core.DailyLogger("PlayerAudio"); } catch { }

            LanguageManager.LanguageChanged += (s, e) => UpdateTimerLabels();
        }

        private void InitializeVLC()
        {
            try
            {
                LibVLCSharp.Shared.Core.Initialize();

                _libVLC = new LibVLC(
                    "--no-video",
                    "--network-caching=10000",
                    "--live-caching=10000",
                    "--clock-jitter=0",
                    "--clock-synchro=0",
                    "--no-audio-time-stretch",
                    "--verbose=2"
                );

                _vlcPlayer = new MediaPlayer(_libVLC);

                _vlcPlayer.Playing += (s, e) => Log("[VLC] ▶️ Playing");
                _vlcPlayer.Paused += (s, e) => Log("[VLC] ⏸️ Paused");
                _vlcPlayer.Stopped += (s, e) => Log("[VLC] ⏹️ Stopped");
                _vlcPlayer.EndReached += (s, e) => Log("[VLC] 🏁 EndReached");
                _vlcPlayer.EncounteredError += (s, e) => Log("[VLC] ❌ Error");

                Log("[VLC] ✅ Inizializzato con successo");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore inizializzazione VLC:\n{ex.Message}\n\nAssicurati di aver installato libvlc.",
                    "Errore VLC", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeAudioEngine()
        {
            try
            {
                int deviceNumber = ConfigurationControl.GetMainOutputDeviceNumber();

                _masterOutput = new WaveOutEvent
                {
                    DeviceNumber = deviceNumber,
                    DesiredLatency = 150,
                    NumberOfBuffers = 3
                };

                _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
                _mixer.ReadFully = true;

                _meterProvider = new MeteringSampleProvider(_mixer);
                _meterProvider.StreamVolume += OnStreamVolume;

                _masterOutput.Init(_meterProvider);
                _masterOutput.Play();

                Log("[Audio] ✅ Engine inizializzato @ 44100Hz");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore init audio:\n{ex.Message}", "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        

        /// <summary>
        /// Sample provider che copia l'audio a NDI
        /// </summary>
        public class NDIAudioSampleProvider : ISampleProvider
        {
            private readonly ISampleProvider _source;
            

            public WaveFormat WaveFormat => _source.WaveFormat;

            public NDIAudioSampleProvider(ISampleProvider source)
            {
                _source = source;
            }

            /// <summary>
            /// Imposta il riferimento al VideoNDIManager
            /// </summary>
            

            public int Read(float[] buffer, int offset, int count)
            {
                int samplesRead = _source.Read(buffer, offset, count);

                // Invia a NDI se disponibile

                return samplesRead;
            }
        }

        /// <summary>
        /// Crea un sample provider con resampling se necessario
        /// </summary>
        /// <summary>
        /// Crea un sample provider con resampling se necessario (supporta anche video)
        /// </summary>
        private ISampleProvider CreateResampledProvider(AudioFileReader audioFile)
        {
            ISampleProvider sampleProvider = audioFile.ToSampleProvider();

            // Converti mono a stereo se necessario
            if (audioFile.WaveFormat.Channels == 1)
            {
                sampleProvider = new MonoToStereoSampleProvider(sampleProvider);
            }

            // Resample se il sample rate non è 44100 (es. video a 48000Hz)
            if (audioFile.WaveFormat.SampleRate != 44100)
            {
                Log($"[Audio] 🔄 Resampling da {audioFile.WaveFormat.SampleRate}Hz a 44100Hz");
                sampleProvider = new WdlResamplingSampleProvider(sampleProvider, 44100);
            }

            return sampleProvider;
        }

        /// <summary>
        /// Crea un sample provider da MediaFoundationReader (per video)
        /// </summary>
        private ISampleProvider CreateResampledProviderFromMedia(MediaFoundationReader mediaReader)
        {
            ISampleProvider sampleProvider = mediaReader.ToSampleProvider();

            // Converti mono a stereo se necessario
            if (mediaReader.WaveFormat.Channels == 1)
            {
                sampleProvider = new MonoToStereoSampleProvider(sampleProvider);
            }

            // Resample se il sample rate non è 44100
            if (mediaReader.WaveFormat.SampleRate != 44100)
            {
                Log($"[Audio] 🔄 Resampling video da {mediaReader.WaveFormat.SampleRate}Hz a 44100Hz");
                sampleProvider = new WdlResamplingSampleProvider(sampleProvider, 44100);
            }

            return sampleProvider;
        }

        private void OnStreamVolume(object sender, StreamVolumeEventArgs e)
        {
            if (e.MaxSampleValues != null && e.MaxSampleValues.Length >= 2)
            {
                _lastVULevelLeft = e.MaxSampleValues[0];
                _lastVULevelRight = e.MaxSampleValues[1];
            }
        }

        public void SetPlaylistQueue(PlaylistQueueControl playlistQueue)
        {
            _playlistQueue = playlistQueue;
        }

        private void UpdateTimerLabels()
        {
            if (_lblElapsedHeader != null)
                _lblElapsedHeader.Text = LanguageManager.GetString("Player.TimeElapsed", "Tempo trascorso");

            if (_lblRemainingHeader != null)
                _lblRemainingHeader.Text = LanguageManager.GetString("Player.TimeRemaining", "Tempo restante");
        }

        private void InitializePlayerUI()
        {
            this.Size = new Size(1400, 160);
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.Padding = new Padding(5);

            Panel topRow = new Panel
            {
                Location = new Point(5, 5),
                Size = new Size(1390, 70),
                BackColor = Color.FromArgb(20, 20, 20)
            };
            this.Controls.Add(topRow);

            CreateTimer(topRow, 5, 5, LanguageManager.GetString("Player.TimeElapsed", "Tempo trascorso"), ref lblElapsed, AppTheme.LEDGreen, 155, 60, ref _lblElapsedHeader);
            CreateTimer(topRow, 165, 5, "Intro", ref lblIntro, AppTheme.LEDYellow, 155, 60, ref _lblRemainingHeader);

            Panel trackInfoPanel = new Panel
            {
                Location = new Point(325, 5),
                Size = new Size(750, 60),
                BackColor = Color.FromArgb(25, 25, 25),
                BorderStyle = BorderStyle.FixedSingle
            };
            topRow.Controls.Add(trackInfoPanel);

            lblArtist = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false
            };
            lblArtist.Paint += LblArtist_Paint;
            trackInfoPanel.Controls.Add(lblArtist);

            Label dummyHeader = null;
            CreateTimer(topRow, 1080, 5, LanguageManager.GetString("Player.TimeRemaining", "Tempo restante"), ref lblRemaining, AppTheme.LEDRed, 155, 60, ref _lblRemainingHeader);

            Panel clockPanel = new Panel
            {
                Location = new Point(1240, 5),
                Size = new Size(145, 60),
                BackColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle
            };
            topRow.Controls.Add(clockPanel);

            lblDate = new Label
            {
                Text = GetFormattedDate(),
                Font = new Font("Segoe UI", 8, FontStyle.Regular),
                ForeColor = Color.Gray,
                Location = new Point(3, 3),
                Size = new Size(145, 12),
                TextAlign = ContentAlignment.TopCenter
            };
            clockPanel.Controls.Add(lblDate);

            lblClock = new Label
            {
                Text = DateTime.Now.ToString("HH:mm:ss"),
                Font = new Font("DSEG7 Classic", 20, FontStyle.Bold),
                ForeColor = AppTheme.LEDGreen,
                Location = new Point(3, 18),
                Size = new Size(140, 40),
                TextAlign = ContentAlignment.MiddleCenter
            };
            clockPanel.Controls.Add(lblClock);

            System.Windows.Forms.Timer clockTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            clockTimer.Tick += (s, e) =>
            {
                lblClock.Text = DateTime.Now.ToString("HH:mm:ss");
                lblDate.Text = GetFormattedDate();
            };
            clockTimer.Start();

            Panel bottomRow = new Panel
            {
                Location = new Point(5, 80),
                Size = new Size(1390, 75),
                BackColor = Color.FromArgb(20, 20, 20)
            };
            this.Controls.Add(bottomRow);

            CreateControlButtons(bottomRow, 5);

            Panel waveformContainer = new Panel
            {
                Location = new Point(415, 5),
                Size = new Size(600, 65),
                BackColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle
            };
            bottomRow.Controls.Add(waveformContainer);

            waveformPanel = new DoubleBufferedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black
            };
            waveformPanel.Paint += WaveformPanel_Paint;
            waveformPanel.MouseClick += WaveformPanel_MouseClick;
            waveformPanel.MouseMove += WaveformPanel_MouseMove;
            waveformPanel.MouseEnter += (s, e) => _isHoveringWaveform = true;
            waveformPanel.MouseLeave += (s, e) => { _isHoveringWaveform = false; _hoverX = 0; waveformPanel.Invalidate(); };
            waveformContainer.Controls.Add(waveformPanel);

            Panel vuMeterContainer = new Panel
            {
                Location = new Point(1020, 5),
                Size = new Size(365, 65),
                BackColor = Color.FromArgb(25, 25, 25),
                BorderStyle = BorderStyle.FixedSingle
            };
            bottomRow.Controls.Add(vuMeterContainer);

            Label lblVULeft = new Label
            {
                Text = "L",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(8, 16),
                Size = new Size(15, 18)
            };
            vuMeterContainer.Controls.Add(lblVULeft);

            vuMeterLeftPanel = new DoubleBufferedPanel
            {
                Location = new Point(28, 18),
                Size = new Size(330, 15),
                BackColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle
            };
            vuMeterLeftPanel.Paint += VuMeterLeft_Paint;
            vuMeterContainer.Controls.Add(vuMeterLeftPanel);

            Label lblVURight = new Label
            {
                Text = "R",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(8, 40),
                Size = new Size(15, 18)
            };
            vuMeterContainer.Controls.Add(lblVURight);

            vuMeterRightPanel = new DoubleBufferedPanel
            {
                Location = new Point(28, 42),
                Size = new Size(330, 15),
                BackColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle
            };
            vuMeterRightPanel.Paint += VuMeterRight_Paint;
            vuMeterContainer.Controls.Add(vuMeterRightPanel);
        }

        private void LblArtist_Paint(object sender, PaintEventArgs e)
        {
            Label lbl = sender as Label;
            if (lbl == null || string.IsNullOrEmpty(lbl.Text))
                return;

            e.Graphics.Clear(lbl.Parent.BackColor);

            string text = System.Net.WebUtility.HtmlDecode(lbl.Text.Replace("&", "&&"));

            Font font = new Font("Segoe UI", 16, FontStyle.Bold);
            SizeF textSize = e.Graphics.MeasureString(text, font);

            float maxWidth = lbl.Width - 20;
            float fontSize = 16f;

            while (textSize.Width > maxWidth && fontSize > 8f)
            {
                fontSize -= 0.5f;
                font = new Font("Segoe UI", fontSize, FontStyle.Bold);
                textSize = e.Graphics.MeasureString(text, font);
            }

            using (SolidBrush brush = new SolidBrush(lbl.ForeColor))
            {
                StringFormat format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };

                e.Graphics.DrawString(text, font, brush, lbl.ClientRectangle, format);
            }

            font.Dispose();
        }

        private string GetFormattedDate()
        {
            CultureInfo italianCulture = new CultureInfo("it-IT");
            DateTime now = DateTime.Now;

            string dayName = italianCulture.DateTimeFormat.GetDayName(now.DayOfWeek);
            dayName = char.ToUpper(dayName[0]) + dayName.Substring(1);

            string monthName = italianCulture.DateTimeFormat.GetMonthName(now.Month);
            monthName = char.ToUpper(monthName[0]) + monthName.Substring(1);

            return $"{dayName} {now.Day:D2} {monthName}";
        }

        private void CreateTimer(Panel parent, int x, int y, string label, ref Label lblTimer, Color ledColor, int width, int height, ref Label headerLabelRef)
        {
            Panel timerPanel = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(width, height),
                BackColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle
            };
            parent.Controls.Add(timerPanel);

            Label lblLabel = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 7, FontStyle.Regular),
                ForeColor = Color.Gray,
                Location = new Point(3, 2),
                AutoSize = true
            };
            timerPanel.Controls.Add(lblLabel);

            if (label.Contains("trascorso") || label.Contains("Elapsed"))
                headerLabelRef = lblLabel;
            else if (label.Contains("restante") || label.Contains("Remaining"))
                headerLabelRef = lblLabel;

            lblTimer = new Label
            {
                Text = "--:--",
                Font = new Font("DSEG7 Classic", 24, FontStyle.Bold),
                ForeColor = ledColor,
                Location = new Point(3, 22),
                Size = new Size(width - 6, height - 24),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Black
            };
            timerPanel.Controls.Add(lblTimer);
        }

        private void CreateControlButtons(Panel parent, int startX)
        {
            int btnY = 8;
            int btnSize = 58;
            int spacing = 5;
            int extraSpacing = 8;

            btnPlay = new Button
            {
                Text = "▶",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                Size = new Size(btnSize, btnSize),
                Location = new Point(startX, btnY),
                BackColor = Color.FromArgb(80, 80, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnPlay.FlatAppearance.BorderSize = 0;
            btnPlay.Click += BtnPlay_Click;
            parent.Controls.Add(btnPlay);

            btnPause = new Button
            {
                Text = "⏸",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                Size = new Size(btnSize, btnSize),
                Location = new Point(startX + btnSize + spacing, btnY),
                BackColor = Color.FromArgb(80, 80, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnPause.FlatAppearance.BorderSize = 0;
            btnPause.Click += BtnPause_Click;
            parent.Controls.Add(btnPause);

            btnStop = new Button
            {
                Text = "⏹",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                Size = new Size(btnSize, btnSize),
                Location = new Point(startX + (btnSize + spacing) * 2, btnY),
                BackColor = Color.FromArgb(80, 80, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnStop.FlatAppearance.BorderSize = 0;
            btnStop.Click += BtnStop_Click;
            parent.Controls.Add(btnStop);

            Button btnAutoManual = new Button
            {
                Name = "btnAutoManual",
                Text = "AUTO",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Size = new Size(130, btnSize),
                Location = new Point(startX + (btnSize + spacing) * 3 + extraSpacing, btnY),
                BackColor = AppTheme.Success,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnAutoManual.FlatAppearance.BorderSize = 0;
            btnAutoManual.Click += BtnAutoManual_Click;
            parent.Controls.Add(btnAutoManual);

            Button btnNext = new Button
            {
                Text = "⏭",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                Size = new Size(btnSize, btnSize),
                Location = new Point(startX + (btnSize + spacing) * 3 + extraSpacing + 130 + spacing + extraSpacing, btnY),
                BackColor = AppTheme.Info,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnNext.FlatAppearance.BorderSize = 0;
            btnNext.Click += BtnNext_Click;
            parent.Controls.Add(btnNext);
        }

        private void InitializeTimer()
        {
            _updateTimer = new System.Windows.Forms.Timer { Interval = 20 };
            _updateTimer.Tick += UpdateTimer_Tick;

            _mixCheckTimer = new System.Windows.Forms.Timer { Interval = 50 };
            _mixCheckTimer.Tick += MixCheckTimer_Tick;

            _fadeOutTimer = new System.Windows.Forms.Timer { Interval = 20 };
            _fadeOutTimer.Tick += FadeOutTimer_Tick;

            _blinkTimer = new System.Windows.Forms.Timer { Interval = 500 };
            _blinkTimer.Tick += BlinkTimer_Tick;

            _streamDurationTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _streamDurationTimer.Tick += StreamDurationTimer_Tick;
        }

        private void BlinkTimer_Tick(object sender, EventArgs e)
        {
            _blinkState = !_blinkState;
            UpdateCounters();
        }

        private void StreamDurationTimer_Tick(object sender, EventArgs e)
        {
            if (!_isStreamingURL || !_isPlaying || _isPaused)
            {
                return;
            }

            TimeSpan elapsed = DateTime.Now - _streamStartTime;
            _currentPosition = elapsed;

            if (_vlcPlayer != null && !_vlcPlayer.IsPlaying)
            {
                _vlcPlayer.Play();
            }

            UpdateCounters();
            waveformPanel.Invalidate();
            vuMeterLeftPanel.Invalidate();
            vuMeterRightPanel.Invalidate();

            if (elapsed >= _streamScheduledDuration)
            {
                _streamDurationTimer.Stop();
                _isStreamingURL = false;

                if (_vlcPlayer != null && _vlcPlayer.IsPlaying)
                {
                    _vlcPlayer.Stop();
                }

                OnTrackEnded();
            }
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            if (_isStreamingURL)
            {
                return;
            }

            AudioFileReader activeAudio = _isPlayerAActive ? _audioFileA : _audioFileB;

            if (activeAudio != null && _isPlaying && !_isPaused)
            {
                _currentPosition = activeAudio.CurrentTime;
                UpdateCounters();
                waveformPanel.Invalidate();
                vuMeterLeftPanel.Invalidate();
                vuMeterRightPanel.Invalidate();

                if (_currentPosition >= _totalDuration.Subtract(TimeSpan.FromMilliseconds(200)))
                {
                    _updateTimer.Stop();
                    OnTrackEnded();
                }
            }
        }

        private void MixCheckTimer_Tick(object sender, EventArgs e)
        {
            if (_isStreamingURL)
            {
                return;
            }

            if (!_isPlaying || _isPaused || _mixRequested)
                return;

            AudioFileReader activeAudio = _isPlayerAActive ? _audioFileA : _audioFileB;
            if (activeAudio == null)
                return;

            int currentMs = (int)activeAudio.CurrentTime.TotalMilliseconds;

            if (_markerMIX > 0 && currentMs >= _markerMIX)
            {
                Log($"");
                Log($"╔════════════════════════════════════════════════════════════╗");
                Log($"║  MIX POINT TRIGGERED                                       ║");
                Log($"╚════════════════════════════════════════════════════════════╝");
                Log($"[MixCheckTimer] Player attivo: {(_isPlayerAActive ? "A" : "B")}");
                Log($"[MixCheckTimer] Posizione: {currentMs}ms");
                Log($"[MixCheckTimer] Marker MIX: {_markerMIX}ms");
                Log($"");

                _mixRequested = true;
                _mixCheckTimer.Stop();

                if (_autoMode)
                {
                    OnMixPointReached();
                }
            }
        }

        private void FadeOutTimer_Tick(object sender, EventArgs e)
        {
            if (_volumeFadingOut == null || _audioFadingOut == null)
            {
                _fadeOutTimer.Stop();
                return;
            }

            try
            {
                int currentMs = (int)_audioFadingOut.CurrentTime.TotalMilliseconds;

                if (currentMs >= _fadeOutEndMs)
                {
                    _volumeFadingOut.Volume = 0f;
                    _mixer.RemoveMixerInput(_volumeFadingOut);
                    _audioFadingOut.Dispose();

                    _volumeFadingOut = null;
                    _audioFadingOut = null;
                    _fadeOutTimer.Stop();
                }
                else
                {
                    int fadeRangeMs = _fadeOutEndMs - _fadeOutStartMs;
                    if (fadeRangeMs > 0)
                    {
                        int elapsedMs = currentMs - _fadeOutStartMs;
                        float progress = Math.Max(0f, Math.Min(1f, (float)elapsedMs / fadeRangeMs));
                        float volume = 1f - progress;
                        _volumeFadingOut.Volume = Math.Max(0f, Math.Min(1f, volume));
                    }
                }
            }
            catch
            {
                _fadeOutTimer.Stop();
                _volumeFadingOut = null;
                _audioFadingOut = null;
            }
        }

        private void OnMixPointReached()
        {
            if (_isStreamingURL)
                return;

            if (!_autoMode || _playlistQueue == null)
                return;

            var items = _playlistQueue.GetAllItems();

            if (items.Count > 1)
            {
                ClearWaveformImmediate();
                var nextItem = items[1];

                try
                {
                    bool nextIsStream = IsStreamUrl(nextItem.FilePath);

                    Log($"");
                    Log($"╔════════════════════════════════════════════════════════════╗");
                    Log($"║  MIX POINT REACHED                                         ║");
                    Log($"╚════════════════════════════════════════════════════════════╝");
                    Log($"[OnMixPointReached] Player attivo PRIMA del mix: {(_isPlayerAActive ? "A" : "B")}");
                    Log($"[OnMixPointReached] Prossimo:  {nextItem.Title}");
                    Log($"[OnMixPointReached] È stream?  {nextIsStream}");
                    Log($"");

                    VolumeSampleProvider oldVolumeProvider = _isPlayerAActive ? _volumeProviderA : _volumeProviderB;
                    AudioFileReader oldAudio = _isPlayerAActive ? _audioFileA : _audioFileB;

                    if (!nextIsStream)
                    {
                        _fadeOutStartMs = _markerMIX;
                        _fadeOutEndMs = _markerOUT > 0 ? _markerOUT : (int)oldAudio.TotalTime.TotalMilliseconds;

                        _volumeFadingOut = oldVolumeProvider;
                        _audioFadingOut = oldAudio;
                        _fadeOutTimer.Start();

                        Log($"[OnMixPointReached] Fade out:  da {_fadeOutStartMs}ms a {_fadeOutEndMs}ms");
                    }
                    else
                    {
                        if (oldVolumeProvider != null)
                        {
                            _mixer.RemoveMixerInput(oldVolumeProvider);
                        }
                        oldAudio?.Dispose();

                        Log($"[OnMixPointReached] Stop immediato per stream");
                    }

                    if (nextIsStream)
                    {
                        Log($"[OnMixPointReached] 🌐 Configurazione stream VLC...");

                        _isStreamingURL = true;
                        _streamScheduledDuration = nextItem.Duration;
                        _streamStartTime = DateTime.Now;
                        _currentPosition = TimeSpan.Zero;
                        _totalDuration = nextItem.Duration;

                        LoadTrackInfo(nextItem);

                        if (_vlcPlayer != null)
                        {
                            var media = new Media(_libVLC, new Uri(nextItem.FilePath));
                            media.AddOption(": no-video");
                            media.AddOption(": network-caching=10000");
                            media.AddOption(": live-caching=10000");

                            _vlcPlayer.Media = media;
                            _vlcPlayer.Play();
                        }

                        if (_isPlayerAActive)
                        {
                            _volumeProviderA = null;
                            _audioFileA = null;
                        }
                        else
                        {
                            _volumeProviderB = null;
                            _audioFileB = null;
                        }

                        _isPlayerAActive = !_isPlayerAActive;

                        _playlistQueue.RemoveItem(0);
                        _playlistQueue.SetCurrentPlaying(0);

                        _mixCheckTimer.Stop();
                        _updateTimer.Stop();
                        _streamDurationTimer.Start();

                        UpdateCounters();

                        Log($"[OnMixPointReached] Player attivo DOPO il mix:  {(_isPlayerAActive ? "A" : "B")}");
                        Log($"[OnMixPointReached] ✅ Stream configurato");
                    }
                    else
                    {
                        Log($"[OnMixPointReached] 🎵 Caricamento file audio normale...");

                        if (_isPlayerAActive)
                        {
                            Log($"[OnMixPointReached] Carico su Player B");

                            if (_volumeProviderB != null)
                            {
                                _mixer.RemoveMixerInput(_volumeProviderB);
                            }
                            _audioFileB?.Dispose();

                            _audioFileB = new AudioFileReader(nextItem.FilePath);

                            if (nextItem.MarkerIN > 0)
                            {
                                _audioFileB.CurrentTime = TimeSpan.FromMilliseconds(nextItem.MarkerIN);
                                Log($"[OnMixPointReached] Player B: Marker IN = {nextItem.MarkerIN}ms");
                            }

                            // USA RESAMPLING
                            var sampleProvider = CreateResampledProvider(_audioFileB);

                            _volumeProviderB = new VolumeSampleProvider(sampleProvider);
                            _volumeProviderB.Volume = 1f;
                            _mixer.AddMixerInput(_volumeProviderB);

                            _volumeProviderA = null;
                            _audioFileA = null;
                        }
                        else
                        {
                            Log($"[OnMixPointReached] Carico su Player A");

                            if (_volumeProviderA != null)
                            {
                                _mixer.RemoveMixerInput(_volumeProviderA);
                            }
                            _audioFileA?.Dispose();

                            _audioFileA = new AudioFileReader(nextItem.FilePath);

                            if (nextItem.MarkerIN > 0)
                            {
                                _audioFileA.CurrentTime = TimeSpan.FromMilliseconds(nextItem.MarkerIN);
                                Log($"[OnMixPointReached] Player A: Marker IN = {nextItem.MarkerIN}ms");
                            }

                            // USA RESAMPLING
                            var sampleProvider = CreateResampledProvider(_audioFileA);

                            _volumeProviderA = new VolumeSampleProvider(sampleProvider);
                            _volumeProviderA.Volume = 1f;
                            _mixer.AddMixerInput(_volumeProviderA);

                            _volumeProviderB = null;
                            _audioFileB = null;
                        }

                        _isPlayerAActive = !_isPlayerAActive;

                        _playlistQueue.RemoveItem(0);
                        _playlistQueue.SetCurrentPlaying(0);

                        LoadTrackInfo(nextItem);

                        _mixRequested = false;
                        _mixCheckTimer.Start();

                        Log($"[OnMixPointReached] Player attivo DOPO il mix:  {(_isPlayerAActive ? "A" : "B")}");
                        Log($"[OnMixPointReached] Nuovo Marker MIX: {_markerMIX}ms");
                        Log($"[OnMixPointReached] ✅ File audio caricato e MixCheckTimer riavviato");
                    }

                    MixPointReached?.Invoke(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    Log($"[OnMixPointReached] ❌ ERRORE: {ex.Message}");
                    Log($"[OnMixPointReached] StackTrace: {ex.StackTrace}");
                    MessageBox.Show($"Errore mix:  {ex.Message}", "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private bool IsStreamUrl(string filePath)
        {
            return filePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                   filePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        }

        private void LoadTrackInfo(PlaylistQueueItem item)
        {
            _isStreamingURL = IsStreamUrl(item.FilePath);

            // Aggiorna proprietà per RadioTV Bridge
            CurrentFilePath = item.FilePath;
            CurrentArtist = item.Artist;
            CurrentTitle = item.Title;

            if (_isStreamingURL)
            {
                _totalDuration = item.Duration;
                _introTime = TimeSpan.Zero;
                _currentPosition = TimeSpan.Zero;
                _streamScheduledDuration = item.Duration;
                _streamStartTime = DateTime.Now;

                _markerIN = 0;
                _markerINTRO = 0;
                _markerMIX = 0;
                _markerOUT = 0;
                _mixRequested = false;

                UpdateCounters();

                Log($"[LoadTrackInfo] 🌐 Stream:  {item.Title}");
                Log($"[LoadTrackInfo] Durata: {_totalDuration}");
            }
            else
            {
                AudioFileReader activeAudio = _isPlayerAActive ? _audioFileA : _audioFileB;

                _totalDuration = activeAudio.TotalTime;
                _introTime = item.Intro;
                _currentPosition = activeAudio.CurrentTime;

                _markerIN = item.MarkerIN;
                _markerINTRO = item.MarkerINTRO;
                _markerMIX = item.MarkerMIX > 0 ? item.MarkerMIX : (int)_totalDuration.TotalMilliseconds;
                _markerOUT = item.MarkerOUT > 0 ? item.MarkerOUT : (int)_totalDuration.TotalMilliseconds;
                _mixRequested = false;

                Log($"[LoadTrackInfo] 🎵 File: {item.Artist} - {item.Title}");
                Log($"[LoadTrackInfo] Player: {(_isPlayerAActive ? "A" : "B")}");
                Log($"[LoadTrackInfo] Marker IN: {_markerIN}ms");
                Log($"[LoadTrackInfo] Marker MIX: {_markerMIX}ms");
                Log($"[LoadTrackInfo] Marker OUT: {_markerOUT}ms");
                Log($"[LoadTrackInfo] Durata totale: {_totalDuration}");
            }

            string displayText = string.IsNullOrEmpty(item.Artist)
                ? item.Title.ToUpper()
                : $"{item.Artist.ToUpper()} - {item.Title.ToUpper()}";

            displayText = displayText.Replace("&&", "&");

            lblArtist.Text = displayText;
            lblArtist.Invalidate();

            lblIntro.Text = item.Intro.ToString(@"mm\:ss");
            lblIntro.ForeColor = Color.White;
            lblIntro.BackColor = _isStreamingURL ? Color.Blue : Color.Red;

            UpdateCounters();

            try
            {
                string metadataSource = ConfigurationControl.GetMetadataSource();
                bool shouldSendMetadata = false;

                if (metadataSource == "MusicOnly" && item.ItemType == "Music")
                {
                    shouldSendMetadata = true;
                }
                else if (metadataSource == "MusicAndClips")
                {
                    shouldSendMetadata = true;
                }

                if (shouldSendMetadata)
                {
                    AirDirector.Services.MetadataManager.UpdateMetadata(item.Artist ?? "", item.Title ?? "", item.ItemType);
                }
            }
            catch { }

            if (!_isStreamingURL && !_isLoadingWaveform && !string.IsNullOrEmpty(item.FilePath))
            {
                Task.Delay(50).ContinueWith(_ => GenerateWaveformAsync(item.FilePath));
            }

            // NOTIFICA RADIOTV BRIDGE
            TrackChanged?.Invoke(this, new TrackChangedEventArgs
            {
                FilePath = item.FilePath,
                Artist = item.Artist,
                Title = item.Title,
                IsVideo = IsVideoFile(item.FilePath),
                ItemType = item.ItemType,
                // ✅ NUOVO: Video associato
                VideoFilePath = item.VideoFilePath ?? "",
                VideoSource = item.VideoSource ?? "",
                NDISourceName = item.NDISourceName ?? "",
                // passa i marker
                MarkerIN = item.MarkerIN,
                MarkerMIX = item.MarkerMIX,
                MarkerOUT = item.MarkerOUT
            });
        }

        /// <summary>
        /// Verifica se il file è un video
        /// </summary>
        private bool IsVideoFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;
            if (IsStreamUrl(filePath)) return false;

            string ext = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
            return ext == ".mp4" || ext == ".avi" || ext == ".mov" ||
                   ext == ".mkv" || ext == ".wmv" || ext == ".webm" || ext == ".m4v";
        }

        private void UpdateCounters()
        {
            if (_isStreamingURL)
            {
                TimeSpan elapsed = _currentPosition;
                lblElapsed.Text = elapsed.ToString(@"mm\:ss");

                TimeSpan remaining = _streamScheduledDuration - elapsed;
                if (remaining.TotalSeconds < 0)
                    remaining = TimeSpan.Zero;

                lblRemaining.Text = "-" + remaining.ToString(@"mm\:ss");

                lblIntro.Text = "";
                lblIntro.BackColor = Color.Black;

                return;
            }

            TimeSpan elapsedFromMarkerIN = _currentPosition - TimeSpan.FromMilliseconds(_markerIN);
            if (elapsedFromMarkerIN.TotalSeconds < 0)
                elapsedFromMarkerIN = TimeSpan.Zero;

            lblElapsed.Text = elapsedFromMarkerIN.ToString(@"mm\:ss");

            TimeSpan remainingToMix = TimeSpan.FromMilliseconds(_markerMIX) - _currentPosition;
            if (remainingToMix.TotalSeconds < 0)
                remainingToMix = TimeSpan.Zero;

            lblRemaining.Text = "-" + remainingToMix.ToString(@"mm\:ss");

            if (remainingToMix.TotalSeconds > 0 && remainingToMix.TotalSeconds <= 10)
            {
                if (!_blinkTimer.Enabled)
                    _blinkTimer.Start();

                if (_blinkState)
                {
                    lblRemaining.BackColor = Color.Red;
                    lblRemaining.ForeColor = Color.Black;
                }
                else
                {
                    lblRemaining.BackColor = Color.Black;
                    lblRemaining.ForeColor = AppTheme.LEDRed;
                }
            }
            else
            {
                _blinkTimer.Stop();
                lblRemaining.BackColor = Color.Black;
                lblRemaining.ForeColor = AppTheme.LEDRed;
            }

            TimeSpan introRemaining = _introTime - _currentPosition;
            if (introRemaining.TotalSeconds > 0)
            {
                lblIntro.Text = introRemaining.ToString(@"mm\:ss");
                lblIntro.ForeColor = Color.White;
                lblIntro.BackColor = Color.Red;
            }
            else
            {
                lblIntro.Text = "";
                lblIntro.BackColor = Color.Black;
            }
        }


        private volatile bool _waveformGenerating = false;
        private volatile string _waveformRequestedFile = "";
        private readonly object _waveformStateLock = new object();
        private Thread _waveformThread = null;
        private volatile bool _waveformAbort = false;

        /// <summary>
        /// Genera la waveform per un file audio/video
        /// </summary>
        private void GenerateWaveformAsync(string filePath)
        {
            // Validazione
            if (string.IsNullOrEmpty(filePath) || IsStreamUrl(filePath))
            {
                ClearWaveformDisplay();
                return;
            }

            if (!File.Exists(filePath))
            {
                ClearWaveformDisplay();
                return;
            }

            lock (_waveformStateLock)
            {
                // Se stiamo già generando per lo stesso file, esci
                if (_waveformGenerating && _waveformRequestedFile == filePath)
                    return;

                // Se c'è già una waveform per questo file, esci
                if (_lastLoadedFile == filePath && _waveformBitmap != null && !IsImageDisposed(_waveformBitmap))
                    return;

                // Abort qualsiasi generazione in corso
                _waveformAbort = true;

                // Aspetta che il thread precedente termini (max 500ms)
                if (_waveformThread != null && _waveformThread.IsAlive)
                {
                    _waveformThread.Join(500);
                }

                // Pulisci waveform precedente
                lock (_waveformLock)
                {
                    try { _waveformBitmap?.Dispose(); } catch { }
                    _waveformBitmap = null;
                    _waveformData = null;
                }

                // Imposta nuovo stato
                _waveformAbort = false;
                _waveformGenerating = true;
                _waveformRequestedFile = filePath;
                _lastLoadedFile = filePath;
                _isLoadingWaveform = true;
            }

            // Mostra "Loading..."
            SafeInvalidateWaveform();

            // Crea copia locale del path
            string localPath = filePath;

            // Avvia thread dedicato (non Task, per maggiore controllo)
            _waveformThread = new Thread(() => WaveformGeneratorThread(localPath))
            {
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal,
                Name = "WaveformGenerator"
            };
            _waveformThread.Start();
        }

        private void WaveformGeneratorThread(string filePath)
        {
            try
            {
                if (_waveformAbort || string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    FinishWaveformGeneration();
                    return;
                }

                // ═══════════════════════════════════════════════════════════
                // PASSATA 1: Waveform veloce (300 punti)
                // ═══════════════════════════════════════════════════════════
                float[] quickData = GenerateWaveformData(filePath, 300);

                if (_waveformAbort)
                {
                    FinishWaveformGeneration();
                    return;
                }

                if (quickData != null && quickData.Length > 0)
                {
                    _waveformData = quickData;
                    CreateWaveformBitmap();
                    SafeInvalidateWaveform();
                }

                if (_waveformAbort)
                {
                    FinishWaveformGeneration();
                    return;
                }

                // ═══════════════════════════════════════════════════════════
                // PASSATA 2: Waveform dettagliata (800 punti)
                // ═══════════════════════════════════════════════════════════
                float[] detailedData = GenerateWaveformData(filePath, 800);

                if (_waveformAbort)
                {
                    FinishWaveformGeneration();
                    return;
                }

                if (detailedData != null && detailedData.Length > 0)
                {
                    _waveformData = detailedData;
                    CreateWaveformBitmap();
                    SafeInvalidateWaveform();
                }

                FinishWaveformGeneration();
            }
            catch (Exception ex)
            {
                Log($"[Waveform] ⚠️ Errore:  {ex.Message}");
                FinishWaveformGeneration();
            }
        }

        /// <summary>
        /// Termina la generazione waveform
        /// </summary>
        private void FinishWaveformGeneration()
        {
            lock (_waveformStateLock)
            {
                _waveformGenerating = false;
                _isLoadingWaveform = false;
            }
            SafeInvalidateWaveform();
        }

        /// <summary>
        /// Pulisce la visualizzazione waveform
        /// </summary>
        private void ClearWaveformDisplay()
        {
            lock (_waveformStateLock)
            {
                _waveformAbort = true;
                _lastLoadedFile = "";
            }

            lock (_waveformLock)
            {
                try { _waveformBitmap?.Dispose(); } catch { }
                _waveformBitmap = null;
                _waveformData = null;
            }

            _isLoadingWaveform = false;
            SafeInvalidateWaveform();
        }

        /// <summary>
        /// Genera i dati waveform da un file
        /// </summary>
        private float[] GenerateWaveformData(string filePath, int targetPoints)
        {
            if (_waveformAbort) return null;
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return null;

            float[] data = new float[targetPoints];

            // Metodo 1: AudioFileReader
            bool success = TryGenerateWaveformAudioReader(filePath, data);

            // Metodo 2: MediaFoundationReader (fallback per video)
            if (!success && !_waveformAbort)
            {
                success = TryGenerateWaveformMediaFoundation(filePath, data);
            }

            // Se entrambi falliscono, genera waveform placeholder
            if (!success && !_waveformAbort)
            {
                for (int i = 0; i < targetPoints; i++)
                {
                    data[i] = 0.05f;
                }
            }

            return _waveformAbort ? null : data;
        }

        private bool TryGenerateWaveformAudioReader(string filePath, float[] data)
        {
            AudioFileReader reader = null;

            try
            {
                if (!File.Exists(filePath))
                    return false;

                reader = new AudioFileReader(filePath);

                if (_waveformAbort) { reader?.Dispose(); return false; }

                var format = reader.WaveFormat;
                if (format == null) { reader?.Dispose(); return false; }

                long totalBytes = reader.Length;
                if (totalBytes <= 0) { reader?.Dispose(); return false; }

                int blockAlign = format.BlockAlign > 0 ? format.BlockAlign : 4;
                int targetPoints = data.Length;
                float[] buffer = new float[1024];

                int consecutiveErrors = 0;

                for (int i = 0; i < targetPoints; i++)
                {
                    if (_waveformAbort) { reader?.Dispose(); return false; }

                    try
                    {
                        long pos = (long)i * totalBytes / targetPoints;
                        pos = (pos / blockAlign) * blockAlign;
                        pos = Math.Max(0, Math.Min(pos, totalBytes - blockAlign * 2));

                        if (pos < 0 || pos >= totalBytes)
                        {
                            data[i] = i > 0 ? data[i - 1] : 0f;
                            continue;
                        }

                        try { reader.Position = pos; }
                        catch
                        {
                            data[i] = i > 0 ? data[i - 1] : 0f;
                            if (++consecutiveErrors >= 10) { reader?.Dispose(); return false; }
                            continue;
                        }

                        int read = 0;
                        try { read = reader.Read(buffer, 0, buffer.Length); }
                        catch
                        {
                            data[i] = i > 0 ? data[i - 1] : 0f;
                            if (++consecutiveErrors >= 10) { reader?.Dispose(); return false; }
                            continue;
                        }

                        consecutiveErrors = 0;

                        if (read > 0)
                        {
                            float max = 0f;
                            for (int j = 0; j < read; j++)
                            {
                                float abs = Math.Abs(buffer[j]);
                                if (abs > max) max = abs;
                            }
                            data[i] = max;
                        }
                        else
                        {
                            data[i] = i > 0 ? data[i - 1] : 0f;
                        }
                    }
                    catch
                    {
                        data[i] = i > 0 ? data[i - 1] : 0f;
                        if (++consecutiveErrors >= 10) { reader?.Dispose(); return false; }
                    }
                }

                reader?.Dispose();
                return true;
            }
            catch
            {
                try { reader?.Dispose(); } catch { }
                return false;
            }
        }

        /// <summary>
        /// Genera waveform con MediaFoundationReader (per video)
        /// </summary>
        private bool TryGenerateWaveformMediaFoundation(string filePath, float[] data)
        {
            MediaFoundationReader reader = null;

            try
            {
                reader = new MediaFoundationReader(filePath);

                if (_waveformAbort) { reader?.Dispose(); return false; }

                var format = reader.WaveFormat;
                if (format == null) { reader?.Dispose(); return false; }

                ISampleProvider samples = null;
                try { samples = reader.ToSampleProvider(); }
                catch { reader?.Dispose(); return false; }

                if (samples == null) { reader?.Dispose(); return false; }

                int targetPoints = data.Length;
                double totalSec = Math.Max(0.1, reader.TotalTime.TotalSeconds);
                int sampleRate = format.SampleRate > 0 ? format.SampleRate : 44100;
                int channels = format.Channels > 0 ? format.Channels : 2;
                long totalSamples = (long)(totalSec * sampleRate * channels);
                int samplesPerPoint = Math.Max(1, (int)(totalSamples / targetPoints));

                float[] buffer = new float[2048];
                int pointIndex = 0;
                float currentMax = 0f;
                int samplesInPoint = 0;

                while (!_waveformAbort && pointIndex < targetPoints)
                {
                    int read = 0;
                    try { read = samples.Read(buffer, 0, buffer.Length); }
                    catch { break; }

                    if (read <= 0) break;

                    for (int i = 0; i < read && pointIndex < targetPoints; i++)
                    {
                        if (_waveformAbort) break;

                        float abs = Math.Abs(buffer[i]);
                        if (abs > currentMax) currentMax = abs;

                        samplesInPoint++;
                        if (samplesInPoint >= samplesPerPoint)
                        {
                            data[pointIndex++] = currentMax;
                            currentMax = 0f;
                            samplesInPoint = 0;
                        }
                    }
                }

                // Riempi punti mancanti
                float lastVal = pointIndex > 0 ? data[pointIndex - 1] : 0f;
                while (pointIndex < targetPoints)
                {
                    data[pointIndex++] = lastVal * 0.8f;
                    lastVal *= 0.8f;
                }

                reader.Dispose();
                return !_waveformAbort;
            }
            catch
            {
                try { reader?.Dispose(); } catch { }
                return false;
            }
        }

        /// <summary>
        /// Crea la bitmap della waveform
        /// </summary>
        private void CreateWaveformBitmap()
        {
            if (_waveformAbort) return;
            if (_waveformData == null || _waveformData.Length == 0) return;

            int width = 600, height = 65;

            try
            {
                if (waveformPanel != null && !waveformPanel.IsDisposed)
                {
                    if (waveformPanel.InvokeRequired)
                    {
                        waveformPanel.Invoke(new Action(() =>
                        {
                            if (!waveformPanel.IsDisposed)
                            {
                                width = waveformPanel.Width;
                                height = waveformPanel.Height;
                            }
                        }));
                    }
                    else
                    {
                        width = waveformPanel.Width;
                        height = waveformPanel.Height;
                    }
                }
            }
            catch { }

            if (width <= 0) width = 600;
            if (height <= 0) height = 65;

            if (_waveformAbort) return;

            Bitmap bmp = null;

            try
            {
                bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Black);

                    if (_waveformAbort) { bmp.Dispose(); return; }

                    int centerY = height / 2;
                    float xStep = (float)width / _waveformData.Length;

                    // Calcola posizioni marker
                    int introX = 0, mixX = width;
                    if (_totalDuration.TotalMilliseconds > 0)
                    {
                        if (_introTime.TotalMilliseconds > 0)
                            introX = (int)((_introTime.TotalMilliseconds / _totalDuration.TotalMilliseconds) * width);
                        if (_markerMIX > 0)
                            mixX = (int)((_markerMIX / _totalDuration.TotalMilliseconds) * width);
                    }

                    // Disegna waveform
                    using (Pen redPen = new Pen(Color.FromArgb(255, 80, 80), 1))
                    using (Pen greenPen = new Pen(Color.FromArgb(0, 200, 0), 1))
                    using (Pen grayPen = new Pen(Color.FromArgb(120, 120, 120), 1))
                    {
                        for (int i = 0; i < _waveformData.Length && !_waveformAbort; i++)
                        {
                            int x = (int)(i * xStep);
                            int amp = Math.Max(1, (int)(_waveformData[i] * (centerY - 2)));

                            Pen pen = x < introX ? redPen : (x < mixX ? greenPen : grayPen);
                            g.DrawLine(pen, x, centerY - amp, x, centerY + amp);
                        }
                    }

                    if (_waveformAbort) { bmp.Dispose(); return; }

                    // Marker IN
                    if (_markerIN > 0 && _totalDuration.TotalMilliseconds > 0)
                    {
                        int mx = (int)((_markerIN / _totalDuration.TotalMilliseconds) * width);
                        if (mx > 0 && mx < width)
                            using (Pen p = new Pen(Color.White, 2)) g.DrawLine(p, mx, 0, mx, height);
                    }

                    // Intro line
                    if (introX > 0 && introX < width)
                        using (Pen p = new Pen(Color.Yellow, 1)) g.DrawLine(p, introX, 0, introX, height);

                    // Mix line
                    if (mixX > 0 && mixX < width)
                        using (Pen p = new Pen(Color.Yellow, 2)) g.DrawLine(p, mixX, 0, mixX, height);

                    // Center line
                    using (Pen p = new Pen(Color.FromArgb(40, 255, 255, 255), 1))
                        g.DrawLine(p, 0, centerY, width, centerY);
                }

                if (_waveformAbort) { bmp.Dispose(); return; }

                // Assegna nuova bitmap
                lock (_waveformLock)
                {
                    try { _waveformBitmap?.Dispose(); } catch { }
                    _waveformBitmap = bmp;
                    bmp = null;
                }
            }
            catch
            {
                try { bmp?.Dispose(); } catch { }
            }
        }

        /// <summary>
        /// Invalida il pannello waveform in modo sicuro
        /// </summary>
        private void SafeInvalidateWaveform()
        {
            try
            {
                if (waveformPanel == null || waveformPanel.IsDisposed || !waveformPanel.IsHandleCreated)
                    return;

                if (waveformPanel.InvokeRequired)
                {
                    waveformPanel.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            if (!waveformPanel.IsDisposed)
                                waveformPanel.Invalidate();
                        }
                        catch { }
                    }));
                }
                else
                {
                    waveformPanel.Invalidate();
                }
            }
            catch { }
        }


        private void WaveformPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighSpeed;

            int width = waveformPanel.Width;
            int height = waveformPanel.Height;

            Bitmap bitmapToDraw = null;

            lock (_waveformLock)
            {
                if (_waveformBitmap != null && !IsImageDisposed(_waveformBitmap))
                {
                    bitmapToDraw = _waveformBitmap;
                }
            }

            if (bitmapToDraw != null)
            {
                try
                {
                    g.DrawImage(bitmapToDraw, 0, 0);
                }
                catch (ArgumentException)
                {
                    g.Clear(Color.Black);
                }
            }
            else
            {
                g.Clear(Color.Black);

                if (_isLoadingWaveform)
                {
                    using (Font font = new Font("Segoe UI", 10, FontStyle.Bold))
                    using (SolidBrush brush = new SolidBrush(Color.Gray))
                    {
                        string loadingText = "Loading...";
                        SizeF textSize = g.MeasureString(loadingText, font);
                        float x = (width - textSize.Width) / 2;
                        float y = (height - textSize.Height) / 2;
                        g.DrawString(loadingText, font, brush, x, y);
                    }
                }
                else if (_isStreamingURL)
                {
                    using (Font font = new Font("Segoe UI", 10, FontStyle.Bold))
                    using (SolidBrush brush = new SolidBrush(Color.Cyan))
                    {
                        string streamText = "> WEB STREAMING <";
                        SizeF textSize = g.MeasureString(streamText, font);
                        float x = (width - textSize.Width) / 2;
                        float y = (height - textSize.Height) / 2;
                        g.DrawString(streamText, font, brush, x, y);
                    }
                }
            }

            if (_totalDuration.TotalSeconds > 0)
            {
                int progressWidth = (int)((_currentPosition.TotalSeconds / _totalDuration.TotalSeconds) * width);

                using (SolidBrush progressBrush = new SolidBrush(Color.FromArgb(200, 255, 0, 0)))
                    g.FillRectangle(progressBrush, 0, 0, progressWidth, 5);
            }

            if (_totalDuration.TotalSeconds > 0 && _isPlaying)
            {
                int cursorX = (int)((_currentPosition.TotalSeconds / _totalDuration.TotalSeconds) * width);

                using (Pen cursorPen = new Pen(Color.Red, 3))
                    g.DrawLine(cursorPen, cursorX, 0, cursorX, height);
            }

            if (_isHoveringWaveform && ModifierKeys.HasFlag(Keys.Control) && _hoverX > 0 && _totalDuration.TotalSeconds > 0 && !_isStreamingURL)
            {
                using (Pen previewPen = new Pen(Color.FromArgb(150, 255, 255, 255), 2))
                {
                    previewPen.DashStyle = DashStyle.Dash;
                    g.DrawLine(previewPen, _hoverX, 0, _hoverX, height);
                }

                double hoverPercentage = (double)_hoverX / width;
                TimeSpan hoverTime = TimeSpan.FromSeconds(_totalDuration.TotalSeconds * hoverPercentage);

                using (Font font = new Font("Segoe UI", 8, FontStyle.Bold))
                using (SolidBrush brush = new SolidBrush(Color.White))
                using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
                {
                    string timeText = hoverTime.ToString(@"mm\:ss");
                    SizeF textSize = g.MeasureString(timeText, font);

                    int textX = _hoverX - (int)(textSize.Width / 2);
                    textX = Math.Max(2, Math.Min(width - (int)textSize.Width - 2, textX));

                    g.FillRectangle(bgBrush, textX - 2, 8, textSize.Width + 4, textSize.Height + 2);
                    g.DrawString(timeText, font, brush, textX, 9);
                }
            }
        }

        private bool IsImageDisposed(Image image)
        {
            try
            {
                var _ = image.Width;
                return false;
            }
            catch (ArgumentException)
            {
                return true;
            }
            catch
            {
                return true;
            }
        }

        private void WaveformPanel_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && ModifierKeys.HasFlag(Keys.Control) && !_isStreamingURL)
            {
                AudioFileReader activeAudio = _isPlayerAActive ? _audioFileA : _audioFileB;

                if (activeAudio != null && _totalDuration.TotalSeconds > 0)
                {
                    try
                    {
                        int width = waveformPanel.Width;
                        double clickPercentage = (double)e.X / width;
                        clickPercentage = Math.Max(0, Math.Min(1, clickPercentage));

                        TimeSpan newPosition = TimeSpan.FromSeconds(_totalDuration.TotalSeconds * clickPercentage);
                        SeekTo(newPosition);

                        waveformPanel.Invalidate();
                    }
                    catch { }
                }
            }
        }

        private void WaveformPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (ModifierKeys.HasFlag(Keys.Control) && !_isStreamingURL)
            {
                _hoverX = e.X;
                waveformPanel.Cursor = Cursors.Hand;
                waveformPanel.Invalidate();
            }
            else
            {
                waveformPanel.Cursor = Cursors.Default;
                if (_hoverX != 0)
                {
                    _hoverX = 0;
                    waveformPanel.Invalidate();
                }
            }
        }

        public void SeekTo(TimeSpan position)
        {
            if (_isStreamingURL)
                return;

            AudioFileReader activeAudio = _isPlayerAActive ? _audioFileA : _audioFileB;

            if (activeAudio == null)
                return;

            try
            {
                if (position < TimeSpan.Zero)
                    position = TimeSpan.Zero;
                if (position > _totalDuration)
                    position = _totalDuration;

                activeAudio.CurrentTime = position;
                _currentPosition = position;

                UpdateCounters();
                waveformPanel.Invalidate();

            }
            catch { }
        }

        private void VuMeterLeft_Paint(object sender, PaintEventArgs e)
        {
            DrawHorizontalVUMeter(e.Graphics, vuMeterLeftPanel, _isPlaying, _lastVULevelLeft);
        }

        private void VuMeterRight_Paint(object sender, PaintEventArgs e)
        {
            DrawHorizontalVUMeter(e.Graphics, vuMeterRightPanel, _isPlaying, _lastVULevelRight);
        }

        private void DrawHorizontalVUMeter(Graphics g, Panel panel, bool isActive, float targetLevel)
        {
            int width = panel.Width;
            int height = panel.Height;
            g.FillRectangle(Brushes.Black, 0, 0, width, height);

            if (!isActive)
            {
                if (panel == vuMeterLeftPanel)
                    _smoothedVULevelLeft = 0f;
                else
                    _smoothedVULevelRight = 0f;
                return;
            }

            float currentSmoothed = (panel == vuMeterLeftPanel) ? _smoothedVULevelLeft : _smoothedVULevelRight;

            float attackSpeed = 0.7f;
            float releaseSpeed = 0.15f;

            float smoothingFactor;
            if (targetLevel > currentSmoothed)
            {
                smoothingFactor = attackSpeed;
            }
            else
            {
                smoothingFactor = releaseSpeed;
            }

            float level = currentSmoothed + (targetLevel - currentSmoothed) * smoothingFactor;
            level = Math.Max(0f, Math.Min(1f, level));

            if (panel == vuMeterLeftPanel)
                _smoothedVULevelLeft = level;
            else
                _smoothedVULevelRight = level;

            int barWidth = (int)(width * level);

            if (barWidth > 0)
            {
                Color color1 = AppTheme.VUGreen;
                Color color2 = level > 0.85f ? AppTheme.VURed : level > 0.7f ? AppTheme.VUYellow : AppTheme.VUGreen;

                using (LinearGradientBrush brush = new LinearGradientBrush(
                    new Rectangle(0, 0, width, height),
                    color1, color2, LinearGradientMode.Horizontal))
                {
                    g.FillRectangle(brush, new Rectangle(0, 0, barWidth, height));
                }

                if (barWidth > 2)
                {
                    using (Pen glowPen = new Pen(Color.FromArgb(150, Color.White), 2))
                    {
                        g.DrawLine(glowPen, barWidth - 1, 0, barWidth - 1, height);
                    }
                }
            }

            using (Pen markerPen = new Pen(Color.Gray, 1))
            {
                for (int i = 1; i < 10; i++)
                {
                    int x = (width * i) / 10;
                    g.DrawLine(markerPen, x, 0, x, height);
                }
            }

            using (Font font = new Font("Arial", 6))
            using (SolidBrush brush = new SolidBrush(Color.White))
            {
                g.DrawString("-20", font, brush, 2, 1);
                g.DrawString("-10", font, brush, width / 2 - 10, 1);
                g.DrawString("-3", font, brush, (width * 8) / 10 - 8, 1);
                g.DrawString("0dB", font, brush, width - 20, 1);
            }
        }

        private void UpdateButtonStates()
        {
            btnPlay.BackColor = _isPlaying && !_isPaused ? AppTheme.Success : Color.FromArgb(80, 80, 80);
            btnPause.BackColor = _isPaused ? AppTheme.Warning : Color.FromArgb(80, 80, 80);
            btnStop.BackColor = !_isPlaying && !_isPaused ? AppTheme.Danger : Color.FromArgb(80, 80, 80);
        }

        private void BtnPlay_Click(object sender, EventArgs e)
        {
            if (_isPaused)
            {
                Resume();
            }
            else
            {
                if (_playlistQueue != null && _playlistQueue.GetItemCount() > 0)
                {
                    var items = _playlistQueue.GetAllItems();
                    if (items.Count > 0)
                    {
                        var firstItem = items[0];

                        LoadTrack(
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

                        Play();
                        _playlistQueue.SetCurrentPlaying(0);
                    }
                    else
                    {
                        MessageBox.Show("La playlist queue è vuota!", "Attenzione",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Nessun file in playlist queue!", "Attenzione",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            UpdateButtonStates();
            PlayRequested?.Invoke(this, EventArgs.Empty);
        }

        private void BtnPause_Click(object sender, EventArgs e)
        {
            Pause();
            UpdateButtonStates();
            PauseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            Stop();
            UpdateButtonStates();
            StopRequested?.Invoke(this, EventArgs.Empty);
        }

        public void SetManualMode()
        {
            _autoMode = false;
            var btnAutoManual = this.Controls.Find("btnAutoManual", true).FirstOrDefault() as Button;
            if (btnAutoManual != null)
            {
                btnAutoManual.Text = "MANUAL";
                btnAutoManual.BackColor = AppTheme.Warning;
            }
            UpdateButtonStates();
        }

        public void SetAutoMode(bool auto)
        {
            _autoMode = auto;
            var btnAutoManual = this.Controls.Find("btnAutoManual", true).FirstOrDefault() as Button;
            if (btnAutoManual != null)
            {
                btnAutoManual.Text = auto ? "AUTO" : "MANUAL";
                btnAutoManual.BackColor = auto ? AppTheme.Success : AppTheme.Warning;
            }
            AutoModeChanged?.Invoke(this, _autoMode);
            UpdateButtonStates();
        }

        private void BtnNext_Click(object sender, EventArgs e)
        {
            ClearWaveformImmediate();
            if (!_isPlaying && !_isPaused)
            {
                if (_playlistQueue != null && _playlistQueue.GetItemCount() > 0)
                {
                    var items = _playlistQueue.GetAllItems();
                    if (items.Count > 0)
                    {
                        var firstItem = items[0];

                        LoadTrack(
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

                        Play();
                        _playlistQueue.SetCurrentPlaying(0);
                    }
                    else
                    {
                        MessageBox.Show("La playlist queue è vuota!", "Attenzione",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Nessun file in playlist queue!", "Attenzione",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                SkipWithFade();
            }

            UpdateButtonStates();
            NextRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ClearWaveformImmediate()
        {
            // Abort generazione in corso
            _waveformAbort = true;

            // Pulisci subito la bitmap
            lock (_waveformLock)
            {
                try { _waveformBitmap?.Dispose(); } catch { }
                _waveformBitmap = null;
                _waveformData = null;
            }

            // Reset stato
            _lastLoadedFile = "";
            _isLoadingWaveform = false;

            // Mostra subito schermo nero
            SafeInvalidateWaveform();
        }
        private void SkipWithFade()
        {
            ClearWaveformImmediate();
            if (_playlistQueue == null || _playlistQueue.GetItemCount() < 2)
            {
                Stop();
                return;
            }

            if (_isStreamingURL)
            {
                Stop();
                _playlistQueue.RemoveItem(0);

                var nextItems = _playlistQueue.GetAllItems();
                if (nextItems.Count > 0)
                {
                    var nextItem = nextItems[0];
                    LoadTrack(
                        nextItem.FilePath,
                        nextItem.Artist,
                        nextItem.Title,
                        nextItem.Intro,
                        nextItem.MarkerIN,
                        nextItem.MarkerINTRO,
                        nextItem.MarkerMIX,
                        nextItem.MarkerOUT,
                        nextItem.ItemType
                    );
                    Play();
                    _playlistQueue.SetCurrentPlaying(0);

                    // ✅ NUOVO: Notifica cambio video
                    NotifyTrackChanged(nextItem);
                }
                return;
            }

            int skipFadeDuration = ConfigurationControl.GetMixDuration();

            AudioFileReader activeAudio = _isPlayerAActive ? _audioFileA : _audioFileB;
            VolumeSampleProvider activeVolume = _isPlayerAActive ? _volumeProviderA : _volumeProviderB;
            int currentMs = (int)activeAudio.CurrentTime.TotalMilliseconds;

            _fadeOutStartMs = currentMs;
            _fadeOutEndMs = currentMs + skipFadeDuration;

            _volumeFadingOut = activeVolume;
            _audioFadingOut = activeAudio;
            _fadeOutTimer.Start();

            _playlistQueue.RemoveItem(0);

            var queueItems = _playlistQueue.GetAllItems();
            if (queueItems.Count > 0)
            {
                var nextItem = queueItems[0];

                if (_isPlayerAActive)
                {
                    if (_volumeProviderB != null)
                    {
                        _mixer.RemoveMixerInput(_volumeProviderB);
                    }
                    _audioFileB?.Dispose();

                    _audioFileB = new AudioFileReader(nextItem.FilePath);

                    if (nextItem.MarkerIN > 0)
                    {
                        _audioFileB.CurrentTime = TimeSpan.FromMilliseconds(nextItem.MarkerIN);
                    }

                    var sampleProvider = CreateResampledProvider(_audioFileB);

                    _volumeProviderB = new VolumeSampleProvider(sampleProvider);
                    _volumeProviderB.Volume = 1f;
                    _mixer.AddMixerInput(_volumeProviderB);

                    _volumeProviderA = null;
                    _audioFileA = null;
                }
                else
                {
                    if (_volumeProviderA != null)
                    {
                        _mixer.RemoveMixerInput(_volumeProviderA);
                    }
                    _audioFileA?.Dispose();

                    _audioFileA = new AudioFileReader(nextItem.FilePath);

                    if (nextItem.MarkerIN > 0)
                    {
                        _audioFileA.CurrentTime = TimeSpan.FromMilliseconds(nextItem.MarkerIN);
                    }

                    var sampleProvider = CreateResampledProvider(_audioFileA);

                    _volumeProviderA = new VolumeSampleProvider(sampleProvider);
                    _volumeProviderA.Volume = 1f;
                    _mixer.AddMixerInput(_volumeProviderA);

                    _volumeProviderB = null;
                    _audioFileB = null;
                }

                _isPlayerAActive = !_isPlayerAActive;

                _playlistQueue.SetCurrentPlaying(0);

                LoadTrackInfo(nextItem);

                // ✅ NUOVO:  Notifica cambio video
                NotifyTrackChanged(nextItem);
            }
        }

        /// <summary>
        /// Notifica RadioTVBridge del cambio traccia per aggiornare il video
        /// </summary>
        private void NotifyTrackChanged(PlaylistQueueItem item)
        {
            try
            {
                TrackChanged?.Invoke(this, new TrackChangedEventArgs
                {
                    FilePath = item.FilePath,
                    Artist = item.Artist,
                    Title = item.Title,
                    IsVideo = IsVideoFile(item.FilePath),
                    ItemType = item.ItemType,
                    VideoFilePath = item.VideoFilePath ?? "",
                    VideoSource = item.VideoSource ?? "",
                    NDISourceName = item.NDISourceName ?? "",
                    MarkerIN = item.MarkerIN,
                    MarkerMIX = item.MarkerMIX,
                    MarkerOUT = item.MarkerOUT
                });
            }
            catch (Exception ex)
            {
                Log($"[PlayerControl] ⚠️ NotifyTrackChanged error: {ex.Message}");
            }
        }

        private void BtnAutoManual_Click(object sender, EventArgs e)
        {
            _autoMode = !_autoMode;
            Button btn = sender as Button;
            btn.Text = _autoMode ? "AUTO" : "MANUAL";
            btn.BackColor = _autoMode ? AppTheme.Success : AppTheme.Warning;
            AutoModeChanged?.Invoke(this, _autoMode);
        }

        private void PlayNextTrack()
        {
            if (_playlistQueue != null && _playlistQueue.GetItemCount() > 0)
            {
                _playlistQueue.RemoveItem(0);

                var items = _playlistQueue.GetAllItems();
                if (items.Count > 0)
                {
                    var nextItem = items[0];

                    LoadTrack(
                        nextItem.FilePath,
                        nextItem.Artist,
                        nextItem.Title,
                        nextItem.Intro,
                        nextItem.MarkerIN,
                        nextItem.MarkerINTRO,
                        nextItem.MarkerMIX,
                        nextItem.MarkerOUT,
                        nextItem.ItemType
                    );

                    if (_autoMode)
                    {
                        Play();
                    }

                    _playlistQueue.SetCurrentPlaying(0);
                }
                else
                {
                    ClearPlayer();
                }
            }
        }

        private void OnTrackEnded()
        {
            if (_autoMode)
            {
                PlayNextTrack();
            }
            else
            {
                Stop();
                ClearPlayer();
                TrackEndedInManualMode?.Invoke(this, EventArgs.Empty);
            }
        }

        public void LoadTrack(string filePath, string artist, string title, TimeSpan intro,
                      int markerIN = 0, int markerINTRO = 0, int markerMIX = 0, int markerOUT = 0, string itemType = "Music")
        {
            try
            {
                ClearWaveformImmediate();
                Stop();

                lock (_waveformLock)
                {
                    _waveformBitmap?.Dispose();
                    _waveformBitmap = null;
                }
                _waveformData = null;
                _lastLoadedFile = "";
                waveformPanel.Invalidate();

                _isStreamingURL = IsStreamUrl(filePath);

                // Aggiorna proprietà per RadioTV Bridge
                CurrentFilePath = filePath;
                CurrentArtist = artist;
                CurrentTitle = title;

                if (_isStreamingURL)
                {
                    _totalDuration = intro;
                    _introTime = TimeSpan.Zero;
                    _currentPosition = TimeSpan.Zero;
                    _streamScheduledDuration = intro;

                    _markerIN = 0;
                    _markerINTRO = 0;
                    _markerMIX = 0;
                    _markerOUT = 0;
                    _mixRequested = false;
                }
                else
                {
                    if (_isPlayerAActive)
                    {
                        if (_volumeProviderA != null)
                        {
                            _mixer.RemoveMixerInput(_volumeProviderA);
                        }
                        _audioFileA?.Dispose();

                        _audioFileA = new AudioFileReader(filePath);
                    }
                    else
                    {
                        if (_volumeProviderB != null)
                        {
                            _mixer.RemoveMixerInput(_volumeProviderB);
                        }
                        _audioFileB?.Dispose();

                        _audioFileB = new AudioFileReader(filePath);
                    }

                    AudioFileReader activeAudio = _isPlayerAActive ? _audioFileA : _audioFileB;

                    _totalDuration = activeAudio.TotalTime;
                    _introTime = intro;
                    _currentPosition = TimeSpan.Zero;

                    _markerIN = markerIN;
                    _markerINTRO = markerINTRO;
                    _markerMIX = markerMIX > 0 ? markerMIX : (int)_totalDuration.TotalMilliseconds;
                    _markerOUT = markerOUT > 0 ? markerOUT : (int)_totalDuration.TotalMilliseconds;
                    _mixRequested = false;
                }

                string displayText = string.IsNullOrEmpty(artist)
                    ? title.ToUpper()
                    : $"{artist.ToUpper()} - {title.ToUpper()}";

                lblArtist.Text = displayText;
                lblArtist.Invalidate();

                lblIntro.Text = intro.ToString(@"mm\:ss");
                lblIntro.ForeColor = Color.White;
                lblIntro.BackColor = _isStreamingURL ? Color.Blue : Color.Red;

                UpdateCounters();

                try
                {
                    string metadataSource = ConfigurationControl.GetMetadataSource();
                    bool shouldSendMetadata = false;

                    if (metadataSource == "MusicOnly" && itemType == "Music")
                    {
                        shouldSendMetadata = true;
                    }
                    else if (metadataSource == "MusicAndClips")
                    {
                        shouldSendMetadata = true;
                    }

                    if (shouldSendMetadata)
                    {
                        AirDirector.Services.MetadataManager.UpdateMetadata(artist ?? "", title ?? "", itemType);
                    }
                }
                catch { }

                // NOTIFICA RADIOTV BRIDGE
                TrackChanged?.Invoke(this, new TrackChangedEventArgs
                {
                    FilePath = filePath,
                    Artist = artist,
                    Title = title,
                    IsVideo = IsVideoFile(filePath),
                    ItemType = itemType,
                    VideoFilePath = "",
                    VideoSource = "",
                    NDISourceName = "",
                    // ✅ NUOVO:  Passa i marker
                    MarkerIN = markerIN,
                    MarkerMIX = markerMIX,
                    MarkerOUT = markerOUT
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore caricamento file:\n{ex.Message}", "Errore",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void Play()
        {
            if (_isStreamingURL)
            {
                try
                {
                    var items = _playlistQueue.GetAllItems();
                    if (items.Count == 0)
                        return;

                    var streamItem = items[0];

                    _streamScheduledDuration = streamItem.Duration;
                    _streamStartTime = DateTime.Now;
                    _currentPosition = TimeSpan.Zero;
                    _totalDuration = streamItem.Duration;
                    _isPlaying = true;
                    _isPaused = false;

                    UpdateCounters();

                    if (_vlcPlayer != null)
                    {
                        var media = new Media(_libVLC, new Uri(streamItem.FilePath));
                        media.AddOption(": no-video");
                        media.AddOption(": network-caching=10000");
                        media.AddOption(": live-caching=10000");

                        _vlcPlayer.Media = media;
                        _vlcPlayer.Play();
                    }

                    _updateTimer.Stop();
                    _mixCheckTimer.Stop();
                    _streamDurationTimer.Start();

                    UpdateButtonStates();

                    // NOTIFICA RADIOTV BRIDGE
                    PlayStateChanged?.Invoke(this, new PlayStateChangedEventArgs
                    {
                        IsPlaying = true,
                        IsPaused = false,
                        FilePath = streamItem.FilePath
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Errore riproduzione stream:\n{ex.Message}",
                        "Errore Stream", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Stop();
                }

                return;
            }

            Log($"[Play] 🎵 Riproduzione file audio normale");

            AudioFileReader activeAudio = _isPlayerAActive ? _audioFileA : _audioFileB;

            if (activeAudio != null && !_isPlaying)
            {
                if (_markerIN > 0)
                {
                    activeAudio.CurrentTime = TimeSpan.FromMilliseconds(_markerIN);
                    _currentPosition = TimeSpan.FromMilliseconds(_markerIN);
                    Log($"[Play] Partenza da Marker IN: {_markerIN}ms");
                }

                // USA RESAMPLING
                var sampleProvider = CreateResampledProvider(activeAudio);

                if (_isPlayerAActive)
                {
                    _volumeProviderA = new VolumeSampleProvider(sampleProvider);
                    _volumeProviderA.Volume = 1f;
                    _mixer.AddMixerInput(_volumeProviderA);
                }
                else
                {
                    _volumeProviderB = new VolumeSampleProvider(sampleProvider);
                    _volumeProviderB.Volume = 1f;
                    _mixer.AddMixerInput(_volumeProviderB);
                }

                _isPlaying = true;
                _isPaused = false;
                _updateTimer.Start();
                _mixCheckTimer.Start();

                UpdateButtonStates();

                Log($"[Play] ✅ File audio avviato su Player {(_isPlayerAActive ? "A" : "B")}");

                // NOTIFICA RADIOTV BRIDGE
                PlayStateChanged?.Invoke(this, new PlayStateChangedEventArgs
                {
                    IsPlaying = true,
                    IsPaused = false,
                    FilePath = CurrentFilePath
                });

                Task.Delay(50).ContinueWith(_ =>
                {
                    if (!_isLoadingWaveform && activeAudio != null && !string.IsNullOrEmpty(activeAudio.FileName))
                    {
                        GenerateWaveformAsync(activeAudio.FileName);
                    }
                });
            }
        }

        public void Pause()
        {
            if (_isPlaying && !_isPaused)
            {
                _isPaused = true;
                _updateTimer.Stop();
                _mixCheckTimer.Stop();
                _streamDurationTimer.Stop();

                if (_isStreamingURL && _vlcPlayer != null && _vlcPlayer.IsPlaying)
                {
                    _vlcPlayer.Pause();
                }
                else if (_isPlayerAActive && _volumeProviderA != null)
                {
                    _mixer.RemoveMixerInput(_volumeProviderA);
                }
                else if (!_isPlayerAActive && _volumeProviderB != null)
                {
                    _mixer.RemoveMixerInput(_volumeProviderB);
                }

                UpdateButtonStates();

                // NOTIFICA RADIOTV BRIDGE
                PlayStateChanged?.Invoke(this, new PlayStateChangedEventArgs
                {
                    IsPlaying = true,
                    IsPaused = true,
                    FilePath = CurrentFilePath
                });
            }
        }

        public void Resume()
        {
            if (_isPaused)
            {
                _isPaused = false;

                if (_isStreamingURL && _vlcPlayer != null)
                {
                    _vlcPlayer.Play();
                    _streamDurationTimer.Start();
                }
                else
                {
                    if (_isPlayerAActive && _volumeProviderA != null)
                    {
                        _mixer.AddMixerInput(_volumeProviderA);
                    }
                    else if (!_isPlayerAActive && _volumeProviderB != null)
                    {
                        _mixer.AddMixerInput(_volumeProviderB);
                    }

                    _updateTimer.Start();
                    _mixCheckTimer.Start();
                }

                UpdateButtonStates();

                // NOTIFICA RADIOTV BRIDGE
                PlayStateChanged?.Invoke(this, new PlayStateChangedEventArgs
                {
                    IsPlaying = true,
                    IsPaused = false,
                    FilePath = CurrentFilePath
                });
            }
        }

        public void Stop()
        {
            _updateTimer.Stop();
            _mixCheckTimer.Stop();
            _fadeOutTimer.Stop();
            _blinkTimer.Stop();
            _streamDurationTimer.Stop();

            // ✅ NUOVO:  Cancella waveform in corso e resetta stato
            _waveformCts?.Cancel();
            _waveformCts?.Dispose();
            _waveformCts = null;
            _isLoadingWaveform = false;
            _lastLoadedFile = "";  // ✅ IMPORTANTE: resetta per permettere nuovo caricamento

            lock (_waveformStateLock)
            {
                _waveformAbort = true;
                _lastLoadedFile = "";
            }

            if (_vlcPlayer != null && _vlcPlayer.IsPlaying)
            {
                _vlcPlayer.Stop();
            }

            if (_volumeProviderA != null)
            {
                _mixer.RemoveMixerInput(_volumeProviderA);
                _volumeProviderA = null;
            }

            if (_volumeProviderB != null)
            {
                _mixer.RemoveMixerInput(_volumeProviderB);
                _volumeProviderB = null;
            }

            if (_volumeFadingOut != null)
            {
                _mixer.RemoveMixerInput(_volumeFadingOut);
                _volumeFadingOut = null;
                _audioFadingOut = null;
            }

            _isPlaying = false;
            _isPaused = false;
            _isStreamingURL = false;

            AudioFileReader activeAudio = _isPlayerAActive ? _audioFileA : _audioFileB;
            if (activeAudio != null)
                activeAudio.Position = 0;

            _currentPosition = TimeSpan.Zero;
            _mixRequested = false;

            lblIntro.Text = "--: --";
            lblIntro.ForeColor = AppTheme.LEDYellow;
            lblIntro.BackColor = Color.Black;

            lblRemaining.BackColor = Color.Black;
            lblRemaining.ForeColor = AppTheme.LEDRed;

            UpdateCounters();
            UpdateButtonStates();
            waveformPanel.Invalidate();
            vuMeterLeftPanel.Invalidate();
            vuMeterRightPanel.Invalidate();

            // NOTIFICA RADIOTV BRIDGE
            PlayStateChanged?.Invoke(this, new PlayStateChangedEventArgs
            {
                IsPlaying = false,
                IsPaused = false,
                FilePath = CurrentFilePath
            });
        }

        private void ClearPlayer()
        {
            Stop();

            ClearWaveformDisplay();
            lblArtist.Text = "";
            lblArtist.Invalidate();
            lblIntro.Text = "--:--";
            lblIntro.BackColor = Color.Black;
            lblIntro.ForeColor = AppTheme.LEDYellow;
            lblElapsed.Text = "--:--";
            lblRemaining.Text = "--:--";
            lblRemaining.BackColor = Color.Black;
            lblRemaining.ForeColor = AppTheme.LEDRed;

            lock (_waveformLock)
            {
                _waveformBitmap?.Dispose();
                _waveformBitmap = null;
            }
            _waveformData = null;
            _lastLoadedFile = "";
            _isLoadingWaveform = false;
            _totalDuration = TimeSpan.Zero;
            _introTime = TimeSpan.Zero;
            _lastVULevelLeft = 0f;
            _lastVULevelRight = 0f;
            _markerIN = 0;
            _markerINTRO = 0;
            _markerMIX = 0;
            _markerOUT = 0;
            _mixRequested = false;
            _isStreamingURL = false;

            CurrentFilePath = "";
            CurrentArtist = "";
            CurrentTitle = "";

            waveformPanel.Invalidate();
            _playlistQueue?.SetCurrentPlaying(-1);
        }

        public bool IsPlaying => _isPlaying && !_isPaused;
        public bool IsAutoMode => _autoMode;

        private void Log(string m) { _dailyLogger?.Log(m); }
        private void LogErr(string m, Exception ex) { _dailyLogger?.LogErr(m, ex); }
        private void LogErr(string m) { _dailyLogger?.LogErr(m); }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // ✅ Cancella waveform in caricamento

                // ✅ AGGIUNGI: Abort waveform
                _waveformAbort = true;
                if (_waveformThread != null && _waveformThread.IsAlive)
                {
                    _waveformThread.Join(1000);
                }
                _waveformCts?.Cancel();
                _waveformCts?.Dispose();

                _updateTimer?.Stop();
                _updateTimer?.Dispose();
                _mixCheckTimer?.Stop();
                _mixCheckTimer?.Dispose();
                _fadeOutTimer?.Stop();
                _fadeOutTimer?.Dispose();
                _blinkTimer?.Stop();
                _blinkTimer?.Dispose();
                _streamDurationTimer?.Stop();
                _streamDurationTimer?.Dispose();

                _vlcPlayer?.Stop();
                _vlcPlayer?.Dispose();
                _libVLC?.Dispose();

                _masterOutput?.Stop();
                _masterOutput?.Dispose();

                _audioFileA?.Dispose();
                _audioFileB?.Dispose();

                lock (_waveformLock)
                {
                    _waveformBitmap?.Dispose();
                    _waveformBitmap = null;
                }

                try { _dailyLogger?.Dispose(); } catch { }
            }
            base.Dispose(disposing);
        }
    }

    // ═══════════════════════════════════════════════════════════
    // EVENT ARGS PER RADIOTV BRIDGE
    // ═══════════════════════════════════════════════════════════
    public class TrackChangedEventArgs : EventArgs
    {
        public string FilePath { get; set; }
        public string Artist { get; set; }
        public string Title { get; set; }
        public bool IsVideo { get; set; }
        public string ItemType { get; set; }

        // ✅ NUOVO: Video associato
        public string VideoFilePath { get; set; }
        public string VideoSource { get; set; }
        public string NDISourceName { get; set; }

        // ✅ NUOVO: Marker per sincronizzazione video
        public int MarkerIN { get; set; }
        public int MarkerMIX { get; set; }
        public int MarkerOUT { get; set; }
    }

    public class PlayStateChangedEventArgs : EventArgs
    {
        public bool IsPlaying { get; set; }
        public bool IsPaused { get; set; }
        public string FilePath { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // CLASSI DI SUPPORTO
    // ═══════════════════════════════════════════════════════════
    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.UserPaint, true);
            this.UpdateStyles();
        }
    }

    public class MeteringSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider _source;
        private readonly float[] _maxSamples;
        private int _sampleCount;
        private readonly int _channels;

        public WaveFormat WaveFormat => _source.WaveFormat;

        public event EventHandler<StreamVolumeEventArgs> StreamVolume;

        public MeteringSampleProvider(ISampleProvider source, int samplesPerNotification = 882)
        {
            _source = source;
            _channels = source.WaveFormat.Channels;
            _maxSamples = new float[_channels];
            SamplesPerNotification = samplesPerNotification;
        }

        public int SamplesPerNotification { get; set; }

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = _source.Read(buffer, offset, count);

            for (int i = 0; i < samplesRead; i++)
            {
                int channel = i % _channels;
                float sampleValue = Math.Abs(buffer[offset + i]);

                _maxSamples[channel] = Math.Max(_maxSamples[channel], sampleValue);
            }

            _sampleCount += samplesRead / _channels;

            if (_sampleCount >= SamplesPerNotification)
            {
                StreamVolume?.Invoke(this, new StreamVolumeEventArgs
                {
                    MaxSampleValues = (float[])_maxSamples.Clone()
                });

                _sampleCount = 0;
                Array.Clear(_maxSamples, 0, _channels);
            }

            return samplesRead;
        }
    }

    /// <summary>
    /// Sample provider che intercetta l'audio e lo invia a NDI
    /// </summary>
    public class NDIAudioTap : ISampleProvider
    {
        private readonly ISampleProvider _source;
        private Action<float[], int, int> _audioCallback;
        private int _sampleRate;
        private int _channels;

        public WaveFormat WaveFormat => _source.WaveFormat;

        public NDIAudioTap(ISampleProvider source)
        {
            _source = source;
            _sampleRate = source.WaveFormat.SampleRate;
            _channels = source.WaveFormat.Channels;
        }

        public void SetCallback(Action<float[], int, int> callback)
        {
            _audioCallback = callback;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = _source.Read(buffer, offset, count);

            if (samplesRead > 0 && _audioCallback != null)
            {
                try
                {
                    // Copia per NDI
                    float[] ndiBuffer = new float[samplesRead];
                    for (int i = 0; i < samplesRead; i++)
                    {
                        ndiBuffer[i] = buffer[offset + i];
                    }

                    _audioCallback(ndiBuffer, _sampleRate, _channels);
                }
                catch { }
            }

            return samplesRead;
        }
    }

    public class StreamVolumeEventArgs : EventArgs
    {
        public float[] MaxSampleValues { get; set; }
    }
}