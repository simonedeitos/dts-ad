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

        // Playback
        private WaveOutEvent _waveOut;
        private BufferedWaveProvider _playbackBuffer;

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

                var captureFormat = _captureDevice.WaveFormat;
                var mp3Format = new WaveFormat(preset.SampleRate, 16, preset.Channels);

                _mp3Writer = new LameMP3FileWriter(_encodingBuffer, mp3Format, preset.Bitrate / 1000);

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
                    long positionBefore = _encodingBuffer.Position;
                    _mp3Writer.Write(e.Buffer, 0, e.BytesRecorded);
                    _mp3Writer.Flush();

                    long newBytes = _encodingBuffer.Position - positionBefore;
                    if (newBytes > 0)
                    {
                        byte[] mp3Data = new byte[newBytes];
                        _encodingBuffer.Position = positionBefore;
                        _encodingBuffer.Read(mp3Data, 0, mp3Data.Length);
                        _encodingBuffer.SetLength(0);
                        _encodingBuffer.Position = 0;

                        // Send as base64 JSON so the WS relay server can forward to browser clients
                        var msg = new { type = "audio_data", data = Convert.ToBase64String(mp3Data) };
                        string json = Newtonsoft.Json.JsonConvert.SerializeObject(msg);
                        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
                        _ = _remoteService.SendRawTextAsync(bytes);
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

        /// <summary>Feed received audio from a base64-encoded JSON audio_data message.</summary>
        public void FeedReceivedAudioBase64(string base64Data)
        {
            try
            {
                byte[] mp3Data = Convert.FromBase64String(base64Data);
                FeedReceivedAudio(mp3Data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RemoteAudioService] ⚠️ FeedReceivedAudioBase64 error: {ex.Message}");
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

        // ── IDisposable ──────────────────────────────────────────────────────

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            StopCapture();
            StopPlayback();
        }
    }
}
