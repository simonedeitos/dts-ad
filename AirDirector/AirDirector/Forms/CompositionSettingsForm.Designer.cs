namespace AirDirector.Forms
{
    partial class CompositionSettingsForm
    {
        private System.ComponentModel.IContainer components = null;


        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.panelTop = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.panelBottom = new System.Windows.Forms.Panel();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.panelMain = new System.Windows.Forms.Panel();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.chkBoostVolume = new System.Windows.Forms.CheckBox();
            this.btnBrowseOutput = new System.Windows.Forms.Button();
            this.txtOutputFile = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.btnBrowseCloser = new System.Windows.Forms.Button();
            this.txtCloserFile = new System.Windows.Forms.TextBox();
            this.chkUseCloser = new System.Windows.Forms.CheckBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.label12 = new System.Windows.Forms.Label();
            this.numVolume = new System.Windows.Forms.NumericUpDown();
            this.btnBrowseBackground = new System.Windows.Forms.Button();
            this.txtBackgroundFile = new System.Windows.Forms.TextBox();
            this.chkUseBackground = new System.Windows.Forms.CheckBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.btnBrowseMain = new System.Windows.Forms.Button();
            this.txtMainFile = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnBrowseOpener = new System.Windows.Forms.Button();
            this.txtOpenerFile = new System.Windows.Forms.TextBox();
            this.chkUseOpener = new System.Windows.Forms.CheckBox();
            this.panelTop.SuspendLayout();
            this.panelBottom.SuspendLayout();
            this.panelMain.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numVolume)).BeginInit();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelTop
            // 
            this.panelTop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.panelTop.Controls.Add(this.lblTitle);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(584, 50);
            this.panelTop.TabIndex = 0;
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(12, 9);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(225, 30);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Composizione Audio";
            // 
            // panelBottom
            // 
            this.panelBottom.BackColor = System.Drawing.Color.WhiteSmoke;
            this.panelBottom.Controls.Add(this.btnCancel);
            this.panelBottom.Controls.Add(this.btnSave);
            this.panelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelBottom.Location = new System.Drawing.Point(0, 467);
            this.panelBottom.Name = "panelBottom";
            this.panelBottom.Padding = new System.Windows.Forms.Padding(5);
            this.panelBottom.Size = new System.Drawing.Size(584, 50);
            this.panelBottom.TabIndex = 1;
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.BackColor = System.Drawing.Color.Gray;
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.FlatAppearance.BorderSize = 0;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCancel.ForeColor = System.Drawing.Color.White;
            this.btnCancel.Location = new System.Drawing.Point(350, 8);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(106, 34);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Annulla";
            this.btnCancel.UseVisualStyleBackColor = false;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnSave
            // 
            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSave.BackColor = System.Drawing.Color.Green;
            this.btnSave.FlatAppearance.BorderSize = 0;
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Location = new System.Drawing.Point(462, 8);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(106, 34);
            this.btnSave.TabIndex = 0;
            this.btnSave.Text = "Salva";
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // panelMain
            // 
            this.panelMain.AutoScroll = true;
            this.panelMain.Controls.Add(this.groupBox5);
            this.panelMain.Controls.Add(this.groupBox4);
            this.panelMain.Controls.Add(this.groupBox3);
            this.panelMain.Controls.Add(this.groupBox2);
            this.panelMain.Controls.Add(this.groupBox1);
            this.panelMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelMain.Location = new System.Drawing.Point(0, 50);
            this.panelMain.Name = "panelMain";
            this.panelMain.Padding = new System.Windows.Forms.Padding(10);
            this.panelMain.Size = new System.Drawing.Size(584, 417);
            this.panelMain.TabIndex = 2;
            // 
            // groupBox5
            // 
            this.groupBox5.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox5.Controls.Add(this.chkBoostVolume);
            this.groupBox5.Controls.Add(this.btnBrowseOutput);
            this.groupBox5.Controls.Add(this.txtOutputFile);
            this.groupBox5.Controls.Add(this.label11);
            this.groupBox5.Location = new System.Drawing.Point(13, 328);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(558, 80);
            this.groupBox5.TabIndex = 4;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "File di Output";
            // 
            // chkBoostVolume
            // 
            this.chkBoostVolume.AutoSize = true;
            this.chkBoostVolume.Location = new System.Drawing.Point(104, 53);
            this.chkBoostVolume.Name = "chkBoostVolume";
            this.chkBoostVolume.Size = new System.Drawing.Size(163, 19);
            this.chkBoostVolume.TabIndex = 3;
            this.chkBoostVolume.Text = "Aumenta volume (-0.5db)";
            this.chkBoostVolume.UseVisualStyleBackColor = true;
            this.chkBoostVolume.CheckedChanged += new System.EventHandler(this.chkBoostVolume_CheckedChanged);
            // 
            // btnBrowseOutput
            // 
            this.btnBrowseOutput.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseOutput.Location = new System.Drawing.Point(516, 24);
            this.btnBrowseOutput.Name = "btnBrowseOutput";
            this.btnBrowseOutput.Size = new System.Drawing.Size(31, 23);
            this.btnBrowseOutput.TabIndex = 2;
            this.btnBrowseOutput.Text = "...";
            this.btnBrowseOutput.UseVisualStyleBackColor = true;
            this.btnBrowseOutput.Click += new System.EventHandler(this.btnBrowseOutput_Click);
            // 
            // txtOutputFile
            // 
            this.txtOutputFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtOutputFile.Location = new System.Drawing.Point(102, 24);
            this.txtOutputFile.Name = "txtOutputFile";
            this.txtOutputFile.Size = new System.Drawing.Size(408, 23);
            this.txtOutputFile.TabIndex = 1;
            this.txtOutputFile.TextChanged += new System.EventHandler(this.txtOutputFile_TextChanged);
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(15, 27);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(80, 15);
            this.label11.TabIndex = 0;
            this.label11.Text = "File di output:";
            // 
            // groupBox4
            // 
            this.groupBox4.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox4.Controls.Add(this.btnBrowseCloser);
            this.groupBox4.Controls.Add(this.txtCloserFile);
            this.groupBox4.Controls.Add(this.chkUseCloser);
            this.groupBox4.Location = new System.Drawing.Point(13, 253);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(558, 69);
            this.groupBox4.TabIndex = 3;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "4.Jingle di Chiusura";
            // 
            // btnBrowseCloser
            // 
            this.btnBrowseCloser.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseCloser.Location = new System.Drawing.Point(516, 33);
            this.btnBrowseCloser.Name = "btnBrowseCloser";
            this.btnBrowseCloser.Size = new System.Drawing.Size(31, 23);
            this.btnBrowseCloser.TabIndex = 2;
            this.btnBrowseCloser.Text = "...";
            this.btnBrowseCloser.UseVisualStyleBackColor = true;
            this.btnBrowseCloser.Click += new System.EventHandler(this.btnBrowseCloser_Click);
            // 
            // txtCloserFile
            // 
            this.txtCloserFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtCloserFile.Location = new System.Drawing.Point(102, 33);
            this.txtCloserFile.Name = "txtCloserFile";
            this.txtCloserFile.Size = new System.Drawing.Size(408, 23);
            this.txtCloserFile.TabIndex = 1;
            this.txtCloserFile.TextChanged += new System.EventHandler(this.txtCloserFile_TextChanged);
            // 
            // chkUseCloser
            // 
            this.chkUseCloser.AutoSize = true;
            this.chkUseCloser.Location = new System.Drawing.Point(15, 37);
            this.chkUseCloser.Name = "chkUseCloser";
            this.chkUseCloser.Size = new System.Drawing.Size(77, 19);
            this.chkUseCloser.TabIndex = 0;
            this.chkUseCloser.Text = "Usa jingle";
            this.chkUseCloser.UseVisualStyleBackColor = true;
            this.chkUseCloser.CheckedChanged += new System.EventHandler(this.chkUseCloser_CheckedChanged);
            // 
            // groupBox3
            // 
            this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox3.Controls.Add(this.label12);
            this.groupBox3.Controls.Add(this.numVolume);
            this.groupBox3.Controls.Add(this.btnBrowseBackground);
            this.groupBox3.Controls.Add(this.txtBackgroundFile);
            this.groupBox3.Controls.Add(this.chkUseBackground);
            this.groupBox3.Location = new System.Drawing.Point(13, 159);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(558, 88);
            this.groupBox3.TabIndex = 2;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "3.Base di Sottofondo";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(101, 61);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(50, 15);
            this.label12.TabIndex = 4;
            this.label12.Text = "Volume:";
            // 
            // numVolume
            // 
            this.numVolume.Location = new System.Drawing.Point(161, 59);
            this.numVolume.Name = "numVolume";
            this.numVolume.Size = new System.Drawing.Size(68, 23);
            this.numVolume.TabIndex = 3;
            this.numVolume.Value = new decimal(new int[] {
            30,
            0,
            0,
            0});
            this.numVolume.ValueChanged += new System.EventHandler(this.numVolume_ValueChanged);
            // 
            // btnBrowseBackground
            // 
            this.btnBrowseBackground.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseBackground.Location = new System.Drawing.Point(516, 33);
            this.btnBrowseBackground.Name = "btnBrowseBackground";
            this.btnBrowseBackground.Size = new System.Drawing.Size(31, 23);
            this.btnBrowseBackground.TabIndex = 2;
            this.btnBrowseBackground.Text = "...";
            this.btnBrowseBackground.UseVisualStyleBackColor = true;
            this.btnBrowseBackground.Click += new System.EventHandler(this.btnBrowseBackground_Click);
            // 
            // txtBackgroundFile
            // 
            this.txtBackgroundFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtBackgroundFile.Location = new System.Drawing.Point(102, 33);
            this.txtBackgroundFile.Name = "txtBackgroundFile";
            this.txtBackgroundFile.Size = new System.Drawing.Size(408, 23);
            this.txtBackgroundFile.TabIndex = 1;
            this.txtBackgroundFile.TextChanged += new System.EventHandler(this.txtBackgroundFile_TextChanged);
            // 
            // chkUseBackground
            // 
            this.chkUseBackground.AutoSize = true;
            this.chkUseBackground.Location = new System.Drawing.Point(15, 37);
            this.chkUseBackground.Name = "chkUseBackground";
            this.chkUseBackground.Size = new System.Drawing.Size(72, 19);
            this.chkUseBackground.TabIndex = 0;
            this.chkUseBackground.Text = "Usa base";
            this.chkUseBackground.UseVisualStyleBackColor = true;
            this.chkUseBackground.CheckedChanged += new System.EventHandler(this.chkUseBackground_CheckedChanged);
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.btnBrowseMain);
            this.groupBox2.Controls.Add(this.txtMainFile);
            this.groupBox2.Controls.Add(this.label10);
            this.groupBox2.Location = new System.Drawing.Point(13, 86);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(558, 67);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "2.File Principale";
            // 
            // btnBrowseMain
            // 
            this.btnBrowseMain.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseMain.Location = new System.Drawing.Point(516, 24);
            this.btnBrowseMain.Name = "btnBrowseMain";
            this.btnBrowseMain.Size = new System.Drawing.Size(31, 23);
            this.btnBrowseMain.TabIndex = 2;
            this.btnBrowseMain.Text = "...";
            this.btnBrowseMain.UseVisualStyleBackColor = true;
            this.btnBrowseMain.Click += new System.EventHandler(this.btnBrowseMain_Click);
            // 
            // txtMainFile
            // 
            this.txtMainFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtMainFile.Location = new System.Drawing.Point(102, 24);
            this.txtMainFile.Name = "txtMainFile";
            this.txtMainFile.Size = new System.Drawing.Size(408, 23);
            this.txtMainFile.TabIndex = 1;
            this.txtMainFile.TextChanged += new System.EventHandler(this.txtMainFile_TextChanged);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(15, 27);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(83, 15);
            this.label10.TabIndex = 0;
            this.label10.Text = "File principale:";
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.btnBrowseOpener);
            this.groupBox1.Controls.Add(this.txtOpenerFile);
            this.groupBox1.Controls.Add(this.chkUseOpener);
            this.groupBox1.Location = new System.Drawing.Point(13, 13);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(558, 67);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "1.Jingle di Apertura";
            // 
            // btnBrowseOpener
            // 
            this.btnBrowseOpener.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseOpener.Location = new System.Drawing.Point(516, 33);
            this.btnBrowseOpener.Name = "btnBrowseOpener";
            this.btnBrowseOpener.Size = new System.Drawing.Size(31, 23);
            this.btnBrowseOpener.TabIndex = 2;
            this.btnBrowseOpener.Text = "...";
            this.btnBrowseOpener.UseVisualStyleBackColor = true;
            this.btnBrowseOpener.Click += new System.EventHandler(this.btnBrowseOpener_Click);
            // 
            // txtOpenerFile
            // 
            this.txtOpenerFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtOpenerFile.Location = new System.Drawing.Point(102, 33);
            this.txtOpenerFile.Name = "txtOpenerFile";
            this.txtOpenerFile.Size = new System.Drawing.Size(408, 23);
            this.txtOpenerFile.TabIndex = 1;
            this.txtOpenerFile.TextChanged += new System.EventHandler(this.txtOpenerFile_TextChanged);
            // 
            // chkUseOpener
            // 
            this.chkUseOpener.AutoSize = true;
            this.chkUseOpener.Location = new System.Drawing.Point(15, 37);
            this.chkUseOpener.Name = "chkUseOpener";
            this.chkUseOpener.Size = new System.Drawing.Size(77, 19);
            this.chkUseOpener.TabIndex = 0;
            this.chkUseOpener.Text = "Usa jingle";
            this.chkUseOpener.UseVisualStyleBackColor = true;
            this.chkUseOpener.CheckedChanged += new System.EventHandler(this.chkUseOpener_CheckedChanged);
            // 
            // CompositionSettingsForm
            // 
            this.AcceptButton = this.btnSave;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(584, 517);
            this.Controls.Add(this.panelMain);
            this.Controls.Add(this.panelBottom);
            this.Controls.Add(this.panelTop);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MinimumSize = new System.Drawing.Size(600, 540);
            this.Name = "CompositionSettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Impostazioni Composizione Audio";
            this.Load += new System.EventHandler(this.CompositionSettingsForm_Load);
            this.panelTop.ResumeLayout(false);
            this.panelTop.PerformLayout();
            this.panelBottom.ResumeLayout(false);
            this.panelMain.ResumeLayout(false);
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numVolume)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

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