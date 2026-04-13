namespace AirDirector.Forms
{
    partial class LicenseRemoveConfirmForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LicenseRemoveConfirmForm));
            SuspendLayout();
            // 
            // LicenseRemoveConfirmForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(385, 262);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "LicenseRemoveConfirmForm";
            Text = "Conferma Rimozione";
            ResumeLayout(false);
        }
    }
}
