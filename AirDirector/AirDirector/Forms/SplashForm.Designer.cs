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
            if (disposing)
            {
                _logoImage?.Dispose();
                _backgroundImage?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            pictureBoxLogo = new PictureBox();
            lblLogo = new Label();
            lblVersion = new Label();
            cardsContainer = new Panel();
            progressBar = new ProgressBar();
            lblPercentage = new Label();
            lblCopyright = new Label();
            ((System.ComponentModel.ISupportInitialize)pictureBoxLogo).BeginInit();
            SuspendLayout();
            // 
            // pictureBoxLogo
            // 
            pictureBoxLogo.BackColor = Color.Transparent;
            pictureBoxLogo.Location = new Point(219, 19);
            pictureBoxLogo.Name = "pictureBoxLogo";
            pictureBoxLogo.Size = new Size(175, 75);
            pictureBoxLogo.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxLogo.TabIndex = 0;
            pictureBoxLogo.TabStop = false;
            pictureBoxLogo.Visible = false;
            // 
            // lblLogo
            // 
            lblLogo.Location = new Point(44, 19);
            lblLogo.Name = "lblLogo";
            lblLogo.Size = new Size(525, 75);
            lblLogo.TabIndex = 1;
            lblLogo.Text = "🎵 AirDirector";
            lblLogo.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblVersion
            // 
            lblVersion.Location = new Point(44, 89);
            lblVersion.Name = "lblVersion";
            lblVersion.Size = new Size(525, 23);
            lblVersion.TabIndex = 2;
            lblVersion.Text = "Professional Playout System";
            lblVersion.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // cardsContainer
            // 
            cardsContainer.BackColor = Color.Transparent;
            cardsContainer.Location = new Point(84, 125);
            cardsContainer.Name = "cardsContainer";
            cardsContainer.Size = new Size(444, 320);
            cardsContainer.TabIndex = 3;
            cardsContainer.Paint += cardsContainer_Paint;
            // 
            // progressBar
            // 
            progressBar.Location = new Point(109, 464);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(352, 9);
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.TabIndex = 4;
            // 
            // lblPercentage
            // 
            lblPercentage.Location = new Point(467, 461);
            lblPercentage.Name = "lblPercentage";
            lblPercentage.Size = new Size(44, 15);
            lblPercentage.TabIndex = 5;
            lblPercentage.Text = "0%";
            lblPercentage.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblCopyright
            // 
            lblCopyright.Location = new Point(44, 499);
            lblCopyright.Name = "lblCopyright";
            lblCopyright.Size = new Size(525, 19);
            lblCopyright.TabIndex = 6;
            lblCopyright.Text = "© 2026 AirDirector - All Rights Reserved";
            lblCopyright.TextAlign = ContentAlignment.MiddleCenter;
            lblCopyright.Click += lblCopyright_Click;
            // 
            // SplashForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(612, 545);
            Controls.Add(pictureBoxLogo);
            Controls.Add(lblLogo);
            Controls.Add(lblVersion);
            Controls.Add(cardsContainer);
            Controls.Add(progressBar);
            Controls.Add(lblPercentage);
            Controls.Add(lblCopyright);
            FormBorderStyle = FormBorderStyle.None;
            Name = "SplashForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "AirDirector";
            Load += SplashForm_Load;
            ((System.ComponentModel.ISupportInitialize)pictureBoxLogo).EndInit();
            ResumeLayout(false);
        }
    }
}