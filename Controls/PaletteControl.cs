using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using AirDirector.Services.Database;
using AirDirector.Services.Localization;
using AirDirector.Themes;
using NAudio.Wave;

namespace AirDirector.Controls
{
    public partial class PaletteControl : UserControl
    {
        private const int COLUMNS = 4;
        private const int ROWS = 4;
        private const int TOTAL_BUTTONS = COLUMNS * ROWS;
        private List<JingleButton> _jingleButtons;
        private ComboBox cboPresets;
        private Button btnSavePreset;
        private Button btnDeletePreset;
        private Button btnLoadFiles;
        private TrackBar trackVolume;
        private CheckBox chkStopOthers;
        private Panel mainPanel;
        private Label lblTitle;
        private Label lblPreset;
        private Label lblVolumeLabel;
        private string _paletteOutputDevice;
        private float _globalVolume = 0.8f;

        public PaletteControl()
        {
            InitializeComponent();
            InitializeUI();
            LoadPaletteOutput();
            ApplyLanguage();
            LoadPresets();

            LanguageManager.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            if (lblTitle != null)
                lblTitle.Text = "🎵 " + LanguageManager.GetString("Palette.Title", "JINGLE PALETTE");

            if (lblPreset != null)
                lblPreset.Text = LanguageManager.GetString("Palette.Preset", "Preset:");

            if (btnSavePreset != null)
                btnSavePreset.Text = "💾 " + LanguageManager.GetString("Common.Save", "Salva");

            if (btnLoadFiles != null)
                btnLoadFiles.Text = "📂 " + LanguageManager.GetString("Palette.LoadFiles", "Carica Files");

            if (chkStopOthers != null)
                chkStopOthers.Text = "⏹️ " + LanguageManager.GetString("Palette.StopOthers", "Stop altri al play");

            if (lblVolumeLabel != null)
                lblVolumeLabel.Text = "🔊 " + LanguageManager.GetString("Palette.Volume", "Volume:");

            if (cboPresets != null && cboPresets.Items.Count > 0 && cboPresets.Items[0].ToString().StartsWith("--"))
                cboPresets.Items[0] = "-- " + LanguageManager.GetString("Palette.NewPreset", "Nuovo Preset") + " --";
        }

        private void LoadPaletteOutput()
        {
            try
            {
                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AirDirector"))
                {
                    _paletteOutputDevice = key?.GetValue("PaletteOutput") as string ?? "";
                }
            }
            catch
            {
                _paletteOutputDevice = "";
            }
        }

        private void InitializeUI()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = AppTheme.BgLight;
            this.Padding = new Padding(0);
            this.AutoScroll = true;

            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = AppTheme.BgDark,
                Padding = new Padding(15, 10, 15, 10)
            };
            this.Controls.Add(headerPanel);

            lblTitle = new Label
            {
                Text = "🎵 JINGLE PALETTE",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 12),
                AutoSize = true
            };
            headerPanel.Controls.Add(lblTitle);

            lblPreset = new Label
            {
                Text = "Preset:",
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = Color.LightGray,
                Location = new Point(15, 45),
                AutoSize = true
            };
            headerPanel.Controls.Add(lblPreset);

            cboPresets = new ComboBox
            {
                Location = new Point(70, 42),
                Size = new Size(150, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White
            };
            cboPresets.SelectedIndexChanged += CboPresets_SelectedIndexChanged;
            headerPanel.Controls.Add(cboPresets);

            btnSavePreset = new Button
            {
                Text = "💾 Salva",
                Size = new Size(75, 28),
                Location = new Point(230, 41),
                BackColor = Color.FromArgb(0, 150, 136),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSavePreset.FlatAppearance.BorderSize = 0;
            btnSavePreset.Click += BtnSavePreset_Click;
            headerPanel.Controls.Add(btnSavePreset);

            btnDeletePreset = new Button
            {
                Text = "🗑️",
                Size = new Size(30, 28),
                Location = new Point(310, 41),
                BackColor = AppTheme.Danger,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnDeletePreset.FlatAppearance.BorderSize = 0;
            btnDeletePreset.Click += BtnDeletePreset_Click;
            headerPanel.Controls.Add(btnDeletePreset);

            btnLoadFiles = new Button
            {
                Text = "📂 Carica Files",
                Size = new Size(120, 28),
                Location = new Point(350, 41),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLoadFiles.FlatAppearance.BorderSize = 0;
            btnLoadFiles.Click += BtnLoadFiles_Click;
            headerPanel.Controls.Add(btnLoadFiles);

            chkStopOthers = new CheckBox
            {
                Text = "⏹️ Stop altri al play",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Checked = true,
                AutoSize = true,
                Location = new Point(540, 18)
            };
            headerPanel.Controls.Add(chkStopOthers);

            lblVolumeLabel = new Label
            {
                Text = "🔊 Volume:",
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = Color.LightGray,
                AutoSize = true,
                Location = new Point(500, 45)
            };
            headerPanel.Controls.Add(lblVolumeLabel);

            trackVolume = new TrackBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = 80,
                TickFrequency = 10,
                Size = new Size(140, 45),
                Location = new Point(550, 45)
            };
            trackVolume.ValueChanged += TrackVolume_ValueChanged;
            headerPanel.Controls.Add(trackVolume);

            mainPanel = new Panel
            {
                Location = new Point(0, 80),
                Size = new Size(this.Width, this.Height - 80),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = AppTheme.BgLight,
                AutoScroll = false,
                Padding = new Padding(10)
            };
            this.Controls.Add(mainPanel);

            this.Resize += (s, e) =>
            {
                mainPanel.Size = new Size(this.Width, this.Height - 80);
                RepositionButtons();
            };

            CreateJingleButtons();
        }

        private void CreateJingleButtons()
        {
            _jingleButtons = new List<JingleButton>();

            for (int i = 0; i < TOTAL_BUTTONS; i++)
            {
                var jBtn = new JingleButton(i, this);
                _jingleButtons.Add(jBtn);
                mainPanel.Controls.Add(jBtn.Panel);
            }

            RepositionButtons();
        }

        private void RepositionButtons()
        {
            int panelWidth = mainPanel.Width - 20;
            int panelHeight = mainPanel.Height - 20;

            int columnWidth = panelWidth / COLUMNS;
            int rowHeight = panelHeight / ROWS;

            int margin = 8;

            for (int i = 0; i < _jingleButtons.Count; i++)
            {
                int col = i % COLUMNS;
                int row = i / COLUMNS;

                int x = col * columnWidth + margin;
                int y = row * rowHeight + margin;
                int width = columnWidth - margin * 2;
                int height = rowHeight - margin * 2;

                _jingleButtons[i].Panel.Location = new Point(x, y);
                _jingleButtons[i].Panel.Size = new Size(width, height);
                _jingleButtons[i].UpdateLayout();
            }
        }

        private void TrackVolume_ValueChanged(object sender, EventArgs e)
        {
            _globalVolume = trackVolume.Value / 100f;

            foreach (var jBtn in _jingleButtons)
            {
                jBtn.UpdateVolume();
            }
        }

        public string GetOutputDevice() => _paletteOutputDevice;
        public float GetGlobalVolume() => _globalVolume;
        public bool StopOthersOnPlay() => chkStopOthers.Checked;

        public void StopAllExcept(JingleButton except)
        {
            foreach (var jBtn in _jingleButtons)
            {
                if (jBtn != except)
                {
                    jBtn.Stop();
                }
            }
        }

        private void BtnLoadFiles_Click(object sender, EventArgs e)
        {
            Form loadForm = new Form
            {
                Text = LanguageManager.GetString("Palette.LoadJingles", "Carica Files Jingle"),
                Size = new Size(700, 680),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.Sizable,
                BackColor = Color.FromArgb(30, 30, 30),
                MinimizeBox = false,
                MaximizeBox = true
            };

            Panel headerLoadPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(138, 43, 226)
            };
            loadForm.Controls.Add(headerLoadPanel);

            Label lblLoadTitle = new Label
            {
                Text = "📂 " + LanguageManager.GetString("Palette.LoadJinglesTitle", "CARICA FILES JINGLE"),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 18),
                AutoSize = true
            };
            headerLoadPanel.Controls.Add(lblLoadTitle);

            Button btnSave = new Button
            {
                Text = "✓ " + LanguageManager.GetString("Common.Save", "SALVA"),
                Size = new Size(120, 35),
                BackColor = Color.FromArgb(0, 180, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            headerLoadPanel.Controls.Add(btnSave);

            Button btnCancel = new Button
            {
                Text = "✕ " + LanguageManager.GetString("Common.Cancel", "ANNULLA"),
                Size = new Size(120, 35),
                BackColor = Color.FromArgb(80, 80, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, ev) => loadForm.Close();
            headerLoadPanel.Controls.Add(btnCancel);

            headerLoadPanel.Resize += (s, ev) =>
            {
                int panelWidth = headerLoadPanel.Width;
                btnCancel.Location = new Point(panelWidth - 130, 12);
                btnSave.Location = new Point(panelWidth - 260, 12);
            };
            headerLoadPanel.PerformLayout();
            btnCancel.Location = new Point(headerLoadPanel.Width - 130, 12);
            btnSave.Location = new Point(headerLoadPanel.Width - 260, 12);

            Panel contentPanel = new Panel
            {
                Location = new Point(0, 60),
                Size = new Size(loadForm.Width, loadForm.Height - 100),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(40, 40, 40),
                AutoScroll = true,
                Padding = new Padding(10)
            };
            loadForm.Controls.Add(contentPanel);

            List<TextBox> textBoxes = new List<TextBox>();
            List<Button> browseBtns = new List<Button>();

            for (int i = 0; i < TOTAL_BUTTONS; i++)
            {
                int index = i;

                Label lblNum = new Label
                {
                    Text = string.Format(LanguageManager.GetString("Palette.JingleNumber", "Jingle {0}: "), i + 1),
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    ForeColor = Color.White,
                    Location = new Point(10, 10 + i * 35),
                    Size = new Size(70, 25)
                };
                contentPanel.Controls.Add(lblNum);

                TextBox txtPath = new TextBox
                {
                    Location = new Point(85, 10 + i * 35),
                    Size = new Size(480, 25),
                    BackColor = Color.FromArgb(50, 50, 50),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9),
                    Text = _jingleButtons[i].FilePath
                };
                textBoxes.Add(txtPath);
                contentPanel.Controls.Add(txtPath);

                Button btnBrowse = new Button
                {
                    Text = "📂",
                    Location = new Point(570, 10 + i * 35),
                    Size = new Size(35, 25),
                    BackColor = Color.FromArgb(0, 120, 215),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 10),
                    Cursor = Cursors.Hand
                };
                btnBrowse.FlatAppearance.BorderSize = 0;
                btnBrowse.Click += (s, ev) =>
                {
                    OpenFileDialog ofd = new OpenFileDialog
                    {
                        Filter = LanguageManager.GetString("Palette.AudioFilter", "Audio Files|*.mp3;*.wav;*.flac;*.aac"),
                        Title = LanguageManager.GetString("Palette.SelectJingle", "Seleziona Jingle")
                    };
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        textBoxes[index].Text = ofd.FileName;
                    }
                };
                browseBtns.Add(btnBrowse);
                contentPanel.Controls.Add(btnBrowse);
            }

            btnSave.Click += (s, ev) =>
            {
                for (int i = 0; i < TOTAL_BUTTONS; i++)
                {
                    string path = textBoxes[i].Text.Trim();
                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    {
                        _jingleButtons[i].FilePath = path;
                        _jingleButtons[i].UpdateTitle();
                    }
                }
                MessageBox.Show(
                    LanguageManager.GetString("Palette.FilesLoaded", "✅ Files caricati con successo! "),
                    LanguageManager.GetString("Common.Success", "Successo"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                loadForm.Close();
            };

            loadForm.ShowDialog();
        }

        private void LoadPresets()
        {
            try
            {
                var presets = DbcManager.LoadFromCsv<PalettePresetEntry>("Palette.dbc");
                cboPresets.Items.Clear();
                cboPresets.Items.Add("-- " + LanguageManager.GetString("Palette.NewPreset", "Nuovo Preset") + " --");
                foreach (var preset in presets.Select(p => p.PresetName).Distinct())
                {
                    cboPresets.Items.Add(preset);
                }
                if (cboPresets.Items.Count > 0)
                    cboPresets.SelectedIndex = 0;
            }
            catch
            {
                cboPresets.Items.Add("-- " + LanguageManager.GetString("Palette.NewPreset", "Nuovo Preset") + " --");
                cboPresets.SelectedIndex = 0;
            }
        }

        private void CboPresets_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboPresets.SelectedIndex <= 0) return;

            string presetName = cboPresets.SelectedItem.ToString();
            LoadPreset(presetName);
        }

        private void LoadPreset(string presetName)
        {
            try
            {
                var presets = DbcManager.LoadFromCsv<PalettePresetEntry>("Palette.dbc");
                var presetData = presets.Where(p => p.PresetName == presetName).ToList();

                foreach (var data in presetData)
                {
                    if (data.ButtonIndex >= 0 && data.ButtonIndex < _jingleButtons.Count)
                    {
                        _jingleButtons[data.ButtonIndex].LoadFromPreset(data);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(LanguageManager.GetString("Palette.LoadPresetError", "Errore caricamento preset:\n{0}"), ex.Message),
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void BtnSavePreset_Click(object sender, EventArgs e)
        {
            string presetName = Microsoft.VisualBasic.Interaction.InputBox(
                LanguageManager.GetString("Palette.PresetNamePrompt", "Nome del preset:"),
                LanguageManager.GetString("Palette.SavePresetTitle", "Salva Preset"),
                LanguageManager.GetString("Palette.NewPreset", "Nuovo Preset"));
            if (string.IsNullOrWhiteSpace(presetName)) return;

            try
            {
                var presets = new List<PalettePresetEntry>();

                try
                {
                    presets = DbcManager.LoadFromCsv<PalettePresetEntry>("Palette.dbc");
                    presets.RemoveAll(p => p.PresetName == presetName);
                }
                catch { }

                for (int i = 0; i < _jingleButtons.Count; i++)
                {
                    var jBtn = _jingleButtons[i];
                    if (!string.IsNullOrEmpty(jBtn.FilePath))
                    {
                        presets.Add(new PalettePresetEntry
                        {
                            PresetName = presetName,
                            ButtonIndex = i,
                            FilePath = jBtn.FilePath,
                            LoopEnabled = jBtn.LoopEnabled ? 1 : 0
                        });
                    }
                }

                DbcManager.SaveToCsv("Palette.dbc", presets);
                LoadPresets();
                cboPresets.SelectedItem = presetName;

                MessageBox.Show(
                    string.Format(LanguageManager.GetString("Palette.PresetSaved", "✅ Preset '{0}' salvato! "), presetName),
                    LanguageManager.GetString("Palette.PresetTitle", "Preset"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(LanguageManager.GetString("Palette.SavePresetError", "Errore salvataggio preset:\n{0}"), ex.Message),
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void BtnDeletePreset_Click(object sender, EventArgs e)
        {
            if (cboPresets.SelectedIndex <= 0)
            {
                MessageBox.Show(
                    LanguageManager.GetString("Palette.SelectPresetToDelete", "Seleziona un preset da eliminare! "),
                    LanguageManager.GetString("Common.Warning", "Attenzione"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            string presetName = cboPresets.SelectedItem.ToString();
            var result = MessageBox.Show(
                string.Format(LanguageManager.GetString("Palette.ConfirmDeletePreset", "Eliminare il preset '{0}'?"), presetName),
                LanguageManager.GetString("Common.Confirm", "Conferma"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            try
            {
                var presets = DbcManager.LoadFromCsv<PalettePresetEntry>("Palette.dbc");
                presets.RemoveAll(p => p.PresetName == presetName);
                DbcManager.SaveToCsv("Palette.dbc", presets);
                LoadPresets();

                MessageBox.Show(
                    string.Format(LanguageManager.GetString("Palette.PresetDeleted", "✅ Preset '{0}' eliminato! "), presetName),
                    LanguageManager.GetString("Palette.PresetTitle", "Preset"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(LanguageManager.GetString("Palette.DeletePresetError", "Errore eliminazione preset:\n{0}"), ex.Message),
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                LanguageManager.SaveMissingKeysToFile();
                LanguageManager.LanguageChanged -= OnLanguageChanged;
            }
            base.Dispose(disposing);
        }
    }

    public class JingleButton
    {
        public Panel Panel;
        public string FilePath = "";
        public bool LoopEnabled = false;

        private Label lblTitle;
        private Button btnPlay;
        private CheckBox chkLoop;
        private ProgressBar progressBar;
        private Label lblCountdown;
        private IWavePlayer _waveOut;
        private AudioFileReader _audioFile;
        private System.Windows.Forms.Timer _timer;
        private PaletteControl _parent;

        public JingleButton(int index, PaletteControl parent)
        {
            _parent = parent;

            Panel = new Panel
            {
                BackColor = Color.FromArgb(45, 45, 45),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(5)
            };

            lblTitle = new Label
            {
                Text = $"Jingle {index + 1}",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                Height = 25,
                Dock = DockStyle.Top
            };
            Panel.Controls.Add(lblTitle);

            btnPlay = new Button
            {
                Text = "▶",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnPlay.FlatAppearance.BorderSize = 0;
            btnPlay.Click += BtnPlay_Click;
            btnPlay.MouseDown += BtnPlay_MouseDown;
            Panel.Controls.Add(btnPlay);

            chkLoop = new CheckBox
            {
                Text = "🔁 Loop",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            chkLoop.CheckedChanged += (s, e) => LoopEnabled = chkLoop.Checked;
            Panel.Controls.Add(chkLoop);

            progressBar = new ProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = 0
            };
            Panel.Controls.Add(progressBar);

            lblCountdown = new Label
            {
                Text = "00:00",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = AppTheme.LEDGreen,
                TextAlign = ContentAlignment.MiddleCenter
            };
            Panel.Controls.Add(lblCountdown);

            _timer = new System.Windows.Forms.Timer { Interval = 100 };
            _timer.Tick += Timer_Tick;
        }

        public void UpdateLayout()
        {
            int width = Panel.Width;
            int height = Panel.Height;

            lblTitle.Width = width;
            lblTitle.Height = 25;
            AdaptTitleFont();

            int playX = 10;
            int playY = 30;
            int playWidth = width - 20;
            int playHeight = height - 80;

            btnPlay.Location = new Point(playX, playY);
            btnPlay.Size = new Size(playWidth, playHeight);

            int progressY = height - 18;
            int loopX = 10;
            int loopY = progressY - 23;
            int loopWidth = width - 20;

            chkLoop.Location = new Point(loopX, loopY);
            chkLoop.Size = new Size(loopWidth, 20);

            int progressX = 10;
            int progressWidth = width - 65;

            progressBar.Location = new Point(progressX, progressY);
            progressBar.Size = new Size(progressWidth, 12);

            int countdownX = width - 52;
            int countdownY = progressY - 2;

            lblCountdown.Location = new Point(countdownX, countdownY);
            lblCountdown.Size = new Size(45, 16);
        }

        private void AdaptTitleFont()
        {
            if (string.IsNullOrEmpty(lblTitle.Text)) return;

            int maxWidth = lblTitle.Width - 10;
            float fontSize = 10f;
            Font testFont = new Font("Segoe UI", fontSize, FontStyle.Bold);

            using (Graphics g = lblTitle.CreateGraphics())
            {
                SizeF textSize = g.MeasureString(lblTitle.Text, testFont);

                while (textSize.Width > maxWidth && fontSize > 6f)
                {
                    fontSize -= 0.5f;
                    testFont = new Font("Segoe UI", fontSize, FontStyle.Bold);
                    textSize = g.MeasureString(lblTitle.Text, testFont);
                }
            }

            lblTitle.Font = testFont;
        }

        public void UpdateTitle()
        {
            if (!string.IsNullOrEmpty(FilePath) && File.Exists(FilePath))
            {
                lblTitle.Text = Path.GetFileNameWithoutExtension(FilePath);
                AdaptTitleFont();

                btnPlay.BackColor = Color.FromArgb(0, 180, 0);
                btnPlay.ForeColor = Color.White;
            }
            else
            {
                btnPlay.BackColor = Color.White;
                btnPlay.ForeColor = Color.Black;
            }
        }

        private void BtnPlay_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                Stop();
            }
        }

        private void BtnPlay_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(FilePath) || !File.Exists(FilePath))
            {
                return;
            }

            if (_waveOut != null && _waveOut.PlaybackState == PlaybackState.Playing)
            {
                Stop();
            }
            else
            {
                Play();
            }
        }

        private void Play()
        {
            try
            {
                if (_parent.StopOthersOnPlay())
                {
                    _parent.StopAllExcept(this);
                }

                _audioFile = new AudioFileReader(FilePath);
                _waveOut = new WaveOutEvent();

                string deviceName = _parent.GetOutputDevice();
                if (!string.IsNullOrEmpty(deviceName))
                {
                    for (int i = 0; i < WaveOut.DeviceCount; i++)
                    {
                        var caps = WaveOut.GetCapabilities(i);
                        if (caps.ProductName.Contains(deviceName))
                        {
                            ((WaveOutEvent)_waveOut).DeviceNumber = i;
                            break;
                        }
                    }
                }

                _audioFile.Volume = _parent.GetGlobalVolume();

                _waveOut.Init(_audioFile);
                _waveOut.PlaybackStopped += WaveOut_PlaybackStopped;
                _waveOut.Play();

                btnPlay.Text = "⏸";
                btnPlay.BackColor = Color.FromArgb(255, 0, 0);
                _timer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(LanguageManager.GetString("Palette.PlaybackError", "Errore riproduzione:\n{0}"), ex.Message),
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        public void Stop()
        {
            _timer.Stop();
            _waveOut?.Stop();
            _waveOut?.Dispose();
            _audioFile?.Dispose();
            _waveOut = null;
            _audioFile = null;

            btnPlay.Text = "▶";

            if (string.IsNullOrEmpty(FilePath) || !File.Exists(FilePath))
            {
                btnPlay.BackColor = Color.White;
                btnPlay.ForeColor = Color.Black;
            }
            else
            {
                btnPlay.BackColor = Color.FromArgb(0, 180, 0);
                btnPlay.ForeColor = Color.White;
            }

            progressBar.Value = 0;
            lblCountdown.Text = "00:00";
        }

        private void WaveOut_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            if (LoopEnabled && !string.IsNullOrEmpty(FilePath))
            {
                Stop();
                Play();
            }
            else
            {
                Stop();
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_audioFile != null && _waveOut != null)
            {
                try
                {
                    double progress = (_audioFile.Position / (double)_audioFile.Length) * 100;
                    progressBar.Value = Math.Min(100, (int)progress);

                    TimeSpan remaining = _audioFile.TotalTime - _audioFile.CurrentTime;

                    int minutes = (int)remaining.TotalMinutes;
                    int seconds = remaining.Seconds;

                    lblCountdown.Text = $"{minutes:D2}:{seconds:D2}";
                }
                catch
                {
                    lblCountdown.Text = "00:00";
                }
            }
        }

        public void UpdateVolume()
        {
            if (_audioFile != null)
            {
                _audioFile.Volume = _parent.GetGlobalVolume();
            }
        }

        public void LoadFromPreset(PalettePresetEntry data)
        {
            FilePath = data.FilePath;
            LoopEnabled = data.LoopEnabled == 1;
            chkLoop.Checked = LoopEnabled;
            UpdateTitle();

            if (!string.IsNullOrEmpty(FilePath) && File.Exists(FilePath))
            {
                btnPlay.BackColor = Color.FromArgb(0, 180, 0);
                btnPlay.ForeColor = Color.White;
            }
            else
            {
                btnPlay.BackColor = Color.White;
                btnPlay.ForeColor = Color.Black;
            }
        }
    }

    public class PalettePresetEntry
    {
        public string PresetName { get; set; }
        public int ButtonIndex { get; set; }
        public string FilePath { get; set; }
        public int LoopEnabled { get; set; }
    }
}