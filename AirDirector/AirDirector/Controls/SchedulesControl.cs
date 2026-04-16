using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using AirDirector.Services.Database;
using AirDirector.Services.Localization;
using AirDirector.Forms;
using AirDirector.Themes;
using AirDirector.Models;

namespace AirDirector.Controls
{
    public partial class SchedulesControl : UserControl
    {
        private FlowLayoutPanel flowSchedules;
        private Label lblStatus;
        private Button btnNew;
        private Button btnRefresh;

        public event EventHandler<string> StatusChanged;

        public SchedulesControl()
        {
            InitializeComponent();
            InitializeUI();
            ApplyLanguage();
            RefreshSchedules();

            LanguageManager.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            ApplyLanguage();
            RefreshSchedules();
        }

        private void ApplyLanguage()
        {
            if (btnNew != null)
                btnNew.Text = "➕ " + LanguageManager.GetString("Schedules.NewSchedule", "Nuova Schedulazione");

            if (btnRefresh != null)
                btnRefresh.Text = "🔄 " + LanguageManager.GetString("Schedules.Refresh", "Aggiorna");
        }

        private void InitializeUI()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = AppTheme.BgLight;

            flowSchedules = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(15),
                BackColor = AppTheme.BgLight,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };
            this.Controls.Add(flowSchedules);

            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = AppTheme.Surface
            };
            this.Controls.Add(headerPanel);

            btnNew = new Button
            {
                Text = "➕ Nuova Schedulazione",
                Location = new Point(10, 15),
                Size = new Size(180, 35),
                BackColor = AppTheme.Success,
                ForeColor = AppTheme.TextInverse,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnNew.FlatAppearance.BorderSize = 0;
            btnNew.Click += BtnNew_Click;
            headerPanel.Controls.Add(btnNew);

            btnRefresh = new Button
            {
                Text = "🔄 Aggiorna",
                Location = new Point(200, 15),
                Size = new Size(110, 35),
                BackColor = AppTheme.Info,
                ForeColor = AppTheme.TextInverse,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9),
                Cursor = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (s, e) => RefreshSchedules();
            headerPanel.Controls.Add(btnRefresh);

            lblStatus = new Label
            {
                Location = new Point(320, 20),
                Size = new Size(600, 25),
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = AppTheme.TextSecondary,
                TextAlign = ContentAlignment.MiddleLeft
            };
            headerPanel.Controls.Add(lblStatus);

            this.Resize += SchedulesControl_Resize;
        }

        private void SchedulesControl_Resize(object sender, EventArgs e)
        {
            if (flowSchedules != null && flowSchedules.Controls.Count > 0)
            {
                int newWidth = flowSchedules.ClientSize.Width - 30;

                foreach (Control ctrl in flowSchedules.Controls)
                {
                    if (ctrl is Panel card)
                    {
                        card.Width = newWidth;

                        int btnX = newWidth - 145;
                        foreach (Control btnCtrl in card.Controls)
                        {
                            if (btnCtrl is Button btn)
                            {
                                if (btn.Text == "✏️")
                                    btn.Location = new Point(btnX, 10);
                                else if (btn.Text.Contains("✓") || btn.Text.Contains("○"))
                                    btn.Location = new Point(btnX + 45, 10);
                                else if (btn.Text == "🗑️")
                                    btn.Location = new Point(btnX + 90, 10);
                            }
                        }
                    }
                }
            }
        }

        private void BtnNew_Click(object sender, EventArgs e)
        {
            var clocks = DbcManager.LoadFromCsv<ClockEntry>("Clocks.dbc");

            if (clocks.Count == 0)
            {
                MessageBox.Show(
                    LanguageManager.GetString("Schedules.NoClocks", "⚠️ Devi creare almeno un Clock prima di aggiungere una schedulazione.\n\nVai nella sezione 'Clocks' per crearne uno."),
                    LanguageManager.GetString("Schedules.NoClocksTitle", "Nessun Clock Disponibile"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            ScheduleEditorForm editorForm = new ScheduleEditorForm(null, clocks);
            if (editorForm.ShowDialog() == DialogResult.OK)
            {
                RefreshSchedules();
                StatusChanged?.Invoke(this, LanguageManager.GetString("Schedules.ScheduleCreated", "✅ Schedulazione creata con successo"));
            }
        }

        public void RefreshSchedules()
        {
            flowSchedules.SuspendLayout();
            flowSchedules.Controls.Clear();

            var schedules = DbcManager.LoadFromCsv<ScheduleEntry>("Schedules.dbc");

            var orderedSchedules = schedules.OrderBy(s => GetFirstTime(s.Times)).ThenBy(s => s.Name).ToList();

            foreach (var schedule in orderedSchedules)
            {
                Panel card = CreateScheduleCard(schedule);
                flowSchedules.Controls.Add(card);
            }

            flowSchedules.ResumeLayout();

            int activeCount = schedules.Count(s => s.IsEnabled == 1);
            lblStatus.Text = string.Format(
                LanguageManager.GetString("Schedules.StatusCount", "📊 {0} schedulazioni ({1} attive, {2} disabilitate)"),
                schedules.Count,
                activeCount,
                schedules.Count - activeCount);
            StatusChanged?.Invoke(this, $"Schedulazioni: {schedules.Count} " + LanguageManager.GetString("Schedules.Elements", "elementi"));
        }

        private TimeSpan GetFirstTime(string times)
        {
            if (string.IsNullOrEmpty(times))
                return TimeSpan.MaxValue;

            var firstTime = times.Split(';')[0];
            if (TimeSpan.TryParse(firstTime, out TimeSpan result))
                return result;

            return TimeSpan.MaxValue;
        }

        private Panel CreateScheduleCard(ScheduleEntry schedule)
        {
            bool isEnabled = schedule.IsEnabled == 1;
            int cardWidth = flowSchedules.ClientSize.Width - 30;

            Panel card = new Panel
            {
                Width = cardWidth,
                Height = 50,
                BackColor = isEnabled ? Color.White : Color.FromArgb(240, 240, 240),
                Margin = new Padding(0, 0, 0, 5),
                Padding = new Padding(10, 5, 10, 5)
            };

            card.Paint += (s, e) =>
            {
                Color borderColor = isEnabled ? AppTheme.Primary : Color.Gray;
                using (Pen pen = new Pen(borderColor, 2))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
                }
            };

            string firstTime = !string.IsNullOrEmpty(schedule.Times) ? schedule.Times.Split(';')[0] : "--:--:--";
            Label lblTime = new Label
            {
                Text = firstTime,
                Location = new Point(10, 12),
                Size = new Size(75, 25),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = isEnabled ? AppTheme.Primary : Color.Gray,
                TextAlign = ContentAlignment.MiddleLeft
            };
            card.Controls.Add(lblTime);

            string icon = schedule.Type == "PlayClock" ? "🕐" :
                         schedule.Type == "PlayAudio" ? "🎵" :
                         (schedule.Type == "PlayMiniPLS" || schedule.Type == "PlayPlaylist") ? "📋" :
                         schedule.Type == "TimeSignal" ? "⏰" :
                         schedule.Type == "URLStreaming" ? "🌐" :
                         schedule.Type == "LogoShow" ? "🟢" :
                         schedule.Type == "LogoHide" ? "🔴" : "📄";

            string target = "";
            if (schedule.Type == "PlayClock")
                target = schedule.ClockName;
            else if (schedule.Type == "PlayAudio")
                target = System.IO.Path.GetFileNameWithoutExtension(schedule.AudioFilePath);
            else if (schedule.Type == "PlayPlaylist")
                target = System.IO.Path.GetFileNameWithoutExtension(schedule.AudioFilePath);
            else if (schedule.Type == "PlayMiniPLS")
                target = $"MiniPLS #{schedule.MiniPLSID}";
            else if (schedule.Type == "TimeSignal")
                target = LanguageManager.GetString("Schedule.TimeSignalLabel", "Segnale Orario");
            else if (schedule.Type == "URLStreaming")
            {
                var urlParts = schedule.ClockName?.Split('|');
                target = urlParts?.Length > 0 ? urlParts[0] : "";
            }
            else if (schedule.Type == "LogoShow" || schedule.Type == "LogoHide")
            {
                target = System.IO.Path.GetFileName(schedule.ClockName ?? "");
            }

            Label lblTitle = new Label
            {
                Text = $"{icon} {schedule.Name} → {target}",
                Location = new Point(95, 8),
                Size = new Size(cardWidth - 265, 18),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = isEnabled ? AppTheme.TextPrimary : Color.Gray,
                AutoEllipsis = true
            };
            card.Controls.Add(lblTitle);

            List<string> days = new List<string>();
            if (schedule.Monday == 1) days.Add(LanguageManager.GetString("Download.DayMon", "Lun"));
            if (schedule.Tuesday == 1) days.Add(LanguageManager.GetString("Download.DayTue", "Mar"));
            if (schedule.Wednesday == 1) days.Add(LanguageManager.GetString("Download.DayWed", "Mer"));
            if (schedule.Thursday == 1) days.Add(LanguageManager.GetString("Download.DayThu", "Gio"));
            if (schedule.Friday == 1) days.Add(LanguageManager.GetString("Download.DayFri", "Ven"));
            if (schedule.Saturday == 1) days.Add(LanguageManager.GetString("Download.DaySat", "Sab"));
            if (schedule.Sunday == 1) days.Add(LanguageManager.GetString("Download.DaySun", "Dom"));

            string daysText = days.Count == 7 ?
                "📅 " + LanguageManager.GetString("Download.EveryDay", "Tutti i giorni") :
                $"📅 {string.Join(", ", days)}";

            string allTimes = !string.IsNullOrEmpty(schedule.Times) ? schedule.Times.Replace(";", ", ") : "--:--:--";
            string timesInfo = schedule.Times?.Split(';').Length > 1 ? $" | ⏰ {allTimes}" : "";

            Label lblDays = new Label
            {
                Text = $"{daysText}{timesInfo}",
                Location = new Point(95, 28),
                Size = new Size(cardWidth - 265, 16),
                Font = new Font("Segoe UI", 8),
                ForeColor = isEnabled ? AppTheme.TextSecondary : Color.Gray,
                AutoEllipsis = true
            };
            card.Controls.Add(lblDays);

            int btnX = cardWidth - 145;

            Button btnEdit = new Button
            {
                Text = "✏️",
                Location = new Point(btnX, 10),
                Size = new Size(35, 30),
                BackColor = AppTheme.Warning,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11),
                Cursor = Cursors.Hand,
                Tag = schedule
            };
            btnEdit.FlatAppearance.BorderSize = 0;
            btnEdit.Click += BtnEditCard_Click;

            Button btnToggle = new Button
            {
                Text = isEnabled ? "✓" : "○",
                Location = new Point(btnX + 45, 10),
                Size = new Size(35, 30),
                BackColor = isEnabled ? AppTheme.Success : Color.FromArgb(180, 180, 180),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Tag = schedule
            };
            btnToggle.FlatAppearance.BorderSize = 0;
            btnToggle.Click += BtnToggleCard_Click;

            Button btnDelete = new Button
            {
                Text = "🗑️",
                Location = new Point(btnX + 90, 10),
                Size = new Size(35, 30),
                BackColor = AppTheme.Danger,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11),
                Cursor = Cursors.Hand,
                Tag = schedule
            };
            btnDelete.FlatAppearance.BorderSize = 0;
            btnDelete.Click += BtnDeleteCard_Click;

            card.Controls.Add(btnEdit);
            card.Controls.Add(btnToggle);
            card.Controls.Add(btnDelete);

            btnEdit.BringToFront();
            btnToggle.BringToFront();
            btnDelete.BringToFront();

            ToolTip tooltip = new ToolTip();
            tooltip.SetToolTip(btnEdit, LanguageManager.GetString("Schedules.Edit", "Modifica schedulazione"));
            tooltip.SetToolTip(btnToggle, isEnabled ?
                LanguageManager.GetString("Schedules.Disable", "Disabilita schedulazione") :
                LanguageManager.GetString("Schedules.Enable", "Abilita schedulazione"));
            tooltip.SetToolTip(btnDelete, LanguageManager.GetString("Schedules.Delete", "Elimina schedulazione"));

            return card;
        }

        private void BtnEditCard_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            ScheduleEntry entry = btn.Tag as ScheduleEntry;

            var clocks = DbcManager.LoadFromCsv<ClockEntry>("Clocks.dbc");

            ScheduleEditorForm editorForm = new ScheduleEditorForm(entry, clocks);
            if (editorForm.ShowDialog() == DialogResult.OK)
            {
                RefreshSchedules();
                StatusChanged?.Invoke(this, LanguageManager.GetString("Schedules.ScheduleUpdated", "✅ Schedulazione aggiornata"));
            }
        }

        private void BtnToggleCard_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            ScheduleEntry entry = btn.Tag as ScheduleEntry;

            entry.IsEnabled = entry.IsEnabled == 1 ? 0 : 1;

            bool success = DbcManager.Update("Schedules.dbc", entry);

            if (success)
            {
                RefreshSchedules();
                string status = entry.IsEnabled == 1 ?
                    LanguageManager.GetString("Schedules.Enabled", "abilitata") :
                    LanguageManager.GetString("Schedules.Disabled", "disabilitata");
                StatusChanged?.Invoke(this, $"✅ " + string.Format(LanguageManager.GetString("Schedules.ScheduleToggled", "Schedulazione {0}"), status));
            }
            else
            {
                MessageBox.Show(
                    LanguageManager.GetString("Schedules.UpdateError", "❌ Errore durante l'aggiornamento della schedulazione"),
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void BtnDeleteCard_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            ScheduleEntry entry = btn.Tag as ScheduleEntry;

            var result = MessageBox.Show(
                string.Format(LanguageManager.GetString("Schedules.ConfirmDelete", "🗑️ Eliminare la schedulazione '{0}'?\n\nQuesta operazione non può essere annullata."), entry.Name),
                LanguageManager.GetString("Common.Confirm", "Conferma Eliminazione"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                bool success = DbcManager.Delete<ScheduleEntry>("Schedules.dbc", entry.ID);

                if (success)
                {
                    RefreshSchedules();
                    StatusChanged?.Invoke(this, LanguageManager.GetString("Schedules.ScheduleDeleted", "✅ Schedulazione eliminata"));
                }
                else
                {
                    MessageBox.Show(
                        LanguageManager.GetString("Schedules.DeleteError", "❌ Errore durante l'eliminazione della schedulazione"),
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
                LanguageManager.SaveMissingKeysToFile();
                LanguageManager.LanguageChanged -= OnLanguageChanged;
            }
            base.Dispose(disposing);
        }
    }
}
