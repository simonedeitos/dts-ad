using System;
using System.IO;
using Newtonsoft.Json;
using AirDirector.Models;

namespace AirDirector.Services.Licensing
{
    /// <summary>
    /// Gestore licenze AirDirector
    /// </summary>
    public static class LicenseManager
    {
        private static readonly string AppDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AirDirector"
        );

        private static readonly string LicenseFilePath = Path.Combine(AppDataPath, "license.lic");

        private static LicenseInfo _cachedLicense = null;

        /// <summary>
        /// Ottiene la licenza corrente (da cache o da file)
        /// </summary>
        public static LicenseInfo GetCurrentLicense()
        {
            if (_cachedLicense != null)
                return _cachedLicense;

            // Carica da file
            if (File.Exists(LicenseFilePath))
            {
                try
                {
                    string json = File.ReadAllText(LicenseFilePath);
                    _cachedLicense = JsonConvert.DeserializeObject<LicenseInfo>(json);

                    if (_cachedLicense != null && _cachedLicense.IsValid())
                    {
                        return _cachedLicense;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Errore caricamento licenza: {ex.Message}");
                }
            }

            // Nessuna licenza valida → Modalità demo
            _cachedLicense = LicenseInfo.CreateDemoLicense();
            return _cachedLicense;
        }

        /// <summary>
        /// Verifica se è in modalità demo
        /// </summary>
        public static bool IsDemoMode()
        {
            var license = GetCurrentLicense();
            return license.IsDemoMode;
        }

        /// <summary>
        /// Verifica se la licenza è attivata e valida
        /// </summary>
        public static bool IsLicenseValid()
        {
            var license = GetCurrentLicense();
            return license.IsActivated && !license.IsDemoMode;
        }

        /// <summary>
        /// Attiva la licenza con email e seriale
        /// </summary>
        public static bool ActivateLicense(string email, string serialKey, out string errorMessage)
        {
            errorMessage = string.Empty;

            // Validazione input
            if (string.IsNullOrWhiteSpace(email))
            {
                errorMessage = "Email non valida";
                return false;
            }

            if (string.IsNullOrWhiteSpace(serialKey))
            {
                errorMessage = "Seriale non valido";
                return false;
            }

            // Normalizza seriale
            serialKey = serialKey.ToUpper().Trim();

            // Verifica formato seriale
            if (!IsValidSerialFormat(serialKey))
            {
                errorMessage = "Formato seriale non valido.\nFormato corretto: AD-XXXX-XXXX-XXXX-XXXX";
                return false;
            }

            // TODO: Interroga API per validare email + seriale su database remoto
            // Per ora simuliamo validazione locale
            bool isValidOnServer = ValidateSerialOnServer(email, serialKey, out string serverError);

            if (!isValidOnServer)
            {
                errorMessage = serverError;
                return false;
            }

            // Crea oggetto licenza
            var license = new LicenseInfo
            {
                Email = email,
                SerialKey = serialKey,
                ActivatedOn = DateTime.Now,
                MachineID = HardwareIdentifier.GetMachineID(),
                ProductName = "AirDirector",
                Version = "1.0.0",
                IsActivated = true,
                IsDemoMode = false
            };

            // Salva licenza su file
            if (!SaveLicenseToFile(license, out string saveError))
            {
                errorMessage = saveError;
                return false;
            }

            // Aggiorna cache
            _cachedLicense = license;

            return true;
        }

        /// <summary>
        /// Rimuove la licenza (per trasferimento su altro PC)
        /// </summary>
        public static bool RemoveLicense(out string errorMessage)
        {
            errorMessage = string.Empty;

            var currentLicense = GetCurrentLicense();

            if (currentLicense.IsDemoMode)
            {
                errorMessage = "Nessuna licenza attiva da rimuovere";
                return false;
            }

            try
            {
                // TODO: Chiamata API per sbloccare seriale su database remoto
                bool unblockedOnServer = UnblockSerialOnServer(currentLicense.Email, currentLicense.SerialKey);

                if (!unblockedOnServer)
                {
                    errorMessage = "Errore durante lo sblocco del seriale sul server";
                    return false;
                }

                // Elimina file licenza locale
                if (File.Exists(LicenseFilePath))
                {
                    File.Delete(LicenseFilePath);
                }

                // Reset cache a demo
                _cachedLicense = LicenseInfo.CreateDemoLicense();

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Errore rimozione licenza: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Verifica formato seriale AD-XXXX-XXXX-XXXX-XXXX
        /// </summary>
        private static bool IsValidSerialFormat(string serial)
        {
            if (string.IsNullOrEmpty(serial))
                return false;

            if (serial.Length != 24)
                return false;

            if (!serial.StartsWith("AD-"))
                return false;

            string[] parts = serial.Split('-');
            if (parts.Length != 5)
                return false;

            if (parts[0] != "AD") return false;
            if (parts[1].Length != 4) return false;
            if (parts[2].Length != 4) return false;
            if (parts[3].Length != 4) return false;
            if (parts[4].Length != 4) return false;

            // Verifica che le parti siano alfanumeriche
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

        /// <summary>
        /// Valida seriale su server (PLACEHOLDER per futura API)
        /// </summary>
        private static bool ValidateSerialOnServer(string email, string serialKey, out string errorMessage)
        {
            errorMessage = string.Empty;

            // TODO: Implementare chiamata API al database seriali
            // Esempio:
            /*
            var client = new HttpClient();
            var request = new
            {
                ProductName = "AirDirector",
                Email = email,
                SerialKey = serialKey,
                MachineID = HardwareIdentifier.GetMachineID()
            };
            
            var response = await client.PostAsync(
                "https://api.airdirector.com/license/activate",
                new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json")
            );
            
            if (!response.IsSuccessStatusCode)
            {
                errorMessage = "Seriale non valido o già utilizzato";
                return false;
            }
            
            return true;
            */

            // SIMULAZIONE: Accetta qualsiasi seriale con formato corretto
            // In produzione, questa funzione interrogherà il database reale

            Console.WriteLine($"[DEMO MODE] Validazione seriale: {email} - {serialKey}");
            Console.WriteLine($"[DEMO MODE] Machine ID: {HardwareIdentifier.GetMachineID()}");

            return true; // Accetta sempre in modalità sviluppo
        }

        /// <summary>
        /// Sblocca seriale su server (PLACEHOLDER per futura API)
        /// </summary>
        private static bool UnblockSerialOnServer(string email, string serialKey)
        {
            // TODO: Implementare chiamata API per sbloccare seriale
            /*
            var client = new HttpClient();
            var request = new
            {
                Email = email,
                SerialKey = serialKey,
                MachineID = HardwareIdentifier.GetMachineID()
            };
            
            var response = await client.PostAsync(
                "https://api.airdirector.com/license/deactivate",
                new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json")
            );
            
            return response.IsSuccessStatusCode;
            */

            Console.WriteLine($"[DEMO MODE] Sblocco seriale: {email} - {serialKey}");
            return true; // Sempre OK in modalità sviluppo
        }

        /// <summary>
        /// Salva licenza su file JSON
        /// </summary>
        private static bool SaveLicenseToFile(LicenseInfo license, out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                // Crea directory se non esiste
                if (!Directory.Exists(AppDataPath))
                {
                    Directory.CreateDirectory(AppDataPath);
                }

                // Serializza in JSON con formattazione
                string json = JsonConvert.SerializeObject(license, Formatting.Indented);

                // Salva su file
                File.WriteAllText(LicenseFilePath, json);

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Errore salvataggio licenza: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Ottiene il percorso del file licenza
        /// </summary>
        public static string GetLicenseFilePath()
        {
            return LicenseFilePath;
        }

        /// <summary>
        /// Forza il reload della licenza da file
        /// </summary>
        public static void ReloadLicense()
        {
            _cachedLicense = null;
            GetCurrentLicense();
        }
    }
}