namespace AirDirector.Models
{
    /// <summary>
    /// Item di una sequenza MiniPLS
    /// </summary>
    public class MiniPLSItem
    {
        public string FilePath { get; set; }
        public int Order { get; set; }

        public MiniPLSItem()
        {
            FilePath = string.Empty;
            Order = 0;
        }

        public MiniPLSItem(string filePath, int order)
        {
            FilePath = filePath;
            Order = order;
        }

        /// <summary>
        /// Ottiene il nome file senza path
        /// </summary>
        public string GetFileName()
        {
            if (string.IsNullOrEmpty(FilePath))
                return string.Empty;

            return System.IO.Path.GetFileName(FilePath);
        }

        public override string ToString()
        {
            return $"{Order}.{GetFileName()}";
        }
    }
}