using AirDirector.Controls;
using AirDirector.Models;
using AirDirector.Services.Database;
using AirDirector.Services.Localization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AirDirector.Forms
{
    public class CommandManagerForm : Form
    {
        private readonly TabControl _tabs;
        private readonly DataGridView _gridHttp;
        private readonly DataGridView _gridUdp;
        private readonly DataGridView _gridLogo;
        private readonly List<CommandEntry> _entries;
        private readonly bool _isRadioTVMode;
        private readonly Button _btnNew;
        private readonly Button _btnEdit;
        private readonly Button _btnDelete;
        private readonly Button _btnClose;

        public CommandManagerForm()
        {
            Text = "📡 " + LanguageManager.GetString("CommandManager.Title", "Gestione Comandi");
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(920, 560);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            _isRadioTVMode = ConfigurationControl.IsRadioTVMode();
            _entries = DbcManager.LoadFromCsv<CommandEntry>("Commands.dbc");

            _tabs = new TabControl
            {
                Dock = DockStyle.Fill
            };

            _gridHttp = BuildGrid();
            _gridUdp = BuildGrid();
            _gridLogo = BuildGrid();

            _tabs.TabPages.Add(BuildTab(LanguageManager.GetString("CommandManager.TabHttp", "Comandi HTTP"), _gridHttp));
            _tabs.TabPages.Add(BuildTab(LanguageManager.GetString("CommandManager.TabUdp", "Comandi UDP"), _gridUdp));
            if (_isRadioTVMode)
                _tabs.TabPages.Add(BuildTab(LanguageManager.GetString("CommandManager.TabLogo", "Logo Addizionale"), _gridLogo));

            var panelButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                Padding = new Padding(8),
                FlowDirection = FlowDirection.RightToLeft
            };

            _btnClose = new Button { Width = 100, Text = LanguageManager.GetString("CommandManager.Close", "Chiudi") };
            _btnClose.Click += (s, e) => Close();
            panelButtons.Controls.Add(_btnClose);

            _btnDelete = new Button { Width = 100, Text = LanguageManager.GetString("CommandManager.Delete", "Elimina") };
            _btnDelete.Click += BtnDelete_Click;
            panelButtons.Controls.Add(_btnDelete);

            _btnEdit = new Button { Width = 100, Text = LanguageManager.GetString("CommandManager.Edit", "Modifica") };
            _btnEdit.Click += BtnEdit_Click;
            panelButtons.Controls.Add(_btnEdit);

            _btnNew = new Button { Width = 100, Text = LanguageManager.GetString("CommandManager.New", "Nuovo") };
            _btnNew.Click += BtnNew_Click;
            panelButtons.Controls.Add(_btnNew);

            Controls.Add(_tabs);
            Controls.Add(panelButtons);
            ReloadGrids();
        }

        private DataGridView BuildGrid()
        {
            var grid = new DataGridView
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
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = LanguageManager.GetString("CommandManager.Name", "Nome"), Width = 240 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = LanguageManager.GetString("CommandManager.String", "Stringa"), AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            return grid;
        }

        private TabPage BuildTab(string title, DataGridView grid)
        {
            var tab = new TabPage(title);
            tab.Controls.Add(grid);
            return tab;
        }

        private string GetCurrentType()
        {
            if (_tabs.SelectedTab == null) return "HTTP";
            if (_tabs.SelectedTab == _tabs.TabPages[0]) return "HTTP";
            if (_tabs.SelectedTab == _tabs.TabPages[1]) return "UDP";
            return "Logo";
        }

        private static bool IsLogoType(string type) => string.Equals(type, "LogoShow", StringComparison.OrdinalIgnoreCase) || string.Equals(type, "LogoHide", StringComparison.OrdinalIgnoreCase);

        private void ReloadGrids()
        {
            _gridHttp.Rows.Clear();
            _gridUdp.Rows.Clear();
            _gridLogo.Rows.Clear();

            foreach (var e in _entries.OrderBy(x => x.Name))
            {
                if (string.Equals(e.Type, "HTTP", StringComparison.OrdinalIgnoreCase))
                {
                    int row = _gridHttp.Rows.Add(e.Name, e.CommandString);
                    _gridHttp.Rows[row].Tag = e;
                }
                else if (string.Equals(e.Type, "UDP", StringComparison.OrdinalIgnoreCase))
                {
                    int row = _gridUdp.Rows.Add(e.Name, e.CommandString);
                    _gridUdp.Rows[row].Tag = e;
                }
                else if (IsLogoType(e.Type))
                {
                    int row = _gridLogo.Rows.Add(e.Name, $"{e.Type} | {e.CommandString}");
                    _gridLogo.Rows[row].Tag = e;
                }
            }
        }

        private DataGridView GetCurrentGrid()
        {
            string type = GetCurrentType();
            if (type == "HTTP") return _gridHttp;
            if (type == "UDP") return _gridUdp;
            return _gridLogo;
        }

        private void BtnNew_Click(object sender, EventArgs e)
        {
            string type = GetCurrentType();
            if (type == "Logo" && !_isRadioTVMode)
                return;

            var entry = new CommandEntry { Type = type == "Logo" ? "LogoShow" : type };
            if (!EditEntry(entry, type))
                return;

            if (DbcManager.Insert("Commands.dbc", entry))
            {
                _entries.Add(entry);
                ReloadGrids();
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            var grid = GetCurrentGrid();
            if (grid.SelectedRows.Count != 1)
                return;

            var selected = grid.SelectedRows[0].Tag as CommandEntry;
            if (selected == null)
                return;

            var edit = new CommandEntry
            {
                ID = selected.ID,
                Name = selected.Name,
                Type = selected.Type,
                CommandString = selected.CommandString
            };

            string editorType = IsLogoType(selected.Type) ? "Logo" : selected.Type;
            if (!EditEntry(edit, editorType))
                return;

            if (DbcManager.Update("Commands.dbc", edit))
            {
                selected.Name = edit.Name;
                selected.Type = edit.Type;
                selected.CommandString = edit.CommandString;
                ReloadGrids();
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            var grid = GetCurrentGrid();
            if (grid.SelectedRows.Count != 1)
                return;

            var selected = grid.SelectedRows[0].Tag as CommandEntry;
            if (selected == null)
                return;

            var confirm = MessageBox.Show(
                string.Format(LanguageManager.GetString("CommandManager.ConfirmDelete", "Eliminare il comando '{0}'?"), selected.Name),
                LanguageManager.GetString("CommandManager.ConfirmDeleteTitle", "Conferma eliminazione"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes)
                return;

            if (DbcManager.Delete<CommandEntry>("Commands.dbc", selected.ID))
            {
                _entries.RemoveAll(x => x.ID == selected.ID);
                ReloadGrids();
            }
        }

        private bool EditEntry(CommandEntry entry, string type)
        {
            using (var form = new CommandEntryEditForm(entry, type, _isRadioTVMode))
                return form.ShowDialog(this) == DialogResult.OK;
        }
    }

    internal class CommandEntryEditForm : Form
    {
        private readonly CommandEntry _entry;
        private readonly string _type;
        private readonly TextBox _txtName;
        private readonly TextBox _txtCommand;
        private readonly RadioButton _radLogoShow;
        private readonly RadioButton _radLogoHide;
        private readonly ComboBox _cmbLogo;
        private readonly List<AdditionalLogo> _logos = new List<AdditionalLogo>();

        public CommandEntryEditForm(CommandEntry entry, string type, bool isRadioTVMode)
        {
            _entry = entry;
            _type = type;

            Text = LanguageManager.GetString("CommandManager.Title", "Gestione Comandi");
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(620, 260);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            Controls.Add(new Label { Left = 12, Top = 20, Width = 130, Text = LanguageManager.GetString("CommandManager.EnterName", "Nome Comando:") });
            _txtName = new TextBox { Left = 150, Top = 18, Width = 440, Text = _entry.Name ?? "" };
            Controls.Add(_txtName);

            if (_type == "Logo" && isRadioTVMode)
            {
                _radLogoShow = new RadioButton { Left = 150, Top = 56, Width = 120, Text = LanguageManager.GetString("CommandManager.LogoShow", "Mostra Logo"), Checked = !string.Equals(_entry.Type, "LogoHide", StringComparison.OrdinalIgnoreCase) };
                _radLogoHide = new RadioButton { Left = 280, Top = 56, Width = 140, Text = LanguageManager.GetString("CommandManager.LogoHide", "Nascondi Logo"), Checked = string.Equals(_entry.Type, "LogoHide", StringComparison.OrdinalIgnoreCase) };
                Controls.Add(_radLogoShow);
                Controls.Add(_radLogoHide);

                Controls.Add(new Label { Left = 12, Top = 92, Width = 130, Text = LanguageManager.GetString("CommandManager.SelectLogo", "Logo:") });
                _cmbLogo = new ComboBox { Left = 150, Top = 90, Width = 440, DropDownStyle = ComboBoxStyle.DropDownList };
                LoadAdditionalLogos();
                foreach (var logo in _logos)
                    _cmbLogo.Items.Add(logo);
                int index = _logos.FindIndex(x => string.Equals(x.ImagePath, _entry.CommandString, StringComparison.OrdinalIgnoreCase));
                if (index >= 0 && _cmbLogo.Items.Count > index) _cmbLogo.SelectedIndex = index;
                else if (_cmbLogo.Items.Count > 0) _cmbLogo.SelectedIndex = 0;
                Controls.Add(_cmbLogo);
                _txtCommand = null;
            }
            else
            {
                Controls.Add(new Label { Left = 12, Top = 56, Width = 130, Text = LanguageManager.GetString("CommandManager.EnterString", "Stringa:") });
                _txtCommand = new TextBox { Left = 150, Top = 54, Width = 440, Text = _entry.CommandString ?? "" };
                Controls.Add(_txtCommand);
                _radLogoShow = null;
                _radLogoHide = null;
                _cmbLogo = null;
            }

            var btnSave = new Button { Left = 430, Top = 160, Width = 75, Text = LanguageManager.GetString("Common.Save", "Salva"), DialogResult = DialogResult.OK };
            btnSave.Click += BtnSave_Click;
            Controls.Add(btnSave);

            var btnCancel = new Button { Left = 515, Top = 160, Width = 75, Text = LanguageManager.GetString("Common.Cancel", "Annulla"), DialogResult = DialogResult.Cancel };
            Controls.Add(btnCancel);

            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }

        private void LoadAdditionalLogos()
        {
            _logos.Clear();
            try
            {
                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AirDirector\CG", false))
                {
                    string logosJson = key?.GetValue("AdditionalLogosJson", "[]")?.ToString() ?? "[]";
                    var data = JsonConvert.DeserializeObject<List<AdditionalLogo>>(logosJson) ?? new List<AdditionalLogo>();
                    _logos.AddRange(data);
                }
            }
            catch
            {
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtName.Text))
            {
                MessageBox.Show(LanguageManager.GetString("CommandManager.ErrorEmptyName", "Il nome non può essere vuoto."));
                DialogResult = DialogResult.None;
                return;
            }

            if (_type == "Logo")
            {
                var selectedLogo = _cmbLogo?.SelectedItem as AdditionalLogo;
                if (selectedLogo == null)
                {
                    MessageBox.Show(LanguageManager.GetString("CommandManager.ErrorEmptyLogo", "Seleziona un logo."));
                    DialogResult = DialogResult.None;
                    return;
                }

                _entry.Type = _radLogoHide?.Checked == true ? "LogoHide" : "LogoShow";
                _entry.CommandString = selectedLogo.ImagePath ?? "";
            }
            else
            {
                if (string.IsNullOrWhiteSpace(_txtCommand?.Text))
                {
                    MessageBox.Show(LanguageManager.GetString("CommandManager.ErrorEmptyString", "La stringa non può essere vuota."));
                    DialogResult = DialogResult.None;
                    return;
                }

                _entry.Type = _type == "UDP" ? "UDP" : "HTTP";
                _entry.CommandString = _txtCommand.Text.Trim();
            }

            _entry.Name = _txtName.Text.Trim();
        }
    }
}
