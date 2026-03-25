using System;
using System.IO;

namespace AirDirector.Services.Core
{
    public sealed class DailyLogger : IDisposable
    {
        private readonly string _prefix;
        private readonly string _logsRoot;
        private readonly object _lock = new object();
        private StreamWriter? _writer;
        private string _currentDate = "";
        private bool _disposed;

        public DailyLogger(string prefix)
        {
            _prefix = prefix;
            _logsRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            EnsureWriter();
            WriteRaw("════════════════════════════════════════════════════════");
            WriteRaw("  APPLICATION START  " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            WriteRaw("════════════════════════════════════════════════════════");
        }

        public void Log(string message)
        {
            string line = "[" + DateTime.Now.ToString("HH:mm:ss.fff") + "] " + message;
            Console.WriteLine(line);
            lock (_lock)
            {
                try
                {
                    EnsureWriter();
                    _writer?.WriteLine(line);
                }
                catch { }
            }
        }

        public void LogErr(string message, Exception ex)
        {
            Log("ERR " + message + ": " + ex.Message);
        }

        public void LogErr(string message)
        {
            Log("ERR " + message);
        }

        private void WriteRaw(string line)
        {
            lock (_lock)
            {
                try
                {
                    EnsureWriter();
                    _writer?.WriteLine(line);
                }
                catch { }
            }
        }

        private void EnsureWriter()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (today == _currentDate && _writer != null)
                return;

            try
            {
                _writer?.Flush();
                _writer?.Dispose();
            }
            catch { }

            try
            {
                string dayFolder = Path.Combine(_logsRoot, today);
                Directory.CreateDirectory(dayFolder);
                string filePath = Path.Combine(dayFolder, _prefix + "-" + today + ".log");
                _writer = new StreamWriter(filePath, true) { AutoFlush = true };
                _currentDate = today;
            }
            catch
            {
                _writer = null;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            lock (_lock)
            {
                try { _writer?.Dispose(); } catch { }
                _writer = null;
            }
        }
    }
}
