using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using AirDirector.Models;
using AirDirector.Services.Licensing;
using AirDirector.Services.Localization;
using AirDirector.Themes;

namespace AirDirector.Forms
{
    public partial class LicenseInfoForm : Form
    {
        public bool LicenseRemoved { get; private set; }

        private readonly LicenseInfo _license;

        public LicenseInfoForm()
        {
            InitializeComponent();
            _license = LicenseManager.GetCurrentLicense();
            BuildUI();
        }

        private void BuildUI()
        {
            this.Text = LanguageManager.GetString("LicenseInfo.Title", "Gestione Licenza");
            this.Size = new Size(520, 460);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = AppTheme.BgLight;

            // ===== HEADER PANEL =====
            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 90,
                BackColor = AppTheme.Primary
            };
            this.Controls.Add(headerPanel);

            Label lblIcon = new Label
            {
                Text = "🔑",
                Font = new Font("Segoe UI", 24),
                ForeColor = AppTheme.TextInverse,
                Location = new Point(20, 22),
                AutoSize = true
            };
            headerPanel.Controls.Add(lblIcon);

            Label lblTitle = new Label
            {
                Text = LanguageManager.GetString("LicenseInfo.Title", "Gestione Licenza"),
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = AppTheme.TextInverse,
                AutoSize = false,
                Size = new Size(380, 36),
                Location = new Point(70, 14),
                TextAlign = ContentAlignment.MiddleLeft
            };
            headerPanel.Controls.Add(lblTitle);

            Label lblSubtitle = new Label
            {
                Text = LanguageManager.GetString("LicenseInfo.Subtitle", "La tua licenza AirDirector"),
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = Color.FromArgb(200, 230, 255),
                AutoSize = false,
                Size = new Size(380, 20),
                Location = new Point(70, 54),
                TextAlign = ContentAlignment.MiddleLeft
            };
            headerPanel.Controls.Add(lblSubtitle);

            // ===== STATUS BADGE =====
            Panel statusBadge = new Panel
            {
                Size = new Size(160, 36),
                BackColor = AppTheme.Success
            };
            statusBadge.Location = new Point(this.ClientSize.Width - statusBadge.Width - 20, 27);
            statusBadge.Region = CreateRoundedRegion(160, 36, 18);
            headerPanel.Controls.Add(statusBadge);

            Label lblStatus = new Label
            {
                Text = "✅  " + LanguageManager.GetString("LicenseInfo.Active", "LICENZA ATTIVA"),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            statusBadge.Controls.Add(lblStatus);

            // ===== CARD PANEL =====
            Panel cardPanel = new Panel
            {
                Location = new Point(24, 110),
                Size = new Size(464, 250),
                BackColor = AppTheme.Surface,
                Padding = new Padding(20)
            };
            cardPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var pen = new Pen(AppTheme.BorderLight, 1))
                    e.Graphics.DrawRectangle(pen, 0, 0, cardPanel.Width - 1, cardPanel.Height - 1);
            };
            this.Controls.Add(cardPanel);

            // --- Card rows ---
            int yRow = 20;
            AddInfoRow(cardPanel, "👤", LanguageManager.GetString("LicenseInfo.Owner", "Proprietario"), _license.OwnerName ?? "—", ref yRow);
            AddInfoRow(cardPanel, "🔑", LanguageManager.GetString("LicenseInfo.Serial", "Codice Seriale"), _license.SerialKey ?? "—", ref yRow);
            AddInfoRow(cardPanel, "📅", LanguageManager.GetString("LicenseInfo.ActivatedOn", "Attivata il"),
                _license.ActivatedOn != DateTime.MinValue ? _license.ActivatedOn.ToString("dd/MM/yyyy HH:mm") : "—", ref yRow);

            // Horizontal separator
            Panel separator = new Panel
            {
                Location = new Point(16, yRow),
                Size = new Size(432, 1),
                BackColor = AppTheme.BorderLight
            };
            cardPanel.Controls.Add(separator);
            yRow += 16;

            AddInfoRow(cardPanel, "🖥️", LanguageManager.GetString("LicenseInfo.Machine", "ID Macchina"),
                TruncateMachineId(_license.MachineID), ref yRow, isSecondary: true);

            // ===== REMOVE BUTTON =====
            Button btnRemove = new Button
            {
                Text = "🗑  " + LanguageManager.GetString("LicenseInfo.Remove", "Rimuovi Licenza"),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Size = new Size(224, 42),
                Location = new Point(24, 376),
                BackColor = AppTheme.Danger,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRemove.FlatAppearance.BorderSize = 0;
            btnRemove.Click += BtnRemove_Click;
            this.Controls.Add(btnRemove);

            // ===== CLOSE BUTTON =====
            Button btnClose = new Button
            {
                Text = LanguageManager.GetString("Common.Close", "Chiudi"),
                Font = new Font("Segoe UI", 10),
                Size = new Size(120, 42),
                Location = new Point(368, 376),
                BackColor = Color.FromArgb(117, 117, 117),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(btnClose);
        }

        private void AddInfoRow(Panel parent, string icon, string label, string value, ref int yPos, bool isSecondary = false)
        {
            Label lblIcon = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI", 13),
                Location = new Point(16, yPos),
                Size = new Size(32, 32),
                TextAlign = ContentAlignment.MiddleCenter
            };
            parent.Controls.Add(lblIcon);

            Label lblLabel = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 8, FontStyle.Regular),
                ForeColor = AppTheme.TextSecondary,
                Location = new Point(54, yPos),
                AutoSize = true
            };
            parent.Controls.Add(lblLabel);

            Label lblValue = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", isSecondary ? 9 : 10, isSecondary ? FontStyle.Regular : FontStyle.Bold),
                ForeColor = isSecondary ? AppTheme.TextSecondary : AppTheme.TextPrimary,
                Location = new Point(54, yPos + 18),
                AutoSize = false,
                Size = new Size(380, 22)
            };
            parent.Controls.Add(lblValue);

            yPos += 54;
        }

        private void BtnRemove_Click(object sender, EventArgs e)
        {
            using (var confirmForm = new LicenseRemoveConfirmForm(_license))
            {
                if (confirmForm.ShowDialog(this) == DialogResult.OK)
                {
                    if (LicenseManager.RemoveLicense(out string error))
                    {
                        LicenseRemoved = true;
                        MessageBox.Show(
                            LanguageManager.GetString("LicenseRemove.RemovedClosing",
                                "Licenza rimossa con successo.\nL'applicazione verrà chiusa."),
                            LanguageManager.GetString("Common.Info", "Informazione"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);

                        // Force close the app even if playing
                        Environment.Exit(0);
                    }
                    else
                    {
                        MessageBox.Show(
                            LanguageManager.GetString("Common.Error", "Errore") + ": " + error,
                            LanguageManager.GetString("Common.Error", "Errore"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
            }
        }

        private static string TruncateMachineId(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length <= 20)
                return id ?? "—";
            return id.Substring(0, 8) + "..." + id.Substring(id.Length - 8);
        }

        private static Region CreateRoundedRegion(int width, int height, int radius)
        {
            var path = new GraphicsPath();
            path.AddArc(0, 0, radius * 2, radius * 2, 180, 90);
            path.AddArc(width - radius * 2, 0, radius * 2, radius * 2, 270, 90);
            path.AddArc(width - radius * 2, height - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(0, height - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return new Region(path);
        }
    }
}
