using NAudio.Wave;
using NAudio.Lame;
using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Net.Sockets;
using System.Text;
using NAudio.CoreAudioApi;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;

namespace AirDirector.Models
{
    public class StreamEncoder : IDisposable
    {
        public int EncoderId { get; set; }
        public string Name { get; set; }
        public string StationName { get; set; }
        public string AudioSourceDevice { get; set; }
        public int AudioDeviceNumber { get; set; } = -1;
        public string ServerUrl { get; set; }
        public int ServerPort { get; set; }
        public string ServerUsername { get; set; }
        public string ServerPassword { get; set; }
        public string MountPoint { get; set; }
        public int Bitrate { get; set; }
        public string Format { get; set; }

        private bool _enableAGC = false;
        public bool EnableAGC => _enableAGC;

        private float _agcTargetLevel = 0.2f;
        public float AGCTargetLevel => _agcTargetLevel;

        private float _agcAttackTime = 0.5f;
        public float AGCAttackTime => _agcAttackTime;

        private float _agcReleaseTime = 3.0f;
        public float AGCReleaseTime => _agcReleaseTime;

        private float _limiterThreshold = 0.95f;
        public float LimiterThreshold => _limiterThreshold;

        public bool IsActive { get; private set; }
        public DateTime StartTime { get; private set; }
        public TimeSpan Uptime => IsActive ? DateTime.Now - StartTime : TimeSpan.Zero;
        public string StatusText => IsActive ? "Online" : "Offline";
        public Color StatusTextColor => IsActive ? Color.White : Color.Gray;
        public Color StatusBackColor => IsActive ? Color.Green : Color.Transparent;
        public bool DSPActive => _dspActive;

        private bool _dspActive = false;

        private IWaveIn _audioSource;
        private TcpClient _tcpClient;
        private NetworkStream _networkStream;
        private LameMP3FileWriter _mp3Writer;

        private byte[][] _audioBuffer;
        private volatile int _writeIndex = 0;
        private volatile int _readIndex = 0;
        private const int BUFFER_SIZE = 150;
        private volatile int _bufferCount = 0;
        private readonly object _bufferLock = new object();

        private Thread _encodingThread;
        private Thread _agcThread;
        private volatile bool _isEncoding = false;
        private AutoResetEvent _audioAvailable = new AutoResetEvent(false);

        private float _leftLevel;
        private float _rightLevel;

        private volatile float _leftGain = 1.0f;
        private volatile float _rightGain = 1.0f;
        private ConcurrentQueue<byte[]> _agcQueue;
        private AutoResetEvent _agcAvailable = new AutoResetEvent(false);

        private readonly float[] _leftHistory = new float[10];
        private readonly float[] _rightHistory = new float[10];
        private int _historyIndex = 0;

        private bool _preBuffering = true;
        private int _preBufferCount = 0;
        private const int PRE_BUFFER_TARGET = 20;

        private bool _disposed = false;

        private string _currentMetadata = "";
        private readonly object _metadataLock = new object();

        public event EventHandler<float[]> LevelMeterUpdated;
        public event EventHandler<bool> DSPStateChanged;

        public StreamEncoder()
        {
            EncoderId = 0;
            Name = "Nuovo Encoder";
            StationName = "Nuova Stazione";
            ServerUrl = "127.0.0.1";
            ServerPort = 8000;
            ServerUsername = "source";
            ServerPassword = "source";
            MountPoint = "";
            Bitrate = 128;
            Format = "MP3";
            IsActive = false;

            _audioBuffer = new byte[BUFFER_SIZE][];
            _agcQueue = new ConcurrentQueue<byte[]>();
        }

        public void SetAGCEnabled(bool enabled)
        {
            if (_enableAGC != enabled)
            {
                _enableAGC = enabled;
                UpdateDSPState();
            }
        }

        public void UpdateAGCSettings(float targetLevel, float attackTime, float releaseTime, float limiterThreshold)
        {
            _agcTargetLevel = targetLevel;
            _agcAttackTime = attackTime;
            _agcReleaseTime = releaseTime;
            _limiterThreshold = limiterThreshold;
        }

        private void UpdateDSPState()
        {
            bool shouldBeActive = IsActive && _enableAGC;
            if (_dspActive != shouldBeActive)
            {
                _dspActive = shouldBeActive;
                DSPStateChanged?.Invoke(this, _dspActive);
            }
        }

        public void LoadAGCSettings()
        {
            // Già caricato da Registry
        }

        public bool Start(out string errorMessage)
        {
            errorMessage = null;

            if (IsActive)
                return true;

            try
            {
                Console.WriteLine($"[{Name ?? StationName}] === AVVIO ENCODER ===");

                if (string.IsNullOrEmpty(MountPoint))
                {
                    errorMessage = "MountPoint non può essere vuoto";
                    return false;
                }

                if (string.IsNullOrEmpty(AudioSourceDevice))
                {
                    errorMessage = "Nessun dispositivo audio selezionato";
                    return false;
                }

                ConnectToServer();
                InitializeAudio();

                _writeIndex = 0;
                _readIndex = 0;
                _bufferCount = 0;
                _preBuffering = true;
                _preBufferCount = 0;
                _isEncoding = true;

                _encodingThread = new Thread(EncodingThreadProc)
                {
                    Name = $"Encoder-{Name ?? StationName}",
                    IsBackground = true,
                    Priority = ThreadPriority.Highest
                };
                _encodingThread.Start();

                if (_enableAGC)
                {
                    _agcThread = new Thread(AGCThreadProc)
                    {
                        Name = $"AGC-{Name ?? StationName}",
                        IsBackground = true,
                        Priority = ThreadPriority.BelowNormal
                    };
                    _agcThread.Start();
                }

                Thread.Sleep(100);
                _audioSource.StartRecording();

                IsActive = true;
                StartTime = DateTime.Now;
                UpdateDSPState();

                Console.WriteLine($"[{Name ?? StationName}] ✅ ENCODER AVVIATO!");
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Errore: {ex.Message}";
                Console.WriteLine($"[{Name ?? StationName}] ❌ ERRORE: {errorMessage}");
                CleanupResources();
                return false;
            }
        }

        private void ConnectToServer()
        {
            _tcpClient = new TcpClient();
            _tcpClient.SendBufferSize = 65536;
            _tcpClient.ReceiveBufferSize = 8192;
            _tcpClient.NoDelay = true;
            _tcpClient.SendTimeout = 10000;

            Console.WriteLine($"[{Name ?? StationName}] Connessione a {ServerUrl}:{ServerPort}...");
            _tcpClient.Connect(ServerUrl, ServerPort);

            _networkStream = _tcpClient.GetStream();

            string mountpoint = MountPoint;
            if (!mountpoint.StartsWith("/"))
                mountpoint = "/" + mountpoint;

            string auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{ServerUsername}:{ServerPassword}"));

            string header =
                $"SOURCE {mountpoint} HTTP/1.0\r\n" +
                $"Authorization: Basic {auth}\r\n" +
                $"User-Agent: AirDirector/1.0\r\n" +
                $"Content-Type: audio/mpeg\r\n" +
                $"ice-name: {StationName}\r\n" +
                $"ice-public: 1\r\n" +
                $"ice-genre: various\r\n" +
                $"ice-description: {StationName}\r\n" +
                $"ice-bitrate: {Bitrate}\r\n" +
                "\r\n";

            byte[] headerBytes = Encoding.ASCII.GetBytes(header);
            _networkStream.Write(headerBytes, 0, headerBytes.Length);
            _networkStream.Flush();

            byte[] responseBuffer = new byte[4096];
            int bytesRead = _networkStream.Read(responseBuffer, 0, responseBuffer.Length);

            if (bytesRead > 0)
            {
                string response = Encoding.ASCII.GetString(responseBuffer, 0, bytesRead);

                if (!response.Contains("200"))
                    throw new Exception($"Server rifiuta: {response}");
            }

            Console.WriteLine($"[{Name ?? StationName}] ✅ Connesso a Icecast!");
        }

        private void InitializeAudio()
        {
            if (AudioSourceDevice != null && AudioSourceDevice.StartsWith("WASAPI - "))
            {
                string deviceName = AudioSourceDevice.Substring(9);
                var enumerator = new MMDeviceEnumerator();
                var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                var device = devices.FirstOrDefault(d => d.FriendlyName == deviceName);

                if (device != null)
                {
                    _audioSource = new WasapiLoopbackCapture(device);
                }
                else
                {
                    var defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                    _audioSource = new WasapiLoopbackCapture(defaultDevice);
                }
            }
            else
            {
                if (AudioDeviceNumber < 0)
                {
                    for (int i = 0; i < WaveIn.DeviceCount; i++)
                    {
                        var caps = WaveIn.GetCapabilities(i);
                        if (AudioSourceDevice == null || caps.ProductName.Contains(AudioSourceDevice))
                        {
                            AudioDeviceNumber = i;
                            break;
                        }
                    }
                    if (AudioDeviceNumber < 0 && WaveIn.DeviceCount > 0)
                        AudioDeviceNumber = 0;
                }

                _audioSource = new WaveInEvent
                {
                    DeviceNumber = AudioDeviceNumber,
                    WaveFormat = new WaveFormat(44100, 16, 2),
                    BufferMilliseconds = 50
                };
            }

            _audioSource.DataAvailable += AudioSource_DataAvailable;
            _mp3Writer = new LameMP3FileWriter(_networkStream, _audioSource.WaveFormat, Bitrate);

            Console.WriteLine($"[{Name ?? StationName}] ✅ Audio inizializzato!");
        }

        private void AudioSource_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (!IsActive || !_isEncoding) return;

            try
            {
                CalculateLevel(e.Buffer, e.BytesRecorded);

                byte[] audioData = new byte[e.BytesRecorded];
                Buffer.BlockCopy(e.Buffer, 0, audioData, 0, e.BytesRecorded);

                lock (_bufferLock)
                {
                    _audioBuffer[_writeIndex] = audioData;
                    _writeIndex = (_writeIndex + 1) % BUFFER_SIZE;

                    if (_bufferCount < BUFFER_SIZE)
                        _bufferCount++;
                    else
                        _readIndex = (_readIndex + 1) % BUFFER_SIZE;
                }

                if (_preBuffering)
                {
                    _preBufferCount++;
                    if (_preBufferCount >= PRE_BUFFER_TARGET)
                        _preBuffering = false;
                }

                if (!_preBuffering)
                    _audioAvailable.Set();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{Name ?? StationName}] Errore cattura: {ex.Message}");
            }
        }

        private void EncodingThreadProc()
        {
            while (_isEncoding)
            {
                try
                {
                    if (!_audioAvailable.WaitOne(100))
                        continue;

                    while (_bufferCount > 0 && _isEncoding)
                    {
                        byte[] audioData = null;

                        lock (_bufferLock)
                        {
                            if (_bufferCount > 0)
                            {
                                audioData = _audioBuffer[_readIndex];
                                _readIndex = (_readIndex + 1) % BUFFER_SIZE;
                                _bufferCount--;
                            }
                        }

                        if (audioData != null)
                        {
                            if (_dspActive && _enableAGC)
                            {
                                byte[] agcData = new byte[audioData.Length];
                                Buffer.BlockCopy(audioData, 0, agcData, 0, audioData.Length);
                                _agcQueue.Enqueue(agcData);
                                _agcAvailable.Set();
                            }

                            try
                            {
                                _mp3Writer?.Write(audioData, 0, audioData.Length);
                            }
                            catch { }
                        }
                    }
                }
                catch
                {
                    Thread.Sleep(5);
                }
            }
        }

        private void AGCThreadProc()
        {
            int agcUpdateCounter = 0;

            while (_isEncoding)
            {
                try
                {
                    if (!_agcAvailable.WaitOne(200))
                        continue;

                    while (_agcQueue.TryDequeue(out byte[] audioData))
                    {
                        if (!_isEncoding) break;
                        ApplySimpleAGC(audioData, audioData.Length, ref agcUpdateCounter);
                    }
                }
                catch
                {
                    Thread.Sleep(10);
                }
            }
        }

        private void ApplySimpleAGC(byte[] buffer, int bytesRecorded, ref int agcUpdateCounter)
        {
            if (_audioSource.WaveFormat.BitsPerSample != 16) return;

            int channels = _audioSource.WaveFormat.Channels;
            if (channels < 1 || channels > 2) return;

            int stride = 2 * channels;
            agcUpdateCounter++;
            bool updateGain = (agcUpdateCounter % 15 == 0);

            float currentLeftLevel = 0;
            float currentRightLevel = 0;

            for (int i = 0; i < bytesRecorded; i += stride * 8)
            {
                if (i + stride <= bytesRecorded)
                {
                    short sampleL = (short)((buffer[i + 1] << 8) | buffer[i]);
                    float levelL = Math.Abs(sampleL) / 32767f;
                    currentLeftLevel = Math.Max(currentLeftLevel, levelL);

                    if (channels > 1)
                    {
                        short sampleR = (short)((buffer[i + 3] << 8) | buffer[i + 2]);
                        float levelR = Math.Abs(sampleR) / 32767f;
                        currentRightLevel = Math.Max(currentRightLevel, levelR);
                    }
                    else
                    {
                        currentRightLevel = currentLeftLevel;
                    }
                }
            }

            if (updateGain)
            {
                _leftHistory[_historyIndex] = currentLeftLevel;
                _rightHistory[_historyIndex] = currentRightLevel;
                _historyIndex = (_historyIndex + 1) % _leftHistory.Length;

                float avgLeft = 0, avgRight = 0;
                for (int i = 0; i < _leftHistory.Length; i++)
                {
                    avgLeft += _leftHistory[i];
                    avgRight += _rightHistory[i];
                }
                avgLeft /= _leftHistory.Length;
                avgRight /= _rightHistory.Length;

                if (avgLeft > 0.02f)
                {
                    float targetLeftGain = _agcTargetLevel / avgLeft;
                    _leftGain += (targetLeftGain - _leftGain) * 0.01f;
                    _leftGain = Math.Min(Math.Max(_leftGain, 0.3f), 2.0f);
                }

                if (avgRight > 0.02f)
                {
                    float targetRightGain = _agcTargetLevel / avgRight;
                    _rightGain += (targetRightGain - _rightGain) * 0.01f;
                    _rightGain = Math.Min(Math.Max(_rightGain, 0.3f), 2.0f);
                }
            }
        }

        private void CalculateLevel(byte[] buffer, int bytesRecorded)
        {
            if (_audioSource.WaveFormat.BitsPerSample != 16) return;

            int channels = _audioSource.WaveFormat.Channels;
            if (channels < 1 || channels > 2) return;

            _leftLevel = 0;
            _rightLevel = 0;

            int stride = 2 * channels;

            for (int i = 0; i < bytesRecorded; i += stride)
            {
                if (i + 2 <= bytesRecorded)
                {
                    short sample = (short)((buffer[i + 1] << 8) | buffer[i]);
                    float normSample = Math.Abs(sample) / 32768f;
                    _leftLevel = Math.Max(_leftLevel, normSample);

                    if (channels > 1 && i + 4 <= bytesRecorded)
                    {
                        short sampleR = (short)((buffer[i + 3] << 8) | buffer[i + 2]);
                        float normSampleR = Math.Abs(sampleR) / 32768f;
                        _rightLevel = Math.Max(_rightLevel, normSampleR);
                    }
                    else
                    {
                        _rightLevel = _leftLevel;
                    }
                }
            }

            LevelMeterUpdated?.Invoke(this, new float[] { _leftLevel, _rightLevel });
        }

        // ✅ AGGIORNA METADATA ICECAST
        public void UpdateMetadata(string artist, string title)
        {
            if (!IsActive)
                return;

            try
            {
                string metadata = string.IsNullOrEmpty(artist) ? title : $"{artist} - {title}";

                System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        string mountpoint = MountPoint;
                        if (!mountpoint.StartsWith("/"))
                            mountpoint = "/" + mountpoint;

                        // ✅ ENCODING MANUALE SENZA System.Web
                        string song = metadata
                            .Replace(" ", "+")
                            .Replace("&", "%26")
                            .Replace("=", "%3D")
                            .Replace("? ", "%3F")
                            .Replace("#", "%23");

                        string url = $"http://{ServerUrl}:{ServerPort}/admin/metadata?mount={mountpoint}&mode=updinfo&song={song}";

                        System.Diagnostics.Debug.WriteLine($"[{Name ?? StationName}] 🎵 URL: {url}");

                        var request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
                        request.Method = "GET";
                        request.Credentials = new NetworkCredential("admin", "hackme");
                        request.UserAgent = "AirDirector/1.0";
                        request.Timeout = 5000;

                        using (var response = (System.Net.HttpWebResponse)request.GetResponse())
                        {
                            using (var reader = new StreamReader(response.GetResponseStream()))
                            {
                                string result = reader.ReadToEnd();
                                System.Diagnostics.Debug.WriteLine($"[{Name ?? StationName}] ✅ OK: {result}");
                            }
                        }
                    }
                    catch (WebException webEx)
                    {
                        if (webEx.Response != null)
                        {
                            using (var reader = new StreamReader(webEx.Response.GetResponseStream()))
                            {
                                string errorResponse = reader.ReadToEnd();
                                System.Windows.Forms.MessageBox.Show(
                                    $"Icecast dice:\n\n{errorResponse}",
                                    "Errore 400",
                                    System.Windows.Forms.MessageBoxButtons.OK,
                                    System.Windows.Forms.MessageBoxIcon.Error
                                );
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"ERRORE: {ex.Message}");
                    }
                });
            }
            catch { }
        }

        public void Stop()
        {
            if (!IsActive) return;

            try
            {
                IsActive = false;
                _dspActive = false;
                _isEncoding = false;

                _audioSource?.StopRecording();
                _audioAvailable?.Set();
                _agcAvailable?.Set();

                _encodingThread?.Join(2000);
                _agcThread?.Join(1000);

                DSPStateChanged?.Invoke(this, false);
                CleanupResources();

                Console.WriteLine($"[{Name ?? StationName}] ✅ Encoder arrestato");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{Name ?? StationName}] Errore stop: {ex.Message}");
            }
        }

        private void CleanupResources()
        {
            if (_audioSource != null)
            {
                try
                {
                    _audioSource.DataAvailable -= AudioSource_DataAvailable;
                    _audioSource.Dispose();
                }
                catch { }
                _audioSource = null;
            }

            _mp3Writer?.Dispose();
            _mp3Writer = null;

            _networkStream?.Close();
            _networkStream = null;

            _tcpClient?.Close();
            _tcpClient = null;

            lock (_bufferLock)
            {
                for (int i = 0; i < BUFFER_SIZE; i++)
                    _audioBuffer[i] = null;

                _bufferCount = 0;
                _writeIndex = 0;
                _readIndex = 0;
            }

            while (_agcQueue.TryDequeue(out _)) { }

            _leftLevel = 0;
            _rightLevel = 0;
            _leftGain = 1.0f;
            _rightGain = 1.0f;
        }

        public string GetFormatString()
        {
            string agcText = _enableAGC ? " + AGC" : "";
            return $"MP3 {Bitrate}kbps{agcText}";
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                Stop();
                _audioAvailable?.Dispose();
                _agcAvailable?.Dispose();
            }
        }
    }
}