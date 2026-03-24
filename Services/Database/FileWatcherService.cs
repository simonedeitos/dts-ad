using System;
using System.IO;
using TimersTimer = System.Timers.Timer;  // ← ALIAS

namespace AirDirector.Services.Database
{
    /// <summary>
    /// Monitora cambiamenti nei file DBC per sync multi-istanza
    /// </summary>
    public class FileWatcherService : IDisposable
    {
        private FileSystemWatcher _watcher;
        private TimersTimer _pollingTimer;  // ← USA ALIAS
        private bool _disposed = false;

        public event EventHandler<string> FileChanged;

        public FileWatcherService()
        {
            InitializeWatcher();
            InitializePollingTimer();
        }

        private void InitializeWatcher()
        {
            string databasePath = @"C:\AirDirector\Database";

            if (!Directory.Exists(databasePath))
            {
                Directory.CreateDirectory(databasePath);
            }

            _watcher = new FileSystemWatcher(databasePath)
            {
                Filter = "*.dbc",
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                EnableRaisingEvents = true
            };

            _watcher.Changed += OnFileChanged;
            _watcher.Created += OnFileChanged;
            _watcher.Deleted += OnFileChanged;
            _watcher.Renamed += OnFileRenamed;

            Console.WriteLine($"FileWatcher avviato su: {databasePath}");
        }

        private void InitializePollingTimer()
        {
            // Timer di polling ogni 30 secondi come fallback
            _pollingTimer = new TimersTimer(30000);  // ← USA ALIAS
            _pollingTimer.Elapsed += OnPollingTimerElapsed;
            _pollingTimer.Start();
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                Console.WriteLine($"File modificato: {e.Name}");
                FileChanged?.Invoke(this, e.Name);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore FileWatcher: {ex.Message}");
            }
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            try
            {
                Console.WriteLine($"File rinominato: {e.OldName} → {e.Name}");
                FileChanged?.Invoke(this, e.Name);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore FileWatcher (rename): {ex.Message}");
            }
        }

        private void OnPollingTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)  // ← FIX tipo parametro
        {
            try
            {
                // Controlla se i file sono cambiati (tramite timestamp)
                string[] files = { "Music.dbc", "Clips.dbc", "Genres.dbc", "Categories.dbc",
                                  "MiniPLS.dbc", "Schedules.dbc", "Clocks.dbc",
                                  "Encoders.dbc", "Recorders.dbc" };

                foreach (string file in files)
                {
                    if (DbcManager.HasFileChanged(file))
                    {
                        Console.WriteLine($"Polling: file modificato esternamente: {file}");
                        FileChanged?.Invoke(this, file);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore polling timer: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _watcher?.Dispose();
            _pollingTimer?.Stop();
            _pollingTimer?.Dispose();

            _disposed = true;
        }
    }
}