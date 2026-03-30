namespace AirDirector.Models
{
    public class RemoteControlConfig
    {
        public string Token { get; set; } = "";
        public bool AutoOpenOnStartup { get; set; } = false;
        public bool MinimizeToTrayOnStartup { get; set; } = false;
        public string AudioSource { get; set; } = "airdirector"; // "airdirector" o nome dispositivo
        public string AudioQuality { get; set; } = "medium"; // low, medium, high, studio
        public string AudioOutputDevice { get; set; } = "default";
    }
}
