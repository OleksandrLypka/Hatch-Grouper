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
                    return JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath, Encoding.UTF8)) ?? new Settings();
            }
            catch
            {
                // damaged file: fall back to defaults
            }
            return new Settings();
        }

        public static void Save(Settings settings)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, options), Encoding.UTF8);
        }
    }
}
