namespace AirDirector.Forms
{
    partial class ReportExportDialog
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ReportExportDialog));
            SuspendLayout();
            // 
            // ReportExportDialog
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(600, 620);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "ReportExportDialog";
            Text = "Export Report CSV";
            ResumeLayout(false);
        }
    }
}
