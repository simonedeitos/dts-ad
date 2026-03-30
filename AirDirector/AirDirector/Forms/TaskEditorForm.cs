using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using AirDirector.Models;
using AirDirector.Services.Localization;
using AirDirector.Themes;

namespace AirDirector.Forms
{
    public partial class TaskEditorForm : Form
    {
        public DownloadTask Task { get; private set; }

        private TabControl tabControl;
        private TextBox txtTaskName;
        private RadioButton radioHttp;
        private RadioButton radioFtp;
        private Panel panelHttpSettings;
        private Panel panelFtpSettings;
        private TextBox txtHttpUrl;
        private TextBox txtHttpUsername;
        private TextBox txtHttpPassword;
        private TextBox txtFtpHost;
        private TextBox txtFtpFilePath;
        private TextBox txtFtpUsername;
        private TextBox txtFtpPassword;
        private TextBox txtLocalPath;
        private CheckBox chkMonday;
        private CheckBox chkTuesday;
        private CheckBox chkWednesday;
        private CheckBox chkThursday;
        private CheckBox chkFriday;
        private CheckBox chkSaturday;
        private CheckBox chkSunday;
        private DateTimePicker dtpTime;
        private ListBox listTimes;
        private CheckBox chkEnableComposition;

        private Label lblName;
        private Label lblHttpUrl;
        private Label lblHttpUser;
        private Label lblHttpPass;
        private Label lblFtpHost;
        private Label lblFtpPath;
        private Label lblFtpUser;
        private Label lblFtpPass;
        private Label lblLocal;
        private Label lblTime;
        private Label lblInfo;
        private Button btnSave;
        private Button btnCancel;
        private Button btnAddTime;
        private Button btnRemoveTime;
        private Button btnEditComposition;
        private GroupBox gbType;
        private GroupBox gbLocal;
        private GroupBox gbDays;
        private GroupBox gbTimes;

        public TaskEditorForm(DownloadTask task)
        {
            if (task != null)
            {
                Task = new DownloadTask();
                Task.CopyFrom(task);
            }
            else
            {
                Task = new DownloadTask
                {
                    Name = "",
                    IsHttpDownload = true,
                    ScheduleTimes = new List<string>()
                };
            }

            InitializeComponent();
            ApplyLanguage();
            LoadTaskData();

            LanguageManager.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            this.Text = LanguageManager.GetString("TaskEditor.Title", "Configura Task Download");

            if (tabControl != null && tabControl.TabPages.Count >= 3)
            {
                tabControl.TabPages[0].Text = "📋 " + LanguageManager.GetString("TaskEditor.TabGeneral", "Generale");
                tabControl.TabPages[1].Text = "📅 " + LanguageManager.GetString("TaskEditor.TabSchedule", "Schedulazione");
                tabControl.TabPages[2].Text = "🎵 " + LanguageManager.GetString("TaskEditor.TabComposition", "Composizione");
            }

            if (lblName != null)
                lblName.Text = LanguageManager.GetString("TaskEditor.TaskName", "Nome Task:");

            if (gbType != null)
                gbType.Text = LanguageManager.GetString("TaskEditor.DownloadType", "Tipo di Download");

            if (radioHttp != null)
                radioHttp.Text = "HTTP/HTTPS";

            if (radioFtp != null)
                radioFtp.Text = "FTP";

            if (lblHttpUrl != null)
                lblHttpUrl.Text = LanguageManager.GetString("TaskEditor.Url", "URL:");

            if (lblHttpUser != null)
                lblHttpUser.Text = LanguageManager.GetString("TaskEditor.Username", "Username:");

            if (lblHttpPass != null)
                lblHttpPass.Text = LanguageManager.GetString("TaskEditor.Password", "Password:");

            if (lblFtpHost != null)
                lblFtpHost.Text = LanguageManager.GetString("TaskEditor.Host", "Host:");

            if (lblFtpPath != null)
                lblFtpPath.Text = LanguageManager.GetString("TaskEditor.FilePath", "Percorso file:");

            if (lblFtpUser != null)
                lblFtpUser.Text = LanguageManager.GetString("TaskEditor.Username", "Username:");

            if (lblFtpPass != null)
                lblFtpPass.Text = LanguageManager.GetString("TaskEditor.Password", "Password:");

            if (gbLocal != null)
                gbLocal.Text = LanguageManager.GetString("TaskEditor.DestinationFile", "File di Destinazione");

            if (lblLocal != null)
                lblLocal.Text = LanguageManager.GetString("TaskEditor.Path", "Percorso:");

            if (gbDays != null)
                gbDays.Text = LanguageManager.GetString("TaskEditor.DownloadDays", "Giorni di Download");

            if (chkMonday != null)
                chkMonday.Text = LanguageManager.GetString("Download.DayMon", "Lun");

            if (chkTuesday != null)
                chkTuesday.Text = LanguageManager.GetString("Download.DayTue", "Mar");

            if (chkWednesday != null)
                chkWednesday.Text = LanguageManager.GetString("Download.DayWed", "Mer");

            if (chkThursday != null)
                chkThursday.Text = LanguageManager.GetString("Download.DayThu", "Gio");

            if (chkFriday != null)
                chkFriday.Text = LanguageManager.GetString("Download.DayFri", "Ven");

            if (chkSaturday != null)
                chkSaturday.Text = LanguageManager.GetString("Download.DaySat", "Sab");

            if (chkSunday != null)
                chkSunday.Text = LanguageManager.GetString("Download.DaySun", "Dom");

            if (gbTimes != null)
                gbTimes.Text = LanguageManager.GetString("TaskEditor.DownloadTimes", "Orari di Download");

            if (lblTime != null)
                lblTime.Text = LanguageManager.GetString("TaskEditor.Time24h", "Orario (24h):");

            if (btnAddTime != null)
                btnAddTime.Text = "➕ " + LanguageManager.GetString("Download.Add", "Aggiungi");

            if (btnRemoveTime != null)
                btnRemoveTime.Text = "🗑️ " + LanguageManager.GetString("TaskEditor.Remove", "Rimuovi");

            if (chkEnableComposition != null)
                chkEnableComposition.Text = LanguageManager.GetString("TaskEditor.EnableComposition", "Abilita Composizione Audio");

            if (btnEditComposition != null)
                btnEditComposition.Text = "⚙️ " + LanguageManager.GetString("TaskEditor.ConfigureComposition", "Configura Composizione Audio");

            if (lblInfo != null)
                lblInfo.Text = LanguageManager.GetString("TaskEditor.CompositionInfo",
                    "La composizione audio permette di:\n\n" +
                    "• Aggiungere jingle di apertura e chiusura\n" +
                    "• Mixare una base musicale di sottofondo\n" +
                    "• Aumentare il volume del file finale\n" +
                    "• Generare un file MP3 composto automaticamente");

            if (btnSave != null)
                btnSave.Text = "💾 " + LanguageManager.GetString("Common.Save", "SALVA");

            if (btnCancel != null)
                btnCancel.Text = "✖ " + LanguageManager.GetString("Common.Cancel", "ANNULLA");
        }

        private void InitializeComponent()
        {
            this.Text = "Configura Task Download";
            this.Size = new Size(750, 650);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(750, 650);
            this.BackColor = AppTheme.BgLight;

            Panel bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(40, 40, 40),
                Padding = new Padding(10)
            };
            this.Controls.Add(bottomPanel);

            btnSave = new Button
            {
                Text = "💾 SALVA",
                Size = new Size(120, 40),
                BackColor = Color.FromArgb(0, 180, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.OK
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;
            bottomPanel.Controls.Add(btnSave);

            btnCancel = new Button
            {
                Text = "✖ ANNULLA",
                Size = new Size(120, 40),
                BackColor = Color.FromArgb(80, 80, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            bottomPanel.Controls.Add(btnCancel);

            bottomPanel.Resize += (s, e) =>
            {
                btnCancel.Location = new Point(bottomPanel.Width - 130, 10);
                btnSave.Location = new Point(bottomPanel.Width - 260, 10);
            };
            bottomPanel.PerformLayout();

            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            this.Controls.Add(tabControl);

            CreateGeneralTab();
            CreateScheduleTab();
            CreateCompositionTab();
        }

        private void CreateGeneralTab()
        {
            TabPage tabGeneral = new TabPage("📋 Generale");
            tabControl.TabPages.Add(tabGeneral);

            Panel contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(15),
                BackColor = Color.White
            };
            tabGeneral.Controls.Add(contentPanel);

            int yPos = 10;

            lblName = new Label
            {
                Text = "Nome Task:",
                Location = new Point(15, yPos),
                Size = new Size(120, 23),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            contentPanel.Controls.Add(lblName);

            txtTaskName = new TextBox
            {
                Location = new Point(140, yPos),
                Size = new Size(550, 23),
                Font = new Font("Segoe UI", 10)
            };
            contentPanel.Controls.Add(txtTaskName);

            yPos += 40;

            gbType = new GroupBox
            {
                Text = "Tipo di Download",
                Location = new Point(15, yPos),
                Size = new Size(680, 220),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            contentPanel.Controls.Add(gbType);

            radioHttp = new RadioButton
            {
                Text = "HTTP/HTTPS",
                Location = new Point(15, 25),
                Size = new Size(120, 23),
                Checked = true,
                Font = new Font("Segoe UI", 10)
            };
            radioHttp.CheckedChanged += RadioHttp_CheckedChanged;
            gbType.Controls.Add(radioHttp);

            radioFtp = new RadioButton
            {
                Text = "FTP",
                Location = new Point(140, 25),
                Size = new Size(80, 23),
                Font = new Font("Segoe UI", 10)
            };
            radioFtp.CheckedChanged += RadioFtp_CheckedChanged;
            gbType.Controls.Add(radioFtp);

            panelHttpSettings = new Panel
            {
                Location = new Point(15, 55),
                Size = new Size(650, 150)
            };
            gbType.Controls.Add(panelHttpSettings);

            lblHttpUrl = new Label { Text = "URL:", Location = new Point(0, 5), Size = new Size(100, 23) };
            panelHttpSettings.Controls.Add(lblHttpUrl);

            txtHttpUrl = new TextBox { Location = new Point(110, 5), Size = new Size(530, 23) };
            panelHttpSettings.Controls.Add(txtHttpUrl);

            lblHttpUser = new Label { Text = "Username:", Location = new Point(0, 35), Size = new Size(100, 23) };
            panelHttpSettings.Controls.Add(lblHttpUser);

            txtHttpUsername = new TextBox { Location = new Point(110, 35), Size = new Size(530, 23) };
            panelHttpSettings.Controls.Add(txtHttpUsername);

            lblHttpPass = new Label { Text = "Password:", Location = new Point(0, 65), Size = new Size(100, 23) };
            panelHttpSettings.Controls.Add(lblHttpPass);

            txtHttpPassword = new TextBox { Location = new Point(110, 65), Size = new Size(530, 23), PasswordChar = '•' };
            panelHttpSettings.Controls.Add(txtHttpPassword);

            panelFtpSettings = new Panel
            {
                Location = new Point(15, 55),
                Size = new Size(650, 150),
                Visible = false
            };
            gbType.Controls.Add(panelFtpSettings);

            lblFtpHost = new Label { Text = "Host:", Location = new Point(0, 5), Size = new Size(100, 23) };
            panelFtpSettings.Controls.Add(lblFtpHost);

            txtFtpHost = new TextBox { Location = new Point(110, 5), Size = new Size(530, 23) };
            panelFtpSettings.Controls.Add(txtFtpHost);

            lblFtpPath = new Label { Text = "Percorso file:", Location = new Point(0, 35), Size = new Size(100, 23) };
            panelFtpSettings.Controls.Add(lblFtpPath);

            txtFtpFilePath = new TextBox { Location = new Point(110, 35), Size = new Size(530, 23) };
            panelFtpSettings.Controls.Add(txtFtpFilePath);

            lblFtpUser = new Label { Text = "Username:", Location = new Point(0, 65), Size = new Size(100, 23) };
            panelFtpSettings.Controls.Add(lblFtpUser);

            txtFtpUsername = new TextBox { Location = new Point(110, 65), Size = new Size(530, 23) };
            panelFtpSettings.Controls.Add(txtFtpUsername);

            lblFtpPass = new Label { Text = "Password:", Location = new Point(0, 95), Size = new Size(100, 23) };
            panelFtpSettings.Controls.Add(lblFtpPass);

            txtFtpPassword = new TextBox { Location = new Point(110, 95), Size = new Size(530, 23), PasswordChar = '•' };
            panelFtpSettings.Controls.Add(txtFtpPassword);

            yPos += 230;

            gbLocal = new GroupBox
            {
                Text = "File di Destinazione",
                Location = new Point(15, yPos),
                Size = new Size(680, 70),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            contentPanel.Controls.Add(gbLocal);

            lblLocal = new Label { Text = "Percorso:", Location = new Point(15, 28), Size = new Size(80, 23) };
            gbLocal.Controls.Add(lblLocal);

            txtLocalPath = new TextBox { Location = new Point(100, 28), Size = new Size(505, 23) };
            gbLocal.Controls.Add(txtLocalPath);

            Button btnBrowseLocal = new Button
            {
                Text = "📂",
                Location = new Point(615, 28),
                Size = new Size(40, 25),
                Font = new Font("Segoe UI", 8),
                FlatStyle = FlatStyle.Flat
            };
            btnBrowseLocal.Click += BtnBrowseLocalPath_Click;
            gbLocal.Controls.Add(btnBrowseLocal);
        }

        private void CreateScheduleTab()
        {
            TabPage tabSchedule = new TabPage("📅 Schedulazione");
            tabControl.TabPages.Add(tabSchedule);

            Panel contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(15),
                BackColor = Color.White
            };
            tabSchedule.Controls.Add(contentPanel);

            gbDays = new GroupBox
            {
                Text = "Giorni di Download",
                Location = new Point(15, 15),
                Size = new Size(680, 80),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            contentPanel.Controls.Add(gbDays);

            chkMonday = new CheckBox { Text = "Lun", Location = new Point(15, 30), Size = new Size(70, 23) };
            gbDays.Controls.Add(chkMonday);

            chkTuesday = new CheckBox { Text = "Mar", Location = new Point(90, 30), Size = new Size(70, 23) };
            gbDays.Controls.Add(chkTuesday);

            chkWednesday = new CheckBox { Text = "Mer", Location = new Point(165, 30), Size = new Size(70, 23) };
            gbDays.Controls.Add(chkWednesday);

            chkThursday = new CheckBox { Text = "Gio", Location = new Point(240, 30), Size = new Size(70, 23) };
            gbDays.Controls.Add(chkThursday);

            chkFriday = new CheckBox { Text = "Ven", Location = new Point(315, 30), Size = new Size(70, 23) };
            gbDays.Controls.Add(chkFriday);

            chkSaturday = new CheckBox { Text = "Sab", Location = new Point(390, 30), Size = new Size(70, 23) };
            gbDays.Controls.Add(chkSaturday);

            chkSunday = new CheckBox { Text = "Dom", Location = new Point(465, 30), Size = new Size(70, 23) };
            gbDays.Controls.Add(chkSunday);

            gbTimes = new GroupBox
            {
                Text = "Orari di Download",
                Location = new Point(15, 105),
                Size = new Size(680, 350),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            contentPanel.Controls.Add(gbTimes);

            lblTime = new Label { Text = "Orario (24h):", Location = new Point(15, 30), Size = new Size(100, 23) };
            gbTimes.Controls.Add(lblTime);

            dtpTime = new DateTimePicker
            {
                Location = new Point(120, 30),
                Size = new Size(150, 23),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "HH:mm:ss",
                ShowUpDown = true
            };
            gbTimes.Controls.Add(dtpTime);

            btnAddTime = new Button
            {
                Text = "➕ Aggiungi",
                Location = new Point(280, 30),
                Size = new Size(100, 28),
                BackColor = Color.FromArgb(0, 180, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnAddTime.FlatAppearance.BorderSize = 0;
            btnAddTime.Click += BtnAddTime_Click;
            gbTimes.Controls.Add(btnAddTime);

            btnRemoveTime = new Button
            {
                Text = "🗑️ Rimuovi",
                Location = new Point(390, 30),
                Size = new Size(100, 28),
                BackColor = AppTheme.Danger,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnRemoveTime.FlatAppearance.BorderSize = 0;
            btnRemoveTime.Click += BtnRemoveTime_Click;
            gbTimes.Controls.Add(btnRemoveTime);

            listTimes = new ListBox
            {
                Location = new Point(15, 70),
                Size = new Size(650, 265),
                Font = new Font("Consolas", 10)
            };
            gbTimes.Controls.Add(listTimes);
        }

        private void CreateCompositionTab()
        {
            TabPage tabComposition = new TabPage("🎵 Composizione");
            tabControl.TabPages.Add(tabComposition);

            Panel contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15),
                BackColor = Color.White
            };
            tabComposition.Controls.Add(contentPanel);

            chkEnableComposition = new CheckBox
            {
                Text = "Abilita Composizione Audio",
                Location = new Point(15, 15),
                Size = new Size(250, 23),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            contentPanel.Controls.Add(chkEnableComposition);

            btnEditComposition = new Button
            {
                Text = "⚙️ Configura Composizione Audio",
                Location = new Point(15, 50),
                Size = new Size(280, 40),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnEditComposition.FlatAppearance.BorderSize = 0;
            btnEditComposition.Click += BtnEditComposition_Click;
            contentPanel.Controls.Add(btnEditComposition);

            lblInfo = new Label
            {
                Text = "La composizione audio permette di:\n\n" +
                       "• Aggiungere jingle di apertura e chiusura\n" +
                       "• Mixare una base musicale di sottofondo\n" +
                       "• Aumentare il volume del file finale\n" +
                       "• Generare un file MP3 composto automaticamente",
                Location = new Point(15, 110),
                Size = new Size(650, 120),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Gray
            };
            contentPanel.Controls.Add(lblInfo);
        }

        private void LoadTaskData()
        {
            if (txtTaskName == null || radioHttp == null || radioFtp == null)
                return;

            txtTaskName.Text = Task.Name;

            radioHttp.Checked = Task.IsHttpDownload;
            radioFtp.Checked = !Task.IsHttpDownload;

            txtHttpUrl.Text = Task.HttpUrl;
            txtHttpUsername.Text = Task.HttpUsername;
            txtHttpPassword.Text = Task.HttpPassword;

            txtFtpHost.Text = Task.FtpHost;
            txtFtpFilePath.Text = Task.FtpFilePath;
            txtFtpUsername.Text = Task.FtpUsername;
            txtFtpPassword.Text = Task.FtpPassword;

            txtLocalPath.Text = Task.LocalFilePath;

            chkMonday.Checked = Task.Monday;
            chkTuesday.Checked = Task.Tuesday;
            chkWednesday.Checked = Task.Wednesday;
            chkThursday.Checked = Task.Thursday;
            chkFriday.Checked = Task.Friday;
            chkSaturday.Checked = Task.Saturday;
            chkSunday.Checked = Task.Sunday;

            RefreshScheduleTimes();

            chkEnableComposition.Checked = Task.CompositionEnabled;

            UpdateUIState();
        }

        private void RefreshScheduleTimes()
        {
            if (listTimes == null) return;

            listTimes.Items.Clear();
            foreach (string time in Task.ScheduleTimes)
            {
                listTimes.Items.Add(time);
            }
        }

        private void UpdateUIState()
        {
            if (panelHttpSettings != null && panelFtpSettings != null)
            {
                panelHttpSettings.Visible = radioHttp.Checked;
                panelFtpSettings.Visible = radioFtp.Checked;
            }
        }

        private void RadioHttp_CheckedChanged(object sender, EventArgs e)
        {
            Task.IsHttpDownload = radioHttp.Checked;
            UpdateUIState();
        }

        private void RadioFtp_CheckedChanged(object sender, EventArgs e)
        {
            Task.IsHttpDownload = !radioFtp.Checked;
            UpdateUIState();
        }

        private void BtnAddTime_Click(object sender, EventArgs e)
        {
            string time = dtpTime.Value.ToString("HH:mm:ss");

            if (!Task.ScheduleTimes.Contains(time))
            {
                Task.ScheduleTimes.Add(time);
                Task.ScheduleTimes.Sort();
                RefreshScheduleTimes();
            }
        }

        private void BtnRemoveTime_Click(object sender, EventArgs e)
        {
            if (listTimes.SelectedIndex >= 0)
            {
                string time = listTimes.SelectedItem.ToString();
                Task.ScheduleTimes.Remove(time);
                RefreshScheduleTimes();
            }
        }

        private void BtnBrowseLocalPath_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = LanguageManager.GetString("TaskEditor.AllFiles", "Tutti i file (*.*)|*.*");
                dialog.Title = LanguageManager.GetString("TaskEditor.SelectDestination", "Seleziona il percorso del file di destinazione");

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtLocalPath.Text = dialog.FileName;
                }
            }
        }

        private void BtnEditComposition_Click(object sender, EventArgs e)
        {
            using (var compositionForm = new CompositionSettingsForm(Task))
            {
                compositionForm.ShowDialog();
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTaskName.Text))
            {
                MessageBox.Show(
                    LanguageManager.GetString("TaskEditor.ErrorTaskName", "Inserisci un nome per il task."),
                    LanguageManager.GetString("TaskEditor.RequiredField", "Campo richiesto"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                tabControl.SelectedIndex = 0;
                txtTaskName.Focus();
                return;
            }

            if (Task.IsHttpDownload)
            {
                if (string.IsNullOrWhiteSpace(txtHttpUrl.Text))
                {
                    MessageBox.Show(
                        LanguageManager.GetString("TaskEditor.ErrorHttpUrl", "Inserisci l'URL HTTP."),
                        LanguageManager.GetString("TaskEditor.RequiredField", "Campo richiesto"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    tabControl.SelectedIndex = 0;
                    txtHttpUrl.Focus();
                    return;
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(txtFtpHost.Text) ||
                    string.IsNullOrWhiteSpace(txtFtpFilePath.Text) ||
                    string.IsNullOrWhiteSpace(txtFtpUsername.Text) ||
                    string.IsNullOrWhiteSpace(txtFtpPassword.Text))
                {
                    MessageBox.Show(
                        LanguageManager.GetString("TaskEditor.ErrorFtpFields", "Compila tutti i campi FTP."),
                        LanguageManager.GetString("TaskEditor.RequiredFields", "Campi richiesti"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    tabControl.SelectedIndex = 0;
                    return;
                }
            }

            if (string.IsNullOrWhiteSpace(txtLocalPath.Text))
            {
                MessageBox.Show(
                    LanguageManager.GetString("TaskEditor.ErrorLocalPath", "Inserisci il percorso locale di salvataggio."),
                    LanguageManager.GetString("TaskEditor.RequiredField", "Campo richiesto"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                tabControl.SelectedIndex = 0;
                txtLocalPath.Focus();
                return;
            }

            if (!chkMonday.Checked && !chkTuesday.Checked && !chkWednesday.Checked &&
                !chkThursday.Checked && !chkFriday.Checked && !chkSaturday.Checked && !chkSunday.Checked)
            {
                MessageBox.Show(
                    LanguageManager.GetString("TaskEditor.ErrorNoDays", "Seleziona almeno un giorno per la schedulazione."),
                    LanguageManager.GetString("TaskEditor.RequiredField", "Campo richiesto"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                tabControl.SelectedIndex = 1;
                return;
            }

            if (Task.ScheduleTimes.Count == 0)
            {
                MessageBox.Show(
                    LanguageManager.GetString("TaskEditor.ErrorNoTimes", "Aggiungi almeno un orario per la schedulazione."),
                    LanguageManager.GetString("TaskEditor.RequiredField", "Campo richiesto"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                tabControl.SelectedIndex = 1;
                return;
            }

            Task.Name = txtTaskName.Text;
            Task.HttpUrl = txtHttpUrl.Text;
            Task.HttpUsername = txtHttpUsername.Text;
            Task.HttpPassword = txtHttpPassword.Text;
            Task.FtpHost = txtFtpHost.Text;
            Task.FtpFilePath = txtFtpFilePath.Text;
            Task.FtpUsername = txtFtpUsername.Text;
            Task.FtpPassword = txtFtpPassword.Text;
            Task.LocalFilePath = txtLocalPath.Text;
            Task.Monday = chkMonday.Checked;
            Task.Tuesday = chkTuesday.Checked;
            Task.Wednesday = chkWednesday.Checked;
            Task.Thursday = chkThursday.Checked;
            Task.Friday = chkFriday.Checked;
            Task.Saturday = chkSaturday.Checked;
            Task.Sunday = chkSunday.Checked;
            Task.CompositionEnabled = chkEnableComposition.Checked;

            DialogResult = DialogResult.OK;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                LanguageManager.SaveMissingKeysToFile();
                LanguageManager.LanguageChanged -= OnLanguageChanged;
            }
            base.Dispose(disposing);
        }
    }
}