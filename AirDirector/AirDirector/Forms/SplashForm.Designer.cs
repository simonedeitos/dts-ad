namespace AirDirector.Forms
{
    partial class SplashForm
    {
        private System.ComponentModel.IContainer components = null;

        // Designer-visible controls
        private System.Windows.Forms.Panel headerPanel;
        private System.Windows.Forms.PictureBox pictureBoxLogo;
        private System.Windows.Forms.Label lblLogo;
        private System.Windows.Forms.Label lblVersion;
        private System.Windows.Forms.Panel statusPanel;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label lblPercentage;
        private System.Windows.Forms.Label lblCopyright;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.headerPanel = new System.Windows.Forms.Panel();
            this.pictureBoxLogo = new System.Windows.Forms.PictureBox();
            this.lblLogo = new System.Windows.Forms.Label();
            this.lblVersion = new System.Windows.Forms.Label();
            this.statusPanel = new System.Windows.Forms.Panel();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.lblPercentage = new System.Windows.Forms.Label();
            this.lblCopyright = new System.Windows.Forms.Label();

            this.headerPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLogo)).BeginInit();
            this.SuspendLayout();

            // 
            // headerPanel
            // 
            this.headerPanel.Controls.Add(this.pictureBoxLogo);
            this.headerPanel.Controls.Add(this.lblLogo);
            this.headerPanel.Controls.Add(this.lblVersion);
            this.headerPanel.Location = new System.Drawing.Point(0, 0);
            this.headerPanel.Name = "headerPanel";
            this.headerPanel.Size = new System.Drawing.Size(750, 120);
            this.headerPanel.TabIndex = 0;

            // 
            // pictureBoxLogo
            // 
            this.pictureBoxLogo.Location = new System.Drawing.Point(300, 10);
            this.pictureBoxLogo.Name = "pictureBoxLogo";
            this.pictureBoxLogo.Size = new System.Drawing.Size(150, 60);
            this.pictureBoxLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxLogo.TabIndex = 0;
            this.pictureBoxLogo.TabStop = false;
            this.pictureBoxLogo.Visible = false;

            // 
            // lblLogo
            // 
            this.lblLogo.AutoSize = false;
            this.lblLogo.Location = new System.Drawing.Point(20, 15);
            this.lblLogo.Name = "lblLogo";
            this.lblLogo.Size = new System.Drawing.Size(710, 55);
            this.lblLogo.TabIndex = 1;
            this.lblLogo.Text = "🎵 AirDirector";
            this.lblLogo.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            // 
            // lblVersion
            // 
            this.lblVersion.AutoSize = false;
            this.lblVersion.Location = new System.Drawing.Point(20, 80);
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Size = new System.Drawing.Size(710, 25);
            this.lblVersion.TabIndex = 2;
            this.lblVersion.Text = "Professional Playout System";
            this.lblVersion.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            // 
            // statusPanel
            // 
            this.statusPanel.Location = new System.Drawing.Point(40, 135);
            this.statusPanel.Name = "statusPanel";
            this.statusPanel.Size = new System.Drawing.Size(670, 280);
            this.statusPanel.TabIndex = 1;

            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(40, 435);
            this.progressBar.Maximum = 100;
            this.progressBar.Minimum = 0;
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(590, 14);
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressBar.TabIndex = 2;
            this.progressBar.Value = 0;

            // 
            // lblPercentage
            // 
            this.lblPercentage.AutoSize = false;
            this.lblPercentage.Location = new System.Drawing.Point(638, 432);
            this.lblPercentage.Name = "lblPercentage";
            this.lblPercentage.Size = new System.Drawing.Size(72, 20);
            this.lblPercentage.TabIndex = 3;
            this.lblPercentage.Text = "0%";
            this.lblPercentage.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

            // 
            // lblCopyright
            // 
            this.lblCopyright.AutoSize = false;
            this.lblCopyright.Location = new System.Drawing.Point(20, 462);
            this.lblCopyright.Name = "lblCopyright";
            this.lblCopyright.Size = new System.Drawing.Size(710, 20);
            this.lblCopyright.TabIndex = 4;
            this.lblCopyright.Text = "© 2025 AirDirector - All Rights Reserved";
            this.lblCopyright.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            // 
            // SplashForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(750, 490);
            this.Controls.Add(this.headerPanel);
            this.Controls.Add(this.statusPanel);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.lblPercentage);
            this.Controls.Add(this.lblCopyright);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "SplashForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "AirDirector";
            this.Load += new System.EventHandler(this.SplashForm_Load);

            this.headerPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLogo)).EndInit();
            this.ResumeLayout(false);
        }
    }
}