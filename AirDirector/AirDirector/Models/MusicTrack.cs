using System;
using System.Collections.Generic;

namespace AirDirector.Models
{
    /// <summary>
    /// Traccia musicale nell'archivio
    /// </summary>
    public class MusicTrack
    {
        public int ID { get; set; }
        public string FilePath { get; set; }
        public string Artist { get; set; }
        public string Title { get; set; }
        public string Genre { get; set; }
        public List<string> Categories { get; set; }
        public int Year { get; set; }
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

        public MusicTrack()
        {
            ID = 0;
            FilePath = string.Empty;
            Artist = string.Empty;
            Title = string.Empty;
            Genre = string.Empty;
            Categories = new List<string>();
            Year = DateTime.Now.Year;
            Duration = 0;
            MarkerIN = 0;
            MarkerINTRO = 5000;  // Default 5 secondi
            MarkerMIX = 0;       // Calcolato automaticamente
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
        /// Verifica se il brano è valido per l'esecuzione in questo momento
        /// </summary>
        public bool IsValidForPlayback(DateTime checkTime)
        {
            return Validation.IsValid(checkTime);
        }

        /// <summary>
        /// Parser da formato "Artista - Titolo"
        /// </summary>
        public static void ParseFileName(string fileName, out string artist, out string title)
        {
            artist = string.Empty;
            title = string.Empty;

            if (string.IsNullOrEmpty(fileName))
                return;

            // Rimuovi estensione
            string nameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(fileName);

            // Cerca separatore " - "
            int dashIndex = nameWithoutExtension.IndexOf(" - ");

            if (dashIndex > 0)
            {
                artist = nameWithoutExtension.Substring(0, dashIndex).Trim();
                title = nameWithoutExtension.Substring(dashIndex + 3).Trim();
            }
            else
            {
                // Nessun separatore, metti tutto in Title
                title = nameWithoutExtension;
            }
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Artist) && !string.IsNullOrEmpty(Title))
                return $"{Artist} - {Title}";
            else if (!string.IsNullOrEmpty(Title))
                return Title;
            else
                return GetFileName();
        }
    }
}