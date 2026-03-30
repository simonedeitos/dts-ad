namespace AirDirector.Forms
{
    partial class EncoderEditorForm
    {
        private System.ComponentModel.IContainer components = null;



        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.lblName = new System.Windows.Forms.Label();
            this.txtName = new System.Windows.Forms.TextBox();
            this.lblStationName = new System.Windows.Forms.Label();
            this.txtStationName = new System.Windows.Forms.TextBox();
            this.lblAudioSource = new System.Windows.Forms.Label();
            this.cboAudioSource = new System.Windows.Forms.ComboBox();
            this.lblServerUrl = new System.Windows.Forms.Label();
            this.txtServerUrl = new System.Windows.Forms.TextBox();
            this.lblServerPort = new System.Windows.Forms.Label();
            this.txtServerPort = new System.Windows.Forms.TextBox();
            this.lblUsername = new System.Windows.Forms.Label();
            this.txtUsername = new System.Windows.Forms.TextBox();
            this.lblPassword = new System.Windows.Forms.Label();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.lblMountPoint = new System.Windows.Forms.Label();
            this.txtMountPoint = new System.Windows.Forms.TextBox();
            this.lblBitrate = new System.Windows.Forms.Label();
            this.cboBitrate = new System.Windows.Forms.ComboBox();
            this.chkEnableAGC = new System.Windows.Forms.CheckBox();
            this.lblAGCTarget = new System.Windows.Forms.Label();
            this.trackAGCTarget = new System.Windows.Forms.TrackBar();
            this.lblAGCTargetValue = new System.Windows.Forms.Label();
            this.lblAGCAttack = new System.Windows.Forms.Label();
            this.trackAGCAttack = new System.Windows.Forms.TrackBar();
            this.lblAGCAttackValue = new System.Windows.Forms.Label();
            this.lblAGCRelease = new System.Windows.Forms.Label();
            this.trackAGCRelease = new System.Windows.Forms.TrackBar();
            this.lblAGCReleaseValue = new System.Windows.Forms.Label();
            this.lblLimiterThreshold = new System.Windows.Forms.Label();
            this.trackLimiterThreshold = new System.Windows.Forms.TrackBar();
            this.lblLimiterThresholdValue = new System.Windows.Forms.Label();
            this.btnResetAGC = new System.Windows.Forms.Button();
            this.btnSetLocalServer = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.trackAGCTarget)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackAGCAttack)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackAGCRelease)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackLimiterThreshold)).BeginInit();
            this.SuspendLayout();
            // 
            // lblName
            // 
            this.lblName.AutoSize = true;
            this.lblName.Location = new System.Drawing.Point(20, 20);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(100, 15);
            this.lblName.TabIndex = 0;
            this.lblName.Text = "Nome Encoder:";
            // 
            // txtName
            // 
            this.txtName.Location = new System.Drawing.Point(150, 17);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(300, 23);
            this.txtName.TabIndex = 1;
            // 
            // lblStationName
            // 
            this.lblStationName.AutoSize = true;
            this.lblStationName.Location = new System.Drawing.Point(20, 55);
            this.lblStationName.Name = "lblStationName";
            this.lblStationName.Size = new System.Drawing.Size(110, 15);
            this.lblStationName.TabIndex = 2;
            this.lblStationName.Text = "Nome Stazione:";
            // 
            // txtStationName
            // 
            this.txtStationName.Location = new System.Drawing.Point(150, 52);
            this.txtStationName.Name = "txtStationName";
            this.txtStationName.Size = new System.Drawing.Size(300, 23);
            this.txtStationName.TabIndex = 3;
            // 
            // lblAudioSource
            // 
            this.lblAudioSource.AutoSize = true;
            this.lblAudioSource.Location = new System.Drawing.Point(20, 90);
            this.lblAudioSource.Name = "lblAudioSource";
            this.lblAudioSource.Size = new System.Drawing.Size(120, 15);
            this.lblAudioSource.TabIndex = 4;
            this.lblAudioSource.Text = "Sorgente Audio:";
            // 
            // cboAudioSource
            // 
            this.cboAudioSource.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboAudioSource.FormattingEnabled = true;
            this.cboAudioSource.Location = new System.Drawing.Point(150, 87);
            this.cboAudioSource.Name = "cboAudioSource";
            this.cboAudioSource.Size = new System.Drawing.Size(300, 23);
            this.cboAudioSource.TabIndex = 5;
            // 
            // lblServerUrl
            // 
            this.lblServerUrl.AutoSize = true;
            this.lblServerUrl.Location = new System.Drawing.Point(20, 125);
            this.lblServerUrl.Name = "lblServerUrl";
            this.lblServerUrl.Size = new System.Drawing.Size(85, 15);
            this.lblServerUrl.TabIndex = 6;
            this.lblServerUrl.Text = "Server URL:";
            // 
            // txtServerUrl
            // 
            this.txtServerUrl.Location = new System.Drawing.Point(150, 122);
            this.txtServerUrl.Name = "txtServerUrl";
            this.txtServerUrl.Size = new System.Drawing.Size(200, 23);
            this.txtServerUrl.TabIndex = 7;
            // 
            // lblServerPort
            // 
            this.lblServerPort.AutoSize = true;
            this.lblServerPort.Location = new System.Drawing.Point(365, 125);
            this.lblServerPort.Name = "lblServerPort";
            this.lblServerPort.Size = new System.Drawing.Size(40, 15);
            this.lblServerPort.TabIndex = 8;
            this.lblServerPort.Text = "Porta:";
            // 
            // txtServerPort
            // 
            this.txtServerPort.Location = new System.Drawing.Point(410, 122);
            this.txtServerPort.Name = "txtServerPort";
            this.txtServerPort.Size = new System.Drawing.Size(60, 23);
            this.txtServerPort.TabIndex = 9;
            // 
            // lblUsername
            // 
            this.lblUsername.AutoSize = true;
            this.lblUsername.Location = new System.Drawing.Point(20, 160);
            this.lblUsername.Name = "lblUsername";
            this.lblUsername.Size = new System.Drawing.Size(70, 15);
            this.lblUsername.TabIndex = 10;
            this.lblUsername.Text = "Username:";
            // 
            // txtUsername
            // 
            this.txtUsername.Location = new System.Drawing.Point(150, 157);
            this.txtUsername.Name = "txtUsername";
            this.txtUsername.Size = new System.Drawing.Size(150, 23);
            this.txtUsername.TabIndex = 11;
            // 
            // lblPassword
            // 
            this.lblPassword.AutoSize = true;
            this.lblPassword.Location = new System.Drawing.Point(20, 195);
            this.lblPassword.Name = "lblPassword";
            this.lblPassword.Size = new System.Drawing.Size(65, 15);
            this.lblPassword.TabIndex = 12;
            this.lblPassword.Text = "Password:";
            // 
            // txtPassword
            // 
            this.txtPassword.Location = new System.Drawing.Point(150, 192);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.PasswordChar = '*';
            this.txtPassword.Size = new System.Drawing.Size(150, 23);
            this.txtPassword.TabIndex = 13;
            // 
            // lblMountPoint
            // 
            this.lblMountPoint.AutoSize = true;
            this.lblMountPoint.Location = new System.Drawing.Point(20, 230);
            this.lblMountPoint.Name = "lblMountPoint";
            this.lblMountPoint.Size = new System.Drawing.Size(85, 15);
            this.lblMountPoint.TabIndex = 14;
            this.lblMountPoint.Text = "Mount Point:";
            // 
            // txtMountPoint
            // 
            this.txtMountPoint.Location = new System.Drawing.Point(150, 227);
            this.txtMountPoint.Name = "txtMountPoint";
            this.txtMountPoint.Size = new System.Drawing.Size(200, 23);
            this.txtMountPoint.TabIndex = 15;
            // 
            // lblBitrate
            // 
            this.lblBitrate.AutoSize = true;
            this.lblBitrate.Location = new System.Drawing.Point(20, 265);
            this.lblBitrate.Name = "lblBitrate";
            this.lblBitrate.Size = new System.Drawing.Size(50, 15);
            this.lblBitrate.TabIndex = 16;
            this.lblBitrate.Text = "Bitrate:";
            // 
            // cboBitrate
            // 
            this.cboBitrate.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboBitrate.FormattingEnabled = true;
            this.cboBitrate.Location = new System.Drawing.Point(150, 262);
            this.cboBitrate.Name = "cboBitrate";
            this.cboBitrate.Size = new System.Drawing.Size(120, 23);
            this.cboBitrate.TabIndex = 17;
            // 
            // btnSetLocalServer
            // 
            this.btnSetLocalServer.Location = new System.Drawing.Point(310, 192);
            this.btnSetLocalServer.Name = "btnSetLocalServer";
            this.btnSetLocalServer.Size = new System.Drawing.Size(140, 23);
            this.btnSetLocalServer.TabIndex = 18;
            this.btnSetLocalServer.Text = "🖥️ Server Locale";
            this.btnSetLocalServer.UseVisualStyleBackColor = true;
            this.btnSetLocalServer.Click += new System.EventHandler(this.btnSetLocalServer_Click);
            // 
            // chkEnableAGC
            // 
            this.chkEnableAGC.AutoSize = true;
            this.chkEnableAGC.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.chkEnableAGC.Location = new System.Drawing.Point(20, 310);
            this.chkEnableAGC.Name = "chkEnableAGC";
            this.chkEnableAGC.Size = new System.Drawing.Size(180, 19);
            this.chkEnableAGC.TabIndex = 19;
            this.chkEnableAGC.Text = "Abilita AGC + Limiter";
            this.chkEnableAGC.UseVisualStyleBackColor = true;
            this.chkEnableAGC.CheckedChanged += new System.EventHandler(this.chkEnableAGC_CheckedChanged);
            // 
            // lblAGCTarget
            // 
            this.lblAGCTarget.AutoSize = true;
            this.lblAGCTarget.Location = new System.Drawing.Point(20, 345);
            this.lblAGCTarget.Name = "lblAGCTarget";
            this.lblAGCTarget.Size = new System.Drawing.Size(90, 15);
            this.lblAGCTarget.TabIndex = 20;
            this.lblAGCTarget.Text = "AGC Target:";
            // 
            // trackAGCTarget
            // 
            this.trackAGCTarget.Location = new System.Drawing.Point(150, 340);
            this.trackAGCTarget.Maximum = 100;
            this.trackAGCTarget.Minimum = 1;
            this.trackAGCTarget.Name = "trackAGCTarget";
            this.trackAGCTarget.Size = new System.Drawing.Size(250, 45);
            this.trackAGCTarget.TabIndex = 21;
            this.trackAGCTarget.TickFrequency = 10;
            this.trackAGCTarget.Value = 20;
            this.trackAGCTarget.ValueChanged += new System.EventHandler(this.trackAGCTarget_ValueChanged);
            // 
            // lblAGCTargetValue
            // 
            this.lblAGCTargetValue.AutoSize = true;
            this.lblAGCTargetValue.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblAGCTargetValue.Location = new System.Drawing.Point(410, 345);
            this.lblAGCTargetValue.Name = "lblAGCTargetValue";
            this.lblAGCTargetValue.Size = new System.Drawing.Size(35, 15);
            this.lblAGCTargetValue.TabIndex = 22;
            this.lblAGCTargetValue.Text = "20%";
            // 
            // lblAGCAttack
            // 
            this.lblAGCAttack.AutoSize = true;
            this.lblAGCAttack.Location = new System.Drawing.Point(20, 385);
            this.lblAGCAttack.Name = "lblAGCAttack";
            this.lblAGCAttack.Size = new System.Drawing.Size(85, 15);
            this.lblAGCAttack.TabIndex = 23;
            this.lblAGCAttack.Text = "AGC Attack:";
            // 
            // trackAGCAttack
            // 
            this.trackAGCAttack.Location = new System.Drawing.Point(150, 380);
            this.trackAGCAttack.Maximum = 50;
            this.trackAGCAttack.Minimum = 1;
            this.trackAGCAttack.Name = "trackAGCAttack";
            this.trackAGCAttack.Size = new System.Drawing.Size(250, 45);
            this.trackAGCAttack.TabIndex = 24;
            this.trackAGCAttack.TickFrequency = 5;
            this.trackAGCAttack.Value = 5;
            this.trackAGCAttack.ValueChanged += new System.EventHandler(this.trackAGCAttack_ValueChanged);
            // 
            // lblAGCAttackValue
            // 
            this.lblAGCAttackValue.AutoSize = true;
            this.lblAGCAttackValue.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblAGCAttackValue.Location = new System.Drawing.Point(410, 385);
            this.lblAGCAttackValue.Name = "lblAGCAttackValue";
            this.lblAGCAttackValue.Size = new System.Drawing.Size(35, 15);
            this.lblAGCAttackValue.TabIndex = 25;
            this.lblAGCAttackValue.Text = "0.5s";
            // 
            // lblAGCRelease
            // 
            this.lblAGCRelease.AutoSize = true;
            this.lblAGCRelease.Location = new System.Drawing.Point(20, 425);
            this.lblAGCRelease.Name = "lblAGCRelease";
            this.lblAGCRelease.Size = new System.Drawing.Size(90, 15);
            this.lblAGCRelease.TabIndex = 26;
            this.lblAGCRelease.Text = "AGC Release:";
            // 
            // trackAGCRelease
            // 
            this.trackAGCRelease.Location = new System.Drawing.Point(150, 420);
            this.trackAGCRelease.Maximum = 100;
            this.trackAGCRelease.Minimum = 1;
            this.trackAGCRelease.Name = "trackAGCRelease";
            this.trackAGCRelease.Size = new System.Drawing.Size(250, 45);
            this.trackAGCRelease.TabIndex = 27;
            this.trackAGCRelease.TickFrequency = 10;
            this.trackAGCRelease.Value = 30;
            this.trackAGCRelease.ValueChanged += new System.EventHandler(this.trackAGCRelease_ValueChanged);
            // 
            // lblAGCReleaseValue
            // 
            this.lblAGCReleaseValue.AutoSize = true;
            this.lblAGCReleaseValue.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblAGCReleaseValue.Location = new System.Drawing.Point(410, 425);
            this.lblAGCReleaseValue.Name = "lblAGCReleaseValue";
            this.lblAGCReleaseValue.Size = new System.Drawing.Size(35, 15);
            this.lblAGCReleaseValue.TabIndex = 28;
            this.lblAGCReleaseValue.Text = "3.0s";
            // 
            // lblLimiterThreshold
            // 
            this.lblLimiterThreshold.AutoSize = true;
            this.lblLimiterThreshold.Location = new System.Drawing.Point(20, 465);
            this.lblLimiterThreshold.Name = "lblLimiterThreshold";
            this.lblLimiterThreshold.Size = new System.Drawing.Size(120, 15);
            this.lblLimiterThreshold.TabIndex = 29;
            this.lblLimiterThreshold.Text = "Limiter Threshold:";
            // 
            // trackLimiterThreshold
            // 
            this.trackLimiterThreshold.Location = new System.Drawing.Point(150, 460);
            this.trackLimiterThreshold.Maximum = 100;
            this.trackLimiterThreshold.Minimum = 50;
            this.trackLimiterThreshold.Name = "trackLimiterThreshold";
            this.trackLimiterThreshold.Size = new System.Drawing.Size(250, 45);
            this.trackLimiterThreshold.TabIndex = 30;
            this.trackLimiterThreshold.TickFrequency = 5;
            this.trackLimiterThreshold.Value = 95;
            this.trackLimiterThreshold.ValueChanged += new System.EventHandler(this.trackLimiterThreshold_ValueChanged);
            // 
            // lblLimiterThresholdValue
            // 
            this.lblLimiterThresholdValue.AutoSize = true;
            this.lblLimiterThresholdValue.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblLimiterThresholdValue.Location = new System.Drawing.Point(410, 465);
            this.lblLimiterThresholdValue.Name = "lblLimiterThresholdValue";
            this.lblLimiterThresholdValue.Size = new System.Drawing.Size(35, 15);
            this.lblLimiterThresholdValue.TabIndex = 31;
            this.lblLimiterThresholdValue.Text = "95%";
            // 
            // btnResetAGC
            // 
            this.btnResetAGC.Location = new System.Drawing.Point(220, 310);
            this.btnResetAGC.Name = "btnResetAGC";
            this.btnResetAGC.Size = new System.Drawing.Size(130, 23);
            this.btnResetAGC.TabIndex = 32;
            this.btnResetAGC.Text = "🔄 Reset Default";
            this.btnResetAGC.UseVisualStyleBackColor = true;
            this.btnResetAGC.Click += new System.EventHandler(this.btnResetAGC_Click);
            // 
            // btnSave
            // 
            this.btnSave.BackColor = System.Drawing.Color.LightGreen;
            this.btnSave.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnSave.Location = new System.Drawing.Point(250, 520);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(100, 35);
            this.btnSave.TabIndex = 33;
            this.btnSave.Text = "💾 Salva";
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.BackColor = System.Drawing.Color.LightCoral;
            this.btnCancel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnCancel.Location = new System.Drawing.Point(360, 520);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 35);
            this.btnCancel.TabIndex = 34;
            this.btnCancel.Text = "❌ Annulla";
            this.btnCancel.UseVisualStyleBackColor = false;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // EncoderEditorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(484, 571);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnResetAGC);
            this.Controls.Add(this.lblLimiterThresholdValue);
            this.Controls.Add(this.trackLimiterThreshold);
            this.Controls.Add(this.lblLimiterThreshold);
            this.Controls.Add(this.lblAGCReleaseValue);
            this.Controls.Add(this.trackAGCRelease);
            this.Controls.Add(this.lblAGCRelease);
            this.Controls.Add(this.lblAGCAttackValue);
            this.Controls.Add(this.trackAGCAttack);
            this.Controls.Add(this.lblAGCAttack);
            this.Controls.Add(this.lblAGCTargetValue);
            this.Controls.Add(this.trackAGCTarget);
            this.Controls.Add(this.lblAGCTarget);
            this.Controls.Add(this.chkEnableAGC);
            this.Controls.Add(this.btnSetLocalServer);
            this.Controls.Add(this.cboBitrate);
            this.Controls.Add(this.lblBitrate);
            this.Controls.Add(this.txtMountPoint);
            this.Controls.Add(this.lblMountPoint);
            this.Controls.Add(this.txtPassword);
            this.Controls.Add(this.lblPassword);
            this.Controls.Add(this.txtUsername);
            this.Controls.Add(this.lblUsername);
            this.Controls.Add(this.txtServerPort);
            this.Controls.Add(this.lblServerPort);
            this.Controls.Add(this.txtServerUrl);
            this.Controls.Add(this.lblServerUrl);
            this.Controls.Add(this.cboAudioSource);
            this.Controls.Add(this.lblAudioSource);
            this.Controls.Add(this.txtStationName);
            this.Controls.Add(this.lblStationName);
            this.Controls.Add(this.txtName);
            this.Controls.Add(this.lblName);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "EncoderEditorForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Editor Encoder";
            this.Load += new System.EventHandler(this.EncoderEditorForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.trackAGCTarget)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackAGCAttack)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackAGCRelease)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackLimiterThreshold)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Label lblStationName;
        private System.Windows.Forms.TextBox txtStationName;
        private System.Windows.Forms.Label lblAudioSource;
        private System.Windows.Forms.ComboBox cboAudioSource;
        private System.Windows.Forms.Label lblServerUrl;
        private System.Windows.Forms.TextBox txtServerUrl;
        private System.Windows.Forms.Label lblServerPort;
        private System.Windows.Forms.TextBox txtServerPort;
        private System.Windows.Forms.Label lblUsername;
        private System.Windows.Forms.TextBox txtUsername;
        private System.Windows.Forms.Label lblPassword;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Label lblMountPoint;
        private System.Windows.Forms.TextBox txtMountPoint;
        private System.Windows.Forms.Label lblBitrate;
        private System.Windows.Forms.ComboBox cboBitrate;
        private System.Windows.Forms.CheckBox chkEnableAGC;
        private System.Windows.Forms.Label lblAGCTarget;
        private System.Windows.Forms.TrackBar trackAGCTarget;
        private System.Windows.Forms.Label lblAGCTargetValue;
        private System.Windows.Forms.Label lblAGCAttack;
        private System.Windows.Forms.TrackBar trackAGCAttack;
        private System.Windows.Forms.Label lblAGCAttackValue;
        private System.Windows.Forms.Label lblAGCRelease;
        private System.Windows.Forms.TrackBar trackAGCRelease;
        private System.Windows.Forms.Label lblAGCReleaseValue;
        private System.Windows.Forms.Label lblLimiterThreshold;
        private System.Windows.Forms.TrackBar trackLimiterThreshold;
        private System.Windows.Forms.Label lblLimiterThresholdValue;
        private System.Windows.Forms.Button btnResetAGC;
        private System.Windows.Forms.Button btnSetLocalServer;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
    }
}