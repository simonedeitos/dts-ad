using AirDirector.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AirDirector.Services.Database
{
    public static class ReportManager
    {
        private static readonly object _lock = new object();

        /// <summary>
        /// Scrive una riga nel Report.dbc
        /// </summary>
        public static void LogTrack(string type, string artist, string title, DateTime startTime, DateTime endTime, TimeSpan fileDuration)
        {
            lock (_lock)
            {
                try
                {
                    string dbPath = ConfigurationControl.GetDatabasePath();
                    string reportPath = Path.Combine(dbPath, "Report.dbc");

                    // ✅ CREA IL FILE SE NON ESISTE (con header)
                    if (!File.Exists(reportPath))
                    {
                        string header = "Date;StartTime;EndTime;Type;Artist;Title;PlayDuration;FileDuration";
                        File.WriteAllText(reportPath, header + Environment.NewLine, Encoding.UTF8);
                    }

                    // ✅ CALCOLA PlayDuration (differenza tra start e end)
                    TimeSpan playDuration = endTime - startTime;

                    // ✅ PREPARA LA RIGA
                    string date = startTime.ToString("yyyy-MM-dd");
                    string start = startTime.ToString("HH:mm:ss");
                    string end = endTime.ToString("HH:mm:ss");
                    string playDur = playDuration.ToString(@"hh\:mm\:ss");
                    string fileDur = fileDuration.ToString(@"hh\:mm\:ss");

                    // ✅ ESCAPE CARATTERI SPECIALI (punto e virgola, virgolette)
                    artist = EscapeCsvField(artist);
                    title = EscapeCsvField(title);

                    string line = $"{date};{start};{end};{type};{artist};{title};{playDur};{fileDur}";

                    // ✅ SCRIVI IN APPEND
                    File.AppendAllText(reportPath, line + Environment.NewLine, Encoding.UTF8);

                    Console.WriteLine($"[ReportManager] ✅ Scritto: {date} {start}-{end} | {artist} - {title} | Play:{playDur} File:{fileDur}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ReportManager] ❌ Errore scrittura: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Carica report filtrato per data
        /// </summary>
        public static List<ReportEntry> LoadReport(DateTime from, DateTime to)
        {
            lock (_lock)
            {
                try
                {
                    string dbPath = ConfigurationControl.GetDatabasePath();
                    string reportPath = Path.Combine(dbPath, "Report.dbc");

                    if (!File.Exists(reportPath))
                        return new List<ReportEntry>();

                    var lines = File.ReadAllLines(reportPath, Encoding.UTF8);
                    var entries = new List<ReportEntry>();

                    for (int i = 1; i < lines.Length; i++) // Skip header
                    {
                        var parts = lines[i].Split(';');
                        if (parts.Length >= 8)
                        {
                            if (DateTime.TryParse(parts[0], out DateTime date))
                            {
                                if (date.Date >= from.Date && date.Date <= to.Date)
                                {
                                    entries.Add(new ReportEntry
                                    {
                                        Date = date,
                                        StartTime = parts[1],
                                        EndTime = parts[2],
                                        Type = parts[3],
                                        Artist = UnescapeCsvField(parts[4]),
                                        Title = UnescapeCsvField(parts[5]),
                                        PlayDuration = parts[6],
                                        FileDuration = parts[7]
                                    });
                                }
                            }
                        }
                    }

                    return entries;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ReportManager] Errore lettura: {ex.Message}");
                    return new List<ReportEntry>();
                }
            }
        }

        /// <summary>
        /// Carica storico passaggi per un brano specifico (artista + titolo)
        /// </summary>
        public static List<ReportEntry> LoadTrackHistory(string artist, string title)
        {
            lock (_lock)
            {
                try
                {
                    string dbPath = ConfigurationControl.GetDatabasePath();
                    string reportPath = Path.Combine(dbPath, "Report.dbc");

                    if (!File.Exists(reportPath))
                        return new List<ReportEntry>();

                    var lines = File.ReadAllLines(reportPath, Encoding.UTF8);
                    var entries = new List<ReportEntry>();

                    for (int i = 1; i < lines.Length; i++) // Skip header
                    {
                        var parts = lines[i].Split(';');
                        if (parts.Length >= 8)
                        {
                            string entryArtist = UnescapeCsvField(parts[4]);
                            string entryTitle = UnescapeCsvField(parts[5]);

                            if (string.Equals(entryArtist, artist, StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(entryTitle, title, StringComparison.OrdinalIgnoreCase))
                            {
                                if (DateTime.TryParse(parts[0], out DateTime date))
                                {
                                    entries.Add(new ReportEntry
                                    {
                                        Date = date,
                                        StartTime = parts[1],
                                        EndTime = parts[2],
                                        Type = parts[3],
                                        Artist = entryArtist,
                                        Title = entryTitle,
                                        PlayDuration = parts[6],
                                        FileDuration = parts[7]
                                    });
                                }
                            }
                        }
                    }

                    return entries;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ReportManager] Errore lettura storico: {ex.Message}");
                    return new List<ReportEntry>();
                }
            }
        }

        /// <summary>
        /// Escape campo CSV (gestisce ; e ")
        /// </summary>
        private static string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "";

            if (field.Contains(";") || field.Contains("\"") || field.Contains("\n"))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }

            return field;
        }

        /// <summary>
        /// Unescape campo CSV
        /// </summary>
        private static string UnescapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "";

            if (field.StartsWith("\"") && field.EndsWith("\""))
            {
                return field.Substring(1, field.Length - 2).Replace("\"\"", "\"");
            }

            return field;
        }
    }

    /// <summary>
    /// Entry del report
    /// </summary>
    public class ReportEntry
    {
        public DateTime Date { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string Type { get; set; }
        public string Artist { get; set; }
        public string Title { get; set; }
        public string PlayDuration { get; set; }  // ✅ DURATA EFFETTIVA DI RIPRODUZIONE
        public string FileDuration { get; set; }  // ✅ DURATA ORIGINALE DEL FILE
    }
}