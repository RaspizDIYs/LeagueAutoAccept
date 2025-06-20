using System;
using System.IO;
using System.Text.Json;

namespace LeagueAutoAccept.Utils;

public class AppSettings
{
    private static readonly string Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LeagueAutoAccept");
    private static readonly string FilePath = Path.Combine(Dir, "settings.json");

    public bool AutoLaunchEnabled { get; set; }

    private static AppSettings? _current;
    public static AppSettings Current => _current ??= Load();

    private static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                var cfg = JsonSerializer.Deserialize<AppSettings>(json);
                if (cfg != null) return cfg;
            }
        }
        catch { /* ignore */ }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Dir);
            var json = JsonSerializer.Serialize(this);
            File.WriteAllText(FilePath, json);
        }
        catch { /* ignore */ }
    }
} 