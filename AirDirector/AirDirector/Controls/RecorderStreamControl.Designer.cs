namespace AirDirector.Controls
{
    partial class RecorderStreamControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            btnEdit = new Button();
            btnDelete = new Button();
            lblRecorderName = new Label();
            lblFormat = new Label();
            lblType = new Label();
            lblStatus = new Label();
            lblAudioDevice = new Label();
            lblPath = new Label();
            lblCurrentFile = new Label();
            lblRecordingInfo = new Label();
            btnStartStop = new Button();
            progressLeft = new ProgressBar();
            progressRight = new ProgressBar();
            lblVULeft = new Label();
            lblVURight = new Label();
            SuspendLayout();
            // 
            // btnEdit
            // 
            btnEdit.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
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
            btnDelete.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnDelete.Location = new Point(38, 10);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(27, 29);
            btnDelete.TabIndex = 1;
            btnDelete.Text = "🗑️";
            btnDelete.UseVisualStyleBackColor = false;
            btnDelete.Click += btnDelete_Click;
            // 
            // lblRecorderName
            // 
            lblRecorderName.AutoSize = true;
            lblRecorderName.Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblRecorderName.Location = new Point(131, 7);
            lblRecorderName.Name = "lblRecorderName";
            lblRecorderName.Size = new Size(119, 20);
            lblRecorderName.TabIndex = 2;
            lblRecorderName.Text = "Nome Recorder";
            // 
            // lblFormat
            // 
            lblFormat.AutoSize = true;
            lblFormat.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblFormat.Location = new Point(298, 7);
            lblFormat.Name = "lblFormat";
            lblFormat.Size = new Size(77, 15);
            lblFormat.TabIndex = 3;
            lblFormat.Text = "MP3 128kbps";
            // 
            // lblType
            // 
            lblType.AutoSize = true;
            lblType.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblType.ForeColor = Color.Orange;
            lblType.Location = new Point(5, 44);
            lblType.Name = "lblType";
            lblType.Size = new Size(34, 15);
            lblType.TabIndex = 4;
            lblType.Text = "Auto";
            lblType.Visible = false;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.BackColor = Color.Transparent;
            lblStatus.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblStatus.Location = new Point(475, 7);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(51, 15);
            lblStatus.TabIndex = 5;
            lblStatus.Text = "Inattivo";
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            lblStatus.Click += lblStatus_Click;
            // 
            // lblAudioDevice
            // 
            lblAudioDevice.AutoSize = true;
            lblAudioDevice.Font = new Font("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblAudioDevice.ForeColor = Color.Gray;
            lblAudioDevice.Location = new Point(131, 29);
            lblAudioDevice.Name = "lblAudioDevice";
            lblAudioDevice.Size = new Size(98, 13);
            lblAudioDevice.TabIndex = 6;
            lblAudioDevice.Text = "Dispositivo Audio";
            // 
            // lblPath
            // 
            lblPath.AutoSize = true;
            lblPath.Font = new Font("Segoe UI", 7F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblPath.ForeColor = Color.DarkGray;
            lblPath.Location = new Point(131, 44);
            lblPath.Name = "lblPath";
            lblPath.Size = new Size(80, 12);
            lblPath.TabIndex = 7;
            lblPath.Text = "📁 C:\\Recordings";
            // 
            // lblCurrentFile
            // 
            lblCurrentFile.AutoSize = true;
            lblCurrentFile.Font = new Font("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblCurrentFile.ForeColor = Color.Gray;
            lblCurrentFile.Location = new Point(298, 27);
            lblCurrentFile.Name = "lblCurrentFile";
            lblCurrentFile.Size = new Size(64, 13);
            lblCurrentFile.TabIndex = 8;
            lblCurrentFile.Text = "Nessun file";
            // 
            // lblRecordingInfo
            // 
            lblRecordingInfo.AutoSize = true;
            lblRecordingInfo.Font = new Font("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblRecordingInfo.Location = new Point(298, 42);
            lblRecordingInfo.Name = "lblRecordingInfo";
            lblRecordingInfo.Size = new Size(83, 13);
            lblRecordingInfo.TabIndex = 9;
            lblRecordingInfo.Text = "00:00:00 | 0 MB";
            // 
            // btnStartStop
            // 
            btnStartStop.BackColor = Color.LightGreen;
            btnStartStop.FlatStyle = FlatStyle.Flat;
            btnStartStop.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnStartStop.Location = new Point(71, 10);
            btnStartStop.Name = "btnStartStop";
            btnStartStop.Size = new Size(54, 48);
            btnStartStop.TabIndex = 10;
            btnStartStop.Text = "🔘 Rec";
            btnStartStop.UseVisualStyleBackColor = false;
            btnStartStop.Click += btnStartStop_Click;
            // 
            // progressLeft
            // 
            progressLeft.ForeColor = Color.LimeGreen;
            progressLeft.Location = new Point(475, 27);
            progressLeft.Name = "progressLeft";
            progressLeft.Size = new Size(201, 10);
            progressLeft.Style = ProgressBarStyle.Continuous;
            progressLeft.TabIndex = 11;
            // 
            // progressRight
            // 
            progressRight.ForeColor = Color.LimeGreen;
            progressRight.Location = new Point(475, 43);
            progressRight.Name = "progressRight";
            progressRight.Size = new Size(201, 10);
            progressRight.Style = ProgressBarStyle.Continuous;
            progressRight.TabIndex = 12;
            // 
            // lblVULeft
            // 
            lblVULeft.AutoSize = true;
            lblVULeft.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblVULeft.Location = new Point(456, 24);
            lblVULeft.Name = "lblVULeft";
            lblVULeft.Size = new Size(13, 15);
            lblVULeft.TabIndex = 13;
            lblVULeft.Text = "L";
            lblVULeft.Click += lblVULeft_Click;
            // 
            // lblVURight
            // 
            lblVURight.AutoSize = true;
            lblVURight.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblVURight.Location = new Point(454, 40);
            lblVURight.Name = "lblVURight";
            lblVURight.Size = new Size(15, 15);
            lblVURight.TabIndex = 14;
            lblVURight.Text = "R";
            // 
            // RecorderStreamControl
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            BorderStyle = BorderStyle.FixedSingle;
            Controls.Add(lblVURight);
            Controls.Add(lblVULeft);
            Controls.Add(progressRight);
            Controls.Add(progressLeft);
            Controls.Add(btnStartStop);
            Controls.Add(lblRecordingInfo);
            Controls.Add(lblCurrentFile);
            Controls.Add(lblPath);
            Controls.Add(lblAudioDevice);
            Controls.Add(lblStatus);
            Controls.Add(lblType);
            Controls.Add(lblFormat);
            Controls.Add(lblRecorderName);
            Controls.Add(btnDelete);
            Controls.Add(btnEdit);
            Name = "RecorderStreamControl";
            Size = new Size(690, 67);
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnEdit;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Label lblRecorderName;
        private System.Windows.Forms.Label lblFormat;
        private System.Windows.Forms.Label lblType;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblAudioDevice;
        private System.Windows.Forms.Label lblPath;
        private System.Windows.Forms.Label lblCurrentFile;
        private System.Windows.Forms.Label lblRecordingInfo;
        private System.Windows.Forms.Button btnStartStop;
        private System.Windows.Forms.ProgressBar progressLeft;
        private System.Windows.Forms.ProgressBar progressRight;
        private System.Windows.Forms.Label lblVULeft;
        private System.Windows.Forms.Label lblVURight;
    }
}