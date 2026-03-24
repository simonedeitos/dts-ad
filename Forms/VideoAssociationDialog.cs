using AirDirector.Controls;
using AirDirector.Services.Database;
using AirDirector.Services.Localization;
using AirDirector.Themes;
using NewTek;
using NewTek.NDI;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AirDirector.Forms
{
    public partial class VideoAssociationDialog : Form
    {
        private readonly bool _isClip;
        private readonly object _entry;

        // UI Controls
        private RadioButton rbNone;
        private RadioButton rbStaticVideo;
        private RadioButton rbNDISource;
        private RadioButton rbBufferVideo;

        private Panel pnlStaticVideo;
        private TextBox txtVideoPath;
        private Button btnBrowseVideo;
        private Label lblVideoInfo;

        private Panel pnlNDI;
        private ComboBox cmbNDISource;
        private Button btnRefreshNDI;
        private Label lblNDIInfo;

        private Button btnSave;
        private Button btnCancel;

        // Result
        public VideoSourceType SelectedVideoSource { get; private set; }
        public string SelectedVideoPath { get; private set; }
        public string SelectedNDISource { get; private set; }

        public VideoAssociationDialog(object entry, bool isClip)
        {
            _entry = entry;
            _isClip = isClip;

            InitializeComponent();
            LoadCurrentSettings();
        }

        private void InitializeComponent()
        {
            this.Text = "🎬 Associa Video/NDI";
            this.Size = new Size(600, 550);
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
                Text = "🎬 ASSOCIA VIDEO O SORGENTE NDI",
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
            // NDI (solo per Clips)
            // ═══════════════════════════════════════════════
            if (_isClip)
            {
                rbNDISource = new RadioButton
                {
                    Text = "📡 Sorgente NDI live (telecamera, OBS, vMix, etc.)",
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    ForeColor = Color.White,
                    Location = new Point(30, yPos),
                    AutoSize = true
                };
                rbNDISource.CheckedChanged += RadioButton_CheckedChanged;
                this.Controls.Add(rbNDISource);

                yPos += 35;

                // Panel NDI
                pnlNDI = new Panel
                {
                    Location = new Point(50, yPos),
                    Size = new Size(520, 80),
                    BackColor = Color.FromArgb(50, 50, 50),
                    Visible = false,
                    BorderStyle = BorderStyle.FixedSingle
                };

                cmbNDISource = new ComboBox
                {
                    Location = new Point(10, 10),
                    Size = new Size(420, 25),
                    Font = new Font("Segoe UI", 9),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    BackColor = Color.FromArgb(40, 40, 40),
                    ForeColor = Color.White
                };
                pnlNDI.Controls.Add(cmbNDISource);

                btnRefreshNDI = new Button
                {
                    Text = "🔄",
                    Location = new Point(440, 8),
                    Size = new Size(70, 28),
                    BackColor = Color.FromArgb(0, 120, 215),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 12, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnRefreshNDI.FlatAppearance.BorderSize = 0;
                btnRefreshNDI.Click += BtnRefreshNDI_Click;
                pnlNDI.Controls.Add(btnRefreshNDI);

                lblNDIInfo = new Label
                {
                    Text = "ℹ️ Latenza: ~8-16ms | Assicurati che la sorgente NDI sia attiva",
                    Location = new Point(10, 45),
                    Size = new Size(500, 20),
                    Font = new Font("Segoe UI", 8),
                    ForeColor = Color.LightGray
                };
                pnlNDI.Controls.Add(lblNDIInfo);

                this.Controls.Add(pnlNDI);

                yPos += 90;
            }

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
                Location = new Point(350, 460),
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
                Location = new Point(470, 460),
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
            string currentNDISourceName = "";

            if (_entry is MusicEntry musicEntry)
            {
                currentSource = musicEntry.VideoSource;
                currentVideoPath = musicEntry.VideoFilePath ?? "";
                currentNDISourceName = musicEntry.NDISourceName ?? "";
            }
            else if (_entry is ClipEntry clipEntry)
            {
                currentSource = clipEntry.VideoSource;
                currentVideoPath = clipEntry.VideoFilePath ?? "";
                currentNDISourceName = clipEntry.NDISourceName ?? "";
            }

            // Imposta radio button corrente
            switch (currentSource)
            {
                case VideoSourceType.None:
                    rbNone.Checked = true;
                    break;

                case VideoSourceType.StaticVideo:
                    rbStaticVideo.Checked = true;
                    txtVideoPath.Text = currentVideoPath;
                    break;

                case VideoSourceType.NDISource:
                    if (_isClip && rbNDISource != null)
                    {
                        rbNDISource.Checked = true;
                        LoadNDISources();
                        SelectNDISource(currentNDISourceName);
                    }
                    break;

                case VideoSourceType.BufferVideo:
                    rbBufferVideo.Checked = true;
                    break;
            }
        }

        private void RadioButton_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton rb = sender as RadioButton;
            if (rb == null || !rb.Checked) return;

            // Nascondi tutti i pannelli
            pnlStaticVideo.Visible = false;
            if (pnlNDI != null)
                pnlNDI.Visible = false;

            // Mostra pannello appropriato
            if (rb == rbStaticVideo)
            {
                pnlStaticVideo.Visible = true;
            }
            else if (rb == rbNDISource && _isClip)
            {
                pnlNDI.Visible = true;
                if (cmbNDISource.Items.Count == 0)
                    LoadNDISources();
            }
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

        private void BtnRefreshNDI_Click(object sender, EventArgs e)
        {
            LoadNDISources();
        }

        private void LoadNDISources()
        {
            cmbNDISource.Items.Clear();

            try
            {
                Console.WriteLine("[VideoAssociationDialog] 🔍 Ricerca sorgenti NDI...");

                if (!NDIlib.initialize())
                {
                    Console.WriteLine("[VideoAssociationDialog] ❌ NDI non inizializzato");
                    cmbNDISource.Items.Add("(Errore inizializzazione NDI)");
                    cmbNDISource.SelectedIndex = 0;
                    lblNDIInfo.Text = "❌ NDI Runtime non disponibile";
                    lblNDIInfo.ForeColor = Color.Orange;
                    return;
                }

                var findSettings = new NDIlib.find_create_t()
                {
                    show_local_sources = true,
                    p_groups = IntPtr.Zero,
                    p_extra_ips = IntPtr.Zero
                };

                IntPtr findInstance = NDIlib.find_create_v2(ref findSettings);

                if (findInstance == IntPtr.Zero)
                {
                    Console.WriteLine("[VideoAssociationDialog] ⚠️ Impossibile creare finder");
                    cmbNDISource.Items.Add("(Errore creazione finder)");
                    cmbNDISource.SelectedIndex = 0;
                    return;
                }

                // Aspetta discovery
                System.Threading.Thread.Sleep(1500);

                uint numSources = 0;
                IntPtr sourcesPtr = NDIlib.find_get_current_sources(findInstance, ref numSources);

                Console.WriteLine($"[VideoAssociationDialog] 📡 Trovate {numSources} sorgenti NDI");

                if (numSources == 0)
                {
                    cmbNDISource.Items.Add("(Nessuna sorgente NDI trovata)");
                    lblNDIInfo.Text = "⚠️ Nessuna sorgente rilevata - Verifica che sia attiva";
                    lblNDIInfo.ForeColor = Color.Orange;
                }
                else
                {
                    int stride = Marshal.SizeOf(typeof(NDIlib.source_t));

                    for (int i = 0; i < numSources; i++)
                    {
                        IntPtr sourcePtr = IntPtr.Add(sourcesPtr, i * stride);
                        NDIlib.source_t source = Marshal.PtrToStructure<NDIlib.source_t>(sourcePtr);

                        string sourceName = Marshal.PtrToStringAnsi(source.p_ndi_name);

                        if (!string.IsNullOrEmpty(sourceName))
                        {
                            cmbNDISource.Items.Add(sourceName);
                            Console.WriteLine($"[VideoAssociationDialog]   ✓ {sourceName}");
                        }
                    }

                    lblNDIInfo.Text = $"✅ {numSources} sorgente/i NDI disponibile/i";
                    lblNDIInfo.ForeColor = Color.LightGreen;
                }

                NDIlib.find_destroy(findInstance);

                if (cmbNDISource.Items.Count > 0)
                    cmbNDISource.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VideoAssociationDialog] ❌ Errore:  {ex.Message}");
                cmbNDISource.Items.Clear();
                cmbNDISource.Items.Add($"(Errore: {ex.Message})");
                cmbNDISource.SelectedIndex = 0;
                lblNDIInfo.Text = "❌ Errore rilevamento sorgenti NDI";
                lblNDIInfo.ForeColor = Color.Red;
            }
        }

        private void SelectNDISource(string sourceName)
        {
            if (string.IsNullOrEmpty(sourceName)) return;

            for (int i = 0; i < cmbNDISource.Items.Count; i++)
            {
                if (cmbNDISource.Items[i].ToString() == sourceName)
                {
                    cmbNDISource.SelectedIndex = i;
                    return;
                }
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            // Determina selezione
            if (rbNone.Checked)
            {
                SelectedVideoSource = VideoSourceType.None;
                SelectedVideoPath = "";
                SelectedNDISource = "";
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
                SelectedNDISource = "";
            }
            else if (_isClip && rbNDISource != null && rbNDISource.Checked)
            {
                if (cmbNDISource.SelectedIndex < 0 ||
                    cmbNDISource.SelectedItem.ToString().StartsWith("("))
                {
                    MessageBox.Show(
                        "⚠️ Seleziona una sorgente NDI valida!\n\n" +
                        "Verifica che:\n" +
                        "- La sorgente NDI sia attiva\n" +
                        "- NDI Runtime sia installato\n" +
                        "- Clicca 🔄 per aggiornare la lista",
                        "Sorgente NDI Non Valida",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                SelectedVideoSource = VideoSourceType.NDISource;
                SelectedVideoPath = "";
                SelectedNDISource = cmbNDISource.SelectedItem.ToString();
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
                SelectedNDISource = "";
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}