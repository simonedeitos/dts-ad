namespace AirDirector.Models
{
    /// <summary>
    /// Helper per gestione marker audio
    /// </summary>
    public class Markers
    {
        public int IN { get; set; }      // Punto di inizio (ms)
        public int INTRO { get; set; }   // Punto intro per countdown (ms)
        public int MIX { get; set; }     // Punto mix con brano successivo (ms)

        public Markers()
        {
            IN = 0;
            INTRO = 0;
            MIX = 0;
        }

        public Markers(int markerIn, int intro, int mix)
        {
            IN = markerIn;
            INTRO = intro;
            MIX = mix;
        }

        /// <summary>
        /// Calcola automaticamente il punto MIX basato sulla durata
        /// </summary>
        public static int CalculateAutoMix(int durationMs, int mixDurationMs)
        {
            // Il punto mix è posizionato a "durata - mixDuration"
            int calculatedMix = durationMs - mixDurationMs;
            return calculatedMix > 0 ? calculatedMix : 0;
        }

        /// <summary>
        /// Formatta un marker in formato MM:SS.mmm
        /// </summary>
        public static string FormatMarker(int milliseconds)
        {
            int totalSeconds = milliseconds / 1000;
            int ms = milliseconds % 1000;
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;

            return $"{minutes:D2}:{seconds:D2}.{ms:D3}";
        }

        /// <summary>
        /// Converte da formato MM:SS.mmm a millisecondi
        /// </summary>
        public static bool TryParseMarker(string markerString, out int milliseconds)
        {
            milliseconds = 0;

            if (string.IsNullOrEmpty(markerString))
                return false;

            try
            {
                // Format: MM:SS.mmm o MM:SS
                string[] parts = markerString.Split(':');
                if (parts.Length != 2)
                    return false;

                int minutes = int.Parse(parts[0]);

                string[] secondsParts = parts[1].Split('.');
                int seconds = int.Parse(secondsParts[0]);
                int ms = 0;

                if (secondsParts.Length > 1)
                    ms = int.Parse(secondsParts[1]);

                milliseconds = (minutes * 60 * 1000) + (seconds * 1000) + ms;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override string ToString()
        {
            return $"IN: {FormatMarker(IN)}, INTRO: {FormatMarker(INTRO)}, MIX: {FormatMarker(MIX)}";
        }
    }
}