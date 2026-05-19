using Microsoft.Win32;

namespace MuteMIC.App;

internal static class StartupRegistrationService
{
    private const string AppName = "Mute MIC";
    private const string LegacyAppName = "MicMute";
    private const string StartupRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static void EnsureRegistered()
    {
        string currentExe = Environment.ProcessPath ?? Application.ExecutablePath;
        if (!IsInstalledAppPath(currentExe))
        {
            return;
        }

        try
        {
            using RegistryKey key = Registry.CurrentUser.CreateSubKey(StartupRegistryPath);
            string expectedValue = $"\"{currentExe}\"";
            if (!string.Equals(key.GetValue(AppName) as string, expectedValue, StringComparison.OrdinalIgnoreCase))
            {
                key.SetValue(AppName, expectedValue);
            }

            key.DeleteValue(LegacyAppName, throwOnMissingValue: false);
        }
        catch
        {
            // Startup repair is best-effort and should never block hotkeys or audio control.
        }
    }

    private static bool IsInstalledAppPath(string executablePath)
    {
        string expectedPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppName,
            $"{AppName}.exe");

        return string.Equals(
            Path.GetFullPath(executablePath),
            Path.GetFullPath(expectedPath),
            StringComparison.OrdinalIgnoreCase);
    }
}
