namespace AirDirector.Services.Licensing
{
    /// <summary>
    /// Limiti della modalità demo
    /// </summary>
    public static class DemoLimits
    {
        // Limiti archivio
        public const int MAX_MUSIC_TRACKS = 50;
        public const int MAX_CLIPS = 15;

        // Limiti streaming
        public const int MAX_ENCODERS = 1;
        public const int MAX_CLOCKS = 2;
        public const int MAX_SCHEDULES = 2;
        public const int MAX_DOWNLOADER_SCHEDULES = 1;

        // Messaggi
        public const string MSG_MUSIC_LIMIT = "Hai raggiunto il limite di 50 brani musicali in modalità demo.\n\nAttiva una licenza per rimuovere questo limite.";
        public const string MSG_CLIPS_LIMIT = "Hai raggiunto il limite di 15 clips in modalità demo.\n\nAttiva una licenza per rimuovere questo limite.";
        public const string MSG_ENCODER_LIMIT = "Hai raggiunto il limite di 1 encoder in modalità demo.\n\nAttiva una licenza per creare encoder multipli.";

        /// <summary>
        /// Verifica se si può aggiungere un brano musicale
        /// </summary>
        public static bool CanAddMusicTrack(int currentCount, bool isLicensed)
        {
            if (isLicensed) return true;
            return currentCount < MAX_MUSIC_TRACKS;
        }

        // Aggiungi dopo MAX_ENCODERS:
        public const int MAX_RECORDERS = 1;

        // Aggiungi dopo MSG_ENCODER_LIMIT:
        public const string MSG_RECORDER_LIMIT = "Hai raggiunto il limite di 1 recorder in modalità demo.\n\nAttiva una licenza per creare recorder multipli.";

        /// <summary>
        /// Verifica se si può aggiungere un recorder
        /// </summary>
        public static bool CanAddRecorder(int currentCount, bool isLicensed)
        {
            if (isLicensed) return true;
            return currentCount < MAX_RECORDERS;
        }

        /// <summary>
        /// Verifica se si può aggiungere un clip
        /// </summary>
        public static bool CanAddClip(int currentCount, bool isLicensed)
        {
            if (isLicensed) return true;
            return currentCount < MAX_CLIPS;
        }

        /// <summary>
        /// Verifica se si può aggiungere un encoder
        /// </summary>
        public static bool CanAddEncoder(int currentCount, bool isLicensed)
        {
            if (isLicensed) return true;
            return currentCount < MAX_ENCODERS;
        }

        /// <summary>
        /// Verifica se si può aggiungere un clock
        /// </summary>
        public static bool CanAddClock(int currentCount, bool isLicensed)
        {
            if (isLicensed) return true;
            return currentCount < MAX_CLOCKS;
        }

        /// <summary>
        /// Verifica se si può aggiungere una schedulazione
        /// </summary>
        public static bool CanAddSchedule(int currentCount, bool isLicensed)
        {
            if (isLicensed) return true;
            return currentCount < MAX_SCHEDULES;
        }

        /// <summary>
        /// Verifica se si può aggiungere una schedulazione downloader
        /// </summary>
        public static bool CanAddDownloaderSchedule(int currentCount, bool isLicensed)
        {
            if (isLicensed) return true;
            return currentCount < MAX_DOWNLOADER_SCHEDULES;
        }
    }
}
