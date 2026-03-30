using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using AirDirector.Models;

namespace AirDirector.Services.Database
{
    public static class DownloaderManager
    {
        private static string DbPath
        {
            get
            {
                try
                {
                    // ✅ LEGGI IL PATH DAL REGISTRY
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AirDirector"))
                    {
                        if (key != null)
                        {
                            string dbPath = key.GetValue("DatabasePath") as string;
                            if (!string.IsNullOrEmpty(dbPath) && Directory.Exists(dbPath))
                            {
                                return Path.Combine(dbPath, "Downloader.dbc");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DownloaderManager] Errore lettura registry: {ex.Message}");
                }

                // ✅ FALLBACK: usa path locale se registry non disponibile
                string defaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database");
                if (!Directory.Exists(defaultPath))
                {
                    Directory.CreateDirectory(defaultPath);
                }
                return Path.Combine(defaultPath, "Downloader.dbc");
            }
        }

        static DownloaderManager()
        {
            EnsureDatabaseExists();
        }

        private static void EnsureDatabaseExists()
        {
            try
            {
                string dbDir = Path.GetDirectoryName(DbPath);
                if (!Directory.Exists(dbDir))
                {
                    Directory.CreateDirectory(dbDir);
                    Console.WriteLine($"[DownloaderManager] Creata directory: {dbDir}");
                }

                if (!File.Exists(DbPath))
                {
                    File.WriteAllText(DbPath, "Name,Type,HttpUrl,HttpUsername,HttpPassword,FtpHost,FtpFilePath,FtpUsername,FtpPassword," +
                        "LocalFilePath,Monday,Tuesday,Wednesday,Thursday,Friday,Saturday,Sunday,ScheduleTimes," +
                        "CompositionEnabled,UseOpener,OpenerFilePath,MainFilePath,UseBackground,BackgroundFilePath," +
                        "BackgroundVolume,UseCloser,CloserFilePath,OutputFilePath,BoostVolume\n");
                    Console.WriteLine($"[DownloaderManager] Creato database: {DbPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DownloaderManager] Errore inizializzazione database: {ex.Message}");
            }
        }

        public static List<DownloadTask> LoadTasks()
        {
            var tasks = new List<DownloadTask>();

            try
            {
                if (!File.Exists(DbPath))
                {
                    Console.WriteLine($"[DownloaderManager] File database non trovato: {DbPath}");
                    return tasks;
                }

                string[] lines = File.ReadAllLines(DbPath);

                for (int i = 1; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i]))
                        continue;

                    var task = DownloadTask.FromCsvLine(lines[i]);
                    if (task != null)
                    {
                        tasks.Add(task);
                    }
                }

                Console.WriteLine($"[DownloaderManager] Caricati {tasks.Count} task da: {DbPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DownloaderManager] Errore caricamento: {ex.Message}");
            }

            return tasks;
        }

        public static void SaveTasks(List<DownloadTask> tasks)
        {
            try
            {
                EnsureDatabaseExists();

                using (StreamWriter writer = new StreamWriter(DbPath, false))
                {
                    writer.WriteLine("Name,Type,HttpUrl,HttpUsername,HttpPassword,FtpHost,FtpFilePath,FtpUsername,FtpPassword," +
                        "LocalFilePath,Monday,Tuesday,Wednesday,Thursday,Friday,Saturday,Sunday,ScheduleTimes," +
                        "CompositionEnabled,UseOpener,OpenerFilePath,MainFilePath,UseBackground,BackgroundFilePath," +
                        "BackgroundVolume,UseCloser,CloserFilePath,OutputFilePath,BoostVolume");

                    foreach (var task in tasks)
                    {
                        writer.WriteLine(task.ToCsvLine());
                    }
                }

                Console.WriteLine($"[DownloaderManager] Salvati {tasks.Count} task in: {DbPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DownloaderManager] Errore salvataggio: {ex.Message}");
                throw;
            }
        }

        public static void AddTask(DownloadTask task)
        {
            var tasks = LoadTasks();
            tasks.Add(task);
            SaveTasks(tasks);
        }

        public static void UpdateTask(string originalName, DownloadTask updatedTask)
        {
            var tasks = LoadTasks();
            var index = tasks.FindIndex(t => t.Name == originalName);
            if (index >= 0)
            {
                tasks[index] = updatedTask;
                SaveTasks(tasks);
            }
        }

        public static void DeleteTask(string taskName)
        {
            var tasks = LoadTasks();
            tasks.RemoveAll(t => t.Name == taskName);
            SaveTasks(tasks);
        }

        // ✅ METODO PER OTTENERE IL PATH DEL DATABASE (PER DEBUG)
        public static string GetDatabasePath()
        {
            return DbPath;
        }
    }
}