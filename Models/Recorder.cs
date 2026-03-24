using System;
using System.Collections.Generic;
using AirDirector.Services.Localization;

namespace AirDirector.Models
{
    /// <summary>
    /// Registratore audio con schedulazione
    /// </summary>
    public class Recorder
    {
        public enum RecorderType
        {
            NinetyDays,     // Registrazione continua 90 giorni (file orari)
            Manual,         // Registrazione manuale on-demand
            Scheduled       // Registrazione schedulata
        }

        public enum AudioFormat
        {
            MP3_64_Mono,
            MP3_64_Stereo,
            MP3_128_Mono,
            MP3_128_Stereo,
            MP3_256_Mono,
            MP3_256_Stereo,
            MP3_320_Mono,
            MP3_320_Stereo
        }

        public int ID { get; set; }
        public string Name { get; set; }
        public RecorderType Type { get; set; }
        public string AudioSourceDevice { get; set; }
        public string OutputPath { get; set; }
        public AudioFormat Format { get; set; }

        // Schedulazione (solo per tipo Scheduled)
        public bool Monday { get; set; }
        public bool Tuesday { get; set; }
        public bool Wednesday { get; set; }
        public bool Thursday { get; set; }
        public bool Friday { get; set; }
        public bool Saturday { get; set; }
        public bool Sunday { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        // 90 Days settings
        public int RetentionDays { get; set; }  // Giorni di conservazione (default 90)
        public bool AutoDeleteOldFiles { get; set; }

        // Status
        public bool IsActive { get; set; }
        public bool IsRecording { get; set; }
        public DateTime RecordingStartTime { get; set; }
        public string CurrentFileName { get; set; }
        public long CurrentFileSize { get; set; }  // Bytes
        public string StatusText { get; set; }

        public Recorder()
        {
            ID = 0;
            Name = string.Empty;
            Type = RecorderType.Manual;
            AudioSourceDevice = string.Empty;
            OutputPath = @"C:\AirDirector\Recordings";
            Format = AudioFormat.MP3_128_Stereo;

            Monday = false;
            Tuesday = false;
            Wednesday = false;
            Thursday = false;
            Friday = false;
            Saturday = false;
            Sunday = false;
            StartTime = new TimeSpan(0, 0, 0);
            EndTime = new TimeSpan(23, 59, 59);

            RetentionDays = 90;
            AutoDeleteOldFiles = true;

            IsActive = false;
            IsRecording = false;
            RecordingStartTime = DateTime.MinValue;
            CurrentFileName = string.Empty;
            CurrentFileSize = 0;
            StatusText = "Inattivo";  // ⚠️ Questo resta hardcoded, viene tradotto in UI
        }

        /// <summary>
        /// Verifica se è attivo in un determinato giorno
        /// </summary>
        public bool IsActiveDayOfWeek(DayOfWeek dayOfWeek)
        {
            switch (dayOfWeek)
            {
                case DayOfWeek.Monday: return Monday;
                case DayOfWeek.Tuesday: return Tuesday;
                case DayOfWeek.Wednesday: return Wednesday;
                case DayOfWeek.Thursday: return Thursday;
                case DayOfWeek.Friday: return Friday;
                case DayOfWeek.Saturday: return Saturday;
                case DayOfWeek.Sunday: return Sunday;
                default: return false;
            }
        }

        /// <summary>
        /// Verifica se deve registrare in questo momento
        /// </summary>
        public bool ShouldRecordNow(DateTime checkTime)
        {
            if (Type == RecorderType.NinetyDays)
                return IsActive;  // Sempre attivo se abilitato

            if (Type == RecorderType.Manual)
                return false;  // Solo manuale

            if (Type == RecorderType.Scheduled)
            {
                if (!IsActive)
                    return false;

                if (!IsActiveDayOfWeek(checkTime.DayOfWeek))
                    return false;

                TimeSpan currentTime = checkTime.TimeOfDay;
                return currentTime >= StartTime && currentTime <= EndTime;
            }

            return false;
        }

        /// <summary>
        /// Ottiene bitrate dal formato
        /// </summary>
        public int GetBitrate()
        {
            switch (Format)
            {
                case AudioFormat.MP3_64_Mono:
                case AudioFormat.MP3_64_Stereo:
                    return 64;
                case AudioFormat.MP3_128_Mono:
                case AudioFormat.MP3_128_Stereo:
                    return 128;
                case AudioFormat.MP3_256_Mono:
                case AudioFormat.MP3_256_Stereo:
                    return 256;
                case AudioFormat.MP3_320_Mono:
                case AudioFormat.MP3_320_Stereo:
                    return 320;
                default:
                    return 128;
            }
        }

        /// <summary>
        /// Verifica se è stereo
        /// </summary>
        public bool IsStereo()
        {
            return Format == AudioFormat.MP3_64_Stereo ||
                   Format == AudioFormat.MP3_128_Stereo ||
                   Format == AudioFormat.MP3_256_Stereo ||
                   Format == AudioFormat.MP3_320_Stereo;
        }

        /// <summary>
        /// Ottiene numero di canali
        /// </summary>
        public int GetChannels()
        {
            return IsStereo() ? 2 : 1;
        }

        /// <summary>
        /// Ottiene stringa descrittiva del formato (TRADOTTA)
        /// </summary>
        public string GetFormatString()
        {
            int bitrate = GetBitrate();
            string channels = IsStereo() ?
                LanguageManager.GetString("Recorder.Stereo", "Stereo") :
                LanguageManager.GetString("Recorder.Mono", "Mono");
            return $"MP3 {bitrate}kbps {channels}";
        }

        /// <summary>
        /// Genera nome file per registrazione
        /// </summary>
        public string GenerateFileName(DateTime recordTime)
        {
            switch (Type)
            {
                case RecorderType.NinetyDays:
                    return $"90days_{recordTime: yyyyMMdd_HH00}.mp3";

                case RecorderType.Manual:
                    return $"Manual_{recordTime: yyyyMMdd_HHmmss}.mp3";

                case RecorderType.Scheduled:
                    string safeName = string.Join("_", Name.Split(System.IO.Path.GetInvalidFileNameChars()));
                    return $"{safeName}_{recordTime: yyyyMMdd_HHmmss}.mp3";

                default:
                    return $"Recording_{recordTime:yyyyMMdd_HHmmss}.mp3";
            }
        }

        /// <summary>
        /// Ottiene dimensione file formattata
        /// </summary>
        public string GetFormattedFileSize()
        {
            if (CurrentFileSize < 1024)
                return $"{CurrentFileSize} B";
            else if (CurrentFileSize < 1024 * 1024)
                return $"{CurrentFileSize / 1024.0:F2} KB";
            else if (CurrentFileSize < 1024 * 1024 * 1024)
                return $"{CurrentFileSize / (1024.0 * 1024.0):F2} MB";
            else
                return $"{CurrentFileSize / (1024.0 * 1024.0 * 1024.0):F2} GB";
        }

        /// <summary>
        /// Ottiene durata registrazione corrente
        /// </summary>
        public TimeSpan GetRecordingDuration()
        {
            if (!IsRecording || RecordingStartTime == DateTime.MinValue)
                return TimeSpan.Zero;

            return DateTime.Now - RecordingStartTime;
        }

        /// <summary>
        /// Ottiene descrizione schedulazione (TRADOTTA)
        /// </summary>
        public string GetScheduleDescription()
        {
            if (Type == RecorderType.NinetyDays)
                return LanguageManager.GetString("Recorder.Continuous", "Continuo (24/7)");

            if (Type == RecorderType.Manual)
                return LanguageManager.GetString("Recorder.Manual", "Manuale");

            if (Type == RecorderType.Scheduled)
            {
                List<string> days = new List<string>();
                if (Monday) days.Add(LanguageManager.GetString("Download.DayMon", "Lun"));
                if (Tuesday) days.Add(LanguageManager.GetString("Download.DayTue", "Mar"));
                if (Wednesday) days.Add(LanguageManager.GetString("Download.DayWed", "Mer"));
                if (Thursday) days.Add(LanguageManager.GetString("Download.DayThu", "Gio"));
                if (Friday) days.Add(LanguageManager.GetString("Download.DayFri", "Ven"));
                if (Saturday) days.Add(LanguageManager.GetString("Download.DaySat", "Sab"));
                if (Sunday) days.Add(LanguageManager.GetString("Download.DaySun", "Dom"));

                string daysStr = days.Count == 7 ?
                    LanguageManager.GetString("Download.EveryDay", "Tutti i giorni") :
                    string.Join(", ", days);
                return $"{daysStr} {StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
            }

            return LanguageManager.GetString("Recorder.NotConfigured", "Non configurato");
        }

        public override string ToString()
        {
            return $"{Name} ({GetFormatString()})";
        }
    }
}