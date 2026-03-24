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

namespace AirDirector.Forms
{
    public partial class VideoConversionForm : Form
    {
        // ── public callback ───────────────────────────────────────────────
        public event Action<List<string>> ConversionCompleted;

        // ── constants ─────────────────────────────────────────────────────
        private const int MAX_CONCURRENT = 5;
        private const int ROW_HEIGHT = 108;
        private const int ROW_MARGIN = 6;

        // ── state ─────────────────────────────────────────────────────────
        private readonly string[] _inputFiles;
        private readonly string _ffmpegPath;
        private readonly string _ffprobePath;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly List<FileRow> _fileRows = new List<FileRow>();
        private int _doneCount;
        private int _errorCount;

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

        private class FileRow
        {
            public Panel Container;
            public Label LblFileName;
            public Label LblInputSpec;
            public Label LblOutputSpec;
            public Label LblStatus;
            public ProgressBar Bar;
            public string InputPath;
            public string OutputPath;
            public VideoSpec Spec;
            public bool Skip;
            public bool Succeeded;
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
        // Form Load
        // ═════════════════════════════════════════════════════════════════

        private async void VideoConversionForm_Load(object sender, EventArgs e)
        {
            lblStatus.Text = "⏳  Reading file specifications…";
            btnStart.Enabled = false;
            btnSkipConvert.Enabled = false;

            var specs = await Task.WhenAll(_inputFiles.Select(f => ProbeFileAsync(f)));

            for (int i = 0; i < _inputFiles.Length; i++)
                AddFileRow(_inputFiles[i], specs[i]);

            pnlScroll.AutoScrollMinSize =
                new Size(0, _fileRows.Count * (ROW_HEIGHT + ROW_MARGIN) + 10);

            int compatible = _fileRows.Count(r => r.Skip);
            lblStatus.Text = compatible > 0
                ? $"✅  {compatible} file(s) already compatible – will be skipped.  Press \"Start Conversion\"."
                : "Ready.  Press \"Start Conversion\".";

            progressOverall.Maximum = _inputFiles.Length;
            progressOverall.Value = 0;
            lblOverall.Text = $"0 / {_inputFiles.Length} files";

            btnStart.Enabled = true;
            btnSkipConvert.Enabled = true;
        }

        // ═════════════════════════════════════════════════════════════════
        // Probe  (ffmpeg -i  →  Shell  →  ffprobe  →  TagLib)
        // ═════════════════════════════════════════════════════════════════

        private Task<VideoSpec> ProbeFileAsync(string filePath)
        {
            return Task.Run(() =>
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

        // ── ffmpeg -i (parses stderr info output) ─────────────────────────

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
        // Build one visual row
        // ═════════════════════════════════════════════════════════════════

        private void AddFileRow(string filePath, VideoSpec spec)
        {
            int idx = _fileRows.Count;
            int top = idx * (ROW_HEIGHT + ROW_MARGIN) + 4;
            int panW = Math.Max(pnlScroll.ClientSize.Width - 8, 400);

            bool compatible = spec.AlreadyCompatible;

            Panel container = new Panel
            {
                Location = new Point(4, top),
                Size = new Size(panW, ROW_HEIGHT),
                BackColor = compatible
                              ? Color.FromArgb(18, 55, 18)
                              : Color.FromArgb(42, 42, 52),
                BorderStyle = BorderStyle.FixedSingle
            };

            // file name
            Label lblName = new Label
            {
                Text = $"🎬  {Path.GetFileName(filePath)}",
                Location = new Point(7, 5),
                Size = new Size(panW - 200, 18),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.White,
                AutoEllipsis = true
            };
            container.Controls.Add(lblName);

            // badge
            string badgeText = !spec.ProbeSuccess
                ? "❓ Specs unavailable"
                : compatible ? "⚡ Already compatible – skip" : "🔄 Needs conversion";
            Color badgeColor = !spec.ProbeSuccess ? Color.Orange
                : compatible ? Color.LightGreen : Color.LightSkyBlue;

            Label lblBadge = new Label
            {
                Text = badgeText,
                Location = new Point(panW - 195, 5),
                Size = new Size(190, 18),
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = badgeColor,
                TextAlign = ContentAlignment.MiddleRight
            };
            container.Controls.Add(lblBadge);

            // input spec
            Label lblIn = new Label
            {
                Text = BuildInputSpecText(spec),
                Location = new Point(7, 26),
                Size = new Size(panW - 14, 18),
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(210, 210, 210),
                AutoEllipsis = true
            };
            container.Controls.Add(lblIn);

            // output spec
            Label lblOut = new Label
            {
                Text = BuildOutputSpecText(spec),
                Location = new Point(7, 46),
                Size = new Size(panW - 14, 18),
                Font = new Font("Segoe UI", 8),
                ForeColor = compatible ? Color.LightGreen : Color.FromArgb(150, 220, 255),
                AutoEllipsis = true
            };
            container.Controls.Add(lblOut);

            // progress bar
            ProgressBar pb = new ProgressBar
            {
                Location = new Point(7, 68),
                Size = new Size(panW - 160, 20),
                Minimum = 0,
                Maximum = 100,
                Value = compatible ? 100 : 0,
                Style = ProgressBarStyle.Continuous
            };
            container.Controls.Add(pb);

            // progress label
            Label lblProg = new Label
            {
                Text = compatible ? "Skipped ✅" : "Waiting…",
                Location = new Point(pb.Right + 8, 70),
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
                Bar = pb,
                InputPath = filePath,
                OutputPath = outputPath,
                Spec = spec,
                Skip = compatible,
                Succeeded = compatible
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

            var toConvert = _fileRows.Where(r => !r.Skip).ToList();
            var alreadyOk = _fileRows.Where(r => r.Skip).ToList();

            Interlocked.Add(ref _doneCount, alreadyOk.Count);
            UpdateOverallLabel();

            var sem = new SemaphoreSlim(MAX_CONCURRENT, MAX_CONCURRENT);
            var tasks = toConvert.Select(row => Task.Run(async () =>
            {
                await sem.WaitAsync(_cts.Token);
                try { await ConvertRowAsync(row, _cts.Token); }
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
        // Per-row conversion
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
            SetRowStatus(row, "Converting  0%", Color.LightSkyBlue, 0);

            bool ok = await RunFfmpegAsync(
                ffArgs,
                totalSec,
                pct =>
                {
                    SetRowProgress(row, pct);
                    SetRowStatus(row, $"Converting  {pct}%", Color.LightSkyBlue, -1);
                },
                ct);

            if (ct.IsCancellationRequested)
            {
                SetRowStatus(row, "Cancelled", Color.Orange, 0);
                Interlocked.Increment(ref _errorCount);
                BumpOverall();
                return;
            }

            if (!ok)
            {
                SetRowStatus(row, "Error ❌", Color.OrangeRed, 0);
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
            SetRowStatus(row, "Done ✅", Color.LightGreen, 100);
            UpdateOutputLabel(row, "✅  Conversion complete – will be imported as MP4");
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
            row.Bar.Value = Math.Max(0, Math.Min(100, pct));
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
            int done = Interlocked.Increment(ref _doneCount);
            progressOverall.Value = Math.Min(done, progressOverall.Maximum);
            lblOverall.Text = $"{done} / {_inputFiles.Length} files completed";
        }

        private void UpdateOverallLabel()
        {
            if (InvokeRequired) { BeginInvoke(new Action(UpdateOverallLabel)); return; }
            progressOverall.Value = Math.Min(_doneCount, progressOverall.Maximum);
            lblOverall.Text = $"{_doneCount} / {_inputFiles.Length} files completed";
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