using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using AirDirector.Services.Licensing;
using AirDirector.Services.Localization;
using AirDirector.Themes;

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
            this.Size = new Size(580, 620);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = AppTheme.BgLight;

            // ===== HEADER =====
            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 100,
                BackColor = AppTheme.Primary
            };
            this.Controls.Add(headerPanel);

            Label lblIcon = new Label
            {
                Text = "🔐",
                Font = new Font("Segoe UI", 28),
                ForeColor = AppTheme.TextInverse,
                Location = new Point(20, 18),
                AutoSize = true
            };
            headerPanel.Controls.Add(lblIcon);

            Label lblTitle = new Label
            {
                Text = LanguageManager.GetString("License_Title", "Attivazione Licenza"),
                Font = new Font("Segoe UI", 19, FontStyle.Bold),
                ForeColor = AppTheme.TextInverse,
                AutoSize = false,
                Size = new Size(430, 38),
                Location = new Point(78, 16),
                TextAlign = ContentAlignment.MiddleLeft
            };
            headerPanel.Controls.Add(lblTitle);

            Label lblSubtitle = new Label
            {
                Text = LanguageManager.GetString("License_Subtitle", "Inserisci il tuo codice seriale per sbloccare tutte le funzionalità"),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(200, 230, 255),
                AutoSize = false,
                Size = new Size(500, 20),
                Location = new Point(78, 58),
                TextAlign = ContentAlignment.MiddleLeft
            };
            headerPanel.Controls.Add(lblSubtitle);

            // ===== MAIN CARD =====
            Panel cardPanel = new Panel
            {
                Location = new Point(24, 116),
                Size = new Size(524, 230),
                BackColor = AppTheme.Surface,
                Padding = new Padding(24)
            };
            cardPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var pen = new Pen(AppTheme.BorderLight, 1))
                    e.Graphics.DrawRectangle(pen, 0, 0, cardPanel.Width - 1, cardPanel.Height - 1);
            };
            this.Controls.Add(cardPanel);

            // Owner Name Label
            Label lblOwner = new Label
            {
                Text = LanguageManager.GetString("License_OwnerName", "Nome / Ragione Sociale"),
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = AppTheme.TextSecondary,
                Location = new Point(24, 20),
                AutoSize = true
            };
            cardPanel.Controls.Add(lblOwner);

            TextBox txtOwner = new TextBox
            {
                Name = "txtOwner",
                Font = new Font("Segoe UI", 11),
                Location = new Point(24, 40),
                Size = new Size(472, 34),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = AppTheme.BgLight
            };
            cardPanel.Controls.Add(txtOwner);

            // Serial Label
            Label lblSerial = new Label
            {
                Text = LanguageManager.GetString("License_Serial", "Codice Seriale"),
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = AppTheme.TextSecondary,
                Location = new Point(24, 92),
                AutoSize = true
            };
            cardPanel.Controls.Add(lblSerial);

            TextBox txtSerial = new TextBox
            {
                Name = "txtSerial",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                Location = new Point(24, 112),
                Size = new Size(472, 36),
                MaxLength = 18,
                CharacterCasing = CharacterCasing.Upper,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = AppTheme.BgLight
            };
            cardPanel.Controls.Add(txtSerial);

            Label lblFormat = new Label
            {
                Text = "Formato: ADR-XXXX-XXXX-XXXX",
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = AppTheme.TextSecondary,
                Location = new Point(24, 158),
                AutoSize = true
            };
            cardPanel.Controls.Add(lblFormat);

            // ===== ACTIVATE BUTTON =====
            Button btnActivate = new Button
            {
                Name = "btnActivate",
                Text = "🔓  " + LanguageManager.GetString("License_Activate", "Attiva Licenza"),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(24, 364),
                Size = new Size(524, 50),
                BackColor = AppTheme.Success,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnActivate.FlatAppearance.BorderSize = 0;
            btnActivate.Click += (s, e) => BtnActivate_Click(txtOwner.Text, txtSerial.Text);
            this.Controls.Add(btnActivate);

            // ===== SEPARATOR =====
            Panel sepPanel = new Panel
            {
                Location = new Point(24, 430),
                Size = new Size(524, 1),
                BackColor = AppTheme.BorderLight
            };
            this.Controls.Add(sepPanel);

            Label lblOr = new Label
            {
                Text = "— oppure —",
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = AppTheme.TextSecondary,
                Location = new Point(196, 438),
                AutoSize = true
            };
            this.Controls.Add(lblOr);

            // ===== DEMO BUTTON =====
            Button btnDemo = new Button
            {
                Name = "btnDemo",
                Text = "⏩  " + LanguageManager.GetString("License_Demo", "Continua in Modalità Demo"),
                Font = new Font("Segoe UI", 10),
                Location = new Point(24, 460),
                Size = new Size(524, 44),
                BackColor = AppTheme.Warning,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnDemo.FlatAppearance.BorderSize = 0;
            btnDemo.Click += BtnDemo_Click;
            this.Controls.Add(btnDemo);

            // ===== DEMO LIMITS INFO =====
            Label lblDemoInfo = new Label
            {
                Text = LanguageManager.GetString("License_DemoLimits", "Limiti Modalità Demo:") + "  " +
                       LanguageManager.GetString("License_DemoMusic", "50 brani") + "  •  " +
                       LanguageManager.GetString("License_DemoClips", "15 clips") + "  •  " +
                       LanguageManager.GetString("License_DemoEncoders", "1 encoder"),
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = AppTheme.TextSecondary,
                Location = new Point(24, 518),
                Size = new Size(524, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblDemoInfo);
        }

        private void BtnActivate_Click(string ownerName, string serial)
        {
            if (string.IsNullOrWhiteSpace(serial))
            {
                MessageBox.Show("Inserisci il codice seriale", "Errore",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool success = LicenseManager.ActivateLicense(serial, ownerName, out string errorMessage);

            if (success)
            {
                MessageBox.Show(
                    LanguageManager.GetString("License_Success", "Licenza attivata con successo!"),
                    "Successo",
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
                    "Errore",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void BtnDemo_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Continuerai in modalità demo con funzionalità limitate.\n\n" +
                "Vuoi proseguire?",
                "Modalità Demo",
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

