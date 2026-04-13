namespace AirDirector.Forms
{
    partial class LicenseForm
    {
        private System.ComponentModel.IContainer components = null;

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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LicenseForm));
            SuspendLayout();
            // 
            // LicenseForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(546, 563);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimumSize = new Size(562, 602);
            Name = "LicenseForm";
            Text = "Attivazione Licenza";
            Load += LicenseForm_Load;
            ResumeLayout(false);
        }
    }
}