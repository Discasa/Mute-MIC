using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using MuteMIC.SetupUi;

namespace MuteMIC.Installer;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        InstallerOptions options = InstallerOptions.Parse(args);
        if (options.Silent)
        {
            return InstallerForm.RunSilent(options);
        }

        Application.Run(new InstallerForm(options));
        return 0;
    }
}

internal sealed record InstallerOptions(bool Silent, bool FromUpdate)
{
    public static InstallerOptions Parse(string[] args)
    {
        bool silent = args.Any(arg => string.Equals(arg, "--silent", StringComparison.OrdinalIgnoreCase));
        bool fromUpdate = args.Any(arg => string.Equals(arg, "--from-update", StringComparison.OrdinalIgnoreCase));
        return new InstallerOptions(silent, fromUpdate);
    }
}

internal sealed class InstallerForm : Form
{
    private const string AppName = "Mute MIC";
    private const string LegacyTaskName = "MicMute";
    private const string UninstallerName = "Mute MIC Uninstaller.exe";
    private const string AppVersion = "1.1.0";
    private const string Publisher = "anderson";
    private const string UninstallRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\Mute MIC";
    private const string StartupRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    private readonly string _installDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        AppName);
    private readonly Label _titleLabel = new();
    private readonly Label _bodyLabel = new();
    private readonly Label _locationLabel = new();
    private readonly Label _statusLabel = new();
    private readonly SetupFieldPanel _locationPanel = new();
    private readonly SetupProgressBar _progressBar = new();
    private readonly SetupButton _installButton = new();
    private readonly SetupButton _cancelButton = new();
    private bool _installComplete;
    private bool _installing;

    public InstallerForm(InstallerOptions options)
    {
        Text = $"{AppName} Installer";
        Icon? icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        if (icon is not null)
        {
            Icon = icon;
        }

        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(560, 360);
        Size fixedSize = Size;
        MinimumSize = fixedSize;
        MaximumSize = fixedSize;
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        BuildUi();
        ApplyTheme();
    }

    public static int RunSilent(InstallerOptions options)
    {
        try
        {
            using InstallerForm form = new(options);
            form.Install(new Progress<InstallProgress>());
            return 0;
        }
        catch (Exception ex)
        {
            LogInstallError(ex);
            return 1;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyTheme();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_installing)
        {
            e.Cancel = true;
        }

        base.OnFormClosing(e);
    }

    private void BuildUi()
    {
        _titleLabel.Text = "Install Mute MIC";
        _titleLabel.Font = new Font("Segoe UI", 18F, FontStyle.Regular, GraphicsUnit.Point);
        _titleLabel.Location = new Point(28, 28);
        _titleLabel.Size = new Size(504, 36);
        _titleLabel.TextAlign = ContentAlignment.MiddleLeft;

        _bodyLabel.Text = "This setup will install Mute MIC and configure startup, Start Menu shortcuts, and Windows Installed Apps integration.";
        _bodyLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        _bodyLabel.Location = new Point(30, 72);
        _bodyLabel.Size = new Size(500, 42);
        _bodyLabel.AutoEllipsis = true;

        Label locationTitle = new()
        {
            Text = "Install location",
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
            Dock = DockStyle.Top,
            Height = 20
        };
        _locationLabel.Text = _installDir;
        _locationLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        _locationLabel.Dock = DockStyle.Fill;
        _locationLabel.AutoEllipsis = true;
        _locationPanel.Location = new Point(28, 134);
        _locationPanel.Size = new Size(504, 58);
        _locationPanel.Controls.Add(_locationLabel);
        _locationPanel.Controls.Add(locationTitle);

        _progressBar.Location = new Point(28, 218);
        _progressBar.Size = new Size(504, 8);
        _progressBar.ProgressValue = 0;
        _progressBar.Visible = false;

        _statusLabel.Text = "Ready to install.";
        _statusLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        _statusLabel.Location = new Point(30, 238);
        _statusLabel.Size = new Size(500, 34);

        _installButton.Text = "Install";
        _installButton.Location = new Point(422, 304);
        _installButton.Size = new Size(110, 36);
        _installButton.Click += InstallButton_Click;
        _cancelButton.Text = "Cancel";
        _cancelButton.Location = new Point(300, 304);
        _cancelButton.Size = new Size(110, 36);
        _cancelButton.Click += (_, _) => Close();

        Panel separator = new()
        {
            Location = new Point(28, 288),
            Size = new Size(504, 1)
        };
        separator.Paint += (_, e) =>
        {
            using Pen pen = new(_locationPanel.BorderColor);
            e.Graphics.DrawLine(pen, 0, 0, separator.Width, 0);
        };

        Controls.AddRange([
            _titleLabel,
            _bodyLabel,
            _locationPanel,
            _progressBar,
            _statusLabel,
            separator,
            _cancelButton,
            _installButton
        ]);
    }

    private async void InstallButton_Click(object? sender, EventArgs e)
    {
        if (_installComplete)
        {
            Close();
            return;
        }

        _installing = true;
        _installButton.Enabled = false;
        _cancelButton.Enabled = false;
        _progressBar.Visible = true;
        _titleLabel.Text = "Installing Mute MIC";
        _bodyLabel.Text = "Please wait while setup installs the application.";

        Progress<InstallProgress> progress = new(UpdateProgress);

        try
        {
            await Task.Run(() => Install(progress));
            UpdateProgress(new InstallProgress(100, "Mute MIC was installed successfully."));
            _titleLabel.Text = "Mute MIC installed";
            _bodyLabel.Text = "The application is installed and ready to use.";
            _installComplete = true;
            _installButton.Text = "Finish";
            _installButton.Enabled = true;
            _cancelButton.Visible = false;
        }
        catch (Exception ex)
        {
            _titleLabel.Text = "Installation failed";
            _bodyLabel.Text = ex.Message;
            _statusLabel.Text = "Setup could not complete.";
            _installButton.Text = "Close";
            _installComplete = true;
            _installButton.Enabled = true;
            _cancelButton.Visible = false;
        }
        finally
        {
            _installing = false;
        }
    }

    private void UpdateProgress(InstallProgress progress)
    {
        _progressBar.ProgressValue = progress.Percent;
        _statusLabel.Text = progress.Message;
    }

    private void Install(IProgress<InstallProgress> progress)
    {
        string appExe = Path.Combine(_installDir, $"{AppName}.exe");
        string uninstallerExe = Path.Combine(_installDir, UninstallerName);
        string appIcon = Path.Combine(_installDir, $"{AppName}.ico");

        progress.Report(new InstallProgress(5, "Preparing installation..."));
        StopKnownProcesses();

        progress.Report(new InstallProgress(18, "Removing old startup entries..."));
        DeleteStartupEntries();

        progress.Report(new InstallProgress(32, "Creating installation folder..."));
        PrepareInstallDirectory(_installDir);

        progress.Report(new InstallProgress(52, "Copying application files..."));
        InstallPayload(_installDir);
        if (!File.Exists(appExe))
        {
            throw new FileNotFoundException("Application executable was not copied.", appExe);
        }

        progress.Report(new InstallProgress(68, "Creating Start Menu shortcuts..."));
        CreateShortcuts(appExe, uninstallerExe, appIcon);

        progress.Report(new InstallProgress(78, "Registering Windows Installed Apps entry..."));
        RegisterInstalledApp(_installDir, appExe, uninstallerExe, appIcon);

        progress.Report(new InstallProgress(90, "Configuring startup entry..."));
        if (string.Equals(Environment.GetEnvironmentVariable("MUTEMIC_SKIP_STARTUP"), "1", StringComparison.Ordinal))
        {
            progress.Report(new InstallProgress(90, "Skipping startup entry for this test run..."));
        }
        else
        {
            CreateStartupEntry(appExe);
        }

        progress.Report(new InstallProgress(96, "Starting Mute MIC..."));
        Process.Start(new ProcessStartInfo(appExe) { UseShellExecute = true });
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

    private static void CreateStartupEntry(string appExe)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(StartupRegistryPath);
        key.SetValue(AppName, $"\"{appExe}\"");
    }

    private static void DeleteStartupEntries()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(StartupRegistryPath, writable: true);
        key?.DeleteValue(AppName, throwOnMissingValue: false);
        key?.DeleteValue(LegacyTaskName, throwOnMissingValue: false);
        DeleteTask(LegacyTaskName);
        DeleteTask(AppName);
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

    private static void LogInstallError(Exception exception)
    {
        try
        {
            string logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AppName);
            Directory.CreateDirectory(logDir);
            File.AppendAllText(
                Path.Combine(logDir, "install.log"),
                $"[{DateTimeOffset.Now:O}] {exception}{Environment.NewLine}");
        }
        catch
        {
        }
    }

    private void ApplyTheme()
    {
        bool lightTheme = IsLightTheme();
        if (IsHandleCreated)
        {
            ApplyWindowFrame(this, lightTheme);
        }

        Color back = lightTheme ? Color.White : Color.FromArgb(32, 32, 32);
        Color fore = lightTheme ? Color.FromArgb(24, 24, 24) : Color.White;
        Color panel = lightTheme ? Color.FromArgb(246, 246, 246) : Color.FromArgb(43, 43, 43);
        Color border = lightTheme ? Color.FromArgb(218, 218, 218) : Color.FromArgb(72, 72, 72);

        BackColor = back;
        ForeColor = fore;
        _locationPanel.FillColor = panel;
        _locationPanel.BorderColor = border;
        _progressBar.TrackColor = lightTheme ? Color.FromArgb(226, 226, 226) : Color.FromArgb(58, 58, 58);
        _progressBar.ProgressColor = Color.FromArgb(0, 120, 212);

        foreach (Control control in Controls.Cast<Control>().SelectMany(FlattenControls))
        {
            control.ForeColor = fore;
            if (control.Parent is SetupFieldPanel parentFieldPanel)
            {
                control.BackColor = parentFieldPanel.FillColor;
            }
            else if (control is SetupFieldPanel fieldPanel)
            {
                fieldPanel.BackColor = back;
                fieldPanel.FillColor = panel;
                fieldPanel.BorderColor = border;
                fieldPanel.Invalidate();
            }
            else if (control is SetupProgressBar progressBar)
            {
                progressBar.BackColor = back;
                progressBar.TrackColor = lightTheme ? Color.FromArgb(226, 226, 226) : Color.FromArgb(58, 58, 58);
                progressBar.ProgressColor = Color.FromArgb(0, 120, 212);
            }
            else if (control is SetupButton setupButton)
            {
                setupButton.BackColor = lightTheme ? Color.FromArgb(252, 252, 252) : Color.FromArgb(37, 37, 37);
                setupButton.BorderColor = border;
                setupButton.HoverBackColor = lightTheme ? Color.FromArgb(242, 242, 242) : Color.FromArgb(50, 50, 50);
                setupButton.PressedBackColor = lightTheme ? Color.FromArgb(235, 235, 235) : Color.FromArgb(58, 58, 58);
                setupButton.ForeColor = fore;
                setupButton.Invalidate();
            }
            else if (control is Panel)
            {
                control.BackColor = back;
            }
            else
            {
                control.BackColor = back;
            }
        }
    }

    private static IEnumerable<Control> FlattenControls(Control root)
    {
        yield return root;
        foreach (Control child in root.Controls)
        {
            foreach (Control nested in FlattenControls(child))
            {
                yield return nested;
            }
        }
    }

    private static bool IsLightTheme()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        object? value = key?.GetValue("AppsUseLightTheme");
        return value is not int intValue || intValue != 0;
    }

    private static void ApplyWindowFrame(Form form, bool lightTheme)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
        {
            return;
        }

        int useDarkMode = lightTheme ? 0 : 1;
        DwmSetWindowAttribute(form.Handle, 20, ref useDarkMode, sizeof(int));

        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            return;
        }

        int cornerPreference = 2;
        int captionColor = ToColorRef(lightTheme ? Color.White : Color.FromArgb(32, 32, 32));
        int textColor = ToColorRef(lightTheme ? Color.FromArgb(24, 24, 24) : Color.White);
        int borderColor = ToColorRef(lightTheme ? Color.FromArgb(208, 208, 208) : Color.FromArgb(64, 64, 64));

        DwmSetWindowAttribute(form.Handle, 33, ref cornerPreference, sizeof(int));
        DwmSetWindowAttribute(form.Handle, 35, ref captionColor, sizeof(int));
        DwmSetWindowAttribute(form.Handle, 36, ref textColor, sizeof(int));
        DwmSetWindowAttribute(form.Handle, 34, ref borderColor, sizeof(int));
    }

    private static int ToColorRef(Color color)
    {
        return color.R | (color.G << 8) | (color.B << 16);
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int attributeValue, int attributeSize);
}

internal readonly record struct InstallProgress(int Percent, string Message);
