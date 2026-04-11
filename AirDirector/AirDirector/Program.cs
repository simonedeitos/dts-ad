using System;
using System.Threading;
using System.Windows.Forms;
using AirDirector.Forms;
using AirDirector.Services.Licensing;
using AirDirector.Services.Localization;
using AirDirector.Services.Database;

namespace AirDirector
{
    static class Program
    {
        private static Mutex mutex = null;

        [STAThread]
        static void Main()
        {
            const string mutexName = "AirDirector_SingleInstance_Mutex";
            bool createdNew;

            mutex = new Mutex(true, mutexName, out createdNew);

            if (!createdNew)
            {
                return;
            }

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Inizializza sistema multilingua
                LanguageManager.Initialize();

                // Inizializza database
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
            finally
            {
                if (mutex != null)
                {
                    mutex.ReleaseMutex();
                    mutex.Dispose();
                }
            }
        }
    }
}