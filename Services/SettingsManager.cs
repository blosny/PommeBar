using System;
using System.IO;
using System.Text.Json;

namespace PommeBar.Services;

public class AppSettings
{
    public string PositionPreset { get; set; } = "left";
    public string LockMode { get; set; } = "free";
    public string DockMode { get; set; } = "floating";
    public string SizePreset { get; set; } = "standard";
    public double CustomWidth { get; set; } = 430;
    public string MediaFilter { get; set; } = "musiconly";
    public double CustomLeft { get; set; } = 20;
    public double CustomTop { get; set; } = 800;
    public bool StartMinimizedToTray { get; set; } = false;
}

public static class SettingsManager
{
    private static readonly string AppDataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PommeBar");
    
    private static readonly string SettingsFilePath = Path.Combine(AppDataDir, "settings.json");
    private static readonly string LegacyConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "position.cfg");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                string json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null) return settings;
            }
            else if (File.Exists(LegacyConfigPath))
            {
                // Migrate legacy position.cfg
                var lines = File.ReadAllLines(LegacyConfigPath);
                var settings = new AppSettings();
                if (lines.Length > 0 && !string.IsNullOrWhiteSpace(lines[0])) settings.PositionPreset = lines[0].Trim().ToLower();
                if (lines.Length > 1 && !string.IsNullOrWhiteSpace(lines[1])) settings.LockMode = lines[1].Trim().ToLower();
                if (lines.Length >= 4 && double.TryParse(lines[2], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double l) 
                    && double.TryParse(lines[3], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double t))
                {
                    settings.CustomLeft = l;
                    settings.CustomTop = t;
                }
                if (lines.Length >= 5 && !string.IsNullOrWhiteSpace(lines[4])) settings.DockMode = lines[4].Trim().ToLower();
                Save(settings);
                return settings;
            }
        }
        catch { }

        return new AppSettings();
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            if (!Directory.Exists(AppDataDir))
            {
                Directory.CreateDirectory(AppDataDir);
            }
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFilePath, json);
        }
        catch { }
    }
}
