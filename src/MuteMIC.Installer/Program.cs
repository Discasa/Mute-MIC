using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace MuteMIC.Installer;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new InstallerForm());
    }
}

internal sealed class InstallerForm : Form
{
    private const string AppName = "Mute MIC";
    private const string LegacyTaskName = "MicMute";
    private const string UninstallerName = "Mute MIC Uninstaller.exe";
    private const string AppVersion = "1.0.3";
    private const string Publisher = "anderson";
    private const string UninstallRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\Mute MIC";

    private readonly string _installDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        AppName);
    private readonly Label _titleLabel = new();
    private readonly Label _bodyLabel = new();
    private readonly Label _locationLabel = new();
    private readonly Label _statusLabel = new();
    private readonly ProgressBar _progressBar = new();
    private readonly Button _installButton = new();
    private readonly Button _cancelButton = new();
    private bool _installComplete;
    private bool _installing;

    public InstallerForm()
    {
        Text = $"{AppName} Installer";
        Icon? icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        if (icon is not null)
        {
            Icon = icon;
        }

        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(560, 330);
        MinimumSize = new Size(560, 330);
        MaximumSize = new Size(560, 330);
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        BuildUi();
        ApplyTheme();
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
        TableLayoutPanel root = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24),
            ColumnCount = 1,
            RowCount = 6
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        _titleLabel.Text = "Install Mute MIC";
        _titleLabel.Font = new Font(Font.FontFamily, 18, FontStyle.Regular);
        _titleLabel.Dock = DockStyle.Fill;
        _titleLabel.TextAlign = ContentAlignment.MiddleLeft;

        _bodyLabel.Text = "This setup will install Mute MIC and configure startup, Start Menu shortcuts, and Windows Installed Apps integration.";
        _bodyLabel.Dock = DockStyle.Fill;
        _bodyLabel.AutoEllipsis = true;

        Panel locationPanel = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            Margin = new Padding(0, 4, 0, 4)
        };
        Label locationTitle = new()
        {
            Text = "Install location",
            Dock = DockStyle.Top,
            Height = 22
        };
        _locationLabel.Text = _installDir;
        _locationLabel.Dock = DockStyle.Fill;
        _locationLabel.AutoEllipsis = true;
        locationPanel.Controls.Add(_locationLabel);
        locationPanel.Controls.Add(locationTitle);

        _progressBar.Dock = DockStyle.Fill;
        _progressBar.Minimum = 0;
        _progressBar.Maximum = 100;
        _progressBar.Value = 0;
        _progressBar.Style = ProgressBarStyle.Continuous;
        _progressBar.Visible = false;

        _statusLabel.Text = "Ready to install.";
        _statusLabel.Dock = DockStyle.Top;
        _statusLabel.Height = 24;

        FlowLayoutPanel buttons = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft
        };
        _installButton.Text = "Install";
        _installButton.Width = 112;
        _installButton.Height = 34;
        _installButton.Click += InstallButton_Click;
        _cancelButton.Text = "Cancel";
        _cancelButton.Width = 112;
        _cancelButton.Height = 34;
        _cancelButton.Click += (_, _) => Close();
        buttons.Controls.Add(_installButton);
        buttons.Controls.Add(_cancelButton);

        root.Controls.Add(_titleLabel, 0, 0);
        root.Controls.Add(_bodyLabel, 0, 1);
        root.Controls.Add(locationPanel, 0, 2);
        root.Controls.Add(_progressBar, 0, 3);
        root.Controls.Add(_statusLabel, 0, 4);
        root.Controls.Add(buttons, 0, 5);

        Controls.Add(root);
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
        _progressBar.Value = Math.Clamp(progress.Percent, 0, 100);
        _statusLabel.Text = progress.Message;
    }

    private void Install(IProgress<InstallProgress> progress)
    {
        string appExe = Path.Combine(_installDir, $"{AppName}.exe");
        string uninstallerExe = Path.Combine(_installDir, UninstallerName);
        string appIcon = Path.Combine(_installDir, $"{AppName}.ico");

        progress.Report(new InstallProgress(5, "Preparing installation..."));
        StopKnownProcesses();

        progress.Report(new InstallProgress(18, "Removing old startup tasks..."));
        DeleteTask(LegacyTaskName);
        DeleteTask(AppName);

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

        progress.Report(new InstallProgress(90, "Configuring startup task..."));
        CreateLogonTask(appExe);

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

    private void ApplyTheme()
    {
        bool lightTheme = IsLightTheme();
        if (IsHandleCreated)
        {
            ApplyWindowFrame(this, lightTheme);
        }

        Color back = lightTheme ? Color.White : Color.FromArgb(32, 32, 32);
        Color fore = lightTheme ? Color.FromArgb(24, 24, 24) : Color.White;
        Color panel = lightTheme ? Color.FromArgb(246, 246, 246) : Color.FromArgb(42, 42, 42);
        Color border = lightTheme ? Color.FromArgb(218, 218, 218) : Color.FromArgb(70, 70, 70);

        BackColor = back;
        ForeColor = fore;

        foreach (Control control in Controls.Cast<Control>().SelectMany(FlattenControls))
        {
            control.ForeColor = fore;
            if (control is Panel)
            {
                control.BackColor = panel;
            }
            else if (control is Button button)
            {
                button.FlatStyle = FlatStyle.Flat;
                button.BackColor = back;
                button.FlatAppearance.BorderColor = border;
                button.FlatAppearance.MouseOverBackColor = panel;
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
