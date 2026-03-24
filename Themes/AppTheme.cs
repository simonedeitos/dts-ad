using System.Drawing;

namespace AirDirector.Themes
{
    /// <summary>
    /// Tema colori dell'applicazione (stile MB Studio)
    /// </summary>
    public static class AppTheme
    {
        // Primary Colors
        public static readonly Color Primary = ColorTranslator.FromHtml("#1976D2");
        public static readonly Color Success = ColorTranslator.FromHtml("#4CAF50");
        public static readonly Color Warning = ColorTranslator.FromHtml("#FF9800");
        public static readonly Color Danger = ColorTranslator.FromHtml("#F44336");
        public static readonly Color Info = ColorTranslator.FromHtml("#2196F3");

        // Backgrounds
        public static readonly Color BgDark = ColorTranslator.FromHtml("#1E1E1E");
        public static readonly Color BgLight = ColorTranslator.FromHtml("#F5F5F5");
        public static readonly Color Surface = ColorTranslator.FromHtml("#FFFFFF");
        public static readonly Color BgPlayer = ColorTranslator.FromHtml("#263238");

        // Text
        public static readonly Color TextPrimary = ColorTranslator.FromHtml("#212121");
        public static readonly Color TextSecondary = ColorTranslator.FromHtml("#757575");
        public static readonly Color TextInverse = ColorTranslator.FromHtml("#FFFFFF");

        // LED Colors (stile WinJay)
        public static readonly Color LEDGreen = ColorTranslator.FromHtml("#00FF41");
        public static readonly Color LEDRed = ColorTranslator.FromHtml("#FF0000");
        public static readonly Color LEDYellow = ColorTranslator.FromHtml("#FFD700");
        public static readonly Color LEDBlue = ColorTranslator.FromHtml("#00BFFF");

        // Accents
        public static readonly Color AccentBlue = ColorTranslator.FromHtml("#03A9F4");
        public static readonly Color AccentPurple = ColorTranslator.FromHtml("#9C27B0");
        public static readonly Color AccentCyan = ColorTranslator.FromHtml("#00BCD4");
        public static readonly Color AccentOrange = ColorTranslator.FromHtml("#FF6B35");

        // VU Meter Gradient
        public static readonly Color VUGreen = ColorTranslator.FromHtml("#4CAF50");
        public static readonly Color VUYellow = ColorTranslator.FromHtml("#FFC107");
        public static readonly Color VURed = ColorTranslator.FromHtml("#F44336");

        // Player States
        public static readonly Color StatePlaying = ColorTranslator.FromHtml("#4CAF50");
        public static readonly Color StatePaused = ColorTranslator.FromHtml("#FF9800");
        public static readonly Color StateStopped = ColorTranslator.FromHtml("#757575");

        // Borders
        public static readonly Color BorderLight = ColorTranslator.FromHtml("#E0E0E0");
        public static readonly Color BorderDark = ColorTranslator.FromHtml("#424242");
    }
}