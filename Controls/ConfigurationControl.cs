using AirDirector.Services.Database;
using AirDirector.Services.Localization;
using AirDirector.Themes;
using NAudio.Wave;
using NewTek;
using NewTek.NDI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Registry = Microsoft.Win32.Registry;
using RegistryKey = Microsoft.Win32.RegistryKey;
using RegistryValueKind = Microsoft.Win32.RegistryValueKind;

namespace AirDirector.Controls
{
    public partial class ConfigurationControl : UserControl
    {
        private const string REGISTRY_KEY = @"SOFTWARE\AirDirector";

        public event EventHandler ConfigurationChanged;

        public ConfigurationControl()
        {
            InitializeComponent();
            LoadAudioDevicesToCombos();
            LoadConfiguration();
            ApplyLanguage();

            LanguageManager.LanguageChanged += (s, e) => ApplyLanguage();

            cmbMode.SelectedIndexChanged += CmbMode_SelectedIndexChanged;
            cmbVideoOutputType.SelectedIndexChanged += CmbVideoOutputType_SelectedIndexChanged;

            // ✅ NUOVO: Aggiungi bottone CG Editor nella tab Video
            CreateCGEditorButton();

            // Mostra tab Video se modalità RadioTV
            if (cmbMode.SelectedIndex == 1 && !tabControl.TabPages.Contains(tabVideo))
            {
                tabControl.TabPages.Insert(3, tabVideo);
            }

            // Mostra pannello NDI corretto
            CmbVideoOutputType_SelectedIndexChanged(null, null);
        }

        private void CreateCGEditorButton()
        {
            Button btnCGEditor = new Button
            {
                Text = "🎬 CG Editor",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Size = new Size(130, 35),
                BackColor = Color.FromArgb(100, 50, 150),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Name = "btnCGEditor"
            };
            btnCGEditor.FlatAppearance.BorderSize = 0;

            // Posiziona sotto il gruppo NDI o a destra del bottone Salva
            // Cerca il bottone btnSaveVideo per posizionarsi di conseguenza
            if (btnSaveVideo != null)
            {
                btnCGEditor.Location = new Point(btnSaveVideo.Right + 20, btnSaveVideo.Top);
            }
            else
            {
                // Posizione di fallback
                btnCGEditor.Location = new Point(450, 350);
            }

            btnCGEditor.Click += BtnCGEditor_Click;

            tabVideo.Controls.Add(btnCGEditor);
        }

        private void BtnCGEditor_Click(object sender, EventArgs e)
        {
            using (var cgForm = new AirDirector.Forms.CGEditorForm())
            {
                cgForm.ShowDialog(this);
            }
        }

        private void CmbMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool isRadioTV = cmbMode.SelectedIndex == 1;

            if (isRadioTV && !tabControl.TabPages.Contains(tabVideo))
            {
                tabControl.TabPages.Insert(3, tabVideo);
            }
            else if (!isRadioTV && tabControl.TabPages.Contains(tabVideo))
            {
                tabControl.TabPages.Remove(tabVideo);
            }
        }

        private void CmbVideoOutputType_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool isNDI = cmbVideoOutputType.SelectedIndex == 0;

            grpNDI.Visible = isNDI;

            if (isNDI)
            {
                LoadNDIDevices();
            }
        }

        private void LoadNDIDevices()
        {
            cmbNDISource.Items.Clear();

            try
            {
                Console.WriteLine("");
                Console.WriteLine("========================================");
                Console.WriteLine("[NDI] 🔧 Inizializzazione NDI.. .");
                Console.WriteLine("========================================");

                if (!NDIlib.initialize())
                {
                    Console.WriteLine("[NDI] ❌ ERRORE:  Inizializzazione fallita");
                    Console.WriteLine("[NDI] 💡 Verifica che Processing.NDI. Lib.x64.dll sia presente");
                    Console.WriteLine("========================================");
                    Console.WriteLine("");

                    cmbNDISource.Items.Add("(Errore inizializzazione NDI)");
                    cmbNDISource.SelectedIndex = 0;

                    MessageBox.Show(
                        "⚠️ NDI non può essere inizializzato!\n\n" +
                        "Possibili cause:\n" +
                        "1. Processing.NDI. Lib.x64.dll mancante\n" +
                        "2. Versione NDI incompatibile\n" +
                        "3. Dipendenze mancanti\n\n" +
                        "Soluzione:\n" +
                        "Reinstalla NDI Tools da https://ndi.video/tools/",
                        "Errore NDI",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                Console.WriteLine("[NDI] ✅ NDI inizializzato correttamente");

                cmbNDISource.Items.Add("(Crea Nuova Sorgente NDI)");

                Console.WriteLine("[NDI] 🔍 Creazione NDI Finder...");

                var findSettings = new NDIlib.find_create_t()
                {
                    show_local_sources = true,
                    p_groups = IntPtr.Zero,
                    p_extra_ips = IntPtr.Zero
                };

                IntPtr findInstance = NDIlib.find_create_v2(ref findSettings);

                if (findInstance == IntPtr.Zero)
                {
                    Console.WriteLine("[NDI] ⚠️ Impossibile creare finder");
                    Console.WriteLine("========================================");
                    Console.WriteLine("");
                    cmbNDISource.SelectedIndex = 0;
                    return;
                }

                Console.WriteLine("[NDI] ✅ Finder creato");
                Console.WriteLine("[NDI] 🔍 Ricerca sorgenti NDI sulla rete...");
                Console.WriteLine("[NDI] ⏳ Attendi 2 secondi per discovery...");

                System.Threading.Thread.Sleep(2000);

                uint numSources = 0;
                IntPtr sourcesPtr = NDIlib.find_get_current_sources(findInstance, ref numSources);

                Console.WriteLine($"[NDI] 📡 Trovate {numSources} sorgenti NDI");

                if (numSources > 0)
                {
                    int stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(NDIlib.source_t));

                    for (int i = 0; i < numSources; i++)
                    {
                        IntPtr sourcePtr = IntPtr.Add(sourcesPtr, i * stride);
                        NDIlib.source_t source = System.Runtime.InteropServices.Marshal.PtrToStructure<NDIlib.source_t>(sourcePtr);

                        string sourceName = System.Runtime.InteropServices.Marshal.PtrToStringAnsi(source.p_ndi_name);

                        if (!string.IsNullOrEmpty(sourceName))
                        {
                            cmbNDISource.Items.Add($"📡 {sourceName}");
                            Console.WriteLine($"[NDI]   ✓ {sourceName}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("[NDI] ℹ️ Nessuna sorgente NDI trovata sulla rete");
                    Console.WriteLine("[NDI] 💡 Assicurati che altre applicazioni NDI siano attive");
                }

                NDIlib.find_destroy(findInstance);
                Console.WriteLine("[NDI] ✅ Finder distrutto");

                Console.WriteLine("========================================");
                Console.WriteLine($"[NDI] 📊 Totale sorgenti nel dropdown: {cmbNDISource.Items.Count}");
                Console.WriteLine("========================================");
                Console.WriteLine("");

                if (cmbNDISource.Items.Count > 0)
                {
                    cmbNDISource.SelectedIndex = 0;
                    Console.WriteLine("[NDI] ✅ Selezione default: (Crea Nuova Sorgente NDI)");
                }
            }
            catch (DllNotFoundException dllEx)
            {
                Console.WriteLine("");
                Console.WriteLine("========================================");
                Console.WriteLine("[NDI] ❌❌❌ DLL NON TROVATA ❌❌❌");
                Console.WriteLine($"[NDI] Messaggio: {dllEx.Message}");
                Console.WriteLine($"[NDI] File: Processing.NDI. Lib.x64.dll");
                Console.WriteLine("[NDI] Percorso atteso:  (stessa cartella di AirDirector. exe)");
                Console.WriteLine("========================================");
                Console.WriteLine("");

                cmbNDISource.Items.Clear();
                cmbNDISource.Items.Add("(DLL NDI non trovata)");
                cmbNDISource.SelectedIndex = 0;

                MessageBox.Show(
                    "❌ Processing.NDI.Lib.x64.dll non trovata!\n\n" +
                    "La DLL deve essere nella stessa cartella di AirDirector.exe\n\n" +
                    "COPIA:\n" +
                    "C:\\Program Files\\NDI\\NDI 5 Tools\\Runtime\\Processing.NDI.Lib.x64.dll\n\n" +
                    "IN:\n" +
                    $"{AppDomain.CurrentDomain.BaseDirectory}\n\n" +
                    "Poi riavvia AirDirector.",
                    "NDI DLL Mancante",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                Console.WriteLine("");
                Console.WriteLine("========================================");
                Console.WriteLine("[NDI] ❌ ERRORE GENERICO");
                Console.WriteLine($"[NDI] Messaggio:  {ex.Message}");
                Console.WriteLine($"[NDI] Tipo: {ex.GetType().Name}");
                Console.WriteLine($"[NDI] StackTrace:");
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine("========================================");
                Console.WriteLine("");

                cmbNDISource.Items.Clear();
                cmbNDISource.Items.Add($"(Errore:  {ex.Message})");
                cmbNDISource.SelectedIndex = 0;

                MessageBox.Show(
                    $"❌ Errore durante l'inizializzazione NDI:\n\n{ex.Message}\n\n" +
                    "Controlla la console per dettagli.",
                    "Errore NDI",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void ApplyLanguage()
        {
            tabGeneral.Text = "⚙️ " + LanguageManager.GetString("Configuration. Tab.General", "Generale");
            tabStation.Text = "📻 " + LanguageManager.GetString("Configuration.Tab.Station", "Stazione");
            tabAudio.Text = "🔊 " + LanguageManager.GetString("Configuration.Tab. Audio", "Audio");
            tabVideo.Text = "🎬 " + LanguageManager.GetString("Configuration.Tab.Video", "Video");
            tabPaths.Text = "📂 " + LanguageManager.GetString("Configuration.Tab. Paths", "Percorsi");
            tabMetadata.Text = "📡 " + LanguageManager.GetString("Configuration.Tab. Metadata", "Metadata");
            tabBackup.Text = "💾 " + LanguageManager.GetString("Configuration.Tab.Backup", "Backup");

            lblGeneralTitle.Text = LanguageManager.GetString("Configuration.General.Title", "⚙️ IMPOSTAZIONI GENERALI");
            lblMode.Text = LanguageManager.GetString("Configuration.Mode", "Modalità:");
            lblLanguage.Text = LanguageManager.GetString("Configuration.Language", "Lingua:");
            lblMixDuration.Text = LanguageManager.GetString("Configuration.MixDuration", "Durata Mix (ms):");
            lblHourlySeparation.Text = LanguageManager.GetString("Configuration.HourlySeparation", "Ore Separazione Brano:");
            lblArtistSeparation.Text = LanguageManager.GetString("Configuration.ArtistSeparation", "Ore Separazione Artista:");
            chkAutoStart.Text = "▶ " + LanguageManager.GetString("Configuration.AutoStart", "Avvia automaticamente player in AUTO");
            chkShowWhatsApp.Text = "💬 " + LanguageManager.GetString("Configuration.ShowWhatsApp", "Mostra WhatsApp (la modifica richiede il riavvio)");
            btnSaveGeneral.Text = "💾 " + LanguageManager.GetString("Common.Save", "Salva");

            lblStationTitle.Text = LanguageManager.GetString("Configuration.Station.Title", "📻 DATI STAZIONE");
            lblStationName.Text = LanguageManager.GetString("Configuration.StationName", "Nome Radio:");
            lblLogoPath.Text = LanguageManager.GetString("Configuration.LogoPath", "PNG Logo (120x70 px):");
            btnBrowseLogo.Text = "📁";
            btnSaveStation.Text = "💾 " + LanguageManager.GetString("Common.Save", "Salva");

            lblAudioTitle.Text = LanguageManager.GetString("Configuration.Audio.Title", "🔊 USCITE AUDIO");
            lblMainOutput.Text = LanguageManager.GetString("Configuration.MainOutput", "Main Output:");
            lblPreviewOutput.Text = LanguageManager.GetString("Configuration.PreviewOutput", "Preview Output:");
            lblPaletteOutput.Text = LanguageManager.GetString("Configuration.PaletteOutput", "Palette Output:");
            btnRefresh.Text = "🔄 " + LanguageManager.GetString("Configuration.Refresh", "Aggiorna Dispositivi");
            btnSaveAudio.Text = "💾 " + LanguageManager.GetString("Common.Save", "Salva");

            lblVideoTitle.Text = LanguageManager.GetString("Configuration.Video.Title", "🎬 CONFIGURAZIONE VIDEO");
            lblVideoOutputType.Text = LanguageManager.GetString("Configuration.VideoOutputType", "Tipo Output Video:");
            lblVideoFrameRate.Text = LanguageManager.GetString("Configuration.VideoFrameRate", "Frame Rate:");
            lblBufferVideoPath.Text = LanguageManager.GetString("Configuration.BufferVideoPath", "Cartella Video Tampone:");
            lblBufferMode.Text = LanguageManager.GetString("Configuration.BufferMode", "Modalità Riproduzione:");
            lblAdvLanner.Text = LanguageManager.GetString("Configuration.AdvLanner", "ADV Lanner Video on Output Playout:");
            btnBrowseBufferVideo.Text = "📁";
            btnRefreshNDI.Text = "🔄 " + LanguageManager.GetString("Configuration.Refresh", "Aggiorna");
            btnSaveVideo.Text = "💾 " + LanguageManager.GetString("Common.Save", "Salva");

            grpNDI.Text = LanguageManager.GetString("Configuration.NDI.Title", "⚡ Configurazione NDI");
            lblNDISource.Text = LanguageManager.GetString("Configuration.NDI. SourceName", "Nome Sorgente NDI:");

            lblPathsTitle.Text = LanguageManager.GetString("Configuration. Paths.Title", "📂 PERCORSI E MAPPATURE");
            lblDatabasePath.Text = LanguageManager.GetString("Configuration.DatabasePath", "Path Database:");
            lblDriveX.Text = LanguageManager.GetString("Configuration.DriveX", "Drive X (Database Condiviso):");
            lblDriveY.Text = LanguageManager.GetString("Configuration. DriveY", "Drive Y (Archivio Commercial):");
            lblDriveZ.Text = LanguageManager.GetString("Configuration.DriveZ", "Drive Z (Archivio Musica):");
            lblTimeSignalPath.Text = LanguageManager.GetString("Configuration.TimeSignalPath", "Cartella Segnale Orario:");
            btnBrowseDatabase.Text = "📁";
            btnBrowseDriveX.Text = "📁";
            btnBrowseDriveY.Text = "📁";
            btnBrowseDriveZ.Text = "📁";
            btnBrowseTimeSignal.Text = "📁";
            btnApplyDriveX.Text = "✓ " + LanguageManager.GetString("Configuration.Apply", "Applica");
            btnApplyDriveY.Text = "✓ " + LanguageManager.GetString("Configuration.Apply", "Applica");
            btnApplyDriveZ.Text = "✓ " + LanguageManager.GetString("Configuration.Apply", "Applica");
            btnSavePaths.Text = "💾 " + LanguageManager.GetString("Configuration.SaveConfiguration", "Salva Configurazione");

            lblMetadataTitle.Text = LanguageManager.GetString("Configuration.Metadata.Title", "📡 METADATA");
            lblMetadataSource.Text = LanguageManager.GetString("Configuration.MetadataSource", "Sorgente Metadata:");
            rbMusicOnly.Text = LanguageManager.GetString("Configuration.MusicOnly", "Solo Musica");
            rbMusicAndClips.Text = LanguageManager.GetString("Configuration.MusicAndClips", "Musica + Clips");
            lblRdsPath.Text = LanguageManager.GetString("Configuration.RdsPath", "File RDS. txt:");
            chkSaveRds.Text = "💾 " + LanguageManager.GetString("Configuration.SaveRds", "Salva file RDS.txt");
            chkSendToEncoders.Text = "📤 " + LanguageManager.GetString("Configuration.SendToEncoders", "Invia metadata agli Encoders attivi");
            btnBrowseRds.Text = "📁";
            btnSaveMetadata.Text = "💾 " + LanguageManager.GetString("Common.Save", "Salva");

            lblBackupTitle.Text = LanguageManager.GetString("Configuration. Backup.Title", "💾 BACKUP AUTOMATICO");
            lblBackupPath.Text = LanguageManager.GetString("Configuration.BackupPath", "Cartella Backup:");
            lblBackupTime.Text = LanguageManager.GetString("Configuration.BackupTime", "Orario Backup Automatico:");
            btnBrowseBackup.Text = "📁";
            btnSaveBackup.Text = "💾 " + LanguageManager.GetString("Common.Save", "Salva");

            int selectedModeIndex = cmbMode.SelectedIndex;
            cmbMode.Items.Clear();
            cmbMode.Items.Add(LanguageManager.GetString("Configuration.ModeRadio", "Radio (solo audio)"));
            cmbMode.Items.Add(LanguageManager.GetString("Configuration.ModeRadioTV", "RadioTV (audio + video)"));
            if (selectedModeIndex >= 0 && selectedModeIndex < cmbMode.Items.Count)
                cmbMode.SelectedIndex = selectedModeIndex;

            // Solo NDI per ora
            int selectedVideoType = cmbVideoOutputType.SelectedIndex;
            cmbVideoOutputType.Items.Clear();
            cmbVideoOutputType.Items.Add("NDI (Network Device Interface)");
            if (selectedVideoType >= 0 && selectedVideoType < cmbVideoOutputType.Items.Count)
                cmbVideoOutputType.SelectedIndex = selectedVideoType;
            else
                cmbVideoOutputType.SelectedIndex = 0; // Default NDI

            int selectedBufferMode = cmbBufferMode.SelectedIndex;
            cmbBufferMode.Items.Clear();
            cmbBufferMode.Items.Add(LanguageManager.GetString("Configuration.BufferMode. RandomLoop", "File Random in Loop"));
            cmbBufferMode.Items.Add(LanguageManager.GetString("Configuration.BufferMode. Slideshow", "Slideshow Sequenziale"));
            if (selectedBufferMode >= 0 && selectedBufferMode < cmbBufferMode.Items.Count)
                cmbBufferMode.SelectedIndex = selectedBufferMode;

            int selectedAdvLanner = cmbAdvLanner.SelectedIndex;
            cmbAdvLanner.Items.Clear();
            cmbAdvLanner.Items.Add(LanguageManager.GetString("Configuration.AdvLanner.YesInternal", "Yes, Internal"));
            cmbAdvLanner.Items.Add(LanguageManager.GetString("Configuration.AdvLanner.NoExternal", "No, External"));
            if (selectedAdvLanner >= 0 && selectedAdvLanner < cmbAdvLanner.Items.Count)
                cmbAdvLanner.SelectedIndex = selectedAdvLanner;
            else
                cmbAdvLanner.SelectedIndex = 0;
        }

        private void LoadAudioDevicesToCombos()
        {
            LoadAudioDevices(cmbMainOutput);
            LoadAudioDevices(cmbPreviewOutput);
            LoadAudioDevices(cmbPaletteOutput);

            cmbMode.Items.Clear();
            cmbMode.Items.Add(LanguageManager.GetString("Configuration.ModeRadio", "Radio (solo audio)"));
            cmbMode.Items.Add(LanguageManager.GetString("Configuration.ModeRadioTV", "RadioTV (audio + video)"));
            cmbMode.SelectedIndex = 0;

            cmbLanguage.Items.Clear();
            foreach (var lang in LanguageManager.GetAvailableLanguages())
            {
                cmbLanguage.Items.Add(lang);
            }
            if (cmbLanguage.Items.Count > 0)
                cmbLanguage.SelectedIndex = 0;

            // Solo NDI per ora
            cmbVideoOutputType.Items.Clear();
            cmbVideoOutputType.Items.Add("NDI (Network Device Interface)");
            cmbVideoOutputType.SelectedIndex = 0;

            cmbVideoFrameRate.Items.Clear();
            cmbVideoFrameRate.Items.Add("25 fps (PAL)");
            cmbVideoFrameRate.Items.Add("30 fps (NTSC)");
            cmbVideoFrameRate.Items.Add("50 fps");
            cmbVideoFrameRate.Items.Add("60 fps");
            cmbVideoFrameRate.SelectedIndex = 0;

            cmbBufferMode.Items.Clear();
            cmbBufferMode.Items.Add("File Random in Loop");
            cmbBufferMode.Items.Add("Slideshow Sequenziale");
            cmbBufferMode.SelectedIndex = 0;

            cmbAdvLanner.Items.Clear();
            cmbAdvLanner.Items.Add("Yes, Internal");
            cmbAdvLanner.Items.Add("No, External");
            cmbAdvLanner.SelectedIndex = 0;
        }

        private void LoadAudioDevices(ComboBox combo)
        {
            combo.Items.Clear();
            combo.Items.Add("(Default System Device)");

            try
            {
                for (int i = 0; i < WaveOut.DeviceCount; i++)
                {
                    var capabilities = WaveOut.GetCapabilities(i);
                    combo.Items.Add($"{i}:  {capabilities.ProductName}");
                }
            }
            catch { }

            if (combo.Items.Count > 0)
                combo.SelectedIndex = 0;
        }

        private void LoadConfiguration()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(REGISTRY_KEY))
                {
                    txtStationName.Text = key.GetValue("StationName", "AirDirector Radio").ToString();
                    txtLogoPath.Text = key.GetValue("LogoPath", "").ToString();

                    SetComboSelection(cmbMainOutput, key.GetValue("MainOutput", "(Default System Device)").ToString());
                    SetComboSelection(cmbPreviewOutput, key.GetValue("PreviewOutput", "(Default System Device)").ToString());
                    SetComboSelection(cmbPaletteOutput, key.GetValue("PaletteOutput", "(Default System Device)").ToString());

                    cmbMode.SelectedIndex = DbcManager.GetConfigValue("Mode", "Radio") == "RadioTV" ? 1 : 0;

                    var currentLang = LanguageManager.GetCurrentLanguage();
                    for (int i = 0; i < cmbLanguage.Items.Count; i++)
                    {
                        if (cmbLanguage.Items[i].ToString() == currentLang)
                        {
                            cmbLanguage.SelectedIndex = i;
                            break;
                        }
                    }

                    int mixDuration = int.Parse(key.GetValue("MixDuration", "5000").ToString());
                    numMixDuration.Value = Math.Max(100, Math.Min(15000, mixDuration));

                    int hourlySep = int.Parse(key.GetValue("HourlySeparation", "3").ToString());
                    numHourlySeparation.Value = Math.Max(1, Math.Min(24, hourlySep));

                    int artistSep = int.Parse(key.GetValue("ArtistSeparation", "2").ToString());
                    numArtistSeparation.Value = Math.Max(1, Math.Min(24, artistSep));

                    chkAutoStart.Checked = Convert.ToBoolean(key.GetValue("AutoStartMode", 0));
                    chkShowWhatsApp.Checked = Convert.ToBoolean(key.GetValue("ShowWhatsApp", 1));

                    txtDatabasePath.Text = key.GetValue("DatabasePath", @"C:\AirDirector\Database").ToString();
                    txtDriveX.Text = key.GetValue("DriveX", "").ToString();
                    txtDriveY.Text = key.GetValue("DriveY", "").ToString();
                    txtDriveZ.Text = key.GetValue("DriveZ", "").ToString();
                    txtTimeSignalPath.Text = key.GetValue("TimeSignalPath", "").ToString();

                    string metadataSource = key.GetValue("MetadataSource", "MusicOnly").ToString();
                    rbMusicOnly.Checked = metadataSource == "MusicOnly";
                    rbMusicAndClips.Checked = metadataSource == "MusicAndClips";

                    txtRdsPath.Text = key.GetValue("RdsFilePath", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "rds.txt")).ToString();
                    chkSaveRds.Checked = Convert.ToBoolean(key.GetValue("SaveRdsFile", 0));
                    chkSendToEncoders.Checked = Convert.ToBoolean(key.GetValue("SendMetadataToEncoders", 0));

                    txtBackupPath.Text = key.GetValue("BackupPath", Path.Combine(GetDatabasePath(), "Backups")).ToString();

                    string backupTime = key.GetValue("BackupTime", "01:00").ToString();
                    if (TimeSpan.TryParse(backupTime, out TimeSpan time))
                    {
                        dtpBackupTime.Value = DateTime.Today.Add(time);
                    }

                    // Solo NDI supportato per ora
                    cmbVideoOutputType.SelectedIndex = 0; // Sempre NDI

                    int videoFrameRate = int.Parse(key.GetValue("VideoFrameRate", "25").ToString());
                    cmbVideoFrameRate.SelectedIndex = videoFrameRate == 30 ? 1 : (videoFrameRate == 50 ? 2 : (videoFrameRate == 60 ? 3 : 0));

                    txtBufferVideoPath.Text = key.GetValue("BufferVideoPath", "").ToString();

                    string bufferMode = key.GetValue("BufferVideoMode", "RandomLoop").ToString();
                    cmbBufferMode.SelectedIndex = bufferMode == "Slideshow" ? 1 : 0;

                    txtNDISourceName.Text = key.GetValue("NDI_SourceName", "AirDirector Output").ToString();

                    // ADV Lanner
                    string advLanner = key.GetValue("AdvLannerPlayout", "YesInternal").ToString();
                    cmbAdvLanner.SelectedIndex = advLanner == "NoExternal" ? 1 : 0;

                    // Forza selezione "(Crea Nuova Sorgente NDI)" se modalità RadioTV
                    if (cmbMode.SelectedIndex == 1 && cmbVideoOutputType.SelectedIndex == 0)
                    {
                        if (cmbNDISource.Items.Count > 0)
                        {
                            cmbNDISource.SelectedIndex = 0; // "(Crea Nuova Sorgente NDI)"
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(LanguageManager.GetString("Configuration.Error.LoadFailed", "Errore caricamento configurazione:\n{0}"), ex.Message),
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void SetComboSelection(ComboBox combo, string value)
        {
            for (int i = 0; i < combo.Items.Count; i++)
            {
                if (combo.Items[i].ToString() == value)
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }
            combo.SelectedIndex = 0;
        }

        private void btnBrowseLogo_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = LanguageManager.GetString("Configuration.Dialog.LogoFilter", "Immagini PNG|*.png|Tutti i file|*.*");
                ofd.Title = LanguageManager.GetString("Configuration.Dialog.LogoTitle", "Seleziona Logo (consigliato 120x70 px)");
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtLogoPath.Text = ofd.FileName;
                }
            }
        }

        private void btnBrowseBufferVideo_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = LanguageManager.GetString("Configuration.Dialog.BufferVideoDescription", "Seleziona cartella Video Tampone");
                fbd.SelectedPath = txtBufferVideoPath.Text;

                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtBufferVideoPath.Text = fbd.SelectedPath;
                }
            }
        }

        private void btnRefreshNDI_Click(object sender, EventArgs e)
        {
            Console.WriteLine("");
            Console.WriteLine("========================================");
            Console.WriteLine("[NDI] 🔄 REFRESH MANUALE RICHIESTO");
            Console.WriteLine("========================================");

            LoadNDIDevices();

            MessageBox.Show(
                LanguageManager.GetString("Configuration. NDI. Refreshed", "✅ Aggiornamento completato!\n\nControlla la console per i dettagli."),
                LanguageManager.GetString("Common.Info", "Info"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void btnBrowseDatabase_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = LanguageManager.GetString("Configuration.Dialog. DatabaseDescription", "Seleziona cartella Database");
                fbd.SelectedPath = txtDatabasePath.Text;

                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtDatabasePath.Text = fbd.SelectedPath;
                }
            }
        }

        private void btnBrowseDriveX_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = LanguageManager.GetString("Configuration. Dialog.DriveXDescription", "Seleziona percorso Drive X (Database Condiviso)");
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtDriveX.Text = fbd.SelectedPath;
                }
            }
        }

        private void btnBrowseDriveY_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = LanguageManager.GetString("Configuration.Dialog.DriveYDescription", "Seleziona percorso Drive Y (Archivio Commercial)");
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtDriveY.Text = fbd.SelectedPath;
                }
            }
        }

        private void btnBrowseDriveZ_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = LanguageManager.GetString("Configuration.Dialog.DriveZDescription", "Seleziona percorso Drive Z (Archivio Musicale)");
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtDriveZ.Text = fbd.SelectedPath;
                }
            }
        }

        private void btnBrowseTimeSignal_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = LanguageManager.GetString("Configuration.Dialog. TimeSignalDescription", "Seleziona cartella Segnale Orario");
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtTimeSignalPath.Text = fbd.SelectedPath;
                }
            }
        }

        private void btnApplyDriveX_Click(object sender, EventArgs e)
        {
            ApplyNetworkDrive("X:", txtDriveX.Text);
        }

        private void btnApplyDriveY_Click(object sender, EventArgs e)
        {
            ApplyNetworkDrive("Y:", txtDriveY.Text);
        }

        private void btnApplyDriveZ_Click(object sender, EventArgs e)
        {
            ApplyNetworkDrive("Z:", txtDriveZ.Text);
        }

        private void ApplyNetworkDrive(string driveLetter, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                MessageBox.Show(
                    string.Format(LanguageManager.GetString("Configuration.Drive.EnterValidPath", "Inserisci un percorso valido per il drive {0}"), driveLetter),
                    LanguageManager.GetString("Common.Warning", "Attenzione"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                bool isLocalPath = path.Length >= 2 && path[1] == ':';
                bool isNetworkPath = path.StartsWith("\\\\");

                if (isLocalPath)
                {
                    if (!System.IO.Directory.Exists(path))
                    {
                        MessageBox.Show(
                            string.Format(LanguageManager.GetString("Configuration.Drive.PathNotExists", "Il percorso locale '{0}' non esiste! "), path),
                            LanguageManager.GetString("Configuration.Drive.InvalidPath", "Percorso Non Valido"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }

                    System.Diagnostics.Process processDelete = new System.Diagnostics.Process();
                    processDelete.StartInfo.FileName = "subst";
                    processDelete.StartInfo.Arguments = $"{driveLetter} /D";
                    processDelete.StartInfo.CreateNoWindow = true;
                    processDelete.StartInfo.UseShellExecute = false;
                    processDelete.Start();
                    processDelete.WaitForExit();

                    System.Diagnostics.Process processMap = new System.Diagnostics.Process();
                    processMap.StartInfo.FileName = "subst";
                    processMap.StartInfo.Arguments = $"{driveLetter} \"{path}\"";
                    processMap.StartInfo.CreateNoWindow = true;
                    processMap.StartInfo.UseShellExecute = false;
                    processMap.StartInfo.RedirectStandardError = true;
                    processMap.Start();

                    string error = processMap.StandardError.ReadToEnd();
                    processMap.WaitForExit();

                    if (processMap.ExitCode == 0 || string.IsNullOrEmpty(error))
                    {
                        try
                        {
                            string registryPath = @"SOFTWARE\AirDirector\DriveMappings";
                            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(registryPath))
                            {
                                if (key != null)
                                {
                                    key.SetValue(driveLetter, path, RegistryValueKind.String);
                                }
                            }

                            CreateStartupBatchScript();
                            RegisterStartupScript();

                            MessageBox.Show(
                                string.Format(LanguageManager.GetString("Configuration.Drive.LocalMappedSuccess", "✅ Drive {0} mappato localmente su:\n{1}\n\n🔄 La mappatura sarà ripristinata automaticamente all'avvio di Windows.\n\n⚠️ NOTA: Il drive virtuale verrà ricreato al prossimo login."), driveLetter, path),
                                LanguageManager.GetString("Configuration.Drive.PersistentConfigured", "Mappatura Persistente Configurata"),
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }
                        catch (Exception regEx)
                        {
                            MessageBox.Show(
                                string.Format(LanguageManager.GetString("Configuration.Drive.MappedNonPersistent", "⚠️ Drive {0} mappato su:\n{1}\n\n❌ Errore configurazione persistenza:\n{2}"), driveLetter, path, regEx.Message),
                                LanguageManager.GetString("Configuration.Drive.NonPersistentMapping", "Mappatura Creata (Non Persistente)"),
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                        }
                    }
                    else
                    {
                        MessageBox.Show(
                            string.Format(LanguageManager.GetString("Configuration.Drive.MappingError", "❌ Errore durante la mappatura:\n\n{0}"), error),
                            LanguageManager.GetString("Configuration.Drive.ErrorTitle", "Errore Mappatura"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
                else if (isNetworkPath)
                {
                    System.Diagnostics.Process processDelete = new System.Diagnostics.Process();
                    processDelete.StartInfo.FileName = "net";
                    processDelete.StartInfo.Arguments = $"use {driveLetter} /delete /yes";
                    processDelete.StartInfo.CreateNoWindow = true;
                    processDelete.StartInfo.UseShellExecute = false;
                    processDelete.Start();
                    processDelete.WaitForExit();

                    System.Diagnostics.Process processMap = new System.Diagnostics.Process();
                    processMap.StartInfo.FileName = "net";
                    processMap.StartInfo.Arguments = $"use {driveLetter} \"{path}\" /persistent:yes";
                    processMap.StartInfo.CreateNoWindow = true;
                    processMap.StartInfo.UseShellExecute = false;
                    processMap.StartInfo.RedirectStandardOutput = true;
                    processMap.StartInfo.RedirectStandardError = true;
                    processMap.Start();

                    string output = processMap.StandardOutput.ReadToEnd();
                    string error = processMap.StandardError.ReadToEnd();
                    processMap.WaitForExit();

                    if (processMap.ExitCode == 0)
                    {
                        MessageBox.Show(
                            string.Format(LanguageManager.GetString("Configuration.Drive.NetworkMappedSuccess", "✅ Drive {0} mappato in rete su:\n{1}\n\n🔒 La mappatura è PERSISTENTE e rimarrà dopo il riavvio. "), driveLetter, path),
                            LanguageManager.GetString("Configuration.Drive.NetworkMappingCreated", "Mappatura di Rete Creata"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show(
                            string.Format(LanguageManager.GetString("Configuration.Drive. NetworkMappingError", "❌ Errore durante la mappatura di rete:\n\n{0}\n\nVerifica che:\n- Il percorso di rete sia raggiungibile\n- Hai i permessi necessari\n- Il formato sia corretto (es. \\\\server\\cartella)"), error),
                            LanguageManager.GetString("Configuration.Drive.ErrorTitle", "Errore Mappatura"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show(
                        LanguageManager.GetString("Configuration.Drive.InvalidFormat", "❌ Formato percorso non valido!\n\nFormati supportati:\n• Percorso locale:  C:\\Cartella\\Sottocartella\n• Percorso di rete: \\\\server\\cartella"),
                        LanguageManager.GetString("Configuration.Drive.InvalidFormatTitle", "Formato Non Valido"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(LanguageManager.GetString("Configuration.Drive.GeneralError", "❌ Errore durante la mappatura:\n{0}"), ex.Message),
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void CreateStartupBatchScript()
        {
            try
            {
                string scriptPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "AirDirector", "RestoreDrives.bat");

                string scriptDir = Path.GetDirectoryName(scriptPath);
                if (!Directory.Exists(scriptDir))
                {
                    Directory.CreateDirectory(scriptDir);
                }

                string registryPath = @"SOFTWARE\AirDirector\DriveMappings";
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(registryPath))
                {
                    if (key != null)
                    {
                        using (StreamWriter writer = new StreamWriter(scriptPath, false))
                        {
                            writer.WriteLine("@echo off");
                            writer.WriteLine("REM AirDirector - Restore Drive Mappings");
                            writer.WriteLine();

                            foreach (string valueName in key.GetValueNames())
                            {
                                string path = key.GetValue(valueName)?.ToString();
                                if (!string.IsNullOrEmpty(path))
                                {
                                    writer.WriteLine($"subst {valueName} /D >nul 2>&1");
                                    writer.WriteLine($"subst {valueName} \"{path}\"");
                                }
                            }

                            writer.WriteLine();
                            writer.WriteLine("exit");
                        }
                    }
                }
            }
            catch { }
        }

        private void RegisterStartupScript()
        {
            try
            {
                string scriptPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "AirDirector", "RestoreDrives.bat");

                string registryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(registryPath, true))
                {
                    if (key != null)
                    {
                        if (key.GetValue("AirDirectorDrives") != null)
                        {
                            key.DeleteValue("AirDirectorDrives", false);
                        }

                        string command = $"cmd. exe /c start /min \"\" \"{scriptPath}\"";
                        key.SetValue("AirDirectorDrives", command, RegistryValueKind.String);
                    }
                }
            }
            catch { }
        }

        public static void RestoreDriveMappingsOnStartup()
        {
            try
            {
                string registryPath = @"SOFTWARE\AirDirector\DriveMappings";
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(registryPath))
                {
                    if (key != null)
                    {
                        foreach (string driveLetter in key.GetValueNames())
                        {
                            string path = key.GetValue(driveLetter)?.ToString();
                            if (!string.IsNullOrEmpty(path) && System.IO.Directory.Exists(path))
                            {
                                System.Diagnostics.Process process = new System.Diagnostics.Process();
                                process.StartInfo.FileName = "subst";
                                process.StartInfo.Arguments = $"{driveLetter} \"{path}\"";
                                process.StartInfo.CreateNoWindow = true;
                                process.StartInfo.UseShellExecute = false;
                                process.Start();
                                process.WaitForExit();
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void btnBrowseRds_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = LanguageManager.GetString("Configuration.Dialog.RdsFilter", "File di testo|*.txt");
                sfd.FileName = "rds. txt";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    txtRdsPath.Text = sfd.FileName;
                }
            }
        }

        private void btnBrowseBackup_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = LanguageManager.GetString("Configuration.Dialog.BackupDescription", "Seleziona cartella backup");
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtBackupPath.Text = fbd.SelectedPath;
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                string oldDatabasePath = GetDatabasePath();
                bool databasePathChanged = txtDatabasePath.Text != oldDatabasePath;
                string oldMode = DbcManager.GetConfigValue("Mode", "Radio");
                string newMode = cmbMode.SelectedIndex == 0 ? "Radio" : "RadioTV";
                bool modeChanged = oldMode != newMode;

                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(REGISTRY_KEY))
                {
                    key.SetValue("StationName", txtStationName.Text);
                    key.SetValue("LogoPath", txtLogoPath.Text);

                    key.SetValue("MainOutput", cmbMainOutput.SelectedItem?.ToString() ?? "(Default System Device)");
                    key.SetValue("PreviewOutput", cmbPreviewOutput.SelectedItem?.ToString() ?? "(Default System Device)");
                    key.SetValue("PaletteOutput", cmbPaletteOutput.SelectedItem?.ToString() ?? "(Default System Device)");

                    key.SetValue("MixDuration", ((int)numMixDuration.Value).ToString());
                    key.SetValue("HourlySeparation", ((int)numHourlySeparation.Value).ToString());
                    key.SetValue("ArtistSeparation", ((int)numArtistSeparation.Value).ToString());

                    key.SetValue("AutoStartMode", chkAutoStart.Checked ? 1 : 0);
                    key.SetValue("ShowWhatsApp", chkShowWhatsApp.Checked ? 1 : 0);

                    key.SetValue("DatabasePath", txtDatabasePath.Text);
                    key.SetValue("DriveX", txtDriveX.Text);
                    key.SetValue("DriveY", txtDriveY.Text);
                    key.SetValue("DriveZ", txtDriveZ.Text);
                    key.SetValue("TimeSignalPath", txtTimeSignalPath.Text);

                    DbcManager.SetConfigValue("Mode", newMode);

                    string selectedLanguage = cmbLanguage.SelectedItem.ToString();
                    string oldLanguage = LanguageManager.GetCurrentLanguage();

                    DbcManager.SetConfigValue("Language", selectedLanguage);
                    key.SetValue("Language", selectedLanguage);

                    if (selectedLanguage != oldLanguage)
                    {
                        LanguageManager.SetLanguage(selectedLanguage);
                    }

                    key.SetValue("MetadataSource", rbMusicOnly.Checked ? "MusicOnly" : "MusicAndClips");
                    key.SetValue("RdsFilePath", txtRdsPath.Text);
                    key.SetValue("SaveRdsFile", chkSaveRds.Checked ? 1 : 0);
                    key.SetValue("SendMetadataToEncoders", chkSendToEncoders.Checked ? 1 : 0);

                    key.SetValue("BackupPath", txtBackupPath.Text);
                    key.SetValue("BackupTime", dtpBackupTime.Value.ToString("HH:mm"));

                    if (newMode == "RadioTV")
                    {
                        // Solo NDI supportato
                        key.SetValue("VideoOutputType", "NDI");

                        int videoFrameRate = cmbVideoFrameRate.SelectedIndex == 1 ? 30 :
                                            (cmbVideoFrameRate.SelectedIndex == 2 ? 50 :
                                            (cmbVideoFrameRate.SelectedIndex == 3 ? 60 : 25));
                        key.SetValue("VideoFrameRate", videoFrameRate.ToString());

                        key.SetValue("BufferVideoPath", txtBufferVideoPath.Text);

                        string bufferMode = cmbBufferMode.SelectedIndex == 1 ? "Slideshow" : "RandomLoop";
                        key.SetValue("BufferVideoMode", bufferMode);

                        key.SetValue("NDI_SourceName", txtNDISourceName.Text);

                        // ADV Lanner
                        string advLanner = cmbAdvLanner.SelectedIndex == 1 ? "NoExternal" : "YesInternal";
                        key.SetValue("AdvLannerPlayout", advLanner);
                    }
                }

                MessageBox.Show(
                    LanguageManager.GetString("Configuration.SaveSuccess", "✅ Configurazione salvata con successo! "),
                    LanguageManager.GetString("Common.Success", "Successo"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                if (databasePathChanged || modeChanged)
                {
                    var result = MessageBox.Show(
                        LanguageManager.GetString("Configuration.RestartRequired", "⚠️ " + (modeChanged ? "La modalità operativa è stata modificata.\n\n" : "Il percorso del database è stato modificato.\n\n") + "È necessario riavviare il software per applicare le modifiche.\n\nVuoi riavviare ora?"),
                        LanguageManager.GetString("Configuration.RestartTitle", "Riavvio Richiesto"),
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (result == DialogResult.Yes)
                    {
                        Application.Restart();
                        Environment.Exit(0);
                    }
                }
                else
                {
                    ConfigurationChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(LanguageManager.GetString("Configuration.Error. SaveFailed", "❌ Errore salvataggio:\n{0}"), ex.Message),
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadAudioDevices(cmbMainOutput);
            LoadAudioDevices(cmbPreviewOutput);
            LoadAudioDevices(cmbPaletteOutput);

            MessageBox.Show(
                LanguageManager.GetString("Configuration. DevicesRefreshed", "✅ Dispositivi audio aggiornati! "),
                LanguageManager.GetString("Common.Info", "Info"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                LanguageManager.SaveMissingKeysToFile();
                LanguageManager.LanguageChanged -= (s, e) => ApplyLanguage();
            }

            base.Dispose(disposing);
        }

        // STATIC GETTERS
        public static string GetStationName() { try { using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY)) { return key?.GetValue("StationName", "AirDirector Radio").ToString() ?? "AirDirector Radio"; } } catch { return "AirDirector Radio"; } }
        public static string GetLogoPath() { try { using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY)) { return key?.GetValue("LogoPath", "").ToString() ?? ""; } } catch { return ""; } }
        public static int GetMixDuration() { try { using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY)) { int value = int.Parse(key?.GetValue("MixDuration", "5000").ToString() ?? "5000"); return Math.Max(100, Math.Min(15000, value)); } } catch { return 5000; } }
        public static int GetHourlySeparation() { try { using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY)) { int value = int.Parse(key?.GetValue("HourlySeparation", "3").ToString() ?? "3"); return Math.Max(1, Math.Min(24, value)); } } catch { return 3; } }
        public static int GetArtistSeparation() { try { using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY)) { int value = int.Parse(key?.GetValue("ArtistSeparation", "2").ToString() ?? "2"); return Math.Max(1, Math.Min(24, value)); } } catch { return 2; } }
        public static bool GetAutoStartMode() { try { using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY)) { return Convert.ToBoolean(key?.GetValue("AutoStartMode", 0) ?? 0); } } catch { return false; } }
        public static bool GetShowWhatsApp() { try { using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY)) { return Convert.ToBoolean(key?.GetValue("ShowWhatsApp", 1) ?? 1); } } catch { return true; } }
        public static string GetDatabasePath() { try { using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY)) { return key?.GetValue("DatabasePath", @"C:\AirDirector\Database").ToString() ?? @"C:\AirDirector\Database"; } } catch { return @"C:\AirDirector\Database"; } }
        public static string GetDriveX() { try { using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY)) { return key?.GetValue("DriveX", "").ToString() ?? ""; } } catch { return ""; } }
        public static string GetDriveY() { try { using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY)) { return key?.GetValue("DriveY", "").ToString() ?? ""; } } catch { return ""; } }
        public static string GetDriveZ() { try { using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY)) { return key?.GetValue("DriveZ", "").ToString() ?? ""; } } catch { return ""; } }
        public static string GetTimeSignalPath() { try { using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY)) { return key?.GetValue("TimeSignalPath", "").ToString() ?? ""; } } catch { return ""; } }
        public static int GetMainOutputDeviceNumber() => GetDeviceNumber("MainOutput");
        public static int GetPreviewOutputDeviceNumber() => GetDeviceNumber("PreviewOutput");
        public static int GetPaletteOutputDeviceNumber() => GetDeviceNumber("PaletteOutput");
        private static int GetDeviceNumber(string keyName) { try { using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY)) { if (key != null) { string device = key.GetValue(keyName, "(Default System Device)").ToString(); if (device == "(Default System Device)") return -1; int colonIndex = device.IndexOf(':'); if (colonIndex > 0 && int.TryParse(device.Substring(0, colonIndex), out int deviceNumber)) { return deviceNumber; } } } } catch { } return -1; }
        public static string GetMetadataSource() { try { using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY)) { return key?.GetValue("MetadataSource", "MusicOnly").ToString() ?? "MusicOnly"; } } catch { return "MusicOnly"; } }
        public static string GetRdsFilePath() { try { using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY)) { return key?.GetValue("RdsFilePath", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "rds.txt")).ToString(); } } catch { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "rds.txt"); } }
        public static bool IsSaveRdsEnabled() { try { using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY)) { return Convert.ToBoolean(key?.GetValue("SaveRdsFile", 0) ?? 0); } } catch { return false; } }
        public static bool IsSendMetadataToEncodersEnabled() { try { using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY)) { return Convert.ToBoolean(key?.GetValue("SendMetadataToEncoders", 0) ?? 0); } } catch { return false; } }
        public static string GetBackupPath() { try { using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY)) { return key?.GetValue("BackupPath", Path.Combine(GetDatabasePath(), "Backups")).ToString(); } } catch { return Path.Combine(GetDatabasePath(), "Backups"); } }
        public static string GetBackupTime() { try { using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY)) { return key?.GetValue("BackupTime", "01:00").ToString() ?? "01:00"; } } catch { return "01:00"; } }

        // VIDEO GETTERS
        public static bool IsRadioTVMode() { try { return DbcManager.GetConfigValue("Mode", "Radio") == "RadioTV"; } catch { return false; } }
        public static string GetVideoOutputType() { try { return "NDI"; } catch { return "NDI"; } } // Solo NDI per ora
        public static int GetVideoFrameRate() { try { using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY)) { return int.Parse(key?.GetValue("VideoFrameRate", "25").ToString() ?? "25"); } } catch { return 25; } }
        public static string GetBufferVideoPath() { try { using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY)) { return key?.GetValue("BufferVideoPath", "").ToString() ?? ""; } } catch { return ""; } }
        public static string GetBufferVideoMode() { try { using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY)) { return key?.GetValue("BufferVideoMode", "RandomLoop").ToString() ?? "RandomLoop"; } } catch { return "RandomLoop"; } }
        public static string GetNDISourceName() { try { using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY)) { return key?.GetValue("NDI_SourceName", "AirDirector Output").ToString() ?? "AirDirector Output"; } } catch { return "AirDirector Output"; } }
        public static string GetSDIDeviceName() { try { using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY)) { return key?.GetValue("SDI_DeviceName", "").ToString() ?? ""; } } catch { return ""; } }
        public static string GetAdvLannerPlayout() { try { using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY)) { return key?.GetValue("AdvLannerPlayout", "YesInternal").ToString() ?? "YesInternal"; } } catch { return "YesInternal"; } }
        public static bool IsAdvLannerInternal() { return GetAdvLannerPlayout() == "YesInternal"; }

        private void lblLogoPath_Click(object sender, EventArgs e) { }
    }
}