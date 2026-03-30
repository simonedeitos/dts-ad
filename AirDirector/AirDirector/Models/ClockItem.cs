using System;

namespace AirDirector.Models
{
    /// <summary>
    /// Elemento di un Clock: rappresenta una categoria o genere con filtri
    /// </summary>
    public class ClockItem
    {
        /// <summary>
        /// Tipo di elemento: "Category" o "Genre"
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Nome della categoria o genere
        /// </summary>
        public string CategoryName { get; set; }

        /// <summary>
        /// Valore (alias per CategoryName per compatibilità)
        /// </summary>
        public string Value
        {
            get => CategoryName;
            set => CategoryName = value;
        }

        /// <summary>
        /// Numero di brani da estrarre (usato nella generazione playlist)
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Se true, applica il filtro anni
        /// </summary>
        public bool YearFilterEnabled { get; set; }

        /// <summary>
        /// Anno minimo (se YearFilterEnabled = true)
        /// </summary>
        public int YearFrom { get; set; }

        /// <summary>
        /// Anno massimo (se YearFilterEnabled = true)
        /// </summary>
        public int YearTo { get; set; }

        /// <summary>
        /// Costruttore vuoto
        /// </summary>
        public ClockItem()
        {
            Type = "Category";
            CategoryName = string.Empty;
            Count = 1;
            YearFilterEnabled = false;
            YearFrom = 1900;
            YearTo = DateTime.Now.Year;
        }

        /// <summary>
        /// Costruttore legacy per compatibilità
        /// </summary>
        public ClockItem(string categoryName, int count = 1)
        {
            Type = "Category";
            CategoryName = categoryName;
            Count = count;
            YearFilterEnabled = false;
            YearFrom = 1900;
            YearTo = DateTime.Now.Year;
        }

        /// <summary>
        /// Costruttore completo con filtro anni
        /// </summary>
        public ClockItem(string type, string value, bool yearFilter = false, int yearFrom = 1900, int yearTo = 0)
        {
            Type = type;
            CategoryName = value;
            Count = 1;
            YearFilterEnabled = yearFilter;
            YearFrom = yearFrom;
            YearTo = yearTo > 0 ? yearTo : DateTime.Now.Year;
        }

        public override string ToString()
        {
            string icon = Type == "Category" ? "📁" : "🎵";
            string filter = YearFilterEnabled ? $" [{YearFrom}-{YearTo}]" : "";
            return $"{icon} {CategoryName}{filter} (x{Count})";
        }

        /// <summary>
        /// Descrizione breve per visualizzazione
        /// </summary>
        public string GetShortDescription()
        {
            string filter = YearFilterEnabled ? $" [{YearFrom}-{YearTo}]" : "";
            return $"{CategoryName}{filter}";
        }
    }
}