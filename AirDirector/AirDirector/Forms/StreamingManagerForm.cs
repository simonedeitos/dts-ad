using AirDirector.Models;
using AirDirector.Services.Database;
using AirDirector.Services.Localization;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AirDirector.Forms
{
    public class StreamingManagerForm : Form
    {
        private readonly DataGridView _grid;
        private readonly List<StreamingEntry> _entries;
        private readonly bool _isRadioTVMode;
        private readonly Button _btnNew;
        private readonly Button _btnEdit;
        private readonly Button _btnDelete;
        private readonly Button _btnClose;

        public StreamingManagerForm(bool isRadioTVMode = false)
        {
            _isRadioTVMode = isRadioTVMode;
            Text = "🌐 " + LanguageManager.GetString("StreamingManager.Title", "Gestione Streaming");
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(820, 500);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            _entries = DbcManager.LoadFromCsv<StreamingEntry>("Streaming.dbc");

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false
            };
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = LanguageManager.GetString("StreamingManager.Name", "Nome"), Width = 220 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = LanguageManager.GetString("StreamingManager.URL", "URL"), AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            if (_isRadioTVMode)
            {
                _grid.Columns.Add(new DataGridViewTextBoxColumn
                {
                    HeaderText = LanguageManager.GetString("StreamingManager.StreamType", "Tipo"),
                    Width = 120
                });
            }

            var panelButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                Padding = new Padding(8),
                FlowDirection = FlowDirection.RightToLeft
            };

            _btnClose = new Button { Width = 100, Text = LanguageManager.GetString("StreamingManager.Close", "Chiudi") };
            _btnClose.Click += (s, e) => Close();
            panelButtons.Controls.Add(_btnClose);

            _btnDelete = new Button { Width = 100, Text = LanguageManager.GetString("StreamingManager.Delete", "Elimina") };
            _btnDelete.Click += BtnDelete_Click;
            panelButtons.Controls.Add(_btnDelete);

            _btnEdit = new Button { Width = 100, Text = LanguageManager.GetString("StreamingManager.Edit", "Modifica") };
            _btnEdit.Click += BtnEdit_Click;
            panelButtons.Controls.Add(_btnEdit);

            _btnNew = new Button { Width = 100, Text = LanguageManager.GetString("StreamingManager.New", "Nuovo") };
            _btnNew.Click += BtnNew_Click;
            panelButtons.Controls.Add(_btnNew);

            Controls.Add(_grid);
            Controls.Add(panelButtons);

            ReloadGrid();
        }

        private void ReloadGrid()
        {
            _grid.Rows.Clear();
            foreach (var e in _entries.OrderBy(x => x.Name))
            {
                int row = _isRadioTVMode
                    ? _grid.Rows.Add(e.Name, e.URL, e.IsVideoStream ? "🎬 Video" : "🔊 Audio")
                    : _grid.Rows.Add(e.Name, e.URL);
                _grid.Rows[row].Tag = e;
            }
        }

        private void BtnNew_Click(object sender, EventArgs e)
        {
            var entry = new StreamingEntry();
            if (!EditEntry(entry))
                return;

            if (DbcManager.Insert("Streaming.dbc", entry))
            {
                _entries.Add(entry);
                ReloadGrid();
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (_grid.SelectedRows.Count != 1)
                return;

            var selected = _grid.SelectedRows[0].Tag as StreamingEntry;
            if (selected == null)
                return;

            var edit = new StreamingEntry
            {
                ID = selected.ID,
                Name = selected.Name,
                URL = selected.URL,
                IsVideoStream = selected.IsVideoStream
            };
            if (!EditEntry(edit))
                return;

            if (DbcManager.Update("Streaming.dbc", edit))
            {
                selected.Name = edit.Name;
                selected.URL = edit.URL;
                selected.IsVideoStream = edit.IsVideoStream;
                ReloadGrid();
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (_grid.SelectedRows.Count != 1)
                return;

            var selected = _grid.SelectedRows[0].Tag as StreamingEntry;
            if (selected == null)
                return;

            var confirm = MessageBox.Show(
                string.Format(LanguageManager.GetString("StreamingManager.ConfirmDelete", "Eliminare lo streaming '{0}'?"), selected.Name),
                LanguageManager.GetString("StreamingManager.ConfirmDeleteTitle", "Conferma eliminazione"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes)
                return;

            if (DbcManager.Delete<StreamingEntry>("Streaming.dbc", selected.ID))
            {
                _entries.RemoveAll(x => x.ID == selected.ID);
                ReloadGrid();
            }
        }

        private bool EditEntry(StreamingEntry entry)
        {
            using (var form = new StreamingEntryEditForm(entry, _isRadioTVMode))
            {
                return form.ShowDialog(this) == DialogResult.OK;
            }
        }
    }

    internal class StreamingEntryEditForm : Form
    {
        private readonly TextBox _txtName;
        private readonly TextBox _txtUrl;
        private readonly CheckBox _chkIsVideo;
        private readonly StreamingEntry _entry;

        public StreamingEntryEditForm(StreamingEntry entry, bool isRadioTVMode = false)
        {
            _entry = entry;
            Text = LanguageManager.GetString("StreamingManager.Title", "Gestione Streaming");
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(520, isRadioTVMode ? 240 : 200);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            Controls.Add(new Label { Left = 12, Top = 20, Width = 120, Text = LanguageManager.GetString("StreamingManager.EnterName", "Nome Streaming:") });
            _txtName = new TextBox { Left = 140, Top = 18, Width = 350, Text = _entry.Name ?? "" };
            Controls.Add(_txtName);

            Controls.Add(new Label { Left = 12, Top = 56, Width = 120, Text = LanguageManager.GetString("StreamingManager.EnterURL", "URL Streaming:") });
            _txtUrl = new TextBox { Left = 140, Top = 54, Width = 350, Text = _entry.URL ?? "" };
            Controls.Add(_txtUrl);

            if (isRadioTVMode)
            {
                Controls.Add(new Label { Left = 12, Top = 92, Width = 120, Text = LanguageManager.GetString("StreamingManager.StreamType", "Tipo Stream:") });
                _chkIsVideo = new CheckBox
                {
                    Left = 140,
                    Top = 90,
                    Width = 280,
                    Text = LanguageManager.GetString("StreamingManager.IsVideoStream", "Stream Video (HLS/RTMP)"),
                    Checked = _entry.IsVideoStream
                };
                Controls.Add(_chkIsVideo);
            }

            int buttonsTop = isRadioTVMode ? 130 : 96;
            var btnSave = new Button { Left = 330, Top = buttonsTop, Width = 75, Text = LanguageManager.GetString("Common.Save", "Salva"), DialogResult = DialogResult.OK };
            btnSave.Click += BtnSave_Click;
            Controls.Add(btnSave);

            var btnCancel = new Button { Left = 415, Top = buttonsTop, Width = 75, Text = LanguageManager.GetString("Common.Cancel", "Annulla"), DialogResult = DialogResult.Cancel };
            Controls.Add(btnCancel);

            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtName.Text))
            {
                MessageBox.Show(LanguageManager.GetString("StreamingManager.ErrorEmptyName", "Il nome non può essere vuoto."));
                DialogResult = DialogResult.None;
                return;
            }
            if (string.IsNullOrWhiteSpace(_txtUrl.Text))
            {
                MessageBox.Show(LanguageManager.GetString("StreamingManager.ErrorEmptyURL", "L'URL non può essere vuoto."));
                DialogResult = DialogResult.None;
                return;
            }

            _entry.Name = _txtName.Text.Trim();
            _entry.URL = _txtUrl.Text.Trim();
            _entry.IsVideoStream = _chkIsVideo?.Checked ?? false;
        }
    }
}
