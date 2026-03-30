using System;

namespace AirDirector.Services.Audio
{
    public class MixManager
    {
        private AudioPlayer _playerA;
        private AudioPlayer _playerB;
        private System.Timers.Timer _mixTimer; // ✅ SPECIFICATO TIPO COMPLETO
        private bool _isMixing;
        private float _mixDuration; // in secondi
        private float _mixProgress = 0f;
        private AudioPlayer _mixFrom;
        private AudioPlayer _mixTo;

        public event EventHandler<string> MixStarted; // "A->B" o "B->A"
        public event EventHandler MixCompleted;

        public MixManager(AudioPlayer playerA, AudioPlayer playerB, float mixDurationMs = 5000)
        {
            _playerA = playerA;
            _playerB = playerB;
            _mixDuration = mixDurationMs / 1000f; // Converti in secondi

            _mixTimer = new System.Timers.Timer(50); // ✅ TIPO COMPLETO - Aggiorna ogni 50ms
            _mixTimer.Elapsed += MixTimer_Tick;
        }

        public void StartMix(string direction)
        {
            if (_isMixing) return;

            _isMixing = true;
            _mixProgress = 0f;

            if (direction == "A->B")
            {
                _mixFrom = _playerA;
                _mixTo = _playerB;
            }
            else
            {
                _mixFrom = _playerB;
                _mixTo = _playerA;
            }

            // Avvia player di destinazione con volume 0
            _mixTo.SetVolume(0f);
            _mixTo.Play();

            _mixTimer.Start();
            MixStarted?.Invoke(this, direction);
        }

        private void MixTimer_Tick(object sender, System.Timers.ElapsedEventArgs e)
        {
            _mixProgress += 0.05f / _mixDuration; // Incremento basato su 50ms

            if (_mixProgress >= 1f)
            {
                // Mix completato
                _mixProgress = 1f;
                _mixFrom.SetVolume(0f);
                _mixTo.SetVolume(1f);

                _mixFrom.Stop();
                _mixTimer.Stop();
                _isMixing = false;

                MixCompleted?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                // Crossfade progressivo
                float volumeFrom = 1f - _mixProgress;
                float volumeTo = _mixProgress;

                _mixFrom.SetVolume(volumeFrom);
                _mixTo.SetVolume(volumeTo);
            }
        }

        public void StopMix()
        {
            _mixTimer?.Stop();
            _isMixing = false;
            _mixProgress = 0f;
        }

        public bool IsMixing => _isMixing;

        public void Dispose()
        {
            _mixTimer?.Stop();
            _mixTimer?.Dispose();
        }
    }
}