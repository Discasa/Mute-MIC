using System.Diagnostics;
using Microsoft.Win32;

namespace MuteMIC.Uninstaller;

internal static class Program
{
    private const string AppName = "Mute MIC";
    private const string LegacyTaskName = "MicMute";

    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        try
        {
            string installDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AppName);

            StopKnownProcesses();
            DeleteTask(AppName);
            DeleteTask(LegacyTaskName);
            DeleteShortcuts();
            DeleteRegistryKeys();
            ScheduleInstallFolderRemoval(installDir);

            MessageBox.Show(
                $"{AppName} was uninstalled successfully.",
                AppName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void StopKnownProcesses()
    {
        foreach (string processName in new[] { AppName, LegacyTaskName })
        {
            foreach (Process process in Process.GetProcessesByName(processName))
            {
                if (process.Id == Environment.ProcessId)
                {
                    continue;
                }

                try
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(5000);
                }
                catch
                {
                    // Folder cleanup will fail loudly if a process still owns files.
                }
            }
        }
    }

    private static void DeleteShortcuts()
    {
        string programs = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
        foreach (string shortcut in new[]
        {
            Path.Combine(programs, $"{AppName}.lnk"),
            Path.Combine(programs, $"Uninstall {AppName}.lnk")
        })
        {
            if (File.Exists(shortcut))
            {
                File.Delete(shortcut);
            }
        }
    }

    private static void DeleteRegistryKeys()
    {
        Registry.CurrentUser.DeleteSubKeyTree(@"Software\Mute MIC", throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(@"Software\MicMute", throwOnMissingSubKey: false);
    }

    private static void ScheduleInstallFolderRemoval(string installDir)
    {
        if (!Directory.Exists(installDir))
        {
            return;
        }

        string localAppData = Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
        string resolvedInstallDir = Path.GetFullPath(installDir);
        if (!resolvedInstallDir.StartsWith(localAppData, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Refusing to remove unexpected folder: {resolvedInstallDir}");
        }

        string command = $"timeout /t 2 /nobreak >nul & rmdir /s /q \"{resolvedInstallDir}\"";
        Process.Start(new ProcessStartInfo("cmd.exe")
        {
            ArgumentList = { "/c", command },
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        });
    }

    private static void DeleteTask(string taskName)
    {
        RunSchtasks(["/end", "/tn", taskName]);
        RunSchtasks(["/delete", "/tn", taskName, "/f"]);
    }

    private static void RunSchtasks(string[] arguments)
    {
        using Process process = new()
        {
            StartInfo = new ProcessStartInfo("schtasks.exe")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            }
        };

        foreach (string argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        process.Start();
        process.WaitForExit();
    }
}
