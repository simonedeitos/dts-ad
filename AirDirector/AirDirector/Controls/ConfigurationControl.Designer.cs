using NAudio.Lame;
using System.Windows.Markup;

namespace AirDirector.Controls
{
    partial class ConfigurationControl
    {
        private System.ComponentModel.IContainer components = null;

        #region Component Designer generated code

        private void InitializeComponent()
        {
            tabControl = new TabControl();
            tabGeneral = new TabPage();
            chkShowWhatsApp = new CheckBox();
            btnSaveGeneral = new Button();
            numArtistSeparation = new NumericUpDown();
            lblArtistSeparation = new Label();
            numHourlySeparation = new NumericUpDown();
            lblHourlySeparation = new Label();
            numMixDuration = new NumericUpDown();
            lblMixDuration = new Label();
            cmbLanguage = new ComboBox();
            lblLanguage = new Label();
            cmbMode = new ComboBox();
            lblMode = new Label();
            chkAutoStart = new CheckBox();
            lblGeneralTitle = new Label();
            tabStation = new TabPage();
            btnSaveStation = new Button();
            btnBrowseLogo = new Button();
            txtLogoPath = new TextBox();
            lblLogoPath = new Label();
            txtStationName = new TextBox();
            lblStationName = new Label();
            lblStationTitle = new Label();
            tabAudio = new TabPage();
            btnSaveAudio = new Button();
            btnRefresh = new Button();
            cmbPaletteOutput = new ComboBox();
            lblPaletteOutput = new Label();
            cmbPreviewOutput = new ComboBox();
            lblPreviewOutput = new Label();
            cmbMainOutput = new ComboBox();
            lblMainOutput = new Label();
            lblAudioTitle = new Label();
            tabVideo = new TabPage();
            btnSaveVideo = new Button();
            grpNDI = new GroupBox();
            btnRefreshNDI = new Button();
            cmbNDISource = new ComboBox();
            lblNDISource = new Label();
            txtNDISourceName = new TextBox();
            lblNDISourceName = new Label();
            cmbBufferMode = new ComboBox();
            lblBufferMode = new Label();
            btnBrowseBufferVideo = new Button();
            txtBufferVideoPath = new TextBox();
            lblBufferVideoPath = new Label();
            cmbVideoFrameRate = new ComboBox();
            lblVideoFrameRate = new Label();
            cmbVideoOutputType = new ComboBox();
            lblVideoOutputType = new Label();
            lblVideoTitle = new Label();
            lblAdvLanner = new Label();
            cmbAdvLanner = new ComboBox();
            chkLocalAudioOutput = new CheckBox();
            tabPaths = new TabPage();
            btnBrowseTimeSignal = new Button();
            txtTimeSignalPath = new TextBox();
            lblTimeSignalPath = new Label();
            btnSavePaths = new Button();
            btnApplyDriveZ = new Button();
            btnBrowseDriveZ = new Button();
            txtDriveZ = new TextBox();
            lblDriveZ = new Label();
            btnApplyDriveY = new Button();
            btnBrowseDriveY = new Button();
            txtDriveY = new TextBox();
            lblDriveY = new Label();
            btnApplyDriveX = new Button();
            btnBrowseDriveX = new Button();
            txtDriveX = new TextBox();
            lblDriveX = new Label();
            btnBrowseDatabase = new Button();
            txtDatabasePath = new TextBox();
            lblDatabasePath = new Label();
            lblPathsTitle = new Label();
            tabMetadata = new TabPage();
            btnSaveMetadata = new Button();
            chkSendToEncoders = new CheckBox();
            chkSaveRds = new CheckBox();
            btnBrowseRds = new Button();
            txtRdsPath = new TextBox();
            lblRdsPath = new Label();
            rbMusicAndClips = new RadioButton();
            rbMusicOnly = new RadioButton();
            lblMetadataSource = new Label();
            lblMetadataTitle = new Label();
            tabBackup = new TabPage();
            btnSaveBackup = new Button();
            dtpBackupTime = new DateTimePicker();
            lblBackupTime = new Label();
            btnBrowseBackup = new Button();
            txtBackupPath = new TextBox();
            lblBackupPath = new Label();
            lblBackupTitle = new Label();
            tabControl.SuspendLayout();
            tabGeneral.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numArtistSeparation).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numHourlySeparation).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numMixDuration).BeginInit();
            tabStation.SuspendLayout();
            tabAudio.SuspendLayout();
            tabVideo.SuspendLayout();
            grpNDI.SuspendLayout();
            tabPaths.SuspendLayout();
            tabMetadata.SuspendLayout();
            tabBackup.SuspendLayout();
            SuspendLayout();
            // 
            // tabControl
            // 
            tabControl.Controls.Add(tabGeneral);
            tabControl.Controls.Add(tabStation);
            tabControl.Controls.Add(tabAudio);
            tabControl.Controls.Add(tabPaths);
            tabControl.Controls.Add(tabMetadata);
            tabControl.Controls.Add(tabBackup);
            tabControl.Dock = DockStyle.Fill;
            tabControl.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            tabControl.Location = new Point(0, 0);
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new Size(1000, 700);
            tabControl.TabIndex = 0;
            // 
            // tabGeneral
            // 
            tabGeneral.BackColor = Color.FromArgb(240, 240, 240);
            tabGeneral.Controls.Add(chkShowWhatsApp);
            tabGeneral.Controls.Add(btnSaveGeneral);
            tabGeneral.Controls.Add(numArtistSeparation);
            tabGeneral.Controls.Add(lblArtistSeparation);
            tabGeneral.Controls.Add(numHourlySeparation);
            tabGeneral.Controls.Add(lblHourlySeparation);
            tabGeneral.Controls.Add(numMixDuration);
            tabGeneral.Controls.Add(lblMixDuration);
            tabGeneral.Controls.Add(cmbLanguage);
            tabGeneral.Controls.Add(lblLanguage);
            tabGeneral.Controls.Add(cmbMode);
            tabGeneral.Controls.Add(lblMode);
            tabGeneral.Controls.Add(chkAutoStart);
            tabGeneral.Controls.Add(lblGeneralTitle);
            tabGeneral.Location = new Point(4, 26);
            tabGeneral.Name = "tabGeneral";
            tabGeneral.Padding = new Padding(20);
            tabGeneral.Size = new Size(992, 670);
            tabGeneral.TabIndex = 0;
            tabGeneral.Text = "⚙️ Generale";
            // 
            // chkShowWhatsApp
            // 
            chkShowWhatsApp.AutoSize = true;
            chkShowWhatsApp.Checked = true;
            chkShowWhatsApp.CheckState = CheckState.Checked;
            chkShowWhatsApp.Font = new Font("Segoe UI", 9F);
            chkShowWhatsApp.Location = new Point(40, 315);
            chkShowWhatsApp.Name = "chkShowWhatsApp";
            chkShowWhatsApp.Size = new Size(298, 19);
            chkShowWhatsApp.TabIndex = 13;
            chkShowWhatsApp.Text = "💬 Mostra WhatsApp (la modifica richiede il riavvio)";
            chkShowWhatsApp.UseVisualStyleBackColor = true;
            // 
            // btnSaveGeneral
            // 
            btnSaveGeneral.BackColor = Color.FromArgb(40, 167, 69);
            btnSaveGeneral.FlatAppearance.BorderSize = 0;
            btnSaveGeneral.FlatStyle = FlatStyle.Flat;
            btnSaveGeneral.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnSaveGeneral.ForeColor = Color.White;
            btnSaveGeneral.Location = new Point(40, 350);
            btnSaveGeneral.Name = "btnSaveGeneral";
            btnSaveGeneral.Size = new Size(200, 40);
            btnSaveGeneral.TabIndex = 12;
            btnSaveGeneral.Text = "💾 Salva";
            btnSaveGeneral.UseVisualStyleBackColor = false;
            btnSaveGeneral.Click += btnSave_Click;
            // 
            // numArtistSeparation
            // 
            numArtistSeparation.Font = new Font("Segoe UI", 9F);
            numArtistSeparation.Location = new Point(280, 250);
            numArtistSeparation.Maximum = new decimal(new int[] { 24, 0, 0, 0 });
            numArtistSeparation.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numArtistSeparation.Name = "numArtistSeparation";
            numArtistSeparation.Size = new Size(80, 23);
            numArtistSeparation.TabIndex = 11;
            numArtistSeparation.Value = new decimal(new int[] { 2, 0, 0, 0 });
            // 
            // lblArtistSeparation
            // 
            lblArtistSeparation.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblArtistSeparation.Location = new Point(40, 250);
            lblArtistSeparation.Name = "lblArtistSeparation";
            lblArtistSeparation.Size = new Size(230, 22);
            lblArtistSeparation.TabIndex = 10;
            lblArtistSeparation.Text = "Ore Separazione Artista:";
            lblArtistSeparation.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // numHourlySeparation
            // 
            numHourlySeparation.Font = new Font("Segoe UI", 9F);
            numHourlySeparation.Location = new Point(280, 210);
            numHourlySeparation.Maximum = new decimal(new int[] { 24, 0, 0, 0 });
            numHourlySeparation.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numHourlySeparation.Name = "numHourlySeparation";
            numHourlySeparation.Size = new Size(80, 23);
            numHourlySeparation.TabIndex = 9;
            numHourlySeparation.Value = new decimal(new int[] { 3, 0, 0, 0 });
            // 
            // lblHourlySeparation
            // 
            lblHourlySeparation.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblHourlySeparation.Location = new Point(40, 210);
            lblHourlySeparation.Name = "lblHourlySeparation";
            lblHourlySeparation.Size = new Size(230, 22);
            lblHourlySeparation.TabIndex = 8;
            lblHourlySeparation.Text = "Ore Separazione Brano:";
            lblHourlySeparation.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // numMixDuration
            // 
            numMixDuration.Font = new Font("Segoe UI", 9F);
            numMixDuration.Increment = new decimal(new int[] { 100, 0, 0, 0 });
            numMixDuration.Location = new Point(280, 170);
            numMixDuration.Maximum = new decimal(new int[] { 15000, 0, 0, 0 });
            numMixDuration.Minimum = new decimal(new int[] { 100, 0, 0, 0 });
            numMixDuration.Name = "numMixDuration";
            numMixDuration.Size = new Size(120, 23);
            numMixDuration.TabIndex = 7;
            numMixDuration.Value = new decimal(new int[] { 5000, 0, 0, 0 });
            // 
            // lblMixDuration
            // 
            lblMixDuration.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblMixDuration.Location = new Point(40, 170);
            lblMixDuration.Name = "lblMixDuration";
            lblMixDuration.Size = new Size(230, 22);
            lblMixDuration.TabIndex = 6;
            lblMixDuration.Text = "Durata Mix (ms):";
            lblMixDuration.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // cmbLanguage
            // 
            cmbLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbLanguage.Font = new Font("Segoe UI", 9F);
            cmbLanguage.FormattingEnabled = true;
            cmbLanguage.Location = new Point(280, 130);
            cmbLanguage.Name = "cmbLanguage";
            cmbLanguage.Size = new Size(200, 23);
            cmbLanguage.TabIndex = 5;
            // 
            // lblLanguage
            // 
            lblLanguage.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblLanguage.Location = new Point(40, 130);
            lblLanguage.Name = "lblLanguage";
            lblLanguage.Size = new Size(230, 22);
            lblLanguage.TabIndex = 4;
            lblLanguage.Text = "Lingua:";
            lblLanguage.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // cmbMode
            // 
            cmbMode.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbMode.Font = new Font("Segoe UI", 9F);
            cmbMode.FormattingEnabled = true;
            cmbMode.Location = new Point(280, 90);
            cmbMode.Name = "cmbMode";
            cmbMode.Size = new Size(200, 23);
            cmbMode.TabIndex = 3;
            // 
            // lblMode
            // 
            lblMode.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblMode.Location = new Point(40, 90);
            lblMode.Name = "lblMode";
            lblMode.Size = new Size(230, 22);
            lblMode.TabIndex = 2;
            lblMode.Text = "Modalità:";
            lblMode.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // chkAutoStart
            // 
            chkAutoStart.AutoSize = true;
            chkAutoStart.Font = new Font("Segoe UI", 9F);
            chkAutoStart.Location = new Point(40, 290);
            chkAutoStart.Name = "chkAutoStart";
            chkAutoStart.Size = new Size(248, 19);
            chkAutoStart.TabIndex = 1;
            chkAutoStart.Text = "▶ Avvia automaticamente player in AUTO";
            chkAutoStart.UseVisualStyleBackColor = true;
            // 
            // lblGeneralTitle
            // 
            lblGeneralTitle.AutoSize = true;
            lblGeneralTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblGeneralTitle.ForeColor = Color.FromArgb(0, 120, 215);
            lblGeneralTitle.Location = new Point(40, 40);
            lblGeneralTitle.Name = "lblGeneralTitle";
            lblGeneralTitle.Size = new Size(271, 25);
            lblGeneralTitle.TabIndex = 0;
            lblGeneralTitle.Text = "⚙️ IMPOSTAZIONI GENERALI";
            // 
            // tabStation
            // 
            tabStation.BackColor = Color.FromArgb(240, 240, 240);
            tabStation.Controls.Add(btnSaveStation);
            tabStation.Controls.Add(btnBrowseLogo);
            tabStation.Controls.Add(txtLogoPath);
            tabStation.Controls.Add(lblLogoPath);
            tabStation.Controls.Add(txtStationName);
            tabStation.Controls.Add(lblStationName);
            tabStation.Controls.Add(lblStationTitle);
            tabStation.Location = new Point(4, 26);
            tabStation.Name = "tabStation";
            tabStation.Padding = new Padding(20);
            tabStation.Size = new Size(992, 670);
            tabStation.TabIndex = 1;
            tabStation.Text = "📻 Stazione";
            // 
            // btnSaveStation
            // 
            btnSaveStation.BackColor = Color.FromArgb(40, 167, 69);
            btnSaveStation.FlatAppearance.BorderSize = 0;
            btnSaveStation.FlatStyle = FlatStyle.Flat;
            btnSaveStation.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnSaveStation.ForeColor = Color.White;
            btnSaveStation.Location = new Point(40, 200);
            btnSaveStation.Name = "btnSaveStation";
            btnSaveStation.Size = new Size(200, 40);
            btnSaveStation.TabIndex = 6;
            btnSaveStation.Text = "💾 Salva";
            btnSaveStation.UseVisualStyleBackColor = false;
            btnSaveStation.Click += btnSave_Click;
            // 
            // btnBrowseLogo
            // 
            btnBrowseLogo.BackColor = Color.FromArgb(0, 120, 215);
            btnBrowseLogo.FlatAppearance.BorderSize = 0;
            btnBrowseLogo.FlatStyle = FlatStyle.Flat;
            btnBrowseLogo.Font = new Font("Segoe UI", 10F);
            btnBrowseLogo.ForeColor = Color.White;
            btnBrowseLogo.Location = new Point(639, 128);
            btnBrowseLogo.Name = "btnBrowseLogo";
            btnBrowseLogo.Size = new Size(40, 26);
            btnBrowseLogo.TabIndex = 5;
            btnBrowseLogo.Text = "📁";
            btnBrowseLogo.UseVisualStyleBackColor = false;
            btnBrowseLogo.Click += btnBrowseLogo_Click;
            // 
            // txtLogoPath
            // 
            txtLogoPath.Font = new Font("Segoe UI", 9F);
            txtLogoPath.Location = new Point(243, 130);
            txtLogoPath.Name = "txtLogoPath";
            txtLogoPath.ReadOnly = true;
            txtLogoPath.Size = new Size(390, 23);
            txtLogoPath.TabIndex = 4;
            // 
            // lblLogoPath
            // 
            lblLogoPath.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblLogoPath.Location = new Point(40, 131);
            lblLogoPath.Name = "lblLogoPath";
            lblLogoPath.Size = new Size(147, 22);
            lblLogoPath.TabIndex = 3;
            lblLogoPath.Text = "PNG Logo (120x70 px):";
            lblLogoPath.TextAlign = ContentAlignment.MiddleLeft;
            lblLogoPath.Click += lblLogoPath_Click;
            // 
            // txtStationName
            // 
            txtStationName.Font = new Font("Segoe UI", 9F);
            txtStationName.Location = new Point(243, 89);
            txtStationName.Name = "txtStationName";
            txtStationName.Size = new Size(390, 23);
            txtStationName.TabIndex = 2;
            // 
            // lblStationName
            // 
            lblStationName.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblStationName.Location = new Point(40, 90);
            lblStationName.Name = "lblStationName";
            lblStationName.Size = new Size(147, 22);
            lblStationName.TabIndex = 1;
            lblStationName.Text = "Nome Radio:";
            lblStationName.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblStationTitle
            // 
            lblStationTitle.AutoSize = true;
            lblStationTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblStationTitle.ForeColor = Color.FromArgb(0, 120, 215);
            lblStationTitle.Location = new Point(40, 40);
            lblStationTitle.Name = "lblStationTitle";
            lblStationTitle.Size = new Size(178, 25);
            lblStationTitle.TabIndex = 0;
            lblStationTitle.Text = "📻 DATI STAZIONE";
            // 
            // tabAudio
            // 
            tabAudio.BackColor = Color.FromArgb(240, 240, 240);
            tabAudio.Controls.Add(btnSaveAudio);
            tabAudio.Controls.Add(btnRefresh);
            tabAudio.Controls.Add(cmbPaletteOutput);
            tabAudio.Controls.Add(lblPaletteOutput);
            tabAudio.Controls.Add(cmbPreviewOutput);
            tabAudio.Controls.Add(lblPreviewOutput);
            tabAudio.Controls.Add(cmbMainOutput);
            tabAudio.Controls.Add(lblMainOutput);
            tabAudio.Controls.Add(lblAudioTitle);
            tabAudio.Location = new Point(4, 26);
            tabAudio.Name = "tabAudio";
            tabAudio.Padding = new Padding(20);
            tabAudio.Size = new Size(992, 670);
            tabAudio.TabIndex = 2;
            tabAudio.Text = "🔊 Audio";
            // 
            // btnSaveAudio
            // 
            btnSaveAudio.BackColor = Color.FromArgb(40, 167, 69);
            btnSaveAudio.FlatAppearance.BorderSize = 0;
            btnSaveAudio.FlatStyle = FlatStyle.Flat;
            btnSaveAudio.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnSaveAudio.ForeColor = Color.White;
            btnSaveAudio.Location = new Point(40, 230);
            btnSaveAudio.Name = "btnSaveAudio";
            btnSaveAudio.Size = new Size(200, 40);
            btnSaveAudio.TabIndex = 8;
            btnSaveAudio.Text = "💾 Salva";
            btnSaveAudio.UseVisualStyleBackColor = false;
            btnSaveAudio.Click += btnSave_Click;
            // 
            // btnRefresh
            // 
            btnRefresh.BackColor = Color.FromArgb(0, 120, 215);
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.FlatStyle = FlatStyle.Flat;
            btnRefresh.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnRefresh.ForeColor = Color.White;
            btnRefresh.Location = new Point(484, 43);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new Size(146, 25);
            btnRefresh.TabIndex = 7;
            btnRefresh.Text = "🔄 Aggiorna Dispositivi";
            btnRefresh.UseVisualStyleBackColor = false;
            btnRefresh.Click += btnRefresh_Click;
            // 
            // cmbPaletteOutput
            // 
            cmbPaletteOutput.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPaletteOutput.Font = new Font("Segoe UI", 9F);
            cmbPaletteOutput.FormattingEnabled = true;
            cmbPaletteOutput.Location = new Point(280, 170);
            cmbPaletteOutput.Name = "cmbPaletteOutput";
            cmbPaletteOutput.Size = new Size(350, 23);
            cmbPaletteOutput.TabIndex = 6;
            // 
            // lblPaletteOutput
            // 
            lblPaletteOutput.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblPaletteOutput.Location = new Point(40, 170);
            lblPaletteOutput.Name = "lblPaletteOutput";
            lblPaletteOutput.Size = new Size(230, 22);
            lblPaletteOutput.TabIndex = 5;
            lblPaletteOutput.Text = "Palette Output: ";
            lblPaletteOutput.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // cmbPreviewOutput
            // 
            cmbPreviewOutput.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPreviewOutput.Font = new Font("Segoe UI", 9F);
            cmbPreviewOutput.FormattingEnabled = true;
            cmbPreviewOutput.Location = new Point(280, 130);
            cmbPreviewOutput.Name = "cmbPreviewOutput";
            cmbPreviewOutput.Size = new Size(350, 23);
            cmbPreviewOutput.TabIndex = 4;
            // 
            // lblPreviewOutput
            // 
            lblPreviewOutput.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblPreviewOutput.Location = new Point(40, 130);
            lblPreviewOutput.Name = "lblPreviewOutput";
            lblPreviewOutput.Size = new Size(230, 22);
            lblPreviewOutput.TabIndex = 3;
            lblPreviewOutput.Text = "Preview Output:";
            lblPreviewOutput.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // cmbMainOutput
            // 
            cmbMainOutput.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbMainOutput.Font = new Font("Segoe UI", 9F);
            cmbMainOutput.FormattingEnabled = true;
            cmbMainOutput.Location = new Point(280, 90);
            cmbMainOutput.Name = "cmbMainOutput";
            cmbMainOutput.Size = new Size(350, 23);
            cmbMainOutput.TabIndex = 2;
            // 
            // lblMainOutput
            // 
            lblMainOutput.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblMainOutput.Location = new Point(40, 90);
            lblMainOutput.Name = "lblMainOutput";
            lblMainOutput.Size = new Size(230, 22);
            lblMainOutput.TabIndex = 1;
            lblMainOutput.Text = "Main Output:";
            lblMainOutput.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblAudioTitle
            // 
            lblAudioTitle.AutoSize = true;
            lblAudioTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblAudioTitle.ForeColor = Color.FromArgb(0, 120, 215);
            lblAudioTitle.Location = new Point(40, 40);
            lblAudioTitle.Name = "lblAudioTitle";
            lblAudioTitle.Size = new Size(169, 25);
            lblAudioTitle.TabIndex = 0;
            lblAudioTitle.Text = "🔊 USCITE AUDIO";
            // 
            // tabVideo
            // 
            tabVideo.BackColor = Color.FromArgb(240, 240, 240);
            tabVideo.Controls.Add(btnSaveVideo);
            tabVideo.Controls.Add(grpNDI);
            tabVideo.Controls.Add(chkLocalAudioOutput);
            tabVideo.Controls.Add(cmbAdvLanner);
            tabVideo.Controls.Add(lblAdvLanner);
            tabVideo.Controls.Add(cmbBufferMode);
            tabVideo.Controls.Add(lblBufferMode);
            tabVideo.Controls.Add(btnBrowseBufferVideo);
            tabVideo.Controls.Add(txtBufferVideoPath);
            tabVideo.Controls.Add(lblBufferVideoPath);
            tabVideo.Controls.Add(cmbVideoFrameRate);
            tabVideo.Controls.Add(lblVideoFrameRate);
            tabVideo.Controls.Add(cmbVideoOutputType);
            tabVideo.Controls.Add(lblVideoOutputType);
            tabVideo.Controls.Add(lblVideoTitle);
            tabVideo.Location = new Point(4, 26);
            tabVideo.Name = "tabVideo";
            tabVideo.Padding = new Padding(20);
            tabVideo.Size = new Size(992, 670);
            tabVideo.TabIndex = 6;
            tabVideo.Text = "🎬 Video";
            // 
            // btnSaveVideo
            // 
            btnSaveVideo.BackColor = Color.FromArgb(40, 167, 69);
            btnSaveVideo.FlatAppearance.BorderSize = 0;
            btnSaveVideo.FlatStyle = FlatStyle.Flat;
            btnSaveVideo.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnSaveVideo.ForeColor = Color.White;
            btnSaveVideo.Location = new Point(40, 520);
            btnSaveVideo.Name = "btnSaveVideo";
            btnSaveVideo.Size = new Size(200, 40);
            btnSaveVideo.TabIndex = 14;
            btnSaveVideo.Text = "💾 Salva";
            btnSaveVideo.UseVisualStyleBackColor = false;
            btnSaveVideo.Click += btnSave_Click;
            // 
            // grpNDI
            // 
            grpNDI.Controls.Add(btnRefreshNDI);
            grpNDI.Controls.Add(cmbNDISource);
            grpNDI.Controls.Add(lblNDISource);
            grpNDI.Controls.Add(txtNDISourceName);
            grpNDI.Controls.Add(lblNDISourceName);
            grpNDI.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            grpNDI.Location = new Point(40, 340);
            grpNDI.Name = "grpNDI";
            grpNDI.Size = new Size(640, 120);
            grpNDI.TabIndex = 12;
            grpNDI.TabStop = false;
            grpNDI.Text = "⚡ Configurazione NDI";
            // 
            // btnRefreshNDI
            // 
            btnRefreshNDI.BackColor = Color.FromArgb(0, 120, 215);
            btnRefreshNDI.FlatAppearance.BorderSize = 0;
            btnRefreshNDI.FlatStyle = FlatStyle.Flat;
            btnRefreshNDI.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            btnRefreshNDI.ForeColor = Color.White;
            btnRefreshNDI.Location = new Point(540, 75);
            btnRefreshNDI.Name = "btnRefreshNDI";
            btnRefreshNDI.Size = new Size(80, 26);
            btnRefreshNDI.TabIndex = 4;
            btnRefreshNDI.Text = "🔄 Aggiorna";
            btnRefreshNDI.UseVisualStyleBackColor = false;
            btnRefreshNDI.Click += btnRefreshNDI_Click;
            // 
            // cmbNDISource
            // 
            cmbNDISource.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbNDISource.Font = new Font("Segoe UI", 9F);
            cmbNDISource.FormattingEnabled = true;
            cmbNDISource.Location = new Point(200, 77);
            cmbNDISource.Name = "cmbNDISource";
            cmbNDISource.Size = new Size(330, 23);
            cmbNDISource.TabIndex = 3;
            // 
            // lblNDISource
            // 
            lblNDISource.Font = new Font("Segoe UI", 9F);
            lblNDISource.Location = new Point(20, 77);
            lblNDISource.Name = "lblNDISource";
            lblNDISource.Size = new Size(170, 20);
            lblNDISource.TabIndex = 2;
            lblNDISource.Text = "Sorgente Esistente:";
            lblNDISource.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // txtNDISourceName
            // 
            txtNDISourceName.Font = new Font("Segoe UI", 9F);
            txtNDISourceName.Location = new Point(200, 35);
            txtNDISourceName.Name = "txtNDISourceName";
            txtNDISourceName.Size = new Size(420, 23);
            txtNDISourceName.TabIndex = 1;
            txtNDISourceName.Text = "AirDirector TV Output";
            // 
            // lblNDISourceName
            // 
            lblNDISourceName.Font = new Font("Segoe UI", 9F);
            lblNDISourceName.Location = new Point(20, 35);
            lblNDISourceName.Name = "lblNDISourceName";
            lblNDISourceName.Size = new Size(170, 20);
            lblNDISourceName.TabIndex = 0;
            lblNDISourceName.Text = "Nome Sorgente NDI:";
            lblNDISourceName.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // cmbAdvLanner
            // 
            cmbAdvLanner.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbAdvLanner.Font = new Font("Segoe UI", 9F);
            cmbAdvLanner.FormattingEnabled = true;
            cmbAdvLanner.Location = new Point(280, 290);
            cmbAdvLanner.Name = "cmbAdvLanner";
            cmbAdvLanner.Size = new Size(350, 23);
            cmbAdvLanner.TabIndex = 16;
            // 
            // lblAdvLanner
            // 
            lblAdvLanner.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblAdvLanner.Location = new Point(40, 290);
            lblAdvLanner.Name = "lblAdvLanner";
            lblAdvLanner.Size = new Size(230, 22);
            lblAdvLanner.TabIndex = 15;
            lblAdvLanner.Text = "ADV Lanner Video on Output Playout:";
            lblAdvLanner.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // chkLocalAudioOutput
            // 
            chkLocalAudioOutput.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            chkLocalAudioOutput.Location = new Point(40, 250);
            chkLocalAudioOutput.Name = "chkLocalAudioOutput";
            chkLocalAudioOutput.Size = new Size(590, 22);
            chkLocalAudioOutput.TabIndex = 17;
            chkLocalAudioOutput.Text = "🔊 Abilita riproduzione audio anche su uscita locale";
            chkLocalAudioOutput.UseVisualStyleBackColor = true;
            // 
            // cmbBufferMode
            // 
            cmbBufferMode.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbBufferMode.Font = new Font("Segoe UI", 9F);
            cmbBufferMode.FormattingEnabled = true;
            cmbBufferMode.Location = new Point(280, 210);
            cmbBufferMode.Name = "cmbBufferMode";
            cmbBufferMode.Size = new Size(350, 23);
            cmbBufferMode.TabIndex = 11;
            // 
            // lblBufferMode
            // 
            lblBufferMode.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblBufferMode.Location = new Point(40, 210);
            lblBufferMode.Name = "lblBufferMode";
            lblBufferMode.Size = new Size(230, 22);
            lblBufferMode.TabIndex = 10;
            lblBufferMode.Text = "Modalità Riproduzione:";
            lblBufferMode.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // btnBrowseBufferVideo
            // 
            btnBrowseBufferVideo.BackColor = Color.FromArgb(0, 120, 215);
            btnBrowseBufferVideo.FlatAppearance.BorderSize = 0;
            btnBrowseBufferVideo.FlatStyle = FlatStyle.Flat;
            btnBrowseBufferVideo.Font = new Font("Segoe UI", 10F);
            btnBrowseBufferVideo.ForeColor = Color.White;
            btnBrowseBufferVideo.Location = new Point(640, 168);
            btnBrowseBufferVideo.Name = "btnBrowseBufferVideo";
            btnBrowseBufferVideo.Size = new Size(40, 26);
            btnBrowseBufferVideo.TabIndex = 9;
            btnBrowseBufferVideo.Text = "📁";
            btnBrowseBufferVideo.UseVisualStyleBackColor = false;
            btnBrowseBufferVideo.Click += btnBrowseBufferVideo_Click;
            // 
            // txtBufferVideoPath
            // 
            txtBufferVideoPath.Font = new Font("Segoe UI", 9F);
            txtBufferVideoPath.Location = new Point(280, 170);
            txtBufferVideoPath.Name = "txtBufferVideoPath";
            txtBufferVideoPath.Size = new Size(350, 23);
            txtBufferVideoPath.TabIndex = 8;
            // 
            // lblBufferVideoPath
            // 
            lblBufferVideoPath.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblBufferVideoPath.Location = new Point(40, 170);
            lblBufferVideoPath.Name = "lblBufferVideoPath";
            lblBufferVideoPath.Size = new Size(230, 22);
            lblBufferVideoPath.TabIndex = 7;
            lblBufferVideoPath.Text = "Cartella Video Tampone:";
            lblBufferVideoPath.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // cmbVideoFrameRate
            // 
            cmbVideoFrameRate.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbVideoFrameRate.Font = new Font("Segoe UI", 9F);
            cmbVideoFrameRate.FormattingEnabled = true;
            cmbVideoFrameRate.Location = new Point(280, 130);
            cmbVideoFrameRate.Name = "cmbVideoFrameRate";
            cmbVideoFrameRate.Size = new Size(200, 23);
            cmbVideoFrameRate.TabIndex = 6;
            // 
            // lblVideoFrameRate
            // 
            lblVideoFrameRate.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblVideoFrameRate.Location = new Point(40, 130);
            lblVideoFrameRate.Name = "lblVideoFrameRate";
            lblVideoFrameRate.Size = new Size(230, 22);
            lblVideoFrameRate.TabIndex = 5;
            lblVideoFrameRate.Text = "Frame Rate:";
            lblVideoFrameRate.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // cmbVideoOutputType
            // 
            cmbVideoOutputType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbVideoOutputType.Font = new Font("Segoe UI", 9F);
            cmbVideoOutputType.FormattingEnabled = true;
            cmbVideoOutputType.Location = new Point(280, 90);
            cmbVideoOutputType.Name = "cmbVideoOutputType";
            cmbVideoOutputType.Size = new Size(350, 23);
            cmbVideoOutputType.TabIndex = 2;
            // 
            // lblVideoOutputType
            // 
            lblVideoOutputType.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblVideoOutputType.Location = new Point(40, 90);
            lblVideoOutputType.Name = "lblVideoOutputType";
            lblVideoOutputType.Size = new Size(230, 22);
            lblVideoOutputType.TabIndex = 1;
            lblVideoOutputType.Text = "Tipo Output Video:";
            lblVideoOutputType.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblVideoTitle
            // 
            lblVideoTitle.AutoSize = true;
            lblVideoTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblVideoTitle.ForeColor = Color.FromArgb(0, 120, 215);
            lblVideoTitle.Location = new Point(40, 40);
            lblVideoTitle.Name = "lblVideoTitle";
            lblVideoTitle.Size = new Size(282, 25);
            lblVideoTitle.TabIndex = 0;
            lblVideoTitle.Text = "🎬 CONFIGURAZIONE VIDEO";
            // 
            // tabPaths
            // 
            tabPaths.BackColor = Color.FromArgb(240, 240, 240);
            tabPaths.Controls.Add(btnBrowseTimeSignal);
            tabPaths.Controls.Add(txtTimeSignalPath);
            tabPaths.Controls.Add(lblTimeSignalPath);
            tabPaths.Controls.Add(btnSavePaths);
            tabPaths.Controls.Add(btnApplyDriveZ);
            tabPaths.Controls.Add(btnBrowseDriveZ);
            tabPaths.Controls.Add(txtDriveZ);
            tabPaths.Controls.Add(lblDriveZ);
            tabPaths.Controls.Add(btnApplyDriveY);
            tabPaths.Controls.Add(btnBrowseDriveY);
            tabPaths.Controls.Add(txtDriveY);
            tabPaths.Controls.Add(lblDriveY);
            tabPaths.Controls.Add(btnApplyDriveX);
            tabPaths.Controls.Add(btnBrowseDriveX);
            tabPaths.Controls.Add(txtDriveX);
            tabPaths.Controls.Add(lblDriveX);
            tabPaths.Controls.Add(btnBrowseDatabase);
            tabPaths.Controls.Add(txtDatabasePath);
            tabPaths.Controls.Add(lblDatabasePath);
            tabPaths.Controls.Add(lblPathsTitle);
            tabPaths.Location = new Point(4, 26);
            tabPaths.Name = "tabPaths";
            tabPaths.Padding = new Padding(20);
            tabPaths.Size = new Size(992, 670);
            tabPaths.TabIndex = 3;
            tabPaths.Text = "📂 Percorsi";
            // 
            // btnBrowseTimeSignal
            // 
            btnBrowseTimeSignal.BackColor = Color.FromArgb(0, 120, 215);
            btnBrowseTimeSignal.FlatAppearance.BorderSize = 0;
            btnBrowseTimeSignal.FlatStyle = FlatStyle.Flat;
            btnBrowseTimeSignal.Font = new Font("Segoe UI", 10F);
            btnBrowseTimeSignal.ForeColor = Color.White;
            btnBrowseTimeSignal.Location = new Point(690, 290);
            btnBrowseTimeSignal.Name = "btnBrowseTimeSignal";
            btnBrowseTimeSignal.Size = new Size(40, 26);
            btnBrowseTimeSignal.TabIndex = 19;
            btnBrowseTimeSignal.Text = "📁";
            btnBrowseTimeSignal.UseVisualStyleBackColor = false;
            btnBrowseTimeSignal.Click += btnBrowseTimeSignal_Click;
            // 
            // txtTimeSignalPath
            // 
            txtTimeSignalPath.Font = new Font("Segoe UI", 9F);
            txtTimeSignalPath.Location = new Point(280, 291);
            txtTimeSignalPath.Name = "txtTimeSignalPath";
            txtTimeSignalPath.Size = new Size(390, 23);
            txtTimeSignalPath.TabIndex = 18;
            // 
            // lblTimeSignalPath
            // 
            lblTimeSignalPath.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblTimeSignalPath.Location = new Point(40, 291);
            lblTimeSignalPath.Name = "lblTimeSignalPath";
            lblTimeSignalPath.Size = new Size(230, 22);
            lblTimeSignalPath.TabIndex = 17;
            lblTimeSignalPath.Text = "Cartella Segnale Orario:";
            lblTimeSignalPath.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // btnSavePaths
            // 
            btnSavePaths.BackColor = Color.FromArgb(40, 167, 69);
            btnSavePaths.FlatAppearance.BorderSize = 0;
            btnSavePaths.FlatStyle = FlatStyle.Flat;
            btnSavePaths.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnSavePaths.ForeColor = Color.White;
            btnSavePaths.Location = new Point(40, 356);
            btnSavePaths.Name = "btnSavePaths";
            btnSavePaths.Size = new Size(200, 40);
            btnSavePaths.TabIndex = 16;
            btnSavePaths.Text = "💾 Salva";
            btnSavePaths.UseVisualStyleBackColor = false;
            btnSavePaths.Click += btnSave_Click;
            // 
            // btnApplyDriveZ
            // 
            btnApplyDriveZ.BackColor = Color.FromArgb(255, 140, 0);
            btnApplyDriveZ.FlatAppearance.BorderSize = 0;
            btnApplyDriveZ.FlatStyle = FlatStyle.Flat;
            btnApplyDriveZ.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnApplyDriveZ.ForeColor = Color.White;
            btnApplyDriveZ.Location = new Point(750, 250);
            btnApplyDriveZ.Name = "btnApplyDriveZ";
            btnApplyDriveZ.Size = new Size(101, 26);
            btnApplyDriveZ.TabIndex = 15;
            btnApplyDriveZ.Text = "✓ Applica";
            btnApplyDriveZ.UseVisualStyleBackColor = false;
            btnApplyDriveZ.Click += btnApplyDriveZ_Click;
            // 
            // btnBrowseDriveZ
            // 
            btnBrowseDriveZ.BackColor = Color.FromArgb(0, 120, 215);
            btnBrowseDriveZ.FlatAppearance.BorderSize = 0;
            btnBrowseDriveZ.FlatStyle = FlatStyle.Flat;
            btnBrowseDriveZ.Font = new Font("Segoe UI", 10F);
            btnBrowseDriveZ.ForeColor = Color.White;
            btnBrowseDriveZ.Location = new Point(690, 250);
            btnBrowseDriveZ.Name = "btnBrowseDriveZ";
            btnBrowseDriveZ.Size = new Size(40, 26);
            btnBrowseDriveZ.TabIndex = 14;
            btnBrowseDriveZ.Text = "📁";
            btnBrowseDriveZ.UseVisualStyleBackColor = false;
            btnBrowseDriveZ.Click += btnBrowseDriveZ_Click;
            // 
            // txtDriveZ
            // 
            txtDriveZ.Font = new Font("Segoe UI", 9F);
            txtDriveZ.Location = new Point(280, 251);
            txtDriveZ.Name = "txtDriveZ";
            txtDriveZ.Size = new Size(390, 23);
            txtDriveZ.TabIndex = 13;
            // 
            // lblDriveZ
            // 
            lblDriveZ.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblDriveZ.Location = new Point(40, 251);
            lblDriveZ.Name = "lblDriveZ";
            lblDriveZ.Size = new Size(230, 22);
            lblDriveZ.TabIndex = 12;
            lblDriveZ.Text = "Drive Z (Archivio Musica):";
            lblDriveZ.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // btnApplyDriveY
            // 
            btnApplyDriveY.BackColor = Color.FromArgb(255, 140, 0);
            btnApplyDriveY.FlatAppearance.BorderSize = 0;
            btnApplyDriveY.FlatStyle = FlatStyle.Flat;
            btnApplyDriveY.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnApplyDriveY.ForeColor = Color.White;
            btnApplyDriveY.Location = new Point(750, 210);
            btnApplyDriveY.Name = "btnApplyDriveY";
            btnApplyDriveY.Size = new Size(101, 26);
            btnApplyDriveY.TabIndex = 11;
            btnApplyDriveY.Text = "✓ Applica";
            btnApplyDriveY.UseVisualStyleBackColor = false;
            btnApplyDriveY.Click += btnApplyDriveY_Click;
            // 
            // btnBrowseDriveY
            // 
            btnBrowseDriveY.BackColor = Color.FromArgb(0, 120, 215);
            btnBrowseDriveY.FlatAppearance.BorderSize = 0;
            btnBrowseDriveY.FlatStyle = FlatStyle.Flat;
            btnBrowseDriveY.Font = new Font("Segoe UI", 10F);
            btnBrowseDriveY.ForeColor = Color.White;
            btnBrowseDriveY.Location = new Point(690, 210);
            btnBrowseDriveY.Name = "btnBrowseDriveY";
            btnBrowseDriveY.Size = new Size(40, 26);
            btnBrowseDriveY.TabIndex = 10;
            btnBrowseDriveY.Text = "📁";
            btnBrowseDriveY.UseVisualStyleBackColor = false;
            btnBrowseDriveY.Click += btnBrowseDriveY_Click;
            // 
            // txtDriveY
            // 
            txtDriveY.Font = new Font("Segoe UI", 9F);
            txtDriveY.Location = new Point(280, 211);
            txtDriveY.Name = "txtDriveY";
            txtDriveY.Size = new Size(390, 23);
            txtDriveY.TabIndex = 9;
            // 
            // lblDriveY
            // 
            lblDriveY.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblDriveY.Location = new Point(40, 211);
            lblDriveY.Name = "lblDriveY";
            lblDriveY.Size = new Size(230, 22);
            lblDriveY.TabIndex = 8;
            lblDriveY.Text = "Drive Y (Archivio Commercial):";
            lblDriveY.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // btnApplyDriveX
            // 
            btnApplyDriveX.BackColor = Color.FromArgb(255, 140, 0);
            btnApplyDriveX.FlatAppearance.BorderSize = 0;
            btnApplyDriveX.FlatStyle = FlatStyle.Flat;
            btnApplyDriveX.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnApplyDriveX.ForeColor = Color.White;
            btnApplyDriveX.Location = new Point(750, 170);
            btnApplyDriveX.Name = "btnApplyDriveX";
            btnApplyDriveX.Size = new Size(101, 26);
            btnApplyDriveX.TabIndex = 7;
            btnApplyDriveX.Text = "✓ Applica";
            btnApplyDriveX.UseVisualStyleBackColor = false;
            btnApplyDriveX.Click += btnApplyDriveX_Click;
            // 
            // btnBrowseDriveX
            // 
            btnBrowseDriveX.BackColor = Color.FromArgb(0, 120, 215);
            btnBrowseDriveX.FlatAppearance.BorderSize = 0;
            btnBrowseDriveX.FlatStyle = FlatStyle.Flat;
            btnBrowseDriveX.Font = new Font("Segoe UI", 10F);
            btnBrowseDriveX.ForeColor = Color.White;
            btnBrowseDriveX.Location = new Point(690, 170);
            btnBrowseDriveX.Name = "btnBrowseDriveX";
            btnBrowseDriveX.Size = new Size(40, 26);
            btnBrowseDriveX.TabIndex = 6;
            btnBrowseDriveX.Text = "📁";
            btnBrowseDriveX.UseVisualStyleBackColor = false;
            btnBrowseDriveX.Click += btnBrowseDriveX_Click;
            // 
            // txtDriveX
            // 
            txtDriveX.Font = new Font("Segoe UI", 9F);
            txtDriveX.Location = new Point(280, 171);
            txtDriveX.Name = "txtDriveX";
            txtDriveX.Size = new Size(390, 23);
            txtDriveX.TabIndex = 5;
            // 
            // lblDriveX
            // 
            lblDriveX.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblDriveX.Location = new Point(40, 171);
            lblDriveX.Name = "lblDriveX";
            lblDriveX.Size = new Size(230, 22);
            lblDriveX.TabIndex = 4;
            lblDriveX.Text = "Drive X (Database Condiviso):";
            lblDriveX.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // btnBrowseDatabase
            // 
            btnBrowseDatabase.BackColor = Color.FromArgb(0, 120, 215);
            btnBrowseDatabase.FlatAppearance.BorderSize = 0;
            btnBrowseDatabase.FlatStyle = FlatStyle.Flat;
            btnBrowseDatabase.Font = new Font("Segoe UI", 10F);
            btnBrowseDatabase.ForeColor = Color.White;
            btnBrowseDatabase.Location = new Point(690, 90);
            btnBrowseDatabase.Name = "btnBrowseDatabase";
            btnBrowseDatabase.Size = new Size(40, 26);
            btnBrowseDatabase.TabIndex = 3;
            btnBrowseDatabase.Text = "📁";
            btnBrowseDatabase.UseVisualStyleBackColor = false;
            btnBrowseDatabase.Click += btnBrowseDatabase_Click;
            // 
            // txtDatabasePath
            // 
            txtDatabasePath.Font = new Font("Segoe UI", 9F);
            txtDatabasePath.Location = new Point(280, 91);
            txtDatabasePath.Name = "txtDatabasePath";
            txtDatabasePath.Size = new Size(390, 23);
            txtDatabasePath.TabIndex = 2;
            // 
            // lblDatabasePath
            // 
            lblDatabasePath.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblDatabasePath.Location = new Point(40, 91);
            lblDatabasePath.Name = "lblDatabasePath";
            lblDatabasePath.Size = new Size(230, 22);
            lblDatabasePath.TabIndex = 1;
            lblDatabasePath.Text = "Path Database:";
            lblDatabasePath.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblPathsTitle
            // 
            lblPathsTitle.AutoSize = true;
            lblPathsTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblPathsTitle.ForeColor = Color.FromArgb(0, 120, 215);
            lblPathsTitle.Location = new Point(40, 40);
            lblPathsTitle.Name = "lblPathsTitle";
            lblPathsTitle.Size = new Size(261, 25);
            lblPathsTitle.TabIndex = 0;
            lblPathsTitle.Text = "📂 PERCORSI E MAPPATURE";
            // 
            // tabMetadata
            // 
            tabMetadata.BackColor = Color.FromArgb(240, 240, 240);
            tabMetadata.Controls.Add(btnSaveMetadata);
            tabMetadata.Controls.Add(chkSendToEncoders);
            tabMetadata.Controls.Add(chkSaveRds);
            tabMetadata.Controls.Add(btnBrowseRds);
            tabMetadata.Controls.Add(txtRdsPath);
            tabMetadata.Controls.Add(lblRdsPath);
            tabMetadata.Controls.Add(rbMusicAndClips);
            tabMetadata.Controls.Add(rbMusicOnly);
            tabMetadata.Controls.Add(lblMetadataSource);
            tabMetadata.Controls.Add(lblMetadataTitle);
            tabMetadata.Location = new Point(4, 26);
            tabMetadata.Name = "tabMetadata";
            tabMetadata.Padding = new Padding(20);
            tabMetadata.Size = new Size(992, 670);
            tabMetadata.TabIndex = 4;
            tabMetadata.Text = "📡 Metadata";
            // 
            // btnSaveMetadata
            // 
            btnSaveMetadata.BackColor = Color.FromArgb(40, 167, 69);
            btnSaveMetadata.FlatAppearance.BorderSize = 0;
            btnSaveMetadata.FlatStyle = FlatStyle.Flat;
            btnSaveMetadata.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnSaveMetadata.ForeColor = Color.White;
            btnSaveMetadata.Location = new Point(40, 280);
            btnSaveMetadata.Name = "btnSaveMetadata";
            btnSaveMetadata.Size = new Size(200, 40);
            btnSaveMetadata.TabIndex = 9;
            btnSaveMetadata.Text = "💾 Salva";
            btnSaveMetadata.UseVisualStyleBackColor = false;
            btnSaveMetadata.Click += btnSave_Click;
            // 
            // chkSendToEncoders
            // 
            chkSendToEncoders.AutoSize = true;
            chkSendToEncoders.Font = new Font("Segoe UI", 9F);
            chkSendToEncoders.Location = new Point(280, 210);
            chkSendToEncoders.Name = "chkSendToEncoders";
            chkSendToEncoders.Size = new Size(220, 19);
            chkSendToEncoders.TabIndex = 8;
            chkSendToEncoders.Text = "📤 Invia metadata agli Encoders attivi";
            chkSendToEncoders.UseVisualStyleBackColor = true;
            // 
            // chkSaveRds
            // 
            chkSaveRds.AutoSize = true;
            chkSaveRds.Font = new Font("Segoe UI", 9F);
            chkSaveRds.Location = new Point(40, 210);
            chkSaveRds.Name = "chkSaveRds";
            chkSaveRds.Size = new Size(128, 19);
            chkSaveRds.TabIndex = 7;
            chkSaveRds.Text = "💾 Salva file RDS. txt";
            chkSaveRds.UseVisualStyleBackColor = true;
            // 
            // btnBrowseRds
            // 
            btnBrowseRds.BackColor = Color.FromArgb(0, 120, 215);
            btnBrowseRds.FlatAppearance.BorderSize = 0;
            btnBrowseRds.FlatStyle = FlatStyle.Flat;
            btnBrowseRds.Font = new Font("Segoe UI", 10F);
            btnBrowseRds.ForeColor = Color.White;
            btnBrowseRds.Location = new Point(690, 170);
            btnBrowseRds.Name = "btnBrowseRds";
            btnBrowseRds.Size = new Size(40, 26);
            btnBrowseRds.TabIndex = 6;
            btnBrowseRds.Text = "📁";
            btnBrowseRds.UseVisualStyleBackColor = false;
            btnBrowseRds.Click += btnBrowseRds_Click;
            // 
            // txtRdsPath
            // 
            txtRdsPath.Font = new Font("Segoe UI", 9F);
            txtRdsPath.Location = new Point(280, 171);
            txtRdsPath.Name = "txtRdsPath";
            txtRdsPath.ReadOnly = true;
            txtRdsPath.Size = new Size(390, 23);
            txtRdsPath.TabIndex = 5;
            // 
            // lblRdsPath
            // 
            lblRdsPath.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblRdsPath.Location = new Point(40, 171);
            lblRdsPath.Name = "lblRdsPath";
            lblRdsPath.Size = new Size(230, 22);
            lblRdsPath.TabIndex = 4;
            lblRdsPath.Text = "File RDS. txt:";
            lblRdsPath.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // rbMusicAndClips
            // 
            rbMusicAndClips.AutoSize = true;
            rbMusicAndClips.Font = new Font("Segoe UI", 9F);
            rbMusicAndClips.Location = new Point(415, 130);
            rbMusicAndClips.Name = "rbMusicAndClips";
            rbMusicAndClips.Size = new Size(103, 19);
            rbMusicAndClips.TabIndex = 3;
            rbMusicAndClips.Text = "Musica + Clips";
            rbMusicAndClips.UseVisualStyleBackColor = true;
            // 
            // rbMusicOnly
            // 
            rbMusicOnly.AutoSize = true;
            rbMusicOnly.Checked = true;
            rbMusicOnly.Font = new Font("Segoe UI", 9F);
            rbMusicOnly.Location = new Point(280, 130);
            rbMusicOnly.Name = "rbMusicOnly";
            rbMusicOnly.Size = new Size(89, 19);
            rbMusicOnly.TabIndex = 2;
            rbMusicOnly.TabStop = true;
            rbMusicOnly.Text = "Solo Musica";
            rbMusicOnly.UseVisualStyleBackColor = true;
            // 
            // lblMetadataSource
            // 
            lblMetadataSource.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblMetadataSource.Location = new Point(40, 130);
            lblMetadataSource.Name = "lblMetadataSource";
            lblMetadataSource.Size = new Size(230, 22);
            lblMetadataSource.TabIndex = 1;
            lblMetadataSource.Text = "Sorgente Metadata:";
            lblMetadataSource.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblMetadataTitle
            // 
            lblMetadataTitle.AutoSize = true;
            lblMetadataTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblMetadataTitle.ForeColor = Color.FromArgb(0, 120, 215);
            lblMetadataTitle.Location = new Point(40, 40);
            lblMetadataTitle.Name = "lblMetadataTitle";
            lblMetadataTitle.Size = new Size(139, 25);
            lblMetadataTitle.TabIndex = 0;
            lblMetadataTitle.Text = "📡 METADATA";
            // 
            // tabBackup
            // 
            tabBackup.BackColor = Color.FromArgb(240, 240, 240);
            tabBackup.Controls.Add(btnSaveBackup);
            tabBackup.Controls.Add(dtpBackupTime);
            tabBackup.Controls.Add(lblBackupTime);
            tabBackup.Controls.Add(btnBrowseBackup);
            tabBackup.Controls.Add(txtBackupPath);
            tabBackup.Controls.Add(lblBackupPath);
            tabBackup.Controls.Add(lblBackupTitle);
            tabBackup.Location = new Point(4, 26);
            tabBackup.Name = "tabBackup";
            tabBackup.Padding = new Padding(20);
            tabBackup.Size = new Size(992, 670);
            tabBackup.TabIndex = 5;
            tabBackup.Text = "💾 Backup";
            // 
            // btnSaveBackup
            // 
            btnSaveBackup.BackColor = Color.FromArgb(40, 167, 69);
            btnSaveBackup.FlatAppearance.BorderSize = 0;
            btnSaveBackup.FlatStyle = FlatStyle.Flat;
            btnSaveBackup.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnSaveBackup.ForeColor = Color.White;
            btnSaveBackup.Location = new Point(40, 200);
            btnSaveBackup.Name = "btnSaveBackup";
            btnSaveBackup.Size = new Size(200, 40);
            btnSaveBackup.TabIndex = 6;
            btnSaveBackup.Text = "💾 Salva";
            btnSaveBackup.UseVisualStyleBackColor = false;
            btnSaveBackup.Click += btnSave_Click;
            // 
            // dtpBackupTime
            // 
            dtpBackupTime.Font = new Font("Segoe UI", 9F);
            dtpBackupTime.Format = DateTimePickerFormat.Time;
            dtpBackupTime.Location = new Point(280, 130);
            dtpBackupTime.Name = "dtpBackupTime";
            dtpBackupTime.ShowUpDown = true;
            dtpBackupTime.Size = new Size(120, 23);
            dtpBackupTime.TabIndex = 5;
            // 
            // lblBackupTime
            // 
            lblBackupTime.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblBackupTime.Location = new Point(40, 130);
            lblBackupTime.Name = "lblBackupTime";
            lblBackupTime.Size = new Size(230, 22);
            lblBackupTime.TabIndex = 4;
            lblBackupTime.Text = "Orario Backup Automatico:";
            lblBackupTime.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // btnBrowseBackup
            // 
            btnBrowseBackup.BackColor = Color.FromArgb(0, 120, 215);
            btnBrowseBackup.FlatAppearance.BorderSize = 0;
            btnBrowseBackup.FlatStyle = FlatStyle.Flat;
            btnBrowseBackup.Font = new Font("Segoe UI", 10F);
            btnBrowseBackup.ForeColor = Color.White;
            btnBrowseBackup.Location = new Point(690, 90);
            btnBrowseBackup.Name = "btnBrowseBackup";
            btnBrowseBackup.Size = new Size(40, 26);
            btnBrowseBackup.TabIndex = 3;
            btnBrowseBackup.Text = "📁";
            btnBrowseBackup.UseVisualStyleBackColor = false;
            btnBrowseBackup.Click += btnBrowseBackup_Click;
            // 
            // txtBackupPath
            // 
            txtBackupPath.Font = new Font("Segoe UI", 9F);
            txtBackupPath.Location = new Point(280, 91);
            txtBackupPath.Name = "txtBackupPath";
            txtBackupPath.ReadOnly = true;
            txtBackupPath.Size = new Size(390, 23);
            txtBackupPath.TabIndex = 2;
            // 
            // lblBackupPath
            // 
            lblBackupPath.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblBackupPath.Location = new Point(40, 91);
            lblBackupPath.Name = "lblBackupPath";
            lblBackupPath.Size = new Size(230, 22);
            lblBackupPath.TabIndex = 1;
            lblBackupPath.Text = "Cartella Backup:";
            lblBackupPath.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblBackupTitle
            // 
            lblBackupTitle.AutoSize = true;
            lblBackupTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblBackupTitle.ForeColor = Color.FromArgb(0, 120, 215);
            lblBackupTitle.Location = new Point(40, 40);
            lblBackupTitle.Name = "lblBackupTitle";
            lblBackupTitle.Size = new Size(242, 25);
            lblBackupTitle.TabIndex = 0;
            lblBackupTitle.Text = "💾 BACKUP AUTOMATICO";
            // 
            // ConfigurationControl
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(tabControl);
            Name = "ConfigurationControl";
            Size = new Size(1000, 700);
            tabControl.ResumeLayout(false);
            tabGeneral.ResumeLayout(false);
            tabGeneral.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numArtistSeparation).EndInit();
            ((System.ComponentModel.ISupportInitialize)numHourlySeparation).EndInit();
            ((System.ComponentModel.ISupportInitialize)numMixDuration).EndInit();
            tabStation.ResumeLayout(false);
            tabStation.PerformLayout();
            tabAudio.ResumeLayout(false);
            tabAudio.PerformLayout();
            tabVideo.ResumeLayout(false);
            tabVideo.PerformLayout();
            grpNDI.ResumeLayout(false);
            grpNDI.PerformLayout();
            tabPaths.ResumeLayout(false);
            tabPaths.PerformLayout();
            tabMetadata.ResumeLayout(false);
            tabMetadata.PerformLayout();
            tabBackup.ResumeLayout(false);
            tabBackup.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TabControl tabControl;
        private TabPage tabGeneral;
        private TabPage tabStation;
        private TabPage tabAudio;
        private TabPage tabVideo;
        private TabPage tabPaths;
        private TabPage tabMetadata;
        private TabPage tabBackup;

        private Label lblGeneralTitle;
        private CheckBox chkAutoStart;
        private CheckBox chkShowWhatsApp;
        private Label lblMode;
        private ComboBox cmbMode;
        private Label lblLanguage;
        private ComboBox cmbLanguage;
        private Label lblMixDuration;
        private NumericUpDown numMixDuration;
        private Label lblHourlySeparation;
        private NumericUpDown numHourlySeparation;
        private Label lblArtistSeparation;
        private NumericUpDown numArtistSeparation;
        private Button btnSaveGeneral;

        private Label lblStationTitle;
        private Label lblStationName;
        private TextBox txtStationName;
        private Label lblLogoPath;
        private TextBox txtLogoPath;
        private Button btnBrowseLogo;
        private Button btnSaveStation;

        private Label lblAudioTitle;
        private Label lblMainOutput;
        private ComboBox cmbMainOutput;
        private Label lblPreviewOutput;
        private ComboBox cmbPreviewOutput;
        private Label lblPaletteOutput;
        private ComboBox cmbPaletteOutput;
        private Button btnRefresh;
        private Button btnSaveAudio;

        private Label lblVideoTitle;
        private Label lblVideoOutputType;
        private ComboBox cmbVideoOutputType;
        private Label lblVideoFrameRate;
        private ComboBox cmbVideoFrameRate;
        private Label lblBufferVideoPath;
        private TextBox txtBufferVideoPath;
        private Button btnBrowseBufferVideo;
        private Label lblBufferMode;
        private ComboBox cmbBufferMode;
        private Label lblAdvLanner;
        private ComboBox cmbAdvLanner;
        private CheckBox chkLocalAudioOutput;
        private GroupBox grpNDI;
        private Label lblNDISourceName;
        private TextBox txtNDISourceName;
        private Label lblNDISource;
        private ComboBox cmbNDISource;
        private Button btnRefreshNDI;
        private Button btnSaveVideo;

        private Label lblPathsTitle;
        private Label lblDatabasePath;
        private TextBox txtDatabasePath;
        private Button btnBrowseDatabase;
        private Label lblDriveX;
        private TextBox txtDriveX;
        private Button btnBrowseDriveX;
        private Button btnApplyDriveX;
        private Label lblDriveY;
        private TextBox txtDriveY;
        private Button btnBrowseDriveY;
        private Button btnApplyDriveY;
        private Label lblDriveZ;
        private TextBox txtDriveZ;
        private Button btnBrowseDriveZ;
        private Button btnApplyDriveZ;
        private Label lblTimeSignalPath;
        private TextBox txtTimeSignalPath;
        private Button btnBrowseTimeSignal;
        private Button btnSavePaths;

        private Label lblMetadataTitle;
        private Label lblMetadataSource;
        private RadioButton rbMusicOnly;
        private RadioButton rbMusicAndClips;
        private Label lblRdsPath;
        private TextBox txtRdsPath;
        private Button btnBrowseRds;
        private CheckBox chkSaveRds;
        private CheckBox chkSendToEncoders;
        private Button btnSaveMetadata;

        private Label lblBackupTitle;
        private Label lblBackupPath;
        private TextBox txtBackupPath;
        private Button btnBrowseBackup;
        private Label lblBackupTime;
        private DateTimePicker dtpBackupTime;
        private Button btnSaveBackup;
    }
}
