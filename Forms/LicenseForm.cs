using System;
using System.Drawing;
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
            // Form settings
            this.Text = LanguageManager.GetString("License_Title", "Attivazione Licenza");
            this.Size = new Size(600, 550);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = AppTheme.BgLight;

            // ===== HEADER =====
            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = AppTheme.Primary
            };
            this.Controls.Add(headerPanel);

            Label lblTitle = new Label
            {
                Text = "🔐 " + LanguageManager.GetString("License_Title", "Attivazione Licenza"),
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = AppTheme.TextInverse,
                AutoSize = false,
                Size = new Size(550, 40),
                Location = new Point(20, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };
            headerPanel.Controls.Add(lblTitle);

            // ===== CONTENT PANEL =====
            Panel contentPanel = new Panel
            {
                Location = new Point(20, 100),
                Size = new Size(540, 340),
                BackColor = AppTheme.Surface
            };
            this.Controls.Add(contentPanel);

            int yPos = 20;

            // Email Label
            Label lblEmail = new Label
            {
                Text = LanguageManager.GetString("License_Email", "Email:"),
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                Location = new Point(20, yPos),
                AutoSize = true
            };
            contentPanel.Controls.Add(lblEmail);

            yPos += 30;

            // Email TextBox
            TextBox txtEmail = new TextBox
            {
                Name = "txtEmail",
                Font = new Font("Segoe UI", 11),
                Location = new Point(20, yPos),
                Size = new Size(490, 30)
            };
            contentPanel.Controls.Add(txtEmail);

            yPos += 50;

            // Serial Label
            Label lblSerial = new Label
            {
                Text = LanguageManager.GetString("License_Serial", "Codice Seriale:"),
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                Location = new Point(20, yPos),
                AutoSize = true
            };
            contentPanel.Controls.Add(lblSerial);

            yPos += 30;

            // Serial TextBox
            TextBox txtSerial = new TextBox
            {
                Name = "txtSerial",
                Font = new Font("Segoe UI", 11),
                Location = new Point(20, yPos),
                Size = new Size(490, 30),
                MaxLength = 24,
                CharacterCasing = CharacterCasing.Upper
            };
            contentPanel.Controls.Add(txtSerial);

            // Placeholder per formato
            Label lblFormat = new Label
            {
                Text = "Formato: AD-XXXX-XXXX-XXXX-XXXX",
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = AppTheme.TextSecondary,
                Location = new Point(20, yPos + 35),
                AutoSize = true
            };
            contentPanel.Controls.Add(lblFormat);

            yPos += 70;

            // Activate Button
            Button btnActivate = new Button
            {
                Name = "btnActivate",
                Text = LanguageManager.GetString("License_Activate", "Attiva Licenza"),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Location = new Point(20, yPos),
                Size = new Size(490, 45),
                BackColor = AppTheme.Success,
                ForeColor = AppTheme.TextInverse,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnActivate.FlatAppearance.BorderSize = 0;
            btnActivate.Click += (s, e) => BtnActivate_Click(txtEmail.Text, txtSerial.Text);
            contentPanel.Controls.Add(btnActivate);

            yPos += 60;

            // Demo Button
            Button btnDemo = new Button
            {
                Name = "btnDemo",
                Text = LanguageManager.GetString("License_Demo", "Continua in Modalità Demo"),
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                Location = new Point(20, yPos),
                Size = new Size(490, 40),
                BackColor = AppTheme.Warning,
                ForeColor = AppTheme.TextInverse,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnDemo.FlatAppearance.BorderSize = 0;
            btnDemo.Click += BtnDemo_Click;
            contentPanel.Controls.Add(btnDemo);

            yPos += 55;

            // Demo Limits Info
            Label lblDemoInfo = new Label
            {
                Text = LanguageManager.GetString("License_DemoLimits", "Limiti Modalità Demo:") + "\n" +
                       LanguageManager.GetString("License_DemoMusic", "• Massimo 50 brani musicali") + "\n" +
                       LanguageManager.GetString("License_DemoClips", "• Massimo 15 clips") + "\n" +
                       LanguageManager.GetString("License_DemoEncoders", "• Massimo 1 encoder streaming"),
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = AppTheme.TextSecondary,
                Location = new Point(20, yPos),
                Size = new Size(490, 80),
                TextAlign = ContentAlignment.TopLeft
            };
            contentPanel.Controls.Add(lblDemoInfo);
        }

        private void BtnActivate_Click(string email, string serial)
        {
            // Validazione
            if (string.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show("Inserisci un indirizzo email valido", "Errore",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(serial))
            {
                MessageBox.Show("Inserisci il codice seriale", "Errore",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Attivazione
            bool success = LicenseManager.ActivateLicense(email, serial, out string errorMessage);

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