namespace AirDirector.Controls
{
    partial class EncoderStreamControl
    {
        private System.ComponentModel.IContainer components = null;

        #region Component Designer generated code

        private void InitializeComponent()
        {
            btnEdit = new Button();
            btnDelete = new Button();
            lblStationName = new Label();
            lblFormat = new Label();
            lblStatus = new Label();
            lblAudioDevice = new Label();
            lblUptime = new Label();
            btnStartStop = new Button();
            progressLeft = new ProgressBar();
            progressRight = new ProgressBar();
            lblDSP = new Label();
            lblVULeft = new Label();
            lblVURight = new Label();
            SuspendLayout();
            // 
            // btnEdit
            // 
            btnEdit.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnEdit.Location = new Point(5, 10);
            btnEdit.Name = "btnEdit";
            btnEdit.Size = new Size(27, 29);
            btnEdit.TabIndex = 0;
            btnEdit.Text = "✏️";
            btnEdit.UseVisualStyleBackColor = true;
            btnEdit.Click += btnEdit_Click;
            // 
            // btnDelete
            // 
            btnDelete.BackColor = Color.LightCoral;
            btnDelete.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnDelete.Location = new Point(38, 10);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(27, 29);
            btnDelete.TabIndex = 1;
            btnDelete.Text = "🗑️";
            btnDelete.UseVisualStyleBackColor = false;
            btnDelete.Click += btnDelete_Click;
            // 
            // lblStationName
            // 
            lblStationName.AutoSize = true;
            lblStationName.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblStationName.Location = new Point(140, 8);
            lblStationName.Name = "lblStationName";
            lblStationName.Size = new Size(115, 20);
            lblStationName.TabIndex = 2;
            lblStationName.Text = "Nome Stazione";
            // 
            // lblFormat
            // 
            lblFormat.AutoSize = true;
            lblFormat.Location = new Point(320, 10);
            lblFormat.Name = "lblFormat";
            lblFormat.Size = new Size(77, 15);
            lblFormat.TabIndex = 3;
            lblFormat.Text = "MP3 128kbps";
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.BackColor = Color.Transparent;
            lblStatus.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblStatus.Location = new Point(440, 10);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(46, 15);
            lblStatus.TabIndex = 4;
            lblStatus.Text = "Offline";
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblAudioDevice
            // 
            lblAudioDevice.AutoSize = true;
            lblAudioDevice.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblAudioDevice.Location = new Point(140, 32);
            lblAudioDevice.Name = "lblAudioDevice";
            lblAudioDevice.Size = new Size(100, 15);
            lblAudioDevice.TabIndex = 5;
            lblAudioDevice.Text = "Dispositivo Audio";
            lblAudioDevice.Click += lblAudioDevice_Click;
            // 
            // lblUptime
            // 
            lblUptime.AutoSize = true;
            lblUptime.Location = new Point(320, 32);
            lblUptime.Name = "lblUptime";
            lblUptime.Size = new Size(67, 15);
            lblUptime.TabIndex = 6;
            lblUptime.Text = "00, 00:00:00";
            // 
            // btnStartStop
            // 
            btnStartStop.BackColor = Color.LightGreen;
            btnStartStop.FlatStyle = FlatStyle.Flat;
            btnStartStop.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnStartStop.Location = new Point(71, 10);
            btnStartStop.Name = "btnStartStop";
            btnStartStop.Size = new Size(63, 48);
            btnStartStop.TabIndex = 7;
            btnStartStop.Text = "▶ Start";
            btnStartStop.UseVisualStyleBackColor = false;
            btnStartStop.Click += btnStartStop_Click;
            // 
            // progressLeft
            // 
            progressLeft.Location = new Point(530, 12);
            progressLeft.Name = "progressLeft";
            progressLeft.Size = new Size(150, 15);
            progressLeft.Style = ProgressBarStyle.Continuous;
            progressLeft.TabIndex = 9;
            // 
            // progressRight
            // 
            progressRight.Location = new Point(530, 32);
            progressRight.Name = "progressRight";
            progressRight.Size = new Size(150, 15);
            progressRight.Style = ProgressBarStyle.Continuous;
            progressRight.TabIndex = 11;
            // 
            // lblDSP
            // 
            lblDSP.AutoSize = true;
            lblDSP.BackColor = Color.Transparent;
            lblDSP.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            lblDSP.Location = new Point(440, 32);
            lblDSP.Name = "lblDSP";
            lblDSP.Size = new Size(30, 13);
            lblDSP.TabIndex = 12;
            lblDSP.Text = "AGC";
            lblDSP.TextAlign = ContentAlignment.MiddleCenter;
            lblDSP.Visible = false;
            // 
            // lblVULeft
            // 
            lblVULeft.AutoSize = true;
            lblVULeft.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblVULeft.Location = new Point(510, 12);
            lblVULeft.Name = "lblVULeft";
            lblVULeft.Size = new Size(13, 15);
            lblVULeft.TabIndex = 8;
            lblVULeft.Text = "L";
            // 
            // lblVURight
            // 
            lblVURight.AutoSize = true;
            lblVURight.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblVURight.Location = new Point(510, 32);
            lblVURight.Name = "lblVURight";
            lblVURight.Size = new Size(15, 15);
            lblVURight.TabIndex = 10;
            lblVURight.Text = "R";
            // 
            // EncoderStreamControl
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            BorderStyle = BorderStyle.FixedSingle;
            Controls.Add(lblDSP);
            Controls.Add(progressRight);
            Controls.Add(lblVURight);
            Controls.Add(progressLeft);
            Controls.Add(lblVULeft);
            Controls.Add(btnStartStop);
            Controls.Add(lblUptime);
            Controls.Add(lblAudioDevice);
            Controls.Add(lblStatus);
            Controls.Add(lblFormat);
            Controls.Add(lblStationName);
            Controls.Add(btnDelete);
            Controls.Add(btnEdit);
            Name = "EncoderStreamControl";
            Size = new Size(690, 67);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Button btnEdit;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Label lblStationName;
        private System.Windows.Forms.Label lblFormat;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblAudioDevice;
        private System.Windows.Forms.Label lblUptime;
        private System.Windows.Forms.Button btnStartStop;
        private System.Windows.Forms.ProgressBar progressLeft;
        private System.Windows.Forms.ProgressBar progressRight;
        private System.Windows.Forms.Label lblDSP;
        private System.Windows.Forms.Label lblVULeft;
        private System.Windows.Forms.Label lblVURight;
    }
}