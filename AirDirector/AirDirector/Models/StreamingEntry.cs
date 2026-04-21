using AirDirector.Services.Database;

namespace AirDirector.Models
{
    public class StreamingEntry : IDbcEntry
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string URL { get; set; }
        public bool IsVideoStream { get; set; }

        public StreamingEntry()
        {
            ID = 0;
            Name = string.Empty;
            URL = string.Empty;
            IsVideoStream = false;
        }

        public override string ToString() => Name;
    }
}
