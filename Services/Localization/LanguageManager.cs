using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AirDirector.Services.Localization
{
    public class LanguageManager
    {
        private static LanguageManager _instance;
        private static readonly object _lock = new object();

        private Dictionary<string, string> _translations;
        private Dictionary<string, string> _missingKeys;
        private string _currentLanguage;
        private string _languagesPath;

        public static event EventHandler LanguageChanged;

        private LanguageManager()
        {
            _translations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _missingKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _languagesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Languages");

            if (!Directory.Exists(_languagesPath))
            {
                Directory.CreateDirectory(_languagesPath);
            }

            // ✅ LEGGI LINGUA SALVATA
            string savedLanguage = GetSavedLanguage();
            _currentLanguage = !string.IsNullOrEmpty(savedLanguage) ? savedLanguage : "Italiano";

            Console.WriteLine($"[LanguageManager] 🌍 Lingua rilevata: {_currentLanguage}");

            LoadLanguage(_currentLanguage);
        }

        public static LanguageManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new LanguageManager();
                        }
                    }
                }
                return _instance;
            }
        }

        public static void Initialize()
        {
            _ = Instance;
        }

        /// <summary>
        /// ✅ LEGGE LA LINGUA DAL REGISTRY (più affidabile del database)
        /// </summary>
        private string GetSavedLanguage()
        {
            try
            {
                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AirDirector"))
                {
                    if (key != null)
                    {
                        string language = key.GetValue("Language", "Italiano")?.ToString();

                        if (!string.IsNullOrEmpty(language))
                        {
                            string filePath = Path.Combine(_languagesPath, $"{language}.ini");
                            if (File.Exists(filePath))
                            {
                                Console.WriteLine($"[LanguageManager] ✅ Lingua dal Registry: {language}");
                                return language;
                            }
                            else
                            {
                                Console.WriteLine($"[LanguageManager] ⚠️ File {language}.ini non trovato");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LanguageManager] ❌ Errore lettura Registry: {ex.Message}");
            }

            return null;
        }

        public void LoadLanguage(string languageName)
        {
            try
            {
                string filePath = Path.Combine(_languagesPath, $"{languageName}.ini");

                Console.WriteLine($"[LanguageManager] 🔍 Caricamento:  {filePath}");
                Console.WriteLine($"[LanguageManager] 🔍 File esiste: {File.Exists(filePath)}");

                if (!File.Exists(filePath))
                {
                    if (languageName != "Italiano")
                    {
                        string italianPath = Path.Combine(_languagesPath, "Italiano.ini");
                        if (File.Exists(italianPath))
                        {
                            Console.WriteLine($"[LanguageManager] ⚠️ Fallback su Italiano");
                            filePath = italianPath;
                            languageName = "Italiano";
                        }
                        else
                        {
                            Console.WriteLine($"[LanguageManager] ❌ Nessun file lingua trovato!");
                            return;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[LanguageManager] ❌ File Italiano.ini non trovato!");
                        return;
                    }
                }

                _translations.Clear();
                _currentLanguage = languageName;

                string currentSection = "";
                int keysLoaded = 0;

                foreach (string line in File.ReadAllLines(filePath, Encoding.UTF8))
                {
                    string trimmedLine = line.Trim();

                    if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith(";") || trimmedLine.StartsWith("#"))
                        continue;

                    if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                    {
                        currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2).Trim();
                        continue;
                    }

                    int equalsIndex = trimmedLine.IndexOf('=');
                    if (equalsIndex > 0)
                    {
                        string key = trimmedLine.Substring(0, equalsIndex).Trim();
                        string value = trimmedLine.Substring(equalsIndex + 1).Trim();

                        string fullKey = string.IsNullOrEmpty(currentSection) ? key : $"{currentSection}.{key}";

                        _translations[fullKey] = value;
                        keysLoaded++;
                    }
                }

                Console.WriteLine($"[LanguageManager] ✅ Caricate {keysLoaded} chiavi da {languageName}.ini");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LanguageManager] ❌ Errore:  {ex.Message}");
            }
        }

        public static string GetString(string key, string defaultValue = null)
        {
            if (Instance._translations.TryGetValue(key, out string value))
            {
                return value.Replace("\\n", Environment.NewLine);
            }

            string fallback = defaultValue ?? key;

            if (!Instance._missingKeys.ContainsKey(key))
            {
                Instance._missingKeys[key] = fallback;
            }

            return fallback.Replace("\\n", Environment.NewLine);
        }

        public static void SaveMissingKeysToFile()
        {
            if (Instance._missingKeys.Count == 0)
                return;

            try
            {
                string italianPath = Path.Combine(Instance._languagesPath, "Italiano.ini");

                var existingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var existingContent = new List<string>();

                if (File.Exists(italianPath))
                {
                    foreach (string line in File.ReadAllLines(italianPath, Encoding.UTF8))
                    {
                        existingContent.Add(line);

                        string trimmed = line.Trim();
                        if (!string.IsNullOrWhiteSpace(trimmed) && !trimmed.StartsWith(";") && !trimmed.StartsWith("#") && !trimmed.StartsWith("["))
                        {
                            int equalsIndex = trimmed.IndexOf('=');
                            if (equalsIndex > 0)
                            {
                                string key = trimmed.Substring(0, equalsIndex).Trim();
                                existingKeys.Add(key);
                            }
                        }
                    }
                }
                else
                {
                    existingContent.Add("; ============================================");
                    existingContent.Add("; AIRDIRECTOR - LANGUAGE FILE (ITALIANO)");
                    existingContent.Add($"; Auto-generato: {DateTime.Now: yyyy-MM-dd HH: mm:ss}");
                    existingContent.Add("; ============================================");
                    existingContent.Add("");
                    existingContent.Add("[General]");
                    existingContent.Add("AppName=AirDirector");
                    existingContent.Add("Version=1.0.0");
                    existingContent.Add("");
                }

                var sectionedKeys = new Dictionary<string, List<KeyValuePair<string, string>>>();

                foreach (var kvp in Instance._missingKeys.OrderBy(k => k.Key))
                {
                    string fullKey = kvp.Key;
                    int lastDotIndex = fullKey.LastIndexOf('.');

                    string section = lastDotIndex > 0 ? fullKey.Substring(0, lastDotIndex) : "General";
                    string simpleKey = lastDotIndex > 0 ? fullKey.Substring(lastDotIndex + 1) : fullKey;

                    if (existingKeys.Contains(simpleKey))
                        continue;

                    if (!sectionedKeys.ContainsKey(section))
                    {
                        sectionedKeys[section] = new List<KeyValuePair<string, string>>();
                    }

                    sectionedKeys[section].Add(new KeyValuePair<string, string>(simpleKey, kvp.Value));
                }

                if (sectionedKeys.Count > 0)
                {
                    existingContent.Add("");
                    existingContent.Add("; ============================================");
                    existingContent.Add($"; AUTO-GENERATO: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    existingContent.Add("; ============================================");
                    existingContent.Add("");

                    foreach (var section in sectionedKeys.OrderBy(s => s.Key))
                    {
                        existingContent.Add($"[{section.Key}]");

                        foreach (var kvp in section.Value)
                        {
                            existingContent.Add($"{kvp.Key}={kvp.Value}");
                        }

                        existingContent.Add("");
                    }
                }

                File.WriteAllLines(italianPath, existingContent, Encoding.UTF8);

                Console.WriteLine($"[LanguageManager] ✅ Salvate {Instance._missingKeys.Count} nuove chiavi in Italiano.ini");
                Instance._missingKeys.Clear();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LanguageManager] ❌ Errore salvataggio chiavi: {ex.Message}");
            }
        }

        public static void SetLanguage(string languageName)
        {
            Console.WriteLine($"[LanguageManager] 🔄 Cambio lingua:  {Instance._currentLanguage} → {languageName}");
            Instance.LoadLanguage(languageName);
            LanguageChanged?.Invoke(null, EventArgs.Empty);
            Console.WriteLine($"[LanguageManager] ✅ Lingua cambiata in: {Instance._currentLanguage}");
        }

        public static string GetCurrentLanguage()
        {
            return Instance._currentLanguage;
        }

        public static List<string> GetAvailableLanguages()
        {
            try
            {
                string languagesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Languages");

                if (!Directory.Exists(languagesPath))
                    return new List<string> { "Italiano" };

                return Directory.GetFiles(languagesPath, "*.ini")
                    .Select(f => Path.GetFileNameWithoutExtension(f))
                    .OrderBy(name => name)
                    .ToList();
            }
            catch
            {
                return new List<string> { "Italiano" };
            }
        }
    }
}