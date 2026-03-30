namespace AirDirector.Forms
{
    partial class RemoteControlForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
                AirDirector.Services.Localization.LanguageManager.LanguageChanged -= OnLanguageChanged;
                _remoteService?.Dispose();
                _audioService?.Dispose();
                _stateTimer?.Stop();
                _stateTimer?.Dispose();
                _vuTimer?.Stop();
                _vuTimer?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(560, 640);
            MinimumSize = new System.Drawing.Size(420, 540);
            Name = "RemoteControlForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            ResumeLayout(false);
        }
    }
}
