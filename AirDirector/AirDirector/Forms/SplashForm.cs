using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using AirDirector.Themes;
using AirDirector.Services.Localization;

namespace AirDirector.Forms
{
    public partial class SplashForm : Form
    {
        private Label[] _statusLabels;

        private readonly (string Icon, string Name)[] _loadingModules =
        {
            ("🎵", "Music"),
            ("🎬", "Clips"),
            ("📅", "Schedules"),
            ("📺", "ADV"),
            ("⏰", "Clocks"),
            ("📡", "Encoders"),
            ("⏺️", "Recorders"),
            ("📊", "Reports")
        };

        public SplashForm()
        {
            InitializeComponent();
            InitializeCustomComponents();
        }

        private void InitializeCustomComponents()
        {
            // ── Form ──────────────────────────────────────────────
            this.BackColor = Color.FromArgb(25, 25, 35);
            this.Region = System.Drawing.Region.FromHrgn(
                CreateRoundRectRgn(0, 0, Width, Height, 15, 15));

            // ── Header panel ──────────────────────────────────────
            headerPanel.BackColor = Color.FromArgb(35, 35, 50);

            lblLogo.Font = new Font("Segoe UI", 36, FontStyle.Bold);
            lblLogo.ForeColor = Color.White;
            lblLogo.Text = "🎵 AirDirector";

            lblVersion.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            lblVersion.ForeColor = Color.FromArgb(150, 180, 255);
            lblVersion.Text =
                $"{LanguageManager.GetString("Splash.Version", "Professional Playout System")} {AppVersion.Current}";

            // ── Logo: image or text ───────────────────────────────
            LoadLogo();

            // ── Status panel ──────────────────────────────────────
            statusPanel.BackColor = Color.FromArgb(30, 30, 40);

            _statusLabels = new Label[_loadingModules.Length];
            for (int i = 0; i < _loadingModules.Length; i++)
            {
                int row = i / 4;
                int col = i % 4;

                _statusLabels[i] = new Label
                {
                    Text = $"{_loadingModules[i].Icon} {_loadingModules[i].Name}",
                    Font = new Font("Segoe UI", 9, FontStyle.Regular),
                    ForeColor = Color.Gray,
                    AutoSize = false,
                    Size = new Size(155, 30),
                    Location = new Point(15 + col * 163, 20 + row * 40),
                    TextAlign = ContentAlignment.MiddleLeft
                };
                statusPanel.Controls.Add(_statusLabels[i]);
            }

            // ── Progress bar ──────────────────────────────────────
            progressBar.BackColor = Color.FromArgb(50, 50, 65);
            progressBar.ForeColor = AppTheme.LEDGreen;

            // ── Percentage label ──────────────────────────────────
            lblPercentage.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            lblPercentage.ForeColor = Color.White;
            lblPercentage.Text = "0%";

            // ── Copyright ─────────────────────────────────────────
            lblCopyright.Font = new Font("Segoe UI", 8, FontStyle.Regular);
            lblCopyright.ForeColor = Color.FromArgb(100, 180, 180, 180);
            lblCopyright.Text = LanguageManager.GetString(
                "Splash.Copyright", "© 2025 AirDirector - All Rights Reserved");
        }

        private void LoadLogo()
        {
            string logoPath = Path.Combine(Application.StartupPath, "Assets", "logo.png");

            if (File.Exists(logoPath))
            {
                try
                {
                    pictureBoxLogo.Image = Image.FromFile(logoPath);
                    pictureBoxLogo.Visible = true;
                    lblLogo.Visible = false;
                    return;
                }
                catch
                {
                    // Fall through to text fallback
                }
            }

            pictureBoxLogo.Visible = false;
            lblLogo.Visible = true;
        }

        private void SplashForm_Load(object sender, EventArgs e)
        {
            // Fade-in animation
            this.Opacity = 0;
            System.Windows.Forms.Timer fadeTimer = new System.Windows.Forms.Timer { Interval = 25 };
            fadeTimer.Tick += (s, ev) =>
            {
                if (this.Opacity < 1.0)
                    this.Opacity += 0.1;
                else
                    fadeTimer.Stop();
            };
            fadeTimer.Start();

            StartLoadingProcess();
        }

        private void StartLoadingProcess()
        {
            System.Windows.Forms.Timer loadingTimer = new System.Windows.Forms.Timer { Interval = 450 };
            int stepIndex = 0;

            loadingTimer.Tick += (s, e) =>
            {
                if (stepIndex < _loadingModules.Length)
                {
                    _statusLabels[stepIndex].ForeColor = AppTheme.LEDGreen;
                    _statusLabels[stepIndex].Font = new Font("Segoe UI", 9, FontStyle.Bold);

                    int progress = (int)((stepIndex + 1) / (float)_loadingModules.Length * 100);
                    progressBar.Value = progress;
                    lblPercentage.Text = progress + "%";

                    stepIndex++;
                }
                else
                {
                    loadingTimer.Stop();
                    // Brief pause before closing — use a timer to avoid blocking the UI thread
                    System.Windows.Forms.Timer closeTimer = new System.Windows.Forms.Timer { Interval = 300 };
                    closeTimer.Tick += (cs, ce) => { closeTimer.Stop(); this.Close(); };
                    closeTimer.Start();
                }
            };
            loadingTimer.Start();
        }

        [System.Runtime.InteropServices.DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
            int nWidthEllipse, int nHeightEllipse);
    }
}