namespace AirDirector.Forms
{
    partial class LicenseInfoForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LicenseInfoForm));
            SuspendLayout();
            // 
            // LicenseInfoForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(455, 431);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "LicenseInfoForm";
            Text = "Gestione Licenza";
            ResumeLayout(false);
        }
    }
}
