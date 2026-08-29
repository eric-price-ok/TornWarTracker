using System;
using System.IO;
using System.Text.Json;
using TornWarTracker.Models;

namespace TornWarTracker.Services
{
    /// <summary>
    /// Loads/saves settings (API keys, faction ID, filters) to a plain JSON
    /// file at %AppData%\TornWarTracker\config.json.
    /// </summary>
    public class ConfigService
    {
        private readonly string _configPath;

        public ConfigService()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TornWarTracker");
            Directory.CreateDirectory(dir);
            _configPath = Path.Combine(dir, "config.json");
        }

        public AppConfig Load()
        {
            if (!File.Exists(_configPath))
                return new AppConfig();

            try
            {
                var json = File.ReadAllText(_configPath);
                return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
            catch
            {
                // Corrupt or unreadable config - start fresh rather than crash.
                return new AppConfig();
            }
        }

        public void Save(AppConfig config)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(config, options);
            File.WriteAllText(_configPath, json);
        }
    }
}
