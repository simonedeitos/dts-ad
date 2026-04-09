using System;
using System.Drawing;
using System.Windows.Forms;
using AirDirector.Models;
using AirDirector.Services.Localization;
using AirDirector.Themes;

namespace AirDirector.Forms
{
    public partial class LicenseRemoveConfirmForm : Form
    {
        private readonly LicenseInfo _license;

        public LicenseRemoveConfirmForm(LicenseInfo license)
        {
            InitializeComponent();
            _license = license;
            BuildUI();
        }

        private void BuildUI()
        {
            this.Text = LanguageManager.GetString("LicenseRemove.Title", "Conferma Rimozione Licenza");
            this.Size = new Size(440, 360);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = AppTheme.Surface;

            // Warning icon row
            Label lblWarningIcon = new Label
            {
                Text = "⚠️",
                Font = new Font("Segoe UI", 32),
                Location = new Point(20, 20),
                Size = new Size(60, 60),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblWarningIcon);

            Label lblTitle = new Label
            {
                Text = LanguageManager.GetString("LicenseRemove.Confirm", "Sei sicuro di voler rimuovere la licenza?"),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = AppTheme.TextPrimary,
                Location = new Point(90, 20),
                Size = new Size(320, 50),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(lblTitle);

            // Info box
            Panel infoBox = new Panel
            {
                Location = new Point(20, 92),
                Size = new Size(390, 80),
                BackColor = Color.FromArgb(255, 243, 205)
            };
            infoBox.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(255, 193, 7), 1))
                    e.Graphics.DrawRectangle(pen, 0, 0, infoBox.Width - 1, infoBox.Height - 1);
            };
            this.Controls.Add(infoBox);

            string ownerDisplay = !string.IsNullOrEmpty(_license.OwnerName) ? _license.OwnerName : (_license.SerialKey ?? "—");
            Label lblInfo = new Label
            {
                Text = string.Format(
                    LanguageManager.GetString("LicenseRemove.Info",
                        "La licenza intestata a «{0}» verrà rimossa da questo computer.\nPotrai riattivarla in un secondo momento."),
                    ownerDisplay),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(133, 100, 4),
                Location = new Point(10, 8),
                Size = new Size(370, 64),
                TextAlign = ContentAlignment.TopLeft
            };
            infoBox.Controls.Add(lblInfo);

            // ===== TYPE 'remove' TO CONFIRM =====
            Label lblTypeRemove = new Label
            {
                Text = LanguageManager.GetString("LicenseRemove.TypeRemove",
                    "Digita 'remove' per confermare la rimozione:"),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = AppTheme.TextPrimary,
                Location = new Point(20, 186),
                Size = new Size(390, 20),
                TextAlign = ContentAlignment.BottomLeft
            };
            this.Controls.Add(lblTypeRemove);

            TextBox txtConfirm = new TextBox
            {
                Name = "txtConfirm",
                Font = new Font("Segoe UI", 12),
                Location = new Point(20, 210),
                Size = new Size(390, 30),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = AppTheme.Surface
            };
            this.Controls.Add(txtConfirm);

            Label lblCloseWarning = new Label
            {
                Text = LanguageManager.GetString("LicenseRemove.CloseWarning",
                    "⚠️ L'applicazione verrà chiusa dopo la rimozione."),
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = AppTheme.Danger,
                Location = new Point(20, 244),
                Size = new Size(390, 18),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(lblCloseWarning);

            // Buttons
            Button btnConfirm = new Button
            {
                Text = LanguageManager.GetString("LicenseRemove.Confirm2", "Sì, rimuovi la licenza"),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Size = new Size(220, 40),
                Location = new Point(20, 272),
                BackColor = AppTheme.Danger,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnConfirm.FlatAppearance.BorderSize = 0;
            btnConfirm.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            this.Controls.Add(btnConfirm);

            // Enable confirm button only when user types 'remove'
            txtConfirm.TextChanged += (s, e) =>
            {
                btnConfirm.Enabled = txtConfirm.Text.Trim().Equals("remove", StringComparison.OrdinalIgnoreCase);
            };

            Button btnCancel = new Button
            {
                Text = LanguageManager.GetString("Common.Cancel", "Annulla"),
                Font = new Font("Segoe UI", 10),
                Size = new Size(130, 40),
                Location = new Point(280, 272),
                BackColor = Color.FromArgb(117, 117, 117),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };
            this.Controls.Add(btnCancel);
        }
    }
}
