using System;
using AirDirector.Services.Licensing;

namespace AirDirector.Models
{
    /// <summary>
    /// Informazioni sulla licenza del software
    /// </summary>
    public class LicenseInfo
    {
        public string SerialKey { get; set; }
        public string OwnerName { get; set; }
        public DateTime ActivatedOn { get; set; }
        public string MachineID { get; set; }
        public string ProductName { get; set; }
        public string Version { get; set; }
        public bool IsActivated { get; set; }
        public bool IsDemoMode { get; set; }

        public LicenseInfo()
        {
            SerialKey = string.Empty;
            OwnerName = string.Empty;
            ActivatedOn = DateTime.MinValue;
            MachineID = string.Empty;
            ProductName = "AirDirector";
            Version = AppVersion.Current;
            IsActivated = false;
            IsDemoMode = true;
        }

        /// <summary>
        /// Verifica se la licenza è valida
        /// </summary>
        public bool IsValid()
        {
            if (IsDemoMode) return true;

            if (string.IsNullOrEmpty(SerialKey))
                return false;

            if (!IsActivated)
                return false;

            // Verifica formato seriale
            if (!IsValidSerialFormat(SerialKey))
                return false;

            // Verifica che l'hardware ID corrisponda a quello corrente
            if (!string.IsNullOrEmpty(MachineID) &&
                MachineID != HardwareIdentifier.GetMachineID())
                return false;

            return true;
        }

        /// <summary>
        /// Verifica formato seriale ADR-XXXX-XXXX-XXXX
        /// </summary>
        private bool IsValidSerialFormat(string serial)
        {
            if (string.IsNullOrEmpty(serial))
                return false;

            // Formato: ADR-XXXX-XXXX-XXXX (18 caratteri)
            if (!serial.StartsWith("ADR-"))
                return false;

            string[] parts = serial.Split('-');
            if (parts.Length != 4)
                return false;

            if (parts[0] != "ADR") return false;
            if (parts[1].Length != 4) return false;
            if (parts[2].Length != 4) return false;
            if (parts[3].Length != 4) return false;

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
        /// Crea una licenza in modalità demo
        /// </summary>
        public static LicenseInfo CreateDemoLicense()
        {
            return new LicenseInfo
            {
                IsDemoMode = true,
                IsActivated = false,
                ProductName = "AirDirector",
                Version = AppVersion.Current
            };
        }

        public override string ToString()
        {
            if (IsDemoMode)
                return "Modalità Demo";

            string displayName = !string.IsNullOrEmpty(OwnerName) ? OwnerName : SerialKey;
            return $"{displayName} - Attivato: {ActivatedOn:dd/MM/yyyy HH:mm}";
        }
    }
}