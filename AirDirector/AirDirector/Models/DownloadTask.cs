using System;
using System.Collections.Generic;

namespace AirDirector.Models
{
    public class DownloadTask
    {
        public string Name { get; set; }
        public bool IsHttpDownload { get; set; }

        // HTTP Settings
        public string HttpUrl { get; set; }
        public string HttpUsername { get; set; }
        public string HttpPassword { get; set; }

        // FTP Settings
        public string FtpHost { get; set; }
        public string FtpFilePath { get; set; }
        public string FtpUsername { get; set; }
        public string FtpPassword { get; set; }

        // Local Settings
        public string LocalFilePath { get; set; }

        // Schedule Settings
        public bool Monday { get; set; }
        public bool Tuesday { get; set; }
        public bool Wednesday { get; set; }
        public bool Thursday { get; set; }
        public bool Friday { get; set; }
        public bool Saturday { get; set; }
        public bool Sunday { get; set; }
        public List<string> ScheduleTimes { get; set; }

        // Composition Settings
        public bool CompositionEnabled { get; set; }
        public bool UseOpener { get; set; }
        public string OpenerFilePath { get; set; }
        public string MainFilePath { get; set; }
        public bool UseBackground { get; set; }
        public string BackgroundFilePath { get; set; }
        public int BackgroundVolume { get; set; }
        public bool UseCloser { get; set; }
        public string CloserFilePath { get; set; }
        public string OutputFilePath { get; set; }
        public bool BoostVolume { get; set; }

        public DownloadTask()
        {
            Name = "";
            IsHttpDownload = true;
            HttpUrl = "";
            HttpUsername = "";
            HttpPassword = "";
            FtpHost = "";
            FtpFilePath = "";
            FtpUsername = "";
            FtpPassword = "";
            LocalFilePath = "";
            Monday = false;
            Tuesday = false;
            Wednesday = false;
            Thursday = false;
            Friday = false;
            Saturday = false;
            Sunday = false;
            ScheduleTimes = new List<string>();
            CompositionEnabled = false;
            UseOpener = false;
            OpenerFilePath = "";
            MainFilePath = "";
            UseBackground = false;
            BackgroundFilePath = "";
            BackgroundVolume = 30;
            UseCloser = false;
            CloserFilePath = "";
            OutputFilePath = "";
            BoostVolume = false;
        }

        public void CopyFrom(DownloadTask source)
        {
            Name = source.Name;
            IsHttpDownload = source.IsHttpDownload;
            HttpUrl = source.HttpUrl;
            HttpUsername = source.HttpUsername;
            HttpPassword = source.HttpPassword;
            FtpHost = source.FtpHost;
            FtpFilePath = source.FtpFilePath;
            FtpUsername = source.FtpUsername;
            FtpPassword = source.FtpPassword;
            LocalFilePath = source.LocalFilePath;
            Monday = source.Monday;
            Tuesday = source.Tuesday;
            Wednesday = source.Wednesday;
            Thursday = source.Thursday;
            Friday = source.Friday;
            Saturday = source.Saturday;
            Sunday = source.Sunday;
            ScheduleTimes = new List<string>(source.ScheduleTimes);
            CompositionEnabled = source.CompositionEnabled;
            UseOpener = source.UseOpener;
            OpenerFilePath = source.OpenerFilePath;
            MainFilePath = source.MainFilePath;
            UseBackground = source.UseBackground;
            BackgroundFilePath = source.BackgroundFilePath;
            BackgroundVolume = source.BackgroundVolume;
            UseCloser = source.UseCloser;
            CloserFilePath = source.CloserFilePath;
            OutputFilePath = source.OutputFilePath;
            BoostVolume = source.BoostVolume;
        }

        // Converte il task in formato CSV per Downloader.dbc
        public string ToCsvLine()
        {
            string days = $"{(Monday ? "1" : "0")},{(Tuesday ? "1" : "0")},{(Wednesday ? "1" : "0")}," +
                         $"{(Thursday ? "1" : "0")},{(Friday ? "1" : "0")},{(Saturday ? "1" : "0")},{(Sunday ? "1" : "0")}";

            string times = string.Join("|", ScheduleTimes);

            return $"{EscapeCsv(Name)},{(IsHttpDownload ? "HTTP" : "FTP")}," +
                   $"{EscapeCsv(HttpUrl)},{EscapeCsv(HttpUsername)},{EscapeCsv(HttpPassword)}," +
                   $"{EscapeCsv(FtpHost)},{EscapeCsv(FtpFilePath)},{EscapeCsv(FtpUsername)},{EscapeCsv(FtpPassword)}," +
                   $"{EscapeCsv(LocalFilePath)},{days},{EscapeCsv(times)}," +
                   $"{(CompositionEnabled ? "1" : "0")},{(UseOpener ? "1" : "0")},{EscapeCsv(OpenerFilePath)}," +
                   $"{EscapeCsv(MainFilePath)},{(UseBackground ? "1" : "0")},{EscapeCsv(BackgroundFilePath)}," +
                   $"{BackgroundVolume},{(UseCloser ? "1" : "0")},{EscapeCsv(CloserFilePath)}," +
                   $"{EscapeCsv(OutputFilePath)},{(BoostVolume ? "1" : "0")}";
        }

        // Crea un task da una riga CSV
        public static DownloadTask FromCsvLine(string line)
        {
            var parts = ParseCsvLine(line);
            if (parts.Length < 28) return null;

            var task = new DownloadTask
            {
                Name = parts[0],
                IsHttpDownload = parts[1] == "HTTP",
                HttpUrl = parts[2],
                HttpUsername = parts[3],
                HttpPassword = parts[4],
                FtpHost = parts[5],
                FtpFilePath = parts[6],
                FtpUsername = parts[7],
                FtpPassword = parts[8],
                LocalFilePath = parts[9],
                Monday = parts[10] == "1",
                Tuesday = parts[11] == "1",
                Wednesday = parts[12] == "1",
                Thursday = parts[13] == "1",
                Friday = parts[14] == "1",
                Saturday = parts[15] == "1",
                Sunday = parts[16] == "1",
                ScheduleTimes = new List<string>(parts[17].Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)),
                CompositionEnabled = parts[18] == "1",
                UseOpener = parts[19] == "1",
                OpenerFilePath = parts[20],
                MainFilePath = parts[21],
                UseBackground = parts[22] == "1",
                BackgroundFilePath = parts[23],
                BackgroundVolume = int.TryParse(parts[24], out int vol) ? vol : 30,
                UseCloser = parts[25] == "1",
                CloserFilePath = parts[26],
                OutputFilePath = parts[27],
                BoostVolume = parts.Length > 28 && parts[28] == "1"
            };

            return task;
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }

        private static string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var currentValue = new System.Text.StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        currentValue.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentValue.ToString());
                    currentValue.Clear();
                }
                else
                {
                    currentValue.Append(c);
                }
            }

            result.Add(currentValue.ToString());
            return result.ToArray();
        }
    }
}