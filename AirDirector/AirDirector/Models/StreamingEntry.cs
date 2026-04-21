using AirDirector.Services.Database;

namespace AirDirector.Models
{
    public class StreamingEntry : IDbcEntry
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string URL { get; set; }

        public StreamingEntry()
        {
            ID = 0;
            Name = string.Empty;
            URL = string.Empty;
        }

        public override string ToString() => Name;
    }
}
