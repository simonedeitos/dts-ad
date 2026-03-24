using System.Drawing;

namespace AirDirector.Models
{
    /// <summary>
    /// Genere musicale
    /// </summary>
    public class Genre
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string ColorHex { get; set; }  // Es: #FF6B6B

        public Genre()
        {
            ID = 0;
            Name = string.Empty;
            ColorHex = "#808080";  // Grigio default
        }

        public Genre(int id, string name, string colorHex)
        {
            ID = id;
            Name = name;
            ColorHex = colorHex;
        }

        /// <summary>
        /// Ottiene il colore come oggetto Color
        /// </summary>
        public Color GetColor()
        {
            try
            {
                return ColorTranslator.FromHtml(ColorHex);
            }
            catch
            {
                return Color.Gray;
            }
        }

        /// <summary>
        /// Imposta il colore da oggetto Color
        /// </summary>
        public void SetColor(Color color)
        {
            ColorHex = ColorTranslator.ToHtml(color);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}