using System;
using System.IO;

namespace AirDirector.Services.Audio
{
    public class AudioPlayer
    {
        public string PlayerName { get; private set; } // "A" o "B"
        public PlayerState State { get; private set; }
        public string CurrentFilePath { get; private set; }
        public TimeSpan CurrentPosition { get; private set; }
        public TimeSpan TotalDuration { get; private set; }
        public float Volume { get; private set; }

        // Eventi
        public event EventHandler PlaybackStarted;
        public event EventHandler PlaybackPaused;
        public event EventHandler PlaybackStopped;
        public event EventHandler PlaybackEnded;
        public event EventHandler<TimeSpan> PositionChanged;

        private System.Timers.Timer _positionTimer;

        public AudioPlayer(string playerName)
        {
            PlayerName = playerName;
            State = PlayerState.Stopped;
            Volume = 1.0f;
            CurrentPosition = TimeSpan.Zero;
            TotalDuration = TimeSpan.Zero;

            _positionTimer = new System.Timers.Timer(100); // Aggiorna ogni 100ms
            _positionTimer.Elapsed += (s, e) => UpdatePosition();
        }

        public bool Load(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                Stop();

                CurrentFilePath = filePath;

                // TODO: Implementare caricamento con NAudio/BASS
                // Per ora simula durata
                TotalDuration = TimeSpan.FromMinutes(3); // Simulato
                CurrentPosition = TimeSpan.Zero;

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{PlayerName}] Errore caricamento: {ex.Message}");
                return false;
            }
        }

        public void Play()
        {
            if (string.IsNullOrEmpty(CurrentFilePath))
                return;

            // TODO: Implementare play con NAudio/BASS
            State = PlayerState.Playing;
            _positionTimer.Start();
            PlaybackStarted?.Invoke(this, EventArgs.Empty);
        }

        public void Pause()
        {
            if (State != PlayerState.Playing)
                return;

            // TODO: Implementare pause con NAudio/BASS
            State = PlayerState.Paused;
            _positionTimer.Stop();
            PlaybackPaused?.Invoke(this, EventArgs.Empty);
        }

        public void Stop()
        {
            // TODO: Implementare stop con NAudio/BASS
            State = PlayerState.Stopped;
            CurrentPosition = TimeSpan.Zero;
            _positionTimer.Stop();
            PlaybackStopped?.Invoke(this, EventArgs.Empty);
        }

        public void SetVolume(float volume)
        {
            Volume = Math.Max(0f, Math.Min(1f, volume));
            // TODO: Applicare volume con NAudio/BASS
        }

        public void Seek(TimeSpan position)
        {
            if (position < TimeSpan.Zero || position > TotalDuration)
                return;

            CurrentPosition = position;
            // TODO: Implementare seek con NAudio/BASS
        }

        private void UpdatePosition()
        {
            if (State == PlayerState.Playing)
            {
                // TODO: Leggere posizione reale da NAudio/BASS
                CurrentPosition = CurrentPosition.Add(TimeSpan.FromMilliseconds(100));

                PositionChanged?.Invoke(this, CurrentPosition);

                // Controlla fine traccia
                if (CurrentPosition >= TotalDuration)
                {
                    State = PlayerState.Stopped;
                    _positionTimer.Stop();
                    PlaybackEnded?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public void Dispose()
        {
            Stop();
            _positionTimer?.Dispose();
            // TODO: Rilasciare risorse NAudio/BASS
        }
    }

    public enum PlayerState
    {
        Stopped,
        Playing,
        Paused
    }
}