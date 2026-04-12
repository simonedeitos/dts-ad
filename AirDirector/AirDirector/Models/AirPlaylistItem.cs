using System;

namespace AirDirector.Models
{
    /// <summary>
    /// Tipo di elemento di una playlist
    /// </summary>
    public enum AirPlaylistItemType
    {
        Track,      // Brano musicale dall'archivio
        Clip,       // Jingle/clip dall'archivio
        Category,   // Regola: categoria (risolto a runtime)
        Genre       // Regola: genere (risolto a runtime)
    }

    /// <summary>
    /// Elemento di una AirPlaylist
    /// </summary>
    public class AirPlaylistItem
    {
        public AirPlaylistItemType Type { get; set; }
        public string FilePath { get; set; }        // Per Track/Clip
        public string Artist { get; set; }          // Per Track
        public string Title { get; set; }           // Per Track/Clip
        public string CategoryName { get; set; }    // Per Category/Genre
        public string RuleSourceType { get; set; }  // "Music" o "Clips" (per regole)
        public string RuleCategoryName { get; set; } // nome categoria regola
        public string RuleGenreName { get; set; }    // nome genere regola
        public int DurationSeconds { get; set; }    // Durata in secondi
        public int MarkerIN { get; set; }           // ms
        public int MarkerMIX { get; set; }          // ms
        public bool YearFilterEnabled { get; set; } // Per Category/Genre
        public int YearFrom { get; set; }
        public int YearTo { get; set; }
        public TimeSpan ScheduledTime { get; set; } // Orario calcolato

        public AirPlaylistItem()
        {
            Type = AirPlaylistItemType.Track;
            FilePath = string.Empty;
            Artist = string.Empty;
            Title = string.Empty;
            CategoryName = string.Empty;
            DurationSeconds = 0;
            MarkerIN = 0;
            MarkerMIX = 0;
            YearFilterEnabled = false;
            YearFrom = 1900;
            YearTo = DateTime.Now.Year;
            ScheduledTime = TimeSpan.Zero;
        }

        /// <summary>
        /// Restituisce la durata effettiva in secondi.
        /// Per Track/Clip usa (MarkerMIX - MarkerIN) / 1000 se disponibile, altrimenti DurationSeconds.
        /// Per Category/Genre usa DurationSeconds (durata media).
        /// </summary>
        public int GetEffectiveDuration()
        {
            if (Type == AirPlaylistItemType.Track || Type == AirPlaylistItemType.Clip)
            {
                if (MarkerMIX > 0 && MarkerIN >= 0 && MarkerMIX > MarkerIN)
                    return (MarkerMIX - MarkerIN) / 1000;
            }
            return DurationSeconds;
        }

        /// <summary>
        /// Restituisce una stringa formattata per la visualizzazione nell'editor
        /// </summary>
        public string GetDisplayName()
        {
            switch (Type)
            {
                case AirPlaylistItemType.Track:
                    if (!string.IsNullOrEmpty(Artist) && !string.IsNullOrEmpty(Title))
                        return $"{Artist} - {Title}";
                    if (!string.IsNullOrEmpty(Title))
                        return Title;
                    return System.IO.Path.GetFileNameWithoutExtension(FilePath ?? "");
                case AirPlaylistItemType.Clip:
                    if (string.IsNullOrEmpty(FilePath))
                    {
                        // Regola Clips (senza FilePath diretto)
                        string source = RuleSourceType ?? "Clips";
                        var clipParts = new System.Collections.Generic.List<string>();
                        if (!string.IsNullOrEmpty(RuleGenreName)) clipParts.Add($"Genere: {RuleGenreName}");
                        if (!string.IsNullOrEmpty(RuleCategoryName)) clipParts.Add($"Categoria: {RuleCategoryName}");
                        if (YearFilterEnabled) clipParts.Add($"Anni: {YearFrom}-{YearTo}");
                        return $"{source} - {string.Join(", ", clipParts)}";
                    }
                    return !string.IsNullOrEmpty(Title) ? Title
                        : System.IO.Path.GetFileNameWithoutExtension(FilePath ?? "");
                case AirPlaylistItemType.Category:
                case AirPlaylistItemType.Genre:
                {
                    string source = RuleSourceType ?? "Music";
                    var parts = new System.Collections.Generic.List<string>();
                    if (!string.IsNullOrEmpty(RuleGenreName)) parts.Add($"Genere: {RuleGenreName}");
                    if (!string.IsNullOrEmpty(RuleCategoryName)) parts.Add($"Categoria: {RuleCategoryName}");
                    if (YearFilterEnabled) parts.Add($"Anni: {YearFrom}-{YearTo}");
                    if (parts.Count > 0)
                        return $"{source} - {string.Join(", ", parts)}";
                    return CategoryName ?? "";
                }
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Icona per il tipo di elemento
        /// </summary>
        public string GetTypeIcon()
        {
            switch (Type)
            {
                case AirPlaylistItemType.Track:    return "🎵";
                case AirPlaylistItemType.Clip:     return "🔔";
                case AirPlaylistItemType.Category: return "📁";
                case AirPlaylistItemType.Genre:    return "🎸";
                default:                           return "❓";
            }
        }
    }
}
