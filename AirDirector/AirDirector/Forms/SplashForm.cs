using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using AirDirector.Themes;

namespace AirDirector.Forms
{
    public partial class SplashForm : Form
    {
        private Panel[] _cardPanels;
        private Label[] _cardStatusLabels;
        private Image _logoImage;
        private Image _backgroundImage;

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

            // ── Logo text fallback ────────────────────────────────
            lblLogo.Font = new Font("Segoe UI", 36, FontStyle.Bold);
            lblLogo.ForeColor = Color.White;
            lblLogo.Text = "🎵 AirDirector";

            // ── Version label ─────────────────────────────────────
            lblVersion.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            lblVersion.ForeColor = Color.FromArgb(150, 180, 255);
            lblVersion.Text = $"Professional Playout System {AppVersion.DisplayVersion}";

            // ── Logo: image or text ───────────────────────────────
            LoadLogo();

            // ── Background ────────────────────────────────────────
            LoadBackground();

            // ── Sliding cards ─────────────────────────────────────
            _cardPanels = new Panel[_loadingModules.Length];
            _cardStatusLabels = new Label[_loadingModules.Length];

            for (int i = 0; i < _loadingModules.Length; i++)
            {
                int yPosition = i * 36;
                Panel card = CreateCard(_loadingModules[i].Icon, _loadingModules[i].Name, yPosition, out Label statusLabel);
                _cardPanels[i] = card;
                _cardStatusLabels[i] = statusLabel;
                cardsContainer.Controls.Add(card);
            }

            // ── Progress bar ──────────────────────────────────────
            progressBar.BackColor = Color.FromArgb(50, 50, 60);
            progressBar.ForeColor = Color.FromArgb(0, 180, 120);

            // ── Percentage label ──────────────────────────────────
            lblPercentage.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblPercentage.ForeColor = Color.White;
            lblPercentage.Text = "0%";

            // ── Copyright ─────────────────────────────────────────
            lblCopyright.Font = new Font("Segoe UI", 8, FontStyle.Regular);
            lblCopyright.ForeColor = Color.FromArgb(120, 120, 130);
            lblCopyright.Text = $"© {DateTime.Now.Year} AirDirector - All Rights Reserved";
        }

        private Panel CreateCard(string icon, string name, int yPosition, out Label statusLabel)
        {
            Panel card = new Panel
            {
                Location = new Point(700, yPosition),
                Size = new Size(580, 32),
                BackColor = Color.FromArgb(40, 40, 50),
                Visible = false
            };

            card.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen pen = new Pen(Color.FromArgb(80, 80, 90), 1))
                {
                    g.DrawRectangle(pen, new Rectangle(0, 0, card.Width - 1, card.Height - 1));
                }
            };

            Label lblIcon = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI Emoji", 10),
                Location = new Point(8, 6),
                Size = new Size(30, 20),
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblIcon);

            Label lblName = new Label
            {
                Text = name,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(45, 8),
                Size = new Size(400, 16),
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblName);

            Label lblStatus = new Label
            {
                Text = "⚡ LOAD",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.Yellow,
                Location = new Point(500, 8),
                Size = new Size(70, 16),
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblStatus);

            statusLabel = lblStatus;
            return card;
        }

        private void LoadLogo()
        {
            string[] logoPaths = new[]
            {
                Path.Combine(Application.StartupPath, "Assets", "logo.png"),
                Path.Combine(Application.StartupPath, "logo.png"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo.png")
            };

            foreach (string path in logoPaths)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        _logoImage = Image.FromFile(path);
                        pictureBoxLogo.Image = _logoImage;
                        pictureBoxLogo.Visible = true;
                        lblLogo.Visible = false;
                        return;
                    }
                    catch
                    {
                        // Try next path
                    }
                }
            }

            pictureBoxLogo.Visible = false;
            lblLogo.Visible = true;
        }

        private void LoadBackground()
        {
            string[] bgPaths = new[]
            {
                Path.Combine(Application.StartupPath, "Assets", "splash_bg.png"),
                Path.Combine(Application.StartupPath, "splash_bg.png")
            };

            foreach (string path in bgPaths)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        _backgroundImage = Image.FromFile(path);
                        this.BackgroundImage = _backgroundImage;
                        this.BackgroundImageLayout = ImageLayout.Stretch;
                        return;
                    }
                    catch
                    {
                        // Try next path
                    }
                }
            }

            // Fallback: solid color already set in InitializeCustomComponents
            this.BackColor = Color.FromArgb(25, 25, 35);
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
            System.Windows.Forms.Timer loadingTimer = new System.Windows.Forms.Timer { Interval = 500 };
            int stepIndex = 0;

            loadingTimer.Tick += (s, e) =>
            {
                if (stepIndex < _loadingModules.Length)
                {
                    int currentIndex = stepIndex;
                    AnimateCardSlideIn(currentIndex);

                    // After 300ms mark as complete
                    System.Windows.Forms.Timer completeTimer = new System.Windows.Forms.Timer { Interval = 300 };
                    completeTimer.Tick += (cs, ce) =>
                    {
                        MarkCardAsComplete(currentIndex);
                        completeTimer.Stop();
                        completeTimer.Dispose();
                    };
                    completeTimer.Start();

                    int progress = (int)((currentIndex + 1) / (float)_loadingModules.Length * 100);
                    progressBar.Value = progress;
                    lblPercentage.Text = $"{progress}%";

                    stepIndex++;
                }
                else
                {
                    loadingTimer.Stop();
                    System.Windows.Forms.Timer closeTimer = new System.Windows.Forms.Timer { Interval = 500 };
                    closeTimer.Tick += (cs, ce) => { closeTimer.Stop(); this.Close(); };
                    closeTimer.Start();
                }
            };
            loadingTimer.Start();
        }

        private void AnimateCardSlideIn(int index)
        {
            Panel card = _cardPanels[index];
            card.Visible = true;

            int startX = 700;
            int finalX = 10;
            int steps = 19; // ~300ms at 16ms interval
            int currentStep = 0;

            System.Windows.Forms.Timer slideTimer = new System.Windows.Forms.Timer { Interval = 16 };
            slideTimer.Tick += (s, ev) =>
            {
                if (currentStep < steps)
                {
                    float progress = (float)currentStep / steps;
                    float easedProgress = 1 - (float)Math.Pow(1 - progress, 3);
                    int currentX = (int)(startX + (finalX - startX) * easedProgress);
                    card.Location = new Point(currentX, card.Location.Y);
                    currentStep++;
                }
                else
                {
                    card.Location = new Point(finalX, card.Location.Y);
                    slideTimer.Stop();
                    slideTimer.Dispose();
                }
            };
            slideTimer.Start();
        }

        private void MarkCardAsComplete(int index)
        {
            Panel card = _cardPanels[index];
            Label statusLabel = _cardStatusLabels[index];

            card.BackColor = Color.FromArgb(30, 100, 60);
            statusLabel.Text = "✓ DONE";
            statusLabel.ForeColor = Color.FromArgb(100, 255, 150);
        }

        [System.Runtime.InteropServices.DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
            int nWidthEllipse, int nHeightEllipse);

        private void lblCopyright_Click(object sender, EventArgs e)
        {

        }

        private void cardsContainer_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
