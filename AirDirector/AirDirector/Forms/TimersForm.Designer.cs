namespace AirDirector.Forms
{
    partial class TimersForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
                _updateTimer?.Stop();
                _updateTimer?.Dispose();
                AirDirector.Services.Localization.LanguageManager.LanguageChanged -= OnLanguageChanged;
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            // 
            // TimersForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(941, 500);
            MinimumSize = new Size(352, 284);
            Name = "TimersForm";
            StartPosition = FormStartPosition.CenterParent;
            ResumeLayout(false);
        }
    }
}
