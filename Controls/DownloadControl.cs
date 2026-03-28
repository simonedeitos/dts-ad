using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using AirDirector.Models;
using AirDirector.Services.Core;
using AirDirector.Services.Database;
using AirDirector.Services.Localization;
using AirDirector.Themes;
using AirDirector.Forms;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Threading;

namespace AirDirector.Controls
{
    public partial class DownloadControl : UserControl
    {
        private List<DownloadTask> _downloadTasks = new List<DownloadTask>();
        private System.Windows.Forms.Timer _schedulerTimer;
        private System.Windows.Forms.Timer _countdownTimer;
        private DailyLogger _dailyLogger;
        private readonly Dictionary<string, DateTime> _lastExecuted = new Dictionary<string, DateTime>();
        private bool _isProcessing = false;
        private bool _logVisible = true;

        private FlowLayoutPanel flowTasks;
        private TextBox txtLog;
        private ProgressBar progressMain;
        private Label lblStatus;
        private Button btnToggleLog;
        private Panel panelLog;
        private SplitContainer splitContainer;

        private Label lblNextTaskTime;
        private Label lblNextTaskName;
        private Label lblCountdown;
        private Label lblTitle;
        private Label lblNextScheduleTitle;
        private Button btnDateHelp;
        private Button btnAddTask;
        private Button btnRefresh;
        private Button btnClearLog;

        public DownloadControl()
        {
            InitializeComponent();

            try { _dailyLogger = new DailyLogger("Download"); } catch { }

            _schedulerTimer = new System.Windows.Forms.Timer();
            _schedulerTimer.Interval = 1000;
            _schedulerTimer.Tick += SchedulerTimer_Tick;
            _schedulerTimer.Start();

            _countdownTimer = new System.Windows.Forms.Timer();
            _countdownTimer.Interval = 1000;
            _countdownTimer.Tick += CountdownTimer_Tick;
            _countdownTimer.Start();

            LoadDownloadTasks();
            ApplyLanguage();
            RefreshDownloadTasksList();
            UpdateNextScheduleInfo();

            LanguageManager.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            ApplyLanguage();
            RefreshDownloadTasksList();
        }

        private void ApplyLanguage()
        {
            if (lblTitle != null)
                lblTitle.Text = "📥 " + LanguageManager.GetString("Download.Title", "DOWNLOADER");

            if (btnDateHelp != null)
                btnDateHelp.Text = "?     " + LanguageManager.GetString("Download.DateHelp", "Date");

            if (lblNextScheduleTitle != null)
                lblNextScheduleTitle.Text = LanguageManager.GetString("Download.NextSchedule", "PROSSIMA SCHEDULAZIONE");

            if (lblNextTaskName != null && lblNextTaskName.Text == "Nessun task")
                lblNextTaskName.Text = LanguageManager.GetString("Download.NoTask", "Nessun task");

            if (btnAddTask != null)
                btnAddTask.Text = "➕ " + LanguageManager.GetString("Download.Add", "Aggiungi");

            if (btnRefresh != null)
                btnRefresh.Text = "🔄 " + LanguageManager.GetString("Download.Refresh", "Aggiorna");

            if (btnClearLog != null)
                btnClearLog.Text = "🧹 " + LanguageManager.GetString("Download.ClearLog", "Pulisci Log");

            if (btnToggleLog != null)
            {
                btnToggleLog.Text = _logVisible ?
                    "▼ " + LanguageManager.GetString("Download.HideLog", "Nascondi Log") :
                    "▲ " + LanguageManager.GetString("Download.ShowLog", "Mostra Log");
            }
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = AppTheme.BgLight;
            this.Padding = new Padding(0);

            Panel headerPanel = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(800, 100),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = AppTheme.BgDark,
                Padding = new Padding(15, 10, 15, 10)
            };
            this.Controls.Add(headerPanel);

            lblTitle = new Label
            {
                Text = "📥 DOWNLOADER",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 15),
                AutoSize = true
            };
            headerPanel.Controls.Add(lblTitle);

            btnDateHelp = new Button
            {
                Text = "? Date",
                Size = new Size(80, 28),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Location = new Point(15, 55)
            };
            btnDateHelp.FlatAppearance.BorderSize = 0;
            btnDateHelp.Click += ShowDateVariablesHelp;
            headerPanel.Controls.Add(btnDateHelp);

            Panel nextSchedulePanel = new Panel
            {
                Size = new Size(380, 80),
                Location = new Point(405, 10),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.FromArgb(40, 40, 40),
                BorderStyle = BorderStyle.FixedSingle
            };
            headerPanel.Controls.Add(nextSchedulePanel);

            lblNextScheduleTitle = new Label
            {
                Text = "PROSSIMA SCHEDULAZIONE",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.LightGray,
                Location = new Point(10, 5),
                Size = new Size(360, 20),
                TextAlign = ContentAlignment.TopCenter
            };
            nextSchedulePanel.Controls.Add(lblNextScheduleTitle);

            lblNextTaskTime = new Label
            {
                Text = "--:--:--",
                Font = new Font("Consolas", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 255, 127),
                Location = new Point(10, 32),
                Size = new Size(110, 25),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft
            };
            nextSchedulePanel.Controls.Add(lblNextTaskTime);

            lblNextTaskName = new Label
            {
                Text = "Nessun task",
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = Color.White,
                Location = new Point(105, 32),
                Size = new Size(160, 25),
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleCenter
            };
            nextSchedulePanel.Controls.Add(lblNextTaskName);

            lblCountdown = new Label
            {
                Text = "--:--:--",
                Font = new Font("Consolas", 14, FontStyle.Bold),
                ForeColor = Color.Orange,
                Location = new Point(270, 32),
                Size = new Size(100, 25),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleRight
            };
            nextSchedulePanel.Controls.Add(lblCountdown);

            progressMain = new ProgressBar
            {
                Size = new Size(360, 8),
                Location = new Point(10, 64),
                Visible = false
            };
            nextSchedulePanel.Controls.Add(progressMain);

            headerPanel.Resize += (s, e) =>
            {
                nextSchedulePanel.Location = new Point(headerPanel.Width - 395, 10);
            };

            Panel controlsPanel = new Panel
            {
                Location = new Point(0, 500),
                Size = new Size(800, 60),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(40, 40, 40),
                Padding = new Padding(10, 8, 10, 8)
            };
            this.Controls.Add(controlsPanel);

            btnAddTask = new Button
            {
                Text = "➕ Aggiungi",
                Size = new Size(110, 44),
                Location = new Point(10, 8),
                BackColor = Color.FromArgb(0, 180, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAddTask.FlatAppearance.BorderSize = 0;
            btnAddTask.Click += BtnAddTask_Click;
            controlsPanel.Controls.Add(btnAddTask);

            btnRefresh = new Button
            {
                Text = "🔄 Aggiorna",
                Size = new Size(110, 44),
                Location = new Point(125, 8),
                BackColor = AppTheme.Info,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (s, e) => RefreshDownloadTasksList();
            controlsPanel.Controls.Add(btnRefresh);

            lblStatus = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true,
                Location = new Point(480, 20)
            };
            controlsPanel.Controls.Add(lblStatus);

            btnClearLog = new Button
            {
                Text = "🧹 Pulisci Log",
                Size = new Size(120, 44),
                BackColor = Color.FromArgb(80, 80, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClearLog.FlatAppearance.BorderSize = 0;
            btnClearLog.Click += BtnClearLog_Click;
            controlsPanel.Controls.Add(btnClearLog);

            btnToggleLog = new Button
            {
                Text = "▼ Nascondi Log",
                Size = new Size(140, 44),
                BackColor = Color.FromArgb(138, 43, 226),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnToggleLog.FlatAppearance.BorderSize = 0;
            btnToggleLog.Click += BtnToggleLog_Click;
            controlsPanel.Controls.Add(btnToggleLog);

            controlsPanel.Resize += (s, e) =>
            {
                int panelWidth = controlsPanel.Width;
                lblStatus.Location = new Point(panelWidth / 2 - 100, 20);
                btnClearLog.Location = new Point(panelWidth - 275, 8);
                btnToggleLog.Location = new Point(panelWidth - 150, 8);
            };

            splitContainer = new SplitContainer
            {
                Location = new Point(0, 100),
                Size = new Size(800, 400),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Orientation = Orientation.Horizontal,
                BackColor = AppTheme.BgLight,
                Panel1MinSize = 100,
                Panel2MinSize = 100
            };
            this.Controls.Add(splitContainer);

            flowTasks = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(15),
                BackColor = AppTheme.BgLight,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };
            splitContainer.Panel1.Controls.Add(flowTasks);

            panelLog = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 20, 20),
                Padding = new Padding(5)
            };
            splitContainer.Panel2.Controls.Add(panelLog);

            txtLog = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.LightGreen,
                Font = new Font("Consolas", 9),
                ScrollBars = ScrollBars.Both,
                BorderStyle = BorderStyle.None
            };
            panelLog.Controls.Add(txtLog);

            this.Load += (s, e) =>
            {
                try
                {
                    int availableHeight = this.Height - 100 - 60;
                    if (availableHeight > 200)
                    {
                        splitContainer.SplitterDistance = availableHeight * 2 / 3;
                    }
                }
                catch { }
            };

            this.Resize += (s, e) =>
            {
                if (this.Height > 200 && this.Width > 400)
                {
                    headerPanel.Size = new Size(this.Width, 100);
                    controlsPanel.Location = new Point(0, this.Height - 60);
                    controlsPanel.Size = new Size(this.Width, 60);
                    splitContainer.Location = new Point(0, 100);

                    int newHeight = this.Height - 100 - 60;
                    if (newHeight > 200)
                    {
                        splitContainer.Size = new Size(this.Width, newHeight);

                        try
                        {
                            splitContainer.SplitterDistance = Math.Max(100, Math.Min(newHeight - 100, newHeight * 2 / 3));
                        }
                        catch { }
                    }

                    ResizeCards();
                }
            };
        }

        private void ResizeCards()
        {
            if (flowTasks != null && flowTasks.Controls.Count > 0)
            {
                int newWidth = flowTasks.ClientSize.Width - 30;

                foreach (Control ctrl in flowTasks.Controls)
                {
                    if (ctrl is Panel card)
                    {
                        card.Width = newWidth;

                        int btnX = newWidth - 145;
                        foreach (Control btnCtrl in card.Controls)
                        {
                            if (btnCtrl is Button btn)
                            {
                                if (btn.Text == "✏️")
                                    btn.Location = new Point(btnX, 10);
                                else if (btn.Text == "▶️")
                                    btn.Location = new Point(btnX + 45, 10);
                                else if (btn.Text == "🗑️")
                                    btn.Location = new Point(btnX + 90, 10);
                            }
                        }
                    }
                }
            }
        }

        private void LoadDownloadTasks()
        {
            _downloadTasks = DownloaderManager.LoadTasks();
        }

        private void RefreshDownloadTasksList()
        {
            if (flowTasks == null)
                return;

            flowTasks.SuspendLayout();
            flowTasks.Controls.Clear();

            var orderedTasks = _downloadTasks
                .OrderBy(t => GetFirstTime(t.ScheduleTimes))
                .ThenBy(t => t.Name)
                .ToList();

            foreach (var task in orderedTasks)
            {
                Panel card = CreateTaskCard(task);
                flowTasks.Controls.Add(card);
            }

            flowTasks.ResumeLayout();

            lblStatus.Text = string.Format(LanguageManager.GetString("Download.TasksConfigured", "📊 {0} task configurati"), _downloadTasks.Count);
        }

        private TimeSpan GetFirstTime(List<string> times)
        {
            if (times == null || times.Count == 0)
                return TimeSpan.MaxValue;

            if (TimeSpan.TryParse(times[0], out TimeSpan result))
                return result;

            return TimeSpan.MaxValue;
        }

        private Panel CreateTaskCard(DownloadTask task)
        {
            int cardWidth = flowTasks.ClientSize.Width - 30;

            Panel card = new Panel
            {
                Width = cardWidth,
                Height = 50,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 5),
                Padding = new Padding(10, 5, 10, 5)
            };

            card.Paint += (s, e) =>
            {
                Color borderColor = task.IsHttpDownload ? Color.FromArgb(0, 120, 215) : Color.FromArgb(255, 140, 0);
                using (Pen pen = new Pen(borderColor, 2))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
                }
            };

            string firstTime = task.ScheduleTimes != null && task.ScheduleTimes.Count > 0
                ? task.ScheduleTimes[0]
                : "--:--:--";

            Label lblTime = new Label
            {
                Text = firstTime,
                Location = new Point(10, 12),
                Size = new Size(75, 25),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = task.IsHttpDownload ? Color.FromArgb(0, 120, 215) : Color.FromArgb(255, 140, 0),
                TextAlign = ContentAlignment.MiddleLeft
            };
            card.Controls.Add(lblTime);

            string icon = task.IsHttpDownload ? "🌐" : "📁";
            Label lblTitle = new Label
            {
                Text = $"{icon} {task.Name}",
                Location = new Point(95, 8),
                Size = new Size(cardWidth - 265, 18),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = AppTheme.TextPrimary,
                AutoEllipsis = true
            };
            card.Controls.Add(lblTitle);

            List<string> days = new List<string>();
            if (task.Monday) days.Add(LanguageManager.GetString("Download.DayMon", "Lun"));
            if (task.Tuesday) days.Add(LanguageManager.GetString("Download.DayTue", "Mar"));
            if (task.Wednesday) days.Add(LanguageManager.GetString("Download.DayWed", "Mer"));
            if (task.Thursday) days.Add(LanguageManager.GetString("Download.DayThu", "Gio"));
            if (task.Friday) days.Add(LanguageManager.GetString("Download.DayFri", "Ven"));
            if (task.Saturday) days.Add(LanguageManager.GetString("Download.DaySat", "Sab"));
            if (task.Sunday) days.Add(LanguageManager.GetString("Download.DaySun", "Dom"));

            string daysText = days.Count == 7 ?
                "📅 " + LanguageManager.GetString("Download.EveryDay", "Tutti i giorni") :
                $"📅 {string.Join(", ", days)}";

            string allTimes = task.ScheduleTimes != null && task.ScheduleTimes.Count > 0
                ? string.Join(", ", task.ScheduleTimes)
                : "--:--:--";
            string timesInfo = task.ScheduleTimes?.Count > 1 ? $" | ⏰ {allTimes}" : "";

            Label lblDays = new Label
            {
                Text = $"{daysText}{timesInfo}",
                Location = new Point(95, 28),
                Size = new Size(cardWidth - 265, 16),
                Font = new Font("Segoe UI", 8),
                ForeColor = AppTheme.TextSecondary,
                AutoEllipsis = true
            };
            card.Controls.Add(lblDays);

            int btnX = cardWidth - 145;

            Button btnEdit = new Button
            {
                Text = "✏️",
                Location = new Point(btnX, 10),
                Size = new Size(35, 30),
                BackColor = AppTheme.Warning,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11),
                Cursor = Cursors.Hand,
                Tag = task
            };
            btnEdit.FlatAppearance.BorderSize = 0;
            btnEdit.Click += BtnEditTask_Click;
            card.Controls.Add(btnEdit);

            Button btnExecute = new Button
            {
                Text = "▶️",
                Location = new Point(btnX + 45, 10),
                Size = new Size(35, 30),
                BackColor = AppTheme.Success,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11),
                Cursor = Cursors.Hand,
                Tag = task
            };
            btnExecute.FlatAppearance.BorderSize = 0;
            btnExecute.Click += (s, e) => ExecuteDownloadTask(task);
            card.Controls.Add(btnExecute);

            Button btnDelete = new Button
            {
                Text = "🗑️",
                Location = new Point(btnX + 90, 10),
                Size = new Size(35, 30),
                BackColor = AppTheme.Danger,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11),
                Cursor = Cursors.Hand,
                Tag = task
            };
            btnDelete.FlatAppearance.BorderSize = 0;
            btnDelete.Click += BtnRemoveTask_Click;
            card.Controls.Add(btnDelete);

            ToolTip tooltip = new ToolTip();
            tooltip.SetToolTip(btnEdit, LanguageManager.GetString("Download.Edit", "Modifica task"));
            tooltip.SetToolTip(btnExecute, LanguageManager.GetString("Download.RunNow", "Esegui ora"));
            tooltip.SetToolTip(btnDelete, LanguageManager.GetString("Download.Delete", "Elimina task"));

            return card;
        }

        private void UpdateNextScheduleInfo()
        {
            DateTime now = DateTime.Now;
            DateTime? nextSchedule = null;
            DownloadTask nextTask = null;

            foreach (var task in _downloadTasks)
            {
                if (task.ScheduleTimes == null || task.ScheduleTimes.Count == 0)
                    continue;

                foreach (var timeStr in task.ScheduleTimes)
                {
                    if (TimeSpan.TryParse(timeStr, out TimeSpan scheduleTime))
                    {
                        DateTime taskDateTime = now.Date.Add(scheduleTime);

                        if (taskDateTime <= now)
                        {
                            taskDateTime = taskDateTime.AddDays(1);
                        }

                        DayOfWeek dayOfWeek = taskDateTime.DayOfWeek;
                        bool dayValid = false;

                        switch (dayOfWeek)
                        {
                            case DayOfWeek.Monday: dayValid = task.Monday; break;
                            case DayOfWeek.Tuesday: dayValid = task.Tuesday; break;
                            case DayOfWeek.Wednesday: dayValid = task.Wednesday; break;
                            case DayOfWeek.Thursday: dayValid = task.Thursday; break;
                            case DayOfWeek.Friday: dayValid = task.Friday; break;
                            case DayOfWeek.Saturday: dayValid = task.Saturday; break;
                            case DayOfWeek.Sunday: dayValid = task.Sunday; break;
                        }

                        if (dayValid && (!nextSchedule.HasValue || taskDateTime < nextSchedule.Value))
                        {
                            nextSchedule = taskDateTime;
                            nextTask = task;
                        }
                    }
                }
            }

            if (nextSchedule.HasValue && nextTask != null)
            {
                lblNextTaskTime.Text = nextSchedule.Value.ToString("HH:mm:ss");
                lblNextTaskName.Text = nextTask.Name;
                lblNextTaskTime.ForeColor = Color.FromArgb(0, 255, 127);
            }
            else
            {
                lblNextTaskTime.Text = "--:--:--";
                lblNextTaskName.Text = LanguageManager.GetString("Download.NoTask", "Nessun task");
                lblCountdown.Text = "--:--:--";
                lblNextTaskTime.ForeColor = Color.Gray;
            }
        }

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            DateTime? nextSchedule = null;

            foreach (var task in _downloadTasks)
            {
                if (task.ScheduleTimes == null || task.ScheduleTimes.Count == 0)
                    continue;

                foreach (var timeStr in task.ScheduleTimes)
                {
                    if (TimeSpan.TryParse(timeStr, out TimeSpan scheduleTime))
                    {
                        DateTime taskDateTime = now.Date.Add(scheduleTime);

                        if (taskDateTime <= now)
                        {
                            taskDateTime = taskDateTime.AddDays(1);
                        }

                        DayOfWeek dayOfWeek = taskDateTime.DayOfWeek;
                        bool dayValid = false;

                        switch (dayOfWeek)
                        {
                            case DayOfWeek.Monday: dayValid = task.Monday; break;
                            case DayOfWeek.Tuesday: dayValid = task.Tuesday; break;
                            case DayOfWeek.Wednesday: dayValid = task.Wednesday; break;
                            case DayOfWeek.Thursday: dayValid = task.Thursday; break;
                            case DayOfWeek.Friday: dayValid = task.Friday; break;
                            case DayOfWeek.Saturday: dayValid = task.Saturday; break;
                            case DayOfWeek.Sunday: dayValid = task.Sunday; break;
                        }

                        if (dayValid && (!nextSchedule.HasValue || taskDateTime < nextSchedule.Value))
                        {
                            nextSchedule = taskDateTime;
                        }
                    }
                }
            }

            if (nextSchedule.HasValue)
            {
                TimeSpan remaining = nextSchedule.Value - now;

                if (remaining.TotalSeconds > 0)
                {
                    lblCountdown.Text = $"{remaining.Hours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
                    lblCountdown.ForeColor = Color.Orange;
                }
                else
                {
                    lblCountdown.Text = LanguageManager.GetString("Download.Running", "ESECUZIONE");
                    lblCountdown.ForeColor = Color.Lime;
                }
            }
            else
            {
                lblCountdown.Text = "--:--:--";
                lblCountdown.ForeColor = Color.Gray;
            }
        }

        private void SchedulerTimer_Tick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            string currentTimeStr = now.ToString("HH:mm:ss");
            DayOfWeek currentDay = now.DayOfWeek;

            foreach (var task in _downloadTasks)
            {
                if (task.ScheduleTimes == null || task.ScheduleTimes.Count == 0)
                    continue;

                bool dayMatch = false;
                switch (currentDay)
                {
                    case DayOfWeek.Monday: dayMatch = task.Monday; break;
                    case DayOfWeek.Tuesday: dayMatch = task.Tuesday; break;
                    case DayOfWeek.Wednesday: dayMatch = task.Wednesday; break;
                    case DayOfWeek.Thursday: dayMatch = task.Thursday; break;
                    case DayOfWeek.Friday: dayMatch = task.Friday; break;
                    case DayOfWeek.Saturday: dayMatch = task.Saturday; break;
                    case DayOfWeek.Sunday: dayMatch = task.Sunday; break;
                }

                if (dayMatch && task.ScheduleTimes.Contains(currentTimeStr))
                {
                    string taskKey = task.Name + "|" + currentTimeStr;
                    if (_lastExecuted.TryGetValue(taskKey, out DateTime lastRun) && (now - lastRun).TotalSeconds < 60)
                        continue;

                    _lastExecuted[taskKey] = now;
                    ExecuteDownloadTask(task);
                }
            }
        }

        private void ShowDateVariablesHelp(object sender, EventArgs e)
        {
            DateTime today = DateTime.Now;
            DateTime tomorrow = today.AddDays(1);
            DateTime dayAfterTomorrow = today.AddDays(2);

            Form helpForm = new Form
            {
                Text = LanguageManager.GetString("Download.DateVariables", "Variabili di Data"),
                Size = new Size(700, 600),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.FromArgb(30, 30, 30),
                AutoScroll = true
            };

            Panel contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(20)
            };
            helpForm.Controls.Add(contentPanel);

            int yPos = 10;

            Label lblSection1 = new Label
            {
                Text = LanguageManager.GetString("Download.DateVariablesTitle", "VARIABILI DI DATA PER IL DOWNLOAD: "),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(10, yPos),
                AutoSize = true
            };
            contentPanel.Controls.Add(lblSection1);
            yPos += 40;

            var variables = new List<(string variable, string result)>
            {
                ("{TODAY}", today.ToString("yyyy-MM-dd")),
                ("{TOMORROW}", tomorrow.ToString("yyyy-MM-dd")),
                ("{YESTERDAY}", today.AddDays(-1).ToString("yyyy-MM-dd")),
                ("{TODAY+2}", dayAfterTomorrow.ToString("yyyy-MM-dd")),
                ("{TODAY-1}", today.AddDays(-1).ToString("yyyy-MM-dd"))
            };

            foreach (var item in variables)
            {
                AddCopyableRow(contentPanel, item.variable, item.result, ref yPos);
            }

            yPos += 20;

            Label lblSection2 = new Label
            {
                Text = LanguageManager.GetString("Download.CustomFormats", "FORMATI PERSONALIZZATI:"),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(10, yPos),
                AutoSize = true
            };
            contentPanel.Controls.Add(lblSection2);
            yPos += 40;

            var formats = new List<(string variable, string result)>
            {
                ("{TODAY_YYYYMMDD}", today.ToString("yyyyMMdd")),
                ("{TODAY_DDMMYYYY}", today.ToString("ddMMyyyy")),
                ("{TODAY_DD_MM_YYYY}", today.ToString("dd-MM-yyyy"))
            };

            foreach (var item in formats)
            {
                AddCopyableRow(contentPanel, item.variable, item.result, ref yPos);
            }

            yPos += 20;

            Label lblSection3 = new Label
            {
                Text = LanguageManager.GetString("Download.ExampleUrl", "ESEMPIO URL:"),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(10, yPos),
                AutoSize = true
            };
            contentPanel.Controls.Add(lblSection3);
            yPos += 40;

            AddCopyableRow(contentPanel, "http://example.com/file_{TODAY}.mp3",
                $"http://example.com/file_{today:yyyy-MM-dd}.mp3", ref yPos);

            Panel footerPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(40, 40, 40),
                Padding = new Padding(10)
            };
            helpForm.Controls.Add(footerPanel);

            Button btnClose = new Button
            {
                Text = "✖ " + LanguageManager.GetString("Common.Close", "Chiudi"),
                Size = new Size(130, 40),
                Location = new Point(10, 10),
                BackColor = Color.FromArgb(80, 80, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, ev) => helpForm.Close();
            footerPanel.Controls.Add(btnClose);

            footerPanel.Resize += (s, ev) =>
            {
                btnClose.Location = new Point(footerPanel.Width - 140, 10);
            };

            helpForm.ShowDialog();
        }

        private void AddCopyableRow(Panel parent, string text, string result, ref int yPos)
        {
            Label lblText = new Label
            {
                Text = $"{text} → {result}",
                Font = new Font("Consolas", 10),
                ForeColor = Color.LightGray,
                Location = new Point(10, yPos + 5),
                Size = new Size(550, 25),
                AutoEllipsis = true
            };
            parent.Controls.Add(lblText);

            Button btnCopy = new Button
            {
                Text = "📋",
                Size = new Size(35, 28),
                Location = new Point(570, yPos),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10),
                Cursor = Cursors.Hand,
                Tag = text
            };
            btnCopy.FlatAppearance.BorderSize = 0;
            btnCopy.Click += (s, e) =>
            {
                try
                {
                    string textToCopy = ((Button)s).Tag.ToString();
                    Clipboard.SetText(textToCopy);

                    Button btn = (Button)s;
                    btn.Text = "✓";
                    btn.BackColor = Color.FromArgb(0, 180, 0);

                    System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
                    timer.Interval = 1000;
                    timer.Tick += (ts, te) =>
                    {
                        btn.Text = "📋";
                        btn.BackColor = Color.FromArgb(0, 120, 215);
                        timer.Stop();
                        timer.Dispose();
                    };
                    timer.Start();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        string.Format(LanguageManager.GetString("Download.CopyError", "Errore copia:  {0}"), ex.Message),
                        LanguageManager.GetString("Common.Error", "Errore"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            };
            parent.Controls.Add(btnCopy);

            yPos += 35;
        }

        private void BtnToggleLog_Click(object sender, EventArgs e)
        {
            _logVisible = !_logVisible;

            if (_logVisible)
            {
                splitContainer.Panel2Collapsed = false;
                btnToggleLog.Text = "▼ " + LanguageManager.GetString("Download.HideLog", "Nascondi Log");
            }
            else
            {
                splitContainer.Panel2Collapsed = true;
                btnToggleLog.Text = "▲ " + LanguageManager.GetString("Download.ShowLog", "Mostra Log");
            }
        }

        private void BtnAddTask_Click(object sender, EventArgs e)
        {
            using (var taskEditor = new TaskEditorForm(null))
            {
                if (taskEditor.ShowDialog() == DialogResult.OK)
                {
                    _downloadTasks.Add(taskEditor.Task);
                    DownloaderManager.SaveTasks(_downloadTasks);
                    RefreshDownloadTasksList();
                    UpdateNextScheduleInfo();
                }
            }
        }

        private void BtnEditTask_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            DownloadTask task = btn.Tag as DownloadTask;

            using (var taskEditor = new TaskEditorForm(task))
            {
                if (taskEditor.ShowDialog() == DialogResult.OK)
                {
                    int index = _downloadTasks.FindIndex(t => t.Name == task.Name);
                    if (index >= 0)
                    {
                        _downloadTasks[index] = taskEditor.Task;
                        DownloaderManager.SaveTasks(_downloadTasks);
                        RefreshDownloadTasksList();
                        UpdateNextScheduleInfo();
                    }
                }
            }
        }

        private void BtnRemoveTask_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            DownloadTask task = btn.Tag as DownloadTask;

            if (MessageBox.Show(
                string.Format(LanguageManager.GetString("Download.ConfirmDelete", "🗑️ Eliminare il task '{0}'?\n\nQuesta operazione non può essere annullata."), task.Name),
                LanguageManager.GetString("Common.Confirm", "Conferma Eliminazione"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _downloadTasks.Remove(task);
                DownloaderManager.SaveTasks(_downloadTasks);
                RefreshDownloadTasksList();
                UpdateNextScheduleInfo();
            }
        }

        private void BtnClearLog_Click(object sender, EventArgs e)
        {
            txtLog.Clear();
        }

        private async void ExecuteDownloadTask(DownloadTask task)
        {
            if (_isProcessing)
            {
                LogMessage(string.Format(LanguageManager.GetString("Download.TaskWaiting", "Un'altra operazione è in corso.Task {0} in attesa."), task.Name));
                return;
            }

            _isProcessing = true;
            progressMain.Visible = true;
            lblStatus.Text = $"⏳ {task.Name}";

            try
            {
                LogMessage(string.Format(LanguageManager.GetString("Download.StartingDownload", "Avvio download per il task:  {0}"), task.Name));
                progressMain.Value = 10;

                bool downloadSuccess = false;
                Exception lastException = null;

                try
                {
                    if (task.IsHttpDownload)
                    {
                        await DownloadHttpFileAsync(task);
                    }
                    else
                    {
                        await DownloadFtpFileAsync(task);
                    }
                    downloadSuccess = true;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    LogMessage(string.Format(LanguageManager.GetString("Download.FirstAttemptFailed", "Primo tentativo fallito: {0}"), ex.Message));

                    for (int attempt = 1; attempt <= 3; attempt++)
                    {
                        try
                        {
                            LogMessage(string.Format(LanguageManager.GetString("Download.WaitingRetry", "Attesa 20 secondi prima del tentativo {0}..."), attempt));
                            await Task.Delay(20000);

                            LogMessage(string.Format(LanguageManager.GetString("Download.Attempt", "Tentativo {0} di download..."), attempt));
                            if (task.IsHttpDownload)
                            {
                                await DownloadHttpFileAsync(task);
                            }
                            else
                            {
                                await DownloadFtpFileAsync(task);
                            }

                            downloadSuccess = true;
                            LogMessage(string.Format(LanguageManager.GetString("Download.SuccessAtAttempt", "Download riuscito al tentativo {0}"), attempt));
                            break;
                        }
                        catch (Exception retryEx)
                        {
                            lastException = retryEx;
                            LogMessage(string.Format(LanguageManager.GetString("Download.AttemptFailed", "Tentativo {0} fallito: {1}"), attempt, retryEx.Message));
                        }
                    }
                }

                if (!downloadSuccess)
                {
                    throw lastException;
                }

                progressMain.Value = 45;

                string processedLocalPath = ProcessDateVariables(task.LocalFilePath);
                if (Path.GetExtension(processedLocalPath).ToLower() == ".mp3")
                {
                    LogMessage(LanguageManager.GetString("Download.ConvertingMp3", "Conversione MP3 a 320kbps..."));
                    await ConvertMp3ToBitrateAsync(processedLocalPath);
                }

                progressMain.Value = 50;

                if (task.CompositionEnabled)
                {
                    await ComposeAudioFilesAsync(task);
                }

                progressMain.Value = 100;
                LogMessage(string.Format(LanguageManager.GetString("Download.CompletedSuccess", "✅ Download completato con successo:  {0}"), task.Name));
            }
            catch (Exception ex)
            {
                LogMessage(string.Format(LanguageManager.GetString("Download.FinalError", "❌ Errore definitivo per task {0}: {1}"), task.Name, ex.Message));
            }
            finally
            {
                _isProcessing = false;
                lblStatus.Text = "";

                await Task.Delay(1000);
                progressMain.Value = 0;
                progressMain.Visible = false;
                UpdateNextScheduleInfo();
            }
        }

        private string ProcessDateVariables(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            DateTime localNow = DateTime.Now;

            input = input.Replace("{TODAY}", localNow.ToString("yyyy-MM-dd"));
            input = input.Replace("{TOMORROW}", localNow.AddDays(1).ToString("yyyy-MM-dd"));
            input = input.Replace("{YESTERDAY}", localNow.AddDays(-1).ToString("yyyy-MM-dd"));
            input = input.Replace("{TODAY_YYYYMMDD}", localNow.ToString("yyyyMMdd"));
            input = input.Replace("{TODAY_DDMMYYYY}", localNow.ToString("ddMMyyyy"));
            input = input.Replace("{TODAY_DD_MM_YYYY}", localNow.ToString("dd-MM-yyyy"));

            while (input.Contains("{TODAY+") || input.Contains("{TODAY-"))
            {
                int startIdx = input.IndexOf("{TODAY+");
                if (startIdx >= 0)
                {
                    int endIdx = input.IndexOf("}", startIdx);
                    if (endIdx > startIdx)
                    {
                        string expression = input.Substring(startIdx, endIdx - startIdx + 1);
                        string daysStr = expression.Substring(7, expression.Length - 8);

                        if (int.TryParse(daysStr, out int days))
                        {
                            string dateStr = localNow.AddDays(days).ToString("yyyy-MM-dd");
                            input = input.Replace(expression, dateStr);
                        }
                    }
                }

                startIdx = input.IndexOf("{TODAY-");
                if (startIdx >= 0)
                {
                    int endIdx = input.IndexOf("}", startIdx);
                    if (endIdx > startIdx)
                    {
                        string expression = input.Substring(startIdx, endIdx - startIdx + 1);
                        string daysStr = expression.Substring(7, expression.Length - 8);

                        if (int.TryParse(daysStr, out int days))
                        {
                            string dateStr = localNow.AddDays(-days).ToString("yyyy-MM-dd");
                            input = input.Replace(expression, dateStr);
                        }
                    }
                }

                if (!input.Contains("{TODAY+") && !input.Contains("{TODAY-"))
                    break;
            }

            return input;
        }

        private async Task DownloadHttpFileAsync(DownloadTask task)
        {
            string processedUrl = ProcessDateVariables(task.HttpUrl);
            string processedLocalPath = ProcessDateVariables(task.LocalFilePath);

            LogMessage(string.Format(LanguageManager.GetString("Download.HttpDownloading", "Download HTTP: {0}"), processedUrl));

            using (WebClient client = new WebClient())
            {
                if (!string.IsNullOrEmpty(task.HttpUsername) && !string.IsNullOrEmpty(task.HttpPassword))
                {
                    client.Credentials = new NetworkCredential(task.HttpUsername, task.HttpPassword);
                }

                string directory = Path.GetDirectoryName(processedLocalPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                client.DownloadProgressChanged += (s, e) =>
                {
                    progressMain.Value = 10 + (e.ProgressPercentage * 30) / 100;
                };

                await client.DownloadFileTaskAsync(new Uri(processedUrl), processedLocalPath);
                LogMessage(string.Format(LanguageManager.GetString("Download.FileDownloaded", "✅ File scaricato:  {0}"), processedLocalPath));
            }
        }

        private async Task DownloadFtpFileAsync(DownloadTask task)
        {
            string processedHost = ProcessDateVariables(task.FtpHost);
            string processedFilePath = ProcessDateVariables(task.FtpFilePath);
            string processedLocalPath = ProcessDateVariables(task.LocalFilePath);

            LogMessage(string.Format(LanguageManager.GetString("Download.FtpDownloading", "Download FTP: {0}{1}"), processedHost, processedFilePath));

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(new Uri($"ftp://{processedHost}{processedFilePath}"));
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            request.Credentials = new NetworkCredential(task.FtpUsername, task.FtpPassword);

            string directory = Path.GetDirectoryName(processedLocalPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (FtpWebResponse response = (FtpWebResponse)await request.GetResponseAsync())
            using (Stream responseStream = response.GetResponseStream())
            using (FileStream fileStream = new FileStream(processedLocalPath, FileMode.Create))
            {
                byte[] buffer = new byte[8192];
                int bytesRead;

                while ((bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                }

                LogMessage(string.Format(LanguageManager.GetString("Download.FtpDownloaded", "✅ File FTP scaricato: {0}"), processedLocalPath));
            }
        }

        // ---------------------------------------------------------------
        // HELPER: verifica se un file è bloccato da un altro processo
        // ---------------------------------------------------------------
        private static bool IsFileLocked(string filePath)
        {
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    return false;
                }
            }
            catch (IOException)
            {
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return true;
            }
        }

        // ---------------------------------------------------------------
        // HELPER: riconosce eccezioni di tipo "file in uso" (0x80070020)
        // ---------------------------------------------------------------
        private static bool IsFileInUseException(Exception ex)
        {
            const int HR_SHARING_VIOLATION = unchecked((int)0x80070020);

            if (ex is IOException ioEx && (ioEx.HResult & 0xFFFF) == 0x0020)
                return true;

            if (ex is System.IO.FileLoadException fileLoadEx && fileLoadEx.HResult == HR_SHARING_VIOLATION)
                return true;

            if (ex.InnerException != null)
                return IsFileInUseException(ex.InnerException);

            return false;
        }

        // ---------------------------------------------------------------
        // Conversione MP3 — con gestione file in uso
        // ---------------------------------------------------------------
        private async Task ConvertMp3ToBitrateAsync(string filePath)
        {
            await Task.Run(() =>
            {
                try
                {
                    // Verifica preventiva: il file è in uso?
                    if (File.Exists(filePath) && IsFileLocked(filePath))
                    {
                        LogMessage(string.Format(
                            LanguageManager.GetString("Download.FileInUseSkip",
                                "⚠️ File in uso da un altro processo, conversione MP3 saltata: {0}"),
                            filePath));
                        return;
                    }

                    string tempFilePath = Path.Combine(
                        Path.GetDirectoryName(filePath),
                        Path.GetFileNameWithoutExtension(filePath) + "_temp.mp3"
                    );

                    using (var reader = new AudioFileReader(filePath))
                    {
                        MediaFoundationEncoder.EncodeToMp3(reader, tempFilePath, 320000);
                    }

                    File.Delete(filePath);
                    File.Move(tempFilePath, filePath);

                    LogMessage(LanguageManager.GetString("Download.Mp3Converted", "✅ Conversione MP3 a 320kbps completata"));
                }
                catch (Exception ex) when (IsFileInUseException(ex))
                {
                    // File in uso durante la codifica: interrompe silenziosamente
                    LogMessage(string.Format(
                        LanguageManager.GetString("Download.FileInUseSkip",
                            "⚠️ File in uso da un altro processo, conversione MP3 saltata: {0}"),
                        filePath));
                }
                catch (Exception ex)
                {
                    LogMessage(string.Format(LanguageManager.GetString("Download.Mp3ConversionError", "❌ Errore conversione MP3: {0}"), ex.Message));
                    throw;
                }
            });
        }

        // ---------------------------------------------------------------
        // Composizione audio — con gestione file in uso
        // ---------------------------------------------------------------
        private async Task ComposeAudioFilesAsync(DownloadTask task)
        {
            LogMessage(LanguageManager.GetString("Download.StartingComposition", "Avvio composizione audio..."));

            try
            {
                string processedLocalPath = ProcessDateVariables(task.LocalFilePath);
                string outputFilePath = ProcessDateVariables(task.OutputFilePath);

                if (string.IsNullOrEmpty(outputFilePath))
                {
                    outputFilePath = Path.Combine(
                        Path.GetDirectoryName(processedLocalPath),
                        Path.GetFileNameWithoutExtension(processedLocalPath) + "_composed" + Path.GetExtension(processedLocalPath)
                    );
                }

                progressMain.Value = 50;

                await Task.Run(() =>
                {
                    // Verifica preventiva: il file di output è in uso?
                    if (File.Exists(outputFilePath) && IsFileLocked(outputFilePath))
                    {
                        LogMessage(string.Format(
                            LanguageManager.GetString("Download.FileInUseSkipCompose",
                                "⚠️ File di output in uso da un altro processo, composizione saltata: {0}"),
                            outputFilePath));
                        return;
                    }

                    string tempWavFile = Path.Combine(
                        Path.GetDirectoryName(outputFilePath),
                        Path.GetFileNameWithoutExtension(outputFilePath) + "_temp.wav"
                    );

                    int standardSampleRate = 44100;
                    int standardChannels = 2;
                    WaveFormat targetFormat = WaveFormat.CreateIeeeFloatWaveFormat(standardSampleRate, standardChannels);

                    var concatenatedParts = new List<ISampleProvider>();

                    string processedOpenerPath = ProcessDateVariables(task.OpenerFilePath);
                    if (task.UseOpener && File.Exists(processedOpenerPath))
                    {
                        using (var opener = new AudioFileReader(processedOpenerPath))
                        {
                            var openerResampled = new MediaFoundationResampler(opener, targetFormat);
                            concatenatedParts.Add(openerResampled.ToSampleProvider());
                        }
                    }

                    ISampleProvider mainAndBackgroundMix;
                    using (var mainReader = new AudioFileReader(processedLocalPath))
                    {
                        var mainResampled = new MediaFoundationResampler(mainReader, targetFormat);
                        var mainSampleProvider = mainResampled.ToSampleProvider();

                        string processedBackgroundPath = ProcessDateVariables(task.BackgroundFilePath);
                        if (task.UseBackground && File.Exists(processedBackgroundPath))
                        {
                            using (var backgroundReader = new AudioFileReader(processedBackgroundPath))
                            {
                                backgroundReader.Volume = task.BackgroundVolume / 100.0f;

                                var backgroundResampled = new MediaFoundationResampler(backgroundReader, targetFormat);
                                var backgroundSampleProvider = backgroundResampled.ToSampleProvider();

                                var offsetProvider = new OffsetSampleProvider(backgroundSampleProvider)
                                {
                                    Take = mainReader.TotalTime
                                };

                                var mixer = new MixingSampleProvider(new[] { mainSampleProvider, offsetProvider });
                                mainAndBackgroundMix = mixer;
                            }
                        }
                        else
                        {
                            mainAndBackgroundMix = mainSampleProvider;
                        }

                        concatenatedParts.Add(mainAndBackgroundMix);
                    }

                    string processedCloserPath = ProcessDateVariables(task.CloserFilePath);
                    if (task.UseCloser && File.Exists(processedCloserPath))
                    {
                        using (var closer = new AudioFileReader(processedCloserPath))
                        {
                            var closerResampled = new MediaFoundationResampler(closer, targetFormat);
                            concatenatedParts.Add(closerResampled.ToSampleProvider());
                        }
                    }

                    var concatenatedProvider = new ConcatenatingSampleProvider(concatenatedParts);

                    ISampleProvider finalProvider = concatenatedProvider;

                    if (task.BoostVolume)
                    {
                        float volumeFactor = 1.059f;
                        var volumeProvider = new VolumeSampleProvider(concatenatedProvider);
                        volumeProvider.Volume = volumeFactor;
                        finalProvider = volumeProvider;
                    }

                    WaveFileWriter.CreateWaveFile16(tempWavFile, finalProvider);

                    try
                    {
                        using (var reader = new AudioFileReader(tempWavFile))
                        {
                            if (Path.GetExtension(outputFilePath).ToLower() == ".mp3")
                            {
                                MediaFoundationEncoder.EncodeToMp3(reader, outputFilePath, 320000);
                            }
                            else
                            {
                                MediaFoundationEncoder.EncodeToWma(reader, outputFilePath);
                            }
                        }
                    }
                    catch (Exception ex) when (IsFileInUseException(ex))
                    {
                        // File di output in uso durante la codifica: interrompe silenziosamente
                        LogMessage(string.Format(
                            LanguageManager.GetString("Download.FileInUseSkipCompose",
                                "⚠️ File di output in uso da un altro processo, composizione saltata: {0}"),
                            outputFilePath));

                        try { File.Delete(tempWavFile); } catch { }
                        return;
                    }

                    try
                    {
                        File.Delete(tempWavFile);
                    }
                    catch { }

                    LogMessage(string.Format(LanguageManager.GetString("Download.ComposedSaved", "✅ File composto salvato: {0}"), outputFilePath));
                });
            }
            catch (Exception ex)
            {
                LogMessage(string.Format(LanguageManager.GetString("Download.CompositionError", "❌ Errore composizione audio: {0}"), ex.Message));
                throw;
            }
        }

        private void LogMessage(string message)
        {
            _dailyLogger?.Log($"[Download] {message}");

            try
            {
                string logEntry = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";

                if (txtLog.InvokeRequired)
                {
                    txtLog.Invoke(new Action(() =>
                    {
                        txtLog.AppendText(logEntry + Environment.NewLine);
                        txtLog.SelectionStart = txtLog.Text.Length;
                        txtLog.ScrollToCaret();
                    }));
                }
                else
                {
                    txtLog.AppendText(logEntry + Environment.NewLine);
                    txtLog.SelectionStart = txtLog.Text.Length;
                    txtLog.ScrollToCaret();
                }
            }
            catch { }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                LanguageManager.SaveMissingKeysToFile();
                LanguageManager.LanguageChanged -= OnLanguageChanged;

                _schedulerTimer?.Stop();
                _schedulerTimer?.Dispose();
                _countdownTimer?.Stop();
                _countdownTimer?.Dispose();
                try { _dailyLogger?.Dispose(); } catch { }
            }
            base.Dispose(disposing);
        }
    }
}
