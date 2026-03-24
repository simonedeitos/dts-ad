using System;
using System.IO;
using System.Text;
using AirDirector.Controls;

namespace AirDirector.Services
{
    public static class MetadataManager
    {
        public static void UpdateMetadata(string artist, string title, string itemType)
        {
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine($"[MetadataManager] ⚡ CHIAMATO!");
            Console.WriteLine($"[MetadataManager] Artist: '{artist}'");
            Console.WriteLine($"[MetadataManager] Title: '{title}'");
            Console.WriteLine($"[MetadataManager] Type: '{itemType}'");

            try
            {
                string metadataSource = ConfigurationControl.GetMetadataSource();
                Console.WriteLine($"[MetadataManager] Filtro configurato: {metadataSource}");

                bool shouldUpdate = false;

                if (metadataSource == "MusicOnly" && itemType == "Music")
                {
                    shouldUpdate = true;
                }
                else if (metadataSource == "MusicAndClips" && (itemType == "Music" || itemType == "Clip"))
                {
                    shouldUpdate = true;
                }

                Console.WriteLine($"[MetadataManager] Should update: {shouldUpdate}");

                if (!shouldUpdate)
                {
                    Console.WriteLine($"[MetadataManager] ❌ Tipo '{itemType}' IGNORATO (filtro: {metadataSource})");
                    Console.WriteLine("═══════════════════════════════════════");
                    return;
                }

                string metadata = string.IsNullOrEmpty(artist)
                    ? title
                    : $"{artist} - {title}";

                Console.WriteLine($"[MetadataManager] ✅ Metadata finale: '{metadata}'");

                // SALVA FILE RDS
                bool saveRds = ConfigurationControl.IsSaveRdsEnabled();
                Console.WriteLine($"[MetadataManager] Salva RDS: {saveRds}");

                if (saveRds)
                {
                    SaveRdsFile(metadata);
                }

                // INVIA AGLI ENCODER
                bool sendToEncoders = ConfigurationControl.IsSendMetadataToEncodersEnabled();
                Console.WriteLine($"[MetadataManager] Invia agli encoder: {sendToEncoders}");

                if (sendToEncoders)
                {
                    SendToEncoders(artist, title);
                }

                Console.WriteLine("═══════════════════════════════════════");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MetadataManager] ❌ ERRORE: {ex.Message}");
                Console.WriteLine($"[MetadataManager] Stack: {ex.StackTrace}");
                Console.WriteLine("═══════════════════════════════════════");
            }
        }

        private static void SaveRdsFile(string metadata)
        {
            try
            {
                string filePath = ConfigurationControl.GetRdsFilePath();
                Console.WriteLine($"[MetadataManager] Path RDS: '{filePath}'");

                if (string.IsNullOrEmpty(filePath))
                {
                    Console.WriteLine($"[MetadataManager] ⚠️ Path RDS vuoto!");
                    return;
                }

                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Console.WriteLine($"[MetadataManager] Directory creata: {directory}");
                }

                File.WriteAllText(filePath, metadata, Encoding.UTF8);
                Console.WriteLine($"[MetadataManager] ✅ File RDS salvato!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MetadataManager] ❌ Errore RDS: {ex.Message}");
            }
        }

        private static void SendToEncoders(string artist, string title)
        {
            try
            {
                Console.WriteLine($"[MetadataManager] 📡 Invio via Dispatcher...");
                Console.WriteLine($"[MetadataManager] Artist: '{artist}', Title: '{title}'");

                MetadataDispatcher.RaiseMetadataUpdate(artist, title);

                Console.WriteLine($"[MetadataManager] ✅ Dispatcher chiamato!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MetadataManager] ❌ Errore dispatcher: {ex.Message}");
                Console.WriteLine($"[MetadataManager] Stack: {ex.StackTrace}");
            }
        }
    }
}