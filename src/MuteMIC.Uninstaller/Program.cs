using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using MuteMIC.SetupUi;

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
    private const string StartupRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    private readonly string _language = SetupStrings.DetectLanguage();
    private readonly string _installDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        AppName);
    private readonly Label _titleLabel = new();
    private readonly Label _bodyLabel = new();
    private readonly Label _locationLabel = new();
    private readonly Label _statusLabel = new();
    private readonly SetupFieldPanel _locationPanel = new();
    private readonly SetupProgressBar _progressBar = new();
    private readonly SetupButton _uninstallButton = new();
    private readonly SetupButton _cancelButton = new();
    private bool _complete;
    private bool _uninstalling;
    private bool _hadInstallFolder;

    public UninstallerForm()
    {
        Text = T("UninstallerTitleBar");
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
        _titleLabel.Text = T("UninstallTitle");
        _titleLabel.Font = new Font("Segoe UI", 18F, FontStyle.Regular, GraphicsUnit.Point);
        _titleLabel.Location = new Point(28, 28);
        _titleLabel.Size = new Size(504, 36);
        _titleLabel.TextAlign = ContentAlignment.MiddleLeft;

        _bodyLabel.Text = T("UninstallBody");
        _bodyLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        _bodyLabel.Location = new Point(30, 72);
        _bodyLabel.Size = new Size(500, 42);
        _bodyLabel.AutoEllipsis = true;

        Label locationTitle = new()
        {
            Text = T("InstalledLocation"),
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

        _statusLabel.Text = T("ReadyToUninstall");
        _statusLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        _statusLabel.Location = new Point(30, 238);
        _statusLabel.Size = new Size(500, 34);

        _uninstallButton.Text = T("UninstallButton");
        _uninstallButton.Location = new Point(422, 304);
        _uninstallButton.Size = new Size(110, 36);
        _uninstallButton.Click += UninstallButton_Click;
        _cancelButton.Text = T("Cancel");
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
            _uninstallButton
        ]);
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
        _titleLabel.Text = T("UninstallingTitle");
        _bodyLabel.Text = T("UninstallingBody");

        Progress<UninstallProgress> progress = new(UpdateProgress);

        try
        {
            await Task.Run(() => Uninstall(progress));
            string completeText = _hadInstallFolder
                ? T("UninstalledStatus")
                : T("AlreadyUninstalledStatus");
            UpdateProgress(new UninstallProgress(100, completeText));
            _titleLabel.Text = _hadInstallFolder ? T("UninstalledTitle") : T("AlreadyUninstalledTitle");
            _bodyLabel.Text = completeText;
            _complete = true;
            _uninstallButton.Text = T("Finish");
            _uninstallButton.Enabled = true;
            _cancelButton.Visible = false;
        }
        catch (Exception ex)
        {
            _titleLabel.Text = T("UninstallFailedTitle");
            _bodyLabel.Text = ex.Message;
            _statusLabel.Text = T("SetupCouldNotComplete");
            _uninstallButton.Text = T("Close");
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
        _progressBar.ProgressValue = progress.Percent;
        _statusLabel.Text = progress.Message;
    }

    private void Uninstall(IProgress<UninstallProgress> progress)
    {
        _hadInstallFolder = Directory.Exists(_installDir);

        progress.Report(new UninstallProgress(8, T("StoppingApp")));
        StopKnownProcesses();

        progress.Report(new UninstallProgress(26, T("RemovingStartup")));
        DeleteStartupEntries();

        progress.Report(new UninstallProgress(45, T("RemovingShortcuts")));
        DeleteShortcuts();

        progress.Report(new UninstallProgress(62, T("RemovingRegistry")));
        DeleteRegistryKeys();

        progress.Report(new UninstallProgress(82, T("SchedulingRemoval")));
        ScheduleInstallFolderRemoval(_installDir);

        progress.Report(new UninstallProgress(94, T("FinishingCleanup")));
    }

    private string T(string key)
    {
        return SetupStrings.Get(_language, key);
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

    private static void DeleteStartupEntries()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(StartupRegistryPath, writable: true);
        key?.DeleteValue(AppName, throwOnMissingValue: false);
        key?.DeleteValue(LegacyTaskName, throwOnMissingValue: false);
        DeleteTask(AppName);
        DeleteTask(LegacyTaskName);
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

        string scriptPath = Path.Combine(Path.GetTempPath(), $"MuteMIC-Uninstall-{Guid.NewGuid():N}.ps1");
        string escapedInstallDir = resolvedInstallDir.Replace("'", "''", StringComparison.Ordinal);
        string escapedScriptPath = scriptPath.Replace("'", "''", StringComparison.Ordinal);
        string[] script =
        [
            "$ErrorActionPreference = 'SilentlyContinue'",
            $"$target = '{escapedInstallDir}'",
            "for ($i = 0; $i -lt 30 -and (Test-Path -LiteralPath $target); $i++) {",
            "    Remove-Item -LiteralPath $target -Recurse -Force",
            "    if (Test-Path -LiteralPath $target) { Start-Sleep -Seconds 1 }",
            "}",
            $"Remove-Item -LiteralPath '{escapedScriptPath}' -Force"
        ];
        File.WriteAllLines(scriptPath, script, Encoding.UTF8);

        Process.Start(new ProcessStartInfo("powershell.exe")
        {
            ArgumentList =
            {
                "-NoLogo",
                "-NoProfile",
                "-NonInteractive",
                "-ExecutionPolicy",
                "Bypass",
                "-WindowStyle",
                "Hidden",
                "-File",
                scriptPath
            },
            WorkingDirectory = Path.GetTempPath(),
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

internal readonly record struct UninstallProgress(int Percent, string Message);
