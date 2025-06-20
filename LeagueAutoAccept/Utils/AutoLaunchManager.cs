using System.Reflection;
using Microsoft.Win32;

namespace LeagueAutoAccept.Utils;

public static class AutoLaunchManager
{
    private const string RegPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "LeagueAutoAccept";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegPath, false);
        return key?.GetValue(ValueName) != null;
    }

    public static void Enable()
    {
        var exe = $"\"{Assembly.GetExecutingAssembly().Location}\"";
        using var key = Registry.CurrentUser.OpenSubKey(RegPath, true);
        key?.SetValue(ValueName, exe);
    }

    public static void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegPath, true);
        key?.DeleteValue(ValueName, false);
    }
} 