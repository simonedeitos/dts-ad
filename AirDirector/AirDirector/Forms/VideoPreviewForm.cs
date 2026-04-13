using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using LibVLCSharp.Shared;
using AirDirector.Services.Localization;

namespace AirDirector.Forms
{
    /// <summary>
    /// Separate video preview window for MusicEditorForm in RadioTV mode.
    /// Shows the video in sync with the audio editor (play/pause/stop/seek).
    /// Uses a shared static LibVLC instance so that disposing this form never corrupts
    /// the VLC runtime used by other components (ArchiveControl mini-player, etc.).
    /// </summary>
    public class VideoPreviewForm : Form
    {
        // ── Shared static LibVLC instance – created once, never disposed ──
        private static LibVLC _sharedVlcLib;
        private static readonly object _vlcLock = new object();

        private LibVLCSharp.Shared.MediaPlayer _vlcMediaPlayer;
        private LibVLCSharp.WinForms.VideoView _videoView;
        private string _videoPath;
        private bool _isDisposed = false;

        private static LibVLC GetSharedLibVLC()
        {
            if (_sharedVlcLib == null)
            {
                lock (_vlcLock)
                {
                    if (_sharedVlcLib == null)
                    {
                        try { LibVLCSharp.Shared.Core.Initialize(); } catch { /* already initialized */ }
                        _sharedVlcLib = new LibVLC(
                            "--no-audio",
                            "--no-osd",
                            "--no-stats",
                            "--quiet",
                            "--avcodec-fast",
                            "--avcodec-threads=2"
                        );
                    }
                }
            }
            return _sharedVlcLib;
        }

        public VideoPreviewForm(string videoPath)
        {
            _videoPath = videoPath;

            this.Text = "📺 " + LanguageManager.GetString("VideoPreview.Title", "Video Preview");
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
            var vlcLib = GetSharedLibVLC();
            _vlcMediaPlayer = new LibVLCSharp.Shared.MediaPlayer(vlcLib);
            _vlcMediaPlayer.Mute = true;

            _videoView = new LibVLCSharp.WinForms.VideoView
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                MediaPlayer = _vlcMediaPlayer
            };
            this.Controls.Add(_videoView);

            // Load the video and show the first frame paused
            LoadMediaAndShowFirstFrame();
        }

        /// <summary>
        /// Loads (or reloads) the video media, plays it briefly to render the first frame,
        /// then immediately pauses. Uses the Playing event instead of Task.Delay so that
        /// it works reliably even when the form is reopened after a previous stop.
        /// </summary>
        private void LoadMediaAndShowFirstFrame()
        {
            if (_isDisposed || _vlcMediaPlayer == null) return;
            try
            {
                var vlcLib = GetSharedLibVLC();
                var media = new Media(vlcLib, new Uri(Path.GetFullPath(_videoPath)));
                _vlcMediaPlayer.Media = media;
                media.Dispose(); // safe to dispose after assigning to the player

                // Subscribe once: pause on the very first Playing event to freeze at frame 0
                EventHandler<EventArgs> onPlaying = null;
                onPlaying = (s, ev) =>
                {
                    try
                    {
                        if (_vlcMediaPlayer != null)
                        {
                            _vlcMediaPlayer.Mute = true;
                            _vlcMediaPlayer.SetPause(true);
                        }
                    }
                    catch { }
                    if (_vlcMediaPlayer != null)
                        _vlcMediaPlayer.Playing -= onPlaying;
                };
                _vlcMediaPlayer.Playing += onPlaying;
                _vlcMediaPlayer.Play();
            }
            catch { }
        }

        // ── Sync methods called by MusicEditorForm ──

        public void SyncPlay()
        {
            if (_isDisposed || _vlcMediaPlayer == null) return;
            try
            {
                var state = _vlcMediaPlayer.State;
                // If player ended or is in an unusable state, reload the media first
                if (state == VLCState.Ended || state == VLCState.Stopped ||
                    state == VLCState.Error || state == VLCState.NothingSpecial)
                {
                    var vlcLib = GetSharedLibVLC();
                    var media = new Media(vlcLib, new Uri(Path.GetFullPath(_videoPath)));
                    _vlcMediaPlayer.Media = media;
                    media.Dispose();
                }

                if (!_vlcMediaPlayer.IsPlaying)
                {
                    _vlcMediaPlayer.Mute = true;
                    _vlcMediaPlayer.Play();
                }
            }
            catch { }
        }

        public void SyncPause()
        {
            if (_isDisposed || _vlcMediaPlayer == null) return;
            try
            {
                if (_vlcMediaPlayer.IsPlaying)
                    _vlcMediaPlayer.SetPause(true);
            }
            catch { }
        }

        public void SyncStop()
        {
            if (_isDisposed || _vlcMediaPlayer == null) return;
            try
            {
                if (_vlcMediaPlayer.IsPlaying)
                    _vlcMediaPlayer.Stop();

                // Reload media so the first frame is visible again
                LoadMediaAndShowFirstFrame();
            }
            catch { }
        }

        public void SyncSeek(int audioMs)
        {
            if (_isDisposed || _vlcMediaPlayer == null) return;
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

        private void InitializeComponent()
        {

        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _isDisposed = true;

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

                // DO NOT dispose _sharedVlcLib – it is shared across the entire application lifetime.
                // Disposing it would corrupt the VLC runtime for all other components (ArchiveControl, etc.).
            }

            base.Dispose(disposing);
        }
    }
}