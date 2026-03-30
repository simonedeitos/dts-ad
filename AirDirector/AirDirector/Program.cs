using System;
using System.Windows.Forms;
using AirDirector.Forms;
using AirDirector.Services.Licensing;
using AirDirector.Services.Localization;
using AirDirector.Services.Database;  // ← AGGIUNGI

namespace AirDirector
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Inizializza sistema multilingua

            // ← AGGIUNGI: Inizializza database
            DbcManager.Initialize();

            // Mostra splash screen
            SplashForm splash = new SplashForm();
            splash.ShowDialog();

            // Verifica licenza
            var currentLicense = LicenseManager.GetCurrentLicense();

            if (currentLicense.IsDemoMode || !currentLicense.IsActivated)
            {
                // Mostra form attivazione licenza
                LicenseForm licenseForm = new LicenseForm();
                DialogResult result = licenseForm.ShowDialog();

                if (result != DialogResult.OK)
                {
                    // Utente ha chiuso il form senza scegliere
                    return;
                }
            }

            // Avvia main form
            Application.Run(new MainForm());
        }
    }
}