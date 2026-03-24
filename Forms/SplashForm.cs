using System;
using System.Drawing;
using System.Windows.Forms;
using AirDirector.Themes;

namespace AirDirector.Forms
{
    public partial class SplashForm : Form
    {
        public SplashForm()
        {
            InitializeComponent();
            InitializeCustomComponents();
        }

        private void InitializeCustomComponents()
        {
            // Form settings
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(600, 400);
            this.BackColor = AppTheme.Primary;

            // Logo Label
            Label lblLogo = new Label
            {
                Text = "🎵 AirDirector",
                Font = new Font("Segoe UI", 48, FontStyle.Bold),
                ForeColor = AppTheme.TextInverse,
                AutoSize = false,
                Size = new Size(550, 80),
                Location = new Point(25, 120),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblLogo);

            // Version Label
            Label lblVersion = new Label
            {
                Text = "Versione 1.0.0",
                Font = new Font("Segoe UI", 12, FontStyle.Regular),
                ForeColor = AppTheme.TextInverse,
                AutoSize = false,
                Size = new Size(550, 30),
                Location = new Point(25, 210),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblVersion);

            // Loading Label
            Label lblLoading = new Label
            {
                Text = "Caricamento...",
                Font = new Font("Segoe UI", 11, FontStyle.Italic),
                ForeColor = AppTheme.LEDGreen,
                AutoSize = false,
                Size = new Size(550, 30),
                Location = new Point(25, 300),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblLoading);

            // Copyright Label
            Label lblCopyright = new Label
            {
                Text = "© 2025 AirDirector - Professional Playout",
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = Color.FromArgb(200, 255, 255, 255),
                AutoSize = false,
                Size = new Size(550, 20),
                Location = new Point(25, 360),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblCopyright);

            // Timer per chiusura automatica (specificato Windows.Forms.Timer)
            System.Windows.Forms.Timer closeTimer = new System.Windows.Forms.Timer
            {
                Interval = 4000 // 4 secondi
            };
            closeTimer.Tick += (s, e) =>
            {
                closeTimer.Stop();
                this.Close();
            };
            closeTimer.Start();
        }

        private void SplashForm_Load(object sender, EventArgs e)
        {
            // Opzionale: animazione fade-in
            this.Opacity = 0;
            System.Windows.Forms.Timer fadeTimer = new System.Windows.Forms.Timer
            {
                Interval = 50
            };
            fadeTimer.Tick += (s, ev) =>
            {
                if (this.Opacity < 1)
                {
                    this.Opacity += 0.05;
                }
                else
                {
                    fadeTimer.Stop();
                }
            };
            fadeTimer.Start();
        }
    }
}