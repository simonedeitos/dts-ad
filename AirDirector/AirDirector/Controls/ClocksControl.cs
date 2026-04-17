using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using AirDirector.Services.Database;
using AirDirector.Services.Licensing;
using AirDirector.Services.Localization;
using AirDirector.Forms;
using AirDirector.Themes;
using AirDirector.Models;
using Newtonsoft.Json;

namespace AirDirector.Controls
{
    public partial class ClocksControl : UserControl
    {
        public event EventHandler<string> StatusChanged;

        public ClocksControl()
        {
            InitializeComponent();
            Console.WriteLine("[ClocksControl] ✅ InitializeComponent completato");

            this.Resize += ClocksControl_Resize;

            ApplyLanguage(); // ✅ APPLICA LINGUA INIZIALE
            RefreshClocks();

            // ✅ SOTTOSCRIVI EVENTO CAMBIO LINGUA
            LanguageManager.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            Console.WriteLine($"[ClocksControl] 🌍 Evento LanguageChanged ricevuto - Nuova lingua: {LanguageManager.GetCurrentLanguage()}");
            ApplyLanguage();
            RefreshClocks(); // ✅ RICARICA LE CARD CON LA NUOVA LINGUA
        }

        /// <summary>
        /// ✅ APPLICA LE TRADUZIONI
        /// </summary>
        private void ApplyLanguage()
        {
            Console.WriteLine($"[ClocksControl] 🌍 ApplyLanguage chiamato - Lingua: {LanguageManager.GetCurrentLanguage()}");

            // ✅ TITOLO PRINCIPALE (se esiste lblTitle o simile)
            if (this.Controls.Find("lblTitle", true).FirstOrDefault() is Label lblTitle)
            {
                lblTitle.Text = LanguageManager.GetString("Clocks.Title", "🕐 GESTIONE CLOCK");
                Console.WriteLine($"[ClocksControl] ✅ lblTitle aggiornato");
            }

            // ✅ AGGIORNA PULSANTI
            if (btnNew != null)
            {
                btnNew.Text = "➕ " + LanguageManager.GetString("Clocks.NewClock", "Nuovo Clock");
                Console.WriteLine($"[ClocksControl] ✅ btnNew aggiornato: {btnNew.Text}");
            }

            if (btnRefresh != null)
            {
                btnRefresh.Text = "🔄 " + LanguageManager.GetString("Clocks.Refresh", "Aggiorna");
                Console.WriteLine($"[ClocksControl] ✅ btnRefresh aggiornato: {btnRefresh.Text}");
            }

            // ✅ AGGIORNA LABEL "Predefinito:" (se già popolata)
            if (lblDefault != null)
            {
                string text = lblDefault.Text;

                if (!string.IsNullOrEmpty(text))
                {
                    if (text.Contains("⭐"))
                    {
                        // Estrai il nome del clock (tutto dopo i due punti)
                        int colonIndex = text.IndexOf(':');
                        if (colonIndex > 0 && colonIndex < text.Length - 1)
                        {
                            string clockName = text.Substring(colonIndex + 1).Trim();
                            lblDefault.Text = "⭐ " + string.Format(
                                LanguageManager.GetString("Clocks.DefaultClock", "Predefinito:  {0}"),
                                clockName);
                            Console.WriteLine($"[ClocksControl] ✅ lblDefault aggiornato: {lblDefault.Text}");
                        }
                    }
                    else if (text.Contains("⚠️"))
                    {
                        lblDefault.Text = LanguageManager.GetString("Clocks.NoDefaultClock", "⚠️ Nessun clock predefinito");
                        Console.WriteLine($"[ClocksControl] ✅ lblDefault (no default) aggiornato");
                    }
                }
            }

            // ✅ AGGIORNA LABEL STATUS (se già popolata)
            if (lblStatus != null && !string.IsNullOrEmpty(lblStatus.Text))
            {
                // Estrai il numero (es."📊 5 clock disponibili" → 5)
                string[] parts = lblStatus.Text.Split(' ');
                if (parts.Length > 1 && int.TryParse(parts[1], out int count))
                {
                    lblStatus.Text = string.Format(
                        LanguageManager.GetString("Clocks.ClocksAvailable", "📊 {0} clock disponibili"),
                        count);
                    Console.WriteLine($"[ClocksControl] ✅ lblStatus aggiornato: {lblStatus.Text}");
                }
            }

            Console.WriteLine($"[ClocksControl] ✅ ApplyLanguage completato");
        }

        private double NormalizeDuration(double duration)
        {
            return duration / 1000.0;
        }

        private void ClocksControl_Resize(object sender, EventArgs e)
        {
            if (flowClocks != null && flowClocks.Controls.Count > 0)
            {
                int newWidth = flowClocks.ClientSize.Width - 30;

                foreach (Control ctrl in flowClocks.Controls)
                {
                    if (ctrl is Panel card)
                    {
                        card.Width = newWidth;

                        int btnX = newWidth - 155;
                        foreach (Control btnCtrl in card.Controls)
                        {
                            if (btnCtrl is Button btn)
                            {
                                if (btn.Text == "✏️")
                                    btn.Location = new Point(btnX, 18);
                                else if (btn.Text == "⭐" || btn.Text == "☆")
                                    btn.Location = new Point(btnX + 50, 18);
                                else if (btn.Text == "🗑️")
                                    btn.Location = new Point(btnX + 100, 18);
                            }
                        }
                    }
                }
            }
        }

        private void BtnNew_Click(object sender, EventArgs e)
        {
            Console.WriteLine("[ClocksControl] 🆕 Nuovo Clock cliccato");
            var clocks = DbcManager.LoadFromCsv<ClockEntry>("Clocks.dbc");
            if (LicenseManager.IsDemoMode() && !DemoLimits.CanAddClock(clocks.Count, false))
            {
                MessageBox.Show(
                    LanguageManager.GetString("Demo.ClockLimitMessage", "Hai raggiunto il limite demo di 2 clock.\n\nAttiva la licenza completa per crearne altri."),
                    LanguageManager.GetString("Archive.DemoLimitTitle", "Limite Demo Raggiunto"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            ClockEditorForm editorForm = new ClockEditorForm(null);
            if (editorForm.ShowDialog() == DialogResult.OK)
            {
                RefreshClocks();
                StatusChanged?.Invoke(this, LanguageManager.GetString("Clocks.ClockCreated", "✅ Clock creato con successo"));
            }
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            Console.WriteLine("[ClocksControl] 🔄 Refresh cliccato");
            RefreshClocks();
        }

        public void RefreshClocks()
        {
            Console.WriteLine("[ClocksControl] 🔄 RefreshClocks iniziato");

            if (flowClocks == null)
            {
                Console.WriteLine("[ClocksControl] ❌ flowClocks è NULL!");
                return;
            }

            flowClocks.SuspendLayout();
            flowClocks.Controls.Clear();

            var clocks = DbcManager.LoadFromCsv<ClockEntry>("Clocks.dbc");
            Console.WriteLine($"[ClocksControl] 📊 Caricati {clocks.Count} clocks dal database");

            ClockEntry defaultClock = null;

            foreach (var clock in clocks.OrderBy(c => c.ClockName))
            {
                Console.WriteLine($"[ClocksControl] 🕐 Creazione card per:  {clock.ClockName}");

                if (clock.IsDefault == 1)
                    defaultClock = clock;

                Panel card = CreateClockCard(clock);
                flowClocks.Controls.Add(card);
            }

            flowClocks.ResumeLayout();

            if (defaultClock != null)
            {
                lblDefault.Text = "⭐ " + string.Format(LanguageManager.GetString("Clocks.DefaultClock", "Predefinito:  {0}"), defaultClock.ClockName);
                lblDefault.ForeColor = Color.FromArgb(255, 152, 0);
            }
            else
            {
                lblDefault.Text = LanguageManager.GetString("Clocks.NoDefaultClock", "⚠️ Nessun clock predefinito");
                lblDefault.ForeColor = Color.Red;
            }

            lblStatus.Text = string.Format(LanguageManager.GetString("Clocks.ClocksAvailable", "📊 {0} clock disponibili"), clocks.Count);
            StatusChanged?.Invoke(this, $"Clocks:  {clocks.Count} " + LanguageManager.GetString("Clocks.Elements", "elementi"));

            Console.WriteLine($"[ClocksControl] ✅ RefreshClocks completato - {flowClocks.Controls.Count} cards aggiunte");
        }

        private Panel CreateClockCard(ClockEntry clock)
        {
            bool isDefault = clock.IsDefault == 1;

            int itemCount = 0;
            List<ClockItem> items = null;
            double totalDuration = 0;

            try
            {
                items = JsonConvert.DeserializeObject<List<ClockItem>>(clock.Items);
                itemCount = items?.Count ?? 0;

                if (items != null)
                {
                    foreach (var item in items)
                    {
                        totalDuration += GetAverageItemDuration(item);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClocksControl] ⚠️ Errore parsing items:  {ex.Message}");
            }

            int cardWidth = flowClocks.ClientSize.Width - 30;

            Panel card = new Panel
            {
                Width = cardWidth,
                Height = 75,
                BackColor = isDefault ? Color.FromArgb(255, 248, 225) : Color.White,
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(12)
            };

            card.Paint += (s, e) =>
            {
                Color borderColor = isDefault ? Color.FromArgb(255, 193, 7) : AppTheme.Primary;
                using (Pen pen = new Pen(borderColor, 2))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
                }
            };

            Label lblName = new Label
            {
                Text = clock.ClockName,
                Location = new Point(12, 8),
                AutoSize = true,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = AppTheme.TextPrimary,
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblName);

            if (isDefault)
            {
                Label lblDefaultBadge = new Label
                {
                    Text = "⭐ " + LanguageManager.GetString("Clocks.Default", "PREDEFINITO"),
                    Location = new Point(lblName.Right + 10, 10),
                    AutoSize = true,
                    Font = new Font("Segoe UI", 7, FontStyle.Bold),
                    ForeColor = Color.White,
                    BackColor = Color.FromArgb(255, 152, 0),
                    Padding = new Padding(5, 2, 5, 2)
                };
                card.Controls.Add(lblDefaultBadge);
            }

            // ✅ FORMATTA TIMESPAN CORRETTAMENTE
            TimeSpan duration = TimeSpan.FromSeconds(totalDuration);
            string formattedDuration = string.Format("{0:D2}:{1:D2}:{2:D2}",
                (int)duration.TotalHours,
                duration.Minutes,
                duration.Seconds);

            string preview = items != null && items.Count > 0 ? GetItemsPreview(items) : LanguageManager.GetString("Clocks.NoElements", "Nessun elemento");

            Label lblInfo = new Label
            {
                Text = $"⏱️ {LanguageManager.GetString("Clocks.ExpectedDuration", "Durata prevista")}: {formattedDuration}  |  📊 {LanguageManager.GetString("Clocks.ElementsPresent", "Elementi presenti")}: {itemCount}  |  {preview}",
                Location = new Point(12, 35),
                Size = new Size(card.Width - 200, 30),
                Font = new Font("Segoe UI", 8),
                ForeColor = AppTheme.TextSecondary,
                AutoEllipsis = true,
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblInfo);

            // PULSANTI
            int btnX = cardWidth - 155;
            int btnY = 18;

            Button btnEdit = new Button
            {
                Text = "✏️",
                Location = new Point(btnX, btnY),
                Size = new Size(38, 38),
                BackColor = AppTheme.Warning,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 13),
                Cursor = Cursors.Hand,
                Tag = clock
            };
            btnEdit.FlatAppearance.BorderSize = 0;
            btnEdit.Click += BtnEditCard_Click;

            Button btnSetDefault = new Button
            {
                Text = isDefault ? "⭐" : "☆",
                Location = new Point(btnX + 50, btnY),
                Size = new Size(38, 38),
                BackColor = isDefault ? Color.FromArgb(255, 193, 7) : Color.FromArgb(200, 200, 200),
                ForeColor = isDefault ? Color.White : Color.Gray,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 13),
                Cursor = Cursors.Hand,
                Tag = clock,
                Enabled = !isDefault
            };
            btnSetDefault.FlatAppearance.BorderSize = 0;
            btnSetDefault.Click += BtnSetDefaultCard_Click;

            Button btnDelete = new Button
            {
                Text = "🗑️",
                Location = new Point(btnX + 100, btnY),
                Size = new Size(38, 38),
                BackColor = AppTheme.Danger,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 13),
                Cursor = Cursors.Hand,
                Tag = clock,
                Enabled = !isDefault
            };
            btnDelete.FlatAppearance.BorderSize = 0;
            btnDelete.Click += BtnDeleteCard_Click;

            card.Controls.Add(btnEdit);
            card.Controls.Add(btnSetDefault);
            card.Controls.Add(btnDelete);

            btnEdit.BringToFront();
            btnSetDefault.BringToFront();
            btnDelete.BringToFront();

            ToolTip tooltip = new ToolTip();
            tooltip.SetToolTip(btnEdit, LanguageManager.GetString("Clocks.EditClock", "Modifica clock"));
            tooltip.SetToolTip(btnSetDefault, isDefault ? LanguageManager.GetString("Clocks.AlreadyDefault", "Già predefinito") : LanguageManager.GetString("Clocks.SetAsDefault", "Imposta come predefinito"));
            tooltip.SetToolTip(btnDelete, isDefault ? LanguageManager.GetString("Clocks.CannotDeleteDefault", "Non puoi eliminare il clock predefinito") : LanguageManager.GetString("Clocks.DeleteClock", "Elimina clock"));

            return card;
        }

        private double GetAverageItemDuration(ClockItem item)
        {
            try
            {
                string source = item.Type.StartsWith("Music_") ? "Music" : "Clips";
                string type = item.Type.Replace("Music_", "").Replace("Clips_", "");

                if (source == "Music")
                {
                    var musicEntries = DbcManager.LoadFromCsv<MusicEntry>("Music.dbc");
                    var filtered = ApplyItemFilterToMusic(musicEntries, item, type);

                    if (filtered.Count > 0)
                    {
                        double totalSeconds = filtered.Sum(e => NormalizeDuration(e.Duration));
                        return totalSeconds / filtered.Count;
                    }
                }
                else
                {
                    var clipEntries = DbcManager.LoadFromCsv<ClipEntry>("Clips.dbc");
                    var filtered = ApplyItemFilterToClips(clipEntries, item, type);

                    if (filtered.Count > 0)
                    {
                        double totalSeconds = filtered.Sum(e => NormalizeDuration(e.Duration));
                        return totalSeconds / filtered.Count;
                    }
                }

                return 180;
            }
            catch
            {
                return 180;
            }
        }

        private List<MusicEntry> ApplyItemFilterToMusic(List<MusicEntry> entries, ClockItem item, string type)
        {
            var filtered = entries.AsEnumerable();

            if (type == "Category")
            {
                filtered = filtered.Where(e =>
                    !string.IsNullOrEmpty(e.Categories) &&
                    e.Categories.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Any(c => c.Trim().Equals(item.Value, StringComparison.OrdinalIgnoreCase))
                );
            }
            else if (type == "Genre")
            {
                filtered = filtered.Where(e =>
                    !string.IsNullOrEmpty(e.Genre) &&
                    e.Genre.Trim().Equals(item.Value, StringComparison.OrdinalIgnoreCase)
                );
            }
            else if (type == "Category+Genre")
            {
                string[] parts = item.Value.Split(new[] { " + " }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    string category = parts[0];
                    string genre = parts[1];

                    filtered = filtered.Where(e =>
                        !string.IsNullOrEmpty(e.Categories) &&
                        e.Categories.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Any(c => c.Trim().Equals(category, StringComparison.OrdinalIgnoreCase)) &&
                        !string.IsNullOrEmpty(e.Genre) &&
                        e.Genre.Trim().Equals(genre, StringComparison.OrdinalIgnoreCase)
                    );
                }
            }

            if (item.YearFilterEnabled)
            {
                filtered = filtered.Where(e => e.Year >= item.YearFrom && e.Year <= item.YearTo);
            }

            return filtered.ToList();
        }

        private List<ClipEntry> ApplyItemFilterToClips(List<ClipEntry> entries, ClockItem item, string type)
        {
            var filtered = entries.AsEnumerable();

            if (type == "Category")
            {
                filtered = filtered.Where(e =>
                    !string.IsNullOrEmpty(e.Categories) &&
                    e.Categories.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Any(c => c.Trim().Equals(item.Value, StringComparison.OrdinalIgnoreCase))
                );
            }
            else if (type == "Genre")
            {
                filtered = filtered.Where(e =>
                    !string.IsNullOrEmpty(e.Genre) &&
                    e.Genre.Trim().Equals(item.Value, StringComparison.OrdinalIgnoreCase)
                );
            }
            else if (type == "Category+Genre")
            {
                string[] parts = item.Value.Split(new[] { " + " }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    string category = parts[0];
                    string genre = parts[1];

                    filtered = filtered.Where(e =>
                        !string.IsNullOrEmpty(e.Categories) &&
                        e.Categories.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Any(c => c.Trim().Equals(category, StringComparison.OrdinalIgnoreCase)) &&
                        !string.IsNullOrEmpty(e.Genre) &&
                        e.Genre.Trim().Equals(genre, StringComparison.OrdinalIgnoreCase)
                    );
                }
            }

            return filtered.ToList();
        }

        private string GetItemsPreview(List<ClockItem> items)
        {
            if (items == null || items.Count == 0)
                return LanguageManager.GetString("Clocks.NoElements", "Nessun elemento");

            var preview = new List<string>();
            int maxItems = Math.Min(items.Count, 3);

            for (int i = 0; i < maxItems; i++)
            {
                var item = items[i];
                string source = "";
                string type = item.Type;

                if (type.StartsWith("Music_"))
                {
                    source = "🎵";
                    type = type.Substring(6);
                }
                else if (type.StartsWith("Clips_"))
                {
                    source = "🎬";
                    type = type.Substring(6);
                }

                string value = !string.IsNullOrEmpty(item.Value) ? item.Value : item.CategoryName;
                preview.Add($"{source} {value}");
            }

            string result = string.Join(" → ", preview);
            if (items.Count > 3)
                result += $" ... (+{items.Count - 3})";

            return result;
        }

        private void BtnEditCard_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            ClockEntry entry = btn.Tag as ClockEntry;

            Console.WriteLine($"[ClocksControl] ✏️ Modifica clock: {entry.ClockName}");

            ClockEditorForm editorForm = new ClockEditorForm(entry);
            if (editorForm.ShowDialog() == DialogResult.OK)
            {
                RefreshClocks();
                StatusChanged?.Invoke(this, LanguageManager.GetString("Clocks.ClockUpdated", "✅ Clock aggiornato"));
            }
        }

        private void BtnSetDefaultCard_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            ClockEntry entry = btn.Tag as ClockEntry;

            Console.WriteLine($"[ClocksControl] ⭐ Imposta predefinito: {entry.ClockName}");

            var allClocks = DbcManager.LoadFromCsv<ClockEntry>("Clocks.dbc");

            foreach (var clock in allClocks)
            {
                clock.IsDefault = (clock.ID == entry.ID) ? 1 : 0;
                DbcManager.Update("Clocks.dbc", clock);
            }

            RefreshClocks();
            StatusChanged?.Invoke(this, "⭐ " + string.Format(LanguageManager.GetString("Clocks.DefaultClockSet", "Clock predefinito: {0}"), entry.ClockName));
        }

        private void BtnDeleteCard_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            ClockEntry entry = btn.Tag as ClockEntry;

            Console.WriteLine($"[ClocksControl] 🗑️ Richiesta eliminazione: {entry.ClockName}");

            if (entry.IsDefault == 1)
            {
                MessageBox.Show(
                    LanguageManager.GetString("Clocks.CannotDeleteDefaultMessage", "❌ Non puoi eliminare il clock predefinito.\n\nImposta prima un altro clock come predefinito."),
                    LanguageManager.GetString("Clocks.OperationNotAllowed", "Operazione Non Consentita"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            var schedules = DbcManager.LoadFromCsv<ScheduleEntry>("Schedules.dbc");
            var usedInSchedules = schedules.Where(s => s.Type == "PlayClock" && s.ClockName == entry.ClockName).ToList();

            if (usedInSchedules.Count > 0)
            {
                MessageBox.Show(
                    string.Format(LanguageManager.GetString("Clocks.ClockInUseMessage", "❌ Questo clock è utilizzato in {0} schedulazione/i.\n\nElimina o modifica prima le schedulazioni che lo utilizzano."), usedInSchedules.Count),
                    LanguageManager.GetString("Clocks.ClockInUse", "Clock In Uso"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                string.Format(LanguageManager.GetString("Clocks.ConfirmDeleteMessage", "🗑️ Eliminare il clock '{0}'?\n\nQuesta operazione non può essere annullata."), entry.ClockName),
                LanguageManager.GetString("Clocks.ConfirmDelete", "Conferma Eliminazione"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                bool success = DbcManager.Delete<ClockEntry>("Clocks.dbc", entry.ID);

                if (success)
                {
                    RefreshClocks();
                    StatusChanged?.Invoke(this, LanguageManager.GetString("Clocks.ClockDeleted", "✅ Clock eliminato"));
                }
                else
                {
                    MessageBox.Show(
                        LanguageManager.GetString("Clocks.DeleteError", "❌ Errore durante l'eliminazione del clock"),
                        LanguageManager.GetString("Common.Error", "Errore"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // ✅ SALVA CHIAVI MANCANTI
                LanguageManager.SaveMissingKeysToFile();

                // ✅ DISOTTOSCRIVI EVENTO (usa il metodo, non la lambda)
                LanguageManager.LanguageChanged -= OnLanguageChanged;
            }

            base.Dispose(disposing);
        }
    }
}
