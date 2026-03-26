using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibVLCSharp.Shared;

namespace AirDirector.Forms
{
    /// <summary>
    /// Separate video preview window for MusicEditorForm in RadioTV mode.
    /// Shows the video in sync with the audio editor (play/pause/stop/seek).
    /// </summary>
    public class VideoPreviewForm : Form
    {
        private LibVLC _vlcLib;
        private LibVLCSharp.Shared.MediaPlayer _vlcMediaPlayer;
        private LibVLCSharp.WinForms.VideoView _videoView;
        private string _videoPath;

        public VideoPreviewForm(string videoPath)
        {
            _videoPath = videoPath;

            this.Text = "📺 Video Preview";
            this.Size = new Size(640, 400);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.SizableToolWindow;
            this.BackColor = Color.Black;
            this.ShowInTaskbar = false;
            this.TopMost = true;

            InitializeVLC();
        }

        private void InitializeVLC()
        {
            try
            {
                LibVLCSharp.Shared.Core.Initialize();
            }
            catch { /* already initialized */ }

            _vlcLib = new LibVLC(
                "--no-audio",
                "--no-osd",
                "--no-stats",
                "--quiet",
                "--avcodec-fast",
                "--avcodec-threads=2"
            );

            _vlcMediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_vlcLib);

            _videoView = new LibVLCSharp.WinForms.VideoView
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                MediaPlayer = _vlcMediaPlayer
            };
            this.Controls.Add(_videoView);

            // Load the video (paused at start)
            var media = new Media(_vlcLib, new Uri(Path.GetFullPath(_videoPath)));
            _vlcMediaPlayer.Media = media;

            _vlcMediaPlayer.Playing += (s, ev) =>
            {
                _vlcMediaPlayer.Mute = true;
            };

            // Play to load the first frame, then pause
            _vlcMediaPlayer.Play();
            Task.Delay(200).ContinueWith(_ =>
            {
                try
                {
                    if (_vlcMediaPlayer != null && _vlcMediaPlayer.IsPlaying)
                    {
                        _vlcMediaPlayer.SetPause(true);
                    }
                }
                catch { }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        // ── Sync methods called by MusicEditorForm ──

        public void SyncPlay()
        {
            if (_vlcMediaPlayer == null) return;
            try
            {
                if (!_vlcMediaPlayer.IsPlaying)
                    _vlcMediaPlayer.Play();
            }
            catch { }
        }

        public void SyncPause()
        {
            if (_vlcMediaPlayer == null) return;
            try
            {
                if (_vlcMediaPlayer.IsPlaying)
                    _vlcMediaPlayer.SetPause(true);
            }
            catch { }
        }

        public void SyncStop()
        {
            if (_vlcMediaPlayer == null) return;
            try
            {
                _vlcMediaPlayer.Stop();
                // Reload media to show first frame
                var media = new Media(_vlcLib, new Uri(Path.GetFullPath(_videoPath)));
                _vlcMediaPlayer.Media = media;
                _vlcMediaPlayer.Play();
                Task.Delay(200).ContinueWith(_ =>
                {
                    try
                    {
                        if (_vlcMediaPlayer != null && _vlcMediaPlayer.IsPlaying)
                            _vlcMediaPlayer.SetPause(true);
                    }
                    catch { }
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch { }
        }

        public void SyncSeek(int audioMs)
        {
            if (_vlcMediaPlayer == null) return;
            try
            {
                if (_vlcMediaPlayer.Length > 0)
                {
                    long seekMs = Math.Max(0L, Math.Min((long)audioMs, _vlcMediaPlayer.Length));
                    _vlcMediaPlayer.Time = seekMs;
                }
            }
            catch { }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    if (_vlcMediaPlayer != null)
                    {
                        if (_vlcMediaPlayer.IsPlaying)
                            _vlcMediaPlayer.Stop();
                        _vlcMediaPlayer.Dispose();
                        _vlcMediaPlayer = null;
                    }
                }
                catch { }

                try { _videoView?.Dispose(); _videoView = null; }
                catch { }

                try { _vlcLib?.Dispose(); _vlcLib = null; }
                catch { }
            }

            base.Dispose(disposing);
        }
    }
}