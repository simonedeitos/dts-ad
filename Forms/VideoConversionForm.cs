using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace AirDirector.Forms
{
    public partial class VideoConversionForm : Form
    {
        // ── public callback ───────────────────────────────────────────────
        public event Action<List<string>> ConversionCompleted;

        // ── constants ─────────────────────────────────────────────────────
        private const int MAX_CONCURRENT = 3;
        private const int ROW_HEIGHT = 128;
        private const int ROW_MARGIN = 6;
        private const string REGISTRY_PATH = @"SOFTWARE\AirDirector";
        private const int PROGRESS_MAX = 1000;
        private const int VBR_BITRATE_TOLERANCE_KBPS = 10;
        private const double SILENCE_MIN_DURATION_SEC = 0.01;

        private static readonly HashSet<string> AudioExtensions = new HashSet<string>(
            new[] { ".mp3", ".wav", ".flac", ".aac", ".ogg", ".wma", ".m4a", ".aif", ".aiff", ".opus" },
            StringComparer.OrdinalIgnoreCase);

        // ── state ─────────────────────────────────────────────────────────
        private readonly string[] _inputFiles;
        private readonly string _ffmpegPath;
        private readonly string _ffprobePath;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly List<FileRow> _fileRows = new List<FileRow>();
        private int _doneCount;
        private int _errorCount;

        // ── pre-editing panel (built programmatically) ────────────────────
        private Panel _pnlPreEditing;
        private CheckBox _chkPreEditing;
        private Label _lblMarkerInThreshold;
        private NumericUpDown _nudMarkerInDb;
        private Label _lblMarkerOutThreshold;
        private NumericUpDown _nudMarkerOutDb;

        // ── tag source panel ──────────────────────────────────────────────
        private Panel _pnlTagSource;
        private RadioButton _rbTagFromFile;
        private RadioButton _rbTagFromFilename;
        private bool _useTagsFromFile = true;  // true = read ID3/file tags, false = parse filename

        // ── pre-editing results ───────────────────────────────────────────
        private readonly Dictionary<string, (int MarkerInMs, int MarkerOutMs)> _preEditResults
            = new Dictionary<string, (int, int)>();

        // ═════════════════════════════════════════════════════════════════
        // Windows Shell Property Store – P/Invoke
        // ═════════════════════════════════════════════════════════════════

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void SHGetPropertyStoreFromParsingName(
            [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            IntPtr pbc,
            int flags,
            ref Guid iid,
            out IPropertyStore propertyStore);

        [ComImport,
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
         Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99")]
        private interface IPropertyStore
        {
            uint GetCount(out uint cProps);
            uint GetAt(uint iProp, out PROPERTYKEY pkey);
            uint GetValue(ref PROPERTYKEY key, out PROPVARIANT pv);
            uint SetValue(ref PROPERTYKEY key, ref PROPVARIANT pv);
            uint Commit();
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct PROPERTYKEY
        {
            public Guid fmtid;
            public uint pid;
            public PROPERTYKEY(Guid g, uint p) { fmtid = g; pid = p; }
        }

        [StructLayout(LayoutKind.Explicit, Size = 16)]
        private struct PROPVARIANT
        {
            [FieldOffset(0)] public ushort vt;
            [FieldOffset(8)] public uint uintVal;
            [FieldOffset(8)] public int intVal;
            [FieldOffset(8)] public double dblVal;
            [FieldOffset(8)] public IntPtr ptrVal;
            [FieldOffset(8)] public ulong ulongVal; // VT_UI8 (21) – duration

            public uint AsUInt()
            {
                if (vt == 19 /* VT_UI4 */ || vt == 3 /* VT_I4 */) return uintVal;
                return 0;
            }
        }

        // Property keys
        private static readonly PROPERTYKEY PKEY_Video_FrameWidth = new PROPERTYKEY(new Guid("64440492-4C8B-11D1-8B70-080036B11A03"), 3);
        private static readonly PROPERTYKEY PKEY_Video_FrameHeight = new PROPERTYKEY(new Guid("64440492-4C8B-11D1-8B70-080036B11A03"), 4);
        private static readonly PROPERTYKEY PKEY_Video_FrameRate = new PROPERTYKEY(new Guid("64440492-4C8B-11D1-8B70-080036B11A03"), 6);
        private static readonly PROPERTYKEY PKEY_Video_Encoding = new PROPERTYKEY(new Guid("64440492-4C8B-11D1-8B70-080036B11A03"), 10);
        private static readonly PROPERTYKEY PKEY_Audio_SampleRate = new PROPERTYKEY(new Guid("64440490-4C8B-11D1-8B70-080036B11A03"), 5);
        private static readonly PROPERTYKEY PKEY_Audio_Channels = new PROPERTYKEY(new Guid("64440490-4C8B-11D1-8B70-080036B11A03"), 7);
        private static readonly PROPERTYKEY PKEY_Audio_Encoding = new PROPERTYKEY(new Guid("64440490-4C8B-11D1-8B70-080036B11A03"), 2);
        private static readonly PROPERTYKEY PKEY_Media_Duration = new PROPERTYKEY(new Guid("64440490-4C8B-11D1-8B70-080036B11A03"), 3);

        // ═════════════════════════════════════════════════════════════════
        // Data classes
        // ═════════════════════════════════════════════════════════════════

        private class VideoSpec
        {
            public int Width;
            public int Height;
            public double Fps;
            public string VideoCodec = "?";
            public string AudioCodec = "?";
            public int AudioSampleRate;
            public int AudioChannels;
            public double DurationSeconds;
            public long FileSizeBytes;
            public bool ProbeSuccess;
            public string ProbeMethod = "none";

            public bool Is169 =>
                Height > 0 && Math.Abs((double)Width / Height - 16.0 / 9.0) < 0.02;

            public bool AlreadyCompatible =>
                ProbeSuccess &&
                Is169 &&
                (VideoCodec ?? "").IndexOf("h264", StringComparison.OrdinalIgnoreCase) >= 0 &&
                (AudioCodec ?? "").IndexOf("aac", StringComparison.OrdinalIgnoreCase) >= 0 &&
                AudioSampleRate == 48000 &&
                AudioChannels == 2;
        }

        private class AudioSpec
        {
            public string AudioCodec = "?";
            public int SampleRate;
            public int Channels;
            public int Bitrate;
            public bool IsVariableBitrate;
            public double DurationSeconds;
            public long FileSizeBytes;
            public bool ProbeSuccess;
            public string ProbeMethod = "none";
            public int BitDepth;

            public bool AlreadyCompatibleMp3 =>
                ProbeSuccess &&
                (AudioCodec ?? "").IndexOf("mp3", StringComparison.OrdinalIgnoreCase) >= 0 &&
                !IsVariableBitrate &&
                Bitrate >= 310 && Bitrate <= 330 &&
                (SampleRate == 44100 || SampleRate == 48000) &&
                Channels <= 2;

            public bool AlreadyCompatibleWav =>
                ProbeSuccess &&
                (AudioCodec ?? "").IndexOf("pcm", StringComparison.OrdinalIgnoreCase) >= 0 &&
                (SampleRate == 44100 || SampleRate == 48000) &&
                BitDepth == 16 &&
                Channels <= 2;

            public bool AlreadyCompatible => AlreadyCompatibleMp3 || AlreadyCompatibleWav;
        }

        private class FileRow
        {
            public Panel Container;
            public Label LblFileName;
            public Label LblInputSpec;
            public Label LblOutputSpec;
            public Label LblStatus;
            public Label LblArtistPreview;
            public Label LblTitlePreview;
            public ProgressBar Bar;
            public CheckBox ChkConvert;
            public string InputPath;
            public string OutputPath;
            public VideoSpec Spec;         // non-null for video files
            public AudioSpec AudioSpec;    // non-null for audio files
            public bool IsAudio;
            public bool Skip;
            public bool Succeeded;
            public bool IsCompleted;       // true when done, errored, or cancelled
            public int LastReportedPct;    // monotonic progress tracking
        }

        // ═════════════════════════════════════════════════════════════════
        // Constructor
        // ═════════════════════════════════════════════════════════════════

        public VideoConversionForm(string[] inputFiles)
        {
            InitializeComponent();
            _inputFiles = inputFiles;
            _ffmpegPath = Path.Combine(
                Path.GetDirectoryName(Application.ExecutablePath) ?? "", "ffmpeg.exe");
            _ffprobePath = Path.Combine(
                Path.GetDirectoryName(Application.ExecutablePath) ?? "", "ffprobe.exe");
        }

        // ═════════════════════════════════════════════════════════════════
        // Pre-editing panel (built programmatically)
        // ═════════════════════════════════════════════════════════════════

        private void BuildPreEditingPanel()
        {
            // load saved state from registry
            bool preEditEnabled = false;
            int markerInDb = -25;
            int markerOutDb = -20;
            bool useTagsFromFile = true;
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(REGISTRY_PATH))
                {
                    if (key != null)
                    {
                        var valEnabled = key.GetValue("PreEditingEnabled");
                        if (valEnabled != null) preEditEnabled = Convert.ToInt32(valEnabled) != 0;
                        var valIn = key.GetValue("PreEditingMarkerInDb");
                        if (valIn != null) markerInDb = Convert.ToInt32(valIn);
                        var valOut = key.GetValue("PreEditingMarkerOutDb");
                        if (valOut != null) markerOutDb = Convert.ToInt32(valOut);
                        var valTagSource = key.GetValue("ImportTagsFromFile");
                        if (valTagSource != null) useTagsFromFile = Convert.ToInt32(valTagSource) != 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VideoConversionForm] Registry read error: {ex.Message}");
            }

            _useTagsFromFile = useTagsFromFile;

            // ── Tag Source panel ──────────────────────────────────────
            _pnlTagSource = new Panel
            {
                Dock = DockStyle.Top,
                Height = 32,
                BackColor = Color.FromArgb(28, 28, 40),
                Padding = new Padding(12, 4, 12, 4)
            };

            var lblTagSource = new Label
            {
                Text = "📋 Tag source:",
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.FromArgb(170, 200, 255),
                AutoSize = true,
                Location = new Point(12, 8)
            };
            _pnlTagSource.Controls.Add(lblTagSource);

            _rbTagFromFile = new RadioButton
            {
                Text = "Read from file tags (ID3)",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(120, 6),
                Checked = _useTagsFromFile
            };
            _rbTagFromFile.CheckedChanged += (s, ev) =>
            {
                if (_rbTagFromFile.Checked)
                {
                    _useTagsFromFile = true;
                    SavePreEditingSettings();
                    RefreshArtistTitlePreviews();
                }
            };
            _pnlTagSource.Controls.Add(_rbTagFromFile);

            _rbTagFromFilename = new RadioButton
            {
                Text = "Parse from filename (Artist - Title.ext)",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(350, 6),
                Checked = !_useTagsFromFile
            };
            _rbTagFromFilename.CheckedChanged += (s, ev) =>
            {
                if (_rbTagFromFilename.Checked)
                {
                    _useTagsFromFile = false;
                    SavePreEditingSettings();
                    RefreshArtistTitlePreviews();
                }
            };
            _pnlTagSource.Controls.Add(_rbTagFromFilename);

            Controls.Add(_pnlTagSource);
            _pnlTagSource.BringToFront();

            // ── Pre-editing panel ────────────────────────────────────
            _pnlPreEditing = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.FromArgb(28, 28, 40),
                Padding = new Padding(12, 6, 12, 6)
            };

            _chkPreEditing = new CheckBox
            {
                Text = "Pre-editing: detect Marker IN / OUT thresholds",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(12, 14),
                Checked = preEditEnabled
            };
            _chkPreEditing.CheckedChanged += (s, ev) => SavePreEditingSettings();
            _pnlPreEditing.Controls.Add(_chkPreEditing);

            _lblMarkerInThreshold = new Label
            {
                Text = "IN (dB):",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(170, 200, 255),
                AutoSize = true,
                Location = new Point(400, 16)
            };
            _pnlPreEditing.Controls.Add(_lblMarkerInThreshold);

            _nudMarkerInDb = new NumericUpDown
            {
                Minimum = -60,
                Maximum = 0,
                Value = Math.Max(-60, Math.Min(0, markerInDb)),
                DecimalPlaces = 0,
                Increment = 1,
                Width = 60,
                Location = new Point(460, 12),
                Font = new Font("Segoe UI", 8),
                BackColor = Color.FromArgb(42, 42, 52),
                ForeColor = Color.White
            };
            _nudMarkerInDb.ValueChanged += (s, ev) => SavePreEditingSettings();
            _pnlPreEditing.Controls.Add(_nudMarkerInDb);

            _lblMarkerOutThreshold = new Label
            {
                Text = "OUT (dB):",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(170, 200, 255),
                AutoSize = true,
                Location = new Point(540, 16)
            };
            _pnlPreEditing.Controls.Add(_lblMarkerOutThreshold);

            _nudMarkerOutDb = new NumericUpDown
            {
                Minimum = -60,
                Maximum = 0,
                Value = Math.Max(-60, Math.Min(0, markerOutDb)),
                DecimalPlaces = 0,
                Increment = 1,
                Width = 60,
                Location = new Point(610, 12),
                Font = new Font("Segoe UI", 8),
                BackColor = Color.FromArgb(42, 42, 52),
                ForeColor = Color.White
            };
            _nudMarkerOutDb.ValueChanged += (s, ev) => SavePreEditingSettings();
            _pnlPreEditing.Controls.Add(_nudMarkerOutDb);

            // insert between pnlTop and pnlScroll
            Controls.Add(_pnlPreEditing);
            _pnlPreEditing.BringToFront();
            pnlTop.BringToFront();
        }

        private void SavePreEditingSettings()
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(REGISTRY_PATH))
                {
                    if (key != null)
                    {
                        key.SetValue("PreEditingEnabled", _chkPreEditing.Checked ? 1 : 0);
                        key.SetValue("PreEditingMarkerInDb", (int)_nudMarkerInDb.Value);
                        key.SetValue("PreEditingMarkerOutDb", (int)_nudMarkerOutDb.Value);
                        key.SetValue("ImportTagsFromFile", _useTagsFromFile ? 1 : 0);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VideoConversionForm] Registry write error: {ex.Message}");
            }
        }

        // ═════════════════════════════════════════════════════════════════
        // Artist / Title extraction helpers
        // ═════════════════════════════════════════════════════════════════

        private (string Artist, string Title) GetArtistTitle(string filePath)
        {
            if (_useTagsFromFile)
                return GetArtistTitleFromTags(filePath);
            else
                return GetArtistTitleFromFilename(filePath);
        }

        private static (string Artist, string Title) GetArtistTitleFromTags(string filePath)
        {
            try
            {
                using var tf = TagLib.File.Create(filePath);
                string artist = tf.Tag.FirstPerformer ?? tf.Tag.FirstAlbumArtist ?? "";
                string title = tf.Tag.Title ?? "";
                if (!string.IsNullOrWhiteSpace(artist) || !string.IsNullOrWhiteSpace(title))
                    return (artist.Trim(), title.Trim());
            }
            catch { /* ignore tag read errors */ }

            // Fallback to filename parsing if tags are empty
            return GetArtistTitleFromFilename(filePath);
        }

        private static (string Artist, string Title) GetArtistTitleFromFilename(string filePath)
        {
            string name = Path.GetFileNameWithoutExtension(filePath);

            // Try "Artist - Title" format (dash separator)
            int idx = name.IndexOf(" - ", StringComparison.Ordinal);
            if (idx > 0)
            {
                string artist = name.Substring(0, idx).Trim();
                string title = name.Substring(idx + 3).Trim();
                return (artist, title);
            }

            // No separator found – use entire name as title
            return ("", name.Trim());
        }

        /// <summary>
        /// Refreshes Artist and Title preview labels for all file rows
        /// based on the current tag source mode.
        /// </summary>
        private void RefreshArtistTitlePreviews()
        {
            foreach (var row in _fileRows)
            {
                var (artist, title) = GetArtistTitle(row.InputPath);
                if (row.LblArtistPreview != null)
                    row.LblArtistPreview.Text = $"👤 {(string.IsNullOrWhiteSpace(artist) ? "(unknown)" : artist)}";
                if (row.LblTitlePreview != null)
                    row.LblTitlePreview.Text = $"🎵 {(string.IsNullOrWhiteSpace(title) ? "(unknown)" : title)}";
            }
        }

        // ═════════════════════════════════════════════════════════════════
        // Pre-editing: marker detection via ffmpeg
        // ═════════════════════════════════════════════════════════════════

        private async Task<(int MarkerInMs, int MarkerOutMs)> DetectMarkersAsync(
            string filePath, double thresholdInDb, double thresholdOutDb, CancellationToken ct)
        {
            int markerIn = -1;
            int markerOut = -1;

            try
            {
                // Use ffmpeg silencedetect with the IN threshold to find the first loud sample
                // Approach: use astats or silencedetect. We'll use silencedetect for simplicity.
                // silencedetect finds silence periods; the end of the first silence period = marker IN
                // For marker OUT we detect from the end.

                string inThreshStr = $"{thresholdInDb:F0}dB";
                string outThreshStr = $"{thresholdOutDb:F0}dB";

                // Marker IN: find end of initial silence (first sample exceeding threshold)
                markerIn = await DetectSilenceEndAsync(filePath, inThreshStr, true, ct);

                // Marker OUT: find start of trailing silence (last sample exceeding threshold)
                markerOut = await DetectSilenceEndAsync(filePath, outThreshStr, false, ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PreEditing] Marker detection error: {ex.Message}");
            }

            return (markerIn, markerOut);
        }

        private Task<int> DetectSilenceEndAsync(
            string filePath, string thresholdDb, bool findStart, CancellationToken ct)
        {
            return Task.Run(() =>
            {
                try
                {
                    // Use silencedetect filter: it reports silence_start and silence_end
                    // For marker IN: we want the silence_end of the first silence period starting at 0
                    // For marker OUT: we want the silence_start of the last silence period ending at EOF
                    string args = $"-hide_banner -i \"{filePath}\" " +
                                  $"-af \"silencedetect=noise={thresholdDb}:d={SILENCE_MIN_DURATION_SEC}\" " +
                                  $"-f null -";

                    var psi = new ProcessStartInfo
                    {
                        FileName = _ffmpegPath,
                        Arguments = args,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    string stderr;
                    using (var p = Process.Start(psi))
                    {
                        var stdoutTask = p.StandardOutput.ReadToEndAsync();
                        stderr = p.StandardError.ReadToEnd();
                        stdoutTask.Wait();
                        p.WaitForExit();
                    }

                    if (string.IsNullOrWhiteSpace(stderr))
                        return -1;

                    if (findStart)
                    {
                        // Find the first silence_end → that's when audio starts
                        var match = Regex.Match(stderr,
                            @"silence_end:\s*([\d.]+)",
                            RegexOptions.IgnoreCase);
                        if (match.Success &&
                            double.TryParse(match.Groups[1].Value,
                                System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture,
                                out double sec))
                        {
                            return (int)(sec * 1000);
                        }

                        // No silence detected at start → marker at 0
                        return 0;
                    }
                    else
                    {
                        // Find the last silence_start → that's where audio effectively ends
                        var matches = Regex.Matches(stderr,
                            @"silence_start:\s*([\d.]+)",
                            RegexOptions.IgnoreCase);
                        if (matches.Count > 0)
                        {
                            var lastMatch = matches[matches.Count - 1];
                            if (double.TryParse(lastMatch.Groups[1].Value,
                                    System.Globalization.NumberStyles.Any,
                                    System.Globalization.CultureInfo.InvariantCulture,
                                    out double sec))
                            {
                                return (int)(sec * 1000);
                            }
                        }

                        // No trailing silence → return -1 (full duration)
                        return -1;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PreEditing] silencedetect error: {ex.Message}");
                    return -1;
                }
            }, ct);
        }

        /// <summary>
        /// Query pre-editing marker results for a file path after conversion is complete.
        /// Returns (-1, -1) if not computed.
        /// </summary>
        public (int MarkerInMs, int MarkerOutMs) GetPreEditMarkers(string filePath)
        {
            if (_preEditResults.TryGetValue(filePath, out var result))
                return result;
            return (-1, -1);
        }

        // ═════════════════════════════════════════════════════════════════
        // Form Load
        // ═════════════════════════════════════════════════════════════════

        private async void VideoConversionForm_Load(object sender, EventArgs e)
        {
            BuildPreEditingPanel();

            // detect if any audio files are present and update title/hint
            bool hasAudio = _inputFiles.Any(f => IsAudioFile(f));
            bool hasVideo = _inputFiles.Any(f => !IsAudioFile(f));

            if (hasAudio && hasVideo)
            {
                lblTitle.Text = "🎬🎵  Media Conversion Queue";
                lblHint.Text =
                    "ℹ️  Video → H.264 16:9 AAC 48kHz 2ch MP4.  " +
                    "Audio → MP3 CBR 320kbps or WAV 16bit (44.1/48kHz).\r\n" +
                    "    Compatible files can be skipped via per-file toggles.  " +
                    "Originals → \"Original Video Files\" / \"Original Audio Files\" subfolder.";
            }
            else if (hasAudio)
            {
                lblTitle.Text = "🎵  Audio Conversion Queue";
                lblHint.Text =
                    "ℹ️  Target: MP3 CBR 320 kbps (44.1/48 kHz) or WAV 16-bit (44.1/48 kHz).\r\n" +
                    "    Compatible files can be skipped via per-file toggles.  " +
                    "Originals → \"Original Audio Files\" subfolder.";
            }

            lblStatus.Text = "⏳  Reading file specifications…";
            btnStart.Enabled = false;
            btnSkipConvert.Enabled = false;

            // probe all files in parallel
            var probeTasks = _inputFiles.Select(f =>
                IsAudioFile(f) ? ProbeAudioFileAsync(f) : ProbeVideoFileAsync(f)).ToArray();
            var probeResults = await Task.WhenAll(probeTasks);

            for (int i = 0; i < _inputFiles.Length; i++)
            {
                bool isAudio = IsAudioFile(_inputFiles[i]);
                if (isAudio)
                    AddAudioFileRow(_inputFiles[i], (AudioSpec)probeResults[i]);
                else
                    AddFileRow(_inputFiles[i], (VideoSpec)probeResults[i]);
            }

            pnlScroll.AutoScrollMinSize =
                new Size(0, _fileRows.Count * (ROW_HEIGHT + ROW_MARGIN) + 10);

            int compatible = _fileRows.Count(r => !r.ChkConvert.Checked);
            lblStatus.Text = compatible > 0
                ? $"✅  {compatible} file(s) already compatible – toggle is OFF.  Press \"Start Conversion\"."
                : "Ready.  Press \"Start Conversion\".";

            progressOverall.Maximum = PROGRESS_MAX; // use weighted progress
            progressOverall.Value = 0;
            lblOverall.Text = $"0 / {_inputFiles.Length} files";

            btnStart.Enabled = true;
            btnSkipConvert.Enabled = true;
        }

        private static bool IsAudioFile(string filePath)
        {
            string ext = Path.GetExtension(filePath);
            return AudioExtensions.Contains(ext);
        }

        // ═════════════════════════════════════════════════════════════════
        // Video Probe  (ffmpeg -i  →  Shell  →  ffprobe  →  TagLib)
        // ═════════════════════════════════════════════════════════════════

        private Task<object> ProbeVideoFileAsync(string filePath)
        {
            return Task.Run<object>(() =>
            {
                var spec = new VideoSpec
                {
                    FileSizeBytes = new FileInfo(filePath).Length
                };

                // 1 – ffmpeg -i  (always works if ffmpeg.exe is present)
                if (File.Exists(_ffmpegPath) && TryProbeFfmpegI(filePath, spec))
                {
                    spec.ProbeSuccess = spec.Width > 0;
                    spec.ProbeMethod = "ffmpeg -i";
                    if (spec.ProbeSuccess) return spec;
                }

                // 2 – Windows Shell Property Store
                if (TryProbeShell(filePath, spec))
                {
                    spec.ProbeSuccess = spec.Width > 0;
                    spec.ProbeMethod = "Windows Shell";
                    if (spec.ProbeSuccess) return spec;
                }

                // 3 – ffprobe
                if (File.Exists(_ffprobePath) && TryProbeFfprobe(filePath, spec))
                {
                    spec.ProbeSuccess = spec.Width > 0;
                    spec.ProbeMethod = "ffprobe";
                    if (spec.ProbeSuccess) return spec;
                }

                // 4 – TagLib (duration only)
                try
                {
                    using var tf = TagLib.File.Create(filePath);
                    if (spec.DurationSeconds <= 0)
                        spec.DurationSeconds = tf.Properties.Duration.TotalSeconds;
                    spec.ProbeMethod = "TagLib (partial)";
                }
                catch { /* ignore */ }

                return spec;
            });
        }

        // ═════════════════════════════════════════════════════════════════
        // Audio Probe  (ffmpeg -i → ffprobe)
        // ═════════════════════════════════════════════════════════════════

        private Task<object> ProbeAudioFileAsync(string filePath)
        {
            return Task.Run<object>(() =>
            {
                var spec = new AudioSpec
                {
                    FileSizeBytes = new FileInfo(filePath).Length
                };

                if (File.Exists(_ffmpegPath) && TryProbeAudioFfmpegI(filePath, spec))
                {
                    spec.ProbeSuccess = true;
                    spec.ProbeMethod = "ffmpeg -i";
                    return spec;
                }

                if (File.Exists(_ffprobePath) && TryProbeAudioFfprobe(filePath, spec))
                {
                    spec.ProbeSuccess = true;
                    spec.ProbeMethod = "ffprobe";
                    return spec;
                }

                // TagLib fallback
                try
                {
                    using var tf = TagLib.File.Create(filePath);
                    if (spec.DurationSeconds <= 0)
                        spec.DurationSeconds = tf.Properties.Duration.TotalSeconds;
                    if (spec.SampleRate == 0)
                        spec.SampleRate = tf.Properties.AudioSampleRate;
                    if (spec.Channels == 0)
                        spec.Channels = tf.Properties.AudioChannels;
                    if (spec.Bitrate == 0)
                        spec.Bitrate = tf.Properties.AudioBitrate; // kbps
                    if (spec.BitDepth == 0)
                        spec.BitDepth = tf.Properties.BitsPerSample;
                    spec.ProbeMethod = "TagLib";
                    spec.ProbeSuccess = spec.SampleRate > 0;
                }
                catch { /* ignore */ }

                return spec;
            });
        }

        private bool TryProbeAudioFfmpegI(string filePath, AudioSpec spec)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = _ffmpegPath,
                    Arguments = $"-hide_banner -i \"{filePath}\"",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                string stderr;
                using (var p = Process.Start(psi))
                {
                    var stdoutTask = p.StandardOutput.ReadToEndAsync();
                    stderr = p.StandardError.ReadToEnd();
                    stdoutTask.Wait();
                    p.WaitForExit();
                }

                if (string.IsNullOrWhiteSpace(stderr))
                    return false;

                // Duration
                var mDur = Regex.Match(stderr,
                    @"Duration:\s*(\d+):(\d+):(\d+)[,\.](\d+)",
                    RegexOptions.IgnoreCase);
                if (mDur.Success)
                {
                    spec.DurationSeconds =
                        int.Parse(mDur.Groups[1].Value) * 3600 +
                        int.Parse(mDur.Groups[2].Value) * 60 +
                        int.Parse(mDur.Groups[3].Value) +
                        double.Parse("0." + mDur.Groups[4].Value);
                }

                // Stream #0:0: Audio: mp3, 44100 Hz, stereo, fltp, 320 kb/s
                var mAudio = Regex.Match(stderr,
                    @"Stream #\S+:\s*Audio:\s*(\S+)[^,]*,\s*(\d+)\s*Hz,\s*(\w+)",
                    RegexOptions.IgnoreCase);

                if (mAudio.Success)
                {
                    spec.AudioCodec = CleanCodecName(mAudio.Groups[1].Value);
                    int.TryParse(mAudio.Groups[2].Value, out spec.SampleRate);

                    string ch = mAudio.Groups[3].Value.ToLower();
                    spec.Channels = ch switch
                    {
                        "mono" => 1,
                        "stereo" => 2,
                        "5.1" => 6,
                        "7.1" => 8,
                        _ => int.TryParse(ch, out int n) ? n : 2
                    };

                    // bitrate from the audio stream line: e.g., "320 kb/s"
                    var mBr = Regex.Match(stderr,
                        @"Stream #\S+:\s*Audio:.*?(\d+)\s*kb/s",
                        RegexOptions.IgnoreCase);
                    if (mBr.Success)
                        int.TryParse(mBr.Groups[1].Value, out spec.Bitrate);

                    // bit depth for PCM: look for "s16" or "s24" or "s32" in the stream line
                    var mBits = Regex.Match(stderr,
                        @"Stream #\S+:\s*Audio:.*?(s|u|f)(\d+)",
                        RegexOptions.IgnoreCase);
                    if (mBits.Success)
                        int.TryParse(mBits.Groups[2].Value, out spec.BitDepth);
                }

                // VBR detection: if bitrate line mentions "VBR" or bitrate in format line differs
                // Also check for "Variable" in the output
                spec.IsVariableBitrate = stderr.IndexOf("Variable", StringComparison.OrdinalIgnoreCase) >= 0;

                // Alternative VBR detection for MP3: check global bitrate vs stream bitrate
                if (!spec.IsVariableBitrate)
                {
                    var mGlobalBr = Regex.Match(stderr,
                        @"bitrate:\s*(\d+)\s*kb/s",
                        RegexOptions.IgnoreCase);
                    if (mGlobalBr.Success && spec.Bitrate > 0)
                    {
                        int globalBr;
                        int.TryParse(mGlobalBr.Groups[1].Value, out globalBr);
                        // If global bitrate differs significantly from stream bitrate, it might be VBR
                        if (globalBr > 0 && Math.Abs(globalBr - spec.Bitrate) > VBR_BITRATE_TOLERANCE_KBPS)
                            spec.IsVariableBitrate = true;
                    }
                }

                Console.WriteLine(
                    $"[AudioProbe ffmpeg-i] {Path.GetFileName(filePath)}: " +
                    $"A:{spec.AudioCodec} {spec.SampleRate}Hz {spec.Channels}ch " +
                    $"{spec.Bitrate}kbps VBR:{spec.IsVariableBitrate} " +
                    $"bits:{spec.BitDepth} dur:{spec.DurationSeconds:F1}s");

                return spec.SampleRate > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AudioProbe ffmpeg-i] Error: {ex.Message}");
                return false;
            }
        }

        private bool TryProbeAudioFfprobe(string filePath, AudioSpec spec)
        {
            try
            {
                if (spec.SampleRate == 0)
                    if (int.TryParse(GetFfprobeValue(filePath, "a:0", "sample_rate"), out int sr))
                        spec.SampleRate = sr;

                if (spec.Channels == 0)
                    if (int.TryParse(GetFfprobeValue(filePath, "a:0", "channels"), out int ch))
                        spec.Channels = ch;

                if (spec.AudioCodec == "?")
                {
                    string ac = GetFfprobeValue(filePath, "a:0", "codec_name");
                    if (!string.IsNullOrWhiteSpace(ac)) spec.AudioCodec = ac;
                }

                if (spec.Bitrate == 0)
                {
                    string br = GetFfprobeValue(filePath, "a:0", "bit_rate");
                    if (int.TryParse(br, out int bitrate))
                        spec.Bitrate = bitrate / 1000; // convert from bps to kbps
                }

                if (spec.BitDepth == 0)
                {
                    string bd = GetFfprobeValue(filePath, "a:0", "bits_per_raw_sample");
                    if (int.TryParse(bd, out int bits))
                        spec.BitDepth = bits;
                    else
                    {
                        // fallback: parse sample_fmt (s16 = 16, s32 = 32, etc.)
                        string fmt = GetFfprobeValue(filePath, "a:0", "sample_fmt");
                        var fmtMatch = Regex.Match(fmt, @"(\d+)");
                        if (fmtMatch.Success)
                            int.TryParse(fmtMatch.Groups[1].Value, out spec.BitDepth);
                    }
                }

                if (spec.DurationSeconds <= 0)
                {
                    string dur = RunFfprobe(
                        $"-v error -show_entries format=duration " +
                        $"-of default=noprint_wrappers=1:nokey=1 \"{filePath}\"");
                    double.TryParse(dur.Trim(),
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out spec.DurationSeconds);
                }

                return spec.SampleRate > 0;
            }
            catch { return false; }
        }

        // ── ffmpeg -i (parses stderr info output) for video ───────────────

        private bool TryProbeFfmpegI(string filePath, VideoSpec spec)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = _ffmpegPath,
                    Arguments = $"-hide_banner -i \"{filePath}\"",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                string stderr;
                using (var p = Process.Start(psi))
                {
                    // read both to avoid deadlock
                    var stdoutTask = p.StandardOutput.ReadToEndAsync();
                    stderr = p.StandardError.ReadToEnd();
                    stdoutTask.Wait();
                    p.WaitForExit();
                    // exit code 1 is normal when no output file is specified – ignore
                }

                if (string.IsNullOrWhiteSpace(stderr))
                    return false;

                // Duration: 00:03:42.50,
                var mDur = Regex.Match(stderr,
                    @"Duration:\s*(\d+):(\d+):(\d+)[,\.](\d+)",
                    RegexOptions.IgnoreCase);
                if (mDur.Success)
                {
                    spec.DurationSeconds =
                        int.Parse(mDur.Groups[1].Value) * 3600 +
                        int.Parse(mDur.Groups[2].Value) * 60 +
                        int.Parse(mDur.Groups[3].Value) +
                        double.Parse("0." + mDur.Groups[4].Value);
                }

                // Stream #0:0: Video: h264 ..., 1920x1080 ..., 25 fps
                var mVideo = Regex.Match(stderr,
                    @"Stream #\S+:\s*Video:\s*(\S+)[^,]*,\s*[^,]+,\s*(\d+)x(\d+)[^,]*,.*?(\d+(?:\.\d+)?)\s*fps",
                    RegexOptions.IgnoreCase);

                if (!mVideo.Success)
                {
                    // simpler fallback: codec + resolution only
                    mVideo = Regex.Match(stderr,
                        @"Stream #\S+:\s*Video:\s*(\S+)[^\n]*?(\d{3,4})x(\d{3,4})",
                        RegexOptions.IgnoreCase);
                }

                if (mVideo.Success)
                {
                    spec.VideoCodec = CleanCodecName(mVideo.Groups[1].Value);
                    int.TryParse(mVideo.Groups[2].Value, out spec.Width);
                    int.TryParse(mVideo.Groups[3].Value, out spec.Height);

                    if (mVideo.Groups.Count > 4 &&
                        double.TryParse(mVideo.Groups[4].Value,
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out double fps))
                        spec.Fps = fps;

                    // fps fallback
                    if (spec.Fps == 0)
                    {
                        var mFps = Regex.Match(stderr,
                            @"(\d+(?:\.\d+)?)\s*(?:fps|tbr)",
                            RegexOptions.IgnoreCase);
                        if (mFps.Success)
                            double.TryParse(mFps.Groups[1].Value,
                                System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture,
                                out spec.Fps);
                    }
                }

                // Stream #0:1: Audio: aac, 48000 Hz, stereo, fltp, 192 kb/s
                var mAudio = Regex.Match(stderr,
                    @"Stream #\S+:\s*Audio:\s*(\S+)[^,]*,\s*(\d+)\s*Hz,\s*(\w+)",
                    RegexOptions.IgnoreCase);

                if (mAudio.Success)
                {
                    spec.AudioCodec = CleanCodecName(mAudio.Groups[1].Value);
                    int.TryParse(mAudio.Groups[2].Value, out spec.AudioSampleRate);

                    string ch = mAudio.Groups[3].Value.ToLower();
                    spec.AudioChannels = ch switch
                    {
                        "mono" => 1,
                        "stereo" => 2,
                        "5.1" => 6,
                        "7.1" => 8,
                        _ => int.TryParse(ch, out int n) ? n : 2
                    };
                }

                Console.WriteLine(
                    $"[Probe ffmpeg-i] {Path.GetFileName(filePath)}: " +
                    $"{spec.Width}x{spec.Height} {spec.Fps:F2}fps " +
                    $"V:{spec.VideoCodec} A:{spec.AudioCodec} " +
                    $"{spec.AudioSampleRate}Hz {spec.AudioChannels}ch " +
                    $"dur:{spec.DurationSeconds:F1}s");

                return spec.Width > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Probe ffmpeg-i] Error: {ex.Message}");
                return false;
            }
        }

        private static string CleanCodecName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "?";
            return Regex.Replace(raw.Trim(), @"[,\(\[].*$", "").Trim();
        }

        // ── Windows Shell Property Store ──────────────────────────────────

        private bool TryProbeShell(string filePath, VideoSpec spec)
        {
            try
            {
                var iid = typeof(IPropertyStore).GUID;
                SHGetPropertyStoreFromParsingName(
                    filePath, IntPtr.Zero, 0, ref iid, out IPropertyStore store);
                if (store == null) return false;

                try
                {
                    PROPVARIANT pv;

                    var key = PKEY_Video_FrameWidth;
                    if (store.GetValue(ref key, out pv) == 0 && spec.Width == 0)
                        spec.Width = (int)pv.AsUInt();

                    key = PKEY_Video_FrameHeight;
                    if (store.GetValue(ref key, out pv) == 0 && spec.Height == 0)
                        spec.Height = (int)pv.AsUInt();

                    key = PKEY_Video_FrameRate;
                    if (store.GetValue(ref key, out pv) == 0 && spec.Fps == 0)
                    {
                        uint raw = pv.AsUInt();
                        spec.Fps = raw > 0 ? raw / 1000.0 : 0;
                    }

                    key = PKEY_Video_Encoding;
                    if (store.GetValue(ref key, out pv) == 0 &&
                        pv.vt == 31 && pv.ptrVal != IntPtr.Zero && spec.VideoCodec == "?")
                        spec.VideoCodec = Marshal.PtrToStringUni(pv.ptrVal) ?? "?";

                    key = PKEY_Audio_SampleRate;
                    if (store.GetValue(ref key, out pv) == 0 && spec.AudioSampleRate == 0)
                        spec.AudioSampleRate = (int)pv.AsUInt();

                    key = PKEY_Audio_Channels;
                    if (store.GetValue(ref key, out pv) == 0 && spec.AudioChannels == 0)
                        spec.AudioChannels = (int)pv.AsUInt();

                    key = PKEY_Audio_Encoding;
                    if (store.GetValue(ref key, out pv) == 0 &&
                        pv.vt == 31 && pv.ptrVal != IntPtr.Zero && spec.AudioCodec == "?")
                        spec.AudioCodec = Marshal.PtrToStringUni(pv.ptrVal) ?? "?";

                    key = PKEY_Media_Duration;
                    if (store.GetValue(ref key, out pv) == 0 &&
                        pv.vt == 21 && pv.ulongVal > 0 && spec.DurationSeconds <= 0)
                        spec.DurationSeconds = pv.ulongVal / 1e7;
                }
                finally
                {
                    Marshal.ReleaseComObject(store);
                }

                return spec.Width > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ShellProbe] {ex.Message}");
                return false;
            }
        }

        // ── ffprobe fallback ──────────────────────────────────────────────

        private bool TryProbeFfprobe(string filePath, VideoSpec spec)
        {
            try
            {
                if (spec.Width == 0)
                    if (int.TryParse(GetFfprobeValue(filePath, "v:0", "width"), out int w))
                        spec.Width = w;

                if (spec.Height == 0)
                    if (int.TryParse(GetFfprobeValue(filePath, "v:0", "height"), out int h))
                        spec.Height = h;

                if (spec.Fps == 0)
                {
                    string fps = GetFfprobeValue(filePath, "v:0", "r_frame_rate");
                    if (fps.Contains("/"))
                    {
                        var parts = fps.Split('/');
                        if (double.TryParse(parts[0], out double n) &&
                            double.TryParse(parts[1], out double d) && d > 0)
                            spec.Fps = n / d;
                    }
                }

                if (spec.VideoCodec == "?")
                {
                    string vc = GetFfprobeValue(filePath, "v:0", "codec_name");
                    if (!string.IsNullOrWhiteSpace(vc)) spec.VideoCodec = vc;
                }

                if (spec.AudioSampleRate == 0)
                    if (int.TryParse(GetFfprobeValue(filePath, "a:0", "sample_rate"), out int sr))
                        spec.AudioSampleRate = sr;

                if (spec.AudioChannels == 0)
                    if (int.TryParse(GetFfprobeValue(filePath, "a:0", "channels"), out int ch))
                        spec.AudioChannels = ch;

                if (spec.AudioCodec == "?")
                {
                    string ac = GetFfprobeValue(filePath, "a:0", "codec_name");
                    if (!string.IsNullOrWhiteSpace(ac)) spec.AudioCodec = ac;
                }

                if (spec.DurationSeconds <= 0)
                {
                    string dur = RunFfprobe(
                        $"-v error -show_entries format=duration " +
                        $"-of default=noprint_wrappers=1:nokey=1 \"{filePath}\"");
                    double.TryParse(dur.Trim(),
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out spec.DurationSeconds);
                }

                return spec.Width > 0;
            }
            catch { return false; }
        }

        private string GetFfprobeValue(string filePath, string stream, string field)
        {
            return RunFfprobe(
                $"-v error -select_streams {stream} " +
                $"-show_entries stream={field} " +
                $"-of default=noprint_wrappers=1:nokey=1 \"{filePath}\"").Trim();
        }

        private string RunFfprobe(string args)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = _ffprobePath,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                string o = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                return o;
            }
            catch { return ""; }
        }

        // ═════════════════════════════════════════════════════════════════
        // Build one visual row – VIDEO
        // ═════════════════════════════════════════════════════════════════

        private void AddFileRow(string filePath, VideoSpec spec)
        {
            int idx = _fileRows.Count;
            int top = idx * (ROW_HEIGHT + ROW_MARGIN) + 4;
            int panW = Math.Max(pnlScroll.ClientSize.Width - 8, 400);

            bool compatible = spec.AlreadyCompatible;
            bool needsConvert = !compatible;

            Panel container = new Panel
            {
                Location = new Point(4, top),
                Size = new Size(panW, ROW_HEIGHT),
                BackColor = compatible
                              ? Color.FromArgb(18, 55, 18)
                              : Color.FromArgb(42, 42, 52),
                BorderStyle = BorderStyle.FixedSingle
            };

            // conversion toggle checkbox
            CheckBox chkConvert = new CheckBox
            {
                Text = "",
                Location = new Point(7, 5),
                Size = new Size(18, 18),
                Checked = needsConvert,
                FlatStyle = FlatStyle.Flat
            };
            chkConvert.CheckedChanged += (s, ev) =>
            {
                container.BackColor = chkConvert.Checked
                    ? Color.FromArgb(42, 42, 52)
                    : Color.FromArgb(18, 55, 18);
            };
            container.Controls.Add(chkConvert);

            // file name (shifted right for checkbox)
            Label lblName = new Label
            {
                Text = $"🎬  {Path.GetFileName(filePath)}",
                Location = new Point(30, 5),
                Size = new Size(panW - 250, 18),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.White,
                AutoEllipsis = true
            };
            container.Controls.Add(lblName);

            // badge – shifted left and brought to front
            string badgeText = !spec.ProbeSuccess
                ? "❓ Specs unavailable"
                : compatible ? "⚡ Already compatible" : "🔄 Needs conversion";
            Color badgeColor = !spec.ProbeSuccess ? Color.Orange
                : compatible ? Color.LightGreen : Color.LightSkyBlue;

            Label lblBadge = new Label
            {
                Text = badgeText,
                Location = new Point(panW - 230, 5),
                Size = new Size(220, 18),
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = badgeColor,
                TextAlign = ContentAlignment.MiddleRight
            };
            container.Controls.Add(lblBadge);
            lblBadge.BringToFront();

            // artist/title preview
            var (artist, title) = GetArtistTitle(filePath);
            Label lblArtistPrev = new Label
            {
                Text = $"👤 {(string.IsNullOrWhiteSpace(artist) ? "(unknown)" : artist)}",
                Location = new Point(30, 24),
                Size = new Size((panW - 37) / 2, 16),
                Font = new Font("Segoe UI", 7.5F, FontStyle.Italic),
                ForeColor = Color.FromArgb(180, 230, 180),
                AutoEllipsis = true
            };
            container.Controls.Add(lblArtistPrev);

            Label lblTitlePrev = new Label
            {
                Text = $"🎵 {(string.IsNullOrWhiteSpace(title) ? "(unknown)" : title)}",
                Location = new Point(30 + (panW - 37) / 2, 24),
                Size = new Size((panW - 37) / 2, 16),
                Font = new Font("Segoe UI", 7.5F, FontStyle.Italic),
                ForeColor = Color.FromArgb(180, 210, 255),
                AutoEllipsis = true
            };
            container.Controls.Add(lblTitlePrev);

            // input spec
            Label lblIn = new Label
            {
                Text = BuildInputSpecText(spec),
                Location = new Point(30, 42),
                Size = new Size(panW - 37, 18),
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(210, 210, 210),
                AutoEllipsis = true
            };
            container.Controls.Add(lblIn);

            // output spec
            Label lblOut = new Label
            {
                Text = BuildOutputSpecText(spec),
                Location = new Point(30, 60),
                Size = new Size(panW - 37, 18),
                Font = new Font("Segoe UI", 8),
                ForeColor = compatible ? Color.LightGreen : Color.FromArgb(150, 220, 255),
                AutoEllipsis = true
            };
            container.Controls.Add(lblOut);

            // progress bar
            ProgressBar pb = new ProgressBar
            {
                Location = new Point(30, 82),
                Size = new Size(panW - 183, 20),
                Minimum = 0,
                Maximum = 100,
                Value = compatible ? 100 : 0,
                Style = ProgressBarStyle.Continuous
            };
            container.Controls.Add(pb);

            // progress label
            Label lblProg = new Label
            {
                Text = needsConvert ? "Waiting…" : "Skip (toggle ON to convert)",
                Location = new Point(pb.Right + 8, 84),
                Size = new Size(panW - pb.Right - 20, 18),
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.LightGray
            };
            container.Controls.Add(lblProg);

            pnlScroll.Controls.Add(container);

            string outputPath = Path.Combine(
                Path.GetDirectoryName(filePath) ?? "",
                Path.GetFileNameWithoutExtension(filePath) + ".mp4");

            _fileRows.Add(new FileRow
            {
                Container = container,
                LblFileName = lblName,
                LblInputSpec = lblIn,
                LblOutputSpec = lblOut,
                LblStatus = lblProg,
                LblArtistPreview = lblArtistPrev,
                LblTitlePreview = lblTitlePrev,
                Bar = pb,
                ChkConvert = chkConvert,
                InputPath = filePath,
                OutputPath = outputPath,
                Spec = spec,
                AudioSpec = null,
                IsAudio = false,
                Skip = compatible,
                Succeeded = false,
                LastReportedPct = 0
            });
        }

        // ═════════════════════════════════════════════════════════════════
        // Build one visual row – AUDIO
        // ═════════════════════════════════════════════════════════════════

        private void AddAudioFileRow(string filePath, AudioSpec spec)
        {
            int idx = _fileRows.Count;
            int top = idx * (ROW_HEIGHT + ROW_MARGIN) + 4;
            int panW = Math.Max(pnlScroll.ClientSize.Width - 8, 400);

            bool compatible = spec.AlreadyCompatible;
            // VBR files should have conversion pre-enabled even if format is compatible
            bool defaultChecked = !compatible || spec.IsVariableBitrate;

            Panel container = new Panel
            {
                Location = new Point(4, top),
                Size = new Size(panW, ROW_HEIGHT),
                BackColor = (compatible && !spec.IsVariableBitrate)
                              ? Color.FromArgb(18, 55, 18)
                              : Color.FromArgb(42, 42, 52),
                BorderStyle = BorderStyle.FixedSingle
            };

            // conversion toggle checkbox
            CheckBox chkConvert = new CheckBox
            {
                Text = "",
                Location = new Point(7, 5),
                Size = new Size(18, 18),
                Checked = defaultChecked,
                FlatStyle = FlatStyle.Flat
            };
            chkConvert.CheckedChanged += (s, ev) =>
            {
                container.BackColor = chkConvert.Checked
                    ? Color.FromArgb(42, 42, 52)
                    : Color.FromArgb(18, 55, 18);
            };
            container.Controls.Add(chkConvert);

            // file name
            Label lblName = new Label
            {
                Text = $"🎵  {Path.GetFileName(filePath)}",
                Location = new Point(30, 5),
                Size = new Size(panW - 250, 18),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.White,
                AutoEllipsis = true
            };
            container.Controls.Add(lblName);

            // badge – shifted left and brought to front
            string badgeText;
            Color badgeColor;
            if (!spec.ProbeSuccess)
            {
                badgeText = "❓ Specs unavailable";
                badgeColor = Color.Orange;
            }
            else if (spec.IsVariableBitrate)
            {
                badgeText = "⚠ VBR → convert to CBR";
                badgeColor = Color.Gold;
            }
            else if (compatible)
            {
                badgeText = "⚡ Already compatible";
                badgeColor = Color.LightGreen;
            }
            else
            {
                badgeText = "🔄 Needs conversion";
                badgeColor = Color.LightSkyBlue;
            }

            Label lblBadge = new Label
            {
                Text = badgeText,
                Location = new Point(panW - 230, 5),
                Size = new Size(220, 18),
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = badgeColor,
                TextAlign = ContentAlignment.MiddleRight
            };
            container.Controls.Add(lblBadge);
            lblBadge.BringToFront();

            // artist/title preview
            var (artist, title) = GetArtistTitle(filePath);
            Label lblArtistPrev = new Label
            {
                Text = $"👤 {(string.IsNullOrWhiteSpace(artist) ? "(unknown)" : artist)}",
                Location = new Point(30, 24),
                Size = new Size((panW - 37) / 2, 16),
                Font = new Font("Segoe UI", 7.5F, FontStyle.Italic),
                ForeColor = Color.FromArgb(180, 230, 180),
                AutoEllipsis = true
            };
            container.Controls.Add(lblArtistPrev);

            Label lblTitlePrev = new Label
            {
                Text = $"🎵 {(string.IsNullOrWhiteSpace(title) ? "(unknown)" : title)}",
                Location = new Point(30 + (panW - 37) / 2, 24),
                Size = new Size((panW - 37) / 2, 16),
                Font = new Font("Segoe UI", 7.5F, FontStyle.Italic),
                ForeColor = Color.FromArgb(180, 210, 255),
                AutoEllipsis = true
            };
            container.Controls.Add(lblTitlePrev);

            // input spec
            Label lblIn = new Label
            {
                Text = BuildAudioInputSpecText(spec),
                Location = new Point(30, 42),
                Size = new Size(panW - 37, 18),
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(210, 210, 210),
                AutoEllipsis = true
            };
            container.Controls.Add(lblIn);

            // output spec
            Label lblOut = new Label
            {
                Text = BuildAudioOutputSpecText(spec),
                Location = new Point(30, 60),
                Size = new Size(panW - 37, 18),
                Font = new Font("Segoe UI", 8),
                ForeColor = (compatible && !spec.IsVariableBitrate) ? Color.LightGreen : Color.FromArgb(150, 220, 255),
                AutoEllipsis = true
            };
            container.Controls.Add(lblOut);

            // progress bar
            ProgressBar pb = new ProgressBar
            {
                Location = new Point(30, 82),
                Size = new Size(panW - 183, 20),
                Minimum = 0,
                Maximum = 100,
                Value = (compatible && !spec.IsVariableBitrate) ? 100 : 0,
                Style = ProgressBarStyle.Continuous
            };
            container.Controls.Add(pb);

            // progress label
            Label lblProg = new Label
            {
                Text = defaultChecked ? "Waiting…" : "Skip (toggle ON to convert)",
                Location = new Point(pb.Right + 8, 84),
                Size = new Size(panW - pb.Right - 20, 18),
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.LightGray
            };
            container.Controls.Add(lblProg);

            pnlScroll.Controls.Add(container);

            // determine output extension based on input
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            string outputExt = (ext == ".wav") ? ".wav" : ".mp3";
            string outputPath = Path.Combine(
                Path.GetDirectoryName(filePath) ?? "",
                Path.GetFileNameWithoutExtension(filePath) + outputExt);

            _fileRows.Add(new FileRow
            {
                Container = container,
                LblFileName = lblName,
                LblInputSpec = lblIn,
                LblOutputSpec = lblOut,
                LblStatus = lblProg,
                LblArtistPreview = lblArtistPrev,
                LblTitlePreview = lblTitlePrev,
                Bar = pb,
                ChkConvert = chkConvert,
                InputPath = filePath,
                OutputPath = outputPath,
                Spec = null,
                AudioSpec = spec,
                IsAudio = true,
                Skip = !defaultChecked,
                Succeeded = false,
                LastReportedPct = 0
            });
        }

        // ─────────────────────────────────────────────────────────────────

        private string BuildInputSpecText(VideoSpec spec)
        {
            string size = FormatBytes(spec.FileSizeBytes);

            if (!spec.ProbeSuccess)
                return $"📥  Input:  {size}  |  ⚠️  Specs could not be read  [method: {spec.ProbeMethod}]";

            string aspect = spec.Width > 0 && spec.Height > 0
                ? (spec.Is169 ? "16:9 ✔" : $"NOT 16:9  ({spec.Width}:{spec.Height})")
                : "?";

            return
                $"📥  Input:  {spec.Width}×{spec.Height}  ({aspect})" +
                $"  {spec.Fps:F2} fps" +
                $"  |  Video: {spec.VideoCodec}" +
                $"  |  Audio: {spec.AudioCodec}  {spec.AudioSampleRate} Hz  {spec.AudioChannels}ch" +
                $"  |  {FormatTime((int)spec.DurationSeconds)}" +
                $"  |  {size}" +
                $"  [via {spec.ProbeMethod}]";
        }

        private string BuildOutputSpecText(VideoSpec spec)
        {
            if (!spec.ProbeSuccess)
                return "📤  Output:  → 1920×1080 letterboxed  H.264 fast  |  → AAC 48000 Hz 2ch @ 192k  |  MP4";

            if (spec.AlreadyCompatible)
                return "✅  Output:  No conversion needed – will be imported as-is";

            bool videoIsH264 = (spec.VideoCodec ?? "")
                .IndexOf("h264", StringComparison.OrdinalIgnoreCase) >= 0;

            string videoOut;
            if (spec.Is169)
            {
                if (videoIsH264)
                    videoOut = $"{spec.Width}×{spec.Height}  (16:9 ✔ + H.264 → video stream copy)";
                else if (spec.Width >= 1920)
                    videoOut = $"{spec.Width}×{spec.Height}  (16:9 ✔, ≥1920 → H.264 re-encode, no resize)";
                else
                {
                    int outH = (int)Math.Round(1920.0 / spec.Width * spec.Height);
                    videoOut = $"{spec.Width}×{spec.Height} → 1920×{outH}  (16:9 kept, scale up → H.264)";
                }
            }
            else
            {
                videoOut =
                    $"{spec.Width}×{spec.Height} → 1920×1080  (NOT 16:9 → letterbox/pillarbox → H.264)";
            }

            bool audioOk =
                (spec.AudioCodec ?? "").IndexOf("aac", StringComparison.OrdinalIgnoreCase) >= 0 &&
                spec.AudioSampleRate == 48000 &&
                spec.AudioChannels == 2;

            string audioOut = audioOk
                ? $"AAC {spec.AudioSampleRate} Hz {spec.AudioChannels}ch (stream copy)"
                : $"{spec.AudioCodec} {spec.AudioSampleRate} Hz → AAC 48000 Hz 2ch @ 192k";

            return $"📤  Output:  {videoOut}  |  {audioOut}  |  MP4";
        }

        private string BuildAudioInputSpecText(AudioSpec spec)
        {
            string size = FormatBytes(spec.FileSizeBytes);

            if (!spec.ProbeSuccess)
                return $"📥  Input:  {size}  |  ⚠️  Specs could not be read  [method: {spec.ProbeMethod}]";

            string vbrTag = spec.IsVariableBitrate ? " (VBR)" : " (CBR)";
            string bitDepthStr = spec.BitDepth > 0 ? $"  {spec.BitDepth}bit" : "";

            return
                $"📥  Input:  {spec.AudioCodec}  {spec.SampleRate} Hz  {spec.Channels}ch" +
                $"  {spec.Bitrate} kbps{vbrTag}{bitDepthStr}" +
                $"  |  {FormatTime((int)spec.DurationSeconds)}" +
                $"  |  {size}" +
                $"  [via {spec.ProbeMethod}]";
        }

        private string BuildAudioOutputSpecText(AudioSpec spec)
        {
            if (!spec.ProbeSuccess)
                return "📤  Output:  → MP3 CBR 320 kbps 44100/48000 Hz stereo";

            if (spec.AlreadyCompatible && !spec.IsVariableBitrate)
                return "✅  Output:  No conversion needed – will be imported as-is";

            // Determine target format
            bool isWav = (spec.AudioCodec ?? "").IndexOf("pcm", StringComparison.OrdinalIgnoreCase) >= 0;
            if (isWav)
            {
                int targetRate = (spec.SampleRate == 48000) ? 48000 : 44100;
                return $"📤  Output:  → WAV 16-bit {targetRate} Hz ≤2ch";
            }

            int targetSr = (spec.SampleRate == 48000) ? 48000 : 44100;
            return $"📤  Output:  → MP3 CBR 320 kbps {targetSr} Hz ≤2ch";
        }

        // ═════════════════════════════════════════════════════════════════
        // START button
        // ═════════════════════════════════════════════════════════════════

        private async void btnStart_Click(object sender, EventArgs e)
        {
            if (!File.Exists(_ffmpegPath))
            {
                MessageBox.Show(
                    $"ffmpeg.exe not found at:\n{_ffmpegPath}\n\n" +
                    "Place ffmpeg.exe in the application folder.",
                    "ffmpeg Not Found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            btnStart.Enabled = false;
            btnSkipConvert.Enabled = false;
            btnCancel.Enabled = true;
            _doneCount = 0;
            _errorCount = 0;

            // split files by toggle state
            var toConvert = _fileRows.Where(r => r.ChkConvert.Checked).ToList();
            var skipRows = _fileRows.Where(r => !r.ChkConvert.Checked).ToList();

            // Mark skipped rows as succeeded immediately
            foreach (var row in skipRows)
            {
                row.Succeeded = true;
                row.IsCompleted = true;
                row.OutputPath = row.InputPath; // pass original path to archive
                SetRowStatus(row, "Skipped ✅", Color.LightGreen, 100);
            }

            Interlocked.Add(ref _doneCount, skipRows.Count);
            UpdateOverallProgress();

            var sem = new SemaphoreSlim(MAX_CONCURRENT, MAX_CONCURRENT);
            var tasks = toConvert.Select(row => Task.Run(async () =>
            {
                await sem.WaitAsync(_cts.Token);
                try
                {
                    if (row.IsAudio)
                        await ConvertAudioRowAsync(row, _cts.Token);
                    else
                        await ConvertRowAsync(row, _cts.Token);
                }
                finally { sem.Release(); }
            }, _cts.Token)).ToList();

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "⏹  Conversion cancelled.";
                btnCancel.Enabled = false;
                btnClose.Enabled = true;
                return;
            }

            // Pre-editing marker detection (if enabled)
            // Analyze audio from INPUT files to position markers based on threshold
            if (_chkPreEditing.Checked && File.Exists(_ffmpegPath))
            {
                lblStatus.Text = "🔍  Analyzing audio for marker detection…";
                var successRows = _fileRows.Where(r => r.Succeeded).ToList();
                foreach (var row in successRows)
                {
                    try
                    {
                        // Analyze the actual file path (output if converted, input if skipped)
                        string fileToAnalyze = File.Exists(row.OutputPath) ? row.OutputPath : row.InputPath;
                        var markers = await DetectMarkersAsync(
                            fileToAnalyze,
                            (double)_nudMarkerInDb.Value,
                            (double)_nudMarkerOutDb.Value,
                            _cts.Token);
                        _preEditResults[row.OutputPath] = markers;

                        // Also store by input path for lookup convenience
                        if (row.InputPath != row.OutputPath)
                            _preEditResults[row.InputPath] = markers;
                    }
                    catch { /* ignore per-file marker errors */ }
                }
            }

            int succeeded = _fileRows.Count(r => r.Succeeded);
            lblStatus.Text = $"✅  Done!  {succeeded} ready,  ❌ {_errorCount} error(s).";
            btnCancel.Enabled = false;
            btnClose.Enabled = true;

            if (succeeded > 0)
            {
                ConversionCompleted?.Invoke(
                    _fileRows
                        .Where(r => r.Succeeded && !string.IsNullOrEmpty(r.OutputPath))
                        .Select(r => r.OutputPath)
                        .ToList());
            }
        }

        // ═════════════════════════════════════════════════════════════════
        // SKIP CONVERSION button
        // ═════════════════════════════════════════════════════════════════

        private void btnSkipConvert_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(
                    "⚠️  Skip Conversion (Not Recommended)\n\n" +
                    "Importing unconverted files may cause playback issues or errors " +
                    "with the VLC-NDI output at 1920×1080 / 48 kHz.\n\n" +
                    "Are you sure you want to import the original files without converting them?",
                    "Skip Conversion — Are you sure?",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes) return;

            ConversionCompleted?.Invoke(_fileRows.Select(r => r.InputPath).ToList());
            Close();
        }

        // ═════════════════════════════════════════════════════════════════
        // Per-row conversion – VIDEO
        // ═════════════════════════════════════════════════════════════════

        private async Task ConvertRowAsync(FileRow row, CancellationToken ct)
        {
            SetRowStatus(row, "Renaming…", Color.LightYellow, -1);

            string dir = Path.GetDirectoryName(row.InputPath) ?? "";
            string nameExt = Path.GetFileName(row.InputPath);
            string prefixed = Path.Combine(dir, "_" + nameExt);

            try
            {
                if (!File.Exists(prefixed))
                    File.Move(row.InputPath, prefixed);
            }
            catch (Exception ex)
            {
                SetRowStatus(row, $"Rename error: {ex.Message}", Color.OrangeRed, 0);
                row.IsCompleted = true;
                Interlocked.Increment(ref _errorCount);
                BumpOverall();
                return;
            }

            var spec = row.Spec;
            string videoArg;
            string audioArg;

            if (spec.ProbeSuccess)
            {
                bool videoIsH264 =
                    (spec.VideoCodec ?? "").IndexOf("h264", StringComparison.OrdinalIgnoreCase) >= 0;

                if (spec.Is169)
                {
                    if (videoIsH264)
                        videoArg = "-vcodec copy";
                    else if (spec.Width >= 1920)
                        videoArg = "-vcodec libx264 -preset fast -crf 20";
                    else
                        videoArg = "-vf \"scale=1920:-2\" -vcodec libx264 -preset fast -crf 20";
                }
                else
                {
                    videoArg =
                        "-vf \"scale=1920:1080:force_original_aspect_ratio=decrease," +
                        "pad=1920:1080:(ow-iw)/2:(oh-ih)/2:color=black\" " +
                        "-vcodec libx264 -preset fast -crf 20";
                }

                bool audioOk =
                    (spec.AudioCodec ?? "").IndexOf("aac", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    spec.AudioSampleRate == 48000 &&
                    spec.AudioChannels == 2;

                audioArg = audioOk
                    ? "-acodec copy"
                    : "-acodec aac -ar 48000 -ac 2 -b:a 192k";
            }
            else
            {
                videoArg =
                    "-vf \"scale=1920:1080:force_original_aspect_ratio=decrease," +
                    "pad=1920:1080:(ow-iw)/2:(oh-ih)/2:color=black\" " +
                    "-vcodec libx264 -preset fast -crf 20";
                audioArg = "-acodec aac -ar 48000 -ac 2 -b:a 192k";
            }

            string ffArgs =
                $"-y -i \"{prefixed}\" {videoArg} {audioArg} " +
                $"-movflags +faststart \"{row.OutputPath}\"";

            double totalSec = spec.DurationSeconds > 0 ? spec.DurationSeconds : 1;
            row.LastReportedPct = 0;
            SetRowStatus(row, "Converting  0%", Color.LightSkyBlue, 0);

            bool ok = await RunFfmpegAsync(
                ffArgs,
                totalSec,
                pct =>
                {
                    // monotonic progress: only allow forward movement
                    int monotonicPct = Math.Max(pct, row.LastReportedPct);
                    row.LastReportedPct = monotonicPct;
                    SetRowProgress(row, monotonicPct);
                    SetRowStatus(row, $"Converting  {monotonicPct}%", Color.LightSkyBlue, -1);
                    UpdateOverallProgress();
                },
                ct);

            if (ct.IsCancellationRequested)
            {
                SetRowStatus(row, "Cancelled", Color.Orange, 0);
                row.IsCompleted = true;
                Interlocked.Increment(ref _errorCount);
                BumpOverall();
                return;
            }

            if (!ok)
            {
                SetRowStatus(row, "Error ❌", Color.OrangeRed, 0);
                row.IsCompleted = true;
                Interlocked.Increment(ref _errorCount);
                BumpOverall();
                return;
            }

            // move original to "Original Video Files"
            try
            {
                string archiveDir = Path.Combine(dir, "Original Video Files");
                Directory.CreateDirectory(archiveDir);
                string dest = Path.Combine(archiveDir, "_" + nameExt);
                if (File.Exists(dest)) File.Delete(dest);
                File.Move(prefixed, dest);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VideoConversionForm] ⚠️  Move original failed: {ex.Message}");
            }

            row.Succeeded = true;
            row.IsCompleted = true;
            SetRowStatus(row, "Done ✅", Color.LightGreen, 100);
            UpdateOutputLabel(row, "✅  Conversion complete – will be imported as MP4");
            BumpOverall();
        }

        // ═════════════════════════════════════════════════════════════════
        // Per-row conversion – AUDIO
        // ═════════════════════════════════════════════════════════════════

        private async Task ConvertAudioRowAsync(FileRow row, CancellationToken ct)
        {
            SetRowStatus(row, "Renaming…", Color.LightYellow, -1);

            string dir = Path.GetDirectoryName(row.InputPath) ?? "";
            string nameExt = Path.GetFileName(row.InputPath);
            string prefixed = Path.Combine(dir, "_" + nameExt);

            try
            {
                if (!File.Exists(prefixed))
                    File.Move(row.InputPath, prefixed);
            }
            catch (Exception ex)
            {
                SetRowStatus(row, $"Rename error: {ex.Message}", Color.OrangeRed, 0);
                row.IsCompleted = true;
                Interlocked.Increment(ref _errorCount);
                BumpOverall();
                return;
            }

            var spec = row.AudioSpec;
            string ffArgs;

            bool isTargetWav = row.OutputPath.EndsWith(".wav", StringComparison.OrdinalIgnoreCase);

            if (isTargetWav)
            {
                // Convert to WAV 16-bit PCM
                int targetRate = (spec != null && spec.SampleRate == 48000) ? 48000 : 44100;
                ffArgs = $"-y -i \"{prefixed}\" -acodec pcm_s16le -ar {targetRate} -ac 2 \"{row.OutputPath}\"";
            }
            else
            {
                // Convert to MP3 CBR 320 kbps
                int targetRate = (spec != null && spec.SampleRate == 48000) ? 48000 : 44100;
                ffArgs = $"-y -i \"{prefixed}\" -acodec libmp3lame -b:a 320k -ar {targetRate} -ac 2 \"{row.OutputPath}\"";
            }

            double totalSec = (spec != null && spec.DurationSeconds > 0) ? spec.DurationSeconds : 1;
            row.LastReportedPct = 0;
            SetRowStatus(row, "Converting  0%", Color.LightSkyBlue, 0);

            bool ok = await RunFfmpegAsync(
                ffArgs,
                totalSec,
                pct =>
                {
                    int monotonicPct = Math.Max(pct, row.LastReportedPct);
                    row.LastReportedPct = monotonicPct;
                    SetRowProgress(row, monotonicPct);
                    SetRowStatus(row, $"Converting  {monotonicPct}%", Color.LightSkyBlue, -1);
                    UpdateOverallProgress();
                },
                ct);

            if (ct.IsCancellationRequested)
            {
                SetRowStatus(row, "Cancelled", Color.Orange, 0);
                row.IsCompleted = true;
                Interlocked.Increment(ref _errorCount);
                BumpOverall();
                return;
            }

            if (!ok)
            {
                SetRowStatus(row, "Error ❌", Color.OrangeRed, 0);
                row.IsCompleted = true;
                Interlocked.Increment(ref _errorCount);
                BumpOverall();
                return;
            }

            // move original to "Original Audio Files"
            try
            {
                string archiveDir = Path.Combine(dir, "Original Audio Files");
                Directory.CreateDirectory(archiveDir);
                string dest = Path.Combine(archiveDir, "_" + nameExt);
                if (File.Exists(dest)) File.Delete(dest);
                File.Move(prefixed, dest);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VideoConversionForm] ⚠️  Move audio original failed: {ex.Message}");
            }

            string fmt = isTargetWav ? "WAV" : "MP3";
            row.Succeeded = true;
            row.IsCompleted = true;
            SetRowStatus(row, "Done ✅", Color.LightGreen, 100);
            UpdateOutputLabel(row, $"✅  Conversion complete – will be imported as {fmt}");
            BumpOverall();
        }

        // ═════════════════════════════════════════════════════════════════
        // ffmpeg runner
        // ═════════════════════════════════════════════════════════════════

        private Task<bool> RunFfmpegAsync(
            string arguments,
            double totalSeconds,
            Action<int> progressCallback,
            CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<bool>();

            var psi = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = arguments,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };

            proc.ErrorDataReceived += (s, e) =>
            {
                if (e.Data == null) return;
                var m = Regex.Match(e.Data, @"time=(\d+):(\d+):(\d+)[,\.](\d+)");
                if (!m.Success) return;

                double cur =
                    int.Parse(m.Groups[1].Value) * 3600 +
                    int.Parse(m.Groups[2].Value) * 60 +
                    int.Parse(m.Groups[3].Value) +
                    double.Parse("0." + m.Groups[4].Value);

                progressCallback((int)Math.Min(99, cur / totalSeconds * 100));
            };

            proc.Exited += (s, e) =>
            {
                bool ok = proc.ExitCode == 0;
                proc.Dispose();
                tcs.TrySetResult(ok);
            };

            ct.Register(() =>
            {
                try { if (!proc.HasExited) proc.Kill(); } catch { /* ignore */ }
                tcs.TrySetCanceled();
            });

            proc.Start();
            proc.BeginErrorReadLine();
            return tcs.Task;
        }

        // ═════════════════════════════════════════════════════════════════
        // Thread-safe UI helpers
        // ═════════════════════════════════════════════════════════════════

        private void SetRowStatus(FileRow row, string text, Color color, int progressValue)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => SetRowStatus(row, text, color, progressValue)));
                return;
            }
            row.LblStatus.Text = text;
            row.LblStatus.ForeColor = color;
            if (progressValue >= 0) row.Bar.Value = progressValue;
        }

        private void SetRowProgress(FileRow row, int pct)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => SetRowProgress(row, pct)));
                return;
            }
            int clamped = Math.Max(0, Math.Min(100, pct));
            // enforce monotonic: never decrease
            if (clamped >= row.Bar.Value)
                row.Bar.Value = clamped;
        }

        private void UpdateOutputLabel(FileRow row, string text)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UpdateOutputLabel(row, text)));
                return;
            }
            row.LblOutputSpec.Text = text;
            row.LblOutputSpec.ForeColor = Color.LightGreen;
        }

        private void BumpOverall()
        {
            if (InvokeRequired) { BeginInvoke(new Action(BumpOverall)); return; }
            Interlocked.Increment(ref _doneCount);
            UpdateOverallProgress();
        }

        /// <summary>
        /// Weighted overall progress: each file contributes proportionally.
        /// Completed files contribute 100%, in-progress files contribute their current %.
        /// </summary>
        private void UpdateOverallProgress()
        {
            if (InvokeRequired) { BeginInvoke(new Action(UpdateOverallProgress)); return; }

            if (_fileRows.Count == 0) return;

            int totalWeight = 0;
            int doneFiles = 0;
            foreach (var row in _fileRows)
            {
                if (row.IsCompleted)
                {
                    totalWeight += 100;
                    doneFiles++;
                }
                else if (!row.ChkConvert.Checked)
                {
                    totalWeight += 100; // skipped = done
                    doneFiles++;
                }
                else
                {
                    totalWeight += row.LastReportedPct;
                }
            }

            int overallPct = totalWeight * PROGRESS_MAX / (_fileRows.Count * 100);
            // enforce monotonic overall progress
            if (overallPct > progressOverall.Value)
                progressOverall.Value = Math.Min(overallPct, PROGRESS_MAX);

            lblOverall.Text = $"{doneFiles} / {_inputFiles.Length} files completed";
        }

        // ═════════════════════════════════════════════════════════════════
        // Cancel / Close
        // ═════════════════════════════════════════════════════════════════

        private void btnCancel_Click(object sender, EventArgs e)
        {
            _cts.Cancel();
            btnCancel.Enabled = false;
            lblStatus.Text = "Cancelling…";
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            _cts.Cancel();
            Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _cts.Cancel();
            base.OnFormClosing(e);
        }

        // ═════════════════════════════════════════════════════════════════
        // Static helpers
        // ═════════════════════════════════════════════════════════════════

        private static string FormatTime(int totalSeconds)
        {
            int m = totalSeconds / 60, s = totalSeconds % 60;
            return $"{m:00}:{s:00}";
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes >= 1_073_741_824) return $"{bytes / 1_073_741_824.0:F2} GB";
            if (bytes >= 1_048_576) return $"{bytes / 1_048_576.0:F1} MB";
            return $"{bytes / 1024.0:F0} KB";
        }
    }
}
