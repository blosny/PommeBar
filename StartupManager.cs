using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace PommeBar;

public static class StartupManager
{
    private const string AppName = "PommeBar";
    private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false);
            if (key == null) return false;

            var value = key.GetValue(AppName) as string;
            return !string.IsNullOrEmpty(value);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to check startup status: {ex.Message}");
            return false;
        }
    }

    public static bool SetStartup(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
            if (key == null) return false;

            if (enable)
            {
                string? exePath = Environment.ProcessPath;
                if (string.IsNullOrEmpty(exePath))
                {
                    exePath = Process.GetCurrentProcess().MainModule?.FileName;
                }

                if (!string.IsNullOrEmpty(exePath))
                {
                    key.SetValue(AppName, $"\"{exePath}\"");
                    return true;
                }
                return false;
            }
            else
            {
                key.DeleteValue(AppName, false);
                return true;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to set startup status: {ex.Message}");
            return false;
        }
    }
}
