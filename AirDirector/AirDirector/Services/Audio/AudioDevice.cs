using System.Collections.Generic;

namespace AirDirector.Services.Audio
{
    public class AudioDevice
    {
        public int DeviceId { get; set; }
        public string DeviceName { get; set; }
        public int Channels { get; set; }
        public int SampleRate { get; set; }

        public AudioDevice()
        {
            DeviceId = -1;
            DeviceName = "Default";
            Channels = 2;
            SampleRate = 44100;
        }

        public override string ToString()
        {
            return DeviceName;
        }

        public static List<AudioDevice> GetAvailableDevices()
        {
            var devices = new List<AudioDevice>();

            // TODO: Implementare con NAudio/BASS
            // Per ora restituisce solo il device di default
            devices.Add(new AudioDevice
            {
                DeviceId = -1,
                DeviceName = "Default Audio Device",
                Channels = 2,
                SampleRate = 44100
            });

            return devices;
        }
    }
}