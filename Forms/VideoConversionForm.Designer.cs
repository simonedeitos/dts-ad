namespace AirDirector.Forms
{
    partial class VideoConversionForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Panel pnlTop;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblHint;
        private System.Windows.Forms.Panel pnlScroll;
        private System.Windows.Forms.Panel pnlBottom;
        private System.Windows.Forms.ProgressBar progressOverall;
        private System.Windows.Forms.Label lblOverall;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnSkipConvert;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnClose;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.pnlTop = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblHint = new System.Windows.Forms.Label();
            this.pnlScroll = new System.Windows.Forms.Panel();
            this.pnlBottom = new System.Windows.Forms.Panel();
            this.progressOverall = new System.Windows.Forms.ProgressBar();
            this.lblOverall = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnStart = new System.Windows.Forms.Button();
            this.btnSkipConvert = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();

            this.pnlTop.SuspendLayout();
            this.pnlBottom.SuspendLayout();
            this.SuspendLayout();

            // ═══════════════════════════════════════════════════════════════
            // pnlTop (Header – altezza 85px)
            // ═══════════════════════════════════════════════════════════════
            this.pnlTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlTop.Height = 85;
            this.pnlTop.BackColor = System.Drawing.Color.FromArgb(28, 28, 40);
            this.pnlTop.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
            this.pnlTop.Controls.Add(this.lblTitle);
            this.pnlTop.Controls.Add(this.lblHint);

            // lblTitle
            this.lblTitle.Text = "📂  File Importer Converter";
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 14, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.AutoSize = true;
            this.lblTitle.Location = new System.Drawing.Point(12, 10);
            this.lblTitle.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;

            // lblHint
            this.lblHint.Text =
                "ℹ️  Target: H.264 16:9 AAC 48kHz 2ch MP4 (video) | MP3 CBR 320kbps or WAV 16bit (audio).\r\n" +
                "    Compatible files can be skipped via per-file toggles. Originals moved to subfolders.";
            this.lblHint.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblHint.ForeColor = System.Drawing.Color.FromArgb(170, 200, 255);
            this.lblHint.Location = new System.Drawing.Point(12, 42);
            this.lblHint.AutoSize = false;
            this.lblHint.Size = new System.Drawing.Size(880, 38);
            this.lblHint.Anchor =
                System.Windows.Forms.AnchorStyles.Top |
                System.Windows.Forms.AnchorStyles.Left |
                System.Windows.Forms.AnchorStyles.Right;

            // ═══════════════════════════════════════════════════════════════
            // pnlScroll (pannello elenco file – PARTE DA TOP = 135px)
            // ═══════════════════════════════════════════════════════════════
            this.pnlScroll.Location = new System.Drawing.Point(0, 167);  // 85 (pnlTop) + 50 (pnlPreEditing) + 32 (pnlTagSource) = 167
            this.pnlScroll.Dock = System.Windows.Forms.DockStyle.None;
            this.pnlScroll.Anchor =
                System.Windows.Forms.AnchorStyles.Top |
                System.Windows.Forms.AnchorStyles.Left |
                System.Windows.Forms.AnchorStyles.Right |
                System.Windows.Forms.AnchorStyles.Bottom;
            this.pnlScroll.AutoScroll = true;
            this.pnlScroll.BackColor = System.Drawing.Color.FromArgb(20, 20, 30);
            this.pnlScroll.Padding = new System.Windows.Forms.Padding(4, 10, 4, 4);

            // Resize dinamico: quando il form si ridimensiona, aggiorna pnlScroll
            this.Resize += (s, ev) =>
            {
                int top = 167; // fisso: 85 + 50 + 32
                int bottom = this.pnlBottom.Height;
                this.pnlScroll.Location = new System.Drawing.Point(0, top);
                this.pnlScroll.Size = new System.Drawing.Size(
                    this.ClientSize.Width,
                    this.ClientSize.Height - top - bottom);

                // Aggiorna larghezza delle righe dei file
                int newW = this.pnlScroll.ClientSize.Width - 8;
                if (newW < 100) return;

                foreach (System.Windows.Forms.Control row in this.pnlScroll.Controls)
                {
                    if (row is System.Windows.Forms.Panel)
                    {
                        row.Width = newW;
                        foreach (System.Windows.Forms.Control child in row.Controls)
                        {
                            if (child is System.Windows.Forms.Label lbl && lbl.AutoEllipsis)
                                lbl.Width = newW - lbl.Left - 14;
                            else if (child is System.Windows.Forms.ProgressBar pb)
                                pb.Width = newW - 183;
                        }
                    }
                }
            };

            // ═══════════════════════════════════════════════════════════════
            // pnlBottom (footer con progress e bottoni)
            // ═══════════════════════════════════════════════════════════════
            this.pnlBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlBottom.Height = 96;
            this.pnlBottom.BackColor = System.Drawing.Color.FromArgb(28, 28, 40);
            this.pnlBottom.Controls.Add(this.progressOverall);
            this.pnlBottom.Controls.Add(this.lblOverall);
            this.pnlBottom.Controls.Add(this.lblStatus);
            this.pnlBottom.Controls.Add(this.btnStart);
            this.pnlBottom.Controls.Add(this.btnSkipConvert);
            this.pnlBottom.Controls.Add(this.btnCancel);
            this.pnlBottom.Controls.Add(this.btnClose);

            this.progressOverall.Location = new System.Drawing.Point(10, 8);
            this.progressOverall.Height = 18;
            this.progressOverall.Minimum = 0;
            this.progressOverall.Value = 0;
            this.progressOverall.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressOverall.Anchor =
                System.Windows.Forms.AnchorStyles.Top |
                System.Windows.Forms.AnchorStyles.Left |
                System.Windows.Forms.AnchorStyles.Right;

            this.lblOverall.Text = "Reading specs…";
            this.lblOverall.Location = new System.Drawing.Point(10, 30);
            this.lblOverall.Height = 16;
            this.lblOverall.Font = new System.Drawing.Font("Segoe UI", 8);
            this.lblOverall.ForeColor = System.Drawing.Color.LightGray;
            this.lblOverall.Anchor =
                System.Windows.Forms.AnchorStyles.Top |
                System.Windows.Forms.AnchorStyles.Left |
                System.Windows.Forms.AnchorStyles.Right;

            this.lblStatus.Text = "";
            this.lblStatus.Location = new System.Drawing.Point(10, 48);
            this.lblStatus.Height = 16;
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 8, System.Drawing.FontStyle.Italic);
            this.lblStatus.ForeColor = System.Drawing.Color.FromArgb(170, 200, 255);
            this.lblStatus.Anchor =
                System.Windows.Forms.AnchorStyles.Top |
                System.Windows.Forms.AnchorStyles.Left |
                System.Windows.Forms.AnchorStyles.Right;

            // ───────────────────────────────────────────────────────────────
            // BOTTONI (con TabStop e BringToFront per visibilità)
            // ───────────────────────────────────────────────────────────────

            // btnStart
            this.btnStart.Text = "▶  Start Conversion";
            this.btnStart.Height = 30;
            this.btnStart.Width = 160;
            this.btnStart.BackColor = System.Drawing.Color.FromArgb(40, 167, 69);
            this.btnStart.ForeColor = System.Drawing.Color.White;
            this.btnStart.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStart.Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold);
            this.btnStart.FlatAppearance.BorderSize = 0;
            this.btnStart.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnStart.Enabled = false;
            this.btnStart.TabStop = true;
            this.btnStart.TabIndex = 1;
            this.btnStart.Anchor =
                System.Windows.Forms.AnchorStyles.Bottom |
                System.Windows.Forms.AnchorStyles.Right;
            this.btnStart.Click += this.btnStart_Click;

            // btnSkipConvert
            this.btnSkipConvert.Text = "⚠  Skip Conversion (Not Recommended)";
            this.btnSkipConvert.Height = 30;
            this.btnSkipConvert.Width = 245;
            this.btnSkipConvert.BackColor = System.Drawing.Color.FromArgb(160, 90, 0);
            this.btnSkipConvert.ForeColor = System.Drawing.Color.White;
            this.btnSkipConvert.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSkipConvert.Font = new System.Drawing.Font("Segoe UI", 8, System.Drawing.FontStyle.Bold);
            this.btnSkipConvert.FlatAppearance.BorderSize = 0;
            this.btnSkipConvert.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSkipConvert.Enabled = false;
            this.btnSkipConvert.TabStop = true;
            this.btnSkipConvert.TabIndex = 2;
            this.btnSkipConvert.Anchor =
                System.Windows.Forms.AnchorStyles.Bottom |
                System.Windows.Forms.AnchorStyles.Right;
            this.btnSkipConvert.Click += this.btnSkipConvert_Click;

            // btnCancel
            this.btnCancel.Text = "⏹  Cancel";
            this.btnCancel.Height = 30;
            this.btnCancel.Width = 100;
            this.btnCancel.BackColor = System.Drawing.Color.FromArgb(220, 53, 69);
            this.btnCancel.ForeColor = System.Drawing.Color.White;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold);
            this.btnCancel.FlatAppearance.BorderSize = 0;
            this.btnCancel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCancel.Enabled = false;
            this.btnCancel.TabStop = true;
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Anchor =
                System.Windows.Forms.AnchorStyles.Bottom |
                System.Windows.Forms.AnchorStyles.Right;
            this.btnCancel.Click += this.btnCancel_Click;

            // btnClose
            this.btnClose.Text = "✖  Close";
            this.btnClose.Height = 30;
            this.btnClose.Width = 100;
            this.btnClose.BackColor = System.Drawing.Color.FromArgb(108, 117, 125);
            this.btnClose.ForeColor = System.Drawing.Color.White;
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold);
            this.btnClose.FlatAppearance.BorderSize = 0;
            this.btnClose.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnClose.Enabled = false;
            this.btnClose.TabStop = true;
            this.btnClose.TabIndex = 4;
            this.btnClose.Anchor =
                System.Windows.Forms.AnchorStyles.Bottom |
                System.Windows.Forms.AnchorStyles.Right;
            this.btnClose.Click += this.btnClose_Click;

            // reposition buttons + progress bar on resize
            this.pnlBottom.Resize += (s, ev) =>
            {
                int panH = this.pnlBottom.ClientSize.Height;
                int panW = this.pnlBottom.ClientSize.Width;
                int btnY = panH - this.btnClose.Height - 8;
                int right = panW - 10;

                this.btnClose.Location = new System.Drawing.Point(right - this.btnClose.Width, btnY);
                this.btnCancel.Location = new System.Drawing.Point(this.btnClose.Left - this.btnCancel.Width - 8, btnY);
                this.btnStart.Location = new System.Drawing.Point(this.btnCancel.Left - this.btnStart.Width - 8, btnY);
                this.btnSkipConvert.Location = new System.Drawing.Point(this.btnStart.Left - this.btnSkipConvert.Width - 16, btnY);

                int barRight = this.btnSkipConvert.Left - 14;
                if (barRight > 20)
                {
                    this.progressOverall.Width = barRight - 10;
                    this.lblOverall.Width = barRight - 10;
                    this.lblStatus.Width = barRight - 10;
                }
            };

            // PORTARE BOTTONI IN PRIMO PIANO (risolve il problema della scritta nascosta)
            this.btnClose.BringToFront();
            this.btnCancel.BringToFront();
            this.btnStart.BringToFront();
            this.btnSkipConvert.BringToFront();

            // ═══════════════════════════════════════════════════════════════
            // Form principale
            // ═══════════════════════════════════════════════════════════════
            this.Text = "File Importer Converter — AirDirector";
            this.ClientSize = new System.Drawing.Size(920, 600);
            this.MinimumSize = new System.Drawing.Size(920, 480);
            this.MaximumSize = new System.Drawing.Size(920, System.Int32.MaxValue);
            this.BackColor = System.Drawing.Color.FromArgb(20, 20, 30);
            this.ForeColor = System.Drawing.Color.White;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.ShowInTaskbar = true;

            this.Load += this.VideoConversionForm_Load;

            this.Controls.Add(this.pnlScroll);
            this.Controls.Add(this.pnlBottom);
            this.Controls.Add(this.pnlTop);

            this.pnlTop.ResumeLayout(false);
            this.pnlTop.PerformLayout();
            this.pnlBottom.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
