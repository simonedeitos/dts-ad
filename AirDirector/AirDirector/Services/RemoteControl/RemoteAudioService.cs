using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Lame;

namespace AirDirector.Services.RemoteControl
{
    public class AudioQualityPreset
    {
        public int SampleRate { get; set; }
        public int Bitrate { get; set; }
        public int Channels { get; set; }

        public static readonly Dictionary<string, AudioQualityPreset> Presets = new()
        {
            { "low",    new AudioQualityPreset { SampleRate = 22050, Bitrate = 32000,  Channels = 1 } },
            { "medium", new AudioQualityPreset { SampleRate = 44100, Bitrate = 128000, Channels = 2 } },
            { "high",   new AudioQualityPreset { SampleRate = 48000, Bitrate = 256000, Channels = 2 } },
            { "studio", new AudioQualityPreset { SampleRate = 48000, Bitrate = 320000, Channels = 2 } }
        };
    }

    public class RemoteAudioService : IDisposable
    {
        private RemoteControlService _remoteService;
        private string _audioSource = "airdirector";
        private string _audioQuality = "medium";
        private string _outputDevice = "default";

        // Capture
        private IWaveIn _captureDevice;
        private MemoryStream _encodingBuffer;
        private LameMP3FileWriter _mp3Writer;
        private readonly object _encodeLock = new object();
        private WaveFormat _captureFormat;
        private WaveFormat _targetMp3Format;
        private bool _needsConversion;
        // Outgoing audio: accumulate encoded MP3 bytes until we have at least this many before sending,
        // so that browser decodeAudioData() gets a chunk large enough to contain valid MP3 frames.
        private readonly MemoryStream _sendAccumulateBuffer = new MemoryStream();
        private const int MinSendBytes = 16384; // 16 KB — ensures complete MP3 frames for browser decoding

        // Playback
        private WaveOutEvent _waveOut;
        private BufferedWaveProvider _playbackBuffer;

        // Receive decode buffer: accumulate WebM/Opus chunks in memory; write to a temp file so
        // MediaFoundationReader can open it (it requires a seekable file path, not a MemoryStream).
        // Using FileShare.ReadWrite avoids lock conflicts between the writer and the MF reader.
        private MemoryStream _receiveWebmBuffer = null;
        private string _receiveTempPath = null;
        private long _receiveReaderPcmPosition = 0;
        private int _receiveChunkCount = 0;
        private readonly object _receiveLock = new object();

        private bool _isCapturing = false;
        private bool _disposed = false;

        public event EventHandler<float> InputLevelChanged;
        public event EventHandler<float> OutputLevelChanged;

        public RemoteAudioService(RemoteControlService remoteService)
        {
            _remoteService = remoteService;
        }

        // ── Configuration ────────────────────────────────────────────────────────

        public void Configure(string audioSource, string audioQuality, string outputDevice)
        {
            _audioSource = audioSource;
            _audioQuality = audioQuality;
            _outputDevice = outputDevice;
        }

        // ── Capture ──────────────────────────────────────────────────────────────

        public void StartCapture()
        {
            if (_isCapturing) return;

            try
            {
                if (!AudioQualityPreset.Presets.TryGetValue(_audioQuality, out var preset))
                    preset = AudioQualityPreset.Presets["medium"];

                _encodingBuffer = new MemoryStream();

                if (_audioSource == "airdirector")
                {
                    // Capture loopback (AirDirector output)
                    var capture = new WasapiLoopbackCapture();
                    _captureDevice = capture;
                }
                else
                {
                    // Capture from named input device
                    int deviceIndex = GetInputDeviceIndex(_audioSource);
                    var waveIn = new WaveInEvent
                    {
                        DeviceNumber = deviceIndex,
                        WaveFormat = new WaveFormat(preset.SampleRate, 16, preset.Channels)
                    };
                    _captureDevice = waveIn;
                }

                _captureFormat = _captureDevice.WaveFormat;
                _targetMp3Format = new WaveFormat(preset.SampleRate, 16, preset.Channels);
                _needsConversion = _captureFormat.Encoding != WaveFormatEncoding.Pcm
                                   || _captureFormat.BitsPerSample != 16
                                   || _captureFormat.SampleRate != _targetMp3Format.SampleRate
                                   || _captureFormat.Channels != _targetMp3Format.Channels;

                _mp3Writer = new LameMP3FileWriter(_encodingBuffer, _targetMp3Format, preset.Bitrate / 1000);

                _captureDevice.DataAvailable += OnCaptureDataAvailable;
                _captureDevice.StartRecording();
                _isCapturing = true;

                Console.WriteLine($"[RemoteAudioService] 🎤 Capture started — source: {_audioSource}, quality: {_audioQuality}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RemoteAudioService] ❌ StartCapture error: {ex.Message}");
            }
        }

        public void StopCapture()
        {
            if (!_isCapturing) return;

            try
            {
                _captureDevice?.StopRecording();
                _captureDevice?.Dispose();
                _captureDevice = null;

                lock (_encodeLock)
                {
                    _mp3Writer?.Dispose();
                    _mp3Writer = null;
                    _encodingBuffer?.Dispose();
                    _encodingBuffer = null;
                    // Discard any buffered-but-unsent data
                    _sendAccumulateBuffer.SetLength(0);
                    _sendAccumulateBuffer.Position = 0;
                }

                _isCapturing = false;
                Console.WriteLine("[RemoteAudioService] ⏹ Capture stopped");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RemoteAudioService] ⚠️ StopCapture error: {ex.Message}");
            }
        }

        private void OnCaptureDataAvailable(object sender, WaveInEventArgs e)
        {
            if (e.BytesRecorded == 0) return;

            // Calculate input level
            float level = CalculateLevel(e.Buffer, e.BytesRecorded);
            InputLevelChanged?.Invoke(this, level);

            // Encode to MP3 and send via WebSocket as base64 JSON so the relay server can forward it
            lock (_encodeLock)
            {
                if (_mp3Writer == null || _encodingBuffer == null) return;
                try
                {
                    byte[] dataToEncode = e.Buffer;
                    int bytesToEncode = e.BytesRecorded;

                    if (_needsConversion)
                    {
                        dataToEncode = ConvertAudioBuffer(e.Buffer, e.BytesRecorded, _captureFormat, _targetMp3Format);
                        bytesToEncode = dataToEncode.Length;
                    }

                    long positionBefore = _encodingBuffer.Position;
                    _mp3Writer.Write(dataToEncode, 0, bytesToEncode);
                    _mp3Writer.Flush();

                    long newBytes = _encodingBuffer.Position - positionBefore;
                    if (newBytes > 0)
                    {
                        byte[] mp3Data = new byte[newBytes];
                        _encodingBuffer.Position = positionBefore;
                        _encodingBuffer.Read(mp3Data, 0, mp3Data.Length);
                        _encodingBuffer.SetLength(0);
                        _encodingBuffer.Position = 0;

                        // Accumulate until we have at least MinSendBytes so the browser's
                        // decodeAudioData() receives a chunk large enough to contain valid MP3 frames.
                        _sendAccumulateBuffer.Write(mp3Data, 0, mp3Data.Length);
                        if (_sendAccumulateBuffer.Length >= MinSendBytes)
                        {
                            byte[] toSend = _sendAccumulateBuffer.ToArray();
                            _sendAccumulateBuffer.SetLength(0);
                            _sendAccumulateBuffer.Position = 0;

                            // Send as base64 JSON so the WS relay server can forward to browser clients
                            var msg = new { type = "audio_data", data = Convert.ToBase64String(toSend) };
                            string json = Newtonsoft.Json.JsonConvert.SerializeObject(msg);
                            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
                            _ = _remoteService.SendRawTextAsync(bytes);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[RemoteAudioService] ⚠️ Encode error: {ex.Message}");
                }
            }
        }

        // ── Playback ─────────────────────────────────────────────────────────────

        public void StartPlayback()
        {
            try
            {
                if (!AudioQualityPreset.Presets.TryGetValue(_audioQuality, out var preset))
                    preset = AudioQualityPreset.Presets["medium"];

                var format = new WaveFormat(preset.SampleRate, 16, preset.Channels);
                _playbackBuffer = new BufferedWaveProvider(format)
                {
                    BufferDuration = TimeSpan.FromSeconds(2),
                    DiscardOnBufferOverflow = true
                };

                int deviceIndex = GetOutputDeviceIndex(_outputDevice);
                _waveOut = new WaveOutEvent { DeviceNumber = deviceIndex };
                _waveOut.Init(_playbackBuffer);
                _waveOut.Play();

                Console.WriteLine("[RemoteAudioService] 🔊 Playback started");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RemoteAudioService] ❌ StartPlayback error: {ex.Message}");
            }
        }

        public void StopPlayback()
        {
            try
            {
                _waveOut?.Stop();
                _waveOut?.Dispose();
                _waveOut = null;
                _playbackBuffer = null;

                // Reset receive decode state so the next session starts fresh
                lock (_receiveLock)
                {
                    _receiveReaderPcmPosition = 0;
                    _receiveChunkCount = 0;
                    _receiveWebmBuffer?.Dispose();
                    _receiveWebmBuffer = null;
                    if (_receiveTempPath != null)
                    {
                        try { File.Delete(_receiveTempPath); }
                        catch (Exception ex) { Console.WriteLine($"[RemoteAudioService] ⚠️ Could not delete receive temp file: {ex.Message}"); }
                        _receiveTempPath = null;
                    }
                }

                Console.WriteLine("[RemoteAudioService] ⏹ Playback stopped");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RemoteAudioService] ⚠️ StopPlayback error: {ex.Message}");
            }
        }

        public void FeedReceivedAudio(byte[] pcmData)
        {
            if (_playbackBuffer == null) return;
            try
            {
                _playbackBuffer.AddSamples(pcmData, 0, pcmData.Length);
                float level = CalculateLevel(pcmData, pcmData.Length);
                OutputLevelChanged?.Invoke(this, level);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RemoteAudioService] ⚠️ FeedReceivedAudio error: {ex.Message}");
            }
        }

        private void FeedReceivedAudio(byte[] pcmData, int length)
        {
            if (_playbackBuffer == null) return;
            try
            {
                _playbackBuffer.AddSamples(pcmData, 0, length);
                float level = CalculateLevel(pcmData, length);
                OutputLevelChanged?.Invoke(this, level);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RemoteAudioService] ⚠️ FeedReceivedAudio error: {ex.Message}");
            }
        }

        /// <summary>Feed received audio from a base64-encoded JSON audio_data message (WebM/Opus or MP3).</summary>
        public void FeedReceivedAudioBase64(string base64Data)
        {
            lock (_receiveLock)
            {
                if (_playbackBuffer == null) return;
                try
                {
                    byte[] encoded = Convert.FromBase64String(base64Data);
                    _receiveChunkCount++;

                    // First, try direct MP3 decoding via Mp3FileReader on a MemoryStream.
                    // This handles the case where the browser (or a future client) sends MP3 directly.
                    try
                    {
                        using var mp3Stream = new MemoryStream(encoded);
                        using var mp3Reader = new Mp3FileReader(mp3Stream);
                        var targetFmt = _playbackBuffer.WaveFormat;
                        byte[] pcmBuf = new byte[4096];
                        int n;
                        if (!mp3Reader.WaveFormat.Equals(targetFmt))
                        {
                            using var resampler = new MediaFoundationResampler(mp3Reader, targetFmt) { ResamplerQuality = 60 };
                            while ((n = resampler.Read(pcmBuf, 0, pcmBuf.Length)) > 0)
                                FeedReceivedAudio(pcmBuf, n);
                        }
                        else
                        {
                            while ((n = mp3Reader.Read(pcmBuf, 0, pcmBuf.Length)) > 0)
                                FeedReceivedAudio(pcmBuf, n);
                        }
                        return;
                    }
                    catch
                    {
                        // Not MP3 — fall through to WebM/Opus path
                    }

                    // Accumulate all encoded chunks in a MemoryStream.
                    // WebM/Opus from browser MediaRecorder requires all chunks because the first one
                    // carries the container header (EBML) needed to decode subsequent clusters.
                    if (_receiveWebmBuffer == null)
                        _receiveWebmBuffer = new MemoryStream();

                    _receiveWebmBuffer.Write(encoded, 0, encoded.Length);

                    // Write accumulated buffer to a temp file so MediaFoundationReader can open it
                    // (MediaFoundationReader requires a seekable file path, not a MemoryStream).
                    if (_receiveTempPath == null)
                        _receiveTempPath = Path.Combine(Path.GetTempPath(), $"ad_rcv_{Guid.NewGuid():N}.webm");

                    // Use ReadWrite sharing so the MF reader can open the file while we write
                    using (var fs = new FileStream(_receiveTempPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                    {
                        _receiveWebmBuffer.Position = 0;
                        _receiveWebmBuffer.CopyTo(fs);
                    }

                    try
                    {
                        using var reader = new MediaFoundationReader(_receiveTempPath);
                        if (_receiveReaderPcmPosition >= reader.Length)
                            return; // No new decodable data yet

                        if (_receiveReaderPcmPosition > 0)
                            reader.Position = _receiveReaderPcmPosition;

                        var targetFmt = _playbackBuffer.WaveFormat;
                        byte[] pcmBuf = new byte[4096];
                        int n;

                        if (!reader.WaveFormat.Equals(targetFmt))
                        {
                            using var resampler = new MediaFoundationResampler(reader, targetFmt) { ResamplerQuality = 60 };
                            while ((n = resampler.Read(pcmBuf, 0, pcmBuf.Length)) > 0)
                                FeedReceivedAudio(pcmBuf, n);
                        }
                        else
                        {
                            while ((n = reader.Read(pcmBuf, 0, pcmBuf.Length)) > 0)
                                FeedReceivedAudio(pcmBuf, n);
                        }

                        // Track reader's PCM position so next call skips already-decoded audio
                        _receiveReaderPcmPosition = reader.Position;
                    }
                    catch (Exception ex)
                    {
                        // The first few chunks may not yet contain enough data for a complete WebM header.
                        // Only log a warning after the 5th failed chunk to avoid spamming on startup.
                        if (_receiveChunkCount > 5)
                            Console.WriteLine($"[RemoteAudioService] ⚠️ Browser→AirDirector audio decode failed ({ex.GetType().Name}): {ex.Message}. " +
                                "Ensure Windows 10+ with Media Foundation codecs installed, or check that browser sends WebM/Opus.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[RemoteAudioService] ⚠️ FeedReceivedAudioBase64 error: {ex.Message}");
                }
            }
        }

        // ── Device enumeration ────────────────────────────────────────────────

        public static List<string> GetInputDevices()
        {
            var list = new List<string>();
            for (int i = 0; i < WaveInEvent.DeviceCount; i++)
            {
                var cap = WaveInEvent.GetCapabilities(i);
                list.Add(cap.ProductName);
            }
            return list;
        }

        public static List<string> GetOutputDevices()
        {
            var list = new List<string>();
            for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                var cap = WaveOut.GetCapabilities(i);
                list.Add(cap.ProductName);
            }
            return list;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static int GetInputDeviceIndex(string deviceName)
        {
            for (int i = 0; i < WaveInEvent.DeviceCount; i++)
            {
                var cap = WaveInEvent.GetCapabilities(i);
                if (cap.ProductName.Equals(deviceName, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return 0;
        }

        private static int GetOutputDeviceIndex(string deviceName)
        {
            if (string.IsNullOrEmpty(deviceName) || deviceName == "default")
                return -1; // -1 = default device in WaveOut

            for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                var cap = WaveOut.GetCapabilities(i);
                if (cap.ProductName.Equals(deviceName, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        private static float CalculateLevel(byte[] buffer, int length)
        {
            if (length < 2) return 0f;
            float sum = 0f;
            int samples = length / 2;
            for (int i = 0; i < length - 1; i += 2)
            {
                short sample = (short)(buffer[i] | (buffer[i + 1] << 8));
                float normalized = sample / 32768f;
                sum += normalized * normalized;
            }
            return (float)Math.Sqrt(sum / samples);
        }

        /// <summary>Convert an audio buffer from sourceFormat (e.g. IEEE Float 32-bit loopback) to targetFormat (PCM 16-bit).</summary>
        private static byte[] ConvertAudioBuffer(byte[] input, int length, WaveFormat sourceFormat, WaveFormat targetFormat)
        {
            using var sourceStream = new RawSourceWaveStream(new MemoryStream(input, 0, length), sourceFormat);
            using var resampler = new MediaFoundationResampler(sourceStream, targetFormat) { ResamplerQuality = 60 };
            var output = new MemoryStream();
            byte[] buffer = new byte[4096];
            int bytesRead;
            while ((bytesRead = resampler.Read(buffer, 0, buffer.Length)) > 0)
                output.Write(buffer, 0, bytesRead);
            return output.ToArray();
        }

        // ── IDisposable ──────────────────────────────────────────────────────

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            StopCapture();
            StopPlayback();
            _sendAccumulateBuffer.Dispose();
        }
    }
}
