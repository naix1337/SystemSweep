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

        public bool AutoAnalyze { get; set; }
        public bool AutoClean { get; set; }
        public bool ShowNotifications { get; set; }
        public bool SafetyBackup { get; set; } = true;
        public DateTime LastCleaned { get; set; }

        private static readonly string SettingsPath = "settings.json";

        private AppSettings()
        {
            AutoAnalyze = false;
            AutoClean = false;
            ShowNotifications = true;
            SafetyBackup = true;
            LastCleaned = DateTime.MinValue;
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
                        AutoAnalyze = settings.AutoAnalyze;
                        AutoClean = settings.AutoClean;
                        ShowNotifications = settings.ShowNotifications;
                        SafetyBackup = settings.SafetyBackup;
                        LastCleaned = settings.LastCleaned;
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
