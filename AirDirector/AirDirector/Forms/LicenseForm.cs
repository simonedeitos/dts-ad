using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using AirDirector.Services.Licensing;
using AirDirector.Services.Localization;

namespace AirDirector.Forms
{
    public partial class LicenseForm : Form
    {
        public bool ContinueInDemoMode { get; private set; }

        public LicenseForm()
        {
            InitializeComponent();
            InitializeCustomComponents();
        }

        private void InitializeCustomComponents()
        {
            this.Text = LanguageManager.GetString("License_Title", "Attivazione Licenza");
            this.ClientSize = new Size(620, 600);
            this.MinimumSize = new Size(640, 640);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(45, 45, 48);

            // ===== HEADER =====
            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 110,
                BackColor = Color.FromArgb(0, 120, 215)
            };
            this.Controls.Add(headerPanel);

            Label lblIcon = new Label
            {
                Text = "🔐",
                Font = new Font("Segoe UI", 30),
                ForeColor = Color.White,
                Location = new Point(20, 20),
                AutoSize = true
            };
            headerPanel.Controls.Add(lblIcon);

            Label lblTitle = new Label
            {
                Text = LanguageManager.GetString("License_Title", "Attivazione Licenza"),
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = false,
                Size = new Size(460, 42),
                Location = new Point(90, 18),
                TextAlign = ContentAlignment.MiddleLeft
            };
            headerPanel.Controls.Add(lblTitle);

            Label lblSubtitle = new Label
            {
                Text = LanguageManager.GetString("License_Subtitle", "Inserisci il tuo codice seriale per sbloccare tutte le funzionalità"),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(200, 230, 255),
                AutoSize = false,
                Size = new Size(520, 22),
                Location = new Point(95, 66),
                TextAlign = ContentAlignment.MiddleLeft
            };
            headerPanel.Controls.Add(lblSubtitle);

            // ===== MAIN CARD =====
            // Layout verticale: header(110) + margin(20) + card(240) + margin(20) + btnActivate(52) + margin(24) + sep(1) + margin(12) + btnDemo(48) + margin(14) + lblDemoInfo(22) + margin(20)
            int margin = 20;
            int cardTop = 110 + margin;

            Panel cardPanel = new Panel
            {
                Location = new Point(margin, cardTop),
                Size = new Size(580, 240),
                BackColor = Color.FromArgb(40, 40, 40),
                Padding = new Padding(24)
            };
            cardPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var pen = new Pen(Color.FromArgb(70, 70, 70), 1))
                    e.Graphics.DrawRectangle(pen, 0, 0, cardPanel.Width - 1, cardPanel.Height - 1);
            };
            this.Controls.Add(cardPanel);

            // Owner Name Label
            Label lblOwner = new Label
            {
                Text = LanguageManager.GetString("License_OwnerName", "Nome / Ragione Sociale"),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 180, 180),
                Location = new Point(24, 18),
                AutoSize = true
            };
            cardPanel.Controls.Add(lblOwner);

            TextBox txtOwner = new TextBox
            {
                Name = "txtOwner",
                Font = new Font("Segoe UI", 11),
                Location = new Point(24, 40),
                Size = new Size(532, 34),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White
            };
            cardPanel.Controls.Add(txtOwner);

            // Serial Label
            Label lblSerial = new Label
            {
                Text = LanguageManager.GetString("License_Serial", "Codice Seriale"),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 180, 180),
                Location = new Point(24, 90),
                AutoSize = true
            };
            cardPanel.Controls.Add(lblSerial);

            TextBox txtSerial = new TextBox
            {
                Name = "txtSerial",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                Location = new Point(24, 112),
                Size = new Size(532, 36),
                MaxLength = 18,
                CharacterCasing = CharacterCasing.Upper,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White
            };
            cardPanel.Controls.Add(txtSerial);

            Label lblFormat = new Label
            {
                Text = LanguageManager.GetString("License.Format", "Formato: ADR-XXXX-XXXX-XXXX"),
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = Color.FromArgb(180, 180, 180),
                Location = new Point(24, 160),
                AutoSize = true
            };
            cardPanel.Controls.Add(lblFormat);

            LinkLabel lnkSite = new LinkLabel
            {
                Text = "www.airdirector.app",
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                LinkColor = Color.FromArgb(0, 120, 215),
                ActiveLinkColor = Color.FromArgb(0, 120, 215),
                VisitedLinkColor = Color.FromArgb(0, 120, 215),
                Location = new Point(24, 182),
                AutoSize = true
            };
            lnkSite.LinkClicked += (s, e) =>
            {
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://www.airdirector.app") { UseShellExecute = true }); }
                catch { }
            };
            cardPanel.Controls.Add(lnkSite);

            // ===== ACTIVATE BUTTON =====
            int btnActivateTop = cardTop + cardPanel.Height + margin;
            Button btnActivate = new Button
            {
                Name = "btnActivate",
                Text = "🔓  " + LanguageManager.GetString("License_Activate", "Attiva Licenza"),
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                Location = new Point(margin, btnActivateTop),
                Size = new Size(580, 52),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnActivate.FlatAppearance.BorderSize = 0;
            btnActivate.Click += (s, e) => BtnActivate_Click(txtOwner.Text, txtSerial.Text);
            this.Controls.Add(btnActivate);

            // ===== SEPARATOR =====
            int sepTop = btnActivateTop + btnActivate.Height + 24;
            Panel sepPanel = new Panel
            {
                Location = new Point(margin, sepTop),
                Size = new Size(580, 1),
                BackColor = Color.FromArgb(70, 70, 70)
            };
            this.Controls.Add(sepPanel);

            // ===== DEMO BUTTON =====
            int btnDemoTop = sepTop + 12;
            Button btnDemo = new Button
            {
                Name = "btnDemo",
                Text = "⏩  " + LanguageManager.GetString("License_Demo", "Continua in Modalità Demo"),
                Font = new Font("Segoe UI", 11),
                Location = new Point(margin, btnDemoTop),
                Size = new Size(580, 48),
                BackColor = Color.FromArgb(255, 140, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnDemo.FlatAppearance.BorderSize = 0;
            btnDemo.Click += BtnDemo_Click;
            this.Controls.Add(btnDemo);

            // ===== DEMO LIMITS INFO =====
            int demoInfoTop = btnDemoTop + btnDemo.Height + 14;
            Label lblDemoInfo = new Label
            {
                Text = LanguageManager.GetString("License_DemoLimits", "Limiti Modalità Demo:") + "  " +
                       LanguageManager.GetString("License_DemoMusic", "50 brani") + "  •  " +
                       LanguageManager.GetString("License_DemoClips", "15 clips") + "  •  " +
                       LanguageManager.GetString("License_DemoEncoders", "1 encoder") + Environment.NewLine +
                       LanguageManager.GetString("License_DemoClocks", "2 clock") + "  •  " +
                       LanguageManager.GetString("License_DemoSchedules", "2 schedulazioni") + "  •  " +
                       LanguageManager.GetString("License_DemoDownloaderSchedules", "1 schedulazione downloader"),
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = Color.FromArgb(180, 180, 180),
                Location = new Point(margin, demoInfoTop),
                Size = new Size(580, 40),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblDemoInfo);

            // Aggiusta altezza client in base al contenuto
            this.ClientSize = new Size(620, demoInfoTop + lblDemoInfo.Height + margin);
        }

        private void BtnActivate_Click(string ownerName, string serial)
        {
            if (string.IsNullOrWhiteSpace(serial))
            {
                MessageBox.Show(LanguageManager.GetString("License.EnterSerial", "Inserisci il codice seriale"), LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool success = LicenseManager.ActivateLicense(serial, ownerName, out string errorMessage);

            if (success)
            {
                MessageBox.Show(
                    LanguageManager.GetString("License_Success", "Licenza attivata con successo!"),
                    LanguageManager.GetString("Common.Success", "Successo"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                ContinueInDemoMode = false;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show(
                    LanguageManager.GetString("License_Error", "Errore durante l'attivazione") + ":\n\n" + errorMessage,
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void BtnDemo_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                LanguageManager.GetString("License.DemoConfirmMessage", "Continuerai in modalità demo con funzionalità limitate.\n\nVuoi proseguire?"),
                LanguageManager.GetString("License.DemoMode", "Modalità Demo"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                ContinueInDemoMode = true;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void LicenseForm_Load(object sender, EventArgs e)
        {
            // Evento Load
        }
    }
}

