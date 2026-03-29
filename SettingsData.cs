using System;
using System.IO;

namespace BluShopModManager
{
    public class SettingsData
    {
        public bool AutoCheckUpdates { get; set; } = true;
        public bool ConfirmDownloads { get; set; } = true;
        public string GorillaTagPath { get; set; } = "";
        public string InstalledModsDates { get; set; } = "";

        public string GetGTFolder()
        {
            if (string.IsNullOrWhiteSpace(GorillaTagPath))
                return string.Empty;

            if (GorillaTagPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                return Path.GetDirectoryName(GorillaTagPath) ?? string.Empty;

            return GorillaTagPath;
        }

        public bool IsGTExeValid()
        {
            if (string.IsNullOrWhiteSpace(GorillaTagPath))
                return false;

            if (!GorillaTagPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                return false;

            return File.Exists(GorillaTagPath) &&
                   Path.GetFileName(GorillaTagPath).Equals("Gorilla Tag.exe", StringComparison.OrdinalIgnoreCase);
        }

        public string GetGTExePath()
        {
            if (string.IsNullOrWhiteSpace(GorillaTagPath))
                return string.Empty;

            if (GorillaTagPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                return GorillaTagPath;

            string exePath = Path.Combine(GorillaTagPath, "Gorilla Tag.exe");
            return File.Exists(exePath) ? exePath : string.Empty;
        }

        public bool NormalizeGTPath()
        {
            if (string.IsNullOrWhiteSpace(GorillaTagPath))
                return false;

            if (!GorillaTagPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                string exePath = Path.Combine(GorillaTagPath, "Gorilla Tag.exe");
                if (File.Exists(exePath))
                {
                    GorillaTagPath = exePath;
                    return true;
                }
                return false;
            }

            return File.Exists(GorillaTagPath) &&
                   Path.GetFileName(GorillaTagPath).Equals("Gorilla Tag.exe", StringComparison.OrdinalIgnoreCase);
        }
    }
}