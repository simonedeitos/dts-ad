using System;
using System.IO;
using System.Threading;
using NAudio.Wave;
using NAudio.Lame;
using NAudio.CoreAudioApi;
using AirDirector.Models;
using System.Linq;
using ThreadingTimer = System.Threading.Timer;

namespace AirDirector.Services.Core
{
    public class RecorderService : IDisposable
    {
        private Recorder _recorder;
        private WaveInEvent _waveIn;
        private LameMP3FileWriter _mp3Writer;
        private ThreadingTimer _hourlyTimer;
        private ThreadingTimer _schedulerTimer;
        private ThreadingTimer _cleanupTimer;
        private bool _disposed = false;
        private object _lockObject = new object();

        private float _leftLevel;
        private float _rightLevel;

        public event EventHandler<string> LogMessage;
        public event EventHandler<long> FileSizeUpdated;
        public event EventHandler RecordingStarted;
        public event EventHandler RecordingStopped;
        public event EventHandler<float[]> LevelMeterUpdated;

        public RecorderService(Recorder recorder)
        {
            _recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
        }

        public bool Start(out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                if (_recorder.IsRecording)
                {
                    errorMessage = "Registration already in progress";
                    return false;
                }

                if (!Directory.Exists(_recorder.OutputPath))
                {
                    Directory.CreateDirectory(_recorder.OutputPath);
                }

                if (string.IsNullOrEmpty(_recorder.AudioSourceDevice))
                {
                    errorMessage = "Audio device not configured";
                    return false;
                }

                _recorder.IsActive = true;

                switch (_recorder.Type)
                {
                    case Recorder.RecorderType.NinetyDays:
                        StartNinetyDaysRecording();
                        break;

                    case Recorder.RecorderType.Manual:
                        StartManualRecording();
                        break;

                    case Recorder.RecorderType.Scheduled:
                        StartScheduledRecording();
                        break;
                }

                Log($"✅ Recorder avviato: {_recorder.Name}");
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Error start recorder: {ex.Message}";
                Log(errorMessage);
                return false;
            }
        }

        public void Stop()
        {
            try
            {
                _recorder.IsActive = false;
                StopCurrentRecording();

                _hourlyTimer?.Dispose();
                _hourlyTimer = null;

                _schedulerTimer?.Dispose();
                _schedulerTimer = null;

                _cleanupTimer?.Dispose();
                _cleanupTimer = null;

                Log($"✅ Recorder fermato: {_recorder.Name}");
            }
            catch (Exception ex)
            {
                Log($"❌ Errore stop recorder: {ex.Message}");
            }
        }

        private void StartNinetyDaysRecording()
        {
            StartActualRecording();

            // Timer per cambio file ogni ora esatta (HH:00:00)
            TimeSpan delayToNextHour = CalculateNextHourDelay();
            _hourlyTimer = new ThreadingTimer(OnHourlyTimer, null, delayToNextHour, Timeout.InfiniteTimeSpan);

            Log($"Prossimo cambio file tra: {delayToNextHour}");

            if (_recorder.AutoDeleteOldFiles)
            {
                _cleanupTimer = new ThreadingTimer(CleanupOldFiles, null, TimeSpan.FromMinutes(5), TimeSpan.FromHours(6));
            }
        }

        private TimeSpan CalculateNextHourDelay()
        {
            DateTime now = DateTime.Now;
            DateTime nextHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0).AddHours(1);
            return nextHour - now;
        }

        private void OnHourlyTimer(object state)
        {
            try
            {
                Log("⏰ Cambio file orario...");

                StopCurrentRecording();
                Thread.Sleep(200);
                StartActualRecording();

                TimeSpan nextDelay = CalculateNextHourDelay();
                _hourlyTimer?.Change(nextDelay, Timeout.InfiniteTimeSpan);

                Log($"Next file change in: {nextDelay}");
            }
            catch (Exception ex)
            {
                Log($"❌ Time file change error: {ex.Message}");
            }
        }

        private void CleanupOldFiles(object state)
        {
            try
            {
                if (!Directory.Exists(_recorder.OutputPath))
                    return;

                DateTime cutoffDate = DateTime.Now.AddDays(-_recorder.RetentionDays);
                var directories = Directory.GetDirectories(_recorder.OutputPath);

                int deletedFiles = 0;
                int deletedFolders = 0;

                foreach (var dir in directories)
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(dir);

                    if (dirInfo.CreationTime < cutoffDate)
                    {
                        try
                        {
                            var files = dirInfo.GetFiles("*.mp3");
                            deletedFiles += files.Length;
                            dirInfo.Delete(true);
                            deletedFolders++;
                            Log($"🗑️ Folder deleted: {dirInfo.Name} ({files.Length} file)");
                        }
                        catch (Exception ex)
                        {
                            Log($"❌ Error deleting folder {dirInfo.Name}: {ex.Message}");
                        }
                    }
                }

                if (deletedFolders > 0)
                {
                    Log($"✅ Cleanup completed: {deletedFolders} folder, {deletedFiles} deleted files");
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Error cleanup: {ex.Message}");
            }
        }

        private void StartManualRecording()
        {
            StartActualRecording();
        }

        private void StartScheduledRecording()
        {
            if (_recorder.ShouldRecordNow(DateTime.Now))
            {
                StartActualRecording();
            }

            _schedulerTimer = new ThreadingTimer(CheckSchedule, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        private void CheckSchedule(object state)
        {
            try
            {
                bool shouldRecord = _recorder.ShouldRecordNow(DateTime.Now);

                if (shouldRecord && !_recorder.IsRecording)
                {
                    StartActualRecording();
                }
                else if (!shouldRecord && _recorder.IsRecording)
                {
                    StopCurrentRecording();
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Error schedule control: {ex.Message}");
            }
        }

        private void StartActualRecording()
        {
            try
            {
                DateTime now = DateTime.Now;

                // Crea struttura cartelle: OutputPath/YYYY-MM-DD/
                string dateFolder = now.ToString("yyyy-MM-dd");
                string fullFolderPath = Path.Combine(_recorder.OutputPath, dateFolder);

                if (!Directory.Exists(fullFolderPath))
                {
                    Directory.CreateDirectory(fullFolderPath);
                    Log($"📁 Folder created: {dateFolder}");
                }

                // Nome file: YYYY-MM-DD_HH-mm-ss.mp3
                string fileName = now.ToString("yyyy-MM-dd_HH-mm-ss") + ".mp3";
                string fullPath = Path.Combine(fullFolderPath, fileName);

                _recorder.CurrentFileName = fileName;
                _recorder.RecordingStartTime = now;
                _recorder.CurrentFileSize = 0;

                // ✅ TROVA DEVICE AUDIO (senza prefisso)
                int deviceNumber = GetDeviceNumber(_recorder.AudioSourceDevice);

                if (deviceNumber < 0)
                {
                    Log($"❌ Device '{_recorder.AudioSourceDevice}' not found, use default");
                    deviceNumber = 0;
                }

                // Configura WaveIn
                _waveIn = new WaveInEvent
                {
                    DeviceNumber = deviceNumber,
                    WaveFormat = new WaveFormat(44100, _recorder.GetChannels()),
                    BufferMilliseconds = 50
                };

                // Configura MP3 Writer
                _mp3Writer = new LameMP3FileWriter(fullPath, _waveIn.WaveFormat, _recorder.GetBitrate());

                _waveIn.DataAvailable += WaveIn_DataAvailable;
                _waveIn.RecordingStopped += WaveIn_RecordingStopped;

                _waveIn.StartRecording();

                _recorder.IsRecording = true;
                _recorder.StatusText = "Recordings...";

                RecordingStarted?.Invoke(this, EventArgs.Empty);
                Log($"🎙️ Recording started: {dateFolder}/{fileName}");
                Log($"🎤 Device: {WaveIn.GetCapabilities(deviceNumber).ProductName}");
            }
            catch (Exception ex)
            {
                Log($"❌ Error starting recording: {ex.Message}");
                _recorder.IsRecording = false;
                _recorder.StatusText = "Errore";
            }
        }

        private void StopCurrentRecording()
        {
            try
            {
                lock (_lockObject)
                {
                    if (_waveIn != null)
                    {
                        _waveIn.StopRecording();
                        _waveIn.DataAvailable -= WaveIn_DataAvailable;
                        _waveIn.RecordingStopped -= WaveIn_RecordingStopped;
                        _waveIn.Dispose();
                        _waveIn = null;
                    }

                    if (_mp3Writer != null)
                    {
                        _mp3Writer.Flush();
                        _mp3Writer.Dispose();
                        _mp3Writer = null;
                    }
                }

                _recorder.IsRecording = false;
                _recorder.StatusText = _recorder.IsActive ? "In attesa" : "Inactive";

                RecordingStopped?.Invoke(this, EventArgs.Empty);
                Log($"⏹️ Recording stopped: {_recorder.CurrentFileName}");
            }
            catch (Exception ex)
            {
                Log($"❌ Recording stop error: {ex.Message}");
            }
        }

        private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            try
            {
                lock (_lockObject)
                {
                    if (_mp3Writer != null)
                    {
                        _mp3Writer.Write(e.Buffer, 0, e.BytesRecorded);
                        _recorder.CurrentFileSize += e.BytesRecorded;
                        FileSizeUpdated?.Invoke(this, _recorder.CurrentFileSize);
                    }
                }

                CalculateLevel(e.Buffer, e.BytesRecorded);
            }
            catch (Exception ex)
            {
                Log($"❌ Errore scrittura audio: {ex.Message}");
            }
        }

        private void WaveIn_RecordingStopped(object sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
            {
                Log($"❌ Errore registrazione: {e.Exception.Message}");
            }
        }

        private void CalculateLevel(byte[] buffer, int bytesRecorded)
        {
            try
            {
                if (_waveIn?.WaveFormat.BitsPerSample != 16) return;

                int channels = _waveIn.WaveFormat.Channels;
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
            catch { }
        }

        // ✅ TROVA DEVICE PER NOME (senza prefisso)
        private int GetDeviceNumber(string deviceName)
        {
            if (string.IsNullOrEmpty(deviceName))
                return 0;

            for (int i = 0; i < WaveIn.DeviceCount; i++)
            {
                var caps = WaveIn.GetCapabilities(i);
                if (caps.ProductName.Equals(deviceName, StringComparison.OrdinalIgnoreCase))
                {
                    Log($"✅ Device trovato: {caps.ProductName} (#{i})");
                    return i;
                }
            }

            Log($"⚠️ Device '{deviceName}' non trovato");
            return -1;
        }

        private void Log(string message)
        {
            Console.WriteLine($"[RecorderService] {message}");
            LogMessage?.Invoke(this, message);
        }

        public void Dispose()
        {
            if (_disposed) return;

            Stop();
            _disposed = true;
        }
    }
}