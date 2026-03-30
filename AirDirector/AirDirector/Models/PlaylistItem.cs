using System;

namespace AirDirector.Models
{
    /// <summary>
    /// Item nella playlist di riproduzione
    /// </summary>
    public class PlaylistItem
    {
        public string FilePath { get; set; }
        public string CategoryName { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime LastPlayed { get; set; }
        public DateTime ExpectedPlayTime { get; set; }

        // Marker (in millisecondi)
        public int MarkerIN { get; set; }
        public int MarkerINTRO { get; set; }
        public int MarkerMIX { get; set; }

        public PlaylistItem()
        {
            FilePath = string.Empty;
            CategoryName = string.Empty;
            Duration = TimeSpan.Zero;
            LastPlayed = DateTime.MinValue;
            ExpectedPlayTime = DateTime.MinValue;
            MarkerIN = 0;
            MarkerINTRO = 0;
            MarkerMIX = 0;
        }

        public PlaylistItem(string filePath, string categoryName, TimeSpan duration)
        {
            FilePath = filePath;
            CategoryName = categoryName;
            Duration = duration;
            LastPlayed = DateTime.MinValue;
            ExpectedPlayTime = DateTime.MinValue;
            MarkerIN = 0;
            MarkerINTRO = 0;
            MarkerMIX = 0;
        }

        /// <summary>
        /// Ottiene il nome file senza path
        /// </summary>
        public string GetFileName()
        {
            if (string.IsNullOrEmpty(FilePath))
                return string.Empty;

            return System.IO.Path.GetFileName(FilePath);
        }

        /// <summary>
        /// Ottiene la durata formattata MM:SS
        /// </summary>
        public string GetFormattedDuration()
        {
            return $"{(int)Duration.TotalMinutes:D2}:{Duration.Seconds:D2}";
        }

        /// <summary>
        /// Verifica se è un file audio valido
        /// </summary>
        public bool IsAudioFile()
        {
            if (string.IsNullOrEmpty(FilePath))
                return false;

            string ext = System.IO.Path.GetExtension(FilePath).ToLower();
            return ext == ".mp3" || ext == ".wav" || ext == ".wma" || ext == ".flac";
        }

        /// <summary>
        /// Verifica se è un file video valido
        /// </summary>
        public bool IsVideoFile()
        {
            if (string.IsNullOrEmpty(FilePath))
                return false;

            string ext = System.IO.Path.GetExtension(FilePath).ToLower();
            return ext == ".mp4" || ext == ".avi" || ext == ".mkv" || ext == ".mov";
        }

        public override string ToString()
        {
            return GetFileName();
        }
    }
}