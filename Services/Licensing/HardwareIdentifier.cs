using System;
using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace AirDirector.Services.Licensing
{
    /// <summary>
    /// Generatore di identificativo univoco hardware
    /// </summary>
    public static class HardwareIdentifier
    {
        /// <summary>
        /// Ottiene un ID univoco basato su hardware
        /// </summary>
        public static string GetMachineID()
        {
            try
            {
                string cpuID = GetCPUId();
                string motherboardID = GetMotherboardID();

                string combined = cpuID + motherboardID;

                // Genera hash SHA256
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
                    StringBuilder sb = new StringBuilder();
                    foreach (byte b in hashBytes)
                    {
                        sb.Append(b.ToString("X2"));
                    }
                    return sb.ToString().Substring(0, 32); // Primi 32 caratteri
                }
            }
            catch (Exception ex)
            {
                // Fallback: usa nome computer
                Console.WriteLine($"Errore generazione Machine ID: {ex.Message}");
                return GetFallbackMachineID();
            }
        }

        /// <summary>
        /// Ottiene CPU ID tramite WMI
        /// </summary>
        private static string GetCPUId()
        {
            try
            {
                string cpuInfo = string.Empty;
                ManagementClass mc = new ManagementClass("Win32_Processor");
                ManagementObjectCollection moc = mc.GetInstances();

                foreach (ManagementObject mo in moc)
                {
                    cpuInfo = mo.Properties["ProcessorId"].Value.ToString();
                    break;
                }

                return cpuInfo;
            }
            catch
            {
                return "CPU_UNKNOWN";
            }
        }

        /// <summary>
        /// Ottiene Motherboard Serial tramite WMI
        /// </summary>
        private static string GetMotherboardID()
        {
            try
            {
                string mbInfo = string.Empty;
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard");

                foreach (ManagementObject mo in searcher.Get())
                {
                    mbInfo = mo["SerialNumber"].ToString();
                    break;
                }

                return mbInfo;
            }
            catch
            {
                return "MB_UNKNOWN";
            }
        }

        /// <summary>
        /// ID di fallback basato su nome computer
        /// </summary>
        private static string GetFallbackMachineID()
        {
            try
            {
                string computerName = Environment.MachineName;
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(computerName));
                    StringBuilder sb = new StringBuilder();
                    foreach (byte b in hashBytes)
                    {
                        sb.Append(b.ToString("X2"));
                    }
                    return sb.ToString().Substring(0, 32);
                }
            }
            catch
            {
                return "FALLBACK_ID_00000000000000000000";
            }
        }
    }
}