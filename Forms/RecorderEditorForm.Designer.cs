namespace AirDirector.Forms
{
    partial class RecorderEditorForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>


        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            lblName = new Label();
            txtName = new TextBox();
            lblType = new Label();
            cmbType = new ComboBox();
            lblAudioDevice = new Label();
            cmbAudioDevice = new ComboBox();
            lblOutputPath = new Label();
            txtOutputPath = new TextBox();
            btnBrowse = new Button();
            lblFormat = new Label();
            cmbFormat = new ComboBox();
            pnlSchedule = new Panel();
            dtpEndTime = new DateTimePicker();
            lblEndTime = new Label();
            dtpStartTime = new DateTimePicker();
            lblStartTime = new Label();
            chkSunday = new CheckBox();
            chkSaturday = new CheckBox();
            chkFriday = new CheckBox();
            chkThursday = new CheckBox();
            chkWednesday = new CheckBox();
            chkTuesday = new CheckBox();
            chkMonday = new CheckBox();
            lblScheduleTitle = new Label();
            pnl90Days = new Panel();
            chkAutoDelete = new CheckBox();
            numRetentionDays = new NumericUpDown();
            lblRetention = new Label();
            lbl90Title = new Label();
            btnSave = new Button();
            btnCancel = new Button();
            pnlSchedule.SuspendLayout();
            pnl90Days.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numRetentionDays).BeginInit();
            SuspendLayout();
            // 
            // lblName
            // 
            lblName.AutoSize = true;
            lblName.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblName.Location = new Point(54, 20);
            lblName.Name = "lblName";
            lblName.Size = new Size(121, 19);
            lblName.TabIndex = 0;
            lblName.Text = "Nome Recorder:";
            // 
            // txtName
            // 
            txtName.Font = new Font("Segoe UI", 10F);
            txtName.Location = new Point(184, 17);
            txtName.Name = "txtName";
            txtName.Size = new Size(289, 25);
            txtName.TabIndex = 1;
            // 
            // lblType
            // 
            lblType.AutoSize = true;
            lblType.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblType.Location = new Point(131, 59);
            lblType.Name = "lblType";
            lblType.Size = new Size(43, 19);
            lblType.TabIndex = 2;
            lblType.Text = "Tipo:";
            // 
            // cmbType
            // 
            cmbType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbType.Font = new Font("Segoe UI", 10F);
            cmbType.FormattingEnabled = true;
            cmbType.Items.AddRange(new object[] { "Manuale", "90 Giorni (file orari)", "Schedulato" });
            cmbType.Location = new Point(184, 56);
            cmbType.Name = "cmbType";
            cmbType.Size = new Size(289, 25);
            cmbType.TabIndex = 3;
            cmbType.SelectedIndexChanged += CmbType_SelectedIndexChanged;
            // 
            // lblAudioDevice
            // 
            lblAudioDevice.AutoSize = true;
            lblAudioDevice.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblAudioDevice.Location = new Point(44, 95);
            lblAudioDevice.Name = "lblAudioDevice";
            lblAudioDevice.Size = new Size(131, 19);
            lblAudioDevice.TabIndex = 4;
            lblAudioDevice.Text = "Dispositivo Audio:";
            // 
            // cmbAudioDevice
            // 
            cmbAudioDevice.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbAudioDevice.Font = new Font("Segoe UI", 10F);
            cmbAudioDevice.FormattingEnabled = true;
            cmbAudioDevice.Location = new Point(184, 92);
            cmbAudioDevice.Name = "cmbAudioDevice";
            cmbAudioDevice.Size = new Size(289, 25);
            cmbAudioDevice.TabIndex = 5;
            // 
            // lblOutputPath
            // 
            lblOutputPath.AutoSize = true;
            lblOutputPath.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblOutputPath.Location = new Point(18, 131);
            lblOutputPath.Name = "lblOutputPath";
            lblOutputPath.Size = new Size(157, 19);
            lblOutputPath.TabIndex = 6;
            lblOutputPath.Text = "Percorso Salvataggio:";
            // 
            // txtOutputPath
            // 
            txtOutputPath.Font = new Font("Segoe UI", 10F);
            txtOutputPath.Location = new Point(184, 129);
            txtOutputPath.Name = "txtOutputPath";
            txtOutputPath.Size = new Size(289, 25);
            txtOutputPath.TabIndex = 7;
            // 
            // btnBrowse
            // 
            btnBrowse.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnBrowse.Location = new Point(479, 129);
            btnBrowse.Name = "btnBrowse";
            btnBrowse.Size = new Size(44, 30);
            btnBrowse.TabIndex = 8;
            btnBrowse.Text = "📁";
            btnBrowse.UseVisualStyleBackColor = true;
            btnBrowse.Click += BtnBrowse_Click;
            // 
            // lblFormat
            // 
            lblFormat.AutoSize = true;
            lblFormat.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblFormat.Location = new Point(60, 169);
            lblFormat.Name = "lblFormat";
            lblFormat.Size = new Size(114, 19);
            lblFormat.TabIndex = 9;
            lblFormat.Text = "Formato Audio:";
            // 
            // cmbFormat
            // 
            cmbFormat.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFormat.Font = new Font("Segoe UI", 10F);
            cmbFormat.FormattingEnabled = true;
            cmbFormat.Items.AddRange(new object[] { "MP3 64kbps Mono", "MP3 64kbps Stereo", "MP3 128kbps Mono", "MP3 128kbps Stereo", "MP3 256kbps Mono", "MP3 256kbps Stereo", "MP3 320kbps Mono", "MP3 320kbps Stereo" });
            cmbFormat.Location = new Point(184, 166);
            cmbFormat.Name = "cmbFormat";
            cmbFormat.Size = new Size(148, 25);
            cmbFormat.TabIndex = 10;
            // 
            // pnlSchedule
            // 
            pnlSchedule.BorderStyle = BorderStyle.FixedSingle;
            pnlSchedule.Controls.Add(dtpEndTime);
            pnlSchedule.Controls.Add(lblEndTime);
            pnlSchedule.Controls.Add(dtpStartTime);
            pnlSchedule.Controls.Add(lblStartTime);
            pnlSchedule.Controls.Add(chkSunday);
            pnlSchedule.Controls.Add(chkSaturday);
            pnlSchedule.Controls.Add(chkFriday);
            pnlSchedule.Controls.Add(chkThursday);
            pnlSchedule.Controls.Add(chkWednesday);
            pnlSchedule.Controls.Add(chkTuesday);
            pnlSchedule.Controls.Add(chkMonday);
            pnlSchedule.Controls.Add(lblScheduleTitle);
            pnlSchedule.Location = new Point(18, 216);
            pnlSchedule.Name = "pnlSchedule";
            pnlSchedule.Size = new Size(505, 150);
            pnlSchedule.TabIndex = 11;
            pnlSchedule.Visible = false;
            // 
            // dtpEndTime
            // 
            dtpEndTime.Font = new Font("Segoe UI", 10F);
            dtpEndTime.Format = DateTimePickerFormat.Time;
            dtpEndTime.Location = new Point(262, 82);
            dtpEndTime.Name = "dtpEndTime";
            dtpEndTime.ShowUpDown = true;
            dtpEndTime.Size = new Size(88, 25);
            dtpEndTime.TabIndex = 11;
            // 
            // lblEndTime
            // 
            lblEndTime.AutoSize = true;
            lblEndTime.Font = new Font("Segoe UI", 9F);
            lblEndTime.Location = new Point(192, 84);
            lblEndTime.Name = "lblEndTime";
            lblEndTime.Size = new Size(54, 15);
            lblEndTime.TabIndex = 10;
            lblEndTime.Text = "Ora Fine:";
            // 
            // dtpStartTime
            // 
            dtpStartTime.CustomFormat = "";
            dtpStartTime.Font = new Font("Segoe UI", 10F);
            dtpStartTime.Format = DateTimePickerFormat.Time;
            dtpStartTime.Location = new Point(88, 82);
            dtpStartTime.Name = "dtpStartTime";
            dtpStartTime.ShowUpDown = true;
            dtpStartTime.Size = new Size(88, 25);
            dtpStartTime.TabIndex = 9;
            // 
            // lblStartTime
            // 
            lblStartTime.AutoSize = true;
            lblStartTime.Font = new Font("Segoe UI", 9F);
            lblStartTime.Location = new Point(9, 84);
            lblStartTime.Name = "lblStartTime";
            lblStartTime.Size = new Size(60, 15);
            lblStartTime.TabIndex = 8;
            lblStartTime.Text = "Ora Inizio:";
            // 
            // chkSunday
            // 
            chkSunday.AutoSize = true;
            chkSunday.Font = new Font("Segoe UI", 9F);
            chkSunday.Location = new Point(376, 47);
            chkSunday.Name = "chkSunday";
            chkSunday.Size = new Size(52, 19);
            chkSunday.TabIndex = 7;
            chkSunday.Text = "Dom";
            chkSunday.UseVisualStyleBackColor = true;
            // 
            // chkSaturday
            // 
            chkSaturday.AutoSize = true;
            chkSaturday.Font = new Font("Segoe UI", 9F);
            chkSaturday.Location = new Point(315, 47);
            chkSaturday.Name = "chkSaturday";
            chkSaturday.Size = new Size(45, 19);
            chkSaturday.TabIndex = 6;
            chkSaturday.Text = "Sab";
            chkSaturday.UseVisualStyleBackColor = true;
            // 
            // chkFriday
            // 
            chkFriday.AutoSize = true;
            chkFriday.Font = new Font("Segoe UI", 9F);
            chkFriday.Location = new Point(254, 47);
            chkFriday.Name = "chkFriday";
            chkFriday.Size = new Size(45, 19);
            chkFriday.TabIndex = 5;
            chkFriday.Text = "Ven";
            chkFriday.UseVisualStyleBackColor = true;
            // 
            // chkThursday
            // 
            chkThursday.AutoSize = true;
            chkThursday.Font = new Font("Segoe UI", 9F);
            chkThursday.Location = new Point(192, 47);
            chkThursday.Name = "chkThursday";
            chkThursday.Size = new Size(44, 19);
            chkThursday.TabIndex = 4;
            chkThursday.Text = "Gio";
            chkThursday.UseVisualStyleBackColor = true;
            // 
            // chkWednesday
            // 
            chkWednesday.AutoSize = true;
            chkWednesday.Font = new Font("Segoe UI", 9F);
            chkWednesday.Location = new Point(131, 47);
            chkWednesday.Name = "chkWednesday";
            chkWednesday.Size = new Size(47, 19);
            chkWednesday.TabIndex = 3;
            chkWednesday.Text = "Mer";
            chkWednesday.UseVisualStyleBackColor = true;
            // 
            // chkTuesday
            // 
            chkTuesday.AutoSize = true;
            chkTuesday.Font = new Font("Segoe UI", 9F);
            chkTuesday.Location = new Point(70, 47);
            chkTuesday.Name = "chkTuesday";
            chkTuesday.Size = new Size(47, 19);
            chkTuesday.TabIndex = 2;
            chkTuesday.Text = "Mar";
            chkTuesday.UseVisualStyleBackColor = true;
            // 
            // chkMonday
            // 
            chkMonday.AutoSize = true;
            chkMonday.Font = new Font("Segoe UI", 9F);
            chkMonday.Location = new Point(9, 47);
            chkMonday.Name = "chkMonday";
            chkMonday.Size = new Size(46, 19);
            chkMonday.TabIndex = 1;
            chkMonday.Text = "Lun";
            chkMonday.UseVisualStyleBackColor = true;
            // 
            // lblScheduleTitle
            // 
            lblScheduleTitle.AutoSize = true;
            lblScheduleTitle.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblScheduleTitle.Location = new Point(9, 9);
            lblScheduleTitle.Name = "lblScheduleTitle";
            lblScheduleTitle.Size = new Size(157, 20);
            lblScheduleTitle.TabIndex = 0;
            lblScheduleTitle.Text = "⏰ SCHEDULAZIONE";
            // 
            // pnl90Days
            // 
            pnl90Days.BorderStyle = BorderStyle.FixedSingle;
            pnl90Days.Controls.Add(chkAutoDelete);
            pnl90Days.Controls.Add(numRetentionDays);
            pnl90Days.Controls.Add(lblRetention);
            pnl90Days.Controls.Add(lbl90Title);
            pnl90Days.Location = new Point(18, 216);
            pnl90Days.Name = "pnl90Days";
            pnl90Days.Size = new Size(505, 113);
            pnl90Days.TabIndex = 12;
            pnl90Days.Visible = false;
            // 
            // chkAutoDelete
            // 
            chkAutoDelete.AutoSize = true;
            chkAutoDelete.Checked = true;
            chkAutoDelete.CheckState = CheckState.Checked;
            chkAutoDelete.Font = new Font("Segoe UI", 9F);
            chkAutoDelete.Location = new Point(9, 80);
            chkAutoDelete.Name = "chkAutoDelete";
            chkAutoDelete.Size = new Size(218, 19);
            chkAutoDelete.TabIndex = 3;
            chkAutoDelete.Text = "Elimina automaticamente file vecchi";
            chkAutoDelete.UseVisualStyleBackColor = true;
            // 
            // numRetentionDays
            // 
            numRetentionDays.Font = new Font("Segoe UI", 10F);
            numRetentionDays.Location = new Point(175, 45);
            numRetentionDays.Maximum = new decimal(new int[] { 365, 0, 0, 0 });
            numRetentionDays.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numRetentionDays.Name = "numRetentionDays";
            numRetentionDays.Size = new Size(70, 25);
            numRetentionDays.TabIndex = 2;
            numRetentionDays.Value = new decimal(new int[] { 90, 0, 0, 0 });
            // 
            // lblRetention
            // 
            lblRetention.AutoSize = true;
            lblRetention.Font = new Font("Segoe UI", 9F);
            lblRetention.Location = new Point(9, 47);
            lblRetention.Name = "lblRetention";
            lblRetention.Size = new Size(133, 15);
            lblRetention.TabIndex = 1;
            lblRetention.Text = "Giorni di conservazione:";
            // 
            // lbl90Title
            // 
            lbl90Title.AutoSize = true;
            lbl90Title.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lbl90Title.Location = new Point(9, 9);
            lbl90Title.Name = "lbl90Title";
            lbl90Title.Size = new Size(232, 20);
            lbl90Title.TabIndex = 0;
            lbl90Title.Text = "📼 REGISTRAZIONE 90 GIORNI";
            // 
            // btnSave
            // 
            btnSave.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnSave.Location = new Point(306, 384);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(88, 38);
            btnSave.TabIndex = 13;
            btnSave.Text = "💾 Salva";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += BtnSave_Click;
            // 
            // btnCancel
            // 
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnCancel.Location = new Point(402, 384);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(88, 38);
            btnCancel.TabIndex = 14;
            btnCancel.Text = "❌ Annulla";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // RecorderEditorForm
            // 
            AcceptButton = btnSave;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(546, 441);
            Controls.Add(btnCancel);
            Controls.Add(btnSave);
            Controls.Add(pnl90Days);
            Controls.Add(pnlSchedule);
            Controls.Add(cmbFormat);
            Controls.Add(lblFormat);
            Controls.Add(btnBrowse);
            Controls.Add(txtOutputPath);
            Controls.Add(lblOutputPath);
            Controls.Add(cmbAudioDevice);
            Controls.Add(lblAudioDevice);
            Controls.Add(cmbType);
            Controls.Add(lblType);
            Controls.Add(txtName);
            Controls.Add(lblName);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "RecorderEditorForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Recorder Editor";
            pnlSchedule.ResumeLayout(false);
            pnlSchedule.PerformLayout();
            pnl90Days.ResumeLayout(false);
            pnl90Days.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numRetentionDays).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Label lblType;
        private System.Windows.Forms.ComboBox cmbType;
        private System.Windows.Forms.Label lblAudioDevice;
        private System.Windows.Forms.ComboBox cmbAudioDevice;
        private System.Windows.Forms.Label lblOutputPath;
        private System.Windows.Forms.TextBox txtOutputPath;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.Label lblFormat;
        private System.Windows.Forms.ComboBox cmbFormat;
        private System.Windows.Forms.Panel pnlSchedule;
        private System.Windows.Forms.DateTimePicker dtpEndTime;
        private System.Windows.Forms.Label lblEndTime;
        private System.Windows.Forms.DateTimePicker dtpStartTime;
        private System.Windows.Forms.Label lblStartTime;
        private System.Windows.Forms.CheckBox chkSunday;
        private System.Windows.Forms.CheckBox chkSaturday;
        private System.Windows.Forms.CheckBox chkFriday;
        private System.Windows.Forms.CheckBox chkThursday;
        private System.Windows.Forms.CheckBox chkWednesday;
        private System.Windows.Forms.CheckBox chkTuesday;
        private System.Windows.Forms.CheckBox chkMonday;
        private System.Windows.Forms.Label lblScheduleTitle;
        private System.Windows.Forms.Panel pnl90Days;
        private System.Windows.Forms.CheckBox chkAutoDelete;
        private System.Windows.Forms.NumericUpDown numRetentionDays;
        private System.Windows.Forms.Label lblRetention;
        private System.Windows.Forms.Label lbl90Title;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
    }
}