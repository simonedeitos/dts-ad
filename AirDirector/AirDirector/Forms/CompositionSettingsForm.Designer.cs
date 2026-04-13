namespace AirDirector.Forms
{
    partial class CompositionSettingsForm
    {
        private System.ComponentModel.IContainer components = null;


        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CompositionSettingsForm));
            panelTop = new Panel();
            lblTitle = new Label();
            panelBottom = new Panel();
            btnCancel = new Button();
            btnSave = new Button();
            panelMain = new Panel();
            groupBox5 = new GroupBox();
            chkBoostVolume = new CheckBox();
            btnBrowseOutput = new Button();
            txtOutputFile = new TextBox();
            label11 = new Label();
            groupBox4 = new GroupBox();
            btnBrowseCloser = new Button();
            txtCloserFile = new TextBox();
            chkUseCloser = new CheckBox();
            groupBox3 = new GroupBox();
            label12 = new Label();
            numVolume = new NumericUpDown();
            btnBrowseBackground = new Button();
            txtBackgroundFile = new TextBox();
            chkUseBackground = new CheckBox();
            groupBox2 = new GroupBox();
            btnBrowseMain = new Button();
            txtMainFile = new TextBox();
            label10 = new Label();
            groupBox1 = new GroupBox();
            btnBrowseOpener = new Button();
            txtOpenerFile = new TextBox();
            chkUseOpener = new CheckBox();
            panelTop.SuspendLayout();
            panelBottom.SuspendLayout();
            panelMain.SuspendLayout();
            groupBox5.SuspendLayout();
            groupBox4.SuspendLayout();
            groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numVolume).BeginInit();
            groupBox2.SuspendLayout();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // panelTop
            // 
            panelTop.BackColor = Color.FromArgb(45, 45, 48);
            panelTop.Controls.Add(lblTitle);
            panelTop.Dock = DockStyle.Top;
            panelTop.Location = new Point(0, 0);
            panelTop.Name = "panelTop";
            panelTop.Size = new Size(584, 50);
            panelTop.TabIndex = 0;
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblTitle.ForeColor = Color.White;
            lblTitle.Location = new Point(12, 9);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(225, 30);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Composizione Audio";
            // 
            // panelBottom
            // 
            panelBottom.BackColor = Color.WhiteSmoke;
            panelBottom.Controls.Add(btnCancel);
            panelBottom.Controls.Add(btnSave);
            panelBottom.Dock = DockStyle.Bottom;
            panelBottom.Location = new Point(0, 467);
            panelBottom.Name = "panelBottom";
            panelBottom.Padding = new Padding(5);
            panelBottom.Size = new Size(584, 50);
            panelBottom.TabIndex = 1;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCancel.BackColor = Color.Gray;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnCancel.ForeColor = Color.White;
            btnCancel.Location = new Point(350, 8);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(106, 34);
            btnCancel.TabIndex = 1;
            btnCancel.Text = "Annulla";
            btnCancel.UseVisualStyleBackColor = false;
            btnCancel.Click += btnCancel_Click;
            // 
            // btnSave
            // 
            btnSave.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSave.BackColor = Color.Green;
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnSave.ForeColor = Color.White;
            btnSave.Location = new Point(462, 8);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(106, 34);
            btnSave.TabIndex = 0;
            btnSave.Text = "Salva";
            btnSave.UseVisualStyleBackColor = false;
            btnSave.Click += btnSave_Click;
            // 
            // panelMain
            // 
            panelMain.AutoScroll = true;
            panelMain.Controls.Add(groupBox5);
            panelMain.Controls.Add(groupBox4);
            panelMain.Controls.Add(groupBox3);
            panelMain.Controls.Add(groupBox2);
            panelMain.Controls.Add(groupBox1);
            panelMain.Dock = DockStyle.Fill;
            panelMain.Location = new Point(0, 50);
            panelMain.Name = "panelMain";
            panelMain.Padding = new Padding(10);
            panelMain.Size = new Size(584, 417);
            panelMain.TabIndex = 2;
            // 
            // groupBox5
            // 
            groupBox5.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            groupBox5.Controls.Add(chkBoostVolume);
            groupBox5.Controls.Add(btnBrowseOutput);
            groupBox5.Controls.Add(txtOutputFile);
            groupBox5.Controls.Add(label11);
            groupBox5.Location = new Point(13, 328);
            groupBox5.Name = "groupBox5";
            groupBox5.Size = new Size(558, 80);
            groupBox5.TabIndex = 4;
            groupBox5.TabStop = false;
            groupBox5.Text = "File di Output";
            // 
            // chkBoostVolume
            // 
            chkBoostVolume.AutoSize = true;
            chkBoostVolume.Location = new Point(104, 53);
            chkBoostVolume.Name = "chkBoostVolume";
            chkBoostVolume.Size = new Size(163, 19);
            chkBoostVolume.TabIndex = 3;
            chkBoostVolume.Text = "Aumenta volume (-0.5db)";
            chkBoostVolume.UseVisualStyleBackColor = true;
            chkBoostVolume.CheckedChanged += chkBoostVolume_CheckedChanged;
            // 
            // btnBrowseOutput
            // 
            btnBrowseOutput.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnBrowseOutput.Location = new Point(516, 24);
            btnBrowseOutput.Name = "btnBrowseOutput";
            btnBrowseOutput.Size = new Size(31, 23);
            btnBrowseOutput.TabIndex = 2;
            btnBrowseOutput.Text = "...";
            btnBrowseOutput.UseVisualStyleBackColor = true;
            btnBrowseOutput.Click += btnBrowseOutput_Click;
            // 
            // txtOutputFile
            // 
            txtOutputFile.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtOutputFile.Location = new Point(102, 24);
            txtOutputFile.Name = "txtOutputFile";
            txtOutputFile.Size = new Size(408, 23);
            txtOutputFile.TabIndex = 1;
            txtOutputFile.TextChanged += txtOutputFile_TextChanged;
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new Point(15, 27);
            label11.Name = "label11";
            label11.Size = new Size(80, 15);
            label11.TabIndex = 0;
            label11.Text = "File di output:";
            // 
            // groupBox4
            // 
            groupBox4.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            groupBox4.Controls.Add(btnBrowseCloser);
            groupBox4.Controls.Add(txtCloserFile);
            groupBox4.Controls.Add(chkUseCloser);
            groupBox4.Location = new Point(13, 253);
            groupBox4.Name = "groupBox4";
            groupBox4.Size = new Size(558, 69);
            groupBox4.TabIndex = 3;
            groupBox4.TabStop = false;
            groupBox4.Text = "4.Jingle di Chiusura";
            // 
            // btnBrowseCloser
            // 
            btnBrowseCloser.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnBrowseCloser.Location = new Point(516, 33);
            btnBrowseCloser.Name = "btnBrowseCloser";
            btnBrowseCloser.Size = new Size(31, 23);
            btnBrowseCloser.TabIndex = 2;
            btnBrowseCloser.Text = "...";
            btnBrowseCloser.UseVisualStyleBackColor = true;
            btnBrowseCloser.Click += btnBrowseCloser_Click;
            // 
            // txtCloserFile
            // 
            txtCloserFile.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtCloserFile.Location = new Point(102, 33);
            txtCloserFile.Name = "txtCloserFile";
            txtCloserFile.Size = new Size(408, 23);
            txtCloserFile.TabIndex = 1;
            txtCloserFile.TextChanged += txtCloserFile_TextChanged;
            // 
            // chkUseCloser
            // 
            chkUseCloser.AutoSize = true;
            chkUseCloser.Location = new Point(15, 37);
            chkUseCloser.Name = "chkUseCloser";
            chkUseCloser.Size = new Size(77, 19);
            chkUseCloser.TabIndex = 0;
            chkUseCloser.Text = "Usa jingle";
            chkUseCloser.UseVisualStyleBackColor = true;
            chkUseCloser.CheckedChanged += chkUseCloser_CheckedChanged;
            // 
            // groupBox3
            // 
            groupBox3.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            groupBox3.Controls.Add(label12);
            groupBox3.Controls.Add(numVolume);
            groupBox3.Controls.Add(btnBrowseBackground);
            groupBox3.Controls.Add(txtBackgroundFile);
            groupBox3.Controls.Add(chkUseBackground);
            groupBox3.Location = new Point(13, 159);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(558, 88);
            groupBox3.TabIndex = 2;
            groupBox3.TabStop = false;
            groupBox3.Text = "3.Base di Sottofondo";
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Location = new Point(101, 61);
            label12.Name = "label12";
            label12.Size = new Size(50, 15);
            label12.TabIndex = 4;
            label12.Text = "Volume:";
            // 
            // numVolume
            // 
            numVolume.Location = new Point(161, 59);
            numVolume.Name = "numVolume";
            numVolume.Size = new Size(68, 23);
            numVolume.TabIndex = 3;
            numVolume.Value = new decimal(new int[] { 30, 0, 0, 0 });
            numVolume.ValueChanged += numVolume_ValueChanged;
            // 
            // btnBrowseBackground
            // 
            btnBrowseBackground.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnBrowseBackground.Location = new Point(516, 33);
            btnBrowseBackground.Name = "btnBrowseBackground";
            btnBrowseBackground.Size = new Size(31, 23);
            btnBrowseBackground.TabIndex = 2;
            btnBrowseBackground.Text = "...";
            btnBrowseBackground.UseVisualStyleBackColor = true;
            btnBrowseBackground.Click += btnBrowseBackground_Click;
            // 
            // txtBackgroundFile
            // 
            txtBackgroundFile.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtBackgroundFile.Location = new Point(102, 33);
            txtBackgroundFile.Name = "txtBackgroundFile";
            txtBackgroundFile.Size = new Size(408, 23);
            txtBackgroundFile.TabIndex = 1;
            txtBackgroundFile.TextChanged += txtBackgroundFile_TextChanged;
            // 
            // chkUseBackground
            // 
            chkUseBackground.AutoSize = true;
            chkUseBackground.Location = new Point(15, 37);
            chkUseBackground.Name = "chkUseBackground";
            chkUseBackground.Size = new Size(72, 19);
            chkUseBackground.TabIndex = 0;
            chkUseBackground.Text = "Usa base";
            chkUseBackground.UseVisualStyleBackColor = true;
            chkUseBackground.CheckedChanged += chkUseBackground_CheckedChanged;
            // 
            // groupBox2
            // 
            groupBox2.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            groupBox2.Controls.Add(btnBrowseMain);
            groupBox2.Controls.Add(txtMainFile);
            groupBox2.Controls.Add(label10);
            groupBox2.Location = new Point(13, 86);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(558, 67);
            groupBox2.TabIndex = 1;
            groupBox2.TabStop = false;
            groupBox2.Text = "2.File Principale";
            // 
            // btnBrowseMain
            // 
            btnBrowseMain.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnBrowseMain.Location = new Point(516, 24);
            btnBrowseMain.Name = "btnBrowseMain";
            btnBrowseMain.Size = new Size(31, 23);
            btnBrowseMain.TabIndex = 2;
            btnBrowseMain.Text = "...";
            btnBrowseMain.UseVisualStyleBackColor = true;
            btnBrowseMain.Click += btnBrowseMain_Click;
            // 
            // txtMainFile
            // 
            txtMainFile.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtMainFile.Location = new Point(102, 24);
            txtMainFile.Name = "txtMainFile";
            txtMainFile.Size = new Size(408, 23);
            txtMainFile.TabIndex = 1;
            txtMainFile.TextChanged += txtMainFile_TextChanged;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(15, 27);
            label10.Name = "label10";
            label10.Size = new Size(83, 15);
            label10.TabIndex = 0;
            label10.Text = "File principale:";
            // 
            // groupBox1
            // 
            groupBox1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            groupBox1.Controls.Add(btnBrowseOpener);
            groupBox1.Controls.Add(txtOpenerFile);
            groupBox1.Controls.Add(chkUseOpener);
            groupBox1.Location = new Point(13, 13);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(558, 67);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "1.Jingle di Apertura";
            // 
            // btnBrowseOpener
            // 
            btnBrowseOpener.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnBrowseOpener.Location = new Point(516, 33);
            btnBrowseOpener.Name = "btnBrowseOpener";
            btnBrowseOpener.Size = new Size(31, 23);
            btnBrowseOpener.TabIndex = 2;
            btnBrowseOpener.Text = "...";
            btnBrowseOpener.UseVisualStyleBackColor = true;
            btnBrowseOpener.Click += btnBrowseOpener_Click;
            // 
            // txtOpenerFile
            // 
            txtOpenerFile.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtOpenerFile.Location = new Point(102, 33);
            txtOpenerFile.Name = "txtOpenerFile";
            txtOpenerFile.Size = new Size(408, 23);
            txtOpenerFile.TabIndex = 1;
            txtOpenerFile.TextChanged += txtOpenerFile_TextChanged;
            // 
            // chkUseOpener
            // 
            chkUseOpener.AutoSize = true;
            chkUseOpener.Location = new Point(15, 37);
            chkUseOpener.Name = "chkUseOpener";
            chkUseOpener.Size = new Size(77, 19);
            chkUseOpener.TabIndex = 0;
            chkUseOpener.Text = "Usa jingle";
            chkUseOpener.UseVisualStyleBackColor = true;
            chkUseOpener.CheckedChanged += chkUseOpener_CheckedChanged;
            // 
            // CompositionSettingsForm
            // 
            AcceptButton = btnSave;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(584, 517);
            Controls.Add(panelMain);
            Controls.Add(panelBottom);
            Controls.Add(panelTop);
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimumSize = new Size(600, 540);
            Name = "CompositionSettingsForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Impostazioni Composizione Audio";
            Load += CompositionSettingsForm_Load;
            panelTop.ResumeLayout(false);
            panelTop.PerformLayout();
            panelBottom.ResumeLayout(false);
            panelMain.ResumeLayout(false);
            groupBox5.ResumeLayout(false);
            groupBox5.PerformLayout();
            groupBox4.ResumeLayout(false);
            groupBox4.PerformLayout();
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numVolume).EndInit();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Panel panelBottom;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Panel panelMain;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btnBrowseOpener;
        private System.Windows.Forms.TextBox txtOpenerFile;
        private System.Windows.Forms.CheckBox chkUseOpener;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button btnBrowseMain;
        private System.Windows.Forms.TextBox txtMainFile;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.NumericUpDown numVolume;
        private System.Windows.Forms.Button btnBrowseBackground;
        private System.Windows.Forms.TextBox txtBackgroundFile;
        private System.Windows.Forms.CheckBox chkUseBackground;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Button btnBrowseCloser;
        private System.Windows.Forms.TextBox txtCloserFile;
        private System.Windows.Forms.CheckBox chkUseCloser;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.CheckBox chkBoostVolume;
        private System.Windows.Forms.Button btnBrowseOutput;
        private System.Windows.Forms.TextBox txtOutputFile;
        private System.Windows.Forms.Label label11;
    }
}