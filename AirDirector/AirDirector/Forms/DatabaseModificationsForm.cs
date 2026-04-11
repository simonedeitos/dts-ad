using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using AirDirector.Services.Database;
using AirDirector.Services.Localization;
using Microsoft.Win32;

namespace AirDirector.Forms
{
    public partial class DatabaseModificationsForm : Form
    {
        // Stores results for the Find & Replace tab so Sostituisci can act on them
        private List<FindReplaceResult> _findReplaceResults = new List<FindReplaceResult>();

        public DatabaseModificationsForm()
        {
            InitializeComponent();

            // Wire events
            btnVerify.Click += BtnVerify_Click;
            btnAnalyze.Click += BtnAnalyze_Click;
            btnReplaceAll.Click += BtnReplaceAll_Click;

            ApplyLanguage();
            ApplyDarkTheme();

            LanguageManager.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object sender, EventArgs e) => ApplyLanguage();

        private void ApplyLanguage()
        {
            this.Text = "🔧 " + LanguageManager.GetString("DatabaseModifications.Title", "Modifiche Database");
            tabVerify.Text = LanguageManager.GetString("DatabaseModifications.TabVerifyAudioFiles", "Verifica Esistenza File Audio");
            tabFindReplace.Text = LanguageManager.GetString("DatabaseModifications.TabFindReplace", "Trova e Sostituisci Percorso");
            chkVerifyMusic.Text = LanguageManager.GetString("DatabaseModifications.VerifyMusic", "Music");
            chkVerifyClips.Text = LanguageManager.GetString("DatabaseModifications.VerifyClips", "Clips");
            btnVerify.Text = "🔍 " + LanguageManager.GetString("DatabaseModifications.BtnVerify", "Verifica");
            colMissingArtist.HeaderText = LanguageManager.GetString("DatabaseModifications.ColumnArtist", "Artista");
            colMissingTitle.HeaderText = LanguageManager.GetString("DatabaseModifications.ColumnTitle", "Titolo");
            colMissingPath.HeaderText = LanguageManager.GetString("DatabaseModifications.ColumnFilePath", "Percorso File");
            chkFRMusic.Text = LanguageManager.GetString("DatabaseModifications.FlagMusic", "Music");
            chkFRClips.Text = LanguageManager.GetString("DatabaseModifications.FlagClips", "Clips");
            chkFRSettings.Text = LanguageManager.GetString("DatabaseModifications.FlagSettings", "Settings");
            chkFRDatabase.Text = LanguageManager.GetString("DatabaseModifications.FlagDatabase", "Database");
            lblFind.Text = LanguageManager.GetString("DatabaseModifications.FindLabel", "Trova:");
            lblReplace.Text = LanguageManager.GetString("DatabaseModifications.ReplaceLabel", "Sostituisci:");
            btnAnalyze.Text = "🔍 " + LanguageManager.GetString("DatabaseModifications.BtnAnalyze", "Analizza");
            btnReplaceAll.Text = "✏️ " + LanguageManager.GetString("DatabaseModifications.BtnReplace", "Sostituisci");
            colSource.HeaderText = LanguageManager.GetString("DatabaseModifications.ColumnSource", "Sorgente");
            colCurrentPath.HeaderText = LanguageManager.GetString("DatabaseModifications.ColumnCurrentPath", "Percorso Attuale");
            colProposedPath.HeaderText = LanguageManager.GetString("DatabaseModifications.ColumnProposedPath", "Percorso Proposto");
        }

        private void ApplyDarkTheme()
        {
            // TabControl styling
            tabMain.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabMain.DrawItem += TabMain_DrawItem;
            tabMain.BackColor = Color.FromArgb(28, 28, 28);
        }

        private void TabMain_DrawItem(object sender, DrawItemEventArgs e)
        {
            TabPage page = tabMain.TabPages[e.Index];
            bool selected = (e.Index == tabMain.SelectedIndex);
            Color bgColor = selected ? Color.FromArgb(40, 40, 40) : Color.FromArgb(30, 30, 30);
            Color fgColor = selected ? Color.White : Color.FromArgb(180, 180, 180);
            using (SolidBrush bgBrush = new SolidBrush(bgColor))
                e.Graphics.FillRectangle(bgBrush, e.Bounds);
            TextRenderer.DrawText(e.Graphics, page.Text, new Font("Segoe UI", 9, FontStyle.Bold),
                e.Bounds, fgColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        // ─────────────────────────────────────────────────────────────
        // Tab 1: Verify Audio Files
        // ─────────────────────────────────────────────────────────────

        private async void BtnVerify_Click(object sender, EventArgs e)
        {
            if (!chkVerifyMusic.Checked && !chkVerifyClips.Checked)
            {
                MessageBox.Show(
                    LanguageManager.GetString("DatabaseModifications.SelectAtLeastOneFlag", "Seleziona almeno un'opzione!"),
                    LanguageManager.GetString("Common.Warning", "Attenzione"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnVerify.Enabled = false;
            dgvMissing.Rows.Clear();
            progressVerify.Value = 0;
            lblVerifyStatus.Text = "";

            try
            {
                var missing = await Task.Run(() => FindMissingFiles(chkVerifyMusic.Checked, chkVerifyClips.Checked));

                if (InvokeRequired)
                    Invoke(new Action(() => PopulateVerifyGrid(missing)));
                else
                    PopulateVerifyGrid(missing);
            }
            finally
            {
                btnVerify.Enabled = true;
            }
        }

        private List<MissingFileResult> FindMissingFiles(bool checkMusic, bool checkClips)
        {
            var results = new List<MissingFileResult>();

            // Build total count for progress
            var musicEntries = checkMusic ? DbcManager.LoadFromCsv<MusicEntry>("Music.dbc") : new List<MusicEntry>();
            var clipEntries = checkClips ? DbcManager.LoadFromCsv<ClipEntry>("Clips.dbc") : new List<ClipEntry>();

            int total = musicEntries.Count + clipEntries.Count;
            int processed = 0;

            foreach (var entry in musicEntries)
            {
                processed++;
                if (!string.IsNullOrEmpty(entry.FilePath) && !File.Exists(entry.FilePath))
                    results.Add(new MissingFileResult { Artist = entry.Artist, Title = entry.Title, FilePath = entry.FilePath });

                int pct = total > 0 ? (processed * 100 / total) : 0;
                string status = string.Format(
                    LanguageManager.GetString("DatabaseModifications.VerifyProgress", "Verifica in corso... {0}/{1}"),
                    processed, total);
                UpdateVerifyProgress(pct, status);
            }

            foreach (var entry in clipEntries)
            {
                processed++;
                if (!string.IsNullOrEmpty(entry.FilePath) && !File.Exists(entry.FilePath))
                    results.Add(new MissingFileResult { Artist = "", Title = entry.Title, FilePath = entry.FilePath });

                int pct = total > 0 ? (processed * 100 / total) : 0;
                string status = string.Format(
                    LanguageManager.GetString("DatabaseModifications.VerifyProgress", "Verifica in corso... {0}/{1}"),
                    processed, total);
                UpdateVerifyProgress(pct, status);
            }

            return results;
        }

        private void UpdateVerifyProgress(int pct, string status)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    progressVerify.Value = Math.Min(pct, 100);
                    lblVerifyStatus.Text = status;
                }));
            }
            else
            {
                progressVerify.Value = Math.Min(pct, 100);
                lblVerifyStatus.Text = status;
            }
        }

        private void PopulateVerifyGrid(List<MissingFileResult> missing)
        {
            dgvMissing.Rows.Clear();
            int totalChecked = (chkVerifyMusic.Checked ? DbcManager.LoadFromCsv<MusicEntry>("Music.dbc").Count : 0)
                             + (chkVerifyClips.Checked ? DbcManager.LoadFromCsv<ClipEntry>("Clips.dbc").Count : 0);

            foreach (var r in missing)
                dgvMissing.Rows.Add(r.Artist, r.Title, r.FilePath);

            progressVerify.Value = 100;
            lblVerifyStatus.Text = string.Format(
                LanguageManager.GetString("DatabaseModifications.VerifyComplete", "Verifica completata: {0} file verificati, {1} mancanti"),
                totalChecked, missing.Count);
        }

        // ─────────────────────────────────────────────────────────────
        // Tab 2: Find & Replace
        // ─────────────────────────────────────────────────────────────

        private void BtnAnalyze_Click(object sender, EventArgs e)
        {
            if (!chkFRMusic.Checked && !chkFRClips.Checked && !chkFRSettings.Checked && !chkFRDatabase.Checked)
            {
                MessageBox.Show(
                    LanguageManager.GetString("DatabaseModifications.SelectAtLeastOneFlag", "Seleziona almeno un'opzione!"),
                    LanguageManager.GetString("Common.Warning", "Attenzione"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string searchText = txtFind.Text;
            if (string.IsNullOrEmpty(searchText))
            {
                MessageBox.Show(
                    LanguageManager.GetString("DatabaseModifications.EnterSearchText", "Inserisci il testo da cercare!"),
                    LanguageManager.GetString("Common.Warning", "Attenzione"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string replaceText = txtReplace.Text ?? "";
            _findReplaceResults = BuildFindReplaceResults(searchText, replaceText);
            PopulateFindReplaceGrid();
        }

        private List<FindReplaceResult> BuildFindReplaceResults(string searchText, string replaceText)
        {
            var results = new List<FindReplaceResult>();

            if (chkFRMusic.Checked)
            {
                var entries = DbcManager.LoadFromCsv<MusicEntry>("Music.dbc");
                foreach (var entry in entries)
                {
                    if (!string.IsNullOrEmpty(entry.FilePath) && entry.FilePath.Contains(searchText))
                        results.Add(new FindReplaceResult
                        {
                            Source = "Music",
                            CurrentPath = entry.FilePath,
                            ProposedPath = entry.FilePath.Replace(searchText, replaceText),
                            EntryId = entry.ID,
                            FieldName = "FilePath"
                        });
                    if (!string.IsNullOrEmpty(entry.VideoFilePath) && entry.VideoFilePath.Contains(searchText))
                        results.Add(new FindReplaceResult
                        {
                            Source = "Music (Video)",
                            CurrentPath = entry.VideoFilePath,
                            ProposedPath = entry.VideoFilePath.Replace(searchText, replaceText),
                            EntryId = entry.ID,
                            FieldName = "VideoFilePath"
                        });
                }
            }

            if (chkFRClips.Checked)
            {
                var entries = DbcManager.LoadFromCsv<ClipEntry>("Clips.dbc");
                foreach (var entry in entries)
                {
                    if (!string.IsNullOrEmpty(entry.FilePath) && entry.FilePath.Contains(searchText))
                        results.Add(new FindReplaceResult
                        {
                            Source = "Clips",
                            CurrentPath = entry.FilePath,
                            ProposedPath = entry.FilePath.Replace(searchText, replaceText),
                            EntryId = entry.ID,
                            FieldName = "FilePath"
                        });
                    if (!string.IsNullOrEmpty(entry.VideoFilePath) && entry.VideoFilePath.Contains(searchText))
                        results.Add(new FindReplaceResult
                        {
                            Source = "Clips (Video)",
                            CurrentPath = entry.VideoFilePath,
                            ProposedPath = entry.VideoFilePath.Replace(searchText, replaceText),
                            EntryId = entry.ID,
                            FieldName = "VideoFilePath"
                        });
                }
            }

            if (chkFRSettings.Checked)
            {
                // Scan Registry key HKCU\SOFTWARE\AirDirector
                try
                {
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AirDirector", false))
                    {
                        if (key != null)
                        {
                            foreach (string valueName in key.GetValueNames())
                            {
                                string val = key.GetValue(valueName)?.ToString() ?? "";
                                if (val.Contains(searchText))
                                    results.Add(new FindReplaceResult
                                    {
                                        Source = "Registry",
                                        CurrentPath = $"{valueName}={val}",
                                        ProposedPath = $"{valueName}={val.Replace(searchText, replaceText)}",
                                        EntryId = 0,
                                        FieldName = valueName
                                    });
                            }
                        }
                    }
                }
                catch { }

                // Scan Config.dbc settings entries
                try
                {
                    var configEntries = DbcManager.LoadFromCsv<ConfigEntry>("Config.dbc");
                    foreach (var entry in configEntries)
                    {
                        if (!string.IsNullOrEmpty(entry.Value) && entry.Value.Contains(searchText))
                            results.Add(new FindReplaceResult
                            {
                                Source = "Settings (Config.dbc)",
                                CurrentPath = $"{entry.Key}={entry.Value}",
                                ProposedPath = $"{entry.Key}={entry.Value.Replace(searchText, replaceText)}",
                                EntryId = 0,
                                FieldName = entry.Key
                            });
                    }
                }
                catch { }
            }

            if (chkFRDatabase.Checked)
            {
                // Scan all .dbc files in database folder
                try
                {
                    string dbPath = DbcManager.GetDatabasePath();
                    var dbcFiles = Directory.GetFiles(dbPath, "*.dbc");
                    foreach (string dbcFile in dbcFiles)
                    {
                        string fileName = Path.GetFileName(dbcFile);
                        // Skip Music and Clips as they are handled separately above if selected
                        try
                        {
                            string[] lines = File.ReadAllLines(dbcFile);
                            for (int i = 1; i < lines.Length; i++) // skip header
                            {
                                string line = lines[i];
                                if (line.Contains(searchText))
                                    results.Add(new FindReplaceResult
                                    {
                                        Source = $"DB:{fileName}",
                                        CurrentPath = line,
                                        ProposedPath = line.Replace(searchText, replaceText),
                                        EntryId = i,
                                        FieldName = $"line:{i}"
                                    });
                            }
                        }
                        catch { }
                    }
                }
                catch { }
            }

            return results;
        }

        private void PopulateFindReplaceGrid()
        {
            dgvResults.Rows.Clear();
            foreach (var r in _findReplaceResults)
                dgvResults.Rows.Add(r.Source, r.CurrentPath, r.ProposedPath);

            if (_findReplaceResults.Count == 0)
            {
                lblOccurrences.ForeColor = Color.FromArgb(255, 152, 0);
                lblOccurrences.Text = LanguageManager.GetString("DatabaseModifications.NoOccurrencesFound", "Nessuna occorrenza trovata");
            }
            else
            {
                lblOccurrences.ForeColor = Color.FromArgb(0, 200, 83);
                lblOccurrences.Text = string.Format(
                    LanguageManager.GetString("DatabaseModifications.OccurrencesFound", "Occorrenze trovate: {0}"),
                    _findReplaceResults.Count);
            }
        }

        private void BtnReplaceAll_Click(object sender, EventArgs e)
        {
            if (_findReplaceResults.Count == 0)
            {
                MessageBox.Show(
                    LanguageManager.GetString("DatabaseModifications.NoOccurrencesFound", "Nessuna occorrenza trovata"),
                    LanguageManager.GetString("Common.Info", "Informazione"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string confirmMsg = string.Format(
                LanguageManager.GetString("DatabaseModifications.ConfirmReplace", "Sei sicuro di voler procedere con la sostituzione di {0} occorrenze?"),
                _findReplaceResults.Count);

            if (MessageBox.Show(confirmMsg,
                LanguageManager.GetString("Common.Confirm", "Conferma"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            int replaced = 0;
            string searchText = txtFind.Text;
            string replaceText = txtReplace.Text ?? "";

            try
            {
                replaced += ApplyMusicReplacements(searchText, replaceText);
                replaced += ApplyClipsReplacements(searchText, replaceText);
                replaced += ApplySettingsReplacements(searchText, replaceText);
                replaced += ApplyDatabaseReplacements(searchText, replaceText);

                _findReplaceResults.Clear();
                dgvResults.Rows.Clear();

                lblOccurrences.ForeColor = Color.FromArgb(0, 200, 83);
                lblOccurrences.Text = string.Format(
                    LanguageManager.GetString("DatabaseModifications.ReplaceComplete", "✅ Sostituzione completata: {0} occorrenze sostituite"),
                    replaced);

                MessageBox.Show(lblOccurrences.Text,
                    LanguageManager.GetString("Common.Success", "Successo"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message,
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private int ApplyMusicReplacements(string searchText, string replaceText)
        {
            if (!chkFRMusic.Checked) return 0;
            int count = 0;
            var entries = DbcManager.LoadFromCsv<MusicEntry>("Music.dbc");
            bool changed = false;
            foreach (var entry in entries)
            {
                if (!string.IsNullOrEmpty(entry.FilePath) && entry.FilePath.Contains(searchText))
                { entry.FilePath = entry.FilePath.Replace(searchText, replaceText); changed = true; count++; }
                if (!string.IsNullOrEmpty(entry.VideoFilePath) && entry.VideoFilePath.Contains(searchText))
                { entry.VideoFilePath = entry.VideoFilePath.Replace(searchText, replaceText); changed = true; count++; }
            }
            if (changed) DbcManager.SaveToCsv("Music.dbc", entries);
            return count;
        }

        private int ApplyClipsReplacements(string searchText, string replaceText)
        {
            if (!chkFRClips.Checked) return 0;
            int count = 0;
            var entries = DbcManager.LoadFromCsv<ClipEntry>("Clips.dbc");
            bool changed = false;
            foreach (var entry in entries)
            {
                if (!string.IsNullOrEmpty(entry.FilePath) && entry.FilePath.Contains(searchText))
                { entry.FilePath = entry.FilePath.Replace(searchText, replaceText); changed = true; count++; }
                if (!string.IsNullOrEmpty(entry.VideoFilePath) && entry.VideoFilePath.Contains(searchText))
                { entry.VideoFilePath = entry.VideoFilePath.Replace(searchText, replaceText); changed = true; count++; }
            }
            if (changed) DbcManager.SaveToCsv("Clips.dbc", entries);
            return count;
        }

        private int ApplySettingsReplacements(string searchText, string replaceText)
        {
            if (!chkFRSettings.Checked) return 0;
            int count = 0;

            // Registry
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AirDirector", true))
                {
                    if (key != null)
                    {
                        foreach (string valueName in key.GetValueNames())
                        {
                            string val = key.GetValue(valueName)?.ToString() ?? "";
                            if (val.Contains(searchText))
                            {
                                key.SetValue(valueName, val.Replace(searchText, replaceText));
                                count++;
                            }
                        }
                    }
                }
            }
            catch { }

            // Config.dbc
            try
            {
                var configEntries = DbcManager.LoadFromCsv<ConfigEntry>("Config.dbc");
                bool changed = false;
                foreach (var entry in configEntries)
                {
                    if (!string.IsNullOrEmpty(entry.Value) && entry.Value.Contains(searchText))
                    { entry.Value = entry.Value.Replace(searchText, replaceText); changed = true; count++; }
                }
                if (changed) DbcManager.SaveToCsv("Config.dbc", configEntries);
            }
            catch { }

            return count;
        }

        private int ApplyDatabaseReplacements(string searchText, string replaceText)
        {
            if (!chkFRDatabase.Checked) return 0;
            int count = 0;
            try
            {
                string dbPath = DbcManager.GetDatabasePath();
                var dbcFiles = Directory.GetFiles(dbPath, "*.dbc");
                foreach (string dbcFile in dbcFiles)
                {
                    try
                    {
                        string[] lines = File.ReadAllLines(dbcFile);
                        bool changed = false;
                        for (int i = 1; i < lines.Length; i++)
                        {
                            if (lines[i].Contains(searchText))
                            {
                                lines[i] = lines[i].Replace(searchText, replaceText);
                                changed = true;
                                count++;
                            }
                        }
                        if (changed) File.WriteAllLines(dbcFile, lines);
                    }
                    catch { }
                }
            }
            catch { }
            return count;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                LanguageManager.LanguageChanged -= OnLanguageChanged;
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        // ─────────────────────────────────────────────────────────────
        // Helper types
        // ─────────────────────────────────────────────────────────────

        private class MissingFileResult
        {
            public string Artist { get; set; } = "";
            public string Title { get; set; } = "";
            public string FilePath { get; set; } = "";
        }

        private class FindReplaceResult
        {
            public string Source { get; set; } = "";
            public string CurrentPath { get; set; } = "";
            public string ProposedPath { get; set; } = "";
            public int EntryId { get; set; }
            public string FieldName { get; set; } = "";
        }
    }
}
