using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace MuteMIC.Uninstaller;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new UninstallerForm());
    }
}

internal sealed class UninstallerForm : Form
{
    private const string AppName = "Mute MIC";
    private const string LegacyTaskName = "MicMute";
    private const string UninstallRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\Mute MIC";

    private readonly string _installDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        AppName);
    private readonly Label _titleLabel = new();
    private readonly Label _bodyLabel = new();
    private readonly Label _locationLabel = new();
    private readonly Label _statusLabel = new();
    private readonly ProgressBar _progressBar = new();
    private readonly Button _uninstallButton = new();
    private readonly Button _cancelButton = new();
    private bool _complete;
    private bool _uninstalling;
    private bool _hadInstallFolder;

    public UninstallerForm()
    {
        Text = $"{AppName} Uninstaller";
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
        if (_uninstalling)
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

        _titleLabel.Text = "Uninstall Mute MIC";
        _titleLabel.Font = new Font(Font.FontFamily, 18, FontStyle.Regular);
        _titleLabel.Dock = DockStyle.Fill;
        _titleLabel.TextAlign = ContentAlignment.MiddleLeft;

        _bodyLabel.Text = "This will remove Mute MIC, startup tasks, Start Menu shortcuts, Windows Installed Apps integration, and settings.";
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
            Text = "Installed location",
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

        _statusLabel.Text = "Ready to uninstall.";
        _statusLabel.Dock = DockStyle.Top;
        _statusLabel.Height = 24;

        FlowLayoutPanel buttons = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft
        };
        _uninstallButton.Text = "Uninstall";
        _uninstallButton.Width = 112;
        _uninstallButton.Height = 34;
        _uninstallButton.Click += UninstallButton_Click;
        _cancelButton.Text = "Cancel";
        _cancelButton.Width = 112;
        _cancelButton.Height = 34;
        _cancelButton.Click += (_, _) => Close();
        buttons.Controls.Add(_uninstallButton);
        buttons.Controls.Add(_cancelButton);

        root.Controls.Add(_titleLabel, 0, 0);
        root.Controls.Add(_bodyLabel, 0, 1);
        root.Controls.Add(locationPanel, 0, 2);
        root.Controls.Add(_progressBar, 0, 3);
        root.Controls.Add(_statusLabel, 0, 4);
        root.Controls.Add(buttons, 0, 5);

        Controls.Add(root);
    }

    private async void UninstallButton_Click(object? sender, EventArgs e)
    {
        if (_complete)
        {
            Close();
            return;
        }

        _uninstalling = true;
        _uninstallButton.Enabled = false;
        _cancelButton.Enabled = false;
        _progressBar.Visible = true;
        _titleLabel.Text = "Uninstalling Mute MIC";
        _bodyLabel.Text = "Please wait while setup removes the application.";

        Progress<UninstallProgress> progress = new(UpdateProgress);

        try
        {
            await Task.Run(() => Uninstall(progress));
            string completeText = _hadInstallFolder
                ? "Mute MIC was uninstalled successfully."
                : "Mute MIC was already uninstalled. Remaining entries were cleaned up.";
            UpdateProgress(new UninstallProgress(100, completeText));
            _titleLabel.Text = _hadInstallFolder ? "Mute MIC uninstalled" : "Mute MIC already uninstalled";
            _bodyLabel.Text = completeText;
            _complete = true;
            _uninstallButton.Text = "Finish";
            _uninstallButton.Enabled = true;
            _cancelButton.Visible = false;
        }
        catch (Exception ex)
        {
            _titleLabel.Text = "Uninstall failed";
            _bodyLabel.Text = ex.Message;
            _statusLabel.Text = "Setup could not complete.";
            _uninstallButton.Text = "Close";
            _complete = true;
            _uninstallButton.Enabled = true;
            _cancelButton.Visible = false;
        }
        finally
        {
            _uninstalling = false;
        }
    }

    private void UpdateProgress(UninstallProgress progress)
    {
        _progressBar.Value = Math.Clamp(progress.Percent, 0, 100);
        _statusLabel.Text = progress.Message;
    }

    private void Uninstall(IProgress<UninstallProgress> progress)
    {
        _hadInstallFolder = Directory.Exists(_installDir);

        progress.Report(new UninstallProgress(8, "Stopping Mute MIC..."));
        StopKnownProcesses();

        progress.Report(new UninstallProgress(26, "Removing startup tasks..."));
        DeleteTask(AppName);
        DeleteTask(LegacyTaskName);

        progress.Report(new UninstallProgress(45, "Removing Start Menu shortcuts..."));
        DeleteShortcuts();

        progress.Report(new UninstallProgress(62, "Removing Windows Installed Apps entry and settings..."));
        DeleteRegistryKeys();

        progress.Report(new UninstallProgress(82, "Scheduling application file removal..."));
        ScheduleInstallFolderRemoval(_installDir);

        progress.Report(new UninstallProgress(94, "Finishing cleanup..."));
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
                    // The delayed folder cleanup retries after the uninstaller exits.
                }
            }
        }
    }

    private static void DeleteShortcuts()
    {
        string programs = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
        string startMenuFolder = Path.Combine(programs, AppName);

        foreach (string shortcut in new[]
        {
            Path.Combine(programs, $"{AppName}.lnk"),
            Path.Combine(programs, $"Uninstall {AppName}.lnk"),
            Path.Combine(startMenuFolder, $"{AppName}.lnk"),
            Path.Combine(startMenuFolder, $"Uninstall {AppName}.lnk")
        })
        {
            if (File.Exists(shortcut))
            {
                File.Delete(shortcut);
            }
        }

        if (Directory.Exists(startMenuFolder))
        {
            Directory.Delete(startMenuFolder, recursive: true);
        }
    }

    private static void DeleteRegistryKeys()
    {
        Registry.CurrentUser.DeleteSubKeyTree(@"Software\Mute MIC", throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(@"Software\MicMute", throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(UninstallRegistryPath, throwOnMissingSubKey: false);
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

        string command = $"for /l %i in (1,1,30) do @if exist \"{resolvedInstallDir}\" (rmdir /s /q \"{resolvedInstallDir}\" 2>nul && exit /b 0 || timeout /t 1 /nobreak >nul) else exit /b 0";
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

internal readonly record struct UninstallProgress(int Percent, string Message);
