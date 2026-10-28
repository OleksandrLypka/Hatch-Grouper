using System.IO;
using System.Text;
using System.Text.Json;

namespace HatchGrouper
{
    /// <summary>Persists the program settings between runs (%AppData%\HatchGrouper\settings.json).</summary>
    public static class SettingsStore
    {
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HatchGrouper", "settings.json");

        /// <summary>Returns saved settings, or defaults if the file is missing or damaged.</summary>
        public static Settings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    string json = File.ReadAllText(FilePath, Encoding.UTF8);
                    // Sanitize legacy "TrimMm": null from older beta builds
                    json = System.Text.RegularExpressions.Regex.Replace(json, @"""TrimMm""\s*:\s*null", @"""TrimMm"": 60");
                    var s = JsonSerializer.Deserialize<Settings>(json);
                    if (s != null)
                    {
                        if (s.TrimMm <= 0) s.TrimMm = 60;
                        if (s.TrimHMm <= 0) s.TrimHMm = 60;
                        if (s.DepthMm <= 0) s.DepthMm = 80;
                        if (s.TrimDiffWMm <= 0) s.TrimDiffWMm = 116.2;
                        if (s.TrimDiffHMm <= 0) s.TrimDiffHMm = 117.8;
                        if (s.HatchDiffWMm <= 0) s.HatchDiffWMm = 131.6;
                        if (s.HatchDiffHMm <= 0) s.HatchDiffHMm = 131.2;
                        return s;
                    }
                }
            }
            catch
            {
                // damaged file: fall back to defaults
            }
            return new Settings();
        }

        public static void Save(Settings settings)
        {
            if (settings == null) return;
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, options), Encoding.UTF8);
        }
    }
}
