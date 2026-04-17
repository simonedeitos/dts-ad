using AirDirector.Services.Database;

namespace AirDirector.Models
{
    public class CommandEntry : IDbcEntry
    {
        public int ID { get; set; }
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string CommandString { get; set; } = "";

        public override string ToString() => Name;
    }
}
