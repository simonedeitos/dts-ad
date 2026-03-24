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
            this.Size = new Size(600, 520);
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
                Size = new Size(540, 320),
                BackColor = AppTheme.Surface
            };
            this.Controls.Add(contentPanel);

            int yPos = 20;

            // Owner Name Label
            Label lblOwner = new Label
            {
                Text = LanguageManager.GetString("License_OwnerName", "Nome / Ragione Sociale:"),
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                Location = new Point(20, yPos),
                AutoSize = true
            };
            contentPanel.Controls.Add(lblOwner);

            yPos += 30;

            // Owner Name TextBox
            TextBox txtOwner = new TextBox
            {
                Name = "txtOwner",
                Font = new Font("Segoe UI", 11),
                Location = new Point(20, yPos),
                Size = new Size(490, 30)
            };
            contentPanel.Controls.Add(txtOwner);

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
                MaxLength = 18,
                CharacterCasing = CharacterCasing.Upper
            };
            contentPanel.Controls.Add(txtSerial);

            // Placeholder per formato
            Label lblFormat = new Label
            {
                Text = "Formato: ADR-XXXX-XXXX-XXXX",
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
            btnActivate.Click += (s, e) => BtnActivate_Click(txtOwner.Text, txtSerial.Text);
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
                       LanguageManager.GetString("License_DemoClips", "• Massimo 25 clips") + "\n" +
                       LanguageManager.GetString("License_DemoEncoders", "• Massimo 1 encoder streaming"),
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = AppTheme.TextSecondary,
                Location = new Point(20, yPos),
                Size = new Size(490, 80),
                TextAlign = ContentAlignment.TopLeft
            };
            contentPanel.Controls.Add(lblDemoInfo);
        }

        private void BtnActivate_Click(string ownerName, string serial)
        {
            if (string.IsNullOrWhiteSpace(serial))
            {
                MessageBox.Show("Inserisci il codice seriale", "Errore",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Attivazione tramite API
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
