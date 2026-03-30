using System;
using System.Collections.Generic;
using Microsoft.Win32;
using AirDirector.Models;

namespace AirDirector.Services.Database
{
    public static class RecorderConfigManager
    {
        private const string REGISTRY_KEY = @"SOFTWARE\AirDirector\Recorders";

        /// <summary>
        /// Carica tutti i recorder dal Registry
        /// </summary>
        public static List<Recorder> LoadRecorders()
        {
            List<Recorder> recorders = new List<Recorder>();

            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY))
                {
                    if (key == null)
                        return recorders;

                    foreach (string subKeyName in key.GetSubKeyNames())
                    {
                        if (int.TryParse(subKeyName, out int recorderId))
                        {
                            using (RegistryKey recorderKey = key.OpenSubKey(subKeyName))
                            {
                                if (recorderKey != null)
                                {
                                    Recorder recorder = LoadRecorderFromRegistry(recorderId, recorderKey);
                                    if (recorder != null)
                                        recorders.Add(recorder);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore caricamento recorders: {ex.Message}");
            }

            return recorders;
        }

        /// <summary>
        /// Salva recorder nel Registry
        /// </summary>
        public static void SaveRecorder(Recorder recorder)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey($"{REGISTRY_KEY}\\{recorder.ID}"))
                {
                    if (key != null)
                    {
                        key.SetValue("Name", recorder.Name ?? "");
                        key.SetValue("Type", (int)recorder.Type);
                        key.SetValue("AudioSourceDevice", recorder.AudioSourceDevice ?? "");
                        key.SetValue("OutputPath", recorder.OutputPath ?? "");
                        key.SetValue("Format", (int)recorder.Format);

                        // Schedulazione
                        key.SetValue("Monday", recorder.Monday ? 1 : 0);
                        key.SetValue("Tuesday", recorder.Tuesday ? 1 : 0);
                        key.SetValue("Wednesday", recorder.Wednesday ? 1 : 0);
                        key.SetValue("Thursday", recorder.Thursday ? 1 : 0);
                        key.SetValue("Friday", recorder.Friday ? 1 : 0);
                        key.SetValue("Saturday", recorder.Saturday ? 1 : 0);
                        key.SetValue("Sunday", recorder.Sunday ? 1 : 0);
                        key.SetValue("StartTime", recorder.StartTime.ToString());
                        key.SetValue("EndTime", recorder.EndTime.ToString());

                        // 90 Days settings
                        key.SetValue("RetentionDays", recorder.RetentionDays);
                        key.SetValue("AutoDeleteOldFiles", recorder.AutoDeleteOldFiles ? 1 : 0);

                        Console.WriteLine($"Recorder salvato: {recorder.Name} (ID: {recorder.ID})");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore salvataggio recorder: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Elimina recorder dal Registry
        /// </summary>
        public static void DeleteRecorder(int recorderId)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY, true))
                {
                    if (key != null)
                    {
                        key.DeleteSubKeyTree(recorderId.ToString(), false);
                        Console.WriteLine($"Recorder eliminato: ID {recorderId}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore eliminazione recorder: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Ottiene prossimo ID disponibile
        /// </summary>
        public static int GetNextRecorderId()
        {
            int maxId = 0;

            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY))
                {
                    if (key != null)
                    {
                        foreach (string subKeyName in key.GetSubKeyNames())
                        {
                            if (int.TryParse(subKeyName, out int id))
                            {
                                if (id > maxId)
                                    maxId = id;
                            }
                        }
                    }
                }
            }
            catch { }

            return maxId + 1;
        }

        /// <summary>
        /// Auto-start abilitato
        /// </summary>
        public static bool GetAutoStartEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY))
                {
                    if (key != null)
                    {
                        return Convert.ToBoolean(key.GetValue("AutoStartRecorders", 0));
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
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(REGISTRY_KEY))
                {
                    if (key != null)
                    {
                        key.SetValue("AutoStartRecorders", enabled ? 1 : 0);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore salvataggio auto-start: {ex.Message}");
            }
        }

        /// <summary>
        /// Carica recorder dal Registry
        /// </summary>
        private static Recorder LoadRecorderFromRegistry(int id, RegistryKey key)
        {
            try
            {
                Recorder recorder = new Recorder
                {
                    ID = id,
                    Name = key.GetValue("Name", "").ToString(),
                    Type = (Recorder.RecorderType)Convert.ToInt32(key.GetValue("Type", 1)),
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

                // Parse TimeSpan
                if (TimeSpan.TryParse(key.GetValue("StartTime", "00:00:00").ToString(), out TimeSpan startTime))
                    recorder.StartTime = startTime;

                if (TimeSpan.TryParse(key.GetValue("EndTime", "23:59:59").ToString(), out TimeSpan endTime))
                    recorder.EndTime = endTime;

                return recorder;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore caricamento recorder {id}: {ex.Message}");
                return null;
            }
        }
    }
}