using System.Diagnostics;
using System.Reflection;
using Microsoft.Win32;

namespace MuteMIC.Installer;

internal static class Program
{
    private const string AppName = "Mute MIC";
    private const string LegacyTaskName = "MicMute";
    private const string UninstallerName = "Mute MIC Uninstaller.exe";
    private const string AppVersion = "1.0.2";
    private const string Publisher = "anderson";
    private const string UninstallRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\Mute MIC";

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
            DeleteTask(LegacyTaskName);
            DeleteTask(AppName);

            PrepareInstallDirectory(installDir);
            InstallPayload(installDir);

            string appExe = Path.Combine(installDir, $"{AppName}.exe");
            string uninstallerExe = Path.Combine(installDir, UninstallerName);
            string appIcon = Path.Combine(installDir, $"{AppName}.ico");
            if (!File.Exists(appExe))
            {
                throw new FileNotFoundException("Application executable was not copied.", appExe);
            }

            CreateShortcuts(appExe, uninstallerExe, appIcon);
            RegisterInstalledApp(installDir, appExe, uninstallerExe, appIcon);
            CreateLogonTask(appExe);
            Process.Start(new ProcessStartInfo(appExe) { UseShellExecute = true });

            MessageBox.Show(
                $"{AppName} was installed successfully.",
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
                try
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(5000);
                }
                catch
                {
                    // The installer continues; replacing files will surface any remaining lock.
                }
            }
        }
    }

    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        foreach (string directory in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(sourceDir, directory);
            Directory.CreateDirectory(Path.Combine(targetDir, relative));
        }

        foreach (string file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(sourceDir, file);
            string target = Path.Combine(targetDir, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }

    private static void PrepareInstallDirectory(string installDir)
    {
        string localAppData = Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
        string resolvedInstallDir = Path.GetFullPath(installDir);
        if (!resolvedInstallDir.StartsWith(localAppData, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Refusing to clean unexpected folder: {resolvedInstallDir}");
        }

        if (Directory.Exists(resolvedInstallDir))
        {
            Directory.Delete(resolvedInstallDir, recursive: true);
        }

        Directory.CreateDirectory(resolvedInstallDir);
    }

    private static void InstallPayload(string installDir)
    {
        string[] resourceNames = Assembly.GetExecutingAssembly()
            .GetManifestResourceNames()
            .Where(name => name.StartsWith("Payload.", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (resourceNames.Length > 0)
        {
            foreach (string resourceName in resourceNames)
            {
                string fileName = resourceName["Payload.".Length..];
                using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
                if (stream is null)
                {
                    throw new InvalidOperationException($"Embedded payload not found: {resourceName}");
                }

                using FileStream target = File.Create(Path.Combine(installDir, fileName));
                stream.CopyTo(target);
            }

            return;
        }

        string payloadDir = Path.Combine(AppContext.BaseDirectory, "payload");
        if (!Directory.Exists(payloadDir))
        {
            throw new DirectoryNotFoundException($"Payload folder not found: {payloadDir}");
        }

        CopyDirectory(payloadDir, installDir);
    }

    private static void CreateShortcuts(string appExe, string uninstallerExe, string appIcon)
    {
        string programs = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
        string startMenuFolder = Path.Combine(programs, AppName);
        Directory.CreateDirectory(startMenuFolder);
        DeleteOldRootShortcuts(programs);

        string appShortcut = Path.Combine(startMenuFolder, $"{AppName}.lnk");
        string uninstallShortcut = Path.Combine(startMenuFolder, $"Uninstall {AppName}.lnk");

        string iconLocation = File.Exists(appIcon) ? appIcon : $"{appExe},0";
        CreateShortcut(appShortcut, appExe, AppName, iconLocation);
        SetShortcutRunAsAdministrator(appShortcut);

        if (File.Exists(uninstallerExe))
        {
            CreateShortcut(uninstallShortcut, uninstallerExe, $"Uninstall {AppName}", iconLocation);
        }
    }

    private static void DeleteOldRootShortcuts(string programs)
    {
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

    private static void RegisterInstalledApp(string installDir, string appExe, string uninstallerExe, string appIcon)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(UninstallRegistryPath);
        key.SetValue("DisplayName", AppName);
        key.SetValue("DisplayVersion", AppVersion);
        key.SetValue("Publisher", Publisher);
        key.SetValue("InstallLocation", installDir);
        key.SetValue("DisplayIcon", File.Exists(appIcon) ? appIcon : $"{appExe},0");
        key.SetValue("UninstallString", $"\"{uninstallerExe}\"");
        key.SetValue("InstallDate", DateTime.Now.ToString("yyyyMMdd"));
        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
        key.SetValue("EstimatedSize", EstimateInstallSizeKb(installDir), RegistryValueKind.DWord);
    }

    private static int EstimateInstallSizeKb(string installDir)
    {
        long bytes = Directory.EnumerateFiles(installDir, "*", SearchOption.AllDirectories)
            .Sum(file => new FileInfo(file).Length);
        return Math.Max(1, (int)Math.Ceiling(bytes / 1024.0));
    }

    private static void CreateShortcut(string shortcutPath, string targetPath, string description, string iconLocation)
    {
        Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType is null)
        {
            throw new InvalidOperationException("Could not access WScript.Shell.");
        }

        dynamic shell = Activator.CreateInstance(shellType)!;
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = targetPath;
        shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
        shortcut.IconLocation = iconLocation;
        shortcut.Description = description;
        shortcut.Save();
    }

    private static void SetShortcutRunAsAdministrator(string shortcutPath)
    {
        byte[] bytes = File.ReadAllBytes(shortcutPath);
        if (bytes.Length > 0x15)
        {
            bytes[0x15] = (byte)(bytes[0x15] | 0x20);
            File.WriteAllBytes(shortcutPath, bytes);
        }
    }

    private static void CreateLogonTask(string appExe)
    {
        RunSchtasks([
            "/create",
            "/sc",
            "ONLOGON",
            "/tn",
            AppName,
            "/tr",
            $"\"{appExe}\"",
            "/rl",
            "HIGHEST",
            "/f"
        ], throwOnError: true);
    }

    private static void DeleteTask(string taskName)
    {
        RunSchtasks(["/end", "/tn", taskName], throwOnError: false);
        RunSchtasks(["/delete", "/tn", taskName, "/f"], throwOnError: false);
    }

    private static void RunSchtasks(string[] arguments, bool throwOnError)
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

        if (throwOnError && process.ExitCode != 0)
        {
            throw new InvalidOperationException($"schtasks failed with exit code {process.ExitCode}.");
        }
    }
}
