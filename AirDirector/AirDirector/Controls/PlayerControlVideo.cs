using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;
using AirDirector.Services;
using AirDirector.Services.Streaming;
using AirDirector.Services.Localization;
using AirDirector.Themes;
using LibVLCSharp.Shared;
using Microsoft.Win32;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NewTek;
using NewTek.NDI;

using Size = System.Drawing.Size;
using Font = System.Drawing.Font;
using FontStyle = System.Drawing.FontStyle;
using Point = System.Drawing.Point;
using Color = System.Drawing.Color;
using Padding = System.Windows.Forms.Padding;

namespace AirDirector.Controls
{
    public partial class PlayerControlVideo : UserControl
    {
        // ═══════════════════════════════════════════════════════════
        // COSTANTI
        // ═══════════════════════════════════════════════════════════
        private const int AUDIO_SAMPLE_RATE = 48000;
        private const int AUDIO_CHANNELS = 2;
        private const int MIN_READY_FRAMES = 3;
        private const int AUDIO_WARMUP_SAMPLES = AUDIO_SAMPLE_RATE / 4 * AUDIO_CHANNELS;
        private const int AUDIO_RING_SIZE = 1 << 19;
        private const int AUDIO_RING_MASK = AUDIO_RING_SIZE - 1;
        private const int AUDIO_DELAY_MS = 40;
        private const int AUDIO_DELAY_SAMPLES = AUDIO_DELAY_MS * AUDIO_SAMPLE_RATE / 1000 * AUDIO_CHANNELS;
        private const float PROGRAM_PIP_SCALE = 0.75f;
        private static readonly string[] HlsContentTypes = { "application/vnd.apple.mpegurl", "application/x-mpegurl", "audio/mpegurl", "audio/x-mpegurl", "application/mpegurl" };
        private static readonly HttpClient _streamProbeHttp = CreateStreamProbeHttpClient();

        private sealed class AudioDelayLine
        {
            private readonly float[] _buf; private readonly int _size; private int _writePos, _readPos;
            public AudioDelayLine(int delaySamples) { _size = delaySamples + AUDIO_SAMPLE_RATE / 10 * AUDIO_CHANNELS; _buf = new float[_size]; _writePos = delaySamples; }
            public void Process(float[] buffer, int offset, int count) { for (int i = 0; i < count; i++) { _buf[_writePos] = buffer[offset + i]; if (++_writePos >= _size) _writePos = 0; buffer[offset + i] = _buf[_readPos]; if (++_readPos >= _size) _readPos = 0; } }
            public void Reset() { Array.Clear(_buf, 0, _buf.Length); _readPos = 0; _writePos = AUDIO_DELAY_SAMPLES; }
        }

        private int _ndiWidth = 1920, _ndiHeight = 1080, _ndiFrameRate = 30;
        private string _ndiSourceName = "AirDirector Output", _bufferVideoPath = "";

        private LibVLC _libVLC; private Sender _ndiSender; private Thread _engineThread;
        private HlsToTsRelay? _hlsRelay;
        private volatile bool _engineRunning, _engineReady, _isDisposed;
        private readonly ConcurrentQueue<Action> _commandQueue = new ConcurrentQueue<Action>();

        private class PlayItem
        {
            public string FilePath = "", Artist = "", Title = "", ItemType = "Music";
            public string VideoFilePath = "", VideoSource = "", NDISourceName = "";
            public TimeSpan Intro = TimeSpan.Zero, Duration = TimeSpan.Zero;
            public int MarkerIN, MarkerINTRO, MarkerMIX, MarkerOUT;
            public int FileDurationMs;
            public bool IsScheduled = false;
            public bool IsVideoStream = false;
            public static PlayItem FromQueueItem(PlaylistQueueItem qi) => new PlayItem
            { FilePath = qi.FilePath ?? "", Artist = qi.Artist ?? "", Title = qi.Title ?? "", Intro = qi.Intro, MarkerIN = qi.MarkerIN, MarkerINTRO = qi.MarkerINTRO, MarkerMIX = qi.MarkerMIX, MarkerOUT = qi.MarkerOUT, ItemType = qi.ItemType ?? "Clip", VideoFilePath = qi.VideoFilePath ?? "", VideoSource = qi.VideoSource ?? "", NDISourceName = qi.NDISourceName ?? "", Duration = qi.Duration, FileDurationMs = qi.FileDurationMs, IsScheduled = qi.IsScheduled, IsVideoStream = qi.IsVideoStream };
        }

        private enum DeckType { VideoClip, AudioTrack, WebStream, Buffer }
        private sealed class AudioRing
        {
            private readonly float[] _buf = new float[AUDIO_RING_SIZE]; private long _wPos, _rPos;
            public int Available => (int)(Interlocked.Read(ref _wPos) - Interlocked.Read(ref _rPos));
            private int Free => AUDIO_RING_SIZE - Available;
            public void Write(float[] src, int offset, int count) { if (count <= 0) return; int free = Free; if (count > free) Interlocked.Add(ref _rPos, count - free); long wp = Interlocked.Read(ref _wPos); for (int i = 0; i < count; i++) _buf[(wp + i) & AUDIO_RING_MASK] = src[offset + i]; Interlocked.Add(ref _wPos, count); }
            public int Read(float[] dst, int offset, int count) { int toRead = Math.Min(count, Available); if (toRead <= 0) return 0; long rp = Interlocked.Read(ref _rPos); for (int i = 0; i < toRead; i++) dst[offset + i] = _buf[(rp + i) & AUDIO_RING_MASK]; Interlocked.Add(ref _rPos, toRead); return toRead; }
            public void Reset() { Interlocked.Exchange(ref _wPos, 0); Interlocked.Exchange(ref _rPos, 0); }
        }
        private class Deck
        {
            public string Name; public DeckType Type; public MediaPlayer VlcPlayer; public IntPtr VideoBufferPtr;
            public readonly AudioRing Ring = new AudioRing();
            public readonly short[] PcmBuf = new short[8192 * AUDIO_CHANNELS];
            public readonly float[] PcmFloat = new float[8192 * AUDIO_CHANNELS];
            private long _vlcTimeMs;
            public long VlcTimeMs { get => Interlocked.Read(ref _vlcTimeMs); set => Interlocked.Exchange(ref _vlcTimeMs, value); }
            public volatile bool WarmupDone; public AudioFileReader FileReader; public ISampleProvider Resampler;
            public volatile bool IsPlaying; public volatile float Volume = 1.0f;
            public volatile bool IsReadyForVideo; public volatile int FrameCount;
            public volatile bool AudioStarted; public PlayItem CurrentItem;
            public volatile bool IsPreBuffered, IsPendingActivation, PreBufferReady;
            public volatile int SessionId, StartPointMs;
            public int WebStreamRetryCount;
            public bool IsVideoStream;
            public string? RelayUrl;
            public readonly System.Diagnostics.Stopwatch StreamClock = new System.Diagnostics.Stopwatch();
        }
        private Deck _deckA, _deckB, _bufferDeck, _activeDeck, _pendingDeck;
        private bool _nextIsA = true; private volatile int _globalSessionId;
        private string _preBufferedFile = ""; private Deck _preBufferedDeck = null;

        private byte[] _compositedVideoFrame;
        private GCHandle _compositedVideoHandle;
        private IntPtr _compositedVideoPtr;
        private byte[] _cgFullFrame;
        private float[] _mixedAudioBuffer, _tempReadBuffer;
        private AudioDelayLine _audioDelay;
        private long _framesSent, _audioSamplesSent;
        private int _pipW, _pipH, _pipX, _pipY;
        private int[] _pipSrcXLut;

        // ═══════════════════════════════════════════════════════════
        // AUDIO MIRROR (uscita locale)
        //
        // Architettura: l'engine loop (30fps, timing preciso) scrive
        // direttamente nel BufferedWaveProvider ad ogni frame.
        // WaveOutEvent ha il suo thread interno con timing gestito
        // dal driver audio di Windows → nessun jitter, nessun underrun.
        //
        // NON usiamo thread separato né ring buffer intermedio:
        // - Thread.Sleep ha granularità ~15ms → timing instabile
        // - Ring buffer aggiuntivo introduce race condition
        // - BufferedWaveProvider.AddSamples è già thread-safe
        // ═══════════════════════════════════════════════════════════
        private IWavePlayer _audioMirrorOutput;
        private BufferedWaveProvider _audioMirrorBuffer;
        private byte[] _audioMirrorConvertBuffer;
        private volatile bool _localAudioEnabled;

        private volatile float _vuLeft, _vuRight, _vuLeftPeak, _vuRightPeak;
        private const float VU_DECAY = 0.93f, VU_PEAK_DECAY = 0.985f;

        private bool _isPlaying, _isPaused, _autoMode = true;
        private bool _autoStartPending = false;
        private int _mixTriggeredFlag = 0;
        private bool TryTriggerMix() => Interlocked.CompareExchange(ref _mixTriggeredFlag, 1, 0) == 0;
        private void ResetMixTrigger() => Interlocked.Exchange(ref _mixTriggeredFlag, 0);
        private bool IsMixTriggered => Interlocked.CompareExchange(ref _mixTriggeredFlag, 0, 0) != 0;
        private volatile int _positionMs;
        private bool _currentFileIsVideo; private string _currentFile = "";
        public string CurrentFilePath { get; private set; } = "";
        public string CurrentArtist { get; private set; } = "";
        public string CurrentTitle { get; private set; } = "";
        public bool IsCurrentlyPlaying => _isPlaying && !_isPaused;
        public bool IsPlaying => _isPlaying && !_isPaused;
        public bool IsAutoMode => _autoMode;
        public int CurrentPositionMs => _positionMs;
        public int CurrentMarkerIN => _markerIN;
        public int CurrentMarkerINTRO => _markerINTRO;
        public int CurrentMarkerMIX => _markerMIX;
        private TimeSpan _totalDuration, _introTime;
        private int _markerIN, _markerINTRO, _markerMIX, _markerOUT;
        private volatile int _mixGeneration = 0;

        private List<string> _bufferPlaylist = new List<string>();
        private readonly Random _bufferRandom = new Random();
        private volatile bool _bufferShouldShow;
        private volatile bool _bufferIsDedicatedVideo;
        private volatile float[] _waveformPeaks; private volatile string _waveformCurrentFile = "";
        private readonly ConcurrentDictionary<string, float[]> _waveformCache = new ConcurrentDictionary<string, float[]>();
        private const int WAVEFORM_BARS = 300;

        // ═══════════════════════════════════════════════════════════
        // LANNER TV
        // ═══════════════════════════════════════════════════════════
        [Serializable] public class LannerCampaign { public int ID { get; set; } public string ClientName { get; set; } = ""; public string CampaignName { get; set; } = ""; public DateTime StartDate { get; set; } public DateTime EndDate { get; set; } public int DailySlots { get; set; } = 1; public List<string> SlotTimes { get; set; } = new List<string>(); public int DurationMinutes { get; set; } = 5; public string ImagePath { get; set; } = ""; public DateTime CreatedDate { get; set; } }
        [Serializable, XmlRoot("LannerTVCampaigns")] public class LannerCampaignData { public List<LannerCampaign> Campaigns { get; set; } = new List<LannerCampaign>(); }
        private class LannerSlot { public TimeSpan StartTime; public int DurationMinutes; public List<string> ImagePaths; public List<string> CampaignNames; }
        private List<LannerSlot> _todayLannerSlots = new List<LannerSlot>();
        private volatile bool _lannerActive;
        private byte[] _lannerImageBGRA;
        private volatile bool _lannerBgDirty;
        private int _lannerImgIdx, _lannerImgDurSec;
        private DateTime _lannerSlotStart, _lannerImgStart, _lastDateCheck = DateTime.MinValue;
        private LannerSlot _lannerSlot; private string _lannerDbPath = "";
        private readonly object _lannerLock = new object();
        private volatile bool _lannerEnabled = false;
        private byte[] _lannerBufA, _lannerBufB; private volatile int _lannerBufSelect;

        private PlaylistQueueControl _playlistQueue;
        private System.Windows.Forms.Timer _updateTimer, _mixCheckTimer, _blinkTimer;
        private Panel waveformPanel;
        private Label lblIntro, lblElapsed, lblRemaining, lblClock, lblDate, lblArtist;
        private Panel vuMeterLeftPanel, vuMeterRightPanel;
        private Button btnPlay, btnPause, btnStop, btnAutoManual;
        private Label _lblElapsedHeader, _lblRemainingHeader; private bool _blinkState;

        public event EventHandler PlayRequested, PauseRequested, StopRequested, NextRequested;
        public event EventHandler<bool> AutoModeChanged;
        public event EventHandler MixPointReached, TrackEndedInManualMode;
        public event EventHandler<TrackChangedEventArgs> TrackChanged;
        public event EventHandler<PlayStateChangedEventArgs> PlayStateChanged;

        private Services.Core.DailyLogger _dailyLogger;
        private static Font SafeFont(string f, float s, FontStyle st) { try { return new Font(new FontFamily(f), s, st); } catch { return new Font(SystemFonts.DefaultFont.FontFamily, s, st); } }
        private static bool IsWebStream(string path) { if (string.IsNullOrEmpty(path)) return false; return path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || path.StartsWith("rtmp://", StringComparison.OrdinalIgnoreCase) || path.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase) || path.StartsWith("mms://", StringComparison.OrdinalIgnoreCase); }
        private static HttpClient CreateStreamProbeHttpClient()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
                AutomaticDecompression = DecompressionMethods.All,
                AllowAutoRedirect = true
            };
            var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(4) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Accept.ParseAdd("*/*");
            return client;
        }

        private static bool LooksLikeHlsPath(Uri uri)
        {
            string path = uri.AbsolutePath;
            return path.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".m3u", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsHlsContentType(string? mediaType)
        {
            if (string.IsNullOrWhiteSpace(mediaType))
                return false;
            return HlsContentTypes.Any(t => string.Equals(t, mediaType.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private static bool StartsWithExtM3u(byte[] buffer, int count)
        {
            if (count <= 0) return false;
            int offset = 0;
            if (count >= 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
                offset = 3;
            if (count - offset < 7) return false;
            return buffer[offset] == (byte)'#' &&
                   buffer[offset + 1] == (byte)'E' &&
                   buffer[offset + 2] == (byte)'X' &&
                   buffer[offset + 3] == (byte)'T' &&
                   buffer[offset + 4] == (byte)'M' &&
                   buffer[offset + 5] == (byte)'3' &&
                   buffer[offset + 6] == (byte)'U';
        }

        private bool ShouldUseHlsRelay(string streamUrl)
        {
            if (string.IsNullOrWhiteSpace(streamUrl) || !Uri.TryCreate(streamUrl, UriKind.Absolute, out var uri))
                return false;
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                return false;
            if (LooksLikeHlsPath(uri))
                return true;

            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, uri);
                req.Headers.Range = new RangeHeaderValue(0, 255);
                using var resp = _streamProbeHttp.Send(req, HttpCompletionOption.ResponseHeadersRead);
                if (!resp.IsSuccessStatusCode)
                    return false;

                if (IsHlsContentType(resp.Content.Headers.ContentType?.MediaType))
                    return true;

                using var stream = resp.Content.ReadAsStream();
                byte[] initial = new byte[256];
                int read = stream.Read(initial, 0, initial.Length);
                return StartsWithExtM3u(initial, read);
            }
            catch (Exception ex)
            {
                Log("[STREAM] probe failed, relay bypass: " + ex.Message);
                return false;
            }
        }

        public PlayerControlVideo()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            InitLog(); LoadConfig(); InitPlayerUI(); InitTimers();
            LanguageManager.LanguageChanged += (s, e) => UpdateTimerLabels();
            InitializeEngine();
        }

        private void InitializeEngine()
        {
            try
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                Log("[INIT] Starting engine...");
                LibVLCSharp.Shared.Core.Initialize();
                _libVLC = new LibVLC("--no-osd", "--no-stats", "--quiet", "--avcodec-fast", "--avcodec-threads=4", "--avcodec-skiploopfilter=4", "--clock-jitter=0", "--clock-synchro=0", "--file-caching=500", "--network-caching=1000", "--avcodec-hw=any", "--gnutls-priorities=NORMAL:%COMPAT", "--no-video-title-show");
                try
                {
                    _libVLC.SetDialogHandlers(
                        (title, text) =>
                        {
                            Log($"[libvlc:Dialog/Error] {title}: {text}");
                            return Task.CompletedTask;
                        },
                        (dialog, title, text, defaultUserName, askStore, token) =>
                        {
                            Log($"[libvlc:Dialog/Login] {title} (auto-cancel)");
                            try { dialog.Dismiss(); } catch (Exception ex) { Log($"[libvlc:Dialog/Login] dismiss failed: {ex.Message}"); }
                            return Task.CompletedTask;
                        },
                        (dialog, title, text, qType, cancel, action1, action2, token) =>
                        {
                            Log($"[libvlc:Dialog/Question] {title}: {text} → auto-accept (action1='{action1}')");
                            try { dialog.PostAction(1); }
                            catch (Exception ex)
                            {
                                Log($"[libvlc:Dialog/Question] post-action failed: {ex.Message}");
                                try { dialog.Dismiss(); } catch (Exception dismissEx) { Log($"[libvlc:Dialog/Question] dismiss failed: {dismissEx.Message}"); }
                            }
                            return Task.CompletedTask;
                        },
                        (dialog, title, text, indeterminate, position, cancel, token) => Task.CompletedTask,
                        (dialog, position, text) => Task.CompletedTask
                    );
                    Log("[INIT] LibVLC dialog handlers registered (TLS auto-accept)");
                }
                catch (Exception ex)
                {
                    Log($"[INIT] ⚠️ SetDialogHandlers failed: {ex.Message}");
                }
                try
                {
                    _hlsRelay = new HlsToTsRelay { Logger = Log };
                }
                catch (Exception ex)
                {
                    Log($"[INIT] ⚠️ Relay start failed: {ex.Message}");
                }
                try
                {
                    _libVLC.Log += (sender, args) =>
                    {
                        if (args.Level == LibVLCSharp.Shared.LogLevel.Warning || args.Level == LibVLCSharp.Shared.LogLevel.Error)
                        {
                            try { Log("[libvlc:" + args.Level + "] " + (args.Module ?? "?") + ": " + (args.Message ?? "")); } catch { }
                        }
                    };
                }
                catch { }
                if (!NDIlib.initialize()) throw new Exception("NDI init failed");
                _ndiSender = new Sender(_ndiSourceName, true, false);
                int fs = _ndiWidth * _ndiHeight * 4;
                _compositedVideoFrame = new byte[fs];
                _compositedVideoHandle = GCHandle.Alloc(_compositedVideoFrame, GCHandleType.Pinned);
                _compositedVideoPtr = _compositedVideoHandle.AddrOfPinnedObject();
                ClearVideoBuffer(_compositedVideoPtr);
                _cgFullFrame = new byte[fs];
                _mixedAudioBuffer = new float[AUDIO_SAMPLE_RATE * AUDIO_CHANNELS];
                _tempReadBuffer = new float[AUDIO_SAMPLE_RATE * AUDIO_CHANNELS];
                _audioDelay = new AudioDelayLine(AUDIO_DELAY_SAMPLES);
                CalculatePipLayout(); BuildPipLookup();
                CheckLannerEnabled();
                _deckA = CreateDeck("DeckA"); _deckB = CreateDeck("DeckB"); _bufferDeck = CreateDeck("Buffer", true);
                _engineRunning = true;
                _engineThread = new Thread(EngineLoop) { Name = "PCV_NDI", Priority = ThreadPriority.Highest, IsBackground = true };
                _engineThread.Start();
                ThreadPool.QueueUserWorkItem(_ => { RefreshBufferPlaylist(); if (_lannerEnabled) LoadLannerSchedule(); });
                _engineReady = true;
                Log("[INIT] ✓ " + sw.ElapsedMilliseconds + "ms | " + _ndiWidth + "x" + _ndiHeight + "@" + _ndiFrameRate + " PIP=" + _pipW + "x" + _pipH + " Lanner=" + (_lannerEnabled ? "YesInternal" : "Disabled"));
                if (_localAudioEnabled) InitAudioMirror();
                else Log("[Audio] Local audio output disabled (registry flag)");
            }
            catch (Exception ex) { LogErr("[INIT]", ex); }
        }

        /// <summary>
        /// Inizializza l'uscita audio locale.
        /// 
        /// Architettura scelta per qualità broadcast:
        /// - BufferedWaveProvider alimentato direttamente dall'engine loop (30fps, timing preciso)
        /// - WaveOutEvent con il suo thread interno gestito dal driver audio Windows
        /// - Nessun thread intermedio, nessun ring buffer aggiuntivo
        /// - Buffer da 500ms per assorbire jitter senza aggiungere latenza percepibile
        /// - 8 piccoli buffer da ~12.5ms ciascuno per latenza bassa e stabile
        /// </summary>
        private void InitAudioMirror()
        {
            try
            {
                int deviceNumber = ConfigurationControl.GetMainOutputDeviceNumber();
                var fmt = WaveFormat.CreateIeeeFloatWaveFormat(AUDIO_SAMPLE_RATE, AUDIO_CHANNELS);

                _audioMirrorBuffer = new BufferedWaveProvider(fmt)
                {
                    BufferDuration = TimeSpan.FromMilliseconds(500),
                    DiscardOnBufferOverflow = true
                };

                // Pre-alloca il buffer di conversione per esattamente 1 frame audio
                // (48000 / 30 = 1600 campioni × 2 canali × 4 byte = 12800 byte)
                int samplesPerFrame = AUDIO_SAMPLE_RATE / _ndiFrameRate * AUDIO_CHANNELS;
                _audioMirrorConvertBuffer = new byte[samplesPerFrame * sizeof(float)];

                _audioMirrorOutput = new WaveOutEvent
                {
                    DeviceNumber = deviceNumber,
                    DesiredLatency = 150,
                    NumberOfBuffers = 8
                };
                _audioMirrorOutput.Init(_audioMirrorBuffer);

                // Pre-fill con 4 frame di silenzio (~133ms) per creare un margine di buffer
                // che assorba brevi pause del engine (GC, I/O, lanner, scheduling)
                int preFrames = 4;
                int silenceBytes = preFrames * (AUDIO_SAMPLE_RATE / _ndiFrameRate * AUDIO_CHANNELS * sizeof(float));
                byte[] silence = new byte[silenceBytes];
                _audioMirrorBuffer.AddSamples(silence, 0, silenceBytes);

                _audioMirrorOutput.Play();

                Log("[Audio] ✅ Mirror audio inizializzato (device=" + deviceNumber + ", latency=150ms, buffers=8, prefill=" + preFrames + " frames, frameSize=" + _audioMirrorConvertBuffer.Length + "B)");
            }
            catch (Exception ex)
            {
                Log("[Audio] ⚠️ Mirror audio non disponibile: " + ex.Message);
                _audioMirrorOutput = null;
                _audioMirrorBuffer = null;
                _audioMirrorConvertBuffer = null;
            }
        }

        private void LoadConfig()
        {
            try
            {
                _localAudioEnabled = ConfigurationControl.IsLocalAudioEnabled();
                try
                {
                    using (var k = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AirDirector"))
                    {
                        if (k != null)
                        {
                            string bvp = (k.GetValue("BufferVideoPath", "") ?? "").ToString().Trim();
                            if (!string.IsNullOrEmpty(bvp) && Directory.Exists(bvp))
                                _bufferVideoPath = bvp;
                        }
                    }
                }
                catch { }
                if (string.IsNullOrEmpty(_bufferVideoPath))
                {
                    try { _bufferVideoPath = ConfigurationControl.GetBufferVideoPath() ?? ""; } catch { }
                }
            }
            catch { }
        }

        private void CalculatePipLayout() { _pipW = (int)(_ndiWidth * PROGRAM_PIP_SCALE); _pipH = (int)(_ndiHeight * PROGRAM_PIP_SCALE); _pipX = _ndiWidth - _pipW; _pipY = 0; if (_pipX + _pipW > _ndiWidth) _pipW = _ndiWidth - _pipX; if (_pipY + _pipH > _ndiHeight) _pipH = _ndiHeight - _pipY; }
        private void BuildPipLookup() { _pipSrcXLut = new int[_pipW]; for (int x = 0; x < _pipW; x++) _pipSrcXLut[x] = x * _ndiWidth / _pipW; }

        private void CheckLannerEnabled()
        {
            try
            {
                using (var k = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AirDirector"))
                {
                    if (k != null)
                    {
                        string val = (k.GetValue("AdvLannerPlayout", "") ?? "").ToString().Trim();
                        _lannerEnabled = val.Equals("YesInternal", StringComparison.OrdinalIgnoreCase);
                        Log("[LANNER] Registry AdvLannerPlayout=" + val + " → " + (_lannerEnabled ? "ENABLED" : "DISABLED"));
                    }
                    else
                    {
                        _lannerEnabled = false;
                        Log("[LANNER] Registry key not found → DISABLED");
                    }
                }
            }
            catch (Exception ex)
            {
                _lannerEnabled = false;
                LogErr("[LANNER] Registry check", ex);
            }
        }

        // ═══════════════════════════════════════════════════════════
        // DECK FACTORY
        // ═══════════════════════════════════════════════════════════
        private Deck CreateDeck(string name, bool isBuffer = false)
        {
            var deck = new Deck { Name = name, Type = isBuffer ? DeckType.Buffer : DeckType.AudioTrack };
            deck.VlcPlayer = new MediaPlayer(_libVLC);
            deck.VideoBufferPtr = Marshal.AllocHGlobal(_ndiWidth * _ndiHeight * 4);
            ClearVideoBuffer(deck.VideoBufferPtr);
            deck.VlcPlayer.SetVideoFormat("RV32", (uint)_ndiWidth, (uint)_ndiHeight, (uint)(_ndiWidth * 4));
            deck.VlcPlayer.SetVideoCallbacks(
                (op, pl) => { Marshal.WriteIntPtr(pl, deck.VideoBufferPtr); return IntPtr.Zero; },
                (op, pic, pl) =>
                {
                    if (!deck.IsPlaying && !deck.IsPreBuffered) return;
                    deck.IsReadyForVideo = true;
                    deck.FrameCount++;
                    try { long t = deck.VlcPlayer.Time; if (t > 0) deck.VlcTimeMs = t; } catch { }
                    if (deck.IsPreBuffered && deck.FrameCount >= MIN_READY_FRAMES && deck.AudioStarted)
                        deck.PreBufferReady = true;
                }, null);
            deck.VlcPlayer.EndReached += (s, e) =>
            {
                int sid = deck.SessionId; deck.IsReadyForVideo = false;
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    if (isBuffer) _commandQueue.Enqueue(() => HandleBufferEnded());
                    else _commandQueue.Enqueue(() => HandleDeckEnded(deck, sid));
                });
            };
            if (!isBuffer)
            {
                deck.VlcPlayer.Opening += (s, e) => Log("[VLC] … " + deck.Name + " Opening (type=" + deck.Type + ")");
                deck.VlcPlayer.Playing += (s, e) => Log("[VLC] ▶ " + deck.Name + " Playing (type=" + deck.Type + ")");
                deck.VlcPlayer.EncounteredError += (s, e) =>
                {
                    string url = "";
                    try { url = deck.VlcPlayer.Media?.Mrl ?? ""; } catch { }
                    Log("[VLC] ❌ EncounteredError on " + deck.Name + " (type=" + deck.Type + ") state=" + deck.VlcPlayer.State + " mrl=" + url);
                    int sid = deck.SessionId;

                    bool isStreamDeck = deck.Type == DeckType.WebStream;
                    bool isCurrentDeckSession = sid == _globalSessionId;
                    bool canRetryStream = string.IsNullOrEmpty(deck.RelayUrl) || (_hlsRelay?.IsRegistered(deck.RelayUrl) ?? false);
                    if (isStreamDeck && isCurrentDeckSession && canRetryStream && !string.IsNullOrEmpty(url) && deck.WebStreamRetryCount < 2)
                    {
                        deck.WebStreamRetryCount++;
                        int retryN = deck.WebStreamRetryCount;
                        string baseRetryUrl = !string.IsNullOrEmpty(deck.RelayUrl) ? deck.RelayUrl : (deck.CurrentItem?.FilePath ?? url);
                        string retryUrl = baseRetryUrl;
                        Log("[VLC] ↻ WebStream retry #" + retryN + " → " + retryUrl);
                        ThreadPool.QueueUserWorkItem(_ =>
                        {
                            Thread.Sleep(500);
                            if (_isDisposed) return;
                            _commandQueue.Enqueue(() =>
                            {
                                try
                                {
                                    if (deck.SessionId != sid || sid != _globalSessionId) return;
                                    if (!string.IsNullOrEmpty(deck.RelayUrl) && !(_hlsRelay?.IsRegistered(deck.RelayUrl) ?? false)) return;
                                    try { deck.VlcPlayer.Stop(); } catch { }
                                    var m = new Media(_libVLC, retryUrl, FromType.FromLocation);
                                    if (!deck.IsVideoStream) m.AddOption(":no-video");
                                    m.AddOption(":network-caching=3000");
                                    m.AddOption(":clock-jitter=0");
                                    m.AddOption(":clock-synchro=0");
                                    if (!string.IsNullOrEmpty(deck.RelayUrl)) m.AddOption(":demux=ts");
                                    deck.VlcPlayer.Media = m;
                                    m.Dispose();
                                    deck.VlcPlayer.Play();
                                }
                                catch (Exception ex) { LogErr("[VLC retry]", ex); }
                            });
                        });
                        return;
                    }

                    _commandQueue.Enqueue(() =>
                    {
                        try
                        {
                            if (deck == _pendingDeck) { StopDeckInternal(deck, true); _pendingDeck = null; }
                            if (deck == _activeDeck) { HandleDeckEnded(deck, sid); }
                            else { SafeInvoke(() => { if (_autoMode) OnMixReached(); else AutoSkipToNext(); }); }
                        }
                        catch (Exception ex) { LogErr("[VLC EncounteredError]", ex); }
                    });
                };
            }
            if (!isBuffer)
            {
                deck.VlcPlayer.SetAudioFormat("S16N", AUDIO_SAMPLE_RATE, AUDIO_CHANNELS);
                deck.VlcPlayer.SetAudioCallbacks(
                    (data, samples, count, pts) =>
                    {
                        if (!deck.IsPlaying && !deck.IsPreBuffered) return;
                        deck.AudioStarted = true;
                        int sc = (int)count * AUDIO_CHANNELS;
                        if (sc <= 0 || sc > deck.PcmBuf.Length) return;
                        Marshal.Copy(samples, deck.PcmBuf, 0, sc);
                        for (int i = 0; i < sc; i++) deck.PcmFloat[i] = deck.PcmBuf[i] / 32768f;
                        deck.Ring.Write(deck.PcmFloat, 0, sc);
                        if (!deck.WarmupDone && deck.Ring.Available >= AUDIO_WARMUP_SAMPLES) deck.WarmupDone = true;
                    }, null, null, null, null);
            }
            return deck;
        }

        private void HandleBufferEnded()
        {
            if (!_bufferShouldShow) return;
            if (_bufferIsDedicatedVideo)
            {
                Log("[BUFFER] Dedicated video ended → switching to generic buffer");
                _bufferIsDedicatedVideo = false;
                _bufferDeck.IsReadyForVideo = false;
                _bufferDeck.FrameCount = 0;
                PlayNextBufferVideo();
            }
            else
            {
                PlayNextBufferVideo();
            }
        }

        private void ClearVideoBuffer(IntPtr buf) { unsafe { long* p = (long*)buf; int n = (_ndiWidth * _ndiHeight * 4) / 8; long blk = unchecked((long)0xFF000000FF000000); for (int i = 0; i < n; i++) p[i] = blk; } }

        private void ResetDeckState(Deck d)
        {
            d.IsPlaying = false; d.IsPreBuffered = false; d.IsPendingActivation = false;
            d.PreBufferReady = false; d.Volume = 0f; d.IsReadyForVideo = false;
            d.FrameCount = 0; d.AudioStarted = false; d.WarmupDone = false;
            d.VlcTimeMs = 0; d.Ring.Reset(); d.CurrentItem = null; d.StartPointMs = 0;
            d.WebStreamRetryCount = 0;
            d.IsVideoStream = false;
            d.RelayUrl = null;
            d.StreamClock.Reset();
        }

        private void StopDeckInternal(Deck d, bool clearVid)
        {
            if (!string.IsNullOrEmpty(d.RelayUrl))
            {
                try { _hlsRelay?.Unregister(d.RelayUrl); } catch { }
            }
            ResetDeckState(d);
            if (clearVid && d.VideoBufferPtr != IntPtr.Zero) ClearVideoBuffer(d.VideoBufferPtr);
            try { if (d.VlcPlayer != null && d.VlcPlayer.IsPlaying) d.VlcPlayer.Stop(); } catch { }
            if (d.FileReader != null) { try { d.FileReader.Dispose(); } catch { } d.FileReader = null; d.Resampler = null; }
        }

        // ═══════════════════════════════════════════════════════════
        // ENGINE LOOP
        // ═══════════════════════════════════════════════════════════
        private void EngineLoop()
        {
            double tps = System.Diagnostics.Stopwatch.Frequency;
            double tpf = tps / _ndiFrameRate;
            int spf = AUDIO_SAMPLE_RATE / _ndiFrameRate;
            long next = System.Diagnostics.Stopwatch.GetTimestamp();
            int tickCount = 0;

            while (_engineRunning)
            {
                int cc = 0;
                while (_commandQueue.TryDequeue(out var cmd) && cc < 16)
                { try { cmd.Invoke(); } catch (Exception ex) { LogErr("[CMD]", ex); } cc++; }

                long now = System.Diagnostics.Stopwatch.GetTimestamp();
                if (now < next) { double w = ((next - now) * 1000.0) / tps; if (w > 2) Thread.Sleep(1); else if (w > 0.3) Thread.SpinWait(50); continue; }
                if (now - next > (long)(tpf * 3)) next = now;
                next += (long)tpf;

                tickCount++;
                if (tickCount >= _ndiFrameRate) { tickCount = 0; if (_lannerEnabled) UpdateLannerState(); }
                if (tickCount % 5 == 0) UpdatePosition();
                CheckPendingTransition();

                // ═════ AUDIO FIRST ═════
                int audioSamples = spf * AUDIO_CHANNELS;
                Array.Clear(_mixedAudioBuffer, 0, audioSamples);
                Deck ad = _activeDeck;
                if (ad != null && ad.IsPlaying && ad.Volume > 0) MixDeckAudio(ad, spf);
                _audioDelay.Process(_mixedAudioBuffer, 0, audioSamples);
                float fl = 0, fr = 0;
                for (int i = 0; i < spf; i++) { float l = Math.Abs(_mixedAudioBuffer[i * 2]); float r = Math.Abs(_mixedAudioBuffer[i * 2 + 1]); if (l > fl) fl = l; if (r > fr) fr = r; }
                _vuLeft = Math.Max(fl, _vuLeft * VU_DECAY); _vuRight = Math.Max(fr, _vuRight * VU_DECAY);
                _vuLeftPeak = Math.Max(fl, _vuLeftPeak * VU_PEAK_DECAY); _vuRightPeak = Math.Max(fr, _vuRightPeak * VU_PEAK_DECAY);
                SendAudio(spf);

                // ═════ VIDEO ═════
                IntPtr programSrc = IntPtr.Zero;
                if (ad != null && ad.IsReadyForVideo)
                    programSrc = ad.VideoBufferPtr;
                else if (_bufferShouldShow && _bufferDeck.IsPlaying && _bufferDeck.IsReadyForVideo)
                    programSrc = _bufferDeck.VideoBufferPtr;

                bool lannerNow = _lannerEnabled && _lannerActive && _lannerImageBGRA != null;

                Array.Clear(_cgFullFrame, 0, _cgFullFrame.Length);
                try { CGRenderer.RenderOverlay(_cgFullFrame, _ndiWidth, _ndiHeight); } catch { }

                if (lannerNow)
                {
                    if (_lannerBgDirty)
                    {
                        byte[] li = _lannerImageBGRA;
                        if (li != null)
                        {
                            int sz = _ndiWidth * _ndiHeight * 4;
                            unsafe { fixed (byte* src = li) Buffer.MemoryCopy(src, (void*)_compositedVideoPtr, sz, Math.Min(li.Length, sz)); }
                        }
                        _lannerBgDirty = false;
                    }
                    else
                    {
                        RepaintLannerPipZone();
                    }

                    if (programSrc != IntPtr.Zero)
                        BlitPipDirect(programSrc, _compositedVideoPtr);

                    BlitCgScaledOverPip();
                }
                else
                {
                    if (programSrc != IntPtr.Zero)
                    {
                        int sz = _ndiWidth * _ndiHeight * 4;
                        unsafe { Buffer.MemoryCopy((void*)programSrc, (void*)_compositedVideoPtr, sz, sz); }
                    }
                    BlitCgFullscreen();
                }

                SendVideo();
            }
        }

        private void RepaintLannerPipZone()
        {
            byte[] li = _lannerImageBGRA;
            if (li == null) return;
            int dstX = _pipX, dstY = _pipY, pw = _pipW, ph = _pipH;
            int outStride = _ndiWidth * 4;
            int srcStride = _ndiWidth * 4;
            unsafe
            {
                byte* dst = (byte*)_compositedVideoPtr.ToPointer();
                fixed (byte* src = li)
                {
                    for (int y = 0; y < ph; y++)
                    {
                        byte* srcRow = src + (dstY + y) * srcStride + dstX * 4;
                        byte* dstRow = dst + (dstY + y) * outStride + dstX * 4;
                        Buffer.MemoryCopy(srcRow, dstRow, pw * 4, pw * 4);
                    }
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        // CG COMPOSITING
        // ═══════════════════════════════════════════════════════════
        private void BlitCgFullscreen()
        {
            int totalPixels = _ndiWidth * _ndiHeight;
            unsafe
            {
                byte* dst = (byte*)_compositedVideoPtr.ToPointer();
                fixed (byte* cg = _cgFullFrame)
                {
                    for (int i = 0; i < totalPixels; i++)
                    {
                        int off = i * 4;
                        int alpha = cg[off + 3];
                        if (alpha == 0) continue;
                        if (alpha == 255) { *(int*)(dst + off) = *(int*)(cg + off); }
                        else
                        {
                            int invA = 255 - alpha;
                            dst[off] = (byte)((cg[off] * alpha + dst[off] * invA + 128) >> 8);
                            dst[off + 1] = (byte)((cg[off + 1] * alpha + dst[off + 1] * invA + 128) >> 8);
                            dst[off + 2] = (byte)((cg[off + 2] * alpha + dst[off + 2] * invA + 128) >> 8);
                            dst[off + 3] = 255;
                        }
                    }
                }
            }
        }

        private void BlitCgScaledOverPip()
        {
            int srcW = _ndiWidth, srcH = _ndiHeight;
            int dstW = _pipW, dstH = _pipH, dstX = _pipX, dstY = _pipY;
            int outStride = _ndiWidth * 4, cgStride = srcW * 4;
            unsafe
            {
                byte* dst = (byte*)_compositedVideoPtr.ToPointer();
                fixed (byte* cg = _cgFullFrame)
                fixed (int* lut = _pipSrcXLut)
                {
                    for (int y = 0; y < dstH; y++)
                    {
                        int srcY = y * srcH / dstH;
                        byte* cgRow = cg + srcY * cgStride;
                        byte* dstRow = dst + (dstY + y) * outStride + dstX * 4;
                        for (int x = 0; x < dstW; x++)
                        {
                            int srcOff = lut[x] * 4;
                            int alpha = cgRow[srcOff + 3];
                            if (alpha == 0) continue;
                            int dstOff = x * 4;
                            if (alpha == 255) { *(int*)(dstRow + dstOff) = *(int*)(cgRow + srcOff); }
                            else
                            {
                                int invA = 255 - alpha;
                                dstRow[dstOff] = (byte)((cgRow[srcOff] * alpha + dstRow[dstOff] * invA + 128) >> 8);
                                dstRow[dstOff + 1] = (byte)((cgRow[srcOff + 1] * alpha + dstRow[dstOff + 1] * invA + 128) >> 8);
                                dstRow[dstOff + 2] = (byte)((cgRow[srcOff + 2] * alpha + dstRow[dstOff + 2] * invA + 128) >> 8);
                                dstRow[dstOff + 3] = 255;
                            }
                        }
                    }
                }
            }
        }

        private void BlitPipDirect(IntPtr srcFrame, IntPtr dstFrame)
        {
            int srcH = _ndiHeight, dstW = _pipW, dstH = _pipH, dstX = _pipX, dstY = _pipY, outStride4 = _ndiWidth;
            unsafe
            {
                int* src = (int*)srcFrame.ToPointer();
                int* dst2 = (int*)dstFrame.ToPointer();
                fixed (int* lut = _pipSrcXLut)
                {
                    for (int y = 0; y < dstH; y++)
                    {
                        int srcY = y * srcH / dstH;
                        int* srcRow = src + srcY * outStride4;
                        int* dstRow = dst2 + (dstY + y) * outStride4 + dstX;
                        for (int x = 0; x < dstW; x++) dstRow[x] = srcRow[lut[x]];
                    }
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        // NDI SEND + AUDIO MIRROR
        // ═══════════════════════════════════════════════════════════
        private void SendAudio(int ns)
        {
            // 1. Invia via NDI (identico alla versione OLD)
            if (_ndiSender != null)
            {
                try
                {
                    using (var f = new AudioFrame(ns, AUDIO_SAMPLE_RATE, AUDIO_CHANNELS))
                    {
                        unsafe
                        {
                            float* dest = (float*)f.AudioBuffer.ToPointer(), pL = dest, pR = dest + ns;
                            for (int i = 0; i < ns; i++)
                            {
                                float l = _mixedAudioBuffer[i * 2], r = _mixedAudioBuffer[i * 2 + 1];
                                if (l > 1) l = 1; else if (l < -1) l = -1;
                                if (r > 1) r = 1; else if (r < -1) r = -1;
                                *pL++ = l; *pR++ = r;
                            }
                        }
                        f.TimeStamp = (_audioSamplesSent * 10_000_000L) / AUDIO_SAMPLE_RATE;
                        _ndiSender.Send(f);
                        _audioSamplesSent += ns;
                    }
                }
                catch { }
            }

            // 2. Mirror audio locale (alimenta direttamente il BufferedWaveProvider)
            MirrorAudioDirect(ns);
        }

        /// <summary>
        /// Copia l'audio mixato direttamente nel BufferedWaveProvider dell'uscita locale.
        /// Chiamato dall'engine loop con timing preciso (ogni ~33ms a 30fps).
        /// BufferedWaveProvider.AddSamples è thread-safe.
        /// Nessuna allocazione, nessun thread intermedio, nessun jitter.
        /// </summary>
        private void MirrorAudioDirect(int ns)
        {
            var provider = _audioMirrorBuffer;
            var convertBuf = _audioMirrorConvertBuffer;
            if (provider == null || convertBuf == null) return;
            try
            {
                int sampleCount = ns * AUDIO_CHANNELS;
                int byteCount = sampleCount * sizeof(float);
                if (byteCount > convertBuf.Length) return;
                Buffer.BlockCopy(_mixedAudioBuffer, 0, convertBuf, 0, byteCount);
                provider.AddSamples(convertBuf, 0, byteCount);
            }
            catch { }
        }

        private void SendVideo() { if (_ndiSender == null) return; try { using (var f = new VideoFrame(_compositedVideoPtr, _ndiWidth, _ndiHeight, _ndiWidth * 4, NDIlib.FourCC_type_e.FourCC_type_BGRX, (float)_ndiWidth / _ndiHeight, _ndiFrameRate, 1, NDIlib.frame_format_type_e.frame_format_type_progressive)) { f.TimeStamp = (_framesSent * 10_000_000L) / _ndiFrameRate; _ndiSender.Send(f); _framesSent++; } } catch { } }

        // ═══════════════════════════════════════════════════════════
        // TRANSITION
        // ═══════════════════════════════════════════════════════════
        private void CheckPendingTransition()
        {
            Deck p = _pendingDeck; if (p == null || !p.IsPendingActivation) return;
            bool ready;
            if (p.Type == DeckType.VideoClip) ready = p.FrameCount >= MIN_READY_FRAMES && p.WarmupDone;
            else if (p.Type == DeckType.AudioTrack && p.VlcPlayer.IsPlaying) ready = p.FrameCount >= MIN_READY_FRAMES;
            else if (p.Type == DeckType.WebStream) ready = p.WarmupDone;
            else ready = true;
            if (!ready) return;
            Deck old = _activeDeck; _activeDeck = p; _pendingDeck = null; p.IsPendingActivation = false;
            if (p.IsPreBuffered) { p.IsPreBuffered = false; p.PreBufferReady = false; ThreadPool.QueueUserWorkItem(_ => { try { p.VlcPlayer.SetPause(false); } catch { } }); }
            Log("[TRANS] " + p.Name + " → ACTIVE (type=" + p.Type + ")");
            ResetMixTrigger();
            if (_lannerActive) _lannerBgDirty = true;
            if (p.Type == DeckType.WebStream) { p.StreamClock.Restart(); Log("[TRANS] WebStream clock started, MIX=" + _markerMIX + "ms OUT=" + _markerOUT + "ms"); }
            SafeInvoke(() => { _mixCheckTimer.Stop(); _mixCheckTimer.Start(); });
            if (old != null && old != p && old.IsPlaying) { old.Volume = 0; old.IsPlaying = false; ThreadPool.QueueUserWorkItem(_ => { Thread.Sleep(80); _commandQueue.Enqueue(() => { StopDeckInternal(old, true); }); }); }
        }

        private void HandleDeckEnded(Deck deck, int sessionId) { if (deck.SessionId != sessionId || deck != _activeDeck) return; if (!TryTriggerMix()) return; Log("[END] " + deck.Name + " ended"); SafeInvoke(() => OnTrackEnded()); }

        // ═══════════════════════════════════════════════════════════
        // POSITION
        // ═══════════════════════════════════════════════════════════
        private void UpdatePosition()
        {
            if (!_isPlaying || _isPaused || IsMixTriggered) return;
            Deck d = _activeDeck; if (d == null || !d.IsPlaying) return;

            if (d.Type == DeckType.WebStream)
            {
                if (!d.StreamClock.IsRunning) return;
                int ms = (int)d.StreamClock.ElapsedMilliseconds;
                _positionMs = ms;
                int outMs = _markerOUT > 0
                    ? _markerOUT
                    : (int)(d.CurrentItem?.Duration.TotalMilliseconds ?? 0);
                if (_autoMode && _markerMIX > 0 && ms >= _markerMIX) { if (TryTriggerMix()) { Log("[POS] WebStream MIX at " + ms + "ms"); SafeInvoke(() => { _mixCheckTimer.Stop(); OnMixReached(); }); } return; }
                if (outMs > 0 && ms >= outMs) { if (TryTriggerMix()) { Log("[POS] WebStream OUT at " + ms + "ms"); SafeInvoke(() => { _mixCheckTimer.Stop(); if (_autoMode) OnMixReached(); else { _isPlaying = false; OnTrackEnded(); } }); } }
                return;
            }

            int fileMs = 0;
            try { if (d.Type == DeckType.VideoClip) fileMs = (int)d.VlcTimeMs; else if (d.FileReader != null) fileMs = (int)d.FileReader.CurrentTime.TotalMilliseconds; } catch { return; }
            if (fileMs > 0) _positionMs = fileMs;
            if (_autoMode && _markerMIX > 0 && fileMs >= _markerMIX) { if (TryTriggerMix()) { SafeInvoke(() => { _mixCheckTimer.Stop(); OnMixReached(); }); } return; }
            if (_markerOUT > 0 && fileMs >= _markerOUT) { if (TryTriggerMix()) { SafeInvoke(() => { _mixCheckTimer.Stop(); if (_autoMode) OnMixReached(); else { _isPlaying = false; OnTrackEnded(); } }); } return; }
            if (d.Type == DeckType.AudioTrack && d.FileReader != null && d.FileReader.Position >= d.FileReader.Length) _commandQueue.Enqueue(() => HandleDeckEnded(d, d.SessionId));
        }

        // ═══════════════════════════════════════════════════════════
        // AUDIO MIXER
        // ═══════════════════════════════════════════════════════════
        private void MixDeckAudio(Deck d, int spf)
        {
            int tot = spf * AUDIO_CHANNELS; float vol = d.Volume;
            if (d.Type == DeckType.AudioTrack && d.Resampler != null) { try { int rd = d.Resampler.Read(_tempReadBuffer, 0, tot); for (int i = 0; i < rd; i++) _mixedAudioBuffer[i] += _tempReadBuffer[i] * vol; } catch { } return; }
            if ((d.Type == DeckType.VideoClip || d.Type == DeckType.WebStream) && d.WarmupDone) { int read = d.Ring.Read(_tempReadBuffer, 0, tot); for (int i = 0; i < read; i++) _mixedAudioBuffer[i] += _tempReadBuffer[i] * vol; }
        }

        // ═══════════════════════════════════════════════════════════
        // PRE-BUFFER
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// FIX: Pre-buffer SOLO file video nativi (.mp4, .avi, .mov, ecc.)
        /// 
        /// I file audio (.mp3, .wav, ecc.) con VideoFilePath associato NON vengono
        /// pre-bufferizzati, perché il pre-buffer li trattava come VideoClip (audio VLC)
        /// mentre PlayInternal li gestisce come AudioTrack (audio NAudio).
        /// Questa incoerenza causava il bug "video senza audio":
        /// - PreBuffer avvia VLC con video+audio → tipo VideoClip
        /// - PlayInternal usa il pre-buffer → MixDeckAudio legge dal Ring (VLC audio)
        /// - Ma VLC è in pausa dal pre-buffer → Ring vuoto → silenzio
        /// 
        /// Soluzione: pre-bufferizzare solo file che sono video nativi (IsVideoFile=true).
        /// I file audio con video associato sono comunque brevi (jingle) e partono velocemente.
        /// </summary>
        /// <summary>
        /// Fase 1 (ThreadPool): valida il prossimo file da pre-bufferizzare.
        /// Solo la validazione file (I/O) avviene qui, poi enqueue del setup al command queue.
        /// </summary>
        private void PreBufferNextAsync()
        {
            if (_playlistQueue == null) return;
            List<PlaylistQueueItem> items;
            try { items = _playlistQueue.GetAllItems(); } catch (Exception ex) { Log("[PREBUF] GetAllItems failed: " + ex.Message); return; }
            if (items.Count < 2) return;

            int nextIndex = -1;
            for (int i = 1; i < items.Count; i++)
            {
                string fp = items[i].FilePath;
                if (string.IsNullOrEmpty(fp) || IsWebStream(fp)) continue;

                if (!IsVideoFile(fp)) continue;

                if (!File.Exists(fp))
                {
                    Log("[PREBUF] ⚠️ File non trovato, skip: " + fp);
                    continue;
                }

                try
                {
                    var fi = new FileInfo(fp);
                    if (fi.Length < 20 * 1024)
                    {
                        Log("[PREBUF] ⚠️ File troppo piccolo (" + fi.Length + " bytes), skip: " + Path.GetFileName(fp));
                        continue;
                    }
                }
                catch
                {
                    Log("[PREBUF] ⚠️ Impossibile leggere info file, skip: " + fp);
                    continue;
                }

                nextIndex = i;
                break;
            }

            if (nextIndex < 0) return;

            int capturedIdx = nextIndex;
            var capturedItem = items[capturedIdx];
            string capturedFile = capturedItem.FilePath;
            _commandQueue.Enqueue(() => PreBufferNextSetup(capturedFile, capturedItem));
        }

        /// <summary>
        /// Fase 2 (Engine thread): setup deck per il pre-buffer.
        /// Nessun I/O su disco, solo operazioni in memoria e VLC.
        /// </summary>
        private void PreBufferNextSetup(string nextFile, PlaylistQueueItem nextItem)
        {
            if (_preBufferedFile == nextFile && _preBufferedDeck != null) return;

            Deck target = null;
            if (_deckA != _activeDeck && _deckA != _pendingDeck && !_deckA.IsPlaying) target = _deckA;
            else if (_deckB != _activeDeck && _deckB != _pendingDeck && !_deckB.IsPlaying) target = _deckB;
            if (target == null) return;

            if (_preBufferedDeck != null && _preBufferedDeck != _activeDeck && _preBufferedDeck != target)
            { StopDeckInternal(_preBufferedDeck, true); _preBufferedDeck = null; _preBufferedFile = ""; }

            StopDeckInternal(target, true);
            target.SessionId = ++_globalSessionId;
            target.Type = DeckType.VideoClip;
            target.IsPreBuffered = true;
            target.PreBufferReady = false;
            target.StartPointMs = nextItem.MarkerIN;

            var media = new Media(_libVLC, nextFile, FromType.FromPath);
            if (nextItem.MarkerIN > 0) media.AddOption($":start-time={nextItem.MarkerIN / 1000.0:F3}");
            media.AddOption(":file-caching=500");
            target.VlcPlayer.Media = media;
            media.Dispose();
            target.VlcPlayer.Play();
            target.WebStreamRetryCount = 0;
            target.CurrentItem = PlayItem.FromQueueItem(nextItem);

            Log("[PREBUF] Pre-buffering VideoClip: " + Path.GetFileName(nextFile) + " → " + target.Name);

            ThreadPool.QueueUserWorkItem(_ =>
            {
                int w = 0;
                while (w < 3000 && target.IsPreBuffered && (target.FrameCount < MIN_READY_FRAMES || !target.WarmupDone))
                { Thread.Sleep(20); w += 20; }
                if (target.IsPreBuffered)
                { try { target.VlcPlayer.SetPause(true); } catch { } target.PreBufferReady = true; }
            });

            _preBufferedDeck = target;
            _preBufferedFile = nextFile;
        }

        // ═══════════════════════════════════════════════════════════
        // PLAY
        // ═══════════════════════════════════════════════════════════
        public void LoadAndPlay(PlaylistQueueItem qi) { if (qi == null || _isDisposed) return; _commandQueue.Enqueue(() => PlayInternal(PlayItem.FromQueueItem(qi))); }
        public void LoadTrack(string fp, string artist, string title, TimeSpan intro, int mIN, int mINTRO, int mMIX, int mOUT, string type) { _commandQueue.Enqueue(() => PlayInternal(new PlayItem { FilePath = fp ?? "", Artist = artist ?? "", Title = title ?? "", Intro = intro, MarkerIN = mIN, MarkerINTRO = mINTRO, MarkerMIX = mMIX, MarkerOUT = mOUT, ItemType = type ?? "Clip" })); }

        private void PlayInternal(PlayItem item)
        {
            string fp = item.FilePath, fn = Path.GetFileName(fp);
            SafeInvoke(() => _mixCheckTimer.Stop());
            Log("[PLAY] " + fn + " | " + item.Artist + " - " + item.Title + " | IN=" + item.MarkerIN + " MIX=" + item.MarkerMIX + " OUT=" + item.MarkerOUT + " Dur=" + item.Duration.TotalMilliseconds + "ms");
            bool isStream = IsWebStream(fp);

            if (!isStream && !File.Exists(fp))
            {
                Log("[PLAY] ⚠️ File non trovato, skip: " + fp);
                ResetMixTrigger();
                SafeInvoke(() => AutoSkipToNext());
                return;
            }

            if (!isStream)
            {
                try
                {
                    var fi = new FileInfo(fp);
                    if (fi.Length < 20 * 1024)
                    {
                        Log("[PLAY] ⚠️ File troppo piccolo (" + fi.Length + " bytes), skip: " + Path.GetFileName(fp));
                        ResetMixTrigger();
                        SafeInvoke(() => AutoSkipToNext());
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Log("[PLAY] ⚠️ Impossibile leggere info file, skip: " + fp + " - " + ex.Message);
                    ResetMixTrigger();
                    SafeInvoke(() => AutoSkipToNext());
                    return;
                }
            }

            bool isVideo = !isStream && IsVideoFile(fp);
            string videoFile = !isStream && !string.IsNullOrEmpty(item.VideoFilePath) && File.Exists(item.VideoFilePath) ? item.VideoFilePath : null;
            bool hasDedVid = isVideo || videoFile != null;
            int sid = ++_globalSessionId; Deck target; bool usedPre = false;
            Interlocked.Exchange(ref _mixTriggeredFlag, 1); _mixGeneration++; _audioDelay.Reset();
            double startTimeSec = item.MarkerIN / 1000.0;

            _bufferIsDedicatedVideo = false;

            if (isStream)
            {
                bool isVideoStream = item.IsVideoStream;
                if (_preBufferedDeck != null) { StopDeckInternal(_preBufferedDeck, true); _preBufferedDeck = null; _preBufferedFile = ""; }
                target = _nextIsA ? _deckA : _deckB; if (target == _activeDeck) target = !_nextIsA ? _deckA : _deckB; _nextIsA = !_nextIsA;
                if (_pendingDeck != null && _pendingDeck != target && _pendingDeck != _activeDeck) { StopDeckInternal(_pendingDeck, true); _pendingDeck = null; }
                StopDeckInternal(target, true); target.SessionId = sid; target.Type = DeckType.WebStream;
                target.IsVideoStream = isVideoStream;
                string mediaUrl = fp;
                string? relayUrl = null;
                bool needsRelay = ShouldUseHlsRelay(fp);
                if (needsRelay && _hlsRelay != null)
                {
                    try
                    {
                        mediaUrl = _hlsRelay.Register(fp);
                        relayUrl = mediaUrl;
                        Log("[STREAM] relay: " + fp + " → " + mediaUrl);
                    }
                    catch (Exception ex)
                    {
                        Log("[STREAM] ⚠️ Relay register failed, using original: " + ex.Message);
                    }
                }
                else
                {
                    Log($"[STREAM] relay bypass (non-HLS): {fp}");
                }
                target.RelayUrl = relayUrl;
                var media = new Media(_libVLC, mediaUrl, FromType.FromLocation);
                if (!isVideoStream) media.AddOption(":no-video");
                media.AddOption(":network-caching=3000");
                media.AddOption(":clock-jitter=0");
                media.AddOption(":clock-synchro=0");
                if (!string.IsNullOrEmpty(relayUrl)) media.AddOption(":demux=ts");
                target.VlcPlayer.Media = media; media.Dispose(); target.VlcPlayer.Play();
                if (isVideoStream)
                {
                    _bufferShouldShow = false; _currentFileIsVideo = true;
                    StopBufferDeck();
                }
                else
                {
                    _bufferShouldShow = true; _currentFileIsVideo = false;
                    EnsureBufferVideoPlaying();
                }
                target.Volume = 1.0f; target.IsPlaying = true; target.WebStreamRetryCount = 0; target.CurrentItem = item; target.IsPendingActivation = true; _pendingDeck = target;
                Log("[PLAY] WebStream " + (isVideoStream ? "VIDEO" : "AUDIO") + " → " + target.Name);
                int streamSid = sid;
                var streamTarget = target;
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    Thread.Sleep(25000);
                    if (_isDisposed) return;
                    if (streamTarget.SessionId == streamSid && !streamTarget.WarmupDone && _pendingDeck == streamTarget)
                    {
                        Log("[STREAM] ❌ Timeout 25s senza audio, skip: " + fp);
                        _commandQueue.Enqueue(() =>
                        {
                            try
                            {
                                if (_pendingDeck == streamTarget) { StopDeckInternal(streamTarget, true); _pendingDeck = null; }
                                SafeInvoke(() => { if (_autoMode) OnMixReached(); else AutoSkipToNext(); });
                            }
                            catch (Exception ex) { LogErr("[STREAM Timeout]", ex); }
                        });
                    }
                });
            }
            else if (_preBufferedDeck != null && _preBufferedFile == fp && _preBufferedDeck.PreBufferReady)
            {
                target = _preBufferedDeck; _preBufferedDeck = null; _preBufferedFile = ""; usedPre = true;
                target.SessionId = sid; target.IsPlaying = true; target.Volume = 1.0f; target.IsPendingActivation = true; target.WebStreamRetryCount = 0; target.CurrentItem = item; target.StartPointMs = item.MarkerIN;
                _pendingDeck = target; _bufferShouldShow = false; _currentFileIsVideo = true;
                Log("[PLAY] Using pre-buffer (VideoClip) → " + target.Name);
            }
            else
            {
                target = _nextIsA ? _deckA : _deckB; if (target == _activeDeck) target = !_nextIsA ? _deckA : _deckB; _nextIsA = !_nextIsA;
                if (_preBufferedDeck == target) { StopDeckInternal(_preBufferedDeck, true); _preBufferedDeck = null; _preBufferedFile = ""; }
                if (_pendingDeck != null && _pendingDeck != target && _pendingDeck != _activeDeck) { StopDeckInternal(_pendingDeck, true); _pendingDeck = null; }
                StopDeckInternal(target, true); target.SessionId = sid; target.StartPointMs = item.MarkerIN;

                if (isVideo)
                {
                    target.Type = DeckType.VideoClip; _bufferShouldShow = false;
                    var m = new Media(_libVLC, fp, FromType.FromPath); if (item.MarkerIN > 0) m.AddOption($":start-time={startTimeSec:F3}"); m.AddOption(":file-caching=500");
                    target.VlcPlayer.Media = m; m.Dispose(); target.VlcPlayer.Play(); _currentFileIsVideo = true;
                    Log("[PLAY] VideoClip → " + target.Name);
                }
                else if (videoFile != null)
                {
                    target.Type = DeckType.AudioTrack; _bufferShouldShow = false;
                    var vm = new Media(_libVLC, videoFile, FromType.FromPath); vm.AddOption(":no-audio"); if (item.MarkerIN > 0) vm.AddOption($":start-time={startTimeSec:F3}"); vm.AddOption(":file-caching=500");
                    target.VlcPlayer.Media = vm; vm.Dispose(); target.VlcPlayer.Play();
                    try { target.FileReader = new AudioFileReader(fp); if (item.MarkerIN > 0) target.FileReader.CurrentTime = TimeSpan.FromMilliseconds(item.MarkerIN); var rs = new WdlResamplingSampleProvider(target.FileReader, AUDIO_SAMPLE_RATE); target.Resampler = target.FileReader.WaveFormat.Channels == 1 ? rs.ToStereo() : (ISampleProvider)rs; target.AudioStarted = true; } catch (Exception ex) { LogErr("[PLAY] NAudio", ex); }
                    _currentFileIsVideo = true;
                    Log("[PLAY] Video+NAudio → " + target.Name);
                }
                else
                {
                    target.Type = DeckType.AudioTrack;
                    _bufferShouldShow = true;

                    try
                    {
                        target.FileReader = new AudioFileReader(fp);
                        if (item.MarkerIN > 0)
                            target.FileReader.CurrentTime = TimeSpan.FromMilliseconds(item.MarkerIN);
                        var rs = new WdlResamplingSampleProvider(target.FileReader, AUDIO_SAMPLE_RATE);
                        target.Resampler = target.FileReader.WaveFormat.Channels == 1 ? rs.ToStereo() : (ISampleProvider)rs;
                        target.AudioStarted = true;
                    }
                    catch (Exception ex)
                    {
                        LogErr("[PLAY] NAudio failed", ex);
                        ResetMixTrigger();
                        SafeInvoke(() => AutoSkipToNext());
                        return;
                    }

                    _currentFileIsVideo = false;

                    StopBufferDeck();

                    string mv = Path.ChangeExtension(fp, ".mp4");
                    if (File.Exists(mv))
                    {
                        Log("[PLAY] AudioOnly + MusicVideo → " + target.Name + " | " + Path.GetFileName(mv));
                        _bufferIsDedicatedVideo = true;
                        StartBufferVideo(mv, false);
                    }
                    else
                    {
                        Log("[PLAY] AudioOnly + GenericBuffer → " + target.Name);
                        _bufferIsDedicatedVideo = false;
                        EnsureBufferVideoPlaying();
                    }
                }

                target.Volume = 1.0f; target.IsPlaying = true; target.WebStreamRetryCount = 0; target.CurrentItem = item;

                if (hasDedVid)
                {
                    target.IsPendingActivation = true; _pendingDeck = target;
                }
                else
                {
                    Deck old = _activeDeck;
                    _activeDeck = target;
                    _pendingDeck = null;
                    target.IsPendingActivation = false;
                    if (old != null && old != target) StopDeckInternal(old, true);
                    ResetMixTrigger();
                    SafeInvoke(() => _mixCheckTimer.Start());
                }
            }

            _waveformPeaks = null; _waveformCurrentFile = "";
            // Durata totale file: priorità FileReader > FileDurationMs > item.Duration
            TimeSpan dur = TimeSpan.Zero;
            if (target.FileReader != null && target.FileReader.TotalTime.TotalMilliseconds > 100)
                dur = target.FileReader.TotalTime;
            if (dur.TotalMilliseconds < 100 && item.FileDurationMs > 100)
                dur = TimeSpan.FromMilliseconds(item.FileDurationMs);
            if (dur.TotalMilliseconds < 100) dur = item.Duration;
            if (dur.TotalMilliseconds < 100 && item.MarkerOUT > 0) dur = TimeSpan.FromMilliseconds(item.MarkerOUT);
            if (dur.TotalMilliseconds < 100 && item.MarkerMIX > 0) dur = TimeSpan.FromMilliseconds(item.MarkerMIX);
            _totalDuration = dur; _markerIN = item.MarkerIN; _markerINTRO = item.MarkerINTRO;
            _markerMIX = item.MarkerMIX;
            _markerOUT = item.MarkerOUT;
            _introTime = item.Intro; if (_introTime.TotalMilliseconds <= 0 && _markerINTRO > 0) _introTime = TimeSpan.FromMilliseconds(Math.Max(0, _markerINTRO - _markerIN));
            _isPlaying = true; _isPaused = false; _positionMs = item.MarkerIN > 0 ? item.MarkerIN : 0;
            _currentFile = fp; CurrentFilePath = fp; CurrentArtist = item.Artist; CurrentTitle = item.Title;
            Log("[PLAY] ✓ " + fn + (usedPre ? " [PREBUF]" : "") + (isStream ? " [STREAM]" : "") + " dur=" + dur.TotalSeconds.ToString("F0") + "s MIX=" + _markerMIX + " OUT=" + _markerOUT + " bufferShow=" + _bufferShouldShow + " bufferPlaying=" + _bufferDeck.IsPlaying + " bufferDedicated=" + _bufferIsDedicatedVideo);
            if (!isStream) LoadWaveformForCurrentFile(fp);
            SafeInvoke(() => { _updateTimer.Start(); UpdateTrackDisplay(item); UpdateBtnStates(); waveformPanel.Invalidate(); });
            TrackChanged?.Invoke(this, new TrackChangedEventArgs { FilePath = fp, Artist = CurrentArtist, Title = CurrentTitle, IsVideo = _currentFileIsVideo, ItemType = item.ItemType, VideoFilePath = item.VideoFilePath, VideoSource = item.VideoSource, NDISourceName = item.NDISourceName, MarkerIN = _markerIN, MarkerMIX = _markerMIX, MarkerOUT = _markerOUT });
            PlayStateChanged?.Invoke(this, new PlayStateChangedEventArgs { IsPlaying = true, IsPaused = false, FilePath = fp });
            UpdateCG(item); SendMeta(item); if (!isStream) PreCacheNextWaveform();
            ThreadPool.QueueUserWorkItem(_ => { Thread.Sleep(500); PreBufferNextAsync(); });
        }

        private void StopBufferDeck()
        {
            try { if (_bufferDeck.VlcPlayer.IsPlaying) _bufferDeck.VlcPlayer.Stop(); } catch { }
            _bufferDeck.IsPlaying = false;
            _bufferDeck.IsReadyForVideo = false;
            _bufferDeck.FrameCount = 0;
            ClearVideoBuffer(_bufferDeck.VideoBufferPtr);
        }

        private void StartBufferVideo(string path, bool loop)
        {
            var m = new Media(_libVLC, path, FromType.FromPath);
            m.AddOption(":no-audio");
            if (loop) m.AddOption(":input-repeat=65535");
            _bufferDeck.VlcPlayer.Media = m;
            m.Dispose();
            _bufferDeck.VlcPlayer.Play();
            _bufferDeck.IsPlaying = true;
            Log("[BUFFER] Started: " + Path.GetFileName(path) + " loop=" + loop);
        }

        private void EnsureBufferVideoPlaying()
        {
            if (_bufferDeck.IsPlaying && _bufferDeck.IsReadyForVideo) return;
            if (_bufferPlaylist.Count == 0) RefreshBufferPlaylist();
            if (_bufferPlaylist.Count == 0) { Log("[BUFFER] No buffer videos available"); return; }
            PlayNextBufferVideo();
        }

        private void AutoSkipToNext()
        {
            if (_playlistQueue == null) { Stop(); return; }

            int maxAttempts = 50;
            int attempts = 0;

            while (attempts < maxAttempts)
            {
                attempts++;
                var items = _playlistQueue.GetAllItems();

                if (items.Count < 2)
                {
                    Stop();
                    return;
                }

                _playlistQueue.RemoveItem(0);

                items = _playlistQueue.GetAllItems();
                if (items.Count == 0) { Stop(); return; }

                var next = items[0];
                string fp = next.FilePath ?? "";
                bool isStream = IsWebStream(fp);

                if (isStream)
                {
                    _playlistQueue.SetCurrentPlaying(0);
                    LoadAndPlay(next);
                    return;
                }

                if (!File.Exists(fp))
                {
                    Log("[SKIP] ⚠️ File non trovato, skip: " + fp);
                    continue;
                }

                try
                {
                    var fi = new FileInfo(fp);
                    if (fi.Length < 25 * 1024)
                    {
                        Log("[SKIP] ⚠️ File troppo piccolo (" + fi.Length + " bytes), skip: " + Path.GetFileName(fp));
                        continue;
                    }
                }
                catch
                {
                    Log("[SKIP] ⚠️ Impossibile leggere info file, skip: " + fp);
                    continue;
                }

                _playlistQueue.SetCurrentPlaying(0);
                LoadAndPlay(next);
                return;
            }

            Log("[SKIP] ❌ Nessun file valido trovato dopo " + maxAttempts + " tentativi");
            Stop();
        }

        // ═══════════════════════════════════════════════════════════
        // BUFFER VIDEO
        // ═══════════════════════════════════════════════════════════
        private void RefreshBufferPlaylist()
        {
            _bufferPlaylist.Clear();
            if (!string.IsNullOrEmpty(_bufferVideoPath) && Directory.Exists(_bufferVideoPath))
                _bufferPlaylist.AddRange(Directory.GetFiles(_bufferVideoPath, "*.*")
                    .Where(f => f.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ||
                                f.EndsWith(".mov", StringComparison.OrdinalIgnoreCase) ||
                                f.EndsWith(".avi", StringComparison.OrdinalIgnoreCase)));
            Log("[BUFFER] " + _bufferPlaylist.Count + " files in " + (_bufferVideoPath ?? "null"));
        }

        private void PlayNextBufferVideo()
        {
            if (_bufferPlaylist.Count == 0 || !_bufferShouldShow) return;
            string f = _bufferPlaylist[_bufferRandom.Next(_bufferPlaylist.Count)];
            Log("[BUFFER] Playing generic: " + Path.GetFileName(f));
            _bufferIsDedicatedVideo = false;
            var m = new Media(_libVLC, f, FromType.FromPath); m.AddOption(":input-repeat=65535"); m.AddOption(":no-audio");
            _bufferDeck.VlcPlayer.Media = m; m.Dispose(); _bufferDeck.VlcPlayer.Play(); _bufferDeck.IsPlaying = true;
        }

        // ═══════════════════════════════════════════════════════════
        // STOP / PAUSE / RESUME / PLAY / SKIP
        // ═══════════════════════════════════════════════════════════
        public void Stop()
        {
            _isPlaying = false; _isPaused = false; ResetMixTrigger();
            SafeInvoke(() => { _updateTimer.Stop(); _mixCheckTimer.Stop(); _blinkTimer.Stop(); UpdateBtnStates(); lblIntro.Text = "--:--"; lblIntro.ForeColor = AppTheme.LEDYellow; lblIntro.BackColor = Color.Black; lblRemaining.BackColor = Color.Black; lblRemaining.ForeColor = AppTheme.LEDRed; lblElapsed.Text = "--:--"; lblRemaining.Text = "--:--"; _waveformPeaks = null; _waveformCurrentFile = ""; _positionMs = 0; _totalDuration = TimeSpan.Zero; _markerIN = 0; _markerOUT = 0; _markerMIX = 0; waveformPanel.Invalidate(); });
            _commandQueue.Enqueue(() =>
            {
                if (_pendingDeck != null) { StopDeckInternal(_pendingDeck, true); _pendingDeck = null; }
                if (_preBufferedDeck != null) { StopDeckInternal(_preBufferedDeck, true); _preBufferedDeck = null; _preBufferedFile = ""; }
                StopDeckInternal(_deckA, true); StopDeckInternal(_deckB, true);
                _activeDeck = null; _bufferShouldShow = false; _bufferIsDedicatedVideo = false;
                StopBufferDeck();
                _vuLeft = 0; _vuRight = 0; _vuLeftPeak = 0; _vuRightPeak = 0; _audioDelay.Reset();
            });
            StopRequested?.Invoke(this, EventArgs.Empty); PlayStateChanged?.Invoke(this, new PlayStateChangedEventArgs { IsPlaying = false, IsPaused = false, FilePath = CurrentFilePath });
        }

        public void Pause()
        {
            if (!_isPlaying || _isPaused) return; _isPaused = true;
            _commandQueue.Enqueue(() =>
            {
                var d = _activeDeck;
                if (d != null)
                {
                    if (d.Type == DeckType.WebStream) d.StreamClock.Stop();
                    if (d.Type == DeckType.VideoClip || d.Type == DeckType.WebStream)
                        ThreadPool.QueueUserWorkItem(_ => { try { d.VlcPlayer.SetPause(true); } catch { } });
                }
            });
            SafeInvoke(() => { _updateTimer.Stop(); _mixCheckTimer.Stop(); UpdateBtnStates(); });
            PauseRequested?.Invoke(this, EventArgs.Empty);
        }

        public void Resume()
        {
            if (!_isPaused) return; _isPaused = false;
            _commandQueue.Enqueue(() =>
            {
                var d = _activeDeck;
                if (d != null)
                {
                    if (d.Type == DeckType.WebStream) d.StreamClock.Start();
                    if (d.Type == DeckType.VideoClip || d.Type == DeckType.WebStream)
                        ThreadPool.QueueUserWorkItem(_ => { try { d.VlcPlayer.SetPause(false); } catch { } });
                }
            });
            SafeInvoke(() => { _updateTimer.Start(); _mixCheckTimer.Start(); UpdateBtnStates(); });
        }

        public void Play() { if (_isPaused) { Resume(); return; } if (!_isPlaying && _playlistQueue != null && _playlistQueue.GetItemCount() > 0) { var i = _playlistQueue.GetAllItems(); if (i.Count > 0) { LoadAndPlay(i[0]); _playlistQueue.SetCurrentPlaying(0); } PlayRequested?.Invoke(this, EventArgs.Empty); } }

        public void NotifyQueueItemsAvailable()
        {
            SafeInvoke(() =>
            {
                if (_autoMode && !_isPlaying && _autoStartPending && _playlistQueue != null && _playlistQueue.GetItemCount() > 0)
                {
                    Log("[NotifyQueueItemsAvailable] Auto-start pending: avvio riproduzione");
                    _autoStartPending = false;
                    Play();
                }
            });
        }

        private void DrainAndExecuteCommands()
        {
            _playlistQueue?.DrainAndExecuteCommandsAtFront();
        }

        private void SkipToNext()
        {
            if (_playlistQueue == null || _playlistQueue.GetItemCount() < 2) { Stop(); return; }
            SafeInvoke(() => { _mixCheckTimer.Stop(); _updateTimer.Stop(); });
            Interlocked.Exchange(ref _mixTriggeredFlag, 1);
            _positionMs = 0;
            _playlistQueue.RemoveItem(0);
            DrainAndExecuteCommands();
            var items = _playlistQueue.GetAllItems();
            if (items.Count > 0)
            {
                _playlistQueue.SetCurrentPlaying(0);
                LoadAndPlay(items[0]);
            }
            else Stop();
        }

        // ═══════════════════════════════════════════════════════════
        // MIX
        // ═══════════════════════════════════════════════════════════
        private void MixCheckTimer_Tick(object s, EventArgs e) { if (!_isPlaying || _isPaused) return; if (_activeDeck == null || !_activeDeck.IsPlaying) return; if (_autoMode && _markerMIX > 0 && _positionMs >= _markerMIX) { if (TryTriggerMix()) { _mixCheckTimer.Stop(); OnMixReached(); } } }
        private void OnMixReached()
        {
            if (_playlistQueue == null) return;
            _mixCheckTimer.Stop();
            int gen = _mixGeneration;
            var items = _playlistQueue.GetAllItems();
            if (items.Count <= 1) { Log("[MIX] No next"); return; }
            if (gen != _mixGeneration) return;
            _mixGeneration++;
            if (_playlistQueue.GetNextPlayableItem() == null) { Log("[MIX] No next playable"); return; }
            _playlistQueue.RemoveItem(0);
            DrainAndExecuteCommands();
            var next = _playlistQueue.GetAllItems();
            if (next.Count > 0)
            {
                Log("[MIX] → " + (next[0].Title ?? "?"));
                _playlistQueue.SetCurrentPlaying(0);
                LoadAndPlay(next[0]);
            }
            MixPointReached?.Invoke(this, EventArgs.Empty);
        }
        private void OnTrackEnded() { _isPlaying = false; _updateTimer.Stop(); _mixCheckTimer.Stop(); _blinkTimer.Stop(); if (_autoMode) OnMixReached(); else { TrackEndedInManualMode?.Invoke(this, EventArgs.Empty); ClearPlayer(); } }
        public void SeekTo(TimeSpan pos) { if (!_isPlaying) return; _commandQueue.Enqueue(() => { var d = _activeDeck; if (d == null || d.Type == DeckType.WebStream) return; try { if (d.Type == DeckType.VideoClip) d.VlcPlayer.Time = (long)pos.TotalMilliseconds; else if (d.FileReader != null) d.FileReader.CurrentTime = pos; d.Ring.Reset(); d.WarmupDone = false; _positionMs = (int)pos.TotalMilliseconds; _audioDelay.Reset(); } catch { } }); }

        // ═══════════════════════════════════════════════════════════
        // LANNER TV
        // ═══════════════════════════════════════════════════════════
        private void LoadLannerSchedule()
        {
            CheckLannerEnabled();
            if (!_lannerEnabled) { Log("[LANNER] Disabled (NoExternal), skipping schedule load"); return; }

            lock (_lannerLock)
            {
                try
                {
                    _todayLannerSlots.Clear();
                    string db = "";
                    try { using (var k = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AirDirector")) if (k != null) db = k.GetValue("DatabasePath", "").ToString(); } catch { }
                    if (string.IsNullOrWhiteSpace(db)) return;
                    _lannerDbPath = db;
                    string xp = Path.Combine(db, "lannertv.xml"); if (!File.Exists(xp)) return;
                    List<LannerCampaign> all;
                    var ser = new XmlSerializer(typeof(LannerCampaignData));
                    using (var r = new StreamReader(xp)) { var d = (LannerCampaignData)ser.Deserialize(r); all = d?.Campaigns ?? new List<LannerCampaign>(); }
                    DateTime td = DateTime.Now.Date;
                    var ac = all.Where(c => c.StartDate.Date <= td && c.EndDate.Date >= td && !string.IsNullOrEmpty(c.ImagePath) && File.Exists(c.ImagePath)).ToList();
                    if (ac.Count == 0) return;
                    var bt = new Dictionary<string, LannerSlot>();
                    foreach (var c in ac)
                        foreach (var t in c.SlotTimes)
                        {
                            string k2 = t.Trim();
                            if (!bt.ContainsKey(k2)) bt[k2] = new LannerSlot { StartTime = TimeSpan.Parse(k2), DurationMinutes = c.DurationMinutes, ImagePaths = new List<string>(), CampaignNames = new List<string>() };
                            bt[k2].ImagePaths.Add(c.ImagePath); bt[k2].CampaignNames.Add(c.CampaignName);
                            if (c.DurationMinutes > bt[k2].DurationMinutes) bt[k2].DurationMinutes = c.DurationMinutes;
                        }
                    _todayLannerSlots = bt.Values.OrderBy(s2 => s2.StartTime).ToList();
                    _lastDateCheck = td;
                    Log("[LANNER] " + _todayLannerSlots.Count + " slots loaded");
                }
                catch (Exception ex) { LogErr("[LANNER]", ex); }
            }
        }

        private void UpdateLannerState()
        {
            if (!_lannerEnabled) return;
            DateTime n = DateTime.Now;
            if (n.Date != _lastDateCheck) { _lastDateCheck = n.Date; ThreadPool.QueueUserWorkItem(_ => LoadLannerSchedule()); return; }
            if (!Monitor.TryEnter(_lannerLock)) return;
            try
            {
                TimeSpan ct = n.TimeOfDay;
                if (_lannerActive)
                {
                    if (_lannerSlot == null) { _lannerActive = false; return; }
                    if ((n - _lannerSlotStart).TotalMinutes >= _lannerSlot.DurationMinutes) { _lannerActive = false; _lannerSlot = null; _lannerImageBGRA = null; _lannerBgDirty = false; Log("[LANNER] OFF"); return; }
                    if (_lannerSlot.ImagePaths.Count > 1 && _lannerImgDurSec > 0 && (n - _lannerImgStart).TotalSeconds >= _lannerImgDurSec) { _lannerImgIdx = (_lannerImgIdx + 1) % _lannerSlot.ImagePaths.Count; string lp = _lannerSlot.ImagePaths[_lannerImgIdx]; _lannerImgStart = n; ThreadPool.QueueUserWorkItem(_ => { LoadLannerImg(lp); _lannerBgDirty = true; }); }
                }
                else
                {
                    foreach (var slot in _todayLannerSlots)
                        if (ct >= slot.StartTime && ct < slot.StartTime.Add(TimeSpan.FromMinutes(slot.DurationMinutes)))
                        { _lannerSlot = slot; _lannerImgIdx = 0; _lannerSlotStart = n; _lannerImgStart = n; _lannerImgDurSec = slot.ImagePaths.Count > 1 ? (slot.DurationMinutes * 60) / slot.ImagePaths.Count : slot.DurationMinutes * 60; _lannerActive = true; Log("[LANNER] ON: " + string.Join(", ", slot.CampaignNames)); string lp = slot.ImagePaths[0]; ThreadPool.QueueUserWorkItem(_ => { LoadLannerImg(lp); _lannerBgDirty = true; }); break; }
                }
            }
            finally { Monitor.Exit(_lannerLock); }
        }

        private void LoadLannerImg(string p) { try { if (!File.Exists(p)) { _lannerImageBGRA = null; return; } int fw = _ndiWidth, fh = _ndiHeight; int needed = fw * fh * 4; if (_lannerBufA == null || _lannerBufA.Length != needed) _lannerBufA = new byte[needed]; if (_lannerBufB == null || _lannerBufB.Length != needed) _lannerBufB = new byte[needed]; int sel = Interlocked.Increment(ref _lannerBufSelect); byte[] buf = (sel & 1) == 0 ? _lannerBufA : _lannerBufB; using (var orig = Image.FromFile(p)) using (var bmp = new Bitmap(fw, fh, PixelFormat.Format32bppArgb)) using (var g = Graphics.FromImage(bmp)) { g.InterpolationMode = InterpolationMode.HighQualityBicubic; g.SmoothingMode = SmoothingMode.HighQuality; g.CompositingQuality = CompositingQuality.HighQuality; g.Clear(Color.Black); float ir = (float)orig.Width / orig.Height, ar = (float)fw / fh; int dw, dh, dx, dy; if (ir > ar) { dw = fw; dh = (int)(fw / ir); dx = 0; dy = (fh - dh) / 2; } else { dh = fh; dw = (int)(fh * ir); dx = (fw - dw) / 2; dy = 0; } g.DrawImage(orig, dx, dy, dw, dh); var bd = bmp.LockBits(new Rectangle(0, 0, fw, fh), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb); Marshal.Copy(bd.Scan0, buf, 0, buf.Length); bmp.UnlockBits(bd); _lannerImageBGRA = buf; } } catch { _lannerImageBGRA = null; } }
        public void ReloadLannerSchedule() => ThreadPool.QueueUserWorkItem(_ => LoadLannerSchedule());

        // ═══════════════════════════════════════════════════════════
        // WAVEFORM
        // ═══════════════════════════════════════════════════════════
        private void LoadWaveformForCurrentFile(string fp) { if (_waveformCache.TryGetValue(fp, out float[] c) && _currentFile == fp) { _waveformPeaks = c; _waveformCurrentFile = fp; SafeInvoke(() => waveformPanel.Invalidate()); return; } Task.Run(() => GenWaveform(fp, true)); }
        private void PreCacheNextWaveform() { if (_playlistQueue == null) return; var i = _playlistQueue.GetAllItems(); if (i.Count < 2) return; string nf = i[1].FilePath; if (!string.IsNullOrEmpty(nf) && !IsWebStream(nf) && !_waveformCache.ContainsKey(nf)) Task.Run(() => GenWaveform(nf, false)); }
        private void GenWaveform(string fp, bool apply)
        {
            try
            {
                if (IsWebStream(fp)) return;
                if (_waveformCache.ContainsKey(fp))
                {
                    if (apply && _currentFile == fp) { _waveformPeaks = _waveformCache[fp]; _waveformCurrentFile = fp; SafeInvoke(() => waveformPanel.Invalidate()); }
                    return;
                }

                float[] pk = new float[WAVEFORM_BARS];
                bool success = false;
                TimeSpan actualDur = TimeSpan.Zero;

                // ── Metodo 1: NAudio WaveStream con position-seeking ──
                try
                {
                    NAudio.Wave.WaveStream rd = null;
                    string ext = Path.GetExtension(fp).ToLowerInvariant();
                    try
                    {
                        if (ext == ".mp3") rd = new NAudio.Wave.Mp3FileReader(fp);
                        else if (ext == ".wav") rd = new NAudio.Wave.WaveFileReader(fp);
                        else rd = new NAudio.Wave.AudioFileReader(fp);
                    }
                    catch { try { rd = new NAudio.Wave.AudioFileReader(fp); } catch { } }

                    if (rd != null)
                    {
                        using (rd)
                        {
                            actualDur = rd.TotalTime;
                            success = ReadWaveformWithSeeking(rd, pk);
                        }
                    }
                }
                catch { }

                // ── Metodo 2: MediaFoundationReader con position-seeking (per video) ──
                if (!success)
                {
                    try
                    {
                        using (var mfr = new NAudio.Wave.MediaFoundationReader(fp))
                        {
                            actualDur = mfr.TotalTime;
                            success = ReadWaveformWithSeeking(mfr, pk);
                        }
                    }
                    catch { }
                }

                // ── Aggiorna durata se significativamente diversa ──
                if (apply && _currentFile == fp && actualDur.TotalMilliseconds > 100)
                {
                    int oldMs = (int)_totalDuration.TotalMilliseconds;
                    int newMs = (int)actualDur.TotalMilliseconds;
                    if (Math.Abs(newMs - oldMs) > 500)
                    {
                        _totalDuration = actualDur;
                    }
                }

                _waveformCache[fp] = pk;
                if (apply && _currentFile == fp) { _waveformPeaks = pk; _waveformCurrentFile = fp; SafeInvoke(() => waveformPanel.Invalidate()); }
            }
            catch { }
        }

        /// <summary>
        /// Legge la waveform usando position-seeking (come MusicEditorForm).
        /// Per ogni barra calcola la posizione target nel file, fa seek e legge i campioni.
        /// Garantisce una waveform completa dall'inizio alla fine del file.
        /// </summary>
        private bool ReadWaveformWithSeeking(NAudio.Wave.WaveStream reader, float[] pk)
        {
            try
            {
                var format = reader.WaveFormat;
                if (format == null) return false;

                long totalBytes = reader.Length;
                if (totalBytes <= 0) return false;

                int bps = format.BitsPerSample / 8;
                if (bps < 1) bps = 2;
                int blockAlign = format.BlockAlign > 0 ? format.BlockAlign : bps;
                byte[] buffer = new byte[4096];
                int consecutiveErrors = 0;

                for (int b = 0; b < pk.Length; b++)
                {
                    try
                    {
                        long targetPos = (long)b * totalBytes / pk.Length;
                        targetPos = (targetPos / blockAlign) * blockAlign;
                        targetPos = Math.Max(0, Math.Min(targetPos, Math.Max(0, totalBytes - blockAlign * 2)));

                        try { reader.Position = targetPos; }
                        catch
                        {
                            pk[b] = b > 0 ? pk[b - 1] : 0f;
                            if (++consecutiveErrors >= 10) return false;
                            continue;
                        }

                        int bytesRead = 0;
                        try { bytesRead = reader.Read(buffer, 0, buffer.Length); }
                        catch
                        {
                            pk[b] = b > 0 ? pk[b - 1] : 0f;
                            if (++consecutiveErrors >= 10) return false;
                            continue;
                        }

                        consecutiveErrors = 0;

                        if (bytesRead > 0)
                        {
                            float mx = 0f;
                            if (bps == 2)
                            {
                                for (int i = 0; i < bytesRead - 1; i += 2)
                                {
                                    float a = Math.Abs(BitConverter.ToInt16(buffer, i) / 32768f);
                                    if (a > mx) mx = a;
                                }
                            }
                            else if (bps == 4)
                            {
                                for (int i = 0; i < bytesRead - 3; i += 4)
                                {
                                    float a = Math.Abs(BitConverter.ToSingle(buffer, i));
                                    if (a > mx) mx = a;
                                }
                            }
                            else
                            {
                                mx = b > 0 ? pk[b - 1] : 0f;
                            }
                            pk[b] = Math.Min(1f, mx);
                        }
                        else
                        {
                            pk[b] = b > 0 ? pk[b - 1] : 0f;
                        }
                    }
                    catch
                    {
                        pk[b] = b > 0 ? pk[b - 1] : 0f;
                        if (++consecutiveErrors >= 10) return false;
                    }
                }

                return true;
            }
            catch { return false; }
        }

        // ═══════════════════════════════════════════════════════════
        // UI
        // ═══════════════════════════════════════════════════════════
        private void InitPlayerUI() { Size = new Size(1400, 160); BackColor = Color.FromArgb(30, 30, 30); this.Padding = new Padding(5); Panel top = new Panel { Location = new Point(5, 5), Size = new Size(1390, 70), BackColor = Color.FromArgb(20, 20, 20) }; Controls.Add(top); Label h1 = null; MkTimer(top, 5, 5, LanguageManager.GetString("Player.TimeElapsed", "Tempo trascorso"), ref lblElapsed, AppTheme.LEDGreen, 155, 60, ref h1); _lblElapsedHeader = h1; Label h2 = null; MkTimer(top, 165, 5, "Intro", ref lblIntro, AppTheme.LEDYellow, 155, 60, ref h2); Panel tp = new Panel { Location = new Point(325, 5), Size = new Size(750, 60), BackColor = Color.FromArgb(25, 25, 25), BorderStyle = BorderStyle.FixedSingle }; top.Controls.Add(tp); lblArtist = new Label { Text = "", Font = SafeFont("Segoe UI", 16f, FontStyle.Bold), ForeColor = Color.White, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, AutoSize = false }; lblArtist.Paint += LblArtPaint; tp.Controls.Add(lblArtist); Label h3 = null; MkTimer(top, 1080, 5, LanguageManager.GetString("Player.TimeRemaining", "Tempo restante"), ref lblRemaining, AppTheme.LEDRed, 155, 60, ref h3); _lblRemainingHeader = h3; Panel cp = new Panel { Location = new Point(1240, 5), Size = new Size(145, 60), BackColor = Color.Black, BorderStyle = BorderStyle.FixedSingle }; top.Controls.Add(cp); lblDate = new Label { Text = FmtD(), Font = SafeFont("Segoe UI", 8f, FontStyle.Regular), ForeColor = Color.Gray, Location = new Point(3, 3), Size = new Size(145, 12), TextAlign = ContentAlignment.TopCenter }; cp.Controls.Add(lblDate); lblClock = new Label { Text = DateTime.Now.ToString("HH:mm:ss"), Font = SafeFont("DSEG7 Classic", 20f, FontStyle.Bold), ForeColor = AppTheme.LEDGreen, Location = new Point(3, 18), Size = new Size(140, 40), TextAlign = ContentAlignment.MiddleCenter }; cp.Controls.Add(lblClock); var clkT = new System.Windows.Forms.Timer { Interval = 1000 }; clkT.Tick += (s, e) => { lblClock.Text = DateTime.Now.ToString("HH:mm:ss"); lblDate.Text = FmtD(); }; clkT.Start(); Panel bot = new Panel { Location = new Point(5, 80), Size = new Size(1390, 75), BackColor = Color.FromArgb(20, 20, 20) }; Controls.Add(bot); MkBtns(bot, 5); Panel wfc = new Panel { Location = new Point(415, 5), Size = new Size(600, 65), BackColor = Color.Black, BorderStyle = BorderStyle.FixedSingle }; bot.Controls.Add(wfc); waveformPanel = new DoubleBufferedPanel { Dock = DockStyle.Fill, BackColor = Color.Black }; waveformPanel.Paint += WfPaint; waveformPanel.MouseClick += WfClick; wfc.Controls.Add(waveformPanel); Panel vc = new Panel { Location = new Point(1020, 5), Size = new Size(365, 65), BackColor = Color.FromArgb(25, 25, 25), BorderStyle = BorderStyle.FixedSingle }; bot.Controls.Add(vc); vc.Controls.Add(new Label { Text = "L", Font = SafeFont("Segoe UI", 11f, FontStyle.Bold), ForeColor = Color.White, Location = new Point(8, 16), Size = new Size(15, 18) }); vuMeterLeftPanel = new DoubleBufferedPanel { Location = new Point(28, 18), Size = new Size(330, 15), BackColor = Color.Black, BorderStyle = BorderStyle.FixedSingle }; vuMeterLeftPanel.Paint += (s, e) => PaintVu(e.Graphics, 330, 15, _vuLeft, _vuLeftPeak); vc.Controls.Add(vuMeterLeftPanel); vc.Controls.Add(new Label { Text = "R", Font = SafeFont("Segoe UI", 11f, FontStyle.Bold), ForeColor = Color.White, Location = new Point(8, 40), Size = new Size(15, 18) }); vuMeterRightPanel = new DoubleBufferedPanel { Location = new Point(28, 42), Size = new Size(330, 15), BackColor = Color.Black, BorderStyle = BorderStyle.FixedSingle }; vuMeterRightPanel.Paint += (s, e) => PaintVu(e.Graphics, 330, 15, _vuRight, _vuRightPeak); vc.Controls.Add(vuMeterRightPanel); }
        private void PaintVu(Graphics g, int w, int h, float lv, float pk) { g.Clear(Color.Black); if (lv <= 0.001f && pk <= 0.001f) return; int bw = (int)(Math.Min(1f, lv) * w), ge = (int)(w * 0.7f), ye = (int)(w * 0.9f); if (bw > 0) { int gw = Math.Min(bw, ge); if (gw > 0) using (var b = new SolidBrush(Color.FromArgb(0, 200, 0))) g.FillRectangle(b, 0, 0, gw, h); if (bw > ge) { int yw = Math.Min(bw - ge, ye - ge); if (yw > 0) using (var b = new SolidBrush(Color.FromArgb(255, 200, 0))) g.FillRectangle(b, ge, 0, yw, h); } if (bw > ye) using (var b = new SolidBrush(Color.FromArgb(255, 40, 40))) g.FillRectangle(b, ye, 0, bw - ye, h); } int ppx = (int)(Math.Min(1f, pk) * w); if (ppx > 2 && ppx < w) using (var p = new Pen(Color.White, 2)) g.DrawLine(p, ppx, 0, ppx, h); }
        private void LblArtPaint(object s, PaintEventArgs e) { Label l = s as Label; if (l == null || string.IsNullOrEmpty(l.Text)) return; e.Graphics.Clear(l.Parent.BackColor); string t = l.Text.Replace("&&", "&"); float fs = 16f; Font f = SafeFont("Segoe UI", fs, FontStyle.Bold); SizeF ts = e.Graphics.MeasureString(t, f); float mw = l.Width - 20; while (ts.Width > mw && fs > 8f) { fs -= 0.5f; f.Dispose(); f = SafeFont("Segoe UI", fs, FontStyle.Bold); ts = e.Graphics.MeasureString(t, f); } using (var b = new SolidBrush(l.ForeColor)) e.Graphics.DrawString(t, f, b, l.ClientRectangle, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center }); f.Dispose(); }
        private string FmtD() { var ci = new CultureInfo("it-IT"); DateTime n = DateTime.Now; string d = ci.DateTimeFormat.GetDayName(n.DayOfWeek); d = char.ToUpper(d[0]) + d.Substring(1); string m = ci.DateTimeFormat.GetMonthName(n.Month); m = char.ToUpper(m[0]) + m.Substring(1); return d + " " + n.Day.ToString("D2") + " " + m; }
        private void MkTimer(Panel p, int x, int y, string lb, ref Label lbl, Color c, int w, int h, ref Label hr) { Panel t = new Panel { Location = new Point(x, y), Size = new Size(w, h), BackColor = Color.Black, BorderStyle = BorderStyle.FixedSingle }; p.Controls.Add(t); Label lh = new Label { Text = lb, Font = SafeFont("Segoe UI", 7f, FontStyle.Regular), ForeColor = Color.Gray, Location = new Point(3, 2), AutoSize = true }; t.Controls.Add(lh); hr = lh; lbl = new Label { Text = "--:--", Font = SafeFont("DSEG7 Classic", 24f, FontStyle.Bold), ForeColor = c, Location = new Point(3, 22), Size = new Size(w - 6, h - 24), TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Black }; t.Controls.Add(lbl); }
        private void MkBtns(Panel p, int sx) { int y = 8, bs = 58, sp = 5, ex = 8; btnPlay = MkB(p, "\u25B6", sx, y, bs, Color.FromArgb(80, 80, 80)); btnPlay.Click += (s, e) => Play(); btnPause = MkB(p, "\u275A\u275A", sx + bs + sp, y, bs, Color.FromArgb(80, 80, 80)); btnPause.Click += (s, e) => { if (_isPaused) Resume(); else Pause(); }; btnStop = MkB(p, "\u23F9", sx + (bs + sp) * 2, y, bs, Color.FromArgb(80, 80, 80)); btnStop.Click += (s, e) => Stop(); btnAutoManual = new Button { Name = "btnAM", Text = "AUTO", Font = SafeFont("Segoe UI", 10f, FontStyle.Bold), Size = new Size(130, bs), Location = new Point(sx + (bs + sp) * 3 + ex, y), BackColor = AppTheme.Success, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand }; btnAutoManual.FlatAppearance.BorderSize = 0; btnAutoManual.Click += (s, e) => { _autoMode = !_autoMode; btnAutoManual.Text = _autoMode ? "AUTO" : "MANUAL"; btnAutoManual.BackColor = _autoMode ? AppTheme.Success : AppTheme.Warning; AutoModeChanged?.Invoke(this, _autoMode); }; p.Controls.Add(btnAutoManual); var bn = MkB(p, "\u23ED", sx + (bs + sp) * 3 + ex + 130 + sp + ex, y, bs, AppTheme.Info); bn.Click += (s, e) => { if (!_isPlaying && !_isPaused) Play(); else SkipToNext(); NextRequested?.Invoke(this, EventArgs.Empty); }; }
        private Button MkB(Panel p, string t, int x, int y, int sz, Color bg) { var b = new Button { Text = t, Font = SafeFont("Segoe UI", 18f, FontStyle.Bold), Size = new Size(sz, sz), Location = new Point(x, y), BackColor = bg, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand }; b.FlatAppearance.BorderSize = 0; p.Controls.Add(b); return b; }
        private void UpdateTimerLabels() { if (_lblElapsedHeader != null) _lblElapsedHeader.Text = LanguageManager.GetString("Player.TimeElapsed", "Tempo trascorso"); if (_lblRemainingHeader != null) _lblRemainingHeader.Text = LanguageManager.GetString("Player.TimeRemaining", "Tempo restante"); }
        private void UpdateBtnStates() { if (btnPlay == null) return; btnPlay.BackColor = _isPlaying && !_isPaused ? AppTheme.Success : Color.FromArgb(80, 80, 80); btnPause.BackColor = _isPaused ? AppTheme.Warning : Color.FromArgb(80, 80, 80); btnStop.BackColor = !_isPlaying && !_isPaused ? AppTheme.Danger : Color.FromArgb(80, 80, 80); }
        private void InitTimers() { _updateTimer = new System.Windows.Forms.Timer { Interval = 33 }; _updateTimer.Tick += (s, e) => { if (!_isPlaying || _isPaused) return; UpdCnt(); waveformPanel?.Invalidate(); vuMeterLeftPanel?.Invalidate(); vuMeterRightPanel?.Invalidate(); }; _mixCheckTimer = new System.Windows.Forms.Timer { Interval = 50 }; _mixCheckTimer.Tick += MixCheckTimer_Tick; _blinkTimer = new System.Windows.Forms.Timer { Interval = 500 }; _blinkTimer.Tick += (s, e) => { _blinkState = !_blinkState; }; }
        private void UpdCnt() { int p = _positionMs; int ee; if (_autoMode && _markerMIX > 0) ee = _markerMIX; else if (_markerOUT > 0) ee = _markerOUT; else ee = (int)_totalDuration.TotalMilliseconds; lblElapsed.Text = TimeSpan.FromMilliseconds(Math.Max(0, p - _markerIN)).ToString(@"mm\:ss"); int r = Math.Max(0, ee - p); lblRemaining.Text = "-" + TimeSpan.FromMilliseconds(r).ToString(@"mm\:ss"); if (r > 0 && r <= 10000) { if (!_blinkTimer.Enabled) _blinkTimer.Start(); lblRemaining.BackColor = _blinkState ? Color.Red : Color.Black; lblRemaining.ForeColor = _blinkState ? Color.Black : AppTheme.LEDRed; } else { _blinkTimer.Stop(); lblRemaining.BackColor = Color.Black; lblRemaining.ForeColor = AppTheme.LEDRed; } int im = (int)_introTime.TotalMilliseconds; int posFromIn = p - _markerIN; if (im > 0 && posFromIn < im) { lblIntro.Text = TimeSpan.FromMilliseconds(im - posFromIn).ToString(@"mm\:ss"); lblIntro.ForeColor = Color.White; lblIntro.BackColor = Color.Red; } else { lblIntro.Text = ""; lblIntro.BackColor = Color.Black; } }
        private void WfPaint(object s, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            int w = waveformPanel.Width, h = waveformPanel.Height;
            g.Clear(Color.Black);

            float tm = (float)_totalDuration.TotalMilliseconds;
            if (tm <= 0) return;

            // ═════ RANGE VISIBILE: intera durata del file ═════
            // Posizione cursore relativa all'intero file [0..1]
            float pr = Math.Max(0f, Math.Min(1f, (float)_positionMs / tm));
            int cx = (int)(pr * (w - 1));

            // Marker IN posizione sulla waveform
            int markerInX = _markerIN > 0 ? (int)((float)_markerIN / tm * (w - 1)) : 0;

            // Marker MIX posizione sulla waveform (in MANUAL mode MIX sparisce)
            int markerMixMs;
            if (_autoMode && _markerMIX > 0)
                markerMixMs = _markerMIX;
            else if (_markerOUT > 0)
                markerMixMs = _markerOUT;
            else
                markerMixMs = (int)tm;
            int markerMixX = (int)((float)markerMixMs / tm * (w - 1));

            // Marker INTRO posizione sulla waveform
            int introX = _markerINTRO > 0 ? (int)(Math.Min(1f, (float)_markerINTRO / tm) * (w - 1)) : 0;

            int cy = h / 2;

            // Background zone: rosso tra MarkerIN e INTRO
            if (introX > markerInX)
                using (var b = new SolidBrush(Color.FromArgb(25, 255, 0, 0)))
                    g.FillRectangle(b, markerInX, 0, introX - markerInX, h);

            // ═════ WAVEFORM: mostra tutte le barre dell'intero file ═════
            float[] pk = _waveformPeaks;
            if (pk != null && pk.Length > 0)
            {
                int nb = pk.Length;
                float bw2 = (float)(w - 1) / nb;

                for (int i = 0; i < nb; i++)
                {
                    float pv = pk[i];
                    float boosted = (float)Math.Pow(pv, 0.6);
                    int bh = Math.Max(1, (int)(boosted * h * 0.95f));
                    int bx = (int)(i * bw2);

                    Color bc;
                    if (bx < markerInX || bx > markerMixX)
                        bc = bx < cx ? Color.FromArgb(50, 50, 50) : Color.FromArgb(60, 60, 60);
                    else if (bx < introX)
                        bc = bx < cx ? Color.FromArgb(70, 70, 70) : Color.FromArgb(255, 80, 80);
                    else if (bx < cx)
                        bc = Color.FromArgb(70, 70, 70);  // già riprodotto
                    else
                        bc = Color.FromArgb(0, 200, 0);    // da riprodurre

                    int barWidth = Math.Max(1, (int)Math.Ceiling(bw2));
                    if (bx + barWidth > w) barWidth = w - bx;

                    using (var br = new SolidBrush(bc))
                        g.FillRectangle(br, bx, cy - bh / 2, barWidth, bh);
                }
            }
            else
            {
                // Fallback senza waveform: barra piena
                if (cx < w - 1)
                    using (var b = new SolidBrush(Color.FromArgb(0, 150, 0)))
                        g.FillRectangle(b, cx, 0, w - cx, h);
                if (cx > 0)
                    using (var b = new SolidBrush(Color.FromArgb(60, 60, 60)))
                        g.FillRectangle(b, 0, 0, cx, h);
            }

            // Progress bar in basso
            if (cx > 0)
                using (var b = new SolidBrush(Color.FromArgb(70, 70, 70)))
                    g.FillRectangle(b, 0, h - 3, cx, 3);

            if (cx < w - 1)
                using (var b = new SolidBrush(Color.FromArgb(0, 255, 0)))
                    g.FillRectangle(b, cx, h - 3, w - cx, 3);

            // Marker IN (linea ciano)
            if (markerInX > 0 && markerInX < w)
                using (var pn = new Pen(Color.FromArgb(200, 0, 200, 255), 2))
                    g.DrawLine(pn, markerInX, 0, markerInX, h);

            // Marker MIX (linea arancione) - solo in AUTO mode
            if (_autoMode && _markerMIX > 0 && markerMixX > 0 && markerMixX < w)
                using (var pn = new Pen(Color.FromArgb(200, 255, 128, 0), 2))
                    g.DrawLine(pn, markerMixX, 0, markerMixX, h);

            // Marker OUT (linea arancione chiara) - se diverso da MIX e fine file
            if (_markerOUT > 0 && _markerOUT < (int)tm)
            {
                int markerOutX = (int)((float)_markerOUT / tm * (w - 1));
                if (markerOutX > 0 && markerOutX < w && (!_autoMode || _markerOUT != _markerMIX))
                    using (var pn = new Pen(Color.FromArgb(150, 255, 128, 0), 1))
                        g.DrawLine(pn, markerOutX, 0, markerOutX, h);
            }

            // Marker INTRO (linea gialla)
            if (introX > 0 && introX < w)
                using (var pn = new Pen(Color.FromArgb(200, 255, 255, 0), 1))
                    g.DrawLine(pn, introX, 0, introX, h);

            // Cursore di riproduzione
            if (_isPlaying && cx > 0)
                using (var pn = new Pen(Color.Red, 2))
                    g.DrawLine(pn, cx, 0, cx, h);

            // Linea centrale
            using (var pn = new Pen(Color.FromArgb(25, 255, 255, 255), 1))
                g.DrawLine(pn, 0, cy, w, cy);
        }
        private void WfClick(object s, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && ModifierKeys.HasFlag(Keys.Control) && _isPlaying && _totalDuration.TotalSeconds > 0)
            {
                float tm = (float)_totalDuration.TotalMilliseconds;
                if (tm <= 0) return;
                float clickRatio = Math.Max(0f, Math.Min(1f, (float)e.X / waveformPanel.Width));
                int ms = (int)(clickRatio * tm);
                SeekTo(TimeSpan.FromMilliseconds(ms));
            }
        }


        // ═══════════════════════════════════════════════════════════
        // PUBLIC API
        // ═══════════════════════════════════════════════════════════
        public void SetPlaylistQueue(PlaylistQueueControl pq) { _playlistQueue = pq; }
        public void SetAutoMode(bool a) { _autoMode = a; SafeInvoke(() => { if (btnAutoManual != null) { btnAutoManual.Text = a ? "AUTO" : "MANUAL"; btnAutoManual.BackColor = a ? AppTheme.Success : AppTheme.Warning; } }); AutoModeChanged?.Invoke(this, _autoMode); }
        public void SetManualMode() { SetAutoMode(false); }
        public void SetAutoStartPending(bool pending) { _autoStartPending = pending; }
        private static bool IsVideoFile(string p) { if (string.IsNullOrEmpty(p)) return false; string ext = Path.GetExtension(p).ToLowerInvariant(); return ext == ".mp4" || ext == ".avi" || ext == ".mov" || ext == ".mkv" || ext == ".wmv" || ext == ".webm" || ext == ".m4v"; }
        private void SafeInvoke(Action a) { if (IsDisposed || _isDisposed) return; try { if (InvokeRequired) BeginInvoke(a); else a(); } catch { } }
        private void UpdateTrackDisplay(PlayItem i) { if (i == null) return; bool isMusicArchive = i.ItemType.Equals("Music", StringComparison.OrdinalIgnoreCase) && !i.IsScheduled; string d = isMusicArchive ? (string.IsNullOrEmpty(i.Artist) ? (i.Title ?? "").ToUpper() : i.Artist.ToUpper() + " - " + (i.Title ?? "").ToUpper()) : (!string.IsNullOrEmpty(i.Title) ? i.Title : Path.GetFileNameWithoutExtension(i.FilePath ?? "")).ToUpper(); lblArtist.Text = d.Replace("&&", "&"); lblArtist.Invalidate(); if (i.Intro.TotalMilliseconds > 0) { lblIntro.Text = i.Intro.ToString(@"mm\:ss"); lblIntro.ForeColor = Color.White; lblIntro.BackColor = Color.Red; } else { lblIntro.Text = ""; lblIntro.BackColor = Color.Black; } }
        private void ClearPlayer() { SafeInvoke(() => { lblArtist.Text = ""; lblArtist.Invalidate(); lblIntro.Text = "--:--"; lblIntro.BackColor = Color.Black; lblIntro.ForeColor = AppTheme.LEDYellow; lblElapsed.Text = "--:--"; lblRemaining.Text = "--:--"; lblRemaining.BackColor = Color.Black; lblRemaining.ForeColor = AppTheme.LEDRed; _waveformPeaks = null; _waveformCurrentFile = ""; waveformPanel.Invalidate(); UpdateBtnStates(); }); _totalDuration = TimeSpan.Zero; _introTime = TimeSpan.Zero; _markerIN = 0; _markerINTRO = 0; _markerMIX = 0; _markerOUT = 0; _positionMs = 0; CurrentFilePath = ""; CurrentArtist = ""; CurrentTitle = ""; _playlistQueue?.SetCurrentPlaying(-1); }

        /// <summary>
        /// Aggiorna il CG overlay.
        /// SOLO gli elementi Music non schedulati (dall'archivio musicale) mostrano artista/titolo.
        /// Tutti gli altri (schedulazioni, jingle, clip, ecc.) passano stringhe vuote → nessuna titolazione.
        /// </summary>
        private void UpdateCG(PlayItem i)
        {
            try
            {
                string t = i?.ItemType ?? "Music";
                bool isMusicArchive = t.Equals("Music", StringComparison.OrdinalIgnoreCase) && !(i?.IsScheduled ?? false);
                if (isMusicArchive)
                {
                    int cgEndMs;
                    if (_autoMode && _markerMIX > 0)
                        cgEndMs = _markerMIX;
                    else if (_markerOUT > 0)
                        cgEndMs = _markerOUT;
                    else
                        cgEndMs = (int)_totalDuration.TotalMilliseconds;
                    TimeSpan effectiveDuration = TimeSpan.FromMilliseconds(Math.Max(0, cgEndMs - _markerIN));
                    CGRenderer.OnTrackChanged(i.Artist ?? "", i.Title ?? "", "Music", effectiveDuration);
                }
                else
                    CGRenderer.OnTrackChanged("", "", t, TimeSpan.Zero);
            }
            catch { }
        }

        private void SendMeta(PlayItem i) { try { string s = ConfigurationControl.GetMetadataSource(); bool sn = s == "MusicAndClips" || (s == "MusicOnly" && (i?.ItemType ?? "Music") == "Music"); if (sn) MetadataManager.UpdateMetadata(i?.Artist ?? "", i?.Title ?? "", i?.ItemType ?? "Music"); } catch { } }

        // ═══════════════════════════════════════════════════════════
        // LOG
        // ═══════════════════════════════════════════════════════════
        private void InitLog() { try { _dailyLogger = new Services.Core.DailyLogger("PlayerVideo"); } catch { } }
        private void Log(string m) { _dailyLogger?.Log(m); }
        private void LogErr(string m, Exception ex) { _dailyLogger?.LogErr(m, ex); }
        private void LogErr(string m) { _dailyLogger?.LogErr(m); }

        // ═══════════════════════════════════════════════════════════
        // DISPOSE
        // ══════════════════════════════════════════════════���════════
        protected override void Dispose(bool disposing)
        {
            if (disposing && !_isDisposed)
            {
                _isDisposed = true; _engineRunning = false;
                _updateTimer?.Stop(); _updateTimer?.Dispose(); _mixCheckTimer?.Stop(); _mixCheckTimer?.Dispose(); _blinkTimer?.Stop(); _blinkTimer?.Dispose();
                if (_engineThread != null && _engineThread.IsAlive) _engineThread.Join(2000);
                if (_deckA != null) { StopDeckInternal(_deckA, true); Marshal.FreeHGlobal(_deckA.VideoBufferPtr); }
                if (_deckB != null) { StopDeckInternal(_deckB, true); Marshal.FreeHGlobal(_deckB.VideoBufferPtr); }
                if (_bufferDeck != null) { StopDeckInternal(_bufferDeck, true); Marshal.FreeHGlobal(_bufferDeck.VideoBufferPtr); }
                if (_compositedVideoHandle.IsAllocated) _compositedVideoHandle.Free();
                _libVLC?.Dispose(); _ndiSender?.Dispose();
                try { _hlsRelay?.Dispose(); } catch { }
                try { _audioMirrorOutput?.Stop(); _audioMirrorOutput?.Dispose(); } catch { }
                try { _dailyLogger?.Dispose(); } catch { }
            }
            base.Dispose(disposing);
        }
    }
}
