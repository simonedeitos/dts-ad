namespace AirDirector.Models
{
    public class AdditionalLogo
    {
        public string Name { get; set; }
        public string ImagePath { get; set; }
        public string Position { get; set; }
        public int MarginX { get; set; }
        public int MarginY { get; set; }
        public float Scale { get; set; }

        public AdditionalLogo()
        {
            Name = string.Empty;
            ImagePath = string.Empty;
            Position = "BottomLeft";
            MarginX = 0;
            MarginY = 0;
            Scale = 1.0f;
        }

        public override string ToString()
        {
            return !string.IsNullOrWhiteSpace(Name)
                ? Name
                : string.IsNullOrWhiteSpace(ImagePath)
                ? Position
                : $"{System.IO.Path.GetFileName(ImagePath)} ({Position})";
        }
    }
}
