using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AirDirector.Models;

namespace AirDirector.Services.Licensing
{
    /// <summary>
    /// Gestore licenze AirDirector
    /// </summary>
    public static class LicenseManager
    {
        // ── Percorso file licenza ──────────────────────────────────────────────
        private static readonly string AppDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "AirDirector"
        );

        private static readonly string LicenseFilePath = Path.Combine(AppDataPath, "AirDirector.lic");

        // ── API ───────────────────────────────────────────────────────────────
        private const string API_BASE = "https://store.airdirector.app/api/";

        private static readonly string API_KEY = LoadApiKey();

        private static readonly HttpClient _http = CreateHttpClient();

        private const string REGISTRY_KEY = @"SOFTWARE\AirDirector";
        private const string REGISTRY_VALUE_NAME = "ApiKey";
        private const string DEFAULT_API_KEY = "73a434a1107442481e13ed52ceba1a574648adb12fd5bc0e0c967f25f6743731";

        /// <summary>
        /// Carica la API key dal Registry di Windows (HKCU\SOFTWARE\AirDirector\ApiKey).
        /// Se il valore non esiste, lo crea con la API key di default e la restituisce.
        /// </summary>
        private static string LoadApiKey()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(REGISTRY_KEY))
                {
                    object? val = key.GetValue(REGISTRY_VALUE_NAME);
                    if (val != null)
                    {
                        string apiKey = val.ToString() ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(apiKey))
                            return apiKey;
                    }

                    // Valore non trovato → crea il campo con la API key di default
                    key.SetValue(REGISTRY_VALUE_NAME, DEFAULT_API_KEY, RegistryValueKind.String);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore lettura ApiKey dal Registry: {ex.Message}");
            }

            return DEFAULT_API_KEY;
        }

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
            if (!string.IsNullOrEmpty(API_KEY))
                client.DefaultRequestHeaders.Add("X-API-Key", API_KEY);
            client.Timeout = TimeSpan.FromSeconds(15);
            return client;
        }

        // ── Cifratura file licenza (AES-256-CBC) ─────────────────────────────
        private const string EncPassphrase = "AirDirector.Lic.2024#Secure";
        private static readonly byte[] EncSalt =
        {
            0x4A, 0x69, 0x2E, 0xAC, 0x7B, 0xF3, 0x1D, 0x88,
            0x5C, 0x40, 0xE2, 0x9A, 0x3F, 0xC1, 0x55, 0x7E
        };

        private static (byte[] key, byte[] iv) DeriveKeyAndIV()
        {
            using var kdf = new Rfc2898DeriveBytes(
                EncPassphrase, EncSalt, 100_000, HashAlgorithmName.SHA256);
            return (kdf.GetBytes(32), kdf.GetBytes(16));
        }

        private static string EncryptLicense(string plainText)
        {
            var (key, iv) = DeriveKeyAndIV();
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs, Encoding.UTF8))
            {
                sw.Write(plainText);
                sw.Flush();
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        private static string DecryptLicense(string cipherText)
        {
            var (key, iv) = DeriveKeyAndIV();
            byte[] cipherBytes = Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var ms = new MemoryStream(cipherBytes);
            using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var sr = new StreamReader(cs, Encoding.UTF8);
            return sr.ReadToEnd();
        }

        // ── Cache ─────────────────────────────────────────────────────────────
        private static LicenseInfo? _cachedLicense = null;

        // ── Lettura licenza ───────────────────────────────────────────────────

        /// <summary>
        /// Ottiene la licenza corrente (da cache o da file)
        /// </summary>
        public static LicenseInfo GetCurrentLicense()
        {
            if (_cachedLicense != null)
                return _cachedLicense;

            if (File.Exists(LicenseFilePath))
            {
                try
                {
                    string fileContent = File.ReadAllText(LicenseFilePath);

                    // Prova prima come contenuto cifrato; se fallisce prova come JSON in chiaro
                    // (compatibilità con file .lic precedenti alla cifratura)
                    string json;
                    bool wasEncrypted = true;
                    try
                    {
                        json = DecryptLicense(fileContent);
                    }
                    catch (FormatException)
                    {
                        // fileContent non è Base64 valido → file in chiaro
                        json = fileContent;
                        wasEncrypted = false;
                    }
                    catch (CryptographicException)
                    {
                        // Decifrazione fallita → file in chiaro o corrotto
                        json = fileContent;
                        wasEncrypted = false;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[LicenseManager] Errore inatteso nella decifrazione: {ex.Message}");
                        json = fileContent;
                        wasEncrypted = false;
                    }

                    var loaded = JsonConvert.DeserializeObject<LicenseInfo>(json);

                    if (loaded != null && loaded.IsValid())
                    {
                        // Se il file era in chiaro, riscrivilo cifrato
                        if (!wasEncrypted)
                            SaveLicenseToFile(loaded, out _);

                        _cachedLicense = loaded;
                        return _cachedLicense;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Errore caricamento licenza: {ex.Message}");
                }
            }

            // Nessuna licenza valida → modalità demo
            _cachedLicense = LicenseInfo.CreateDemoLicense();
            return _cachedLicense;
        }

        /// <summary>Verifica se è in modalità demo</summary>
        public static bool IsDemoMode()
        {
            return GetCurrentLicense().IsDemoMode;
        }

        /// <summary>Verifica se la licenza è attivata e valida</summary>
        public static bool IsLicenseValid()
        {
            var license = GetCurrentLicense();
            return license.IsActivated && !license.IsDemoMode;
        }

        // ── Attivazione ───────────────────────────────────────────────────────

        /// <summary>
        /// Verifica e attiva la licenza tramite le API del server.
        /// Restituisce true se l'attivazione è riuscita.
        /// </summary>
        public static bool ActivateLicense(string serialKey, string ownerName, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(serialKey))
            {
                errorMessage = "Inserisci il codice seriale";
                return false;
            }

            serialKey = serialKey.ToUpper().Trim();

            if (!IsValidSerialFormat(serialKey))
            {
                errorMessage = "Formato seriale non valido.\nFormato corretto: ADR-XXXX-XXXX-XXXX";
                return false;
            }

            string hwId = HardwareIdentifier.GetMachineID();

            try
            {
                // 1. Verifica licenza sul server
                var checkResult = Task.Run(() => CheckLicenseOnServerAsync(serialKey))
                                      .GetAwaiter().GetResult();

                if (!checkResult.exists)
                {
                    errorMessage = "Seriale non valido";
                    return false;
                }

                if (!checkResult.orderConfirmed)
                {
                    errorMessage = "Ordine in attesa di conferma";
                    return false;
                }

                if (checkResult.expired)
                {
                    errorMessage = "Licenza scaduta";
                    return false;
                }

                if (checkResult.isActive && checkResult.hardwareId != hwId)
                {
                    errorMessage = "Licenza già attiva su un altro dispositivo.\nDisattivala prima dal tuo account.";
                    return false;
                }

                // 2. Se non è ancora attiva su questo PC → attiva
                if (!checkResult.isActive)
                {
                    var activateResult = Task.Run(() => ActivateLicenseOnServerAsync(serialKey, hwId))
                                             .GetAwaiter().GetResult();
                    if (!activateResult)
                    {
                        errorMessage = "Errore durante l'attivazione sul server";
                        return false;
                    }
                }

                // 3. Crea e salva la licenza locale
                var license = new LicenseInfo
                {
                    SerialKey   = serialKey,
                    OwnerName   = string.IsNullOrWhiteSpace(ownerName) ? serialKey : ownerName.Trim(),
                    ActivatedOn = DateTime.Now,
                    MachineID   = hwId,
                    ProductName = "AirDirector",
                    Version     = AppVersion.Current,
                    IsActivated = true,
                    IsDemoMode  = false
                };

                if (!SaveLicenseToFile(license, out string saveError))
                {
                    errorMessage = saveError;
                    return false;
                }

                _cachedLicense = license;
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Errore di connessione al server di licenze:\n{ex.Message}";
                return false;
            }
        }

        // ── Disattivazione ────────────────────────────────────────────────────

        /// <summary>
        /// Rimuove la licenza locale e la disattiva sul server.
        /// </summary>
        public static bool RemoveLicense(out string errorMessage)
        {
            errorMessage = string.Empty;

            var current = GetCurrentLicense();

            if (current.IsDemoMode)
            {
                errorMessage = "Nessuna licenza attiva da rimuovere";
                return false;
            }

            try
            {
                bool serverOk = Task.Run(() => DeactivateLicenseOnServerAsync(current.SerialKey))
                                    .GetAwaiter().GetResult();

                if (!serverOk)
                {
                    errorMessage = "Errore durante la disattivazione sul server";
                    return false;
                }

                if (File.Exists(LicenseFilePath))
                    File.Delete(LicenseFilePath);

                _cachedLicense = LicenseInfo.CreateDemoLicense();
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Errore rimozione licenza: {ex.Message}";
                return false;
            }
        }

        // ── Check periodico ───────────────────────────────────────────────────

        /// <summary>
        /// Verifica periodica della licenza sul server (da chiamare all'avvio o ogni 24 ore).
        /// Se la licenza risulta disattivata torna in modalità demo.
        /// </summary>
        public static bool PeriodicCheck(out string statusMessage)
        {
            statusMessage = string.Empty;

            var current = GetCurrentLicense();
            if (current.IsDemoMode)
                return true; // niente da verificare in demo

            try
            {
                var result = Task.Run(() => CheckLicenseOnServerAsync(current.SerialKey))
                                  .GetAwaiter().GetResult();

                if (!result.exists || !result.isActive)
                {
                    // Licenza non più valida → torna in demo
                    if (File.Exists(LicenseFilePath))
                        File.Delete(LicenseFilePath);
                    _cachedLicense = LicenseInfo.CreateDemoLicense();
                    statusMessage = "Licenza disattivata. Il software tornerà in modalità demo.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                // Nessuna connessione → continua a funzionare localmente
                Console.WriteLine($"[LicenseManager] PeriodicCheck: connessione assente — {ex.Message}");
                return true;
            }
        }

        // ── Chiamate API ──────────────────────────────────────────────────────

        private static async Task<(bool exists, bool orderConfirmed, bool expired, bool isActive, string hardwareId)>
            CheckLicenseOnServerAsync(string serial)
        {
            var response = await _http.GetAsync($"{API_BASE}license_check.php?serial={Uri.EscapeDataString(serial)}");
            string json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Errore server ({(int)response.StatusCode}): {json}");

            var obj = JObject.Parse(json);

            bool exists   = obj["exists"]?.Value<bool>() ?? false;
            bool orderOk  = obj["order_confirmed"]?.Value<bool>() ?? false;
            bool expired  = obj["expired"]?.Value<bool>() ?? false;
            bool isActive = (obj["is_active"]?.Value<int>() ?? 0) == 1;
            string hwId   = obj["hardware_id"]?.Value<string>() ?? string.Empty;

            return (exists, orderOk, expired, isActive, hwId);
        }

        private static async Task<bool> ActivateLicenseOnServerAsync(string serial, string hardwareId)
        {
            var body = JsonConvert.SerializeObject(new { serial, hardware_id = hardwareId });
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync($"{API_BASE}license_activate.php", content);

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                return false; // già attiva su altro PC

            string json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Errore attivazione ({(int)response.StatusCode}): {json}");

            var obj = JObject.Parse(json);
            bool success       = obj["success"]?.Value<bool>() ?? false;
            bool alreadyActive = obj["already_active"]?.Value<bool>() ?? false;

            return success || alreadyActive;
        }

        private static async Task<bool> DeactivateLicenseOnServerAsync(string serial)
        {
            var body = JsonConvert.SerializeObject(new { serial });
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync($"{API_BASE}license_deactivate.php", content);

            string json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Errore disattivazione ({(int)response.StatusCode}): {json}");

            var obj = JObject.Parse(json);
            return obj["success"]?.Value<bool>() ?? false;
        }

        // ── Utilità ───────────────────────────────────────────────────────────

        /// <summary>
        /// Verifica formato seriale ADR-XXXX-XXXX-XXXX
        /// </summary>
        private static bool IsValidSerialFormat(string serial)
        {
            if (string.IsNullOrEmpty(serial))
                return false;

            if (!serial.StartsWith("ADR-"))
                return false;

            string[] parts = serial.Split('-');
            if (parts.Length != 4)
                return false;

            if (parts[0] != "ADR") return false;
            if (parts[1].Length != 4) return false;
            if (parts[2].Length != 4) return false;
            if (parts[3].Length != 4) return false;

            for (int i = 1; i < parts.Length; i++)
            {
                foreach (char c in parts[i])
                {
                    if (!char.IsLetterOrDigit(c))
                        return false;
                }
            }

            return true;
        }

        private static bool SaveLicenseToFile(LicenseInfo license, out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                if (!Directory.Exists(AppDataPath))
                    Directory.CreateDirectory(AppDataPath);

                string json = JsonConvert.SerializeObject(license, Formatting.Indented);
                File.WriteAllText(LicenseFilePath, EncryptLicense(json));
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Errore salvataggio licenza: {ex.Message}";
                return false;
            }
        }

        /// <summary>Percorso file licenza</summary>
        public static string GetLicenseFilePath() => LicenseFilePath;

        /// <summary>Forza il reload della licenza da file</summary>
        public static void ReloadLicense()
        {
            _cachedLicense = null;
            GetCurrentLicense();
        }
    }
}
