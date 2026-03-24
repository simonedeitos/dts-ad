using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using AirDirector.Services.Localization;

namespace AirDirector.Controls
{
    public partial class WhatsAppControl : UserControl
    {
        private WebView2 webView;
        private Button btnReload;
        private Button btnZoomIn;
        private Button btnZoomOut;
        private Button btnAudioInfo;
        private Label lblStatus;
        private Label lblZoom;
        private Label lblTitle;
        private Label lblZoomLabel;
        private string _whatsAppOutputDevice = "";
        private double _currentZoom = 0.8;

        public WhatsAppControl()
        {
            InitializeComponent();
            LoadWhatsAppOutput();
            InitializeUI();
            ApplyLanguage();
            InitializeWebView();

            LanguageManager.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            if (lblTitle != null)
                lblTitle.Text = "💬 " + LanguageManager.GetString("WhatsApp.Title", "WHATSAPP WEB");

            if (lblZoomLabel != null)
                lblZoomLabel.Text = "🔍 " + LanguageManager.GetString("WhatsApp.Zoom", "Zoom:");

            if (btnAudioInfo != null)
                btnAudioInfo.Text = "🔊 " + LanguageManager.GetString("WhatsApp.Audio", "AUDIO");

            if (lblStatus != null && lblStatus.Text == "Caricamento...")
                lblStatus.Text = LanguageManager.GetString("WhatsApp.Loading", "Caricamento...");
        }

        private void LoadWhatsAppOutput()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AirDirector"))
                {
                    _whatsAppOutputDevice = key?.GetValue("WhatsAppOutput") as string ?? "";
                }
            }
            catch
            {
                _whatsAppOutputDevice = "";
            }
        }

        private void InitializeUI()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(25, 25, 25);

            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(37, 211, 102)
            };
            this.Controls.Add(headerPanel);

            lblTitle = new Label
            {
                Text = "💬 WHATSAPP WEB",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 12),
                AutoSize = true
            };
            headerPanel.Controls.Add(lblTitle);

            lblZoomLabel = new Label
            {
                Text = "🔍 Zoom:",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(250, 15),
                AutoSize = true
            };
            headerPanel.Controls.Add(lblZoomLabel);

            btnZoomOut = new Button
            {
                Text = "➖",
                Location = new Point(320, 10),
                Size = new Size(40, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnZoomOut.FlatAppearance.BorderSize = 0;
            btnZoomOut.Click += BtnZoomOut_Click;
            headerPanel.Controls.Add(btnZoomOut);

            lblZoom = new Label
            {
                Text = "80%",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(368, 15),
                Size = new Size(50, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };
            headerPanel.Controls.Add(lblZoom);

            btnZoomIn = new Button
            {
                Text = "➕",
                Location = new Point(425, 10),
                Size = new Size(40, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnZoomIn.FlatAppearance.BorderSize = 0;
            btnZoomIn.Click += BtnZoomIn_Click;
            headerPanel.Controls.Add(btnZoomIn);

            btnReload = new Button
            {
                Text = "🔄",
                Location = new Point(485, 10),
                Size = new Size(40, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 14),
                Cursor = Cursors.Hand
            };
            btnReload.FlatAppearance.BorderSize = 0;
            btnReload.Click += (s, e) => webView?.Reload();
            headerPanel.Controls.Add(btnReload);

            btnAudioInfo = new Button
            {
                Text = "🔊 AUDIO",
                Location = new Point(545, 10),
                Size = new Size(90, 30),
                BackColor = Color.FromArgb(255, 140, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAudioInfo.FlatAppearance.BorderSize = 0;
            btnAudioInfo.Click += BtnAudioInfo_Click;
            headerPanel.Controls.Add(btnAudioInfo);

            lblStatus = new Label
            {
                Text = "Caricamento...",
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = Color.LightGray,
                Location = new Point(15, 38),
                AutoSize = true
            };
            headerPanel.Controls.Add(lblStatus);

            Panel webViewContainer = new Panel
            {
                Location = new Point(0, 60),
                Size = new Size(this.Width, this.Height - 60),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White
            };
            this.Controls.Add(webViewContainer);

            webView = new WebView2
            {
                Dock = DockStyle.Fill,
                Source = new Uri("https://web.whatsapp.com")
            };
            webViewContainer.Controls.Add(webView);
        }

        private void BtnAudioInfo_Click(object sender, EventArgs e)
        {
            string message = LanguageManager.GetString("WhatsApp.AudioInfoMessage",
                "ℹ️ INFORMAZIONI AUDIO WHATSAPP WEB\\n\\n" +
                "L'audio di WhatsApp Web viene riprodotto sul dispositivo\\n" +
                "impostato come PREDEFINITO in Windows.\\n\\n" +
                "🔧 PER CAMBIARE OUTPUT AUDIO:\\n\\n" +
                "1.Aprire il pannello Audio di Windows\\n" +
                "2.Posizionarsi su Riproduzione\\n" +
                "3.Individuare l'uscita audio\\n" +
                "4.Premere tasto destro\\n" +
                "5.Selezionare: Imposta come dispositivo predefinito.\\n");

            MessageBox.Show(
                message,
                LanguageManager.GetString("WhatsApp.AudioInfoTitle", "🔊 Audio WhatsApp Web"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private async void InitializeWebView()
        {
            try
            {
                await webView.EnsureCoreWebView2Async(null);

                webView.ZoomFactor = _currentZoom;
                UpdateZoomLabel();

                webView.CoreWebView2.NavigationCompleted += (s, e) =>
                {
                    if (e.IsSuccess)
                    {
                        lblStatus.Text = "✅ " + LanguageManager.GetString("WhatsApp.Connected", "Connesso");
                        lblStatus.ForeColor = Color.LightGreen;
                    }
                    else
                    {
                        lblStatus.Text = "❌ " + LanguageManager.GetString("WhatsApp.ConnectionError", "Errore connessione");
                        lblStatus.ForeColor = Color.Red;
                    }
                };

                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
                webView.CoreWebView2.Settings.IsWebMessageEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(LanguageManager.GetString("WhatsApp.InitError", "Errore inizializzazione WebView2:\\n{0}\\n\\nInstalla WebView2 Runtime! "), ex.Message),
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void BtnZoomIn_Click(object sender, EventArgs e)
        {
            if (_currentZoom < 2.0)
            {
                _currentZoom += 0.1;
                _currentZoom = Math.Min(_currentZoom, 2.0);
                ApplyZoom();
            }
        }

        private void BtnZoomOut_Click(object sender, EventArgs e)
        {
            if (_currentZoom > 0.5)
            {
                _currentZoom -= 0.1;
                _currentZoom = Math.Max(_currentZoom, 0.5);
                ApplyZoom();
            }
        }

        private void ApplyZoom()
        {
            if (webView != null && webView.CoreWebView2 != null)
            {
                webView.ZoomFactor = _currentZoom;
                UpdateZoomLabel();
            }
        }

        private void UpdateZoomLabel()
        {
            int zoomPercent = (int)(_currentZoom * 100);
            lblZoom.Text = $"{zoomPercent}%";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                LanguageManager.SaveMissingKeysToFile();
                LanguageManager.LanguageChanged -= OnLanguageChanged;
                webView?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}