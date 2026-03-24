using System;

namespace AirDirector.Services.Database
{
    [Serializable]
    public class EncoderEntry : IDbcEntry  // ✅ AGGIUNGI INTERFACCIA
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string StationName { get; set; }
        public string Host { get; set; }
        public string ServerUrl { get; set; }
        public int Port { get; set; }
        public int ServerPort { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string MountPoint { get; set; }
        public string Format { get; set; }
        public int Bitrate { get; set; }
        public int SampleRate { get; set; }  // ✅ AGGIUNTO
        public int AutoConnect { get; set; }  // ✅ AGGIUNTO
        public int IsActive { get; set; }  // ✅ AGGIUNTO
        public string AudioSourceDevice { get; set; }
        public bool EnableAGC { get; set; }
        public float AGCTargetLevel { get; set; }
        public float AGCAttackTime { get; set; }
        public float AGCReleaseTime { get; set; }
        public float LimiterThreshold { get; set; }

        public EncoderEntry()
        {
            ID = 0;
            Name = string.Empty;
            StationName = string.Empty;
            Host = string.Empty;
            ServerUrl = string.Empty;
            Port = 8000;
            ServerPort = 8000;
            Username = string.Empty;
            Password = string.Empty;
            MountPoint = string.Empty;
            Format = "MP3";
            Bitrate = 128;
            SampleRate = 44100;
            AutoConnect = 0;
            IsActive = 0;
            AudioSourceDevice = string.Empty;
            EnableAGC = false;
            AGCTargetLevel = 0.2f;
            AGCAttackTime = 0.5f;
            AGCReleaseTime = 3.0f;
            LimiterThreshold = 0.95f;
        }
    }
}