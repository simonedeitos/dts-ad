using AirDirector.Models;
using AirDirector.Services.Database;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AirDirector.Services.Database
{
    public class EncoderConfigManager
    {
        private const string REGISTRY_KEY = @"SOFTWARE\AirDirector\Encoders";


        private static Recorder LoadRecorderFromRegistry(int id, RegistryKey key)
        {
            try
            {
                int typeValue = Convert.ToInt32(key.GetValue("Type", 1));

                Console.WriteLine($"[RecorderConfigManager] Carico ID={id}, Type INT={typeValue}");

                Recorder recorder = new Recorder
                {
                    ID = id,
                    Name = key.GetValue("Name", "").ToString(),
                    Type = (Recorder.RecorderType)typeValue,
                    AudioSourceDevice = key.GetValue("AudioSourceDevice", "").ToString(),
                    OutputPath = key.GetValue("OutputPath", @"C:\AirDirector\Recordings").ToString(),
                    Format = (Recorder.AudioFormat)Convert.ToInt32(key.GetValue("Format", 3)),

                    Monday = Convert.ToBoolean(key.GetValue("Monday", 0)),
                    Tuesday = Convert.ToBoolean(key.GetValue("Tuesday", 0)),
                    Wednesday = Convert.ToBoolean(key.GetValue("Wednesday", 0)),
                    Thursday = Convert.ToBoolean(key.GetValue("Thursday", 0)),
                    Friday = Convert.ToBoolean(key.GetValue("Friday", 0)),
                    Saturday = Convert.ToBoolean(key.GetValue("Saturday", 0)),
                    Sunday = Convert.ToBoolean(key.GetValue("Sunday", 0)),

                    RetentionDays = Convert.ToInt32(key.GetValue("RetentionDays", 90)),
                    AutoDeleteOldFiles = Convert.ToBoolean(key.GetValue("AutoDeleteOldFiles", 1))
                };

                if (TimeSpan.TryParse(key.GetValue("StartTime", "00:00:00").ToString(), out TimeSpan startTime))
                    recorder.StartTime = startTime;

                if (TimeSpan.TryParse(key.GetValue("EndTime", "23:59:59").ToString(), out TimeSpan endTime))
                    recorder.EndTime = endTime;

                Console.WriteLine($"[RecorderConfigManager] ✅ '{recorder.Name}' Type={recorder.Type}");

                return recorder;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RecorderConfigManager] ❌ Errore ID {id}: {ex.Message}");
                return null;
            }
        }

        public static List<EncoderEntry> LoadEncoders()
        {
            var encoders = new List<EncoderEntry>();

            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY))
                {
                    if (key != null)
                    {
                        foreach (string subKeyName in key.GetSubKeyNames())
                        {
                            using (RegistryKey encoderKey = key.OpenSubKey(subKeyName))
                            {
                                if (encoderKey != null)
                                {
                                    var encoder = new EncoderEntry
                                    {
                                        ID = int.Parse(subKeyName),
                                        Name = encoderKey.GetValue("Name", "")?.ToString(),
                                        StationName = encoderKey.GetValue("StationName", "")?.ToString(),
                                        Host = encoderKey.GetValue("Host", "")?.ToString(),
                                        ServerUrl = encoderKey.GetValue("ServerUrl", "")?.ToString(),
                                        Port = Convert.ToInt32(encoderKey.GetValue("Port", 8000)),
                                        ServerPort = Convert.ToInt32(encoderKey.GetValue("ServerPort", 8000)),
                                        Username = encoderKey.GetValue("Username", "")?.ToString(),
                                        Password = encoderKey.GetValue("Password", "")?.ToString(),
                                        MountPoint = encoderKey.GetValue("MountPoint", "")?.ToString(),
                                        Format = encoderKey.GetValue("Format", "MP3")?.ToString(),
                                        Bitrate = Convert.ToInt32(encoderKey.GetValue("Bitrate", 128)),
                                        AudioSourceDevice = encoderKey.GetValue("AudioSourceDevice", "")?.ToString(),
                                        EnableAGC = Convert.ToBoolean(encoderKey.GetValue("EnableAGC", false)),
                                        AGCTargetLevel = Convert.ToSingle(encoderKey.GetValue("AGCTargetLevel", 0.2f)),
                                        AGCAttackTime = Convert.ToSingle(encoderKey.GetValue("AGCAttackTime", 0.5f)),
                                        AGCReleaseTime = Convert.ToSingle(encoderKey.GetValue("AGCReleaseTime", 3.0f)),
                                        LimiterThreshold = Convert.ToSingle(encoderKey.GetValue("LimiterThreshold", 0.95f))
                                    };

                                    encoders.Add(encoder);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore caricamento encoder da registry: {ex.Message}");
            }

            return encoders;
        }

        public static void SaveEncoder(EncoderEntry encoder)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey($"{REGISTRY_KEY}\\{encoder.ID}"))
                {
                    if (key != null)
                    {
                        key.SetValue("Name", encoder.Name ?? "");
                        key.SetValue("StationName", encoder.StationName ?? "");
                        key.SetValue("Host", encoder.Host ?? "");
                        key.SetValue("ServerUrl", encoder.ServerUrl ?? "");
                        key.SetValue("Port", encoder.Port);
                        key.SetValue("ServerPort", encoder.ServerPort);
                        key.SetValue("Username", encoder.Username ?? "");
                        key.SetValue("Password", encoder.Password ?? "");
                        key.SetValue("MountPoint", encoder.MountPoint ?? "");
                        key.SetValue("Format", encoder.Format ?? "MP3");
                        key.SetValue("Bitrate", encoder.Bitrate);
                        key.SetValue("AudioSourceDevice", encoder.AudioSourceDevice ?? "");
                        key.SetValue("EnableAGC", encoder.EnableAGC ? 1 : 0);
                        key.SetValue("AGCTargetLevel", encoder.AGCTargetLevel.ToString());
                        key.SetValue("AGCAttackTime", encoder.AGCAttackTime.ToString());
                        key.SetValue("AGCReleaseTime", encoder.AGCReleaseTime.ToString());
                        key.SetValue("LimiterThreshold", encoder.LimiterThreshold.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore salvataggio encoder su registry: {ex.Message}");
            }
        }

        public static void DeleteEncoder(int id)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY, true))
                {
                    if (key != null)
                    {
                        key.DeleteSubKeyTree(id.ToString(), false);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore eliminazione encoder da registry: {ex.Message}");
            }
        }

        public static int GetNextEncoderId()
        {
            var encoders = LoadEncoders();
            if (encoders.Count == 0)
                return 1;

            return encoders.Max(e => e.ID) + 1;
        }

        public static bool GetAutoStartEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\AirDirector\\Encoders"))
                {
                    if (key != null)
                    {
                        return Convert.ToBoolean(key.GetValue("AutoStartEncoders", 0));
                    }
                }
            }
            catch { }

            return false;
        }

        public static void SetAutoStartEnabled(bool enabled)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\AirDirector\\Encoders"))
                {
                    if (key != null)
                    {
                        key.SetValue("AutoStartEncoders", enabled ? 1 : 0);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore salvataggio auto-start: {ex.Message}");
            }
        }
    }
}