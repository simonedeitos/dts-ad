namespace AirDirector.Forms
{
    partial class SplashForm
    {
        private System.ComponentModel.IContainer components = null;

        // Designer-visible controls
        private System.Windows.Forms.PictureBox pictureBoxLogo;
        private System.Windows.Forms.Label lblLogo;
        private System.Windows.Forms.Label lblVersion;
        private System.Windows.Forms.Panel cardsContainer;
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
            this.pictureBoxLogo = new System.Windows.Forms.PictureBox();
            this.lblLogo = new System.Windows.Forms.Label();
            this.lblVersion = new System.Windows.Forms.Label();
            this.cardsContainer = new System.Windows.Forms.Panel();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.lblPercentage = new System.Windows.Forms.Label();
            this.lblCopyright = new System.Windows.Forms.Label();

            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLogo)).BeginInit();
            this.SuspendLayout();

            // 
            // pictureBoxLogo
            // 
            this.pictureBoxLogo.Location = new System.Drawing.Point(250, 20);
            this.pictureBoxLogo.Name = "pictureBoxLogo";
            this.pictureBoxLogo.Size = new System.Drawing.Size(200, 80);
            this.pictureBoxLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxLogo.TabIndex = 0;
            this.pictureBoxLogo.TabStop = false;
            this.pictureBoxLogo.BackColor = System.Drawing.Color.Transparent;
            this.pictureBoxLogo.Visible = false;

            // 
            // lblLogo
            // 
            this.lblLogo.AutoSize = false;
            this.lblLogo.Location = new System.Drawing.Point(50, 20);
            this.lblLogo.Name = "lblLogo";
            this.lblLogo.Size = new System.Drawing.Size(600, 80);
            this.lblLogo.TabIndex = 1;
            this.lblLogo.Text = "🎵 AirDirector";
            this.lblLogo.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            // 
            // lblVersion
            // 
            this.lblVersion.AutoSize = false;
            this.lblVersion.Location = new System.Drawing.Point(50, 95);
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Size = new System.Drawing.Size(600, 25);
            this.lblVersion.TabIndex = 2;
            this.lblVersion.Text = "Professional Playout System";
            this.lblVersion.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            // 
            // cardsContainer
            // 
            this.cardsContainer.Location = new System.Drawing.Point(50, 130);
            this.cardsContainer.Name = "cardsContainer";
            this.cardsContainer.Size = new System.Drawing.Size(600, 290);
            this.cardsContainer.TabIndex = 3;
            this.cardsContainer.BackColor = System.Drawing.Color.Transparent;

            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(50, 460);
            this.progressBar.Maximum = 100;
            this.progressBar.Minimum = 0;
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(540, 10);
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressBar.TabIndex = 4;
            this.progressBar.Value = 0;

            // 
            // lblPercentage
            // 
            this.lblPercentage.AutoSize = false;
            this.lblPercentage.Location = new System.Drawing.Point(600, 457);
            this.lblPercentage.Name = "lblPercentage";
            this.lblPercentage.Size = new System.Drawing.Size(50, 16);
            this.lblPercentage.TabIndex = 5;
            this.lblPercentage.Text = "0%";
            this.lblPercentage.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

            // 
            // lblCopyright
            // 
            this.lblCopyright.AutoSize = false;
            this.lblCopyright.Location = new System.Drawing.Point(50, 500);
            this.lblCopyright.Name = "lblCopyright";
            this.lblCopyright.Size = new System.Drawing.Size(600, 20);
            this.lblCopyright.TabIndex = 6;
            this.lblCopyright.Text = "© 2026 AirDirector - All Rights Reserved";
            this.lblCopyright.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            // 
            // SplashForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(700, 550);
            this.Controls.Add(this.pictureBoxLogo);
            this.Controls.Add(this.lblLogo);
            this.Controls.Add(this.lblVersion);
            this.Controls.Add(this.cardsContainer);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.lblPercentage);
            this.Controls.Add(this.lblCopyright);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "SplashForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "AirDirector";
            this.Load += new System.EventHandler(this.SplashForm_Load);

            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLogo)).EndInit();
            this.ResumeLayout(false);
        }
    }
}