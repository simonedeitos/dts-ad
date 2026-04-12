using System.Drawing;
using System.Windows.Forms;
using AirDirector.Themes;

namespace AirDirector.Forms
{
    partial class ScheduleEditorForm
    {
        private System.ComponentModel.IContainer components = null;

        private TextBox txtName;
        private RadioButton radClock;
        private RadioButton radAudio;
        private RadioButton radMiniPLS;
        private RadioButton radTimeSignal;
        private RadioButton radURLStreaming;
        private ComboBox cmbClock;
        private TextBox txtAudioFile;
        private Button btnBrowseAudio;
        private ComboBox cmbPlaylist;
        private TextBox txtStreamURL;
        private MaskedTextBox txtStreamDuration;

        private TextBox txtVideoBufferPath;
        private Button btnBrowseVideoBuffer;
        private Label lblVideoBuffer;

        private CheckBox chkMonday;
        private CheckBox chkTuesday;
        private CheckBox chkWednesday;
        private CheckBox chkThursday;
        private CheckBox chkFriday;
        private CheckBox chkSaturday;
        private CheckBox chkSunday;

        private DateTimePicker dtpTime;
        private ListBox lstTimes;
        private Button btnAddTime;
        private Button btnRemoveTime;
        private Button btnSave;
        private Button btnCancel;

        private Label lblName;
        private GroupBox grpAction;
        private GroupBox grpDays;
        private GroupBox grpTimes;
        private Label lblAddTime;
        private Button btnAllDays;
        private Label lblStreamURL;
        private Label lblStreamDuration;


        private void InitializeComponent()
        {
            lblName = new Label();
            txtName = new TextBox();
            grpAction = new GroupBox();
            radClock = new RadioButton();
            cmbClock = new ComboBox();
            radAudio = new RadioButton();
            txtAudioFile = new TextBox();
            btnBrowseAudio = new Button();
            radMiniPLS = new RadioButton();
            cmbPlaylist = new ComboBox();
            radTimeSignal = new RadioButton();
            radURLStreaming = new RadioButton();
            lblStreamURL = new Label();
            txtStreamURL = new TextBox();
            lblStreamDuration = new Label();
            txtStreamDuration = new MaskedTextBox();
            lblVideoBuffer = new Label();
            txtVideoBufferPath = new TextBox();
            btnBrowseVideoBuffer = new Button();
            grpDays = new GroupBox();
            chkMonday = new CheckBox();
            chkTuesday = new CheckBox();
            chkWednesday = new CheckBox();
            chkThursday = new CheckBox();
            chkFriday = new CheckBox();
            chkSaturday = new CheckBox();
            chkSunday = new CheckBox();
            btnAllDays = new Button();
            grpTimes = new GroupBox();
            lblAddTime = new Label();
            dtpTime = new DateTimePicker();
            btnAddTime = new Button();
            btnRemoveTime = new Button();
            lstTimes = new ListBox();
            btnSave = new Button();
            btnCancel = new Button();
            grpAction.SuspendLayout();
            grpDays.SuspendLayout();
            grpTimes.SuspendLayout();
            SuspendLayout();
            // 
            // lblName
            // 
            lblName.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblName.Location = new Point(20, 20);
            lblName.Name = "lblName";
            lblName.Size = new Size(150, 20);
            lblName.TabIndex = 0;
            lblName.Text = "Nome Schedulazione: ";
            // 
            // txtName
            // 
            txtName.Font = new Font("Segoe UI", 10F);
            txtName.Location = new Point(180, 18);
            txtName.Name = "txtName";
            txtName.Size = new Size(490, 25);
            txtName.TabIndex = 1;
            // 
            // grpAction
            // 
            grpAction.Controls.Add(txtStreamURL);
            grpAction.Controls.Add(radClock);
            grpAction.Controls.Add(cmbClock);
            grpAction.Controls.Add(radAudio);
            grpAction.Controls.Add(txtAudioFile);
            grpAction.Controls.Add(btnBrowseAudio);
            grpAction.Controls.Add(radMiniPLS);
            grpAction.Controls.Add(cmbPlaylist);
            grpAction.Controls.Add(radTimeSignal);
            grpAction.Controls.Add(radURLStreaming);
            grpAction.Controls.Add(lblStreamURL);
            grpAction.Controls.Add(lblStreamDuration);
            grpAction.Controls.Add(txtStreamDuration);
            grpAction.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            grpAction.Location = new Point(20, 60);
            grpAction.Name = "grpAction";
            grpAction.Size = new Size(650, 180);
            grpAction.TabIndex = 2;
            grpAction.TabStop = false;
            grpAction.Text = "Tipo Azione";
            // 
            // radClock
            // 
            radClock.Checked = true;
            radClock.Location = new Point(10, 25);
            radClock.Name = "radClock";
            radClock.Size = new Size(155, 25);
            radClock.TabIndex = 0;
            radClock.TabStop = true;
            radClock.Text = "▶ Riproduci Clock";
            radClock.CheckedChanged += RadAction_CheckedChanged;
            // 
            // cmbClock
            // 
            cmbClock.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbClock.Font = new Font("Segoe UI", 9F);
            cmbClock.Location = new Point(170, 23);
            cmbClock.Name = "cmbClock";
            cmbClock.Size = new Size(450, 23);
            cmbClock.TabIndex = 1;
            // 
            // radAudio
            // 
            radAudio.Location = new Point(10, 55);
            radAudio.Name = "radAudio";
            radAudio.Size = new Size(155, 25);
            radAudio.TabIndex = 2;
            radAudio.Text = "🎵 Riproduci Audio";
            radAudio.CheckedChanged += RadAction_CheckedChanged;
            // 
            // txtAudioFile
            // 
            txtAudioFile.Enabled = false;
            txtAudioFile.Font = new Font("Segoe UI", 9F);
            txtAudioFile.Location = new Point(170, 53);
            txtAudioFile.Name = "txtAudioFile";
            txtAudioFile.Size = new Size(395, 23);
            txtAudioFile.TabIndex = 3;
            // 
            // btnBrowseAudio
            // 
            btnBrowseAudio.Cursor = Cursors.Hand;
            btnBrowseAudio.Enabled = false;
            btnBrowseAudio.FlatStyle = FlatStyle.Flat;
            btnBrowseAudio.Location = new Point(585, 53);
            btnBrowseAudio.Name = "btnBrowseAudio";
            btnBrowseAudio.Size = new Size(45, 27);
            btnBrowseAudio.TabIndex = 4;
            btnBrowseAudio.Text = "📁";
            btnBrowseAudio.Click += BtnBrowseAudio_Click;
            // 
            // radMiniPLS
            // 
            radMiniPLS.Location = new Point(10, 85);
            radMiniPLS.Name = "radMiniPLS";
            radMiniPLS.Size = new Size(185, 25);
            radMiniPLS.TabIndex = 5;
            radMiniPLS.Text = "📋 Riproduci Mini Playlist";
            radMiniPLS.CheckedChanged += RadAction_CheckedChanged;
            // 
            // cmbPlaylist
            // 
            cmbPlaylist.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPlaylist.Enabled = false;
            cmbPlaylist.Font = new Font("Segoe UI", 9F);
            cmbPlaylist.Location = new Point(201, 85);
            cmbPlaylist.Name = "cmbPlaylist";
            cmbPlaylist.Size = new Size(419, 23);
            cmbPlaylist.TabIndex = 6;
            // 
            // radTimeSignal
            // 
            radTimeSignal.Location = new Point(10, 115);
            radTimeSignal.Name = "radTimeSignal";
            radTimeSignal.Size = new Size(155, 25);
            radTimeSignal.TabIndex = 7;
            radTimeSignal.Text = "⏰ Segnale Orario";
            radTimeSignal.CheckedChanged += RadAction_CheckedChanged;
            // 
            // radURLStreaming
            // 
            radURLStreaming.Location = new Point(10, 145);
            radURLStreaming.Name = "radURLStreaming";
            radURLStreaming.Size = new Size(155, 25);
            radURLStreaming.TabIndex = 8;
            radURLStreaming.Text = "🌐 URL Streaming";
            radURLStreaming.CheckedChanged += RadAction_CheckedChanged;
            // 
            // lblStreamURL
            // 
            lblStreamURL.Enabled = false;
            lblStreamURL.Font = new Font("Segoe UI", 9F);
            lblStreamURL.Location = new Point(170, 147);
            lblStreamURL.Name = "lblStreamURL";
            lblStreamURL.Size = new Size(35, 20);
            lblStreamURL.TabIndex = 9;
            lblStreamURL.Text = "URL:";
            lblStreamURL.Visible = false;
            // 
            // txtStreamURL
            // 
            txtStreamURL.Enabled = false;
            txtStreamURL.Font = new Font("Segoe UI", 9F);
            txtStreamURL.Location = new Point(170, 145);
            txtStreamURL.Name = "txtStreamURL";
            txtStreamURL.Size = new Size(393, 23);
            txtStreamURL.TabIndex = 10;
            // 
            // lblStreamDuration
            // 
            lblStreamDuration.Enabled = false;
            lblStreamDuration.Font = new Font("Segoe UI", 9F);
            lblStreamDuration.Location = new Point(569, 126);
            lblStreamDuration.Name = "lblStreamDuration";
            lblStreamDuration.Size = new Size(75, 16);
            lblStreamDuration.TabIndex = 11;
            lblStreamDuration.Text = "Durata:";
            lblStreamDuration.Click += lblStreamDuration_Click;
            // 
            // txtStreamDuration
            // 
            txtStreamDuration.Enabled = false;
            txtStreamDuration.Font = new Font("Consolas", 9F);
            txtStreamDuration.Location = new Point(569, 145);
            txtStreamDuration.Mask = "00:00:00";
            txtStreamDuration.Name = "txtStreamDuration";
            txtStreamDuration.Size = new Size(75, 22);
            txtStreamDuration.TabIndex = 12;
            txtStreamDuration.Text = "010000";
            // 
            // lblVideoBuffer
            // 
            lblVideoBuffer.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblVideoBuffer.Location = new Point(20, 252);
            lblVideoBuffer.Name = "lblVideoBuffer";
            lblVideoBuffer.Size = new Size(150, 20);
            lblVideoBuffer.TabIndex = 20;
            lblVideoBuffer.Text = "🎬 Video Buffer (opt.):";
            // 
            // txtVideoBufferPath
            // 
            txtVideoBufferPath.Font = new Font("Segoe UI", 9F);
            txtVideoBufferPath.Location = new Point(180, 250);
            txtVideoBufferPath.Name = "txtVideoBufferPath";
            txtVideoBufferPath.Size = new Size(415, 23);
            txtVideoBufferPath.TabIndex = 21;
            // 
            // btnBrowseVideoBuffer
            // 
            btnBrowseVideoBuffer.Cursor = Cursors.Hand;
            btnBrowseVideoBuffer.FlatStyle = FlatStyle.Flat;
            btnBrowseVideoBuffer.Location = new Point(605, 249);
            btnBrowseVideoBuffer.Name = "btnBrowseVideoBuffer";
            btnBrowseVideoBuffer.Size = new Size(45, 27);
            btnBrowseVideoBuffer.TabIndex = 22;
            btnBrowseVideoBuffer.Text = "📁";
            btnBrowseVideoBuffer.Click += BtnBrowseVideoBuffer_Click;
            // 
            // grpDays
            // 
            grpDays.Controls.Add(chkMonday);
            grpDays.Controls.Add(chkTuesday);
            grpDays.Controls.Add(chkWednesday);
            grpDays.Controls.Add(chkThursday);
            grpDays.Controls.Add(chkFriday);
            grpDays.Controls.Add(chkSaturday);
            grpDays.Controls.Add(chkSunday);
            grpDays.Controls.Add(btnAllDays);
            grpDays.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            grpDays.Location = new Point(20, 285);
            grpDays.Name = "grpDays";
            grpDays.Size = new Size(650, 60);
            grpDays.TabIndex = 3;
            grpDays.TabStop = false;
            grpDays.Text = "Giorni della Settimana";
            // 
            // chkMonday
            // 
            chkMonday.Checked = true;
            chkMonday.CheckState = CheckState.Checked;
            chkMonday.Font = new Font("Segoe UI", 9F);
            chkMonday.Location = new Point(15, 25);
            chkMonday.Name = "chkMonday";
            chkMonday.Size = new Size(70, 25);
            chkMonday.TabIndex = 0;
            chkMonday.Text = "Lun";
            // 
            // chkTuesday
            // 
            chkTuesday.Checked = true;
            chkTuesday.CheckState = CheckState.Checked;
            chkTuesday.Font = new Font("Segoe UI", 9F);
            chkTuesday.Location = new Point(95, 25);
            chkTuesday.Name = "chkTuesday";
            chkTuesday.Size = new Size(70, 25);
            chkTuesday.TabIndex = 1;
            chkTuesday.Text = "Mar";
            // 
            // chkWednesday
            // 
            chkWednesday.Checked = true;
            chkWednesday.CheckState = CheckState.Checked;
            chkWednesday.Font = new Font("Segoe UI", 9F);
            chkWednesday.Location = new Point(175, 25);
            chkWednesday.Name = "chkWednesday";
            chkWednesday.Size = new Size(70, 25);
            chkWednesday.TabIndex = 2;
            chkWednesday.Text = "Mer";
            // 
            // chkThursday
            // 
            chkThursday.Checked = true;
            chkThursday.CheckState = CheckState.Checked;
            chkThursday.Font = new Font("Segoe UI", 9F);
            chkThursday.Location = new Point(255, 25);
            chkThursday.Name = "chkThursday";
            chkThursday.Size = new Size(70, 25);
            chkThursday.TabIndex = 3;
            chkThursday.Text = "Gio";
            // 
            // chkFriday
            // 
            chkFriday.Checked = true;
            chkFriday.CheckState = CheckState.Checked;
            chkFriday.Font = new Font("Segoe UI", 9F);
            chkFriday.Location = new Point(335, 25);
            chkFriday.Name = "chkFriday";
            chkFriday.Size = new Size(70, 25);
            chkFriday.TabIndex = 4;
            chkFriday.Text = "Ven";
            // 
            // chkSaturday
            // 
            chkSaturday.Checked = true;
            chkSaturday.CheckState = CheckState.Checked;
            chkSaturday.Font = new Font("Segoe UI", 9F);
            chkSaturday.Location = new Point(415, 25);
            chkSaturday.Name = "chkSaturday";
            chkSaturday.Size = new Size(70, 25);
            chkSaturday.TabIndex = 5;
            chkSaturday.Text = "Sab";
            // 
            // chkSunday
            // 
            chkSunday.Checked = true;
            chkSunday.CheckState = CheckState.Checked;
            chkSunday.Font = new Font("Segoe UI", 9F);
            chkSunday.Location = new Point(495, 25);
            chkSunday.Name = "chkSunday";
            chkSunday.Size = new Size(70, 25);
            chkSunday.TabIndex = 6;
            chkSunday.Text = "Dom";
            // 
            // btnAllDays
            // 
            btnAllDays.Cursor = Cursors.Hand;
            btnAllDays.FlatStyle = FlatStyle.Flat;
            btnAllDays.Font = new Font("Segoe UI", 7F);
            btnAllDays.Location = new Point(575, 23);
            btnAllDays.Name = "btnAllDays";
            btnAllDays.Size = new Size(55, 25);
            btnAllDays.TabIndex = 7;
            btnAllDays.Text = "✓ Tutti";
            btnAllDays.Click += BtnAllDays_Click;
            // 
            // grpTimes
            // 
            grpTimes.Controls.Add(lblAddTime);
            grpTimes.Controls.Add(dtpTime);
            grpTimes.Controls.Add(btnAddTime);
            grpTimes.Controls.Add(btnRemoveTime);
            grpTimes.Controls.Add(lstTimes);
            grpTimes.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            grpTimes.Location = new Point(20, 355);
            grpTimes.Name = "grpTimes";
            grpTimes.Size = new Size(650, 250);
            grpTimes.TabIndex = 4;
            grpTimes.TabStop = false;
            grpTimes.Text = "Orari di Esecuzione";
            // 
            // lblAddTime
            // 
            lblAddTime.Font = new Font("Segoe UI", 9F);
            lblAddTime.Location = new Point(18, 48);
            lblAddTime.Name = "lblAddTime";
            lblAddTime.Size = new Size(100, 20);
            lblAddTime.TabIndex = 0;
            lblAddTime.Text = "Aggiungi Orario: ";
            // 
            // dtpTime
            // 
            dtpTime.CustomFormat = "HH:mm:ss";
            dtpTime.Font = new Font("Segoe UI", 9F);
            dtpTime.Format = DateTimePickerFormat.Custom;
            dtpTime.Location = new Point(123, 46);
            dtpTime.Name = "dtpTime";
            dtpTime.ShowUpDown = true;
            dtpTime.Size = new Size(120, 23);
            dtpTime.TabIndex = 1;
            // 
            // btnAddTime
            // 
            btnAddTime.BackColor = Color.FromArgb(76, 175, 80);
            btnAddTime.Cursor = Cursors.Hand;
            btnAddTime.FlatAppearance.BorderSize = 0;
            btnAddTime.FlatStyle = FlatStyle.Flat;
            btnAddTime.ForeColor = Color.White;
            btnAddTime.Location = new Point(138, 93);
            btnAddTime.Name = "btnAddTime";
            btnAddTime.Size = new Size(90, 27);
            btnAddTime.TabIndex = 2;
            btnAddTime.Text = "➕ Aggiungi";
            btnAddTime.UseVisualStyleBackColor = false;
            btnAddTime.Click += BtnAddTime_Click;
            // 
            // btnRemoveTime
            // 
            btnRemoveTime.BackColor = Color.FromArgb(244, 67, 54);
            btnRemoveTime.Cursor = Cursors.Hand;
            btnRemoveTime.FlatAppearance.BorderSize = 0;
            btnRemoveTime.FlatStyle = FlatStyle.Flat;
            btnRemoveTime.ForeColor = Color.White;
            btnRemoveTime.Location = new Point(138, 135);
            btnRemoveTime.Name = "btnRemoveTime";
            btnRemoveTime.Size = new Size(90, 27);
            btnRemoveTime.TabIndex = 3;
            btnRemoveTime.Text = "🗑️ Rimuovi";
            btnRemoveTime.UseVisualStyleBackColor = false;
            btnRemoveTime.Click += BtnRemoveTime_Click;
            // 
            // lstTimes
            // 
            lstTimes.Font = new Font("Consolas", 10F);
            lstTimes.ItemHeight = 15;
            lstTimes.Location = new Point(273, 30);
            lstTimes.Name = "lstTimes";
            lstTimes.Size = new Size(290, 199);
            lstTimes.TabIndex = 4;
            // 
            // btnSave
            // 
            btnSave.BackColor = Color.FromArgb(76, 175, 80);
            btnSave.Cursor = Cursors.Hand;
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnSave.ForeColor = Color.White;
            btnSave.Location = new Point(480, 625);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(90, 35);
            btnSave.TabIndex = 5;
            btnSave.Text = "💾 Salva";
            btnSave.UseVisualStyleBackColor = false;
            btnSave.Click += BtnSave_Click;
            // 
            // btnCancel
            // 
            btnCancel.BackColor = Color.FromArgb(244, 67, 54);
            btnCancel.Cursor = Cursors.Hand;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnCancel.ForeColor = Color.White;
            btnCancel.Location = new Point(580, 625);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(90, 35);
            btnCancel.TabIndex = 6;
            btnCancel.Text = "✖ Annulla";
            btnCancel.UseVisualStyleBackColor = false;
            // 
            // ScheduleEditorForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(245, 245, 245);
            CancelButton = btnCancel;
            ClientSize = new Size(700, 685);
            Controls.Add(btnCancel);
            Controls.Add(btnSave);
            Controls.Add(grpTimes);
            Controls.Add(grpDays);
            Controls.Add(btnBrowseVideoBuffer);
            Controls.Add(txtVideoBufferPath);
            Controls.Add(lblVideoBuffer);
            Controls.Add(grpAction);
            Controls.Add(txtName);
            Controls.Add(lblName);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ScheduleEditorForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "📅 Schedulazione";
            grpAction.ResumeLayout(false);
            grpAction.PerformLayout();
            grpDays.ResumeLayout(false);
            grpTimes.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }
    }
}