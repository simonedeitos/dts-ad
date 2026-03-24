using AirDirector.Controls;
using AirDirector.Services.Database;
using AirDirector.Services.Localization;
using AirDirector.Themes;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace AirDirector.Forms
{
    public partial class VideoAssociationDialog : Form
    {
        private readonly object _entry;

        // UI Controls
        private RadioButton rbNone;
        private RadioButton rbStaticVideo;
        private RadioButton rbBufferVideo;

        private Panel pnlStaticVideo;
        private TextBox txtVideoPath;
        private Button btnBrowseVideo;
        private Label lblVideoInfo;

        private Button btnSave;
        private Button btnCancel;

        // Result
        public VideoSourceType SelectedVideoSource { get; private set; }
        public string SelectedVideoPath { get; private set; }
        public string SelectedNDISource { get; private set; } = "";

        // isClip is kept for backward compatibility with existing callers; NDI is no longer supported.
        public VideoAssociationDialog(object entry, bool isClip = false)
        {
            _entry = entry;

            InitializeComponent();
            LoadCurrentSettings();
        }

        private void InitializeComponent()
        {
            this.Text = "🎬 Associa Video";
            this.Size = new Size(600, 430);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = AppTheme.BgDark;
            this.ForeColor = Color.White;

            // ═══════════════════════════════════════════════
            // HEADER
            // ═══════════════════════════════════════════════
            Label lblTitle = new Label
            {
                Text = "🎬 ASSOCIA VIDEO",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 20),
                AutoSize = true
            };
            this.Controls.Add(lblTitle);

            string entryTitle = "";
            if (_entry is MusicEntry musicEntry)
                entryTitle = $"{musicEntry.Artist} - {musicEntry.Title}";
            else if (_entry is ClipEntry clipEntry)
                entryTitle = clipEntry.Title;

            Label lblEntryName = new Label
            {
                Text = $"📝 {entryTitle}",
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = Color.LightGray,
                Location = new Point(20, 50),
                Size = new Size(550, 20)
            };
            this.Controls.Add(lblEntryName);

            // ═══════════════════════════════════════════════
            // RADIO BUTTONS
            // ═══════════════════════════════════════════════
            int yPos = 90;

            rbNone = new RadioButton
            {
                Text = "🎵 Nessun video (solo audio)",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(30, yPos),
                AutoSize = true,
                Checked = true
            };
            rbNone.CheckedChanged += RadioButton_CheckedChanged;
            this.Controls.Add(rbNone);

            yPos += 40;

            rbStaticVideo = new RadioButton
            {
                Text = "🎬 File video statico (MP4, MOV, AVI, MKV)",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(30, yPos),
                AutoSize = true
            };
            rbStaticVideo.CheckedChanged += RadioButton_CheckedChanged;
            this.Controls.Add(rbStaticVideo);

            yPos += 35;

            // Panel Static Video
            pnlStaticVideo = new Panel
            {
                Location = new Point(50, yPos),
                Size = new Size(520, 80),
                BackColor = Color.FromArgb(50, 50, 50),
                Visible = false,
                BorderStyle = BorderStyle.FixedSingle
            };

            txtVideoPath = new TextBox
            {
                Location = new Point(10, 10),
                Size = new Size(420, 25),
                Font = new Font("Segoe UI", 9),
                ReadOnly = true,
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White
            };
            pnlStaticVideo.Controls.Add(txtVideoPath);

            btnBrowseVideo = new Button
            {
                Text = "📁 Sfoglia",
                Location = new Point(440, 8),
                Size = new Size(70, 28),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnBrowseVideo.FlatAppearance.BorderSize = 0;
            btnBrowseVideo.Click += BtnBrowseVideo_Click;
            pnlStaticVideo.Controls.Add(btnBrowseVideo);

            lblVideoInfo = new Label
            {
                Text = "Formati supportati: MP4, MOV, AVI, MKV, WMV",
                Location = new Point(10, 45),
                Size = new Size(500, 20),
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.LightGray
            };
            pnlStaticVideo.Controls.Add(lblVideoInfo);

            this.Controls.Add(pnlStaticVideo);

            yPos += 90;

            // ═══════════════════════════════════════════════
            // BUFFER VIDEO
            // ═══════════════════════════════════════════════
            rbBufferVideo = new RadioButton
            {
                Text = "🖼️ Video tampone casuale (dalla cartella configurata)",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(30, yPos),
                AutoSize = true
            };
            rbBufferVideo.CheckedChanged += RadioButton_CheckedChanged;
            this.Controls.Add(rbBufferVideo);

            yPos += 40;

            Label lblBufferInfo = new Label
            {
                Text = $"📂 Cartella tampone: {ConfigurationControl.GetBufferVideoPath()}",
                Location = new Point(50, yPos),
                Size = new Size(520, 20),
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.LightGray
            };
            this.Controls.Add(lblBufferInfo);

            // ═══════════════════════════════════════════════
            // BUTTONS
            // ═══════════════════════════════════════════════
            btnCancel = new Button
            {
                Text = "❌ Annulla",
                Location = new Point(350, 350),
                Size = new Size(110, 35),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            this.Controls.Add(btnCancel);

            btnSave = new Button
            {
                Text = "💾 Salva",
                Location = new Point(470, 350),
                Size = new Size(110, 35),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            this.CancelButton = btnCancel;
        }

        private void LoadCurrentSettings()
        {
            VideoSourceType currentSource = VideoSourceType.None;
            string currentVideoPath = "";

            if (_entry is MusicEntry musicEntry)
            {
                currentSource = musicEntry.VideoSource;
                currentVideoPath = musicEntry.VideoFilePath ?? "";
            }
            else if (_entry is ClipEntry clipEntry)
            {
                currentSource = clipEntry.VideoSource;
                currentVideoPath = clipEntry.VideoFilePath ?? "";
            }

            switch (currentSource)
            {
                case VideoSourceType.None:
                    rbNone.Checked = true;
                    break;

                case VideoSourceType.StaticVideo:
                    rbStaticVideo.Checked = true;
                    txtVideoPath.Text = currentVideoPath;
                    break;

                case VideoSourceType.BufferVideo:
                    rbBufferVideo.Checked = true;
                    break;

                default:
                    rbNone.Checked = true;
                    break;
            }
        }

        private void RadioButton_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton rb = sender as RadioButton;
            if (rb == null || !rb.Checked) return;

            pnlStaticVideo.Visible = (rb == rbStaticVideo);
        }

        private void BtnBrowseVideo_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Seleziona file video";
                ofd.Filter = "Video Files|*.mp4;*. mov;*.avi;*.mkv;*. wmv;*.m4v|All Files|*.*";
                ofd.Multiselect = false;

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtVideoPath.Text = ofd.FileName;

                    // Mostra info file
                    try
                    {
                        FileInfo fi = new FileInfo(ofd.FileName);
                        double sizeMB = fi.Length / (1024.0 * 1024.0);
                        lblVideoInfo.Text = $"✅ {fi.Name} ({sizeMB:F2} MB)";
                        lblVideoInfo.ForeColor = Color.LightGreen;
                    }
                    catch
                    {
                        lblVideoInfo.Text = "✅ File selezionato";
                        lblVideoInfo.ForeColor = Color.LightGreen;
                    }
                }
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (rbNone.Checked)
            {
                SelectedVideoSource = VideoSourceType.None;
                SelectedVideoPath = "";
            }
            else if (rbStaticVideo.Checked)
            {
                if (string.IsNullOrWhiteSpace(txtVideoPath.Text))
                {
                    MessageBox.Show(
                        "⚠️ Seleziona un file video! ",
                        "Video Mancante",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                if (!File.Exists(txtVideoPath.Text))
                {
                    MessageBox.Show(
                        "❌ Il file video selezionato non esiste!",
                        "File Non Trovato",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                SelectedVideoSource = VideoSourceType.StaticVideo;
                SelectedVideoPath = txtVideoPath.Text;
            }
            else if (rbBufferVideo.Checked)
            {
                string bufferPath = ConfigurationControl.GetBufferVideoPath();

                if (string.IsNullOrWhiteSpace(bufferPath) || !Directory.Exists(bufferPath))
                {
                    MessageBox.Show(
                        "⚠️ Cartella video tampone non configurata!\n\n" +
                        "Vai in Configurazione → Video → Cartella Video Tampone",
                        "Cartella Tampone Mancante",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                SelectedVideoSource = VideoSourceType.BufferVideo;
                SelectedVideoPath = "";
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}