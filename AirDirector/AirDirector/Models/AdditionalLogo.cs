namespace AirDirector.Models
{
    public class AdditionalLogo
    {
        public string ImagePath { get; set; }
        public string Position { get; set; }
        public int MarginX { get; set; }
        public int MarginY { get; set; }

        public AdditionalLogo()
        {
            ImagePath = string.Empty;
            Position = "BottomLeft";
            MarginX = 0;
            MarginY = 0;
        }

        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(ImagePath)
                ? Position
                : $"{System.IO.Path.GetFileName(ImagePath)} ({Position})";
        }
    }
}
