using System;
using System.Drawing;
using System.Windows.Forms;
using AirDirector.Services.Database;
using AirDirector.Services.Localization;
using AirDirector.Services.Licensing;
using AirDirector.Themes;

namespace AirDirector.Controls
{
    public partial class LogControl : UserControl
    {
        private TextBox txtLog;

        public LogControl()
        {
            InitializeComponent();
            InitializeUI();
            InitializeLog();
        }

        private void InitializeUI()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = AppTheme.BgLight;

            txtLog = new TextBox
            {
                Name = "txtLog",
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9),
                ReadOnly = true,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.LimeGreen,
                WordWrap = false
            };
            this.Controls.Add(txtLog);

            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = AppTheme.Surface
            };
            this.Controls.Add(headerPanel);

            Label lblTitle = new Label
            {
                Text = "📝 " + LanguageManager.GetString("LogControl.Title", "SYSTEM LOG"),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = AppTheme.TextPrimary,
                Location = new Point(10, 12),
                AutoSize = true
            };
            headerPanel.Controls.Add(lblTitle);

            Button btnClear = new Button
            {
                Text = "🗑️ " + LanguageManager.GetString("LogControl.ClearButton", "Pulisci Log"),
                Location = new Point(headerPanel.Width - 130, 10),
                Size = new Size(120, 30),
                BackColor = AppTheme.Danger,
                ForeColor = AppTheme.TextInverse,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            btnClear.FlatAppearance.BorderSize = 0;
            btnClear.Click += (s, e) =>
            {
                txtLog.Clear();
                AddLog(LanguageManager.GetString("LogControl.LogCleared", "Log pulito dall'utente"));
            };
            headerPanel.Controls.Add(btnClear);
        }

        private void InitializeLog()
        {
            AddLog(LanguageManager.GetString("LogControl.Started", "AirDirector avviato"));
            AddLog($"Database: {DbcManager.GetDatabasePath()}");
            AddLog($"Lingua: {LanguageManager.GetCurrentLanguage()}");
            AddLog(string.Format(LanguageManager.GetString("LogControl.License", "Licenza: {0}"), LicenseManager.IsDemoMode() ? "DEMO" : LanguageManager.GetString("LogControl.LicenseActive", "ATTIVA")));
            AddLog(LanguageManager.GetString("LogControl.FileWatcherActive", "FileWatcher: attivo"));
            AddLog(LanguageManager.GetString("LogControl.PlaylistQueueInit", "Playlist Queue: inizializzata"));
            AddLog(LanguageManager.GetString("LogControl.AllSystemsReady", "Tutti i sistemi operativi"));
        }

        public void AddLog(string message)
        {
            if (txtLog != null && !txtLog.IsDisposed)
            {
                if (txtLog.InvokeRequired)
                {
                    txtLog.Invoke(new Action(() =>
                    {
                        txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
                        txtLog.SelectionStart = txtLog.Text.Length;
                        txtLog.ScrollToCaret();
                    }));
                }
                else
                {
                    txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
                    txtLog.SelectionStart = txtLog.Text.Length;
                    txtLog.ScrollToCaret();
                }
            }
        }

        public void AddError(string message)
        {
            AddLog($"❌ ERROR: {message}");
        }

        public void AddWarning(string message)
        {
            AddLog($"⚠️ WARNING: {message}");
        }

        public void AddSuccess(string message)
        {
            AddLog($"✅ SUCCESS: {message}");
        }
    }
}