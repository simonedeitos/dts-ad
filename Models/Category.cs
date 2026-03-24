using System.Drawing;

namespace AirDirector.Models
{
    /// <summary>
    /// Categoria per brani e clips
    /// </summary>
    public class Category
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string ColorHex { get; set; }  // Es: #4ECDC4
        public bool IgnoreHourlySeparation { get; set; }

        public Category()
        {
            ID = 0;
            Name = string.Empty;
            ColorHex = "#2196F3";  // Blu default
            IgnoreHourlySeparation = false;
        }

        public Category(int id, string name, string colorHex, bool ignoreHourlySeparation = false)
        {
            ID = id;
            Name = name;
            ColorHex = colorHex;
            IgnoreHourlySeparation = ignoreHourlySeparation;
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
                return Color.Blue;
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