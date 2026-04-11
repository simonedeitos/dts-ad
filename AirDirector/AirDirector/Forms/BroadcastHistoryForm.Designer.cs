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
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle4 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle6 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle7 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle10 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle8 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle9 = new DataGridViewCellStyle();
            filterPanel = new Panel();
            lblFrom = new Label();
            dtpFrom = new DateTimePicker();
            lblTo = new Label();
            dtpTo = new DateTimePicker();
            lblType = new Label();
            cmbType = new ComboBox();
            lblSearch = new Label();
            txtSearch = new TextBox();
            btnRefresh = new Button();
            btnExport = new Button();
            dgvHistory = new DataGridView();
            colDate = new DataGridViewTextBoxColumn();
            colTime = new DataGridViewTextBoxColumn();
            colType = new DataGridViewTextBoxColumn();
            colArtist = new DataGridViewTextBoxColumn();
            colTitle = new DataGridViewTextBoxColumn();
            colDuration = new DataGridViewTextBoxColumn();
            statsPanel = new Panel();
            lblTotal = new Label();
            lblTotalDuration = new Label();
            lblTopArtist = new Label();
            lblTopTrack = new Label();
            btnStatistics = new Button();
            filterPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvHistory).BeginInit();
            statsPanel.SuspendLayout();
            SuspendLayout();
            // 
            // filterPanel
            // 
            filterPanel.BackColor = Color.FromArgb(40, 40, 40);
            filterPanel.Controls.Add(lblFrom);
            filterPanel.Controls.Add(dtpFrom);
            filterPanel.Controls.Add(lblTo);
            filterPanel.Controls.Add(dtpTo);
            filterPanel.Controls.Add(lblType);
            filterPanel.Controls.Add(cmbType);
            filterPanel.Controls.Add(lblSearch);
            filterPanel.Controls.Add(txtSearch);
            filterPanel.Controls.Add(btnRefresh);
            filterPanel.Controls.Add(btnExport);
            filterPanel.Dock = DockStyle.Top;
            filterPanel.Location = new Point(0, 0);
            filterPanel.Name = "filterPanel";
            filterPanel.Padding = new Padding(10, 8, 10, 8);
            filterPanel.Size = new Size(1253, 55);
            filterPanel.TabIndex = 2;
            // 
            // lblFrom
            // 
            lblFrom.AutoSize = true;
            lblFrom.Font = new Font("Segoe UI", 9F);
            lblFrom.ForeColor = Color.White;
            lblFrom.Location = new Point(10, 17);
            lblFrom.Name = "lblFrom";
            lblFrom.Size = new Size(24, 15);
            lblFrom.TabIndex = 0;
            lblFrom.Text = "Da:";
            // 
            // dtpFrom
            // 
            dtpFrom.Format = DateTimePickerFormat.Short;
            dtpFrom.Location = new Point(50, 12);
            dtpFrom.Name = "dtpFrom";
            dtpFrom.Size = new Size(110, 23);
            dtpFrom.TabIndex = 1;
            // 
            // lblTo
            // 
            lblTo.AutoSize = true;
            lblTo.Font = new Font("Segoe UI", 9F);
            lblTo.ForeColor = Color.White;
            lblTo.Location = new Point(176, 17);
            lblTo.Name = "lblTo";
            lblTo.Size = new Size(18, 15);
            lblTo.TabIndex = 2;
            lblTo.Text = "A:";
            // 
            // dtpTo
            // 
            dtpTo.Format = DateTimePickerFormat.Short;
            dtpTo.Location = new Point(211, 13);
            dtpTo.Name = "dtpTo";
            dtpTo.Size = new Size(110, 23);
            dtpTo.TabIndex = 3;
            // 
            // lblType
            // 
            lblType.AutoSize = true;
            lblType.Font = new Font("Segoe UI", 9F);
            lblType.ForeColor = Color.White;
            lblType.Location = new Point(351, 17);
            lblType.Name = "lblType";
            lblType.Size = new Size(34, 15);
            lblType.TabIndex = 4;
            lblType.Text = "Tipo:";
            // 
            // cmbType
            // 
            cmbType.BackColor = Color.FromArgb(50, 50, 50);
            cmbType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbType.FlatStyle = FlatStyle.Flat;
            cmbType.ForeColor = Color.White;
            cmbType.Items.AddRange(new object[] { "Tutti", "Music", "Clip" });
            cmbType.Location = new Point(397, 13);
            cmbType.Name = "cmbType";
            cmbType.Size = new Size(100, 23);
            cmbType.TabIndex = 5;
            // 
            // lblSearch
            // 
            lblSearch.AutoSize = true;
            lblSearch.Font = new Font("Segoe UI", 9F);
            lblSearch.ForeColor = Color.White;
            lblSearch.Location = new Point(546, 16);
            lblSearch.Name = "lblSearch";
            lblSearch.Size = new Size(40, 15);
            lblSearch.TabIndex = 6;
            lblSearch.Text = "Cerca:";
            // 
            // txtSearch
            // 
            txtSearch.BackColor = Color.FromArgb(50, 50, 50);
            txtSearch.BorderStyle = BorderStyle.FixedSingle;
            txtSearch.ForeColor = Color.White;
            txtSearch.Location = new Point(604, 12);
            txtSearch.Name = "txtSearch";
            txtSearch.Size = new Size(150, 23);
            txtSearch.TabIndex = 7;
            // 
            // btnRefresh
            // 
            btnRefresh.BackColor = Color.FromArgb(0, 120, 215);
            btnRefresh.Cursor = Cursors.Hand;
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.FlatStyle = FlatStyle.Flat;
            btnRefresh.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnRefresh.ForeColor = Color.White;
            btnRefresh.Location = new Point(776, 10);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new Size(110, 30);
            btnRefresh.TabIndex = 8;
            btnRefresh.Text = "🔄 Aggiorna";
            btnRefresh.UseVisualStyleBackColor = false;
            // 
            // btnExport
            // 
            btnExport.BackColor = Color.FromArgb(0, 150, 136);
            btnExport.Cursor = Cursors.Hand;
            btnExport.FlatAppearance.BorderSize = 0;
            btnExport.FlatStyle = FlatStyle.Flat;
            btnExport.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnExport.ForeColor = Color.White;
            btnExport.Location = new Point(950, 11);
            btnExport.Name = "btnExport";
            btnExport.Size = new Size(130, 30);
            btnExport.TabIndex = 9;
            btnExport.Text = "💾 Esporta CSV";
            btnExport.UseVisualStyleBackColor = false;
            // 
            // dgvHistory
            // 
            dgvHistory.AllowUserToAddRows = false;
            dgvHistory.AllowUserToDeleteRows = false;
            dgvHistory.AllowUserToResizeRows = false;
            dataGridViewCellStyle6.BackColor = Color.FromArgb(35, 35, 35);
            dgvHistory.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle6;
            dgvHistory.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvHistory.BackgroundColor = Color.FromArgb(30, 30, 30);
            dgvHistory.BorderStyle = BorderStyle.None;
            dataGridViewCellStyle7.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle7.BackColor = Color.FromArgb(50, 50, 50);
            dataGridViewCellStyle7.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            dataGridViewCellStyle7.ForeColor = Color.White;
            dgvHistory.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle7;
            dgvHistory.ColumnHeadersHeight = 40;
            dgvHistory.Columns.AddRange(new DataGridViewColumn[] { colDate, colTime, colType, colArtist, colTitle, colDuration });
            dataGridViewCellStyle10.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle10.BackColor = Color.FromArgb(40, 40, 40);
            dataGridViewCellStyle10.Font = new Font("Segoe UI", 10F);
            dataGridViewCellStyle10.ForeColor = Color.White;
            dataGridViewCellStyle10.Padding = new Padding(5);
            dataGridViewCellStyle10.SelectionBackColor = Color.FromArgb(0, 120, 215);
            dataGridViewCellStyle10.SelectionForeColor = Color.White;
            dataGridViewCellStyle10.WrapMode = DataGridViewTriState.False;
            dgvHistory.DefaultCellStyle = dataGridViewCellStyle10;
            dgvHistory.Dock = DockStyle.Fill;
            dgvHistory.EnableHeadersVisualStyles = false;
            dgvHistory.Font = new Font("Segoe UI", 10F);
            dgvHistory.GridColor = Color.FromArgb(60, 60, 60);
            dgvHistory.Location = new Point(0, 55);
            dgvHistory.MultiSelect = false;
            dgvHistory.Name = "dgvHistory";
            dgvHistory.ReadOnly = true;
            dgvHistory.RowHeadersVisible = false;
            dgvHistory.RowTemplate.Height = 32;
            dgvHistory.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvHistory.Size = new Size(1253, 595);
            dgvHistory.TabIndex = 0;
            // 
            // colDate
            // 
            dataGridViewCellStyle8.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colDate.DefaultCellStyle = dataGridViewCellStyle8;
            colDate.FillWeight = 12F;
            colDate.HeaderText = "📅 Data";
            colDate.Name = "colDate";
            colDate.ReadOnly = true;
            // 
            // colTime
            // 
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colTime.DefaultCellStyle = dataGridViewCellStyle1;
            colTime.FillWeight = 10F;
            colTime.HeaderText = "🕐 Ora";
            colTime.Name = "colTime";
            colTime.ReadOnly = true;
            // 
            // colType
            // 
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colType.DefaultCellStyle = dataGridViewCellStyle2;
            colType.FillWeight = 10F;
            colType.HeaderText = "🎵 Tipo";
            colType.Name = "colType";
            colType.ReadOnly = true;
            // 
            // colArtist
            // 
            dataGridViewCellStyle9.Alignment = DataGridViewContentAlignment.MiddleLeft;
            colArtist.DefaultCellStyle = dataGridViewCellStyle9;
            colArtist.FillWeight = 25F;
            colArtist.HeaderText = "🎤 Artista";
            colArtist.Name = "colArtist";
            colArtist.ReadOnly = true;
            // 
            // colTitle
            // 
            dataGridViewCellStyle4.Alignment = DataGridViewContentAlignment.MiddleLeft;
            colTitle.DefaultCellStyle = dataGridViewCellStyle4;
            colTitle.FillWeight = 25F;
            colTitle.HeaderText = "🎶 Titolo";
            colTitle.Name = "colTitle";
            colTitle.ReadOnly = true;
            // 
            // colDuration
            // 
            dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colDuration.DefaultCellStyle = dataGridViewCellStyle3;
            colDuration.FillWeight = 10F;
            colDuration.HeaderText = "⏱️ Durata";
            colDuration.Name = "colDuration";
            colDuration.ReadOnly = true;
            // 
            // statsPanel
            // 
            statsPanel.BackColor = Color.FromArgb(35, 35, 35);
            statsPanel.Controls.Add(lblTotal);
            statsPanel.Controls.Add(lblTotalDuration);
            statsPanel.Controls.Add(lblTopArtist);
            statsPanel.Controls.Add(lblTopTrack);
            statsPanel.Controls.Add(btnStatistics);
            statsPanel.Dock = DockStyle.Bottom;
            statsPanel.Location = new Point(0, 650);
            statsPanel.Name = "statsPanel";
            statsPanel.Padding = new Padding(10, 8, 10, 8);
            statsPanel.Size = new Size(1253, 50);
            statsPanel.TabIndex = 1;
            // 
            // lblTotal
            // 
            lblTotal.AutoSize = true;
            lblTotal.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblTotal.ForeColor = Color.White;
            lblTotal.Location = new Point(10, 15);
            lblTotal.Name = "lblTotal";
            lblTotal.Size = new Size(54, 15);
            lblTotal.TabIndex = 0;
            lblTotal.Text = "Totale: 0";
            // 
            // lblTotalDuration
            // 
            lblTotalDuration.AutoSize = true;
            lblTotalDuration.Font = new Font("Segoe UI", 9F);
            lblTotalDuration.ForeColor = Color.FromArgb(0, 255, 65);
            lblTotalDuration.Location = new Point(110, 15);
            lblTotalDuration.Name = "lblTotalDuration";
            lblTotalDuration.Size = new Size(90, 15);
            lblTotalDuration.TabIndex = 1;
            lblTotalDuration.Text = "Durata: 00:00:00";
            // 
            // lblTopArtist
            // 
            lblTopArtist.AutoSize = true;
            lblTopArtist.Font = new Font("Segoe UI", 9F);
            lblTopArtist.ForeColor = Color.FromArgb(0, 150, 136);
            lblTopArtist.Location = new Point(280, 15);
            lblTopArtist.Name = "lblTopArtist";
            lblTopArtist.Size = new Size(75, 15);
            lblTopArtist.TabIndex = 2;
            lblTopArtist.Text = "Top Artista: -";
            // 
            // lblTopTrack
            // 
            lblTopTrack.AutoSize = true;
            lblTopTrack.Font = new Font("Segoe UI", 9F);
            lblTopTrack.ForeColor = Color.FromArgb(0, 150, 136);
            lblTopTrack.Location = new Point(500, 15);
            lblTopTrack.Name = "lblTopTrack";
            lblTopTrack.Size = new Size(72, 15);
            lblTopTrack.TabIndex = 3;
            lblTopTrack.Text = "Top Brano: -";
            // 
            // btnStatistics
            // 
            btnStatistics.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnStatistics.BackColor = Color.FromArgb(40, 167, 69);
            btnStatistics.Cursor = Cursors.Hand;
            btnStatistics.FlatAppearance.BorderSize = 0;
            btnStatistics.FlatStyle = FlatStyle.Flat;
            btnStatistics.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnStatistics.ForeColor = Color.White;
            btnStatistics.Location = new Point(2013, 9);
            btnStatistics.Name = "btnStatistics";
            btnStatistics.Size = new Size(130, 32);
            btnStatistics.TabIndex = 4;
            btnStatistics.Text = "📊 Statistiche";
            btnStatistics.UseVisualStyleBackColor = false;
            // 
            // BroadcastHistoryForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(28, 28, 28);
            ClientSize = new Size(1253, 700);
            Controls.Add(dgvHistory);
            Controls.Add(statsPanel);
            Controls.Add(filterPanel);
            ForeColor = Color.White;
            MinimumSize = new Size(900, 600);
            Name = "BroadcastHistoryForm";
            Text = "Broadcast History";
            filterPanel.ResumeLayout(false);
            filterPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvHistory).EndInit();
            statsPanel.ResumeLayout(false);
            statsPanel.PerformLayout();
            ResumeLayout(false);
        }
    }
}
