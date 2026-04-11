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
            DataGridViewCellStyle headerStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle cellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle altStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle headerStyle2 = new DataGridViewCellStyle();
            DataGridViewCellStyle cellStyle2 = new DataGridViewCellStyle();
            DataGridViewCellStyle altStyle2 = new DataGridViewCellStyle();

            tabMain = new System.Windows.Forms.TabControl();
            tabVerify = new System.Windows.Forms.TabPage();
            tabFindReplace = new System.Windows.Forms.TabPage();

            // --- Tab 1 controls ---
            panelVerifyTop = new System.Windows.Forms.Panel();
            chkVerifyMusic = new System.Windows.Forms.CheckBox();
            chkVerifyClips = new System.Windows.Forms.CheckBox();
            btnVerify = new System.Windows.Forms.Button();
            lblVerifyStatus = new System.Windows.Forms.Label();
            progressVerify = new System.Windows.Forms.ProgressBar();
            dgvMissing = new System.Windows.Forms.DataGridView();
            colMissingArtist = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colMissingTitle = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colMissingPath = new System.Windows.Forms.DataGridViewTextBoxColumn();

            // --- Tab 2 controls ---
            panelFRTop = new System.Windows.Forms.Panel();
            chkFRMusic = new System.Windows.Forms.CheckBox();
            chkFRClips = new System.Windows.Forms.CheckBox();
            chkFRSettings = new System.Windows.Forms.CheckBox();
            chkFRDatabase = new System.Windows.Forms.CheckBox();
            lblFind = new System.Windows.Forms.Label();
            txtFind = new System.Windows.Forms.TextBox();
            lblReplace = new System.Windows.Forms.Label();
            txtReplace = new System.Windows.Forms.TextBox();
            btnAnalyze = new System.Windows.Forms.Button();
            btnReplaceAll = new System.Windows.Forms.Button();
            lblOccurrences = new System.Windows.Forms.Label();
            dgvResults = new System.Windows.Forms.DataGridView();
            colSource = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colCurrentPath = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colProposedPath = new System.Windows.Forms.DataGridViewTextBoxColumn();

            tabMain.SuspendLayout();
            tabVerify.SuspendLayout();
            tabFindReplace.SuspendLayout();
            panelVerifyTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvMissing).BeginInit();
            panelFRTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvResults).BeginInit();
            SuspendLayout();

            // ── tabMain ──
            tabMain.Controls.Add(tabVerify);
            tabMain.Controls.Add(tabFindReplace);
            tabMain.Dock = System.Windows.Forms.DockStyle.Fill;
            tabMain.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            tabMain.Location = new System.Drawing.Point(0, 0);
            tabMain.Name = "tabMain";
            tabMain.SelectedIndex = 0;
            tabMain.Size = new System.Drawing.Size(1000, 680);
            tabMain.TabIndex = 0;

            // ── tabVerify ──
            tabVerify.BackColor = System.Drawing.Color.FromArgb(28, 28, 28);
            tabVerify.Controls.Add(dgvMissing);
            tabVerify.Controls.Add(progressVerify);
            tabVerify.Controls.Add(lblVerifyStatus);
            tabVerify.Controls.Add(panelVerifyTop);
            tabVerify.Location = new System.Drawing.Point(4, 27);
            tabVerify.Name = "tabVerify";
            tabVerify.Padding = new System.Windows.Forms.Padding(8);
            tabVerify.Size = new System.Drawing.Size(992, 649);
            tabVerify.TabIndex = 0;
            tabVerify.Text = "Verifica Esistenza File Audio";

            // ── panelVerifyTop ──
            panelVerifyTop.BackColor = System.Drawing.Color.FromArgb(35, 35, 35);
            panelVerifyTop.Controls.Add(chkVerifyMusic);
            panelVerifyTop.Controls.Add(chkVerifyClips);
            panelVerifyTop.Controls.Add(btnVerify);
            panelVerifyTop.Dock = System.Windows.Forms.DockStyle.Top;
            panelVerifyTop.Location = new System.Drawing.Point(8, 8);
            panelVerifyTop.Name = "panelVerifyTop";
            panelVerifyTop.Padding = new System.Windows.Forms.Padding(10, 8, 10, 8);
            panelVerifyTop.Size = new System.Drawing.Size(976, 50);
            panelVerifyTop.TabIndex = 0;

            // ── chkVerifyMusic ──
            chkVerifyMusic.AutoSize = true;
            chkVerifyMusic.Checked = true;
            chkVerifyMusic.CheckState = System.Windows.Forms.CheckState.Checked;
            chkVerifyMusic.Font = new System.Drawing.Font("Segoe UI", 10F);
            chkVerifyMusic.ForeColor = System.Drawing.Color.White;
            chkVerifyMusic.Location = new System.Drawing.Point(15, 13);
            chkVerifyMusic.Name = "chkVerifyMusic";
            chkVerifyMusic.Size = new System.Drawing.Size(70, 23);
            chkVerifyMusic.TabIndex = 0;
            chkVerifyMusic.Text = "Music";

            // ── chkVerifyClips ──
            chkVerifyClips.AutoSize = true;
            chkVerifyClips.Checked = true;
            chkVerifyClips.CheckState = System.Windows.Forms.CheckState.Checked;
            chkVerifyClips.Font = new System.Drawing.Font("Segoe UI", 10F);
            chkVerifyClips.ForeColor = System.Drawing.Color.White;
            chkVerifyClips.Location = new System.Drawing.Point(105, 13);
            chkVerifyClips.Name = "chkVerifyClips";
            chkVerifyClips.Size = new System.Drawing.Size(65, 23);
            chkVerifyClips.TabIndex = 1;
            chkVerifyClips.Text = "Clips";

            // ── btnVerify ──
            btnVerify.BackColor = System.Drawing.Color.FromArgb(0, 120, 215);
            btnVerify.Cursor = System.Windows.Forms.Cursors.Hand;
            btnVerify.FlatAppearance.BorderSize = 0;
            btnVerify.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnVerify.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            btnVerify.ForeColor = System.Drawing.Color.White;
            btnVerify.Location = new System.Drawing.Point(220, 10);
            btnVerify.Name = "btnVerify";
            btnVerify.Size = new System.Drawing.Size(120, 30);
            btnVerify.TabIndex = 2;
            btnVerify.Text = "🔍 Verifica";
            btnVerify.UseVisualStyleBackColor = false;

            // ── progressVerify ──
            progressVerify.Dock = System.Windows.Forms.DockStyle.None;
            progressVerify.Location = new System.Drawing.Point(8, 66);
            progressVerify.Name = "progressVerify";
            progressVerify.Size = new System.Drawing.Size(976, 18);
            progressVerify.TabIndex = 1;

            // ── lblVerifyStatus ──
            lblVerifyStatus.AutoSize = false;
            lblVerifyStatus.Font = new System.Drawing.Font("Segoe UI", 9F);
            lblVerifyStatus.ForeColor = System.Drawing.Color.FromArgb(200, 200, 200);
            lblVerifyStatus.Location = new System.Drawing.Point(8, 90);
            lblVerifyStatus.Name = "lblVerifyStatus";
            lblVerifyStatus.Size = new System.Drawing.Size(970, 20);
            lblVerifyStatus.TabIndex = 2;
            lblVerifyStatus.Text = "";

            // ── dgvMissing ──
            dgvMissing.AllowUserToAddRows = false;
            dgvMissing.AllowUserToDeleteRows = false;
            dgvMissing.AllowUserToResizeRows = false;
            altStyle1.BackColor = System.Drawing.Color.FromArgb(35, 35, 35);
            dgvMissing.AlternatingRowsDefaultCellStyle = altStyle1;
            dgvMissing.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            dgvMissing.BackgroundColor = System.Drawing.Color.FromArgb(30, 30, 30);
            dgvMissing.BorderStyle = System.Windows.Forms.BorderStyle.None;
            headerStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            headerStyle1.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
            headerStyle1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            headerStyle1.ForeColor = System.Drawing.Color.White;
            dgvMissing.ColumnHeadersDefaultCellStyle = headerStyle1;
            dgvMissing.ColumnHeadersHeight = 35;
            dgvMissing.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { colMissingArtist, colMissingTitle, colMissingPath });
            cellStyle1.BackColor = System.Drawing.Color.FromArgb(40, 40, 40);
            cellStyle1.Font = new System.Drawing.Font("Segoe UI", 9F);
            cellStyle1.ForeColor = System.Drawing.Color.White;
            cellStyle1.SelectionBackColor = System.Drawing.Color.FromArgb(0, 120, 215);
            cellStyle1.SelectionForeColor = System.Drawing.Color.White;
            dgvMissing.DefaultCellStyle = cellStyle1;
            dgvMissing.EnableHeadersVisualStyles = false;
            dgvMissing.GridColor = System.Drawing.Color.FromArgb(60, 60, 60);
            dgvMissing.Location = new System.Drawing.Point(8, 114);
            dgvMissing.MultiSelect = false;
            dgvMissing.Name = "dgvMissing";
            dgvMissing.ReadOnly = true;
            dgvMissing.RowHeadersVisible = false;
            dgvMissing.RowTemplate.Height = 28;
            dgvMissing.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dgvMissing.Size = new System.Drawing.Size(976, 519);
            dgvMissing.TabIndex = 3;

            // ── colMissingArtist ──
            colMissingArtist.FillWeight = 20F;
            colMissingArtist.HeaderText = "Artista";
            colMissingArtist.Name = "colMissingArtist";
            colMissingArtist.ReadOnly = true;

            // ── colMissingTitle ──
            colMissingTitle.FillWeight = 25F;
            colMissingTitle.HeaderText = "Titolo";
            colMissingTitle.Name = "colMissingTitle";
            colMissingTitle.ReadOnly = true;

            // ── colMissingPath ──
            colMissingPath.FillWeight = 55F;
            colMissingPath.HeaderText = "Percorso File";
            colMissingPath.Name = "colMissingPath";
            colMissingPath.ReadOnly = true;

            // ── tabFindReplace ──
            tabFindReplace.BackColor = System.Drawing.Color.FromArgb(28, 28, 28);
            tabFindReplace.Controls.Add(dgvResults);
            tabFindReplace.Controls.Add(lblOccurrences);
            tabFindReplace.Controls.Add(panelFRTop);
            tabFindReplace.Location = new System.Drawing.Point(4, 27);
            tabFindReplace.Name = "tabFindReplace";
            tabFindReplace.Padding = new System.Windows.Forms.Padding(8);
            tabFindReplace.Size = new System.Drawing.Size(992, 649);
            tabFindReplace.TabIndex = 1;
            tabFindReplace.Text = "Trova e Sostituisci Percorso";

            // ── panelFRTop ──
            panelFRTop.AutoSize = true;
            panelFRTop.BackColor = System.Drawing.Color.FromArgb(35, 35, 35);
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
            panelFRTop.Dock = System.Windows.Forms.DockStyle.Top;
            panelFRTop.Location = new System.Drawing.Point(8, 8);
            panelFRTop.Name = "panelFRTop";
            panelFRTop.Padding = new System.Windows.Forms.Padding(10, 8, 10, 8);
            panelFRTop.Size = new System.Drawing.Size(976, 95);
            panelFRTop.TabIndex = 0;

            // ── chkFRMusic ──
            chkFRMusic.AutoSize = true;
            chkFRMusic.Checked = true;
            chkFRMusic.CheckState = System.Windows.Forms.CheckState.Checked;
            chkFRMusic.Font = new System.Drawing.Font("Segoe UI", 9F);
            chkFRMusic.ForeColor = System.Drawing.Color.White;
            chkFRMusic.Location = new System.Drawing.Point(15, 12);
            chkFRMusic.Name = "chkFRMusic";
            chkFRMusic.Size = new System.Drawing.Size(65, 19);
            chkFRMusic.TabIndex = 0;
            chkFRMusic.Text = "Music";

            // ── chkFRClips ──
            chkFRClips.AutoSize = true;
            chkFRClips.Font = new System.Drawing.Font("Segoe UI", 9F);
            chkFRClips.ForeColor = System.Drawing.Color.White;
            chkFRClips.Location = new System.Drawing.Point(100, 12);
            chkFRClips.Name = "chkFRClips";
            chkFRClips.Size = new System.Drawing.Size(57, 19);
            chkFRClips.TabIndex = 1;
            chkFRClips.Text = "Clips";

            // ── chkFRSettings ──
            chkFRSettings.AutoSize = true;
            chkFRSettings.Font = new System.Drawing.Font("Segoe UI", 9F);
            chkFRSettings.ForeColor = System.Drawing.Color.White;
            chkFRSettings.Location = new System.Drawing.Point(180, 12);
            chkFRSettings.Name = "chkFRSettings";
            chkFRSettings.Size = new System.Drawing.Size(75, 19);
            chkFRSettings.TabIndex = 2;
            chkFRSettings.Text = "Settings";

            // ── chkFRDatabase ──
            chkFRDatabase.AutoSize = true;
            chkFRDatabase.Font = new System.Drawing.Font("Segoe UI", 9F);
            chkFRDatabase.ForeColor = System.Drawing.Color.White;
            chkFRDatabase.Location = new System.Drawing.Point(275, 12);
            chkFRDatabase.Name = "chkFRDatabase";
            chkFRDatabase.Size = new System.Drawing.Size(78, 19);
            chkFRDatabase.TabIndex = 3;
            chkFRDatabase.Text = "Database";

            // ── lblFind ──
            lblFind.AutoSize = true;
            lblFind.Font = new System.Drawing.Font("Segoe UI", 9F);
            lblFind.ForeColor = System.Drawing.Color.White;
            lblFind.Location = new System.Drawing.Point(15, 58);
            lblFind.Name = "lblFind";
            lblFind.Size = new System.Drawing.Size(43, 15);
            lblFind.TabIndex = 4;
            lblFind.Text = "Trova:";

            // ── txtFind ──
            txtFind.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
            txtFind.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            txtFind.Font = new System.Drawing.Font("Segoe UI", 9F);
            txtFind.ForeColor = System.Drawing.Color.White;
            txtFind.Location = new System.Drawing.Point(70, 55);
            txtFind.Name = "txtFind";
            txtFind.Size = new System.Drawing.Size(280, 23);
            txtFind.TabIndex = 5;

            // ── lblReplace ──
            lblReplace.AutoSize = true;
            lblReplace.Font = new System.Drawing.Font("Segoe UI", 9F);
            lblReplace.ForeColor = System.Drawing.Color.White;
            lblReplace.Location = new System.Drawing.Point(370, 58);
            lblReplace.Name = "lblReplace";
            lblReplace.Size = new System.Drawing.Size(68, 15);
            lblReplace.TabIndex = 6;
            lblReplace.Text = "Sostituisci:";

            // ── txtReplace ──
            txtReplace.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
            txtReplace.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            txtReplace.Font = new System.Drawing.Font("Segoe UI", 9F);
            txtReplace.ForeColor = System.Drawing.Color.White;
            txtReplace.Location = new System.Drawing.Point(450, 55);
            txtReplace.Name = "txtReplace";
            txtReplace.Size = new System.Drawing.Size(280, 23);
            txtReplace.TabIndex = 7;

            // ── btnAnalyze ──
            btnAnalyze.BackColor = System.Drawing.Color.FromArgb(0, 120, 215);
            btnAnalyze.Cursor = System.Windows.Forms.Cursors.Hand;
            btnAnalyze.FlatAppearance.BorderSize = 0;
            btnAnalyze.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnAnalyze.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            btnAnalyze.ForeColor = System.Drawing.Color.White;
            btnAnalyze.Location = new System.Drawing.Point(755, 53);
            btnAnalyze.Name = "btnAnalyze";
            btnAnalyze.Size = new System.Drawing.Size(100, 27);
            btnAnalyze.TabIndex = 8;
            btnAnalyze.Text = "🔍 Analizza";
            btnAnalyze.UseVisualStyleBackColor = false;

            // ── btnReplaceAll ──
            btnReplaceAll.BackColor = System.Drawing.Color.FromArgb(220, 53, 69);
            btnReplaceAll.Cursor = System.Windows.Forms.Cursors.Hand;
            btnReplaceAll.FlatAppearance.BorderSize = 0;
            btnReplaceAll.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnReplaceAll.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            btnReplaceAll.ForeColor = System.Drawing.Color.White;
            btnReplaceAll.Location = new System.Drawing.Point(865, 53);
            btnReplaceAll.Name = "btnReplaceAll";
            btnReplaceAll.Size = new System.Drawing.Size(100, 27);
            btnReplaceAll.TabIndex = 9;
            btnReplaceAll.Text = "✏️ Sostituisci";
            btnReplaceAll.UseVisualStyleBackColor = false;

            // ── lblOccurrences ──
            lblOccurrences.AutoSize = false;
            lblOccurrences.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            lblOccurrences.ForeColor = System.Drawing.Color.FromArgb(0, 200, 83);
            lblOccurrences.Location = new System.Drawing.Point(8, 111);
            lblOccurrences.Name = "lblOccurrences";
            lblOccurrences.Size = new System.Drawing.Size(500, 20);
            lblOccurrences.TabIndex = 1;
            lblOccurrences.Text = "";

            // ── dgvResults ──
            dgvResults.AllowUserToAddRows = false;
            dgvResults.AllowUserToDeleteRows = false;
            dgvResults.AllowUserToResizeRows = false;
            altStyle2.BackColor = System.Drawing.Color.FromArgb(35, 35, 35);
            dgvResults.AlternatingRowsDefaultCellStyle = altStyle2;
            dgvResults.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            dgvResults.BackgroundColor = System.Drawing.Color.FromArgb(30, 30, 30);
            dgvResults.BorderStyle = System.Windows.Forms.BorderStyle.None;
            headerStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            headerStyle2.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
            headerStyle2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            headerStyle2.ForeColor = System.Drawing.Color.White;
            dgvResults.ColumnHeadersDefaultCellStyle = headerStyle2;
            dgvResults.ColumnHeadersHeight = 35;
            dgvResults.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { colSource, colCurrentPath, colProposedPath });
            cellStyle2.BackColor = System.Drawing.Color.FromArgb(40, 40, 40);
            cellStyle2.Font = new System.Drawing.Font("Segoe UI", 9F);
            cellStyle2.ForeColor = System.Drawing.Color.White;
            cellStyle2.SelectionBackColor = System.Drawing.Color.FromArgb(0, 120, 215);
            cellStyle2.SelectionForeColor = System.Drawing.Color.White;
            dgvResults.DefaultCellStyle = cellStyle2;
            dgvResults.EnableHeadersVisualStyles = false;
            dgvResults.GridColor = System.Drawing.Color.FromArgb(60, 60, 60);
            dgvResults.Location = new System.Drawing.Point(8, 135);
            dgvResults.MultiSelect = false;
            dgvResults.Name = "dgvResults";
            dgvResults.ReadOnly = true;
            dgvResults.RowHeadersVisible = false;
            dgvResults.RowTemplate.Height = 28;
            dgvResults.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dgvResults.Size = new System.Drawing.Size(976, 498);
            dgvResults.TabIndex = 2;

            // ── colSource ──
            colSource.FillWeight = 15F;
            colSource.HeaderText = "Sorgente";
            colSource.Name = "colSource";
            colSource.ReadOnly = true;

            // ── colCurrentPath ──
            colCurrentPath.FillWeight = 42F;
            colCurrentPath.HeaderText = "Percorso Attuale";
            colCurrentPath.Name = "colCurrentPath";
            colCurrentPath.ReadOnly = true;

            // ── colProposedPath ──
            colProposedPath.FillWeight = 43F;
            colProposedPath.HeaderText = "Percorso Proposto";
            colProposedPath.Name = "colProposedPath";
            colProposedPath.ReadOnly = true;

            // ── DatabaseModificationsForm ──
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.Color.FromArgb(28, 28, 28);
            ClientSize = new System.Drawing.Size(1000, 680);
            Controls.Add(tabMain);
            ForeColor = System.Drawing.Color.White;
            MinimumSize = new System.Drawing.Size(900, 600);
            Name = "DatabaseModificationsForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Modifiche Database";

            tabMain.ResumeLayout(false);
            tabVerify.ResumeLayout(false);
            tabFindReplace.ResumeLayout(false);
            tabFindReplace.PerformLayout();
            panelVerifyTop.ResumeLayout(false);
            panelVerifyTop.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvMissing).EndInit();
            panelFRTop.ResumeLayout(false);
            panelFRTop.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvResults).EndInit();
            ResumeLayout(false);
        }

        #endregion
    }
}
