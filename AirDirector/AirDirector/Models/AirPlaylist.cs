using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace AirDirector.Models
{
    /// <summary>
    /// Playlist di AirDirector, salvata come file .airpls (JSON)
    /// </summary>
    public class AirPlaylist
    {
        public string Name { get; set; }
        public TimeSpan StartTime { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public List<AirPlaylistItem> Items { get; set; }

        public AirPlaylist()
        {
            Name = string.Empty;
            StartTime = TimeSpan.Zero;
            CreatedDate = DateTime.Now;
            ModifiedDate = DateTime.Now;
            Items = new List<AirPlaylistItem>();
        }

        /// <summary>
        /// Salva la playlist su file .airpls (JSON)
        /// </summary>
        public void Save(string filePath)
        {
            ModifiedDate = DateTime.Now;
            string dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);
        }

        /// <summary>
        /// Carica una playlist da file .airpls (JSON)
        /// </summary>
        public static AirPlaylist Load(string filePath)
        {
            string json = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
            var playlist = JsonConvert.DeserializeObject<AirPlaylist>(json);
            return playlist ?? new AirPlaylist();
        }

        /// <summary>
        /// Calcola la durata totale della playlist
        /// </summary>
        public TimeSpan GetTotalDuration()
        {
            int totalSeconds = Items?.Sum(i => i.GetEffectiveDuration()) ?? 0;
            return TimeSpan.FromSeconds(totalSeconds);
        }

        /// <summary>
        /// Ricalcola gli orari di ogni elemento a partire da StartTime
        /// </summary>
        public void RecalculateTimings()
        {
            if (Items == null) return;
            TimeSpan current = StartTime;
            foreach (var item in Items)
            {
                item.ScheduledTime = current;
                current = current.Add(TimeSpan.FromSeconds(item.GetEffectiveDuration()));
            }
        }

        /// <summary>
        /// Clona la playlist
        /// </summary>
        public AirPlaylist Clone()
        {
            string json = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<AirPlaylist>(json) ?? new AirPlaylist();
        }

        public override string ToString() => Name ?? string.Empty;
    }
}
