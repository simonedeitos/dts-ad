using System;
using System.Collections.Generic;
using AirDirector.Controls;

namespace AirDirector.Services.Audio
{
    public class AudioEngine
    {
        private AudioPlayer _playerA;
        private AudioPlayer _playerB;
        private MixManager _mixManager;
        private AudioPlayer _activePlayer;
        private Queue<PlaylistQueueItem> _queue;
        private bool _autoMode;

        // Eventi
        public event EventHandler<string> PlayerChanged; // "A" o "B"
        public event EventHandler<TimeSpan> PositionChanged;
        public event EventHandler TrackEnded;
        public event EventHandler<string> StatusChanged;

        public AudioEngine()
        {
            _playerA = new AudioPlayer("A");
            _playerB = new AudioPlayer("B");
            _mixManager = new MixManager(_playerA, _playerB, 5000);
            _activePlayer = _playerA;
            _queue = new Queue<PlaylistQueueItem>();
            _autoMode = false;

            // Sottoscrivi eventi
            _playerA.PlaybackEnded += Player_TrackEnded;
            _playerB.PlaybackEnded += Player_TrackEnded;
            _playerA.PositionChanged += (s, pos) => PositionChanged?.Invoke(this, pos);
            _playerB.PositionChanged += (s, pos) => PositionChanged?.Invoke(this, pos);
            _mixManager.MixCompleted += MixManager_Completed;
        }

        public void LoadQueue(List<PlaylistQueueItem> items)
        {
            _queue.Clear();
            foreach (var item in items)
            {
                _queue.Enqueue(item);
            }
        }

        public void Play()
        {
            if (_activePlayer.State == PlayerState.Paused)
            {
                _activePlayer.Play();
            }
            else if (_queue.Count > 0)
            {
                PlayNext();
            }
        }

        public void Pause()
        {
            _activePlayer.Pause();
        }

        public void Stop()
        {
            _playerA.Stop();
            _playerB.Stop();
            _mixManager.StopMix();
        }

        public void Next()
        {
            if (_autoMode)
            {
                StartAutoMix();
            }
            else
            {
                PlayNext();
            }
        }

        private void PlayNext()
        {
            if (_queue.Count == 0)
            {
                StatusChanged?.Invoke(this, "Queue vuota");
                return;
            }

            var nextItem = _queue.Dequeue();

            // Ferma player corrente
            _activePlayer.Stop();

            // Carica e riproduci
            if (_activePlayer.Load(nextItem.FilePath))
            {
                _activePlayer.Play();
                StatusChanged?.Invoke(this, $"Playing: {nextItem.Artist} - {nextItem.Title}");
            }
        }

        private void StartAutoMix()
        {
            if (_queue.Count == 0) return;

            var nextItem = _queue.Dequeue();
            AudioPlayer nextPlayer = _activePlayer == _playerA ? _playerB : _playerA;

            // Carica prossima traccia nel player inattivo
            if (nextPlayer.Load(nextItem.FilePath))
            {
                // Avvia mix
                string direction = _activePlayer == _playerA ? "A->B" : "B->A";
                _mixManager.StartMix(direction);
            }
        }

        private void MixManager_Completed(object sender, EventArgs e)
        {
            // Cambia player attivo
            _activePlayer = _activePlayer == _playerA ? _playerB : _playerA;
            PlayerChanged?.Invoke(this, _activePlayer.PlayerName);

            // In auto mode, prepara prossima traccia
            if (_autoMode && _queue.Count > 0)
            {
                // Calcola quando avviare prossimo mix (es: 10 secondi prima della fine)
                // TODO: Implementare timer per auto-mix
            }
        }

        private void Player_TrackEnded(object sender, EventArgs e)
        {
            TrackEnded?.Invoke(this, EventArgs.Empty);

            if (_autoMode)
            {
                PlayNext();
            }
        }

        public void SetAutoMode(bool enabled)
        {
            _autoMode = enabled;
            StatusChanged?.Invoke(this, enabled ? "AUTO Mode" : "MANUAL Mode");
        }

        public AudioPlayer GetPlayerA() => _playerA;
        public AudioPlayer GetPlayerB() => _playerB;
        public AudioPlayer GetActivePlayer() => _activePlayer;
        public bool IsAutoMode => _autoMode;

        public void Dispose()
        {
            _playerA?.Dispose();
            _playerB?.Dispose();
            _mixManager?.Dispose();
        }
    }
}