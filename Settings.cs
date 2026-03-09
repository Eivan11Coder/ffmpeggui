using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace FPBetaVer_1
{
    class Settings
    {
        public string FFmpegPath { get; set; } = "ffmpeg.exe";
        public string LastInputPath { get; set; }
        public string LastOutputPath { get; set; }

        private static readonly string SettingsFile = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

        public static Settings Load()
        {
            if (File.Exists(SettingsFile))
            {
                try
                {
                    string json = File.ReadAllText(SettingsFile);
                    return JsonConvert.DeserializeObject<Settings>(json) ?? new Settings();
                }
                catch { }
            }
            return new Settings();
        }

        public void Save()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(SettingsFile, json);
        }
    }
}
