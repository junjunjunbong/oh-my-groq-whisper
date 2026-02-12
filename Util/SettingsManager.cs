using System.IO;
using System.Text.Json;

namespace GroqWhisperPTT.Util;

public class WindowSettings
{
    public double Width { get; set; } = 400;
    public double Height { get; set; } = 200;
}

public static class SettingsManager
{
    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "GroqWhisperPTT");
    
    private static readonly string SettingsPath = Path.Combine(SettingsDir, "settings.json");

    public static WindowSettings LoadWindowSettings()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer.Deserialize<WindowSettings>(json);
                if (settings != null)
                {
                    // Validate minimum sizes
                    settings.Width = Math.Max(300, settings.Width);
                    settings.Height = Math.Max(150, settings.Height);
                    return settings;
                }
            }
        }
        catch
        {
            // Ignore errors and return defaults
        }
        
        return new WindowSettings();
    }

    public static void SaveWindowSettings(WindowSettings settings)
    {
        try
        {
            if (!Directory.Exists(SettingsDir))
            {
                Directory.CreateDirectory(SettingsDir);
            }

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Ignore save errors
        }
    }
}
