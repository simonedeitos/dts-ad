using AirDirector.Services.Database;
using AirDirector.Services.Localization;
using AirDirector.Themes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;

namespace AirDirector.Forms
{
    public partial class ScheduleEditorForm : Form
    {
        private ScheduleEntry _schedule;
        private List<ClockEntry> _clocks;
        private bool _isNewSchedule;

        public ScheduleEditorForm(ScheduleEntry schedule, List<ClockEntry> clocks)
        {
            _clocks = clocks;
            _isNewSchedule = (schedule == null);

            if (_isNewSchedule)
            {
                _schedule = new ScheduleEntry
                {
                    Name = "",
                    Type = "PlayClock",
                    Monday = 1,
                    Tuesday = 1,
                    Wednesday = 1,
                    Thursday = 1,
                    Friday = 1,
                    Saturday = 1,
                    Sunday = 1,
                    Times = "",
                    ClockName = "",
                    AudioFilePath = "",
                    MiniPLSID = 0
                };
            }
            else
            {
                _schedule = schedule;
            }

            InitializeComponent();
            ApplyLanguage();
            LoadData();

            LanguageManager.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            this.Text = _isNewSchedule ?
                "📅 " + LanguageManager.GetString("ScheduleEditor.New", "Nuova Schedulazione") :
                "✏️ " + LanguageManager.GetString("ScheduleEditor.Edit", "Modifica Schedulazione");

            if (lblName != null)
                lblName.Text = LanguageManager.GetString("ScheduleEditor.ScheduleName", "Nome Schedulazione:");

            if (grpAction != null)
                grpAction.Text = LanguageManager.GetString("ScheduleEditor.ActionType", "Tipo Azione");

            if (radClock != null)
                radClock.Text = "▶ " + LanguageManager.GetString("ScheduleEditor.PlayClock", "Riproduci Clock");

            if (radAudio != null)
                radAudio.Text = "🎵 " + LanguageManager.GetString("ScheduleEditor.PlayAudio", "Riproduci Audio");

            if (radMiniPLS != null)
                radMiniPLS.Text = "📋 " + LanguageManager.GetString("ScheduleEditor.PlayMiniPlaylist", "Riproduci Mini Playlist");

            if (radTimeSignal != null)
                radTimeSignal.Text = "⏰ " + LanguageManager.GetString("ScheduleEditor.TimeSignal", "Segnale Orario");

            if (radURLStreaming != null)
                radURLStreaming.Text = "🌐 " + LanguageManager.GetString("ScheduleEditor.URLStreaming", "URL Streaming");

            if (lblStreamURL != null)
                lblStreamURL.Text = LanguageManager.GetString("ScheduleEditor.StreamURL", "URL:");

            if (lblStreamDuration != null)
                lblStreamDuration.Text = LanguageManager.GetString("ScheduleEditor.StreamDuration", "Durata:");

            if (grpDays != null)
                grpDays.Text = LanguageManager.GetString("ScheduleEditor.DaysOfWeek", "Giorni della Settimana");

            if (chkMonday != null)
                chkMonday.Text = LanguageManager.GetString("Download.DayMon", "Lun");

            if (chkTuesday != null)
                chkTuesday.Text = LanguageManager.GetString("Download.DayTue", "Mar");

            if (chkWednesday != null)
                chkWednesday.Text = LanguageManager.GetString("Download.DayWed", "Mer");

            if (chkThursday != null)
                chkThursday.Text = LanguageManager.GetString("Download.DayThu", "Gio");

            if (chkFriday != null)
                chkFriday.Text = LanguageManager.GetString("Download.DayFri", "Ven");

            if (chkSaturday != null)
                chkSaturday.Text = LanguageManager.GetString("Download.DaySat", "Sab");

            if (chkSunday != null)
                chkSunday.Text = LanguageManager.GetString("Download.DaySun", "Dom");

            if (btnAllDays != null)
                btnAllDays.Text = "✓ " + LanguageManager.GetString("ScheduleEditor.All", "Tutti");

            if (grpTimes != null)
                grpTimes.Text = LanguageManager.GetString("ScheduleEditor.ExecutionTimes", "Orari di Esecuzione");

            if (lblAddTime != null)
                lblAddTime.Text = LanguageManager.GetString("ScheduleEditor.AddTime", "Aggiungi Orario:");

            if (btnAddTime != null)
                btnAddTime.Text = "➕ " + LanguageManager.GetString("Download.Add", "Aggiungi");

            if (btnRemoveTime != null)
                btnRemoveTime.Text = "🗑️ " + LanguageManager.GetString("TaskEditor.Remove", "Rimuovi");

            if (btnSave != null)
                btnSave.Text = "💾 " + LanguageManager.GetString("Common.Save", "Salva");

            if (btnCancel != null)
                btnCancel.Text = "✖ " + LanguageManager.GetString("Common.Cancel", "Annulla");
        }

        private void SetAllDays(bool check)
        {
            chkMonday.Checked = check;
            chkTuesday.Checked = check;
            chkWednesday.Checked = check;
            chkThursday.Checked = check;
            chkFriday.Checked = check;
            chkSaturday.Checked = check;
            chkSunday.Checked = check;
        }

        private void LoadData()
        {
            txtName.Text = _schedule.Name;

            cmbClock.Items.Clear();
            foreach (var clock in _clocks)
            {
                cmbClock.Items.Add(clock.ClockName);
            }

            if (cmbClock.Items.Count > 0)
            {
                if (!string.IsNullOrEmpty(_schedule.ClockName) && cmbClock.Items.Contains(_schedule.ClockName))
                    cmbClock.SelectedItem = _schedule.ClockName;
                else
                    cmbClock.SelectedIndex = 0;
            }

            if (_schedule.Type == "PlayClock")
                radClock.Checked = true;
            else if (_schedule.Type == "PlayAudio")
                radAudio.Checked = true;
            else if (_schedule.Type == "PlayMiniPLS")
                radMiniPLS.Checked = true;
            else if (_schedule.Type == "TimeSignal")
                radTimeSignal.Checked = true;
            else if (_schedule.Type == "URLStreaming")
                radURLStreaming.Checked = true;

            txtAudioFile.Text = _schedule.AudioFilePath;
            numMiniPLSID.Value = _schedule.MiniPLSID > 0 ? _schedule.MiniPLSID : 1;

            if (!string.IsNullOrEmpty(_schedule.ClockName) && _schedule.Type == "URLStreaming")
            {
                var parts = _schedule.ClockName.Split('|');
                if (parts.Length >= 1)
                    txtStreamURL.Text = parts[0];
                if (parts.Length >= 2)
                    txtStreamDuration.Text = parts[1];
                else
                    txtStreamDuration.Text = "01:00:00";
            }

            chkMonday.Checked = _schedule.Monday == 1;
            chkTuesday.Checked = _schedule.Tuesday == 1;
            chkWednesday.Checked = _schedule.Wednesday == 1;
            chkThursday.Checked = _schedule.Thursday == 1;
            chkFriday.Checked = _schedule.Friday == 1;
            chkSaturday.Checked = _schedule.Saturday == 1;
            chkSunday.Checked = _schedule.Sunday == 1;

            if (!string.IsNullOrEmpty(_schedule.Times))
            {
                var times = _schedule.Times.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var time in times)
                {
                    lstTimes.Items.Add(time.Trim());
                }
            }

            RadAction_CheckedChanged(null, null);
        }

        private void RadAction_CheckedChanged(object sender, EventArgs e)
        {
            cmbClock.Enabled = radClock.Checked;
            txtAudioFile.Enabled = radAudio.Checked;
            btnBrowseAudio.Enabled = radAudio.Checked;
            numMiniPLSID.Enabled = radMiniPLS.Checked;
            txtStreamURL.Enabled = radURLStreaming.Checked;
            txtStreamDuration.Enabled = radURLStreaming.Checked;
            lblStreamURL.Enabled = radURLStreaming.Checked;
            lblStreamDuration.Enabled = radURLStreaming.Checked;
        }

        private void BtnBrowseAudio_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = LanguageManager.GetString("ScheduleEditor.AudioFilter", "File Audio (*.mp3;*.wav;*.wma;*.aac)|*.mp3;*.wav;*.wma;*.aac|Tutti i file (*.*)|*.*");
                ofd.Title = LanguageManager.GetString("ScheduleEditor.SelectAudioFile", "Seleziona File Audio");

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtAudioFile.Text = ofd.FileName;
                }
            }
        }

        private void BtnAddTime_Click(object sender, EventArgs e)
        {
            string time = dtpTime.Value.ToString("HH:mm:ss");

            if (!lstTimes.Items.Contains(time))
            {
                lstTimes.Items.Add(time);

                var sortedTimes = lstTimes.Items.Cast<string>()
                    .OrderBy(t => TimeSpan.Parse(t))
                    .ToList();

                lstTimes.Items.Clear();
                foreach (var t in sortedTimes)
                {
                    lstTimes.Items.Add(t);
                }
            }
            else
            {
                MessageBox.Show(
                    string.Format(LanguageManager.GetString("ScheduleEditor.TimeDuplicate", "⚠️ L'orario {0} è già presente nella lista"), time),
                    LanguageManager.GetString("ScheduleEditor.DuplicateTime", "Orario Duplicato"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void BtnRemoveTime_Click(object sender, EventArgs e)
        {
            if (lstTimes.SelectedIndex >= 0)
            {
                lstTimes.Items.RemoveAt(lstTimes.SelectedIndex);
            }
            else
            {
                MessageBox.Show(
                    LanguageManager.GetString("ScheduleEditor.SelectTimeToRemove", "⚠️ Seleziona un orario da rimuovere"),
                    LanguageManager.GetString("ScheduleEditor.NoSelection", "Nessuna Selezione"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void BtnAllDays_Click(object sender, EventArgs e)
        {
            SetAllDays(true);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show(
                    LanguageManager.GetString("ScheduleEditor.ErrorEmptyName", "❌ Il nome della schedulazione non può essere vuoto"),
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                txtName.Focus();
                return;
            }

            if (!chkMonday.Checked && !chkTuesday.Checked && !chkWednesday.Checked &&
                !chkThursday.Checked && !chkFriday.Checked && !chkSaturday.Checked && !chkSunday.Checked)
            {
                MessageBox.Show(
                    LanguageManager.GetString("ScheduleEditor.ErrorNoDay", "❌ Devi selezionare almeno un giorno della settimana"),
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            if (lstTimes.Items.Count == 0)
            {
                MessageBox.Show(
                    LanguageManager.GetString("ScheduleEditor.ErrorNoTime", "❌ Devi specificare almeno un orario"),
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            if (radClock.Checked && cmbClock.SelectedIndex < 0)
            {
                MessageBox.Show(
                    LanguageManager.GetString("ScheduleEditor.ErrorNoClock", "❌ Devi selezionare un Clock"),
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            if (radAudio.Checked && string.IsNullOrWhiteSpace(txtAudioFile.Text))
            {
                MessageBox.Show(
                    LanguageManager.GetString("ScheduleEditor.ErrorNoAudioFile", "❌ Devi selezionare un file audio"),
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            if (radAudio.Checked && !File.Exists(txtAudioFile.Text))
            {
                MessageBox.Show(
                    LanguageManager.GetString("ScheduleEditor.ErrorAudioFileNotExists", "❌ Il file audio selezionato non esiste"),
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            if (radURLStreaming.Checked && string.IsNullOrWhiteSpace(txtStreamURL.Text))
            {
                MessageBox.Show(
                    LanguageManager.GetString("ScheduleEditor.ErrorNoStreamURL", "❌ Devi inserire un URL valido"),
                    LanguageManager.GetString("Common.Error", "Errore"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            _schedule.Name = txtName.Text.Trim();
            _schedule.Monday = chkMonday.Checked ? 1 : 0;
            _schedule.Tuesday = chkTuesday.Checked ? 1 : 0;
            _schedule.Wednesday = chkWednesday.Checked ? 1 : 0;
            _schedule.Thursday = chkThursday.Checked ? 1 : 0;
            _schedule.Friday = chkFriday.Checked ? 1 : 0;
            _schedule.Saturday = chkSaturday.Checked ? 1 : 0;
            _schedule.Sunday = chkSunday.Checked ? 1 : 0;

            List<string> times = new List<string>();
            foreach (var item in lstTimes.Items)
            {
                times.Add(item.ToString());
            }
            _schedule.Times = string.Join(";", times);

            if (radClock.Checked)
            {
                _schedule.Type = "PlayClock";
                _schedule.ClockName = cmbClock.SelectedItem?.ToString() ?? "";
                _schedule.AudioFilePath = "";
                _schedule.MiniPLSID = 0;
            }
            else if (radAudio.Checked)
            {
                _schedule.Type = "PlayAudio";
                _schedule.ClockName = "";
                _schedule.AudioFilePath = txtAudioFile.Text;
                _schedule.MiniPLSID = 0;
            }
            else if (radMiniPLS.Checked)
            {
                _schedule.Type = "PlayMiniPLS";
                _schedule.ClockName = "";
                _schedule.AudioFilePath = "";
                _schedule.MiniPLSID = (int)numMiniPLSID.Value;
            }
            else if (radTimeSignal.Checked)
            {
                _schedule.Type = "TimeSignal";
                _schedule.ClockName = "";
                _schedule.AudioFilePath = "";
                _schedule.MiniPLSID = 0;
            }
            else if (radURLStreaming.Checked)
            {
                _schedule.Type = "URLStreaming";
                _schedule.ClockName = $"{txtStreamURL.Text.Trim()}|{txtStreamDuration.Text}";
                _schedule.AudioFilePath = "";
                _schedule.MiniPLSID = 0;
            }

            bool success;
            if (_isNewSchedule)
            {
                success = DbcManager.Insert("Schedules.dbc", _schedule);
            }
            else
            {
                success = DbcManager.Update("Schedules.dbc", _schedule);
            }

            if (success)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show(
                    LanguageManager.GetString("ScheduleEditor.SaveError", "❌ Errore durante il salvataggio della schedulazione"),
                    LanguageManager.GetString("ScheduleEditor.DatabaseError", "Errore Database"),
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

        private void lblStreamDuration_Click(object sender, EventArgs e)
        {

        }
    }
}