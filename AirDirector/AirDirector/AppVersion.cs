using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace AirDirector
{
    /// <summary>
    /// Gestione centralizzata della versione dell'applicazione con auto-incremento
    /// </summary>
    public static class AppVersion
    {
        private static string? _cachedVersion;
        private static readonly object _lock = new object();
        private static readonly string VersionFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "version.json");

        /// <summary>
        /// Ottiene la versione corrente in formato Major.Minor.Patch
        /// </summary>
        public static string Current
        {
            get
            {
                if (_cachedVersion == null)
                {
                    lock (_lock)
                    {
                        if (_cachedVersion == null)
                        {
                            _cachedVersion = LoadAndIncrementVersion();
                        }
                    }
                }
                return _cachedVersion;
            }
        }

        /// <summary>
        /// Ottiene la versione formattata per display (es: "v1.25.348")
        /// </summary>
        public static string DisplayVersion => $"v{Current}";

        /// <summary>
        /// Carica la versione dal file JSON e incrementa Minor/Patch
        /// </summary>
        private static string LoadAndIncrementVersion()
        {
            try
            {
                if (!File.Exists(VersionFilePath))
                {
                    // Versione di fallback se il file non esiste
                    return "1.0.0";
                }

                string json = File.ReadAllText(VersionFilePath);
                JObject versionData = JObject.Parse(json);

                int major = versionData["major"]?.Value<int>() ?? 1;
                int minor = versionData["minor"]?.Value<int>() ?? 0;
                int patch = versionData["patch"]?.Value<int>() ?? 0;

                // Incrementa Patch ad ogni build
                patch++;

                // Ogni 100 build, incrementa Minor e resetta Patch
                if (patch >= 100)
                {
                    minor++;
                    patch = 0;
                }

                // Salva i nuovi valori
                versionData["minor"] = minor;
                versionData["patch"] = patch;

                try
                {
                    File.WriteAllText(VersionFilePath, versionData.ToString());
                }
                catch
                {
                    // Se non riesce a scrivere (es. permessi), continua comunque
                }

                return $"{major}.{minor}.{patch}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AppVersion] Errore caricamento versione: {ex.Message}");
                return "1.0.0";
            }
        }

        /// <summary>
        /// Legge solo il valore Major dal file (per permettere modifiche manuali)
        /// </summary>
        public static int GetMajorVersion()
        {
            try
            {
                if (File.Exists(VersionFilePath))
                {
                    string json = File.ReadAllText(VersionFilePath);
                    JObject versionData = JObject.Parse(json);
                    return versionData["major"]?.Value<int>() ?? 1;
                }
            }
            catch { }
            return 1;
        }

        /// <summary>
        /// Permette di modificare manualmente il Major version.
        /// USARE SOLO PER AGGIORNAMENTI MANUALI IMPORTANTI.
        /// </summary>
        public static void SetMajorVersion(int newMajor)
        {
            try
            {
                if (File.Exists(VersionFilePath))
                {
                    string json = File.ReadAllText(VersionFilePath);
                    JObject versionData = JObject.Parse(json);
                    versionData["major"] = newMajor;
                    File.WriteAllText(VersionFilePath, versionData.ToString());
                    _cachedVersion = null; // Reset cache
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AppVersion] Errore impostazione major version: {ex.Message}");
            }
        }
    }
}
