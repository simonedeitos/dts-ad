using System;
using System.Collections.Generic;

namespace AirDirector.Models
{
    /// <summary>
    /// Clip audio/video nell'archivio
    /// </summary>
    public class Clip
    {
        public int ID { get; set; }
        public string FilePath { get; set; }
        public string Title { get; set; }
        public string Genre { get; set; }
        public List<string> Categories { get; set; }
        public int Duration { get; set; }  // Durata in secondi

        // Marker (in millisecondi)
        public int MarkerIN { get; set; }
        public int MarkerINTRO { get; set; }
        public int MarkerMIX { get; set; }

        // Calendario validità
        public ValidationCalendar Validation { get; set; }

        // Statistiche
        public DateTime LastPlayed { get; set; }
        public int PlayCount { get; set; }

        public Clip()
        {
            ID = 0;
            FilePath = string.Empty;
            Title = string.Empty;
            Genre = string.Empty;
            Categories = new List<string>();
            Duration = 0;
            MarkerIN = 0;
            MarkerINTRO = 0;
            MarkerMIX = 0;
            Validation = new ValidationCalendar();
            LastPlayed = DateTime.MinValue;
            PlayCount = 0;
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
            TimeSpan ts = TimeSpan.FromSeconds(Duration);
            return $"{(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}";
        }

        /// <summary>
        /// Verifica se il clip è valido per l'esecuzione in questo momento
        /// </summary>
        public bool IsValidForPlayback(DateTime checkTime)
        {
            return Validation.IsValid(checkTime);
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Title))
                return Title;
            else
                return GetFileName();
        }
    }
}