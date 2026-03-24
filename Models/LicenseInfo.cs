using System;

namespace AirDirector.Models
{
    /// <summary>
    /// Informazioni sulla licenza del software
    /// </summary>
    public class LicenseInfo
    {
        public string Email { get; set; }
        public string SerialKey { get; set; }
        public DateTime ActivatedOn { get; set; }
        public string MachineID { get; set; }
        public string ProductName { get; set; }
        public string Version { get; set; }
        public bool IsActivated { get; set; }
        public bool IsDemoMode { get; set; }

        public LicenseInfo()
        {
            Email = string.Empty;
            SerialKey = string.Empty;
            ActivatedOn = DateTime.MinValue;
            MachineID = string.Empty;
            ProductName = "AirDirector";
            Version = "1.0.0";
            IsActivated = false;
            IsDemoMode = true;
        }

        /// <summary>
        /// Verifica se la licenza è valida
        /// </summary>
        public bool IsValid()
        {
            if (IsDemoMode) return true;

            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(SerialKey))
                return false;

            if (!IsActivated)
                return false;

            // Verifica formato seriale
            if (!IsValidSerialFormat(SerialKey))
                return false;

            return true;
        }

        /// <summary>
        /// Verifica formato seriale AD-XXXX-XXXX-XXXX-XXXX
        /// </summary>
        private bool IsValidSerialFormat(string serial)
        {
            if (string.IsNullOrEmpty(serial))
                return false;

            // Formato: AD-XXXX-XXXX-XXXX-XXXX (22 caratteri)
            if (serial.Length != 24)
                return false;

            if (!serial.StartsWith("AD-"))
                return false;

            string[] parts = serial.Split('-');
            if (parts.Length != 5)
                return false;

            // Verifica lunghezza delle parti
            if (parts[0] != "AD") return false;
            if (parts[1].Length != 4) return false;
            if (parts[2].Length != 4) return false;
            if (parts[3].Length != 4) return false;
            if (parts[4].Length != 4) return false;

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
                Version = "1.0.0"
            };
        }

        public override string ToString()
        {
            if (IsDemoMode)
                return "Modalità Demo";

            return $"{Email} - {SerialKey} - Attivato: {ActivatedOn:dd/MM/yyyy HH:mm}";
        }
    }
}