using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

namespace ModernFileCleaner
{
    public sealed class AppSettings
    {
        private static readonly Lazy<AppSettings> lazy = new Lazy<AppSettings>(() => new AppSettings());
        public static AppSettings Instance { get { return lazy.Value; } }

        public string Theme { get; set; } = "Dark";
        public bool AutoAnalyze { get; set; }
        public bool AutoClean { get; set; }
        public bool ShowNotifications { get; set; }
        public bool SafetyBackup { get; set; } = true;
        public bool RestorePointSkipped { get; set; }
        public DateTime LastCleaned { get; set; }
        public List<long> DiskHistory { get; set; } = new();

        private static readonly string SettingsPath = "settings.json";

        private AppSettings()
        {
            Theme = "Dark";
            AutoAnalyze = false;
            AutoClean = false;
            ShowNotifications = true;
            SafetyBackup = true;
            RestorePointSkipped = false;
            LastCleaned = DateTime.MinValue;
            DiskHistory = new List<long>();
        }

        public void Load()
        {
            if (File.Exists(SettingsPath))
            {
                try
                {
                    string json = File.ReadAllText(SettingsPath);
                    var settings = JsonConvert.DeserializeObject<AppSettings>(json);
                    if (settings != null)
                    {
                        Theme = settings.Theme;
                        AutoAnalyze = settings.AutoAnalyze;
                        AutoClean = settings.AutoClean;
                        ShowNotifications = settings.ShowNotifications;
                        SafetyBackup = settings.SafetyBackup;
                        RestorePointSkipped = settings.RestorePointSkipped;
                        LastCleaned = settings.LastCleaned;
                        DiskHistory = settings.DiskHistory ?? new List<long>();
                    }
                }
                catch (Exception ex) { Debug.WriteLine($"[AppSettings] {ex.Message}"); }
            }
        }

        public void Save()
        {
            try
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex) { Debug.WriteLine($"[AppSettings] {ex.Message}"); }
        }
    }
}
