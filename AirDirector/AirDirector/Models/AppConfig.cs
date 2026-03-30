using System;

namespace AirDirector.Models
{
    /// <summary>
    /// Configurazione generale dell'applicazione
    /// </summary>
    public class AppConfig
    {
        // Modalità operativa
        public enum OperatingMode
        {
            Radio,      // Solo audio
            RadioTV     // Audio + Video
        }

        public OperatingMode Mode { get; set; }

        // Percorsi
        public string DatabasePath { get; set; }
        public string BackupPath { get; set; }

        // Lingua
        public string Language { get; set; }

        // Audio
        public string OutputDevice { get; set; }
        public int MixDuration { get; set; }           // Millisecondi
        public int HourlySeparation { get; set; }      // Ore

        // Video/NDI (solo in modalità RadioTV)
        public string NDIOutputName { get; set; }
        public bool EnableVideoPreview { get; set; }

        // Licenza
        public bool LicenseActivated { get; set; }

        public RemoteControlConfig RemoteControl { get; set; } = new RemoteControlConfig();

        public AppConfig()
        {
            // Valori di default
            Mode = OperatingMode.Radio;
            DatabasePath = @"C:\AirDirector\Database";
            BackupPath = @"C:\AirDirector\Backup";
            Language = "Italiano";
            OutputDevice = "Default";
            MixDuration = 5000;
            HourlySeparation = 3;
            NDIOutputName = "AirDirector_Output";
            EnableVideoPreview = true;
            LicenseActivated = false;
        }

        /// <summary>
        /// Verifica se è in modalità RadioTV
        /// </summary>
        public bool IsRadioTVMode()
        {
            return Mode == OperatingMode.RadioTV;
        }

        /// <summary>
        /// Crea configurazione di default
        /// </summary>
        public static AppConfig CreateDefault()
        {
            return new AppConfig();
        }
    }
}