namespace AirDirector.Forms
{
    partial class MusicEditorForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Panel toolbarPanel;
        private System.Windows.Forms.Button btnPlay;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Button btnLoop;

        private System.Windows.Forms.Panel leftPanel;
        private System.Windows.Forms.Label lblCurrentPosition;
        private System.Windows.Forms.Label lblCurrentPositionMs;
        private System.Windows.Forms.Label lblTotalDuration;

        private System.Windows.Forms.Label lblMarkerInLabel;
        private System.Windows.Forms.TextBox txtMarkerIn;
        private System.Windows.Forms.Button btnSetMarkerIn;
        private System.Windows.Forms.Button btnMarkerInUp;
        private System.Windows.Forms.Button btnMarkerInDown;
        private System.Windows.Forms.Button btnPlayFromIn;

        private System.Windows.Forms.Label lblMarkerIntroLabel;
        private System.Windows.Forms.TextBox txtMarkerIntro;
        private System.Windows.Forms.Button btnSetMarkerIntro;
        private System.Windows.Forms.Button btnMarkerIntroUp;
        private System.Windows.Forms.Button btnMarkerIntroDown;
        private System.Windows.Forms.Button btnPlayFromIntro;

        private System.Windows.Forms.Label lblMarkerMixLabel;
        private System.Windows.Forms.TextBox txtMarkerMix;
        private System.Windows.Forms.Button btnSetMarkerMix;
        private System.Windows.Forms.Button btnMarkerMixUp;
        private System.Windows.Forms.Button btnMarkerMixDown;
        private System.Windows.Forms.Button btnPlayFromMix;

        private System.Windows.Forms.Label lblMarkerOutLabel;
        private System.Windows.Forms.TextBox txtMarkerOut;
        private System.Windows.Forms.Button btnSetMarkerOut;
        private System.Windows.Forms.Button btnMarkerOutUp;
        private System.Windows.Forms.Button btnMarkerOutDown;
        private System.Windows.Forms.Button btnPlayFromOut;

        // ✅ VU METER panel (sopra la waveform)
        private System.Windows.Forms.Panel vuMeterPanel;

        private System.Windows.Forms.PictureBox picWaveform;
        private System.Windows.Forms.Panel bottomPanel;

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.TextBox txtTitle;
        private System.Windows.Forms.Label lblArtist;
        private System.Windows.Forms.TextBox txtArtist;
        private System.Windows.Forms.Label lblAlbum;
        private System.Windows.Forms.TextBox txtAlbum;
        private System.Windows.Forms.Label lblYear;
        private System.Windows.Forms.NumericUpDown numYear;
        private System.Windows.Forms.Label lblGenre;
        private System.Windows.Forms.ComboBox cmbGenre;
        private System.Windows.Forms.Label lblCategories;
        private System.Windows.Forms.TextBox txtCategoriesDisplay;
        private System.Windows.Forms.Button btnCategoriesDropdown;
        private System.Windows.Forms.Label lblFeaturedArtists;
        private System.Windows.Forms.TextBox txtFeaturedArtistsDisplay;
        private System.Windows.Forms.Button btnFeaturedArtistsDropdown;
        private System.Windows.Forms.Label lblFilePath;
        private System.Windows.Forms.TextBox txtFilePath;

        private System.Windows.Forms.GroupBox grpPeriod;
        private System.Windows.Forms.CheckBox chkEnableValidFrom;
        private System.Windows.Forms.DateTimePicker dtpValidFrom;
        private System.Windows.Forms.CheckBox chkEnableValidTo;
        private System.Windows.Forms.DateTimePicker dtpValidTo;

        private System.Windows.Forms.GroupBox grpMonths;
        private System.Windows.Forms.GroupBox grpDays;
        private System.Windows.Forms.GroupBox grpHours;

        // ✅ VOLUME BOOST
        private System.Windows.Forms.GroupBox grpVolume;
        private System.Windows.Forms.TrackBar trkVolume;
        private System.Windows.Forms.Label lblVolumeDb;
        private System.Windows.Forms.Button btnApplyVolume;
        private System.Windows.Forms.CheckBox chkColoredPeaks;

        // ✅ ZOOM
        private System.Windows.Forms.Panel zoomPanel;
        private System.Windows.Forms.Label lblZoom;
        private System.Windows.Forms.TrackBar trkZoom;
        private System.Windows.Forms.Label lblZoomPercent;
        private System.Windows.Forms.HScrollBar hScrollWaveform;

        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;

        private void InitializeComponent()
        {
            toolbarPanel = new System.Windows.Forms.Panel();
            btnPlay = new System.Windows.Forms.Button();
            btnStop = new System.Windows.Forms.Button();
            btnLoop = new System.Windows.Forms.Button();

            // ✅ ZOOM controls nella toolbar
            zoomPanel = new System.Windows.Forms.Panel();
            lblZoom = new System.Windows.Forms.Label();
            trkZoom = new System.Windows.Forms.TrackBar();
            lblZoomPercent = new System.Windows.Forms.Label();

            // ✅ VOLUME controls nella toolbar
            grpVolume = new System.Windows.Forms.GroupBox();
            trkVolume = new System.Windows.Forms.TrackBar();
            lblVolumeDb = new System.Windows.Forms.Label();
            btnApplyVolume = new System.Windows.Forms.Button();
            chkColoredPeaks = new System.Windows.Forms.CheckBox();

            leftPanel = new System.Windows.Forms.Panel();
            lblCurrentPosition = new System.Windows.Forms.Label();
            lblCurrentPositionMs = new System.Windows.Forms.Label();
            lblTotalDuration = new System.Windows.Forms.Label();
            lblMarkerInLabel = new System.Windows.Forms.Label();
            txtMarkerIn = new System.Windows.Forms.TextBox();
            btnSetMarkerIn = new System.Windows.Forms.Button();
            btnMarkerInUp = new System.Windows.Forms.Button();
            btnMarkerInDown = new System.Windows.Forms.Button();
            btnPlayFromIn = new System.Windows.Forms.Button();
            lblMarkerIntroLabel = new System.Windows.Forms.Label();
            txtMarkerIntro = new System.Windows.Forms.TextBox();
            btnSetMarkerIntro = new System.Windows.Forms.Button();
            btnMarkerIntroUp = new System.Windows.Forms.Button();
            btnMarkerIntroDown = new System.Windows.Forms.Button();
            btnPlayFromIntro = new System.Windows.Forms.Button();
            lblMarkerMixLabel = new System.Windows.Forms.Label();
            txtMarkerMix = new System.Windows.Forms.TextBox();
            btnSetMarkerMix = new System.Windows.Forms.Button();
            btnMarkerMixUp = new System.Windows.Forms.Button();
            btnMarkerMixDown = new System.Windows.Forms.Button();
            btnPlayFromMix = new System.Windows.Forms.Button();
            lblMarkerOutLabel = new System.Windows.Forms.Label();
            txtMarkerOut = new System.Windows.Forms.TextBox();
            btnSetMarkerOut = new System.Windows.Forms.Button();
            btnMarkerOutUp = new System.Windows.Forms.Button();
            btnMarkerOutDown = new System.Windows.Forms.Button();
            btnPlayFromOut = new System.Windows.Forms.Button();

            // ✅ VU METER
            vuMeterPanel = new System.Windows.Forms.Panel();

            picWaveform = new System.Windows.Forms.PictureBox();
            hScrollWaveform = new System.Windows.Forms.HScrollBar();

            bottomPanel = new System.Windows.Forms.Panel();
            lblTitle = new System.Windows.Forms.Label();
            txtTitle = new System.Windows.Forms.TextBox();
            lblArtist = new System.Windows.Forms.Label();
            txtArtist = new System.Windows.Forms.TextBox();
            lblAlbum = new System.Windows.Forms.Label();
            txtAlbum = new System.Windows.Forms.TextBox();
            lblYear = new System.Windows.Forms.Label();
            numYear = new System.Windows.Forms.NumericUpDown();
            lblGenre = new System.Windows.Forms.Label();
            cmbGenre = new System.Windows.Forms.ComboBox();
            lblCategories = new System.Windows.Forms.Label();
            txtCategoriesDisplay = new System.Windows.Forms.TextBox();
            btnCategoriesDropdown = new System.Windows.Forms.Button();
            lblFeaturedArtists = new System.Windows.Forms.Label();
            txtFeaturedArtistsDisplay = new System.Windows.Forms.TextBox();
            btnFeaturedArtistsDropdown = new System.Windows.Forms.Button();
            lblFilePath = new System.Windows.Forms.Label();
            txtFilePath = new System.Windows.Forms.TextBox();
            grpPeriod = new System.Windows.Forms.GroupBox();
            chkEnableValidFrom = new System.Windows.Forms.CheckBox();
            dtpValidFrom = new System.Windows.Forms.DateTimePicker();
            chkEnableValidTo = new System.Windows.Forms.CheckBox();
            dtpValidTo = new System.Windows.Forms.DateTimePicker();
            grpMonths = new System.Windows.Forms.GroupBox();
            grpDays = new System.Windows.Forms.GroupBox();
            grpHours = new System.Windows.Forms.GroupBox();
            btnSave = new System.Windows.Forms.Button();
            btnCancel = new System.Windows.Forms.Button();

            toolbarPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)trkZoom).BeginInit();
            ((System.ComponentModel.ISupportInitialize)trkVolume).BeginInit();
            leftPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picWaveform).BeginInit();
            bottomPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numYear).BeginInit();
            grpPeriod.SuspendLayout();
            grpVolume.SuspendLayout();
            SuspendLayout();

            // ========== toolbarPanel ==========
            toolbarPanel.Controls.Add(btnPlay);
            toolbarPanel.Controls.Add(btnStop);
            toolbarPanel.Controls.Add(btnLoop);
            toolbarPanel.Controls.Add(zoomPanel);
            toolbarPanel.Controls.Add(grpVolume);
            toolbarPanel.Dock = System.Windows.Forms.DockStyle.Top;
            toolbarPanel.Location = new System.Drawing.Point(0, 0);
            toolbarPanel.Name = "toolbarPanel";
            toolbarPanel.Size = new System.Drawing.Size(1263, 55);
            toolbarPanel.TabIndex = 0;

            // btnPlay
            btnPlay.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnPlay.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            btnPlay.Location = new System.Drawing.Point(15, 8);
            btnPlay.Name = "btnPlay";
            btnPlay.Size = new System.Drawing.Size(90, 38);
            btnPlay.TabIndex = 0;
            btnPlay.Text = "▶ PLAY";
            btnPlay.UseVisualStyleBackColor = true;
            btnPlay.Click += btnPlay_Click;

            // btnStop
            btnStop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnStop.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            btnStop.Location = new System.Drawing.Point(115, 8);
            btnStop.Name = "btnStop";
            btnStop.Size = new System.Drawing.Size(90, 38);
            btnStop.TabIndex = 1;
            btnStop.Text = "⏹ STOP";
            btnStop.UseVisualStyleBackColor = true;
            btnStop.Click += btnStop_Click;

            // btnLoop
            btnLoop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnLoop.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            btnLoop.Location = new System.Drawing.Point(1161, 8);
            btnLoop.Name = "btnLoop";
            btnLoop.Size = new System.Drawing.Size(90, 38);
            btnLoop.TabIndex = 2;
            btnLoop.Text = "🔁 LOOP";
            btnLoop.UseVisualStyleBackColor = true;
            btnLoop.Visible = false;

            // ========== ZOOM PANEL (in toolbar) ==========
            zoomPanel.Location = new System.Drawing.Point(220, 3);
            zoomPanel.Name = "zoomPanel";
            zoomPanel.Size = new System.Drawing.Size(300, 50);
            zoomPanel.TabIndex = 3;
            zoomPanel.Controls.Add(lblZoom);
            zoomPanel.Controls.Add(trkZoom);
            zoomPanel.Controls.Add(lblZoomPercent);

            lblZoom.Text = "🔍 Zoom:";
            lblZoom.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            lblZoom.ForeColor = System.Drawing.Color.White;
            lblZoom.Location = new System.Drawing.Point(0, 15);
            lblZoom.Size = new System.Drawing.Size(60, 20);
            lblZoom.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            trkZoom.Location = new System.Drawing.Point(62, 5);
            trkZoom.Name = "trkZoom";
            trkZoom.Size = new System.Drawing.Size(180, 45);
            trkZoom.Minimum = 100;
            trkZoom.Maximum = 2000;
            trkZoom.Value = 100;
            trkZoom.TickFrequency = 100;
            trkZoom.SmallChange = 10;
            trkZoom.LargeChange = 100;
            trkZoom.TabIndex = 0;

            lblZoomPercent.Text = "100%";
            lblZoomPercent.Font = new System.Drawing.Font("Consolas", 10F, System.Drawing.FontStyle.Bold);
            lblZoomPercent.ForeColor = System.Drawing.Color.Cyan;
            lblZoomPercent.Location = new System.Drawing.Point(245, 15);
            lblZoomPercent.Size = new System.Drawing.Size(55, 20);
            lblZoomPercent.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // ========== VOLUME GROUP (to the right of zoom, same row) ==========
            grpVolume.Text = "🔊 Volume Boost";
            grpVolume.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            grpVolume.ForeColor = System.Drawing.Color.White;
            grpVolume.Location = new System.Drawing.Point(530, 0);
            grpVolume.Size = new System.Drawing.Size(560, 50);
            grpVolume.TabIndex = 4;
            grpVolume.Controls.Add(trkVolume);
            grpVolume.Controls.Add(lblVolumeDb);
            grpVolume.Controls.Add(btnApplyVolume);
            grpVolume.Controls.Add(chkColoredPeaks);

            trkVolume.Location = new System.Drawing.Point(10, 18);
            trkVolume.Name = "trkVolume";
            trkVolume.Size = new System.Drawing.Size(240, 30);
            trkVolume.Minimum = -20;
            trkVolume.Maximum = 20;
            trkVolume.Value = 0;
            trkVolume.TickFrequency = 2;
            trkVolume.SmallChange = 1;
            trkVolume.LargeChange = 3;
            trkVolume.TabIndex = 0;

            lblVolumeDb.Text = "0 dB";
            lblVolumeDb.Font = new System.Drawing.Font("Consolas", 11F, System.Drawing.FontStyle.Bold);
            lblVolumeDb.ForeColor = System.Drawing.Color.Lime;
            lblVolumeDb.Location = new System.Drawing.Point(255, 20);
            lblVolumeDb.Size = new System.Drawing.Size(65, 25);
            lblVolumeDb.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            btnApplyVolume.Text = "APPLY";
            btnApplyVolume.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            btnApplyVolume.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnApplyVolume.BackColor = System.Drawing.Color.FromArgb(200, 120, 0);
            btnApplyVolume.ForeColor = System.Drawing.Color.White;
            btnApplyVolume.Location = new System.Drawing.Point(325, 17);
            btnApplyVolume.Size = new System.Drawing.Size(85, 30);
            btnApplyVolume.TabIndex = 1;
            btnApplyVolume.FlatAppearance.BorderSize = 0;
            btnApplyVolume.Cursor = System.Windows.Forms.Cursors.Hand;

            chkColoredPeaks.Text = "🎨 Picchi colorati";
            chkColoredPeaks.Name = "chkColoredPeaks";
            chkColoredPeaks.Font = new System.Drawing.Font("Segoe UI", 8F);
            chkColoredPeaks.ForeColor = System.Drawing.Color.White;
            chkColoredPeaks.Location = new System.Drawing.Point(420, 19);
            chkColoredPeaks.Size = new System.Drawing.Size(130, 22);
            chkColoredPeaks.TabIndex = 2;
            chkColoredPeaks.Checked = false;
            chkColoredPeaks.UseVisualStyleBackColor = false;
            chkColoredPeaks.BackColor = System.Drawing.Color.Transparent;
            chkColoredPeaks.Cursor = System.Windows.Forms.Cursors.Hand;
            chkColoredPeaks.CheckedChanged += new System.EventHandler(this.ChkColoredPeaks_CheckedChanged);

            // ========== leftPanel ==========
            leftPanel.Controls.Add(lblCurrentPosition);
            leftPanel.Controls.Add(lblCurrentPositionMs);
            leftPanel.Controls.Add(lblTotalDuration);
            leftPanel.Controls.Add(lblMarkerInLabel);
            leftPanel.Controls.Add(txtMarkerIn);
            leftPanel.Controls.Add(btnSetMarkerIn);
            leftPanel.Controls.Add(btnMarkerInUp);
            leftPanel.Controls.Add(btnMarkerInDown);
            leftPanel.Controls.Add(btnPlayFromIn);
            leftPanel.Controls.Add(lblMarkerIntroLabel);
            leftPanel.Controls.Add(txtMarkerIntro);
            leftPanel.Controls.Add(btnSetMarkerIntro);
            leftPanel.Controls.Add(btnMarkerIntroUp);
            leftPanel.Controls.Add(btnMarkerIntroDown);
            leftPanel.Controls.Add(btnPlayFromIntro);
            leftPanel.Controls.Add(lblMarkerMixLabel);
            leftPanel.Controls.Add(txtMarkerMix);
            leftPanel.Controls.Add(btnSetMarkerMix);
            leftPanel.Controls.Add(btnMarkerMixUp);
            leftPanel.Controls.Add(btnMarkerMixDown);
            leftPanel.Controls.Add(btnPlayFromMix);
            leftPanel.Controls.Add(lblMarkerOutLabel);
            leftPanel.Controls.Add(txtMarkerOut);
            leftPanel.Controls.Add(btnSetMarkerOut);
            leftPanel.Controls.Add(btnMarkerOutUp);
            leftPanel.Controls.Add(btnMarkerOutDown);
            leftPanel.Controls.Add(btnPlayFromOut);
            leftPanel.Dock = System.Windows.Forms.DockStyle.Left;
            leftPanel.Location = new System.Drawing.Point(0, 55);
            leftPanel.Name = "leftPanel";
            leftPanel.Size = new System.Drawing.Size(380, 355);
            leftPanel.TabIndex = 1;

            // lblCurrentPosition
            lblCurrentPosition.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            lblCurrentPosition.Font = new System.Drawing.Font("Consolas", 16F, System.Drawing.FontStyle.Bold);
            lblCurrentPosition.Location = new System.Drawing.Point(16, 182);
            lblCurrentPosition.Name = "lblCurrentPosition";
            lblCurrentPosition.Size = new System.Drawing.Size(220, 40);
            lblCurrentPosition.TabIndex = 0;
            lblCurrentPosition.Text = "00:00:00.000";
            lblCurrentPosition.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            // lblCurrentPositionMs
            lblCurrentPositionMs.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            lblCurrentPositionMs.Font = new System.Drawing.Font("Consolas", 10F, System.Drawing.FontStyle.Bold);
            lblCurrentPositionMs.Location = new System.Drawing.Point(246, 182);
            lblCurrentPositionMs.Name = "lblCurrentPositionMs";
            lblCurrentPositionMs.Size = new System.Drawing.Size(120, 40);
            lblCurrentPositionMs.TabIndex = 1;
            lblCurrentPositionMs.Text = "0 ms";
            lblCurrentPositionMs.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            // lblTotalDuration
            lblTotalDuration.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            lblTotalDuration.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Bold);
            lblTotalDuration.Location = new System.Drawing.Point(15, 234);
            lblTotalDuration.Name = "lblTotalDuration";
            lblTotalDuration.Size = new System.Drawing.Size(350, 35);
            lblTotalDuration.TabIndex = 2;
            lblTotalDuration.Text = "00:00:00.000";
            lblTotalDuration.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            // lblMarkerInLabel
            lblMarkerInLabel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            lblMarkerInLabel.ForeColor = System.Drawing.Color.Red;
            lblMarkerInLabel.Location = new System.Drawing.Point(9, 15);
            lblMarkerInLabel.Name = "lblMarkerInLabel";
            lblMarkerInLabel.Size = new System.Drawing.Size(50, 28);
            lblMarkerInLabel.TabIndex = 3;
            lblMarkerInLabel.Text = "IN";
            lblMarkerInLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

            // txtMarkerIn
            txtMarkerIn.Font = new System.Drawing.Font("Consolas", 12F);
            txtMarkerIn.Location = new System.Drawing.Point(64, 15);
            txtMarkerIn.Name = "txtMarkerIn";
            txtMarkerIn.ReadOnly = true;
            txtMarkerIn.Size = new System.Drawing.Size(141, 26);
            txtMarkerIn.TabIndex = 4;
            txtMarkerIn.Text = "00:00:00.000";
            txtMarkerIn.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;

            // btnSetMarkerIn
            btnSetMarkerIn.BackColor = System.Drawing.SystemColors.ControlLightLight;
            btnSetMarkerIn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnSetMarkerIn.Font = new System.Drawing.Font("Arial", 10F, System.Drawing.FontStyle.Bold);
            btnSetMarkerIn.Location = new System.Drawing.Point(213, 15);
            btnSetMarkerIn.Name = "btnSetMarkerIn";
            btnSetMarkerIn.Size = new System.Drawing.Size(30, 28);
            btnSetMarkerIn.TabIndex = 5;
            btnSetMarkerIn.Text = "⬇";
            btnSetMarkerIn.UseVisualStyleBackColor = false;
            btnSetMarkerIn.Click += btnSetMarkerIn_Click;

            // btnMarkerInUp
            btnMarkerInUp.BackColor = System.Drawing.SystemColors.ControlLightLight;
            btnMarkerInUp.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnMarkerInUp.Font = new System.Drawing.Font("Arial", 7F, System.Drawing.FontStyle.Bold);
            btnMarkerInUp.Location = new System.Drawing.Point(274, 10);
            btnMarkerInUp.Name = "btnMarkerInUp";
            btnMarkerInUp.Size = new System.Drawing.Size(22, 26);
            btnMarkerInUp.TabIndex = 6;
            btnMarkerInUp.Text = "▲";
            btnMarkerInUp.UseVisualStyleBackColor = false;
            btnMarkerInUp.Click += btnMarkerInUp_Click;

            // btnMarkerInDown
            btnMarkerInDown.BackColor = System.Drawing.SystemColors.ControlLightLight;
            btnMarkerInDown.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnMarkerInDown.Font = new System.Drawing.Font("Arial", 7F, System.Drawing.FontStyle.Bold);
            btnMarkerInDown.Location = new System.Drawing.Point(249, 20);
            btnMarkerInDown.Name = "btnMarkerInDown";
            btnMarkerInDown.Size = new System.Drawing.Size(22, 26);
            btnMarkerInDown.TabIndex = 7;
            btnMarkerInDown.Text = "▼";
            btnMarkerInDown.UseVisualStyleBackColor = false;
            btnMarkerInDown.Click += btnMarkerInDown_Click;

            // btnPlayFromIn
            btnPlayFromIn.BackColor = System.Drawing.Color.LawnGreen;
            btnPlayFromIn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnPlayFromIn.Font = new System.Drawing.Font("Arial", 10F, System.Drawing.FontStyle.Bold);
            btnPlayFromIn.Location = new System.Drawing.Point(301, 15);
            btnPlayFromIn.Name = "btnPlayFromIn";
            btnPlayFromIn.Size = new System.Drawing.Size(30, 28);
            btnPlayFromIn.TabIndex = 8;
            btnPlayFromIn.Text = "▶";
            btnPlayFromIn.UseVisualStyleBackColor = false;
            btnPlayFromIn.Click += btnPlayFromIn_Click;

            // lblMarkerIntroLabel
            lblMarkerIntroLabel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            lblMarkerIntroLabel.ForeColor = System.Drawing.Color.Magenta;
            lblMarkerIntroLabel.Location = new System.Drawing.Point(-4, 55);
            lblMarkerIntroLabel.Name = "lblMarkerIntroLabel";
            lblMarkerIntroLabel.Size = new System.Drawing.Size(63, 28);
            lblMarkerIntroLabel.TabIndex = 9;
            lblMarkerIntroLabel.Text = "INTRO";
            lblMarkerIntroLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

            // txtMarkerIntro
            txtMarkerIntro.Font = new System.Drawing.Font("Consolas", 12F);
            txtMarkerIntro.Location = new System.Drawing.Point(64, 55);
            txtMarkerIntro.Name = "txtMarkerIntro";
            txtMarkerIntro.ReadOnly = true;
            txtMarkerIntro.Size = new System.Drawing.Size(141, 26);
            txtMarkerIntro.TabIndex = 10;
            txtMarkerIntro.Text = "00:00:00.000";
            txtMarkerIntro.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;

            // btnSetMarkerIntro
            btnSetMarkerIntro.BackColor = System.Drawing.SystemColors.ControlLightLight;
            btnSetMarkerIntro.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnSetMarkerIntro.Font = new System.Drawing.Font("Arial", 10F, System.Drawing.FontStyle.Bold);
            btnSetMarkerIntro.Location = new System.Drawing.Point(213, 55);
            btnSetMarkerIntro.Name = "btnSetMarkerIntro";
            btnSetMarkerIntro.Size = new System.Drawing.Size(30, 28);
            btnSetMarkerIntro.TabIndex = 11;
            btnSetMarkerIntro.Text = "⬇";
            btnSetMarkerIntro.UseVisualStyleBackColor = false;
            btnSetMarkerIntro.Click += btnSetMarkerIntro_Click;

            // btnMarkerIntroUp
            btnMarkerIntroUp.BackColor = System.Drawing.SystemColors.ControlLightLight;
            btnMarkerIntroUp.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnMarkerIntroUp.Font = new System.Drawing.Font("Arial", 7F, System.Drawing.FontStyle.Bold);
            btnMarkerIntroUp.Location = new System.Drawing.Point(274, 50);
            btnMarkerIntroUp.Name = "btnMarkerIntroUp";
            btnMarkerIntroUp.Size = new System.Drawing.Size(21, 26);
            btnMarkerIntroUp.TabIndex = 12;
            btnMarkerIntroUp.Text = "▲";
            btnMarkerIntroUp.UseVisualStyleBackColor = false;
            btnMarkerIntroUp.Click += btnMarkerIntroUp_Click;

            // btnMarkerIntroDown
            btnMarkerIntroDown.BackColor = System.Drawing.SystemColors.ControlLightLight;
            btnMarkerIntroDown.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnMarkerIntroDown.Font = new System.Drawing.Font("Arial", 7F, System.Drawing.FontStyle.Bold);
            btnMarkerIntroDown.Location = new System.Drawing.Point(249, 60);
            btnMarkerIntroDown.Name = "btnMarkerIntroDown";
            btnMarkerIntroDown.Size = new System.Drawing.Size(22, 26);
            btnMarkerIntroDown.TabIndex = 13;
            btnMarkerIntroDown.Text = "▼";
            btnMarkerIntroDown.UseVisualStyleBackColor = false;
            btnMarkerIntroDown.Click += btnMarkerIntroDown_Click;

            // btnPlayFromIntro
            btnPlayFromIntro.BackColor = System.Drawing.Color.LawnGreen;
            btnPlayFromIntro.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnPlayFromIntro.Font = new System.Drawing.Font("Arial", 10F, System.Drawing.FontStyle.Bold);
            btnPlayFromIntro.Location = new System.Drawing.Point(301, 55);
            btnPlayFromIntro.Name = "btnPlayFromIntro";
            btnPlayFromIntro.Size = new System.Drawing.Size(30, 28);
            btnPlayFromIntro.TabIndex = 14;
            btnPlayFromIntro.Text = "▶";
            btnPlayFromIntro.UseVisualStyleBackColor = false;
            btnPlayFromIntro.Click += btnPlayFromIntro_Click;

            // lblMarkerMixLabel
            lblMarkerMixLabel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            lblMarkerMixLabel.ForeColor = System.Drawing.Color.Yellow;
            lblMarkerMixLabel.Location = new System.Drawing.Point(9, 95);
            lblMarkerMixLabel.Name = "lblMarkerMixLabel";
            lblMarkerMixLabel.Size = new System.Drawing.Size(50, 28);
            lblMarkerMixLabel.TabIndex = 15;
            lblMarkerMixLabel.Text = "MIX";
            lblMarkerMixLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

            // txtMarkerMix
            txtMarkerMix.Font = new System.Drawing.Font("Consolas", 12F);
            txtMarkerMix.Location = new System.Drawing.Point(64, 95);
            txtMarkerMix.Name = "txtMarkerMix";
            txtMarkerMix.ReadOnly = true;
            txtMarkerMix.Size = new System.Drawing.Size(141, 26);
            txtMarkerMix.TabIndex = 16;
            txtMarkerMix.Text = "00:00:00.000";
            txtMarkerMix.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;

            // btnSetMarkerMix
            btnSetMarkerMix.BackColor = System.Drawing.SystemColors.ControlLightLight;
            btnSetMarkerMix.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnSetMarkerMix.Font = new System.Drawing.Font("Arial", 10F, System.Drawing.FontStyle.Bold);
            btnSetMarkerMix.Location = new System.Drawing.Point(213, 95);
            btnSetMarkerMix.Name = "btnSetMarkerMix";
            btnSetMarkerMix.Size = new System.Drawing.Size(30, 28);
            btnSetMarkerMix.TabIndex = 17;
            btnSetMarkerMix.Text = "⬇";
            btnSetMarkerMix.UseVisualStyleBackColor = false;
            btnSetMarkerMix.Click += btnSetMarkerMix_Click;

            // btnMarkerMixUp
            btnMarkerMixUp.BackColor = System.Drawing.SystemColors.ControlLightLight;
            btnMarkerMixUp.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnMarkerMixUp.Font = new System.Drawing.Font("Arial", 7F, System.Drawing.FontStyle.Bold);
            btnMarkerMixUp.Location = new System.Drawing.Point(274, 90);
            btnMarkerMixUp.Name = "btnMarkerMixUp";
            btnMarkerMixUp.Size = new System.Drawing.Size(22, 26);
            btnMarkerMixUp.TabIndex = 18;
            btnMarkerMixUp.Text = "▲";
            btnMarkerMixUp.UseVisualStyleBackColor = false;
            btnMarkerMixUp.Click += btnMarkerMixUp_Click;

            // btnMarkerMixDown
            btnMarkerMixDown.BackColor = System.Drawing.SystemColors.ControlLightLight;
            btnMarkerMixDown.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnMarkerMixDown.Font = new System.Drawing.Font("Arial", 7F, System.Drawing.FontStyle.Bold);
            btnMarkerMixDown.Location = new System.Drawing.Point(249, 100);
            btnMarkerMixDown.Name = "btnMarkerMixDown";
            btnMarkerMixDown.Size = new System.Drawing.Size(22, 26);
            btnMarkerMixDown.TabIndex = 19;
            btnMarkerMixDown.Text = "▼";
            btnMarkerMixDown.UseVisualStyleBackColor = false;
            btnMarkerMixDown.Click += btnMarkerMixDown_Click;

            // btnPlayFromMix
            btnPlayFromMix.BackColor = System.Drawing.Color.LawnGreen;
            btnPlayFromMix.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnPlayFromMix.Font = new System.Drawing.Font("Arial", 10F, System.Drawing.FontStyle.Bold);
            btnPlayFromMix.Location = new System.Drawing.Point(301, 95);
            btnPlayFromMix.Name = "btnPlayFromMix";
            btnPlayFromMix.Size = new System.Drawing.Size(30, 28);
            btnPlayFromMix.TabIndex = 20;
            btnPlayFromMix.Text = "▶";
            btnPlayFromMix.UseVisualStyleBackColor = false;
            btnPlayFromMix.Click += btnPlayFromMix_Click;

            // lblMarkerOutLabel
            lblMarkerOutLabel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            lblMarkerOutLabel.ForeColor = System.Drawing.Color.FromArgb(255, 140, 0);
            lblMarkerOutLabel.Location = new System.Drawing.Point(9, 135);
            lblMarkerOutLabel.Name = "lblMarkerOutLabel";
            lblMarkerOutLabel.Size = new System.Drawing.Size(50, 28);
            lblMarkerOutLabel.TabIndex = 21;
            lblMarkerOutLabel.Text = "OUT";
            lblMarkerOutLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

            // txtMarkerOut
            txtMarkerOut.Font = new System.Drawing.Font("Consolas", 12F);
            txtMarkerOut.Location = new System.Drawing.Point(64, 135);
            txtMarkerOut.Name = "txtMarkerOut";
            txtMarkerOut.ReadOnly = true;
            txtMarkerOut.Size = new System.Drawing.Size(141, 26);
            txtMarkerOut.TabIndex = 22;
            txtMarkerOut.Text = "00:00:00.000";
            txtMarkerOut.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;

            // btnSetMarkerOut
            btnSetMarkerOut.BackColor = System.Drawing.SystemColors.ControlLightLight;
            btnSetMarkerOut.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnSetMarkerOut.Font = new System.Drawing.Font("Arial", 10F, System.Drawing.FontStyle.Bold);
            btnSetMarkerOut.Location = new System.Drawing.Point(213, 135);
            btnSetMarkerOut.Name = "btnSetMarkerOut";
            btnSetMarkerOut.Size = new System.Drawing.Size(30, 28);
            btnSetMarkerOut.TabIndex = 23;
            btnSetMarkerOut.Text = "⬇";
            btnSetMarkerOut.UseVisualStyleBackColor = false;
            btnSetMarkerOut.Click += btnSetMarkerOut_Click;

            // btnMarkerOutUp
            btnMarkerOutUp.BackColor = System.Drawing.SystemColors.ControlLightLight;
            btnMarkerOutUp.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnMarkerOutUp.Font = new System.Drawing.Font("Arial", 7F, System.Drawing.FontStyle.Bold);
            btnMarkerOutUp.Location = new System.Drawing.Point(274, 131);
            btnMarkerOutUp.Name = "btnMarkerOutUp";
            btnMarkerOutUp.Size = new System.Drawing.Size(22, 26);
            btnMarkerOutUp.TabIndex = 24;
            btnMarkerOutUp.Text = "▲";
            btnMarkerOutUp.UseVisualStyleBackColor = false;
            btnMarkerOutUp.Click += btnMarkerOutUp_Click;

            // btnMarkerOutDown
            btnMarkerOutDown.BackColor = System.Drawing.SystemColors.ControlLightLight;
            btnMarkerOutDown.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnMarkerOutDown.Font = new System.Drawing.Font("Arial", 7F, System.Drawing.FontStyle.Bold);
            btnMarkerOutDown.Location = new System.Drawing.Point(249, 140);
            btnMarkerOutDown.Name = "btnMarkerOutDown";
            btnMarkerOutDown.Size = new System.Drawing.Size(22, 26);
            btnMarkerOutDown.TabIndex = 25;
            btnMarkerOutDown.Text = "▼";
            btnMarkerOutDown.UseVisualStyleBackColor = false;
            btnMarkerOutDown.Click += btnMarkerOutDown_Click;

            // btnPlayFromOut
            btnPlayFromOut.BackColor = System.Drawing.Color.LawnGreen;
            btnPlayFromOut.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnPlayFromOut.Font = new System.Drawing.Font("Arial", 10F, System.Drawing.FontStyle.Bold);
            btnPlayFromOut.Location = new System.Drawing.Point(301, 135);
            btnPlayFromOut.Name = "btnPlayFromOut";
            btnPlayFromOut.Size = new System.Drawing.Size(30, 28);
            btnPlayFromOut.TabIndex = 26;
            btnPlayFromOut.Text = "▶";
            btnPlayFromOut.UseVisualStyleBackColor = false;
            btnPlayFromOut.Click += btnPlayFromOut_Click;

            // ========== VU METER PANEL ==========
            vuMeterPanel.Dock = System.Windows.Forms.DockStyle.Top;
            vuMeterPanel.Location = new System.Drawing.Point(380, 55);
            vuMeterPanel.Name = "vuMeterPanel";
            vuMeterPanel.Size = new System.Drawing.Size(883, 24);
            vuMeterPanel.BackColor = System.Drawing.Color.FromArgb(15, 15, 15);
            vuMeterPanel.TabIndex = 10;

            // ========== picWaveform ==========
            picWaveform.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            picWaveform.Dock = System.Windows.Forms.DockStyle.Fill;
            picWaveform.Location = new System.Drawing.Point(380, 79);
            picWaveform.Name = "picWaveform";
            picWaveform.Size = new System.Drawing.Size(883, 331);
            picWaveform.TabIndex = 2;
            picWaveform.TabStop = false;
            picWaveform.Paint += picWaveform_Paint;
            picWaveform.MouseDown += picWaveform_MouseDown;
            picWaveform.MouseMove += picWaveform_MouseMove;
            picWaveform.MouseUp += picWaveform_MouseUp;

            // ========== hScrollWaveform ==========
            hScrollWaveform.Dock = System.Windows.Forms.DockStyle.Bottom;
            hScrollWaveform.Location = new System.Drawing.Point(380, 390);
            hScrollWaveform.Name = "hScrollWaveform";
            hScrollWaveform.Size = new System.Drawing.Size(883, 20);
            hScrollWaveform.TabIndex = 11;
            hScrollWaveform.Visible = false;
            hScrollWaveform.Minimum = 0;
            hScrollWaveform.Maximum = 100;
            hScrollWaveform.LargeChange = 10;
            hScrollWaveform.SmallChange = 1;

            // ========== bottomPanel ==========
            bottomPanel.AutoScroll = true;
            bottomPanel.Controls.Add(lblTitle);
            bottomPanel.Controls.Add(txtTitle);
            bottomPanel.Controls.Add(lblArtist);
            bottomPanel.Controls.Add(txtArtist);
            bottomPanel.Controls.Add(lblAlbum);
            bottomPanel.Controls.Add(txtAlbum);
            bottomPanel.Controls.Add(lblYear);
            bottomPanel.Controls.Add(numYear);
            bottomPanel.Controls.Add(lblGenre);
            bottomPanel.Controls.Add(cmbGenre);
            bottomPanel.Controls.Add(lblCategories);
            bottomPanel.Controls.Add(txtCategoriesDisplay);
            bottomPanel.Controls.Add(btnCategoriesDropdown);
            bottomPanel.Controls.Add(lblFeaturedArtists);
            bottomPanel.Controls.Add(txtFeaturedArtistsDisplay);
            bottomPanel.Controls.Add(btnFeaturedArtistsDropdown);
            bottomPanel.Controls.Add(lblFilePath);
            bottomPanel.Controls.Add(txtFilePath);
            bottomPanel.Controls.Add(grpPeriod);
            bottomPanel.Controls.Add(grpMonths);
            bottomPanel.Controls.Add(grpDays);
            bottomPanel.Controls.Add(grpHours);
            bottomPanel.Controls.Add(btnSave);
            bottomPanel.Controls.Add(btnCancel);
            bottomPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            bottomPanel.Location = new System.Drawing.Point(0, 410);
            bottomPanel.Name = "bottomPanel";
            bottomPanel.Size = new System.Drawing.Size(1263, 317);
            bottomPanel.TabIndex = 3;

            // lblTitle
            lblTitle.AutoSize = true;
            lblTitle.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            lblTitle.Location = new System.Drawing.Point(15, 18);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new System.Drawing.Size(42, 15);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Titolo:";

            // txtTitle
            txtTitle.Font = new System.Drawing.Font("Segoe UI", 10F);
            txtTitle.Location = new System.Drawing.Point(100, 15);
            txtTitle.Name = "txtTitle";
            txtTitle.Size = new System.Drawing.Size(420, 25);
            txtTitle.TabIndex = 1;

            // lblArtist
            lblArtist.AutoSize = true;
            lblArtist.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            lblArtist.Location = new System.Drawing.Point(15, 53);
            lblArtist.Name = "lblArtist";
            lblArtist.Size = new System.Drawing.Size(47, 15);
            lblArtist.TabIndex = 2;
            lblArtist.Text = "Artista:";

            // txtArtist
            txtArtist.Font = new System.Drawing.Font("Segoe UI", 10F);
            txtArtist.Location = new System.Drawing.Point(100, 50);
            txtArtist.Name = "txtArtist";
            txtArtist.Size = new System.Drawing.Size(420, 25);
            txtArtist.TabIndex = 3;

            // lblAlbum
            lblAlbum.AutoSize = true;
            lblAlbum.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            lblAlbum.Location = new System.Drawing.Point(540, 18);
            lblAlbum.Name = "lblAlbum";
            lblAlbum.Size = new System.Drawing.Size(46, 15);
            lblAlbum.TabIndex = 4;
            lblAlbum.Text = "Album:";

            // txtAlbum
            txtAlbum.Font = new System.Drawing.Font("Segoe UI", 10F);
            txtAlbum.Location = new System.Drawing.Point(620, 15);
            txtAlbum.Name = "txtAlbum";
            txtAlbum.Size = new System.Drawing.Size(280, 25);
            txtAlbum.TabIndex = 5;

            // lblYear
            lblYear.AutoSize = true;
            lblYear.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            lblYear.Location = new System.Drawing.Point(920, 18);
            lblYear.Name = "lblYear";
            lblYear.Size = new System.Drawing.Size(39, 15);
            lblYear.TabIndex = 6;
            lblYear.Text = "Anno:";

            // numYear
            numYear.Font = new System.Drawing.Font("Segoe UI", 10F);
            numYear.Location = new System.Drawing.Point(980, 15);
            numYear.Maximum = new decimal(new int[] { 2100, 0, 0, 0 });
            numYear.Minimum = new decimal(new int[] { 1900, 0, 0, 0 });
            numYear.Name = "numYear";
            numYear.Size = new System.Drawing.Size(100, 25);
            numYear.TabIndex = 7;
            numYear.Value = new decimal(new int[] { 2024, 0, 0, 0 });

            // lblGenre
            lblGenre.AutoSize = true;
            lblGenre.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            lblGenre.Location = new System.Drawing.Point(540, 53);
            lblGenre.Name = "lblGenre";
            lblGenre.Size = new System.Drawing.Size(52, 15);
            lblGenre.TabIndex = 8;
            lblGenre.Text = "Genere:";

            // cmbGenre
            cmbGenre.Font = new System.Drawing.Font("Segoe UI", 10F);
            cmbGenre.FormattingEnabled = true;
            cmbGenre.Location = new System.Drawing.Point(620, 50);
            cmbGenre.Name = "cmbGenre";
            cmbGenre.Size = new System.Drawing.Size(200, 25);
            cmbGenre.TabIndex = 9;

            // lblCategories
            lblCategories.AutoSize = true;
            lblCategories.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            lblCategories.Location = new System.Drawing.Point(840, 53);
            lblCategories.Name = "lblCategories";
            lblCategories.Size = new System.Drawing.Size(64, 15);
            lblCategories.TabIndex = 10;
            lblCategories.Text = "Categorie:";

            // txtCategoriesDisplay
            txtCategoriesDisplay.Font = new System.Drawing.Font("Segoe UI", 10F);
            txtCategoriesDisplay.Location = new System.Drawing.Point(920, 50);
            txtCategoriesDisplay.Name = "txtCategoriesDisplay";
            txtCategoriesDisplay.Size = new System.Drawing.Size(270, 25);
            txtCategoriesDisplay.TabIndex = 11;
            txtCategoriesDisplay.ReadOnly = true;
            txtCategoriesDisplay.Cursor = System.Windows.Forms.Cursors.Hand;
            txtCategoriesDisplay.Click += (s, e) => ShowCategoryPopup();

            // btnCategoriesDropdown
            btnCategoriesDropdown.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnCategoriesDropdown.Font = new System.Drawing.Font("Segoe UI", 9F);
            btnCategoriesDropdown.Location = new System.Drawing.Point(1190, 50);
            btnCategoriesDropdown.Name = "btnCategoriesDropdown";
            btnCategoriesDropdown.Size = new System.Drawing.Size(30, 25);
            btnCategoriesDropdown.TabIndex = 12;
            btnCategoriesDropdown.Text = "▼";
            btnCategoriesDropdown.UseVisualStyleBackColor = true;
            btnCategoriesDropdown.Click += (s, e) => ShowCategoryPopup();

            // lblFeaturedArtists
            lblFeaturedArtists.AutoSize = true;
            lblFeaturedArtists.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            lblFeaturedArtists.Location = new System.Drawing.Point(15, 88);
            lblFeaturedArtists.Name = "lblFeaturedArtists";
            lblFeaturedArtists.Size = new System.Drawing.Size(80, 15);
            lblFeaturedArtists.TabIndex = 15;
            lblFeaturedArtists.Text = "Artisti Feat.:";

            // txtFeaturedArtistsDisplay
            txtFeaturedArtistsDisplay.Font = new System.Drawing.Font("Segoe UI", 10F);
            txtFeaturedArtistsDisplay.Location = new System.Drawing.Point(100, 85);
            txtFeaturedArtistsDisplay.Name = "txtFeaturedArtistsDisplay";
            txtFeaturedArtistsDisplay.Size = new System.Drawing.Size(420, 25);
            txtFeaturedArtistsDisplay.TabIndex = 16;
            txtFeaturedArtistsDisplay.ReadOnly = true;
            txtFeaturedArtistsDisplay.Cursor = System.Windows.Forms.Cursors.Hand;
            txtFeaturedArtistsDisplay.Click += (s, e) => { if (!_isClip) ShowFeaturedArtistsPopup(); };

            // btnFeaturedArtistsDropdown
            btnFeaturedArtistsDropdown.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnFeaturedArtistsDropdown.Font = new System.Drawing.Font("Segoe UI", 9F);
            btnFeaturedArtistsDropdown.Location = new System.Drawing.Point(520, 85);
            btnFeaturedArtistsDropdown.Name = "btnFeaturedArtistsDropdown";
            btnFeaturedArtistsDropdown.Size = new System.Drawing.Size(30, 25);
            btnFeaturedArtistsDropdown.TabIndex = 17;
            btnFeaturedArtistsDropdown.Text = "▼";
            btnFeaturedArtistsDropdown.UseVisualStyleBackColor = true;
            btnFeaturedArtistsDropdown.Click += (s, e) => { if (!_isClip) ShowFeaturedArtistsPopup(); };

            // lblFilePath
            lblFilePath.AutoSize = true;
            lblFilePath.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            lblFilePath.Location = new System.Drawing.Point(25, 261);
            lblFilePath.Name = "lblFilePath";
            lblFilePath.Size = new System.Drawing.Size(64, 15);
            lblFilePath.TabIndex = 13;
            lblFilePath.Text = "File Audio:";

            // txtFilePath
            txtFilePath.Font = new System.Drawing.Font("Consolas", 9F);
            txtFilePath.Location = new System.Drawing.Point(110, 258);
            txtFilePath.Name = "txtFilePath";
            txtFilePath.ReadOnly = true;
            txtFilePath.Size = new System.Drawing.Size(800, 22);
            txtFilePath.TabIndex = 14;

            // grpPeriod
            grpPeriod.Controls.Add(chkEnableValidFrom);
            grpPeriod.Controls.Add(dtpValidFrom);
            grpPeriod.Controls.Add(chkEnableValidTo);
            grpPeriod.Controls.Add(dtpValidTo);
            grpPeriod.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            grpPeriod.Location = new System.Drawing.Point(15, 95);
            grpPeriod.Name = "grpPeriod";
            grpPeriod.Size = new System.Drawing.Size(365, 55);
            grpPeriod.TabIndex = 14;
            grpPeriod.TabStop = false;
            grpPeriod.Text = "📅 Periodo Validità";

            // chkEnableValidFrom
            chkEnableValidFrom.AutoSize = true;
            chkEnableValidFrom.Font = new System.Drawing.Font("Segoe UI", 8F);
            chkEnableValidFrom.Location = new System.Drawing.Point(8, 24);
            chkEnableValidFrom.Name = "chkEnableValidFrom";
            chkEnableValidFrom.Size = new System.Drawing.Size(40, 17);
            chkEnableValidFrom.TabIndex = 0;
            chkEnableValidFrom.Text = "Da";
            chkEnableValidFrom.UseVisualStyleBackColor = true;

            // dtpValidFrom
            dtpValidFrom.Enabled = false;
            dtpValidFrom.Font = new System.Drawing.Font("Segoe UI", 8F);
            dtpValidFrom.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            dtpValidFrom.Location = new System.Drawing.Point(60, 22);
            dtpValidFrom.Name = "dtpValidFrom";
            dtpValidFrom.Size = new System.Drawing.Size(120, 22);
            dtpValidFrom.TabIndex = 1;

            // chkEnableValidTo
            chkEnableValidTo.AutoSize = true;
            chkEnableValidTo.Font = new System.Drawing.Font("Segoe UI", 8F);
            chkEnableValidTo.Location = new System.Drawing.Point(190, 24);
            chkEnableValidTo.Name = "chkEnableValidTo";
            chkEnableValidTo.Size = new System.Drawing.Size(33, 17);
            chkEnableValidTo.TabIndex = 2;
            chkEnableValidTo.Text = "A";
            chkEnableValidTo.UseVisualStyleBackColor = true;

            // dtpValidTo
            dtpValidTo.Enabled = false;
            dtpValidTo.Font = new System.Drawing.Font("Segoe UI", 8F);
            dtpValidTo.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            dtpValidTo.Location = new System.Drawing.Point(240, 22);
            dtpValidTo.Name = "dtpValidTo";
            dtpValidTo.Size = new System.Drawing.Size(120, 22);
            dtpValidTo.TabIndex = 3;

            // grpMonths
            grpMonths.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            grpMonths.Location = new System.Drawing.Point(15, 160);
            grpMonths.Name = "grpMonths";
            grpMonths.Size = new System.Drawing.Size(680, 55);
            grpMonths.TabIndex = 15;
            grpMonths.TabStop = false;
            grpMonths.Text = "📆 Mesi Consentiti";

            // grpDays
            grpDays.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            grpDays.Location = new System.Drawing.Point(736, 160);
            grpDays.Name = "grpDays";
            grpDays.Size = new System.Drawing.Size(500, 55);
            grpDays.TabIndex = 16;
            grpDays.TabStop = false;
            grpDays.Text = "📅 Giorni Consentiti";

            // grpHours
            grpHours.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            grpHours.Location = new System.Drawing.Point(386, 95);
            grpHours.Name = "grpHours";
            grpHours.Size = new System.Drawing.Size(850, 55);
            grpHours.TabIndex = 17;
            grpHours.TabStop = false;
            grpHours.Text = "🕐 Ore Consentite";

            // btnSave
            btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnSave.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            btnSave.Location = new System.Drawing.Point(1006, 245);
            btnSave.Name = "btnSave";
            btnSave.Size = new System.Drawing.Size(110, 45);
            btnSave.TabIndex = 18;
            btnSave.Text = "💾 Salva";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;

            // btnCancel
            btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnCancel.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            btnCancel.Location = new System.Drawing.Point(1126, 245);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(110, 45);
            btnCancel.TabIndex = 19;
            btnCancel.Text = "✖ Annulla";
            btnCancel.UseVisualStyleBackColor = true;

            // ========== MusicEditorForm ==========
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = btnCancel;
            this.ClientSize = new System.Drawing.Size(1263, 727);

            // Ordine di aggiunta critico per il docking corretto:
            this.Controls.Add(picWaveform);        // Fill - area centrale
            this.Controls.Add(hScrollWaveform);     // Bottom dentro area fill
            this.Controls.Add(vuMeterPanel);        // Top sotto toolbar, sopra waveform
            this.Controls.Add(leftPanel);           // Left
            this.Controls.Add(bottomPanel);         // Bottom
            this.Controls.Add(toolbarPanel);        // Top

            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "MusicEditorForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "🎵 Music Editor Professional";

            toolbarPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)trkZoom).EndInit();
            ((System.ComponentModel.ISupportInitialize)trkVolume).EndInit();
            leftPanel.ResumeLayout(false);
            leftPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)picWaveform).EndInit();
            bottomPanel.ResumeLayout(false);
            bottomPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numYear).EndInit();
            grpPeriod.ResumeLayout(false);
            grpPeriod.PerformLayout();
            grpVolume.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}