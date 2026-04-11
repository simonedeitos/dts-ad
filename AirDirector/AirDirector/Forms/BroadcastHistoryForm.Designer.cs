namespace AirDirector.Forms
{
    partial class BroadcastHistoryForm
    {
        private System.ComponentModel.IContainer components = null;

        // Filter panel controls
        private System.Windows.Forms.Panel filterPanel;
        private System.Windows.Forms.Label lblFrom;
        private System.Windows.Forms.Label lblTo;
        private System.Windows.Forms.DateTimePicker dtpFrom;
        private System.Windows.Forms.DateTimePicker dtpTo;
        private System.Windows.Forms.Label lblType;
        private System.Windows.Forms.ComboBox cmbType;
        private System.Windows.Forms.Label lblSearch;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnExport;

        // DataGridView
        private System.Windows.Forms.DataGridView dgvHistory;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn colType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colArtist;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTitle;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDuration;

        // Stats panel
        private System.Windows.Forms.Panel statsPanel;
        private System.Windows.Forms.Label lblTotal;
        private System.Windows.Forms.Label lblTotalDuration;
        private System.Windows.Forms.Label lblTopArtist;
        private System.Windows.Forms.Label lblTopTrack;
        private System.Windows.Forms.Button btnStatistics;

        private void InitializeComponent()
        {
            this.filterPanel = new System.Windows.Forms.Panel();
            this.lblFrom = new System.Windows.Forms.Label();
            this.dtpFrom = new System.Windows.Forms.DateTimePicker();
            this.lblTo = new System.Windows.Forms.Label();
            this.dtpTo = new System.Windows.Forms.DateTimePicker();
            this.lblType = new System.Windows.Forms.Label();
            this.cmbType = new System.Windows.Forms.ComboBox();
            this.lblSearch = new System.Windows.Forms.Label();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.btnExport = new System.Windows.Forms.Button();

            this.dgvHistory = new System.Windows.Forms.DataGridView();
            this.colDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colArtist = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTitle = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDuration = new System.Windows.Forms.DataGridViewTextBoxColumn();

            this.statsPanel = new System.Windows.Forms.Panel();
            this.lblTotal = new System.Windows.Forms.Label();
            this.lblTotalDuration = new System.Windows.Forms.Label();
            this.lblTopArtist = new System.Windows.Forms.Label();
            this.lblTopTrack = new System.Windows.Forms.Label();
            this.btnStatistics = new System.Windows.Forms.Button();

            this.filterPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHistory)).BeginInit();
            this.statsPanel.SuspendLayout();
            this.SuspendLayout();

            // ── filterPanel ──────────────────────────────────────────────
            this.filterPanel.BackColor = System.Drawing.Color.FromArgb(40, 40, 40);
            this.filterPanel.Controls.Add(this.lblFrom);
            this.filterPanel.Controls.Add(this.dtpFrom);
            this.filterPanel.Controls.Add(this.lblTo);
            this.filterPanel.Controls.Add(this.dtpTo);
            this.filterPanel.Controls.Add(this.lblType);
            this.filterPanel.Controls.Add(this.cmbType);
            this.filterPanel.Controls.Add(this.lblSearch);
            this.filterPanel.Controls.Add(this.txtSearch);
            this.filterPanel.Controls.Add(this.btnRefresh);
            this.filterPanel.Controls.Add(this.btnExport);
            this.filterPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.filterPanel.Height = 55;
            this.filterPanel.Name = "filterPanel";
            this.filterPanel.Padding = new System.Windows.Forms.Padding(10, 8, 10, 8);

            // lblFrom
            this.lblFrom.AutoSize = true;
            this.lblFrom.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblFrom.ForeColor = System.Drawing.Color.White;
            this.lblFrom.Location = new System.Drawing.Point(10, 17);
            this.lblFrom.Name = "lblFrom";
            this.lblFrom.Text = "Da:";

            // dtpFrom
            this.dtpFrom.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpFrom.Location = new System.Drawing.Point(40, 13);
            this.dtpFrom.Name = "dtpFrom";
            this.dtpFrom.Size = new System.Drawing.Size(110, 25);

            // lblTo
            this.lblTo.AutoSize = true;
            this.lblTo.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblTo.ForeColor = System.Drawing.Color.White;
            this.lblTo.Location = new System.Drawing.Point(160, 17);
            this.lblTo.Name = "lblTo";
            this.lblTo.Text = "A:";

            // dtpTo
            this.dtpTo.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpTo.Location = new System.Drawing.Point(185, 13);
            this.dtpTo.Name = "dtpTo";
            this.dtpTo.Size = new System.Drawing.Size(110, 25);

            // lblType
            this.lblType.AutoSize = true;
            this.lblType.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblType.ForeColor = System.Drawing.Color.White;
            this.lblType.Location = new System.Drawing.Point(305, 17);
            this.lblType.Name = "lblType";
            this.lblType.Text = "Tipo:";

            // cmbType
            this.cmbType.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
            this.cmbType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbType.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbType.ForeColor = System.Drawing.Color.White;
            this.cmbType.Items.AddRange(new object[] { "Tutti", "Music", "Clip" });
            this.cmbType.Location = new System.Drawing.Point(345, 13);
            this.cmbType.Name = "cmbType";
            this.cmbType.SelectedIndex = 0;
            this.cmbType.Size = new System.Drawing.Size(100, 25);

            // lblSearch
            this.lblSearch.AutoSize = true;
            this.lblSearch.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblSearch.ForeColor = System.Drawing.Color.White;
            this.lblSearch.Location = new System.Drawing.Point(455, 17);
            this.lblSearch.Name = "lblSearch";
            this.lblSearch.Text = "Cerca:";

            // txtSearch
            this.txtSearch.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
            this.txtSearch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtSearch.ForeColor = System.Drawing.Color.White;
            this.txtSearch.Location = new System.Drawing.Point(505, 13);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(150, 25);

            // btnRefresh
            this.btnRefresh.BackColor = System.Drawing.Color.FromArgb(0, 120, 215);
            this.btnRefresh.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefresh.FlatAppearance.BorderSize = 0;
            this.btnRefresh.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnRefresh.ForeColor = System.Drawing.Color.White;
            this.btnRefresh.Location = new System.Drawing.Point(665, 11);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(110, 30);
            this.btnRefresh.Text = "🔄 Aggiorna";

            // btnExport
            this.btnExport.BackColor = System.Drawing.Color.FromArgb(0, 150, 136);
            this.btnExport.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnExport.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExport.FlatAppearance.BorderSize = 0;
            this.btnExport.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnExport.ForeColor = System.Drawing.Color.White;
            this.btnExport.Location = new System.Drawing.Point(785, 11);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(130, 30);
            this.btnExport.Text = "💾 Esporta CSV";

            // ── dgvHistory ───────────────────────────────────────────────
            this.dgvHistory.AllowUserToAddRows = false;
            this.dgvHistory.AllowUserToDeleteRows = false;
            this.dgvHistory.AllowUserToResizeRows = false;
            this.dgvHistory.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvHistory.BackgroundColor = System.Drawing.Color.FromArgb(30, 30, 30);
            this.dgvHistory.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvHistory.ColumnHeadersHeight = 40;
            System.Windows.Forms.DataGridViewCellStyle dgvHeaderStyle = new System.Windows.Forms.DataGridViewCellStyle();
            dgvHeaderStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dgvHeaderStyle.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
            dgvHeaderStyle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            dgvHeaderStyle.ForeColor = System.Drawing.Color.White;
            this.dgvHistory.ColumnHeadersDefaultCellStyle = dgvHeaderStyle;
            System.Windows.Forms.DataGridViewCellStyle dgvDefaultStyle = new System.Windows.Forms.DataGridViewCellStyle();
            dgvDefaultStyle.BackColor = System.Drawing.Color.FromArgb(40, 40, 40);
            dgvDefaultStyle.ForeColor = System.Drawing.Color.White;
            dgvDefaultStyle.Padding = new System.Windows.Forms.Padding(5);
            dgvDefaultStyle.SelectionBackColor = System.Drawing.Color.FromArgb(0, 120, 215);
            dgvDefaultStyle.SelectionForeColor = System.Drawing.Color.White;
            this.dgvHistory.DefaultCellStyle = dgvDefaultStyle;
            System.Windows.Forms.DataGridViewCellStyle dgvAltStyle = new System.Windows.Forms.DataGridViewCellStyle();
            dgvAltStyle.BackColor = System.Drawing.Color.FromArgb(35, 35, 35);
            this.dgvHistory.AlternatingRowsDefaultCellStyle = dgvAltStyle;
            this.dgvHistory.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colDate,
                this.colTime,
                this.colType,
                this.colArtist,
                this.colTitle,
                this.colDuration
            });
            this.dgvHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvHistory.EnableHeadersVisualStyles = false;
            this.dgvHistory.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.dgvHistory.ForeColor = System.Drawing.Color.White;
            this.dgvHistory.GridColor = System.Drawing.Color.FromArgb(60, 60, 60);
            this.dgvHistory.MultiSelect = false;
            this.dgvHistory.Name = "dgvHistory";
            this.dgvHistory.ReadOnly = true;
            this.dgvHistory.RowHeadersVisible = false;
            this.dgvHistory.RowTemplate.Height = 32;
            this.dgvHistory.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.dgvHistory.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;

            // colDate
            System.Windows.Forms.DataGridViewCellStyle colCenterStyle = new System.Windows.Forms.DataGridViewCellStyle();
            colCenterStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            System.Windows.Forms.DataGridViewCellStyle colLeftStyle = new System.Windows.Forms.DataGridViewCellStyle();
            colLeftStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            this.colDate.DefaultCellStyle = colCenterStyle;
            this.colDate.FillWeight = 12F;
            this.colDate.HeaderText = "📅 Data";
            this.colDate.Name = "colDate";
            // colTime
            this.colTime.DefaultCellStyle = colCenterStyle;
            this.colTime.FillWeight = 10F;
            this.colTime.HeaderText = "🕐 Ora";
            this.colTime.Name = "colTime";
            // colType
            this.colType.DefaultCellStyle = colCenterStyle;
            this.colType.FillWeight = 10F;
            this.colType.HeaderText = "🎵 Tipo";
            this.colType.Name = "colType";
            // colArtist
            this.colArtist.DefaultCellStyle = colLeftStyle;
            this.colArtist.FillWeight = 25F;
            this.colArtist.HeaderText = "🎤 Artista";
            this.colArtist.Name = "colArtist";
            // colTitle
            this.colTitle.DefaultCellStyle = colLeftStyle;
            this.colTitle.FillWeight = 25F;
            this.colTitle.HeaderText = "🎶 Titolo";
            this.colTitle.Name = "colTitle";
            // colDuration
            this.colDuration.DefaultCellStyle = colCenterStyle;
            this.colDuration.FillWeight = 10F;
            this.colDuration.HeaderText = "⏱️ Durata";
            this.colDuration.Name = "colDuration";

            // ── statsPanel ───────────────────────────────────────────────
            this.statsPanel.BackColor = System.Drawing.Color.FromArgb(35, 35, 35);
            this.statsPanel.Controls.Add(this.lblTotal);
            this.statsPanel.Controls.Add(this.lblTotalDuration);
            this.statsPanel.Controls.Add(this.lblTopArtist);
            this.statsPanel.Controls.Add(this.lblTopTrack);
            this.statsPanel.Controls.Add(this.btnStatistics);
            this.statsPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.statsPanel.Height = 50;
            this.statsPanel.Name = "statsPanel";
            this.statsPanel.Padding = new System.Windows.Forms.Padding(10, 8, 10, 8);

            // lblTotal
            this.lblTotal.AutoSize = true;
            this.lblTotal.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblTotal.ForeColor = System.Drawing.Color.White;
            this.lblTotal.Location = new System.Drawing.Point(10, 15);
            this.lblTotal.Name = "lblTotal";
            this.lblTotal.Text = "Totale: 0";

            // lblTotalDuration
            this.lblTotalDuration.AutoSize = true;
            this.lblTotalDuration.Font = new System.Drawing.Font("Segoe UI", 9F);
            // AppTheme.LEDGreen = #00FF41
            this.lblTotalDuration.ForeColor = System.Drawing.Color.FromArgb(0, 255, 65);
            this.lblTotalDuration.Location = new System.Drawing.Point(110, 15);
            this.lblTotalDuration.Name = "lblTotalDuration";
            this.lblTotalDuration.Text = "Durata: 00:00:00";

            // lblTopArtist
            this.lblTopArtist.AutoSize = true;
            this.lblTopArtist.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblTopArtist.ForeColor = System.Drawing.Color.FromArgb(0, 150, 136);
            this.lblTopArtist.Location = new System.Drawing.Point(280, 15);
            this.lblTopArtist.Name = "lblTopArtist";
            this.lblTopArtist.Text = "Top Artista: -";

            // lblTopTrack
            this.lblTopTrack.AutoSize = true;
            this.lblTopTrack.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblTopTrack.ForeColor = System.Drawing.Color.FromArgb(0, 150, 136);
            this.lblTopTrack.Location = new System.Drawing.Point(500, 15);
            this.lblTopTrack.Name = "lblTopTrack";
            this.lblTopTrack.Text = "Top Brano: -";

            // btnStatistics
            this.btnStatistics.Anchor = System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Top;
            this.btnStatistics.BackColor = System.Drawing.Color.FromArgb(40, 167, 69);
            this.btnStatistics.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnStatistics.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStatistics.FlatAppearance.BorderSize = 0;
            this.btnStatistics.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnStatistics.ForeColor = System.Drawing.Color.White;
            this.btnStatistics.Location = new System.Drawing.Point(960, 9);
            this.btnStatistics.Name = "btnStatistics";
            this.btnStatistics.Size = new System.Drawing.Size(130, 32);
            this.btnStatistics.Text = "📊 Statistiche";

            // ── Form ─────────────────────────────────────────────────────
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(28, 28, 28);
            this.ClientSize = new System.Drawing.Size(1100, 700);
            // Add controls: Fill first, then Bottom, then Top (WinForms docking order)
            this.Controls.Add(this.dgvHistory);
            this.Controls.Add(this.statsPanel);
            this.Controls.Add(this.filterPanel);
            this.ForeColor = System.Drawing.Color.White;
            this.MinimumSize = new System.Drawing.Size(900, 600);
            this.Name = "BroadcastHistoryForm";
            this.Text = "Broadcast History";

            this.filterPanel.ResumeLayout(false);
            this.filterPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHistory)).EndInit();
            this.statsPanel.ResumeLayout(false);
            this.statsPanel.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}
