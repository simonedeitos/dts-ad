using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace AirDirector.Services.Database
{
    public class DbcManager
    {
        private static string DatabasePath
        {
            get
            {
                try
                {
                    return AirDirector.Controls.ConfigurationControl.GetDatabasePath();
                }
                catch
                {
                    return @"C:\AirDirector\Database";
                }
            }
        }

        private static Dictionary<string, DateTime> _fileLastWriteTimes = new Dictionary<string, DateTime>();
        private static readonly object _lock = new object();

        public event EventHandler<string> FileChanged;

        public static void Initialize()
        {
            try
            {
                string dbPath = DatabasePath;

                if (!Directory.Exists(dbPath))
                {
                    Directory.CreateDirectory(dbPath);
                    Console.WriteLine($"[DbcManager] Directory database creata:  {dbPath}");
                }

                CreateDefaultFiles();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DbcManager] ❌ Errore inizializzazione: {ex.Message}");
            }
        }

        private static void CreateDefaultFiles()
        {
            string configPath = Path.Combine(DatabasePath, "Config.dbc");
            if (!File.Exists(configPath))
            {
                var defaultConfig = new List<ConfigEntry>
                {
                    new ConfigEntry { Key = "Mode", Value = "Radio" },
                    new ConfigEntry { Key = "DatabasePath", Value = DatabasePath },
                    new ConfigEntry { Key = "BackupPath", Value = @"C:\AirDirector\Backup" },
                    new ConfigEntry { Key = "Language", Value = "Italiano" },
                    new ConfigEntry { Key = "OutputDevice", Value = "Default" },
                    new ConfigEntry { Key = "MixDuration", Value = "5000" },
                    new ConfigEntry { Key = "HourlySeparation", Value = "3" },
                    new ConfigEntry { Key = "NDIOutputName", Value = "AirDirector_Output" },
                    new ConfigEntry { Key = "LicenseActivated", Value = "0" }
                };
                SaveToCsvInternal(configPath, defaultConfig);
                Console.WriteLine("[DbcManager] Config.dbc creato");
            }

            string genresPath = Path.Combine(DatabasePath, "Genres.dbc");
            if (!File.Exists(genresPath))
            {
                var defaultGenres = new List<GenreEntry>
                {
                    new GenreEntry { ID = 1, GenreName = "Pop", Color = "#FF6B6B" },
                    new GenreEntry { ID = 2, GenreName = "Rock", Color = "#4ECDC4" },
                    new GenreEntry { ID = 3, GenreName = "Chill", Color = "#95E1D3" },
                    new GenreEntry { ID = 4, GenreName = "Dance", Color = "#FEE440" },
                    new GenreEntry { ID = 5, GenreName = "Jazz", Color = "#9B59B6" }
                };
                SaveToCsvInternal(genresPath, defaultGenres);
                Console.WriteLine("[DbcManager] Genres.dbc creato");
            }

            string categoriesPath = Path.Combine(DatabasePath, "Categories.dbc");
            if (!File.Exists(categoriesPath))
            {
                var defaultCategories = new List<CategoryEntry>
                {
                    new CategoryEntry { ID = 1, CategoryName = "Rotation", Color = "#2196F3", IgnoreHourlySeparation = 0 },
                    new CategoryEntry { ID = 2, CategoryName = "Jingle", Color = "#FF9800", IgnoreHourlySeparation = 1 },
                    new CategoryEntry { ID = 3, CategoryName = "Promo", Color = "#9C27B0", IgnoreHourlySeparation = 1 }
                };
                SaveToCsvInternal(categoriesPath, defaultCategories);
                Console.WriteLine("[DbcManager] Categories.dbc creato");
            }

            string musicPath = Path.Combine(DatabasePath, "Music.dbc");
            if (!File.Exists(musicPath))
            {
                SaveToCsvInternal(musicPath, new List<MusicEntry>());
                Console.WriteLine("[DbcManager] Music.dbc creato");
            }

            string clipsPath = Path.Combine(DatabasePath, "Clips.dbc");
            if (!File.Exists(clipsPath))
            {
                SaveToCsvInternal(clipsPath, new List<ClipEntry>());
                Console.WriteLine("[DbcManager] Clips.dbc creato");
            }

            string miniPLSPath = Path.Combine(DatabasePath, "MiniPLS.dbc");
            if (!File.Exists(miniPLSPath))
            {
                SaveToCsvInternal(miniPLSPath, new List<MiniPLSEntry>());
                Console.WriteLine("[DbcManager] MiniPLS.dbc creato");
            }

            string schedulesPath = Path.Combine(DatabasePath, "Schedules.dbc");
            if (!File.Exists(schedulesPath))
            {
                SaveToCsvInternal(schedulesPath, new List<ScheduleEntry>());
                Console.WriteLine("[DbcManager] Schedules.dbc creato");
            }

            string clocksPath = Path.Combine(DatabasePath, "Clocks.dbc");
            if (!File.Exists(clocksPath))
            {
                SaveToCsvInternal(clocksPath, new List<ClockEntry>());
                Console.WriteLine("[DbcManager] Clocks.dbc creato");
            }

            string encodersPath = Path.Combine(DatabasePath, "Encoders.dbc");
            if (!File.Exists(encodersPath))
            {
                SaveToCsvInternal(encodersPath, new List<EncoderEntry>());
                Console.WriteLine("[DbcManager] Encoders.dbc creato");
            }

            string recordersPath = Path.Combine(DatabasePath, "Recorders.dbc");
            if (!File.Exists(recordersPath))
            {
                SaveToCsvInternal(recordersPath, new List<RecorderEntry>());
                Console.WriteLine("[DbcManager] Recorders.dbc creato");
            }
        }

        public static List<T> LoadFromCsv<T>(string fileName)
        {
            string fullPath = Path.Combine(DatabasePath, fileName);

            lock (_lock)
            {
                try
                {
                    if (!File.Exists(fullPath))
                    {
                        Console.WriteLine($"[DbcManager] ⚠️ File non trovato: {fileName}");
                        return new List<T>();
                    }

                    _fileLastWriteTimes[fileName] = File.GetLastWriteTime(fullPath);

                    using (var reader = new StreamReader(fullPath, Encoding.UTF8))
                    using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        HasHeaderRecord = true,
                        Delimiter = ",",
                        MissingFieldFound = null,
                        BadDataFound = null
                    }))
                    {
                        var records = csv.GetRecords<T>().ToList();
                        Console.WriteLine($"[DbcManager] ✅ Caricati {records.Count} record da {fileName}");
                        return records;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DbcManager] ❌ Errore caricamento {fileName}: {ex.Message}");
                    Console.WriteLine($"[DbcManager]    StackTrace: {ex.StackTrace}");
                    return new List<T>();
                }
            }
        }

        public static bool SaveToCsv<T>(string fileName, List<T> data)
        {
            string fullPath = Path.Combine(DatabasePath, fileName);

            lock (_lock)
            {
                try
                {
                    if (File.Exists(fullPath))
                    {
                        string backupPath = fullPath + ".bak";
                        File.Copy(fullPath, backupPath, true);
                    }

                    using (var writer = new StreamWriter(fullPath, false, Encoding.UTF8))
                    using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        HasHeaderRecord = true,
                        Delimiter = ",",
                        ShouldQuote = args => true
                    }))
                    {
                        csv.WriteRecords(data);
                    }

                    _fileLastWriteTimes[fileName] = File.GetLastWriteTime(fullPath);

                    Console.WriteLine($"[DbcManager] ✅ Salvati {data.Count} record in {fileName}");
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DbcManager] ❌ Errore salvataggio {fileName}:  {ex.Message}");
                    Console.WriteLine($"[DbcManager]    StackTrace: {ex.StackTrace}");

                    string backupPath = fullPath + ".bak";
                    if (File.Exists(backupPath))
                    {
                        File.Copy(backupPath, fullPath, true);
                        Console.WriteLine("[DbcManager] ⚠️ Backup ripristinato");
                    }

                    return false;
                }
            }
        }

        private static bool SaveToCsvInternal<T>(string fullPath, List<T> data)
        {
            try
            {
                using (var writer = new StreamWriter(fullPath, false, Encoding.UTF8))
                using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    Delimiter = ","
                }))
                {
                    csv.WriteRecords(data);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DbcManager] ❌ Errore salvataggio interno: {ex.Message}");
                return false;
            }
        }

        public static bool Insert<T>(string fileName, T item) where T : IDbcEntry
        {
            var data = LoadFromCsv<T>(fileName);

            int newId = data.Count > 0 ? data.Max(x => x.ID) + 1 : 1;
            item.ID = newId;

            data.Add(item);
            return SaveToCsv(fileName, data);
        }

        public static bool Update<T>(string fileName, T entry) where T : class
        {
            lock (_lock)
            {
                try
                {
                    Console.WriteLine($"\n[DbcManager] ========== UPDATE ==========");
                    Console.WriteLine($"[DbcManager] File: {fileName}");
                    Console.WriteLine($"[DbcManager] Tipo:  {typeof(T).Name}");

                    var allEntries = LoadFromCsv<T>(fileName);
                    Console.WriteLine($"[DbcManager] Entry totali caricati: {allEntries.Count}");

                    var idProperty = typeof(T).GetProperty("ID");
                    if (idProperty == null)
                    {
                        Console.WriteLine($"[DbcManager] ❌ Tipo {typeof(T).Name} non ha proprietà ID");
                        return false;
                    }

                    int entryId = (int)idProperty.GetValue(entry);
                    Console.WriteLine($"[DbcManager] ID da aggiornare: {entryId}");

                    Console.WriteLine($"[DbcManager] === ID PRESENTI NEL FILE {fileName} ===");
                    foreach (var item in allEntries)
                    {
                        int currentId = (int)idProperty.GetValue(item);

                        if (typeof(T) == typeof(ClipEntry))
                        {
                            var clipItem = item as ClipEntry;
                            Console.WriteLine($"[DbcManager]   - ID: {currentId}, Title: {clipItem?.Title}");
                        }
                        else if (typeof(T) == typeof(MusicEntry))
                        {
                            var musicItem = item as MusicEntry;
                            Console.WriteLine($"[DbcManager]   - ID: {currentId}, Title: {musicItem?.Title}, Artist: {musicItem?.Artist}");
                        }
                        else
                        {
                            Console.WriteLine($"[DbcManager]   - ID:  {currentId}");
                        }
                    }
                    Console.WriteLine($"[DbcManager] ================================================");

                    bool found = false;
                    for (int i = 0; i < allEntries.Count; i++)
                    {
                        int currentId = (int)idProperty.GetValue(allEntries[i]);
                        if (currentId == entryId)
                        {
                            Console.WriteLine($"[DbcManager] ✅ Entry ID={entryId} trovato alla posizione {i}");

                            var properties = typeof(T).GetProperties();
                            Console.WriteLine($"[DbcManager] === CAMPI MODIFICATI ===");
                            foreach (var prop in properties)
                            {
                                var oldValue = prop.GetValue(allEntries[i]);
                                var newValue = prop.GetValue(entry);
                                if (oldValue?.ToString() != newValue?.ToString())
                                {
                                    Console.WriteLine($"[DbcManager]   {prop.Name}: '{oldValue}' → '{newValue}'");
                                }
                            }

                            allEntries[i] = entry;
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        Console.WriteLine($"[DbcManager] ❌ Entry ID={entryId} NON TROVATO nel database!");
                        Console.WriteLine($"[DbcManager] ❌ AGGIORNAMENTO FALLITO - ID INESISTENTE");
                        Console.WriteLine($"[DbcManager] ====================================\n");
                        return false;
                    }

                    bool success = SaveToCsv(fileName, allEntries);

                    Console.WriteLine($"[DbcManager] Risultato salvataggio CSV: {success}");
                    Console.WriteLine($"[DbcManager] ====================================\n");

                    return success;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DbcManager] ❌ ECCEZIONE:  {ex.Message}");
                    Console.WriteLine($"[DbcManager] StackTrace: {ex.StackTrace}");
                    Console.WriteLine($"[DbcManager] ====================================\n");
                    return false;
                }
            }
        }

        public static bool Delete<T>(string fileName, int id) where T : IDbcEntry
        {
            var data = LoadFromCsv<T>(fileName);
            var item = data.FirstOrDefault(x => x.ID == id);

            if (item != null)
            {
                data.Remove(item);
                Console.WriteLine($"[DbcManager] 🗑️ Eliminato ID={id} da {fileName}");
                return SaveToCsv(fileName, data);
            }

            Console.WriteLine($"[DbcManager] ⚠️ ID={id} non trovato in {fileName}");
            return false;
        }

        public static T GetById<T>(string fileName, int id) where T : IDbcEntry
        {
            var data = LoadFromCsv<T>(fileName);
            var result = data.FirstOrDefault(x => x.ID == id);

            if (result != null)
            {
                Console.WriteLine($"[DbcManager] ✅ GetById:  Trovato ID={id} in {fileName}");
            }
            else
            {
                Console.WriteLine($"[DbcManager] ❌ GetById:  ID={id} NON trovato in {fileName}");
            }

            return result;
        }

        public static bool HasFileChanged(string fileName)
        {
            string fullPath = Path.Combine(DatabasePath, fileName);

            if (!File.Exists(fullPath))
                return false;

            if (!_fileLastWriteTimes.ContainsKey(fileName))
                return true;

            DateTime currentWriteTime = File.GetLastWriteTime(fullPath);
            DateTime cachedWriteTime = _fileLastWriteTimes[fileName];

            return currentWriteTime > cachedWriteTime;
        }

        public static string GetConfigValue(string key, string defaultValue = "")
        {
            var config = LoadFromCsv<ConfigEntry>("Config.dbc");
            var entry = config.FirstOrDefault(c => c.Key == key);
            return entry != null ? entry.Value : defaultValue;
        }

        public static bool SetConfigValue(string key, string value)
        {
            var config = LoadFromCsv<ConfigEntry>("Config.dbc");
            var entry = config.FirstOrDefault(c => c.Key == key);

            if (entry != null)
            {
                entry.Value = value;
            }
            else
            {
                config.Add(new ConfigEntry { Key = key, Value = value });
            }

            return SaveToCsv("Config.dbc", config);
        }

        public static string GetFilePath(string fileName)
        {
            return Path.Combine(DatabasePath, fileName);
        }

        public static string GetDatabasePath()
        {
            return DatabasePath;
        }

        public static void RecreateClipsDbc()
        {
            string clipsPath = Path.Combine(GetDatabasePath(), "Clips.dbc");

            Console.WriteLine("[DbcManager] 🔧 Ricreazione Clips.dbc in corso...");

            if (File.Exists(clipsPath))
            {
                string backupPath = clipsPath + ".old_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                File.Copy(clipsPath, backupPath, true);
                Console.WriteLine($"[DbcManager] 💾 Backup creato: {backupPath}");
            }

            var oldClips = File.Exists(clipsPath) ? LoadFromCsv<ClipEntry>("Clips.dbc") : new List<ClipEntry>();

            SaveToCsv("Clips.dbc", oldClips);

            Console.WriteLine($"[DbcManager] ✅ Clips.dbc ricreato con {oldClips.Count} clips");
        }
    }

    // ═══════════════════════════════════════════════════════════
    // ENUMS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Tipo di sorgente video associata a un brano/clip o per output NDI
    /// </summary>
    public enum VideoSourceType
    {
        /// <summary>Solo audio (nessun video)</summary>
        None = 0,

        /// <summary>File video statico (MP4, MOV, etc.)</summary>
        StaticVideo = 1,

        /// <summary>Sorgente NDI live (telecamera, OBS, vMix)</summary>
        NDISource = 2,

        /// <summary>Video tampone casuale dalla cartella config</summary>
        BufferVideo = 3,

        /// <summary>Immagine statica (logo, slate)</summary>
        StaticImage = 4,

        /// <summary>Colore solido (nero, bars, ecc.)</summary>
        SolidColor = 5
    }

    /// <summary>
    /// Stato corrente del VideoNDIManager
    /// </summary>
    public enum VideoNDIState
    {
        /// <summary>Fermo</summary>
        Stopped = 0,

        /// <summary>In riproduzione</summary>
        Playing = 1,

        /// <summary>In pausa</summary>
        Paused = 2,

        /// <summary>Errore</summary>
        Error = 3
    }

    // ═══════════════════════════════════════════════════════════
    // INTERFACES & CLASSES
    // ═══════════════════════════════════════════════════════════

    public interface IDbcEntry
    {
        int ID { get; set; }
    }

    public class ConfigEntry
    {
        public string Key { get; set; } = "";
        public string Value { get; set; } = "";
    }

    public class GenreEntry : IDbcEntry
    {
        public int ID { get; set; }
        public string GenreName { get; set; } = "";
        public string Color { get; set; } = "";
    }

    public class CategoryEntry : IDbcEntry
    {
        public int ID { get; set; }
        public string CategoryName { get; set; } = "";
        public string Color { get; set; } = "";
        public int IgnoreHourlySeparation { get; set; }
    }

    public class MusicEntry : IDbcEntry
    {
        public int ID { get; set; }
        public string FilePath { get; set; } = "";
        public string Artist { get; set; } = "";
        public string Title { get; set; } = "";
        public string Album { get; set; } = "";
        public string Genre { get; set; } = "";
        public string Categories { get; set; } = "";
        public int Year { get; set; }
        public int Duration { get; set; }
        public long FileSize { get; set; }
        public string Format { get; set; } = "";
        public int Bitrate { get; set; }
        public int SampleRate { get; set; }
        public int Channels { get; set; }
        public int MarkerIN { get; set; }
        public int MarkerINTRO { get; set; }
        public int MarkerMIX { get; set; }
        public int MarkerOUT { get; set; }
        public string ValidMonths { get; set; } = "";
        public string ValidDays { get; set; } = "";
        public string ValidHours { get; set; } = "";
        public string ValidFrom { get; set; } = "";
        public string ValidTo { get; set; } = "";
        public string LastPlayed { get; set; } = "";
        public int PlayCount { get; set; }
        public string AddedDate { get; set; } = "";

        // Campi Video
        public string VideoFilePath { get; set; } = "";
        public string NDISourceName { get; set; } = "";
        public VideoSourceType VideoSource { get; set; } = VideoSourceType.None;
    }

    public class ClipEntry : IDbcEntry
    {
        public int ID { get; set; }
        public string FilePath { get; set; } = "";
        public string Title { get; set; } = "";
        public string Genre { get; set; } = "";
        public string Categories { get; set; } = "";
        public int Duration { get; set; }
        public int MarkerIN { get; set; }
        public int MarkerINTRO { get; set; }
        public int MarkerMIX { get; set; }
        public int MarkerOUT { get; set; }
        public string ValidMonths { get; set; } = "";
        public string ValidDays { get; set; } = "";
        public string ValidHours { get; set; } = "";
        public string ValidFrom { get; set; } = "";
        public string ValidTo { get; set; } = "";
        public string AddedDate { get; set; } = "";
        public string LastPlayed { get; set; } = "";
        public int PlayCount { get; set; }

        // Campi Video
        public string VideoFilePath { get; set; } = "";
        public string NDISourceName { get; set; } = "";
        public VideoSourceType VideoSource { get; set; } = VideoSourceType.None;
    }

    public class MiniPLSEntry : IDbcEntry
    {
        public int ID { get; set; }
        public string PlaylistName { get; set; } = "";
        public string Description { get; set; } = "";
        public string Items { get; set; } = "";
    }

    public class ScheduleEntry : IDbcEntry
    {
        public int ID { get; set; }
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public int Monday { get; set; }
        public int Tuesday { get; set; }
        public int Wednesday { get; set; }
        public int Thursday { get; set; }
        public int Friday { get; set; }
        public int Saturday { get; set; }
        public int Sunday { get; set; }
        public string Times { get; set; } = "";
        public string ClockName { get; set; } = "";
        public string AudioFilePath { get; set; } = "";
        public int MiniPLSID { get; set; }
        public int IsEnabled { get; set; } = 1;
        public string VideoBufferPath { get; set; } = "";
    }

    public class ClockEntry : IDbcEntry
    {
        public int ID { get; set; }
        public string ClockName { get; set; } = "";
        public int IsDefault { get; set; }
        public string Items { get; set; } = "";
    }

    

    public class RecorderEntry : IDbcEntry
    {
        public int ID { get; set; }
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string AudioSourceDevice { get; set; } = "";
        public string OutputPath { get; set; } = "";
        public string OutputFolder { get; set; } = "";
        public string Format { get; set; } = "";
        public int Bitrate { get; set; }
        public int SampleRate { get; set; }
        public int Monday { get; set; }
        public int Tuesday { get; set; }
        public int Wednesday { get; set; }
        public int Thursday { get; set; }
        public int Friday { get; set; }
        public int Saturday { get; set; }
        public int Sunday { get; set; }
        public string StartTime { get; set; } = "";
        public string EndTime { get; set; } = "";
        public int RetentionDays { get; set; }
        public int AutoStart { get; set; }
        public int IsActive { get; set; }
        public int AutoDeleteOldFiles { get; set; }
        public int DeleteOldFiles { get; set; }
    }
}