namespace AirDirector.Forms
{
    partial class DatabaseModificationsForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TabControl tabMain;
        private System.Windows.Forms.TabPage tabVerify;
        private System.Windows.Forms.TabPage tabFindReplace;

        // --- Tab 1: Verify Audio Files ---
        private System.Windows.Forms.Panel panelVerifyTop;
        private System.Windows.Forms.CheckBox chkVerifyMusic;
        private System.Windows.Forms.CheckBox chkVerifyClips;
        private System.Windows.Forms.Button btnVerify;
        private System.Windows.Forms.Label lblVerifyStatus;
        private System.Windows.Forms.ProgressBar progressVerify;
        private System.Windows.Forms.DataGridView dgvMissing;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMissingArtist;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMissingTitle;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMissingPath;

        // --- Tab 2: Find & Replace ---
        private System.Windows.Forms.Panel panelFRTop;
        private System.Windows.Forms.CheckBox chkFRMusic;
        private System.Windows.Forms.CheckBox chkFRClips;
        private System.Windows.Forms.CheckBox chkFRSettings;
        private System.Windows.Forms.CheckBox chkFRDatabase;
        private System.Windows.Forms.Label lblFind;
        private System.Windows.Forms.TextBox txtFind;
        private System.Windows.Forms.Label lblReplace;
        private System.Windows.Forms.TextBox txtReplace;
        private System.Windows.Forms.Button btnAnalyze;
        private System.Windows.Forms.Button btnReplaceAll;
        private System.Windows.Forms.Label lblOccurrences;
        private System.Windows.Forms.DataGridView dgvResults;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSource;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCurrentPath;
        private System.Windows.Forms.DataGridViewTextBoxColumn colProposedPath;

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle4 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle5 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle6 = new DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DatabaseModificationsForm));
            tabMain = new TabControl();
            tabVerify = new TabPage();
            dgvMissing = new DataGridView();
            colMissingArtist = new DataGridViewTextBoxColumn();
            colMissingTitle = new DataGridViewTextBoxColumn();
            colMissingPath = new DataGridViewTextBoxColumn();
            progressVerify = new ProgressBar();
            lblVerifyStatus = new Label();
            panelVerifyTop = new Panel();
            chkVerifyMusic = new CheckBox();
            chkVerifyClips = new CheckBox();
            btnVerify = new Button();
            tabFindReplace = new TabPage();
            dgvResults = new DataGridView();
            colSource = new DataGridViewTextBoxColumn();
            colCurrentPath = new DataGridViewTextBoxColumn();
            colProposedPath = new DataGridViewTextBoxColumn();
            lblOccurrences = new Label();
            panelFRTop = new Panel();
            chkFRMusic = new CheckBox();
            chkFRClips = new CheckBox();
            chkFRSettings = new CheckBox();
            chkFRDatabase = new CheckBox();
            lblFind = new Label();
            txtFind = new TextBox();
            lblReplace = new Label();
            txtReplace = new TextBox();
            btnAnalyze = new Button();
            btnReplaceAll = new Button();
            tabMain.SuspendLayout();
            tabVerify.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvMissing).BeginInit();
            panelVerifyTop.SuspendLayout();
            tabFindReplace.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvResults).BeginInit();
            panelFRTop.SuspendLayout();
            SuspendLayout();
            // 
            // tabMain
            // 
            tabMain.Controls.Add(tabVerify);
            tabMain.Controls.Add(tabFindReplace);
            tabMain.Dock = DockStyle.Fill;
            tabMain.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            tabMain.Location = new Point(0, 0);
            tabMain.Name = "tabMain";
            tabMain.SelectedIndex = 0;
            tabMain.Size = new Size(1000, 680);
            tabMain.TabIndex = 0;
            // 
            // tabVerify
            // 
            tabVerify.BackColor = Color.FromArgb(28, 28, 28);
            tabVerify.Controls.Add(dgvMissing);
            tabVerify.Controls.Add(progressVerify);
            tabVerify.Controls.Add(lblVerifyStatus);
            tabVerify.Controls.Add(panelVerifyTop);
            tabVerify.Location = new Point(4, 26);
            tabVerify.Name = "tabVerify";
            tabVerify.Padding = new Padding(8);
            tabVerify.Size = new Size(992, 650);
            tabVerify.TabIndex = 0;
            tabVerify.Text = "Verifica Esistenza File Audio";
            // 
            // dgvMissing
            // 
            dgvMissing.AllowUserToAddRows = false;
            dgvMissing.AllowUserToDeleteRows = false;
            dgvMissing.AllowUserToResizeRows = false;
            dataGridViewCellStyle1.BackColor = Color.FromArgb(35, 35, 35);
            dgvMissing.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            dgvMissing.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvMissing.BackgroundColor = Color.FromArgb(30, 30, 30);
            dgvMissing.BorderStyle = BorderStyle.None;
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.BackColor = Color.FromArgb(50, 50, 50);
            dataGridViewCellStyle2.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dataGridViewCellStyle2.ForeColor = Color.White;
            dgvMissing.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            dgvMissing.ColumnHeadersHeight = 35;
            dgvMissing.Columns.AddRange(new DataGridViewColumn[] { colMissingArtist, colMissingTitle, colMissingPath });
            dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = Color.FromArgb(40, 40, 40);
            dataGridViewCellStyle3.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle3.ForeColor = Color.White;
            dataGridViewCellStyle3.SelectionBackColor = Color.FromArgb(0, 120, 215);
            dataGridViewCellStyle3.SelectionForeColor = Color.White;
            dataGridViewCellStyle3.WrapMode = DataGridViewTriState.False;
            dgvMissing.DefaultCellStyle = dataGridViewCellStyle3;
            dgvMissing.EnableHeadersVisualStyles = false;
            dgvMissing.GridColor = Color.FromArgb(60, 60, 60);
            dgvMissing.Location = new Point(8, 114);
            dgvMissing.MultiSelect = false;
            dgvMissing.Name = "dgvMissing";
            dgvMissing.ReadOnly = true;
            dgvMissing.RowHeadersVisible = false;
            dgvMissing.RowTemplate.Height = 28;
            dgvMissing.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvMissing.Size = new Size(976, 519);
            dgvMissing.TabIndex = 3;
            // 
            // colMissingArtist
            // 
            colMissingArtist.FillWeight = 20F;
            colMissingArtist.HeaderText = "Artista";
            colMissingArtist.Name = "colMissingArtist";
            colMissingArtist.ReadOnly = true;
            // 
            // colMissingTitle
            // 
            colMissingTitle.FillWeight = 25F;
            colMissingTitle.HeaderText = "Titolo";
            colMissingTitle.Name = "colMissingTitle";
            colMissingTitle.ReadOnly = true;
            // 
            // colMissingPath
            // 
            colMissingPath.FillWeight = 55F;
            colMissingPath.HeaderText = "Percorso File";
            colMissingPath.Name = "colMissingPath";
            colMissingPath.ReadOnly = true;
            // 
            // progressVerify
            // 
            progressVerify.Location = new Point(8, 66);
            progressVerify.Name = "progressVerify";
            progressVerify.Size = new Size(976, 18);
            progressVerify.TabIndex = 1;
            // 
            // lblVerifyStatus
            // 
            lblVerifyStatus.Font = new Font("Segoe UI", 9F);
            lblVerifyStatus.ForeColor = Color.FromArgb(200, 200, 200);
            lblVerifyStatus.Location = new Point(8, 90);
            lblVerifyStatus.Name = "lblVerifyStatus";
            lblVerifyStatus.Size = new Size(970, 20);
            lblVerifyStatus.TabIndex = 2;
            // 
            // panelVerifyTop
            // 
            panelVerifyTop.BackColor = Color.FromArgb(35, 35, 35);
            panelVerifyTop.Controls.Add(chkVerifyMusic);
            panelVerifyTop.Controls.Add(chkVerifyClips);
            panelVerifyTop.Controls.Add(btnVerify);
            panelVerifyTop.Dock = DockStyle.Top;
            panelVerifyTop.Location = new Point(8, 8);
            panelVerifyTop.Name = "panelVerifyTop";
            panelVerifyTop.Padding = new Padding(10, 8, 10, 8);
            panelVerifyTop.Size = new Size(976, 50);
            panelVerifyTop.TabIndex = 0;
            // 
            // chkVerifyMusic
            // 
            chkVerifyMusic.AutoSize = true;
            chkVerifyMusic.Checked = true;
            chkVerifyMusic.CheckState = CheckState.Checked;
            chkVerifyMusic.Font = new Font("Segoe UI", 10F);
            chkVerifyMusic.ForeColor = Color.White;
            chkVerifyMusic.Location = new Point(15, 13);
            chkVerifyMusic.Name = "chkVerifyMusic";
            chkVerifyMusic.Size = new Size(64, 23);
            chkVerifyMusic.TabIndex = 0;
            chkVerifyMusic.Text = "Music";
            // 
            // chkVerifyClips
            // 
            chkVerifyClips.AutoSize = true;
            chkVerifyClips.Checked = true;
            chkVerifyClips.CheckState = CheckState.Checked;
            chkVerifyClips.Font = new Font("Segoe UI", 10F);
            chkVerifyClips.ForeColor = Color.White;
            chkVerifyClips.Location = new Point(105, 13);
            chkVerifyClips.Name = "chkVerifyClips";
            chkVerifyClips.Size = new Size(57, 23);
            chkVerifyClips.TabIndex = 1;
            chkVerifyClips.Text = "Clips";
            // 
            // btnVerify
            // 
            btnVerify.BackColor = Color.FromArgb(0, 120, 215);
            btnVerify.Cursor = Cursors.Hand;
            btnVerify.FlatAppearance.BorderSize = 0;
            btnVerify.FlatStyle = FlatStyle.Flat;
            btnVerify.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnVerify.ForeColor = Color.White;
            btnVerify.Location = new Point(220, 10);
            btnVerify.Name = "btnVerify";
            btnVerify.Size = new Size(120, 30);
            btnVerify.TabIndex = 2;
            btnVerify.Text = "🔍 Verifica";
            btnVerify.UseVisualStyleBackColor = false;
            // 
            // tabFindReplace
            // 
            tabFindReplace.BackColor = Color.FromArgb(28, 28, 28);
            tabFindReplace.Controls.Add(dgvResults);
            tabFindReplace.Controls.Add(lblOccurrences);
            tabFindReplace.Controls.Add(panelFRTop);
            tabFindReplace.Location = new Point(4, 26);
            tabFindReplace.Name = "tabFindReplace";
            tabFindReplace.Padding = new Padding(8);
            tabFindReplace.Size = new Size(992, 650);
            tabFindReplace.TabIndex = 1;
            tabFindReplace.Text = "Trova e Sostituisci Percorso";
            // 
            // dgvResults
            // 
            dgvResults.AllowUserToAddRows = false;
            dgvResults.AllowUserToDeleteRows = false;
            dgvResults.AllowUserToResizeRows = false;
            dataGridViewCellStyle4.BackColor = Color.FromArgb(35, 35, 35);
            dgvResults.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle4;
            dgvResults.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvResults.BackgroundColor = Color.FromArgb(30, 30, 30);
            dgvResults.BorderStyle = BorderStyle.None;
            dataGridViewCellStyle5.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle5.BackColor = Color.FromArgb(50, 50, 50);
            dataGridViewCellStyle5.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dataGridViewCellStyle5.ForeColor = Color.White;
            dgvResults.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle5;
            dgvResults.ColumnHeadersHeight = 35;
            dgvResults.Columns.AddRange(new DataGridViewColumn[] { colSource, colCurrentPath, colProposedPath });
            dataGridViewCellStyle6.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle6.BackColor = Color.FromArgb(40, 40, 40);
            dataGridViewCellStyle6.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle6.ForeColor = Color.White;
            dataGridViewCellStyle6.SelectionBackColor = Color.FromArgb(0, 120, 215);
            dataGridViewCellStyle6.SelectionForeColor = Color.White;
            dataGridViewCellStyle6.WrapMode = DataGridViewTriState.False;
            dgvResults.DefaultCellStyle = dataGridViewCellStyle6;
            dgvResults.EnableHeadersVisualStyles = false;
            dgvResults.GridColor = Color.FromArgb(60, 60, 60);
            dgvResults.Location = new Point(8, 135);
            dgvResults.MultiSelect = false;
            dgvResults.Name = "dgvResults";
            dgvResults.ReadOnly = true;
            dgvResults.RowHeadersVisible = false;
            dgvResults.RowTemplate.Height = 28;
            dgvResults.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvResults.Size = new Size(976, 498);
            dgvResults.TabIndex = 2;
            // 
            // colSource
            // 
            colSource.FillWeight = 15F;
            colSource.HeaderText = "Sorgente";
            colSource.Name = "colSource";
            colSource.ReadOnly = true;
            // 
            // colCurrentPath
            // 
            colCurrentPath.FillWeight = 42F;
            colCurrentPath.HeaderText = "Percorso Attuale";
            colCurrentPath.Name = "colCurrentPath";
            colCurrentPath.ReadOnly = true;
            // 
            // colProposedPath
            // 
            colProposedPath.FillWeight = 43F;
            colProposedPath.HeaderText = "Percorso Proposto";
            colProposedPath.Name = "colProposedPath";
            colProposedPath.ReadOnly = true;
            // 
            // lblOccurrences
            // 
            lblOccurrences.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblOccurrences.ForeColor = Color.FromArgb(0, 200, 83);
            lblOccurrences.Location = new Point(8, 111);
            lblOccurrences.Name = "lblOccurrences";
            lblOccurrences.Size = new Size(500, 20);
            lblOccurrences.TabIndex = 1;
            // 
            // panelFRTop
            // 
            panelFRTop.AutoSize = true;
            panelFRTop.BackColor = Color.FromArgb(35, 35, 35);
            panelFRTop.Controls.Add(chkFRMusic);
            panelFRTop.Controls.Add(chkFRClips);
            panelFRTop.Controls.Add(chkFRSettings);
            panelFRTop.Controls.Add(chkFRDatabase);
            panelFRTop.Controls.Add(lblFind);
            panelFRTop.Controls.Add(txtFind);
            panelFRTop.Controls.Add(lblReplace);
            panelFRTop.Controls.Add(txtReplace);
            panelFRTop.Controls.Add(btnAnalyze);
            panelFRTop.Controls.Add(btnReplaceAll);
            panelFRTop.Dock = DockStyle.Top;
            panelFRTop.Location = new Point(8, 8);
            panelFRTop.Name = "panelFRTop";
            panelFRTop.Padding = new Padding(10, 8, 10, 8);
            panelFRTop.Size = new Size(976, 91);
            panelFRTop.TabIndex = 0;
            // 
            // chkFRMusic
            // 
            chkFRMusic.AutoSize = true;
            chkFRMusic.Checked = true;
            chkFRMusic.CheckState = CheckState.Checked;
            chkFRMusic.Font = new Font("Segoe UI", 9F);
            chkFRMusic.ForeColor = Color.White;
            chkFRMusic.Location = new Point(15, 12);
            chkFRMusic.Name = "chkFRMusic";
            chkFRMusic.Size = new Size(58, 19);
            chkFRMusic.TabIndex = 0;
            chkFRMusic.Text = "Music";
            // 
            // chkFRClips
            // 
            chkFRClips.AutoSize = true;
            chkFRClips.Font = new Font("Segoe UI", 9F);
            chkFRClips.ForeColor = Color.White;
            chkFRClips.Location = new Point(100, 12);
            chkFRClips.Name = "chkFRClips";
            chkFRClips.Size = new Size(52, 19);
            chkFRClips.TabIndex = 1;
            chkFRClips.Text = "Clips";
            // 
            // chkFRSettings
            // 
            chkFRSettings.AutoSize = true;
            chkFRSettings.Font = new Font("Segoe UI", 9F);
            chkFRSettings.ForeColor = Color.White;
            chkFRSettings.Location = new Point(180, 12);
            chkFRSettings.Name = "chkFRSettings";
            chkFRSettings.Size = new Size(68, 19);
            chkFRSettings.TabIndex = 2;
            chkFRSettings.Text = "Settings";
            // 
            // chkFRDatabase
            // 
            chkFRDatabase.AutoSize = true;
            chkFRDatabase.Font = new Font("Segoe UI", 9F);
            chkFRDatabase.ForeColor = Color.White;
            chkFRDatabase.Location = new Point(275, 12);
            chkFRDatabase.Name = "chkFRDatabase";
            chkFRDatabase.Size = new Size(74, 19);
            chkFRDatabase.TabIndex = 3;
            chkFRDatabase.Text = "Database";
            // 
            // lblFind
            // 
            lblFind.AutoSize = true;
            lblFind.Font = new Font("Segoe UI", 9F);
            lblFind.ForeColor = Color.White;
            lblFind.Location = new Point(15, 58);
            lblFind.Name = "lblFind";
            lblFind.Size = new Size(39, 15);
            lblFind.TabIndex = 4;
            lblFind.Text = "Trova:";
            // 
            // txtFind
            // 
            txtFind.BackColor = Color.FromArgb(50, 50, 50);
            txtFind.BorderStyle = BorderStyle.FixedSingle;
            txtFind.Font = new Font("Segoe UI", 9F);
            txtFind.ForeColor = Color.White;
            txtFind.Location = new Point(70, 55);
            txtFind.Name = "txtFind";
            txtFind.Size = new Size(280, 23);
            txtFind.TabIndex = 5;
            // 
            // lblReplace
            // 
            lblReplace.AutoSize = true;
            lblReplace.Font = new Font("Segoe UI", 9F);
            lblReplace.ForeColor = Color.White;
            lblReplace.Location = new Point(370, 58);
            lblReplace.Name = "lblReplace";
            lblReplace.Size = new Size(63, 15);
            lblReplace.TabIndex = 6;
            lblReplace.Text = "Sostituisci:";
            // 
            // txtReplace
            // 
            txtReplace.BackColor = Color.FromArgb(50, 50, 50);
            txtReplace.BorderStyle = BorderStyle.FixedSingle;
            txtReplace.Font = new Font("Segoe UI", 9F);
            txtReplace.ForeColor = Color.White;
            txtReplace.Location = new Point(450, 55);
            txtReplace.Name = "txtReplace";
            txtReplace.Size = new Size(280, 23);
            txtReplace.TabIndex = 7;
            // 
            // btnAnalyze
            // 
            btnAnalyze.BackColor = Color.FromArgb(0, 120, 215);
            btnAnalyze.Cursor = Cursors.Hand;
            btnAnalyze.FlatAppearance.BorderSize = 0;
            btnAnalyze.FlatStyle = FlatStyle.Flat;
            btnAnalyze.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnAnalyze.ForeColor = Color.White;
            btnAnalyze.Location = new Point(755, 53);
            btnAnalyze.Name = "btnAnalyze";
            btnAnalyze.Size = new Size(100, 27);
            btnAnalyze.TabIndex = 8;
            btnAnalyze.Text = "🔍 Analizza";
            btnAnalyze.UseVisualStyleBackColor = false;
            // 
            // btnReplaceAll
            // 
            btnReplaceAll.BackColor = Color.FromArgb(220, 53, 69);
            btnReplaceAll.Cursor = Cursors.Hand;
            btnReplaceAll.FlatAppearance.BorderSize = 0;
            btnReplaceAll.FlatStyle = FlatStyle.Flat;
            btnReplaceAll.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnReplaceAll.ForeColor = Color.White;
            btnReplaceAll.Location = new Point(865, 53);
            btnReplaceAll.Name = "btnReplaceAll";
            btnReplaceAll.Size = new Size(100, 27);
            btnReplaceAll.TabIndex = 9;
            btnReplaceAll.Text = "✏️ Sostituisci";
            btnReplaceAll.UseVisualStyleBackColor = false;
            // 
            // DatabaseModificationsForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(28, 28, 28);
            ClientSize = new Size(1000, 680);
            Controls.Add(tabMain);
            ForeColor = Color.White;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimumSize = new Size(900, 600);
            Name = "DatabaseModificationsForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Modifiche Database";
            tabMain.ResumeLayout(false);
            tabVerify.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvMissing).EndInit();
            panelVerifyTop.ResumeLayout(false);
            panelVerifyTop.PerformLayout();
            tabFindReplace.ResumeLayout(false);
            tabFindReplace.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvResults).EndInit();
            panelFRTop.ResumeLayout(false);
            panelFRTop.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
    }
}
